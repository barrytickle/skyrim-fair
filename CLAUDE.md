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
python tools/cameos/build_voices.py                                                  # cameos/<name>/mono/*.wav + lines json -> build/cameos/ (.fuz)
python tools/build_papyrus.py --ck "C:/Program Files (x86)/Steam/steamapps/common/skyrim"
# after a layout change, for the navmesh's obstacle footprints (then run the generator again):
python tools/make_footprints.py --data "E:/Modlists/Still In Skyrim/stock/Data" --extra "E:/Modlists/Still In Skyrim/mods/Holidays"
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json         # run from the repo root
python tools/deploy.py --to "E:/Modlists/Still In Skyrim/mods/Skyrim Fair"
python tools/package.py --version 1.0.0     # a release: dist/release/SkyrimFair-<version>/ (the Data folder, the .zip, NEXUS_PAGE.md, RELEASE_TODO.md)
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

  Inside a range, append only. Diff FormIDs against the deployed plugin every pass.
- **Rotations:** Skyrim turns a reference about the world's Z, then Y, then X, clockwise.
  To lie on a slope of normal n: `y = -asin(nx)`, `x = atan2(ny, nz)` (`FairExterior.OnGround`).
- **Grass needs a cache in Barry's profile** (`bAllowCreateGrass=0`). The fair's worldspace
  has none: `docs/RELEASE.md`, "Grass-cache setups".
- **Edit scripts:** write Python edit scripts to the scratchpad with the Write tool.
  Bash heredocs break on apostrophes.

## Where we are (end of 2026-09-25): ready for Nexus

Plugin `709eeed250ad09a0` (dev and release builds identical), deployed. The day's detail is in
`docs/AUDIT.md`, newest first. **Release 1.0.0 is packaged and ready to upload**
(`dist/release/SkyrimFair-1.0.0/`, from `python tools/package.py --version 1.0.0`):
- `SkyrimFair-1.0.0.zip` (the main file), `SkyrimFair-1.0.0-Compatibility.zip` (optional:
  the instructions and the SPID Patcher)
- `NEXUS_DESCRIPTION.bbcode.txt` and `NEXUS_CREDITS.bbcode.txt`, to paste into Nexus

**Confirmed in game by Barry today:**
- the shops: all 33 trades, Browse on the counters, the gear stalls at 20x
- the steady show, the roof horse staying put, the keepers at their spots
- the paper lanterns ("incredible"), with no frame-rate cost
- the fair without Holidays ("a proper seamless transition"): `Skyrim.esm` is the only master

**Built, not yet confirmed in game:** the singers looping their cheer on their marks.

**Before uploading (Barry):** a fresh-install test of the zip (untick the dev "Skyrim Fair"
and Holidays, then new game), and the SPID lines checked against the three mods' current files.

**Settled:** every licence (the welcome sign, Mixamo, ElevenLabs voices), the Nexus page text
in Barry's voice, the Nexus links, and the exterior's limits and the missing grass cache
accepted for 1.0.

**Next, if Barry comes back to it:**
- whatever players report after launch
- the cameo hint on the page, if Barry says who Garrick and Claudius are based on
- a lighter lantern model, only if the frame rate ever needs it
- the backlog: the MCM (`docs/MCM.md`), bard animation variants (`docs/BARDS.md`), a grass
  cache for the fair's worldspace

**The show's data carries a 2** (`Songs2`, `CameoIdles2`, ...): a save keeps a script's
property values, so data under old names never reached Barry's save. Rename again (3) when an
update must reach saves already playing (`docs/CODEX_HANDOVER.md`).

**Housekeeping to know:**
- The other agent's `tools/crowd/` changes and `library.json` are uncommitted; they're
  theirs to commit.
- `character-actors/` is Barry's and Astra's, untracked; don't commit it.
- Some records are kept only so no FormID moves: the retired guard's FormList, the
  `SkyrimFairAtFair` global, and `SkyrimFairNpcGuard.pex`. Drop the pex from any release
  package.
- The old Tamriel terrace code (`FairFoundation.cs`) still runs, so its FormIDs stay
  allocated; `FairExterior` drops its references. Don't delete the code without reserving
  its IDs.
- Don't write to Barry's MO2 profile or other mods' folders. The auto-mode classifier
  refuses it, and Barry installs mods himself. `deploy.py` into `mods/Skyrim Fair` is fine.

**Open items before any public release** (the full list is `docs/RELEASE.md`; keep it
current):
- the SPID fix goes on the mod page as instructions (`docs/COMPATIBILITY.md`), never as files
- a grass cache for the fair's worldspace, with the mod-page note
- the Tamriel exterior's limits: the navmesh, LOD, and landscape mods in that area
- Professional Dancer: optional but recommended (Dance.esp, then Pandora); nothing of it ships
- the scaffold tower asset's source and licence
- the music and crowd recordings' provenance
- leave the retired Stroti outhouse files out of the package (replaced by Strifey7's, `CREDITS.md`)
