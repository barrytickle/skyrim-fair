# Skyrim Fair: working notes for Claude

"The Wanderer's Fair": a festival worldspace for Skyrim SE. The plugin is generated, not
hand-made: a C#/Mutagen generator reads `fair.config.json` and writes `dist/SkyrimFair.esp`.

## Rules (from Barry; they always apply)

- **`AGENTS.md`: never use UI or computer-control automation.** Don't click, type into or
  drive the game, the Creation Kit, MO2 or any GUI. Explain the steps and let Barry do
  them. Command-line build, inspection and verification is fine.
- **Never save the ESP through the Creation Kit.** The generator owns it.
- Work on branch `feat/bootstrap-generator`. **Do not merge.** Commit and push each pass,
  ending commit messages with the Co-Authored-By line.
- Keep `docs/AUDIT.md` (a "Current pass" section at the top of the log), `docs/CODEX_HANDOVER.md`
  (gotchas and conventions) and `CREDITS.md` up to date every pass.
- Deploy to Barry's MO2 mod folder after every build. Barry tests in game and reports back
  with screenshots and crash logs. Don't claim anything works in game until he confirms it.
- Keep a pass to what was asked. Stop for Barry's in-game review when a brief says so.

## Build and deploy

```
python tools/make_static_props.py --data "E:/Modlists/Still In Skyrim/stock/Data"   # only when props change
python tools/build_audio.py                                                          # music/, sound-effects/ -> assets/sound/
python tools/build_papyrus.py --ck "C:/Program Files (x86)/Steam/steamapps/common/skyrim"
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json         # run from the repo root
python tools/deploy.py --to "E:/Modlists/Still In Skyrim/mods/Skyrim Fair"
```

- The generator must be deterministic: run it twice and compare the SHA256.
- `deploy.py` copies only changed game files and checks each copy byte-for-byte.
- Git-ignored build output and third-party files: `assets/sound/`, `assets/scripts/`,
  `build/`, `assets/meshes/SkyrimFair/Props/`, `assets/textures/SkyrimFair/Ground/`
  (the Whiterun cobbles, permission pending), `assets/*/Stroti/`, and `external/`.
  `music/` and `sound-effects/` are Barry's untracked audio sources.
- Test setup:
  - MO2 profile "Still in Skyrim Plus" with Community Shaders and Terrain Helper
  - Stock game: `E:\Modlists\Still In Skyrim\stock`
  - Creation Kit (LE, command-line compiler only): `C:\Program Files (x86)\Steam\steamapps\common\skyrim`

## Code map

- `src/SkyrimFair.Generator/`:
  - `FairWorld.cs`: worldspace, terrain and ground painting
  - `FairMarket.cs`: stalls, modules, vignettes
  - `FairStage.cs`
  - `FairVendors.cs`
  - `FairCrowds.cs`: visitors and animals
  - `FairArchery.cs`
  - `FairTowers.cs`
  - `FairAudio.cs`: sound, the stage quest, the band
  - `FairConfig.cs`: every config record
- `src/Papyrus/*.psc`: the stage audio controller, its player alias, and the pen horse.
- `tools/`: props, BSA extraction, NIF preview, audio build, Papyrus build, deploy.
- `docs/`:
  - `AUDIT.md`: the pass-by-pass log, newest first
  - `CODEX_HANDOVER.md`: conventions and gotchas; **read it first**
  - `AUDIO.md`: sound design
  - `STALLS.md`: generated stall directory
  - `DESIGN.md`, `MUSIC.md`, `MCM.md`: plans
- To inspect the built plugin, write small throwaway Mutagen console programs in the
  scratchpad. That's the verification method: read records back, and compare new record
  types subrecord by subrecord with vanilla's.

## Hard-won gotchas (details in CODEX_HANDOVER.md)

- **Terrain layer opacities are shares:** at each vertex they add up to 1 at most, as
  vanilla's do. The game draws the base plus 5 alpha layers a quadrant. Fade edges over
  more than 128 (the vertex spacing).
- **Mutagen leaves out subrecords that vanilla always has** unless they're set. An `SNDR`
  without `CNAM` crashed the game on boot. Set them explicitly, and diff new record types
  against vanilla.
- **Papyrus `Sound` properties must point at a sound marker (`SOUN`)**, not a descriptor
  (`SNDR`), or they load as None.
- **Static props:**
  - Rigid bodies are made FIXED; never null collision links.
  - The BSA LZ4 blocks must be decoded as linked (an old bug corrupted files over 64 KB).
