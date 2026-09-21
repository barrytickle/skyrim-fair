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

## In-game milestone reached

**The generated plugin has now been loaded in game successfully.** This is the first end-to-end confirmation that the C# -> Mutagen -> Skyrim pipeline works.

Confirmed by Barry in game:

- the plugin loads without CTD
- `The Wanderer's Fair` renders on the world map
- the placed vanilla market stall renders in-world at the intended location
- Barry reached it on foot and confirmed the object exists

Two problems found:

- **fast travel to the custom marker is broken** — selecting it returns the player to their current location instead of travelling. Cause identified below; a proven fix exists but is not yet implemented.
- **the current site is rejected for the fair layout.** Cell `2, -2` is much hillier in actual gameplay than the previous audit implied. See the methodology correction below.

## Fast-travel diagnosis — cause found

**Cause: our map-marker reference is missing the `Persistent` record flag (`0x400`).**

Measured across every map marker in Tamriel in `Skyrim.esm`:

| | Vanilla | Ours |
| --- | --- | --- |
| Markers examined | 347 | 1 |
| Raw record flags | `0x00000400` (329), `0x00000C00` (17), `0x40000400` (1) | **`0x00000000`** |
| Carry `Persistent` (`0x400`) | **347 / 347 — all of them** | **no** |

Our marker is stored in the correct persistent child group (group type 8) of Tamriel's persistent cell, but the record itself is flagged non-persistent. The map still draws it, because the marker data is read from the record, but the engine cannot resolve a non-persistent reference as a fast-travel destination without its cell loaded — which matches the observed symptom exactly.

Mutagen's `SkyrimMajorRecordFlag` enum does **not** expose `Persistent`, which is why the generator never set it. Mutagen does not derive it from `Cell.Persistent` membership either.

### Proven fix

Verified in a scratch plugin (not committed): setting `MajorRecordFlagsRaw = 0x400` on the `PlacedObject` writes `rawFlags=0x00000400` correctly.

```csharp
new PlacedObject(mod)
{
    EditorID = "FairSiteMapMarker",
    MajorRecordFlagsRaw = 0x400,          // Persistent - required for fast travel
    Base = new FormLinkNullable<IPlaceableObjectGetter>(ParseFormKey(marker.BaseObject)),
    MapMarker = new MapMarker { /* unchanged */ },
    Radius = 1250f,                        // XRDS
    Placement = new Placement { /* unchanged */ },
};
marker.LocationRefTypes = new ExtendedList<IFormLinkGetter<ILocationReferenceTypeGetter>>
{
    new FormLink<ILocationReferenceTypeGetter>(FormKey.Factory("10F63C:Skyrim.esm")), // MapMarkerRefType
};
```

With those changes the scratch plugin's subrecord order became `EDID, NAME, XRDS, XLRT, XMRK, FNAM, FULL, TNAM, DATA` — **identical to `WhiterunStablesMapMarker`**.

### Secondary differences (conventional, not proven mandatory)

| Subrecord | Vanilla coverage | Notes |
| --- | --- | --- |
| `XLRT` Location Ref Type | 346/347 | 333 use `MapMarkerRefType` = `0010F63C:Skyrim.esm`. Recommended. |
| `XLKR` Linked Reference | 341/347 | Links the marker to a nearby reference. Not reproducible generically yet. |
| `XRDS` Radius | 278/347 | 39 distinct values; 1250.0 is the most common. `WhiterunStablesMapMarker` uses 1500.0. |
| `EDID` | 151/347 | Optional. |

Only the `Persistent` flag is universal, so that is the fix. `XLRT` and `XRDS` are cheap and worth adding for consistency. A linked location (`XLCN`) is **not** required — `WhiterunStablesMapMarker` and `WhiterunWatchtowerMapMarker` both have none.

Not yet ruled out: whether the arrival point must be navmesh-accessible. That cannot be confirmed without an in-game test, so the flag fix should be tested first.

