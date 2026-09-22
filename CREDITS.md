# Credits and third-party assets

Skyrim Fair is built with help from the Skyrim modding community. This file records third-party mods, assets, authors, licences, and redistribution conditions used or evaluated by the project.

This document is intentionally conservative. An asset should not be copied into Skyrim Fair until its original author and redistribution permission have been confirmed.

## Professional Dancer

**Mod:** Professional Dancer  
**Nexus Mods:** https://www.nexusmods.com/skyrimspecialedition/mods/124608  
**Created by:** contentcat and davidgilbertking  
**Current referenced version:** 1.5.0  
**Licence stated by the author:** CC BY-NC 4.0

Professional Dancer provides the dance system and dance animations being evaluated for fairground stage performances.

The mod page credits the source dance animations to creators whose animations were found on Mixamo.

### Skyrim Fair usage

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

