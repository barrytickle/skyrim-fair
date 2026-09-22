# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: the generator reproduces Barry's completed Creation Kit layout

Barry finished the perimeter by hand in the Creation Kit and saved it as
`reference/SkyrimFair.esp` (35,173 bytes, 2026-09-22 17:53). Per his instruction that
plugin is the **design source of truth**, and a **visual/layout reference only**: it is
not authoritative as a plugin and never replaces the generator's output. This pass reads
it, extracts every intentional change, and reproduces the layout from rules so the
generator's plugin lands on the same design.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 83,585 bytes |
| SHA256 | `eed678bc989d072adb1a4f01289101f0f570b21874505c74a685568c65446499` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes | unchanged this pass; all 13 deployed NIFs match `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Cells | `-3,-4`, `-2,-4`, `-1,-4`, `-3,-3`, `-2,-3`, `-1,-3` (all byte-identical to vanilla) |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| **Footprint** | **26 cells** (was 33): see below |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps |

## What the reference contains, and what was taken from it

The reference was built from an earlier generated plugin, so a FormID diff against the
current one is meaningless; it was read as a complete layout instead, grouped by base
object, and every hand-placed group was classified.

### Footprint: Barry removed the east column and the south tongue

The reference paves 26 cells. Reconstructed from its floor caps, row by row from the
north: `..####.`, `.#####.`, `.#####.`, `.#####.`, `.#####.`, `.##....`. Against the
old 33-cell mask, column 6 (the far east strip) and the south tongue (row 5 columns 3
to 5, all of row 6) are gone, and the row-0 / column-5 notch is filled. His terrace walls
wrap this new outline (east rows at `X -4544` from row 0 to row 4, south rows at
`Y -14290` for columns 3 to 5 and `Y -14836` for columns 1 to 2), so it is deliberate.
**Adopted**: `footprint` in `fair.config.json` is the 26-cell mask. Because the north
row is now four tiles wide, the entrance column is pinned with `rampAlign: "offset:1"`
so the approved staircase stays at `X -5888` ("centre" would have moved it one tile
east). Three of his floor caps had no body under them (he deleted the 1024 fill they
replaced); the generator emits bodies and caps for all 26 cells.

### Perimeter: per-edge layer stacks, not one prototype

The north band is the prototype measured earlier. The other sides differ, and the
generator now carries **one layer stack per compass edge** (`terraceBand.edges`):

| Edge | Outward `StonewallTerrace01` | Inward `StonewallTerrace01` | `Stonewall01` parapet (0.98) | Extra |
| --- | --- | --- | --- | --- |
| N | origin +117, **on grade** | +111, **115 above** the outward piece: the grass slope | -15, floor -153 | |
| E | +64, fixed **floor -296** | +64, fixed floor -424: a grass plinth under the wall | +24, floor -149 | |
| S | +98, fixed floor -296 | +136, fixed floor -424 | +12, floor -149 | |
| W | +136, fixed floor -296 | +136, fixed floor -424 | +20, floor -149 | lower `Stonewall01` at 0.9 on grade, +397 |

Offsets are plan distances from the paving edge, positive outward, taken from his
pieces. On the shallower east, south and west he set the outward wall at one level
(`Z -5632`, crest at floor -121) and, on east and south, an inward piece 128 below it so
nothing floats. On the west he used a third, lower field wall on grade instead. His west
outward walls have no plinth and float 60 to 200 above ground in places; the generator
gives the west the same plinth as east and south. That is the one place it adds to his
layout rather than copying it, and it is flagged here.

Rows are laid along offsets of the outline with even spacing and flush ends as before,
**stop 96 short of every convex corner** (his rows end 78 to 167 short and the corner
is finished by the bastion), and split either side of the stairs. The outward and
inward rows run under the stair solid to the flank, as his do; the parapet runs to 20
past the cheeks' outer face.

### Short step faces: one big field wall

The three one-tile faces that end at a re-entrant corner (row 0's west face, row 1's
north face at column 1, row 5's east face) carry no rows in the reference. He walled
them with one or two `Stonewall01` or `StonewallEndL01` scaled 2.2 to 2.7 so the crest
reaches the floor. **Adopted** as a rule: a face of one tile touching a re-entrant corner
gets one `Stonewall01` on grade, 24 out, scaled to `(drop + 8) / 175` (2.2 to 2.5 here).

### Corners: bastions of scaled wall-ends, no knolls

The reference has **no** `StonewallTerraceCorner01` and no corner boulders. Every
convex corner is closed by `StonewallEndL01` scaled 2.2 to 3.2 so its crest is at floor
level, forming an L that projects outward along both face lines (north-east,
south-east, south-west), or a single wall where the other leg would have stood in front
of a neighbouring face's band (north-west of the stairs: the north-running wall only;
north-west of row 1: the west-running wall only; south-east of row 5: the south-running
wall only). **Adopted** as a rule: at each convex corner, two `StonewallEndL01` on grade,
16 inside their face line, starting 32 inside the corner and running outward `222 x
scale`, scale `(drop + 8) / 171` clamped 1.5 to 3.3, finished end outward; a leg whose
middle would lie within 560 of another edge's outward strip is not placed. That
reproduces his nine corner walls exactly: 9 generated, 9 in the reference.

### Entrance dressing, reproduced piece for piece

