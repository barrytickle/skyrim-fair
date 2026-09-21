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

# --- kit dimensions, all in Skyrim units -------------------------------------

FLOOR_THICKNESS = 32
RETAIN_HEIGHT = 256      # max measured floor exposure is 160u, so this always covers
RETAIN_DEPTH = 128
RAMP_RUN = 512
RAMP_RISE = 64           # 1:8 grade; chain 3 for the ~160u fall on the west edge
RAMP_DEPTH = 256         # buried, so local terrain variation never undercuts it
SHOULDER_RUN = 256
SHOULDER_THICKNESS = 32


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.objects):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def make_object(name, verts, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.validate()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def box(name, x0, x1, y0, y1, z0, z1):
    verts = [
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1),
    ]
    faces = [
        (0, 1, 2, 3),   # bottom
        (7, 6, 5, 4),   # top
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    return make_object(name, verts, faces)


def sloped_box(name, x0, x1, y0, y1, z_bottom, z_near, z_far):
    """Box whose top face slopes from z_near at y0 down to z_far at y1."""
    verts = [
        (x0, y0, z_bottom), (x1, y0, z_bottom), (x1, y1, z_bottom), (x0, y1, z_bottom),
        (x0, y0, z_near), (x1, y0, z_near), (x1, y1, z_far), (x0, y1, z_far),
    ]
    faces = [
        (0, 1, 2, 3),
        (7, 6, 5, 4),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    return make_object(name, verts, faces)


def wedge(name, half_x, run, thickness):
    """Triangular prism tapering from `thickness` at y=0 to a knife edge at y=run."""
    verts = [
        (-half_x, 0, 0), (half_x, 0, 0),
        (-half_x, 0, -thickness), (half_x, 0, -thickness),
        (-half_x, run, -thickness), (half_x, run, -thickness),
    ]
    faces = [
        (0, 1, 3, 2),   # thick end
        (0, 4, 5, 1),   # sloped top
        (2, 3, 5, 4),   # underside
        (0, 2, 4),      # left
        (1, 5, 3),      # right
    ]
    return make_object(name, verts, faces)


def build_kit():
    pieces = {}

    # 1. interior fill tile - 9 of these cover a 3072 core
    pieces["SkyrimFair_FloorFill_1024"] = box(
        "SkyrimFair_FloorFill_1024", -512, 512, -512, 512, -FLOOR_THICKNESS, 0)

    # 2. half-size edge tile, so the paved outline can step in 512u increments
    #    and read as irregular rather than rectangular
    pieces["SkyrimFair_FloorEdge_512"] = box(
        "SkyrimFair_FloorEdge_512", -256, 256, -256, 256, -FLOOR_THICKNESS, 0)

    # 3. retaining face, hangs below the floor plane
    pieces["SkyrimFair_Retain_512"] = box(
        "SkyrimFair_Retain_512", -256, 256, -RETAIN_DEPTH, 0, -RETAIN_HEIGHT, 0)

    # 4. outer corner for turning the stepped outline
    pieces["SkyrimFair_RetainCorner_128"] = box(
        "SkyrimFair_RetainCorner_128", -RETAIN_DEPTH, 0, -RETAIN_DEPTH, 0, -RETAIN_HEIGHT, 0)

    # 5. chainable ramp, descends outward in +Y
    pieces["SkyrimFair_Ramp_512"] = sloped_box(
        "SkyrimFair_Ramp_512", -256, 256, 0, RAMP_RUN, -RAMP_DEPTH, 0, -RAMP_RISE)

    # 6. rough-earth / grass shoulder laid outside the paving to soften the join
    pieces["SkyrimFair_Shoulder_512"] = wedge(
        "SkyrimFair_Shoulder_512", 256, SHOULDER_RUN, SHOULDER_THICKNESS)

    return pieces


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
    collider="none"  - no collision at all (the shoulder wedge sits on native
                       ground and would only create a snag lip).
    """
    result = {"rigidbody": None, "collider": None}

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
    angle = math.atan2(RAMP_RISE, RAMP_RUN)
    slope_len = math.hypot(RAMP_RUN, RAMP_RISE)
    thickness = 64
    child = box(obj.name + "_Collider", -256, 256,
                -slope_len / 2.0, slope_len / 2.0, -thickness, 0)
    child.rotation_euler = (-angle, 0.0, 0.0)
    # centre of the sloped top face, in the ramp's local space
    child.location = (0.0, RAMP_RUN / 2.0, -RAMP_RISE / 2.0)
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

    # Build 1:1 in Skyrim units; no unit scaling is applied on export.
    scene = bpy.context.scene
    scene.unit_settings.system = "NONE"

    pieces = build_kit()
    print(f"\nbuilt {len(pieces)} pieces")

    collider_mode = {
        "SkyrimFair_FloorFill_1024": "self",
        "SkyrimFair_FloorEdge_512": "self",
        "SkyrimFair_Retain_512": "self",
        "SkyrimFair_RetainCorner_128": "self",
        "SkyrimFair_Ramp_512": "child",
        "SkyrimFair_Shoulder_512": "none",
    }

    print("\n=== collision ===")
    collision = {}
    for name, obj in pieces.items():
        collision[name] = apply_collision(obj, collider_mode[name])
        print(f"  {name:32s} rigidbody={collision[name]['rigidbody']}")
        print(f"  {'':32s} collider={collision[name]['collider']}")

    print("\n=== geometry report ===")
    for name, obj in pieces.items():
        bb = [obj.matrix_world @ v.co for v in obj.data.vertices]
        xs = [p.x for p in bb]; ys = [p.y for p in bb]; zs = [p.z for p in bb]
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
                apply_unit_scale=False,
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
