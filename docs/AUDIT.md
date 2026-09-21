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
- `dotnet build -c Release`: passing, **0 warnings, 0 errors** (`TreatWarningsAsErrors` is on)
- generator run: passing
- Mutagen.Bethesda.Skyrim: 0.54.4 (latest on nuget.org)

### Generated plugin — first visible build

- output: `dist/SkyrimFair.esp`
- size: **47,039 bytes**
- sha256: `66e9f53788ce023501e4bd6b73666c141a9e87075a1d492e1363c961cd2d5bd9`
- masters: **`Skyrim.esm`** (single master, derived from FormKeys, no hardcoded index)
- TES4 Author (`CNAM`): **`BarryRim Event Planner`** — confirmed present in the written ESP
- not ESL-flagged
- HEDR: form version 1.71, next object ID `0x802`
- records: **1 WRLD, 2 CELL, 2 REFR** (5 records; HEDR counts 10 including groups)
- no navmesh, LAND, NPC, quest, script, package, music or animation records

#### FormKeys allocated by Skyrim Fair

| FormKey | EditorID | What it is |
| --- | --- | --- |
| `000800:SkyrimFair.esp` | `FairSiteMapMarker` | map marker "The Wanderer's Fair" |
| `000801:SkyrimFair.esp` | `FairTestMarketStall` | placed `SMarketStall01` |

#### Verified structure

Verified by parsing the written ESP independently of Mutagen:

- Tamriel worldspace override `0000003C:Skyrim.esm`
- exterior block `(0, -1)` / sub-block `(0, -1)` — matches Skyrim.esm exactly
- cell override `000095FE:Skyrim.esm`, grid `(2, -2)`
- stall REFR in the **temporary** child group, base `00064B87:Skyrim.esm`, position `(10880, -7552, -4616)`, rotation `(0, 0, 0)`
- persistent cell override `00000D74:Skyrim.esm`, grid `(0, 0)`
- marker REFR in the **persistent** child group, base `00000010:Skyrim.esm`, `XMRK` present
- marker `FULL` = `The Wanderer's Fair`
- marker `FNAM` = `0x03` (Visible + CanTravelTo)
- marker `TNAM` = `0x02` (town/village icon — Mutagen `MarkerType.Town`)

### MO2 test deployment

Prepared for Barry to enable manually. **Nothing has been enabled or launched.**

