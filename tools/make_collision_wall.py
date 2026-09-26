"""An invisible collision wall: meshes/SkyrimFair/Collision/StageWall.nif.

    "<Blender 3.6>/3.6/python/bin/python.exe" tools/make_collision_wall.py

(Blender's bundled Python, for PyNifly's standalone NIF library, which lives with the crowd
tools outside the repo: crowd_lib.NIFLY.) Writes a Skyrim SE NIF with no geometry at all: a
BSFadeNode, BSXFlags Havok, and one fixed rigid body with a box 256 long (local X), 16 thick
(Y) and 400 high (Z), centred on the origin. The rigid body's settings are copied from the
kit's SkyrimFair_StairCollision.nif (AssetWatcher's output, a box collider that works in
game): layer STATIC, mass 0, fixed motion.

Why not the CollisionMarker box primitives: in game they didn't stop the player at all on
L_TRANSPARENT, and on the default layer SkyParkour mantled onto them. A real static is
solid, and at 400 it's above SkyParkour's highest climb (250), so it can't be vaulted.
"""
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NIFLY = Path(r'C:\Users\Barry\Downloads\Skyrim Actors\crowd-tools\pynifly\io_scene_nifly')
SOURCE = ROOT / 'assets' / 'nif' / 'SkyrimFair' / 'SkyrimFair_StairCollision.nif'
OUT = ROOT / 'assets' / 'meshes' / 'SkyrimFair' / 'Collision' / 'StageWall.nif'
HALF = (128.0, 8.0, 200.0)  # half-extents, game units

sys.path.insert(0, str(NIFLY))
from pyn.pynifly import NifFile, BSXFlags  # noqa: E402
from pyn.nifdefs import bhkBoxShapeProps, PynBufferTypes  # noqa: E402
from pyn.nifconstants import HAVOC_SCALE_FACTOR  # noqa: E402


def blocks(data):
    """(type, offset, size) of every block in a BS 100 NIF (as tools/make_static_props.py reads them)."""
    p = data.index(bytes([10])) + 1
    version, _, _user, count, bs = struct.unpack_from("<IBIII", data, p)
    p += 17
    assert version == 0x14020007 and bs == 100, (hex(version), bs)
    for _ in range(3):
        p += 1 + data[p]
    ntypes = struct.unpack_from("<H", data, p)[0]
    p += 2
    types = []
    for _ in range(ntypes):
        n = struct.unpack_from("<I", data, p)[0]
        p += 4
        types.append(data[p:p + n].decode("latin1"))
        p += n
    idx = struct.unpack_from(f"<{count}H", data, p)
    p += 2 * count
    sizes = struct.unpack_from(f"<{count}I", data, p)
    p += 4 * count
    nstrings, _ = struct.unpack_from("<II", data, p)
    p += 8
    for _ in range(nstrings):
        n = struct.unpack_from("<I", data, p)[0]
        p += 4 + n
    ngroups = struct.unpack_from("<I", data, p)[0]
    p += 4 + 4 * ngroups
    out = []
    for t, size in zip(idx, sizes):
        out.append((types[t], p, size))
        p += size
    return out


def main():
    ref = NifFile(str(SOURCE))
    body = ref.rootNode.collision_object.body
    props = body.properties
    material = body.shape.properties.bhkMaterial if hasattr(body.shape.properties, 'bhkMaterial') else 0

    OUT.parent.mkdir(parents=True, exist_ok=True)
    nif = NifFile()
    nif.initialize('SKYRIMSE', str(OUT), root_type='BSFadeNode', root_name='SkyrimFairStageWall')
    BSXFlags.New(nif, name='BSX', flags=2, parent=nif.rootNode)  # Havok

    box = bhkBoxShapeProps()
    box.bhkMaterial = material
    box.bhkRadius = 0.05
    for i, h in enumerate(HALF):
        box.bhkDimensions[i] = h / HAVOC_SCALE_FACTOR
    shape = nif.add_shape(box)

    coll = nif.rootNode.add_collision(None, flags=ref.rootNode.collision_object.flags)
    props.bufType = PynBufferTypes.bhkRigidBodyBufType
    props.shapeID = shape.id
    for i in range(4):
        props.translation[i] = 0.0
    props.rotation[0] = props.rotation[1] = props.rotation[2] = 0.0
    props.rotation[3] = 1.0
    coll.add_body(props)
    nif.save()

    # PyNifly's buffer writes processContactCallbackDelay as 0x00FF; the stair's (and
    # vanilla's) is 0xFFFF. Both bodies are the same 250-byte layout, so ours takes the
    # stair's bytes whole, all but its first four (the link to its own shape).
    src = SOURCE.read_bytes()
    out = bytearray(OUT.read_bytes())
    (_, so, ss), = [b for b in blocks(src) if b[0].startswith('bhkRigidBody')]
    (_, oo, os_), = [b for b in blocks(bytes(out)) if b[0].startswith('bhkRigidBody')]
    assert ss == os_ == 250, (ss, os_)
    out[oo + 4:oo + os_] = src[so + 4:so + ss]
    OUT.write_bytes(bytes(out))

    # Read it back: layer, motion and the box, as the game will.
    back = NifFile(str(OUT))
    b = back.rootNode.collision_object.body
    dims = [round(d * HAVOC_SCALE_FACTOR, 2) for d in b.shape.properties.bhkDimensions]
    print(f'{OUT.relative_to(ROOT)}: {len(back.shapes)} render shapes, body {type(b).__name__} layer {b.properties.collisionFilter_layer} '
          f'mass {b.properties.mass} motion {b.properties.motionSystem}, box half-extents {dims}')


if __name__ == '__main__':
    main()
