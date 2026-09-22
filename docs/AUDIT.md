# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: closed stair cheeks and restored entrance retaining face

Barry's in-game screenshots of 2026-09-22 showed two related construction faults:

- the vanilla `Stonewall01` cheek references were raised to keep their crests near the
  stair nosing, exposing the terrain-dependent meshes as repeated vertical towers with
  visible undersides;
- reserving the 512-unit entrance segment skipped its structural retaining face, leaving
  the terrace open around the narrower stair head.

Both are now project-authored closed geometry. The approved site, footprint, elevation,
stair dimensions and content scope did not change.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 85,363 bytes |
| SHA256 | `ea456f0b74de7c21633f705323761431dbcaa29b575bf37c48b50088d8d46b6b` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | all 13 NIFs under `meshes\SkyrimFair\`, hashes identical to `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps |

## Continuous descending stair cheeks

`SkyrimFair_StairCheek_192` replaces all twelve raised `Stonewall01` cheek references.
There is now one closed cheek per side per flight: six total.

- At scale 1 the piece is 48 wide, 192 long and 240 overall depth. Its visible crest
  has the same eight 24-run / 14-rise steps as `SkyrimFair_Stair_192`.
- It is placed at the stair's scale 1.3, so each section runs 249.6 and drops 145.6,
  exactly matching its flight. Consecutive sections meet on the next stair riser rather
  than forming upright towers.
- West crests remain 56 above the nosing; east crests remain 80 above it. The inner
  face overlaps the stair flank by 4 units, hiding the seam without narrowing the
  217.1-unit walking surface.
- The solid extends 128 below the descending crest at scale 1 and is closed on its
  back, ends and underside. It no longer relies on terrain to hide empty space.
- Each NIF contains farmhouse `StoneWall01` diffuse/normal paths and a child
  `bhkBoxShape` aligned to the 30.3-degree flight.

World placements:

| Side | X | Y flight heads | crest Z at each head |
| --- | --- | --- | --- |
| W | -6024 | -11648, -11398, -11149 | -5280, -5426, -5571 |
| E | -5752 | -11648, -11398, -11149 | -5256, -5402, -5547 |

## Closed retaining face around the stair head

Two `SkyrimFair_EntranceRetainWing_144` references restore the structural face omitted
for the entrance segment. Each wing is a closed 144 x 128 x 256 solid with earth/cliff
material and its own `bhkBoxShape`.

- positions: `(-6072, -11648, -5336)` and `(-5704, -11648, -5336)`;
- outer edges meet the 512-unit segment at X `-6144` / `-5632`;
- inner edges stop at X `-6000` / `-5776`, leaving a 224-unit opening;
- the scaled stair is 217.1 wide, leaving about 3.45 units clearance on each side;
- because the wings are separate meshes, no visual face or collision bridges the steps.

The new NIF hashes are:

| Mesh | SHA256 |
| --- | --- |
| `SkyrimFair_StairCheek_192.nif` | `c43742bf54355c9ff057ae75ce83145762cc6b3e7c5fd9b604b8a2d01b68aca8` |
| `SkyrimFair_EntranceRetainWing_144.nif` | `e795b75631f58828e7758111db7c4397451a1594e60ed7c448e3702ff23776c6` |

## Foundation and entrance retained unchanged

- 33-cell irregular footprint, emitted as 12 structural paving bodies and 12 caps.
- Three `SkyrimFair_Stair_192` flights at scale 1.3: 112 rise over 192 run at scale 1,
  eight steps, built-in smooth box collision.
- Stair heads remain `(-5888, -11648 / -11398 / -11149)`, at
  `Z -5336 / -5482 / -5627` (rounded display values).
- Entrance banks use closed boulders on the west and closed grassy piles on the east.
- No dirt-cliff mesh is placed anywhere; `cliffMinRunSegments` remains 99.
- All-four-edge layered perimeter remains enabled with `Stonewall01` at scale 0.75.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Kit stair flights | 3 |
| Project-authored descending cheek sections | 6 |
| Entrance retaining wings | 2 |
| Entrance bank pieces | 11 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces (all edges) | 57 |
| Part-buried rocks ending a run | 12 |
| Embankment rocks | 84 |
| Corner stones | 8 |
| Toe rocks | 68 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 142 |
| Cliff pieces | 0 |
| Vanilla references placed | 382 |
| Project-kit references placed | 114 |
| Test stall and map marker | 2 |
| STAT records created | 10 |
| Rejected as oversized | 18 |
| Rejected for blocking the entrance | 75 |
| Rejected for protruding through the market floor | 45 |

Relative to the previous plugin, twelve vanilla cheek-wall references were removed, six
project cheek references and two retaining-wing references were added, and two STAT
records were added. One deterministic bank placement was gained and one shrub placement
was lost after the cheek depth changed; total vanilla references therefore fell from
394 to 382. Tamriel WRLD and the same four exterior CELL records remain overridden.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -- fair.config.json  # twice
python tools\footprint_audit.py --data <stock Data> \
    --profile <Still in Skyrim Plus profile> --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Blender build: 13 pieces; all convex faces wound outward. Both stair and cheek report
  16 upward tread triangles. Cheek collision reports 30.3 degrees.
- AssetWatcher converted all FBXs; the two new NIFs were checked directly for their
  expected texture paths and `bhkBoxShape` records.
- Generator run twice: identical 85,363-byte output and identical SHA256.
- Full-load-order audit: 40 vanilla references intersect the footprint/margin, with zero
  references standing proud of floor `-5336`.
- Opening arithmetic independently checked: 224 structural opening, 217.1 stair width,
  no wing overlap; cheek/stair seam overlap is 4 units.
- Generated ESP copied to the MO2 mod and compared byte-for-byte by SHA256.
- All 13 source/deployed NIF hashes compared; zero mismatches.
- `modlist.txt`, `plugins.txt`, `loadorder.txt`, saves, grass cache and generated LOD were
  not touched.

## Known and deliberately not done

- This pass is structurally verified but still needs Barry's in-game visual and collision
  test; a NIF/parser check cannot prove the final read under Skyrim lighting and terrain.
- Timber fence, braziers and the cobbled road spur remain unstarted pending entrance
  approval.
- NGIO grass cache has not been regenerated. No navmesh exists.
- The Tamriel WRLD override still writes literal English `Skyrim` and drops RNAM.
- The deliberate `SMarketStall01` test stall remains on the paving.
- Untracked `music/` WAV files remain untouched and outside git.

## What Barry should test in game

1. Approach from both sides: the walls should now form two low descending lines that
   follow the staircase, with no tall repeated towers.
2. Look beneath and behind every cheek section: no black underside or missing back should
   be visible.
3. Check the wall directly around the top stair opening: the former open rear face should
   be closed on both sides without narrowing or blocking the steps.
4. Walk up, down and brush both edges. Stair and cheek collision should remain smooth,
   and the 217-unit route should stay clear.
5. Check the joins between the three cheek sections and the floor at the stair head.
