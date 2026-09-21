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

## Deployed build — naturalised terrace at +192

The position, elevation and footprint are the **approved base site** and are unchanged
by this pass. What changed is the treatment of the edges and the ramp.

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | **67,019 bytes** |
| sha256 | `53d76a11875ebe5d30ca857dc4b54cd0fb8204f24e600b3e8109d454821f9388` |
| Masters | `Skyrim.esm` only |
| Records | 1 WRLD, 6 CELL, 6 STAT, 247 REFR |
| Centre | `X -5888, Y -12928` (unchanged) |
| Floor Z | `-5504`, +192 above grade (unchanged) |
| Cells touched | `-3,-4`, `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` (unchanged) |
| Deployed | byte-identical; all 6 NIFs redeployed |

### The ramp-to-floor connection — what was actually wrong

The seam itself measured **exact**, and it is worth recording how that was established
rather than assumed. The paving and the ramp were rebuilt as solids in world space from
the kit definitions and the placements parsed out of the ESP, then the top surface was
sampled every 64 units along the ramp centreline:

```
      Y   carried by                              top Z     step
 -12224   FloorEdge512, FloorFill1024            -5504.0    +0.0
 -12160   FloorEdge512, FloorFill1024, Ramp512   -5504.0    +0.0
 -12096   Ramp512                                -5516.0   -12.0
 -12032   Ramp512                                -5528.0   -12.0
    ...                                             ...
 -10112   Ramp512                                -5888.0   -12.0
```

Continuous, no step and no gap: the paving's north edge and the ramp's high end are both
at `-5504` at `Y -12160`, and the fall is a constant 12 units per 64 from there to the
foot. **Collision matches the visual surface exactly, by construction** rather than by
luck: the ramp's collider is a box rotated by `atan(96/512)` about its own origin, and
that origin is placed at `(RUN/2, -RISE/2)` — the midpoint of the line from `(0, 0)` to
`(512, -96)`. A plane through the midpoint at the slope's own gradient is the slope. The
converted NIF confirms it survived: `bhkBoxShape` 512 x 520.9 x 64, the 520.9 being
`hypot(512, 96)`, with the rotation stored as 0.983 / 0.184.

Three things at the connection *were* wrong, and all three are now fixed:

1. **Daylight under the ramp head.** The ramp slab was 256 deep and its head stands 288
   above native ground on the western flank, so the underside sat **32 units clear of
   the ground** and you could see under the entrance. `RAMP_DEPTH` is now **384**, which
   buries it along the whole run with margin. This is the one real visual fault at the
   junction and it needed a mesh change, not a placement change.
2. **Bare flanks.** The ramp was a grey slab hanging in the air beside the terrace, with
   nothing at its sides. Both flanks are now treated as edge segments (below).
3. **Bare cutting wall.** The paved spur at footprint cell `(3,0)` sits immediately west
   of the ramp head, so the first thing you see on the way in was its grey retaining
   face. It is now faced with rock, via the slim-piece retry described below.

No tuck or lead-in was added at the seam. The two faces there are coplanar with opposed
normals and zero separation, which is the ordinary case for butted kit pieces and is
back-face culled; inventing a new mesh topology to insure against a float error that
does not exist would have risked bad normals for no gain.

### Exposure, measured before designing anything

Every perimeter segment was sampled at its face and 192 and 384 units further out:

| | segments | drop at face |
| --- | --- | --- |
| North | 4 | 192-280, mean 232 |
| South | 6 | 192-280, mean 232 |
| East | 5 | 200-272, mean 221 |
| West | 5 | 216-288, mean 246 |
| **All non-ramp** | **20** | **192-288, mean 233** |

**Not one segment is under 192.** That is the finding that shaped this pass, and it
contradicts the brief in one respect: there is no perimeter edge with a "smaller height
difference" to give a soft earth transition to. The whole outline is wall. The old
`shoulderMaxDrop` rule was gating the verge wedge on floor-to-ground drop at the face,
which is why it produced zero wedges — it was working correctly against a condition that
no longer occurs anywhere on the site.

### Banded edge treatment

The perimeter treatment is now chosen per segment from its measured exposure.

| Band | Condition | Treatment |
| --- | --- | --- |
| Embankment | exposure > 140 | 3 tall rocks, each scaled to the exposure it faces, crown solved onto the floor plane |
| Toe | always | 2 low rock piles bedded into native grade beyond the embankment |
| Verge | local ground step <= 112 | the project-owned rough-earth wedge, then 2 shrubs or scrub |

