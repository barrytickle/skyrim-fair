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

## What changed in this pass

The prototype has been moved to Site 1 and the fast-travel fix implemented. **Not yet tested in game.**

1. Map marker now carries the `Persistent` record flag (`0x400`) — the proven cause of the fast-travel failure.
2. Marker icon changed from `Town` to vanilla **`Pass`** (`TNAM 0x18`), per `docs/DESIGN.md`.
3. Marker gained `XLRT` (`MapMarkerRefType`) and `XRDS` (radius 1800).
4. Marker and stall moved from cell `2,-2` to **Site 1** in cell `-2,-4`.
5. No platform, navmesh, LAND edits, NPCs or fair layout were added.

## Generated plugin — current state

| Field | Value |
| --- | --- |
| Output | `dist/SkyrimFair.esp` |
| Size | **46,997 bytes** |
| sha256 | `2fb5379e10d05c9fffef4ba761ae62013edab8672930e4277fc0c3d64800097e` |
| Masters | **`Skyrim.esm` only** (derived from FormKeys, no hardcoded index) |
| TES4 Author (`CNAM`) | `BarryRim Event Planner` |
| HEDR | form version 1.71, next object ID `0x802` |
| Records | **1 WRLD, 2 CELL, 2 REFR** |
| Excluded | no LAND, NAVM, NPC, quest, script, package, music or animation records |

| FormKey | EditorID | What it is |
| --- | --- | --- |
| `000800:SkyrimFair.esp` | `FairSiteMapMarker` | map marker "The Wanderer's Fair" |
| `000801:SkyrimFair.esp` | `FairTestMarketStall` | placed `SMarketStall01` (`00064B87:Skyrim.esm`) |

### Site 1 placement

| Field | Value |
| --- | --- |
| Worldspace | Tamriel `0000003C:Skyrim.esm` |
| Exterior cell | **`00009A28:Skyrim.esm`** |
| Cell grid | **`-2, -4`** |
| Exterior block / sub-block | `-1, -1` / `-1, -1` (verified against Skyrim.esm) |
| Marker + stall position | **`X -5632, Y -12800, Z -5720`** |
| Persistent cell (marker) | `00000D74:Skyrim.esm`, grid `0, 0` |

**Z deviation from the task brief, deliberate.** The brief specified test elevation `Z -5672`. That figure came from the previous audit's *platform* analysis — it is the terrain **maximum** across the 3072-unit core, i.e. the future platform floor. Native terrain at exactly `(-5632, -12800)` is **`-5720`**, so placing the prototype stall at `-5672` would leave it floating 48 units (~0.7 m) above the ground and would read as a bug during the retest. The prototype therefore sits on native ground at `-5720`, and `-5672` is preserved in config as `site.plannedPlatformFloorZ` for the platform milestone. Nothing is lost.

Terrain context at Site 1: 2048 core min `-5792` / max `-5696` / mean `-5728`; 3072 core min `-5832` / max `-5672` / mean `-5734`.

### Marker record — verified as written

```text
REFR 000800:SkyrimFair.esp  EDID=FairSiteMapMarker
  raw record flags : 0x00000400   (Persistent)
  child group      : 8 (cell persistent children)
  subrecords       : EDID, NAME, XRDS, XLRT, XMRK, FNAM, FULL, TNAM, DATA
  NAME  000010:Skyrim.esm   (MapMarker base STAT)
  XRDS  1800.0
  XLRT  0010F63C:Skyrim.esm (MapMarkerRefType)
  FNAM  0x03                (Visible + CanTravelTo)
  FULL  "The Wanderer's Fair"
  TNAM  0x18                (Pass)
  DATA  pos (-5632, -12800, -5720) rot (0, 0, 0)
```

That subrecord order is **identical to `WhiterunStablesMapMarker`**.

`XRDS 1800.0` follows `WhiterunWatchtowerMapMarker` (also 1800.0), the nearest vanilla marker to Site 1, rather than the global modal value of 1250.0. 1800 also suits a 3072-unit market core. Nearby precedents: Western Watchtower 1800.0, Whiterun Stables 1500.0, Fort Greymoor 3400.0.

`XLKR` (linked reference) is still omitted. 341 of 347 vanilla markers have one, but it links to an arbitrary nearby reference and cannot be generated meaningfully yet. It is not required for fast travel.

