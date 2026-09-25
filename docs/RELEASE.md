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
| **Professional Dancer** (Nexus SE 124608), optional but recommended. Needs `Dance.esp` enabled (its masters: SkyUI, UIExtensions) and **Pandora or Nemesis run after installing** (Pandora picks up its FNIS list itself; it doesn't appear in Pandora's mod list) | The crowd's dancers use only the vanilla Cicero dances. With it, while dancing, they take five of its dances in turn (its animation events; `songs.config.json` `moreDances`). The fair checks for it at runtime and ships nothing of it |

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
- **The patch (built):**
  - The generator writes copies of those two `_DISTR.ini` files, each regenerated from
    the mod's current file with the exclusion added (`spidPatches`, `dist/spid/`).
  - Installed above the mods, they override the mods' own files, which are never
    edited.
  - After a mod update, rebuild. If the line has changed shape, the build fails instead
    of shipping a stale copy.
- **For a release (Barry, 2026-09-25): instructions, not files.** The copies carry the
  mods' other lines too, so shipping them would redistribute their files. Instead, the mod
  page tells players which line to edit, word for word: `docs/COMPATIBILITY.md`. The
  generated copies are for Barry's own game only, and are never packaged.
  - Before release, check each quoted line against the mod's current file.
  - Also worth doing: ask the authors to add `-SkyrimFairNPC` upstream. It's harmless to
    them, and saves players the edit.
- **Earlier plan (dropped):**
  - Ship each copy as an optional file for users of that mod ("Skyrim Fair – Maximum
    Destruction SPID patch", "… Stealth Detection Fixes SPID patch"), installed with a
    higher priority than the mod.
  - Rebuild the patches whenever either mod changes its ini. Or ask both authors to add
    `-SkyrimFairNPC` upstream, which is harmless to them and makes the patches
    unnecessary.
- **Also excluded (2026-09-24), per-NPC extras that do nothing on invulnerable fair NPCs:**
  - `StealthKillDetectionFix_DISTR.ini`: `0x80B`, `madStealthKillFixSpellSleep`
  - `StealthKillDetectionFix_Killmove_DISTR.ini`: `0x819`, `madStealthKillFixSpellKillmove`
  - `StrangeRunes_DISTR.ini` (Strange Runes): `0x68855`, `po3_RUNE_DetectCastNPCAbility`, a
    script on every NPC that runs on each equip

  So a release ships five patched files, grouped per mod (Stealth Detection Fixes three,
  Maximum Destruction one, Strange Runes one); each needs its author's permission.
- **Other mods like these:** any mod whose SPID line gives *every* NPC a cloak or a
  per-effect script. Add `-SkyrimFairNPC` the same way, and list the mod here.

## Grass-cache setups: no grass at the fair unless a cache ships

- **Who:** anyone whose grass loads only from a pregenerated cache: No Grass In Objects
  (NGIO) or Grass Cache Helper NG with a grass-cache mod, as in most big modlists
  (`bAllowCreateGrass=0`). Players without a cache get grass live and need nothing.
- **What they'd see:** the fair's ground with no grass at all. Nothing breaks.
- **The fix, to ship:** the fair's own cache, `Grass\SkyrimFairWorldx....y....cgid` for its
  9 cells, generated once by a precache run on Barry's setup (only the `SkyrimFairWorld`
  files kept). Harmless to players without a cache setup. If a user's grass mod changes
  the grass records' density, the fair's grass is a little off for them (cosmetic).
- **The note for the mod page:** "If you use a grass cache (NGIO / Grass Cache Helper NG),
  install the included grass cache files, or the fair's ground will have no grass. If you
  regenerate your own cache, do it with Skyrim Fair installed and it will be included."
- **Testing it without a cache** (Barry's setup): `SetGrassLoadCreate = 0` in
  `SKSE\Plugins\GrassCacheHelperNG.ini` and `bAllowCreateGrass=1` in the profile's
  `skyrim.ini` [Grass]; put both back after.

## The Tamriel exterior: known limits

- Tamriel's navmesh isn't cut by the compound's wall, so vanilla NPCs and animals may walk
  into it. The fix is a navmesh edit in the CK (on a copy, never saving the generated
  ESP) or a generated navmesh override. Decide before release.
- No LOD: the compound shows only within loaded cells. DynDOLOD / xLODGen object LOD for
  the palisade, stage and towers would show it from the mountains.
- It sits on vanilla ground at (-2..-1, -4..-3): landscape mods that change that area
  can bury or float the wall. Check the popular ones.

## Before any public release

Permissions and provenance (details in `CREDITS.md`):
- [x] The avenue cobbles no longer use Whiterun Mossy Wet Stonefloor (Nexus 99294):
      they're vanilla `wrstonefloor01` with the fair's own parallax map (2026-09-25,
      `tools/make_cobble.py`). Build them from Bethesda's archives (the Steam install).
- [x] **The singers' face tints** (rebuilt from the Steam install's archives, 2026-09-25; `singers.faceArchives`) (`textures\actors\character\FaceGenData\FaceTint\SkyrimFair.esp\*.dds`)
      are copied from Vanilla Remastered - The New Normal's archives (Nexus 153879), not
      Bethesda's. Rebuild them from the vanilla `Skyrim - Textures*.bsa` (Steam's install)
      before release. Their face meshes are already Bethesda's.
