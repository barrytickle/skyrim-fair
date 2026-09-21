# Local Audit

This file is the current source of truth for Barry's local Skyrim Fair development environment.

## Workflow

A local coding agent (currently Claude) should **overwrite this file in full** after each audit or local verification pass, then commit and push the change to the active project branch.

Do not append historical reports. Git history already preserves previous versions.

Recommended commit message:

```text
docs: refresh local audit
```

ChatGPT should read the latest version of this file from GitHub before making changes that depend on Barry's local Skyrim installation, load order, installed asset packs, animation stack, or generated plugin output.

## Deployed build — raised terrace at +192, ramp landing on grade

The fairground reads as a deliberate raised terrace, with the entrance ramp descending
from the north-east corner to the existing vanilla road. The ramp was re-cut this pass:
at 1:8 its foot hung **128 units in the air**, because the ground north of the site falls
away almost as fast as a 1:8 ramp descends. At **1:5.3** it lands dead on grade.

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | **55,730 bytes** |
| sha256 | `32e8f98388e89d4e297c44a3febe927ae0734f2cbaac7eb29808a4fc810783de` |
| Masters | `Skyrim.esm` only |
| Records | 1 WRLD, 6 CELL, 5 STAT, 96 REFR |
| Centre | `X -5888, Y -12928` (unchanged) |
| **Floor Z** | **-5504**, raised **+192** from -5696 |
| Cells touched | `-3,-4`, `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| Deployed | byte-identical; `SkyrimFair_Ramp_512.nif` rebuilt and redeployed, other 5 NIFs unchanged |

Footprint outline and the 22-tile mask are **unchanged**.

### Why the platform previously looked buried

Two causes, and neither was terrain poking through — re-sampling every 128-unit
heightmap point under the paving found **zero** points above the floor.

1. The floor sat at *exactly* the terrain maximum, so it was flush at its highest point
   and only 0-120u proud elsewhere. It never read as a platform.
2. **Grass.** `No Grass In Objects` is installed with `Use-grass-cache = true` and
   `Load-from-BSA = true`, and the cache in `Still in Skyrim - Grass Cache.bsa` predates
   the platform. NGIO culls grass under objects at **cache generation** time, so grass
   still renders where the platform now stands. **Regenerating the grass cache would
   remove it.** At +192 the paving also clears tundra grass height anyway.

### Elevation

| Lift | Floor | Exposure min / max / mean | Retaining courses |
| --- | --- | --- | --- |
| +160 | -5536 | 160 / 256 / 203 | 1 |
| **+192** | **-5504** | **192 / 288 / 235** | **2** |
| +208 | -5488 | 208 / 304 / 251 | 2 |

Per edge at +192: N 239 mean / 280 max, S 232 / 280, E 221 / 272, W 246 / 288.

Retaining rises to **24 pieces**, two stacked courses on the deeper edges. Two courses
are intentional: the height is to be disguised later with rocky embankments, earth,
shrubs and larger stones rather than reduced.

**Shoulder wedges dropped to zero**, and the shoulder `STAT` is no longer created at all
(hence 5 STAT records, not 6). That is the `shoulderMaxDrop` rule working as designed — a
32-unit verge only reads as a soft transition against a small step, and every edge now
exceeds 192u. Embankment treatment for a wall this tall needs different pieces.

### The road, and the ramp that reaches it

The vanilla road is built from **meshes**, not land texture: `RoadStraightLong02`,
`RoadSCurveR01` and `RoadChunkL/M/S*`, running east-west at Y about -10,050 to -10,750,
immediately north of the site. `LDirtPath01` exists only about 5,900u away to the WNW and
is not the relevant approach.

Critically, **the road sits in a dip directly north of the site and climbs eastward**:

| Road point | Z | Grade from floor -5504 |
| --- | --- | --- |
| `(-7081, -10064)` | -5992 | 1:3.3 |
| `(-5960, -10040)` | -5906 | **1:4.0** |
| `(-4993, -10242)` | -5875 | 1:3.9 |
| `(-3943, -10360)` | -5748 | **1:7.6** |
| `(-3034, -10744)` | -5640 | 1:14.2 |

A ramp centred on the north edge — where it previously sat — would have needed **1:4**.
The ramp therefore moved to the **eastern end of the north edge**, where the road has
climbed and the intervening ground is flatter. A new `rampAlign` config option controls
this.

The first cut of that ramp used 1:8, and in game its foot floated clearly above the
ground. Re-measuring along the ramp centreline (`x -4864`) showed why: the terrain there
drops about 1:6 going north, so a 1:8 ramp *loses* ground the further it runs. The fix is
a steeper ramp, not a longer one.

| Rise per tile | Grade | Tiles | Run | Foot Z | Ground Z | Gap |
| --- | --- | --- | --- | --- | --- | --- |
| 64 | 1:8.0 | 5 | 2,560 | -5824 | -5832 | +8 |
| **96** | **1:5.3** | **4** | **2,048** | **-5888** | **-5888** | **0** |
| 128 | 1:4.0 | 2 | 1,024 | -5760 | -5776 | +16 |

96 wins on every count: it lands exactly on grade, keeps the same four-tile run, and needs
no new tile length — only `RAMP_RISE` in the kit script changes, so the ramp mesh is
re-exported rather than redesigned.

| Field | Value |
| --- | --- |
| Orientation | **due north, rotation 0** (rotation-safe) |
| Position | 2 segments wide at X -5120 and -4608, edge Y -12160 |
| Length | **4 chained tiles, 2,048u run** |
| Grade | **1:5.3** (96u rise per 512u tile) |
| Tile Z levels | -5504, -5600, -5696, -5792 |
| Surface | -5504 at the terrace down to a foot at **-5888** |
| Total fall | **384u** |
| Native ground at the foot | **-5888** |
| **Vertical gap at the foot** | **0 units** |

The nearest road piece is `047F9B:Skyrim.esm RoadSCurveR01`, origin `(-4993, -10242, -5875)`
with a 1,282-unit mesh radius, so the ramp foot at `(-4864, -10112)` sits **183 units from
its origin, well inside its own footprint**. Its origin Z of -5875 is 13 units above native
ground, which is what a road mesh laid on the terrain should be. An earlier pass quoted
"road Z -5747 to -5763" for this junction; those figures belong to the `RoadChunk` pieces
900-1,000 units further **east**, not to the piece the ramp actually meets.

The map marker sits at the ramp foot, `(-4864, -10112, -5888)`, so fast travel arrives at
the roadside and the approach is up the ramp.

### References disabled — five, all named explicitly

| FormID | EditorID | Radius | Why |
| --- | --- | --- | --- |
| `00048032:Skyrim.esm` | `RockTundraLand01Tundra01` | 1418 | slab over the paving |
| `0004801A:Skyrim.esm` | `RockTundraLand02Tundra01` | 1767 | slab over the paving |
| `00048031:Skyrim.esm` | `RockTundraLand02Tundra01` | 1767 | slab over the paving |
| `00047F8C:Skyrim.esm` | `RockTundraLand02FieldGrass01` | 1767 | slab over the paving |
| `00023362:Skyrim.esm` | `DirtCliffs01FieldGrass01` | 899 | earth cliff in the ramp corridor |

All five are plain scenery — verified in the written plugin to carry only `DATA`, `NAME`
and, for the scaled cliff, `XSCL`. No LAND edit, so the ground beneath is untouched.

### Deliberately preserved

- **The road itself** — `047F9B:Skyrim.esm RoadSCurveR01` and every other road piece.
- **The roadside detail** — both `FenceWoven01` and `HandCart01Wheel`, to keep the
  entrance feeling inhabited.
- **`critterSpawnInsects_Many`** and the `LvlAnimalPlainsPrey` spawn — both
  enable-parented; absent from the plugin entirely.
- **All vegetation.** Checked against the ramp surface using OBND heights: of the eight
  references in the ramp corridor, only **one** physically breaks the ramp plane, and that
  is the handcart, clipping by about 23 units at the extreme foot. Nothing was cleared for
  tidiness.

### Classifier fix, now committed

`tools/footprint_audit.py` is a standalone, re-runnable audit carrying the fix for the bug
that hid two large statics:

- Bethesda appends texture variants to EditorIDs, and they mislead.
  `RockTundraLand02FieldGrass01` is a 1767-unit rock slab and `DirtCliffs01FieldGrass01`
  an 899-unit earth cliff, but a raw substring match filed both under *vegetation* because
  of "FieldGrass". Variant suffixes are now stripped before any keyword test, rock and
  earth tokens are matched before vegetation, and **mesh radius outranks keywords** —
  anything at or above 600 units is a large mass.
- It also prepends the implicit masters, which MO2's `plugins.txt` omits. Forgetting them
  had silently dropped the whole vanilla layer from an earlier pass, reporting 30
  intersecting references instead of 101.

Both slabs now classify correctly as **large rock / earth mass**.

### Verification

25 structural checks, all passing, by parsing the written ESP independently of Mutagen:
single master, author intact, mesh paths under `SkyrimFair\`, 34 references on the floor
plane, 8 ramp tiles at four Z levels 96u apart all at rotation 0 and on the eastern
segments, the ramp foot landing at exactly -5888 where native ground is -5888, **exactly five** references disabled and exactly the five named, road, fences,
handcart, vegetation and critter markers all untouched, marker persistent and at the ramp
foot, and no LAND / NAVM / NPC / quest / script records.

**All six cell overrides byte-identical to vanilla.** `modlist.txt`, `plugins.txt` and
`loadorder.txt` unchanged.

### What to test in game

1. **does the foot of the ramp now meet the ground cleanly** — the point of this pass
2. does 1:5.3 feel walkable, or steep enough to fight the player's step-up
3. does the ramp collision feel smooth up and down, with no snag at the top or bottom
4. does the ramp read as joining the road, rather than merely ending near it
5. does +192 still feel substantial without being ridiculous
6. do the surviving fences and handcart help the entrance feel integrated

Expect grey untextured geometry, no navmesh, and **grass still growing through the paving
until the grass cache is regenerated**.

## Footprint blueprint and intersection audit (deployed placement)

Derived from the **actual placements in `dist/SkyrimFair.esp`**: the plugin is parsed,
the paving references located, each tile's extent computed from its kit dimensions, and
the unshared tile edges traced into a closed outline.

- paving: **22 tiles** on a common 512-unit grid (4 x 1024 fill + 6 x 512 edge)
- bounding extent: **X -7424 .. -4352**, **Y -14208 .. -11648** (3072 x 2560)
- floor plane: **Z -5696**
- the outline is **not rectangular** - 14 corners

### Perimeter, closed polygon (world units, in order)

| # | X | Y | | # | X | Y |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | -7424 | -13184 | | 8 | -4352 | -13696 |
| 2 | -6912 | -13184 | | 9 | -4352 | -12160 |
| 3 | -6912 | -13696 | | 10 | -5376 | -12160 |
| 4 | -6400 | -13696 | | 11 | -5376 | -11648 |
| 5 | -6400 | -14208 | | 12 | -6912 | -11648 |
| 6 | -4864 | -14208 | | 13 | -6912 | -12672 |
| 7 | -4864 | -13696 | | 14 | -7424 | -12672 |

### Paved tile centres (512-unit grid)
```text
Y  -11904:    -6656    -6144    -5632
Y  -12416:    -6656    -6144    -5632    -5120    -4608
Y  -12928:    -7168    -6656    -6144    -5632    -5120    -4608
Y  -13440:    -6656    -6144    -5632    -5120    -4608
Y  -13952:    -6144    -5632    -5120
```

### Intersecting references: 32 total, 18 on the foundation

Bounds come from each base object's `OBND`, scaled and Z-rotated into world space, so
objects whose **origin lies outside** the footprint are still caught. Winning records
only, resolved through the full load order including the implicit masters.

| Class | Total | On foundation |
| --- | --- | --- |
| UNSAFE - do not touch | 2 | 1 |
| decorative clutter | 4 | 3 |
| disabled by Skyrim Fair | 3 | 3 |
| vegetation | 23 | 11 |

| FormID | Base | Type | World X, Y, Z | Scale | Radius | AABB (X / Y) | Overlaps | Class | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `048032:Skyrim.esm` | `039224:Skyrim.esm` RockTundraLand01Tundra01 | STAT | -6806, -11401, -5976 | 1.0 | 1418 | -8241..-5513 / -12695..-10337 | **foundation** | disabled by Skyrim Fair | initially-disabled |
| `04801A:Skyrim.esm` | `03925D:Skyrim.esm` RockTundraLand02Tundra01 | STAT | -5729, -13941, -5887 | 1.0 | 1767 | -7085..-3860 / -15823..-12288 | **foundation** | disabled by Skyrim Fair | initially-disabled |
| `048031:Skyrim.esm` | `03925D:Skyrim.esm` RockTundraLand02Tundra01 | STAT | -4074, -11675, -5846 | 1.0 | 1767 | -5871..-2506 / -12749..-10087 | **foundation** | disabled by Skyrim Fair | initially-disabled |
| `0DC5B5:Skyrim.esm` | `0422B1:Skyrim.esm` LvlAnimalPlainsPrey | NPC_ | -6611, -14689, -5853 | 1.0 | 0 | -6611..-6611 / -14689..-14689 | margin | UNSAFE - do not touch | enable-parented, ACTOR |
| `0CB03E:Skyrim.esm` | `0C5209:Skyrim.esm` critterSpawnInsects_Many | ACTI | -5259, -13344, -5704 | 1.0 | 91 | -5323..-5195 / -13408..-13280 | **foundation** | UNSAFE - do not touch | enable-parented, base has script |
| `0CB048:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -7375, -11444, -5808 | 1.0 | 20 | -7379..-7363 / -11469..-11430 | margin | decorative clutter |  |
| `0CB041:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -5514, -13834, -5660 | 1.0 | 20 | -5530..-5510 / -13851..-13812 | **foundation** | decorative clutter |  |
| `0CB03F:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -5469, -13142, -5661 | 1.0 | 20 | -5491..-5458 / -13159..-13125 | **foundation** | decorative clutter |  |
| `0CB040:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -4762, -13127, -5699 | 1.0 | 20 | -4777..-4759 / -13143..-13104 | **foundation** | decorative clutter |  |
| `0CB03D:Skyrim.esm` | `03554E:Skyrim.esm` RockShelf01FieldGrass01 | STAT | -4971, -13024, -5713 | 1.0 | 660 | -5631..-4388 / -13529..-12562 | **foundation** | vegetation |  |
| `047F8B:Skyrim.esm` | `03925C:Skyrim.esm` RockTundraLand02FieldGrass01 | STAT | -9356, -11877, -6087 | 1.0 | 1767 | -11020..-7494 / -13183..-10046 | margin | vegetation |  |
| `047F8C:Skyrim.esm` | `03925C:Skyrim.esm` RockTundraLand02FieldGrass01 | STAT | -8642, -13520, -5876 | 1.0 | 1767 | -10179..-6752 / -15047..-11557 | **foundation** | vegetation |  |
| `048023:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -5139, -13619, -5734 | 0.9 | 126 | -5254..-5026 / -13721..-13512 | **foundation** | vegetation |  |
| `048054:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -7344, -11460, -5840 | 0.85 | 135 | -7451..-7237 / -11558..-11363 | margin | vegetation |  |
| `048051:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -6415, -11175, -5775 | 0.98 | 155 | -6543..-6287 / -11293..-11057 | margin | vegetation |  |
| `048021:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5521, -13780, -5721 | 0.99 | 157 | -5678..-5364 / -13936..-13624 | **foundation** | vegetation |  |
| `048024:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5470, -13125, -5708 | 0.86 | 136 | -5589..-5351 / -13236..-13014 | **foundation** | vegetation |  |
| `048052:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -6209, -11104, -5763 | 0.91 | 150 | -6357..-6061 / -11249..-10960 | margin | vegetation |  |
| `048027:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -6037, -12587, -5729 | 0.82 | 135 | -6142..-5932 / -12682..-12492 | **foundation** | vegetation |  |
| `04805B:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4292, -11668, -5746 | 0.82 | 135 | -4399..-4185 / -11784..-11553 | margin | vegetation |  |
| `048053:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -7179, -11336, -5818 | 0.99 | 107 | -7293..-7089 / -11448..-11238 | margin | vegetation |  |
| `048050:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -6560, -11130, -5791 | 0.87 | 94 | -6638..-6457 / -11216..-11043 | margin | vegetation |  |
| `048028:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -6148, -12417, -5729 | 0.7 | 75 | -6204..-6101 / -12485..-12367 | **foundation** | vegetation |  |
| `04801F:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -5962, -14469, -5777 | 0.66 | 71 | -6042..-5900 / -14539..-14401 | margin | vegetation |  |
| `048022:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -5336, -13734, -5708 | 0.83 | 89 | -5416..-5243 / -13826..-13661 | **foundation** | vegetation |  |
| `04805A:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -4328, -11796, -5746 | 0.79 | 85 | -4423..-4255 / -11879..-11715 | margin | vegetation |  |
| `04802A:Skyrim.esm` | `0AAE74:Skyrim.esm` TreeYellowShrub02 | TREE | -7470, -12436, -5849 | 0.87 | 148 | -7603..-7338 / -12564..-12308 | margin | vegetation |  |
| `048020:Skyrim.esm` | `0AAE74:Skyrim.esm` TreeYellowShrub02 | TREE | -5806, -14665, -5800 | 0.85 | 144 | -5908..-5705 / -14773..-14557 | margin | vegetation |  |
| `048025:Skyrim.esm` | `0AAE74:Skyrim.esm` TreeYellowShrub02 | TREE | -5526, -12946, -5712 | 0.93 | 158 | -5638..-5414 / -13064..-12827 | **foundation** | vegetation |  |
| `04802B:Skyrim.esm` | `0AAE75:Skyrim.esm` TreeYellowShrub03 | TREE | -7746, -12406, -5862 | 1.01 | 171 | -7916..-7575 / -12577..-12234 | margin | vegetation |  |
| `048029:Skyrim.esm` | `0AAE75:Skyrim.esm` TreeYellowShrub03 | TREE | -5963, -12407, -5750 | 0.92 | 156 | -6102..-5825 / -12550..-12264 | **foundation** | vegetation |  |
| `048026:Skyrim.esm` | `0AAE75:Skyrim.esm` TreeYellowShrub03 | TREE | -5266, -13343, -5706 | 1.06 | 180 | -5414..-5119 / -13498..-13189 | **foundation** | vegetation |  |

## Site 1 hazards and safe build envelope

Barry's in-game observations were checked against the winning-LAND heightmap and all confirmed.

### Hazards

| Direction | Hazard | Detail |
| --- | --- | --- |
| **East-south-east** | **Western Watchtower** | marker `000DB889` at `(1660, -14699)`, bearing 105 deg, **7,535u** away. Its cells `0,-4` and `1,-4` are the Mirmulnir dragon fight from MQ104 "Dragon Rising". Ground also climbs east: +152u at 2k, +328u at 4k, +320u at 8k. Eastward clearance before those cells: **5,632u**. |
| **South** | **mountain** | level or falling out to ~5,000u, then climbs: +136u at 7,168u, +312u at 8,192u, +472u at 9,216u, **+1,296u at 11,264u**. Practical southern limit **6,000–7,000u**. |
| **South-west diagonal** | **Fort Greymoor exterior** | cell `-3,-5` `FortGreymoorExteriorEdge`, nearest edge **4,404u** away. Also `-3,-3` `FortGreymoorExterior02` at 2,611u (north-west) and `-4,-3` `FortGreymoorExterior01` at 6,676u. The fort's own marker is WNW at 9,219u with a 3,400u radius. |

### Safe envelope

South-west is the only bearing that descends smoothly and cleanly the whole way: −40u at 2k, −256u at 4k, −344u at 6k, −456u at 8k.

The permitted build area is an **L-shape**: the site cell `-2,-4`, due west `-3,-4`, due south `-2,-5`, and east `-1,-4`. All are unnamed vanilla cells with vanilla `LAND` and **zero navmesh edits**. The `-3,-5` diagonal corner is excluded because it is Fort Greymoor's exterior edge.

> **The L-shape is a build constraint, never the visible shape.** Per `docs/DESIGN.md`, the fairground the player sees must read as an irregular organic rocky terrace — curving, tapering, varying in width — not as any geometric outline. Keep the paving on a grid so it can be generated seamlessly, but drive the outline irregularly and hide the stepped silhouette under the shoulder, rocks, shrubs and stall placement.

### Where the flat core belongs

Offsetting the paved core south-west does **not** improve flatness; the best 3072 core stays essentially at the marker:

| Core centre | Offset from marker | Relief |
| --- | --- | --- |
| `(-5632, -12928)` | dy −128 | **160u** |
| `(-5760, -13056)` | dx −128, dy −256 | 160u |
| `(-5888, -13184)` | dx −256, dy −384 | 192u |

So: keep the **paved market core at/near the marker** where it is flattest, and let the **outer fair** — games, archery, stables, future jousting, which `docs/DESIGN.md` already allows on natural ground — spread south-west into `-3,-4` and `-2,-5` where the ground opens out and falls away.

## Platform geometry — measured

3072 x 3072 core centred `(-5632, -12800)`, floor at **Z -5672** (terrain maximum across the core, so pure fill, zero cut, **no LAND edits**).

Extents: X `-7168`..`-4096`, Y `-14336`..`-11264`.

**Cells spanned: two, not four.** An earlier audit said four; that was a sampling artefact from including the exact `x = -4096` boundary, which belongs to cell `-1`. Corrected:

| Cell | FormKey | EditorID | Navmesh edits |
| --- | --- | --- | --- |
| `-2,-4` | `00009A28:Skyrim.esm` | none | **none** |
| `-2,-3` | `00009A07:Skyrim.esm` | none | **none** |

Floor exposure above native terrain, sampled every 128u along each edge:

| Edge | Min | Max | Mean |
| --- | --- | --- | --- |
| **East** | **0u** | 72u | **32u** |
| North | 40u | 160u | 82u |
| South | 40u | 152u | 89u |
| **West** | 16u | **160u** | **92u** |

- The **east edge meets grade** — 0u exposure at `(-4096, -13440)`. This is the natural walk-on side and the cheapest navmesh join.
- The **west edge is most exposed** — up to 160u, peaking at the north-west corner `(-7168, -11264)`. This is the low side, so the **broad entrance ramp belongs on the west / south-west**, matching both the design brief and Barry's build direction.

An earlier audit described the high side as north-east; the measured data says **east**. Corrected here.

## Phase 1 — local asset-authoring toolchain

Read-only inspection. Nothing was installed.

### Available

| Tool | Location | Notes |
| --- | --- | --- |
| **Creation Kit 2.0 (SSE)** | `E:\SteamLibrary\steamapps\common\Skyrim Special Edition\CreationKit.exe` | **v1.7.99.0**, "Bethesda Softworks: Creation Kit 2.0" |
| **BGS Art Tools [Skyrim]** (Blender addon) | `...\Skyrim Special Edition\Tools\ArtTools\Blender\bgs_skyrim_tools.zip` | v1.0.0. Includes `operators/collision_ops.py` — a Collision tab with Create Collider, child colliders, mass/friction |
| **BGS FBX Exporter [Skyrim]** (Blender addon) | `...\Tools\ArtTools\Blender\io_scene_bsfbx_skyrim.zip` | v1.0.0. Bundles its own copy of Blender's `export_fbx_bin.py` / `fbx_utils.py` |
| **AssetWatcher** | `...\Tools\AssetWatcher` | watches FBX output and converts to NIF |
| **Elric** | `...\Tools\Elric` | texture conversion |
| **Archive.exe** | `...\Tools\Archive\Archive.exe` | BSA packing |
| **HavokBehaviorPostProcess** | `...\Tools\HavokBehaviorPostProcess` | behaviour post-processing |
| **Official guide** | `...\Tools\Exporting Blender Art Assets for Skyrim.pdf` | 18 pages, includes "Adding Collision to a Mesh" and "Collision Best Practices" |
| **NifSkope** | `E:\Modlists\Still In Skyrim\tools\nifscope\NifSkope.exe` | |
| **Cathedral Assets Optimizer** | `E:\Modlists\Still In Skyrim\tools\cao` | |
| **Blender** | `C:\Program Files\Blender Foundation\Blender 5.2` | **version 5.2** |
| Also present | SSEEdit, DynDOLOD, xLODGen, Pandora, LOOT, Synthesis, Bethini, ACMOS | |

There is an **officially supported pipeline**: model in Blender -> assign collision with BGS Art Tools -> export BSFBX -> AssetWatcher converts to NIF -> register in the Creation Kit. This needs no third-party collision tooling, which is why the absence of ChunkMerge, NifUtilsSuite and hkxcmd (all confirmed not installed) does not matter.

### Blender version blocker — RESOLVED

Barry has extracted a portable **Blender 3.6.23** build to:

```text
C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\
```

Version verified as `Blender 3.6.23` (build date 2025-06-17). Both Bethesda addons are now installed and enabled in it:

| Addon | Version | Requires | Enabled |
| --- | --- | --- | --- |
| `bgs_skyrim_tools` — "BGS Art Tools [Skyrim]" | 1.0.0 | Blender 3.6.0 | **yes** |
| `io_scene_bsfbx_skyrim` — "BGS FBX Exporter [Skyrim]" | 1.0.0 | Blender 3.6.0 | **yes** |

Installed to `C:\Users\Barry\AppData\Roaming\Blender Foundation\Blender\3.6\scripts\addons`, which is version-scoped, so **Blender 5.2 is untouched**.

Verified present and callable:

- exporter operator `bpy.ops.export_scene.bsfbx_skyrim`
- collision operators `bgs_skyrim.create_rigidbody_skyrim`, `create_collider_skyrim`, `remove_collider_skyrim`, `add_constraint_to_rigidbody_skyrim` and 11 others
- `bgs_skyrim.set_recommended_unit_scale_skyrim` exists but **cannot be invoked headlessly** ("invalid operator call" — it needs UI context), so scene units are set directly in the build script instead

Original blocker, retained for context:

**The pipeline could not be used with Blender 5.2.**

- Both Bethesda addons declare `"blender": (3, 6, 0)` in a legacy `bl_info` dict, and neither ships a `blender_manifest.toml`.
- Bethesda's own guide states: *"Blender version 3.6 is the last Long-Term Support (LTS) version prior to 4.0. Support for Blender 4.0 is currently in beta."*
- Barry has **Blender 5.2** — three major versions past even the beta-supported 4.0.
- The FBX exporter bundles a 3.6-era copy of Blender's FBX exporter internals, which is very unlikely to run against Blender 5.2's Python API.
- Blender currently has **no addons or extensions installed at all**, so neither plugin is even registered yet.

Per this stage's brief, that is a stop-and-document point rather than something to work around.

**What is needed:** an install of **Blender 3.6 LTS** (side-by-side with 5.2 is fine — Blender supports parallel versions), then install the two zips from `Tools\ArtTools\Blender\` into that 3.6 install. Both zips are already present locally; no download beyond Blender itself is required.

Resolved by installing Blender 3.6.23 alongside 5.2, as above. The whole pipeline is now available and no third-party NIF or collision tooling was needed.

## Phase 2 — project-owned tile kit specification

Specified before modelling, as the brief requires, and now built — see Phase 3.

Grid: **128 units**, matching Skyrim's architectural grid and the exterior heightmap interval. All pieces are multiples of 128 so they can be generated and placed from config.

| Piece | Footprint | Height | Purpose |
| --- | --- | --- | --- |
| **Floor fill tile** | 1024 x 1024 | 32u thick | interior of the paved core. A 3072 core needs 9 of these |
| **Floor edge tile** | 512 x 512 | 32u thick | the outer ring, at half the fill size so the outline can step in 512u increments and read as irregular |
| **Retaining edge** | 512 wide x 128 deep | 256u tall | rough stone face hanging below the floor plane. Max measured exposure is 160u, so one height covers every case and the surplus buries in terrain |
| **Retaining corner** | 128 x 128 | 256u tall | outer and inner corner variants to turn the stepped outline |
| **Ramp tile** | 512 x 512 | rises 96u | 1:5.3 grade, chainable. The deployed 192u terrace uses 4 chained tiles over 2,048u, falling 384u to meet the ground |
| **Shoulder wedge** | 512 x 256 | tapers 32u to 0u | rough-earth/grass transition strip laid outside the paving to soften the join |

Pivot convention, chosen so the generator needs no offset arithmetic:

- **floor and ramp tiles:** pivot at the **centre of the top face**, so placing at `Z = -5672` puts the walking surface exactly on the platform floor
- **retaining pieces:** pivot at the **top outer edge**, so the piece hangs down from the floor plane and any excess buries
- **shoulder wedge:** pivot at the **thick end, top face**

Collision, following Bethesda's stated best practices (avoid concave collision, prefer primitive boxes):

- floor tiles: a **single box** collider matching the tile, not a mesh copy
- ramp tiles: one angled box, or two stepped boxes if an angled primitive proves awkward
- retaining and corner pieces: box child colliders approximating the face. Visual rock detail needs no collision fidelity because nothing walks on the face
- shoulder wedge: **no collision** — it sits on native ground and would only create snag geometry

No LAND edits at any point: the floor sits above native terrain everywhere, and the retaining pieces plus vanilla rocks cover the gap.

The kit is deliberately small — six pieces — and project-owned, which is tier 2 in the `docs/DESIGN.md` asset preference rather than a third-party dependency.

## Phase 3 — foundation tile kit authored

All six pieces are built, collisioned and exported. **The build is reproducible from a committed script**, matching the project's code-first approach — the script is the source of truth and the `.blend` and `.fbx` are build outputs.

```powershell
& "C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe" --background --python assets\blender\build_foundation_kit.py
```

### Project-owned assets created

| Path | Size | What |
| --- | --- | --- |
| `assets/blender/build_foundation_kit.py` | 10,677 B | build script — **source of truth** |
| `assets/blender/fair_foundation_kit.blend` | 837,612 B | Blender scene, build output |
| `assets/fbx/SkyrimFair/SkyrimFair_FloorFill_1024.fbx` | 11,836 B | BSFBX export |
| `assets/fbx/SkyrimFair/SkyrimFair_FloorEdge_512.fbx` | 11,836 B | BSFBX export |
| `assets/fbx/SkyrimFair/SkyrimFair_Retain_512.fbx` | 11,820 B | BSFBX export |
| `assets/fbx/SkyrimFair/SkyrimFair_RetainCorner_128.fbx` | 11,836 B | BSFBX export |
| `assets/fbx/SkyrimFair/SkyrimFair_Ramp_512.fbx` | 13,692 B | BSFBX export, includes child collider |
| `assets/fbx/SkyrimFair/SkyrimFair_Shoulder_512.fbx` | 11,692 B | BSFBX export |
| `assets/README.md` | — | pipeline, kit spec, manual conversion step |

Nothing third-party is copied or referenced. All geometry is original box and wedge primitives authored for this project.

### Verified geometry, as built

| Piece | X | Y | Z | Verts | Origin |
| --- | --- | --- | --- | --- | --- |
| `SkyrimFair_FloorFill_1024` | −512..512 | −512..512 | −32..0 | 8 | (0,0,0) |
| `SkyrimFair_FloorEdge_512` | −256..256 | −256..256 | −32..0 | 8 | (0,0,0) |
| `SkyrimFair_Retain_512` | −256..256 | −128..0 | −256..0 | 8 | (0,0,0) |
| `SkyrimFair_RetainCorner_128` | −128..0 | −128..0 | −256..0 | 8 | (0,0,0) |
| `SkyrimFair_Ramp_512` | −256..256 | 0..512 | −256..0 | 8 | (0,0,0) |
| `SkyrimFair_Shoulder_512` | −256..256 | 0..256 | −32..0 | 6 | (0,0,0) |

Pivot conventions as specified in Phase 2 and implemented exactly: floor and ramp pivots at the centre of the top face, retaining pieces at the top outer edge, corner at the top outer corner, shoulder at the thick end top face. For retaining, ramp and shoulder pieces **+Y points away from the platform centre**.

### Collision, as built

| Piece | Rigidbody | Collider |
| --- | --- | --- |
| `FloorFill_1024` | unyielding, mass 0 | self, Box |
| `FloorEdge_512` | unyielding, mass 0 | self, Box |
| `Retain_512` | unyielding, mass 0 | self, Box |
| `RetainCorner_128` | unyielding, mass 0 | self, Box |
| `Ramp_512` | unyielding, mass 0 | **child box rotated 10.62 deg** (`SkyrimFair_Ramp_512_Collider`) |
| `Shoulder_512` | unyielding, mass 0 | **none, intentional** |

Two corrections were needed against the BGS defaults, both worth knowing for future assets:

1. **The default rigidbody is a movable prop** — mass 80, `unyielding` off. For static world geometry that is wrong, so every piece is now set `unyielding = True, mass = 0`.
2. **A bounding-box collider on the ramp would be a solid 512 x 512 x 256 block** and would stop the player walking up the slope. The ramp instead uses a separate box child collider rotated 10.62 degrees (`atan(96/512)`) to lie along the slope — the "Adding Collision using Child Collider Meshes" method from Bethesda's guide. Confirmed present in the exported FBX.

### FBX to NIF conversion — DONE, and verified

AssetWatcher is a Qt GUI app with no CLI, so Barry configured the watch project by hand:

| Field | Value |
| --- | --- |
| Source folder | `E:\html\skyrim-fair\skyrim-fairssetsbx` |
| Output Folder | `E:\html\skyrim-fair\skyrim-fairssets
if` |

AssetWatcher mirrors the Source subfolder structure into Output, so the FBX living in `assetsbx\SkyrimFair\` produce `assets
if\SkyrimFair\*.nif`, giving plugin mesh paths of `meshes\SkyrimFair\<name>.nif`. It converts on file change, so re-running the build script triggers it.

**All six NIFs exist and were verified by parsing them directly:**

- valid SSE NIFs — `Gamebryo File Format, Version 20.2.0.7`, userVersion 12, bsVersion 100
- **visual mesh scale exact** — every bounding sphere matches its expected radius, ratio 1.000
- **collision half-extents exact** — 512x512x32, 1024x1024x32, 512x128x256, 128x128x256 as specified
- five pieces carry `bhkCollisionObject`, `bhkRigidBodyT`, `bhkBoxShape`, `bhkConvexTransformShape`
- the **ramp's rotated collider survived conversion** — transform reads cos 0.983 / sin 0.184 (10.62 deg), box 512 x 520.9 x 64, the 520.9 being the slope length `hypot(512, 96)`
- the **shoulder has no collision blocks**, as intended
- all six carry `BSLightingShaderProperty` and `BSShaderTextureSet`, so a texture slot is ready

### Scale — a real trap, found and fixed

**1 Blender unit converts to exactly 40 Skyrim units.**

The first converted build was **exactly 40x too large** on every piece, on both visual mesh and Havok collision. This was caught by parsing the NIFs, not by eye — the FBX and the Blender scene both looked correct.

Blender's scene unit settings do **not** affect it. Building with `system="NONE"` and with Bethesda's own recommended `IMPERIAL` / `INCHES` / `scale_length=1` (read from `BGS_SKYRIM_OT_set_recommended_unit_scale` in `bgs_skyrim_tools/operators/export_ops.py`, since that operator needs UI context and cannot be invoked headlessly) produced identical oversized output. The factor lives in the FBX to NIF conversion itself.

The build script now writes every dimension in readable Skyrim units and divides by `BLENDER_UNITS_PER_SKYRIM_UNIT` (1/40) at mesh-construction time. **Any future project-owned mesh must use the same constant.**

### Still unverified
- **Collider `type` / `layer` / `material` enums.** Populated by a UI callback, so they cannot be enumerated in headless Blender. Left at BGS defaults (`type='Box'`, `layer='1'`) and should be reviewed in the Blender UI.
- **Material and texture.** No texture assigned. Geometry proof first, per the brief. Cobblestone character still needs a material pass — and note that vanilla Skyrim has no generic cobblestone paving texture path confirmed by this audit either.
- **In-game appearance.** Nothing has been placed or seen in game yet; the kit is untextured, so it will render with a default material until a texture pass happens.

## Phase 5 — navmesh assessment

Documented only. **No navmesh was authored, and none should be until Barry approves the visual foundation.**

- **Cells the paved core spans:** `-2,-4` (`00009A28`) and `-2,-3` (`00009A07`). Both have vanilla navmesh with **no mod navmesh edits in the active load order** — the cleanest possible starting point.
- **Where existing navmesh approaches:** vanilla exterior navmesh covers all of this tundra continuously. The relevant question is not coverage but elevation — the platform surface will sit 0–160u above it.
- **Where new navmesh is needed:** over the paved core surface in both cells, plus the ramp.
- **Likely join points:**
  - **East edge, around `(-4096, -13440)`** — 0u exposure, the paving meets native grade. This is the cheapest and most reliable join and should be the primary one.
  - **West / south-west ramp foot** — the intended main entrance. Join at the bottom of the ramp chain, roughly 1,536u out from the paving edge.
  - Two joins are enough. Every additional access point is another join to maintain.
- **Should geometry change before navmesh work?** Yes. Fix the number and position of access points first, because each one is a navmesh join. Decide the final outline and ramp placement before any navmesh is cut.
- **Capability:** this project's Mutagen pipeline cannot practically generate navmesh. It is Creation Kit work, and it breaks the `docs/ROADMAP.md` principle of deferring custom navmesh — that principle now has to give. Affects vendors, ambient crowds, Garrick Tallow, Claudius Vale and the stage performers.
- **Interim option:** place the foundation visual-only and accept that NPCs cannot walk on it, purely to judge the look before committing to navmesh.

## Terrain audit methodology

An earlier audit reported the original prototype cell `2,-2` as "24 units relief" and called it flat. That was measured over a 512 x 512 footprint — far too small for a fairground:

| Footprint | Relief | Max step per 128u |
| --- | --- | --- |
| 512 x 512 | 24u | 16u |
| 1024 x 1024 | 112u | 64u |
| 2048 x 2048 | 240u | 88u |
| 3072 x 3072 | 400u | 88u |
| 4096 x 4096 | **472u** | 88u |

Relief grows roughly 20x from the 512 sample to fair scale. That was the hilliness Barry saw in game.

**All future terrain audits must sample at the real footprint size** (2048 minimum for the market core, 3072–4096 for the whole site) and report maximum local step per 128-unit interval alongside total relief.

Method: heightmaps decoded from `LAND` `VHGT` across a 17 x 15 cell box (x -8..8, y -12..2), 255 cells, all present. **The winning `LAND` record per cell was resolved through the full active load order**, not vanilla alone. Only 18 of 255 cells have mod-altered heights: SLaWF (12), SLaWF Majestic Mountains patch (1), SLaWF Tundra Homestead patch (2), `Embers XD.esp` (1), `Helgen Reborn.esp` (2). `MajesticMountains_Landscape.esm` supplies `LAND` for 89 region cells but changes no heights.

Why natural terrain was rejected: the flattest *clean* 3072 site anywhere near the Western Watchtower is 160u relief with a 48u maximum step per 128 units, roughly a 21-degree local slope at worst. The design generates aligned rows of stalls from config, which needs a level floor.

## Vanilla statics of interest

**Vanilla Skyrim has no generic cobblestone paving tile.** Whiterun's streets and plaza are baked into its `WRTerrain` architecture meshes. A search of all `STAT` records for paving, plaza, street, cobble, courtyard and floor patterns returned 119 records, none a plain repeatable exterior paving tile. This is the main argument for a project-owned kit.

Closest vanilla families, kept for reference and for edge dressing:

| FormKey | EditorID | Use |
| --- | --- | --- |
| `001044CB:Skyrim.esm` | `NorTmpExtPlatFloorRaised01CutStone` | Nordic exterior cut-stone raised floor |
| `00028A62:Skyrim.esm` | `NorTmpExtPlatFloorRaised01` | as above, plain |
| `00026F7B:Skyrim.esm` | `NorTmpExtPlatCorOut01` | outer corner |
| `00026F79:Skyrim.esm` | `NorTmpExtPlatCorIn01` | inner corner |
| `0002BE41:Skyrim.esm` | `NorTmpExtPlatExSmFree01` | small free-standing platform |
| `000506DF:Skyrim.esm` | `WRCarlottaPlatform01` | Whiterun building-levelling plinth |
| `000510E0:Skyrim.esm` | `WRStairsPlatform01` | Whiterun stepped plinth |
| `0000099B:Skyrim.esm` | `Stonewall01` | farmhouse dry-stone wall — retaining |
| `0000099D:Skyrim.esm` | `Stonewall02` | as above, variant |
| `0003F93B:Skyrim.esm` | `ImpExtStairs01` | exterior stone steps |
| `000F03D3:Skyrim.esm` | `RTTemplePlazaStairs01` | wide plaza stairs |

**Unverified:** exact mesh dimensions and pivots. NIF geometry cannot be read with the tooling used in this audit, so tile sizes and seam behaviour are **unknown** and must be checked in NifSkope or the Creation Kit before relying on any of them.

## Map marker implementation notes

- Base object: `MapMarker`, `00000010:Skyrim.esm` (a `STAT`)
- Markers live in Tamriel's persistent cell `00000D74:Skyrim.esm`, not the grid cell they sit over
- `XMRK` presence is what makes a REFR a map marker
- **The record must carry the `Persistent` flag `0x400`.** All 347 vanilla Tamriel markers have it. Mutagen's `SkyrimMajorRecordFlag` enum does not expose it and does not derive it from `Cell.Persistent` membership, so the generator sets `MajorRecordFlagsRaw` explicitly. **This was the fast-travel bug and it is now fixed and confirmed in game.**
- `FULL`: display name. `TNAM`: icon type. `FNAM`: flags. `DATA`: position + rotation. `XRDS`: radius. `XLRT`: location ref type
- `FNAM` bit `0x01` = Visible, bit `0x02` = Can Travel To. Vanilla: 332 markers at `0x00`, 4 at `0x01`, 11 at `0x03`
- `XLRT` should be `MapMarkerRefType` = `0010F63C:Skyrim.esm` (333 of 347 vanilla markers)
- `XRDS` in use: 1800, matching `WhiterunWatchtowerMapMarker`, the nearest vanilla marker to the site
- A linked location (`XLCN`) is **not** required — the Whiterun Stables and Western Watchtower markers both have none
- Current icon: `0x18` **Pass** (Mutagen `MarkerType.Pass`), confirmed correct in game
- Other useful icons: `0x02` town/village (`MarkerType.Town`), `0x0D` farm (incl. `MerryFairMapMarker`), `0x05` camp, `0x1E` shack, `0x15` stable
- Mutagen's `MarkerType.Settlement` is `0x03`, which vanilla uses for Honningbrew Meadery and Goldenglow Estate. The name is misleading.

## Skyrim install

- Mod manager: Mod Organizer 2 v2.5.2, portable instance at `E:\Modlists\Still In Skyrim`
- Game the mod list runs: `E:\Modlists\Still In Skyrim\stock` (MO2 stock-game copy), Data at `...\stock\Data`
- Runtime: 1.6.1170.0 (Skyrim SE/AE, Steam)
- Active profile: `Still in Skyrim Plus`, 426 active plugins
- SKSE 2.2.6 (matches runtime), Address Library 11.0.0
- Pandora Behaviour Engine+ is the active behaviour generator (output mod v4.3.0); Open Animation Replacer 3.1.6.0; no FNIS or Nemesis installed
- A **separate Steam install** exists at `E:\SteamLibrary\steamapps\common\Skyrim Special Edition`, which is where the Creation Kit lives. Its `Skyrim.esm` is 249,752,131 bytes versus 249,753,412 in the stock copy — **they are not identical**. The generator deliberately reads the **stock** copy, which is what the game actually loads. Anyone opening the Creation Kit should be aware it points at the Steam install by default.
- A Skyrim **Legendary Edition** Creation Kit v1.9.36 also exists at `C:\Program Files (x86)\Steam\steamapps\common\skyrim\CreationKit.exe`. Wrong edition — not usable for SSE work.

## Asset packs

All three are archives in `external/` (git-ignored) and **none is installed in MO2**.

### Medieval Markets

- Nexus 161479; archive filename says 1.1.1, bundled `meta.ini` says 1.1.0.0
- `Medieval Markets.esp`, ESL-flagged
- Edits **zero** Tamriel exterior cells (city worldspaces only) and does **not** override `SMarketStall01`
- Permissions: JJerem's own assets reusable with credit, no selling; third-party assets follow their original permissions
- JJerem-authored per his own description: market stall tent, stick shelving, slanted table stand, basket powder mound
- Third-party credited: PraedythXVI (Fruits and Veggies), Brumbek (SMIM), gooball60 (Palpable Baskets), JPSteel2 (Northern Roads tent + textures)
- Preferred strategy: dependency-only referencing, no redistribution

### Incaendo's Banner Resource 2

- Nexus 94919, version 1.0; author inferred from paths, not documentation
- Pure resource: no plugin, no readme, no licence file
- 163 mesh/texture pairs under `Data\Meshes\Incaendo\Banners\` and `Data\Textures\Incaendo\Banners\`
- All textures taller than wide: 146 at 1024x2048, 17 at 256x2048. **No bunting.**
- Redistribution terms unconfirmed; dependency-only referencing preferred

### Professional Dancer

- Version 1.5.0, Nexus 124608
- `Dance.esp`, ESL-flagged; masters `Skyrim.esm`, `SkyUI_SE.esp`, `UIExtensions.esp` (all already active)
- `DanceQuest` = `000802:Dance.esp`; `DancePower` = `000800:Dance.esp`
- Callable API on `DanceQuestScript`: `StartDanceOnTarget`, `StopTargetDance`, `StopAllTargetDances`, `PlayAnimationOnActor`, `IsNPCDancing`
- Mod-event channel: `PlayCustomDance_<actorFormID>` / `StopCustomDance_<actorFormID>`
- Lowest-level trigger: `Debug.SendAnimationEvent(actor, "Dance1")` through `"Dance14"`; stop with `"IdleForceDefaultState"`
- 15 `.hkx` registered via FNIS-format list; Pandora consumes it, so no FNIS/Nemesis install needed

## Current blockers / cautions

- **Blocked on the FBX to NIF conversion.** AssetWatcher is GUI-only with no CLI, so Barry must run it against `assets\fbx\`. Phase 4 placement cannot start until NIFs exist — see Phase 3 above.
- Asset **scale, collider enums and materials are unverified**; all need a look in the Blender UI or Creation Kit.
- **Navmesh is on the critical path** for the platform milestone and cannot be generated by this pipeline. Creation Kit work, and it should follow Barry approving the visual foundation.
- Vanilla mesh dimensions and pivots are unverified; no vanilla generic paving tile exists.
- The stall's Z is native terrain height; `SMarketStall01`'s mesh origin was never read, so a small vertical offset may still be wanted.
- The generator copies the WRLD/CELL records it overrides from `Skyrim.esm`, not from the winning record in the active load order. Correct load-order placement (now in effect) makes this harmless, but building from the load order would be more robust. Not implemented.
- Permanent exterior placements will eventually require DynDOLOD/Occlusion regeneration.
- `external/` holds large third-party mod archives and must never be committed.

## Next local verification

The foundation has moved to the player's position and the vanilla clutter under it is
disabled. Barry's in-game look is the gate.

What to check:

1. **Are the rocks gone?** No boulders or shrubs poking through the paved surface. 56
   references are disabled; if anything still pokes through, tell me what it looks like
   and I will widen `clearMargin` or add its base type to the clearable set.
2. **Is the centre flat?** The market stall stands at the centre on the floor plane.
3. **The north ramp.** Four chained tiles dropping 256u, on the Whiterun approach. Fast
   travel now arrives at its foot. Walk up it.
4. **The west side.** This is the tall one — up to 392u faced in two stacked courses.
   Does it read as a rocky terrace or as a wall? This is the main open question.
5. **Does the outline still read as organic** rather than a rectangle?
6. **Rotation check.** North and south retaining faces should look right regardless. If
   east and west are wrong, the Z-rotation sign is inverted — a one-constant fix.

Still outstanding regardless of the result: the kit is **untextured**, and there is **no
navmesh**, so NPCs cannot use the platform. Both are their own milestones.
