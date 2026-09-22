"""Make the fair's static props: vanilla item meshes with their physics removed.

    python tools/make_static_props.py --data "<stock Data>"

reads tools/static_props_sources.json, writes each prop to assets/meshes/<out> (by
default SkyrimFair/Props/<Name>.nif), and records every prop's model path and measured
bounds in tools/static_props.json, which the generator turns into STAT records named
SkyrimFairProp<Name> (market pieces use them as @Prop<Name>).

Why: the goods a vanilla market shows (cheese, bread, bottles, weapons, pelts) are loose
havok items. Placed on a counter they can be knocked off, stolen, or pushed out of the
collision they spawn in, as the first tower lantern was (2026-09-22). Each copy keeps the
geometry, textures, add-ons and animation byte for byte and only unhooks the collision:
every node's collision link is set to none and the BSX havok, ragdoll, complex, dynamic
and articulated bits are cleared. The rigid body blocks stay in the file unreferenced.

The outputs are modified Bethesda meshes, so they are generated rather than committed
(.gitignore) and are shipped only inside the built mod. tools/static_props.json (paths and
bounds only) is committed, so the generator runs without the meshes present.
"""

import argparse
import json
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bsa_extract  # noqa: E402
import nif_preview  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
SOURCES = os.path.join(HERE, "static_props_sources.json")
MANIFEST = os.path.join(HERE, "static_props.json")
AV_OBJECTS = {"BSFadeNode", "NiNode", "BSValueNode", "BSTriShape", "BSMultiBoundNode", "BSLeafAnimNode",
              "BSOrderedNode", "NiSwitchNode", "BSDynamicTriShape", "BSLODTriShape", "BSSubIndexTriShape"}
BSX_PHYSICS = 0x2 | 0x4 | 0x8 | 0x40 | 0x80  # havok, ragdoll, complex, dynamic, articulated


def blocks(data):
    """(type, offset, size) of every block in a BS 100 NIF."""
    p = data.index(b"\n") + 1
    version, _, _user, count, bs = struct.unpack_from("<IBIII", data, p)
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


def unhook(data):
    """Unhook every collision link and clear the BSX physics bits, in place."""
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
    return unhooked


def bounds(path):
    """Measured bounds (min xyz, max xyz), or None when the mesh has no readable geometry."""
    try:
        tris = nif_preview.triangles(path)
    except Exception:  # noqa: BLE001 - skinned or unusual meshes fall back
        return None
    points = [p for t in tris for p in t]
    if not points:
        return None
    return ([min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", required=True, help="Skyrim Data folder holding the BSAs")
    args = ap.parse_args()

    sources = json.load(open(SOURCES, encoding="utf-8"))["props"]
    index = bsa_extract.build_index(args.data)
    manifest = {}
    missing = []
    for name, spec in sources.items():
        key = spec["source"].replace("\\", "/").lower()
        if key not in index:
            missing.append(key)
            continue
        data = bytearray(bsa_extract.extract(index[key]))
        unhook(data)
        out = spec.get("out", f"SkyrimFair/Props/{name}.nif")
        dest = os.path.join(ROOT, "assets", "meshes", *out.split("/"))
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        with open(dest, "wb") as f:
            f.write(data)
        b = bounds(dest)
        lo, hi = b if b else ([-10, -10, 0], [10, 10, 20])
        manifest[name] = {
            "model": out.replace("/", "\\"),
            "min": [round(v) for v in lo],
            "max": [round(v) for v in hi],
            "measured": b is not None,
        }
    with open(MANIFEST, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"_comment": "Written by tools/make_static_props.py; do not edit by hand.", "props": manifest},
                  f, indent=1, sort_keys=True)
        f.write("\n")
    print(f"{len(manifest)} props written, {sum(not m['measured'] for m in manifest.values())} with fallback bounds")
    for m in missing:
        print(f"  missing from the archives: {m}")
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
