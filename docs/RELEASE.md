# Release: requirements, optional files, and the checklist

What a player needs to run the Wanderer's Fair, what ships as optional, and what must be
settled before any public release. Keep this up to date whenever a dependency, patch or
licence question changes.

## Required

| Requirement | Why |
| --- | --- |
| Skyrim Special Edition / Anniversary Edition | `SkyrimFair.esp` masters `Skyrim.esm` |
| **Holidays** (Nexus Mods SE 1533) | A master of `SkyrimFair.esp`: the festival rope lines, lanterns and festive props are its records (`CREDITS.md`) |

The fair's own scripts use vanilla Papyrus only, so SKSE is needed only for Open Animation
Replacer, below.

## Recommended

| Mod | Without it |
| --- | --- |
| **Open Animation Replacer** (needs SKSE) | Astra's paired folk dance doesn't play. The two folk dancers do the vanilla Cicero dance instead (`fairWorld.folkDance`, `CODEX_HANDOVER.md`) |
| Terrain Parallax 1.5 – 4K2K (Nexus SE 54860) | The ground loses its parallax. It's a visual replacer only, and nothing of it ships |

## Optional compatibility patches: they must ship with any release

**Include these in every release**, as optional files or FOMOD options. Tell players:
**install the patch for each of these mods you use.** Without the patch, the fair
**freezes the game** about a minute after arriving: Papyrus is flooded, the music never
starts, and the bards and dancers stand still.

| Patch (built by the generator, `compatPatches`) | For | What it does |
| --- | --- | --- |
| `SkyrimFair - Maximum Destruction Patch.esp` | Maximum Destruction | Switches off `MD_GoreHumanoidMagic` (`8E6289`, a script on every human NPC that runs on every magic effect) inside the fair's worldspace only |
| `SkyrimFair - Stealth Detection Fixes Patch.esp` | Stealth Detection Fixes (Nexus SE 145336) | Switches off `madDetectionCloak` (`0x817`, a detection cloak SPID puts on every NPC) inside the fair's worldspace only |

- **Why both matter:** at the fair's crowd density, the cloak on every NPC hits every
  other NPC, and Maximum Destruction's script reacts to each hit. That's about 190² events
  a pulse; Barry's log reached 2 million queued. Either patch alone breaks the chain
  between the two mods, and each also helps on its own:
  - The Maximum Destruction patch stops its script reacting at the fair, whatever mod
    applies the effects.
  - The Stealth Detection Fixes patch stops the cloak pulsing at the fair, whatever
    reacts to it.
- **Load order:** each patch after its mod and after `SkyrimFair.esp`. They're
  light-flagged, so they take no load-order slot.
- **Each patch masters its mod**, so a player without that mod must not install it: the
  game won't start with a missing master. Hence optional files, one per mod. A FOMOD can
  tick each one when it detects the mod's plugin.
- **Versions:** each patch overrides one spell as that mod's current version has it. If
  either mod changes that spell, rebuild the patch (`dotnet run` builds it from the mod's
  plugin, `compatPatches[].source`).
- **Other mods like these:** any mod that gives *every* NPC a cloak or a per-effect
  script could do the same at the fair. Add it to `compatPatches` when one turns up, and
  list it here.

## Before any public release

Permissions and provenance (details in `CREDITS.md`):
- [ ] Permission for the Whiterun Mossy Wet Stonefloor textures (the avenue cobbles),
      or replace them.
- [ ] The scaffold tower asset's source and licence.
- [ ] The music and crowd recordings' provenance and terms (`docs/AUDIO.md`).
- [ ] Stroti's Outdoor Toilet can't be re-uploaded: ship it as a dependency, or replace
      it.
- [ ] Astra: how they want to be credited for the folk dance, and permission to ship the
      re-based clips.
- [ ] Maximum Destruction's and Stealth Detection Fixes' authors credited, and their
      patch policies checked (most allow patches).
- [ ] Holidays and Open Animation Replacer credited as requirements.
- [ ] The static crowd figures and `Props/` are rebuilt from vanilla meshes. Confirm that
      shipping them is fine; modified vanilla meshes are common on Nexus, but check.

Files and packaging:
- [ ] `SkyrimFair.esp`, `meshes\` (props, crowd figures, the OAR folk-dance folder with
      its `config.json` files), `textures\`, `Sound\`, `Scripts\`. `tools/deploy.py`
      copies exactly this set.
- [ ] The two compatibility patches, as optional files (above).
- [ ] Remove the retired `SkyrimFairNpcGuard.pex` from the package; nothing uses it now.

Performance (`CLAUDE.md`):
- [ ] The actor count at the fair, and the real lights near the stage at night.
