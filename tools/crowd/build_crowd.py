"""Build one static crowd figure from its recipe (tools/crowd/recipes/<name>.json).

    blender --background --factory-startup --python-exit-code 1 --python tools/crowd/build_crowd.py -- Clapping01
    ... -- Clapping01 --sheet 0,30,60      render the posed figure at several frames, to choose one
    ... -- Clapping01 --frame 42           override the recipe's frame

Outputs: the NIF at the recipe's `output`, and in build/crowd/<name>/ the posed and baked
.blend files, preview renders and report.json.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import crowd_lib as cl  # noqa: E402


def camera(name, location, target, ortho=150):
    cam = bpy.data.cameras.new(name)
    cam.type, cam.ortho_scale = 'ORTHO', ortho
    obj = bpy.data.objects.new(name, cam)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    direction = Vector(target) - Vector(location)
    obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    return obj


def setup_render(res=(600, 800)):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'TEXTURE'
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.film_transparent = False
    scene.world = scene.world or bpy.data.worlds.new('World')
    return scene


def render(path, views, centre):
    """Front, side and back views. Skyrim actors face +Y, so the front camera sits on +Y."""
    scene = bpy.context.scene
    cx, cy, cz = centre
    for label, offset in views:
        cam = camera(f'cam_{label}', (cx + offset[0], cy + offset[1], cz + offset[2]), centre)
        scene.camera = cam
        scene.render.filepath = str(path.with_name(f'{path.stem}_{label}.png'))
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(cam)


VIEWS = [('front', (0, 300, 0)), ('threequarter', (212, 212, 0)), ('side', (300, 0, 0)), ('back', (0, -300, 0))]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('recipe')
    ap.add_argument('--sheet')
    ap.add_argument('--frame', type=int)
    args = ap.parse_args(sys.argv[sys.argv.index('--') + 1:])

    recipe = cl.load_recipe(args.recipe)
    work = cl.ROOT / 'build/crowd' / args.recipe
    work.mkdir(parents=True, exist_ok=True)
    skel = cl.Skeleton()
    anim_path = cl.ANIMATIONS / recipe['animation']
    anim = cl.load_skyrim_animation(str(anim_path))

    cl.clear_scene()
    setup_render()
    textures = cl.Textures(work / 'preview_resources')
    rig = cl.build_armature(skel)
    parts = cl.load_parts(recipe, skel)
    objs = [cl.build_mesh(p, rig, textures) for p in parts]

    if args.sheet:
        for f in [int(x) for x in args.sheet.split(',')]:
            cl.pose_armature(rig, skel, skel.pose(anim, f))
            render(work / 'sheet' / f'f{f:03d}', VIEWS[:3], (0, 0, 68))
        print('SHEET', work / 'sheet')
        return

    frame = args.frame if args.frame is not None else recipe['frame']
    worlds = skel.pose(anim, frame)
    cl.pose_armature(rig, skel, worlds)
    bpy.ops.wm.save_as_mainfile(filepath=str(work / f'{args.recipe}_posed.blend'))

    reference = [cl.skinned(p, skel, worlds) for p in parts]
    cl.bake(objs, rig)
    baked = [[o.matrix_world @ v.co for v in o.data.vertices] for o in objs]
    bake_error = max((a - b).length for verts, ref in zip(baked, reference) for a, b in zip(verts, ref[0]))

    offset = cl.ground_offset(parts, baked, skel, worlds)
    shift = Matrix.Translation(offset)
    for obj in objs:
        obj.data.transform(shift @ obj.matrix_world)
        obj.matrix_world = Matrix.Identity(4)
    baked = [[v.co.copy() for v in o.data.vertices] for o in objs]

    report = {'name': recipe['name'], 'skeleton': str(skel.path), 'skeleton_sha256': cl.sha256(skel.path),
              'animation': str(anim_path), 'animation_sha256': cl.sha256(anim_path),
              'animation_frames': anim.num_frames, 'animation_duration': anim.duration,
              'frame': frame, 'time_seconds': round(frame * anim.frame_duration, 4),
              'weight': recipe['weight'], 'bake_max_error_vs_independent_skinning': bake_error,
              'ground_offset': list(offset), 'shapes': []}
    normals = []
    for part, verts, (ref_v, ref_n, rot) in zip(parts, baked, reference):
        notes = cl.static_shader(part, max(rot), recipe.get('tints'))
        normals.append(ref_n)
        report['shapes'].append({
            'role': part.role, 'shape': part.name, 'sources': part.sources, 'vertices': len(verts),
            'triangles': len(part.tris), 'bones_removed': len(part.skin_to_bone),
            'bind_spread': part.bind_spread, 'max_rotation_from_bind': max(rot),
            'shader': part.shader_name, 'shader_type': part.export_shader.Shader_Type,
            'textures': [t for t in part.export_slots], 'alpha': part.alpha, 'notes': notes,
            'bounds_min': [min(v[i] for v in verts) for i in range(3)],
            'bounds_max': [max(v[i] for v in verts) for i in range(3)]})

    out = cl.ROOT / recipe['output']
    out.parent.mkdir(parents=True, exist_ok=True)
    cl.export_static(out, parts, baked, normals, recipe['name'])
    report['output'] = str(out)
    report['missing_preview_textures'] = textures.missing

    bpy.ops.wm.save_as_mainfile(filepath=str(work / f'{args.recipe}_baked.blend'))
    height = max(s['bounds_max'][2] for s in report['shapes'])
    render(work / 'preview', VIEWS, (0, 0, height / 2))
    (work / 'report.json').write_text(json.dumps(report, indent=2))
    print('BUILT', out, 'bake error %.6f' % bake_error, 'height %.2f' % height)


if __name__ == '__main__':
    main()
