"""Make the fair's static props: vanilla item meshes with their physics removed.

    python tools/make_static_props.py --data "<stock Data>"

reads tools/static_props_sources.json, writes each prop to assets/meshes/<out> (by
default SkyrimFair/Props/<Name>.nif), and records every prop's model path and measured
bounds in tools/static_props.json, which the generator turns into STAT records named
SkyrimFairProp<Name> (market pieces use them as @Prop<Name>).

Why: the goods a vanilla market shows (cheese, bread, bottles, weapons, pelts) are loose
havok items. Placed on a counter they can be knocked off, stolen, or pushed out of the
collision they spawn in, as the first tower lantern was (2026-09-22). Each copy keeps
every block, link and flag byte for byte and only makes its rigid bodies fixed, the way
vanilla's unmoving barrels and crates are authored: collision layer STATIC (both filter
copies), mass and inertia 0, motion system FIXED with fixed quality (the four bytes at
+224 become 05 01 01 00, as in Barrel02.nif and CommonCrate01.nif).

Do not unhook collision links instead. The first version set the node's collision link
to none and left the rigid body blocks orphaned; loading such a mesh crashed the game
(2026-09-23, ElvenSword.nif: the -1 link was dereferenced as a pointer).

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


def validate(data, name):
    """The NIF must end exactly at its footer (root count and root links) after the last
    block, with every root inside the block list: a truncated or corrupt extraction fails."""
    parsed = blocks(bytes(data))
    kind, off, size = parsed[-1]
    p = off + size
    nroots = struct.unpack_from("<I", data, p)[0]
    roots = struct.unpack_from(f"<{nroots}i", data, p + 4)
    if p + 4 + 4 * nroots != len(data) or not roots or any(r < 0 or r >= len(parsed) for r in roots):
        raise ValueError(f"{name}: bad NIF footer (roots {roots}, {len(data) - p} trailing bytes)")
    if parsed[0][0] not in ("BSFadeNode", "NiNode", "BSLeafAnimNode", "BSTreeNode"):
        raise ValueError(f"{name}: unexpected root block {parsed[0][0]}")


def make_fixed(data):
    """Make every rigid body fixed in place, in place. Returns how many were changed."""
    fixed = 0
    for kind, off, size in blocks(bytes(data)):
        if not kind.startswith("bhkRigidBody"):
            continue
        if size < 232:
            raise ValueError(f"{kind} of {size} bytes: not the SSE bhkRigidBodyCInfo2010 layout")
        data[off + 4] = 1                                   # havok filter layer: STATIC
        data[off + 36] = 1                                  # its copy in the construction info
        for field in (116, 136, 156, 180):                  # inertia diagonal, mass
            struct.pack_into("<f", data, off + field, 0.0)
        data[off + 224:off + 228] = bytes((5, 1, 1, 0))     # motion FIXED, deactivator, solver, quality FIXED
        fixed += 1
    return fixed


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
        validate(data, name)
        make_fixed(data)
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
