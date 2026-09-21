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

## Current build — foundation moved to the player's position

Barry reported vanilla rocks poking through the paving at Site 1 and asked for the fair
to be moved to where he was standing. Both are done.

**Not yet seen in game.**

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | **59,931 bytes** |
| sha256 | `eab8eebf37742f6b9d5713b8e30b598841fcb84468f0cc5ca8d67ea00a4f17b6` |
| Masters | `Skyrim.esm` only |
| Records | 1 WRLD, 4 CELL, 6 STAT, 156 REFR |
| Deployed | `mods\Skyrim Fair\SkyrimFair.esp`, byte-identical; 6 NIFs unchanged |

### Player position read from the save

Read directly from `Save50_144F99E7_0_4261727279_Tamriel_000455_20260921172310_7_1.ess`
(save 50, 2026-09-21 17:23, character "Barry", level 7).

The save is LZ4-compressed, so the body was decompressed with a small pure-Python LZ4
block decoder and the **Player Location** entry (global data type 1) parsed out:

| Field | Value |
| --- | --- |
| worldspace | `0x00003C` = Tamriel |
| position | **X -5432.4, Y -18253.0, Z -5627.7** |
| stored cell grid | `-2, -4` — **stale**, the position implies `-2, -5` |

The position is the authoritative field and cross-checks against terrain: native ground
at that XY is `-5656`, putting the player 28 units above it, exactly right for standing.
Two useful side confirmations from the save: `SkyrimFair.esp` is in its load order, and
the save carries 94 regular + 412 light plugins, matching `loadorder.txt`'s 506 lines.

**New fair centre: `X -5376, Y -18304`** — the player's position snapped to the 128-unit
heightmap grid, 76 units away. Snapping makes every tile corner land on a terrain sample
point, so floor and exposure calculations are exact rather than interpolated.

### The rocks — cause and fix

**15 vanilla references were standing inside the old paved footprint** and the paving was
laid straight over them: `RockShelf01FieldGrass01`, `RockTundraLand02Tundra01`,
`TreeThicket01`, eight tundra and yellow shrubs, and three critter markers.

The generator now overrides vanilla clutter within `clearMargin` (160u) of the paving and
sets **Initially Disabled** (`0x800`) on it. At the new site that is **56 references**.

This is safe:

- the references are **disabled, not deleted** — non-destructive and reversible
- **no LAND edit**, so the ground itself is completely untouched; only the objects
  standing on it stop rendering
- only `STAT`, `TREE` and `FLOR` base objects qualify. Activators, containers, doors,
  furniture and anything an NPC or quest might reference are deliberately excluded

Separately, dressing now starts **192u** beyond the paving edge (was 32u) with a 384u
spread, because vanilla rock meshes are large enough to spill onto the surface from close
range.

### New site geometry

| Field | Value |
| --- | --- |
| Cell | `00009A49:Skyrim.esm` (`TestTundra2`), grid `-2, -5` |
| Cells touched | `-2,-5`, `-1,-5`, `-2,-4` |
| Floor Z | **-5568** (terrain maximum, pure fill, no LAND edits) |
| Relief under paving | **392u** |
| Paved extent | X `-6912..-3840`, Y `-19584..-17024` |

Edge exposure and slope, measured:

| Edge | Mean | Max | Ground beyond |
| --- | --- | --- | --- |
| **West** | 224u | **392u** | falls away |
| **North** | 175u | 320u | **falls away** |
| South | 92u | 208u | rises |
| East | 83u | 136u | rises |

**The ramp moved from south to north.** At the old site the terrain fell away south; here
it rises into the hill that way, so a south ramp would have climbed uphill into nothing.
North both falls away and is rotation-safe (0 degrees), so it keeps the one piece whose
orientation cannot come out backwards on an unconfirmed rotation convention. Four chained
tiles drop 256u, stepping `-5568 → -5632 → -5696 → -5760`.

The map marker moved with it, to the **north ramp foot at `(-5376, -14720, -5792)`** —
on native ground, on the Whiterun approach, so fast travel arrives facing the ramp.

**Retaining now stacks.** A single piece is 256u tall and the west edge needs up to 392u,
which would have left a gap showing open terrain. Edges deeper than one course now place
additional courses downward; the build uses two courses at `-5568` and `-5824`.

### What is placed

| Piece | Count |
| --- | --- |
| floor fill 1024 / floor edge 512 | 4 / 6 |
| retaining face (stacked) | 22 |
| retaining corner | 2 |
| ramp | 8 (2 wide x 4 chained) |
| shoulder wedge | 19 |
| vanilla dressing | 44 |
| **vanilla clutter disabled** | **56** |

### Verification

23 structural checks, all passing, by parsing the written ESP independently of Mutagen:
single master, author intact, 6 STAT records with mesh paths under `SkyrimFair\`, paving
on the floor plane, 8 ramp tiles at four Z levels 64u apart all at rotation 0, retaining
stacked in 256u courses, no shoulder above the floor, marker at the ramp foot and outside
the paving, stall on the paving, 56 vanilla references flagged Initially Disabled and all
of them vanilla FormKeys, and no LAND / NAVM / NPC / quest / script records.

All four cell overrides remain byte-identical to vanilla.

### Concern: this site is three times less flat

Worth stating plainly. The audited Site 1 had **120u** of relief under the paving; the
player's position has **392u**. The west side becomes a 392u (about 5.6 m) faced wall.
That is now structurally handled by stacked retaining and it may well be the look Barry
wants — he asked about making the platform taller than the rocks — but it is a much
bigger intervention in the landscape than the original site needed, and the terrain here
is the lower slope of the hill that rises south and east.

If it reads as too monumental in game, the options are: shrink the footprint onto flatter
ground, shift back north-west toward the audited site, or keep it and lean into the
terrace look. All three are config-only changes.

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
