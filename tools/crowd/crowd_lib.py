"""Static crowd figures: pose vanilla Skyrim actor meshes and bake them into STAT geometry.

Runs inside Blender 3.6 (the project's Skyrim Blender). PyNifly's standalone NIF library and
HKX codec are used for file I/O only; the PyNifly Blender add-on itself is not installed.

The pipeline for one figure (see docs/CROWD.md):
  skeleton.hkx + an HKX idle -> Blender armature posed at one frame
  vanilla body, head and outfit NIFs -> Blender meshes skinned to that armature
  apply the Armature modifiers -> plain posed geometry, armature deleted
  plain BSTriShapes under a BSFadeNode -> a Skyrim SE static NIF, no skin, no animation
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

NIFLY = Path(r'C:\Users\Barry\Downloads\Skyrim Actors\crowd-tools\pynifly\io_scene_nifly')
ACTORS = Path(r'C:\Users\Barry\Downloads\Skyrim Actors\meshes')
CLOTHING = Path(r'C:\Users\Barry\Downloads\Skyrim Clothing\meshes')
DATA = Path(r'E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data')
ROOT = Path(__file__).resolve().parents[2]
sys.path[:0] = [str(NIFLY), str(NIFLY / 'hkx'), str(ROOT / 'tools')]

import pyn.pynifly as pynifly  # noqa: E402
from pyn.pynifly import NifFile  # noqa: E402
from pyn.nifdefs import NiShapeBuf, PynBufferTypes, AlphaPropertyBuf  # noqa: E402
from anim_skyrim import load_skyrim_skeleton, load_skyrim_animation  # noqa: E402

SKELETON = ACTORS / 'actors/character/character assets/skeleton.hkx'
ANIMATIONS = ACTORS / 'actors/character/animations'

SLSF1_SKINNED = 0x2
SLSF1_FACEGEN_DETAIL = 0x400
SLSF1_MODEL_SPACE_NORMALS = 0x1000
SHADER_DEFAULT, SHADER_SKIN_TINT, SHADER_FACE_TINT, SHADER_HAIR_TINT = 0, 5, 4, 6
FLAT_NORMAL = r'textures\default_n.dds'
# Past this many degrees between a skin vertex's bind and posed orientation, its model-space
# normal map would light it from the wrong side, so the shape falls back to the flat normal.
MSN_MAX_ROTATION = 20.0


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def hkx_matrix(t, q, s):
    return Matrix.LocRotScale(Vector(t), Quaternion((q[3], q[0], q[1], q[2])), Vector(s))


def nif_matrix(t):
    m = Matrix([list(t.rotation[0]), list(t.rotation[1]), list(t.rotation[2])]).to_4x4()
    for i in range(3):
        for j in range(3):
            m[i][j] *= t.scale
    m.translation = Vector(t.translation)
    return m


class Skeleton:
    """The vanilla humanoid skeleton.hkx and its world matrices for the rest pose or a frame."""

    def __init__(self, path=SKELETON):
        self.path = Path(path)
        self.hkx = load_skyrim_skeleton(str(self.path))
        self.bones = self.hkx.bones
        self.parents = self.hkx.parents
        self.index = {n: i for i, n in enumerate(self.bones)}
        self.rest = self.worlds([hkx_matrix(p.translation, p.rotation, p.scale) for p in self.hkx.reference_pose])

    def worlds(self, local):
        out = []
        for i, m in enumerate(local):
            out.append(m if self.parents[i] < 0 else out[self.parents[i]] @ m)
        return out

    def pose(self, anim, frame):
        # Skyrim idles carry one track per skeleton bone, in skeleton order (their names are blank).
        if anim.num_tracks != len(self.bones):
            raise ValueError(f'{anim.num_tracks} tracks for {len(self.bones)} bones')
        return self.worlds([hkx_matrix(t.translations[frame], t.rotations[frame], t.scales[frame]) for t in anim.tracks])


def resolve_part(rel, weight):
    """A vanilla NIF path, with '{w}' standing for the _0/_1 weight variant."""
    if '{w}' not in rel:
        return [(CLOTHING / rel, 1.0)]
    lo, hi = CLOTHING / rel.format(w='0'), CLOTHING / rel.format(w='1')
    return [(lo, 1.0 - weight), (hi, weight)]


class Part:
    """One vanilla shape: geometry, skin, shader and textures as read from its NIF."""

    def __init__(self, role, shape, variants, skeleton):
        self.role, self.name = role, shape.name
        self.sources = [str(p) for p, _ in variants]
        self.tris = [tuple(t) for t in shape.tris]
        self.uvs = [tuple(u) for u in shape.uvs]
        self.verts = [Vector(v) for v in shape.verts]
        self.normals = [Vector(n) for n in shape.normals] if shape.normals else None
        self.shader_name = shape.shader_block_name
        self.shader = shape.shader.properties.copy()
        # All nine texture-set slots by index: PyNifly's named view skips slots whose flag is unset.
        self.slots = [shape.shader._readtexture(shape.file._handle, shape._handle, i) for i in range(1, 10)]
        self.alpha = None
        if shape.has_alpha_property:
            a = shape.alpha_property.properties
            self.alpha = (a.flags, a.threshold)
        self.skin_to_bone = {b: nif_matrix(shape.get_shape_skin_to_bone(b)) for b in shape.bone_names}
        self.weights = [dict() for _ in self.verts]
        for bone, pairs in shape.bone_weights.items():
            for v, w in pairs:
                if w > 0:
                    self.weights[v][bone] = w
        # Rest-space placement: bone rest @ skin-to-bone must agree across the shape's bones.
        binds = {b: skeleton.rest[skeleton.index[b]] @ s for b, s in self.skin_to_bone.items()}
        heaviest = max(binds, key=lambda b: sum(w.get(b, 0) for w in self.weights))
        self.bind = binds[heaviest]
        self.bind_spread = max(max(abs(x) for r in (m - self.bind) for x in r) for m in binds.values())

    def blend(self, shape, t):
        """Mix in another weight variant of the same shape (Skyrim's _0/_1 body weight lerp)."""
        if len(shape.verts) != len(self.verts):
            raise ValueError(f'{self.name}: weight variants differ in vertex count')
        self.verts = [a.lerp(Vector(b), t) for a, b in zip(self.verts, shape.verts)]
        if self.normals and shape.normals:
            self.normals = [a.lerp(Vector(b), t).normalized() for a, b in zip(self.normals, shape.normals)]


def load_parts(recipe, skeleton):
    parts = []
    for role, rel in recipe['parts'].items():
        variants = resolve_part(rel, recipe['weight'])
        nifs = [NifFile(str(p)) for p, _ in variants]
        for i, shape in enumerate(nifs[0].shapes):
            part = Part(role, shape, variants, skeleton)
            if len(nifs) == 2:
                part.blend(nifs[1].shapes[i], variants[1][1])
            parts.append(part)
    return parts


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def build_armature(skeleton, name='SkyrimSkeleton'):
    arm = bpy.data.armatures.new(name)
    obj = bpy.data.objects.new(name, arm)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    edit = []
    for i, bone in enumerate(skeleton.bones):
        eb = arm.edit_bones.new(bone)
        eb.head, eb.tail = (0, 0, 0), (0, 2, 0)
        eb.matrix = skeleton.rest[i]
        edit.append(eb)
    for i, p in enumerate(skeleton.parents):
        if p >= 0:
            edit[i].parent = edit[p]
    bpy.ops.object.mode_set(mode='OBJECT')
    return obj


def pose_armature(rig, skeleton, worlds):
    """Pose every bone so its armature-space matrix is the HKX frame's world matrix."""
    for i, bone in enumerate(skeleton.bones):
        rest = rig.data.bones[bone].matrix_local
        p = skeleton.parents[i]
        if p < 0:
            rest_local, pose_local = rest, worlds[i]
        else:
            rest_local = rig.data.bones[skeleton.bones[p]].matrix_local.inverted() @ rest
            pose_local = worlds[p].inverted() @ worlds[i]
        rig.pose.bones[bone].matrix_basis = rest_local.inverted() @ pose_local
    bpy.context.view_layer.update()


def preview_material(part, textures):
    mat = bpy.data.materials.new(f'{part.role}:{part.name}')
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value = 0.8
    diffuse = part.slots[0]
    img = textures.image(diffuse) if diffuse else None
    if img:
        tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex.image = img
        mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
        if part.alpha:
            mat.node_tree.links.new(tex.outputs['Alpha'], bsdf.inputs['Alpha'])
            mat.blend_method = 'CLIP'
            mat.alpha_threshold = part.alpha[1] / 255
    tint = None
    if part.shader.Shader_Type == SHADER_HAIR_TINT:
        tint = part.shader.hairTintColor
    elif part.shader.Shader_Type in (SHADER_SKIN_TINT, SHADER_FACE_TINT):
        tint = part.shader.skinTintColor
    if img and tint:
        mix = mat.node_tree.nodes.new('ShaderNodeMixRGB')
        mix.blend_type, mix.inputs['Fac'].default_value = 'MULTIPLY', 1.0
        mix.inputs['Color2'].default_value = (tint[0], tint[1], tint[2], 1)
        mat.node_tree.links.new(tex.outputs['Color'], mix.inputs['Color1'])
        mat.node_tree.links.new(mix.outputs['Color'], bsdf.inputs['Base Color'])
    return mat


def build_mesh(part, rig, textures):
    me = bpy.data.meshes.new(f'{part.role}:{part.name}')
    me.from_pydata([part.bind @ v for v in part.verts], [], part.tris)
    uv = me.uv_layers.new(name='UVMap')
    for loop in me.loops:
        u, v = part.uvs[loop.vertex_index]
        uv.data[loop.index].uv = (u, 1.0 - v)
    me.polygons.foreach_set('use_smooth', [True] * len(me.polygons))
    me.materials.append(preview_material(part, textures))
    obj = bpy.data.objects.new(me.name, me)
    bpy.context.scene.collection.objects.link(obj)
    groups = {b: obj.vertex_groups.new(name=b) for b in part.skin_to_bone}
    for v, ws in enumerate(part.weights):
        for b, w in ws.items():
            groups[b].add([v], w, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new('Armature', 'ARMATURE')
    mod.object, mod.use_vertex_groups, mod.use_deform_preserve_volume = rig, True, False
    obj['part'] = part.role
    return obj


def bake(objects, rig):
    """Apply each Armature modifier, strip skinning, and delete the armature."""
    for obj in objects:
        with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj]):
            bpy.ops.object.modifier_apply(modifier='Armature')
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
        obj.vertex_groups.clear()
    bpy.data.objects.remove(rig)


