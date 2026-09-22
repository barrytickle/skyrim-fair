# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: entrance and foundation correction

Implements Barry's correction brief of 2026-09-22, written against two in-game
screenshots: the entrance read as a fortified gate, and the staircase could not be
climbed. **Scoped to the entrance and its surrounding bank.** No stalls, NPCs, navmesh,
stage content or expansion. Footprint, floor height, stair width and road connection
are unchanged.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 79,554 bytes |
| SHA256 | `e948fd103def5925e81332ea55ccb6104480201e348cc1f19d53563b41ced019` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | all 10 NIFs under `meshes\SkyrimFair\`, hashes identical to `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| **Vanilla references disabled** | **0** |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps (see note below) |

## The two faults, and what fixed them

### The stairs could not be climbed

The corridor had no physical blocker. The cause is the vanilla stair mesh's own
collision: `StonewallTerraceStairs01` carries a `bhkCompressedMeshShape`, and that shape
does not scale reliably with a reference's `XSCL`. At scale 1.0 the flight was climbable;
at 1.3 it was not.

The fix is the one the brief allowed: **a hidden smooth collision ramp under each flight.**

- New kit piece `SkyrimFair_StairCollision` (`assets/blender/build_foundation_kit.py`).
  A 160-wide slab, 192 long, dropping 112, so it matches one flight at scale 1.0
  exactly (30.3 degrees). Its collider is a `bhkBoxShape` rotated to the tread line,
  which is a primitive and does scale correctly. The visible slab is sunk 24 below the
  tread line and wears the earth material, so if a sliver ever shows between steps it
  reads as packed earth under them, not a floating box.
- One placed per flight at the flight's own scale, rotation `rot` (the piece descends
  in local +Y). Verified in the written plugin, reading the ESP independently of the
  generator:

| Flight | Top tread (mesh) | Slab top | Slab bottom |
| --- | --- | --- | --- |
| 1 | `Y -11648, Z -5336` | `Y -11648, Z -5336` | `Y -11398, Z -5482` |
| 2 | `Y -11398, Z -5482` | `Y -11398, Z -5482` | `Y -11149, Z -5627` |
| 3 | `Y -11149, Z -5627` | `Y -11149, Z -5627` | `Y -10899, Z -5773` |

Each slab starts on its flight's top tread and ends on the next flight's top tread. The
slabs are 160 wide inside the 217-wide stair gap, so nothing protrudes beyond the walls.
The stairs were not moved, rescaled or narrowed.

### The entrance read as a fortification

Two things built the gate. The previous pass had added `Stonewall01` flank walls beside
every flight, two on one side and three on the other, on top of the 666-wide drystone
wall each flight already brings. And the north-edge masonry bias was highest of all four
edges, so field wall ran right up to the stairs at the top.

- **Flank walls removed entirely.** The config and code for them are gone.
- **No perimeter masonry within 900 of the stair centreline** on the entrance edge
  (`PerimeterWall.EntranceClear`). The stairs bring their own wall; nothing else is
  laid beside them.
- **The stair walls are buried, not decorated.** The wall is part of the vanilla mesh
  and cannot be removed, so an **entrance bank** is laid against the outer face of each
  flight's wall (`Dressing.EntranceBank`). Every piece is sized to the wall face it
  hides, bedded to the *lowest* ground sampled under it so no edge floats, and capped so
  its crown sits below the wall crest and below the floor plane.
- **The two sides are different by design.** The west side gets closed boulders
  (`RockL02`, `RockL04`, `RockL05`, `RockPileL01`), two per flight. The east side gets one
  grassy hump per flight (`DirtCliffsIsland01FieldGrass01`, the closed, all-round dirt
  cliff), which reads as the earth bank rather than more masonry.

Verified positions of the bank, from the written plugin:

| Side | Flight | Piece | Scale | Position | Crown |
| --- | --- | --- | --- | --- | --- |
| W | 1 | RockPileL01TundraRocks | 1.11 | `-6598, -11470` | `-5416` |
| W | 2 | RockL04 / RockL05 | 0.73 / 1.05 | `-6292, -11196` / `-6488, -11276` | `-5501` / `-5530` |
| W | 3 | RockL02 x2 | 0.75 / 0.67 | `-6386, -11008` / `-6699, -11103` | `-5678` / `-5703` |
| E | 1 | DirtCliffsIsland01FieldGrass01 | 1.20 | `-4788, -11535` | `-5470` |
| E | 2 | DirtCliffsIsland01FieldGrass01 | 1.01 | `-4940, -11329` | `-5532` |
| E | 3 | DirtCliffsIsland01FieldGrass01 | 0.60 | `-5266, -11025` | `-5666` |

Every crown is under the floor plane (-5336) and under its wall's crest; every base is
at least 40 under local grade.

## Open-backed cliff pieces

`DirtCliffs01` and `DirtCliffs02` were measured from the extracted vanilla meshes
(`tools/bsa_extract.py`, then a face-direction histogram): both are one-sided strips,
with 5 to 7 times more face area on the front than the back. `DirtCliffsIsland01` and
the `RockPile` family are modelled all round.

- **`DirtCliffs02FieldGrass01` is out of the free-spinning embankment pool.** Spun at
  random it could present its open back to the player. The entrance bank draws only
  from closed pieces.
