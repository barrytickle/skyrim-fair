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

## Current status

### Toolchain

- .NET SDK: 10.0.401
- `dotnet restore`: passing
- `dotnet build -c Release`: passing
- generator run: passing
- generated plugin: `dist/SkyrimFair.esp`
- current generated plugin size: 42 bytes
- current plugin is header-only with zero records and zero masters

### First test site

- Worldspace: Tamriel
- Cell grid: `2, -2`
- Cell FormKey: `000095FE:Skyrim.esm`
- Proposed placement: `X 10880, Y -7552, Z -4616`
- Site remains suitable and has no navmesh edits from the audited load order

### First vanilla test object

- EditorID: `SMarketStall01`
- Type: `STAT`
- FormKey: `00064B87:Skyrim.esm`
- No active plugin override found

### Medieval Markets

- Archive present locally but not installed into MO2
- Nexus mod ID: 161479
- Plugin: `Medieval Markets.esp` (ESL-flagged)
- Preferred strategy: dependency-only asset referencing, no redistribution
- Strong candidates include JJerem-authored stall/shelving/table assets
- Third-party provenance remains mixed for some baskets, crates, tents and textures

### Banner resource

- Identified locally as `Incaendo's Banner Resource 2`
- Nexus mod ID: 94919
- Version: 1.0
- Pure resource, no plugin
- 163 mesh/texture pairs
- No bundled licence/permissions file
- No bunting identified
- Redistribution terms still need external confirmation
- Dependency-only referencing is preferred until permissions are confirmed

### Professional Dancer

- Version audited: 1.5.0
- Not yet installed into active MO2 load order
- Papyrus source exposes callable dance functions
- Dance animations can also be triggered with animation events such as `Dance1` through `Dance14`
- Pandora is the active behaviour generator and can consume the FNIS-format list used by the mod

### Map marker implementation

- Base object: `MapMarker`
- FormKey: `00000010:Skyrim.esm`
- Marker should be placed in the Tamriel persistent cell
- `FULL`: display name
- `TNAM`: marker icon/type
- `FNAM`: visibility / fast-travel flags
- `FNAM 0x00`: hidden until discovered
- `FNAM 0x03`: visible and immediately fast-travelable
- village/settlement icon type is currently the leading fit for The Wanderer's Fair

## Current blockers / cautions

- The next generated plugin must add `Skyrim.esm` as a master before placing records
- Medieval Markets and the banner resource are currently archives, not live MO2 mods
- Incaendo banner redistribution permissions are not confirmed
- Medieval Markets has mixed third-party provenance
- permanent exterior placements will eventually require DynDOLOD/Occlusion regeneration

## Next local verification

The next local pass should verify whatever ChatGPT most recently changed in the generator, confirm the generated plugin structure, and update this file with any new exact paths, FormKeys, asset provenance, integration findings, warnings, or blockers.
