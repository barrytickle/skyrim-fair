# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: the terrace band, round the whole outline

Implements Barry's instruction of 2026-09-22 (afternoon): carry the approved embankment
prototype he built beside the stairs round the whole site, with an even distance kept
round every corner, using rotated terrace walls and rocks at the corners as he did. The
staircase is approved and was not touched.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 93,399 bytes |
| SHA256 | `c5628dd7f7dc80dda5a1da0e7dff9d2bdb2382e6c6fd1415ffc596286495bce6` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes | unchanged this pass; all 13 deployed NIFs still match `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Cells | `-2,-5`, `-3,-4`, `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` (all byte-identical to vanilla) |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps |

## The band, as generated

The three layers are the ones measured from Barry's Creation Kit prototype (reference
section below). Each layer is laid along an **offset of the paved outline**, so the band
is the same width on every edge and turns every corner at the same distance: the paving
outline is traced as a counter-clockwise polygon, each edge's line is pushed out by the
layer's plan offset, and neighbouring offset lines meet at the offset corners. That is
exact for this rectilinear footprint and behaves the same at convex and re-entrant
corners.

| Layer | Piece | Scale | Faces | Plan offset (origin) | Z rule |
| --- | --- | --- | --- | --- | --- |
| Lower wall | `StonewallTerrace01` | 1.0 | outward | +117 (face at +442) | ground at the face, 6 sunk; crest never above floor -48 |
| Grass slope | `StonewallTerrace01` | 1.0 | inward (wall buried) | +111 | lower Z + 115; slope top at the retaining face never above floor -48 |
| Parapet | `Stonewall01` | 0.98 | outward | -15 (straddles the edge) | floor -153, crest floor +18.5 |

Along each run the pieces are spaced **evenly**: the first and last sit exactly at the
run's ends and the rest are spread between at or just under piece length, so ends are
flush at every corner and nothing gaps. Runs continue 64 past a convex corner so their
ends are swallowed by the knoll; at re-entrant corners the two runs meet at the offset
vertex and overlap inside each other.

**On the entrance edge each run is split** either side of the stairs and each half is
spaced on its own, so the band finishes exactly at the clearance on both sides: the lower
wall runs under the stair solid to the flank, the slope and parapet stop 16 outside the
cheeks' outer face. Both sides measure the same.

### Corners: grass knolls with boulders

Every convex corner gets `StonewallTerraceCorner01` on native ground, **turned so its two
walls face the terrace and its rounded grass shoulder faces out**, walls' corner 40
outside the paving corner on both axes. That is the orientation Barry used at the
north-east corner (rotation -90 there; the generator derives the rotation from the two
edge normals, so every corner gets the matching turn). The walls are buried under the
grass slopes; what shows is a rounded grass shoulder wrapping the corner 154 beyond the
lower wall faces, into which both lower walls die. Two closed boulders sit on each
shoulder (`RockL04`, `RockL05`, `RockL02` at 0.7 to 1.1, bedded to grade and lifted only
enough to clear the grass). Knolls within 512 of the stair centreline are not placed.

### What was retired

- The 0.75-scale `Stonewall01` course runs (`perimeterWall.enabled = false`).
- Embankment wall rocks (`wallPerSegment = 0`) and the old corner stones; the band and
  its knolls replace both.
- The entrance bank rocks either side of the stairs (`rockSide = 0`, `earthSide = 0`);
  the band, the cheeks and the two nearest knolls frame the entrance now, as in the
  prototype.
- Toe rocks and scrub moved out to 480 to 960 from the edge, in front of the lower wall
  instead of inside the band.

### Shallow ground

The site is shallowest along the east face, where native ground is 234 to 266 below the
floor. There the lower wall is sunk so its crest stays 48 under the floor and the slope
piece is lowered so its top meets the retaining face 48 under the floor; the band
compresses to wall, near-flat grass, parapet. No piece is refused anywhere.

## The entrance, unchanged and approved

Three `SkyrimFair_Stair_192` flights at scale 1.3 at `(-5888, -11648 / -11398 / -11149)`,
`Z -5336 / -5482 / -5627`, eight steps each, box collision on the nosing line lifted one
riser. Beside them the eighteen vanilla Stonewall-family cheek references at 0.6: seven
`Stonewall01` blocks per side pitched +30.3 degrees with the stairs, a level block at
each foot and a `StonewallEndL01` cap at each terrace landing, centrelines at
`X -6022` and `-5754`. Two `SkyrimFair_EntranceRetainWing_144` close the structural face
either side of the stair head. None of this moved.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Kit stair flights | 3 |
| Cheek blocks, level ends, tapered caps, retaining wings | 18, 4, 2, 2 |
| Structural retaining courses / corners | 53 / 3 |
| Band: lower walls | 72 |
| Band: grass slopes | 70 |
| Band: parapet pieces | 67 |
| Band: corner knolls / knoll boulders | 10 / 20 |
| Toe rocks | 74 |
| Rough-earth verge wedges | 29 |
| Shrubs and scrub | 150 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **481** |
| Rejected as oversized | 15 |
| Rejected for blocking the entrance | 9 |
| Rejected for protruding through the market floor | 0 |

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
  floor on the foundation.
- Independent read of the written ESP: every vanilla-based placement checked against the
  eroded market floor and the 208-wide walkable stair width. **One hit: the deliberate
  `SMarketStall01` test stall.** The approved cheeks and the parapet, which straddle the
  edge by design, are excluded from that test and noted as such.
- Band geometry read back from the plugin: lower origins at +117, slopes at +111,
  parapets at -15 from their edges; slope Z = lower Z + 115 everywhere; runs end flush at
  offset vertices (checked at the re-entrant corner `(-5120, -14720)`); stair-side runs
  end symmetrically (lower to the flank, slope and parapet 189 and 171 from the
  centreline on both sides).
- Plan-view renders of the whole site and of the north-east and east quadrants were
  drawn from the plugin and inspected: band continuous on every edge, knolls at all ten
  convex corners, no piece on the stairs.
- **All six cell overrides byte-identical to vanilla.** WRLD deviation unchanged (RNAM
  dropped, FULL literal).
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

`SKYRIMFAIR_TRACE=1` prints cheek, bank and band decisions (including any band piece
refused for shallow ground) to stderr.

## Approved embankment reference, from Barry's Creation Kit layout plugin

`reference/SkyrimFair_CK_LAYOUT_REFERENCE.esp` (40,056 bytes, SHA256
`6e74233a982809d617a1...`, saved 2026-09-22 15:21) is a hand-edited copy of the
generated plugin. **It is a visual and layout reference only.** It is not authoritative
and does not replace the generator-owned `SkyrimFair.esp`. It was read independently and
diffed against the generated plugin of the time by reference FormID.

### What the diff said

- 21 added, 32 removed, 4 genuinely moved; ~290 other "changes" were the Creation Kit
  rounding every scale to two decimals on save.
- **The staircase was unchanged** and is approved.
- **Cheek caps moved in the CK**: both upper `StonewallEndL01` caps at
  `Y -11611.1, Z -5379.1`, rotation `95.73` degrees, against the generated
  `Y -11698.6, Z -5406.6, 90` degrees. Recorded, not adopted.
- **Removed in the CK**: the east entrance retaining wing, six structural `Retain512`
  bodies and one `RetainCorner128` at the north-east corner, three perimeter courses and
  a dozen embankment rocks, cleared for the hand-laid bank.
- **Added**: one `SkyrimFairPaveCap512` at `(-4865, -11908)` filling the row-0 / col-5
  notch; two `StonewallTerraceCorner01`; one `StonewallTerrace01` at `Z 0.0` (`000FCF`),
  a CK accident, ignored.

### The pattern, east of the stairs

World Y increases outward on this edge; paving edge `Y -11648`, floor `Z -5336`, native
ground about `Z -5777`. `StonewallTerrace01`: a 69-thick wall at local `Y -325..-256`
with its face on local -Y and crest at 175, then a grass slope falling from 168 at the
wall back to 96 at local `Y +256`. `StonewallTerraceCorner01`: walls on local -Y and +X
meeting in a rounded corner at about local `(300, -300)`, grass falling toward local
`(-X, +Y)`.

| Layer | Base object | Scale | Rotation Z | Origin Z | Origin Y (offset from paving edge) | X positions |
| --- | --- | --- | --- | --- | --- | --- |
| Upper wall | `Stonewall01` `0000099B` | 0.98 | 180 (face outward) | -5489.0 (floor -153) | -11662.9 (-15) | -5622.2, -5521.1, -5272.6 |
| Sloped grass | `StonewallTerrace01` `000009C6` | 1.0 | 0 (face inward, buried) | -5662.0 (ground +115) | -11536.8 (+111) | -5616.3, -5508.1, -5253.8, -5000.1 |
| Lower wall | `StonewallTerrace01` `000009C6` | 1.0 | 180 (face outward) | -5777.0 (on ground) | -11531.1 (+117) | -5784.1, -5541.2, -5288.4 |
| Corner knoll | `StonewallTerraceCorner01` `00000A74` | 1.0 | -90 (walls inward) | -5776 (on ground) | origin `(-4673, -11534)` | with `RockL05` 0.7 and `RockL04` 1.13 on the shoulder |

Vertical relationship, foot to crest: ground `-5777` -> lower wall crest `-5602` (175)
-> 36 lip -> grass slope foot `-5566` rising to `-5514` at the retaining face -> upper
parapet base `-5489` and crest `-5317.5`.

Fit notes carried into the generator: the parapet is **not** sunk (sinking it 32 would
put its crest under the floor and lose it); the 23 to 31 of retaining face showing under
the parapet's outer edge is the prototype's own look and is kept. The lower row is
allowed to run under the stair solid to the flank, as Barry's did.

## Known and deliberately not done

- **Timber fence** along the top edge and the road (`WRFenceStr01` on `WRFenceBaseStr01`).
- **Braziers on plinths** at the stair foot; the cobbled spur from the road; bunting,
  pavilion, signpost text.
- Shrubs are still planted at grade beyond the lower wall, not on the grass slope.
- The overrides now touch six cells instead of four because toe rocks and scrub sit
  further out; all six copies are byte-identical to vanilla.
- NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test in game

1. **Walk the whole perimeter at ground level.** Each edge should read as: lower
   drystone wall, grass bank, knee-high parapet, the same width everywhere, and every
   corner should turn as a rounded grass shoulder with two boulders on it.
2. **Compare the generated north-east corner with your own.** Yours had the knoll further
   out, with the walls' corner about 150 east and 190 south of the paving corner; the
   generator puts the walls' corner 40 outside the paving corner on both axes so all ten
   corners match. Say if you want it pushed out.
3. **The east face** is the shallow side: the band compresses there. Does it still read
   as terracing, or does it want the lower wall dropped where ground is high?
4. **Look at the stairs from the road**: the band should meet the cheeks evenly on both
   sides with no bank rocks between.
5. Market floor still clean; stairs still climbable.
