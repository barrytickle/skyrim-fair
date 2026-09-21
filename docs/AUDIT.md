# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass and decision gate

Focused foundation polish pass, driven by Barry's first textured in-game test. No stalls, NPCs, navmesh, stage content or terrace expansion. The approved footprint, elevation and entrance are unchanged.

Five faults were reported from screenshots. All five traced back to three underlying causes, and all five are fixed:

| Reported in game | Actual cause | Fix |
| --- | --- | --- |
| Cliff beside the ramp is one-sided, vanishes from behind | `DirtCliffs01` is an open shell whose face is on its local **-Y** side; the generator pointed local **+Y** outward, so the face aimed into the platform and the missing back wall aimed at the player | half a turn added to the rotation, offset changed from outward to inward, top sunk below the floor plane |
| Paving material on vertical ramp faces | the ramp was one textured box, so the paving material reached all six sides | structural body now carries an earth material and has no upward face; a separate cap carries the paving |
| Ramp walkable surface not showing the paving | the walkable face was wound **inward**, so it was culled and what rendered was the slab's underside 384 units below | winding fixed; the walking surface is now a dedicated cap |
| Visible raised/vertical boundaries between paving tiles | same inversion: the visible surface was each slab's underside, framed by the rim of its side faces, so every tile read as a shallow recessed pan | winding fixed and the paving moved to zero-thickness caps, which have no side faces at all |
| Perimeter rocks clipping into the paved floor | rocks were allowed 192 units of overlap onto the paving with no upper bound on how far a large mesh could reach | overlap cut to 80 and a paving exclusion guard added |

The decision gate is again Barry's in-game visual review. Codex/Claude did not operate Skyrim, MO2, AssetWatcher's GUI, or any other GUI tool.

## The root cause worth remembering

**Every face of every piece in the kit was wound inward.** All 12 NIFs, since the kit was first authored.

This was verified by parsing the converted NIFs and, for each triangle, comparing its geometric normal against the direction from the mesh centroid. Every piece is convex, so a face is outward exactly when its normal points away from the centroid. The result before this pass was `outward=0, INWARD=12` on every box, and `0 / 8` on the wedge.

An inside-out solid still renders something, which is why this survived several passes: from outside, the near faces are back-face culled and you see the *inside* of the far faces instead. On a 32-unit grey slab that is nearly indistinguishable from a correct one. The moment a normal-mapped stone texture went on, it became obvious — the lighting was wrong, the visible surface sat 32 units low, and every tile boundary showed the rim of its side faces.

`box()`, `sloped_box()` and `wedge()` now wind every face outward, and `main()` **asserts** it for every piece rather than reporting it, so this cannot regress quietly.

## Foundation construction method

This is now the intended permanent method.

```
structural body   collision + outer earth/rock faces, NO upward face
visual cap        zero-thickness upward polygon, paving material, NO collision
```

Both are placed at the same position, rotation and Z. Consequences:

- the paving material can only ever appear on a walking surface;
- adjacent caps abut with no geometry between them, so the floor reads continuous;
- collision stays exactly on the floor plane, on the structural body;
- the structural body's sides are still available to be hidden by cliff and rock dressing.

| Role | Piece | Collision |
| --- | --- | --- |
| `floorFill` | `SkyrimFair_FloorFill_1024` | self box |
| `paveCapFill` | `SkyrimFair_PaveCap_1024` | none |
| `floorEdge` | `SkyrimFair_FloorEdge_512` | self box |
| `paveCapEdge` | `SkyrimFair_PaveCap_512` | none |
| `ramp` | `SkyrimFair_Ramp_512` | child box, 10.62 deg |
| `rampCap` | `SkyrimFair_RampCap_512` | none |
| `retain` | `SkyrimFair_Retain_512` | self box |
| `retainCorner` | `SkyrimFair_RetainCorner_128` | self box |
| `shoulder` | `SkyrimFair_Shoulder_512` | rigidbody, no collider |

The four UV phase variants of the floor edge and ramp have been **removed**. They existed to stop a small texture period stamping visibly, but the vanilla Whiterun floor repeats every 256 units, which divides the 512 grid exactly — so phasing it would have broken continuity rather than helped, and in vanilla mode all four variants were byte-identical anyway. `PhasedRole` now falls back to the base role when a variant is not configured, so `project_cobble` mode can reintroduce them without further code changes. The kit went from 12 NIFs to 9.

## Materials

