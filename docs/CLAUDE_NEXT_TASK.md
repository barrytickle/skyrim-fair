# Claude next task: move the prototype to Site 1 and fix fast travel

Work from the latest `feat/bootstrap-generator` branch.

Read `docs/AUDIT.md` and `docs/DESIGN.md` first. Treat them as the current source of truth.

Barry has approved Plan B in principle: the final fair should use a purpose-built, perfectly flat paved market core blended into the tundra rather than forcing stalls onto sloped natural terrain.

This task does **not** build the full platform yet. First prove the corrected marker and new site in game.

## Goal

Produce the next test build with:

1. the fast-travel marker fixed,
2. the fair marker changed to Skyrim's vanilla `Pass` icon,
3. the prototype marker and stall moved to the recommended Site 1,
4. no platform, navmesh, LAND edits, NPCs or fair layout yet.

## 1. Fix the map marker

Apply the proven marker fix from `docs/AUDIT.md`.

Required:

- set the marker REFR raw major-record flags to include `Persistent` = `0x400`
- add `XLRT` using `MapMarkerRefType` = `0010F63C:Skyrim.esm`
- add `XRDS` radius, using the audited conventional value unless there is a stronger nearby vanilla precedent
- keep the marker in Tamriel's persistent cell
- keep it visible + fast-travelable for prototype testing
- change marker type from `Town` to `Pass`
- preserve the name `The Wanderer's Fair`

After generation, verify the written ESP carries:

- raw flags containing `0x400`
- `XLRT`
- `XRDS`
- `TNAM 0x18` / Mutagen `MarkerType.Pass`

## 2. Move the prototype to Site 1

Use the recommended audited Site 1:

- fair centre: `X -5632, Y -12800`
- test elevation: `Z -5672`
- broader area spans cells around `-2,-4 / -2,-3 / -1,-4 / -1,-3`
- no LAND edits
- no navmesh edits
- no scripted / dragon / civil-war content in the target footprint

Update config rather than scattering coordinates through implementation code.

Move both:

- `FairSiteMapMarker`
- `FairTestMarketStall`

to the new site.

Do not create the cobblestone platform yet.

## 3. Preserve the approved Plan B direction

The final market core is expected to be approximately `3072 x 3072` and **perfectly flat**.

Design requirements already approved:

- paved / cobblestone appearance
- not a giant exposed rectangular block
- floor should visually merge into Skyrim's terrain
- high edge can meet grade naturally
- low edge should become the intentional main entrance approach
- use ramps, stone retaining details, rocks, grass, hay, fences, shrubs and stall placement to hide transitions
- archery, stables and future jousting can remain on natural ground outside the paved core
- avoid LAND edits if possible

The current preferred technical direction is a **small custom project-owned tile kit**, likely:

- repeatable floor tile
- edge piece
- corner piece
- ramp / transition piece

Do not model or generate those assets in this task. The point of this test is to confirm Site 1 and fast travel before spending time on the platform.

## 4. Rebuild and deploy the test ESP

Run:

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Verify the generated ESP structurally.

Then copy the new ESP into:

```text
E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp
```

Replacing only the existing Skyrim Fair test ESP is allowed.

Do not enable/disable other mods, reorder other plugins, launch Skyrim, touch saves, modify Pandora output, or run DynDOLOD/Occlusion.

## 5. Load-order warning

The audit found `SkyrimFair.esp` currently loads after `DynDOLOD.esp` and `Occlusion.esp`.

Do not reorder Barry's load order automatically.

Record clearly in `docs/AUDIT.md` that Barry should move `SkyrimFair.esp` **before both `DynDOLOD.esp` and `Occlusion.esp`** before the next in-game test.

## 6. Verification

After generating the plugin, verify:

- `Skyrim.esm` remains the only master
- TES4 author remains `BarryRim Event Planner`
- map marker uses `Pass`
- map marker has `Persistent` flag
- `XLRT` and `XRDS` are present
- marker and stall are at Site 1
- no LAND records
- no NAVM records
- no NPC / quest / script / package / music / animation records
- deployed ESP is byte-identical to generated ESP

## 7. Audit and Git workflow

When complete:

1. overwrite `docs/AUDIT.md`,
2. record exact new coordinates, cell structure, marker flags/subrecords, file size/hash and deployment hash,
3. keep the Plan B platform recommendation in the audit,
4. note that the next user test must confirm:
   - Pass icon displays correctly
   - fast travel now moves Barry to Site 1
   - Site 1 looks appropriate as the landscape surrounding a future raised/levelled market platform
5. commit and push to `feat/bootstrap-generator`,
6. do not merge the PR.

Suggested commit message:

```text
feat: move prototype and fix fair marker
```

The Git history is the audit history. `docs/AUDIT.md` should describe only the latest known state.
