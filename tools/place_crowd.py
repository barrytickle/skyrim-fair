"""Plan where the static crowd figures stand, and append them to fair.config.json.

Reads what the generator last built:
- build/crowd_sites.json (fairWorld.crowdSitesDump): benches (and whether a real sitter
  uses each), the horse pen's fence rails, and every enabled actor
- build/navmesh_raster.txt (fairWorld.navmesh.debugRaster): the walkable ground
- tools/crowd/figures.json: each figure's STAT entry

It writes new `crowdFigures` definitions (one per figure not yet defined) and **appends**
`crowdPlacements`. Placements already in the config are never moved or reordered: they
fix record order, and so FormIDs. Run it once per placing pass, then build again. It
isn't part of the normal build, so later layout changes can't shift figures already
placed.

Barry's rules (docs/CLAUDE_CROWD_LIBRARY_TASK.md):
- background and midground, mixed with real NPCs, never beside the lanes the player walks
- every copy's heading varied
- no two copies of one figure near each other
- seated figures on a seat marker of a bench no real NPC uses (the generator makes that
  bench non-sittable)
- leaners 34 units out from the pen's rail, facing it

    python tools/place_crowd.py            # plan and append
    python tools/place_crowd.py --dry-run  # print the plan only
"""
import argparse, hashlib, json, math, pathlib, sys
from collections import deque

ROOT = pathlib.Path(__file__).resolve().parents[1]
PREFIX = "SkyrimFairCrowd"

# Zones: rectangles [x0, y0, x1, y1] to fill, the point the figures look toward, and the
# figures to use there (each copy picks the next that isn't already standing close by).
ZONES = [
    {"name": "archery, behind the spectators", "rects": [[530, 950, 830, 1960]], "focus": [-450, None],
     "count": 13, "figures": ["LookFar01", "Pointing01", "Clapping02", "Cheering01", "ArmsCrossed01", "LookFar02",
                              "Clapping03", "Pointing02", "Cheering02", "ArmsCrossed02", "Clapping04", "Cheering03",
                              "HandsBehind01"]},
    {"name": "stage, west of the audience", "rects": [[700, 4250, 1150, 5250]], "focus": [2048, 4650],
     "count": 10, "figures": ["ClappingHigh01", "Tankard01", "Cheering02", "Toast01", "Waving01", "Laughing02",
                              "Talking01", "HandsOnHips01", "Tankard03", "Standing02"]},
    {"name": "stage, east of the audience", "rects": [[2950, 4250, 3400, 5250]], "focus": [2048, 4650],
     "count": 10, "figures": ["ClappingHigh02", "Tankard02", "Cheering03", "Toast02", "Waving02", "Laughing01",
                              "Talking02", "HandsOnHips02", "Cheering01", "Standing01"]},
    {"name": "stage, south of the audience", "rects": [[1250, 3860, 1780, 4080], [2320, 3860, 2850, 4080]],
     "focus": [2048, 4700], "count": 8,
     "figures": ["Tankard01", "ClappingHigh02", "Toast02", "Cheering03", "Tankard02", "ClappingHigh01",
                 "Waving01", "Toast01"]},
]
# Kept clear: the lanes the player walks, and the stage steps.
LANES = [[1830, -2000, 2270, 4000], [3560, -2000, 4000, 4000], [1650, 4450, 2480, 5600]]
SEATED = {"count": 7, "figures": ["Seated01", "Seated02", "Seated03"],
          # Prefer seats by the archery range and round the stage, then the market.
          "prefer": [[-400, 500, 900, 2300], [300, 3900, 3800, 5600]]}
LEANING = {"figures": ["Leaning01", "Leaning02", "Leaning01"], "pen": [300, 150], "out": 34}

# Collision boxes [minX, minY, maxX, maxY, height] in the figure's frame: the body, not the arms.
BODY = [-18, -14, 18, 14]
LEAN_BODY = [-18, -6, 18, 26]

MIN_ACTOR = 65      # from any real NPC
MIN_FIGURE = 75     # from any other figure
MIN_SAME = 700      # between copies of one figure
JITTER = 28         # degrees either side of facing the focus


def h(*parts):
    return int(hashlib.sha256("|".join(map(str, parts)).encode()).hexdigest()[:12], 16) / float(16 ** 12)


