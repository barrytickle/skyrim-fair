# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: layered perimeter prototype

Implements Barry's construction-language brief of 2026-09-22. **Deliberately scoped to
a prototype**: the entrance plus two edges, so the new language can be judged against
the old treatment on the same site before it is applied all the way round. Footprint,
size, floor height and the road connection are unchanged, and no stalls, NPCs or
navmesh were started.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 84,310 bytes |
| SHA256 | `27ad659e4a78a8072516aa2eb6d4dd9161cf34e0624fd464be83060f02179d85` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| **Vanilla references disabled** | **0** |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |

## The prototype, edge by edge

`PerimeterWall.PrototypeEdges` selects which compass edges get the new language.
Currently `["N", "W"]`. South and east keep the previous rock-and-cliff treatment, so
the two can be compared directly in game.

## What the new language does

### Not one continuous wall

Masonry is laid in **short stretches with gaps**, not per segment. Each straight run of
the outline is walked, and at each step the generator either lays a stretch of 2-3
segments or leaves a gap, drawn fresh each time so no rhythm establishes itself.
Whether a stretch happens at all is a per-edge probability:

| Edge | Masonry bias |
| --- | --- |
| North (entrance) | 0.70 |
| West | 0.50 |
| South | 0.35 |
| East | 0.25 |

That bias is what makes the fair read as more built on one side than another. The
player should not be able to trace a rectangle.

### Battered, not vertical

The slant is **not a rotation**. A drystone retaining wall is battered by stepping each
course back from the one below, so the face is wider at the foot. Courses of
`Stonewall01` (`0000099B`), the ordinary 256-wide, 175-tall field wall, step out by
`CourseBatter` jittered 0.7 to 1.3 per course. Only about half of stretches get a
second course (`SecondCourseChance` 0.55), which is what makes the number of visual
tiers vary along the edge instead of being uniform.

Verified in the written plugin: 29 wall pieces spread across **seven distinct Z bands**
from -5500 down to -6150, and along the west edge they sit at Y -14571, -14331, -14066,
-13818, -13541, -13327, -13082, -12770, -12548, -12281, -12123 - irregular spacing, not
a ruled line.

### Masonry dies into the bank

Every stretch ends in a **part-buried rock**, sized to 80% of the local drop and sunk 64
below the crest so it reads as embedded rather than dropped on. Six placed. Masonry
never stops in mid-air.

### Varying widths

`OffsetJitter` 96 pushes whole stretches in or out; `AlongJitter` 64 wanders each piece
sideways within its segment. Combined with the jittered batter, no two stretches line
up.

### The entrance, cut into the bank

Low field walls step down beside each stair flight, `FlankWalls` 2 per side with
`FlankWallsBias` 1 extra on one side only, so the approach is never mirrored. Short
walls next to the player rather than cliff faces, which is what the brief asked for.

The staircase itself is three flights of `StonewallTerraceStairs01` at scale 1.3:
a 217-wide flight with a 666 x 224 drystone wall, chained nose to tail so the top tread
of each sits on the bottom tread of the one above. The top tread is exactly on the
floor plane and the foot lands dead on grade.

### The bank below

`DirtCliffs02FieldGrass01` and `DirtCliffsIsland01FieldGrass01` are in the embankment
pool - earth cliffs with grass tops, so the layer below the masonry reads as a grass
and earth bank rather than more bare rock.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Stair flights | 3 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces | 29 |
| Part-buried rocks ending a run | 6 |
| Embankment rocks | 57 |
| Corner stones | 9 |
| Toe rocks | 66 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 139 |
| **Vanilla references placed** | **311** |
| Rejected as oversized | 44 |
| Rejected for blocking the entrance | 68 |
| Rejected for protruding through the market floor | 28 |

## The interior stays clean

