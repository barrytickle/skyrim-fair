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

## Where we are (2026-09-24, morning pass)

Plugin `27bae193c1a05128...` (stage show B1 cheer lead-in, B2 unvaultable stage walls), deployed. The detail is in `docs/AUDIT.md`, newest first.

**Confirmed in game by Barry (2026-09-24):** the taller palisade and gate, the stage
lanterns and the stage, **crowd culling at margin 512** (a big fps gain; distant
pop-in only when looking for it), and **the varied crowd** (the new face pool). Performance "stable", but he has a frame-generation mod on, so
real frame times are unknown.

**Built and deployed on 2026-09-24, not yet confirmed in game:**
- the folk-dance keyword fix (last night's build, deployed this morning)
- **a taller palisade** (confirmed above): scale 4 (560 tall), measured from sightlines to hide the ground
  to ~5,300 past the wall; the gate is 3.6. 59 panels, with 89 FormIDs reserved
  (`reservedPanels`), so nothing renumbered
- **late-built dressing** (`market.lateDressing`): 18 Holidays lanterns hung 3 into the
  stage rafters, three Whiterun flags behind the back bays, packing gear north of
  `range_storage`, and four parked Helgen carts (gate forecourt x2, north-east camps,
  stables; no room by the south-east camps)
- SPID exclusions for Stealth Detection Fixes' sleep and killmove and Strange Runes'
  rune detection (plan step 2, done)
- **a varied crowd:** the face lists were six Imperial bandits a sex (the "tattoo faces");
  now 245 men's and 145 women's vanilla faces by race weight, with fixed human faces for
  the band and the folk pair. Confirmed by Barry: "it looks good"

**Confirmed in game by Barry:**
- the stage set: songs, cheer, next song, and the audio levels (don't change them)
- the bards playing, and the 12-man orchestra
- the navmesh, which also fixed the archers after a reload
- the backdrop
- the signs (the honey-vendor layout)
- the pen horses
- a visitor walking to a bench and sitting
- **the startup crash is fixed**: the singers' topics are now BardSongs' kind
- **the freeze is fixed by the SPID exclusion patch**, from a save made before the fair
  had been visited. Old saves keep the spells SPID gave before; test SPID changes from
  a clean state (`cow SkyrimFairWorld 0 0` at the main menu, or an earlier save)

**Built, not yet confirmed in game:**
- **Dances:** each dancer's next dance starts as the last ends (6.7 s and 6 s clips).
- **Astra's folk pair:** a 57.6 s clip, six loops, through Open Animation Replacer, at
  (2048, 4420).
- **The stage singers' lip sync:** only on Fiddle and Dragonborn-Approved, songs 2 and 3.
  `set SkyrimFairAudioFirstTrack to 2` starts with Fiddle.
- **The children**, the wanderers, and the archery-bench sitters.

**Switched off:** the static crowd figures (`crowdFiguresEnabled: false`). Barry and the
other agent couldn't fix their glowing. The library, tools and placements stay in the
repo.

## Plan for 2026-09-24 (Barry: "a plan list for tomorrow")

1. **Performance baseline (Barry, in game).** From a clean save, at the square, note the
   fps:
   - all crowd layers on
   - `set SkyrimFairCrowdLayers to 0` (vendors, bards and archers only)
   - `tai` (all AI off)

   Every earlier reading was taken during the script flood, so this is the first true
   measure. It says whether the cost is AI, rendering or scripts.
2. **Done 2026-09-24.** ~~Exclude more per-NPC extras (Claude, small).~~ Add them to `spidPatches`, the same
   generated patch; SPID's log shows every fair NPC getting them:
   - Stealth Detection Fixes' sleep (`0x80B`, `StealthKillDetectionFix_DISTR.ini`) and
     killmove (`0x819`, `_Killmove_DISTR.ini`) abilities, which are pointless on
     invulnerable NPCs
   - Strange Runes' `po3_RUNE_DetectCastNPCAbility` (`StrangeRunes_DISTR.ini`), a script
     on every NPC that runs on each equip

   Check the new Papyrus log for other per-NPC scripts too (the footprints mod's
   `footprintsFootstepsScriptHuman` appears).
3. **Only run the crowd where the player is (Claude, the big one; plan first, then
   build).** **Built and deployed 2026-09-24; Barry: "a huge boost", 77-96 -> 90-120 fps
   (frame generation and DLSS on) at the square, ~130 looking away from the dancers; no
   pop-in. Seated re-sitting not yet reported
   (`docs/AUDIT.md`):
   Barry's baseline (FG on: 77-96, layers 0: 120, `tai`: ~102) showed drawing, not AI,
   is the cost. Per-actor culling from a generated visibility table: 173 switchable, about
   29 off at the square and 38 on average (margin 768). `SkyrimFairCrowdCulling 0` turns
   it off.
   - All nine cells stay loaded, so every actor runs AI all the time.
   - Give each zone its own crowd layers: the dance floor, the market seats, the archery
     range, the wanderers.
   - The stage script enables each zone's layers only within a set distance of the
     player, just beyond clear view, with a fade.
4. **Barry's test list:**
   - Do the dances run on through a song?
   - Do the folk pair turn together, arms meeting?
   - Do the singers' lips move with Fiddle?
   - Do the children look right?
   - Do the seated NPCs sit?
5. **Immersion (Barry, added at the end of the day).** The palisade, gate, packing gear
   and carts were built on 2026-09-24 (see above; the palisade used reserved FormIDs, not
   the visual-scale idea below). The more-life audit is still to do:
   - **A taller palisade, to hide the hills beyond.**
     - It's `fairWorld.palisade.scale` 2.5 now: 350 units tall.
     - **Don't just raise `scale`:** pieces are spaced by width × scale, so their number
       would change and every later FormID would move.
     - Add a visual scale (for example 3.5–4) and keep the spacing on 2.5. The pieces
       overlap more, their count stays the same, and nothing renumbers.
     - Check the corners, the gate tuck, and the navmesh wall margin (the thicker
       collision).
     - Then check what still shows above it: the RNAM mountains should, the near hills
       shouldn't. Use Barry's screenshots.
   - **A bigger gate:** `fairWorld.gatePiece.scale` 2.5. The same caution: keep the gap and
     tuck on the old width, and check how it meets the palisade.
   - **Packing-up gear by the archery range:** crates, chests, sacks, bundled arrows and
     bedrolls beside `range_storage` (-900, 1400).
   - **Travellers' kit:** empty carts and wagons with no horses, parked by the camps, the
     gate forecourt and the stables.
     - Find vanilla carriage and wagon statics (the Helgen-style cart, farm wagons) with
       Mutagen.
     - Build on the existing `cart` and `camp` modules.
     - Place them as new dressing entries **built last** (the late section), or with
       `reserve`, so no FormID moves.
   - **An audit: how to add more life.**
     - Shrubs and flowers inside the walls and along the paths (vanilla shrub statics,
       non-harvestable flora).
     - Grass on the ground textures, and more props: laundry lines, lanterns, hay,
       bunting, tools, food.
     - Small animals: chickens, dogs, goats. Mind the AI cost.
     - Smoke from the cook fires, and the fair's sounds.
     - Report options and cost before building.
6. **The stage show (Barry, end of the day):**
   - **A shorter gap between a song's end and the cheer.**
     - Measured: the songs' files end within 0.06–0.52 s of their last sound, and the
       cheer is audible from 0.05 s. So the gap is the script's.
     - The cheer waits for the song's full length on the update timer (late when Papyrus
       is busy), then stops the song's instance.
     - Fix: a new `CheerLead` property (about 1 s) starts the cheer as the last note
       rings, and the song finishes on its own instead of being stopped. Keep its
       instance for `StopAll`.
     - Add a trace of the cheer's start, to measure.
   - **The singers do something while they sing:** cheer and gesture idles between lines,
     and walking left and right across the deck. Options:
     - a patrol package between two or three deck markers (the deck is its own navmesh
       island; `Say()` still works while walking)
     - OAR-swapped "performing" idles (see the next item)
   - **Generic lip sync instead of the tailored lines.** Barry: the tailored sync "doesn't
     seem to work that much". Options:
     - reuse vanilla bard songs' `.fuz` lip tracks (real singing mouths, not our words) as
       each line's lip file
     - drive the mouth from script with MfgFix's phoneme functions (`mfgfix.dll` is in
       Barry's list): random open and close every 0.2 s while singing, stopped at the
       cheer. Mind Papyrus cost: three singers only
     - Check first whether the tailored lines played at all in Barry's test. Look for
       `Say` in `Papyrus.0.log`; add a trace per line if needed.
   - **Block the stage off from the player completely:** the collision walls already keep
     the performers in. Add a collision box across the steps (and anywhere else the
     player could climb), and check the navmesh stays the performers' island.
   - **Instruments timed to the song:** play drums only in the drum sections and everyone
     elsewhere, from each song's `cues.json` `intensity` (drums and strings, 2.5 s
     resolution).
     - The stage script switches each bard's idle (`PlayIdle` the instrument or
       `IdleStop`) at section changes, timed from the song's start like the singer
       lines.
     - **Faster, more intense playing:** OAR doesn't change playback speed as far as I
       know (check its docs). Instead, generate faster copies of the vanilla loops (the
       HKX codec can retime, as `rebase_folk.py` rebuilds clips), and swap them in with
       OAR for the fair's bards, on a global the script sets from the intensity. Re-send
       the idle when the level changes, so OAR re-evaluates (`docs/BARDS.md`, "Animation
       side").
7. **Dancing (Barry, last thing on 2026-09-23):**
   - **Astra's folk dance never played; fix built, not yet deployed.**
     - OAR's condition was `IsActorBase` on our records, but the folk dancers are
       templated. In game they run on runtime copies: SPID's log shows their bases as
       `FF0021C1` and `FF0012B0`. So OAR never matched, and they did Cicero's dance.
     - Now each has its own keyword (`SkyrimFairFolkDancerMale`/`Female`, the last
       records, so nothing renumbers). OAR's condition is `HasKeyword`, in the same
       format as EVG Conditional Idles.
     - Built (`e166ad798013ffe2...`), committed, but **not deployed**: `deploy.py` hit
       "Permission denied" on `SkyrimFair.esp`, with the game or MO2 holding it. Deploy
       first thing; nothing was copied, so the mod folder is consistent.
     - Then check OAR's in-game menu lists "Skyrim Fair folk dance", and that the pair
       turn together.
     - **Same trap elsewhere:** anything conditioned on a fair NPC's base record fails
       for templated NPCs. Use keywords.
   - **Audit the dance mod for variety:** `external/Professional Dancer 124608 1.5.0
     ....7z` (CC BY-NC 4.0, to use as a dependency, not bundle; `CREDITS.md`).
     - How does it make an NPC dance: spell, package, keyword, script API, OAR or
       behaviour?
     - What dances does it have?
     - Can the stage script start and stop them per dancer, with the songs?
     - Or can its clips feed our existing replay-at-clip-end scheme through OAR, on a
       keyword, as the folk dance does?
   - **The crowd in the instrument timing:** some songs, or sections, have the crowd
     cheering and clapping instead of dancing.
     - Drive it from the same per-song schedule as the instruments (`cues.json`
       `intensity`, or a per-song "crowd mode" in the config).
     - The script switches the dancers' idles (dance / cheer / clap) at section changes,
       timed from the song's start.
   - **Crowd idles Barry picked for the cheering parts:**
     - `IdleCivilWarCheer` (`0F7C8C`), with no conditions
     - `IdleApplaud2`–`5` (`0D8730`–`0D8733`); their only conditions are no shield and
       no torch out
     - `IdleApplaud2`, `3` and `IdleCivilWarCheer` are already the dancers' `CheerIdles`
     - These are one-shot clips like the dances: replay each as it ends (read the clips'
       lengths from the archive, as for the Cicero dances)
8. **Built 2026-09-24.** Stage dressing (Barry, 2026-09-23 night):
   - **Holidays' lanterns hanging from the stage roof:**
     - the same lanterns as the overhead runs: `035D0D`, `035D12`–`035D16`, from
       `Holidays.esp`
     - hung from the roof rafters: `stage.rafters`, u −780 to 780 across, v −540 to 540,
       z 690 in the stage's frame
     - check each lantern model's hanging point, so they hang from the beam and don't
       float or sink into it
   - **Whiterun flags at the back of the stage:** `CityBannerWhiterun01` (`0D2025`), and
     `CivilWarBanner01` (`060166`) poles as the `banner_whiterun` module uses them, along
     the back wall (the deck's back edge is y ≈ 6000, facing the square).
   - **FormID-safe:** the stage is built early, so add both as late-built dressing (the
     late section, appended) or with `reserve`, never inside the stage's own records.
9. **Afterwards, from the backlog, as Barry chooses:**
   - vendor inventories at festival prices (`docs/STALLS.md`)
   - the MCM (`docs/MCM.md`)
   - the 6 trades not yet placed
   - the music heard outside the Tamriel gate
   - bard animation variants (`docs/BARDS.md`, "Animation side")
   - the release checklist (`docs/RELEASE.md`)

**Housekeeping to know:**
- The other agent's `tools/crowd/` changes and `library.json` are uncommitted; they're
  theirs to commit.
- `character-actors/` is Barry's and Astra's, untracked; don't commit it.
- Some records are kept only so no FormID moves: the retired guard's FormList, the
  `SkyrimFairAtFair` global, and `SkyrimFairNpcGuard.pex`. Drop the pex from any release
  package.

**Open items before any public release** (the full list, with requirements and optional
files, is `docs/RELEASE.md`; keep it current):
- **ship the SPID exclusion patches** (`dist/spid/`, for Maximum Destruction and Stealth
  Detection Fixes) as optional files. Without them the fair freezes the game for those
  mods' users. The ESP condition patches were tried and don't work
- permission for the Whiterun stonefloor textures
- the scaffold tower asset's source and licence
- the music and crowd recordings' provenance
- Stroti can't be re-uploaded (see `CREDITS.md`)
- performance: about 190 actors and about 24 real lights near the stage at night (tomorrow's plan)
