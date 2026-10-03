"""Per-model footprints for the fair's navmesh (docs/NAVMESH.md).

The generator lists every model it cuts an obstacle for (build/navmesh_models.txt). This
reads each one's geometry and records which 16-unit cells of its local plan hold any, and
at which heights, as a bitmask of 16-unit bands from -64 to 448. The generator then
blocks the navmesh only where an object's geometry stands between the ground and head
height: a stall's posts and counter, not the open space under its roof; a fence's rails,
not the pen inside it. Models it can't read fall back to their bounds in the generator.

Models are found, in order, loose in the repo's assets/, loose in each --extra folder,
in each --extra folder's BSAs, then in the stock Data BSAs. Deterministic: sorted output.

    python tools/make_footprints.py --data "E:/Modlists/Still In Skyrim/stock/Data" \\
        --extra "E:/Modlists/Still In Skyrim/mods/Holidays"

Run it after a generator build that changed what's placed, then build again.
"""
import argparse
import json
import math
import os
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
import bsa_extract  # noqa: E402
sys.argv, _argv = ["x"], sys.argv
import nif_preview  # noqa: E402
sys.argv = _argv

CELL = 16.0
BAND = 16.0
BAND_BASE = -64.0


def footprint(tris):
    """(ix, iy) -> band mask, from triangles sampled at half a cell."""
    cells = {}
    step = CELL / 2
    for a, b, c in tris:
        edges = [math.dist(a, b), math.dist(b, c), math.dist(c, a)]
        n = max(1, int(math.ceil(max(edges) / step)))
        for i in range(n + 1):
            for j in range(n + 1 - i):
                u, v = i / n, j / n
                w = 1 - u - v
                x = a[0] * w + b[0] * u + c[0] * v
                y = a[1] * w + b[1] * u + c[1] * v
                z = a[2] * w + b[2] * u + c[2] * v
                band = int(math.floor((z - BAND_BASE) / BAND))
                if band < 0 or band > 31:
                    continue
                key = (int(math.floor(x / CELL)), int(math.floor(y / CELL)))
                cells[key] = cells.get(key, 0) | (1 << band)
    return cells


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", required=True, help="stock Skyrim Data (the vanilla BSAs)")
    ap.add_argument("--extra", action="append", default=[], help="a mod folder: loose meshes and BSAs")
    ap.add_argument("--models", default=str(ROOT / "build" / "navmesh_models.txt"))
    ap.add_argument("--out", default=str(ROOT / "tools" / "navmesh_footprints.json"))
    args = ap.parse_args()

    models = [m.strip() for m in open(args.models, encoding="utf-8") if m.strip()]
    loose = [ROOT / "assets"] + [pathlib.Path(e) for e in args.extra]
    indexes = [bsa_extract.build_index(e) for e in args.extra] + [bsa_extract.build_index(args.data)]
    scratch = ROOT / "build" / "footprint_nifs"
    scratch.mkdir(parents=True, exist_ok=True)

    out, missing, failed = {}, [], []
    for model in models:
        rel = model.replace("\\", "/")
        path = next((str(d / rel) for d in loose if (d / rel).is_file()), None)
        if path is None:
            entry = next((ix[rel] for ix in indexes if rel in ix), None)
            if entry is None:
                missing.append(model)
                continue
            path = str(scratch / (rel.replace("/", "_")))
            with open(path, "wb") as f:
                f.write(bsa_extract.extract(entry))
        try:
            tris = nif_preview.triangles(path)
        except Exception as exc:  # noqa: BLE001
            failed.append(f"{model}: {exc}")
            continue
        if not tris:
            failed.append(f"{model}: no geometry")
            continue
        cells = footprint(tris)
        out[model] = [[x, y, m] for (x, y), m in sorted(cells.items())]

    doc = {"cell": CELL, "band": BAND, "bandBase": BAND_BASE, "models": dict(sorted(out.items()))}
    with open(args.out, "w", encoding="utf-8") as f:
        json.dump(doc, f, separators=(",", ":"))
        f.write("\n")
    print(f"{len(out)} footprints written to {args.out}; {len(missing)} models not found, {len(failed)} unreadable")
    for m in missing:
        print(f"  not found: {m}")
    for m in failed:
        print(f"  unreadable: {m}")


if __name__ == "__main__":
    main()
