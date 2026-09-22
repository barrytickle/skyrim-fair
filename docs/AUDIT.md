# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: main stage architecture (2026-09-22, night)

The brief: an open, handmade, temporary Nordic timber pavilion at the north end of the
avenue, facing the gate, built from vanilla pieces. Barry's concept image was used as
loose art direction ("take the concept with a pinch of salt"). **Nothing else moved**:
read back against the mountain build (`b77160ca...`), 835 of 836 existing non-backdrop
records are identical. The one change is intended: `SkyrimFairWorldStageMarker` now
stands on the deck, at Z 134. The 133 stage references are new, and the trees and
mountains are unchanged in content, only renumbered. No NPCs, music, animation,
navmesh, stalls or dressing.

Preview renders from the written plugin (every placed mesh, rotated as the engine
does): `docs/images/stage_preview_views.png` (from the gate, and from the crowd square,
both at eye height 120), `stage_preview_side.png` and `stage_preview_plan.png`. The red
boxes are 128-tall figures for scale.

### Placement, derived from the config

- **Anchor**: the configured `Stage` zone's marker, (2048, 6548), heading **180**
  (south). The stage faces the marker's heading. The deck centre is the marker moved
  `forwardOffset` 160 toward the audience, to **(2048, 6388)**, which keeps the rear
  timbers clear of the wall.
- **On the sightline**: the main gate at (2048, -2777) already faces the Stage marker,
  so the deck, steps and frame are centred on **x 2048**, the gate-to-stage axis. No
  post, beam or clutter stands on that axis in front of the deck; the steps are on it by
  design.
- `fairWorld.stage` holds everything in stage-local coordinates (`u` across, `v`
  toward the audience), so the stage follows its zone if the zone ever moves. The
  marker must face along a world axis, and the generator refuses otherwise (see the
  rotation note below).

### Dimensions

| Part | Value |
| --- | --- |
| Deck | **1,750 wide x 912 deep**, top **134** above the crowd square (about head height), 7 x 4 porch sections |
| Steps | one central flight, **744 wide**, 5 treads, **22.3 risers**, 110 treads, **550 run** (about 14 degrees), foot at (2048, 5382) |
| Frame | posts **700** tall; header beams at 640 and 575, rafters at 690; overall about 1,900 x 1,560 on plan, **713** high |
| Audience space | the crowd square in front, from the step foot (y 5382) back to about 4,350: roughly 1,000 deep, **empty** |

### Vanilla assets used (all Skyrim.esm, referenced, nothing copied)

| Role | STAT | FormID | Count | Scale | Collision |
| --- | --- | --- | --- | --- | --- |
| Deck | `Walkway01` (farmhouse porch: plank deck on corner posts with knee braces) | `01C570` | 28 | 1.0 | compressed mesh, unscaled |
| Steps | `StockadeScaffoldTop0Sided01` (camp scaffold plank top) | `0533C0` | 15 | 1.0 | **box** |
| Risers | `StockadeWoodplanks01` | `0533D5` | 15 | 1.0 | compressed mesh |
| Deck skirt | `StockadeWoodplanks04` (250 x 143 plank panel) | `0533D8` | 22 | 1.0 | compressed mesh |
| Posts, beams, rafters, braces | `StockadeWoodbeam01` (bark log, 306 long) | `0533D0` | 6 + 20 + 21 + 6 | 1.55-2.34 | convex hull |

**133 references in total.**

- **Deck**: tiled at **250 x 228**, the size of the plank surface measured from the mesh.
  The object bounds say 272 x 256, which includes post ends; tiled at that size, the
  preview showed a slit at every joint. The porch's own posts carry it down to the
  ground, with a plank skirt on the front, sides and back, left open behind the steps.
- **Steps**: each tread rests on the one below. A board under each front edge closes
  the riser. There are no railings.
- **Frame**:
  - chunky round-log posts at the front corners, with four more along the rear
  - a doubled header at the front and a top beam and rail at the rear
  - side beams, and seven log rafters as a slatted partial canopy
  - the rear bays X-braced as a light timber backing
  - the sides and front left open
- **Handmade**: every log wanders up to 5 in height, 1 degree in yaw and 3% in size.
  Posts lean up to 0.8 degrees and sink up to 12, all from fixed integer hashes.

