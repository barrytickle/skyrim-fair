"""Builds the Skyrim Fair landscaped-foundation tile kit.

Run headless with the portable Blender 3.6 build:

    blender.exe --background --python assets/blender/build_foundation_kit.py

Everything here is project-owned geometry authored for Skyrim Fair. No
third-party mesh or texture data is copied or referenced.

Units: 1 Blender unit == 1 Skyrim unit, built 1:1 and exported without unit
scaling. Scale must be confirmed on first Creation Kit import.

Pivot conventions, chosen so the Mutagen generator needs no offset arithmetic:

  * floor and ramp tiles  - pivot at the centre of the TOP face, so placing a
    reference at the platform floor Z puts the walking surface exactly there
  * retaining pieces      - pivot at the top OUTER edge, so the piece hangs
    below the floor plane and any surplus height buries in terrain
  * retaining corner      - pivot at the top outer corner
  * shoulder wedge        - pivot at the thick end, top face

For retaining, ramp and shoulder pieces, +Y points AWAY from the platform
centre (outward / downhill).
"""

import os
import sys

import bpy
from mathutils import Vector

# --- scale --------------------------------------------------------------------
#
# Measured empirically from the NIFs AssetWatcher produced: 1 Blender unit comes
# out as exactly 40 Skyrim units, on both the visual mesh and the Havok collision.
# Blender's scene unit settings do not affect this - building with system "NONE"
# and with Bethesda's recommended IMPERIAL/INCHES/1 gave identical output - so the
# factor lives in the FBX to NIF conversion itself.
#
# Every dimension below is therefore written in readable Skyrim units and divided
# by this factor at mesh-construction time.
BLENDER_UNITS_PER_SKYRIM_UNIT = 1.0 / 40.0

# --- kit dimensions, all in Skyrim units -------------------------------------

FLOOR_THICKNESS = 32
# One course. Measured exposure round the approved footprint is 192-288, so the
# generator stacks two courses on the deeper edges rather than this being a
# single-piece cover as it was when the floor sat only 160 above grade.
RETAIN_HEIGHT = 256
RETAIN_DEPTH = 128
RAMP_RUN = 512
# 1:4.3 grade. Sized against the GROUND at the ramp foot, not the road mesh on top of
# it - the road sits about 20 units proud of the ground it is laid on, and targeting the
# mesh left the foot floating 48 to 104. Over the 1,536 run now available, 120 per tile
# lands the foot within 32 units of grade on both lanes.
RAMP_RISE = 120
# Deep enough that the underside never surfaces. This is a measured figure, not a
# guess: the ramp head stands 288 units above native ground on its western flank, so
# at 256 the slab's underside sat 32 units clear of the ground and you could see
# daylight under the entrance. 384 buries it along the whole run with margin.
RAMP_DEPTH = 384
# Hidden collision under the vanilla staircase. StonewallTerraceStairs01 carries a
# bhkCompressedMeshShape, and those do not scale reliably with a reference's XSCL in
# Skyrim: at scale 1.0 the stair was climbable, and it stopped being climbable once it
# was scaled. Rather than give up a stair that looks right, a smooth box-collider slope
# is laid under the treads. Box shapes scale predictably.
#
# The slope is the MESH's own 112-over-192, which is independent of scale, so one piece
# placed per flight at the stair's scale always lines up.
STAIRCOL_RUN = 192
STAIRCOL_RISE = 112
STAIRCOL_HALF_X = 80        # inside the 167-wide stair gap, so it never protrudes
STAIRCOL_SINK = 24          # visible slab sits this far under the tread line
# A smooth diagonal through the step noses falls by one complete 14-unit riser
# across each flat tread. Lift the collision plane by that riser height so it is
# never below the visible walking surface; otherwise boots sink into every tread.
STAIRCOL_LIFT = 14
# Must exceed SINK + RISE (136) or the sloped top dips below the base at the far
# end and the box turns itself inside out - the orientation guard caught exactly that.
STAIRCOL_DEPTH = 192

# Closed project-authored stair cheeks.  Their top profile uses the same 8 steps
# as the stair flight, so each section descends with the nosing instead of
# standing up as a terrain-dependent vanilla wall segment.  The solid continues
# below that profile so no underside or missing back can be exposed.
STAIR_CHEEK_HALF_X = 24
STAIR_CHEEK_DEPTH = 128

# The 512-wide perimeter segment at the stair head needs a structural face on
# both sides of the 217-wide opening.  Two 144-wide wings leave a few units of
# tolerance around the stair at its configured 1.3 scale.
ENTRANCE_WING_HALF_X = 72

