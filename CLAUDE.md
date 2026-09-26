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
build/texvenv/Scripts/python tools/make_cobble.py --data "E:/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"   # the cobbles (numpy, Pillow)
build/texvenv/Scripts/python tools/make_bunting.py --data "E:/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"  # the bunting's colourways
build/texvenv/Scripts/python tools/make_lantern_colours.py                                                          # the paper lanterns' colours
python tools/bards/build_tempo.py --data "E:/Modlists/Still In Skyrim/stock/Data"     # fast and held instrument loops (when a speed changes)
python tools/cameos/build_voices.py                                                  # cameos/<name>/mono/*.wav + lines json -> build/cameos/ (.fuz), the Passport's two lines too
python tools/cameos/borrow_lips.py --data "E:/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"   # off (no member has lipsFrom): SSE Engine Fixes is recommended instead
python tools/build_papyrus.py --ck "C:/Program Files (x86)/Steam/steamapps/common/skyrim"
# after a layout change, for the navmesh's obstacle footprints (then run the generator again):
python tools/make_footprints.py --data "E:/Modlists/Still In Skyrim/stock/Data" --extra "E:/Modlists/Still In Skyrim/mods/Holidays"
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json         # run from the repo root
python tools/deploy.py --to "E:/Modlists/Still In Skyrim/mods/Skyrim Fair"
python tools/package.py --version 2.0.1.3   # a release: dist/release/SkyrimFair-<version>/ (the Data folder, the .zip, NEXUS_PAGE.md, RELEASE_TODO.md)
```

- The generator must be deterministic: run it twice and compare the SHA256.
- `deploy.py` copies only changed game files and checks each copy byte-for-byte.
- Git-ignored build output and third-party files: `assets/sound/`, `assets/scripts/`,
  `build/`, `assets/meshes/SkyrimFair/Props/`, `assets/textures/SkyrimFair/Ground/`
  (the cobbles, from `tools/make_cobble.py`), `assets/*/Stroti/` (no longer used), and `external/`.
  `music/` and `sound-effects/` are Barry's untracked audio sources.
- Test setup:
  - MO2 profile "Still in Skyrim Plus" with Community Shaders and Terrain Helper
  - Stock game: `E:\Modlists\Still In Skyrim\stock`
  - Creation Kit (LE, command-line compiler only): `C:\Program Files (x86)\Steam\steamapps\common\skyrim`

## Code map

- **`songs.config.json`**: the whole stage show, pointed to by `fair.config.json`
  (`fairWorld.audio.stage.songsFile`). Barry edits it:
  - the songs in playlist order, each with two timelines, an entry a line:
    `drums: [second, rest|play|intense]` and `crowd: [second, dance|clap|cheer]`
  - the instruments by name
  - the band and orchestra
  - the crowd's moves: the dance, clap and cheer idles with their clip lengths
  - `fast` (each instrument's fast-loop speed), `singerMoves` (sing / rest / end) and `singerGap`
  - `moreDances` (Professional Dancer's dances, used only when Dance.esp is loaded) and `fireworks` (the fair's own, after each song)

- `src/SkyrimFair.Generator/`:
  - `FairWorld.cs`: worldspace, terrain and ground painting
  - `FairMarket.cs`: stalls, modules, vignettes
  - `FairStage.cs`
  - `FairVendors.cs`
  - `FairCrowds.cs`: visitors and animals
  - `FairArchery.cs`
  - `FairTowers.cs`
  - `FairAudio.cs`: sound, the stage quest, the band
  - `FairExterior.cs`: the compound's exterior in Tamriel (the gate, the wall, silhouettes, the outside show), own FormID range (`exterior.formIdBase`)
  - `FairLife.cs`: palisade banners and ropes, plants, props, smoke, animals, on their own FormID range (`life.formIdBase`)
  - `FairCameos.cs`: Garrick Sol V and Claudius Vale, their lines (`lineSlots`: lines added after release go at the end of the range), rounds, posters, the roof horse
  - `FairShops.cs`, `FairPaperLanterns.cs`, `FairHolidaysFree.cs`: the shops, the fair's own lanterns, bunting and props (no Holidays master)
  - `FairPassport.cs`: the Fair Passport quest (built last, range 0xB0000)
  - `FairConfig.cs`: every config record
- `src/Papyrus/*.psc`: the stage script (`SkyrimFairAudioScript`: the show, the music duck, the re-seat), the cameos' and the Passport's line fragments, the talk perk (`SkyrimFairTalkDuck`), the Passport (`SkyrimFairPassport`), the counters, the keepers, the horses.
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
- **FormID ranges.** New things that must not renumber anything go in a range of their own:
  switch `mod.ModHeader.Stats.NextFormID`, build, then put it back.
  - `0x10000` crowd figures (off)
  - `0x20000` `fairWorld.life`
  - `0x30000` `exterior`
  - `0x40000` the singers' anchor (`singers.anchor`; off with the steps)
  - `0x50000` the added songs (`"added": true` in `songs.config.json`)
  - `0x60000` the cameos (`cameos`: Garrick Sol V, Claudius Vale, their rounds, the posters, the roof horse)
  - `0x70000` the companions' finder (`companions`)
  - `0x80000` the lights at the lanterns (`lights`)
  - `0x90000` the shops (`shops`: factions, merchant chests, buy lists)
  - `0xA0000` the paper lanterns (`paperLanterns`: a texture set and a static per shape and colour)
  - `0xB0000` the Fair Passport (`passport`: the quest, its items, Claudius's two lines)

  Inside a range, append only. Diff FormIDs against the deployed plugin every pass.
- **Rotations:** Skyrim turns a reference about the world's Z, then Y, then X, clockwise.
  To lie on a slope of normal n: `y = -asin(nx)`, `x = atan2(ny, nz)` (`FairExterior.OnGround`).
- **Grass needs a cache in Barry's profile** (`bAllowCreateGrass=0`). The fair's worldspace
  has none: `docs/RELEASE.md`, "Grass-cache setups".
- **Edit scripts:** write Python edit scripts to the scratchpad with the Write tool.
  Bash heredocs break on apostrophes.

## Where we are (end of 2026-09-26): 2.0.1.3 in progress

**Released on Nexus** since 2026-09-25, updated from player reports. Versions so far: 1.0.0,
2 (the grass fix), 2.0.1 (volume), 2.0.1.1 (music at 60%, louder voices), 2.0.1.2 (the seats,
the herbalist's sign, the music ducking when you talk to anyone: confirmed by Barry, packaged,
changelog written; Barry was uploading it). Bump the version for every release and package with
`tools/package.py --version <v>`. SSE Engine Fixes is recommended on the page (a pinned post)
for the cameos' lip sync on plain SE.

**2.0.1.3, built and deployed, not yet packaged** (plugin `231fe0d02971ba36`, deterministic;
detail in `docs/AUDIT.md`'s current pass):
- **Claudius's new voice:** Barry re-recorded all 16 lines "to make him more of a character",
  and added a 17th ("Oh for fuck sake, how'd that horse get up there?", Barry's choice to keep).
  Cameo lines now have `lineSlots` (Garrick 12, Claudius 16): lines past it take FormIDs at the
  end of the cameos' range. Line 17 is `060058`.
- **The Fair Passport** (Barry's pick from the minigame ideas): Claudius hands it out when first
  spoken to (voiced: "Everyone at this fair requires a passport..."). Journal objectives are the
  stamps: a whole song, Garrick, Claudius, 5 visitors talked to, the roof horse seen. The hand-in
  (voiced: "Every stamp. All in order...") gives Claudius's Seal of Approval (gold necklace,
  Fortify Barter 10%) and the Deed to the Roof Horse. The passport is a leather journal held by
  a Quest Object alias (can't be dropped); Claudius takes it back.
- **An xEdit error a player found:** the sandbox's COC marker (interior block 9) was in a
  Persistent group without the Persistent flag. Fixed (one byte); no other ref has the problem.
- **Claudius's new face** (Barry: too young; "monk style hair, bit grey"; a rarely met face):
  his donor is now the Dark Brotherhood's "Nervous Patron" (087B8B): Nord, balding, bright grey,
  clean-shaven.
- **Garrick's new face** (Barry: "more wood elf than demon elf"): his donor is now Edorfin
  (01E957), a Wood Elf cut from the game (placed nowhere): clean skin, black hair.

**Barry's tests for tomorrow:**
1. Claudius's new voice: level beside Garrick, lip sync, line 17 turning up. And his new face:
   old enough, the hair monkish enough (if not, another donor is one config line), no dark face.
   Garrick's new face too: less sinister, no dark face.
2. The Passport, start to finish on a save that hasn't had it: the hand-over (voice, journal,
   his stamp), a whole song, Garrick, the chat counter (0/5 to 5/5 in the journal), the horse
   stamp when looking at the roof, then the hand-in and the rewards. Check the passport can't
   be dropped.

**Waiting on Barry's decision:**
- **Visitor lines: 10 voiced fairgoers** (Barry chose voiced lines, 2026-09-26). His set is in
  `cameos/fair-visitors/`: `fair-visitors.json` (`"fairgoers"`: file, voice, text), recordings
  in `mono/` (01-10.wav, mono 44.1 kHz, 4-9 s; the ElevenLabs originals in `original/`). Each line
  is a different character (`voice`: young_nord_male, older_nord_female, breton_male,
  drunk_nord_male, imperial_female, young_breton_female, older_imperial_male, rough_nord_female,
  cheerful_breton_male, older_nord_male). **Plan (to confirm with Barry, then build):**
  - 10 featured fairgoers built like the cameos: their own NPC records with faces copied from
    vanilla NPCs of the right race, sex and age (`FairSingers.CopyFace`), each with its own voice
    type, a Hello topic line per entry (more entries with the same `voice` later: random among
    them), played when talked to; the talk duck and the Passport's chat stamp should count them
  - no face-pool problem (the pool's visitors keep their vanilla voices); only these 10 lose
    vanilla barks
  - placed where their lines fit: the drunk and the rough Nord woman by the mead stalls, the
    archery one by the range, the bard-watcher at the stage, the older Nord man with a view of
    the roof horse, the others about the avenue
  - `build_voices.py` builds them like the cameos (build/cameos/Fairgoers/<voice>/); their own
    FormID range, 0xC0000, after the Passport
- A quest marker on Claudius for "Return the Fair Passport" (left out to keep the pass small).
- Then package 2.0.1.3 and write its changelog.

**Parked ideas** (Barry, 2026-09-26): the Festival Spirit buff (a whole song gives a skill-rate
buff, 2/10), the archery contest (5/10), the Shout toss (needs a lent scroll: Unrelenting Force
comes late in the main quest), and a rhythm game: **Bard Hero** (Nexus 186544) already plays
Clone Hero chart folders, so charts of the fair's songs as an optional add-on would be ~3/10.

**Compatibility answered for players** (`docs/COMPATIBILITY.md`, "Checked"): Elysium Estate,
Whiterun Manor, Pondside Cottage and JK's Whiterun Outskirts are all compatible. The fair's Tamriel footprint is cells (-3,-4), (-3,-3), (-2,-4), (-2,-3), with no
landscape edits; about 37 vanilla refs there are disabled or sunk.

**The show's data carries a 2** (`Songs2`, `CameoIdles2`, ...): a save keeps a script's
property values, so data under old names never reached Barry's save. Rename again (3) when an
update must reach saves already playing (`docs/CODEX_HANDOVER.md`). Saves also keep placed
refs' positions: `Reseat()` puts pieces back once per `seatSwap.layoutVersion` (now 4).

**Housekeeping to know:**
- The other agent's `tools/crowd/` changes and `library.json` are uncommitted; they're
  theirs to commit.
- `character-actors/`, `cameos/` (Barry's recordings and lines file), `music/` and
  `sound-effects/` are untracked; don't commit them.
- Some records are kept only so no FormID moves: the retired guard's FormList, the
  `SkyrimFairAtFair` global, and `SkyrimFairNpcGuard.pex`. Drop the pex from any release
  package.
- The old Tamriel terrace code (`FairFoundation.cs`) still runs, so its FormIDs stay
  allocated; `FairExterior` drops its references. Don't delete the code without reserving
  its IDs.
- Don't write to Barry's MO2 profile or other mods' folders. The auto-mode classifier
  refuses it, and Barry installs mods himself. `deploy.py` into `mods/Skyrim Fair` is fine.
- Four Oldrim Creation Kit files were copied into `E:\SteamLibrary\steamapps\common\Skyrim`
  while chasing lip sync; harmless, removable if Barry wants.

**Open items** (the full list is `docs/RELEASE.md`; keep it current):
- the SPID fix stays on the mod page as instructions (`docs/COMPATIBILITY.md`, and the optional
  Compatibility download), never as plugin files
- a grass cache for the fair's worldspace
- the Tamriel exterior's limits: the navmesh, LOD, and landscape mods in that area
- Professional Dancer: optional but recommended (Dance.esp, then Pandora); nothing of it ships
- the scaffold tower asset's source and licence; the music and crowd recordings' provenance
- leave the retired Stroti outhouse files out of the package (replaced by Strifey7's, `CREDITS.md`)
