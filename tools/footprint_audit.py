"""Audit what intersects the fair footprint, and classify it.

Standalone: reads plugins directly, needs no Mutagen and no game running.

    python tools/footprint_audit.py --data "E:\\Modlists\\Still In Skyrim\\stock\\Data" \\
        --profile "E:\\Modlists\\Still In Skyrim\\profiles\\Still in Skyrim Plus" \\
        --mods "E:\\Modlists\\Still In Skyrim\\mods" \\
        --esp dist/SkyrimFair.esp

Two things this gets right that a naive audit does not:

1.  **Implicit masters.** MO2's plugins.txt omits Skyrim.esm and the other base
    masters. Forgetting to prepend them silently drops the entire vanilla layer -
    an earlier pass of this audit reported 30 intersecting references instead of
    101 for exactly that reason.

2.  **Mesh extent, not origin.** Objects are tested by their OBND bounds, scaled
    and Z-rotated into world space. RockTundraLand02Tundra01 has a 1767-unit
    radius, so its origin can sit a whole cell away while its geometry still
    blankets the footprint.
"""

import argparse
import collections
import math
import os
import re
import struct
import zlib

REC_HDR = 24
CELL_SIZE = 4096

IMPLICIT_MASTERS = [
    "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm",
]

BASE_TYPES = {
    b"STAT", b"TREE", b"FLOR", b"MSTT", b"ACTI", b"CONT", b"FURN", b"DOOR", b"LIGH",
    b"SCOL", b"MISC", b"ALCH", b"INGR", b"WEAP", b"ARMO", b"BOOK", b"AMMO", b"KEYM",
    b"SLGM", b"NPC_", b"LVLN", b"TACT",
}

# --- classification ---------------------------------------------------------
#
# Texture and season variants are appended to Bethesda EditorIDs, and they lie:
# "RockTundraLand02FieldGrass01" is a 1767-unit rock slab, not grass, and
# "DirtCliffs01FieldGrass01" is an 899-unit earth cliff. Matching raw substrings
# files both under vegetation. So variant suffixes are stripped before any
# keyword test, and mesh radius outranks keywords entirely.
VARIANT_SUFFIX = re.compile(
    r"(FieldGrass\d*|Tundra\d*|Snow\d*|Moss|NoGrass|Weathered|Ice|Dirt\d*|"
    r"_LightSN|LightSN|Reach\d*|Fall\d*|Pine\d*|Marsh\d*)+$", re.I)

WATER = ("stream", "rapids", "water", "river", "falls", "marsh", "pond", "lake", "kelp")
ROCK = ("rock", "boulder", "cliff", "scree", "stone", "rubble")
VEG = ("tree", "shrub", "grass", "flora", "scrub", "cotton", "thicket", "fern",
       "plant", "mushroom", "bush", "lavender", "flower")

LARGE_RADIUS = 600.0

UNSAFE_SUBRECORDS = {
    "XESP": "enable-parented", "XLKR": "linked reference", "VMAD": "has script",
    "XOWN": "owned", "XTEL": "teleport door", "XPRM": "primitive",
    "XLRT": "location ref type", "XLCN": "linked location", "XEZN": "encounter zone",
    "XAPR": "activate parent", "XPWR": "water link", "XCNT": "item count",
}


def core_name(editor_id):
    """EditorID with trailing texture/season variant suffixes removed."""
    if not editor_id:
        return ""
    return VARIANT_SUFFIX.sub("", editor_id)


def classify(editor_id, base_type, radius, reasons):
    """Category for one placed reference. Radius and record type beat keywords."""
    name = core_name(editor_id).lower()

    if any(k in name for k in WATER):
        return "water / stream"
    if "initially-disabled" in reasons:
        return "disabled by Skyrim Fair"
    if reasons:
        return "UNSAFE - do not touch"

    # A rock or earth mass is what it is regardless of the grass painted on it.
    if any(k in name for k in ROCK):
        return "large rock / earth mass" if radius >= LARGE_RADIUS else "rock / boulder"
    if base_type in ("TREE", "FLOR") or any(k in name for k in VEG):
        return "vegetation"
    if radius >= LARGE_RADIUS:
        return "large environment piece"
    return "decorative clutter"


# --- plugin reading ---------------------------------------------------------

