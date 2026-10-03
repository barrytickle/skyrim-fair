"""Render a built static crowd NIF, read back from disk, for visual review.

    blender --background --factory-startup --python tools/crowd/render_nif.py -- Clapping01 [--highlight ShapeName]

Loads the exported BSTriShapes (not the build scene) with their diffuse textures and the tints
stored in their shaders, and renders full-figure views plus close-ups of the hands and head.
With --highlight, that shape is drawn solid red to show how much of it is visible.
"""
import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import crowd_lib as cl  # noqa: E402
from build_crowd import camera, setup_render  # noqa: E402


def load(path, textures, highlight):
    nif = cl.NifFile(str(path))
    objs = []
    for sh in nif.shapes:
        me = bpy.data.meshes.new(sh.name)
        me.from_pydata([tuple(v) for v in sh.verts], [], [tuple(t) for t in sh.tris])
        uv = me.uv_layers.new(name='UVMap')
        for loop in me.loops:
            u, v = sh.uvs[loop.vertex_index]
            uv.data[loop.index].uv = (u, 1 - v)
        me.polygons.foreach_set('use_smooth', [True] * len(me.polygons))
        me.use_auto_smooth = True
        me.normals_split_custom_set_from_vertices([tuple(n) for n in sh.normals])
        mat = bpy.data.materials.new(sh.name)
        mat.use_nodes = True
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        bsdf = nodes['Principled BSDF']
        if sh.name == highlight:
            bsdf.inputs['Base Color'].default_value = (1, 0, 0, 1)
            mat.diffuse_color = (1, 0, 0, 1)
        else:
            img = textures.image(sh.shader._readtexture(nif._handle, sh._handle, 1))
            tex = nodes.new('ShaderNodeTexImage')
            tex.image = img
            out = tex.outputs['Color']
            p = sh.shader.properties
            tint = p.hairTintColor if p.Shader_Type == cl.SHADER_HAIR_TINT else p.skinTintColor if p.Shader_Type == cl.SHADER_SKIN_TINT else None
            if tint:
                mix = nodes.new('ShaderNodeMixRGB')
                mix.blend_type, mix.inputs['Fac'].default_value = 'MULTIPLY', 1
                mix.inputs['Color2'].default_value = (*tint, 1)
                links.new(out, mix.inputs['Color1'])
                out = mix.outputs['Color']
            links.new(out, bsdf.inputs['Base Color'])
            if sh.has_alpha_property:
                links.new(tex.outputs['Alpha'], bsdf.inputs['Alpha'])
                mat.blend_method = 'CLIP'
                mat.alpha_threshold = sh.alpha_property.properties.threshold / 255
        me.materials.append(mat)
        obj = bpy.data.objects.new(sh.name, me)
        bpy.context.scene.collection.objects.link(obj)
        objs.append(obj)
    return objs


def shot(path, eye, target, ortho):
    cam = camera('cam', eye, target, ortho)
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('recipe')
    ap.add_argument('--highlight')
    args = ap.parse_args(sys.argv[sys.argv.index('--') + 1:])
    recipe = cl.load_recipe(args.recipe)
    work = cl.ROOT / 'build/crowd' / args.recipe
    out = work / ('review_' + args.highlight if args.highlight else 'review')
    out.mkdir(parents=True, exist_ok=True)
    cl.clear_scene()
    scene = setup_render((700, 900))
    scene.display.shading.color_type = 'MATERIAL' if args.highlight else 'TEXTURE'
    objs = load(cl.ROOT / recipe['output'], cl.Textures(work / 'preview_resources'), args.highlight)
    # A ground plane at Z = 0, to show the feet rest on it.
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, 0))

    hands = next(o for o in objs if 'Hand' in o.name)
    head = next(o for o in objs if 'Head' in o.name and 'Brows' not in o.name)
    centre = lambda o: sum((Vector(v.co) for v in o.data.vertices), Vector()) / len(o.data.vertices)
    for label, d in [('front', (0, 1, 0)), ('threequarter', (0.7, 0.7, 0)), ('side', (1, 0, 0)),
                     ('back', (0, -1, 0)), ('otherside', (-1, 0, 0))]:
        shot(out / f'{label}.png', Vector(d) * 300 + Vector((0, 0, 68)), (0, 0, 68), 150)
    for label, o, size in [('hands', hands, 45), ('head', head, 40)]:
        c = centre(o)
        for view, d in [('front', (0, 1, 0.1)), ('side', (1, 0.3, 0.1)), ('top', (0.2, 0.4, 1))]:
            shot(out / f'{label}_{view}.png', c + Vector(d).normalized() * 200, c, size)
    shot(out / 'feet.png', Vector((0.8, 1, 0.05)).normalized() * 300 + Vector((0, 0, 8)), (0, 0, 8), 50)
    print('REVIEW', out)


main()
