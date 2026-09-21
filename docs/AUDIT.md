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

## Current build — nothing disabled, audit first

Per Barry's instruction, **auto-disabling of vanilla clutter is switched off**
(`site.foundation.clearClutter = false`) and the deployed plugin contains no disabled
overrides. The footprint blueprint and intersection audit below are for review first.

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | 55,123 bytes, sha256 `30e827ab840d6d187bc49b7752a813e18828536b5aa63cef060592480cf0eff6` |
| Masters | `Skyrim.esm` only |
| Records | 1 WRLD, 4 CELL, 6 STAT, 96 REFR (0 Initially Disabled) |
| Site centre | `X -5376, Y -18304` (the player's save position, snapped to the 128-unit grid) |
| Floor Z | -5568 |
| Cells touched | `-2,-5`, `-1,-5`, `-2,-4` |

### Why the rocks were swallowing the platform — my bug

The screenshot showed the platform buried in boulders. The cause was **my own dressing**:
`RockTundraLand01Tundra01` and `RockTundraLand02Tundra01` were in the edge-rock pool, and
those are not clutter, they are landscape slabs with mesh radii of **1418 and 1767 units**.
Placed ~190 units beyond the paving edge, a single one covers the entire footprint.

Fixed three ways:

1. both landscape slabs removed from the dressing pool, leaving only small and medium
   rock piles (radii 177–404)
2. every dressing piece is now offset by **its own mesh radius**, less a deliberate 64-unit
   edge overlap, so a rock breaks the silhouette instead of engulfing the platform
3. a hard `dressing.maxRadius` of 500 rejects any oversized pick outright, so this cannot
   recur

### Two findings that matter more than the rocks

**1. A stream runs straight through the footprint.** The audit found
`TundraStreamStraight01Tundra01` and `TundraStreamBend01Tundra01` overlapping the
foundation, plus `TundraStreamTransition01`, `FXRapids`, `FXRapids02` and
`FXrapidsFallsLine01` — water meshes and rapids effects. The terrain textures confirm it:
`LRiverMud01` is painted in cells `-2,-5` and `-1,-5`. The platform is currently laid over
a watercourse.

**2. The rocky look is the terrain itself, not objects.** `LTundraRocks01` is painted
across all three cells, and **no vanilla rock or boulder reference intersects the footprint
at all**. The rounded slabs in the screenshot are the sculpted LAND heightmap with a rock
texture — which is also why this site measures 392u of relief against the audited Site 1's
120u. **Disabling references cannot flatten this site.** Only a taller platform, a smaller
footprint, moving, or LAND edits would change it.

## Foundation footprint blueprint

Derived from the **actual placements in `dist/SkyrimFair.esp`**, not from config: the
plugin was parsed, the paving references located, each tile's extent computed from its
kit dimensions, and the unshared tile edges traced into a closed outline.

- paving: **22 tiles** on a common 512-unit grid (4 x 1024 fill + 6 x 512 edge)
- bounding extent: **X -6912 .. -3840**, **Y -19584 .. -17024** (3072 x 2560)
- floor plane: **Z -5568**
- the outline is **not rectangular** - it has 14 corners

### Perimeter, closed polygon (world units, in order)

| # | X | Y | | # | X | Y |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | -6912 | -18560 | | 8 | -3840 | -19072 |
| 2 | -6400 | -18560 | | 9 | -3840 | -17536 |
| 3 | -6400 | -19072 | | 10 | -4864 | -17536 |
| 4 | -5888 | -19072 | | 11 | -4864 | -17024 |
| 5 | -5888 | -19584 | | 12 | -6400 | -17024 |
| 6 | -4352 | -19584 | | 13 | -6400 | -18048 |
| 7 | -4352 | -19072 | | 14 | -6912 | -18048 |

### Paved tile centres (512-unit grid)

```text
Y  -17280:    -6144    -5632    -5120
Y  -17792:    -6144    -5632    -5120    -4608    -4096
Y  -18304:    -6656    -6144    -5632    -5120    -4608    -4096
Y  -18816:    -6144    -5632    -5120    -4608    -4096
Y  -19328:    -5632    -5120    -4608
```

## Footprint intersection audit

Every placed reference whose **world-space geometry** intersects the footprint or a
512-unit landscaping margin around it. Bounds come from each base object's `OBND`
record, scaled and rotated into world space, so large objects whose **origin lies
outside** the footprint are still caught. Winning records only, resolved through the
full load order including the implicit masters.

**101 intersecting references** - 64 overlap the foundation itself, 37 only the margin. Our own pieces are excluded.

| Classification | Count | On foundation |
| --- | --- | --- |
| vegetation | 86 | 54 |
| decorative clutter | 11 | 8 |
| large environment piece | 3 | 2 |
| UNSAFE - do not touch | 1 | 0 |

**Nothing has been disabled.** `site.foundation.clearClutter` is `false` in
`fair.config.json` and the deployed plugin contains no disabled overrides.

### Every intersecting reference

| FormID | Base | Type | World X, Y, Z | Scale | Radius | AABB (X / Y) | Overlaps | Class | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `0CB05E:Skyrim.esm` | `0C5209:Skyrim.esm` critterSpawnInsects_Many | ACTI | -6506, -16886, -5748 | 1.0 | 91 | -6570..-6442 / -16950..-16822 | margin | UNSAFE - do not touch | enable-parented, has script, base has script |
| `016089:Skyrim.esm` | `01542D:Skyrim.esm` TundraStreamBend01Tundra01 | STAT | -6801, -17246, -5821 | 1.0 | 1823 | -8385..-5412 / -18557..-15963 | **foundation** | large environment piece |  |
| `01608B:Skyrim.esm` | `01542D:Skyrim.esm` TundraStreamBend01Tundra01 | STAT | -2264, -19436, -5569 | 1.0 | 1823 | -3826..-505 / -20958..-17924 | margin | large environment piece |  |
| `01608C:Skyrim.esm` | `01608F:Skyrim.esm` TundraStreamStraight01Tundra01 | STAT | -4513, -18411, -5688 | 1.0 | 1566 | -5990..-2860 / -19982..-16933 | **foundation** | large environment piece |  |
| `0CB064:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -6360, -17550, -5721 | 1.0 | 20 | -6368..-6340 / -17569..-17532 | **foundation** | decorative clutter |  |
| `0CB062:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -6270, -16915, -5730 | 1.0 | 20 | -6273..-6257 / -16938..-16899 | margin | decorative clutter |  |
| `0CB063:Skyrim.esm` | `022201:Skyrim.esm` CritterLandingMarker_Small | STAT | -5649, -17237, -5730 | 1.0 | 20 | -5672..-5634 / -17246..-17237 | **foundation** | decorative clutter |  |
| `03919F:Skyrim.esm` | `01B37D:Skyrim.esm` FXRapids | MSTT | -5881, -17486, -5803 | 0.56 | 386 | -6232..-5510 / -17871..-17144 | **foundation** | decorative clutter |  |
| `03921B:Skyrim.esm` | `01B37D:Skyrim.esm` FXRapids | MSTT | -4015, -18753, -5686 | 0.79 | 544 | -4528..-3465 / -19314..-18247 | **foundation** | decorative clutter |  |
| `107FB4:Skyrim.esm` | `106A1D:Skyrim.esm` FXRapids02 | MSTT | -7038, -16681, -5843 | 0.65 | 447 | -7388..-6742 / -16998..-16364 | margin | decorative clutter |  |
| `107FB5:Skyrim.esm` | `106A1D:Skyrim.esm` FXRapids02 | MSTT | -3316, -19317, -5564 | 0.85 | 585 | -3869..-2790 / -19828..-18740 | **foundation** | decorative clutter |  |
| `0391A4:Skyrim.esm` | `01B37E:Skyrim.esm` FXrapidsFallsLine01 | MSTT | -5156, -17939, -5677 | 0.53 | 436 | -5575..-4729 / -18312..-17472 | **foundation** | decorative clutter |  |
| `039159:Skyrim.esm` | `03BD73:Skyrim.esm` TundraStreamEnd01Tundra01 | STAT | -7121, -16235, -5837 | 1.0 | 705 | -7809..-6509 / -16893..-15630 | margin | decorative clutter |  |
| `0390BC:Skyrim.esm` | `043F4B:Skyrim.esm` TundraStreamTransition01 | MSTT | -5590, -17686, -5764 | 1.0 | 413 | -6001..-5178 / -18098..-17275 | **foundation** | decorative clutter |  |
| `039186:Skyrim.esm` | `043F4B:Skyrim.esm` TundraStreamTransition01 | MSTT | -3723, -19049, -5637 | 1.0 | 413 | -4136..-3310 / -19462..-18636 | **foundation** | decorative clutter |  |
| `03C08F:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -6716, -16917, -5783 | 0.91 | 214 | -6941..-6517 / -17111..-16743 | margin | vegetation |  |
| `03C08A:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -6397, -16852, -5805 | 1.07 | 252 | -6665..-6163 / -17085..-16602 | **foundation** | vegetation |  |
| `03C016:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -5042, -17718, -5638 | 0.7 | 165 | -5143..-4946 / -17846..-17565 | **foundation** | vegetation |  |
| `03C024:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -4558, -18329, -5670 | 0.85 | 200 | -4764..-4378 / -18495..-18178 | **foundation** | vegetation |  |
| `03C023:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -4498, -18447, -5648 | 0.75 | 177 | -4620..-4365 / -18623..-18295 | **foundation** | vegetation |  |
| `03BFC6:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -4407, -18153, -5703 | 0.66 | 156 | -4554..-4246 / -18294..-17993 | **foundation** | vegetation |  |
| `03C02B:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -3764, -18606, -5666 | 0.99 | 233 | -3957..-3535 / -18762..-18447 | **foundation** | vegetation |  |
| `03C02A:Skyrim.esm` | `0A731C:Skyrim.esm` TreeDeadShrub | TREE | -3745, -18732, -5669 | 0.87 | 205 | -3928..-3535 / -18884..-18565 | **foundation** | vegetation |  |
| `0CB05F:Skyrim.esm` | `0BB947:Skyrim.esm` TreeFloraTundraCotton01 | TREE | -5642, -17227, -5795 | 1.0 | 45 | -5686..-5600 / -17238..-17211 | **foundation** | vegetation |  |
| `03BFCF:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -6875, -17183, -5829 | 0.98 | 137 | -6967..-6778 / -17292..-17073 | margin | vegetation |  |
| `03C089:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -6535, -16839, -5819 | 0.78 | 109 | -6642..-6424 / -16946..-16736 | margin | vegetation |  |
| `03BFCD:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -6508, -17032, -5824 | 0.63 | 88 | -6597..-6421 / -17120..-16947 | margin | vegetation |  |
| `03BFCE:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -6444, -17073, -5829 | 0.75 | 105 | -6534..-6355 / -17151..-16990 | **foundation** | vegetation |  |
| `03C088:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -6426, -16986, -5831 | 0.82 | 115 | -6520..-6337 / -17088..-16886 | **foundation** | vegetation |  |
| `03C015:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -5540, -17278, -5789 | 0.85 | 119 | -5632..-5442 / -17383..-17173 | **foundation** | vegetation |  |
| `03C014:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -5492, -17352, -5785 | 0.95 | 133 | -5623..-5358 / -17479..-17220 | **foundation** | vegetation |  |
| `03C01F:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4673, -18341, -5690 | 0.74 | 104 | -4777..-4573 / -18437..-18241 | **foundation** | vegetation |  |
| `03C025:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4663, -18273, -5684 | 0.68 | 95 | -4742..-4585 / -18341..-18202 | **foundation** | vegetation |  |
| `03C020:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4623, -18444, -5683 | 0.96 | 134 | -4750..-4490 / -18579..-18312 | **foundation** | vegetation |  |
| `03C022:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4442, -18583, -5687 | 0.98 | 137 | -4556..-4328 / -18681..-18479 | **foundation** | vegetation |  |
| `03C026:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4348, -18087, -5690 | 0.63 | 88 | -4433..-4266 / -18176..-18001 | **foundation** | vegetation |  |
| `03C021:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4304, -18514, -5679 | 0.76 | 106 | -4407..-4197 / -18621..-18409 | **foundation** | vegetation |  |
| `03C027:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -4253, -18201, -5649 | 0.74 | 104 | -4351..-4158 / -18288..-18110 | **foundation** | vegetation |  |
| `03BFCC:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3678, -19297, -5603 | 0.95 | 133 | -3794..-3564 / -19397..-19191 | margin | vegetation |  |
| `03BFCB:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3568, -19335, -5584 | 0.68 | 95 | -3635..-3497 / -19413..-19257 | margin | vegetation |  |
| `03C030:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3496, -18969, -5594 | 0.92 | 129 | -3599..-3397 / -19083..-18858 | margin | vegetation |  |
| `03C02F:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3446, -19096, -5589 | 0.92 | 129 | -3562..-3328 / -19201..-18986 | margin | vegetation |  |
| `03C034:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3288, -18799, -5571 | 0.63 | 88 | -3357..-3218 / -18861..-18740 | margin | vegetation |  |
| `03C035:Skyrim.esm` | `0A7329:Skyrim.esm` TreeThicket01 | TREE | -3234, -18705, -5576 | 0.71 | 99 | -3334..-3139 / -18803..-18605 | margin | vegetation |  |
| `03C091:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -6515, -17989, -5749 | 0.8 | 127 | -6602..-6429 / -18085..-17893 | **foundation** | vegetation |  |
| `03C08C:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -6274, -16910, -5770 | 0.8 | 127 | -6400..-6149 / -17032..-16788 | **foundation** | vegetation |  |
| `03C092:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -6130, -18184, -5756 | 0.86 | 136 | -6260..-6001 / -18308..-18059 | **foundation** | vegetation |  |
| `03C00E:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5290, -16704, -5800 | 0.85 | 135 | -5417..-5163 / -16825..-16583 | margin | vegetation |  |
| `03C000:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5280, -18785, -5662 | 0.84 | 133 | -5402..-5159 / -18900..-18670 | **foundation** | vegetation |  |
| `03C019:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5041, -18584, -5652 | 0.97 | 154 | -5167..-4916 / -18699..-18468 | **foundation** | vegetation |  |
| `03C001:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -5003, -18998, -5621 | 0.9 | 143 | -5114..-4891 / -19099..-18897 | **foundation** | vegetation |  |
| `03C018:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -4973, -17780, -5639 | 0.91 | 144 | -5086..-4861 / -17882..-17677 | **foundation** | vegetation |  |
| `03C01A:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -4431, -17775, -5630 | 0.91 | 144 | -4558..-4305 / -17908..-17642 | **foundation** | vegetation |  |
| `03C01D:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -4298, -17642, -5621 | 0.88 | 140 | -4427..-4170 / -17776..-17509 | **foundation** | vegetation |  |
| `03C029:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -4149, -18120, -5629 | 0.93 | 148 | -4276..-4022 / -18255..-17986 | **foundation** | vegetation |  |
| `03C02D:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -3867, -19363, -5588 | 0.94 | 149 | -3994..-3740 / -19481..-19245 | margin | vegetation |  |
| `03C083:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -3609, -18086, -5629 | 0.81 | 128 | -3713..-3506 / -18180..-17991 | margin | vegetation |  |
| `03C082:Skyrim.esm` | `0AAE7A:Skyrim.esm` TreeTundraShrub02 | TREE | -3226, -17008, -5771 | 0.88 | 140 | -3358..-3095 / -17134..-16882 | margin | vegetation |  |
| `03BFD3:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -6614, -18611, -5774 | 0.93 | 296 | -6863..-6365 / -18790..-18433 | **foundation** | vegetation |  |
| `03BFD2:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -6302, -18593, -5769 | 0.93 | 296 | -6587..-6016 / -18888..-18297 | **foundation** | vegetation |  |
| `03BFD1:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -6010, -18640, -5752 | 0.85 | 271 | -6215..-5804 / -18893..-18386 | **foundation** | vegetation |  |
| `03BFFF:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -5217, -18639, -5666 | 0.92 | 293 | -5510..-4924 / -18919..-18359 | **foundation** | vegetation |  |
| `03C006:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -4952, -19929, -5606 | 0.99 | 316 | -5267..-4636 / -20227..-19631 | margin | vegetation |  |
| `03BFD7:Skyrim.esm` | `0AAE7B:Skyrim.esm` TreeTundraShrub03 | TREE | -3250, -18509, -5606 | 0.86 | 274 | -3418..-3083 / -18741..-18278 | margin | vegetation |  |
| `03C008:Skyrim.esm` | `0AAE7F:Skyrim.esm` TreeTundraShrub04 | TREE | -5636, -19271, -5656 | 0.97 | 316 | -5942..-5337 / -19520..-18920 | **foundation** | vegetation |  |
| `03C009:Skyrim.esm` | `0AAE7F:Skyrim.esm` TreeTundraShrub04 | TREE | -5278, -17082, -5780 | 0.94 | 306 | -5576..-5046 / -17387..-16850 | **foundation** | vegetation |  |
| `03C007:Skyrim.esm` | `0AAE7F:Skyrim.esm` TreeTundraShrub04 | TREE | -5207, -19885, -5608 | 0.91 | 297 | -5546..-4958 / -20161..-19574 | **foundation** | vegetation |  |
| `03BFE3:Skyrim.esm` | `0AAE7F:Skyrim.esm` TreeTundraShrub04 | TREE | -3878, -19634, -5575 | 0.86 | 280 | -4098..-3586 / -19916..-19409 | margin | vegetation |  |
| `03BFE4:Skyrim.esm` | `0AAE7F:Skyrim.esm` TreeTundraShrub04 | TREE | -3405, -18659, -5602 | 0.82 | 267 | -3624..-3098 / -18925..-18397 | margin | vegetation |  |
| `03C08B:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -6599, -16887, -5790 | 0.85 | 140 | -6739..-6458 / -17027..-16747 | margin | vegetation |  |
| `03C012:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -5437, -17239, -5776 | 0.93 | 153 | -5540..-5335 / -17354..-17125 | **foundation** | vegetation |  |
| `03C017:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4996, -17603, -5634 | 0.86 | 142 | -5137..-4854 / -17744..-17461 | **foundation** | vegetation |  |
| `03C011:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4965, -17132, -5739 | 0.95 | 157 | -5121..-4808 / -17288..-16975 | **foundation** | vegetation |  |
| `03C00F:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4822, -16661, -5748 | 0.82 | 135 | -4952..-4693 / -16794..-16528 | margin | vegetation |  |
| `03C010:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4709, -16602, -5747 | 0.84 | 139 | -4846..-4573 / -16735..-16469 | margin | vegetation |  |
| `03C01E:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4385, -19136, -5631 | 0.96 | 158 | -4539..-4231 / -19286..-18987 | **foundation** | vegetation |  |
| `03C01B:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4380, -17579, -5620 | 0.99 | 163 | -4511..-4248 / -17699..-17458 | **foundation** | vegetation |  |
| `03C028:Skyrim.esm` | `0AAE81:Skyrim.esm` TreeTundraShrub05 | TREE | -4120, -18261, -5628 | 0.85 | 140 | -4252..-3989 / -18397..-18125 | **foundation** | vegetation |  |
| `03C093:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -6108, -18254, -5748 | 0.68 | 63 | -6167..-6046 / -18317..-18193 | **foundation** | vegetation |  |
| `03C094:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -5846, -17954, -5750 | 0.64 | 59 | -5900..-5789 / -18010..-17896 | **foundation** | vegetation |  |
| `03C004:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -5230, -19414, -5636 | 0.6 | 56 | -5280..-5182 / -19466..-19362 | **foundation** | vegetation |  |
| `03C00A:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -5091, -17191, -5731 | 0.85 | 79 | -5171..-5014 / -17270..-17114 | **foundation** | vegetation |  |
| `03C00D:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -5022, -16816, -5782 | 0.99 | 92 | -5093..-4955 / -16892..-16741 | margin | vegetation |  |
| `03C01C:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -4320, -17742, -5624 | 0.79 | 73 | -4376..-4265 / -17791..-17691 | **foundation** | vegetation |  |
| `03C02E:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -3841, -19158, -5634 | 1.0 | 93 | -3927..-3758 / -19246..-19069 | **foundation** | vegetation |  |
| `03C02C:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -3754, -19347, -5570 | 0.77 | 71 | -3824..-3681 / -19418..-19275 | margin | vegetation |  |
| `03BFE1:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -3634, -19599, -5563 | 0.64 | 59 | -3687..-3581 / -19648..-19548 | margin | vegetation |  |
| `03C031:Skyrim.esm` | `0AAE85:Skyrim.esm` TreeTundraShrub07 | TREE | -3363, -18976, -5542 | 0.75 | 70 | -3428..-3295 / -19044..-18907 | margin | vegetation |  |
| `03C08E:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -6650, -16782, -5790 | 0.81 | 87 | -6736..-6581 / -16845..-16703 | margin | vegetation |  |
| `03C08D:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -6270, -16787, -5774 | 0.78 | 84 | -6365..-6197 / -16874..-16706 | margin | vegetation |  |
| `03C032:Skyrim.esm` | `0AAE87:Skyrim.esm` TreeTundraShrub08 | TREE | -3398, -18947, -5555 | 0.63 | 68 | -3445..-3345 / -18995..-18882 | margin | vegetation |  |
| `03C090:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -6687, -18030, -5759 | 0.95 | 303 | -6909..-6465 / -18309..-17751 | **foundation** | vegetation |  |
| `03BFD0:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -5946, -18199, -5752 | 0.88 | 281 | -6120..-5773 / -18437..-17961 | **foundation** | vegetation |  |
| `03C013:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -5546, -17093, -5790 | 0.85 | 271 | -5786..-5305 / -17276..-16909 | **foundation** | vegetation |  |
| `03C005:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -5424, -19431, -5634 | 0.93 | 296 | -5684..-5164 / -19626..-19235 | **foundation** | vegetation |  |
| `03BFE2:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -3530, -19731, -5555 | 0.85 | 271 | -3799..-3260 / -19994..-19467 | margin | vegetation |  |
| `03C07F:Skyrim.esm` | `0AAE89:Skyrim.esm` TreeTundraShrub09 | TREE | -3097, -17284, -5757 | 0.94 | 300 | -3388..-2807 / -17532..-17036 | margin | vegetation |  |
| `03C002:Skyrim.esm` | `0AAE73:Skyrim.esm` TreeYellowShrub01 | TREE | -5001, -19477, -5614 | 0.78 | 84 | -5086..-4924 / -19560..-19395 | **foundation** | vegetation |  |
| `03C00B:Skyrim.esm` | `0AAE73:Skyrim.esm` TreeYellowShrub01 | TREE | -4943, -16646, -5750 | 0.86 | 93 | -5019..-4862 / -16724..-16561 | margin | vegetation |  |
| `03C003:Skyrim.esm` | `0AAE75:Skyrim.esm` TreeYellowShrub03 | TREE | -5101, -19348, -5633 | 0.98 | 166 | -5268..-4935 / -19514..-19181 | **foundation** | vegetation |  |
| `03C00C:Skyrim.esm` | `0AAE75:Skyrim.esm` TreeYellowShrub03 | TREE | -4960, -16745, -5770 | 0.87 | 148 | -5068..-4853 / -16859..-16631 | margin | vegetation |  |

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
| **Ramp tile** | 512 x 512 | rises 64u | 1:8 grade, chainable. 160u of fall needs 3 chained tiles over ~1,536u |
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
| `Ramp_512` | unyielding, mass 0 | **child box rotated 7.13 deg** (`SkyrimFair_Ramp_512_Collider`) |
| `Shoulder_512` | unyielding, mass 0 | **none, intentional** |

Two corrections were needed against the BGS defaults, both worth knowing for future assets:

1. **The default rigidbody is a movable prop** — mass 80, `unyielding` off. For static world geometry that is wrong, so every piece is now set `unyielding = True, mass = 0`.
2. **A bounding-box collider on the ramp would be a solid 512 x 512 x 256 block** and would stop the player walking up the slope. The ramp instead uses a separate box child collider rotated 7.13 degrees (`atan(64/512)`) to lie along the slope — the "Adding Collision using Child Collider Meshes" method from Bethesda's guide. Confirmed present in the exported FBX.

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
- the **ramp's rotated collider survived conversion** — transform reads cos 0.992 / sin 0.124 (7.13 deg), box 512 x 516 x 64, the 516 being the slope length `hypot(512, 64)`
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