- destination: `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp`
- the mod folder **did not exist** and was created by this pass
- folder contents: `SkyrimFair.esp` only — **no `meta.ini` or other metadata was added**, because MO2 does not require any to recognise a folder in `mods\` (it writes its own metadata on refresh)
- deployed size: **47,039 bytes**
- deployed sha256: `66e9f53788ce023501e4bd6b73666c141a9e87075a1d492e1363c961cd2d5bd9`
- **byte-identical** to `dist/SkyrimFair.esp`, confirmed by matching sha256 and by `cmp`

Confirmed **not** done:

- mod not enabled in MO2's left pane
- plugin not enabled in the right-hand plugin pane
- load order not reordered
- `modlist.txt`, `plugins.txt` and `loadorder.txt` are byte-for-byte unchanged (sha256 captured before and after the copy, identical both times)
- `stock\Data` untouched; no existing mod, Pandora output or save touched
- Skyrim and SKSE not launched; DynDOLOD/Occlusion not run

Note on MO2 behaviour: when MO2 next refreshes it will discover the new folder and add it to the active profile's `modlist.txt` itself. Depending on MO2's settings a newly discovered mod can be added **enabled**, and a newly discovered plugin can be added to `plugins.txt` enabled. Barry should confirm the intended enabled/disabled state in both panes after refreshing rather than assuming it arrives disabled.

### Generator design notes

- All site data lives in `fair.config.json` under `site`; no coordinates or FormKeys in implementation code.
- The plugin author string lives in `identity.author` and is written to the TES4 header as `CNAM`.
- FormKeys are accepted in the eight-digit project notation (`00064B87:Skyrim.esm`) and normalised to Mutagen's six-digit form.
- Exterior block / sub-block numbers are derived by floor division (32 and 8), verified against Skyrim.esm.
- `MarkerType.Town` (raw `TNAM 0x02`) is used, not Mutagen's `Settlement`. Vanilla uses `0x02` for Riverwood, Rorikstead and Shor's Stone; Mutagen's `Settlement` (`0x03`) is what Honningbrew Meadery and Goldenglow Estate use, so the name is misleading.

### Overriding vanilla WRLD/CELL records

Overriding a WRLD or CELL replaces the record wholesale — anything omitted is lost in game. A first attempt wrote stub records and would have stripped `XCLR` (regions), `XCLW` (water height), `TVDT`/`MHDT` and the worldspace map data.

The generator now reads `Skyrim.esm` **read-only** and deep-copies the real records, excluding only the child collections it rebuilds:

- `Worldspace`: excludes `SubCells`, `TopCell`, `LargeReferences`
- `Cell`: excludes `Persistent`, `Temporary`, `Landscape`, `NavigationMeshes`

Confirmed byte-for-byte: our `CELL 000095FE` and `CELL 00000D74` overrides now carry the identical subrecord set to vanilla. The only WRLD differences are intentional:

- `RNAM` (large references) is dropped. The Creation Kit omits it on every WRLD override, and so does every mod in the audited load order (USSEP, SLaWF, Jobs, Butterflies, Holidays, Missives, GreatWarSkyrim). **DynDOLOD.esm is the one plugin that carries `RNAM`** and regenerates it from a later load-order slot; carrying ~1.4MB of stale `RNAM` would clobber it.
- `FULL` is 7 bytes rather than vanilla's 4. Skyrim.esm is localized so its `FULL` is a string-table ID; our plugin is not localized so it stores the literal string. Same value.

Config field: `site.skyrimDataPath` (currently `E:\Modlists\Still In Skyrim\stock\Data`), overridable by the `SKYRIM_DATA_PATH` environment variable. If neither resolves, the generator still emits a structurally valid plugin and prints a loud warning — that fallback output is **not safe to load in game**. CI only builds and never runs the generator, so the local path does not affect it.

### Skyrim install

- Mod manager: Mod Organizer 2 v2.5.2, portable instance at `E:\Modlists\Still In Skyrim`
- Install dir: `E:\Modlists\Still In Skyrim\stock`
- Data dir: `E:\Modlists\Still In Skyrim\stock\Data`
- Runtime: 1.6.1170.0 (Skyrim SE/AE, Steam)
- Active profile: `Still in Skyrim Plus`
- `plugins.txt`: `E:\Modlists\Still In Skyrim\profiles\Still in Skyrim Plus\plugins.txt`
- Active plugins: 425
- SKSE 2.2.6 (matches runtime), Address Library 11.0.0
- Pandora Behaviour Engine+ is the active behaviour generator (output mod v4.3.0); Open Animation Replacer 3.1.6.0; no FNIS or Nemesis installed

### First test site

- Worldspace: Tamriel
- Cell grid: `2, -2`
- Cell FormKey: `000095FE:Skyrim.esm`
- Placement: `X 10880, Y -7552, Z -4616`
- Terrain relief across the surrounding 512x512 patch: 24 units; nearest vanilla reference 1,090 units away
- Only two plugins in the load order touch this cell: `Landscape and Water Fixes.esp` (LAND, height-identical to vanilla) and `Occlusion.esp` (cell record). No navmesh edits, no REFR conflicts.
- Nearest vanilla map marker: `WhiterunStablesMapMarker` (`00072879:Skyrim.esm`), 8,059 units away

### First vanilla test object

- EditorID: `SMarketStall01`
- Type: `STAT`
- FormKey: `00064B87:Skyrim.esm`
- Model: `Architecture\Solitude\Clutter\SMarketStall01.nif`
- No override in the active load order

### Medieval Markets

- Archive present at `external/`, **not installed into MO2**
- Nexus mod ID: 161479; archive filename says 1.1.1, bundled `meta.ini` says 1.1.0.0
- Plugin: `Medieval Markets.esp` (ESL-flagged)
- Edits **zero** Tamriel exterior cells (city worldspaces only), and does **not** override `SMarketStall01` — installing it will not disturb the test site
- Permissions: JJerem's assets reusable with credit, no selling; third-party assets follow their original permissions
- JJerem-authored (per his own description): market stall tent, stick shelving, slanted table stand, basket powder mound
- Third-party credited: PraedythXVI (Fruits and Veggies), Brumbek (SMIM), gooball60 (Palpable Baskets), JPSteel2 (Northern Roads tent + textures)
- Preferred strategy: dependency-only asset referencing, no redistribution

### Banner resource

- `Incaendo's Banner Resource 2`, Nexus mod ID 94919, version 1.0
- Archive present at `external/`, **not installed into MO2**
- Pure resource: no plugin, no readme, no licence file
- 163 mesh/texture pairs under `Data\Meshes\Incaendo\Banners\` and `Data\Textures\Incaendo\Banners\`
- All textures are taller than wide: 146 at 1024x2048, 17 at 256x2048. **No bunting.**
- Author name inferred from paths, not from bundled documentation
- Redistribution terms unconfirmed; dependency-only referencing preferred

### Professional Dancer

- Version 1.5.0, Nexus 124608; archive present at `external/`, not installed
- `Dance.esp`, ESL-flagged; masters `Skyrim.esm`, `SkyUI_SE.esp`, `UIExtensions.esp` (all three already active)
- `DanceQuest` = `000802:Dance.esp`; `DancePower` = `000800:Dance.esp`
- Callable API on `DanceQuestScript`: `StartDanceOnTarget`, `StopTargetDance`, `StopAllTargetDances`, `PlayAnimationOnActor`, `IsNPCDancing`
- Mod-event channel: `PlayCustomDance_<actorFormID>` / `StopCustomDance_<actorFormID>`
- Lowest-level trigger: `Debug.SendAnimationEvent(actor, "Dance1")` through `"Dance14"`; stop with `"IdleForceDefaultState"`
- 15 `.hkx` registered via FNIS-format list; Pandora consumes it, so no FNIS/Nemesis install needed

### Map marker implementation

- Base object: `MapMarker`, `00000010:Skyrim.esm` (a `STAT`)
- Markers live in Tamriel's persistent cell `00000D74:Skyrim.esm`, not the grid cell they sit over
- `XMRK` presence is what makes a REFR a map marker
- `FULL`: display name. `TNAM`: icon type. `FNAM`: flags. `DATA`: position + rotation
- `FNAM` bit `0x01` = Visible, bit `0x02` = Can Travel To. Vanilla: 332 markers at `0x00`, 4 at `0x01`, 11 at `0x03`
- Useful icon types: `0x02` town/village, `0x0D` farm (incl. `MerryFairMapMarker`), `0x05` camp, `0x1E` shack, `0x15` stable

## Current blockers / cautions

- The prototype marker is Visible + Can Travel To from game start. That is deliberate for testing and should become `FNAM 0x00` (discover-on-approach) before release.
- `SkyrimFair.esp` is staged in MO2 but has **never been enabled or loaded in game**. Nothing here is in-game verified — only structurally verified.
- The stall's Z is terrain height. `SMarketStall01`'s mesh origin may need a small offset once seen in game; the NIF bounding box was not read.
- Generating without `skyrimDataPath` produces a plugin that must not be loaded (see above).
- Medieval Markets and the banner resource are archives, not live MO2 mods.
- Incaendo banner redistribution permissions are unconfirmed; Medieval Markets has mixed third-party provenance.
- Permanent exterior placements will eventually require DynDOLOD/Occlusion regeneration.
- `external/` holds large third-party mod archives and is git-ignored. It must never be committed.

## Next local verification

The next local pass should verify whatever ChatGPT most recently changed in the generator, confirm the generated plugin structure, and update this file with any new exact paths, FormKeys, asset provenance, integration findings, warnings, or blockers.

The plugin is now staged at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\`. The next step is Barry's: refresh MO2, enable the `Skyrim Fair` mod and its plugin last in the load order, then confirm in game that "The Wanderer's Fair" appears on the map and that the market stall is standing on the ground at the test site.

Things worth reporting back from that run: whether the marker is where expected, whether the stall clips into or floats above the terrain, and whether cell `2, -2` still behaves normally (weather, ambient sound, water) given the worldspace and cell overrides.
