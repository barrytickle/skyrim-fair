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

## Where we are (end of 2026-09-23)

Plugin `497f5fd2d8ad0198...`, committed (`54a7b97`) and deployed. The day's detail is in
`docs/AUDIT.md`, newest first.

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
2. **Exclude more per-NPC extras (Claude, small).** Add them to `spidPatches`, the same
   generated patch; SPID's log shows every fair NPC getting them:
   - Stealth Detection Fixes' sleep (`0x80B`, `StealthKillDetectionFix_DISTR.ini`) and
     killmove (`0x819`, `_Killmove_DISTR.ini`) abilities, which are pointless on
     invulnerable NPCs
   - Strange Runes' `po3_RUNE_DetectCastNPCAbility` (`StrangeRunes_DISTR.ini`), a script
     on every NPC that runs on each equip

   Check the new Papyrus log for other per-NPC scripts too (the footprints mod's
   `footprintsFootstepsScriptHuman` appears).
3. **Only run the crowd where the player is (Claude, the big one; plan first, then
   build).** Depends on step 1's numbers.
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
5. **Immersion (Barry, added at the end of the day):**
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
6. **Afterwards, from the backlog, as Barry chooses:**
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