- **Market pieces:**
  - `exact: true` skips the random nudge, for signs, their posts and bars.
  - Modules and vignettes both honour `rotX`/`rotY`.
- **Archery (Solitude package):** needs a persistent target linked ref, plus an unkeyed
  linked ref to a persistent `PatrolIdleMarker`.

## Where we are (2026-09-23, end of session)

Last commit `c8c652a`, deployed; plugin SHA256 `9c65b621fa3e5af5...`. **Barry has not yet
tested this build.** He's done for the night.

**Barry's test of this build (end of 2026-09-23). Do these first next session:**

- ✅ **Working in game:**
  - the stage music plays
  - the cheer follows each song
  - the next song starts on its own
  - the pen horses show the ride prompt but can't be ridden, which is how Barry wants it
- **Stage music far too quiet:** Barry wants "like 100x", so it sounds like a concert.
  Levers:
  - `stage.staticAttenuation` is 0 dB. Is a negative value allowed? Check the
    `SNDR` BNAM limits.
  - raise `stage.minDistance` and `maxDistance` (1500 / 7500)
  - the stage category's `StaticVolumeMultiplier`
  - `SetInstanceVolume` is capped at 1
  - boost the WAVs themselves in `build_audio.py`: normalise or apply gain with a
    limiter, since the songs peak around -3 dBFS and average far lower
  - a second speaker, or a stage output model with a flatter curve
- **The bards don't play instruments.** They stand without playing. Check:
  - whether the copied Candlehearth package really runs
  - whether the idle marker needs to be linked or reserved
  - how vanilla's `BardSongs` quest starts the playing: `IdleLuteStart` etc. may need
    the anim event from a script, or the instrument item
  - Compare with a vanilla inn bard's reference and package setup
- **Crowd ambience:** still wants a bit more volume. It's now 4 dB down; try 0 to 2.
- **Archery sign still wrong:** move it south, or hang it on the left post rather than the
  one with the wreath. That's the `archery_booth` module in `fair.config.json`: posts at
  y 60 and 150, sign `000F0A22` at y 157.5, bar `000533D3` at y 105.

**From the previous build, still to confirm:**

1. **Stage music.** Fixed by pointing the script at sound markers. Does a song start about
   4 s after arriving? Then cheer, 2 s pause, next song? Does the music fade walking away?
   Is nothing left playing after leaving, or after a save and load?
   - If it's still silent: ask Barry to set `bEnableLogging=1` under `[Papyrus]` in
     `profiles\Still in Skyrim Plus\Skyrim.ini` and send
     `Documents\My Games\Skyrim Special Edition\Logs\Script\Papyrus.0.log`. The script
     traces lines starting "SkyrimFairAudio:".
   - Suspects to check next: `Sound.Play` from an XMarker speaker; the quest not starting
     in an existing save.
2. **The band (new, untested).** Three bards on the deck: lute (1840, 5380), drum
   (2048, 5480), flute (2256, 5380), z 134, at vanilla instrument idle markers with copies
   of Candlehearth Hall's bard package. Do they appear and play? Are they on the deck, not
   in it or floating?
3. **Crowd ambience.** It was very quiet, so it's now 4 dB down, from 10. Is the level
   right? (`fairWorld.audio.ambience.staticAttenuation`)
4. **Cobbles** back along the avenue after the opacity-share fix? Barry confirmed the
   worn-ground gradient.
5. **Archery sign:** hanging from the new flat stockade-beam bar, lined up with the posts?
   Also check the booth's arrow bundles, leaning target, pitchfork and broom, which now
   tilt as authored.
6. **Pen horses:** can't be ridden now (`SkyrimFairPenHorse` blocks activation)? They
   already stay in the pen.

**Next likely work:**
- Tune the audio balance.
- Performer cues: bards and dancers stop at a song's end and resume about 5 s in
  (`docs/MUSIC.md` step 3). Professional Dancer is the planned dance system; see
  `CREDITS.md`.
- The music heard outside the Tamriel gate (step 4).
- The MCM (`docs/MCM.md`): the `SkyrimFairAudio*` globals are ready for it.
- The 6 trades not yet placed (`docs/STALLS.md`).
- Navmesh, vendor inventories and quests: only when Barry asks.

**Open items before any public release:**
- permission for the Whiterun stonefloor textures
- the scaffold tower asset's source and licence
- the music and crowd recordings' provenance
- Stroti can't be re-uploaded (see `CREDITS.md`)
- performance: about 123 actors and about 24 real lights near the stage at night
