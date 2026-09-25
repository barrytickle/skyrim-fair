"""Render the paper lanterns in every colour, for choosing (headless Blender, no window).

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --python tools/lanterns_preview_blender.py

Astra's two lanterns (assets/Skyrim_Paper_Lanterns_Authoring/Blender), lit as in her own
preview (work/preview.py: a dark studio, two area lights, the paper emissive), with each
colour's paper from tools/make_lantern_colours.py (build/lanterns/<shape>_<colour>.png).
Writes build/lanterns/render_<colour>.png, one per colour with both shapes, then
build/lanterns/lanterns_colours.png, all of them in a grid. A preview, not a game screenshot.
"""
import pathlib

import bpy
from mathutils import Vector

ROOT = pathlib.Path(__file__).resolve().parent.parent
BLEND = ROOT / "assets" / "Skyrim_Paper_Lanterns_Authoring" / "Blender"
OUT = ROOT / "build" / "lanterns"
SHAPES = [("tall", "lantern_tall_red", -0.25), ("round", "lantern_round_blue", 0.22)]
COLOURS = ["red", "orange", "yellow", "green", "blue", "purple"]


def scene_with_lanterns():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    placed = {}
    for shape, name, x in SHAPES:
        with bpy.data.libraries.load(str(BLEND / f"{name}.blend"), link=False) as (src, dst):
            dst.objects = src.objects
        for o in dst.objects:
            bpy.context.collection.objects.link(o)
            if o.parent is None:
                o.location.x = x
        placed[shape] = dst.objects
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.world = bpy.data.worlds.new("Studio")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.014, 0.018, 0.023, 1)
    bg.inputs[1].default_value = 0.3
    bpy.ops.object.camera_add(location=(0.25, -2, 0.35))
    cam = bpy.context.object
    target = Vector((0, 0, -0.30))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 1.13
    scene.camera = cam
    for location, energy, size in [((-1, -1, 1), 55, 2), ((1, 0.4, 0.4), 45, 1)]:
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.data.energy = energy
        light.data.size = size
        light.rotation_euler = (target - light.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.resolution_x = 700
    scene.render.resolution_y = 550
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "Standard"
    try:
        scene.view_settings.look = "Medium High Contrast"
    except TypeError:
        pass
    scene.render.image_settings.file_format = "PNG"
    return scene, placed


def paper_nodes(objects):
    """The image nodes that carry a lantern's paper (not its bronze cap)."""
    nodes = []
    for o in objects:
        for slot in getattr(o, "material_slots", []):
            mat = slot.material
            if not (mat and mat.use_nodes):
                continue
            for n in mat.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image and "bronze" not in n.image.name.lower():
                    nodes.append(n)
    return nodes


def main():
    scene, placed = scene_with_lanterns()
    papers = {shape: paper_nodes(objs) for shape, objs in placed.items()}
    for shape, nodes in papers.items():
        assert nodes, f"no paper texture found on the {shape} lantern"
    renders = []
    for colour in COLOURS:
        for shape, nodes in papers.items():
            img = bpy.data.images.load(str(OUT / f"{shape}_{colour}.png"), check_existing=True)
            for n in nodes:
                n.image = img
        path = OUT / f"render_{colour}.png"
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        renders.append(path)
        print(f"rendered {path.name}")

    # All six in a grid, three across, each labelled.
    cols, w, h = 3, 700, 550
    sheet = bpy.data.images.new("sheet", cols * w, 2 * h)
    pixels = [0.0] * (cols * w * 2 * h * 4)
    for i, path in enumerate(renders):
        img = bpy.data.images.load(str(path))
        src = list(img.pixels)
        cx, cy = (i % cols) * w, (1 - i // cols) * h  # Blender's rows run bottom up
        for y in range(h):
            row = (y * w) * 4
            dst = ((cy + y) * cols * w + cx) * 4
            pixels[dst:dst + w * 4] = src[row:row + w * 4]
    sheet.pixels = pixels
    sheet.filepath_raw = str(OUT / "lanterns_colours.png")
    sheet.file_format = "PNG"
    sheet.save()
    print("wrote lanterns_colours.png")


main()