- [ ] Leave the old procedural cobble (`textures\SkyrimFair\SkyrimFair_Cobble01*.dds`) out of
      the package: the plugin doesn't use it, and it's only left over in the deploy folder.
- [x] The scaffold tower: "scaffold" by Js_TuruokaJunpei (Sketchfab), CC BY 4.0. Credit and the changes made are in `CREDITS.md`.
- [x] The music and crowd sounds: made with Suno on the Pro plan (Barry, 2026-09-25), so Barry owns them. Credit "made with Suno" anyway.
- [x] Stroti's Outdoor Toilet can't be re-uploaded: **replaced** (2026-09-25) by "Outhouse"
      by Strifey7 (Sketchfab, CC BY 4.0), converted by Barry (`meshes\barry_outhouse\`).
- [ ] **The welcome sign** ("Low-Poly Wooden Sign made of Three Planks" by JeffK, Sketchfab Standard):
      ask the author to confirm CC BY for a free Skyrim mod. Until then, leave
      `meshes\barry_fair_sign\` and `textures\barry_fair_sign\` out of the package, and set
      `exterior.fairSign.enabled` to false for a release build.
- [ ] Leave `meshes\Stroti\` and `textures\Stroti\` out of the package: the plugin no longer
      uses them (they're still in Barry's deploy folder).
- [ ] The cameos' voice recordings (`cameos/`): record their source and terms in `CREDITS.md`.
- [ ] The singers' cheer (a Mixamo clip, `SkyrimFairBardSinger`): check Adobe's current Mixamo terms for shipping it inside a mod.
- [x] The folk dance: Barry's, made with ChatGPT ("Astra"). Nothing to ask.
- [x] Stealth Detection Fixes, Maximum Destruction and Strange Runes: no permission needed.
      Players make the one-word edits themselves, from `docs/COMPATIBILITY.md` on the mod page.
      Optionally, ask the authors to add `-SkyrimFairNPC` upstream.
- [ ] Holidays and Open Animation Replacer credited as requirements.
- [ ] The static crowd figures and `Props/` are rebuilt from vanilla meshes. Confirm that
      shipping them is fine; modified vanilla meshes are common on Nexus, but check.

Files and packaging: **`python tools/package.py --version <x.y.z>`** builds all of it
(2026-09-25). It builds a release plugin (the release switches, into `build/release/plugin`,
twice, compared) and copies the deploy set less `EXCLUDE`. It checks every mesh the plugin names
from the fair's own folders is in the package, then writes the `.zip` (Data layout at its top
level), `NEXUS_PAGE.md` and `RELEASE_TODO.md` into `dist/release/SkyrimFair-<version>/`.
- [ ] `SkyrimFair.esp`, `meshes\` (props, crowd figures, the OAR folk-dance folder with
      its `config.json` files, the singers' FaceGen heads), `textures\` (their FaceGen
      tints), `Sound\` (including `Voice\SkyrimFair.esp\`, the singers' lip files),
      `Scripts\`. `tools/deploy.py` copies exactly this set.
- [ ] Updating: script property values are saved in the save, so an update that changes the show's data (songs, idles) needs a new game or a cleaned save. Say so on the mod page, or move such data to new property names per release.
- [ ] Ship `Seq\SkyrimFair.seq` (generated next to the plugin): without it the cameos' greetings stay dead.
- [ ] **Not** the SPID copies (`dist/spid/`, and the `*_DISTR.ini` that `deploy.py` puts at the
      mod folder's root): they're for Barry's game only. Paste `docs/COMPATIBILITY.md`
      onto the mod page instead, with its lines checked against the mods' current files.
- [ ] Remove the retired `SkyrimFairNpcGuard.pex` from the package; nothing uses it now.
- [ ] The fair's grass cache (`Grass\SkyrimFairWorld*.cgid`), from a precache run, with the
      mod-page note for grass-cache users (see "Grass-cache setups" above).

Performance (`CLAUDE.md`):
- [ ] The actor count at the fair, and the real lights near the stage at night.
