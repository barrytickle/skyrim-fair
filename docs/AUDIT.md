# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass and decision gate

The approved terrace footprint, height and entrance are unchanged. This pass:

- dresses long exposed structural retaining runs with elongated vanilla tundra cliff faces;
- preserves the established large-rock treatment at corners, irregular transitions and ramp flanks;
- prepares the project-owned procedural paving material, but pauses its adoption;
- puts the current terrace on vanilla `WRStoneFloor02` for an in-game A/B comparison;
- adds no terrace expansion, festival content, NPCs, navmesh or LAND edits.

The decision gate is now human visual review: compare the deployed vanilla Whiterun floor against the project-owned procedural attempt and keep vanilla if it integrates better. Codex did not operate Skyrim, MO2, AssetWatcher or any GUI.

## Generated and deployed plugin

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 66,809 bytes |
| SHA256 | `c3a1b8ac167a66d9487a9ca783b3815ee1f2eecfad3ce7c3303e65eef796359f` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Masters | `Skyrim.esm` only |
| Records | 1 TES4, 1 WRLD, 6 CELL, 11 STAT, 235 REFR |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO or SCEN |
| Centre / floor | `X -5888, Y -12928`, floor `Z -5504` |
| Cells | `-3,-4`, `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |

The deployed mod contains 12 project NIFs. Every deployed NIF is byte-identical to its repository copy. The three project-owned procedural DDS files remain in the mod for a reversible comparison but are not referenced by the current test NIFs.

## Approved foundation retained

The 22-cell mask is unchanged:

```text
.###..
.#####
######
.#####
..###.
```

It resolves to four 1024-unit fill references, six 512-unit edge references and eight ramp references. There are 24 retaining-course references, two retaining-corner references and 24 shoulder references. The ramp remains four tiles long by two tiles wide, drops 96 units per 512-unit tile, and keeps its 480-unit clear walking channel plus 512-unit landing extension.

## Targeted retaining-wall dressing

The visible long-run skin is vanilla `DirtCliffs01Tundra01`, `00097065:Skyrim.esm`, model `Meshes\Landscape\DirtCliffs\DirtCliffs01.nif`.

Audited bounds:

- approximately `1944 x 465 x 276` Skyrim units;
- local bounds `[-955,-135,-10] .. [989,330,266]`;
- broad real mesh silhouette on its long axis;
- shallow physical variation and 276-unit height, so it remains convincing without parallax.

The generator groups contiguous same-facing wall segments. Runs of two to four 512-unit segments receive one cliff reference, scaled 0.90-1.25 to cover the measured drop and offset 72 units outward while still intersecting the structural wall. Current output: three cliff references covering eight straight wall segments. An independent raw ESP scan confirmed exactly three references to `00097065:Skyrim.esm`.

Cliff-covered segments no longer receive three unrelated boulders each. Toe rocks, verge and vegetation remain. Short runs, corners, irregular joins and ramp flanks retain the large-rock treatment. Embankment-rock count falls from 63 to 39 without changing the approved footprint.

Each prospective cliff's rotated length/depth AABB is tested against the entrance channel. Because the vanilla mesh pivot is asymmetric in depth (`Y -135..330`), the test deliberately uses the full 465-unit depth as its normal-axis half-extent rather than assuming a centred pivot. A face reaching that conservative envelope is rejected. Existing radius guards remain for rocks and plants. The project retaining mesh remains structural only; the visible face is the vanilla tundra cliff. No vanilla mesh or cliff texture is redistributed.

## Graphics-stack audit

Audited against active profile `E:\Modlists\Still In Skyrim\profiles\Still in Skyrim Plus` and the stock game root.

| Capability | Result |
| --- | --- |
| Community Shaders | active; `CommunityShaders.dll` version 1.4.11.0 |
| CS Extended Materials / complex parallax | available; shader package 1-1-0 implements parallax coordinates and soft self-shadowing |
| Active parallax precedent | Rustic Reliefs, Rustic Windows, Peltapalooza CPO, Assorted Mesh Fixes - Parallax and local ParallaxGen output |
| ENB complex parallax | unavailable; no `d3d11.dll`, `dxgi.dll`, `enbseries.ini` or `enblocal.ini` in the stock root |
| Standard Skyrim parallax convention | supported by the asset toolchain; height is texture slot 3, normally `_p.dds` |
| No-parallax fallback | required and supported; physical mesh gives silhouette, normal map gives surface response |

Active CPO assets use single-channel BC4 `_p.dds` height maps with mipmaps. The project-owned height output follows that convention, but the currently deployed Whiterun floor test has no height texture or parallax flags.

Community Shaders' local metadata reports installed 1.4.11.0 and newest 1.7.3.0. Nothing was updated in this pass.

## Vanilla Whiterun floor audit and test

### Stock textures inspected

The original Bethesda assets were extracted to a temporary audit directory from the separate Steam install's `Skyrim - Textures1.bsa`; they were not copied into the project or MO2 mod.

| Set | Diffuse | Normal |
| --- | --- | --- |
| `WRStoneFloor01` | 2048² BC1, 11 mips | 1024² BC3, 10 mips |
| `WRStoneFloor02` | 2048² BC1, 11 mips | 1024² BC3, 10 mips |

Both are muted, dirty Whiterun fieldstone. `02` is the appropriate general exterior choice: among 23 audited Whiterun exterior terrain/platform NIFs using this family, 22 use `02`; `01` appears only as an additional variant on `WRGreatHousePlatform02`.

### Native UV scale

The stock `WRMainRoadMarket.nif` was extracted temporarily and parsed directly. Its `WRStoneFloor02` BSTriShape has 182 vertices / 260 triangles. Across 777 usable triangle edges, world-distance-per-UV-repeat measured:

- 10th percentile: 254.33 units;
- median: 256.15 units;
- 90th percentile: 258.71 units;
- dominant rounded value: 256 units (717 edges).

The project NIFs therefore map this material at exactly 256 Skyrim units per UV repeat. Independent parsing of the converted outputs reports:

- `SkyrimFair_FloorFill_1024.nif`: 256.0 units/UV, 4 x 4 repeats;
- `SkyrimFair_FloorEdge_512.nif`: 256.0 units/UV, 2 x 2 repeats;
- `SkyrimFair_Ramp_512.nif`: 256.0 units/UV, 2 x 2 repeats.

### Current test material

The paving/ramp NIFs reference only:

- `textures\architecture\whiterun\WRStoneFloor02.dds`;
- `textures\architecture\whiterun\WRStoneFloor02_n.dds`.

They use wrap addressing, tangent-space normals, no height slot and no parallax flags. Shader values match the audited vanilla `WRMainRoadMarket` material: glossiness 80, white specular colour, specular strength 1, UV scale 1, UV offset 0, clamp mode 3.

No third-party Whiterun replacer was inspected, copied or adopted. At runtime the ordinary game asset-resolution rules may show whichever legally installed replacer wins Barry's own load order, but Skyrim Fair redistributes none of it and depends only on the vanilla paths.

## Project-owned procedural comparison material

This candidate remains available but is not active in the current NIFs.

- source: `assets/textures/source/SkyrimFair_Cobble_Source.png`;
- builder: `assets/textures/build_paving_material.py`;
- diffuse: `SkyrimFair_Cobble01.dds`, BC1 sRGB, 1024², 11 mips;
- normal/gloss: `SkyrimFair_Cobble01_n.dds`, BC3, 1024², 11 mips;
- optional height: `SkyrimFair_Cobble01_p.dds`, BC4, 1024², 11 mips.

The bitmap source was generated specifically for Skyrim Fair with OpenAI's built-in image generator: top-down irregular medieval fieldstone/cobble, muted grey-earth stones, dirty compacted joints, no modern bond, borders, symbols or strong baked directional light. The deterministic builder makes the layout toroidally periodic, creates tangent-space normal/gloss and a restrained optional height map. No photographed, vanilla, Nexus or other third-party source is included.

The Blender source supports both comparison modes:

```powershell
# current/default vanilla test
blender.exe --background --python assets\blender\build_foundation_kit.py

