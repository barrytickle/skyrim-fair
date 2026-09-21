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

## Current build — foundation prototype deployed

Site 1, the `Pass` icon and fast travel are all confirmed in game. This pass adds the
landscaped foundation prototype: an irregular paved area built from the project-owned
tile kit, with vanilla rocks and plants dressing the boundary.

**Not yet seen in game.** Everything below is structurally verified only.

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | **55,865 bytes** |
| sha256 | `2e19b22caeebeda74db1a9858ec534ec437ee47d1c445037721392ee01f819b1` |
| Masters | `Skyrim.esm` only |
| TES4 Author (`CNAM`) | `BarryRim Event Planner` |
| Records | **1 WRLD, 5 CELL, 6 STAT, 98 REFR** |
| Excluded | no LAND, NAVM, NPC, quest, script, package, music or animation records |
| Load order | position 82, before `DynDOLOD.esp` (416) and `Occlusion.esp` (417) |

### STAT records — the tile kit

| FormKey | EditorID | Mesh |
| --- | --- | --- |
| `000802:SkyrimFair.esp` | `SkyrimFairFloorFill1024` | `SkyrimFair\SkyrimFair_FloorFill_1024.nif` |
| `000807:SkyrimFair.esp` | `SkyrimFairFloorEdge512` | `SkyrimFair\SkyrimFair_FloorEdge_512.nif` |
| `00080E:SkyrimFair.esp` | `SkyrimFairRetain512` | `SkyrimFair\SkyrimFair_Retain_512.nif` |
| `000839:SkyrimFair.esp` | `SkyrimFairRetainCorner128` | `SkyrimFair\SkyrimFair_RetainCorner_128.nif` |
| `00082E:SkyrimFair.esp` | `SkyrimFairRamp512` | `SkyrimFair\SkyrimFair_Ramp_512.nif` |
| `000810:SkyrimFair.esp` | `SkyrimFairShoulder512` | `SkyrimFair\SkyrimFair_Shoulder_512.nif` |

