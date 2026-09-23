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

## Compatibility with per-NPC spell mods: must be solved before any release

**Without this, the fair freezes the game** about a minute after arrival, for anyone using
**Maximum Destruction** or **Stealth Detection Fixes** (Nexus SE 145336). Papyrus floods,
the music stops, and the performers freeze.

- **Why:**
  - Stealth Detection Fixes' SPID gives *every* NPC a detection cloak (`0x817`).
  - Maximum Destruction's gives every human a script that runs on each effect applied
    (`8E6289`).
  - At the fair's crowd density that's about 190² script events a pulse.
- **What doesn't work** (tried and measured):
  - Removing the spells in Papyrus: the guard's events wait in the same flooded queue.
  - ESP patches that add a condition ("not at the fair") to the spells: a Cloak effect
    keeps casting regardless, and the freeze came at the same rate.
  - A SPID filter on `SkyrimFair.esp`: most fair NPCs are runtime copies of their
    templates, which don't belong to the plugin.
- **What works: a SPID exclusion.**
  - Every fair NPC carries the keyword **`SkyrimFairNPC`**.
  - The mods' SPID lines need `-SkyrimFairNPC` in their string-filter field:
    - `StealthKillDetectionFix_Attack_DISTR.ini`:
      `Spell = 0x817~StealthKillDetectionFix.esp|-SkyrimFairNPC|NONE|NONE|NONE|NONE|NONE`
    - `MaximumDestruction_DISTR.ini`, the "MD_Gore Human Magic" line:
      `Spell = 0x8E6289~MaximumDestruction.esp|ActorTypeNPC,Charmed Vigilant,Spellsword,Arch-Curate Vyrthur,Estormo,-SkyrimFairNPC|NONE|NONE|NONE|NONE|100`
- **For a release, choose one:**
  - Ship optional **replacement `_DISTR.ini` files** for those two mods, with the
    exclusion added, installed to overwrite theirs. This needs their authors'
    permission, and a rebuild whenever they change their inis.
  - Document the one-line edit for each mod.
  - Ask both authors to add `-SkyrimFairNPC` upstream: it's harmless to them.
- **Other mods like these:** any mod whose SPID line gives *every* NPC a cloak or a
  per-effect script. Add `-SkyrimFairNPC` the same way, and list the mod here.

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
- [ ] Maximum Destruction's and Stealth Detection Fixes' authors: permission for any
      replacement `_DISTR.ini`, or ask them to add `-SkyrimFairNPC` upstream.
- [ ] Holidays and Open Animation Replacer credited as requirements.
- [ ] The static crowd figures and `Props/` are rebuilt from vanilla meshes. Confirm that
      shipping them is fine; modified vanilla meshes are common on Nexus, but check.

Files and packaging:
- [ ] `SkyrimFair.esp`, `meshes\` (props, crowd figures, the OAR folk-dance folder with
      its `config.json` files, the singers' FaceGen heads), `textures\` (their FaceGen
      tints), `Sound\` (including `Voice\SkyrimFair.esp\`, the singers' lip files),
      `Scripts\`. `tools/deploy.py` copies exactly this set.
- [ ] The per-NPC spell mods' SPID exclusions (above), shipped or documented.
- [ ] Remove the retired `SkyrimFairNpcGuard.pex` from the package; nothing uses it now.

Performance (`CLAUDE.md`):
- [ ] The actor count at the fair, and the real lights near the stage at night.
