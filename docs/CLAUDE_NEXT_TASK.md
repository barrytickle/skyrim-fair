# Claude next task: first visible Skyrim Fair build

Work from the latest `feat/bootstrap-generator` branch.

Read `docs/AUDIT.md` first. Treat it as the current source of truth for Barry's local setup.

## Goal

Upgrade the C# / Mutagen generator from the current header-only ESP into the first **visible in-game proof** of Skyrim Fair.

This milestone should generate an ESP containing:

1. one vanilla market stall at the audited fair test site, and
2. a temporary map marker named **The Wanderer's Fair** so Barry can reach the site easily during testing.

Do not install or enable the ESP in MO2 yet. Do not modify Skyrim, MO2, the load order, Pandora output, or any installed mod files.

## Known-good audited data

### Test exterior cell

- Worldspace: Tamriel
- cell grid: `2, -2`
- cell FormKey: `000095FE:Skyrim.esm`
- placement: `X 10880, Y -7552, Z -4616`

### Test object

- EditorID: `SMarketStall01`
- type: `STAT`
- FormKey: `00064B87:Skyrim.esm`

### Map marker

- base object: `MapMarker`
- FormKey: `00000010:Skyrim.esm`
- display name: `The Wanderer's Fair`
- place the marker in Tamriel's persistent cell, as vanilla exterior markers are structured
- for this prototype, make it visible and fast-travelable immediately so Barry can reach the test site easily
- use the village / settlement-style marker icon identified in the audit unless Mutagen exposes a clearer typed equivalent

## Implementation requirements

- Use Mutagen APIs, not binary patching or hand-written ESP bytes.
- Reference records with `FormKey` / typed FormLinks. Never hardcode a load-order index.
- Ensure `Skyrim.esm` is present as a master in the generated plugin.
- Preserve the existing config-driven generator structure where practical.
- Prefer adding explicit prototype-site configuration fields over scattering raw coordinates through implementation code.
- Keep this milestone minimal. Do not add Medieval Markets, banner assets, dancers, music, NPCs, navmesh, LAND edits, scripts, packages, or DynDOLOD work yet.
- Do not write into Skyrim's `Data` directory.
- Output remains `dist/SkyrimFair.esp`.

## Verification

Run:

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Then inspect the generated ESP read-only and verify:

- it parses successfully
- `Skyrim.esm` is a master
- it contains a placed reference whose base is `00064B87:Skyrim.esm`
- that reference resolves under Tamriel cell `000095FE:Skyrim.esm`
- its position is the intended test-site coordinate
- it contains a map-marker reference using base `00000010:Skyrim.esm`
- the marker name is `The Wanderer's Fair`
- the marker is visible + fast-travelable for the prototype
- no navmesh, LAND, NPC, quest, script, package, music, or animation records were accidentally added

If Mutagen requires a different structural approach for exterior-cell overrides or persistent worldspace references, use the correct Mutagen-native approach and document it. Do not fake the result merely to satisfy the expected shape.

## Git workflow

You may edit the generator/config/tests/docs required for this milestone.

When complete:

1. overwrite `docs/AUDIT.md` with the latest verified local state,
2. include the generated ESP size, record counts, exact new FormKeys allocated by Skyrim Fair, build result, and any caveats,
3. commit all code changes and the refreshed audit,
4. push them to `feat/bootstrap-generator`,
5. do not merge the PR,
6. tell Barry that the first visible build has been pushed, or clearly report the blocker if it cannot be completed.

Suggested final commit message:

```text
feat: generate first visible fair site
```

The Git history is the audit history. `docs/AUDIT.md` must describe only the latest known state.