- **The cliff skins had exposed open ENDS.** The strip is 2430 long at the scale used,
  and it overhangs the 3 or 4 segment runs it skins by 190 to 450 at each end. At a
  convex corner that overhang stuck out past the corner with the hollow shell showing
  from the side face; the east face was doing exactly this at both ends. Now each end is
  checked: at a re-entrant corner the overhang runs into the neighbouring body and is
  buried; at a convex corner the piece is slid toward a buried end so the exposed end is
  tucked 96 inside the corner (`CliffEndInset`); if neither end can be buried the skin
  is not placed and rocks cover the run. Result: the west skin is kept and slid north
  287; the east skin is refused (counter `CliffEndSkipped` = 1).

## A determinism bug, fixed

The per-edge random stream for the layered masonry was seeded from
`string.GetHashCode()`. .NET randomises string hashes per process, so **the masonry came
out differently on every run of the generator** - which is why the drystone course count
in earlier audits wandered. It is now seeded from a fixed per-edge salt table. Two
consecutive runs of the generator now produce byte-identical plugins (verified by
SHA256).

## The floor material - a conflict to flag

The brief says `WRStoneFloor02` remains the active floor material. **The deployed floor
is not `WRStoneFloor02`.** The paving caps reference vanilla `road01.dds` worn earth,
which Barry chose explicitly on 2026-09-22 ("Worn earth/gravel throughout") after
seeing the stone floor in game. This pass did not touch the floor material either way.
If Barry wants the stone floor back, it is one setting: `PAVING_MATERIAL_MODE` in
`assets/blender/build_foundation_kit.py`, then rebuild the kit.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Stair flights / hidden collision slabs | 3 / 3 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces | 12 |
| Part-buried rocks ending a run | 4 |
| Entrance bank pieces | 8 |
| Cliff skins placed / refused for an open end | 1 / 1 |
| Embankment rocks | 71 |
| Corner stones | 9 |
| Toe rocks | 69 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 138 |
| **Vanilla references placed** | **315** |
| Rejected as oversized | 25 |
| Rejected for blocking the entrance | 74 |
| Rejected for protruding through the market floor | 32 |

The masonry count is low on purpose: nothing within 900 of the stairs, and the prototype
still covers north and west only. The west edge carries most of what remains.

## Guards, and how they changed

- **Paving guard unchanged.** The market floor is eroded by `PavingRimAllowance` 144 on
  outer edges; anything whose crown clears the floor plane and whose mesh reaches inside
  is refused.
- **Stair surface guard narrowed to the steps.** It previously protected the whole
  512-wide tile per flight, which refused the very bank meant to bury the walls. It now
  protects the 217-wide gap plus `RampRimAllowance` 32, from one inset in front of the
  origin for one flight run, with the tread slope of the stairs rather than the ramp's.
  Plain ramp tiles keep the old full-tile rectangle.
- **Entrance channel unchanged** (800 wide, plus the landing). The bank does not use it
  as its test, because the stair walls themselves sit inside it; the bank's test is the
  stair gap.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Kit build: all 13 pieces wound outward (`inward=0`); `SkyrimFair_StairCollision`
  collider reported as a Box at 30.3 degrees.
- Generator run twice: identical SHA256 both times.
- `footprint_audit.py` against the full load order: **0 vanilla references standing
  proud of the floor** on the foundation; 40 intersect the footprint, all buried by 200
  or more.
- Independent read of the written ESP (no generator code): every vanilla-based
  placement's crown and footprint disc checked against the eroded market floor and the
  stair gap. **One hit: the deliberate `SMarketStall01` test stall.** Nothing else on the
  floor, nothing in the stair gap.
- Stair flights meet tread to tread; each collision slab spans exactly one flight.
- **All four cell overrides byte-identical to vanilla.**
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

### Accepted deviation

The Tamriel `WRLD` override differs from vanilla in exactly two subrecords: `RNAM`, the
region cache, dropped by design; and `FULL`, where vanilla stores a localised string ID
and this plugin writes the literal `Skyrim` because it is not flagged localised. All
other subrecords match byte for byte.

## Debug aid

`SKYRIMFAIR_TRACE=1` in the environment makes the generator print every entrance-bank
decision (piece, side, scale, reach, position, crown, ground, or why it was refused) to
stderr. Nothing else changes.

## Known and deliberately not done

- **The perimeter prototype still covers north and west only.** South and east keep the
  older treatment for comparison. Rolling out means adding those names to
  `PerimeterWall.PrototypeEdges`.
- **The east face has no cliff skin now.** Its run is too short to bury both ends of the
  strip; rocks cover it. If a skin is wanted there, the mesh would need a run of 5
  segments, or a shorter cliff piece.
- **No timber fencing on the top edge yet.** `WRFenceStr01` and `WRFenceBaseRubble01`
  remain the intended next step for breaking the paving-edge silhouette.
- **No bunting and no pavilion.** Both confirmed absent from vanilla; both need
  authoring.
- NGIO grass cache not regenerated, so grass still grows through the paving.
- No navmesh, so NPCs cannot use the terrace or the stairs.
- An untracked `music/` folder exists in the working tree. It was not added to git: its
  provenance is unknown and it may be third-party audio.

## What Barry should test in game

1. **Walk up the stairs from the road.** You should reach the terrace without jumping.
   If you sink slightly into a tread, that is the slab sitting on the tread line under a
   nosing; say so and it can be lifted a few units.
2. From the road, does the entrance still read as a gate? What you should see is steps
   cut into a bank: boulders on the left, a grass hump on the right, a low parapet line
   where the top flight's wall shows above the paving.
3. Look back at the terrace from the north-east and south-east. **No hollow cliff back
   or open end should be visible anywhere.**
4. Is the market floor still clean?
5. Does the top of the stairs meet the floor cleanly, with nothing standing on the
   paving at the head?
