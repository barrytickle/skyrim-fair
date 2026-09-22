# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: wall-less staircase, low cheek walls, no cliff pieces

Barry's direction of 2026-09-22, after seeing the previous build in game: **no dirt-cliff
pieces at all** ("they have an invisible piece to them"), **just small rock walls**, and
the two concept images: broad stone steps with low drystone cheeks either side, terraces
of short low walls, boulders, grass and scrub.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 85,389 bytes |
| SHA256 | `8aeba0275e8954ee642bf7c5855578c30552513e0f181d9fbfde6e920522c314` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | all 11 NIFs under `meshes\SkyrimFair\`, hashes identical to `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| **Vanilla references disabled** | **0** |
| **Dirt-cliff pieces placed** | **0** (DirtCliffs01, DirtCliffs02, DirtCliffsIsland01 all out) |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps (Barry's choice; brief still says WRStoneFloor02) |

## The staircase is now project-authored

`SkyrimFair_Stair_192` replaces the vanilla `StonewallTerraceStairs01` flight. The vanilla
piece could not be separated from its 666-wide drystone wall, and three of those walls
stacked each side were the gatehouse. Vanilla was audited first: there is no free-standing
outdoor stone stair without a wall in the base game (the Whiterun stairs are platform
pieces built into their own terrain).

- **Same geometry as the vanilla flight at scale 1**: 112 rise over 192 run, 168 wide. So
  the three-flight chain at scale 1.3, every guard, and the bank placement carry over.
- **Eight steps of 14 rise**, a real step, instead of the vanilla piece's 17 ripples.
- **Collision built in**: a `bhkBoxShape` on the nosing line, the same box the hidden slab
  used. Verified in the NIF: one box shape, no compressed mesh, one rigid body. The
  separate slab piece is kept for the vanilla fallback (`entrance.kitStair = false`) and
  is not placed in this build.
- **Two vanilla materials**: Whiterun flagstones (`WRStoneFloor02`) on the treads, farmhouse
  drystone (`StoneWall01`) on risers and flanks, so it ties into the cheek walls. Both
  referenced by game path; replacers win at runtime.
- A solid 192 deep below the treads, so nothing shows under the steps.

Flights at `(-5888, -11648 / -11398 / -11149)`, `Z -5336 / -5482 / -5627`, scale 1.3,
rotation 0. The top tread is on the floor plane.

## Cheek walls: the only masonry at the entrance

`Stonewall01` at scale 0.6 (105 tall, 154 long), two pieces per flight per side, laid
along the steps with each piece's crest 56 (west) or 80 (east) above the nosing line
where it stands, so the two sides never match. Inner face flush with the stair flank
(4 units into the solid, so no seam), first piece flush with the stair head so nothing
pokes onto the paving.

| Side | X | Y | crest Z |
| --- | --- | --- | --- |
| W | -6034 | -11571, -11475, -11322, -11226, -11072, -10976 | -5325 ... -5672 |
| E | -5742 | same | -5301 ... -5648 |

The top piece's crest is 11 to 35 above the floor plane: a low parapet at the stair head,
as in the concept.

## Nothing open-backed anywhere

- `DirtCliffs02FieldGrass01` and `DirtCliffsIsland01FieldGrass01` are out of every pool.
- The cliff-skin pass is switched off (`cliffMinRunSegments` 99): zero placed.
- The entrance bank draws only closed boulders (`RockL02/04/05`, `RockPileL01`) on the
  west and closed grassy piles (`RockPileL02FieldGrass01Moss`, `RockPileM02FieldGrass01Moss`,
  `RockPileL01TundraRocks`) on the east. 10 placed, all bedded to the lowest ground
  under them, crowns below the cheek crest and below the floor plane.

## Perimeter: the layered language on all four edges

`PerimeterWall.PrototypeEdges` is now empty, which means every edge. Courses are
`Stonewall01` at `PieceScale` 0.75 (131 tall, 192 long, three per 512 segment), stepped
outward as they descend, in short stretches with gaps, each stretch ending in a
part-buried rock. Per-edge bias N 0.5, W 0.6, S 0.45, E 0.35. No masonry within 900 of
the stair centreline on the north edge.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Kit stair flights | 3 |
| Low drystone cheeks | 12 |
| Entrance bank pieces | 10 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces (all edges) | 57 |
| Part-buried rocks ending a run | 12 |
| Embankment rocks | 84 |
| Corner stones | 8 |
| Toe rocks | 68 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 143 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **394** |
| Rejected as oversized | 18 |
| Rejected for blocking the entrance | 75 |
| Rejected for protruding through the market floor | 45 |

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Kit build: 11 pieces; `SkyrimFair_Stair_192` reports 16 upward tread triangles and a
  Box collider at 30.3 degrees; every convex piece wound outward.
- Converted NIF inspected: two shapes (treads / sides), both vanilla texture paths, one
  `bhkBoxShape`, no compressed mesh.
- Generator run twice: identical SHA256.
- `footprint_audit.py` against the full load order: 0 vanilla references proud of the
  floor; 40 intersect the footprint, all buried by 200 or more.
- Independent read of the written ESP: every vanilla-based placement checked against the
  eroded market floor and the 208-wide walkable stair width. **One hit: the deliberate
  `SMarketStall01` test stall.** The cheek walls stop exactly at the walkable width.
- **All four cell overrides byte-identical to vanilla.** WRLD deviation unchanged (RNAM
  dropped, FULL literal).
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

## Known and deliberately not done

- **Timber fence** along the top edge and the road (`WRFenceStr01` on `WRFenceBaseStr01`,
  both audited: 106 and 75 tall). Prominent in the concept; the intended next pass.
- **Braziers on plinths at the stair foot**: fair dressing, not started.
- **Stair width** kept at 217 per the earlier brief. The concept's steps are broader;
  `entrance.stairScale` is the one number that changes it (the kit piece, its collider
  and the cheeks all scale together).
- Cobbled approach from the road, bunting, pavilion, signpost text: unchanged, not started.
- NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test in game

1. **Walk up the steps from the road.** They are now real steps with a hidden ramp
   collider, so it should feel like any vanilla stair.
2. From the road: does the entrance now read as steps cut into a bank with low walls
   either side, rather than a gate? The only masonry beside you should be waist high.
3. Walk all four edges. Every edge now has the short-run low-wall language. Is the wall
   height right (about waist high), or should it go lower still?
4. Look for anything hollow or see-through. There should be nothing: no cliff strips
   exist in this build.
5. Is the market floor still clean, and does the stair head meet the floor cleanly?
