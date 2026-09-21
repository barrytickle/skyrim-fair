# Local Skyrim audit brief

This is the hand-off for a local coding agent running on Barry's Windows machine.

## Goal

Do not change the Skyrim installation yet.

Inspect the local environment so the next generator commit can place one visible vanilla object into a safe exterior Skyrim cell without guessing FormIDs, paths, or load-order assumptions.

Barry has now installed the .NET 10 SDK and has also added Medieval Markets to the local mod setup, so verify both of those changes in this pass.

## 1. Verify the repository toolchain

From the repository root:

```powershell
dotnet --info
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Report:

- installed .NET SDK version
- whether restore/build succeeds
- whether `dist/SkyrimFair.esp` is created
- the generated file size
- any exception or warning in full

Do not copy the generated ESP into Skyrim yet unless Barry explicitly asks.

## 2. Identify the active Skyrim setup

Report:

- Skyrim Special Edition install directory
- Skyrim `Data` directory
- game runtime version
- mod manager in use, if detectable
- whether SKSE is installed and its version
- whether Pandora, Nemesis, FNIS, or OAR are installed
- path to the active `plugins.txt`
- path to the active `loadorder.txt`, if present

Do not reorder or enable/disable any mods.

## 3. Inspect the load order for exterior-world conflicts

We are looking for a future fairground in an open, relatively flat Skyrim exterior.

Report mods that appear to modify:

- Whiterun exterior / tundra
- roads around Whiterun
- farms or settlements around Whiterun
- landscape
- navmesh
- large exterior overhauls

Do not decide a final site yet. Produce a short candidate/conflict report.

## 4. Find one safe test placement

For the first visible proof, identify:

- one exterior cell that is easy for Barry to reach
- its cell EditorID if one exists
- cell grid coordinates
- Tamriel worldspace reference
- one vanilla static or activator that is visually unmistakable and safe to place temporarily
- the source plugin and FormKey/FormID for that record

Prefer a harmless object such as a banner, crate, table, or small platform. Avoid quest objects, doors, markers, navmesh edits, containers with ownership, or anything scripted.

The next generator commit should be able to reference this object without a guessed load-order index.

## 5. Inspect Professional Dancer

If Professional Dancer is installed, report:

- installed version
- plugin name(s)
- scripts included by the mod
- whether it exposes a callable Papyrus API, spell/power, quest, global, or other obvious integration point
- animation framework requirements detected locally
- relevant EditorIDs/FormKeys if they can be identified safely

Do not modify, unpack, redistribute, or copy its animation assets.

## 6. Inspect Medieval Markets

Barry has added Medieval Markets:

- Nexus mod ID: 161479
- Project credits currently identify the mod author as JJerem
- We may want to reuse selected market stall, tent, shelving, table, basket, produce-display, and general market-dressing assets

Inspect the installed mod read-only and report:

- installed version
- plugin name(s), if any
- exact mod folder/path in MO2
- list of meshes and textures that look useful for Skyrim Fair
- for each promising asset, its exact relative file path
- whether that asset appears to be authored directly by JJerem or comes from another credited source
- any bundled credits/readme/permissions information relevant to redistribution
- whether the asset can safely be referenced as an external dependency instead of copied into Skyrim Fair
- if plugin records already place or reference those assets, provide useful EditorIDs/FormKeys where safe to do so

Pay special attention to provenance. Medieval Markets credits third-party assets from authors including PraedythXVI, Brumbek, gooball60, and JPSteel2. Do not assume JJerem's permission covers those files.

Do not modify, repack, redistribute, or copy any Medieval Markets assets during this audit.

## Output format

Return a concise report under these headings:

1. Toolchain
2. Skyrim install
3. Animation stack
4. Exterior conflict notes
5. Recommended first test cell
6. Recommended vanilla test object
7. Professional Dancer integration notes
8. Medieval Markets asset audit
9. Unknowns / blockers

Include exact paths and identifiers where relevant. Do not guess missing values.