**The palette changed, and that is the main reason the terrace read as a grey box.** The
old dressing pool was `RockPileM01/M02/S01/S02`, which are **54 to 102 units tall**. They
were being asked to hide a **192-288 unit** wall, so they sat round its foot like gravel.
They are kept, but demoted to the toe band, where a low pile is the right piece.

The embankment pool is new, and every piece in it is a static **vanilla itself places
within 6,000 units of this site**, so the result reads as the same landform family as the
surrounding tundra:

| FormID | EditorID | Radius | Height | Nearby in vanilla |
| --- | --- | --- | --- | --- |
| `00018199` | `RockL01` | 270 | 288 | 9 |
| `0001819A` | `RockL02` | 399 | 263 | - |
| `00018BA5` | `RockL03` | 289 | 180 | 4 |
| `0001A6E2` | `RockL04` | 178 | 418 | - |
| `0001B0A8` | `RockL05` | 215 | 276 | - |
| `000332C7` | `RockPileL01TundraRocks` | 512 | 372 | 2 |

Three rules make the embankment sit correctly, and each of them was added because the
first attempt got it wrong and the verifier caught it:

- **Solve Z from the piece's own bounds.** A vanilla rock's origin sits near its base,
  not its centre — `RockL01` runs from `-59` to `+229` — so the reference Z is derived
  from `OBND.ZMax` to put the crown on the floor plane. Assuming a fixed offset is what
  leaves dressing either floating or sunk out of sight.
- **Bed against the ground under the rock, not at the face.** The ground falls away from
  the platform, so a rock standing a couple of hundred units out sits on ground well
  below the face it is facing. The first build had two rocks hanging 6 and 44 units in
  the air for exactly this reason. Each rock now re-samples the terrain at its own
  position and is rescaled to that.
- **Only draw pieces that can reach the top.** A 180-unit rock asked to face a 288-unit
  edge can only crown it by lifting its base off the ground, so the draw is filtered to
  pieces whose height at maximum scale clears the exposure. Short pieces are not wasted:
  they are what the lower ramp flanks want.

Overshoot above the floor plane is capped at 48 units **and** by the piece's own spare
height, so crowning the edge can never lift a base clear of grade. Measured over the
finished build, embankment crowns land **0 to 48 units above the floor plane** — which is
the silhouette break: seen from on the terrace, rock crowns interrupt the paving edge
instead of it ending in a drawn line.

### The ramp flanks, and where the earth transition actually belongs

Both flanks of all four ramp tiles are treated as edge segments against the exposure
measured at that tile. This is where the small height differences live:

| | ramp surface above native ground |
| --- | --- |
| At the head | 224-288 |
| Mid run | 96-176 |
| Near the foot | 24-64 |

So the flanks pick up rock high up and give way to rough earth and grass low down,
which is the gradient the brief asked for — it just is not on the perimeter.

Two guards stop this from going wrong:

- **A clear walking channel**, 480 units wide down the middle of the ramp and continued
  512 units past the foot toward the road. Nothing is placed whose mesh radius reaches
  into it. 41 picks were rejected on this rule.
- **No treatment where the flank's outward side is the terrace itself.** The ramp leaves
  the platform, so its first tile has paving alongside it; treating that side pushed rock
  and scrub outward onto the terrace. The first build put two pieces on the platform this
  way.

Because a fat rock beside the entrance is always rejected by the channel rule, a
rejection **retries once with the slimmest piece that can still reach the top**.
`RockL04` is 418 tall on a 178 radius, so it faces the cutting wall from close in where a
399-radius `RockL02` cannot. That single retry is why `RockL04` is now the most-used
embankment piece, with 16 placements.

### Corner stones

Every convex corner of the outline gets one larger stone set diagonally across it, from
`RockL02` / `RockPileL01TundraRocks` / `RockL04`, sized to the corner's own exposure.
Eight placed. A flat top edge reads as a built rectangle most obviously at its corners.

### What is on the site now

| Role | Count |
| --- | --- |
| Paving (`floorFill` 1024 / `floorEdge` 512) | 4 / 6 |
| Ramp tiles | 8 |
| Grey retaining courses | 24 |
| Retaining corners | 2 |
| **Rough-earth verge wedges** | **24** (previously 0) |
| Embankment rocks | 63 |
| Corner stones | 8 |
| Toe rocks | 43 |
| Shrubs and scrub | 58 |
| Vanilla references placed, total | 172 |
| Rejected as oversized | 10 |
| Rejected for blocking the entrance channel | 41 |