### Rejected, and why

| Candidate | Why not |
| --- | --- |
| `WalkwayStairs1/3/4/8/15` (farmhouse porch stairs) | steep narrow flights with newel posts, and `WalkwayREnd01` has a rope rail; the brief asks for broad and welcoming, no rails |
| `StockadeScaffoldStairs01` | 45 degrees with open ladder treads: defensive, not welcoming |
| `ShipStairs01` | small, awkward profile |
| `StockadeScaffoldBase*` as the deck | 192 tall, wants sinking, and reads as military scaffolding; the farmhouse porch reads rustic and civilian |
| Stepping with `Walkway01` pieces | their corner posts would stand in the treads, in the walking line |
| `ShackRoofMid01` / `Side01` as a canopy | **single-sided** planes: invisible from below, where the audience stands |
| `OrcAwning*` | bone and hide: reads orcish |
| `LargeNordicTent01`, `LargeImperialTent01` | a closed hide mound, and an Imperial military tent with the dragon emblem |
| `SMarketStallTop`, `MrkMarketStallRoof01` | a wooden slat pergola; a roof built onto its own small stall frame |
| Market-stall canvas (`whmarketstallroof01.dds`, the concept's cream-and-grey stripe) | **exists only baked into whole Windhelm and Riften stall meshes**, with their counters and frames |
| `StockadeFreewallBeam01` as posts | compressed-mesh collision, which the project knows scales unreliably; the log's convex hull scales |

**Cloth canopy: open question for Barry.** Vanilla has no standalone cloth awning, so
this pass uses the brief's "timber and/or cloth" allowance: log rafters give the
silhouette and leave the stage open. A true cloth awning like the concept's needs a
small project-owned mesh, either a draped sheet or a pair of swags, textured with
vanilla `whmarketstallroof01.dds` by path, not copied. The brief said not to author one
unless vanilla genuinely can't, and the audit above shows that vanilla can't. It is
left for Barry to approve.

### Rotation note (durable)

Skyrim applies a reference's rotations about the world axes, **Z first, then Y, then
X**. The approved pitched stair cheeks depend on it: they are yawed 90 degrees, then
pitched about world X. So every stage log is laid with a yaw, then at most one tilt
about the world axis across it, which is why the stage must face along a world axis.
Braces are crossing pairs about a shared centre, so they come out right whichever sign
a tilt takes. The preview renderer uses the same convention.

### Future attachment points (nothing placed yet)

These are all in `fairWorld.stage`, in stage-local coordinates:

| For | Where |
| --- | --- |
| Front banners and bunting | the header beams at 640 and 575, spanning u ±955 |
| Rear backdrop banners | the rear rail at 380 and the rear top beam at 640 (v -490) |
| Braziers at the front corners | on the deck, inside the posts at u ±905, v 490 |
| Performer markers | on the deck (top 134, 1,750 x 912); the Stage marker already stands on it |

### Collision observations (not yet tested in game)

- **Deck**: `Walkway01` at scale 1, its own compressed mesh, so it should walk like any
  farmhouse porch.
- **Steps**: box colliders at scale 1, with 22-unit risers, well within the player's
  step height.
- **Logs**: convex hulls at scale 1.55-2.34. They should scale with the mesh, but walk
  into a post to confirm.
- **Skirt boards**: compressed mesh, unscaled.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `158d7203...`.
- Read back against `b77160ca...`: 835 of 836 non-backdrop records identical, 1
  intended change (the Stage marker's Z), 133 new stage references. Trees (518) and
  mountains (24, all `0x10400`) are unchanged in content.
- Geometry checked against the written plugin's meshes:
  - 0 stage vertices outside the compound, and the nearest above-ground stage geometry
    is 100 from the wall line
  - 0 stage geometry in the audience area south of the step foot
  - nothing but the steps on the gate-to-stage axis in front of the deck
- Preview renders made from the plugin: the deck is continuous (after the 250 x 228
  fix), the risers are closed, and the frame reads from the gate.
- ESP deployed byte-identical. No new assets this pass.

### What Barry should test in game (this pass)

1. **From the gate** (`player.moveto SkyrimFairWorldEntranceMarker`, face north): do you
   immediately read the timber pavilion at the end of the avenue as the main stage?
2. **Walk up the avenue and the steps**: are the steps broad and easy, is the deck solid,
   and do any tiles show a seam?
3. **Walk round it**: how does it read from the sides and behind, and does anything float
   or poke through?
4. **Collision**: walk into a post and a skirt board.
5. **Scale**: the deck is at head height and the frame about 5.5 people tall. Does it
   hold the crowd square, or want to be bigger or smaller?
6. **The canopy**: are timber rafters enough for now, or do you want the small cloth
   awning mesh (see above)?

## Mountain backdrop and the view through the gate (previous pass; awaiting review)

Still current. The trees and mountains are renumbered by the stage pass, with their content unchanged.

Barry reviewed the palisade in game: "the gate looks incredible". Two things came back.
The gate model **is open** (the asset, not the placement). And through it, and over
the wall, the outside looked bare. So this pass adds vanilla mountains to the backdrop
and closes the forest behind a short clearing in front of the gate. **Nothing else
changed**: read back against the reviewed build (`da78301a...`), all **836** records
that are not trees or mountains are identical. That covers the wall, gate, terrain,
zones, markers, Tamriel and the sandbox.

### How the mountains draw in a world with no LOD

The world has no LOD, so an ordinary reference draws only while its cell is loaded. A
mountain 15,000 away would come and go as the player walked about. Vanilla solves this
in its small worlds, and it was audited before anything was placed. Skuldafn, Sovngarde,
Japhet's Folly and Tamriel itself keep their distant cloud meshes as references in the
worldspace's **persistent cell**, flagged **Persistent + Is Full LOD (`0x10400`)**, and
always inside the world's object bounds (Skuldafn's sit about 200,000 out, within its
bounds of cells 0..65). The mountains are placed exactly that way. Full LOD alone,
without Persistent, is not what vanilla does for distant scenery, so it was not tried.

### The mountains

**24 vanilla snow-covered mountain STATs** (the `_HeavySN` snow-shader variants, to match
the concept's snowy peaks), in two irregular rings round the compound centre (2223, 2198):

| Row | Radius | Count | Pieces |
| --- | --- | --- | --- |
| near | 13,000-15,500 | 11 | ridges and small cliffs: tops 1,970-4,840, 8-18 degrees above a standing player's eye at the centre |
| far | 17,500-20,500 | 13 | peaks and big cliffs: tops 5,420-8,220, 15-24 degrees |

| STAT | FormID | Placed | Scale |
| --- | --- | --- | --- |
| `MountainPeak01_HeavySN` | `043321` | 7 | 0.95-1.13 |
| `MountainCliffSm01_HeavySN` | `050DC0` | 6 | 0.90-1.14 |
| `MountainCliff04_HeavySN` | `027DDC` | 3 | 0.84-1.16 |
| `MountainRidge01_HeavySN` | `05205B` | 3 | 0.94-1.07 |
| `MountainCliff01_HeavySN` | `048DE0` | 2 | 1.18-1.19 |
| `MountainPeak02_HeavySN` | `046031` | 2 | 1.00-1.07 |
| `MountainRidge02_Heavy_SN` | `05304E` | 1 | 1.08 |

- Each row is evenly spaced in angle, with each piece wandering up to 30-35% of the
  spacing. The two rows are offset (start bearings 9 and 23) so near and far pieces do
  not line up, and every piece takes a hashed radius within its band, yaw and scale.
- **Sunk so no base ever shows.** Every mesh's lowest point is placed at Z -1,000, read
  from its own bounds in Skyrim.esm. At mountain distance the wall hides everything
  below about 500 from anywhere inside the compound. When the hill cells beyond are not
  loaded, a mountain therefore still reads as rising from behind the trees, not floating.
- Everything sits inside the world's object bounds (cells -5..6). The generator
  refuses a mountain outside them. The WRLD record is unchanged.
- Due south, straight out through the open gate, is a far `MountainCliff01` at bearing
  186 with near ridges at 164 and 211 either side.

Plan, 2,048 units per character, north up (`o` compound, `n` near row, `M` far row):

```text
    ......................
    ..............M.......
    .........M............
    .......M..............
    .......n...n..........
    ....M..........n......
    ..................M...
    ......................
    .....n................
    .........oooo....n....
    .........oooo.......M.
    ..M.n....oooo.........
    .........oooo.........
    .................n....
    ....n.............M...
    ...M..................
    .......n.......n......
    ...M........n...M.....
    ......................
    ...............M......
    .........M............
    ......................
```

### The view through the gate

The palisade pass kept every tree 1,800 from the gate and out of a 28-degree cone
straight out of it. That was for an approach from outside, which this isolated world
will never have, and it is why the open gate showed bare ground. Now the rules are:

- no tree within 700 of the gate;
- an 18-degree clearing straight out, only for the first 2,200, which reads as a path
  leading away;
- beyond that the forest closes in.

Trees: **518** (was 487). The nearest to the gate is 936 away. 17 now stand in the view
straight out of the gate, the nearest 2,300 out, with the far mountain behind them. The
per-species spread is otherwise as before: `TreePineForest01` 77, `02` 142, `03` 12,
`04` 131, `05` 156.

The gate's own collision is unchanged. Barry's package gives it a closed-gate box, so the
opening should be solid even though it looks open. **Walk into it to confirm.** The
teleport pass will decide what the open gateway shows.

### Records changed

| Type | Change |
| --- | --- |
| REFR, persistent cell | +24 mountains (`0x10400`) |
| REFR, cells | trees 487 -> 518, and all renumbered (they are placed last; nothing references them) |
| Everything else | identical |

### Verification of this pass

- Release build clean; generator run twice, identical SHA256 `b77160ca...`.
- Read back against `da78301a...`: 836 of 836 non-backdrop records identical. The 24
  mountains are all `0x10400`, all vanilla STATs, all in the persistent cell, 0 outside
  the world's bounds, and every mesh bottom is at -1,000. The persistent cell still
  holds the five zone markers.
- ESP deployed byte-identical. No new assets this pass.
- **Not verified in game**: how the mountains look, their draw distance and fog, and
  frame rate.

### What Barry should test in game (this pass)

1. From the centre, turn round: snowy mountains above the treeline all the way round,
   with the far peaks behind the near ridges, and none floating.
2. At the gate, look out through it: a short path, then trees, then mountains.
3. Walk from one end of the compound to the other. The mountains should never pop in
   or out. If they do, the Full LOD approach is not working in this world. Say so.
4. Are they too big, too small or too close? Each row's radius and scales are config
   values.
5. Frame rate.

## Palisade compound, main gate and forest backdrop (previous pass; gate approved in game)

Still current, except the forest's gate clearing, which the mountain pass above shortened (518 trees now, renumbered).

Barry approved the isolated `SkyrimFairWorld` prototype in game. This pass replaces the
temporary banner-pole markers with his custom palisade, adds the Viking gate at the main
entrance, and rings the compound with vanilla conifers. **The approved layout is
unchanged**: the perimeter polygon, gate point, avenue, zones, terrain and ground
painting are the same config values as before. Read back from the written plugin, all
121 LAND records, every cell header, the WRLD header, the persistent cell and the five
zone markers are identical to the approved build. The Tamriel terrace, staircase,
embankment and sandbox are untouched: all 543 records outside the fair world's cells are
identical.

Not built, by instruction: the teleport / "Enter The Wanderer's Fair" interaction, the
Tamriel gate, stalls, NPCs, navmesh, stage systems, music, archery, clutter.

### Custom assets, bundled

Barry's Skyrim-ready conversions, both **CC BY 4.0** (attribution recorded in
`CREDITS.md`). The deployable files live in the repo at `assets/meshes/barry_palisades/`
and `assets/textures/barry_palisades/`, byte-identical to the `Data/` folder of Barry's
package `assets/Skyrim_Palisade_Assets/`. They are deployed to the MO2 mod at the same
relative paths:

| File | SHA256 | Deployed |
| --- | --- | --- |
| `meshes\barry_palisades\palisade.nif` (24,655 bytes) | `d1d92fc9...43b2` | identical |
| `meshes\barry_palisades\viking_palisade_gate.nif` (468,624 bytes) | `7f5bfd7f...8349` | identical |
| `textures\barry_palisades\palisade\material_00_{d,n}.dds` | | identical |
| `textures\barry_palisades\viking_palisade_gate\material_00..23_{d,n}.dds` (48 files) | | identical |

The NIFs reference their textures as `textures\barry_palisades\...`, which resolve to
those files (checked in the NIF strings). Measured, per the package's validation and
build scripts: the palisade is **138.97 wide (local X) x 11.29 deep x 140 tall**, and the
gate is **181.44 x 43.52 x 175.45**. Both have origins at bottom centre, fixed zero-mass
box collision on the Static layer, and no LOD meshes.

### New STAT records

| EditorID | FormID | MODL | OBND |
| --- | --- | --- | --- |
| `SkyrimFairPalisade` | `000B0F` | `barry_palisades\palisade.nif` | -70,-6,0 .. 70,6,140 |
| `SkyrimFairPalisadeGate` | `000B10` | `barry_palisades\viking_palisade_gate.nif` | -91,-22,0 .. 91,22,176 |

Bounds come from the measured sizes (the terrace kit STATs carry none).

### Palisade: placement strategy

- **106 panels** at scale **2.5**, so each is 347 wide and **350 tall** (about 5 m). The
  crest wanders between **316 and 364** because each panel's scale varies by up to 4% and
  it sinks by up to 20. That is well above a standing player's eye (about 120), so the
  empty ground beyond is never visible over the wall.
- Laid **edge by edge along the approved 17-point outline**, each panel turned to its
  edge. Each edge's run carries **48 past both vertices**, so neighbouring edges cross at
  every bend. Each run is filled with the fewest panels that still **overlap by 6%** (21
  units), spaced evenly so there is never a short filler piece.
- **Handmade, not plotted**: from a fixed integer hash (never a string hash), each panel
  wanders up to 1.2 degrees in yaw and 5 units off the line, varies up to ±4% in scale
  and sinks 0-20. About half are turned round so the same face does not repeat along the
  wall.
- **Blended into the gate**: the south edge's run is split at the gate, and the panel
  either side **tucks 31 and 36 units into the gate** (the config asks for 32).
- The wall stands on the painted perimeter strip, so the stony strip reads as its
  trodden footing.

### Main gate

- `SkyrimFairWorldMainGate` (`000B11`), a static `SkyrimFairPalisadeGate`, closed, with no
  script or animation. It stands at the approved gate point **(2048, -2777)**, where the
  avenue starts, at scale **2.5**: 454 wide and **439 tall**, so it rises about 90 above
  the wall.
- **It faces the Stage marker** (heading 0, due north). The view out of the forecourt
  therefore runs straight up the avenue to the stage at (2048, 6548). The avenue's own
  gentle bend stays as approved.
- The model's front and back were not identifiable from the data. If the gate turns out
  to be facing the wrong way, `fairWorld.gatePiece.yawOffsetDegrees: 180` turns it round.
- The teleport and activation come in a later pass.

### Forest backdrop: placement strategy

**487 vanilla trees**, all Skyrim.esm TREE records, scenery only:

| Tree | FormID | Count | Scale | Distance beyond the wall |
| --- | --- | --- | --- | --- |
| `TreePineForest01` (2,498 tall at 1.0) | `01306D` | 71 | 0.80-1.20 | 574-4,608 |
| `TreePineForest02` (2,501) | `018A02` | 132 | 0.80-1.25 | 482-5,188 |
| `TreePineForest04` (2,085 above origin) | `04FBB0` | 121 | 0.80-1.25 | 556-4,826 |
| `TreePineForest05` (1,465) | `051126` | 151 | 0.86-1.35 | 424-5,191 |
| `TreePineForest03` (860, understory, within 1,800 only) | `04B016` | 12 | 0.94-1.48 | 382-1,762 |

These are the Falkreath and Rift pine-forest conifers, and in Tamriel they are placed at
0.35-1.67. Snow and dead variants are excluded.

- **Candidates** come from a 440-unit jittered grid over the band from 320 to 5,200
  beyond the wall. Each is kept by chance against a density that starts **sparse by the
  wall**, peaks from 700 to 2,600 out and thins toward 5,200, so the view has layers of
  trunks and canopy.
- **Clumps and clearings**: that density is multiplied by a low-frequency clustering
  field with a 1,700-unit period, and where the field is low there are no trees. The
  result is irregular stands with gaps of sky between them, not a ring. Because the
  ground rises beyond the flat margin, trees further out stand higher, which stacks the
  canopy.
- **Variation per tree**: species by weight, scale within its range, any yaw, and a lean
  of up to 1.5 degrees. Trunks are sunk 24.
- **Gate kept open**: no tree within 1,800 of the gate, and none in a 28-degree cone
  straight out of it, so a future approach from outside stays clear. From inside, the
  gate sightline runs north to the stage and past it into the northern stands.
- Canopies do not overhang the wall. Each species has a minimum distance (320 to 520)
  set to about its canopy radius.

Plan read back from the written plugin, 512 units per character, north up (`#` wall,
`G` gate, `^` a cell holding trees):

```text
                   ^
                 ^^      ^^
                ^  ^ ^ ^  ^ ^^^ ^
                  ^^  ^^^^ ^   ^^
          ^        ^^^^^^ ^^^  ^^^^
           ^       ^^^^^ ^^^^   ^ ^
             ^ ^^  ^   ^^ ^^^^ ^^^ ^
             ^^^^^^^        ^^^ ^ ^^^
            ^^ ^^^ ^         ^^^^ ^  ^^
         ^^^^^^^            ^^^  ^^^^ ^
        ^ ^   ^     #########^ ^ ^^  ^
         ^ ^^^^^  ###       ## ^^^^^^
        ^ ^^^    ##          ## ^^^^^^
           ^^^^^##            ##^^^ ^^ ^ ^
       ^        #              # ^^^ ^
        ^ ^   ^##              ##^^  ^ ^
      ^^^ ^ ^^^ #               #^ ^^^ ^^^
        ^^     ^#              ##^^ ^ ^   ^
        ^   ^ ^ #              # ^   ^ ^^^
       ^    ^ ^^#              # ^^^^^   ^
        ^    ^^ #              #^^ ^ ^^ ^
       ^ ^ ^^^^##              # ^^^   ^^
       ^  ^^^ ^#               # ^^^^^^
       ^^^^^ ^ #               ##^  ^^
             ^  #              # ^^^^^    ^
        ^^^^ ^^^#              #^ ^^^^
      ^  ^^^^ ^^##             #^^^^  ^
         ^^^^^ ^ ##          ### ^^^^
           ^^ ^   ###      ###  ^^^^^
        ^^  ^^^^^ ^ ###G#### ^^^ ^^^ ^^
         ^^^^   ^^ ^^       ^ ^ ^ ^
         ^ ^^  ^ ^^^^      ^^  ^^^^^^
           ^    ^^^^      ^^^  ^ ^  ^^
           ^^   ^ ^^^         ^^ ^^^^ ^
           ^^     ^^ ^       ^^    ^^
            ^      ^^         ^^
            ^   ^^ ^        ^^^
                  ^         ^ ^^
                              ^
```

**Loading.** The world has no LOD, so a tree only draws while its cell is loaded. **197**
of the 487 are in cells -1..1, which are loaded from anywhere in the compound at the
default `uGridsToLoad` of 5. The rest (cells ±2) load as the player nears that side, and
may pop in at the far side of the compound. The wall hides their bases, so what pops is
canopy. The lasting fix is tree LOD for this worldspace (DynDOLOD/xLODGen, which can use
vanilla pine billboards). There are no real distant mountains in this world; the
"mountains" in the gaps are the generated hills beyond the flat margin.

### Records changed

| Type | Change |
| --- | --- |
| STAT | +2 (`SkyrimFairPalisade`, `SkyrimFairPalisadeGate`) |
| REFR | -33 `FarmBannerPost01` scale posts; +106 palisade panels, +1 gate, +487 trees. SkyrimFairWorld now holds 599 references: 5 zone markers + 594 in its cells |
| CELL, LAND, WRLD, CLMT | content unchanged |

**One-time FormID shift.** The 121 exterior CELL and LAND records are now allocated
straight after the zone markers and before anything placed in them. They moved down by
33 FormIDs, the slots the posts had used, and their content is identical. From now on,
changing the wall or forest never moves them. Nothing references them. The WRLD
(`000A16`), climate (`000A15`), persistent cell (`000A17`) and zone markers
(`000A18`-`000A1C`) keep their IDs.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `da78301a...`.
- The previous approved build (`8b7eb313...`, the deployed copy) and the new one were
  read side by side with Mutagen:
  - **543 of 543** records outside the fair world's cells are identical (Tamriel,
    sandbox, kit STATs, climate, WRLD headers).
  - All 121 LAND records are identical, as are the cell headers apart from FormID, the
    persistent cell and the markers.
