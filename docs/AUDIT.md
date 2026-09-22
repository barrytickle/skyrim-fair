# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: isolated festival worldspace prototype (2026-09-22, night)

Barry changed direction: the full Wanderer's Fair will eventually live in its own
isolated outdoor worldspace, a large irregular palisade compound reached through a gate
in Tamriel. This pass builds **only the empty canvas**: a new exterior worldspace with
sky, weather, exterior lighting and generated flat ground, with the broad plan painted
into the ground. It is a **parallel prototype**. The raised Tamriel terrace, the
approved staircase and embankment, and the sandbox cell are untouched: all 541 records
of the previous plugin are present and field-for-field identical in the new one (checked
by reading both files, below).

Not built, by instruction: the Tamriel gate, the palisade itself, stalls, NPCs, archery,
stage systems, quests, navmesh, music, clutter.

### The worldspace

| Field | Value |
| --- | --- |
| EditorID | `SkyrimFairWorld` |
| FormID | `000A16:SkyrimFair.esp` (in game `xx000A16`, `xx` = Skyrim Fair's load-order index) |
| Name (FULL) | `The Wanderer's Fair` (loading screen / HUD) |
| Entry | **`cow SkyrimFairWorld 0 0`** |
| Exit | `cow Tamriel -2 -4` (the Tamriel fair site), or fast travel |
| Zone jumps | `player.moveto SkyrimFairWorldEntranceMarker` (also `...MarketMarker`, `...ActivityMarker`, `...CrowdMarker`, `...StageMarker`) |
| Parent | Tamriel (`00003C`), **Use Map Data only**, as vanilla sub-worlds do. Nothing else is inherited: not land, LOD, water, climate or image space |
| Flags | `NoLodWater`. Fast travel is allowed so the player can leave from the map |
| Climate | `SkyrimFairWorldClimate` (`000A15`), a copy of `SkyrimClimate` (`000812`: sun, glare, moons, sky model, sunrise 05:30-10:00, sunset 16:00-20:30) with its weather list replaced by `WeatherTundraNoPrecip`'s (`1046C9`): `SkyrimCloudyTU` 45, `SkyrimClearTU` 35, `SkyrimClearTU_A` 10, `SkyrimCloudyTU_A` 10. That is what the Whiterun plains get. Tamriel's own climate lists only `SkyrimCloudy` because Tamriel takes its weather from regions, which do not apply here |
| Lighting | Exterior, driven by the weathers (no LGTM on the world or its cells, same as Tamriel) |
| Water | None. No cell has Has Water; default water height -50,000 as a backstop; no WNAM |
| Land defaults | land 0, water -50,000 |
| Object bounds | cells -5,-5 to 6,6 |
| Music, location, encounter zone | none |
| Persistent cell | `000A17`, grid 0,0, Persistent flag set; holds the five zone markers |

### Cells and terrain

- **121 exterior cells, -5..5 on both axes** (45,056 units a side), each with a
  generated LAND record. Every cell is a new record; none is an override.
- **Terrain strategy: generated from rules, not sculpted.** Ground is exactly flat at
  **Z 0** inside the planned perimeter and for 1,024 units beyond it, so the palisade
  will stand on level ground wherever it is finally drawn. Past that it rises over
  8,192 units into low hills of about 1,536 (max 1,688 with a small deterministic
  undulation), closing the view where there is no wall yet. Undulation comes from an
  integer-hashed value noise, never a string hash, so it regenerates byte-identically.
- LAND carries heights (VHGT) and normals (VNML) computed across cell seams from the
  same function; DATA flags `0x1D`, exactly Sovngarde's; no vertex colours (Sovngarde
  has none either); zlib-compressed like vanilla. Checked by decoding the written
  file: **0 seam mismatches** across all 220 cell edges, **0 non-flat vertices** in the
  2,793 sampled across the compound's core.
- **The plan is painted into the ground** with vanilla landscape textures (referenced,
  never copied), at most 4 alpha layers in any quadrant (vanilla uses up to 5):