The shoulder `STAT` is back in use, so there are **6** STAT records rather than 5.

### Is there still a fairground to build on?

Yes. Sampling the paved surface on a 32-unit grid against every piece that crowns at or
above the floor plane, using OBND radius as the reach (the horizontal half-diagonal, so
deliberately pessimistic):

| | |
| --- | --- |
| Paved area | 5.77 million sq units |
| Clear of rock | **3.71 million sq units, 64.3%** |
| Largest clear square | **1,216 x 1,216 units** |

The clear area is one contiguous core with a rocky rim, which is the shape wanted. The
biggest intrusions are `RockL02` at radius 399 leaning on the concave parts of the
outline; if the interior wants to be cleaner, `wallMaxRadius` down or `wallEdgeOverlap`
up will pull the rim back without touching anything else.

### Verification

All checks pass, by parsing the written ESP independently of Mutagen and rebuilding every
piece in world space from the kit definitions and Skyrim.esm OBND data:

- single master, author intact, no LAND / NAVM / NPC / quest / script records
- ramp chain untouched: 8 tiles, four Z levels 96 apart, rotation 0, foot at `-5888`
- **no embankment rock floats above native ground** (0 of 71)
- **every embankment rock crown reaches the surface it faces** (0 short)
- no crown overshoots the configured 48
- **nothing reaches into the entrance walking channel** (0 blockers)
- nothing centred inside the paving inset by `wallEdgeOverlap`
- all 24 verge wedges sit exactly on native ground and below the floor plane
- all dressing scales within 0.70-1.50
- exactly 5 references disabled, and exactly the five named
- road pieces, fences, handcart and `critterSpawnInsects_Many` untouched

**All six overridden CELL records are byte-identical to vanilla.** `modlist.txt`,
`plugins.txt` and `loadorder.txt` were not written by the generator or the deploy.

#### One accepted deviation, now measured properly

The overridden `WRLD` Tamriel record differs from vanilla in exactly two subrecords, and
nothing else:

| Subrecord | Vanilla | Ours | Why |
| --- | --- | --- | --- |
| `RNAM` | 1,349,616 bytes | absent | The region-cell cache, dropped by design. Every real mod that touches Tamriel drops it and the game rebuilds it. |
| `FULL` | 4 bytes | 7 bytes | Vanilla stores the worldspace name as a localised string ID; this plugin is not flagged localised, so the literal `Skyrim` is written instead. Standard for a non-localised override, and identical text in English, but it would force English for a localised install. |

All 16 other subrecords, including `OFST` at 45,600 bytes, match byte for byte.

### Known and deliberately not fixed

- **The ramp foot's eastern corner runs into a bank.** Across the ramp's 1,024 width the
  western half lands clean, but native ground rises on the eastern side over the last
  128 units, covering the ramp by up to 96 there. The terrain slope at that corner is
  about 29 degrees, which is walkable, so it reads as the ramp emerging from a bank
  rather than as a wall. Fixing it properly would mean either a LAND edit or moving the
  ramp, and the placement is approved. Flagged for review rather than changed.
- **No textures.** Expect grey geometry on the project-owned pieces. The vanilla rock,
  earth and plant dressing is fully textured, so the contrast will be stark.
- **No navmesh**, so NPCs cannot use the terrace or the ramp yet.
- **Grass still grows through the paving** until the No Grass In Objects cache is
  regenerated. Barry's step; nothing in the plugin can change it.

### What to test in game

1. does the terrace now read as a landform rather than a built platform
2. is the foot of the ramp still clean now the slab is 384 deep, with no gap underneath
3. does the entrance read as a cutting through rock, and is the way down clearly readable
4. do the ramp flanks feel integrated, rock at the top giving way to earth at the bottom
5. do the rock crowns break the top edge without making the terrace feel cluttered
6. is the clear core big enough to lay out stalls and a stage

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
| **Ramp tile** | 512 x 512 x 384 deep | rises 96u | 1:5.3 grade, chainable. The deployed 192u terrace uses 4 chained tiles over 2,048u, falling 384u to meet the ground |
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
| `SkyrimFair_Ramp_512` | −256..256 | 0..512 | **−384..0** | 8 | (0,0,0) |
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
2. **A bounding-box collider on the ramp would be a solid 512 x 512 x 384 block** and would stop the player walking up the slope. The ramp instead uses a separate box child collider rotated 10.62 degrees (`atan(96/512)`) to lie along the slope — the "Adding Collision using Child Collider Meshes" method from Bethesda's guide. Confirmed present in the exported FBX.

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