# Project-authored staircase, replacing the vanilla StonewallTerraceStairs01 flight
# whose 666-wide drystone wall read as a gatehouse. Same 112 rise over 192 run and
# same 167 width as the vanilla flight at scale 1.0, so every placement the generator
# already does for the chain, and the box collider, carry over unchanged. Eight steps
# of 14 rise - a real step, not the vanilla piece's 7-unit ripples.
STAIR_RUN = 192
STAIR_RISE = 112
STAIR_HALF_X = 84
STAIR_STEPS = 8
STAIR_DEPTH = 192           # solid below the treads, buried in the bank

SHOULDER_RUN = 256
SHOULDER_THICKNESS = 32
# Deliberate A/B gate.  Keep the project-owned material build in the repository,
# but put the current in-game terrace on vanilla Whiterun stone for comparison.
PAVING_MATERIAL_MODE = os.environ.get(
    "SKYRIM_FAIR_PAVING_MATERIAL", "vanilla_road_dirt")
_MODES = {"vanilla_road_dirt", "vanilla_whiterun_test", "project_cobble"}
if PAVING_MATERIAL_MODE not in _MODES:
    raise ValueError(f"SKYRIM_FAIR_PAVING_MATERIAL must be one of {sorted(_MODES)}")
PAVING_TEXTURE_PERIOD = 1024 if PAVING_MATERIAL_MODE == "project_cobble" else 256
PAVING_PHASE_STEP = 0.5 if PAVING_MATERIAL_MODE == "project_cobble" else 0.0


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.objects):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def make_object(name, verts, faces):
    """Vertices arrive in Skyrim units and are converted to Blender units here."""
    s = BLENDER_UNITS_PER_SKYRIM_UNIT
    verts = [(x * s, y * s, z * s) for x, y, z in verts]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.validate()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def make_paving_material():
    """BGS lighting material for the current paving target.

    The default is worn earth, not stone. WRStoneFloor02 is the material Bethesda
    uses for a stone CITY floor, and at fairground scale that is exactly why the
    terrace read as a slab of concrete: one city-plaza texture over 8.65 million
    square units. A fair pitched on prepared ground is earth and gravel, and stone
    belongs later on the main avenue and stage square where traffic would wear it in.

    road01 measures about 267 units per repeat on the shipped road meshes. This maps
    it at 256, within four per cent of native and an exact divisor of the 512 tile
    grid, so the surface stays continuous from tile to tile.
    """
    if PAVING_MATERIAL_MODE == "vanilla_road_dirt":
        material = bpy.data.materials.new("SkyrimFair_RoadDirt01")
        props = material.bgs_props
        props.texture_diffuse = r"textures\landscape\roads\road01.dds"
        props.texture_normal = r"textures\landscape\roads\road01_n.dds"
        props.texture_height = ""
        props.clamp_mode = "WRAP_S_WRAP_T"
        props.parallax_enabled = False
        props.parallax_occlusion_enabled = False
        props.model_space_normals = False
        # Matte. Compacted earth should never catch a highlight the way stone does.
        props.shininess = 20.0
        props.specular_enabled = True
        props.specular_mult = 0.2
        props.specular_color = (1.0, 1.0, 1.0)
        props.vertex_colors_enabled = False
        return material

    if PAVING_MATERIAL_MODE == "vanilla_whiterun_test":
        material = bpy.data.materials.new("SkyrimFair_WRStoneFloor02_Test")
        props = material.bgs_props
        props.texture_diffuse = r"textures\architecture\whiterun\WRStoneFloor02.dds"
        props.texture_normal = r"textures\architecture\whiterun\WRStoneFloor02_n.dds"
        props.texture_height = ""
        props.clamp_mode = "WRAP_S_WRAP_T"
        props.parallax_enabled = False
        props.parallax_occlusion_enabled = False
        props.model_space_normals = False
        # Match the audited WRMainRoadMarket vanilla shader values.
        props.shininess = 80.0
        props.specular_enabled = True
        props.specular_mult = 1.0
        props.specular_color = (1.0, 1.0, 1.0)
        # The audited vanilla mesh has a vertex-colour layer and this one does not,
        # so the flag is deliberately not copied across; claiming colours that are
        # absent leaves the shader reading undefined data.
        props.vertex_colors_enabled = False
        return material

    material = bpy.data.materials.new("SkyrimFair_Cobble01")
    props = material.bgs_props
    props.texture_diffuse = r"textures\SkyrimFair\SkyrimFair_Cobble01.dds"
    props.texture_normal = r"textures\SkyrimFair\SkyrimFair_Cobble01_n.dds"
    props.texture_height = r"textures\SkyrimFair\SkyrimFair_Cobble01_p.dds"
    props.clamp_mode = "WRAP_S_WRAP_T"
    props.parallax_enabled = True
    props.parallax_occlusion_enabled = True
    props.model_space_normals = False
    props.shininess = 18.0
    props.specular_enabled = True
    props.specular_mult = 0.22
    props.specular_color = (0.45, 0.43, 0.39)
    props.vertex_colors_enabled = False
    return material