- **Wall closure**:
  - 3,863 points every 8 units along the outline all lie inside a wall or gate
    footprint (worst -6.8, i.e. inside).
  - **20,160 sightline rays** from seven interior points (centre, gate, stage, both
    sides, two corners) all cross a wall or gate centreline.
  - A first build had one ray slip between two centrelines at a shallow bend. Panel
    thickness blocked it, but the corner carry-over went from 24 to 48 so it no longer
    depends on thickness.
- **Usable space kept**: no wall or gate footprint enters a zone. The nearest is the
  gate's inner face meeting the entrance forecourt's paint, by design. 0 references
  filed in the wrong cell. 0 banner posts left.
- **Trees**:
  - the nearest is 382 outside the wall line, and the nearest to the gate is 1,862
  - 0 in the approach cone, and the closest pair is 174 apart
  - every tree is a TREE record in Skyrim.esm
- **Deployment**: the ESP is byte-identical in the mod folder, and so are all 52 asset
  files (2 NIF, 50 DDS).
- **Not verified in game**: how it looks, the gate's facing, collision against the wall,
  and performance with 487 full trees.

### What Barry should test in game (this pass)

1. `cow SkyrimFairWorld 0 0`, then turn round slowly. Check for a continuous wall with no
   gaps at the bends, trees above it on every side, and occasional sky between stands.