| Area | LTEX | Look in game |
| --- | --- | --- |
| Compound ground | `LFieldGrass01` `013428` | Whiterun field grass |
| Beyond the perimeter | `LTundra01` `024E30` | rougher tundra |
| Perimeter line, 256 wide | `LTundraRocks01NoRocks` `06DE8B` | stony strip, broken at the gate |
| Avenue, entrance forecourt, crowd square | `LDirtPath01` `0B424C` | bare path, no grass |
| Market side | `LFieldDirtGrass01` `0134B7` | worn earth |
| Activity side | `LFieldGrass01NoGrass` `024E46` | same field texture, no grass: an open range |
| Stage footprint | `LDirt02` `000C16` | darker bare earth |

### The broad plan

The compound is centred on the middle of cell 0,0 (world 2048, 2048), so `cow
SkyrimFairWorld 0 0` puts the player in the middle of it. The entrance is at the south
and the stage at the north, as in DESIGN.md's layout: **entrance, then a busy central
avenue, then the stage as the anchor**, with the market branching west and activities
east.

| Element | Where | Size |
| --- | --- | --- |
| Planned perimeter | irregular 17-point polygon, X -2,052..6,498, Y -2,802..7,198 | **8,550 x 10,000** (about 122 m x 143 m), roughly 3x the Tamriel terrace's span each way |
| Main gate | south wall at (2048, -2777), 640 wide | |
| Entrance forecourt | just inside the gate, widening to about 2,000 | |
| Central avenue | (2048,-2752) to (1898,48) to (2198,2748) to (2048,4748), a slight handmade bend | 800 wide, 7,500 long |
| Crowd square | north end, (2048, 5348) | about 5,300 x 2,000 |
| Stage | backs onto the north wall, faces south, (2048, 6548) | 2,150 x 600 footprint |
| Market | west of the avenue, (148, 1748) | about 2,500 x 5,500 |
| Activity / archery | east of the avenue, marker (3548, 1248) facing east, so an archery line shoots toward the east wall | about 3,000 x 5,600 |

Generator plan view, 512 units per character, north up (`#` perimeter strip, `E`
entrance, `=` avenue, `M` market, `A` activity, `C` crowd, `S` stage):

```text
           #######
        ###SSSSS..##
       ##.CCCCCCCC.##
      ##.CCCCCCCCC..##
      #..CCCCCCCCC...#
     #...CCCCCCCCC...#
      #..MMM.=.AAAAAA.#
      #.MMMM.=.AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     ##MMMMM==.AAAAAA#
      #.MMMM==.AAAAAA#
      #...MM.=.AAAAA.#
       #.....=..AAA.##
        ##..EEE...##
         ####EE###
              #
```

**Why the compound fits in cells -1..1.** The world has no LOD, so only the loaded grid
of cells renders (5 x 5 at the default `uGridsToLoad`). With the whole compound inside
a 3 x 3 block of cells, every cell of it is loaded from anywhere inside it, so the
future palisade will always draw and nothing beyond it needs to. Growing the compound
past that block means the far wall can drop out of view; it would then need the walls
flagged Full LOD, or generated LOD.

### Temporary scale markers

- **33 `FarmBannerPost01`** (`1083D7`, 422 tall, about the height a palisade might be)
  on the perimeter line: one at each vertex and at even spacing up to 1,024 apart,
  turned along the wall. None stands across the gate; one post stands at each side of
  the opening. They are there to show the scale. The final wall replaces them.
- **Five persistent `XMarkerHeading`** (`000034`, invisible in game) with EditorIDs,
  for `player.moveto` and for the Creation Kit:

| EditorID | FormID | Position | Faces |
| --- | --- | --- | --- |
| `SkyrimFairWorldEntranceMarker` | `000A1A` | 2048, -2502 | north, up the avenue |
| `SkyrimFairWorldMarketMarker` | `000A18` | 148, 1748 | east, toward the avenue |
| `SkyrimFairWorldActivityMarker` | `000A19` | 3548, 1248 | east, down the range |
| `SkyrimFairWorldCrowdMarker` | `000A1B` | 2048, 5348 | north, at the stage |
| `SkyrimFairWorldStageMarker` | `000A1C` | 2048, 6548 | south, at the crowd |