Placed relative to the stair head (`entranceDressing`), so the relationships hold if the
entrance ever moves. All positions equal the reference's to 0.1:

- two `WHfirebrazier01` on the level cheek ends at the foot, with
  `FXfireWithEmbersHeavy` fire above each;
- four `Stonewall01` at 0.6 as low wing walls flanking the foot, with a
  `StonewallEndL01` at 0.6 finishing each pair;
- two `FarmBannerPost01` mid-flight, 318 either side of the centreline, with two
  `MarkarthBanner01` on the west post and a `NightingaleBannerAnim02` on the east;
- three `Stonewall01` (1.06, 1.06, 0.6) under the west cheeks so they do not float.

`MarkarthBanner01` and `NightingaleBannerAnim02` are Barry's picks and are reproduced as
placed; they are Reach-city and Nightingale banners, so they read as placeholders for
fair banners that vanilla does not have.

### Ignored as accidental or noise

- Duplicate references at identical transforms (banner posts x4, several walls x2).
- One `StonewallTerrace01` at `Z 0.0` (`000FCF`), 5,300 units in the air.
- One `TreePineShrub01Snow`, a snow shrub in the tundra.
- One `StonewallTerrace02` (the rubble-apron variant) among 68 `Terrace01`.
- Scale rounding to two decimals and sub-unit position noise.
- A vanilla `LvlAnimalPlainsPrey` actor (`0DC5B7`) marked deleted in the CK file. The
  generator never touches vanilla actors; this stays out.
- Three of the generator's old 0.75-scale course walls he kept; they belong to the
  retired language.

### Kept from the generator although absent or fewer in the reference

- **Structural retaining bodies** (43) and both **entrance retaining wings**. He deleted
  48 of 53 retaining bodies and both wings. They are structural: they close the hollow
  under the floor slab and carry the edge collision. In this layout they are hidden
  behind his walls except for a strip of at most 25 units under the parapet, so keeping
  them changes nothing he saw and avoids a see-through gap.
- **The cheek caps** at their generated position; his sit 87 further out and 27 higher.
  The staircase is approved as generated.
- The map marker, the test stall, toe rocks, verge wedges and scrub, which the
  generator regenerates in equivalent positions.

## How close the result is

Nearest-piece match of the generated plugin against the reference, after removing his
duplicates:

| Group | Reference | Generated | Matched | Plan distance median / max |
| --- | --- | --- | --- | --- |
| `StonewallTerrace01` | 68 | 76 | 68 | 57 / 169 |
| `Stonewall01` (parapets, cheeks, big walls, wings) | 90 | 75 | 87 | 30 / 155 |
| `StonewallEndL01` (bastions, caps, foot ends) | 15 | 13 | 12 | 81 / 177 |
| Stairs, braziers, fire, posts, banners | 12 | 12 | 12 | 0 / 0 |

The extra generated terrace pieces are even spacing on runs where he left gaps; the
unmatched walls are his short-face wall-ends where the generator uses one `Stonewall01`
instead, and his three retired courses.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 11 / 11 (26 cells) |
| Kit stair flights | 3 |
| Cheek blocks, level ends, tapered caps, retaining wings | 18, 4, 2, 2 |
| Structural retaining courses / corners | 43 / 1 |
| Band: outward walls / inward pieces / parapets | 38 / 38 / 38 |
| Band: west lower field walls | 11 |
| Band: big walls on short faces / bastion wall-ends | 3 / 9 |
| Entrance dressing | 18 |
| Toe rocks / verge wedges / shrubs and scrub | 60 / 25 / 129 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **362** |
| Rejected as oversized / for blocking the entrance / for the market floor | 17 / 10 / 0 |

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json   # twice
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256.
- `footprint_audit.py` against the full load order: 0 vanilla references proud of the
  floor on the foundation (33 intersect, all buried).
- Independent read of the written ESP against the eroded market floor and the walkable
  stair width: **one hit, the deliberate `SMarketStall01` test stall.** Cheeks, parapet
  and the stair-foot dressing (which straddle those lines by design) are excluded and
  noted.
- Nearest-piece match against the reference as tabled above; stairs, braziers, fire,
  banner posts and banners at the reference positions exactly.
- **All six cell overrides byte-identical to vanilla.** WRLD deviation unchanged (RNAM
  dropped, FULL literal).
- Plan view drawn from the plugin: bands on every long face of the 26-cell outline,
  stairs at `X -5888`.
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

`SKYRIMFAIR_TRACE=1` prints cheek, bank and band decisions to stderr.

## Known and deliberately not done

- No parapet on the three short step faces (he had two on one of them).
- The rows stop a fixed 96 before corners; his stop 78 to 167.
- Shrubs are still planted at grade beyond the walls, not on the grass.
- Timber fence on the top edge, the cobbled spur from the road, bunting, pavilion,
  signpost text: not started. NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test in game

1. **The whole perimeter**: each long side should read as your own build did - terrace
   wall, grass, knee-high parapet - and every corner should be closed by the tall
   wall-ends.
2. **The west side** now has the grass plinth under the wall that the east and south
   have. Say if you would rather it stayed as you left it.
3. **The stair foot**: braziers lit on the cheek ends, wing walls and banners where you
   put them.
4. **The retaining bodies you deleted are back** behind the walls. If any earth face
   shows where you had cleared it, tell me where.
5. Market floor clean, stairs climbable, nothing hollow.
