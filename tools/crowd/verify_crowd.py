"""Check a built static crowd NIF, independently of the build script.

    python tools/crowd/verify_crowd.py Clapping01

Reads the NIF header block table with its own parser (not PyNifly) to prove there is no skin,
animation, controller or Havok block, then reads the shapes back with PyNifly to check vertex
format, shader flags, texture paths (against the stock BSAs), ground placement and cost.
Writes build/crowd/<name>/verify.json and exits non-zero on any failure.
"""
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
NIFLY = Path(r'C:\Users\Barry\Downloads\Skyrim Actors\crowd-tools\pynifly\io_scene_nifly')
DATA = Path(r'E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data')
sys.path[:0] = [str(NIFLY), str(ROOT / 'tools')]
from pyn.pynifly import NifFile  # noqa: E402
import bsa_extract  # noqa: E402

FORBIDDEN = ('Skin', 'Controller', 'Interpolator', 'bhk', 'hkp', 'NiSequence', 'NiKeyframe', 'BSBehaviorGraph',
             'NiTransformData', 'BSAnimNote', 'NiStringExtraData', 'BSBound')
VF_SKINNED, VF_TANGENTS, VF_NORMALS = 1 << 6, 1 << 4, 1 << 3
SLSF1_SKINNED = 0x2


def header_blocks(path):
    """Block types from the NIF header (Skyrim SE: 20.2.0.7, user version 12, BS version 100)."""
    d = Path(path).read_bytes()
    nl = d.index(b'\n')
    header = d[:nl].decode()
    o = nl + 1
    version, = struct.unpack_from('<I', d, o); o += 4
    o += 1  # endian
    user, nblocks = struct.unpack_from('<II', d, o); o += 8
    bsver, = struct.unpack_from('<I', d, o); o += 4
    for _ in range(3):  # author, process script, export script
        o += 1 + d[o]
    ntypes, = struct.unpack_from('<H', d, o); o += 2
    types = []
    for _ in range(ntypes):
        n, = struct.unpack_from('<I', d, o); o += 4
        types.append(d[o:o + n].decode()); o += n
    idx = struct.unpack_from(f'<{nblocks}H', d, o)
    return {'header': header, 'version': hex(version), 'user_version': user, 'bs_version': bsver,
            'blocks': [types[i & 0x7FFF] for i in idx]}


def main():
    name = sys.argv[1]
    recipe = json.loads((ROOT / 'tools/crowd/recipes' / f'{name}.json').read_text())
    path = ROOT / recipe['output']
    failures = []

    def check(ok, msg):
        if not ok:
            failures.append(msg)

    hdr = header_blocks(path)
    check(hdr['bs_version'] == 100 and hdr['user_version'] == 12, 'not a Skyrim SE NIF version')
    check(hdr['blocks'][0] == 'BSFadeNode', 'root is not BSFadeNode')
    bad = sorted({b for b in hdr['blocks'] if any(f in b for f in FORBIDDEN)})
    check(not bad, f'forbidden blocks: {bad}')

    index = bsa_extract.build_index(str(DATA))
    nif = NifFile(str(path))
    shapes, textures, materials, missing = [], set(), set(), []
    lo, hi = [1e9] * 3, [-1e9] * 3
    for sh in nif.shapes:
        vdesc = sh.properties.vertexDesc
        # PyNifly reports the BSVertexDesc attribute flags already unpacked (vanilla statics: 0x1b).
        flags = (vdesc >> 44) & 0xFFF if vdesc > 0xFFFF else vdesc
        slots = [sh.shader._readtexture(nif._handle, sh._handle, i) for i in range(1, 10)]
        for t in filter(None, slots):
            key = t.replace('\\', '/').lower()
            textures.add(key)
            if key not in index and not (DATA / key).exists():
                missing.append(key)
        materials.add((sh.shader.properties.Shader_Type, sh.shader.properties.Shader_Flags_1,
                       sh.shader.properties.Shader_Flags_2, tuple(slots)))
        check(sh.blockname == 'BSTriShape', f'{sh.name}: {sh.blockname}')
        check(not flags & VF_SKINNED, f'{sh.name}: vertex format still skinned')
        check(flags & VF_NORMALS and flags & VF_TANGENTS, f'{sh.name}: normals/tangents missing')
        check(not sh.shader.properties.Shader_Flags_1 & SLSF1_SKINNED, f'{sh.name}: shader Skinned flag set')
        check(not sh.bone_names, f'{sh.name}: has bones')
        for v in sh.verts:
            for i in range(3):
                lo[i], hi[i] = min(lo[i], v[i]), max(hi[i], v[i])
        shapes.append({'name': sh.name, 'block': sh.blockname, 'vertices': len(sh.verts), 'triangles': len(sh.tris),
                       'vertex_flags': hex(flags), 'shader_type': sh.shader.properties.Shader_Type,
                       'shader_flags1': hex(sh.shader.properties.Shader_Flags_1), 'alpha': sh.has_alpha_property,
                       'textures': [t for t in slots if t]})
    check(not missing, f'textures not in the stock Data: {missing}')
    check(abs(lo[2]) < 0.01, f'lowest point z={lo[2]:.3f}, not on the ground')
    height = hi[2] - lo[2]
    check(110 < height < 145, f'height {height:.1f} outside the human range')

    report = {'nif': str(path), 'bytes': path.stat().st_size, 'header': hdr, 'block_types': sorted(set(hdr['blocks'])),
              'shapes': shapes, 'shape_count': len(shapes), 'material_count': len(materials),
              'triangles': sum(s['triangles'] for s in shapes), 'vertices': sum(s['vertices'] for s in shapes),
              'bounds_min': lo, 'bounds_max': hi, 'height': height, 'textures': sorted(textures),
              'missing_textures': missing, 'failures': failures}
    (ROOT / 'build/crowd' / name / 'verify.json').write_text(json.dumps(report, indent=2))
    print(json.dumps({k: report[k] for k in ('block_types', 'shape_count', 'material_count', 'triangles',
                                             'vertices', 'bounds_min', 'bounds_max', 'height', 'bytes',
                                             'missing_textures', 'failures')}, indent=1))
    sys.exit(1 if failures else 0)


if __name__ == '__main__':
    main()
