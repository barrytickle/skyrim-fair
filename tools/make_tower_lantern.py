"""Make the light towers' lantern: vanilla CandleLanternwithCandle01 with its physics removed.

    python tools/make_tower_lantern.py --data "<stock Data>"

writes assets/meshes/SkyrimFair/TowerLantern.nif.

Why: the vanilla lantern is havok clutter. Placed on a tower deck it spawns inside the
tower's collision, is pushed out, and falls to the ground (seen in game, 2026-09-22).
This copy keeps the geometry, textures, candle add-on and flicker animation byte for
byte, and only unhooks the collision: every node's collision link is set to none and
the BSX havok, ragdoll, complex, dynamic and articulated bits are cleared. The rigid
body blocks stay in the file but nothing references them, so the engine never builds
them.

The output is a modified Bethesda mesh, so it is generated rather than committed
(.gitignore) and is only shipped inside the built mod.
"""

import argparse
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bsa_extract  # noqa: E402

SOURCE = "meshes/clutter/common/candlelanternwithcandle01.nif"
OUT = os.path.join(os.path.dirname(__file__), "..", "assets", "meshes", "SkyrimFair", "TowerLantern.nif")
AV_OBJECTS = {"BSFadeNode", "NiNode", "BSValueNode", "BSTriShape", "BSMultiBoundNode", "BSLeafAnimNode"}
BSX_PHYSICS = 0x2 | 0x4 | 0x8 | 0x40 | 0x80  # havok, ragdoll, complex, dynamic, articulated


def blocks(data):
    """(type, offset, size) of every block in a BS 100 NIF."""
    p = data.index(b"\n") + 1
    version, _, user, count, bs = struct.unpack_from("<IBIII", data, p)
    p += 17
    assert version == 0x14020007 and bs == 100, (hex(version), bs)
    for _ in range(3):
        p += 1 + data[p]
    ntypes = struct.unpack_from("<H", data, p)[0]; p += 2
    types = []
    for _ in range(ntypes):
        n = struct.unpack_from("<I", data, p)[0]; p += 4
        types.append(data[p:p + n].decode("latin1")); p += n
    idx = struct.unpack_from(f"<{count}H", data, p); p += 2 * count
    sizes = struct.unpack_from(f"<{count}I", data, p); p += 4 * count
    nstrings, _ = struct.unpack_from("<II", data, p); p += 8
    for _ in range(nstrings):
        n = struct.unpack_from("<I", data, p)[0]; p += 4 + n
    ngroups = struct.unpack_from("<I", data, p)[0]; p += 4 + 4 * ngroups
    out = []
    for t, size in zip(idx, sizes):
        out.append((types[t], p, size))
        p += size
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", required=True, help="Skyrim Data folder holding the BSAs")
    ap.add_argument("--out", default=OUT)
    args = ap.parse_args()

    index = bsa_extract.build_index(args.data)
    data = bytearray(bsa_extract.extract(index[SOURCE]))
    unhooked = 0
    for kind, off, _ in blocks(bytes(data)):
        if kind in AV_OBJECTS:
            # NiAVObject (BS 100): name, extra data list, controller, flags, translation,
            # rotation, scale, then the collision link.
            extra = struct.unpack_from("<I", data, off + 4)[0]
            coll = off + 4 + 4 + 4 * extra + 4 + 4 + 12 + 36 + 4
            if struct.unpack_from("<i", data, coll)[0] != -1:
                struct.pack_into("<i", data, coll, -1)
                unhooked += 1
        elif kind == "BSXFlags":
            flags = struct.unpack_from("<I", data, off + 4)[0]
            struct.pack_into("<I", data, off + 4, flags & ~BSX_PHYSICS)
            print(f"BSX {flags:#x} -> {flags & ~BSX_PHYSICS:#x}")
    assert unhooked, "no collision link found"
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    with open(args.out, "wb") as f:
        f.write(data)
    print(f"unhooked {unhooked} collision link(s) -> {os.path.abspath(args.out)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