def read_subrecords(data):
    i, n, pending = 0, len(data), None
    while i + 6 <= n:
        typ = data[i:i + 4]
        size = struct.unpack_from("<H", data, i + 4)[0]
        i += 6
        if typ == b"XXXX":
            pending = struct.unpack_from("<I", data, i)[0]
            i += size
            continue
        if pending is not None:
            size, pending = pending, None
        yield typ, data[i:i + size]
        i += size


def record_data(handle, size, flags):
    raw = handle.read(size)
    if flags & 0x00040000:
        try:
            return zlib.decompress(raw[4:])
        except Exception:
            return b""
    return raw


class Plugin:
    def __init__(self, path):
        self.path = path
        self.name = os.path.basename(path)
        self.masters = []

    def resolve(self, form_id):
        index, local = form_id >> 24, form_id & 0xFFFFFF
        if index == 0xFE:
            return None, local
        if index < len(self.masters):
            return self.masters[index], local
        return self.name, local

    def key(self, form_id):
        owner, local = self.resolve(form_id)
        return f"{local:06X}:{owner}"


def open_plugin(path):
    plugin = Plugin(path)
    handle = open(path, "rb")
    header = handle.read(REC_HDR)
    if header[:4] != b"TES4":
        handle.close()
        return None, None
    size = struct.unpack_from("<I", header, 4)[0]
    for typ, data in read_subrecords(record_data(handle, size, 0)):
        if typ == b"MAST":
            plugin.masters.append(data.split(b"\0")[0].decode("cp1252"))
    return plugin, handle


def load_order(profile, mods, data_dir):
    enabled = [
        line.rstrip("\n")[1:]
        for line in open(os.path.join(profile, "modlist.txt"), encoding="utf-8")
        if line.startswith("+") and not line.rstrip().endswith("_separator")
    ]
    active = [
        line.strip()[1:]
        for line in open(os.path.join(profile, "plugins.txt"), encoding="utf-8")
        if line.startswith("*")
    ]
    order = IMPLICIT_MASTERS + [p for p in active if p not in IMPLICIT_MASTERS]
    index = {}
    folders = [os.path.join(mods, m) for m in enabled] + [data_dir]
    for folder in folders:
        if not os.path.isdir(folder):
            continue
        for entry in os.listdir(folder):
            if entry.lower().endswith((".esp", ".esm", ".esl")):
                index.setdefault(entry.lower(), os.path.join(folder, entry))
    return order, index


def scan_bases(path):
    """FormKey -> (record type, EditorID, OBND, has script)."""
    plugin, handle = open_plugin(path)
    if plugin is None:
        return {}
    out, size = {}, os.path.getsize(path)
    while handle.tell() < size:
        pos = handle.tell()
        head = handle.read(REC_HDR)
        if len(head) < REC_HDR or head[:4] != b"GRUP":
            break
        group_size, label, _ = struct.unpack_from("<IIi", head, 4)
        end = pos + group_size
        if label.to_bytes(4, "little") not in BASE_TYPES:
            handle.seek(end)
            continue
        top = label.to_bytes(4, "little").decode()
        while handle.tell() < end:
            start = handle.tell()
            rec = handle.read(REC_HDR)
            if len(rec) < REC_HDR:
                break
            rec_size, flags, form_id = struct.unpack_from("<III", rec, 4)
            if rec[:4] == b"GRUP":
                handle.seek(start + rec_size)
                continue
            data = record_data(handle, rec_size, flags)
            editor_id, bounds, scripted = None, None, False
            for typ, sub in read_subrecords(data):
                if typ == b"EDID":
                    editor_id = sub.split(b"\0")[0].decode("cp1252", "replace")
                elif typ == b"OBND" and len(sub) >= 12:
                    bounds = struct.unpack_from("<6h", sub, 0)
                elif typ == b"VMAD":
                    scripted = True
            out[plugin.key(form_id)] = (top, editor_id, bounds, scripted)
            handle.seek(start + REC_HDR + rec_size)
    handle.close()
    return out


