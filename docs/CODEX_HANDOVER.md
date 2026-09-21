# Codex handover

This document hands the local Skyrim Fair implementation workflow from Claude to Codex.

Codex should treat the repository and the latest local audit as authoritative. Do not rely on chat memory when the repo says something different.

## First things to read

Before changing anything, read these in order:

1. `docs/AUDIT.md` — current verified local/build/deployment state
2. `docs/DESIGN.md` — design north star and approved visual direction
3. `docs/CLAUDE_AFTER_SITE_TEST.md` — historical foundation milestone brief; useful context, but many phases are now complete
4. `fair.config.json`
5. generator source under `src/SkyrimFair.Generator`
6. asset build script under `assets/blender/build_foundation_kit.py`

The active branch is `feat/bootstrap-generator`. Do not merge the open PR unless Barry explicitly asks.

## Project goal

The Wanderer's Fair should feel like a UK Christmas market translated into medieval Skyrim: warm, dense, handmade, lively, slightly chaotic, and recognisably Skyrim.

The current architecture is a raised, flat, irregular market terrace in the Tamriel worldspace near Whiterun, connected to an existing road by a broad ramp. The terrace is intentionally substantial rather than hidden at native grade. Its perimeter is meant to read as a natural rocky landform / prepared fairground, not as a rectangular platform.

The current site, footprint, height and entrance are approved unless Barry explicitly changes them.

## Current approved foundation state

Read `docs/AUDIT.md` for exact current values and hashes. At handover time the important state is:

- centre approximately `X -5888, Y -12928`
- floor `Z -5504`
- the platform was deliberately raised by +192 from the earlier test
- irregular 22-tile paving footprint
- broad ramp connects toward the existing road
- the ramp seam to the paving is geometrically continuous
- ramp slab depth was increased to remove visible daylight beneath the head
- perimeter naturalisation uses large vanilla tundra rocks, toe rocks, rough-earth verge wedges and vegetation
- paving and ramp have a material pipeline; the current deployed test references vanilla
  `WRStoneFloor02`, while the structural retaining geometry is hidden by rock/cliff dressing
- no LAND edits
- no NAVM edits
- no NPCs, quests, packages, music or animation records yet
- selected safe vanilla environment references are disabled where they physically conflict with the terrace
- scripted / enable-parented / unsafe references are intentionally left alone
- NGIO grass cache has not yet been regenerated, so grass may still appear through paving
- the ramp foot's eastern corner is partially covered by a walkable native bank; this is currently accepted pending in-game visual review
- the Tamriel WRLD override currently rewrites the FULL value as literal English `Skyrim`; this is known technical cleanup because vanilla stores a localised string ID

## Current gate

Do not immediately add stalls, NPCs, stage content, navmesh, final textures, or expand the platform.

Barry is currently visually testing the targeted cliff-wall dressing and the vanilla
`WRStoneFloor02` paving test in game. The project-owned procedural cobble candidate is
kept as a reproducible comparison, but it is not the current NIF material. Keep the
wall/cliff decision separate from the floor-material decision.

The next implementation step should be based on Barry's verdict from screenshots / in-game testing.

Likely near-term directions, only after approval:

- minor naturalisation cleanup if any rocks float, repeat too obviously, intrude too far, or clutter the clear core
- improve / texture the project-owned paving, retaining and ramp kit
- decide whether the current usable terrace needs expansion
- rough-zone the terrace for stage, market lanes, food, crowd space and specialist stalls
- only after the geometry/layout is accepted, perform a dedicated navmesh milestone

## Expansion rule

The current footprint is not permanently locked.

It may be expanded later if the festival layout needs more room. Preserve the modular approach:

- extend paving only where needed
- extend the rocky / earth / vegetation perimeter with it
- keep the visible outline irregular
- avoid obvious rectangles, L-shapes or grid silhouettes
- keep archery, stables and future jousting available as natural-ground activities outside the paved core where appropriate

Do not enlarge the platform simply because more space might be useful. First prove a need from the actual festival layout.

## Road and entrance

The existing road is part of the site's visual integration.

- never disable or alter the road reference just to simplify the ramp
- preserve useful roadside details unless they physically block access
- keep a clear walking channel from road to ramp to terrace
- the final entrance should feel like an existing Skyrim road naturally rises into the fair
- later decoration can add banners, lanterns, signage and festival framing around the approach

## Asset pipeline

Project-owned mesh work is code-first and reproducible.

- Blender 3.6.23 portable is used for the Skyrim asset pipeline
- BGS Art Tools [Skyrim] and BGS FBX Exporter [Skyrim] are installed and enabled
- `assets/blender/build_foundation_kit.py` is the reproducible source for the kit
- FBX is converted to NIF using Bethesda AssetWatcher
- conversion has a 40x scale behaviour; the build script compensates using the established Blender-to-Skyrim unit factor
- static world geometry must remain unyielding / mass 0
- ramp collision uses the verified rotated child box approach
- shoulder wedge intentionally has no collision
- do not save the authoritative ESP from Creation Kit