Mesh paths are relative to `Data\meshes\`.

### What is placed

| Piece | Count |
| --- | --- |
| floor fill 1024 | 4 |
| floor edge 512 | 6 |
| retaining face | 18 |
| retaining corner | 2 |
| ramp | 6 (2 wide x 3 chained) |
| shoulder wedge | 18 |
| vanilla rock / shrub / scrub dressing | 44 |
| map marker + market stall | 2 |

Plus the original two references: `FairSiteMapMarker` and `FairTestMarketStall`.

### Footprint

The outline is driven by an irregular mask in `fair.config.json`, one character per
512-unit cell, so the paving stays on a clean grid while the silhouette does not read
as a rectangle or as the L-shaped safe envelope:

```text
.###..
.#####
######
.#####
..###.
```

- paved extent `X -7168..-4096`, `Y -14080..-11520` (3072 x 2560 bounding box, 22 cells paved)
- floor **Z -5672** — the terrain maximum across the footprint, so the platform is
  **pure fill with zero cut and no LAND edits**
- terrain relief under the paving: 120u
- 4 fill tiles cover the interior via greedy 2x2 blocks; the remaining 6 cells take
  512 edge tiles, which is what lets the outline step in 512u increments

### Edges

Measured floor exposure above native terrain, per compass edge:

| Edge | Mean | Max | Treatment |
| --- | --- | --- | --- |
| West | 74u | 104u | retaining + shoulder |
| South | 64u | 96u | **entrance ramp** + retaining |
| North | 63u | 80u | retaining + shoulder |
| East | 32u | 80u | partly **meets grade** |

Two east segments where native ground reaches within 16u of the floor get **no**
retaining and no shoulder: there is no step to hide there, so the paving simply meets
grade. That is the `minExposure` rule in config.

The ramp runs south, chained 3 tiles deep, stepping `-5672 → -5736 → -5800`. It is
placed on the **south** edge deliberately: 180 degrees is the one rotation that is
identical under either Z-rotation sign convention, so the most orientation-sensitive
piece in the kit cannot come out backwards on its first test.

### Two placement problems found and fixed

1. **The stall would have been buried.** It sat at native ground `Z -5720`, which is
   48 units *below* the new paving at `-5672`. It now stands on the platform at the
   floor height, giving a familiar object for judging surface level and scale.
2. **The map marker would have trapped the player.** It was at the site centre at
   `-5720`, under a 32-unit slab whose underside is at `-5704` — a 16-unit gap. Fast
   travelling there would have dropped the player beneath the paving. The marker has
   moved to open native ground beyond the ramp foot at **`(-5632, -15872, -5912)`**,
   so arrival is now at the fair entrance, facing the ramp.

### Cells touched

| Cell | FormKey | Contents |
| --- | --- | --- |
| `-2,-4` | `00009A28:Skyrim.esm` | most of the paving, ramp, stall |
| `-2,-3` | `00009A07:Skyrim.esm` | northern paving and dressing |
| `-1,-4` | `00009A27:Skyrim.esm` | eastern dressing only |
| `-1,-3` | `00009A06:Skyrim.esm` | eastern dressing only |
| persistent `00000D74` | | map marker |

The two `-1` cells are touched only because dressing spills past the eastern paving
edge. **All five cell overrides are byte-identical to vanilla** — same subrecord kinds,
same values.

### Verification

19 structural checks, all passing, by parsing the written ESP independently of Mutagen:
single master, author intact, 6 STAT records with mesh paths all under `SkyrimFair\`
and ending `.nif`, 32 references on the floor plane, 6 ramp tiles at exactly three Z
levels 64u apart sharing one rotation, no shoulder above the floor, dressing varied in
scale and rotation, marker off the paved area, stall on it, and no LAND / NAVM / NPC /
quest / script / package / music / animation records.

### MO2 deployment

| Field | Value |
| --- | --- |
| Plugin | `mods\Skyrim Fair\SkyrimFair.esp`, byte-identical, sha256 `2e19b22c…f819b1` |
| Meshes | `mods\Skyrim Fair\meshes\SkyrimFair\*.nif` — all 6, byte-identical |

`modlist.txt`, `plugins.txt` and `loadorder.txt` hashed before and after: **unchanged**.
Nothing enabled or reordered, no saves touched, Skyrim not launched.

### Known caveats for the in-game test

- **The kit is untextured.** It will render with a default material. Judge geometry,
  shape and how the edge meets the landscape, not looks.
- **No navmesh.** NPCs cannot path onto the platform; that is the next milestone.
- **Z-rotation convention is unconfirmed.** Retaining faces use 0 / 90 / 180 / 270
  degrees with +Y outward, on the right-handed assumption. If the sign is flipped, the
  north and south faces will look right and the **east and west ones will be wrong** —
  that is the diagnostic. The ramp is immune, being at 180.
- Only north-east style outer corners get a corner piece, for the same reason. Other
  corners rely on rocks, which is the intended treatment anyway.

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

Phase 4 is done and deployed. **Barry's in-game inspection is the gate** before anything else.

What to judge, in order:

1. **Is the centre genuinely flat?** Walk the paved area. The market stall stands at the
   centre on the floor plane as a reference.
2. **Does the outline read as organic?** From a distance and from the ramp, the perimeter
   should curve and vary, never reading as a rectangle or an L.
3. **Does the edge treatment work?** Look at where retaining faces, shoulder wedges and
   vanilla rocks meet native tundra. The join is what this milestone exists to test.
4. **Does the ramp work?** Walk up and down the south entrance; check the three chained
   tiles and whether collision carries you smoothly.
5. **East side.** The paving should meet grade there with no retaining wall. Check it
   does not float or leave a lip.
6. **Rotation check.** If retaining faces look right on north and south but wrong on east
   and west, the Z-rotation sign is inverted — a one-constant fix.
7. **Fast travel.** Should now arrive on open ground south of the ramp, not under the paving.

Then, depending on the result:

- if the foundation reads well, the next milestones are a texture/material pass and then
  navmesh, which is required before any NPC can use the platform
- if the shape or edge treatment is wrong, iterate on the footprint mask and dressing in
  `fair.config.json` — both are data, so no code changes are needed