def skinned(part, skeleton, worlds):
    """Independent linear-blend skin of the source data, used to check Blender's bake and for normals."""
    mats = {b: worlds[skeleton.index[b]] @ s for b, s in part.skin_to_bone.items()}
    verts, normals, rotation = [], [], []
    for i, ws in enumerate(part.weights):
        total = sum(ws.values())
        p, n, ang = Vector(), Vector(), 0.0
        for b, w in ws.items():
            p += w / total * (mats[b] @ part.verts[i])
            if part.normals:
                n += w / total * (mats[b].to_3x3() @ part.normals[i])
            bind_rot = (skeleton.rest[skeleton.index[b]] @ part.skin_to_bone[b]).to_quaternion()
            ang += w / total * math.degrees(bind_rot.rotation_difference(mats[b].to_quaternion()).angle)
        verts.append(p)
        normals.append(n.normalized() if part.normals else None)
        rotation.append(ang)
    return verts, normals, rotation


class Textures:
    """Loads vanilla textures from the stock BSAs for the Blender preview only (never copied to the mod)."""

    def __init__(self, cache):
        import bsa_extract
        self.cache, self.bsa, self.index, self.missing = Path(cache), bsa_extract, None, []

    def image(self, rel):
        key = rel.replace('\\', '/').lower()
        dest = self.cache / key
        if not dest.exists():
            if self.index is None:
                self.index = self.bsa.build_index(str(DATA))
            if key in self.index:
                dest.parent.mkdir(parents=True, exist_ok=True)
                dest.write_bytes(self.bsa.extract(self.index[key]))
            elif (DATA / key).exists():
                dest = DATA / key
            else:
                self.missing.append(key)
                return None
        return bpy.data.images.load(str(dest), check_existing=True)