Every piece now has a material. The retaining faces, retaining corner and verge wedge previously had **none**, so they rendered with the engine default — the flat lavender surfaces visible across the whole terrace in Barry's screenshots. 50 references were affected (24 retain, 2 corner, 24 shoulder).

| Surface | Diffuse | UV period |
| --- | --- | --- |
| Paving caps | `textures\architecture\whiterun\WRStoneFloor02.dds` | 256 |
| Structural bodies, retaining, corner | `textures\landscape\dirtcliffs\dirtcliffs01.dds` | 512 |
| Verge wedge | `textures\landscape\fieldgrass02.dds` | 512 |

All are vanilla paths referenced directly. Nothing vanilla is copied into the project or the mod. The dirt-cliff and field-grass paths were read out of shipped meshes (`dirtcliffs01_lod_0.nif`, SMIM's `dirtcliffs01moss.nif`) rather than guessed.

Measured UV scale on the converted caps: `PaveCap_1024` and `PaveCap_512` both **256.0** Skyrim units per repeat; `RampCap_512` **258.2**, the 1.7% being planar projection across a 10.62-degree slope. The audited vanilla `WRMainRoadMarket` spread is 254–259, so the ramp sits inside vanilla's own tolerance.

`vertex_colors_enabled` is now **false** on the paving material. It had been copied from the audited vanilla shader, but that mesh carries a vertex-colour layer and these do not, so the flag was telling the shader to read data that was not exported.

## Cliff dressing, corrected

The visible long-run skin is still vanilla `DirtCliffs01Tundra01`, `00097065:Skyrim.esm`.

Its geometry was measured rather than assumed, from `dirtcliffs01_lod_0.nif` (the plain mesh) and cross-checked against SMIM's `dirtcliffs01moss.nif`:

| Shape | Area by facing | Boundary edges |
| --- | --- | --- |
| `DirtCliffs01:0` — the cliff face | **82% local -Y** | open |
| `DirtCliffs01:1` — the grassy top cap | **96% +Z** | open |

Local bounds run `Y -97 .. +330`: the face is on the -Y edge, the body extends to +Y, and the +Y side is simply absent — 176 boundary edges on the main face alone. It is a shell designed to be embedded in terrain.

| Field | Before | Now |
| --- | --- | --- |
| Rotation | `OutwardRotation` (local +Y outward) | `OutwardRotation + pi` (face outward) |
| Offset | 72 units **outward** | 48 units **inward** (`cliffInset`) |
| Top Z | floor **+24** | floor **-40** (`cliffTopSink`) |

The inward offset is what buries the missing back wall inside the structural slab. Sinking the top hides the grassy cap: it is a broad horizontal surface running the full depth of the mesh, so any part of it above the floor plane read as a grass shelf lying across the market floor — which is exactly how it looked in game.

Verified in the written plugin: 3 cliff references, every top below the floor plane, and for each one a point 200 units along its local +Y lands inside the paved footprint, so no open back faces open air.

## Paving exclusion guard

New backstop so no future generated placement can protrude through the market floor.

The protected region is the paved footprint eroded inward by `pavingRimAllowance` on **outer edges only** — shared edges between two paved cells are not eroded, or the protection would be full of holes along every internal tile boundary. Any dressing whose crown clears `floorZ + pavingClearance` is refused if its mesh radius reaches into that region.

| Setting | Value | Why |
| --- | --- | --- |
| `wallEdgeOverlap` | 192 → **80** | enough to interrupt the edge line; at 192 rocks stood well inside the floor |
| `pavingRimAllowance` | **112** | must exceed the overlap, or the guard would refuse the rim rocks it is meant to permit |
| `pavingClearance` | **8** | below this a piece is under the walking surface and harmless |

It is wired into all four dressing bands: embankment rocks, corner stones, toe rocks and verge plants. **21 picks were refused** on this rule in the current build.

Effect on the usable surface, sampled on a 32-unit grid against every piece crowning at or above the floor plane, using OBND radius as the reach (the horizontal half-diagonal, so deliberately pessimistic):

| | Before | Now |
| --- | --- | --- |
| Paved surface clear of rock | 64.3% | **95.5%** |
| Largest clear square | 1,216 | **1,376** units |

## Generated and deployed plugin

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | **67,160 bytes** |
| SHA256 | `73ee5d3dc4ad52e25fdc6e886e04be8727d0ab91ae00a9b2771904e506533313` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Masters | `Skyrim.esm` only |
| Records | 1 TES4, 1 WRLD, 6 CELL, 9 STAT, 246 REFR |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Centre / floor | `X -5888, Y -12928`, floor `Z -5504` |
| Cells | `-3,-4`, `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |

Nine project NIFs deployed, each byte-identical to its repository copy. The six stale phase-variant NIFs were removed from the mod folder. The three project-owned procedural DDS files remain for a reversible comparison and are not referenced by the current NIFs.

## Approved foundation retained

The 22-cell mask is unchanged:

```text
.###..
.#####
######
.#####
..###.
```

| Piece | Count |
| --- | --- |
| `floorFill` / `paveCapFill` | 4 / 4 |
| `floorEdge` / `paveCapEdge` | 6 / 6 |
| `ramp` / `rampCap` | 8 / 8 |
| `retain` | 24 |
| `retainCorner` | 2 |
| `shoulder` | 24 |

The ramp remains four tiles long by two wide, drops 96 per 512-unit tile, foot landing at `-5888`, rotation 0, with its 480-unit clear walking channel and 512-unit landing extension.

| Vanilla dressing | Count |
| --- | --- |
| Tundra cliff faces / wall segments covered | 3 / 8 |
| Embankment rocks | 33 |
| Corner stones | 9 |
| Toe rocks | 48 |
| Shrubs and scrub | 60 |
| **Total vanilla references placed** | **153** |
| Rejected as oversized | 8 |
| Rejected for blocking the entrance channel | 36 |
| Rejected for protruding through the market floor | 21 |

Five vanilla references remain disabled, and only those five: `048032`, `04801A`, `048031`, `023362`, `047F8C`.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Independent checks, parsing the written ESP and the converted NIFs without Mutagen or Blender:

- Release build: zero warnings, zero errors.
- `Skyrim.esm` sole master; author `BarryRim Event Planner` intact.
- Only WRLD/CELL/REFR/STAT records; no LAND, NAVM, NPC, quest, package or scene records.
- **No kit mesh is inside-out** (was: all 12).
- **Paving material appears on upward faces only.**
- **No kit mesh ships without a texture** (was: 3 pieces, 50 references).
- Every structural body has exactly one cap at the same position, rotation and Z: 4/4, 6/6, 8/8.
- All flat paving caps sit exactly on the floor plane.
- Ramp chain untouched: 8 bodies, four Z levels 96 apart, rotation 0, foot at `-5888`.
- All 3 cliff tops below the floor plane; all 3 open backs run into the paved platform.
- **No vanilla dressing protrudes through the protected market floor.**
- Exactly 5 references disabled, and exactly the five named.
- **All six overridden CELL records byte-identical to vanilla.**
- `modlist.txt`, `plugins.txt` and `loadorder.txt` were not written by the generator or the deploy; their timestamps are from Barry's own MO2 session.

### Accepted deviation on the Tamriel WRLD override

The `WRLD` record differs from vanilla in exactly two subrecords and nothing else. All 16 others, including `OFST` at 45,600 bytes, match byte for byte.

| Subrecord | Vanilla | Ours | Why |
| --- | --- | --- | --- |
| `RNAM` | 1,349,616 bytes | absent | region-cell cache, dropped by design; every real mod that touches Tamriel drops it and the game rebuilds it |
| `FULL` | 4 bytes | 7 bytes | vanilla stores a localised string ID; this plugin is not flagged localised, so the literal `Skyrim` is written. Standard for a non-localised override, identical in English, but it would force English on a localised install |

## Known and deliberately not fixed

- **Ramp foot's eastern corner** runs into a walkable native bank over its last 128 units, covering the ramp by up to 96. The slope there is about 29 degrees, so it is walkable and reads as the ramp emerging from a bank. Fixing it needs a LAND edit or moving an approved placement.
- **NGIO grass cache** not regenerated, so grass may still grow through the paving. Barry's step; nothing in the plugin can change it.
- **No navmesh**, so NPCs cannot use the terrace or ramp.
- The project-owned procedural cobble remains a reversible A/B option, not the current material.

## What Barry should test in game

1. Does the paved floor now read as **one continuous surface**, with no grid of raised lines between tiles?
2. Walk up and down the ramp: is the paving on the **slope** now, and gone from its vertical sides?
3. Does the cliff dressing stay visible when you walk round and look back at it from outside?
4. Is there any remaining flat lavender/untextured surface anywhere on or under the terrace?
5. Do the perimeter rocks now stop at the edge rather than standing on the market floor?
6. Does the terrace still read as a rocky landform rather than a built platform, now the edges are earth-textured instead of bare?