def scan_references(path, cells, out):
    """Placed references in the given Tamriel cells; later plugins overwrite."""
    plugin, handle = open_plugin(path)
    if plugin is None:
        return
    size = os.path.getsize(path)

    def tamriel(label):
        owner, local = plugin.resolve(label)
        return owner is not None and owner.lower() == "skyrim.esm" and local == 0x3C

    def walk(end, world=None, cell=None):
        while handle.tell() < end:
            pos = handle.tell()
            head = handle.read(REC_HDR)
            if len(head) < REC_HDR:
                return
            if head[:4] == b"GRUP":
                group_size, label, group_type = struct.unpack_from("<IIi", head, 4)
                stop = pos + group_size
                if group_type == 0 and label.to_bytes(4, "little") == b"WRLD":
                    walk(stop, world, cell)
                elif group_type == 1:
                    walk(stop, "T", cell) if tamriel(label) else handle.seek(stop)
                elif group_type in (4, 5) and world == "T":
                    walk(stop, world, cell)
                elif group_type in (6, 8, 9) and world == "T" and cell in cells:
                    walk(stop, world, cell)
                else:
                    handle.seek(stop)
                continue

            rec_size, flags, form_id = struct.unpack_from("<III", head, 4)
            typ = head[:4]
            if world == "T" and typ == b"CELL":
                data = record_data(handle, rec_size, flags)
                for sub_type, sub in read_subrecords(data):
                    if sub_type == b"XCLC" and len(sub) >= 8:
                        cell = struct.unpack_from("<ii", sub, 0)
            elif world == "T" and typ in (b"REFR", b"ACHR") and cell in cells:
                data = record_data(handle, rec_size, flags)
                subs = {}
                for sub_type, sub in read_subrecords(data):
                    subs.setdefault(sub_type, sub)
                placement, base = subs.get(b"DATA"), subs.get(b"NAME")
                if placement and base and len(placement) >= 24:
                    out[plugin.key(form_id)] = {
                        "source": plugin.name,
                        "record": typ.decode(),
                        "flags": flags,
                        "cell": list(cell),
                        "base": plugin.key(struct.unpack_from("<I", base, 0)[0]),
                        "pos": list(struct.unpack_from("<3f", placement, 0)),
                        "rot_z": struct.unpack_from("<3f", placement, 12)[2],
                        "scale": (struct.unpack("<f", subs[b"XSCL"])[0]
                                  if b"XSCL" in subs else 1.0),
                        "subs": sorted(k.decode() for k in subs),
                    }
            else:
                handle.seek(pos + REC_HDR + rec_size)

    walk(size)
    handle.close()


# --- footprint --------------------------------------------------------------

PIECE_SIZE = {
    "SkyrimFairFloorFill1024": (1024, 1024),
    "SkyrimFairFloorEdge512": (512, 512),
}


def read_footprint(esp):
    """Paved tile rects, read from the plugin's own placements."""
    plugin, handle = open_plugin(esp)
    size = os.path.getsize(esp)
    statics, rects = {}, []

    def walk(end):
        while handle.tell() < end:
            pos = handle.tell()
            head = handle.read(REC_HDR)
            if len(head) < REC_HDR:
                return
            if head[:4] == b"GRUP":
                group_size, _, group_type = struct.unpack_from("<IIi", head, 4)
                stop = pos + group_size
                walk(stop) if group_type in (0, 1, 4, 5, 6, 8, 9) else handle.seek(stop)
                continue
            rec_size, flags, form_id = struct.unpack_from("<III", head, 4)
            data = record_data(handle, rec_size, flags)
            subs = {t.decode(): s for t, s in read_subrecords(data)}
            if head[:4] == b"STAT":
                statics[plugin.key(form_id)] = \
                    subs.get("EDID", b"").split(b"\0")[0].decode()
            elif head[:4] == b"REFR" and "NAME" in subs and "DATA" in subs:
                base = plugin.key(struct.unpack_from("<I", subs["NAME"], 0)[0])
                name = statics.get(base)
                if name in PIECE_SIZE and len(subs["DATA"]) >= 12:
                    x, y, _ = struct.unpack_from("<3f", subs["DATA"], 0)
                    w, h = PIECE_SIZE[name]
                    rects.append((x - w / 2, y - h / 2, x + w / 2, y + h / 2))

    walk(size)
    handle.close()
    return rects


def world_bounds(ref, bounds, scale):
    if not bounds:
        return (ref["pos"][0], ref["pos"][1], ref["pos"][0], ref["pos"][1]), 0.0
    x1, y1, _, x2, y2, _ = bounds
    cos_z, sin_z = math.cos(ref["rot_z"]), math.sin(ref["rot_z"])
    xs, ys = [], []
    for ax, ay in ((x1, y1), (x2, y1), (x2, y2), (x1, y2)):
        ax, ay = ax * scale, ay * scale
        xs.append(ref["pos"][0] + ax * cos_z - ay * sin_z)
        ys.append(ref["pos"][1] + ax * sin_z + ay * cos_z)
    radius = math.hypot((x2 - x1) / 2 * scale, (y2 - y1) / 2 * scale)
    return (min(xs), min(ys), max(xs), max(ys)), radius