## Terrain audit methodology correction

The previous audit reported cell `2, -2` as having "24 units relief" and called it essentially flat. That number was correct but **measured over a 512x512 footprint — far too small to describe a fairground.**

Same site, same heightmap, increasing footprint:

| Footprint | Relief | Max step per 128u |
| --- | --- | --- |
| 512 x 512 | **24u** | 16u |
| 1024 x 1024 | 112u | 64u |
| 2048 x 2048 | 240u | 88u |
| 3072 x 3072 | 400u | 88u |
| 4096 x 4096 | **472u** | 88u |

The 512-unit sample landed on a small local sweet spot inside otherwise rolling tundra. Relief grows roughly 20x from the 512 sample to a fair-sized footprint, which is exactly the hilliness Barry saw in game.

**All future terrain audits must sample at the real footprint size** (2048 minimum for the market core, 3072–4096 for the whole site) and report maximum local step per 128-unit heightmap interval alongside total relief.

### Method used in this audit

- Heightmaps decoded from `LAND` `VHGT` records for a 17 x 15 cell search box (x -8..8, y -12..2), 255 cells, all present.
- **The winning `LAND` record per cell** was resolved through the full active load order, not just vanilla. Only 18 of 255 cells have mod-altered heights: `Landscape and Water Fixes.esp` (12), `Landscape and Water Fixes - Patch - Majestic Mountains.esp` (1), `Landscape and Water Fixes - Patch - Tundra Homestead.esp` (2), `Embers XD.esp` (1), `Helgen Reborn.esp` (2). `MajesticMountains_Landscape.esm` provides `LAND` for 89 region cells but changes **no** heights.
- Cells assembled into one continuous grid (128-unit spacing) and scanned with sliding windows.

## Natural site candidates near Western Watchtower

Reference point: `WhiterunWatchtowerMapMarker` (`000DB889:Skyrim.esm`) at `(1659.8, -14699.4, -4832.1)`.

Search constrained to windows that overlap **no** scripted/named vanilla cell and **no** cell with navmesh edits, within 13,000 units of the tower. Excluded cell name prefixes: `WhiterunWatchtower`, `WhiterunAttack`, `WhiterunSiege`, `MilitaryCamp`, `WhiterunExterior`, `PelagiaFarm`, `Whiterun`.

### Ranked candidates

#### 1st — world `(-5632, -12800)` — RECOMMENDED

| Field | Value |
| --- | --- |
| Cells spanned (3072 footprint) | `-2,-4` (`00009A28`), `-2,-3` (`00009A07`), `-1,-4` (`00009A27`), `-1,-3` (`00009A06`) |
| Grid | centred on the `-2,-4` / `-1,-3` corner junction |
| Proposed fair centre | `X -5632, Y -12800, Z -5696` (terrain max within the core) |
| Relief 2048 / 3072 / 4096 | **96u / 160u / 416u** |
| Max step per 128u | 24u (2048), 48u (3072) — **the smoothest gradient found anywhere in the search box** |
| Distance from Western Watchtower | 7,535u (1.8 cells) |
| Nearest map markers | Western Watchtower 7,535u; Fort Greymoor 9,219u |
| Winning LAND | **`Skyrim.esm` — vanilla, no mod height edits** |
| Navmesh edits | **none** |
| Placed-reference conflicts | USSEP (8 + 9), Butterflies (4 + 3), SLaWF (1 + 1) — all low, all ambient clutter |
| Civil-war / dragon / quest / settlement / farm content | **none** |
| Vanilla references in footprint | **18** — 11 tundra shrubs, 4 critter markers, `RockTundraLand01Tundra01` x1, `RockShelf01FieldGrass01` x1, `TreeThicket01` x1 |
| Obstruction concerns | Only two rock references to work around or disable. Very clean. |