### Records added (nothing removed or newly overridden)

| Type | Count |
| --- | --- |
| WRLD | 1 (`SkyrimFairWorld`) |
| CLMT | 1 (`SkyrimFairWorldClimate`) |
| CELL | 122 (121 exterior + the persistent cell) |
| LAND | 121 |
| REFR | 38 (33 posts + 5 markers) |

Built last in the generator, so it only appends FormIDs: every Tamriel and sandbox
record keeps its previous ID. The exterior block / sub-block grouping was moved into a
shared `ExteriorCellGrid` used by both worldspaces; the Tamriel output is unchanged by
it (identical records, below).

### Verification of this pass

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 `8b7eb313...`.
- Baseline comparison: the previous audited build (`2eaeeeba...`, regenerated from the
  pre-pass tree first and matching this file's old hash) and the new build read side by
  side with Mutagen. **541 of 541 records identical** (WRLD and CELL compared on their
  own fields, every other record in full), 0 missing, 0 changed. Tamriel still has 6
  cells.
- New world decoded from the written ESP: WRLD fields as tabled; climate carries the
  four tundra weathers; 121 cells, 0 with water; 33 posts; five markers with the
  Persistent flag; heights 0..1,688; 0 seam mismatches; compound core flat; LAND
  `0,0` flags `0x1D`, compressed, flat normal `(0, 0, 127)`, two alpha layers per
  quadrant.
- **Not verified: loading in game.** Nothing here has been run in Skyrim. `cow
  SkyrimFairWorld 0 0` needs Barry's test.

### Technical limitations found

- **No LOD** of any kind (terrain, object, tree). Beyond the loaded cells the world
  is empty sky. Handled by the sizing above. xLODGen / DynDOLOD could generate LOD
  for this world later if a wider view is ever wanted.
- **No OFST offset table on the WRLD and no MHDT max-height data on its cells.** The
  Creation Kit normally writes both. Mutagen does not generate them. Vanilla has
  precedent for cells with no MHDT (`CWSiegeTestWorld`). Whether a missing OFST
  matters is **not yet confirmed in game**. It is the first thing to suspect if `cow`
  fails.
- **Grass**: the ground textures carry grass, but the world has no NGIO grass cache.
  If NGIO is set to load grass only from the cache, the fair ground will have no grass
  until the cache is regenerated. The layout reads either way.
- **Map**: the pause-menu map is Tamriel's (Use Map Data). The player's marker there
  means nothing. Fast travel out should work as it does from a city world. Not yet
  tested.
- **No navmesh**, so NPCs cannot path here yet. Nothing needs to until content goes in.
- Weather is climate-driven (no regions), so `fw <weather id>` is how to force one when
  testing.

### What Barry should test in game (this pass)

1. `cow SkyrimFairWorld 0 0`: does it load, with sky, sun and weather, and are you
   standing on grass in the middle of the avenue? If it hangs or drops you into a void,
   say so before anything else. That points at the missing OFST / MHDT data.
2. Walk south to the gate (or `player.moveto SkyrimFairWorldEntranceMarker`) and look
   north: the dirt avenue should run up to the big crowd square and the darker stage
   patch, with the worn-earth market on your left and the grass-free range on your right.
3. The posts show the planned wall line. Does the compound feel the right size? It is
   about 122 m x 143 m. Too big and it will feel empty; too small and it won't hold
   everything in DESIGN.md.
4. Look out over the posts: the ground should rise gently into hills and never stop in
   a visible edge.
5. Open the map and fast travel out, to check leaving works.
6. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## Tamriel terrace (unchanged this pass)

### Previous pass: the generator reproduces Barry's completed Creation Kit layout

**Late amendment (2026-09-22 evening):** after seeing the result in game Barry asked for
the big scaled walls to go entirely. At two to three times size the flat cut end of a
wall piece reads as a huge smooth slab, whichever way it is turned. The corner bastions
and the single walls on the short step faces are switched off (`bastions`,
`shortFaceWalls`), and the terrace rows now run through to every corner and along the
short faces too (`cornerStop` 0). No scaled-up Stonewall piece remains in the plugin;
the parapets (0.98), the west field wall (0.9), the cheeks (0.6) and Barry's two 1.06
cheek underpinnings are the only Stonewall01 left. Everything else below still holds.

**Sandbox cell added (2026-09-22, late):** the plugin now carries one interior CELL,
`SkyrimFairSandbox`, so pieces can be looked at in isolation. It is 25 fill bodies and
25 paving caps from the kit in a 5 x 5 square (5,120 units a side, floor at Z 0), a
`COCMarkerHeading` at the centre, and nothing else. Flags are Interior + Show Sky + Use
Sky Lighting (`DATA 0x0181`); every lighting value is inherited from
`DefaultLightingTemplate` (`XCLL` inherit `0x7FF`), the sky comes from
`WeatherTundraNoPrecip` and the image space is `DefaultImageSpaceExterior`. It shares the
foundation's two STAT records, so the STAT count stays at 9. It touches no vanilla record.
The six Tamriel cells and everything in them are unchanged from the row below.

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
| Size | 366,226 bytes (88,124 before the worldspace; the difference is the 121 LAND records) |
| SHA256 | `8b7eb313ec3a798e98576a3328abe1e3e8cbbee988f12ac455946cb3071855c2` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` (2026-09-22, night; replaced `2eaeeeba...`, the previous audited build) |
| Sandbox cell | `SkyrimFairSandbox` (`0009E1:SkyrimFair.esp`), interior, 5 x 5 kit tiles, `coc SkyrimFairSandbox` in, `cow Tamriel -2 -4` out |
| Isolated worldspace | `SkyrimFairWorld` (`000A16:SkyrimFair.esp`), 121 cells, `cow SkyrimFairWorld 0 0` in; see the current pass above |
| Kit meshes | unchanged this pass; all 13 deployed NIFs match `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Tamriel cells | `-3,-4`, `-2,-4`, `-1,-4`, `-3,-3`, `-2,-3`, `-1,-3` (all byte-identical to vanilla) |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| **Footprint** | **26 cells** (was 33): see below |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN anywhere; 0 LAND in Tamriel. The 121 LAND records are new records in `SkyrimFairWorld`, not edits to any vanilla landscape |
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
| Band: outward walls / inward pieces / parapets | 44 / 44 / 49 |
| Band: west lower field walls | 12 |
| Band: big walls on short faces / bastion wall-ends | 0 / 0 (switched off, see amendment) |
| Entrance dressing | 18 |
| Toe rocks / verge wedges / shrubs and scrub | 60 / 25 / 129 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **374** |
| Rejected as oversized / for blocking the entrance / for the market floor | 17 / 10 / 0 |

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json   # twice
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 (re-verified after the amendment).
- `footprint_audit.py` against the full load order: 0 vanilla references proud of the
  floor on the foundation.
- No Stonewall-family reference above scale 1.1 remains in the plugin (checked by
  reading the ESP).
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

- Corners are now simply where two rows meet, with the outward walls at their per-edge
  offsets, so a north/east corner has a 53-unit step between the two wall faces. No
  corner piece of any kind is placed; if a corner needs closing, it wants a new idea
  rather than a scaled wall.
- Shrubs are still planted at grade beyond the walls, not on the grass.
- Timber fence on the top edge, the cobbled spur from the road, bunting, pavilion,
  signpost text: not started. NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test on the terrace (from the previous pass)

1. **The whole perimeter**: each side should read as your own build did - terrace wall,
   grass, knee-high parapet. The big slabs are gone; look at the corners, where the two
   rows now just meet, and say whether they need closing with something else.
2. **The west side** now has the grass plinth under the wall that the east and south
   have. Say if you would rather it stayed as you left it.
3. **The stair foot**: braziers lit on the cheek ends, wing walls and banners where you
   put them.
4. **The retaining bodies you deleted are back** behind the walls. If any earth face
   shows where you had cleared it, tell me where.
5. Market floor clean, stairs climbable, nothing hollow.