def overlaps(box, rects, pad=0.0):
    return any(box[0] <= x1 + pad and box[2] >= x0 - pad
               and box[1] <= y1 + pad and box[3] >= y0 - pad
               for x0, y0, x1, y1 in rects)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True, help="Skyrim Data folder MO2 runs")
    parser.add_argument("--profile", required=True, help="MO2 profile folder")
    parser.add_argument("--mods", required=True, help="MO2 mods folder")
    parser.add_argument("--esp", default="dist/SkyrimFair.esp")
    parser.add_argument("--margin", type=float, default=512.0)
    parser.add_argument("--search", type=float, default=2048.0)
    args = parser.parse_args()

    rects = read_footprint(args.esp)
    if not rects:
        raise SystemExit("no paving found in the plugin - nothing to audit")
    min_x = min(r[0] for r in rects) - args.search
    max_x = max(r[2] for r in rects) + args.search
    min_y = min(r[1] for r in rects) - args.search
    max_y = max(r[3] for r in rects) + args.search
    cells = {(cx, cy)
             for cx in range(int(min_x // CELL_SIZE), int(max_x // CELL_SIZE) + 1)
             for cy in range(int(min_y // CELL_SIZE), int(max_y // CELL_SIZE) + 1)}

    order, index = load_order(args.profile, args.mods, args.data)
    bases, winning = {}, {}
    for name in order:
        path = index.get(name.lower())
        if not path:
            continue
        try:
            bases.update(scan_bases(path))
            scan_references(path, cells, winning)
        except Exception:
            continue

    print(f"paving tiles {len(rects)}, cells searched {len(cells)}, "
          f"base records {len(bases)}, winning references {len(winning)}")

    rows = []
    for key, ref in winning.items():
        if key.endswith(":SkyrimFair.esp"):
            continue
        base_type, editor_id, bounds, base_scripted = bases.get(
            ref["base"], ("?", None, None, False))
        box, radius = world_bounds(ref, bounds, ref["scale"])
        if not overlaps(box, rects, args.margin):
            continue
        reasons = []
        if ref["flags"] & 0x400:
            reasons.append("PERSISTENT")
        if ref["flags"] & 0x800:
            reasons.append("initially-disabled")
        reasons += [label for tag, label in UNSAFE_SUBRECORDS.items()
                    if tag in ref["subs"]]
        if base_scripted:
            reasons.append("base has script")
        if ref["record"] == "ACHR" or base_type == "NPC_":
            reasons.append("ACTOR")
        if base_type in ("DOOR", "CONT", "FURN", "TACT"):
            reasons.append(f"base type {base_type}")
        rows.append({
            "key": key, "base": ref["base"], "editor_id": editor_id,
            "base_type": base_type, "source": ref["source"],
            "pos": [round(v) for v in ref["pos"]], "scale": round(ref["scale"], 2),
            "radius": round(radius), "box": [round(v) for v in box],
            "on_foundation": overlaps(box, rects),
            "category": classify(editor_id, base_type, radius, reasons),
            "reasons": reasons,
        })

    on_foundation = sum(1 for r in rows if r["on_foundation"])
    print(f"\nintersecting references {len(rows)}: "
          f"{on_foundation} on the foundation, {len(rows) - on_foundation} margin only\n")
    for category, count in collections.Counter(r["category"] for r in rows).most_common():
        inside = sum(1 for r in rows
                     if r["category"] == category and r["on_foundation"])
        print(f"  {count:4d}  {category}  ({inside} on foundation)")

    order_key = {"disabled by Skyrim Fair": 0, "water / stream": 1,
                 "UNSAFE - do not touch": 2, "large rock / earth mass": 3,
                 "large environment piece": 4, "rock / boulder": 5,
                 "decorative clutter": 6, "vegetation": 7}
    rows.sort(key=lambda r: (order_key.get(r["category"], 9), r["editor_id"] or ""))
    print()
    for row in rows:
        where = "FOUNDATION" if row["on_foundation"] else "margin    "
        note = f"  [{', '.join(row['reasons'])}]" if row["reasons"] else ""
        print(f"  {row['category']:24s} {where} {row['key']:22s} "
              f"{row['base_type']:5s} {str(row['editor_id']):34s} "
              f"r={row['radius']:5d} pos={row['pos']}{note}")


if __name__ == "__main__":
    main()