Best overall: cleanest conflict profile, smoothest gradient, lowest platform fill requirement, no scripted content. Trade-off is that it is ~1.8 cells from the tower rather than adjacent, and slightly further from Whiterun's roads.

#### 2nd — world `(768, -11520)`

| Field | Value |
| --- | --- |
| Cells spanned (3072 footprint) | `-1,-4`, `-1,-3`, `0,-4` (**`WhiterunWatchtowerExterior`**), `0,-3` (`TestTundra`, `00009A05`) |
| Proposed fair centre | `X 768, Y -11520, Z -5416` |
| Relief 2048 / 3072 / 4096 | **80u / 200u / 352u** |
| Max step per 128u | 40u (2048), 48u (3072) |
| Distance from Western Watchtower | 3,302u (0.8 cells) — closest viable |
| Winning LAND | `Skyrim.esm` for `0,-3`; `Landscape and Water Fixes - Patch - Majestic Mountains.esp` for `0,-4` |
| Navmesh edits | none in the spanned cells |
| Placed-reference conflicts | `0,-4` is heavy: USSEP 40, SLaWF 7, GreatWarSkyrim 2, DynDOLOD 1 |
| Vanilla references in footprint | 57 — much more shrub and rock clutter to clear |
| **Blocker** | **A 3072 footprint clips `0,-4` = `WhiterunWatchtowerExterior`**, the Mirmulnir dragon-fight cell from MQ104 "Dragon Rising" |

Flattest 2048 core found near the tower and by far the closest, but the footprint must be shifted north to clear `0,-4` before it is usable. Shifting north degrades the relief figures. `0,-3` is named `TestTundra` — a Bethesda test-cell name, not scripted content.

#### 3rd — world `(10240, -10752)`

| Field | Value |
| --- | --- |
| Cells spanned (3072 footprint) | `2,-3` only (single cell, `00009A04` region) |
| Proposed fair centre | `X 10240, Y -10752, Z -4552` |
| Relief 2048 / 3072 | **256u / 304u** |
| Max step per 128u | 80u (2048) |
| Distance from Western Watchtower | 9,445u |
| Nearest map markers | Whiterun Stables 8,073u; Western Watchtower 9,445u |
| Navmesh edits | none |
| Civil-war / dragon / quest content | none |
| Vanilla references in footprint | 15 — but **11 are pine trees** (`TreePineForest01/02/03`, `TreePineShrub02`) |
| Concerns | Noticeably less flat; pine cover changes the visual character away from open tundra |

Closest to Whiterun and its roads, entirely within one unnamed cell, but the least flat of the three.

### Verdict on natural terrain: not good enough

**No naturally flat site in the Western Watchtower area meets the hard requirement that the market core be genuinely level.**

The best clean 3072 x 3072 site anywhere in the search box is 160u relief with a 48u maximum step per 128 units. For context, a 48u rise across 128 units is roughly a 21-degree local slope. The design direction calls for regular rows of stalls, a traders' crossing, aligned trading rows and a stage square — a grid-like layout that is generated from config rather than hand-fitted per object. On 160u of relief every stall, table and paved surface would need individual Z and rotation fitting, and any flat paved surface would float at one end and sink at the other.

Ranking the three above satisfies the brief, but the honest recommendation is **Plan B**.

## Plan B — purpose-built flat fairground platform

Assessed at the three candidate sites. All figures from the same winning-LAND heightmap.

### Platform footprint and elevation

