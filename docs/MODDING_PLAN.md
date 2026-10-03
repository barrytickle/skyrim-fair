# Skyrim SE modding plan

Recon for The Wanderer's Fair (2026-10-03, `um scan`). The fair is long past recon; this file
records the setup in one place. Conventions and gotchas are in `CODEX_HANDOVER.md`.

- **Installs:**
  - Steam: `E:\SteamLibrary\steamapps\common\Skyrim Special Edition` (app 489830; the scan
    reported the Creation Kit's app, 1946180, from the same folder), `SkyrimSE.exe` **1.7.104**.
    Untouched vanilla BSAs; the generator copies FaceGen from here.
  - The test game, Wabbajack's stock copy: `E:\Modlists\Still In Skyrim\stock`, `SkyrimSE.exe`
    **1.6.1170**, run through MO2 (`E:\Modlists\Still In Skyrim`, profile "Still in Skyrim Plus").
  - Creation Kit (LE), used only for its command-line Papyrus compiler:
    `C:\Program Files (x86)\Steam\steamapps\common\skyrim`.
- **Engine:** Bethesda Creation Engine (Gamebryo lineage), native x64 C++. Plugins are ESP/ESM/ESL
  records; scripts are Papyrus (`.psc` compiled to `.pex`).
- **Anti-cheat / online:** none. Single player, offline.
- **Saves:** MO2 local saves, `E:\Modlists\Still In Skyrim\profiles\Still in Skyrim Plus\saves`
  (`.ess`, plus `.skse` co-saves).
- **Config:** the profile's INIs (MO2), baked to `Documents\My Games\Skyrim Special Edition\`.
- **Logs:**
  - Papyrus: `Documents\My Games\Skyrim Special Edition\Logs\Script\Papyrus.0.log`
  - SKSE plugins and crash logs (Crash Logger): `Documents\My Games\Skyrim Special Edition\SKSE\`
- **Community route:** plugins plus Papyrus, with SKSE and its plugins on top. Barry's modlist
  runs SKSE, Address Library, Crash Logger, SSE Engine Fixes and several crash fixes
  (LeveledList Crash Fix, SMP-NPC, BGSWaterCollision, Script Effect Archetype, Keyboard Input
  Offset).
- **Chosen route for the fair:** a generated plugin. The C#/Mutagen generator
  (`src/SkyrimFair.Generator`) reads `fair.config.json` and writes `dist/SkyrimFair.esp`; Papyrus
  scripts are built from `src/Papyrus`; assets come from the `tools/` scripts. **The released
  mod needs only Skyrim.esm: no SKSE, no MCM.** That keeps it working on any game version and
  setup. Anything that would need SKSE or a replaced interface file (custom map icons, for
  example) is an optional add-on, never a requirement.
- **Lab:** `tools/deploy.py` into `mods\Skyrim Fair` in MO2. Barry tests in game and reports
  back. **No UI automation** (`AGENTS.md`): agents never launch, click or drive the game, the
  Creation Kit or MO2. Verification is by command line: Mutagen read-backs and diffs against
  vanilla and the last release.
- **Unknowns and risks:**
  - **The test rig is more forgiving than a plain install.** Engine Fixes and the crash fixes
    above can hide crashes a player without them would hit. Open case: a player's CTD loading a
    2.0.1.1 save on 2.0.1.3. The plugins diff clean, so we're waiting on their crash log. If the
    log points at something the rig masks, a plainer test profile would be worth having.
  - **Game versions:** the Steam copy (1.7.104) is newer than the test game (1.6.1170). The
    plugin's header is 1.71 (form version 44); SE 1.5.97 players are untested.
  - The shared modding knowledge base (`um kb search "Skyrim"`) has nothing on Skyrim yet.
