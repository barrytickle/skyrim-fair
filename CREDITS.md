# Credits and third-party assets

Skyrim Fair is built with help from the Skyrim modding community. This file records third-party mods, assets, authors, licences, and redistribution conditions used or evaluated by the project.

This document is intentionally conservative. An asset should not be copied into Skyrim Fair until its original author and redistribution permission have been confirmed.

## Stage music and crowd sounds (Barry's)

**Files:** `music/mono/` (Round the Green; Hey-Ho, Skyrim; Fiddle; Dragonborn-Approved) and
`sound-effects/mono/` (crowd, crowd-cheer), supplied by Barry and converted to mono 44.1 kHz
16-bit WAV. `tools/build_audio.py` builds the runtime copies under `Sound\SkyrimFair\`.

**Status:** used in Barry's local build only. The sources and the built files are kept out
of git. **Provenance and redistribution terms are still to confirm**, for example if the
songs came from an AI music service or the crowd recordings from a sound library. Record
the terms here before any public release.

The fair's scripts (`src/Papyrus/`) are the project's own. They compile against the vanilla
Papyrus sources in the Creation Kit, which are not redistributed.

## Professional Dancer

**Mod:** Professional Dancer  
**Nexus Mods:** https://www.nexusmods.com/skyrimspecialedition/mods/124608  
**Created by:** contentcat and davidgilbertking  
**Current referenced version:** 1.5.0  
**Licence stated by the author:** CC BY-NC 4.0

Professional Dancer provides the dance system and dance animations being evaluated for fairground stage performances.

The mod page credits the source dance animations to creators whose animations were found on Mixamo.

### Skyrim Fair usage

**An optional, recommended dependency, detected at runtime (2026-09-24):** when `Dance.esp` is
loaded, the stage script plays five of its dances on the fair's dancers through its own
animation events (`Dance1`, `Dance3`, `Dance4`, `Dance8`, `Dance9`; they exist once the user has
run Pandora or Nemesis with the mod installed). Without it, the vanilla dances play. **No file of
Professional Dancer is copied, bundled or redistributed**: the fair only sends its event names.
An earlier build that copied its clips into an OAR folder was removed for that reason.

Our preferred integration is to treat Professional Dancer as an external dependency rather than redistributing its animation files inside Skyrim Fair.

If any Professional Dancer files are ever bundled, modified, or redistributed directly, their use must comply with the author's CC BY-NC 4.0 terms and the provenance/licensing of the underlying animation assets must be checked separately.

Attribution:

> Professional Dancer by contentcat and davidgilbertking.

## Medieval Markets

**Mod:** Medieval Markets  
**Nexus Mods:** https://www.nexusmods.com/skyrimspecialedition/mods/161479  
**Created by:** JJerem  
**Current referenced version:** 1.1 / 1.1.1 file updates

Medieval Markets contains custom market assets being evaluated for Skyrim Fair's stalls, tents, shelving, tables, baskets, produce displays, and general market dressing.

The Nexus permissions state that assets made by JJerem may be used in another project without separate permission provided JJerem is credited. The assets may not be used in a mod or file that is sold for money.

### Important provenance note

Medieval Markets also contains assets originating from other authors. JJerem's description specifically credits:

- **PraedythXVI** for *Fruits and Veggies - modder's resource*
- **Brumbek** for *Static Mesh Improvement Mod (SMIM)*
- **gooball60** for *Palpable Baskets*
- **JPSteel2** for tent assets/textures from *Northern Roads*

Those assets remain subject to their original authors' permissions. JJerem's permission must not be treated as blanket permission to redistribute third-party assets contained in Medieval Markets.

### Skyrim Fair usage

Before copying any Medieval Markets mesh or texture into this repository:

1. identify the exact asset file,
2. identify whether it was created by JJerem or another credited author,
3. record its original source here,
4. confirm that source's redistribution permissions,
5. preserve any required attribution.

Where practical, prefer assets authored directly by JJerem or make Medieval Markets an external dependency instead of rebundling third-party assets.

### Audit of the local download (2026-09-22)

See `docs/AUDIT.md` ("Medieval Markets audit"). In short:
- The stall pieces (`NewMarketStall01`, `MarketStandLong01/02`, `SlantedShelf01`,
  `ShortFarmTable`, `fenceWovenTall01`, weapon racks) use vanilla textures only.
- The produce crates and baskets use PraedythXVI's Fruits and Veggies textures.
- The Nord tent is an unconverted LE mesh with COTN textures.
- **Nothing is bundled yet.** The open question is which stall pieces derive from SMIM.

Attribution:

> Medieval Markets by JJerem.

## Whiterun Stone Stairs

**Mod:** Whiterun Stone Stairs  
**Nexus Mods:** https://www.nexusmods.com/skyrimspecialedition/mods/147164  
**Created by:** Chiselsky  
**Current referenced version:** 1.2

Whiterun Stone Stairs provides higher-detail meshes and 4K complex-material textures for Whiterun stairs and wall borders while preserving the vanilla Whiterun visual language. It is being evaluated for The Wanderer's Fair entrance and retaining-wall treatment.

The Nexus permissions state that the mod's assets may not be reused in another mod without permission from Chiselsky. The author specifically permits reuse of their meshes/files for compatibility patches between Whiterun Stone Stairs and other mods. The mod page also credits Arthmoor for USSEP meshes used as a base.

### Skyrim Fair usage

Treat Whiterun Stone Stairs as an external visual dependency / optional replacer unless explicit asset-use permission is obtained from Chiselsky.

Do not copy, modify, convert, or redistribute its meshes or textures inside Skyrim Fair solely because the mod has been downloaded locally. If the fair ultimately references vanilla Whiterun assets whose paths are replaced by Whiterun Stone Stairs at runtime, users without the mod should retain the vanilla appearance.

If direct asset reuse is later approved, record the exact files, permission, required attribution, and any upstream provenance before bundling anything.

Attribution:

> Whiterun Stone Stairs by Chiselsky.

## Riverwood Has Charm and Walls

**Mod:** Riverwood Has Charm and Walls  
**Nexus Mods:** https://www.nexusmods.com/skyrimspecialedition/mods/146520  
**Created by:** J3w3ls  
**Current referenced version:** 1.4.1

Riverwood Has Walls adds a wooden palisade, gates, watchtowers and covered bridges around Riverwood. Its wall and gate meshes are being evaluated for Skyrim Fair's perimeter. The full file-level audit is in [docs/RIVERWOOD_WALLS_AUDIT.md](docs/RIVERWOOD_WALLS_AUDIT.md).

The Nexus permissions state that you must get permission from J3w3ls before using any of the assets in this file, before modifying them, and that they may not be uploaded elsewhere, converted to other games, used in sold mods, or earn Donation Points. The author's notes add that the assets "will be included in a modder's resource eventually, free to use there".

### Important provenance note

J3w3ls credits riton67000 (Farmhouse Parallax II, WoodPost02 textures), LucidAPs (High Poly Project smelter rocks), Vermunds (Bells of Skyrim bell) and others. The wall and gate meshes themselves reference only J3w3ls's own `RiverwoodPost` and `RiverwoodWoodPlanks` textures plus vanilla texture paths, but the bell in the bell tower is Vermunds's and would need a separate check.

### Skyrim Fair usage

Treat Riverwood Has Charm and Walls as a visual reference only until one of the following happens:

1. J3w3ls grants explicit written permission for the specific files listed in the audit, or
2. J3w3ls publishes the promised modder's resource, whose own permissions then apply.

Do not copy, downscale, rename, or redistribute its meshes or textures before then. If permission is granted, record the exact files, the permission text and date, and the required attribution here before bundling anything.

Attribution:

> Riverwood Has Charm and Walls by J3w3ls.

## Palisade and Viking Palisade gate (Sketchfab, CC BY 4.0)

These two models form the wall and main gate of the isolated fair worldspace, and they
**are bundled** with Skyrim Fair as `meshes\barry_palisades\` and
`textures\barry_palisades\`. Both are licensed CC BY 4.0, which allows redistribution
and modification with attribution.

| Model | Author | Source | Licence |
| --- | --- | --- | --- |
| Palisade | adam127 (https://sketchfab.com/adam127) | https://sketchfab.com/3d-models/palisade-c88f57eb0d734484b010c3f170e48d4a | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |
| Viking Palisade gate | Sereib (https://sketchfab.com/Sereib) | https://sketchfab.com/3d-models/viking-palisade-gate-9c87007db87c41c9b161dae827ced741 | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |

### Changes made

Barry converted both for Skyrim SE / AE with Blender 3.6.23, the BGS exporter and
AssetWatcher (the package is `assets/Skyrim_Palisade_Assets/`, with its own README and
CREDITS). The palisade was uniformly enlarged to 3.5 m and turned to span its X axis; the
gate keeps its original size. Both were re-origined to bottom centre, given simplified
fixed box collision, re-materialised as opaque double-sided matte wood, and their
textures re-encoded as DDS. The palisade's normal map has its green channel inverted, and
the gate, which shipped diffuse maps only, was given neutral normal maps. In the plugin,
the palisade is placed at about 2.5x scale and the gate at 2.5x.

Attribution:

> "Palisade" by adam127, licensed under CC BY 4.0. "Viking Palisade gate" by Sereib,
> licensed under CC BY 4.0. Converted and modified for Skyrim.

## Stroti's Outdoor Toilet (modder's resource)

The fair's outhouses are **bundled** in the built mod as `meshes\Stroti\Outdoor Toilet\`
(`OutdoorToilet.nif`, `ToiletDoor.nif`) and `textures\Stroti\` (six custom DDS files).

| Role | Who | Source |
| --- | --- | --- |
| Original model (Oblivion) | Stroti | http://oblivion.nexusmods.com/mods/37634 |
| Skyrim conversion | Tamira | https://www.nexusmods.com/skyrimspecialedition/mods/4086 |
| SE / AE format conversion (stream 83 NiTriShape to stream 100 BSTriShape) | Astra, for Skyrim Fair | `external/Astra_Stroti_Outdoor_Toilet_Modern_SE.zip` |

Permissions, from the original readme: *"This is a modder's resource. You may use the
meshes and textures for your own mods as long as you give credit and you do not charge
money for it. Do not upload to other sites."* So:
- Skyrim Fair must stay free, and this credit must ship with it.
- The files are **not committed to this repository**, so git never uploads them anywhere
  on their own; `.gitignore` excludes `assets/meshes/Stroti/` and
  `assets/textures/Stroti/`. To restore them, unzip the `meshes/` and `textures/` folders
  of Astra's zip into `assets/`. The original readme and Astra's conversion notes sit
  next to the meshes.
- The resource also uses six vanilla textures (Whiterun floorboards, Solitude roof slate,
  a torn page), which Skyrim supplies and which are not redistributed.

The fair places both as plain statics: the outhouse cannot be sat in and the door does
not open.

Attribution:

> Outdoor Toilet by Stroti, converted for Skyrim by Tamira, SE conversion by Astra.

## Holidays (required master)

**Mod:** Holidays, Nexus Mods (Skyrim SE) 1533, version 2.20 Alpha 1

Skyrim Fair uses Holidays as a **master**: the fair's festival rope lines (Holidays'
Whiterun, Saturalia, Riften, Windhelm, Imperial and Stormcloak colourways of the vanilla
Solitude festival line), its coloured hanging lanterns, and its apple basket, mead crate,
silver platter and sign stand are Holidays' own records, placed by reference (the lanterns
also hang from the palisade's pennant ropes and the lantern posts, 2026-09-24). **No
Holidays file is copied or redistributed**; players need Holidays installed and loaded
before SkyrimFair.esp.

Attribution:

> Festival decorations from Holidays (Nexus Mods, Skyrim Special Edition, mod 1533).

## Static crowd figures (built from vanilla assets)

The crowd figures (`meshes\SkyrimFair\Crowd\`, `docs/CROWD.md`) are vanilla Skyrim
geometry posed with a vanilla animation:
- the body, clothes, hair, head, hands and feet meshes, both skeletons, and the idle and
  furniture animations each pose is taken from are Bethesda's
- the props held in hand (the AnimObject tankard and MQ201 goblet) are Bethesda's
- every texture path is a vanilla one; no texture is shipped

Like `Props/`, the built NIFs are kept out of git.

**PyNifly** (by Bad Dog, GPL-3.0: the licence is vendored with the folk-dance tools as `character-actors/folk-dance/vendor/LICENSE-PyNifly`) is used as a **build tool only**, to write the NIFs
from the command line. None of its code is bundled with the mod.

## Stage singers' faces (vanilla)

The three singers copy vanilla face-template NPCs' faces (`039CF6`, `039CFF`, `039D17`).
Their FaceGen head meshes and tint textures are copied under the singers' FormIDs.
- **The head meshes are Bethesda's**, byte-identical to the Steam install's.
- **The tint textures are not, yet** (audit, 2026-09-25): Barry's modlist has no vanilla
  texture archives. Its `Skyrim - Textures*.bsa` are Vanilla Remastered - The New Normal's
  (Nexus 153879), and the three tints match those. Rebuild them from Bethesda's archives
  before any release (`docs/RELEASE.md`). The singers' voice files are built from Barry's song stems (see the music's
provenance above).

## Compatibility patches (Maximum Destruction, Stealth Detection Fixes, Strange Runes)

The SPID exclusion patches (`dist/spid/`, built by the generator's `spidPatches`) are
**copies of these mods' own `_DISTR.ini` files**, with `-SkyrimFairNPC` added to the lines
that give every NPC a cloak, ability or script (`docs/RELEASE.md`). Each copy carries the
mod's other lines unchanged, so shipping them needs each author's permission:
- Maximum Destruction: Nexus Mods, Skyrim SE (`MaximumDestruction_DISTR.ini`)
- Stealth Detection Fixes: Nexus Mods, Skyrim SE, mod 145336 (`StealthKillDetectionFix_Attack_DISTR.ini`,
  `StealthKillDetectionFix_DISTR.ini`, `StealthKillDetectionFix_Killmove_DISTR.ini`)
- Strange Runes: Nexus Mods, Skyrim SE (`StrangeRunes_DISTR.ini`)

The earlier ESP condition patches (`SkyrimFair - Maximum Destruction Patch.esp`,
`SkyrimFair - Stealth Detection Fixes Patch.esp`) didn't work and aren't shipped.
Alternatively, ask the authors to add `-SkyrimFairNPC` upstream.

## Paired folk dance (Astra)

**Animation:** `folk_turn_A.hkx` and `folk_turn_B.hkx` by Astra, made for Skyrim Fair.
They're authored in Blender 3.6 on the vanilla male and female skeletons (no bones added)
and encoded with PyNifly's standalone HKX codec (GPL-3.0, a build tool only).

**Skyrim Fair usage:**
- `tools/folk/rebase_folk.py` rewrites only the root track, so each dancer's clip starts
  at their own feet.
- The two clips replace the Cicero dance's animation for the fair's two folk dancers only,
  through **Open Animation Replacer**. OAR is a dependency and isn't bundled.
- The sources stay in Barry's untracked `character-actors/`, and the derived clips aren't
  committed. Confirm with Astra how they want to be credited before any public release.

## Recommended texture replacers (not bundled)

These replace vanilla textures that the fair's own ground texture sets point at. Skyrim
Fair ships none of their files. It only names the vanilla texture paths (with `_p`
parallax slots), so installing them is optional and improves the look:

| Mod | Nexus (SE) | What the fair uses it for |
| --- | --- | --- |
| Whiterun Mossy Wet Stonefloor – Grey 2k | 99294 | **no longer used** (2026-09-25). The avenue's cobbles were its textures; they're now vanilla `wrstonefloor01` with the fair's own parallax map (`tools/make_cobble.py`). Installing it no longer changes the fair |
| Terrain Parallax 1.5 – 4K2K | 54860 | parallax on the fair's grass, dirt and path ground (landscape `_p` maps) |

## Audit of what ships (2026-09-25)

Every file in the deployed mod folder was traced to its source:
- **Every texture path in the shipped meshes** is either in Bethesda's own archives
  (checked against the Steam install) or one of the credited assets above: the palisade
  and gate, the scaffold tower, Stroti's outhouse.
- **The plugin's own texture paths** are vanilla, apart from the cobbles
  (`SkyrimFair\Ground\Cobble01*`). Those were Nexus 99294's; now they're vanilla
  `wrstonefloor01` plus the fair's own parallax map.
- **Props (174 sources):** Bethesda's meshes from the modlist's stock folder. They differ
  from the Steam install's in 8 bytes of Havok data, the mark of another official game
  version, with no mod edits. The stock folder has no loose files.
- **Masters:** Skyrim.esm and Holidays.esp only.
- **Downloaded and read, but nothing used or shipped:** Crowded Streets (127723), Diverse
  Archery Targets (98142), Fireworks (183953; the fair's fireworks are vanilla effects),
  Incaendo's Banner Resource 2 (94919), Terrain Parallax (54860), and the Medieval Markets,
  Riverwood Has Charm and Walls and Whiterun Stone Stairs entries above.
- **Still to settle:** the singers' tints, the scaffold tower's
  source, the music's provenance, Astra's credit, and the SPID patches' authors
  (`docs/RELEASE.md`).
- The old procedural cobble (`textures\SkyrimFair\SkyrimFair_Cobble01*`, committed
  2026-09-21 as project-made) isn't used by the plugin, so it's left out of the package.

## The avenue's cobbles (vanilla, with the fair's own parallax)

`textures\SkyrimFair\Ground\Cobble01.dds` and `Cobble01_n.dds` are Bethesda's
`textures\architecture\whiterun\wrstonefloor01.dds` and `_n.dds`, unchanged, copied from
the vanilla archives. They sit under the fair's own path so that they always match the
height map. `Cobble01_p.dds` is the project's own, computed from the vanilla normal map
(`tools/make_cobble.py`).

## Bethesda Game Studios

The Elder Scrolls V: Skyrim Special Edition and its original game assets are property of Bethesda Game Studios / Bethesda Softworks.

Skyrim Fair is an unofficial fan-made mod and is not affiliated with or endorsed by Bethesda.

## Project asset ledger

As assets are selected for the shipped mod, record them here.

| Skyrim Fair use | File(s) | Original project | Original author | Permission checked | Bundled or dependency |
| --- | --- | --- | --- | --- | --- |
| Stage dancing | TBD | Professional Dancer | contentcat and davidgilbertking / underlying animation creators | Pending exact integration audit | Prefer dependency |
| Market stalls / tents | TBD | Medieval Markets | TBD per asset | Pending downloaded-file audit | TBD |
| Entrance stairs / wall borders | TBD | Whiterun Stone Stairs | Chiselsky | Asset reuse requires author permission; patch reuse allowed | Prefer external dependency / optional replacer |
| Perimeter palisade / gates | `J3_RW_WallStraight*.nif`, `J3_RW_WallJunction.nif`, `J3_RW_GateNoFrame.nif`, `J3_RW_GateWay.nif`, `RiverwoodPost*.dds`, `RiverwoodWoodPlanks*.dds` (see audit) | Riverwood Has Charm and Walls | J3w3ls | Asset reuse requires author permission; modder's resource promised | Blocked pending permission; visual reference only |
| Fair worldspace palisade wall | `meshes\barry_palisades\palisade.nif`, `textures\barry_palisades\palisade\*.dds` | Palisade (Sketchfab) | adam127 | CC BY 4.0: redistribution and modification allowed with attribution | Bundled |
| Fair worldspace main gate | `meshes\barry_palisades\viking_palisade_gate_closed.nif` (placed), `viking_palisade_gate.nif` (open variant, not placed), `textures\barry_palisades\viking_palisade_gate\*.dds` | Viking Palisade gate (Sketchfab) | Sereib | CC BY 4.0: redistribution and modification allowed with attribution | Bundled |
| Fair worldspace outhouses | `meshes\Stroti\Outdoor Toilet\OutdoorToilet.nif`, `ToiletDoor.nif`, `textures\Stroti\*.dds` | Stroti's Outdoor Toilet Resource | Stroti; Tamira (Skyrim); Astra (SE conversion) | Modder's resource: credit, free, not re-uploaded | Bundled in the built mod; kept out of git |
| Fair worldspace light towers | `meshes\barry_scaffold\scaffold.nif`, `textures\barry_scaffold\scaffold_d.dds`, `scaffold_n.dds` | Barry's scaffold watchtower | Barry (original source to confirm) | Pending: record the source and licence | Bundled |
| Market goods (148 props) | `meshes\SkyrimFair\Props\*.nif`, generated by `tools/make_static_props.py` | Skyrim | Bethesda Game Studios | Vanilla meshes with their rigid bodies made fixed, as mods routinely ship | Built mod only; kept out of git |
| Festival lines, lanterns, festive props | Holidays.esp records | Holidays | (Nexus 1533) | Referenced as a master, nothing copied | Dependency |