def ground_offset(parts, baked, skeleton, worlds):
    """Translation putting the feet on Z = 0 with the origin centred between them."""
    lowest = min(v.z for part, verts in zip(parts, baked) if part.role == 'boots' for v in verts)
    feet = [worlds[skeleton.index[b]].translation for b in ('NPC L Foot [Lft ]', 'NPC R Foot [Rft ]')]
    mid = (feet[0] + feet[1]) / 2
    return Vector((-mid.x, -mid.y, -lowest))


def export_static(path, parts, baked, normals, root_name):
    """Write plain BSTriShapes under a BSFadeNode: no skin instance, no partitions, no controllers."""
    nif = NifFile()
    nif.initialize('SKYRIMSE', str(path), 'BSFadeNode', root_name)
    for part, verts, norms in zip(parts, baked, normals):
        props = NiShapeBuf()
        props.bufType = PynBufferTypes.BSTriShapeBufType
        shape = nif.createShapeFromData(part.name, [tuple(v) for v in verts], part.tris, part.uvs,
                                        [tuple(n) for n in norms] if norms[0] is not None else None,
                                        props=props, parent=nif.rootNode)
        shape.shader.name = part.shader_name
        shape.shader._properties = part.export_shader
        shape.save_shader_attributes()
        for slot, tex in enumerate(part.export_slots):
            if tex:
                pynifly.nifly.setShaderTextureSlot(nif._handle, shape._handle, slot, tex.encode('utf-8'))
        if part.alpha:
            shape.has_alpha_property = True
            shape.alpha_property.properties.flags = part.alpha[0]
            shape.alpha_property.properties.threshold = part.alpha[1]
            shape.save_alpha_property()
    nif.save()