### Permanent material pipeline

- `assets/blender/build_foundation_kit.py` owns geometry, UVs and BGS material setup
- default paving comparison mode is `vanilla_whiterun_test`, using vanilla
  `textures\architecture\whiterun\WRStoneFloor02.dds` and `_n.dds`
- vanilla `WRStoneFloor02` is mapped at its measured native scale: one UV repeat per
  256 Skyrim units
- set environment variable `SKYRIM_FAIR_PAVING_MATERIAL=project_cobble` before the
  headless Blender build to regenerate the project-owned alternative
- the project candidate is built by `assets/textures/build_paving_material.py` and has
  diffuse, tangent-space normal/gloss and optional BC4 `_p` height outputs
- Community Shaders Extended Materials is active and supports the optional height path;
  ENB is not installed; materials must always remain convincing with height/parallax off
- physical mesh supplies large silhouette, normal maps supply surface detail, and
  optional parallax is restricted to medium/small cracks and relief
- never copy vanilla `WRStoneFloor` DDS files into the project or mod; reference their
  game paths directly
- do not adopt third-party Whiterun replacer assets without explicit reuse and
  redistribution permission

The Creation Kit and Bethesda art tools are against Barry's separate Steam Skyrim install, while the active modlist uses the stock Skyrim copy under MO2. Plugin records must continue to be generated against the stock data via Mutagen.

## Local environment

Current known paths include:

- MO2 portable root: `E:\Modlists\Still In Skyrim`
- stock Skyrim Data: `E:\Modlists\Still In Skyrim\stock\Data`
- deployed mod: `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp`
- active MO2 profile: `Still in Skyrim Plus`
- portable Blender 3.6.23: `C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\`

Do not assume these paths if the latest `docs/AUDIT.md` says otherwise.

## Build commands

The normal generator flow is:

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

After generation, structurally verify the written ESP independently where practical, then deploy only the Skyrim Fair output to the existing MO2 mod directory.

Do not touch Barry's save files, Pandora output, DynDOLOD, Occlusion, load order, or unrelated mods unless Barry explicitly asks.

## Git and documentation workflow

Barry wants Codex to document the work continuously, not just dump a summary at the end.

For every meaningful implementation pass:

1. read the latest `docs/AUDIT.md` before making local-dependent assumptions
2. state the plan before destructive or broad changes
3. make the smallest coherent change
4. verify it
5. overwrite `docs/AUDIT.md` with the complete latest verified state
6. include:
   - what changed
   - why it changed
   - exact coordinates / FormKeys / paths / dimensions where relevant
   - commands run
   - generated ESP size and SHA256
   - deployed ESP SHA256 and whether it is byte-identical
   - records added / removed / overridden
   - assets created or regenerated
   - verification performed
   - known deviations / unresolved issues
   - what Barry should test in game
7. commit with a descriptive message
8. push to `feat/bootstrap-generator`
9. do not merge

Do not append old audit reports inside `docs/AUDIT.md`. Git history is the history. The file should describe the latest state in full.

If a pass involves exploratory work that does not modify the plugin, still document the findings and commit the docs if they materially affect future decisions.

## Working style

Be conservative with Barry's live modlist.

- audit first, mutate second
- report unsafe / scripted / quest-linked references rather than touching them
- prefer config-driven values over hard-coded placement coordinates
- preserve the code-first reproducible pipeline
- do not silently change approved layout decisions
- when a request has visual ambiguity, measure and report before making a large layout move
- never claim something is verified unless it was actually checked against the generated/deployed output

If a tool or local dependency is missing, stop and document the blocker rather than inventing a workaround that risks the modlist.

## Design reminders for the later fair build

Once the foundation itself is approved, the intended fair includes:

- main stage and crowd square
- live multi-song stage set with short cheer / pause transitions
- dense market lanes
- specialist armour, magic and curiosity traders
- Imperial and Stormcloak rival stalls facing one another
- food and drink lane
- mandatory sweetroll-only stall, disproportionately popular for no explained reason
- archery and other games toward the outskirts
- future jousting on natural ground
- Garrick Tallow, a cheerful Bosmer wandering bard
- Claudius Vale, a reserved Imperial records-keeper / inspector

Do not add these before Barry approves the foundation and requests the next content milestone.

## Immediate handover behaviour

On taking over:

1. inspect the latest branch and `docs/AUDIT.md`
2. verify the working tree is clean or explain any local changes
3. do not rebuild/redeploy merely for the sake of taking over
4. summarise the current state to Barry in a short handover note
5. wait for Barry's in-game feedback or next explicit task
6. from then on, own the implementation and documentation workflow above