def make_landscape_material(name, diffuse, normal, shininess, spec_mult):
    """Vanilla landscape material, referenced by game path and never redistributed.

    The structural slab, retaining faces and verge wedge previously shipped with no
    material at all, so they rendered with the engine default: the flat lavender
    surfaces visible all over the terrace. They are meant to be hidden behind rock
    and cliff dressing, but anything that does peek through should read as earth or
    grass rather than as a missing texture.
    """
    material = bpy.data.materials.new(name)
    props = material.bgs_props
    props.texture_diffuse = diffuse
    props.texture_normal = normal
    props.texture_height = ""
    props.clamp_mode = "WRAP_S_WRAP_T"
    props.parallax_enabled = False
    props.parallax_occlusion_enabled = False
    props.model_space_normals = False
    props.shininess = shininess
    props.specular_enabled = True
    props.specular_mult = spec_mult
    props.specular_color = (1.0, 1.0, 1.0)
    props.vertex_colors_enabled = False
    return material


def apply_paving_material(obj, material, u_phase=0.0, v_phase=0.0, period=None):
    """World-scale UVs; phases let 512u kit pieces share one larger texture tile."""
    period = PAVING_TEXTURE_PERIOD if period is None else period
    obj.data.materials.append(material)
    uv_layer = obj.data.uv_layers.new(name="UVMap")
    inv = 1.0 / BLENDER_UNITS_PER_SKYRIM_UNIT
    coords = [v.co * inv for v in obj.data.vertices]
    min_x = min(v.x for v in coords)
    min_y = min(v.y for v in coords)
    min_z = min(v.z for v in coords)
    for polygon in obj.data.polygons:
        normal = polygon.normal
        for loop_index in polygon.loop_indices:
            vertex = coords[obj.data.loops[loop_index].vertex_index]
            if abs(normal.z) >= max(abs(normal.x), abs(normal.y)):
                u = (vertex.x - min_x) / period + u_phase
                v = (vertex.y - min_y) / period + v_phase
            elif abs(normal.x) >= abs(normal.y):
                u = (vertex.y - min_y) / period + v_phase
                v = (vertex.z - min_z) / period
            else:
                u = (vertex.x - min_x) / period + u_phase
                v = (vertex.z - min_z) / period
            uv_layer.data[loop_index].uv = (u, v)


def box(name, x0, x1, y0, y1, z0, z1, open_top=False):
    """Axis-aligned box with every face wound OUTWARD.

    The winding is not cosmetic. Every face of every piece in this kit used to be
    wound inward, which makes the whole solid render inside-out: the walking
    surface becomes a back face and is culled, and what you actually see from
    above is the inside of the slab's underside, 32 units lower, framed by the
    rim of the side faces. That is invisible on untextured grey geometry and
    glaring the moment a normal-mapped stone texture is applied.

    open_top drops the upward face. Used for structural pieces that are capped by
    a separate thin visual layer, so the two never fight over the same plane.
    """
    verts = [
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1),
    ]
    faces = [
        (3, 2, 1, 0),   # bottom, -Z
        (0, 1, 5, 4),   # -Y
        (1, 2, 6, 5),   # +X
        (2, 3, 7, 6),   # +Y
        (3, 0, 4, 7),   # -X
    ]
    if not open_top:
        faces.append((4, 5, 6, 7))   # top, +Z
    return make_object(name, verts, faces)


def sloped_box(name, x0, x1, y0, y1, z_bottom, z_near, z_far, open_top=False):
    """Box whose top face slopes from z_near at y0 down to z_far at y1.

    Same outward winding and same open_top contract as box().
    """
    verts = [
        (x0, y0, z_bottom), (x1, y0, z_bottom), (x1, y1, z_bottom), (x0, y1, z_bottom),
        (x0, y0, z_near), (x1, y0, z_near), (x1, y1, z_far), (x0, y1, z_far),
    ]
    faces = [
        (3, 2, 1, 0),   # bottom, -Z
        (0, 1, 5, 4),   # -Y
        (1, 2, 6, 5),   # +X
        (2, 3, 7, 6),   # +Y
        (3, 0, 4, 7),   # -X
    ]
    if not open_top:
        faces.append((4, 5, 6, 7))   # sloped top, up and tilted outward
    return make_object(name, verts, faces)


def wedge(name, half_x, run, thickness):
    """Triangular prism tapering from `thickness` at y=0 to a knife edge at y=run."""
    verts = [
        (-half_x, 0, 0), (half_x, 0, 0),
        (-half_x, 0, -thickness), (half_x, 0, -thickness),
        (-half_x, run, -thickness), (half_x, run, -thickness),
    ]
    faces = [
        (2, 3, 1, 0),   # thick end, -Y
        (1, 5, 4, 0),   # sloped top, up
        (4, 5, 3, 2),   # underside, -Z
        (4, 2, 0),      # left, -X
        (3, 5, 1),      # right, +X
    ]
    return make_object(name, verts, faces)