### Verification performed

22 structural checks, all passing, by parsing the written ESP independently of Mutagen:

- `Skyrim.esm` is the only master; author is `BarryRim Event Planner`
- exactly one map marker and one placed stall
- marker: `Persistent` flag set, `TNAM 0x18`, `FNAM 0x03`, `XLRT 10F63C`, `XRDS 1800`, name preserved, in persistent group, at Site 1
- stall: base `00064B87:Skyrim.esm`, at Site 1, in temporary group, parent cell grid `-2,-4`
- cells `00009A28` and `00000D74` present; worldspace is Tamriel; block/sub-block `(-1,-1)`
- no LAND / NAVM / NPC / quest / script / package / music / animation records
- only WRLD, CELL and REFR record types

Override fidelity, diffed against `Skyrim.esm`:

| Record | Result |
| --- | --- |
| `CELL 00009A28` (Site 1) | **identical to vanilla** — same subrecord kinds, same values |
| `CELL 00000D74` (persistent) | **identical to vanilla** |
| `WRLD 0000003C` (Tamriel) | identical except `FULL` (localized string ID vs literal string — same value) and `RNAM`, deliberately dropped |

## MO2 deployment

| Field | Value |
| --- | --- |
| Destination | `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Action | **replaced the existing test ESP only** |
| Deployed size | 46,997 bytes |
| Deployed sha256 | `2fb5379e10d05c9fffef4ba761ae62013edab8672930e4277fc0c3d64800097e` |
| Byte-identical to `dist` | **yes** — matching sha256 and `cmp` reports no differences |
| Folder contents | `SkyrimFair.esp` only, no metadata files added |

Confirmed not done: no mods enabled or disabled, no plugins reordered, no profile edits, Skyrim and SKSE not launched, no saves touched, Pandora output untouched, DynDOLOD/Occlusion not run. `modlist.txt`, `plugins.txt` and `loadorder.txt` were hashed before and after the copy and are **byte-for-byte unchanged**.

### Load order — previous warning resolved

The previous audit warned that `SkyrimFair.esp` was loading after `DynDOLOD.esp` and `Occlusion.esp`, letting our Tamriel `WRLD`/`CELL` overrides beat their occlusion and LOD data.

**Barry has already moved it.** Verified current positions in the active `plugins.txt`:

| Position | Plugin |
| --- | --- |
| 82 | `*SkyrimFair.esp` |
| 416 | `*DynDOLOD.esp` |
| 417 | `*Occlusion.esp` |

`SkyrimFair.esp` is now correctly before both. No further action needed; it should stay in that region of the order as the fair grows.

## Next in-game test — what Barry needs to confirm

1. **The `Pass` icon displays correctly** on the world map for "The Wanderer's Fair".
2. **Fast travel now moves the player to Site 1** rather than returning them to their current location. This is the primary test of the `Persistent` flag fix.
3. **Site 1 reads appropriately as the landscape surrounding a future raised/levelled market platform** — open tundra, correct character, nothing awkward in the immediate surroundings.
4. Secondary: whether the stall sits cleanly on the ground at `Z -5720`, which validates the terrain-height figure the platform work will build on.

If fast travel still fails after this build, the remaining untested hypothesis is that the arrival point must be navmesh-accessible. Site 1 sits on vanilla-navmeshed tundra with no navmesh edits in any spanned cell, so this is unlikely, but it cannot be ruled out without the in-game result.

## Plan B — flat fairground platform (approved direction, not yet built)

Barry has approved Plan B in principle. Natural terrain cannot provide a genuinely level market core: the flattest clean 3072 x 3072 site anywhere near the Western Watchtower is 160 units of relief with a 48-unit maximum step per 128 units — roughly a 21-degree local slope at worst. The design calls for generated, aligned rows of stalls, which need a level floor.

### Recommended platform

**Site 1, 3072 x 3072 paved core, floor at `Z -5672`** — the terrain maximum across the core, so the platform is **pure fill with zero cut** and needs **no LAND edits**.

| Field | Value |
| --- | --- |
| Max fill (floor above terrain) | **160u** |
| Max cut | **0u** |
| Perimeter step to native ground | 0u on the high edge to 160u on the low edge |
| At a 2048 core instead | only 96u of fill |

The low edge should become the intentional main entrance approach: 160u over a 1:8 ramp is about 1,280 units of sloped approach. The high edge meets grade with essentially no treatment. Side edges taper between the two and suit stepped retaining, rocks, hay, shrubs, fences and stall placement rather than ramps — matching the layered perimeter treatment in `docs/DESIGN.md`.

### Why Site 1

| Field | Value |
| --- | --- |
| Cells spanned by a 3072 footprint | `-2,-4` (`00009A28`), `-2,-3` (`00009A07`), `-1,-4` (`00009A27`), `-1,-3` (`00009A06`) |
| Relief 2048 / 3072 / 4096 | 96u / 160u / 416u |
| Max step per 128u | 24u (2048), 48u (3072) — smoothest gradient found in the whole search box |
| Distance from Western Watchtower | 7,535u (1.8 cells) |
| Nearest map markers | Western Watchtower 7,535u; Fort Greymoor 9,219u |
| Winning LAND | **`Skyrim.esm` — vanilla, no mod height edits** |
| Navmesh edits | **none in any spanned cell** |
| Placed-reference conflicts | USSEP (8 + 9), Butterflies (4 + 3), SLaWF (1 + 1) — all low, all ambient clutter |
| Civil-war / dragon / quest / settlement / farm content | **none** |
| Vanilla references in footprint | 18 — 11 tundra shrubs, 4 critter markers, `RockTundraLand01Tundra01` x1, `RockShelf01FieldGrass01` x1, `TreeThicket01` x1 |

Rejected alternatives: `(768, -11520)` was flatter at 2048 (80u) and closer to the tower (3,302u), but a 3072 footprint clips `WhiterunWatchtowerExterior` (`0,-4`), the Mirmulnir dragon-fight cell from MQ104. `(10240, -10752)` is closest to Whiterun but least flat (256u at 2048) and pine-covered.

### Paving assets

**Vanilla Skyrim has no generic cobblestone paving tile.** Whiterun's streets and plaza are baked into its `WRTerrain` architecture meshes. A search of all `STAT` records for paving, plaza, street, cobble, courtyard and floor patterns returned 119 records, none of which is a plain repeatable exterior paving tile.

Nordic exterior cut-stone platform kit — a genuine modular exterior set with floor, edges and corners (62 records including snow/ice variants). Reads as ancient ruin masonry, arguably too monumental for a temporary handmade market:

| FormKey | EditorID | Model |
| --- | --- | --- |
| `001044CB:Skyrim.esm` | `NorTmpExtPlatFloorRaised01CutStone` | `Dungeons\Nordic\Exterior\NorTmpExtPlatFloorRaised01Stone.nif` |
| `00028A62:Skyrim.esm` | `NorTmpExtPlatFloorRaised01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatFloorRaised01.nif` |
| `00026F7B:Skyrim.esm` | `NorTmpExtPlatCorOut01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorOut01.nif` |
| `00026F79:Skyrim.esm` | `NorTmpExtPlatCorIn01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorIn01.nif` |
| `00026F7A:Skyrim.esm` | `NorTmpExtPlatCorDblEnd01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorDblEnd01.nif` |
| `0002BE41:Skyrim.esm` | `NorTmpExtPlatExSmFree01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatExSmFree01.nif` |

Whiterun terrain platforms — the plinths vanilla uses to level Whiterun's buildings against its own slope. Visually native to the tundra, but building-shaped footprints that will not tessellate into a square floor:

| FormKey | EditorID | Model |
| --- | --- | --- |
| `000506DF:Skyrim.esm` | `WRCarlottaPlatform01` | `Architecture\WhiteRun\WRTerrain\WRCarlottaPlatform01.nif` |
| `000510E2:Skyrim.esm` | `WRCommonHousePlatform01` | `Architecture\WhiteRun\WRTerrain\WRCommonHousePlatform01.nif` |
| `0005071F:Skyrim.esm` | `WRGreatHousePlatform01` | `Architecture\WhiteRun\WRTerrain\WRGreatHousePlatform01.nif` |
| `000510F7:Skyrim.esm` | `WRGreatHousePlatform02` | `Architecture\WhiteRun\WRTerrain\WRGreatHousePlatform02.nif` |
| `0005071A:Skyrim.esm` | `WRHallOfDeadPlatform01` | `Architecture\WhiteRun\WRTerrain\WRHallOfDeadPlatform01.nif` |
| `000510E0:Skyrim.esm` | `WRStairsPlatform01` | `Architecture\WhiteRun\WRTerrain\WRStairsPlatform01.nif` |
| `000506ED:Skyrim.esm` | `WRUlfberhPlatform01` | `Architecture\WhiteRun\WRTerrain\WRUlfberhPlatform01.nif` |

Edge and transition pieces:

| FormKey | EditorID | Use |
| --- | --- | --- |
| `0000099B:Skyrim.esm` | `Stonewall01` | farmhouse dry-stone wall — retaining / embankment |
| `0000099D:Skyrim.esm` | `Stonewall02` | as above, variant |
| `0000099C:Skyrim.esm` | `Stonewall01Ivy` | as above, ivy |
| `0003F93B:Skyrim.esm` | `ImpExtStairs01` | exterior stone steps |
| `000F03D3:Skyrim.esm` | `RTTemplePlazaStairs01` | wide plaza stairs |
| `0010EC69:Skyrim.esm` | `MrkDocksidePlatforms03stairs` | dockside platform stairs |

**Unverified:** exact mesh dimensions, pivot points and whether any of these tile seamlessly. NIF geometry cannot be read with the tooling used here, so tile sizes and edge alignment are **unknown** and must be checked in the Creation Kit or NifSkope before committing to a kit.

### Preferred technical direction

A **small custom project-owned tile kit** — repeatable floor tile, edge piece, corner piece, ramp/transition piece. Reasons: no vanilla repeatable paving tile exists, so a 3072 floor from vanilla pieces means hundreds of statics with visible seams, Z-fighting risk and heavy draw-call cost; one clean collision surface per tile beats hundreds of overlapping ones; and a tile kit is project-owned, which is tier 2 in the `docs/DESIGN.md` asset preference rather than a third-party dependency.

### Navmesh — now on the critical path

- Statics do **not** generate navmesh. NPCs will not path onto a raised platform without new navmesh over it.
- New navmesh must be authored and **joined to the existing exterior navmesh** at the ramp and step edges, or vendors, crowds, Garrick Tallow, Claudius Vale and the stage performers will refuse to enter and leave.
- Site 1 has **no navmesh edits in any spanned cell**, the cleanest possible starting point, and a strong argument for choosing it.
- This project's Mutagen pipeline cannot practically generate navmesh. It is Creation Kit work, and it breaks the `docs/ROADMAP.md` principle of deferring custom navmesh. **This milestone needs it.**
- A lower platform helps: at 160u of fill a generous ramp can carry navmesh smoothly.
- Interim option: place the platform visual-only and accept that NPCs cannot walk on it, purely to evaluate how the flat floor looks before committing to navmesh work.

### Compatibility: platform vs LAND edit

| Approach | Compatibility |
| --- | --- |
| **Static platform, no LAND edit** | Conflicts only with mods placing objects in the same cells. Does not fight `Landscape and Water Fixes`, `Majestic Mountains` or any landscape mod, and survives their updates. Requires navmesh work. |
| **LAND height edit (flattening)** | Directly conflicts with SLaWF and the Majestic Mountains patches, which already author `LAND` in 18 nearby cells. Needs a per-mod compatibility patch, breaks on their updates, and creates visible cell-border seams. Still needs navmesh work. |

At Site 1 specifically `LAND` is vanilla-authored with no mod overrides, so a LAND edit there would be less conflict-prone than elsewhere — but the static platform remains the more compatible choice and matches the approved direction.

## Terrain audit methodology

An earlier audit reported cell `2,-2` as "24 units relief" and called it flat. That was measured over a 512 x 512 footprint — far too small for a fairground. Same site, same heightmap:

| Footprint | Relief | Max step per 128u |
| --- | --- | --- |
| 512 x 512 | 24u | 16u |
| 1024 x 1024 | 112u | 64u |
| 2048 x 2048 | 240u | 88u |
| 3072 x 3072 | 400u | 88u |
| 4096 x 4096 | **472u** | 88u |

The 512 sample landed on a local sweet spot inside rolling tundra; relief grows roughly 20x at fair scale. That is the hilliness Barry saw in game.

**All future terrain audits must sample at the real footprint size** (2048 minimum for the market core, 3072–4096 for the whole site) and report maximum local step per 128-unit heightmap interval alongside total relief.

Method used: heightmaps decoded from `LAND` `VHGT` for a 17 x 15 cell box (x -8..8, y -12..2), 255 cells, all present. **The winning `LAND` record per cell was resolved through the full active load order**, not vanilla alone. Only 18 of 255 cells have mod-altered heights: SLaWF (12), SLaWF Majestic Mountains patch (1), SLaWF Tundra Homestead patch (2), `Embers XD.esp` (1), `Helgen Reborn.esp` (2). `MajesticMountains_Landscape.esm` provides `LAND` for 89 region cells but changes no heights.

## Map marker implementation notes

- Base object: `MapMarker`, `00000010:Skyrim.esm` (a `STAT`)
- Markers live in Tamriel's persistent cell `00000D74:Skyrim.esm`, not the grid cell they sit over
- `XMRK` presence is what makes a REFR a map marker
- **The record must carry the `Persistent` flag `0x400`.** All 347 vanilla Tamriel markers have it. Mutagen's `SkyrimMajorRecordFlag` enum does not expose it and does not derive it from `Cell.Persistent` membership, so the generator sets `MajorRecordFlagsRaw` explicitly.
- `FULL`: display name. `TNAM`: icon type. `FNAM`: flags. `DATA`: position + rotation. `XRDS`: radius. `XLRT`: location ref type.
- `FNAM` bit `0x01` = Visible, bit `0x02` = Can Travel To. Vanilla: 332 markers at `0x00`, 4 at `0x01`, 11 at `0x03`
- `XLRT` should be `MapMarkerRefType` = `0010F63C:Skyrim.esm` (333 of 347 vanilla markers)
- A linked location (`XLCN`) is **not** required — the Whiterun Stables and Western Watchtower markers both have none
- Icon types: `0x18` **Pass** (Mutagen `MarkerType.Pass`, now in use, 3 vanilla border crossings), `0x02` town/village (`MarkerType.Town` — Riverwood, Rorikstead, Shor's Stone), `0x0D` farm (incl. `MerryFairMapMarker`), `0x05` camp, `0x1E` shack, `0x15` stable
- Mutagen's `MarkerType.Settlement` is `0x03`, which vanilla uses for Honningbrew Meadery and Goldenglow Estate. The name is misleading.

## Skyrim install

- Mod manager: Mod Organizer 2 v2.5.2, portable instance at `E:\Modlists\Still In Skyrim`
- Install dir: `E:\Modlists\Still In Skyrim\stock`
- Data dir: `E:\Modlists\Still In Skyrim\stock\Data`
- Runtime: 1.6.1170.0 (Skyrim SE/AE, Steam)
- Active profile: `Still in Skyrim Plus`
- Active plugins: 426 (including `SkyrimFair.esp`)
- SKSE 2.2.6 (matches runtime), Address Library 11.0.0
- Pandora Behaviour Engine+ is the active behaviour generator (output mod v4.3.0); Open Animation Replacer 3.1.6.0; no FNIS or Nemesis installed

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

- **The fast-travel fix is implemented but unverified in game.** Cause and fix were both proven structurally; only Barry's retest can confirm the engine behaviour.
- **Navmesh is on the critical path** for the platform milestone and cannot be generated by this pipeline. Creation Kit work.
- **Platform mesh dimensions are unknown.** No vanilla generic paving tile exists; tile sizes and seam behaviour need checking in the Creation Kit or NifSkope.
- The stall's Z is native terrain height; `SMarketStall01`'s mesh origin was never read, so a small vertical offset may still be needed.
- The generator copies the WRLD/CELL records it overrides from `Skyrim.esm`, not from the winning record in the active load order. Correct load-order placement (now in effect) makes this harmless, but building from the load order would be more robust. Not implemented.
- Permanent exterior placements will eventually require DynDOLOD/Occlusion regeneration.
- `external/` holds large third-party mod archives and must never be committed.

## Next local verification

The next pass depends on Barry's retest result.

If fast travel works, the Pass icon is right and Site 1 is approved, the queued follow-up is `docs/CLAUDE_AFTER_SITE_TEST.md` — prototype the landscaped fairground foundation.

If fast travel still fails, investigate whether the arrival point must be navmesh-accessible; that is the only remaining untested hypothesis.