Recommended: **3072 x 3072 paved core** (the market core, traders' crossing, trading rows and stage square), with archery, stables and future jousting left on natural ground outside the paving.

Floor placed at the terrain maximum within the core, so the platform is **pure fill with zero cut** — nothing is buried, nothing pokes through:

| Site | Platform floor Z | Max fill (floor above terrain) | Perimeter step to native ground |
| --- | --- | --- | --- |
| **1 — `(-5632, -12800)`** | **-5672** | **160u** | 0u (high edge) to 160u (low edge) |
| 2 — `(768, -11520)` | -5336 | 200u | 0u to 200u |
| 3 — `(10240, -10752)` | -4552 | 256u | 8u to 256u |

At a 2048 core, site 1 needs only **96u** of fill and site 2 only **80u**.

A balanced cut-and-fill floor (at the mean) halves the visible step but requires cutting into terrain, which means either LAND edits or terrain poking through the paving. **Pure fill at the terrain maximum is the recommended approach** — it needs no LAND edits at all.

Site 1 is the best platform site on every measure: lowest fill, lowest perimeter step, smoothest surrounding gradient.

### Where ramps and blending are needed

With a pure-fill floor at site 1, the step to native ground runs 0u on the high edge to 160u on the low edge. Practically:

- the high (north-east) edge needs essentially nothing — the paving meets grade
- the low edge needs roughly 160u of transition: a shallow ramp at 1:8 is about 1,280u long, which comfortably becomes the main entrance approach
- the two side edges taper between those extremes and suit stepped retaining, embankment rocks, hay, shrubs and stall placement rather than ramps
- the design's entrance avenue should be placed on the **low** edge so the ramp becomes a deliberate feature

### Vanilla statics for a paved surface

**Important finding: vanilla Skyrim has no generic cobblestone paving tile.** Whiterun's streets and plaza are not separate statics — the city's ground is baked into the `WRTerrain` architecture meshes. Searching all `STAT` records for paving, plaza, street, cobble, courtyard and floor patterns returned 119 records, and none is a plain repeatable exterior paving tile.

The two closest usable families:

**Nordic exterior cut-stone platform kit** — a genuine modular exterior platform set with floor, edges and corners:

| FormKey | EditorID | Model |
| --- | --- | --- |
| `001044CB:Skyrim.esm` | `NorTmpExtPlatFloorRaised01CutStone` | `Dungeons\Nordic\Exterior\NorTmpExtPlatFloorRaised01Stone.nif` |
| `00028A62:Skyrim.esm` | `NorTmpExtPlatFloorRaised01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatFloorRaised01.nif` |
| `00026F7B:Skyrim.esm` | `NorTmpExtPlatCorOut01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorOut01.nif` |
| `00026F79:Skyrim.esm` | `NorTmpExtPlatCorIn01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorIn01.nif` |
| `00026F7A:Skyrim.esm` | `NorTmpExtPlatCorDblEnd01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatCorDblEnd01.nif` |
| `0002BE41:Skyrim.esm` | `NorTmpExtPlatExSmFree01` | `Dungeons\Nordic\Exterior\NorTmpExtPlatExSmFree01.nif` |

62 records in this family, including snow and ice variants. Reads as ancient Nordic ruin masonry — atmospheric, but arguably too monumental for a temporary handmade market.

**Whiterun terrain platforms** — the stone plinths vanilla uses to level Whiterun's buildings against its own slope. Exactly this problem, and visually native to the Whiterun tundra:

| FormKey | EditorID | Model |
| --- | --- | --- |
| `000506DF:Skyrim.esm` | `WRCarlottaPlatform01` | `Architecture\WhiteRun\WRTerrain\WRCarlottaPlatform01.nif` |
| `000510E2:Skyrim.esm` | `WRCommonHousePlatform01` | `Architecture\WhiteRun\WRTerrain\WRCommonHousePlatform01.nif` |
| `0005071F:Skyrim.esm` | `WRGreatHousePlatform01` | `Architecture\WhiteRun\WRTerrain\WRGreatHousePlatform01.nif` |
| `000510F7:Skyrim.esm` | `WRGreatHousePlatform02` | `Architecture\WhiteRun\WRTerrain\WRGreatHousePlatform02.nif` |
| `0005071A:Skyrim.esm` | `WRHallOfDeadPlatform01` | `Architecture\WhiteRun\WRTerrain\WRHallOfDeadPlatform01.nif` |
| `000510E0:Skyrim.esm` | `WRStairsPlatform01` | `Architecture\WhiteRun\WRTerrain\WRStairsPlatform01.nif` |
| `000506ED:Skyrim.esm` | `WRUlfberhPlatform01` | `Architecture\WhiteRun\WRTerrain\WRUlfberhPlatform01.nif` |

These are building-shaped footprints, not tiles, so they will not tessellate into a large square floor.

**Edge and transition pieces:**

| FormKey | EditorID | Use |
| --- | --- | --- |
| `0000099B:Skyrim.esm` | `Stonewall01` | farmhouse dry-stone wall — retaining / embankment |
| `0000099D:Skyrim.esm` | `Stonewall02` | as above, variant |
| `0000099C:Skyrim.esm` | `Stonewall01Ivy` | as above, ivy |
| `0003F93B:Skyrim.esm` | `ImpExtStairs01` | exterior stone steps |
| `000F03D3:Skyrim.esm` | `RTTemplePlazaStairs01` | wide plaza stairs |
| `0010EC69:Skyrim.esm` | `MrkDocksidePlatforms03stairs` | dockside platform stairs |

**Unverified:** exact mesh dimensions and whether any of these tile seamlessly. NIF geometry cannot be read with the tooling used here, so tile sizes, pivot points and edge alignment are **unknown** and must be checked in the Creation Kit or NifSkope before committing to a kit.

### Custom mesh vs assembled vanilla pieces

**A custom static would very likely be substantially cleaner.** Reasons:

- there is no vanilla repeatable paving tile, so a 3072 x 3072 floor from vanilla pieces means hundreds of statics with visible seams and Z-fighting risk
- a single custom mesh (or a small purpose-built tile set) gives one clean collision surface instead of hundreds of overlapping ones
- draw-call and reference-count cost drops dramatically
- the paved surface, its edge bevel and its retaining lip can be authored as one coherent piece

Counter-argument: a custom mesh means leaving the "vanilla assets first" preference in `docs/DESIGN.md`, adds a BSA/loose-asset pipeline the project does not have yet, and needs authored collision. A reasonable middle path is a **small custom tile set** (one floor tile, one edge, one corner, one ramp) rather than one monolithic mesh — still project-owned, which is preference tier 2 in the design brief.

### Collision implications

- Vanilla `STAT` records carry their own collision from the NIF, so a platform assembled from statics is walkable without extra work.
- Overlapping tiles risk Z-fighting on the surface and seam snagging on collision. A custom tile set with exact dimensions avoids both.
- The platform floor must sit at or above the highest terrain point in the footprint, or terrain will poke through the paving. That is why pure fill is recommended.
- Havok clutter (barrels, crates) resting on a static platform is fine; `docs/DESIGN.md` already prefers static display objects, which suits this.

### Navmesh implications

This is the main risk in Plan B, and it applies to vendors, ambient crowds, Garrick Tallow, Claudius Vale and the stage performers.

- Statics do **not** generate navmesh. NPCs will not path onto a raised platform without new navmesh over it.
- New navmesh must be authored and then **joined to the existing exterior navmesh** at the ramp and step edges, or NPCs will refuse to enter and leave.
- Exterior navmesh in the candidate cells is currently authored by `Skyrim.esm` with USSEP edits nearby. Site 1 has **no navmesh edits in any spanned cell**, which is the cleanest possible starting point and is a strong argument for choosing it.
- Navmesh cannot currently be generated by this project's Mutagen pipeline in any practical way. It is Creation Kit work, and the project principle in `docs/ROADMAP.md` is to avoid custom navmesh until a milestone needs it. **This milestone needs it.**
- A lower platform reduces the problem: at 160u of fill the step is small enough that a generous ramp can carry navmesh smoothly. At 256u (site 3) it is harder.
- Interim option for a visual-only test: place the platform and accept that NPCs cannot walk on it, purely to evaluate how the flat floor looks before committing to navmesh work.

### Avoiding LAND edits

**Yes — Plan B can avoid `LAND` edits entirely**, provided the floor is placed at or above the terrain maximum (pure fill). That is the main reason to prefer it over flattening the landscape.

Compatibility comparison:

| Approach | Compatibility |
| --- | --- |
| **Static platform, no LAND edit** | Conflicts only with mods that place objects in the same cells. Does **not** fight `Landscape and Water Fixes`, `Majestic Mountains` or any landscape mod. Survives their updates. Requires navmesh work. |
| **LAND height edit (flattening)** | Directly conflicts with SLaWF and the Majestic Mountains patches, which already author `LAND` in 18 nearby cells. Needs a compatibility patch per landscape mod, breaks on their updates, and creates visible seams at cell borders. Also needs navmesh work regardless. |

At site 1 specifically, `LAND` is vanilla-authored with no mod overrides, so a LAND edit there would be less conflict-prone than elsewhere — but the static platform is still the more compatible choice and matches the brief's stated preference.

### Recommendation

**Site 1 at `(-5632, -12800)` with a 3072 x 3072 pure-fill static platform at Z `-5672`.** 160u of fill, zero cut, zero LAND edits, zero navmesh edits to contend with, zero scripted content, and only two rock references in the footprint.

Suggested sequencing:

1. fix the fast-travel flag and move the existing prototype marker and stall to site 1 (config-only change)
2. test in game that fast travel works and the ground genuinely reads as flat enough
3. only then decide between a vanilla-assembled platform and a small custom tile set
4. treat navmesh as its own milestone

## Generated plugin — current state

- output: `dist/SkyrimFair.esp`
- size: 47,039 bytes
- sha256: `66e9f53788ce023501e4bd6b73666c141a9e87075a1d492e1363c961cd2d5bd9`
- masters: `Skyrim.esm` (single master, derived from FormKeys, no hardcoded index)
- TES4 Author (`CNAM`): `BarryRim Event Planner`
- records: 1 WRLD, 2 CELL, 2 REFR
- no navmesh, LAND, NPC, quest, script, package, music or animation records

| FormKey | EditorID | What it is |
| --- | --- | --- |
| `000800:SkyrimFair.esp` | `FairSiteMapMarker` | map marker "The Wanderer's Fair" |
| `000801:SkyrimFair.esp` | `FairTestMarketStall` | placed `SMarketStall01` |

Current placement (unchanged by this audit): Tamriel cell `000095FE:Skyrim.esm`, grid `2, -2`, position `10880, -7552, -4616`.

### MO2 deployment

- `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp`, byte-identical to `dist`
- folder contains the ESP only, no metadata files added
- Barry has enabled the mod and its plugin himself and loaded the game

**Load-order caution:** MO2 placed `SkyrimFair.esp` last, at position 427, after `DynDOLOD.esp` (414) and `Occlusion.esp` (415). Our Tamriel `WRLD` and `CELL` overrides therefore currently beat theirs. Measured differences against the records Occlusion.esp/DynDOLOD.esp would otherwise win: `TVDT`, `XCLC` and `XCLR` on cell `2,-2`, and `TNAM`/`UNAM` on the worldspace. **`SkyrimFair.esp` should sit before `DynDOLOD.esp` and `Occlusion.esp`.** Our two references are records of their own and still load from an earlier slot.

### Known generator weakness

The generator copies the WRLD/CELL records it overrides from `Skyrim.esm`, not from the winning record in the active load order. Correct load-order placement makes this harmless, but building from the load order would be more robust. Not yet implemented.

## Skyrim install

- Mod manager: Mod Organizer 2 v2.5.2, portable instance at `E:\Modlists\Still In Skyrim`
- Install dir: `E:\Modlists\Still In Skyrim\stock`
- Data dir: `E:\Modlists\Still In Skyrim\stock\Data`
- Runtime: 1.6.1170.0 (Skyrim SE/AE, Steam)
- Active profile: `Still in Skyrim Plus`
- Active plugins: 426 (425 before `SkyrimFair.esp`)
- SKSE 2.2.6 (matches runtime), Address Library 11.0.0
- Pandora Behaviour Engine+ is the active behaviour generator (output mod v4.3.0); Open Animation Replacer 3.1.6.0; no FNIS or Nemesis installed

## Map marker implementation notes

- Base object: `MapMarker`, `00000010:Skyrim.esm` (a `STAT`)
- Markers live in Tamriel's persistent cell `00000D74:Skyrim.esm`, not the grid cell they sit over
- `XMRK` presence is what makes a REFR a map marker
- **The record must carry the `Persistent` flag `0x400`** — see the diagnosis above
- `FULL`: display name. `TNAM`: icon type. `FNAM`: flags. `DATA`: position + rotation
- `FNAM` bit `0x01` = Visible, bit `0x02` = Can Travel To. Vanilla: 332 markers at `0x00`, 4 at `0x01`, 11 at `0x03`
- `XLRT` should be `MapMarkerRefType` = `0010F63C:Skyrim.esm`
- Useful icon types: `0x02` town/village (Mutagen `MarkerType.Town` — Riverwood, Rorikstead, Shor's Stone), `0x0D` farm (incl. `MerryFairMapMarker`), `0x05` camp, `0x1E` shack, `0x15` stable
- Mutagen's `MarkerType.Settlement` is `0x03`, which vanilla uses for Honningbrew Meadery and Goldenglow Estate. The name is misleading; `Town` is the village icon.

### Pass icon preference — no technical blocker

`docs/DESIGN.md` now specifies the **Pass** icon for the final fair marker instead of Town/Village. This is viable:

- Mutagen exposes it as `MapMarker.MarkerType.Pass` (value 24, raw `TNAM 0x18`)
- vanilla uses it for 3 Tamriel markers — `000F5EB1`, `000F5EAF` and `000C2EFA`, all unnamed border-pass crossings at the map edges
- it is a one-word config change: `site.mapMarker.type` from `"Town"` to `"Pass"`, since the generator already parses the type name straight onto the Mutagen enum

The icon choice is independent of the fast-travel bug — that is caused by the missing `Persistent` flag, not by `TNAM`. Changing the icon will not fix travel, and fixing travel does not constrain the icon. The prototype can keep `Town` until the flag fix is tested, then switch.

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

- **Fast travel is broken** until the `Persistent` flag fix is implemented. Cause proven, fix proven, not yet committed.
- **No naturally flat fair site exists** in the Western Watchtower area. Plan B (static platform) is the recommended path.
- **Navmesh is now on the critical path.** A raised platform needs authored navmesh joined to exterior navmesh, and this project's Mutagen pipeline cannot practically generate it. This breaks the roadmap principle of deferring custom navmesh.
- **Platform mesh dimensions are unknown.** No vanilla generic paving tile exists; tile sizes and seam behaviour must be checked in the Creation Kit or NifSkope.
- `SkyrimFair.esp` is currently loading after `DynDOLOD.esp` and `Occlusion.esp` and should be moved earlier.
- The stall's Z is terrain height; its mesh origin was never read, so a vertical offset may still be needed.
- Permanent exterior placements will eventually require DynDOLOD/Occlusion regeneration.
- `external/` holds large third-party mod archives and must never be committed.

## Next local verification

No generator placement coordinates were changed in this audit, and no new ESP was deployed.

The next pass should, in order:

1. implement the `Persistent` flag fix (plus `XLRT` and `XRDS`) and retest fast travel in game
2. if Barry approves, move the prototype marker and stall to site 1 at `(-5632, -12800, -5672)` — a config-only change
3. confirm in game that site 1 reads as acceptably flat before any platform work begins
4. only then choose between a vanilla-assembled platform and a small custom tile set