The paving guard refuses anything whose crown clears the floor plane and whose mesh
reaches inside the paved footprint eroded by `PavingRimAllowance`. **Nothing stands
proud of the market floor**, verified against the full load order. The ramp and stair
surfaces are guarded the same way, with a tighter `RampRimAllowance` of 32.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
python tools/footprint_audit.py --data <stock Data> --profile <profile> --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Nothing stands proud of the market floor.
- Stair flights meet tread to tread; top tread on the floor plane; foot on grade.
- Wall pieces spread across seven Z bands with irregular spacing - no ruled line.
- **All cell overrides byte-identical to vanilla.**
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

### Accepted deviation

The Tamriel `WRLD` override differs from vanilla in exactly two subrecords: `RNAM`, the
region cache, dropped by design; and `FULL`, where vanilla stores a localised string ID
and this plugin writes the literal `Skyrim` because it is not flagged localised. All
other subrecords match byte for byte.

## Candidate dependency: Whiterun Stone Stairs — inspected, not needed

`external/Whiterun Stone Stairs 147164 1.2 2026-07-10T16-11Z oPpNfU7Iq.7z`, Nexus
147164 version 1.2. Listed without extracting into the project.

| Field | Finding |
| --- | --- |
| Contents | 66 entries: 26 `.nif`, 3 `.dds`, 2 `.xml` (a FOMOD installer), 3 preview `.jpg` |
| Plugin | **none** - there is no `.esp` or `.esl` in the archive |
| New assets | **none** |
| Install options | `common`, `standard`, plus `WR3DSW`, `FYX_Guard_Towers` and `Water_in_wells` compatibility variants |
| Readme / licence | **not present in the archive**; permissions are whatever the Nexus page states |

**It is a pure replacer.** Every mesh sits at an existing vanilla path under
`meshes/architecture/whiterun/` - `wrstairswater01`, `wrcastlestairs01`,
`wrpondstairs01/02`, `wrstairsplatform01`, `wrmainroadmarket`,
`wrgreathouseplatform01` and so on. It adds no mesh Skyrim Fair could place that does
not already exist in the base game.

**Consequences, and they are good ones:**

- **No dependency is required or declared.** Skyrim Fair references vanilla game paths
  and FormIDs. If Barry installs this, his copy wins at runtime and anything we
  reference simply looks better. That is the same relationship the project already has
  with Blended Roads for `road01.dds` and Nordic Stonewalls for `Stonewall01`.
- **Nothing is redistributed**, so its permissions do not constrain the project. The
  archive carries no licence text anyway, which is a reason not to depend on it.
- Worth knowing: it replaces `wrmainroadmarket.nif`, which is the mesh the 256-unit UV
  scale was originally measured from.

**Does it beat stock for the entrance?** It cannot, because it *is* stock, improved.
The real question is whether Whiterun **city** stairs suit the fair better than the
**farm** drystone terrace stairs now in use, and the answer is no: the meshes this mod
touches are city platform and castle approach pieces built into Whiterun's own terrain,
not free-standing flights, and the fair's story is a rural rise beside the road rather
than a city plaza. `StonewallTerraceStairs01` stays.

## Known and deliberately not done

- **The prototype covers north and west only.** South and east keep the older treatment
  on purpose, for comparison. Rolling out means adding those names to `PrototypeEdges`.
- **No timber fencing on the top edge yet.** `WRFenceStr01` (106 tall) and
  `WRFenceBaseRubble01` (93 tall, a rubble retaining base) are audited and available;
  they are the intended next step for breaking the paving-edge silhouette.
- **No bunting and no pavilion.** Both confirmed absent from vanilla; both need
  authoring.
- NGIO grass cache not regenerated, so grass still grows through the paving.
- No navmesh, so NPCs cannot use the terrace or the stairs.

## What Barry should test in game

1. Walk the **north and west** edges, then the **south and east** ones. The first two
   are the new language; the last two are the old. Is the difference worth rolling out?
2. Does the masonry read as **short runs dying into rock**, or is it still a border?
3. Does the batter read as a leaning wall holding back earth, or still as a face?
4. Coming up the stairs, does the route feel **cut into the bank**?
5. Can you still mentally trace a rectangle around the fair? If yes, where?
6. Is the market floor still clean and usable?