def load_raster(path):
    lines = path.read_text(encoding="utf-8").split("\n")
    ix0, iy0, res, w, hh = map(int, lines[0].split(","))
    grid = [lines[1 + j] for j in range(hh)]
    # The main walkable area: the largest connected run of kept ground ('a').
    seen, best = set(), set()
    for j in range(hh):
        for i in range(w):
            if grid[j][i] == "a" and (i, j) not in seen:
                comp, q = set(), deque([(i, j)])
                seen.add((i, j))
                while q:
                    a, b = q.popleft()
                    comp.add((a, b))
                    for da, db in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        na, nb = a + da, b + db
                        if 0 <= na < w and 0 <= nb < hh and (na, nb) not in seen and grid[nb][na] == "a":
                            seen.add((na, nb))
                            q.append((na, nb))
                if len(comp) > len(best):
                    best = comp

    def open_ground(x, y, reach=1):
        i, j = int(math.floor(x / res)) - ix0, int(math.floor(y / res)) - iy0
        return all((i + a, j + b) in best for a in range(-reach, reach + 1) for b in range(-reach, reach + 1))
    return open_ground


def heading_to(x, y, fx, fy):
    return math.degrees(math.atan2(fx - x, fy - y)) % 360


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--config", default=str(ROOT / "fair.config.json"))
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    cfg_path = pathlib.Path(args.config)
    text = cfg_path.read_text(encoding="utf-8")
    cfg = json.loads(text)
    world = cfg["fairWorld"]
    sites = json.loads((ROOT / world["crowdSitesDump"]).read_text(encoding="utf-8"))
    open_ground = load_raster(ROOT / world["navmesh"]["debugRaster"])
    figures = {r["editorId"]: r for r in json.loads((ROOT / "tools" / "crowd" / "figures.json").read_text(encoding="utf-8"))}

    actors = [tuple(a) for a in sites["actors"]]
    existing = world.get("crowdPlacements", [])
    placed = []  # (x, y, figure)
    for p in existing:
        if p.get("at"):
            placed.append((p["at"][0], p["at"][1], p["figure"]))
        elif p.get("seat"):
            placed.append((p["seat"][0], p["seat"][1], p["figure"]))
    new = []

    def clear(x, y, fig, min_actor=MIN_ACTOR):
        if any(l[0] <= x <= l[2] and l[1] <= y <= l[3] for l in LANES):
            return False
        if any((ax - x) ** 2 + (ay - y) ** 2 < min_actor ** 2 for ax, ay in actors):
            return False
        for px, py, pf in placed:
            d2 = (px - x) ** 2 + (py - y) ** 2
            if d2 < MIN_FIGURE ** 2 or (pf == fig and d2 < MIN_SAME ** 2):
                return False
        return True

    # ---- standing figures, zone by zone
    for zone in ZONES:
        cands = []
        for r in zone["rects"]:
            for x in range(int(r[0]), int(r[2]) + 1, 16):
                for y in range(int(r[1]), int(r[3]) + 1, 16):
                    if open_ground(x, y):
                        cands.append((h(zone["name"], x, y), x, y))
        cands.sort()
        made, k = 0, 0
        figs = [PREFIX + f for f in zone["figures"]]
        for _, x, y in cands:
            if made >= zone["count"]:
                break
            for step in range(len(figs)):
                fig = figs[(k + step) % len(figs)]
                if clear(x, y, fig):
                    fx, fy = zone["focus"]
                    fy = y if fy is None else fy
                    yaw = round((heading_to(x, y, fx, fy) + (h("yaw", x, y) * 2 - 1) * JITTER) % 360)
                    new.append({"figure": fig, "at": [x, y, yaw]})
                    placed.append((x, y, fig))
                    made += 1
                    k += step + 1
                    break
        print(f"  {zone['name']}: {made} of {zone['count']}")

    # ---- seated figures, on seats no real NPC uses
    def key(form):  # "00074EC6:Skyrim.esm" and "074EC6:Skyrim.esm" are the same FormKey
        fid, plugin = form.split(":")
        return int(fid, 16), plugin.lower()
    markers = {key(m["furniture"]): m["markers"] for m in world["seatMarkers"]}
    used_seats = {(p["seat"][0], p["seat"][1]) for p in existing if p.get("seat")}

    def preferred(s):
        for rank, r in enumerate(SEATED["prefer"]):
            if r[0] <= s["x"] <= r[2] and r[1] <= s["y"] <= r[3]:
                return rank
        return len(SEATED["prefer"])
    seats = sorted((s for s in sites["seats"] if not s["taken"] and (s["x"], s["y"]) not in used_seats),
                   key=lambda s: (preferred(s), h("seat", round(s["x"]), round(s["y"]))))
    made, k = 0, 0
    for s in seats:
        if made >= SEATED["count"]:
            break
        fig = PREFIX + SEATED["figures"][k % len(SEATED["figures"])]
        # A marker away from anyone standing close; one figure per bench.
        ms = markers[key(s["furniture"])]
        m = int(h("marker", s["x"], s["y"]) * len(ms))
        yaw = math.radians(s["yaw"])
        mx = s["x"] + ms[m][0] * math.cos(yaw) + ms[m][1] * math.sin(yaw)
        my = s["y"] - ms[m][0] * math.sin(yaw) + ms[m][1] * math.cos(yaw)
        if not clear(mx, my, fig, min_actor=50):
            continue
        new.append({"figure": fig, "seat": [round(s["x"], 1), round(s["y"], 1)], "marker": m})
        placed.append((mx, my, fig))
        made += 1
        k += 1
    print(f"  seated: {made} of {SEATED['count']} ({len(seats)} free seats)")

    # ---- leaners on the pen's fence, outside it, facing in
    px, py = LEANING["pen"]
    rails = sorted(sites["rails"], key=lambda r: h("rail", round(r["x"]), round(r["y"])))
    made = 0
    for r in rails:
        if made >= len(LEANING["figures"]):
            break
        yaw = math.radians(r["yaw"])
        along = (math.sin(yaw), math.cos(yaw))        # the rail runs along its local +Y
        normal = (math.cos(yaw), -math.sin(yaw))
        if normal[0] * (px - r["x"]) + normal[1] * (py - r["y"]) > 0:
            normal = (-normal[0], -normal[1])            # point away from the pen
        t = (h("along", r["x"], r["y"]) * 2 - 1) * 60
        x = r["x"] + along[0] * t + normal[0] * LEANING["out"]
        y = r["y"] + along[1] * t + normal[1] * LEANING["out"]
        fig = PREFIX + LEANING["figures"][made]
        # The fence's own padding blocks the raster right beside it, so check the ground just
        # behind the leaner instead: open there means they're outside the pen, on the lane side.
        bx, by = x + normal[0] * 40, y + normal[1] * 40
        if not open_ground(bx, by, reach=0) or not clear(x, y, fig, min_actor=55):
            continue
        face = math.degrees(math.atan2(-normal[0], -normal[1])) % 360
        new.append({"figure": fig, "at": [round(x), round(y), round(face)]})
        placed.append((x, y, fig))
        made += 1
    print(f"  leaning on the pen: {made} of {len(LEANING['figures'])}")

    # ---- definitions for figures not yet defined, appended
    defined = {f["editorId"] for f in world.get("crowdFigures", [])}
    defs = []
    for p in new:
        f = p["figure"]
        if f in defined:
            continue
        defined.add(f)
        row = figures[f]
        seated = row["pose"] == "seated"
        body = LEAN_BODY if row["pose"] == "leaning" else BODY
        d = {"editorId": f, "model": row["model"], "width": row["width"], "depth": row["depth"], "height": row["height"]}
        if seated:
            d["solid"] = False        # the bench under it is solid already
        else:
            d["collision"] = body + [row["height"]]
        defs.append(d)

    print(f"{len(new)} new placements, {len(defs)} new figure definitions")
    if args.dry_run:
        for p in new:
            print("   ", json.dumps(p))
        return

    # Append as text, so the rest of the file keeps its layout.
    def append_to(text, key, items):
        start = text.index(f'"{key}": [')
        depth, i = 0, text.index("[", start)
        while True:
            c = text[i]
            if c == "[":
                depth += 1
            elif c == "]":
                depth -= 1
                if depth == 0:
                    break
            i += 1
        body = text[text.index("[", start) + 1:i].rstrip()
        sep = "," if body.strip() else ""
        add = "".join(f"{sep if n == 0 else ','}\n      {json.dumps(it)}" for n, it in enumerate(items))
        return text[:text.index("[", start) + 1] + body + add + "\n    " + text[i:]
    if defs:
        text = append_to(text, "crowdFigures", defs)
    if new:
        text = append_to(text, "crowdPlacements", new)
    json.loads(text)
    cfg_path.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