def static_shader(part, max_rotation, tints):
    """Shader for an unskinned copy: Skinned flag off; skin loses its model-space normals if posed far."""
    s = part.shader.copy()
    slots = list(part.slots)
    s.Shader_Flags_1 &= ~SLSF1_SKINNED
    notes = []
    if s.Shader_Type == SHADER_FACE_TINT:
        # FaceGen tint masks exist only for named NPCs; a generic head is skin-tinted like the hands.
        s.Shader_Type = s.bslspShaderType = SHADER_SKIN_TINT
        s.Shader_Flags_1 &= ~SLSF1_FACEGEN_DETAIL
        slots[6] = ''  # the FaceGen detail map
        notes.append('FaceTint -> SkinTint, FaceGen detail map dropped')
    if tints and s.Shader_Type == SHADER_SKIN_TINT:
        s.skinTintColor[:] = tints['skin']
        s.Skin_Tint_Alpha = 1.0
    if tints and 'hair' in tints and s.Shader_Type == SHADER_HAIR_TINT:
        s.hairTintColor[:] = tints['hair']
    if s.Shader_Flags_1 & SLSF1_MODEL_SPACE_NORMALS and max_rotation > MSN_MAX_ROTATION:
        s.Shader_Flags_1 &= ~SLSF1_MODEL_SPACE_NORMALS
        slots[1] = FLAT_NORMAL
        notes.append(f'model-space normals -> {FLAT_NORMAL} (posed {max_rotation:.0f} deg from bind)')
    part.export_shader, part.export_slots = s, slots
    return notes


def load_recipe(name):
    return json.loads((ROOT / 'tools/crowd/recipes' / f'{name}.json').read_text())
