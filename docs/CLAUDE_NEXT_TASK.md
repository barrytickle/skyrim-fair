# Claude next task: prepare the first visible fair build

Work from the current `feat/bootstrap-generator` branch.

## Goal

Produce the exact local information needed for ChatGPT to make the next code commit that places the first visible fair assets and map marker.

Do not change Skyrim, MO2, the active load order, Pandora output, or any installed mod files. Repository build output under `dist/` is fine.

## 1. Verify the generator now that .NET 10 SDK is installed

From the repository root run:

```powershell
dotnet --info
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Report:

- exact .NET SDK version detected
- restore/build result
- whether `dist/SkyrimFair.esp` is produced
- generated ESP size in bytes
- all warnings/errors, if any

Do not install the ESP into MO2 yet.

## 2. Reconfirm the first test site

Use the previous audit result as the starting point:

- Tamriel exterior cell grid: `2, -2`
- cell FormKey: `000095FE:Skyrim.esm`
- proposed placement: `X 10880, Y -7552, Z -4616`
- vanilla test object: `SMarketStall01`
- test object FormKey: `00064B87:Skyrim.esm`

Reconfirm that none of the newly installed resource mods changed this cell or override `SMarketStall01`.

Do not redo the whole worldspace audit unless something has changed.

## 3. Inspect Medieval Markets

Identify the installed Medieval Markets mod and produce a shortlist of useful fair assets.

For each shortlisted asset report:

- exact relative mesh path
- exact relative texture path(s), where practical
- plugin record EditorID/FormKey if the installed plugin exposes one
- plain-English description of what it is
- likely use in Skyrim Fair
- provenance: JJerem-authored or third-party source
- redistribution status if clearly documented
- whether dependency-only use would avoid redistribution concerns

Prioritise:

- market stalls
- tents/canopies
- shelving
- tables
- baskets
- produce/display clutter
- anything that would make a fair lane look populated

Do not copy or modify the assets.

## 4. Inspect the newly installed banner resource

Barry has installed a banner resource after the previous audit.

First identify the exact mod from the local MO2 folders / metadata. Do not guess its title, author, or Nexus ID.

Report:

- exact mod name
- author
- installed version
- Nexus/mod source identifier if locally available
- plugin name(s), if any
- exact MO2 mod folder
- bundled licence / permissions / credits documentation

Then shortlist the best assets for a fairground.

For each shortlisted banner/bunting asset report:

- mesh path
- texture path(s)
- EditorID/FormKey if exposed by a plugin
- visual description inferred from file/record naming
- attribution/provenance
- redistribution conditions if clearly documented

Prioritise:

- strings of bunting
- colourful hanging banners
- entrance banners
- stage decorations
- generic medieval/festival designs rather than hold-specific heraldry

Do not copy or modify the assets.

## 5. Find the vanilla map-marker records we need

Inspect Skyrim.esm and identify the exact record information needed to create a new exterior map marker programmatically.

Report:

- base `MapMarker` placed-object/base-object FormKey used by vanilla exterior markers
- relevant record fields controlling marker name
- icon/type options that would make sense for a fair, camp, settlement, or miscellaneous location
- fields/flags controlling visibility, discovery, and fast travel
- one nearby vanilla map marker record as a known-good structural example

Do not modify any plugin.

## 6. Output

Return one concise report with these headings:

1. Generator verification
2. Test-site reconfirmation
3. Medieval Markets shortlist
4. Banner resource identification and shortlist
5. Vanilla map-marker implementation notes
6. Blockers / uncertainties

For asset shortlists, use tables where possible.

Do not guess. If provenance, permissions, a FormKey, or a file relationship cannot be confirmed locally, mark it as unknown.