2. `player.moveto SkyrimFairWorldEntranceMarker` and look at the gate close up. Is it
   the right way round (if not, it's one config value)? Do the panels either side run
   into its posts cleanly? Then turn north: the view should run up the avenue to the
   stage.
3. **Height and scale**: does a 5 m wall with a 6 m gate feel right beside your
   character? Scale is one number each (`palisade.scale`, `gatePiece.scale`).
4. Walk into the wall and the gate: both should stop you.
5. Walk to each side of the compound and watch the far trees. Say if their popping in
   is distracting.
6. Frame rate with the forest in view.
7. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## The isolated worldspace (previous pass; approved by Barry in game)

Everything in this section still holds, except that the temporary posts are gone (replaced by the palisade above) and the cell and LAND FormIDs moved once (see "Records changed" above).

Barry changed direction: the full Wanderer's Fair will eventually live in its own
isolated outdoor worldspace, a large irregular palisade compound reached through a gate
in Tamriel. This pass builds **only the empty canvas**: a new exterior worldspace with
sky, weather, exterior lighting and generated flat ground, with the broad plan painted
into the ground. It is a **parallel prototype**. The raised Tamriel terrace, the
approved staircase and embankment, and the sandbox cell are untouched: all 541 records
of the previous plugin are present and field-for-field identical in the new one (checked
by reading both files, below).

Not built at that pass, by instruction: the Tamriel gate, the palisade itself, stalls, NPCs, archery,
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

- The 33 temporary `FarmBannerPost01` scale posts that stood on the perimeter line
  were **removed in the palisade pass**. The wall now stands on that line.
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
| REFR | 38 at that pass (33 posts + 5 markers); the posts are gone now, see the palisade pass |

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

### What Barry was asked to test (done: approved)

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
| Size | 421,468 bytes (412,426 before the stage; 408,356 before the mountains; 366,226 before the palisade; 88,124 before the worldspace) |
| SHA256 | `158d72035e5ba11ecc0e18710357250774a81e26d6fa381ab5859e14d647249e` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` (2026-09-22, night; replaced `b77160ca...`, the mountain build) |
| Sandbox cell | `SkyrimFairSandbox` (`0009E1:SkyrimFair.esp`), interior, 5 x 5 kit tiles, `coc SkyrimFairSandbox` in, `cow Tamriel -2 -4` out |
| Isolated worldspace | `SkyrimFairWorld` (`000A16:SkyrimFair.esp`), 121 cells, `cow SkyrimFairWorld 0 0` in; palisade, gate and forest per the palisade pass, mountains per the mountain pass, main stage per the current pass |
| Palisade assets | `meshes\barry_palisades\` (2 NIF) and `textures\barry_palisades\` (50 DDS), deployed byte-identical to `assets/` |
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