# project-owned procedural comparison
$env:SKYRIM_FAIR_PAVING_MATERIAL = "project_cobble"
blender.exe --background --python assets\blender\build_foundation_kit.py
```

The project candidate uses a 1024-unit period and four UV-phase variants for 512-unit pieces to avoid a 512-unit stamp. Its optional height supplies only cracks, joints and smaller relief; geometry remains responsible for silhouette.

## Asset outputs and verification

The logical six-piece kit is unchanged. UV variants produce 12 NIFs: one fill; four edge phases; retaining face; retaining corner; four ramp phases; shoulder.

Headless Blender 3.6.23 regenerated the `.blend` and all FBXs. The already-running, human-configured AssetWatcher converted the NIFs automatically. Codex did not interact with its GUI. All ramp variants retain the 10.62-degree child box collider; shoulder collision remains intentionally absent.

Verification performed:

```powershell
dotnet build SkyrimFair.sln -c Release
python assets/textures/build_paving_material.py --texconv <texconv.exe>
blender.exe --background --python assets\blender\build_foundation_kit.py
dotnet run --project src/SkyrimFair.Generator -c Release -- fair.config.json
python tools/footprint_audit.py --data <stock Data> --profile <active profile> --mods <MO2 mods> --esp dist/SkyrimFair.esp
```

Results:

- Release build: zero warnings, zero errors.
- ESP and all NIFs: deployed hashes match repository outputs.
- Independent ESP scan: only TES4/WRLD/CELL/STAT/REFR; three cliff references; no LAND/NAVM/content records.
- Converted test NIFs: correct vanilla texture paths, no custom height path, parallax disabled, 256-unit UV period.
- Footprint audit still finds ten paving references over six searched cells.
- Unsafe scripted `critterSpawnInsects_Many` and the enable-parented animal reference remain untouched.
- No load order, profile, save, DynDOLOD, Occlusion, Pandora or unrelated mod files were changed.

## Human in-game checks requested

1. Does vanilla `WRStoneFloor02` feel naturally part of Whiterun/tundra, or too recognisably city-specific?
2. Is its native 256-unit stone scale right on the broad terrace and ramp?
3. Are any seams visible between fill, edge and ramp pieces?
4. Do the three long cliff faces hide every clean structural slab without looking repeated or over-dense?
5. Walk the ramp and both shoulders to confirm no cliff or rock collision reaches the clear channel.

After that verdict, either keep vanilla and remove unnecessary custom deployment, or switch back to `project_cobble` for a direct second screenshot/test. The wall/cliff decision remains separate from the floor material choice.

Known unchanged cautions: the ramp-foot eastern bank is still accepted pending review; NGIO grass cache is not regenerated; Tamriel WRLD FULL localisation caveat remains; no navmesh should begin until this environmental pass is accepted.
