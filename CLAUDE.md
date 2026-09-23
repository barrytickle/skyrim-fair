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
# after a layout change, for the navmesh's obstacle footprints (then run the generator again):
python tools/make_footprints.py --data "E:/Modlists/Still In Skyrim/stock/Data" --extra "E:/Modlists/Still In Skyrim/mods/Holidays"
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

## Where we are (2026-09-23, backdrop pass)

Committed and deployed; plugin SHA256 `fa26bb754bc2ad46...` (the backdrop, plus the one-sided rocks turned to face the fair). **Barry has not yet tested
it.** He asked for the backdrop pass (his brief is in `docs/AUDIT.md`, "Current pass"),
then **the navmesh next**. Stop for his visual review of the backdrop first.

**Backdrop pass, awaiting Barry's test:** the mountains weren't loading from the middle
of the fair (Persistent + Full LOD doesn't load outside `uGridsToLoad`). They're now
vanilla-style large references in an RNAM table, plus a midground ridge row and a denser
treeline. Clear-weather test: `fw 10a240`. Horizon model:
`docs/images/backdrop_horizon_before_after.png`.

**Archers (latest, awaiting Barry's test):** his log showed the training package running
but stuck in its opening Travel (no navmesh after a load). Its "Use Weapon Location" is
now near self. The hold-package flip is retired. Plugin SHA256 `8cb83cf315ebec13...`.
Papyrus logging is now on in the "Still in Skyrim Plus" profile, so the log at
`Documents/My Games/Skyrim Special Edition/Logs/Script/Papyrus.0.log` can be read
directly (grep `SkyrimFairAudio`).

**Awaiting Barry's test from the last bug-fix pass (`d6605f9`):**
- Signs: Elven Goods (curios group), woodworker (trader group) and the archery booth now
  use the honey sign's layout with boards that carry their own bar.
- Archers: a hold package gated by `SkyrimFairArcherHold`, flipped on and off by the
  stage script on arrival and load, restarts their training package. If they still
  don't shoot: `Papyrus.0.log`, and ask Barry to try `resetai` on one in the console.

**Confirmed working in game:** the stage set, the pen horses, the bards playing, and
the audio levels (don't change them).

**Crowds and orchestra: built and deployed, awaiting Barry's test** (plugin `cbbd6c578f889607...`).
- Crowded Streets audited: it spawns cheap sandbox NPCs by script and deletes them on
  leaving; its cap of 50 is a performance guard. For the fair, placed tiers are better.
- Three crowd tiers switched live by `SkyrimFairCrowdTier` (default 3): stage audience 42,
  wanderers (sandbox) 34, busier stalls 16. Actors go from 126 to 227, all on the navmesh.
- A 12-man orchestra: 5 lutes, 4 flutes, 3 drums.
- Barry to find his comfortable tier by frame rate.

**Navmesh phases 1 to 3: confirmed in game (2026-09-23)** (plugin `fbfdf1eb5f5d84e3...`).
Barry: no issues, nothing out of the ordinary. **It fixed the archers**: they shoot after a
reload. (Their package's opening Travel needed a path. The near-self "Use Weapon
Location" stays; it's harmless.) Next, possibly: auditing bespoke animations Barry and
Astra may supply (the untracked `character-actors/` folder is theirs; don't commit it
unasked).

**Barry's morning list (added 2026-09-23 late; item 1 done this pass, the rest not started):**

1. ✅ *(done this pass, untested)* **Bug: the archers stop their animation after a reload.** The Solitude training package
   (`GuardSolitudeRangedTrainingPackage`) doesn't resume after a load. Check:
   - how Castle Dour's archers survive a reload (package conditions, persistence, linked
     refs, `EvaluatePackage`)
   - otherwise, a load-game nudge from the audio quest's player alias
     (`OnPlayerLoadGame` → `EvaluatePackage` on each archer)
2. **Dancing NPCs.** Audit the external dance mod first:
   `external/Professional Dancer 124608 1.5.0 ....7z`. Its licence is CC BY-NC 4.0; the
   plan is to use it as a dependency, not bundle it (`CREDITS.md`). Find out how it makes
   an NPC dance (spell, package, keyword, script API) and how our stage controller could
   start and stop dancers with the songs.
3. **A large crowd by the bards** to make the cheer and the performance feel real.
   Mind performance: there are already about 123 actors.
4. **Audit "Crowded Streets"** (`Crowded Streets.esp`, in Barry's load order; the mod is in
   `E:\Modlists\Still In Skyrim\mods\`) to see how it adds so many NPCs to towns: leveled
   actors, spawners, packages, performance tricks. Read the plugin with Mutagen,
   read-only.
5. **Navmesh** so NPCs can walk: start looking into generating one. That's a big,
   separate job; plan it before building.
6. **Vendor inventories:** populate the stalls' vendors, with **prices higher than usual
   because it's a festival**. That means vendor factions, merchant chests, buy/sell
   markup. The per-theme stock is in `docs/STALLS.md`.

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
- Performer cues: the bards now start and stop with each song; dancers still to come,
  and the "resume about 5 s in" timing (`docs/MUSIC.md` step 3). Professional Dancer is the planned dance system; see
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