def stair(name, half_x, run, rise, steps, depth):
    """Stepped solid: treads descend in +Y, top tread at the origin, closed all round.

    Non-convex, so it is built as a profile swept across X and then handed to
    Blender's normal recalculation, which is reliable for a closed manifold. The
    orientation assertion in main() knows to treat it separately.
    """
    t = run / steps
    r = rise / steps
    profile = []
    for i in range(steps):
        profile.append((i * t, -i * r))
        profile.append(((i + 1) * t, -i * r))
    profile.append((run, -depth))
    profile.append((0.0, -depth))
    n = len(profile)
    verts = [(-half_x, y, z) for y, z in profile] + [(half_x, y, z) for y, z in profile]
    faces = [tuple(range(n)), tuple(reversed(range(n, 2 * n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    obj = make_object(name, verts, faces)
    select_only(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


def stair_cheek(name, half_x, run, rise, steps, depth):
    """Closed drystone cheek whose crest follows the staircase step for step.

    The lower edge descends in parallel with the flight, keeping a substantial
    retaining body beneath every tread.  Unlike a raised vanilla field-wall
    reference, this has a closed back, ends and underside and therefore never
    depends on native terrain to hide empty space.
    """
    t = run / steps
    r = rise / steps
    profile = []
    for i in range(steps):
        profile.append((i * t, -i * r))
        profile.append(((i + 1) * t, -i * r))
    profile.append((run, -rise - depth))
    profile.append((0.0, -depth))
    n = len(profile)
    verts = [(-half_x, y, z) for y, z in profile] + [(half_x, y, z) for y, z in profile]
    faces = [tuple(range(n)), tuple(reversed(range(n, 2 * n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    obj = make_object(name, verts, faces)
    select_only(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.mesh.quads_convert_to_tris(quad_method="BEAUTY", ngon_method="BEAUTY")
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


def apply_split_material(obj, top, sides, period):
    """Treads take one material, risers and flanks another. World-scale UVs as usual."""
    obj.data.materials.append(top)
    obj.data.materials.append(sides)
    uv_layer = obj.data.uv_layers.new(name="UVMap")
    inv = 1.0 / BLENDER_UNITS_PER_SKYRIM_UNIT
    coords = [v.co * inv for v in obj.data.vertices]
    min_x = min(v.x for v in coords)
    min_y = min(v.y for v in coords)
    min_z = min(v.z for v in coords)
    for polygon in obj.data.polygons:
        normal = polygon.normal
        polygon.material_index = 0 if normal.z > 0.5 else 1
        for loop_index in polygon.loop_indices:
            vertex = coords[obj.data.loops[loop_index].vertex_index]
            if abs(normal.z) >= max(abs(normal.x), abs(normal.y)):
                u = (vertex.x - min_x) / period
                v = (vertex.y - min_y) / period
            elif abs(normal.x) >= abs(normal.y):
                u = (vertex.y - min_y) / period
                v = (vertex.z - min_z) / period
            else:
                u = (vertex.x - min_x) / period
                v = (vertex.z - min_z) / period
            uv_layer.data[loop_index].uv = (u, v)


def quad(name, corners):
    """Single upward-facing polygon: a visual surface with no thickness.

    A cap has no sides, so tiles laid edge to edge cannot show a vertical face
    between them, and the paving material cannot appear on anything but the
    walking surface.
    """
    return make_object(name, list(corners), [(0, 1, 2, 3)])


def build_kit():
    """The kit, split into structural bodies and thin top-only visual layers.

    Construction method, settled after the first textured in-game test:

      * a structural body carries collision and the outer earth/rock faces, and
        has NO upward face at all;
      * a separate zero-thickness cap carries the paved walking surface and has
        no collision.

    Two problems drove this. Paving material was reaching vertical faces, because
    the structural box was textured on all six sides; and every tile boundary
    showed a raised vertical edge, because what the player could actually see was
    the rim of each slab's side faces. With the cap architecture the only
    upward-facing surface in the whole terrace is a flat plane, so adjacent tiles
    abut with no vertical face between them and the floor reads as continuous.
    """
    pieces = {}
    visual_only = set()
    nonconvex = set()
    paving = make_paving_material()
    earth = make_landscape_material(
        "SkyrimFair_DirtCliffs01_Structural",
        r"textures\landscape\dirtcliffs\dirtcliffs01.dds",
        r"textures\landscape\dirtcliffs\dirtcliffs01_n.dds",
        shininess=30.0, spec_mult=0.15)
    grass = make_landscape_material(
        "SkyrimFair_FieldGrass02_Verge",
        r"textures\landscape\fieldgrass02.dds",
        r"textures\landscape\fieldgrass02_n.dds",
        shininess=20.0, spec_mult=0.1)

    # Stone for the staircase: Whiterun flagstones on the treads, farmhouse drystone
    # on the risers and flanks so it ties into the Stonewall01 cheek walls beside it.
    # Both vanilla paths; replacers win at runtime as everywhere else.
    steps_top = make_landscape_material(
        "SkyrimFair_WRStoneFloor02_Steps",
        r"textures\architecture\whiterun\WRStoneFloor02.dds",
        r"textures\architecture\whiterun\WRStoneFloor02_n.dds",
        shininess=40.0, spec_mult=0.3)
    steps_side = make_landscape_material(
        "SkyrimFair_StoneWall01_Steps",
        r"textures\architecture\farmhouse\StoneWall01.dds",
        r"textures\architecture\farmhouse\StoneWall01_n.dds",
        shininess=30.0, spec_mult=0.2)

    # Phase variants only exist to stop a small texture period stamping visibly.
    # The vanilla Whiterun floor tiles at 256 units, which divides the 512 grid
    # exactly, so a phase offset there would break continuity rather than help.
    phase = PAVING_PHASE_STEP
    if phase == 0.0:
        variants = (("", 0.0, 0.0),)
    else:
        variants = (("", 0.0, 0.0), ("_U1", phase, 0.0),
                    ("_V1", 0.0, phase), ("_U1V1", phase, phase))

    # 1. structural fill body - collision and outer faces, no top
    pieces["SkyrimFair_FloorFill_1024"] = box(
        "SkyrimFair_FloorFill_1024", -512, 512, -512, 512, -FLOOR_THICKNESS, 0,
        open_top=True)
    apply_paving_material(pieces["SkyrimFair_FloorFill_1024"], earth, period=512)

    # 2. structural edge body, so the paved outline can step in 512u increments
    pieces["SkyrimFair_FloorEdge_512"] = box(
        "SkyrimFair_FloorEdge_512", -256, 256, -256, 256, -FLOOR_THICKNESS, 0,
        open_top=True)
    apply_paving_material(pieces["SkyrimFair_FloorEdge_512"], earth, period=512)

    # 3. retaining face, hangs below the floor plane, hidden behind cliff dressing
    pieces["SkyrimFair_Retain_512"] = box(
        "SkyrimFair_Retain_512", -256, 256, -RETAIN_DEPTH, 0, -RETAIN_HEIGHT, 0)
    apply_paving_material(pieces["SkyrimFair_Retain_512"], earth, period=512)

    # 4. outer corner for turning the stepped outline
    pieces["SkyrimFair_RetainCorner_128"] = box(
        "SkyrimFair_RetainCorner_128", -RETAIN_DEPTH, 0, -RETAIN_DEPTH, 0, -RETAIN_HEIGHT, 0)
    apply_paving_material(pieces["SkyrimFair_RetainCorner_128"], earth, period=512)

    # 5. chainable structural ramp body, descends outward in +Y, no top face
    pieces["SkyrimFair_Ramp_512"] = sloped_box(
        "SkyrimFair_Ramp_512", -256, 256, 0, RAMP_RUN, -RAMP_DEPTH, 0, -RAMP_RISE,
        open_top=True)
    apply_paving_material(pieces["SkyrimFair_Ramp_512"], earth, period=512)

    # 6. rough-earth / grass shoulder laid outside the paving to soften the join
    pieces["SkyrimFair_Shoulder_512"] = wedge(
        "SkyrimFair_Shoulder_512", 256, SHOULDER_RUN, SHOULDER_THICKNESS)
    apply_paving_material(pieces["SkyrimFair_Shoulder_512"], grass, period=512)

    # 7. hidden collision slope for the staircase. The visible slab is sunk under the
    #    treads and wears the earth material, so if a sliver ever shows between steps it
    #    reads as packed earth beneath them rather than a floating box.
    pieces["SkyrimFair_StairCollision"] = sloped_box(
        "SkyrimFair_StairCollision", -STAIRCOL_HALF_X, STAIRCOL_HALF_X,
        0, STAIRCOL_RUN, -STAIRCOL_DEPTH, -STAIRCOL_SINK, -STAIRCOL_SINK - STAIRCOL_RISE)
    apply_paving_material(pieces["SkyrimFair_StairCollision"], earth, period=512)

    # 5. Staircase flight: the walking surface IS this piece (visible treads), and the
    #    collision is a box on the nosing line, exactly as the hidden slab did it for
    #    the vanilla flight. No drystone wall comes with it; the cheek walls beside the
    #    steps are separate vanilla pieces the generator lays.
    pieces["SkyrimFair_Stair_192"] = stair(
        "SkyrimFair_Stair_192", STAIR_HALF_X, STAIR_RUN, STAIR_RISE, STAIR_STEPS, STAIR_DEPTH)
    apply_split_material(pieces["SkyrimFair_Stair_192"], steps_top, steps_side, period=256)
    nonconvex.add("SkyrimFair_Stair_192")

    # 9. One closed low retaining cheek per side and flight.  The generator places
    #    it at the same scale and origin as the stair, offset sideways and raised by
    #    the configured low crest height.
    pieces["SkyrimFair_StairCheek_192"] = stair_cheek(
        "SkyrimFair_StairCheek_192", STAIR_CHEEK_HALF_X, STAIR_RUN,
        STAIR_RISE, STAIR_STEPS, STAIR_CHEEK_DEPTH)
    apply_paving_material(pieces["SkyrimFair_StairCheek_192"], steps_side, period=256)
    nonconvex.add("SkyrimFair_StairCheek_192")

    # 10. One of two closed structural wings around the opening at the stair head.
    #     Together they restore the retaining face that the entrance reservation
    #     intentionally omits, without putting collision across the steps.
    pieces["SkyrimFair_EntranceRetainWing_144"] = box(
        "SkyrimFair_EntranceRetainWing_144",
        -ENTRANCE_WING_HALF_X, ENTRANCE_WING_HALF_X,
        -RETAIN_DEPTH, 0, -RETAIN_HEIGHT, 0)
    apply_paving_material(pieces["SkyrimFair_EntranceRetainWing_144"], earth, period=512)

    # --- visual paving caps: the only upward-facing surfaces on the terrace ---
    pieces["SkyrimFair_PaveCap_1024"] = quad(
        "SkyrimFair_PaveCap_1024",
        [(-512, -512, 0), (512, -512, 0), (512, 512, 0), (-512, 512, 0)])
    apply_paving_material(pieces["SkyrimFair_PaveCap_1024"], paving)
    visual_only.add("SkyrimFair_PaveCap_1024")

    for suffix, u_phase, v_phase in variants:
        name = "SkyrimFair_PaveCap_512" + suffix
        pieces[name] = quad(name, [(-256, -256, 0), (256, -256, 0),
                                   (256, 256, 0), (-256, 256, 0)])
        apply_paving_material(pieces[name], paving, u_phase, v_phase)
        visual_only.add(name)

    for suffix, u_phase, v_phase in variants:
        name = "SkyrimFair_RampCap_512" + suffix
        pieces[name] = quad(name, [(-256, 0, 0), (256, 0, 0),
                                   (256, RAMP_RUN, -RAMP_RISE),
                                   (-256, RAMP_RUN, -RAMP_RISE)])
        apply_paving_material(pieces[name], paving, u_phase, v_phase)
        visual_only.add(name)

    return pieces, visual_only, nonconvex


def select_only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_collision(obj, collider="self"):
    """Give a piece a Havok rigidbody and, optionally, a collider.

    collider="self"  - the object's own mesh becomes the collider. BGS defaults
                       the shape to a bounding box, which is correct for every
                       box-shaped piece in this kit.
    collider="child" - used for the ramp. A bounding box round a sloped mesh
                       would be a solid block and would stop the player walking
                       up it, so a separate box child collider is rotated to lie
                       along the slope instead. This is the "Adding Collision
                       using Child Collider Meshes" method from Bethesda's guide.
    collider="none"  - rigidbody but no collider (the shoulder wedge sits on
                       native ground and would only create a snag lip).
    collider="visual" - neither rigidbody nor collider, for the paving caps.
                       Collision belongs to the structural body beneath them.
    """
    result = {"rigidbody": None, "collider": None}

    if collider == "visual":
        result["rigidbody"] = "intentionally none (visual layer)"
        result["collider"] = "intentionally none (visual layer)"
        return result

    select_only(obj)
    try:
        bpy.ops.bgs_skyrim.create_rigidbody_skyrim()
        # Static world geometry must not be a movable physics body. The BGS
        # default is mass 80 and unyielding off, which would be a loose prop.
        obj.bgs_rigidbody.unyielding = True
        obj.bgs_rigidbody.mass = 0.0
        result["rigidbody"] = "ok (unyielding, mass 0)"
    except Exception as exc:                                 # noqa: BLE001
        result["rigidbody"] = f"failed: {exc}"

    if collider == "none":
        result["collider"] = "intentionally none"
        return result

    if collider == "self":
        try:
            bpy.ops.bgs_skyrim.create_collider_skyrim()
            result["collider"] = f"self, {obj.bgs_collider.type}"
        except Exception as exc:                             # noqa: BLE001
            result["collider"] = f"failed: {exc}"
        return result

    # child box collider, aligned to the ramp slope
    import math
    if obj.name.startswith("SkyrimFair_StairCheek_"):
        angle = math.atan2(STAIRCOL_RISE, STAIRCOL_RUN)
        slope_len = math.hypot(STAIRCOL_RUN, STAIRCOL_RISE)
        child = box(obj.name + "_Collider", -STAIR_CHEEK_HALF_X, STAIR_CHEEK_HALF_X,
                    -slope_len / 2.0, slope_len / 2.0, -STAIR_CHEEK_DEPTH, 0)
        child.rotation_euler = (-angle, 0.0, 0.0)
        s2 = BLENDER_UNITS_PER_SKYRIM_UNIT
        child.location = (0.0, STAIRCOL_RUN / 2.0 * s2, -STAIRCOL_RISE / 2.0 * s2)
        child.parent = obj
        child.matrix_parent_inverse = obj.matrix_world.inverted()
        select_only(child)
        try:
            bpy.ops.bgs_skyrim.create_collider_skyrim()
            result["collider"] = (f"cheek collision slope {math.degrees(angle):.1f} deg "
                                  f"({child.name}, {child.bgs_collider.type})")
        except Exception as exc:                             # noqa: BLE001
            result["collider"] = f"child failed: {exc}"
        return result

    if obj.name.startswith("SkyrimFair_StairCollision") or obj.name.startswith("SkyrimFair_Stair_"):
        angle = math.atan2(STAIRCOL_RISE, STAIRCOL_RUN)
        slope_len = math.hypot(STAIRCOL_RUN, STAIRCOL_RISE)
        thickness = 64
        child = box(obj.name + "_Collider", -STAIRCOL_HALF_X, STAIRCOL_HALF_X,
                    -slope_len / 2.0, slope_len / 2.0, -thickness, 0)
        child.rotation_euler = (-angle, 0.0, 0.0)
        s2 = BLENDER_UNITS_PER_SKYRIM_UNIT
        # The diagonal top face is lifted one riser above the nosing line. Across
        # each flat tread it descends from one riser above to exactly flush, so the
        # actor never stands below the visible stone surface.
        child.location = (
            0.0,
            STAIRCOL_RUN / 2.0 * s2,
            (-STAIRCOL_RISE / 2.0 + STAIRCOL_LIFT) * s2)
        child.parent = obj
        child.matrix_parent_inverse = obj.matrix_world.inverted()
        select_only(child)
        try:
            bpy.ops.bgs_skyrim.create_collider_skyrim()
            result["collider"] = (f"stair collision slope {math.degrees(angle):.1f} deg, "
                                  f"lift {STAIRCOL_LIFT} "
                                  f"({child.name}, {child.bgs_collider.type})")
        except Exception as exc:                             # noqa: BLE001
            result["collider"] = f"child failed: {exc}"
        return result

    angle = math.atan2(RAMP_RISE, RAMP_RUN)
    slope_len = math.hypot(RAMP_RUN, RAMP_RISE)
    thickness = 64
    child = box(obj.name + "_Collider", -256, 256,
                -slope_len / 2.0, slope_len / 2.0, -thickness, 0)
    child.rotation_euler = (-angle, 0.0, 0.0)
    # centre of the sloped top face, in the ramp's local space (Skyrim units -> Blender)
    s = BLENDER_UNITS_PER_SKYRIM_UNIT
    child.location = (0.0, RAMP_RUN / 2.0 * s, -RAMP_RISE / 2.0 * s)
    child.parent = obj
    child.matrix_parent_inverse = obj.matrix_world.inverted()
    select_only(child)
    try:
        bpy.ops.bgs_skyrim.create_collider_skyrim()
        result["collider"] = (f"child box rotated {math.degrees(angle):.2f} deg "
                              f"({child.name}, {child.bgs_collider.type})")
    except Exception as exc:                                 # noqa: BLE001
        result["collider"] = f"child failed: {exc}"
    return result


def main():
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    blend_path = os.path.join(repo, "assets", "blender", "fair_foundation_kit.blend")
    # AssetWatcher mirrors the Source folder's subfolder structure into its Output
    # folder, so the SkyrimFair subfolder is what puts the NIFs at meshes\SkyrimFair\.
    fbx_dir = os.path.join(repo, "assets", "fbx", "SkyrimFair")
    os.makedirs(fbx_dir, exist_ok=True)

    print("=== Skyrim Fair foundation kit build ===")
    print("Blender", bpy.app.version_string)

    clear_scene()

    # Bethesda's own scene units, copied from BGS_SKYRIM_OT_set_recommended_unit_scale
    # in bgs_skyrim_tools/operators/export_ops.py. That operator needs UI context and
    # cannot be invoked headlessly, so its constants are applied directly.
    # Getting this wrong is not subtle: building with system="NONE" produced meshes and
    # collision exactly 40x too large.
    scene = bpy.context.scene
    scene.unit_settings.system = "IMPERIAL"
    scene.unit_settings.length_unit = "INCHES"
    scene.unit_settings.scale_length = 1

    pieces, visual_only, nonconvex = build_kit()
    print(f"\nbuilt {len(pieces)} pieces")

    # Guard the bug that made the whole kit render inside-out. Every piece here is
    # convex, so a face is outward exactly when its normal points away from the mesh
    # centroid. This is asserted rather than reported: shipping inverted geometry a
    # second time is not worth the risk of a warning nobody reads.
    print("")
    print("=== face orientation ===")
    for name, obj in pieces.items():
        mesh = obj.data
        centroid = sum((v.co for v in mesh.vertices), Vector((0.0, 0.0, 0.0)))
        centroid /= len(mesh.vertices)
        inward = sum(1 for poly in mesh.polygons
                     if poly.normal.dot(poly.center - centroid) <= 0.0)
        upward = sum(1 for poly in mesh.polygons if poly.normal.z > 0.3)
        if name in visual_only:
            print(f"  {name:34s} faces={len(mesh.polygons):2d} upward={upward} (visual cap)")
        elif name in nonconvex:
            # The centroid test is meaningless for a stepped solid: a riser faces
            # away from the steps below it, toward the centroid. Blender recalculated
            # these outward on a closed manifold; check that the treads face up and
            # nothing faces straight down except the underside.
            treads = sum(1 for poly in mesh.polygons if poly.normal.z > 0.9)
            print(f"  {name:34s} faces={len(mesh.polygons):2d} tread-tris={treads} (non-convex, recalculated)")
            if treads < 2 * STAIR_STEPS:
                raise AssertionError(f"{name}: expected {2 * STAIR_STEPS} upward tread triangles, found {treads}")
            continue
        else:
            print(f"  {name:34s} faces={len(mesh.polygons):2d} inward={inward} upward={upward}")
        if name in visual_only:
            if upward != len(mesh.polygons):
                raise AssertionError(f"{name}: a visual cap must face upward only")
        elif inward:
            raise AssertionError(f"{name}: {inward} inward-facing polygons")

    collider_mode = {}
    for name in pieces:
        if name in {"SkyrimFair_StairCollision", "SkyrimFair_Stair_192", "SkyrimFair_StairCheek_192"}:
            collider_mode[name] = "child"
        elif name in visual_only:
            # Visual caps get no rigidbody at all. Collision stays on the
            # structural body underneath, at the same plane.
            collider_mode[name] = "visual"
        elif name.startswith("SkyrimFair_Ramp_512"):
            collider_mode[name] = "child"
        elif name == "SkyrimFair_Shoulder_512":
            collider_mode[name] = "none"
        else:
            collider_mode[name] = "self"

    print("\n=== collision ===")
    collision = {}
    for name, obj in pieces.items():
        collision[name] = apply_collision(obj, collider_mode[name])
        print(f"  {name:32s} rigidbody={collision[name]['rigidbody']}")
        print(f"  {'':32s} collider={collision[name]['collider']}")

    print("\n=== geometry report (Skyrim units) ===")
    inv = 1.0 / BLENDER_UNITS_PER_SKYRIM_UNIT
    for name, obj in pieces.items():
        bb = [obj.matrix_world @ v.co for v in obj.data.vertices]
        xs = [p.x * inv for p in bb]; ys = [p.y * inv for p in bb]; zs = [p.z * inv for p in bb]
        print(f"  {name:32s} verts={len(obj.data.vertices):3d} "
              f"X[{min(xs):7.0f},{max(xs):7.0f}] "
              f"Y[{min(ys):7.0f},{max(ys):7.0f}] "
              f"Z[{min(zs):7.0f},{max(zs):7.0f}] "
              f"origin={tuple(round(c) for c in obj.location)}")

    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"\nsaved blend: {blend_path}")

    print("\n=== BSFBX export ===")
    for name, obj in pieces.items():
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        for child in obj.children:          # child collider meshes must export too
            child.select_set(True)
        bpy.context.view_layer.objects.active = obj
        out = os.path.join(fbx_dir, name + ".fbx")
        try:
            bpy.ops.export_scene.bsfbx_skyrim(
                filepath=out,
                check_existing=False,
                use_selection=True,
                export_centered_at_origin=False,   # our pivots are deliberate
                apply_unit_scale=True,             # BGS exporter default
                global_scale=1.0,
                use_bgs_materials=True,
                object_types={"MESH", "EMPTY"},
                bake_anim=False,
            )
            size = os.path.getsize(out) if os.path.exists(out) else 0
            print(f"  {name:32s} -> {os.path.basename(out)} ({size} bytes)")
        except Exception as exc:                             # noqa: BLE001
            print(f"  {name:32s} EXPORT FAILED: {exc}")

    print("\ndone")


if __name__ == "__main__":
    main()
