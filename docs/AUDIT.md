# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: the singers step across the deck (2026-09-25)

### Later: 2.0.1.2: the music ducks when you talk to anyone at the fair

Barry: "Are we able to detect when any of the fair visitors are talking when you interact with
them? And also reduce the music there?", "any interaction with an npc", "but keep it seperate
from garrick and claudius".
- A visitor's greeting opens no menu (`Utility.IsInMenuMode` misses it), and vanilla's greeting
  lines aren't ours to fragment. So we catch the key press instead: **`SkyrimFairTalkPerk`**
  (`0xA0041`), a hidden player perk in the shape of vanilla **PlayerWerewolfFeed**
  (`02BA1D`):
  - one `AddActivateChoice` entry: `DATA 0e0902`, `EPFT 04`, `EPF3` 3 (RunImmediately +
    ReplaceDefault), no label (no `EPF2`), no spell (no `EPFD`), as vanilla's spell-less
    entries
  - tab 0 (the player): GetInWorldspace SkyrimFairWorld, IsSneaking 0 (pickpocketing stays
    vanilla)
  - tab 1 (the target): HasKeyword SkyrimFairNPC, GetDead 0 (looting stays vanilla), and
    GetIsID not Garrick and not Claudius (their own lines duck it)
  - fragment `SkyrimFairTalkDuck` (VMAD v5, format 2, Local, extra bind data 2, fragment #0,
    unknown2 1, as vanilla's): it sets its own global `SkyrimFairTalkDuckUntil` (`0xA0040`)
    to now + 5 s, then `akTargetRef.Activate(akActor, True)`, so the talk, greeting or shop
    runs as usual
- The stage script gives the player the perk (`FairTalk`) and ducks while `TalkDuckUntil` is
  ahead, alongside the cameos and menus.
- Plugin `c28f4efb1f9408db`, deterministic; the FormID diff shows only the two new records.
  Deployed. **2.0.1.2** packaged (with the seats, below).
- **To test:**
  - pressing E on a visitor dips the music while they answer, and talking still works
  - shops open; sneaking still pickpockets
  - Garrick and Claudius as before

### Later: 2.0.1.2: seated visitors placed on their seats

A Nexus player's screenshot (Barry): at a round table (`CommonTableRound01`), one visitor sat in
the air beside her stool, another in the air with her stool behind her.
- Each stool had one sitter and a vanilla sit package (`defaultSitLinkedRefNoConv`, the seat as
  an unkeyed linked ref). But **each sitter started 70 units out** from the seat, meant to walk
  over and sit. Skyrim often snaps a loading NPC straight into the sit pose where it stands,
  and the crowd culling reloads visitors as the player moves. Both women sat at their start
  spots.
- `fairWorld.crowds.sitStart` (default 0): sitters start **on** their seat (stools exactly,
  bench places up to 23 apart as before), still facing the table. Positions only; no FormID
  change.
- (The four "PatrolIdleMarker seats" were the archers' idle markers, correctly linked.)
- Barry's next screenshot: the two still sat forward of their stools, "they've got a leaning
  animation, but they're not leaning on anything". **WoodenBarStool** (`074EC6`) gives the
  bar-counter lean, and there's no counter. So **`fairWorld.seatSwap`**, last of all (FormIDs
  stay):
  - every WoodenBarStool becomes **CommonChair02** (`0B9C04`), a plain chair with the normal
    sit and drink poses
  - each chair turns to face its table: front = local +Y, as 33 vanilla CommonChair02s sit at
    tables. 12 face a table, 2 their sitter's way, 11 spares keep their angle.
- Plugin `bd03edbc07fb2f28`, deterministic, no FormID change. Deployed. **2.0.1.2** packaged
  (the seats on start, and the chairs).
- **To test:** the seated visitors upright on chairs at the tables, drinking, not leaning on
  thin air.
- Barry's next screenshot, in his existing save: the sitters were on the chairs, but the chairs
  faced away from the table, so the drinking pose rested on nothing. The plugin has them
  facing their tables: the table at local +Y of each; CommonChair02's backrest on -Y; its
  furniture marker heading 0 (+Y). **The save kept the old stools' angles and the visitors'
  old spots.** "the issue is other people will have saves now".
- **The fix, for every save:** the stage script's `Reseat()` runs once per
  `seatSwap.layoutVersion` (1), on arriving at the fair:
  - `SetAngle` turns each of the 25 chairs to its heading (`SeatChairs`, `SeatYaw`)
  - each of the 39 seated visitors (`Sitters`, the archers excluded) gets
    `MoveToMyEditorLocation` then `EvaluatePackage`
  - `seatLayoutDone` records it, so bumping the version re-runs it everywhere
- Plugin `a26ee022ef557f8c`, deterministic, no FormID change. Deployed. **2.0.1.2**
  repackaged.
- Barry, after layout 1: the chairs faced their tables and one visitor sat properly, but another
  sat beside his chair. He was already seated when his chair was turned under him, and a sitting
  NPC keeps his old seat's transform. **Layout 2:** `Reseat()` now disables the seated first,
  then turns the chairs, then moves each back to his seat, `Enable()`s him and runs
  `EvaluatePackage`, so he sits fresh (the culling re-hides any it doesn't want shown). The
  version bump re-runs it in saves that ran layout 1. Plugin `d959dfe43d9e3a1b`. Deployed,
  **2.0.1.2** repackaged.

### Later: the lip sync decision: SSE Engine Fixes

Barry, on vanilla with 2.0.1.4: "claudius has stopped working", though his lip files were
byte-identical to 2.0.1.3's, which worked. "maybe we should go with the SSE version".
- So plain SE's lip sync is **intermittent**: the same files animate in one session and not the
  next. That's the "desync" bug Engine Fixes' `bLipSync` fixes, and it's likely what the Nexus
  commenter saw on other NPCs too. Borrowing vanilla lips can't make it dependable.
- **Decision:** the cameos keep their own lip tracks, generated from their words, so they're
  properly in sync where Engine Fixes is installed. SSE Engine Fixes stays recommended on the
  page. `borrow_lips.py` stays in the repo, off (no `lipsFrom` in the config).
- The rebuilt voices equal 2.0.1.1's: the audio byte-identical, the lips the same kind (the lip
  generator isn't byte-repeatable). So **2.0.1.1, on Nexus, is the version**; only the page
  text changes. Deployed; the test builds (2.0.1.2-2.0.1.4, LipTest) were removed.

### Later: 2.0.1.4: spoken lines only for the borrowed lips

Barry, on vanilla with 2.0.1.3: "claudius worked, garrick didn't". So the length theory was
wrong: Claudius's 95% lips animated, Garrick's shorter 80% ones didn't.
- The difference is the sources. Four of Garrick's twelve lips came from `bardsongs_*` (the
  bards' sung songs, which vanilla plays through performance scenes). Claudius's, and the bard
  *poem* lip that worked in the LipTest, all came from spoken dialogue. Headers didn't separate
  the sets.
- `borrow_lips.py`: `lipsExclude` (default `["bardsongs"]`), and Garrick at `lipShare` 0.95
  like Claudius. Garrick's lips now come from spoken lines (CW scenes, general dialogue, one
  Bards College poem). Claudius's set is unchanged: the one that worked.
- **2.0.1.4** packaged for the vanilla test.

### Later: 2.0.1.3: borrowed lips shorter than the line (a threshold test)

Barry: "for some reason 2.0.1.2 didn't work".
- Checked: the zip's 28 lips are vanilla's own, byte for byte (line 01 = its source), and
  Vortex deployed all 56 voice files into the game's `Data`, all matching 2.0.1.2. So plain
  SE read the borrowed vanilla lips and still didn't animate them.
- The one lip that worked (the bard's, LipTest) lasted **4.69 s on a 6.22 s line**, about 75%.
  The 2.0.1.2 lips, and our own SE-generated ones, match the audio's length. **Working
  theory:** plain SE skips a lip that runs as long as its sound (Engine Fixes calls its fix a
  "desync" fix).
- `borrow_lips.py` takes `lipShare`: the share of each line's length the borrowed lip should
  run. **Test: Garrick at 80%, Claudius at 95%** (within 19 and 23 ms).
- Plugin unchanged; deployed; **2.0.1.3** packaged for the vanilla test.

### Later: 2.0.1.2: borrowed vanilla lip tracks, the same length as each line

Barry: "could we do it with the fix you used for garrick? Just use another lip sync, i honestly
didn't notice it was out of sync. The only important caveat is, can it be the same length as
their voice lines?"
- First, the Oldrim Creation Kit: with original Skyrim installed (`E:\SteamLibrary\...\Skyrim`),
  it starts once its four files sit next to `TESV.exe`, and FonixData is in its
  `Data\Sound\Voice\Processing`. But `-GenerateSingleLip` exits 53 however the audio is given
  (absolute, relative, inside `Data\Sound\Voice`). The Oldrim CK makes lips only from its
  dialogue window. The four CK files stay in Barry's Skyrim folder (harmless); the test audio
  was removed.
- **`tools/cameos/borrow_lips.py`** (after `build_voices.py`):
  - each cameo line takes the lip track of the unused vanilla line closest to it in length,
    from a voice that suits him: `cameos.members[].lipsFrom`, MaleYoungEager (1,922
    candidates) for Garrick and MaleCondescending (1,568) for Claudius
  - it reads each length from the xWMA header's last `dpds` entry, with no decoding
  - the .fuz keeps its own audio, and `lines.json` records the source and both lengths
  - **all 28 within 12 ms**, most exact
- Correction: the header word I read as "3 vs 43" varies widely among vanilla's own lips (3,
  11, 111, 9771...), so it wasn't the tell. The shipped lips are now vanilla's own, byte for
  byte, which plain SE animated in the LipTest.
- Plugin unchanged (`877104f8d56febf6`); 28 lip files deployed. **2.0.1.2** packaged for the
  vanilla test. If they lip-sync, SSE Engine Fixes can come off the page's recommendations.

### Later: 2.0.1.1: louder cameos, quieter music, and the lip-sync test

Barry: "crank garrick and claudius up volume wise, even with the music down it's still a bit
loud. Can we put the music down to 60% default?", and "call this one 2.0.1.1". Stopping
Garrick's idle (`sendanimevent IdleForceDefaultState`) didn't bring the lip sync back on vanilla.
- **A bug in `tools/cameos/build_voices.py`:** the gain after loudnorm (-12 LUFS) was
  `volume={-12 - loud}dB`, which is -3 dB for `voiceLoudness` -9, not +3. So the voices sat
  at about -15.7 LUFS. Now it's `volume={loud + 12}dB`, and `voiceLoudness` is -6: measured
  -9.6 LUFS, true peak -0.3 dBFS, **6 dB louder**.
- **The music at 60%:** new property `MusicMix2` 0.6, since saves that ran 2.0.1 keep
  `MusicMix` 0.8. `MusicMix` is retired.
- **The lip-sync test** (`cameos.looseLip`, on):
  - Each cameo line now ships as the `.fuz` unpacked: its lip track as `<name>.lip` and its
    audio as `<name>.xwm`. That's the classic layout plain Skyrim reads itself; the packed
    `.fuz` lip-synced only in Barry's modlist.
  - Ruled out before this: the lip data (present, vanilla's layout), the faces
    (byte-identical to vanilla `001065EE` and `000457F6`, races matching), and the idles.
  - The 28 stale cameo `.fuz` files were removed from Barry's MO2 mod folder, which
    `deploy.py` never cleans.
- Plugin `877104f8d56febf6`, deterministic, no FormID change. Deployed. **Release 2.0.1.1**
  packaged (`dist/release/SkyrimFair-2.0.1.1/`).
- **To test on vanilla:** do Garrick and Claudius lip-sync now? Are they loud enough over the
  60% music?
- **Barry, on vanilla:** the volume is "perfect on both accounts" (confirmed). The loose
  `.lip` files didn't bring the lip sync back.
- **Found:** with the LipTest add-on (a vanilla bard's lip file in place of Garrick's line
  01), Garrick lip-synced on vanilla. So plain SE rejects our lip files, not the character.
  - The Creation Kit's own generator (`-GenerateSingleLip`) makes the same header as
    `LipGenerator.exe`. Vanilla's 2011 lips differ: the sixth header int's high word is 3 in
    vanilla's, 43 in ours.
  - The Oldrim CK won't start without the Oldrim game (`0xC0000135`).
  - Barry's modlist lip-syncs them through **SSE Engine Fixes**' `bLipSync` fix, which
    turns four conditional jumps in the game's lip function into unconditional ones
    (`src/fixes/lip_sync.h`). Plain SE runs four checks that today's lip files fail.
- **Decision:** SSE Engine Fixes (Nexus SE 17230) is recommended on the page, in the credits
  and in `RELEASE.md`. Without it, the two are heard, with subtitles, but their mouths stay
  still. `FonixData.cdf` was copied into the SE install's `Data\Sound\Voice\Processing\`
  (Barry agreed); it's harmless and ready for any future lip builds.
- The earlier test, `dist/release/SkyrimFair-LipTest.zip`: one vanilla lip track (from
  `bardscolle_bardscollegepoe_000e774e_1.fuz`) in place of Garrick's line 01 `.lip`. If his
  mouth moves, our lip files are what vanilla can't read; if not, it's something about the
  characters.

### Later: 2.0.1: the music makes room for talking (the first Nexus feedback)

A Nexus comment: "I would recommend lowering the volume of the song a bit though, I found it
hard to hear the NPCs that were trying to talk to me, and some of their mouths did not move at
all". Barry: "reduce the fair volume to about 80%, and maybe drop it to like 5% when a character
is speaking". He also found "garrick and claudius doesn't lip sync on vanilla".
- **The mix** (script only; new properties, so it reaches saves):
  - `MusicMix` 0.8 on the stage music and the crowd's cheer
  - `SpeechDuckLevel` 0.05 while anyone speaks: `CameoTalking()` or `Utility.IsInMenuMode()`
    (any dialogue or barter menu, vanilla Papyrus)
  - `SpeechPoll` 1.0: while a song plays, the script looks at least every second, so the dip
    lands within about a second
  - `CameoDuckLevel` retired; config in the stage audio (`musicMix`, `speechDuckLevel`,
    `speechPoll`)
- **Lip sync, not fixed yet.** What's checked:
  - all 28 cameo and 147 singer `.fuz` files carry lip data, laid out like vanilla's
    (version 1, the same header shape)
  - the audio plays, so the paths are right
  - Garrick's FaceGen head is `MaleHeadWoodElf` and his race Wood Elf; Claudius's is
    `MaleHeadBreton` and his race Breton, so the heads match their races and head parts
  - next: whether they lip-sync in Barry's modlist (which has mfgfix and FaceGen Fixes), to
    tell our files from the plain game
- Plugin `9cf36fdb0024fc3d`, deterministic, no FormID change. Deployed. **Release 2.0.1**
  packaged (`dist/release/SkyrimFair-2.0.1/`): the grass-fix upload went to Nexus as version 2,
  so the numbering carries on from there.

### Later: released on Nexus

Barry uploaded The Wanderer's Fair to Nexus Mods (2026-09-25), then the update with the
grass fix. Vortex on his plain Skyrim saw the update on Nexus and downloaded it: the update
route works end to end. Next releases: `python tools/package.py --version <x.y.z>`, with the
version matching Nexus's.

### Later: a meadow in the fair (plain Skyrim's live grass)

Barry, testing the release zip on a completely vanilla Skyrim: "do you know why there's so
much grass???" (a screenshot: tall orange tundra grass everywhere off the cobbles).
- Barry's modlist loads grass only from a cache (`bAllowCreateGrass=0`), and the fair has
  none, so no grass had ever shown there. Plain Skyrim grows it live on every ground texture
  that carries grass. The fair's `SkyrimFairGroundGrass` (a copy of vanilla LTEX 013428)
  carried TundraGrass01 (71 tall, density 14), TundraGrass04 (19), TundraGrass03 (12) and
  FieldGrass02 (3): wild tundra, not trampled fairground. Every player without a cache would
  have seen it.
- **`fairWorld.ground.grassOverride`**: that texture's list is replaced by the fair's own
  copies:
  - TundraGrass04 at 6, TundraGrass03 at 4, FieldGrass02 at 3
  - TundraGrass01 dropped
  - built after the holidays-free pieces, in `0xA003D`-`0xA003F`; the LTEX keeps its FormID
  - the copies match vanilla's GRAS byte for byte, except the density byte
  - being the fair's own records, grass mods that edit vanilla's grasses can't change them
- Plugin `a1968d3df1f05d5a`, deterministic; the FormID diff shows only the three grasses.
  Deployed, and the release package rebuilt.
- **To test (on plain Skyrim):** scattered short tufts and a few flowers on the fair's ground,
  not a meadow.

### Later: the Compatibility download, and the description in BBCode

Barry: "can you also have a compatibility.md as an optional download as well as the py? It
doesn't seem to convert properly on nexus."
- The optional download is now **`SkyrimFair-<version>-Compatibility.zip`**, in place of the
  patcher-only zip. It holds `COMPATIBILITY.md` (the players' instructions, as on the page),
  `skyrimfair_spid_patcher.py` and its `README.txt`. The page and `COMPATIBILITY.md` call it
  the **Compatibility** download.
- The cause of the conversion trouble: Nexus's description box takes BBCode, not Markdown.
  `tools/package.py` now also writes **`NEXUS_DESCRIPTION.bbcode.txt`** with its own small
  converter (`bbcode()`):
  - headings become `[size][b]`
  - `**bold**`, links, inline code (Courier New) and code blocks (`[code]`) are converted
  - lists become `[list]` / `[list=1]`
  - each paragraph and list item goes on one line, since Nexus keeps line breaks
  - checked: no Markdown left over

### Later: the SPID Patcher for players

Barry: "could we make a py file for the compatibility stuff?" (a patcher for players).
- **`tools/compat/skyrimfair_spid_patcher.py`**: standalone, Python 3.8+, standard library
  only.
  - Players point it at MO2's `mods` or Skyrim's `Data`, or double-click it and paste the
    folder.
  - It adds `-SkyrimFairNPC` to exactly the five lines on the Compatibility page: second
    field, `NONE` replaced or appended after a comma.
  - It keeps the BOM and line endings, backs up each changed file
    (`.skyrimfair-backup`), and has `--dry-run` and `--undo`.
  - A line that has changed is left alone, with "NOT FOUND ... check the fair's mod page"
    and exit code 2.
- **Tested on copies** of Barry's originals, read-only from the modlist:
  - the output is byte-identical to `dist/spid/` (the generator's patches)
  - a second run changes nothing
  - `--undo` restores byte-identical originals and removes the backups
  - a changed line is refused
  - no crash without keyboard input
- `tools/package.py` writes `SkyrimFair-<version>-SPID-Patcher.zip` (the script and a
  README), for the mod page's optional files. It stops if the patcher targets a line the
  Compatibility page doesn't list.
- `docs/COMPATIBILITY.md` and the Nexus description mention it as "the easy way", before the
  by-hand edits.

### Later: the Nexus description, complete

Barry: "generate a new NEXUS_DESCRIPTION ... for the new dependencies" and "incorporate the
compatibility instructions at the bottom".
- `docs/NEXUS_DESCRIPTION.md`: the requirements were already Skyrim only. Its requirements
  note points to a new last section, **Compatibility**: `docs/COMPATIBILITY.md`'s player
  instructions, their headings one level down.
- `tools/package.py` stops a release if that section drifts from `COMPATIBILITY.md` (the one
  to keep current when a mod changes its line), and copies the description into the release
  folder.
- The trap: after Barry disabled `00078307` (20:25), its lines stop; the reload at 20:42 has
  none. The session's fair traces show no errors, the archers queued, and the singers homed
  once and didn't step again.

### Later: the singers loop their cheer; the licences settled; the archers' "strike"

Barry: "the archers have gone on strike, same with the bards". Then: "ignore the bard singers
comments. But can we just have the bard singers constantly doing that animation, like we have
with the people dancing". And: the welcome sign "is okay", Mixamo allows commercial and
personal use, the voices were made with ElevenLabs Pro, and "everything else is okay".
- **The strike is a vanilla trap, not the fair.** Papyrus.0.log:
  - `TrapDweThresher` on `00078307` (Skyrim.esm, in Raldbthar02) fired 28,701 times from the
    moment the save loaded ("VM is thawing"), each time lacking its 3D. That's the vanilla
    thresher bug: a trap caught mid-swing when its cell unloaded.
  - The flood starved every script. The stage script's 8-second singer steps went quiet for
    104 s (07:59:44-08:01:28), which stopped whatever it drives: the band, the singers, the
    archers' queue.
  - The fair doesn't touch the trap. The fix is in Barry's save (below).
- **The singers loop their cheer** (script only): the log showed them still stepping ("singers
  step to ±200"). That's the saved-property trap: Barry's save kept the old
  `SingerStepOffsets`, though `songs.config.json` has none. And the cheer waited for each
  step, so it only fitted in between.
  - In the steady show, `SingerSteps` now brings them home once (offset 0) and never steps.
  - `SingerGestures` no longer waits for a step, so the cheer replays back to back, as the
    dancers' loops do.
- **Licences:**
  - the welcome sign cleared to ship; the release no longer reserves it
  - Mixamo: personal and commercial use
  - the cameo voices: ElevenLabs Pro
  - `CREDITS.md`, `RELEASE.md` and the Nexus description are updated.
  - For 1.0 there's no grass-cache file: the page tells cache users to regenerate their cache
    with the fair installed.
- `tools/package.py`:
  - no release switches now, so the release plugin is the dev one (`709eeed250ad09a0`)
  - the sign is in the zip (521 files, 172 MB)
  - the only open item is checking the SPID lines before each upload
- **To test:** the singers stand on their marks and loop the cheer without stopping.

### Later: Holidays no longer needed

Barry: "frame rate is still the same :) And yes please!!" (drop Holidays).
- **The bunting** (`tools/make_bunting.py`, git-ignored output like the cobbles):
  - Vanilla's festival line maps its nine pennants to three triangles in the Solitude city
    banner's top-left corner (read from the NIF's UVs).
  - Each colourway repaints them in its three colours, keeping the weave and the alpha
    cut-out: Whiterun, Saturalia, Riften, Windhelm, Imperial, Stormcloak. Holidays'
    colourways were a colour reference only.
  - DXT5, full mips, as vanilla's.
- **`FairHolidaysFree.cs`**, appended to the lanterns' range (`0xA0018`-`0xA003C`):
  - per scheme, a texture set and a static: vanilla's line with the nine `FlagsNN:44` shapes
    swapped. That's the same `MODS` size as Holidays' (202).
  - the silver platter: a static of vanilla's own mesh
  - the props change base, and gain new references in their cell:
    - the apple basket: the fair's `Basket05` with five apples
    - the mead crate: vanilla's small crate at 0.6, with four bottles on top
    - the sign stand: vanilla's Whiterun post at 0.66, with its hanging board (placed as in
      the market's sign kit)
  - 84 bunting lines, 9 props, 24 pieces added; every Holidays reference keeps its FormID.
- **SkyrimFair.esp's only master is `Skyrim.esm`**, with no links into Holidays.
  - Its own records now carry master index 01 in the file. Saves track plugins by name, and
    the `.seq` is written after the plugin.
  - `tools/package.py` now stops a release if the masters are ever anything else.
- The docs are updated: the requirements in `RELEASE.md`, the Nexus description and the
  package's page text, plus `CREDITS.md`.
- Plugin `709eeed250ad09a0`, deterministic. The FormID diff shows only the new records
  (6 texture sets, 7 statics, 24 references). Deployed. The release package builds and
  passes both checks.
- **To test:**
  - The bunting's colours along the ropes.
  - The apple baskets (fruit stalls), the mead crates (drinks and mead stalls), the silver
    platters (sweetrolls, prize booth), and the sign stands (score board, prize booth).
  - Optionally, disable Holidays in MO2: the fair should load and look the same.
- **Confirmed in game by Barry:** "a proper seamless transition" (the bunting and props without Holidays).

### Later: the paper lanterns replace Holidays'

Barry: "let's do that :D". Twelve kinds instead of Holidays' six: two shapes in six colours.
- **`FairPaperLanterns.cs`**, in a new range `0xA0000`-`0xA0017`: per shape and colour, a
  texture set and a static.
  - The texture set: the coloured paper as diffuse and glow map, plus the shared normal.
    It's the same shape as Holidays' `_WetHolidayLanternTxt_Red` (TX00, TX01, TX03, DNAM 0).
  - The static: Astra's NIF, with the texture set swapped onto `Ribbed_paper` (3D index 1,
    read from the NIFs).
- **All 222 placed Holidays lanterns** (the fair's, life's and the exterior's in Tamriel) keep
  their FormIDs and only change base. That happens last of all, in `FairPluginGenerator`,
  after the exterior: run at the end of `FairWorld`, it missed the exterior's last 8.
  - Each keeps its colour, and is tall or round by a hash of its FormID: 111 tall and 111
    round.
  - Scaled to Holidays' 24 wide: tall ×1.5, round ×1.45, times their own scale.
  - Lifted 6 (times their scale) to hang from Holidays' hook point.
- The FormID diff against the deployed plugin: only the 24 new records. The statics' 12-byte
  `DNAM` matches every other static in the plugin.
- Plugin `f7ca9173822b4a47`, deterministic. Deployed, with the two NIFs and 14 textures.
- Holidays is still a master, for the rope lines and a few props (`docs/RELEASE.md`).
- **Confirmed in game by Barry:** "these lanterns are incredible".
- **To test:**
  - The lanterns hang from the ropes and posts where the old ones did, at a good size.
  - Their colours, and the glow at night.
  - The frame rate: 111 × 9,472 and 111 × 6,592 triangles. If it drops, ask Astra for
    lighter versions.

### Later: the paper lanterns in six colours

Barry: Astra's lanterns zip, "a round and a tall variant". "Can we add some colour glows?"
- Astra's two lanterns (original meshes, AI paper art) are in the repo:
  - `assets/meshes|textures/barry_paper_lanterns/`: the game files, tracked
  - `assets/Skyrim_Paper_Lanterns_Authoring/`: the authoring package, untracked like the others
- Each one's paper texture is also its Glow shader's glow map (shader type 2, emission white
  1.0). So a recoloured paper glows in its own colour.
- **`tools/make_lantern_colours.py`** (texture venv) makes the other ten papers, red, orange,
  yellow, green, blue and purple for both shapes, as BC1 DDS with full mips (the same as
  Astra's: 699,192 bytes, 11 levels):
  - it moves each pixel toward the new colour by how much of the paper's own colour it
    carries, so the warm core stays warm and the motifs follow the colour
  - it keeps the fade bright, so no grey-brown where blue meets the warm core
  - the round shape gets a colour boost, because Astra's blue is soft
  - each shape's own colour is Astra's texture, never overwritten (a first run did overwrite
    both; they were restored from her zip, byte for byte)
- **`tools/lanterns_preview_blender.py`** renders all six colours headless in Blender 5.2,
  with Astra's studio lighting (`build/lanterns/lanterns_colours.png`).
- Not in the plugin yet: nothing places them, and Holidays' lanterns stay. 9,472 and 6,592
  triangles are heavy for about 222 lanterns; a lighter version would be wise before placing.

### Later: the release package (`tools/package.py`)

Barry: "could you generate me a production folder? with everything in there?"
- **`tools/package.py --version 1.0.0`** writes `dist/release/SkyrimFair-1.0.0/`:
  - `SkyrimFair-1.0.0/`, the Data folder: 496 game files, 207 MB
  - `SkyrimFair-1.0.0.zip`, the upload (163 MB), with the Data layout at its top level
  - `NEXUS_PAGE.md`: the page text, the compatibility instructions and the credits
  - `RELEASE_TODO.md`: what's still open
- **The release plugin** is built from a copy of the config with the release switches, into
  `build/release/plugin`, twice (`fe82576616efb182` both times). `dist/` is untouched.
  - The one switch, `exterior.fairSign.reserve`: the welcome sign's licence is unconfirmed.
  - New in `FairExterior`: with `reserve`, the sign's static takes vanilla's `MarkerX.nif`
    and its reference starts disabled. The FormIDs stay (`030145`/`030146`), and the same
    4,529 as the deployed plugin.
- **Left out:**
  - the welcome sign's mesh and textures
  - the static crowd figures (the plugin places none)
  - Stroti's outhouse
  - the old procedural cobble
  - `SkyrimFairNpcGuard.pex`
  - the SPID `_DISTR.ini` copies
- **Found by its mesh check:** the terrace and stair pieces in `assets/nif/SkyrimFair/`
  (FloorFill, PaveCap and the rest, 50 placed references). `deploy.py` never copied them:
  Barry's mod folder had them only from a hand copy, byte-identical. `deploy.py` now copies
  `assets/nif` into `meshes\` too.
- The package against the mod folder: everything in the folder but not the package is one of
  the intended exclusions.
- **Still open** (`RELEASE_TODO.md`): the welcome sign's licence, the Mixamo terms, the cameo
  voices' source, the grass cache, the exterior's limits, and checking the SPID lines.

### Later: Barry's checks pass

Barry: "Yup all good!!!". **Confirmed in game:**
- the steady show (every instrument and the singers' cheer loop through each song)
- the roof horse staying on its planks with its AI off
- the keepers kept at their spots (the clothing stall's back behind the counter)
- Browse on the counters
- the shops showing all their stock

### Later: the gear stalls at 20x

Barry: "can we change it to 20x". `shops.priceMultiplier` 20 (the perk's EPFD `0000a041`).
Plugin `18e3b92ce18797d9`, deterministic; no FormID changed. Deployed.
**Confirmed in game by Barry:** "i think it's good now" (the shops, with the gear stalls at 20x).

### Later: fair prices only at the gear stalls, 40x

Barry: 50x is "too expensive". Instead, "apply 80% of that to just the armor and gear stalls".
- `shops.priceMultiplier` 40 (80% of 50), and `shops.pricedTrades`: imperial, stormcloak,
  smith, elven, dwemer, fletcher, hunter, rare. Every other stall is back to normal prices.
- The perk keeps its tab-0 condition (the player in the fair's worldspace) and adds a tab-1
  condition, on the merchant (the speaker): in one of those trades' factions, OR'd.
  - ModBuyPrices has only these two tabs (perk owner and speaker), so it can't condition on
    the item.
  - Checked against vanilla MGArchMageVendorPerk (`10F9DB`), which conditions tab 1 on a
    faction the same way.
- Plugin `b5908a82a9ab83f0`, deterministic; no FormID changed. Deployed.
- **To test:** gear stalls cost 40x and the food and goods stalls normal prices.

### Later: the roof horse kept moving

Barry: "something keeps making the roof horse move around". Once it was on the stage,
another time "5ft in the air".
- Likely cause: its AI package. There's no navmesh on the roof, and an actor whose package
  paths with no navmesh can be warped to the nearest navmesh (the stage). The put-back script
  then moved it home and caught it mid-drop.
- `SkyrimFairRoofHorse.psc`: once it's home, `EnableAI(False)`, so no package can move it and
  it stays where it's put in its standing idle. The put-back is tighter (20 up or down, 40
  across). Script only; the plugin is unchanged (`e23aecf5d412566b`). Deployed.
- **To test:** the horse stays on its planks, and still looks alive, not frozen mid-step.

### Later: fair prices, every shop showing its stock, rarer gear

Barry: all the shops work now, but the candle and tallow maker's is empty. He asked for prices
"like 50x" ("i don't want people to use the fair as a cheat"), and more and rarer gear:
"more imperial gear, more stormcloak gear".
- **Why the candle shop was empty:** a merchant shows only the chest items its buy list
  allows. The candle maker's list was `VendorItemClutter`, but torches are `VendorItemTorch`
  and the beeswax is ingredients. Checked across all trades: 84 stock lines were hidden the
  same way. Many goods (pottery, instruments, tools, beeswax, soups, stews) have no vendor
  keyword at all, so no themed list can show them.
- **The fix (`shops.tradeAnything`):** every trade's list is inverted (`VENV` "not sell/buy")
  and excludes only `VendorNoSale`, as a general store's does. Each keeper sells all their
  stock. They also buy anything, but only up to their ~100 gold, and at normal price. The
  `buys` lists stay in the config, unused.
- **Fair prices (`shops.priceMultiplier` 50):** a hidden perk `SkyrimFairShopPrices`
  (`0x900BB`, appended to the shops' range) in vanilla Haggling's shape:
  - entry point ModBuyPrices, multiply by 50
  - a condition on the perk owner: GetInWorldspace SkyrimFairWorld
  - the stage script adds it to the player while at the fair (new property `FairPrices`)
  - it does nothing anywhere else, and selling prices are unchanged
  - checked subrecord by subrecord against Haggling00 (`0BE128`)
- **Rarer gear** (49 lines appended to seven trades):
  - imperial: studded and light sets, the officer's helmet, the Imperial sword
  - stormcloak: the officer set, the sleeved cuirass, steel axes
  - smith: steel plate, scaled, some orcish
  - elven: gilded and light, glass dagger, sword and helmet
  - dwemer: the dwarven armour set and weapons
  - fletcher: elven, glass and ebony arrows, elven, orcish and dwarven bows
  - rare: an ebony dagger, sword and helmet, a glass bow and cuirass
- Plugin `e23aecf5d412566b`, deterministic. The FormID diff against the deployed plugin
  shows only the new perk. Deployed (after the game was closed).
- **To test:**
  - Prices at the fair are 50 times the usual. Outside they're normal.
  - The candle maker, pottery, toys, bard and woodworker shops show their goods.
  - The new gear appears. A chest already opened in a save keeps its old stock until the
    merchant restocks (vanilla: 48 game hours), so wait two days or test on a new save.

### Later: Browse opened nothing

Barry: at the flagged stalls he gets both "Browse" and "Talk". Browse does nothing, Talk
works, and he wants to keep both.
- Cause: the counter searched for its keeper by the keeper's NPC record
  (`FindClosestReferenceOfTypeFromRef`). A keeper's reference runs on a copy of the record,
  made at load because their faces come from a leveled list, so the search found nobody.
- Now each counter has its keeper's own reference: new properties `KeeperRefId` (the
  reference's FormID in the plugin) and `KeeperPlugin`, looked up with `Game.GetFormFromFile`
  when the counter is used. `Keeper` is kept but retired. A failed lookup is traced
  ("SkyrimFairStallCounter: no keeper for ...").
- All 59 counters' IDs land on their own stall's keeper (checked by name).
- Plugin `941d460f4cbbe8f6`, deterministic; no FormID changed. Deployed.
- **To test:** Browse at the flagged stalls opens the shop.

### Later: the startup crash on the counters' activators

Barry: a crash on startup (CrashLogger: `EXCEPTION_ACCESS_VIOLATION` at `SkyrimSE.exe+0D066F4`
during `InitTESThread`, with `TESObjectACTI "Imperial Armourer" 0x90084` on the stack twice).
- Our activators carried the static's `MODT` (texture hashes, 96 bytes). The stack held
  `0x736464` ("dds", the extension hash inside `MODT` entries) next to the activator, so the
  game died reading them.
- Now the activators have no `MODT` (about 100 vanilla activators with a model have none),
  plus `PNAM` (cc4c3300, vanilla's usual) and `FNAM` 0, which Mutagen leaves out unless set.
  They are `EDID OBND FULL MODL PNAM RNAM FNAM`, vanilla's order.
- Plugin `a2d47692d712a82d`, deterministic over two builds; no FormID changed. Deployed.
- **If it still crashes:** set `shops.counters.enabled` to false in `fair.config.json` and
  rebuild. That puts the counters back as statics and leaves everything else.

### Later: a steady show, counters you can browse, keepers kept at their spots

Barry: "have all instruments and singing and animations for singing playing constantly. I
feel like the timing doesn't match up much." And: eight keepers (spices, cheese, elven,
fishmonger, herbalist, furs, pottery, bard) couldn't be traded with. He could reach them
and got the E prompt, but at some stalls "they're stood too far back to interact with them
from our side". Also: the clothing keeper "can't get to the stall because there's props in
the way".
- **The steady show** (`songs.config.json`: `"steady": true`, `"singerLoop": 2.9`). New
  script properties `SteadyShow` and `SteadySingerLoop`, so they reach running saves.
  - Every instrument plays at its normal loop for the whole song: no rests, no fast stretches.
  - The singers sing throughout, with their sing move (IdleCivilWarCheer, the Mixamo cheer
    through OAR) replayed back to back at its real 2.9 s, with no gap.
  - The drums and singers timelines stay in the file but are ignored. The crowd's timeline
    still runs, and between songs is as before (the cheer, fireworks, the band puts
    instruments away).
- **Counters to browse** (`shops.counters`). The keepers' records were all alike: factions,
  flags, package. The difference is reach: they stand behind deep counters (WRMarketStand02's
  counter is about 95 deep), and tall goods hide their bodies. So each stall's counter
  (WRMarketStand02, WHMarketStall01/02) is now an activator with the same model, named for
  the stall. "Browse Cheese & Dairy Stall", and E opens the nearest keeper's barter menu
  (`SkyrimFairStallCounter.psc`, `Game.FindClosestReferenceOfTypeFromRef` on the keeper's
  record, then `ShowBarterMenu`).
  - 55 activators, appended to the shops' range (`0x90084`-`0x900BA`). The 59 counter
    references keep their FormIDs and only change base, last of all (after the navmesh and
    the sight table).
  - All 59 counters point to their own stall's keeper (checked by name).
  - Talking to a keeper still works as before.
- **Keepers kept at their spots** (`shops.keeperHome`, `SkyrimFairKeeper.psc` on each keeper
  record). A moment after loading, and every 20 s while loaded, a keeper more than 60 from
  their spot is moved back to it. Not while the player talks to them or in combat.
- Plugin `2ee8341ca27afa2f`, deterministic over two builds. The FormID diff against the
  deployed plugin shows only the 55 new activators. Deployed.
- **To test:**
  - Look at a counter (the eight stalls first): do you see "Browse ...", and does E open the
    stall's shop?
  - Is the clothing keeper back behind the counter?
  - During a song: do all instruments play throughout, and do the singers loop the cheer
    without stopping?

### Later: the singers' own cheer (Barry's Mixamo retarget)

Barry: "help me install that zip packaged with the mod" (`character-actors/cheering-retarget/`,
Mixamo's Cheering retargeted with ChatGPT, a 2.9 s loop on the vanilla skeleton).
- It ships in the fair's own OAR tree, as the folk dance's and the tempo clips do:
  `OpenAnimationReplacer\SkyrimFairBardSinger\Singers\special_civilwarcheer.hkx`. So it
  replaces IdleCivilWarCheer, the singers' "sing" move, only for them.
- The generator (`singers.cheerOar`) copies the clip from the package, and writes both
  `config.json` files with the singers' own records (`IsActorBase` 16B2, 16B5, 16B8). The
  singers aren't templated, so their base records are theirs in game, unlike the folk pair.
  It's byte-identical to the zip's clip, and the configs match the zip's.
- The plugin is unchanged (`3d1b5b1f...`). Deployed.
- **To test:** during a song, do the three singers do the new cheering loop, and the crowd
  still the vanilla cheer?

### Later: the stalls are shops

Barry: "we've not actually hooked up the vendors to a shop yet". He chose stock themed to
each stall, and "sell only" (they barely buy).
- **`FairShops.cs`**, in a new range `0x90000` (132 records: 33 trades x a faction, a
  merchant chest, its reference and a buy list). The pattern is vanilla's: Belethor's
  `ServicesWhiterunBelethorsGoods` and the Khajiit caravans' factions.
  - **The faction:** `Vendor` and `CanBeOwner`; open 0-24; the location `NearSelf` (as the
    caravans); its chest; a buy list of the trade's own keywords (not inverted: cheese sells
    to the dairy, arrows to the fletcher). It has the caravans' crime values (`CRVA`, which
    Mutagen leaves out unless set) and no ranks, and was checked subrecord by subrecord
    against vanilla.
  - **The chest** (`Respawns`) stands in the holding cell `SkyrimFairSandbox`. It has
    Belethor's chest's model and bounds, the trade's stock, and 100 gold (150-250 for the gear
    trades).
  - **The keepers** (59, by EditorID `SkyrimFairVendor<Theme><NN>`) join their trade's
    faction and `JobMerchantFaction`. That faction's line in DialogueGeneric's
    `OfferServicesTopic`, "What have you got for sale?", opens the barter menu, in their own
    vanilla voices.
- **The stock** (`fair.config.json` `shops.trades`): 248 lines across 33 trades, items and
  vanilla vendor leveled lists by EditorID, resolved against Skyrim.esm (a wrong name stops
  the build). For example: cheese, all six cheeses and the mammoth bowl; the herbalist, 16
  ingredients; the fletcher, arrows and three bows; Imperial and Stormcloak armour; the
  mage, tomes, soul gems, scrolls and a staff.
- Plugin `3d1b5b1f98d0956b...`, deterministic, deployed. Against `dce9957d...`, 132
  records are added; otherwise the keepers only gain factions.
- **To test:**
  - Ask a keeper "What have you got for sale?": does the barter menu open with their stall's
    goods?
  - Do they buy only their own kind of goods?
  - Does stock come back after a couple of days?

### Later: Claudius unstuck, louder voices and a deeper dip, the exterior wall fuller

Barry: the vendors aren't hooked up to shops yet (planned next); Claudius seems idle; the
exterior "seems a bit empty still"; the voices louder, the music lower, or both.
- **Claudius:** Hadvar's ledger ends with a furniture exit (`IdleChairExitStart`), which
  may never fire standing. That leaves him in the pose, and his package can't walk him.
  - Every idle now ends in two steps: its own stop, then 3 s on
    `IdleForceDefaultState` and a package re-evaluation. He doesn't move on mid-exit.
  - Fewer idles, and shorter: every 50-90 s, the ledger 12 s. He moves on every 35-60 s.
  - Under new names, since the old ones are saved: `CameoHolds3`, `CameoEveryMin3/Max3`,
    `CameoMoveMin3/Max3`.
- **Louder voices:** after the compressor and loudnorm (-12), +3 dB into a limiter at
  -1 dB: about -10 LUFS, level with the songs (were about -12.7).
- **A deeper dip:** the music plays at 0.2 (about -14 dB) while a cameo speaks. That's a new
  property, `CameoDuckLevel`, because `CameoDuck`'s 0.4 is saved.
- **The exterior wall fuller** (`exterior.wallDressing`, appended):
  - a Whiterun banner on every outer panel without one within 80
  - a Riften lamp post every third panel, its arm toward the wall, with
    `WRLightFireStreet01` at its lamp
  - nothing within 450 of the gate
  - 38 pieces, `0x3015E`-`0x30183`
- Plugin `dce9957d854439d9...`, deterministic, deployed with the scripts and the voices.
  Against `a0ad4187...`, 38 records are added, and nothing else changed.

### Later (wrap-up): the playlist reaches the save, the roof horse held, the exterior dressed

Barry: the moved gate "looks much much better"; three last things: the roof horse has
vanished; Round the Green still opens; the exterior "looks a bit like a cultist camp".
- **The show's data under new names:** Barry's save kept the old property values (see
  the saved-property trap below). So every playlist, section and line array, and the
  cameos' idle arrays, now carry a 2:
  - `Songs2`, `SongLengths2`, `SongCheers2`
  - `SectionStarts2`, `SectionPlay2`, `SectionSing2`, `SectionCrowd2`, `SongFirstSection2`,
    `SongSectionCount2`
  - `SingerTopics2`, `SingerStarts2`, `SongFirstLine2`, `SongLineCount2`
  - `CameoIdles2`, `CameoHolds2`, `CameoStops2`, `CameoFirstIdle2`, `CameoIdleCount2`,
    `CameoEveryMin2`, `CameoEveryMax2`
  - The outside show has `Songs2`, `SongLengths2`.
  - New names take the plugin's values in an existing save. So The Wanderer's Fair, the
    six-song list, Claudius's ledger and Garrick's continuous lute reach it.
  - A later update that must reach running saves renames again (3, ...).
- **The roof horse held on its planks:** an actor can load before the planks' collision and
  drop through. Two seconds after each load, and every 15 s while it's loaded,
  `SkyrimFairRoofHorse` checks it's within 60 across and 40 up or down of its spot (`HomeX/Y/Z`,
  from the reference). If not, it's put back (`MoveToMyEditorLocation`) and held again.
- **The exterior dressed as a festival entrance** (`exterior.decor`, in the gate's frame,
  23 pieces appended at `0x30147`-`0x3015D`):
  - a lit brazier each side of the gate: `WHfirebrazier01`, an ember fire and
    `WRFireLightNS`
  - a Whiterun banner post each side of the path (`FarmBannerPost01`,
    `CityBannerWhiterun01`)
  - three Riften lamp posts (`RTLampPost01`), their arms over the path, each with
    `WRLightFireStreet01`
  - by the welcome sign: a cart, a hay bale and scatter, a mead barrel, a barrel, two crates,
    laid on the slope
- Plugin `a0ad418712664b98...`, deterministic, deployed with the scripts. Against
  `d2637a92...`, 23 records are added, and nothing else changed.
- **To test:**
  - Does The Wanderer's Fair open the show?
  - Is the horse back on the roof?
  - Does the outside read as a fair's entrance: fire at the gate, banners and lamps up the
    path?
  - Is Claudius using the ledger, and does Garrick keep playing?

### Later: the gate moved west, the original slab back over the dip

Barry: "we move the door to the right? and return the original rock. It doesn't need to be
perfect on the outside, just a way to get in".
- **The slab** (`023362`, `DirtCliffs01FieldGrass01`) is about 1,750 wide (east-west) and
  about 420 deep. It lies along the front of the wall, centred on the old gate at
  (-5887, -10440), and it's what hid the dip where DynDOLOD's underside shows.
  - It's back as vanilla has it: `exterior.keep` exempts it from clearing, and it's no
    longer overridden.
- **The compound moved 1,150 west** (to the right, facing the gate from outside):
  `exterior.gate` (-7050, -10800), and the approach's road end with it, (-7050, -10120). So
  the path corridor keeps its length, and its reserved FormIDs their count.
  - The gateway now opens west of the slab's end (about -6770). Everything outside is placed
    from the gate, so it moved with it: the wall, the towers, the silhouette, the doors and
    arrival points, the map marker, the show, the clearing.
- **Clear of the slab:** the gate flags are 260 either side (were 450; the east one would
  have stood in the slab). The road sign is on the path's west side (side -1, 220 out, 240
  forward), near the welcome sign. The small cover rock is gone (it was `0x30147`, the
  range's last).
- The clearing now touches cells (-3..-2, -4..-3): 37 vanilla references, 2 large ones sunk.
  The 28 vanilla overrides that changed are that clearing moving. None of the fair's own
  records moved, apart from the cover gone.
- Plugin `d2637a92ca6d2036...`, deterministic, deployed.
- **To test:**
  - Walk up from the road: is the gateway clear, with the slab to its left?
  - Is the floating grass gone?
  - Do the flags, the road sign and the welcome sign stand clear?
  - Does the wall sit acceptably at its new spot?

### Later: the saved-property trap, crowd layers at 3, the tower banners, Garrick's swap undone

Barry: the music dip "works great"; the walls are brighter; Round the Green still opens;
Garrick's current spot is "great, so we can axe that change"; `SkyrimFairCrowdLayers` at 3
helped the frame rate, with little visible difference, so keep it; the rock still doesn't cover
the floating grass; banners outside "seem to be getting stuck".
- **Why Round the Green still opens: saved script properties.** The log said
  `song 0 playing`, so the start-at-0 fix works. But its cheer came at 148.8 s of a 149.6 s
  song, Round the Green's length. In Barry's save the song list is still the old one.
  - **Skyrim stores a script's property values in the save** the first time the script
    runs. Later plugin changes to an *existing* property never reach that save; only *new*
    properties take the plugin's values.
  - So in Barry's save these didn't take effect: the new songs, Claudius's ledger, and
    Garrick's continuous lute (`Songs`, `CameoIdles`, `CameoHolds`...).
  - These did, being new properties or records: the duck, the companions, the rounds, the
    posters, the lights.
  - **To see property changes:** a fresh start (`coc SkyrimFairWorld` from the main menu),
    or ReSaver (Fallrim Tools) to delete `SkyrimFairAudioScript`'s instance from the save.
  - For release: updates that change the show's data need a new game or a cleaned save,
    unless the data moves to properties with new names.
- **Crowd layers default to 3 of 6** (`crowds.tierDefault`): the dance floor, front and
  back, and the seated market visitors. The archery spectators, the wanderers and the
  children are off.
- **The exterior's tower banners:** the towers are copied whole inside the smaller wall, so
  their outward banners hung in it. Copies of `DEE54` at the towers' 1.25 within 120 of the
  wall line are now copied disabled (FormIDs kept): the two by the gate (`0x300F4`,
  `0x300FC`). The wall's own banners stay.
- **Garrick's visitor swap undone:** the visitor (`0x149D`) is back. Garrick stands (the
  stay package, the lute) at his corner, (1044, 4630).
- **No empty array properties** are written any more: the engine can't initialise them,
  and logged "incorrect type" for `SingerCheerMoves`, `SingerCheerLengths`, `StripIds` and
  `StripPlugins`.
- Plugin `4c8884908ffb115e...`, deterministic, deployed; the same 4,290 records.

### Later: the music ducks for real, Garrick stands in, the wanderers stand, lights, the opening song

Barry, in game:
- the voices are louder, but the music doesn't dip
- Garrick "wasn't by the watchtower": replace this visitor with him, "static standing
  still, with his lute"
- frame dips, 30-50 against 120 (with frame generation), at the archery and the centre:
  "sacrifice some vanilla npc's walking for our cameo ones?"
- The Wanderer's Fair doesn't play
- brighter lights round the walls and the centre aisle
- the rock over the floating grass is too small, and doesn't sit on the hill

What changed:
- **The opening song:** the log said `song 1 playing` on arrival. The script carried the
  playlist on from the save (at 1, from before the new songs), so The Wanderer's Fair
  (now 0) came only after Raise Your Cups. Now every arrival or load starts at 0, unless
  `SkyrimFairAudioFirstTrack` names another. The same log shows **"brought 1 companions to
  the player"**: the companions work.
- **The music duck:** a greeting from an NPC with no topics opens no dialogue menu, so
  `IsInDialogueWithPlayer` never went true.
  - Each of the cameos' 28 lines now has a begin fragment (`SkyrimFairCameoLine.psc`,
    `extends TopicInfo`). It sets `SkyrimFairCameoDuckUntil` to the real time the line ends:
    its length, from `build_voices.py`, plus 0.5 s.
  - While that holds, the stage script plays the song at 0.4. It's reset on load.
  - The VMAD is checked byte for byte against vanilla's INFO `0684FF`.
- **Garrick stands in for the visitor** at the west watchtower (`0x149D`,
  `SkyrimFairVisitorMale04`, at (604, 5279)):
  - That visitor is disabled (it keeps its FormID), and the culling skips disabled actors.
  - Garrick takes its place and facing, with `DefaultStayAtEditorLocation` first, and plays
    the lute almost without a break (held 600 s, restarted 2-4 s later).
  - His six spots are kept as records, but he doesn't walk them.
- **The wanderers stand** (Barry's trade): the four adult wanderers' records
  (`...Wanderer`) keep their FormIDs but run the visitors' stay-put package
  (`crowds...stand`). The children still wander.
  - The real lever for the frame dips is probably the number of actors in view (mean 127
    at a spot). The test: `set SkyrimFairCrowdLayers to 3` (fewer crowd layers) in the
    console, and compare.
- **Lights** (`FairLights.cs`, a new range `0x80000`), at the Holidays lanterns (they have
  no light of their own), spaced out, 40 under the lantern:
  - 16 along the walls: `WRFireLightNS`, radius 768, 800 apart
  - 14 over the lanes: `WRLightFireStreet01`, radius 512, 700 apart
  - All no-shadow, and the exterior's silhouette skips them.
- **The rock over the floating grass:** `RockPileM02FieldGrass01WR` is now at 0.8 (about
  300 x 380), its long side down the slope, at (-5655, -10630).
  - It's tilted to the ground's mean slope over 150 either way (the cover's `OnGround`),
    not the 16 at its centre, so it lies on the hill.
- Plugin `6b4fa8af189ffe07...`, deterministic, deployed with the scripts and the voices.
  Against `1c3a7a4b...`, 31 records are added (the duck global `0x60057`, and 30 lights at
  `0x80000`-`0x8001D`). Otherwise only data changed: the visitor disabled, Garrick moved,
  the packages, the INFO fragments.
- **To test:**
  - Does the music dip while Garrick or Claudius speaks?
  - Does The Wanderer's Fair open the show?
  - Is Garrick standing at the watchtower, playing?
  - Is the fair brighter at the walls and over the lanes?
  - Do the frame dips ease?
  - Does the rock now hide the floating grass, lying on the slope?

### Later: louder cameos, Garrick's shady corner, the grass covered, companions through the gate

Barry: "the posters are perfect".
- **The cameos' voices, louder, and the music ducked for them:**
  - Barry's recordings sat at -17 (Garrick) and -19 (Claudius) LUFS, under the stage songs
    (-10). `build_voices.py` now runs each line through a 3:1 speech compressor and
    loudnorm to -12 (about -12.7 in practice, true peak -1): about 6 dB up.
  - While the player talks to a cameo (`IsInDialogueWithPlayer`), the song plays at 0.4
    (`CameoDuck`, about -8 dB). With a cameo within 600, the update comes every 0.5 s, so
    the duck is quick.
- **"I cannot find Garrick... near the watchtower left of the stage, so he looks shady":**
  - He starts at Barry's spot (1044, 4630), by the tower at (798, 5250).
  - Three of his six spots are that corner, and the others are close (the dance floor's
    west edge, (1600, 4000), (1250, 3800)), all at radius 200. So he lurks there, and
    wanders out now and then.
  - Same records, so no FormIDs move.
- **The floating grass, covered:** Barry's spot is (-5691, -10621, -5806), 210 east of the
  gate's centre and 180 out.
  - It gets `RockPileM02FieldGrass01WR` (a low grassy Whiterun-tundra rock pile) at 0.55,
    at (-5665, -10635), set 6 into the ground and tilted to the slope (`exterior.cover`).
  - It's appended to the exterior's range (`0x30147`).
- **"Game companions can't seem to enter the fair":**
  - Followers go through a load door only by navmesh door links, and neither side of the
    gate has any: the fair's generated navmesh, or Tamriel's (not edited, for
    compatibility).
  - So there's a finder quest, `SkyrimFairCompanions` (`0x70000`, a new range), of 6
    optional, allow-reserved aliases matching the player's companions anywhere, not only in
    the loaded area. The match is `GetPlayerTeammate` OR in `CurrentFollowerFaction`, AND
    `WaitingForPlayer` 0 AND alive.
  - The stage script (`BringCompanions`) restarts it on arriving at the fair (and on a load
    there) and on leaving. Each one it finds that isn't within 1,500 of the player is moved
    to them, a step behind.
  - Its aliases were compared with vanilla's `WIDragonKilled` spectators; they drop only
    those aliases' faction and package extras.
- Plugin `1c3a7a4bc6b47e4f...`, deterministic, deployed with the scripts and the voices.
  Against `b8a4381f...`, 2 records are added (`0x30147`, `0x70000`).
- **To test:**
  - Are the voices clear over the music, and does the music dip while one talks?
  - Is Garrick by the watchtower?
  - Has the floating grass gone under the rocks, without rocks in the gateway?
  - Does a follower appear beside you a few seconds after you arrive, and again after you
    leave?
  - Does a follower told to wait stay put?

### Later: the cameos walk rounds; the Fair Inspector's posters; the floating grass found

- **The floating grass is DynDOLOD's `Tamriel_UNDERSIDE`** (Barry's click, again), poking up
  through a dip in the terrain.
  - The dip was covered by the big slab (`023362`, 360 out from the gate) that Barry
    first asked to be removed.
  - DynDOLOD's underside follows the smoothed distant terrain, not the real ground, so a
    re-run won't change it. Covering the spot will.
  - Waiting on Barry's `player.getpos x/y/z` at the spot.
- **"Garrick and Claudius be the rare ones that walk around"** (they stood still in one wide
  sandbox):
  - Each has a round of 6 spots, with a spot global (`SkyrimFairCameo<Id>Spot`), an XMarker
    at each spot, and a sandbox package per spot. That's a copy of the vanilla no-conversation
    sandbox, round the marker, radius 350, energy 100, on `GetGlobalValue == k`.
  - The spot packages come first in his list, and the wide sandbox is the fallback.
  - The stage script (`CameoIdles`) moves the global to another spot every 60-120 s, when
    he isn't mid-idle, and re-evaluates his package. So he walks across the fair to it.
  - **Garrick's spots:** the dance floor's west and east edges, the avenue at 3000 and
    1900, the activity field, the gate forecourt.
  - **Claudius's spots:** East Lane at 400 and 2800, East Wall Walk, East Cross, the avenue
    at 1000, the East Entry.
- **The posters** (Barry's 3 packs, 5 designs, on "Poster (Basic & Low Poly)" by
  filththemutt, CC BY 4.0), `cameos.posters`:
  - The game files are copied to `assets/meshes|textures/barry_<design>/`, and the
    authoring packages to `assets/Skyrim_Poster_Assets/`.
  - The meshes are 19 x 26 (converted at 40 units a metre), so they're hung at 2.5, and
    Garrick's at 3.
  - **Where:** on the palisade's inner face, 24 from the panel's centre line (just proud of
    the logs), 150 up, facing the fair. Only on panels 600 clear of the gate and 100 clear of
    a banner, every second free one.
  - **Garrick's statement once**, on the free panel nearest (2048, 6400): the north wall
    behind the stage, at (1891, 6273).
  - The other 12 go round the wall, the four designs in turn.
  - The exterior's silhouette copy skips them (the cameos' prefix).
- 44 records appended in the cameos' range (`0x6002B`-`0x60056`); nothing else changed.
  Plugin `b8a4381fd5313f79...`, deterministic, deployed with the script and the posters.
- **To test:**
  - Do Garrick and Claudius walk from place to place every minute or two?
  - Do the posters sit flat on the logs (not sunk into them, not floating), readable, at
    eye height?
  - Is Garrick's statement behind the stage?

### Later: the cameos couldn't be talked to; Claudius's ledger; the floating grass again

Barry, in game: the horse is "excellent"; "Garrick and Claudius isn't interactable"; the
floating grass is still there; and could Claudius do "the paperclip animation from the vanilla
intro... one of the imperials has got a list" (Hadvar's ledger at Helgen).
- **Two fixes for talking to them:**
  - **Their voice types allow default dialogue now** (`DNAM 01`, as every vanilla NPC voice
    type: MaleYoungEager `013AD1` has it). The singers' voice types, copied for the cameos,
    had 0.
  - **A SEQ file:** `dist/Seq/SkyrimFair.seq` (deployed to `Seq\`), as xEdit writes it: the
    start-game-enabled quests that carry dialogue, each FormID as the file stores it, with
    the plugin's own index (02, after Skyrim.esm and Holidays.esp). They're
    `SkyrimFairSingers` (`0x16BA`) and `SkyrimFairCameos` (`0x6000A`). The generator writes it
    after the plugin, once the masters are known, and it's deterministic.
- **Claudius's ledger:** Hadvar's, `IdleHadvarWriteLedgerEnter` (`10ACAD`, 20 s), ended with
  its own `IdleHadvarWriteLedgerExit` (`10ACAE`): the cameos' idles take a `stop` now
  (`CameoStops`; None falls back to `BandStop`). In turn: ledger, note, ledger, a look round.
- **The floating grass:** the plugin places nothing there, and the console click went
  through it. The next test is Barry's: `tg` (grass off) and `tll` (distant LOD off), to see
  which one makes it vanish.
- Plugin `4862de5d0bc42b97...`, deterministic; the same 4,213 records; deployed with the
  script and the SEQ.

### Later: the cameos speak (Barry's recorded lines)

Barry: "a cameos folder... a json of their lines and their filename... so they say it when
you interact with them".
- **The lines file** (`cameos/skyrim_fair_voicelines.json`) had two commas wrong at the end
  of Claudius's list (one missing after 15, one extra after 16), and was fixed. Garrick has 12
  lines, Claudius 16. The recordings are mono 44.1 kHz 16-bit.
- **`tools/cameos/build_voices.py`:** for each line, LipGenerator with the line's text (curly
  quotes straightened for it), then xwmaencode and LIPFuzer, into `build/cameos/<id>/<nn>.fuz`
  plus `lines.json`. All 28 are checked to carry a lip track.
- **The dialogue** (`FairCameos.cs`), when you talk to him (Skyrim's greeting is the Hello
  topic, `DialogueGenericHello`'s):
  - each has his own voice type (`SkyrimFairGarrickVoice`, `SkyrimFairClaudiusVoice`), so no
    vanilla line, all filtered by voice type, is ever his
  - a quest, `SkyrimFairCameos`, with a Hello topic each (Misc, subtype 0x4F, `SNAM HELO`,
    priority 50, no branch)
  - one INFO per line: Random, `GetIsID` him, the text as the subtitle, Happy (Garrick) or
    Puzzled (Claudius)
  - the .fuz copied to `Sound\Voice\SkyrimFair.esp\<voice type>\skyrimfaircameos__<INFO id>_1.fuz`
  - Checked subrecord by subrecord against `DialogueGenericHello` and its INFO `0D2C26`:
    - the topic's DATA (`00074f00`) and SNAM are identical
    - the INFOs have the same ENAM/CNAM/TRDT/NAM1-3/CTDA
    - the voice types' DNAM is 0, as the singers' is
- 33 records appended in the cameos' range (`0x6000A`-`0x6002A`); nothing else moves.
  Plugin `c27c7ee4d4576023...`, deterministic, deployed with the 28 voice files.
- Vanilla may also greet the player as he passes with a Hello; that's the same topic, so
  they may say a line then too.
- **To test:**
  - Talk to Garrick and Claudius: a line each time, their own voice, lips moving, the
    subtitle right?
  - Does the conversation close by itself, or does an empty menu stay open?

### Later: the cameos' names, and a horse on the stage roof

- **"Change their tag to their name":** in game they showed their face template's label
  ("Courier", "Breton"). A Traits-templated NPC shows the template's name. **No template
  now:**
  - Each copies the vanilla face field by field (race, voice, head parts, morphs, tints),
    and its FaceGen head and tint come from Bethesda's archives under his own FormID
    (`FairSingers.CopyFace`, as the singers'). All four files are byte-identical to
    Bethesda's.
  - So they're "Garrick Sol V" and "Claudius Vale". Same records, same FormIDs.
- **A horse on the stage roof** (Barry: "several cameo references to a horse being on a
  roof"), `cameos.roofHorse`:
  - The stage has no solid roof, just 7 rafter logs at z 690, 260 apart. So a little
    platform of two `StockadeScaffoldTop0Sided01` planks (248 x 262, 19 thick) lies across
    the middle rafters (u 0 and 260), at x 1918, y 5413/5675, z 723, resting on the logs'
    tops (about 708).
  - On it stands a copy of `EncHorseBrown` at (1918, 5544, 729), facing the crowd. Its
    package is `DefaultStayAtEditorLocation`, and it's Invulnerable, with `SkyrimFairNPC`.
    `SkyrimFairRoofHorse.psc` blocks activation (no riding it down) and holds it still
    (`SetDontMove`, on every load): there's no navmesh up there.
  - It has 4 records in the cameos' range (`0x60006`-`0x60009`). The exterior's silhouette
    copy now skips the cameos' objects, so its records don't move.
- Plugin `1ea844d83258391b...`, deterministic, deployed with the new script.
- **Barry, in game: the roof horse "looks fantastic and safe".** Confirmed.
- **To test:**
  - Do Garrick and Claudius show their names?

### Later: two new songs, two cameos, the dog, the floating grass

Barry: "the rock has been defeated!" Then:
- **The floating bush outside the east wall:** Barry's console click went through it and
  hit DynDOLOD's `Tamriel_UNDERSIDE`, so it isn't a real object.
  - The plugin has no landscape records in Tamriel, and places no plants outside.
  - It's most likely stale tree LOD, from Barry's 2026-09-14 DynDOLOD run, of a tundra
    shrub the exterior disables. The fix is a DynDOLOD re-run (Barry's tool), which also
    gives the compound LOD. Nothing is changed in the plugin.
- **The dog on the hay bale (east side):** its spot, (4800, 3950), was 57 from a
  `HayBale01`. It spawned inside the bale, was pushed on top, and had no navmesh to get
  down. The spot is now (4600, 3830), about 180 clear of the bale, the tent and the banner
  post.
  - An actor keeps its position in a save, so in Barry's save: click the dog in the
    console and type `moveto player`.
- **Two new songs:** "The Wanderer's Fair" (238.9 s) is **first** in the playlist, and
  "Raise Your Cups for Sol and Claude" (197.2 s) is last. They're built by
  `build_audio.py`, both at the stage's loudness.
  - They're flagged `"added": true`, so their records go in a new range, **`0x50000`**
    (`FairAddedSongs`, shared with the exterior's quieter copies): 8 records, and nothing
    renumbers.
  - They have no stems, so no timelines: every instrument plays and the crowd dances
    throughout, until Barry adds timelines. They have no singer lines either, so the
    singers gesture without lip sync.
- **The cameos** (`FairCameos.cs`, `fair.config.json` `cameos`), in their own range
  **`0x60000`** (6 records: a package, an NPC and a reference each):
  - **Garrick Sol V**, a Bosmer bard: the vanilla road courier's face and voice (young,
    eager), and fine clothes with a hat. He wanders a 2,200 sandbox from the avenue at
    (2048, 3300): the stage square, the dance floor and the social areas. Every 45-90 s,
    when he's free, he strikes up the lute (`IdleLuteStart`) for 25 s.
  - **Claudius Vale**, a Breton inspector: a vanilla commoner's face and voice, and the
    plain fine clothes. He wanders a 2,200 sandbox from (2048, 1000): the market lanes and
    the gate forecourt. Every 25-50 s he reads his notes (`IdleNoteRead`, the parchment),
    examines things (`IdleExamine`) or reads a ledger (`IdleBook_Reading`), in turn.
  - Both are Unique, Protected and Invulnerable, carry `SkyrimFairNPC`, and have no
    conversations (a copy of vanilla's `DefaultSandboxEditorLocation1024NoConv`). The
    culling never switches them off (`alwaysOn`). The stage script runs their idles
    (`CameoIdles`): it stops each idle after its hold (`BandStop`) and hands them back to
    their package.
- Plugin `5705780cafe9e63c...`, deterministic, deployed with the scripts and the two
  songs. Against `d1d9fe75...`, 14 records are added (0x50000-0x50007, 0x60000-0x60005),
  and nothing else changed.
- **To test:**
  - Does "The Wanderer's Fair" play first (from a fresh arrival or load)?
  - Is "Raise Your Cups" last?
  - Do you find Garrick round the stage and square, now and then playing the lute?
  - Do you find Claudius in the market, reading his notes and peering at things?
  - Do their faces look right (no dark face)?
  - Is the dog off the bale?

### Barry, in game: the sign "looking good"; the rock still there

- **The welcome sign, "a little" smaller:** scale 2.2 -> 1.8 (about 218 tall). Plugin
  `2b417b40429a36d1...`, deterministic, deployed.
- **"Whiterun might be west":** it's east. Whiterun Stables' map marker is at x 18,313,
  and the gate at -5,900; Rorikstead is west (-84,302). Facing the gate (south), the arm
  points to the left, east. Left as it is.
- **The rock in front of the gate was the fair's own path.** Barry clicked it in the
  console: `RoadChunkM01`, ref `0x30133`, the first road chunk of the approach.
  - The chunks are thin (M01 is 520 x 225, 41 deep). Each was tilted to the slope at its
    centre and set 6 into the ground, so where the ground curves, an edge rode out of it:
    knee-high by the gate, where the ground falls away. From the front, the path read as
    rocks. (The vanilla slab was already gone.)
  - **Barry chose: remove the path.** `exterior.approach.pave` is false. The way stays
    cleared, and the 12 pieces' FormIDs stay taken (`0x30133`-`0x3013E`), so the flags,
    road sign and welcome sign keep theirs.
  - Plugin `d1d9fe7591c55b68...`, deterministic, deployed. Against `2b417b40...`, only the
    12 chunks are gone.
  - The large-reference sinking stays: it's right regardless, and a DynDOLOD re-run is
    still due for the compound's LOD.

### Later: the "Whiterun Fair" welcome sign outside the gate

Barry: "could we implement the sign outside the exterior gate?" GPT's conversion of "Low-Poly
Wooden Sign made of Three Planks" (JeffK, Sketchfab), carved "Whiterun" / "Fair" with a
jester, was in `Downloads`.
- **Licence:** Sketchfab Standard, not CC BY (the package's CREDITS says so too). So it's in
  Barry's game only: git-ignored, out of any package until the author confirms
  (`CREDITS.md`, `docs/RELEASE.md`).
- **Copied:** the package to `assets/Skyrim_Fair_Sign_Assets/`, and its game files to
  `assets/meshes/barry_fair_sign/` and `assets/textures/barry_fair_sign/`. The zip and the
  folder's `Data/` were byte-identical.
- **The model:** 80 x 11 x 121, the origin at ground centre, the lettered face on its -Y
  (the `nif_preview` view from -Y matches GPT's render), and four fixed collision boxes.
- **Placed** (`exterior.fairSign`), at scale 2.2 (about 266 tall, like the road-sign post):
  - 300 left of the path and 480 out from the gate: (-6200, -10320), 200 short of the
    road, just clear of the path's edge stones. The road sign is across the path.
  - Its face points out toward the road, turned 15° toward the path.
  - The static and its reference are built with the exterior (`0x30145`, `0x30146`, the
    end of its range), so nothing renumbers.
- Plugin `117060471b14cf90...`, deterministic. Against `10285b9f...`, 2 records are added.
- **To test:**
  - Is it readable from the road, and turned to face you as you come in?
  - Is it upright, and sitting on the ground?
  - Is the lettering the right way round, with no stretching?
  - Is it solid?

### Barry's review, and fixes

Barry, in game: "it's looking really good". The outhouses were fine. Then:

- **The singers' steps are off:** "they got stuck at the back of the stage... just keep
  them static, just flailing their arms around".
  - `singerSteps.offsets` is `[]`, so the anchor isn't built (`0x40000`, `0x40001` are
    gone; that range is otherwise unused).
  - Their gestures carry on. On load, the script lets go of any offset (`SingersStand`),
    so in a save where they were stuck, their package walks them back to their marks.
  - The step code stays, for another try.
- **The clothes sign floated** (by the woodworker; it's the Clothing stall's, #26).
  - The plugin placed it as designed: composing the real meshes (`nif_preview`, the
    root's 180° turn applied) puts the board between the posts, its hooks on the
    `StockadeWoodbeamShort01`.
  - But in game the beam wasn't there. SMIM's board and USSEP's beam have the same
    extents, so the cause is still unknown.
  - Barry's fix, taken: **the honey sign's layout**, a board that carries its own bar. No
    vanilla clothes board has one, so it's the general goods sign (`0010704F`). The beam
    is `reserve`, so no FormID moves.
- **The palisade's banners, inside and out, are the towers' tall ones:**
  `CityBannerWhiterun01InsideTall` (`000DEE54`) at 1.25, as on the towers, where the
  wall had the short `DEE46`/`DEE4F`. On the exterior they're at its 60% (0.75, drop 42).
  Same counts, same FormIDs.
- **More rocks in front of the exterior gate:** every vanilla object there was already
  disabled, and no plugin in the load order places anything there. **The cause: large
  references.**
  - The flat slab (`023362`, `DirtCliffs01FieldGrass01`) is on Tamriel's large-reference
    list (RNAM). The engine shows a large reference's LOD model until the reference itself
    is loaded and showing, and a disabled one never is. So its LOD stood there.
  - **Fix:** cleared large references are no longer disabled. They're left enabled and sunk
    3,000 under the ground (`exterior.sinkLarge`), so the engine loads them and hides the
    LOD. Three are: `023362`, `039159` and `0CB03D`.
  - Barry's DynDOLOD output dates from 2026-09-14, before the exterior. Re-running it
    would also give the compound LOD.
- **The Whiterun road sign is outside now:** it stood inside, squeezed between a tower's
  legs. Barry chose "outside, by the gate".
  - The inside pair is `reserve`: single dressing pieces now take `reserve: true` too.
  - The new sign is `exterior.roadSign`: the post and the Whiterun arm, 280 to the right
    of the path and 320 out from the gate, the arm pointing east along the road
    (`0x30143`, `0x30144`, appended to the exterior's range).
- Plugin `10285b9fc2a971e4...`, deterministic. Against `b173d103...`:
  - 5 records gone: the beam, the inside road sign pair, the anchor pair
  - 2 added: the outside road sign
  - 3 vanilla overrides changed: the sunk large references
  - nothing renumbered

  The footprints lost the road sign and Solitude board. The market places 1,720 pieces.
- **To test:**
  - Are the singers back on their marks, gesturing?
  - Does the Clothing stall's sign hang from its post?
  - The tall banners on the palisade, inside and out: do they clear the ground, and sit
    against the logs?
  - Outside:
    - Is the slab in front of the gate gone?
    - Is the road sign beside the path, pointing along the road?

### Later: Stroti's outhouse replaced by Strifey7's (CC BY 4.0)

Barry: "our skyrim outhouse is also ready... install the Nif". GPT's conversion of
"Outhouse" by Strifey7 (Sketchfab, CC BY 4.0) was in `Downloads`.
- **Copied:** the package to `assets/Skyrim_Outhouse_Assets/` (untracked, like the
  palisade's), and its game files to `assets/meshes/barry_outhouse/outhouse.nif` and
  `assets/textures/barry_outhouse/` (committed; CC BY allows it). The NIF's three texture
  paths resolve.
- **The model** (checked with `nif_preview`): 48.5 x 58.5 x 88, origin centred at ground
  level, door on its -y face and part of the mesh.
- **Placed as the old one was:**
  - scale 2.2 (about 192 tall, like Skyrim's doors; Stroti's was 195)
  - y 0, not 39: its origin is centred, where Stroti's wasn't
  - yaw 0, not 180: its door faces the other way
  - The six outhouses keep their FormIDs and positions, and their doors still face the
    lane.
- **The separate door pieces are gone**, as `reserve: true`: their six FormIDs stay taken,
  so nothing renumbers. The door STAT (`SkyrimFairOuthouseDoor`) is kept for its FormID,
  pointing at the new mesh, and nothing places it.
- Plugin `b173d10375cebd1b...`, deterministic. Against `e89da4ef...`, the only change is
  the 6 door references gone (`0x11EA`-`0x11F4`, even IDs). Footprints are regenerated:
  the outhouse's added, Stroti's two dropped. The market places 1,723 pieces (6 fewer);
  the navmesh has 7,909 triangles (was 7,916).
- **To test:** the two outhouse rows (west of the market, and by the east wall walk).
  - Are the outhouses the right size beside a person?
  - Do their doors face the lane?
  - Do they sit on the ground, with solid collision?
  - How do the textures and the glossy material look?

Barry: the singers should move, "maybe like crab walking style". He chose **the three
sliding as a line**, **200 units, every 8 s**.

Plugin `e89da4ef21206fab...`, deterministic (two builds, same SHA256), deployed with the
recompiled scripts. Against the deployed `eed0547c...`: every record is unchanged, and **2
are added** in a new range, `0x40000` (`SkyrimFairSingerAnchor` NPC and its reference).
The navmesh is identical: 9 meshes, 6,267 vertices, 7,916 triangles.

- **How:** while the show runs, each singer holds an offset from an **anchor** with
  `KeepOffsetFromActor`, and his heading holds too. The anchor is an invisible Nord at
  scale 0.5, AI off, under the deck's centre (2048, 5544, 0), facing north. Each singer's
  offset is his mark plus the line's sideways shift, facing his own mark's heading (175 to
  185°, the crowd).
  - Moving the shift should make the engine walk them sideways with its own left and right
    walk animations, still facing the crowd. That's the crab walk. No new animations, no OAR.
- **The steps** (`songs.config.json` `singerSteps`, Barry's to edit): offsets `0, -200, 0,
  200` in turn (plus is east, the crowd's right), one every 8 s while they sing. The first
  comes 6 s into a song, and each song starts on the marks.
  - **Resting** (a `rest` stretch), they go back to their marks to clap. The next step comes
    a full 8 s after they start singing again.
  - **At a song's end** they go back to their marks for the wave.
  - Leaving the fair, or a game load, lets go of the anchor: their stay-at-editor-location
    package has them again.
  - **Gestures wait for steps:** none starts during a step (3 s, `stepSeconds`), or if it
    wouldn't end before the next step. So they gesture together in the 5 s between steps.
  - An empty `offsets` keeps them still, as before.
- **Papyrus:** `SkyrimFairAudioScript`: `SingerSteps`, `SingerOffset`, `HoldGestures` and
  `SingersStand`; the properties `SingerAnchor`, `SingerHomeX/Y/Z`, `SingerFacing`,
  `SingerStepOffsets`, `SingerStepEvery`, `SingerStepSeconds`, `SingerFirstStep`,
  `SingerCatchUp` (1000: under it they walk, never run) and `SingerFollow` (12). The log
  says `SkyrimFairAudio: singers step to <offset>` on each step.

**Unknowns, for the test:**
- Do they really sidestep, or turn to walk and then turn back? The engine decides that.
- Is the angle in degrees? If they face the wrong way, it's radians.
- Does the anchor stay put under the deck, and out of sight?
- Does the lip sync keep up while they move (Fiddle, Dragonborn-Approved)?

**To test (Barry):**
1. Watch a song from the dance floor. After about 6 s, does the line step to its right (your
   left)? Then back, then the other way, every 8 s?
2. Do they face you while they move, or turn?
3. Do they walk (not run, not slide)? Do they bump the lutes behind them?
4. In a rest stretch, do they go back and clap? At the end, do they wave on their marks?
5. Can you see the anchor anywhere (a small figure under or on the deck)?
6. If it goes wrong, `Papyrus.0.log` lines starting `SkyrimFairAudio: singers step`.

### Permissions audit (Barry: "audit if there's any mods we borrowed from that we still need to ask permission for")

Every file in the deployed folder was traced to its source (the detail is in `CREDITS.md`,
"Audit of what ships"):
- **Whiterun Mossy Wet Stonefloor (99294): the avenue cobbles were its `wrstonefloor01`
  textures, byte for byte.** Replaced (below).
- **New finding: Vanilla Remastered - The New Normal (153879).** The singers' three face
  tints came from its archives. The modlist's `Skyrim - Textures*.bsa` are its, and
  `singers.faceArchives` searched them. The face meshes are Bethesda's.
  - **Fixed:** `singers.faceArchives` is now the Steam install's `Data` alone. All six
    FaceGen files are byte-identical to Bethesda's. The plugin is unchanged
    (`e89da4ef...`), and the three tints are deployed.
- **CREDITS.md rewritten** around what ships: a summary table, then the bundled
  third-party assets, the files built from vanilla, the project's own work,
  dependencies (Holidays, OAR, Professional Dancer), the SPID patches, what was looked at but
  not used, and the build tools. The ledger is updated.
- **Still pending, as before:** the scaffold tower's source and licence, the music's
  provenance, Astra's credit, and the SPID `_DISTR.ini` copies' authors (Maximum
  Destruction, Stealth Detection Fixes, Strange Runes).
- **Clear:**
  - the palisade and gate (CC BY 4.0)
  - Stroti's outhouse (a modder's resource; not to be re-uploaded)
  - Holidays (a master, nothing copied)
  - Professional Dancer (detected at runtime, nothing copied)
  - the 174 props (Bethesda's meshes)
  - the crowd figures and the OAR tempo clips (vanilla)
- **Downloaded, not used:** Crowded Streets, Diverse Archery Targets, Fireworks (183953),
  Incaendo's Banner Resource, Terrain Parallax, Medieval Markets, Riverwood Walls and
  Whiterun Stone Stairs.
- **Then, Barry: "let's replace them. Can we make our own parallax textures?"** Done:
  - The cobbles are vanilla `wrstonefloor01` (diffuse and normal, byte for byte, from the
    Steam install's archives), and **the fair's own parallax map**, `Cobble01_p.dds`.
  - `tools/make_cobble.py` builds the set. The height is integrated from the vanilla
    normal map's slopes: a least-squares surface in the Fourier domain, which wraps
    without a seam because the texture tiles.
    - Features wider than two stones are filtered out, and 15% of the diffuse's light and
      dark is added for grain.
    - The top 5% is clipped, so the stone tops plateau and the mortar is deep.
    - The green channel is read "down", chosen because that reading agrees with the
      diffuse (+0.29 against -0.07).
  - It's an 8-bit luminance DDS, 1024 x 1024, with all 11 mip levels, like the old one
    (512, no compression).
  - **Barry chose copies over vanilla's paths.** A Whiterun replacer lays its stones out
    differently (Mossy Wet's correlate -0.02 with vanilla's), so pointing at vanilla's
    paths would put our bumps under someone else's stones.
  - Same paths, so the plugin is unchanged (`e89da4ef...`). The three textures are
    deployed.
  - **The look changes:** vanilla's stone is darker and greener than Mossy Wet's grey.
  - **To test:** walk the avenue. Do the cobbles stand up from the mortar, and do the
    bumps sit on the stones? Is there no seam where the texture repeats, and no shimmer
    at a distance?
- **A leftover:** the deploy folder still has the old procedural cobble
  (`textures\SkyrimFair\SkyrimFair_Cobble01*`), which the plugin doesn't use. It stays out of
  any package.

## the compound's exterior in Tamriel, replacing the old terrace (2026-09-24)

Barry: "build the exterior outside of whiterun. I did have a place at -2 -4 in tamriel. I
think we should knock that down and replace it with the exterior of the compound". He
chose:
- the spot: shifted south, with the gate on the road
- 60% scale
- the wall following the ground (no landscape edits)
- silhouettes inside

Then he added: "Could we show the watchtowers as well? And have the fireworks come out
from it? And some faint music".

Plugin `fbe8255c0b5bd0e2...`, deterministic, deployed with `SkyrimFairOutsideShow.pex`.

**Barry, in game: "honestly astonished at how good you've done this!"** He asked for the rock
in front of the door to go, a path up to it ("or a stone wall, whatever you think is more
natural"), and "the two large whiterun flags at both sides of the door".

**Follow-up** (plugin `eed0547ce3b3aff8...`, deployed): all 4,110 records are unchanged,
and 16 are added (`0x30133`-`0x30142`).
- **The approach:** from 150 out of the gate to the road at (-5900, -10120), 900 wide,
  every vanilla thing is disabled except the road. That's the flat rock (a vanilla
  `DirtCliffs01FieldGrass01`, too big for the wall's clearing), two rock piles and 9
  tundra shrubs.
- **A path, rather than a wall:** a path reads more natural where the gate meets the road.
  - It's made of vanilla road chunks every 170: a `RoadChunkM01` across the middle and
    ragged `S01`/`L01` pieces at the edges (12 pieces).
  - Each is tilted to the ground's slope. The convention was measured from vanilla's road
    pieces in the area: Z, then Y, then X, about world axes, clockwise; this fits their
    terrain to 5°, against 7.4° flat.
- **Two Whiterun flags** (`CivilWarBanner01` pole with `CityBannerWhiterun01`, as the
  avenue's `banner_whiterun`), 450 either side of the gate and 110 out, facing the road.


**FormIDs:** against the life pass (`904835d8...`):
- 3,802 records are unchanged.
- **471 are gone: the whole old Tamriel prototype** (the terrace, its clutter and the test
  stall, `0x801`-`0x9E0`). No FormID is reused.
- One changed: the fair's main gate (same FormID) now has a door base.
- 307 are added, all in the exterior's range `0x30000`-`0x30132` (`FairExterior.cs`, built
  last, the counter put back after).
- The terrace's STAT kit records and the sandbox cell stay.

- **Where:**
  - The fair's outline, scaled 0.6 about its gate and turned 180°, with the gate at
    (-5900, -10800), about 750 south of the Whiterun-Rorikstead road and facing it.
  - It spans cells (-2..-1, -4..-3), 4,300 x 5,000. It's centred on the old site, so it's
    basically where the terrace was.
  - Inside, nothing important was in the way: no road, stream or Civil War markers. The
    road runs along y ≈ -10,050, and the Fort Greymoor siege markers lie west and north.
  - The relief inside is about 300.
- **The wall:** 36 of the fair's own palisade panels, laid by the same code on Tamriel's
  ground. Each stands on the lowest ground under it, so no end floats on the slopes (the
  west side falls up to 0.4).
  - The outer face carries 18 banners, 4 pennant-rope halves and 8 lanterns (the life
    palisade settings, outward).
  - No landscape is edited.
- **The gate is a load door both ways:**
  - `SkyrimFairExteriorGate` ("The Wanderer's Fair") and `SkyrimFairExitGate` ("Whiterun
    Hold") are copies of Whiterun's main gate door (its open and close sounds), with
    Barry's closed gate model.
  - The fair's own gate reference is now the exit door, persistent.
  - You arrive 300 in from the gate, facing the stage, or 300 out, facing the road.
  - The map marker (same FormID) moved to the road, 900 in front of the gate.
- **Inside, what shows over the wall and from the hills:** 217 pieces copied from the
  built fair, whole and unscaled:
  - the stage, 4,380 in from the gate
  - the four light towers (two by the gate, two flanking the stage, moved out to fit the
    smaller compound), with their banners and braziers
  - the three south cook fires, with their smoke and lights, at their scaled places
- **Heard and seen from outside:**
  - `SkyrimFairOutsideShow`, on a marker at the stage, runs while its cell is attached.
  - The fair's four songs play in turn, 20 s apart, from a speaker over the stage. They're
    12 dB under the stage mix, heard from 1,500 to 9,000.
  - A crowd-murmur loop plays at the middle, 6 dB down, out to 4,000.
  - At night (20:00-05:00), about every 150 s (varied), a volley of the fair's fireworks
    goes up from its three launch sites behind the stage, a colour each, rotating.
  - `SkyrimFairFireworks` and the music global turn them off.
- **Disabled vanilla:** 37 references inside the outline or through the wall line (trees,
  rocks, a critter spawner, three plains-prey spawns). Landscape-sized rocks (radius over
  900) are left alone.

**Known limits (for Barry and `docs/RELEASE.md`):**
- **Tamriel's navmesh isn't cut:** vanilla NPCs and animals may try to walk through the
  walls.
- **No LOD:** the compound shows only within the loaded cells (about two cells away), not
  from far mountains. Object LOD (DynDOLOD / xLODGen) would fix it; that's a Barry-run
  tool.
- The music and fireworks run only while the compound's cell is loaded.

**To test (Barry):**
1. `coc` somewhere near, or fast-travel to the map marker "The Wanderer's Fair".
2. From the road, check:
   - Does the wall sit on the ground all round?
   - Do the stage roof and the towers show over it?
   - Do the banners hang on the outer face?
3. Walk to the gate and activate it: do you arrive inside, facing the stage? Activate the
   gate from inside: do you come out facing the road?
4. Can you hear the music faintly near the walls, and the crowd murmur near them? Too
   loud, or too quiet? (12 and 6 dB)
5. At night, wait a couple of minutes near the gate: fireworks?
6. Climb a hill nearby and look down: does the inside read as the fair?

## Previous pass: more life (palisade banners, plants, grass, props, smoke, animals) (2026-09-24)

Barry picked options A, B, C, D, E and G of the audit below ("The sounds will be drowned out
by the bards", so F is dropped). He also confirmed the children and the seated visitors
("look great"), and the folk pair is fine at a distance. The singers don't walk the deck;
that isn't built.

**Barry, in game: "The decorations you've made look fantastic, you've really helped bring
this to life."** Confirmed as a whole; grass (the live-grass test) and the details on the
test list below not yet reported one by one.

Plugin `904835d8ddbbc0fb...`, deterministic (two runs, same SHA256), deployed. Against
the deployed `7ec9724f...`: **all 3,915 records keep their FormID and content**; 359 are
added, all in the new life range `0x20000`-`0x20166` (`FairLife.cs`, see
CODEX_HANDOVER). The counter is switched to that range and put back, so the navmesh, the
culling global and everything built after keep their FormIDs. The navmesh and the culling
see the new things, since they're built first.

- **A. The palisade:** 34 Whiterun banners (`CityBannerWhiterun01Inside`/`02Inside`,
  alternating) on the inner face, every second panel, 70 below the wall's top, none within
  520 of the gate. Between each pair on a straight run, a Holidays pennant rope (the two
  colourways) is swagged in two mirrored halves, as the lane crossings are: 32 halves hung
  with 78 Holidays lanterns.
- **B. Plants:** 81, as candidates along a strip 110-190 inside the wall and just past
  the lanes' edges, each kept only where the market's fit check passes and clear of NPCs.
  - Mostly tundra shrubs, Reach ferns and tundra scrub.
  - A quarter are flowers: lavender, the mountain flowers and tundra cotton. These can be
    picked, like vanilla's.
  - Few spots along the lanes passed; the market is full there.
  - Plants cut no navmesh and hide no one from the culling (`navmesh.ignoreBases`).
- **C. Grass:** the grass texture's copy now carries FieldGrass02 (with flowers) and
  TundraGrass03 on top of its TundraGrass01/04. **But this profile only loads grass from a
  cache** (`bAllowCreateGrass=0`, set by Grass Cache Helper NG), and there's no cache for
  `SkyrimFairWorld`, so no grass will show until one is generated. See "To test".
- **D. Props:** 17 groups, all placed:
  - a goat pen of 8 Whiterun fence pieces with hay and a bucket (at about (0, 3500), on
    the open west ground), with a haystack, a feed store and a tool corner beside it
  - three pelt-drying lines: two hitching posts, a rope, pelts draped over it and garlic
    hanging from it (at the east edge, the north-east camps and the north-west)
  - five lantern posts: a farm banner post with two Holidays lanterns hung from its
    crossbar (356-376)
  - three tool corners: a pitchfork, shovel and broom leant on a barrel, with a bucket and
    an axe
  - three produce piles by the cook fires: a food barrel, baskets, cabbages, potatoes,
    apples and a sack
  - Vanilla has no laundry line, so the drying lines stand in for it.
- **E. Smoke:** `FXSmokeChimney01` at 0.6 scale over the 5 cook and social fires.
- **G. Animals:** fair copies of vanilla creatures, invulnerable, with the SkyrimFairNPC
  keyword (the SPID exclusions), switched by the crowd culling (184 switchable, was 173):
  - 6 chickens in pairs by the three south cook fires
  - 3 goats in the goat pen (vanilla's 512 sandbox; the pen's fences keep them in)
  - 2 dogs by the camps: unaggressive, in CreatureFaction only (not DogFaction), on the
    creature sandbox
  - All 244 actors are on the navmesh.
- The culling table: mean 128 on at a spot, 138 of 184 at the square (the same share as
  before).

**To test (Barry):**
- Look along the palisade, from the square and near the wall:
  - Do the banners hang against the logs, not floating off them or sunk into them? (`out`
    34, `bannerDrop` 70)
  - Do the ropes swag between them, with the lanterns on the curve?
- Do the plants look right by the wall and the lanes, not in anyone's way?
- Check the new props:
  - The goat pen: do the goats stay in?
  - The pelt lines: are the pelts draped over the rope, not beside it?
  - The lantern posts: do the lanterns hang from the crossbar?
  - The tool corners: are the tools leant on the barrel, not away from it or into it?
  - The produce piles
- The smoke: is the chimney plume right over a campfire, or too big? (0.6 scale)
- Do the chickens and dogs wander near their spots without fighting?
- Is the frame rate still fine?
- **Grass, a quick test:** in `Grass Cache Helper NG`'s ini set `SetGrassLoadCreate = 0`.
  In the profile's `skyrim.ini` [Grass], set `bAllowCreateGrass=1`. Go to the fair and
  see if grass grows. Put both back after.
  - If it looks good, keeping it needs a grass cache for `SkyrimFairWorld` (a precache
    run), or those settings left on.
  - Shipping a cache is a release question (`docs/RELEASE.md`): it depends on the user's
    grass mods.

## Previous pass: audit, "more life" in the fair (2026-09-24, research only)

Barry confirmed: the children and the seated visitors "look great"; the folk pair is fine at
a distance. **The singers don't walk the deck** (that isn't built yet). He asked for the
audit (plan item 5) plus **"banners and flags stuck to the wooden fencing around the area"**.
Nothing is built. Surveyed from `Skyrim.esm` with Mutagen and from the modlist's folders.

Every option below is **built last** (appended after the fireworks), so no FormID moves.

| # | Option | What (vanilla records) | Cost | Risk |
|---|---|---|---|---|
| A | **Banners on the palisade** | Wall-hung Whiterun banners `CityBannerWhiterun01Inside` 0DEE46, `02Inside` 0DEE4F, `01InsideTall` 0DEE54 (already on the towers), cloth-sim MSTTs. Poles: `FarmBannerPost01` 1083D7, `SBannerPost01` 0A6E56, `CivilWarBanner01` 060166. Placed every Nth panel from the palisade's own panel positions, inner face, alternating heights | small (static refs; a dozen or two MSTTs sway) | low; check the gate and corners |
| B | **Shrubs and ferns** in the walls and along the paths | `TreeTundraShrub01`-`10` 0AAE79-0AAE8B, `TreeReachFern01`-`03` 0B8A6E-70, `TreeFallForestFernCluster01`-`04` 0B8A6A-6D (not harvestable). Flowers are all harvestable ingredients (`TreeFloraLavender01`, the mountain flowers, tundra cotton): fine as picking flavour, or skip | small | low; keep them off the navmesh paths (footprints) |
| C | **Grass on the ground** | `FieldGrass02` 1098CA (with flowers), `ForestGrass02` 0F69A4, `FallForestGrass02` 05B5A8 on the fair's LTEX copies | small (GPU) | **high**: the modlist loads grass from a cache (Grass Cache Helper NG, `UseCGID=1`, and it switches off live grass creation when not precaching). A new worldspace has no cache, so probably no grass shows until a precache run covers `SkyrimFairWorld`. Test one LTEX first |
| D | **More props**: laundry, lanterns, hay, bunting, tools, food | Laundry: vanilla has only `WRHerbDryingRack01` 0B9BEA; lines would be a rope plus hung clothes (misc items, statics). Lanterns: the Holidays runs already exist, so more is config. Hay, tools, food: the existing modules | small per piece | low |
| E | **Smoke from the cook fires** | `FXSmokeChimney01`/`02` 0B09E4/0B09ED, `FXSmokeWispsLgVolAdjust` 01643C, `FXSmokeSmokeLg1x1` 0B9496 at each cook fire and camp | small (a few particle systems) | low |
| F | **The fair's sounds** | `AMBTavernExteriorLP` 100F8B (a murmur by the food), `NPCHumanBlacksmithRepairHammer` 0EA51D (Distant 0EA51E), `NPCHumanWoodChop` 06D1CA, `FXFireMedium01LP` 0DDE3C at the cook fires, `NPCDogBark` 05B37D/Distant 05B37C, the cow sounds by the pens | tiny | low; keep them under the stage's levels (confirmed, don't change) |
| G | **Small animals** | `EncChicken` 0A91A0, `EncDog` 023A92, `EncGoatDomestic` 04359C, `EncCow` 023A90 | **real AI cost** per animal; culling only switches the fair's own records, so each would be a duplicated fair NPC (culled) with a sandbox package in a pen or a yard | medium: dogs pick fights with other creatures; wandering chickens block paths |

**Recommended order:** A (asked for), E and F (cheap, big effect), B, then D. G: a few
penned animals (chickens by the food, goats in the pen), duplicated and culled. C only after
a one-LTEX test, since the grass cache likely hides it.

## Previous pass: the show keeps time with the music (a real-time clock) (2026-09-24)

Barry: **"we got new dances!!"** (Professional Dancer through Pandora: confirmed), but
"there's a big delay, like 10-20s between the song ending, the cheer & firework starting".

- **Measured from `Papyrus.0.log`:**
  - Song 0 started at 16:10:44. The script put its cheer at 148.9 s, on time by its own
    clock, but logged it at 16:13:48: 184 s of real time.
  - Through the song the script's clock ran 12-35% behind real time (a table of log
    timestamps against the script's "at X s").
- **The cause:** the show was timed on game time, which the engine lets fall behind real
  time on long frames. The music plays in real time. (A 30 s real / 7 s script stretch was
  a menu or the console: our stage category is under `AudioCategoryPausedDuringMenuFade`,
  so the music pauses there too.)
- **The fix:** a show clock (`ShowNow`) that counts real seconds, each step capped at 1.6x
  the game time that passed plus 0.25 s, so menu time isn't counted. The music's schedule
  runs on it: song and cheer ends, sections, sung lines, the band's last note. The
  animations' replays stay on game time, since their clips run on game time: the dances,
  singer gestures, the folk pair, the drift-in caps.
- Script only; the plugin is unchanged. Deployed.
- **To test:** does the cheer (and the fireworks) now follow the end of each song within a
  second or two? The log's `cheer ... at X s` against its timestamps should agree.

## Previous pass: dependency-free fireworks of our own; Professional Dancer only when it's installed (2026-09-24)

Barry: "let's do the fireworks ourselves ... Then maybe we just have a check if dances.esp
is installed? if not use the vanilla dances, if they are have more varied dances?"

### Fireworks: the fair's own, vanilla effects, no dependency
- Fireworks.esp isn't used. Per colour, **duplicates of vanilla records** (so every
  vanilla subrecord is kept), appended last:
  - EXPL `SkyrimFairFireworkBurst{Gold,Blue,Violet}`:
    - gold: FireStormExplosion, with FireballStormImpactExplosionNoDamage placed
    - blue: crExplosionFrost01
    - violet: ChainLightningMassExplosion
    - All: damage 0, force 0, radius 400, no enchantment or impact data, a vanilla
      light. Knockdown flags cleared; read back, none of the three had one (flag 4).
  - PROJ `SkyrimFairFireworkShell*`: the vanilla fire bolt, speed 1500, Explosion +
    AltTrigger, timer 3.0 / 2.2 / 2.4 s (`fireworks.climb`), so it bursts at about
    4,500 / 3,300 / 3,600 units.
  - MGEF and SPEL: FireDamageFFAimed and Firebolt, magnitude 0, cost 0.
- The launch sites (`175E`-`1760`, same FormIDs) are now vanilla `xMarkerActivator`, which
  can cast. There's an aim marker above each (`1762`-`1764`, spread 120 apart).
- The script casts `FireworkShells[k].Cast(site, aim)` as each song ends, `stagger` 0.6 s
  apart, and `atNight` (20:00-05:00) more from the middle site. The Fireworks.esp lookup
  properties are gone.

### More dances: only when Professional Dancer is installed
- `songs.config.json` `moreDances` (events Dance1/3/4/8/9, with the clip lengths) and
  `moreDancesPlugin` (Dance.esp). The script checks for DanceIdle1 (`000803`) once per
  load with `Game.GetFormFromFile`. When it's there, dancers take those dances in turn:
  `Debug.SendAnimationEvent(IdleForceDefaultState)`, 0.1 s, then the dance event, as the
  mod does (a package can swallow the first). Otherwise, vanilla dances.
- The events exist only after Pandora or Nemesis has run with the mod installed. Its
  dances loop, so leaving a mod dance (clap, cheer, the song's end, leaving the fair)
  settles the dancer with `IdleForceDefaultState` first.
- The copied-clip OAR route is gone: `build_dances.py` deleted, the clips removed from
  `assets/` and from the mod folder (only the 11 files deployed earlier).

- Plugin `7ec9724f8fe91a24...`, deterministic. 15 records added (3 aims, 12 firework
  records), 3 sites re-based. Deployed.
- **To test:** fireworks after every song, no mod needed (`set gamehour to 22` for the
  night shells). For the dances: install Professional Dancer with Dance.esp enabled and
  re-run Pandora; the log says `Dance.esp loaded: True`.

## Previous pass: dance styles from Professional Dancer; fireworks after every song (2026-09-24)

Barry: "Yeah that sounds good for dancer. And for fireworks yeah let's use the lighter ones
:) as long as we can just automatically trigger them. Maybe they go off after the end of
each song?"

### Dance styles
- `songs.config.json` `danceStyles`: the five steady clips from the audit: dance1 25.367,
  dance3 15.233, dance4 9.4, dance8 21.767, dance9 15.633 s. The big-sway dance2/5/7/10 and
  the short dance6 are left out.
- `tools/bards/build_dances.py` copies each clip from the installed mod (a mods folder
  named `*Professional Dancer*`) or else from its archive in `external/` (py7zr). It
  writes `OpenAnimationReplacer/SkyrimFairDances/StyleNN/special_cicerodance1.hkx` and
  checks each length with the HKX codec. Git-ignored, never shipped (the clips are
  Mixamo's).
- The generator writes each submod's `config.json`: `HasKeyword SkyrimFairNPC` plus
  `CompareValues` actor value 77 (**Variable10**, not read by anything in Barry's list;
  checked), `"actorValueType": "Value"`, == N. Priority 1900000002. The format is from
  OAR's `SharedTypes.cpp`.
- Script: while dancing, each dancer takes the styles in turn (`SetActorValue("Variable10",
  N)` then `PlayIdle(IdleCiceroDance1)`), replayed at the style's length
  (`DanceStyleLengths`).
- **Mode changes:** the long clips mean a dancer could otherwise take up to 25 s to
  switch, so `CapDances` cuts each dancer's move to 0-3.5 s from the change (staggered by
  dancer). Also at the song-end cheer.

### Fireworks
- `songs.config.json` `fireworks`: plugin `Fireworks.esp`; `afterSong` =
  FWSkyBursterMultiActivator 029EBD, FWSkyBursterSingleActivator 009417, 029EBD (one per
  site); `atNight` = FWWhiteFlareSingleActivator 03915F; three sites behind the palisade
  at (1450, 6600), (2048, 6650), (2650, 6600), 200-380 from the nearest tree; stagger 0.6
  s. The IDs were checked against the plugin.
- Generator, last of all: persistent XMarkers `SkyrimFairFireworkSite01-03`
  (`175E`-`1760`), global `SkyrimFairFireworks` (`1761`, 1 = on), and script properties.
- Script `Fireworks()`, as each song ends (with the cheer):
  - `Game.GetFormFromFile` finds whether the plugin is loaded, once per load. Missing:
    nothing happens.
  - `PlaceAtMe` places a launcher at each site in turn. The launchers fire themselves,
    burst about 4 s later during the cheer, and delete themselves.
  - Between 20:00 and 05:00, a flare from the middle site as well.
- Plugin `238c94af132ae392...`, deterministic; only those 4 records added. Deployed with
  the scripts, the dance clips and their configs.
- **Barry to install:** Professional Dancer (Dance.esp can stay off; `build_dances.py`
  then reads the install instead of the archive) and Fireworks with `Fireworks.esp` on
  (check its Nexus permissions). Then test from a clean start.

## Previous pass: audits of Professional Dancer and Fireworks (2026-09-24, research only)

### Professional Dancer 1.5.0 (Nexus 124608), `external/`
- An FNIS behaviour mod: `FNIS_Dance_List.txt` makes events `Dance1`-`Dance14`
  (behaviour generation needed). It dances by `Debug.SendAnimationEvent`, from the
  player's powers and a menu. `Dance.esp` is ESL; masters SkyUI_SE and UIExtensions. Not
  installed in Barry's list.
- **10 new dances**, lengths read with the HKX codec: dance1 25.37, dance2 28.0, dance3
  15.23, dance4 9.40, dance5 11.93, dance6 3.20, dance7 26.13, dance8 21.77, dance9 15.63,
  dance10 27.33 s. Plus `spectatorcheer.hkx`, 26.67 s and seamless. Dance11-13 are copies
  of vanilla Cicero.
  - No root motion.
  - dance2, 5, 7 and 10 sway the body 0.8-1.3 m (keep them for dancers with room).
  - Most pop 7-15 degrees at the wrap; our restart-with-a-blend replay hides that.
- **Recommended:** OAR per-dancer swaps of `special_cicerodance1/2.hkx`, conditioned on
  `HasKeyword SkyrimFairNPC` plus `CompareValues` on a per-actor value the script sets
  before `PlayIdle` (the `Variable0x` slot still to be checked against the list). The
  script keeps the exact replay timing from a length table. No Pandora, no `Dance.esp`.
- **Licence:** CC BY-NC (CREDITS.md), but the clips are Mixamo's, so the .hkx can't be
  shipped. A local tool would copy them from the user's install into our OAR folder.

### Fireworks (Nexus 183953), `external/`
- `Fireworks.esp` is a full ESP (not ESL), masters Skyrim/Update. It edits vanilla
  Honningbrew, Dragonsreach, navmesh 0BFB15, `WhiterunHoldLocation` and package 0978E3.
- 56 LE-format NIFs (unconverted); scripts with source. No licence file; the Nexus page's
  permissions are unread (403).
- **Firing needs no player.** A launcher activator placed with `PlaceAtMe` fires itself
  (OnLoad, about a 1 s prep, `Spell.Cast` straight up, bursts at about 4,500 units),
  then deletes itself.
  - Our script can look it up with `Game.GetFormFromFile` (a soft dependency, no master,
    nothing bundled).
  - Or cast the shell spells directly, for tighter timing.
- **Cheap:** mortar launchers `FWSkyBursterMultiActivator` 029EBD (10 shells) and
  `FWSkyBursterSingleActivator` 009417, and the white flare 03915F (one radius-3000 light,
  30 s).
- **Heavy:** star bursters and rockets spawn 120-200 scripted dynamic lights each.
- **Never:** `FWBungerActivator` (casts Firebolt), `FWFlameJetActivator` (fire spray).
  Explosions do damage and knockdown, harmless only at altitude in the open.
- Timing: place about 4 s before the burst is wanted.

## Previous pass: the crowd drifts into a change; no mid-song cheers (2026-09-24)

Barry: **"I can't get on stage"** (the solid walls work, confirmed). And: "it feels a bit
unnatural when the crowd goes to cheer from dancing. They just stop what they doing eerily
and turn to the stage... Maybe we shouldn't have the cheer."

- **No mid-song crowd cheers:** in Fiddle, Hey-Ho and Dragonborn-Approved each `cheer`
  became `dance` (repeats merged). The song-end cheer stays.
- **Changes drift in:** a crowd change no longer cuts every dancer off at once. Each dancer
  takes the new mode up when the move in hand ends (clips of 6-9 s, so the floor changes
  over a few seconds). A dancer switching into clap or cheer turns to the stage then,
  alone, in the 3 smooth steps (`FaceStage(which)`).
- The song-end cheer works the same way: `Cheer()` sets the cheer mode, and `Dance()`
  keeps running through the cheer phase.
- Plugin `8d824fb8095588e9...`, deterministic, FormIDs unchanged, deployed with the
  scripts.
- Audits of Professional Dancer (dance variety) and Fireworks (auto-triggering) are
  running.

## Previous pass: arrangements of Fiddle and Hey-Ho, Skyrim, from their stems (2026-09-24)

The same method as Dragonborn-Approved.
- **Fiddle** (202.8 s, onsets suggest 72 bpm, likely half-time):
  - a drumless fiddle intro
  - instrumental hooks at 0:23-0:35, 2:05-2:16 and 3:10-3:16: all fast, singers clap,
    crowd cheers in
  - a near-silent stop at 0:37-0:47, a 2.5 s dead stop at 1:00, and quiet stretches at
    1:37-1:52 and 2:17-2:40: holds, crowd claps
  - Vocal gaps from `cues.json`.
- **Hey-Ho, Skyrim** (219.9 s, about 146 bpm):
  - no `cues.json`, so its lines come from the vocal stem (`lines_from_vocal`)
  - drum dips at 0:10, 0:50, 1:52 and 2:17 (drums hold, crowd claps at the first two)
  - a build to 1:05-1:20 (drums and lute fast, flute fast over the vocal gap at
    1:19-1:23, crowd cheers)
  - a full stop at 2:45-2:52, and a 2.5 s stop at 3:30 before a fast, cheering finish
- Built schedules match independent merges (Fiddle 29 sections, Hey-Ho 24). Plugin
  `7ea819bac05f9d19...`, deterministic, FormIDs unchanged. Deployed. All four songs are now
  arranged.

## Previous pass: an arrangement of Dragonborn-Approved, from its stems (2026-09-24)

Barry: "are you able to break the song down and have a go at it for the entry "dragonborn
approved"".

- **Analysis:**
  - the instrumental stem in 2.5 s steps, level against the song's 90th percentile, in
    three bands: 40-150 Hz (drums), 200-1500 (lute), 1500-5000 (flute)
  - tempo about 146 bpm (bass-band onset autocorrelation)
  - the sung lines from `cues.json`: vocals 0:06 to the end, gaps 2:33.4-2:58.5 (the
    instrumental break) and 4:07.7-4:14.1
- **The shape it shows:** intro; a quiet verse 0:15-0:25 (the drums all but gone); build
  0:27; chorus 0:45-1:12; verse 2 1:15-1:45 (flute low); chorus 1:50-2:17; the drums
  drop out 2:17-2:32; the loud instrumental break 2:32-2:57; a near-silent breakdown
  3:25-3:37; a finale building to the loudest point.
- **Arrangement** (entries 0.1 s early):
  - drums rest in the quiet parts and go fast in the choruses, the break and the finale
  - the lute holds in the quiet verse and the breakdown, and goes fast in the break and
    from 3:55
  - the flute sits out verse 2 and the dropout, and goes fast in the break and at 4:05
  - singers: rest in the intro, the break and before the last shout
  - crowd: claps in the intro, the quiet verse and the breakdown; cheers into the break
    (2:32) and at the end (4:07)
- The built schedule (27 sections) matches an independent merge. Plugin
  `4ae0152ad3d78c45...`, deterministic, FormIDs unchanged. Deployed.
- **To hear it:** `set SkyrimFairAudioFirstTrack to 3`.

## Previous pass: the singers cheer while they sing and clap while they rest (2026-09-24)

Barry: "can we have a civilwarcheer emote for the bards as they're singing?", then "can we
just bind cheer to "sing" clap to "rest"".

- `singerMoves.sing` is IdleCivilWarCheer alone (4.767 s), `rest` is IdleApplaud2/3, and
  the song-end wave moved to its own `end` (script property `SingerEndMove`;
  `SingerCheerMove` retired).
- When the singers' timeline switches (sing <-> rest), each singer starts the new mode's
  move at once instead of finishing the gesture in hand.
- (A separate "cheer" singers option was built and removed again at Barry's word. The
  script's `SingerCheerMoves` stays declared, and unused.)
- Plugin `bdee0e603aabfbe8...`, deterministic, FormIDs unchanged, deployed with the
  scripts.

## Previous pass: Barry's own arrangement of Round the Green (2026-09-24)

- Barry wrote Round the Green's five timelines by hand (lute 19 entries, drum 32, flute 8,
  singers 11, crowd dancing throughout).
- Checked before building: 12 missing commas and 5 trailing commas were fixed; one drum
  time out of order (`127` after `145`) is `147` on Barry's word. Every word is valid and
  every time is inside the song (149.6 s). The file was rewritten in the tool's layout.
- The built plugin's Round the Green schedule (55 sections) matches an independent merge
  of the file exactly. Plugin `0049791a4dbb7da6...`, deterministic, FormIDs unchanged.
  Deployed.
- **To hear it:** `set SkyrimFairAudioFirstTrack to 0` (it's the first song anyway).

## Previous pass: "rest" holds the instrument; "away" puts it away (2026-09-24)

Barry: "is there a way we can use "rest" to just have them idle with the instrument there?
That way they're not putting the instrument away every 30s mid song".

- The instrument levels are now `rest` (0: held, still), `normal` (1), `fast` (2) and
  `away` (3: put away, the old rest).
- `build_tempo.py` also writes `<Instrument>Rest/`: each loop with its first frame (the
  pose it starts from, straight out of taking the instrument up) on every frame, at the
  same length. Read back: largest movement 0.
- The generator writes a `Rest` submod per instrument (`SkyrimFairTempo<Instrument>` ==
  0, interruptible) beside the `Fast` ones.
- The script puts an instrument away only on `away`. Every level change sets the tempo
  global, so OAR swaps in the held, normal or fast loop.
- The seeded `rest` stretches (the drum's calm parts) now hold. The plugin is unchanged
  (`d9a2f942...`): the change is the script and the OAR clips and configs, deployed.
- **To test:** in Fiddle's calm stretches (0:00, 0:37.5), do the drummers stand holding
  their sticks, still, and play again at 0:22.5 and 0:50?

## Previous pass: the singers move: gestures while singing, clapping while resting, a wave at the end (2026-09-24)

Barry: "For the singers, i'd like them to have animations, so they're not just stood still".

- `songs.config.json` `singerMoves` (the same shape as `crowdMoves`) and `singerGap`:
  - **sing:** IdlePointFar_01, IdlePointFar_02, IdlePointClose (2.167 s each),
    LooseDialogueResponsePositive (6.833 s, the longest of its three random clips) and
    IdleCivilWarCheer (4.767 s)
  - **rest:** IdleApplaud2, IdleApplaud3 (clapping along in a `singers` rest stretch)
  - **cheer:** IdleWave (2.033 s), at every song's end with the crowd
  - All are vanilla idles with no conditions. Clip lengths were read from the archive (the
    pointing clips are `mt_pointfar_01/02`, `mt_pointclose`, found through
    `animationsetdata`).
- Why not the talking gestures: `TalkingExpressive` and the `Dialogue*Expressive` idles
  need a dialogue emotion of 50 or more and come from the talking system, not from `Say`.
- Script `SingerGestures(now)`: each singer makes the next move as the last ends plus
  `SingerGap` (2 s) plus 0.4 s per singer, first moves staggered 1.5 / 2.8 / 4.1 s into
  the song. A move that doesn't play is retried a second later. The update wakes for the
  next one.
- Plugin `d9a2f942bf8b4f68...`, deterministic; no records added; every idle's label
  checked against Skyrim.esm. Deployed with the scripts.
- **Not yet:** walking across the deck while singing (a patrol between deck markers,
  planned).
- **To test:** do the singers point, gesture and fist-pump while singing, clap in a rest
  stretch, and wave at the end? Does their lip sync still run (Fiddle)?

## Previous pass: fast instruments: normal and fast tempo, through OAR (2026-09-24)

Barry: "for the instruments, can we have like "normal" "Fast" as well? So it speeds up the
animation of the instruments to look like it matches the tempo of the songs."

- The instrument levels are now `rest | normal | fast` (`play` became normal; the drum's
  seeded `intense` stretches became fast). `songs.config.json` `fast` sets each
  instrument's speed: lute 1.3, drum 1.4, flute 1.3.
- **Clips:** `tools/bards/build_tempo.py` (plain Python, the vendored HKX codec) extracts
  the vanilla loops and writes copies with every frame kept and the frame time, length and
  block length divided by the speed:
  - `animobjectluteloop` 8.767 -> 6.744 s
  - `animobjectdrumloop` 16.667 -> 11.905 s
  - `animobjectflutelong/short` 12 -> 9.231 s
  - Decoded poses match the originals to 0.0005. The loops have no annotations (the
    tool refuses a clip that has any). Deterministic. Git-ignored (derived vanilla),
    shipped in the built mod.
- **OAR:** `SkyrimFairTempo/<Instrument>Fast/config.json`, written by the generator:
  `HasKeyword SkyrimFairNPC` and `CompareValues` (global `SkyrimFairTempo<Instrument>`
  == 2), `interruptible: true` so the switch lands on the section, not at the end of a
  loop. Format read from OAR's source (`SharedTypes.cpp` NumericValue: `"form"`;
  `BaseConditions.h`: `"=="`).
- **Globals** `SkyrimFairTempoLute/Drum/Flute` (`175B`-`175D`, last of all). The script
  (`InstrumentTempo`) holds each at the section's level, resets them to 1 at each song's
  start and when the set stops.
- Plugin `3076e9bbe5949e5e...`, deterministic; only the 3 globals added. Deployed with the
  clips, configs and scripts.
- **To test:** OAR's in-game menu should list "Skyrim Fair tempo". In a fast drum stretch
  (Fiddle 0:22.5), do the drummers play visibly faster, and switch back at 0:37.5?

## Previous pass: a timeline for every part of the band, and the singers (2026-09-24)

Barry: "is it just drums? Can we have the singers, the lute, the flute".

- Each song in `songs.config.json` has `lute`, `drum`, `flute` (`[second, rest | play |
  intense]`), `singers` (`[second, sing | rest]`) and `crowd` timelines. The seed is the
  drum's from the stems; lute and flute play and the singers sing throughout.
- Script: `SectionPlay` (per section, each instrument in `InstrumentIdles` order: lute,
  drum, flute) replaces `SectionDrums`; `SectionSing` makes resting singers skip their
  lines. A musician whose idle is one of `InstrumentIdles` follows that timeline. The old
  `SectionDrums`/`DrumIdle` properties stay declared, unused (saves keep them).
- Plugin `2d5c14bac391a35b...`, deterministic; FormIDs unchanged; 58 sections, 174 play
  levels. Deployed with the scripts.

## Previous pass: one stage-show file, and drums and crowd on timelines of their own (2026-09-24)

Barry: "can we integrate the performers into the songs.config?" and "ideally i want at
second [0] crowd does X, also at second [0] drums start."

- `songs.config.json` now holds the whole show: songs, instruments, band, orchestra and
  crowd moves. `performers.config.json` is gone. Program.cs reads it as `StageShowFile`.
- Each song's `sections` became two timelines, an entry a line where it changes:
  - `drums: [second, "rest" | "play" | "intense"]`
  - `crowd: [second, "dance" | "clap" | "cheer"]`
  - Before its first entry, the drums play and the crowd dances.
  - `FairAudio.SongSections` merges them: a section at every second where either
    changes, carrying the other's last value. The script and its properties are
    unchanged.
- Converted from the old sections (calm -> rest, normal -> play, intense -> intense;
  repeats dropped). **The plugin is byte-identical** (`34f72fd94733297d...`).
- `build_sections.py` seeds and writes the timelines, and keeps the rest of the file (one
  formatter for all of it; round-trip byte-identical).

## Previous pass: the musicians and the crowd's moves in performers.config.json (2026-09-24)

Barry: "could we combine the instrument config with the crowd config?"

- `performers.config.json`:
  - `instruments`: `lute`, `drum`, `flute`, `putAway`, `holdPackage`. A musician's
    `idle` names an instrument; the drummers' rest idle is `drum`, no longer a code
    default.
  - `band` (3) and `orchestra` (9), one musician a line.
  - `crowdMoves`: `dance`, `clap` and `cheer`, each idles, names (labels only) and clip
    lengths. The generator checks the counts match.
- Removed from `fair.config.json`: `audio.stage.band/orchestra/bandStop/bandPackage` and
  `crowds.dance/clap/cheer idles and lengths`; `audio.stage.performersFile` points to the
  new file.
- **The plugin is byte-identical** (`34f72fd94733297d...`). Nothing to deploy.

## Previous pass: the songs in their own file, songs.config.json (2026-09-24)

Barry: "can we do a songs.config.json with just the songs? I'm very lost in the fair.config.json".

- `songs.config.json` at the repo root holds the four stage songs (name, source, file,
  cheer, sections), a section a line, with an "about" note on how to edit them.
- `fair.config.json`'s `audio.stage.songs` is replaced by `"songsFile": "songs.config.json"`.
- Readers:
  - Program.cs loads it into `StageAudioConfig.Songs` (record `SongsFile`)
  - `tools/build_audio.py` follows `songsFile`
  - `tools/bards/build_sections.py` reads and writes it, in the same layout (round-trip
    byte-identical)
- **The plugin is byte-identical** to the deployed `34f72fd94733297d...`, so nothing to
  deploy.

## Previous pass: a solid invisible wall round the stage; the crowd faces the stage to clap and cheer (2026-09-24)

Barry: "the invisible wall stuff still happens... I can just walk up the stairs as well".
"When the crowd clap or cheer, can we get them to look towards the stage?"

- **The CollisionMarker primitives don't stop the player.** On L_TRANSPARENT he walked
  straight through them. Before that, SkyParkour's highest climb is 250
  (`HardcodedVariables.h`), so the 400-high boxes can't have been what he mantled. He
  must have been climbing the stage itself (the deck is 134).
- **Now a real static:** `tools/make_collision_wall.py` writes
  `meshes/SkyrimFair/Collision/StageWall.nif` with PyNifly (Blender's Python). It has no
  geometry, BSXFlags Havok, and a `bhkBoxShape` 256 x 16 x 400, centred. Its 250-byte
  rigid body is the kit's working stair collider's, byte for byte, but for its own shape
  link: layer STATIC, mass 0, fixed motion. Deterministic (`524c2c5f...`), committed
  (project-authored), deployed.
- `collisionWalls[].solid: true` on all 8 stage walls. The 29 pieces keep their FormIDs:
  at the very end (after the navmesh and the culling global) their base becomes the new
  STAT `SkyrimFairStageWall` (`175A`), the primitive is dropped, and they turn -90 degrees
  (the mesh is long on local X). The navmesh and the visibility table are made first, from
  the same boxes, so they're unchanged (7,515 triangles).
- At 400 high, the walls are above SkyParkour's climb limit.
- **Facing the stage:** `FaceStage()` turns every loaded dancer toward the stage speaker
  (`GetHeadingAngle`) in 3 steps 0.06 s apart (`FaceStageSteps`), not a snap, when a
  section switches the crowd to clap or cheer, and at the song-end cheer. Dancers within
  10 degrees stay put.
- Against the deployed plugin: 29 wall pieces change base, 1 STAT added, nothing else.
  Plugin `34f72fd94733297d...`, deterministic, deployed with the mesh and scripts.
- **To test:** walk, jump and vault at the steps, sides and back. Clap and cheer
  sections: does everyone turn to the stage smoothly?

## Previous pass: the stage show, B3 and B4: drums and crowd follow each song's sections (2026-09-24)

Barry chose per-song sections, set in the config.

- **Sections** (`audio.stage.songs[].sections`, each `[start s, drums, crowd]`):
  - drums: `calm` (the drummers rest), `normal` or `intense` (they play)
  - crowd: `dance`, `clap` or `cheer`
  - Seeded by a new tool, `tools/bards/build_sections.py` (Blender's Python, ffmpeg).
    It reads the drums' intensity from each song's instrumental stem with
    `build_vocals.intensity` (2.5 s steps, blips under 5 s merged) and makes no singer
    lines. All four songs have stems now.
  - The seed claps where the drums are calm and dances elsewhere: Round the Green 13
    sections (5 calm), Hey-Ho 14 (3), Fiddle 16 (8), Dragonborn-Approved 15 (3). Fiddle's
    seed matches its `cues.json`.
  - **Tune by hand.** `--write` only fills songs that have none; `--force` reseeds.
- **Script** (`SkyrimFairAudioScript`, new properties, so old saves get them):
  - `Sections(now)` applies each section whose start has come, timed from the song's own
    start, and the update wakes for the next one.
  - Drums calm: every bard whose idle is `DrumIdle` (vanilla `IdleDrumStart`) is given
    `IdleStop` and skipped by `PlayAll` until the drums return.
  - A crowd change clears each dancer's clip, so they switch at once. `Dance()` picks the
    mode's idles:
    - dance: `DanceIdles`
    - clap: `ClapIdles`, Applaud 2-5, 7.333 / 7.133 / 9.0 / 6.333 s
    - cheer: `CheerIdles`, Applaud 2, Civil War cheer, Applaud 3, 7.333 / 4.767 / 7.133 s
  - Each clip is replayed as it ends, as the dances are. Lengths read from the archive
    with the vendored HKX codec (it reads the Cicero dance as 6.667, which matches).
  - Traces: `SkyrimFairAudio: drums N at X s`, `crowd N at X s`.
- Plugin `4f698904e36a7f52...`, deterministic; all 3,892 records keep FormID identity;
  58 sections read back. Deployed with the scripts.
- **Next (B3b):** faster, livelier instrument loops in the intense sections (retimed
  copies of the vanilla loops through OAR). Not started.
- **To test:** watch a song through. Do the drummers stop and restart with the drums? Do
  the dancers switch to clapping in the calm parts? Does anything restart visibly
  mid-clip?

## Previous pass: the stage show, B2: the stage walls can't be vaulted (2026-09-24)

Barry could get onto the stage "from vaulting on the walls round the back and sides, and
jumping on the stairs from a different angle".

- **The cause: SkyParkour** (in the modlist). Its ledge check climbs onto any collision
  layer except NonCollidable, CharController, Weapon, Projectile, Transparent, Clutter,
  Biped, ActorZone and DebrisLarge (`include/_References/ExclusionLists.h` in
  github.com/Tsptds/skyrim-SkyParkourNG). The stage's 29 invisible boxes had no layer
  set, which is CollisionBox: a 400-high ledge to mantle onto and drop over.
- **Fix:** `collisionWalls[].layer: 3`, **L_TRANSPARENT**. Read from Skyrim.esm's COLL
  records, it collides with the character controller and bipeds (player and NPCs) but not
  projectiles. Vanilla uses it on 22 CollisionMarker boxes, and SkyParkour excludes it
  from climbing. Only the boxes' `XCZC` layer changes.
- Plugin `27bae193c1a05128...`, deterministic; all 3,892 records keep FormID identity;
  navmesh unchanged (9 meshes, 7,515 triangles). Deployed.
- **To test:** try to vault the walls at the back and sides, and jump at the steps from
  the side. Do the band and singers still stay in?

## Previous pass: the stage show, B1: the cheer starts as the last note rings (2026-09-24)

- **Before:** the cheer waited for the song's full measured length on the update timer
  (late when Papyrus is busy), then stopped the song. The files end within 0.06-0.52 s of
  their last sound, so the gap was the script's.
- **Now:** `CheerLead` (`audio.stage.cheerLead`, 1.0 s, a new property, so old saves
  get it) starts the cheer that long before the song's end. The song isn't stopped: it
  finishes under the cheer, its instance kept for `StopAll`. The band plays until the
  song really ends (`bandUntil`, woken for), then puts the instruments away. A song with
  no cheer pauses `lead + PauseAfterCheer`, as before.
- **Trace:** `SkyrimFairAudio: cheer N at X s into song T (L s long, lead 1)`. With
  that, the cheer's real timing can be read from `Papyrus.0.log`.
- Plugin `039172b91f95a5c9...`, deterministic; all 3,892 records unchanged; deployed with
  the scripts.
- **To test:** listen to a song's end. If the cheer now tramples the last note, lower
  `cheerLead`; if there's still a gap, raise it.

## Previous pass: a varied crowd: 390 faces instead of 12 bandits (2026-09-24)

Barry: "I keep seeing a lot of the same people. Especially the women with the tattoo
faces."

- **The cause:** visitors, stall-keepers, archers, the band and the folk pair take their
  looks (Traits) from the fair's copies of vanilla `0001A319`/`0001A31E`. Those are
  `LCharBanditMeleeCommonerM/F`: **six Imperial bandits a sex**, warpaint and all. The
  whole fair had 12 faces.
- **The pool** (`fairWorld.faces`, `FairFaces.cs`), from Skyrim.esm:
  - kept: generic NPCs (not unique, so no clones of named characters) of every playable
    race with their own face
  - filtered out: warpaint over 0.05; dirt over 0.4; names or EditorIDs with ghost,
    corpse, phantom, Sovngarde and so on; test and summon records
  - checked: both FaceGen files present in the archives (none renders dark)
  - scars allowed: they're common on vanilla townsfolk; warpaint is what reads as a
    bandit
  - dropped for women / men: 357 / 606 unique, 98 / 138 warpaint, 124 / 268 dirt
- **The lists** keep their records (FormIDs unchanged; renamed `SkyrimFairFacesMale`/
  `Female`) and are refilled with up to 250 entries by race weight:
  - Nord 40, Imperial 14, Breton 12, Redguard 10, Dunmer 8, Altmer 5, Orc 4, Bosmer 3,
    Khajiit 2, Argonian 2
  - spread through each race's faces, and no face more than twice (`maxRepeat`), so a
    race with few faces gets fewer entries, not look-alikes
  - **245 different men's faces and 145 women's** (there are fewer clean vanilla women)
- **Fixed faces** (`faces.fixed`) for records whose animations want a human skeleton: the
  12 musicians (Nord, Imperial, Breton, Redguard, Dunmer, Bosmer faces) and the folk pair
  (Imperials of height 1.0, as before, so the paired clip still meets). Only their
  `Template` changes.
- Plugin `eb0bdc6052f5d707...`, deterministic, deployed. Against the deployed plugin: all
  3,892 records keep their FormIDs; the only changes are the two lists' entries and names,
  and 14 NPC templates.
- **To see it: start clean** (`cow SkyrimFairWorld 0 0` from the main menu). A templated
  NPC's face is rolled when its runtime copy is made, and a save keeps it.
- **Confirmed by Barry:** "yup it looks good!!!"

## Previous pass: crowd culling margin 768 -> 512 (2026-09-24)

Barry saw no pop-in at 768 and asked for the smaller margin ("even if i get some pop in,
it's not the end of the world").

- `crowdCulling.margin` 512. On at a spot (of 173): square 144 -> 129, market east lane
  147 -> 119, avenue 154 -> 143, archery field 153 -> 148, gate 129 -> 117; mean 135 -> 120.
- Only the table's values change: against the deployed plugin all 3,892 records keep
  FormID and content identity, none added. Readback as before: no negative word, every
  actor on at its own square, retired untouched.
- Plugin `4dae188cafc0412f...`, deterministic, deployed.
- **Confirmed by Barry:** "that works wonderfully". Pop-in can be seen in the distance
  when looking for it, "i don't think it's noticable if you're not trying to look for
  it". Keep 512; 640 is the fallback if that changes.

## Previous pass: the crowd switched off where it can't be seen (built, deployed, 2026-09-24)

Barry's baseline, with frame generation on (it can't be turned off in his pack), at the
square:

| Test | Displayed fps |
|---|---|
| everything on | 77-96 |
| `SkyrimFairCrowdLayers 0` (93 fewer actors) | 120 (possibly a cap) |
| `tai` (no AI) | ~102 |

Removing actors outright gained more than stopping all AI, so most of the cost is drawing
and animating them. The fair has no occlusion, so actors behind stalls are drawn too.
Barry: "let's implement it".

- **Generator** (`FairVisibility.cs`, `fairWorld.crowdCulling`), after the navmesh:
  - every placed object (1,745 with footprints) is voxelised as the navmesh cuts it
  - 622 standing spots on a 256 grid; rays from eyes at 150 and 210 to each actor at 40,
    100 and 160
  - each grid square's entry is every actor seen from any standing spot within 768: 29 x
    33 squares x 6 words = 5,742 numbers, 31 actors a word
- **Which actors:** 173 switchable. Always on: the band, the singers, the archers, the folk
  pair (`alwaysOn`) and the horses (not the fair's records). The 36 retired visitors
  (placed disabled) are left out. *The earlier estimate counted those 36 as savings, so
  it was too high.*
- **The switchable actors** are made persistent (the script names them) and lose their
  crowd layer's enable parent (a reference with one can't be enabled by script). The new
  script applies the layers itself: an actor is on if it's seen **and** its layer is on,
  so `SkyrimFairCrowdLayers` works as before. The old layer markers stay, now empty.
- **Script** `SkyrimFairCrowdCull`, a second script on the stage quest, with its own
  0.5 s poll. When the player's square changes, it enables or disables only the actors
  whose bit changed (`EnableNoWait`/`DisableNoWait`). Papyrus has no bitwise operators,
  so bits are read by division, 31 to a word so words stay positive. The alias calls
  `Resync()` on every load. If the table loads short (a property-array limit), it leaves
  everyone on and traces why.
- **Switch:** `set SkyrimFairCrowdCulling to 0` turns every actor back on.
- **Expected** (of 173 switchable): square 144 on (29 off), market east lane 147, avenue
  154, archery field 153, gate 129; mean 135. A smaller margin saves more (512: square
  129, market 119) but risks pop-in at a sprint (0.5 s poll, a 256 square, the 3D load).
  768 is the default.

### Verification

- Five scripts compile. Generator deterministic: `8a5b898f2d0f0752...`, run twice.
- Against the deployed plugin (`e3e982d8`): all 3,891 records keep FormID, type, EditorID
  and base; **1 added**, the global `SkyrimFairCrowdCulling` (`1759`, after the navmesh).
- Read back:
  - 173 actors, 173 layers, table 5,742 = 29 x 33 x 6, no negative word
  - every switchable actor persistent, none with an enable parent
  - every actor on at its own square
  - the 36 retired untouched and not in the table
- Deployed: plugin and scripts, byte-checked.
- **Not verified in game:** the property arrays loading whole (over 128 entries), the
  frame rate, pop-in, and seated visitors re-sitting after being switched back on.

### In game (Barry, 2026-09-24)

- **"a huge boost in fps"**: 90-120 displayed (frame generation on), from 77-96 before.
  120 may be a cap. About 130 when not looking at the dance floor.
- **No pop-in seen** walking and sprinting round the fair: the 768 margin holds.
- Barry runs DLSS with frame generation too, so these are displayed rates on a
  GPU-assisted setup; the culling's saving is on the actors' side, which DLSS doesn't
  touch.
- Not yet reported: seated visitors re-sitting. The dance floor, always in view from the
  square, is now the heaviest thing on screen.

### What Barry should test

1. From a clean start (`cow SkyrimFairWorld 0 0` from the main menu, since the actors'
   enable parents changed): `Papyrus.0.log` should show `SkyrimFairCrowdCull: 173 actors,
   table 5742 of 5742, ready True`.
2. The same three readings at the square, plus one with `set SkyrimFairCrowdCulling to 0`
   (culling off) to compare.
3. Walk and sprint around the market, the avenue and the archery field: does anyone pop
   in or out in view? Do seated visitors sit when you reach them?
4. Does `set SkyrimFairCrowdLayers to 0` still clear the layers?

## Previous pass: can the crowd be switched off where it can't be seen? (analysis, 2026-09-24)

Barry confirmed the palisade, the lanterns and the stage in game. Performance is
"stable", but with a frame-generation mod on. He asked for the optimisation (plan step 3).
Nothing was built; this is the measurement the plan asked for first.

- **The actors (read from the plugin), 233 in all:**

  | Area | Actors | Of which |
  |---|---|---|
  | Square and stage | 90 | 43 dancers, 20 standing visitors, 15 performers, 8 seated |
  | East market | 86 | 59 stall-keepers, 13 standing, 11 seated |
  | Avenue | 35 | 20 standing, 11 seated |
  | West field | 22 | 8 archery spectators, 4 archers, 3 horses |

  24 stay on regardless (performers, archers, folk pair, horses: script-driven and
  confirmed), leaving 209 that could be switched.
- **Method** (throwaway, in the scratchpad):
  - every placed object voxelised from its navmesh footprint (16-unit cells, 16-unit
    height bands to 448)
  - from 610 standing spots (256 grid, eye 150), rays to each actor at 60 and 150
  - an actor counts as seen if any ray is clear
- **Results** (switchable actors on, at a typical spot, of 209):

  | Scheme | Mean | p90 | Pops in view |
  |---|---|---|---|
  | perfect per-actor visibility, no margin | 70 | 118 | yes, while moving |
  | per-actor, 768 margin (one 2 s update at a sprint) | 138 | 164 | no |
  | 10 zones, 768 margin | 200 | 209 | no |
  | 24 zones, 768 margin | 177 | 199 | no |
  | distance only, 3,000 | 94 | 147 | ~25 visible actors vanish per spot |

- **Conclusion:** the zone plan as written saves under 10%. The fair is open and the
  stage is in view the whole length of the avenue. Without visible popping, the ceiling
  is about a third of the actors, and only with per-actor switching (209 markers and a
  generated visibility table). Whether that's worth building depends on what the cost is.
  Plan step 1's three readings (layers on / `SkyrimFairCrowdLayers 0` / `tai`, frame
  generation off) decide it.

## Previous pass: a taller palisade and gate, stage lanterns and flags, packing gear, parked wagons, more SPID exclusions (2026-09-24)

First, the folk-dance fix from last night (`e166ad79...`) is **deployed**: the plugin and both
OAR `config.json` files, byte-checked.

### The palisade: tall enough not to see over

Barry: "we just need it tall enough to not be able to see over them".

- **Measured, not guessed.** A throwaway Mutagen dump of the built LAND (121 cells) and
  the placed panels, then sightlines from 1,280 interior points (192 grid, 96+ from the
  wall, eye 125). Only cells within two of the eye's cell count (no LOD), and the gate
  opening doesn't block. The wall height that hides the ground out to a given distance past
  the wall:

  | Hidden to | Wall needed | Ground there rises to |
  |---|---|---|
  | 2,500 | 140 | 144 |
  | 3,500 | 290 | 360 |
  | 4,500 | 441 | 632 |
  | 6,000 | 622 | 1,072 |
  | everything drawn | 780 (819 at eye 200) | ~1,500 |

- **Barry chose scale 4**: panels 556 wide x 560 tall (were 347 x 350). The gate is 3.6:
  628 tall and 653 wide, still about 70 over the wall as before.
- **Last night's plan was changed:** keeping the 2.5 spacing and scaling only the look would
  overlap neighbours by about 40%, and flat panels overlapping that much z-fight. Instead
  the panels are spaced by their real width (6% overlap as before), so there are 59, and
  `palisade.reservedPanels: 89` keeps the other 30 FormIDs (`B5A`-`B77`) unused. The
  generator throws if a wall ever needs more than it reserves.
- **Verified** (the wall-closure method from the worldspace pass, rerun):
  - outline: 3,218 points every 8 units, all inside a panel or the gate (worst 14.2 inside)
  - 20,160 sightline rays from seven interior points: 0 slip through
  - no panel footprint inside a zone; the gate's inner face meets the Entrance zone, by
    design, as before
  - crests 523 to 571 (sink and scale jitter); the lowest still hides the ground to about
    5,200 past the wall
- `navmesh.wallMargin` 48 -> 56: the panel is 44 deep now (was 28), so the walkable edge
  keeps its old clearance from the thicker wall.

### Late-built dressing (`market.lateDressing`)

A new list, placed after every other record but the navmesh, so nothing before it
renumbers. `FairMarket.Build` hands back its dressing placer (`MarketResult.PlaceLate`):
the same fit checks and spiral search as `market.dressing`, plus a 40 clearance from every
placed NPC (the crowds are already down by then). `force: true` places a group exactly with
no checks (the stage roof). Seeds are `1800 + index`, so the list is append-only.

- **Stage lanterns** (`stage_lanterns`, at the stage origin (2048, 5544), yaw 180, which
  is the stage's own u/v frame): 18 Holidays swinging lanterns (`035D0D`,
  `035D12`-`035D16`), cycling colours, scale 1.5, three under each even rafter
  (v -360/0/360) and two under each odd one (v -180/180). They are `MSTT`s with **no
  light**, so the real-light count doesn't change. Each lantern's height was set from the
  built rafters: every rafter log's height jitters by up to 5, so each lantern's top
  (+6 x scale above its origin) is now exactly **3 into the log above it** (read back, all
  18).
- **Whiterun flags** (`stage_flags`): three `CivilWarBanner01` poles with
  `CityBannerWhiterun01`, as `banner_whiterun` hangs them, behind the back posts at
  v -560 (y 6104), one in each back bay (u -602 / 0 / 602). Crossbars run along world X,
  so they face the square, as the avenue's banners face the avenue. From the square they
  show between the deck (134) and the back beam (380).
- **Packing gear by the range** (`range_packing`, at (-950, 1750), north of
  `range_storage`): an open chest (vanilla `ChestOpen`), a long crate carrying a bundle of
  five iron arrows, a small crate, two sacks, a satchel and a laid-out bedroll. There's no
  vanilla static closed chest or sack; the sacks are the existing props. New props would
  have renumbered records, because prop STATs are made early, sorted by name.
- **Parked wagons** (`wagon_parked`): vanilla `CartFurnStatic01` (`104F6F`), the empty
  Helgen horse cart (180 x 553, shaft resting on the ground), with an optional sack and
  crate. Four placed, each lengthwise along the free space: gate forecourt west (650,
  -1250) and east (3500, -1300), by the north-east camps (5150, 4450), and by the
  stables (750, -250).
  - **Not placed by the south-east camps:** between the tents, East Wall Walk's end and
    the wall corner there's no pocket 580 long (checked on a map drawn from the plugin).
  - `VendorCartStatic01` was rejected: its shafts float level.
- **Navmesh fix:** `CartFurnStatic01` has an all-zero OBND, and the navmesh skipped
  anything under 12 in its bounds before looking up its model's footprint, so the wagons
  were walkable. Now a *static* with unset bounds is cut by its model footprint (and
  listed as unknown if there isn't one). Moveable statics, such as the banner cloth, which
  is also zero, are unchanged.

### SPID exclusions: three more lines

`spidPatches` now also rebuilds `StealthKillDetectionFix_DISTR.ini` (`0x80B`,
`madStealthKillFixSpellSleep`), `StealthKillDetectionFix_Killmove_DISTR.ini` (`0x819`,
`madStealthKillFixSpellKillmove`) and `StrangeRunes_DISTR.ini` (`0x68855`,
`po3_RUNE_DetectCastNPCAbility`). The records were read from each mod's ESP. Skyrim Fair is
first in MO2's priority, so the copies win.

- **Nothing else per-NPC in last night's `Papyrus.0.log` (22:44):**
  - Footprints switched to its SKSE mode, so it runs no Papyrus per NPC.
  - Conditional Expressions has no `_DISTR.ini`; its 13 lines are one `[None]` effect
    ending.
- The log's bulk (27,750 of 141,553 lines) is the vanilla Dwemer thresher trap `078307`
  firing without 3D, outside the fair. It isn't ours, but it's script load during the fps
  baseline.
- As before, SPID additions stay in a save: test from a clean state.

### Verification

- Release build: 0 warnings. Generator run twice: identical SHA256 `e3e982d8c3f76baa...`
  (767,789 bytes). Deployed; byte-identical in the mod folder, plus the three new
  `_DISTR.ini` files.
- **FormIDs against last night's plugin:** all 3,836 earlier records keep their FormID,
  type, EditorID and base. The only differences:
  - the 30 reserved panel IDs, now unused
  - the navmesh, which now comes after the 38 new dressing references (`1722`-`174F`),
    as it always follows appended records
- Navmesh: 9 meshes, 7,515 triangles; all 233 actors on the mesh; footprints refreshed
  (`tools/make_footprints.py`: chest, bedroll, small sack, cart).
- `docs/STALLS.md` unchanged: 33 stalls.
- **Not verified in game:** everything above.

### What Barry should test in game

1. From the square, the market and the archery field: can you see ground over the wall?
   (Mountains, ridges and treetops should still show.) Check the corners and the gate
   join.
2. Does the thicker wall look right up close (its logs are 1.6x as thick)?
3. The stage: lanterns hanging from the rafters (not floating, not sunk), their size, and
   the three flags at the back.
4. The packing gear by the range, and the four wagons: nobody walks through them.
5. Performance baseline (the plan's step 1), from a clean save.

## Previous pass: why Astra's folk dance never played (deployed 2026-09-24) (2026-09-23)

- OAR's condition was `IsActorBase` on the folk dancers' records. They're templated, so in
  game they run on runtime copies (`FF0021C1`, `FF0012B0` in SPID's log), and OAR never
  matched.
- Now each dancer has its own keyword (`SkyrimFairFolkDancerMale`/`Female`), built last:
  against the deployed plugin, only those 2 records are added, plus the navmeshes. OAR's
  condition is `HasKeyword`, in EVG Conditional Idles' format.
- Plugin `e166ad798013ffe2...`, deterministic. **Not deployed:** `SkyrimFair.esp` was locked
  (the game or MO2), and Barry is out of time. Deploy first thing tomorrow. Barry's other
  requests (a dance-mod audit, crowd timing) are in `CLAUDE.md`'s plan.

## Previous pass: the static crowd figures switched off (2026-09-23)

Barry: reloading from an earlier save fixed the crashing. He wants the object NPCs gone:
"there's glowing issues with them which i'm just struggling to resolve".

- `fairWorld.crowdFiguresEnabled: false`: no figure is placed, and the benches they'd sit
  on stay sittable. The definitions, placements, library and tools all stay, for later.
- Against the deployed plugin: exactly the figures' 131 records are gone (36 STATs, 51
  figures, 44 collision boxes), all in their own FormID range. **Nothing else changed or
  renumbered.**
- Navmesh: 9 meshes, 7,325 triangles, 0 errors; it opens the ground the figures stood
  on.
- Plugin `497f5fd2d8ad0198...`, deterministic. Deployed. The NIFs stay in the mod folder,
  unused.

## Previous pass: crowd NIFs, fifth rebuild, redeployed (2026-09-23)

- The other agent rebuilt the crowd NIFs: hair is coloured through vertex colours
  instead of the hair-tint shader, which doesn't work on statics (`docs/CROWD.md`,
  "Fifth test").
- All 38 are in the mod folder and match byte for byte.
- Bounds confirmed unchanged against `figures.json`, so the plugin is unchanged
  (`536becda924886e6...`).

## Previous pass: the freeze is the save's; dances replay at their end; a first-song switch (2026-09-23)

Barry: (1) still crashing; (2) "the npc's do one dance loop then stop"; (3) the lip sync
doesn't work; (4) the static bodies don't work.

### 1. The freeze: the exclusion works, the save carries the old spells

- **SPID's log for this session:** no fair NPC got `madDetectionCloak` or
  `MD_GoreHumanoidMagic`. Only the three pen horses got the cloak. So the patched inis
  are read, and `-SkyrimFairNPC` matches.
- **The dump still floods** (755,395). The actors running the Maximum Destruction script,
  and casting the cloak, are the same fair NPCs as before (`250013C9`, `25001427`, ...).
  - The spells SPID gave them in earlier sessions are **kept in the save**, most likely
    on the templated NPCs' runtime `FF` records, which saves store.
  - SPID only adds; it never removes.
- **Test with a clean world:** from the main menu, `cow SkyrimFairWorld 0 0`, or a save
  from before the first visit to the fair.
- A save that has already visited keeps the spells until the fair's cells reset.

### 2. Dances play once: now replayed as each ends

- The clips' lengths, read from the vanilla archive: `special_cicerodance1` 6.667 s,
  `2` 6.0 s, `3` 2.333 s. The third is dropped: too short to repeat without twitching.
- The stage script gives each dancer the next dance the moment the last one ends
  (`DanceLengths`), and wakes exactly then. No clip is ever re-sent mid-way, which was
  the earlier jank.
- **The folk pair:** `rebase_folk.py --repeat 6` chains Astra's seamless loop six times
  into one 57.6 s clip, restarted together as it ends. That's once a minute instead of
  every 9.6 s. Round trip 0.0005; deterministic.
  - The length is a new property, `FolkClipLength`: a save keeps `FolkLength`'s old 9.6.

### 3. Lip sync: never reached

- Only **song 0** has ever played in any log (19:01, 19:20, 19:30), and it's Round the
  Green, with no singer lines.
- Fiddle and Dragonborn-Approved are songs 2 and 3, about six minutes in. The flood froze
  things first.
- **New test switch:** `SkyrimFairAudioFirstTrack` (default −1).
  `set SkyrimFairAudioFirstTrack to 2` starts with Fiddle on the next arrival or load.

### 4. Static figures

- The deployed plugin has all 51 references of 36 figures, with the right models and
  bounds. What "not working" looks like in game is still to hear from Barry.

- Plugin `536becda924886e6...`, deterministic. Four scripts compile. Deployed.

## Previous pass: crowd NIFs, fourth rebuild, redeployed (2026-09-23)

- The other agent rebuilt the crowd NIFs: hair shine and the dynamic-decal flags are off,
  for the white hair streaks (`docs/CROWD.md`, "Fourth test").
- All 38 were copied to the mod folder and match byte for byte.
- Bounds confirmed unchanged against `figures.json`. The plugin is unchanged
  (`f8781aeca5637ad5...`).

## Previous pass: the SPID exclusion as a generated patch (2026-09-23)

Barry: "is it possible to make it a patch instead? Just incase the mods need updating in
future".

- **`FairSpidPatches.cs`, `spidPatches`:** on every build the generator reads each mod's
  *current* `_DISTR.ini`, adds `-SkyrimFairNPC` to the named spells' lines, and writes a
  copy under the same filename to `dist/spid/`.
  - `NONE` becomes the exclusion; an existing filter list gets `,-SkyrimFairNPC`.
  - It keeps the byte-order mark and the line endings.
  - Both copies are byte for byte the mod's file apart from the exclusion.
  - If a named line is missing, the build fails, so a mod update can't leave a stale
    copy.
- **Deployed to the root of the Skyrim Fair mod folder.** In Barry's `modlist.txt`, Skyrim
  Fair is line 2, above Maximum Destruction (42) and Stealth Detection Fixes (594), so
  its copies win the conflict. **The mods' own files are never edited.**
- **After a mod update:** rebuild and deploy; the patch is regenerated from the new file.
- **Lines patched:**
  - `StealthKillDetectionFix_Attack_DISTR.ini`, `0x817` (`madDetectionCloak`)
  - `MaximumDestruction_DISTR.ini`, `0x8E6289` (`MD_GoreHumanoidMagic`)
- **The no-op ESP condition patches are gone** (`FairPatches.cs`, `compatPatches`).
- Plugin unchanged: `f8781aeca5637ad5...`. Patches deterministic.

### Test

1. Load at the fair. Does the game stay up past two minutes, with no "Suspended stack
   count" in `Papyrus.0.log`?
2. `SKSE/po3_SpellPerkItemDistributor.log`:
   - "Fair Visitor", "Fair Bard", "Singer" and the other fair NPCs should list neither
     `madDetectionCloak` nor `MD_GoreHumanoidMagic`.
   - NPCs elsewhere should still get both.
3. MO2 shows Skyrim Fair overriding those two ini files. That's intended.

## Previous pass: crowd NIFs, third rebuild, redeployed (2026-09-23)

- The other agent rebuilt the crowd NIFs: heads are now written without vertex normals,
  as vanilla's are, fixing the white heads and the camera-following flare
  (`docs/CROWD.md`, "Third test").
- All 38 are in the mod folder and match byte for byte; 24 had changed.
- `figures.json`: no figure's bounds changed. Footprints regenerated, also unchanged.
- The plugin is unchanged: `f8781aeca5637ad5...`, deterministic.
- The SPID exclusion edits for Maximum Destruction and Stealth Detection Fixes still wait
  on Barry's go-ahead (previous pass).

## Previous pass: the freeze needs SPID exclusions; the compatibility patches can't do it (2026-09-23)

Barry: "it will crash out again". There's no new crash log after the 18:55 one: that
launch loaded a save and ran, so **the startup crash is fixed**. But `Papyrus.0.log`
(214 MB) shows the flood again.

- **The patches have no effect.**
  - Before them, the load at the fair froze about 66 s in. With them it froze 64 s in
    (19:01:28 load, 19:02:32 dump).
  - The flooding effect is still `FE0B3811`, Stealth Detection Fixes' dummy.
  - Neither the worldspace condition nor the global stops the cloak casting: a Cloak
    effect keeps applying its spell whatever its ability's conditions say.
  - The patches are removed from the config, `dist/` and the mod folder. They did
    nothing, and their docs claimed they would.
- **What SPID's log shows** (`SKSE/po3_SpellPerkItemDistributor.log`):
  - It distributes when each actor loads, and names every spell it gives.
  - Every fair NPC gets `madDetectionCloak` (`817`, from
    `StealthKillDetectionFix_Attack_DISTR.ini`) and `MD_GoreHumanoidMagic` (`8E6289`).
  - The pen horses get the cloak too.
  - Most fair NPCs are **runtime copies** (`NPC_:FF001272` "Fair Visitor"), made from
    their face template. So a filter on `SkyrimFair.esp` would miss them: my earlier
    `-SkyrimFair.esp` suggestion was wrong.
- **Fix, part 1 (built):** a keyword, `SkyrimFairNPC`, on all 114 fair NPC records,
  built last (only the navmeshes renumbered). Templated NPCs' runtime copies keep their
  record's keywords.
- **Fix, part 2 (needs Barry's go-ahead, since it edits other mods' files):** add
  `-SkyrimFairNPC` to two SPID lines:
  - `StealthKillDetectionFix_Attack_DISTR.ini`:
    `Spell = 0x817~StealthKillDetectionFix.esp|-SkyrimFairNPC|NONE|NONE|NONE|NONE|NONE`
  - `MaximumDestruction_DISTR.ini`:
    `Spell = 0x8E6289~MaximumDestruction.esp|ActorTypeNPC,Charmed Vigilant,Spellsword,Arch-Curate Vyrthur,Estormo,-SkyrimFairNPC|NONE|NONE|NONE|NONE|100`
- **Check, after:** SPID's log should list neither spell for "Fair Visitor", "Fair
  Bard", "Singer" and the others. And a load at the fair should outlast two minutes.
- Plugin `f8781aeca5637ad5...`, deterministic. Deployed.

## Previous pass: fixing the singers' startup crash (2026-09-23)

Barry: a startup crash, 30 s after launch.
- CrashLogger: `EXCEPTION_ACCESS_VIOLATION` at `SkyrimSE.exe+03D3E15` during init.
- In the registers: our quest `SkyrimFairSingers` ("Stage singers", `250016BA`), and one
  of its topics (`2500171B`), whose name reads as garbage memory.

- **Cause (found in the raw bytes):** our 49 song topics were written with
  `DATA 00000000` (category Topic, subtype Custom) and `SNAM 00000000`, an empty
  subtype code.
  - In `Skyrim.esm`, **all 6,503 Topic-category DIALs have a branch (`BNAM`)**. Ours had
    none.
  - BardSongs' song topic is `DATA 00 07 5400`, `SNAM "IDAT"`: category Misc, subtype
    `0x54`, no branch.
- **Fix:** the song topics are now exactly BardSongs' kind: Misc, `0x54`, `SNAM IDAT`.
  All 49 match its `DATA`/`SNAM` bytes.
  - That subtype is also used for idle chatter, and BardSongs guards its lines with
    conditions. So each of our INFOs has **`GetIsVoiceType` in
    `SkyrimFairSingerVoices`** (a new FormList of the three singer voice types). Only
    the singers can say them.
  - The new FormList shifted the INFO IDs by one, so the voice files were renamed to
    match. The old deployed ones were cleared first; 147 of 147 are present.
- Plugin deterministic: `57fb5199cd343a9b...`. Deployed.
- **Not confirmed:** that this is the only startup problem. If it still crashes, the
  crash log will say whether it's the same place. `fairWorld.singers.enabled: false`
  builds the fair without the singers, to isolate it.

## Previous pass: the stage singers, and the crowd NIFs' second rebuild (2026-09-23)

Barry: (1) redeploy the rebuilt crowd NIFs (zero-length skin normals fixed; the tankard
held) and refresh the prop figures' bounds. (2) Implement the singers from
`docs/BARDS.md`, "Generator side".

### 1. Crowd NIFs

- All 38 redeployed, matching byte for byte.
- Bounds refreshed from `figures.json`: Tankard01–03 are 75 wide (were 70), Toast01–02
  are 75 deep (were 76).
- Footprints regenerated.

### 2. The singers

**The voice-file naming rule, checked against vanilla first** (Mutagen over `Skyrim.esm`,
against the 61,811 `.fuz` names in `Skyrim - Voices_en0.bsa`):
- `<quest EditorID>_<topic EditorID>_<INFO FormID, 8 hex, lowercase>_<response number>.fuz`
  under `Sound\Voice\<plugin>\<voice type EditorID>\`.
- **Short names:** 2,995 of 3,004 sampled lines are found by exactly that name. The 9
  misses are lines with no recording.
- **Long names:** when quest plus topic are over 25 characters, the quest is cut to 10
  and the topic to 15. That's the best of the rules tried: 3,071 of 4,001, and every
  other variant scored lower or 0.
- **BardSongs:** the song lines' topics have no EditorID (`bardsongs__00074773_1`).
  35 of its 64 responses are found under the rule. The other 29 weren't checked
  (probably the request-branch lines, which have no recording).
- **So:** a new quest `SkyrimFairSingers` (17 characters) with **unnamed topics**, as
  BardSongs has. It's under 25 characters, so it's never truncated. Our files are
  `skyrimfairsingers__<INFO id>_1.fuz`.

**Built** (`FairSingers.cs`, `fairWorld.singers`, late, before the navmesh):
- **Three voice types**: `SkyrimFairSingerLead`, `Left` and `Right`, with no flags, so no
  generic dialogue.
- **Three singers, "Singer"**, men, at the deck front: lead at (2048, 5140), left at
  (1848, 5160), right at (2248, 5160), z 134, facing the square.
  - Persistent, invulnerable, `DefaultStayAtEditorLocation`, in bard's clothes.
  - **Not Traits-templated.** Traits carries the voice type, so a templated singer would
    use the template's voice and never find our files.
  - Each is a new NPC with a vanilla NPC's face copied field by field: `039CF6`,
    `039CFF` and `039D17`, from the fair's male face list. That's head parts, morphs,
    face parts, tints, hair colour, head texture and texture lighting.
  - That NPC's FaceGen head (`Skyrim - Meshes0.bsa`) and tint (`Skyrim - Textures1.bsa`,
    which is in Barry's Vanilla Remastered mod) are copied under the singer's FormID.
    They're read with Mutagen's archive reader from `singers.faceArchives`.
- **One topic per sung line**, 49 in all (Fiddle 19, Dragonborn-Approved 30):
  - Topic / Custom, priority 50, one INFO each.
  - One response: Happy 50, "use emotion animation", number 1, **no text** (so no
    subtitle).
- **Voice files:** 147 (49 × 3) copied from `build/bards/<song>/voice/<singer>/` to the
  engine's names under `assets/sound/Voice/SkyrimFair.esp/<voice type>/`. They're
  deployed to `Sound\Voice`.
  - The folder is cleared first, so a renumbered INFO leaves no stale file.
  - All three singers' files are lip-only, with silent audio, as `docs/BARDS.md` says.
    The speaker keeps the full mix.
- **Compared subrecord by subrecord with vanilla** (BardSongs' DIAL and INFO, a quest
  with no aliases, a vanilla NPC):
  - The DIAL matches.
  - The INFO needed `CNAM` (the favor level) set.
  - The quest needed `ANAM` (next alias ID): all 204 vanilla quests without aliases
    have it.
  - The NPC needed `DNAM` (skills): all 5,118 vanilla NPCs have it.
  - All three are now set explicitly. The NPC's face subrecords match the vanilla
    NPC's.
- **The stage script:**
  - On a song start it looks up that song's lines (`SongFirstLine`, `SongLineCount`).
  - Each update, every line whose start has come (`SingerStarts`, seconds from the
    song's own start, so timer error never adds up) is said by all three
    (`Say(SingerTopics[i])`).
  - It wakes exactly at the next line's start.
  - It stops at the cheer and when the player leaves.
  - `Recover()` restarts the song, and with it the schedule.

**Crowd figures get their own FormID range** (`crowdFormIdBase`, 0x10000 up, in
placement order):
- Appending figures used to renumber everything built after them, now the singers and
  the `SkyrimFairAtFair` global, and the singers are held by the stage script in saves.
- Now figures and later records can't shift each other.
- The generator refuses to build if its own counter ever reaches the range.
- The figures were renumbered once, to 0x10000–0x10080. They're plain statics, not in
  saves.

### Verification

- Plugin deterministic: `06618bba7143ad4e...`. The voice files are too.
- Navmesh: 9 meshes, **0 errors**. Actors on the mesh: 233 of 233, singers included.
- 147 of 147 voice files exist under the engine's names, with none extra.
- The three FaceGen heads and tints are deployed. Four scripts compile.

### Test

1. Fiddle and Dragonborn-Approved:
   - Do the three singers' mouths move with the singing and stop in the pauses?
   - How far behind the music are the lips?
   - No subtitles?
   - No audible doubling?
   - In sync over the whole song, and after a save and load mid-song?
2. The singers' faces: normal, no dark-face bug, and each different?
3. The other two songs have no singer lines yet, until their stems exist.

## Previous pass: the patches get a dependable condition; dances loop (2026-09-23)

Barry: "we get the bards back now", but "Can we have the people dancing just loop? ... it
looks a bit janky", and "I still get the freeze".

- **The freeze, still: the patches loaded but the cloak kept pulsing.**
  - The dump: `MD_DeathEffectsHumanoidMagicScript.OnMagicEffectApply` at 545,625,
    down from 2 million.
  - The effect is still `FE0B3811`, Stealth Detection Fixes' dummy, cast by the fair's
    NPCs and by the player, all inside SkyrimFairWorld.
  - Both patches are enabled and load last. Only spell `0x817` carries the cloak, and
    it's the patched one. So `GetInWorldspace SkyrimFairWorld == 0` didn't hold in game.
    A likely reason: the fair's worldspace has Tamriel as its parent (`UseMapData`).
- **Fix: a global, the condition vanilla and Stealth Detection Fixes use for exactly
  this.**
  - `SkyrimFairAtFair` is built last before the navmesh. The stage script sets it to 1
    in the fair and 0 outside, on every update.
  - It's saved, so a load at the fair starts with it on.
  - Both patches now put `GetGlobalValue SkyrimFairAtFair == 0` first on every effect,
    then the worldspace test, then the mod's own conditions, all ANDed.
- **Dances loop.**
  - Each dancer starts one dance when a song starts and keeps it looping until the cheer.
    They used to get a new dance every 2 updates, which restarted it mid-loop.
  - Each song gives each dancer the next dance, so the floor still varies.
  - The folk pair start together once a song, and aren't restarted every 9.6 s.
  - If a vanilla dance state doesn't loop, its dancers will stop after one pass: that
    would show in the test.
- Checked: the patches carry both conditions first, with the original conditions and
  flags kept. One record added (the global). Plugins deterministic: `SkyrimFair.esp`
  `9a3606b0c7a81caa...`, patches `744ef98b...` (MD) and `7a7b28fb...` (Stealth).
  Deployed.

### Test

1. Load at the fair and wait a few seconds: the first update sets the global. Does the
   game stay up past two minutes? No "Suspended stack count" in `Papyrus.0.log`?
2. Dancers: do they dance smoothly through the whole song, or stop after one pass?
3. The folk pair: do they keep turning through the song?

## Previous pass: compatibility patches for Maximum Destruction and Stealth Detection Fixes (2026-09-23)

Barry: "is there a way we can make it compatible with that mod? Or do we need to disable
it?" Then: "let's do the proper fix".

- **Two light patch plugins**, written by the generator beside the plugin
  (`compatPatches`, `FairPatches.cs`), and deployed with it:
  - `SkyrimFair - Maximum Destruction Patch.esp` overrides `MD_GoreHumanoidMagic`
    (`8E6289`): the ability SPID gives every human, whose `OnMagicEffectApply` script
    was the flood.
  - `SkyrimFair - Stealth Detection Fixes Patch.esp` overrides `madDetectionCloak`
    (`0x817`, 3 effects): the detection cloak on every NPC that fed it.
  - Every effect gets **`GetInWorldspace SkyrimFairWorld == 0` first** in its
    conditions. First, because OR binds tighter than AND, and appending after an OR'd
    condition could change the original logic.
  - The engine keeps these effects inactive inside the fair and nowhere else. No script
    has to run, so nothing has to win a race with the Papyrus queue.
  - Both mods work as before everywhere else in Skyrim.
- **The Papyrus guard is retired.** It could never run before the flood.
  - No NPC carries `SkyrimFairNpcGuard` any more (`npcGuard.script` is empty), and the
    spell list is empty.
  - Invulnerability stays.
  - The guard's FormList record stays, so no later FormID moves.
- **Checked** by reading the patches back beside the originals:
  - light-flagged, one override each, no new records
  - masters: Skyrim.esm, the mod, SkyrimFair.esp
  - form version 44, as the originals
  - every original condition, magnitude, area, duration and spell field unchanged
  - the new condition points at SkyrimFairWorld
- Plugins deterministic: `SkyrimFair.esp` `6da22e995f0ee016...`, patches `c99b3224...`
  (MD) and `43af7fab...` (Stealth).
- `deploy.py` copies `dist/SkyrimFair - * Patch.esp` too. A patch whose source mod isn't
  found is skipped, not built.

### Test

1. **Enable the two new plugins in MO2's plugin list**, at the bottom, after
   `MaximumDestruction.esp`.
2. At the fair:
   - the game stays up
   - the first song starts about 4 s after arriving, and the bards play
   - the dancers dance, and the folk pair turns
   - `Papyrus.0.log` has no "Suspended stack count" dump, and is small

## Previous pass: rebuilt crowd meshes redeployed; the guard can't win against the flood (2026-09-23)

- The other agent rebuilt the crowd NIFs, fixing the white hands, blue faces and streaky
  hair (`docs/CROWD.md`). All 38 are redeployed and match byte for byte.
- LookFar02 gained boots: its STAT bounds come from `tools/crowd/figures.json`, and it's
  136 tall (was 135), collision box too. Footprints were regenerated. Plugin
  `7c5233ab55d0204d...`, deterministic. No placement changed.
- **Barry's test of the guard build:** no bard music (one bard played the flute
  animation), and the game froze again.
  - `Papyrus.0.log` (382 MB): the dump shows `MD_DeathEffectsHumanoidMagicScript.OnMagicEffectApply`
    at a frequency of 2,072,452, about 66 s after the load, as the first time.
  - Not one `SkyrimFairGuard:` line. The guard's `OnLoad` events run in the same queue,
    and the flood starts the moment the save loads, so they never get their turn.
  - A Papyrus fix can't win that race. The SPID exclusion (`-SkyrimFair.esp` in the two
    `_DISTR.ini` lines) acts when the game starts, before any script runs. That's the
    fix, pending Barry's go-ahead to edit his mods' files.

## Previous pass: the crowd library placed (51 figures), and the guard fixed (2026-09-23)

Barry's test of the folk-dance build: "the game stays, but ... the bards don't play, and
nobody does even a vanilla dance anymore". Then his brief: place the library
(`docs/CLAUDE_CROWD_LIBRARY_TASK.md`).

### Why nothing played: the guard never removed a spell

- `Papyrus.0.log` shows no error. There was no `song 0 playing` line either, in 50 s at
  the fair; the previous build started the song 3 s after arrival.
- No `SkyrimFairGuard:` line at all. The list filled (`strips 2 spells`), but no NPC ever
  removed one.
- So the Stealth Detection Fixes x Maximum Destruction flood was still running. It built
  up from arrival, and the stage script's updates queued behind it. The songs never
  started, so the bards and dancers never did either. None of the new script code runs
  before the first song.
- **Cause:** SPID puts the spells on the NPC *record*, which vanilla `RemoveSpell` and
  `HasSpell` don't reach.
- **Fix:** the guard calls Papyrus Extender's `PO3_SKSEFunctions.RemoveBaseSpell`, which
  takes a spell off the record and the actor, plus `RemoveSpell` for spells added to the
  actor.
  - It's compiled against a one-line stub (`src/Papyrus/stubs/`, on the compiler's import
    path only, never deployed), so the fair doesn't need Papyrus Extender to build or
    load.
  - Without Papyrus Extender the call fails harmlessly, and the SPID ini exclusions in
    `CODEX_HANDOVER.md` are the fallback.
- The guard also runs on `OnCellAttach`, for NPCs a save brings in without an `OnLoad`.
- It re-strips on every load: SPID puts the spells back each game start.

### The crowd library

The other agent built 38 figures: 16 poses, 12 characters, 21 outfits
(`docs/CROWD.md`, "The library"). **51 copies of 36 figures are placed.** Standing03 and
HandsBehind02 aren't used yet.

| Where | Figures |
| --- | --- |
| Archery, behind the real spectators (x 530–830), facing the targets | 13 + the clapper: LookFar01–02, Pointing01–02, Clapping02–04, Cheering01–03, ArmsCrossed01–02, HandsBehind01 |
| Stage, west of the audience (x 700–1150) | 10: ClappingHigh01, Tankard01, Tankard03, Cheering02, Toast01, Waving01, Laughing02, Talking01, HandsOnHips01, Standing02 |
| Stage, east of the audience (x 2950–3400) | 10: ClappingHigh02, Tankard02, Cheering01, Cheering03, Toast02, Waving02, Laughing01, Talking02, HandsOnHips02, Standing01 |
| Stage, south of the audience, either side of the avenue | 8: two Tankards, both ClappingHighs, both Toasts, Cheering03, Waving01 |
| Seated on free seats | 7: Seated01–03 at the prize booth and score-board stools, the social benches west and east of the square, a stool by the east lane, and a bench at the gate picnic |
| Leaning on the horse pen's south fence, facing the horses | 2: Leaning01–02 |

**How it's placed:**
- **`fairWorld.crowdPlacements`** is a flat, **append-only** list, in record order.
  - A figure's STAT is made at its first placement. Then comes each copy, then its body
    collision box.
  - So appending a figure, or another copy of an old one, only ever adds records at the
    end.
  - The clapper moved into the list with the same records in the same order: the plugin
    was byte-identical before the new placements were added.
- **`tools/place_crowd.py`** plans the placements once and appends them; it never moves
  existing ones. Its rules:
  - Ground must be open on the navmesh raster's main area, which keeps figures out of
    stall keepers' pockets.
  - At least 65 units from any real NPC and 75 from another figure.
  - At least 700 between copies of one figure.
  - Nothing in the avenue, the east lane or the stage steps.
  - Headings face the focus (the targets, or the dance floor), ±28°.
  - Its inputs are the generator's new site dump (`build/crowd_sites.json`: benches and
    whether a real sitter uses each, the pen's rails, every enabled actor), the navmesh
    raster and `tools/crowd/figures.json`.
- **Seated figures** (`seat` and `marker` in a placement):
  - Placed on the furniture reference nearest `seat`, at its seat marker, facing as the
    sitter does.
  - The markers are read from the vanilla NIFs into `fairWorld.seatMarkers`:
    FarmBench01 x ±30, CommonBench01 x −46/0/46, both facing −Y; WoodenBarStool (0, 4.8)
    facing +Y.
  - That seat becomes its **non-sittable static twin**, so no NPC sits down inside a
    figure.
  - The generator refuses a seat a real sitter is linked to.
- **Leaners:** 34 units out from the pen fence, facing it. Its lower rail is at 77–85 up,
  near the figures' 64–80 hand height.
- **Collision:** a body box [−18, −14, 18, 14] at full height. Leaners use [−18, −6, 18, 26];
  seated figures have none, because the bench is solid.

### Verification

- 36 STATs, each with the MODL and OBND from `figures.json` (0 mismatches). 51
  references.
- Against the deployed plugin: 128 records added, none removed. The only renumbered IDs
  are the navmeshes, which always come last.
- Standing figures: the nearest real NPC is 68 away, the nearest other figure 80.
- Navmesh: 9 meshes, 7,819 triangles, 58 islands, **0 errors**. Figures are cut by their
  model footprints (245 models), and actors on the mesh are 230 of 230.
- Generator run twice: identical SHA256 `145abe7b16945a82...`. Four scripts compile.
- Deployed: all 38 NIFs are in the mod folder, byte for byte.

### Test

1. **First, the guard** (it fixes the music):
   - Does the first song start about 4 s after arriving, with the bards and dancers?
   - `Papyrus.0.log` should show `SkyrimFairGuard: ... removed from the record True`.
2. **Archery range:** 13 figures behind the real spectators, around the clapper.
3. **The stage:** figures behind the audience on the west, east and south, with
   tankards, goblets and overhead clapping.
4. **Seated figures:**
   - at the archery prize booth and score board
   - on the social benches at both sides of the square
   - at the gate picnic
   Do they sit on the seat, not in it or floating above it, and facing the right way?
5. **Leaners:** on the horse pen's south fence. Do their hands meet the rail?
6. Skin tones (Nord, Breton, Imperial, Redguard), hair and beard colours, the props in
   hand, how repetitive the faces look at 5, 15 and 30 m, and the frame rate.

## Previous pass: Astra's folk dance in game (OAR), the figure's collision, the guard's retry (2026-09-23)

Barry confirmed the first crowd figure in game: "he blended in that well that i had to do
a double take", the right height and a natural skin colour. The only issue: no
collision. The colour went odd only with the camera inside him. Then: "i believe Astra
made a bespoke animation, could we try that".

### Astra's folk dance

`character-actors/folk-dance` (Barry's and Astra's, untracked) holds a paired dance:
- `folk_turn_A.hkx` for the male skeleton and `folk_turn_B.hkx` for the female
- 9.6 s, 30 fps, 289 samples, the vanilla 99 bones
- the two dancers turn arm in arm round a shared centre, 26 units from it (44 when they
  step apart)
- it has only been checked by a codec round trip, never in game

- **Re-based per dancer** (`tools/folk/rebase_folk.py`).
  - As authored, both clips share one origin, so both NPCs would stand on one spot, and
    the game pushes overlapping actors apart.
  - Only the root track is rewritten, relative to its own first frame. Every other bone is
    untouched, and the tool checks that no other bone carries motion.
  - Each NPC stands where its dancer starts: him at (+26, 0) facing 270, her at (−26, 0)
    facing 90. They're 52 units apart, and their bodies still meet over the centre.
  - Root round-trip error 0.0005 units. The output is deterministic and git-ignored, as
    the source is Astra's.
- **Played through Open Animation Replacer** (installed in Barry's list).
  - Each clip replaces vanilla `special_cicerodance1.hkx`, the animation of
    `IdleCiceroDance1`, **only for its own NPC record** (an OAR `IsActorBase` condition).
  - The generator writes the OAR `config.json` files next to the clips, from the records'
    FormIDs, so the conditions can't go stale.
  - Nobody else in Skyrim is affected, Cicero included.
- **Two new dancers** (`fairWorld.folkDance`), "Folk Dancer":
  - copies of visitors Male01 and Female01, persistent, built late (before the guard, so
    they're invulnerable too)
  - at the dance floor's centre front, (2048, 4420)
  - the crowd layers keep about 115 units clear round them (`keepClear`); the nearest
    dancer is now 106 away
- **The stage script** (`FolkDancers`, `FolkIdle`, `FolkLength`):
  - starts both in the same update once both are loaded
  - restarts them every 9.6 s through a song, waking exactly on time so there's no gap
  - stops them at the cheer, and starts them afresh with the next song
- **`deploy.py`** now also copies OAR's `config.json` files; it only took game-file
  extensions before.

### The figure's collision

- An optional `collision: [minX, minY, maxX, maxY, height]` per figure, in its own frame.
  It's the kind of invisible collision box the stage walls use, which works in game.
- The clapper's box is his body, not his reaching hands: [−20, −12, 24, 30, 132], turned
  and placed with him.
- The guard's FormList is now built before the figures, so adding figures never
  renumbers it.

### The NPC guard's retry

- Barry's log showed the list filled (`NPC guard strips 2 spells`), but no NPC traced a
  removal.
  - On that first load the NPCs loaded 4 s before the stage quest filled the list, so
    they found it empty.
  - The log was also 0.7 MB, against 543 MB before, but it stopped about a minute in, so
    it doesn't prove the freeze is gone.
- Now an NPC that finds the list empty tries again every 5 s, up to 3 times.

### Verification

- Against the deployed plugin: no record removed. 5 records were added and only the late
  ones renumbered (the figure, the guard's list, the folk pair, the navmeshes).
- The OAR conditions name `SkyrimFair.esp` records `16AB` (male) and `16AD` (female),
  the folk dancers' own records.
- 111 of 111 NPC records invulnerable, with the guard script.
- Navmesh: 9 meshes, 7,344 triangles, 58 islands, **0 errors**. Actors on the mesh: 230
  of 230.
- Generator run twice: identical SHA256 `09563660fbab2002...` (752,786 bytes). Four
  scripts compile. Deployed: the plugin, both clips and the three OAR configs.

### Test

1. **The folk pair**, centre front of the dance floor, when a song plays:
   - Do they dance, not stand?
   - Do their forearms meet as they turn?
   - Does the loop restart cleanly every 9.6 s?
   - Do they stop at the cheer?
   - If they only do the Cicero dance, OAR didn't pick up the conditions: check that the
     OAR menu lists "Skyrim Fair folk dance".
2. **The clapper:** can you walk into him now?
3. **The guard:** in `Papyrus.0.log`, `SkyrimFairGuard:` lines should say `removed True,
   still has it False`. Also: no "Suspended stack count" dump, and the game stays up.

## Previous pass: the first static crowd figure, and the NPC guard (2026-09-23)

Barry: add the static crowd prototype (another agent built it, `docs/CROWD.md`, and wrote
the brief `docs/CLAUDE_CROWD_TASK.md`), noting what was fixed and why. Also make the fair's
NPCs "have the same effects as child npc's where they can't be damaged", and deal with
the freeze.

### The crowd figure

- A new `fairWorld.crowdFigures` list, built after everything but the navmesh. Its first
  entry is `SkyrimFairCrowdClapping01`: a Nord man in farm clothes, clapping; one static
  mesh, no AI.
- One reference at (580, 1640), yaw 270: east of the archery spectators, facing the
  targets.
- **Three changes from the brief**, with the reasons, recorded in
  `docs/CLAUDE_CROWD_TASK.md` ("Implemented"):
  1. The brief's `market.fixed` only takes lane stalls.
  2. Its spot would have put him in front of the spectators, not behind.
  3. Both of its routes would have renumbered every later record, which saves depend
     on.

### The NPC guard (`fairWorld.npcGuard`)

- **Invulnerable:** all 109 fair NPC records. They take no damage and can't be killed,
  so no stealth kills or kill moves. It's the engine's Invulnerable flag; vanilla children get the same protection from their race.
- **Other mods' spells taken off:** `SkyrimFairNpcGuard`, an Actor script on every fair
  NPC record, removes the spells in the FormList `SkyrimFairStripSpells` in `OnLoad`.
  - The stage quest fills the list on start and on every load with `GetFormFromFile`
    from `stripSpells` (`plugin|id`).
  - So neither mod becomes a master, and a missing one is skipped.
  - First entries: Stealth Detection Fixes' `madDetectionCloak` (`0x817`) and Maximum
    Destruction's `MD_Gore Human Magic` (`0x8E6289`). Their clash queued 2.2 million
    Papyrus events at the fair and froze the VM.
- **Unverified:** whether `RemoveSpell` removes a spell SPID put on the NPC record. The
  script traces each attempt (`SkyrimFairGuard: ... removed True/False, still has it
  ...`). If it doesn't hold, the fallback is the two SPID ini exclusions
  (`-SkyrimFair.esp`, in `CODEX_HANDOVER.md`).

### Verification

- Against the deployed plugin: 3 records added at the end (the STAT, its reference, the
  FormList). Only the navmeshes renumbered; nothing else moved.
- STAT: OBND (−26, −35, 0) to (26, 35, 132), model
  `Meshes\SkyrimFair\Crowd\SkyrimFairCrowd_Clapping01.nif`; same layout as the outhouse.
  One REFR, at ground Z (0, the same as the hay bale beside it).
- 109 of 109 NPC records invulnerable, with the guard script and its property.
- Navmesh: the figure's footprint is cut (196 footprints). 7,344 triangles; actors on the
  mesh 228 of 228.
- Generator run twice: identical SHA256 `1c3377521927c995...`. Four scripts compile.
  Deployed, NIF checksum `012524da…`.

### Test

Best from a save outside the fair.

1. **The freeze:** does the fair stay up now? In `Papyrus.0.log`:
   - `SkyrimFairAudio: NPC guard strips 2 spells`
   - `SkyrimFairGuard:` lines, which should say `removed True, still has it False`
   - no "Suspended stack count" dump
2. **Frame rate:** with the Papyrus flood gone, compare with before.
3. **The figure:** east of the archery spectators, past the hay pile.
   - Height against the real NPCs
   - Feet on the ground
   - Facing the range
   - Skin colour of the face and hands, and lighting
   - How he reads from 5, 15 and 30 m
4. **Invulnerable:** hitting a visitor does no damage. That's still a crime, as it is with
   children.

## Previous pass: the crowd redistributed: a dance floor, people seated at the tables, children (2026-09-23)

Barry: about 60% of the crowd in the centre dancing, 30% in the market and 10% by the
archery and horses. Fewer standing NPCs in the market, replaced by people sitting at the
picnic tables, at least two a table. Some wanderers and a few children. Object people
come later. His `tai` test (30 to 110 fps) showed AI is the bottleneck.

### What changed

- **Seats:** the benches and stools became their sittable vanilla twins, with the same
  meshes, so the footprints didn't change:
  - `FarmBench01Static` → `FarmBench01F` (2 seats)
  - `CommonBench01STATIC` → `CommonBench01` (3 markers, counted as 2)
  - `WoodenBarStool01Static` → `WoodenBarStool`

  The player can sit on them too.
- **19 market groups retired**, placed as before but **initially disabled**
  (`crowds.groups[].retired`), so no FormID moved. Retired: mead, hot drinks, roast,
  bakery, cheese, pies, the onlookers, the dwemer browser, jewellery, smith, traders'
  crossing, picnic, picnic hay, braziers, cook fires, social edge, barrel drinkers and
  hay bale sitters. Kept: the sweetroll queue, the three stage-watching groups, the
  archery spectators and the stable hand.
- **Quiet visitors:** the visitor looks run a copy of `DefaultStayAtEditorLocation` that
  only greets the player (`SkyrimFairVisitorQuietStayPackage`: no chatter between
  visitors, no world interactions), so there are fewer AI checks.
- **Six crowd layers**, replacing the three tiers. Each is on its own enable-parent
  marker and switched live by the new global **`SkyrimFairCrowdLayers`** (default 6).
  The script properties are new (`CrowdLayers`, `CrowdLayer`).

  | Layer | Who | Placed |
  | --- | --- | --- |
  | 1 | **dance floor, front**: 4 rings across the square, below the stage ramp | 26 |
  | 2 | **dance floor, back**: 2 rings either side of the ramp | 17 |
  | 3 | **seated in the market**: `defaultSitLinkedRefNoConv`, each linked (unkeyed) to its bench or stool. Up to 3 at each picnic and busy picnic table, 2 at the mixed tables, social tables and benches, 1 at some micro benches | 31 |
  | 4 | **archery spectators**, seated on the two spectator benches | 8 |
  | 5 | **wanderers**, `DefaultSandboxEditorLocation1024NoConv` | 5 |
  | 6 | **children** ("Fair Child", faces and voices from 6 vanilla children as Traits templates, vanilla child clothes), sandboxing by the dance floor, the sweetrolls and the pen | 6 |

- **Dancers:** persistent, and given to the stage script as `Dancers`. During a song, each
  gets one of the Cicero dances (`IdleCiceroDance1-3`) every 2 updates (`DanceEvery`),
  staggered so the floor doesn't move in step. At the cheer, they applaud or cheer
  (`IdleApplaud2`, `IdleApplaud3`, `IdleCivilWarCheer`).
- **Split:**
  - centre: 11 watching + 43 dancers + 4 at the social tables, about 58
  - market: 4 queueing + 27 seated + 5 wanderers + 4 children, about 40
  - archery and pen: 2 + 1 + 8 seated + 2 children, about 13

  That's roughly 55/36/12.
- **Active actors: 192**, down from 227. There are 228 placed, 36 of them disabled.

### Verification

- Against the deployed plugin: no record removed. Only records built last were
  renumbered: the tier and layer records and the navmeshes. Tier 1's marker kept its
  FormID.
- Seats: 29 furniture refs used, 1 or 2 sitters each, every sitter within reach of its
  seat. Nobody stands on the stage ramp (the first layout had a ring on it; caught on the
  plan).
- Navmesh validator: 9 meshes, 7,325 triangles, 58 islands, **0 errors**. Actors on the
  mesh: 228 of 228.
- Generator run twice: identical SHA256 `93a50609179b1067...` (744,867 bytes).
  Footprints regenerated (unchanged). Scripts compile. Deployed.

### Test

Best from a save made **outside the fair**: the renumbered late records can pick up
state an older save holds for their old FormIDs.

1. Frame rate at the square, compared with last time's 30. If it's still low, try
   `set SkyrimFairCrowdLayers to 4`, then `3` or `2`, which drops the children, the
   wanderers, the archery seats and the market seats in turn.
2. The dance floor: do they dance through a song? Is there a gap between dances? If so,
   `DanceEvery` goes to 1. Do they clap and cheer at the song's end?
3. The picnic tables: do people walk to the benches and sit, two or more a table?
4. Children: do they look right (faces, clothes, size) and wander?
5. The market: quieter now, with only the sweetroll queue standing?

## Previous pass: bigger crowds (after the Crowded Streets audit) and a bard orchestra (2026-09-23)

Barry: audit Crowded Streets and "see how far we can push the crowds"; more bards on the
stage, "a bard orchestra", all men "because the songs are male singers".

### Crowded Streets, audited (read-only)

`Crowded Streets.esp` (v1.1.3, master Skyrim.esm), its readme, and its Papyrus sources
(`Source/`):

- **Cheap NPCs:** 8 archetypes (peasant, beggar, hunter, mage, mercenary, merchant, miner,
  priest). Each is a Traits + Model/Animation template on one of two leveled lists: 33
  and 110 face presets. Each has a single sandbox package (`CrStr_Sandbox`), no dialogue,
  no schedule, no script, and no voice.
- **Faces:** 110 face-preset NPCs whose FaceGen ships in its BSA (228 files), which is how
  it avoids dark faces. The fair already gets the same result by copying vanilla's own
  face lists.
- **Dynamic, and deleted:** a Story Manager location-change event (`CLOC`) starts a
  city, town or inn quest.
  - The script spawns 15–25 NPCs (5–10 in towns and inns; hard cap 50) with `PlaceAtMe`,
    disabled, at the location's centre marker.
  - It moves each within 2,500 of a random XMarker, then enables it, which snaps it to the
    navmesh.
  - It hides them at night and deletes them when the player leaves, so saves don't bloat.
- **The limit is performance:** the cap of 50 is there "to prevent players from setting it
  too high, crashing the game".
- It needs a Location with the LocTypeTown/City/Inn keyword and a centre marker, and
  other worldspaces are off by default. **It doesn't run at the fair.**

**What it means here:** the fair is small and always loaded, so placed actors beat
spawning: deterministic, no scripts, no save bloat. The question is only how many
actors Barry's machine carries, so the new crowd comes in **switchable tiers**.

### What was built

- **Crowd tiers** (`fairWorld.crowds.tiers`), built after every other record. Each tier's
  visitors hang off one persistent enable-parent marker, which the stage script enables up
  to the new global **`SkyrimFairCrowdTier`** (default 3, all on). They're switched live
  (checked every update at the fair):

  | Tier | Who | Placed |
  | --- | --- | --- |
  | 1 | **stage audience**: a fan south of the dance floor and groups along both its sides, facing the stage (the dance floor stays clear for dancers) | 42 |
  | 2 | **wanderers**: the visitors' looks with vanilla `DefaultSandboxEditorLocation512`, in groups along the avenue, the east lane and the west field. They walk, sit and chat on the new navmesh | 34 |
  | 3 | **busier stalls**: more customers at 15 popular trades (the stalls were already busy, so fewer fit) | 16 |

  Actors at the fair go from 126 to **227**. All of them stand on the navmesh.
- **The orchestra:** 12 bards, **all men**, facing the square, as vanilla bards play (the
  stage script's `PlayIdle`):
  - 5 lutes in the front row (y 5,250)
  - 4 flutes in the middle (y 5,440)
  - 3 drums at the back (y 5,630)

  The first three bards move into the rows, and the flute player becomes male (his record
  keeps its FormID). The other nine are built last and reach the script as the new
  properties **`Orchestra`** and **`OrchestraIdles`**. An existing save fills new
  properties from the plugin; a property it already holds keeps its saved value.
- **Stage script:** plays and stops the band and the orchestra together (`PlayAll`,
  `StopAllOf`), and applies the crowd tier (`ApplyCrowdTier`).
- `FairCrowds.PlaceGroups` is factored out of `Build`, so later tiers share its placement
  rules and its list of who's standing where.

### Verification

- Against the deployed plugin: 122 records added, none removed. The only FormIDs reused
  are the 9 navmeshes', which are always allocated last and aren't saved state.
- Navmesh validator: 9 meshes, 7,251 triangles, 57 islands, **0 errors**. Actors on the
  mesh: 227 of 227.
- Plan: `docs/images/navmesh_plan.png` (orchestra purple, visitors blue).
- Generator run twice: identical SHA256 `cbbd6c578f889607...` (737,294 bytes).
  Footprints regenerated (unchanged). Scripts compile. Deployed.

### Test

1. Frame rate at the stage square with everything on. Then try
   `set SkyrimFairCrowdTier to 2`, then `1` and `0`: the tiers vanish within a couple of
   seconds. Find where it's comfortable.
2. The orchestra: all 12 take up their instruments with the song and put them away at the
   cheer? All men?
3. The audience: facing the stage, and the dance floor clear?
4. The wanderers: walking about and sitting without getting stuck?

## Previous pass: navmesh phases 2 and 3 (real footprints, keepers, the pen, the stage) (2026-09-23)

**Confirmed in game (Barry):** no crash and nothing out of the ordinary, and **the archers
now shoot after a reload**. The navmesh was their missing piece.

Barry: "let's do stage two and 3 first, the archers can be a lower priority for now".
Phase 1's mesh cut whole stall blocks, the pen and the stage out, and left 70 of 126
actors off it.

### What changed

| Area | Phase 1 | Now |
| --- | --- | --- |
| Obstacle shapes | every object's bounding box, padded | **each model's own geometry**: `tools/make_footprints.py` samples every placed model's triangles into 16-unit cells with a bitmask of 16-unit height bands (-64 to 448), written to `tools/navmesh_footprints.json` (195 of the 209 models). An object blocks a raster cell only where its geometry stands between that cell's floor + 20 and head height (160). Posts, counters and rails block; the open ground under a roof, behind a counter or inside a fence doesn't. The other 14 (4 Holidays meshes in the old format, 10 small props the NIF reader can't parse) fall back to their bounds |
| Padding | 32 | **24** (an actor's radius); obstacles must reach 20 above the floor (was 12, so floor planks in the lowest band stopped blocking) |
| Stall keepers | off the mesh, their blocks cut solid | every actor's standing spot is opened again (24 radius; they already stand on free ground), and **islands with a purpose are kept**: the main area plus any pocket an actor stands in. Keepers stand on navmesh behind their counters; the long double block's shared aisle joins up where the pieces allow |
| The pen | cut solid | fence rails block only as rails; the horses stand on the pen's own mesh |
| The stage (phase 3) | cut solid | **raised platforms**: the deck at its height (134) and a ramp over the treads, from the deck's front edge to the ground one tread beyond the foot. Obstacles are tested against each cell's own floor, so posts and braziers on the deck block and the deck itself doesn't. The stage's own deck, treads, risers and skirt never block its platforms but still wall off the ground beside and under it. **The stage stays walled off from the square**: the invisible collision boxes Barry asked for earlier ("the performers stay inside") are respected, so the deck and steps are the performers' own island. The bards stand on it |
| Triangles | 8,066 | **7,253** (rectangles up to 32 x 32 raster cells). The busiest cell went from 2,945 to 2,456 |
| Actors on the mesh | 56 of 126 | **126 of 126** |

**The gate (phase 3):** still a closed static with no door, so there's nothing to link a
door triangle to. When the Tamriel gate becomes a real door, its triangles and linked
door go in the NAVI entry. Recorded in `docs/NAVMESH.md`.

### Build order change

`make_footprints.py` reads the model list the generator writes, so after a layout change:

```
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json   # writes build/navmesh_models.txt
python tools/make_footprints.py --data "E:/Modlists/Still In Skyrim/stock/Data" --extra "E:/Modlists/Still In Skyrim/mods/Holidays"
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json   # uses the footprints
```

`tools/navmesh_footprints.json` is committed (occupancy data, like `static_props.json`).
A model missing from it falls back to its bounds, so a stale file is never wrong, only
coarser. `build/navmesh_raster.txt` (`navmesh.debugRaster`) is a text dump of the raster
for debugging.

### Verification

- Validator: 9 meshes, 7,253 triangles, none degenerate or clockwise, every link
  reciprocal, every triangle in its grid, **56 islands** (the main area, the stage, the
  pen, the keepers' pockets), **0 errors**. NAVI: 19 entries, all 9 of ours.
- NAVM layout unchanged (one compressed `NVNM`, `0x40000`, form version 44).
- Generator run twice: identical SHA256 `fbfdf1eb5f5d84e3...` (724,451 bytes).
  `make_footprints.py` run twice: identical. Against phase 1's plugin, no record added,
  removed or renumbered. Deployed.
- Plan: `docs/images/navmesh_plan.png` (the mesh, and every actor on it).

### Known limits

- Keepers' pockets aren't joined to the lanes wherever the stall's pieces close them in.
  They can stand and turn but not walk out. Opening a keeper's gap is a module change.
- The perimeter edge is still a raster staircase with thin triangles.
- Islands inside a navmesh are common in vanilla, but 56 is a lot; watch for anything
  odd.

### Test

1. Load the fair: any crash, or near a cell line?
2. Do visitors, vendors and the band look normal (nobody sliding, sinking or snapping)?
3. `tcai` twice (AI off and on) near the stalls and the stage: do people settle back
   where they were?
4. The archers, when you get to them.

## Previous pass: navmesh, phase 1 (the ground) (2026-09-23)

Barry: "let's navmesh it", after the archers turned out to be stalled in a step that needs
a path. The plan and the record research are in `docs/NAVMESH.md`. This pass is its
phase 1: a generated ground navmesh for the whole compound, and the navmesh info map
entries.

### What was built

`src/SkyrimFair.Generator/FairNavmesh.cs` (`fairWorld.navmesh`), run as the very last step:

- **Walkable area:** a 32-unit raster of the ground inside the palisade, 48 in from it.
- **Obstacles cut:** 1,637 placed objects whose world bounds (turned and scaled) stand
  between 12 above the ground and head height (160), with a footprint over 12, each
  padded by an actor's radius (32). Statics, moveable statics, furniture, containers,
  activators, doors and the invisible collision walls block. Lights, sounds, markers,
  plants and overhead or on-table pieces don't.
- Bounds come from Skyrim.esm, the fair's own statics and **Holidays.esp**
  (`navmesh.extraMasters`, read from the MO2 mods folder). A base whose bounds can't be
  read gets a 60 x 60 x 120 footprint and is listed in the build output (none this time).
- **The largest connected area kept** (35 areas before, the rest being pockets inside
  stalls and under tables).
- **Rectangles** (up to 16 x 16 raster cells) that never cross a cell line. Each one's edge
  is split at every other rectangle's corner on it, so no edge has a T-junction. Then it's
  triangulated counter-clockwise: two triangles, or a fan from its centre.
- **Records:** 9 `NAVM` (cells -1..1), each with internal links, links across the cell
  lines (472), the lookup grid (row by row, as vanilla's are read), bounds, version 12,
  the constant CRC, compressed, and form version 44. A **NAVI override** of vanilla's
  `012FB4` carries the 9 new entries plus the 10 vanilla entries and the preferred-pathing
  block that Holidays and Fertility Adventures copy. Form version 44.

| Cell | Vertices | Triangles | Links out |
| --- | --- | --- | --- |
| -1, -1 | 241 | 334 | 17 |
| -1, 0 | 512 | 794 | 42 |
| -1, 1 | 184 | 255 | 32 |
| 0, -1 | 976 | 1,370 | 37 |
| 0, 0 | 1,955 | 2,945 | 127 |
| 0, 1 | 631 | 817 | 101 |
| 1, -1 | 310 | 395 | 17 |
| 1, 0 | 587 | 834 | 56 |
| 1, 1 | 236 | 322 | 43 |

### Verification

- **Validator** (scratchpad Mutagen program, reading the built plugin): 8,066 triangles.
  None degenerate or clockwise. Every internal and cross-cell link is reciprocal (the
  neighbour holds the same edge reversed and links back). Every triangle is in its
  mesh's lookup grid, and **every triangle is reachable from one start**: one island.
  **0 errors.** NAVI: 19 entries, all 9 of ours.
- **Record layout** compared byte-level with Fertility Adventures and Holidays:
  - NAVM is one compressed `NVNM`, flags `0x40000`, form version 44, as theirs
  - NAVI is `NVER`, the `NVMI` entries, then `NVPP` (25,696 bytes, the same as
    Holidays'), form version 44 (vanilla's own record is 40; corrected)
- Plan: `docs/images/navmesh_plan.png`. The mesh is green, and the dots are actors:
  - archers (red) are on the mesh
  - bards (purple) are on the stage and off it (phase 3)
  - pen horses (white) are off it
  - vendors (orange) stand inside the stall blocks, which the stall frames' bounds cut
    whole
  - most visitors (blue) are on it; 56 of 126 actors in all
- Generator run twice: identical SHA256 `9e29fb2fb6495056...` (733,223 bytes). Read back
  against the deployed plugin: 9 records added (the navmeshes), none removed or
  renumbered. Deployed.

### Known limits (phase 2)

- Vendors stand off the mesh inside the stall blocks. They hold still, as before. For
  them to walk in and out, the stall's area behind the counter needs to stay open (cut
  by the stall's pieces, not the whole frame).
- The perimeter edge is a raster staircase with thin triangles, and cell 0, 0 has 2,945
  triangles. A coarser raster in open ground, or merging, would cut both.
- No navmesh on the stage, in the pen or through the gate yet.

### Test

1. Load a save by the range: **do the archers shoot?** Then load one made away from it.
2. Watch the visitors and anyone walking. Anyone stuck, sliding or walking into things?
3. Optional: `tcai` twice to reset AI and watch. Or open `SkyrimFair.esp` in the Creation
   Kit read-only and look at the navmesh view. **Don't save it there.**
4. Any crash on loading the fair, or near a cell line?

## Previous pass: the archers, read from the game (2026-09-23)

Barry: the archers still don't shoot. His Papyrus log (today, fair profile) settles what
the earlier two fixes guessed at:

- Straight after the load, before the script touched them, **all four were running the
  fair's training package** (`SkyrimFairArcherTrainingPackage`). The package isn't lost
  on a load. It runs and does nothing.
- The hold-package flip then left them on the hold package ("released, now running
  ...001557"). It didn't restart anything.

**Cause:** the `UseWeapon` template's tree starts with a **Travel to "Use Weapon Location"**,
vanilla's being near the unkeyed linked ref (the stand) within 32. On a fresh cell load
the archer is standing there and the Travel finishes at once, so they shoot (Barry's
first visits). After a save and load the package resumes inside that Travel, which asks
for a path. **With no navmesh the path never comes, so the step never finishes and the
shooting branch is never reached.** `MoveTo` onto the stand doesn't finish a Travel.

**Fix:** the fair's copy sets "Use Weapon Location" to **near self, within 64**, the same
location type the package already uses for its weapon search. The archer is always
there, so the Travel completes with or without navmesh. The target is still the
`TrainingTarget` linked ref, and the trigger radius is still 20,000. The script still
squares each archer onto their stand and re-evaluates on arrival and load. The hold
package and its global keep their records (their FormIDs are in saves), but the archers
no longer carry the package, and the script only resets the global to 0.

Generator run twice: `8cb83cf315ebec13...`; no record added, removed or renumbered;
both archer NPCs carry only the training package; its inputs read back: 0 near self
1,000, 3 near self 64, 30 = 20,000. Scripts compile. Deployed.

**Test:** load a save by the range, then one made away from it: do they shoot within a
few seconds? If not, `getcurrentaiprocedure` on one, and `resetai`.

## Previous pass: the backdrop (large-reference mountains, a midground of ridges, a denser treeline) (2026-09-23)

Barry's brief: from the middle of the fair the horizon is bare. The palisade and nearby
trees show, but the mountains often don't until he walks up to a wall. Diagnose first,
then build layered depth (treeline, ridges, mountains, sky) that holds from everywhere
the player spends time. Don't touch the fair's interior.

### Diagnosis: A, an object-loading problem (with some fog on top)

Read back from the deployed plugin (`fbdc66277b56f95a...`):

- The 24 mountains were **Persistent + Is Full LOD (`0x10400`) in the world's
  persistent cell**, 10,800 to 23,300 from the origin, in cells 3 to 5 out.
- Barry's INI: `uGridsToLoad=5` (two cells round the player's) and
  `uLargeRefLODGridSize=11`. The world had **no large-reference table** (RNAM, 0 groups)
  and no LOD of any kind (no `.lod` settings, no object, terrain or tree LOD).
- From the centre (cell 0, 0) only cells -2..2 load. **Is Full LOD doesn't load a
  reference whose cell is outside the grid.** Walk to a wall and the grid slides over,
  bringing that side's nearest mountains in: exactly Barry's report. The handover's rule
  that Persistent + Full LOD "draws them whatever cells are loaded" was never verified,
  and is wrong for this case.
- Modelled from seven points (below): **before, almost no mountain rises above the wall
  from anywhere inside the fair**, only trees against sky.
- Fog adds to it but isn't the cause: the clear weathers (`SkyrimClearTU`) fog linearly
  from 0 to 40,000, so a peak at 17,000 is about 40% fogged; the cloudy ones from 1,000
  to 100,000.
- Placement also mattered: the near row's tops (about 2,700 high at 13,000 to 15,500) sit
  barely above the tall pines' crowns from the centre.
- Only 99 trees stood outside the wall, not the 490 the handover recorded.
- Terrain: unloaded terrain beyond the grid never shows from inside, because the wall-top
  sightline hides everything beyond the wall that low. Only the gate opening looks at
  ground level.

### The fix: vanilla's own method, large references

Vanilla Tamriel's mountains aren't persistent and don't use Full LOD. They're ordinary
references in their cells, listed in the world's **RNAM large-reference table**, which
the game loads within `uLargeRefLODGridSize` (five cells round the player's) at full
detail. Read back from Skyrim.esm: 8,455 groups, 120,659 entries for temporary
references, **each reference listed under every cell its footprint overlaps** (its own
and neighbours up to two away), with the cell stored as (Y, X) in Mutagen's naming.

- The generator now writes the same table for `SkyrimFairWorld` (`FairWorld.cs`, "the
  large-reference table"). The mountains are ordinary references in their own cells.
- **Every large reference must lie in cells -4..4** (`mountains.largeReferenceCellLimit`),
  so it is within five cells of anywhere in the compound (cells -1..1). The generator
  throws if one doesn't. The far row came in from 17,500–20,500 to **15,800–17,800**.
- `mountains.largeReferences: false` restores the old Persistent + Full LOD placement.

**Generated LOD was assessed and not used in this pass.** Object LOD isn't needed:
large references draw the real meshes at the distances that matter, with no assets.
Terrain LOD isn't needed from inside: the wall hides ground beyond it. Both tools are in
Barry's modlist (`tools/xlodgen`, `tools/dyndolod`), but they are GUI tools. DynDOLOD
would regenerate the Tamriel output and write its own plugin. xLODGen terrain LOD for
this world is a possible later step, as a Barry-run, documented stage, if the gate view
needs it. Tree LOD would stop the far side's trees unloading (below). Neither is built.

### New layers

| Layer | What | Count |
| --- | --- | --- |
| Treeline (new, `fairWorld.treeline`) | a second, denser band 150 to 2,000 beyond the wall, clumped, with clearings (`clusterPeriod` 1,100, `clearingThreshold` 0.36). The smaller pines (`TreePineForest03`, `05`) stand nearest the wall; the tall ones (`01`, `02`, `04`, up to 3,300 high) from 800 to 1,000 out, scaled down. Pine and tundra shrubs as undergrowth, and pine-forest rock piles and cliffs (sunk 60 to 180). Its own hash salt, so it doesn't repeat the first layer's grid | 181 (trees, shrubs, rocks) |
| Forest (unchanged) | the first layer, 320 to 5,200 out | 99 |
| Ridges (new row, `ridges`) | the midground, 9,000 to 11,800 from the centre (5,000 to 8,000 behind the wall): groups of 1 to 3 overlapping pieces, 30% of groups left out for sky. Plain and sparse-snow `MountainRidge01/02/03`, `MountainCliffSm01`, the tundra-rock and pine-forest `MountainCliffSlope`, base Z -100, scale 1.0 to 1.5 | 11 |
| Gate view | two pinned ridge pieces behind the gate's clearing: a pine-forest slope at bearing 186, 8,600 out, and `MountainRidge01` at 170, 10,600 | (in the 11) |
| Near mountains (unchanged) | heavy-snow ridges and cliffs, 13,000 to 15,500 | 11 |
| Far mountains | heavy-snow peaks and cliffs, now 15,800 to 17,800 | 13 |

FormIDs: the treeline and the ridges row (`placeLast`) are generated after every other
record, so nothing before them moved. The mountains keep their FormIDs; they only moved
from the persistent cell into their own cells.

### Verification

- **Horizon model** (a throwaway Mutagen program in the session scratchpad, same method
  as the other read-backs). From seven points (centre, market, Traders' Crossing,
  archery, picnic, stage square, entrance), it takes every palisade panel, tree, shrub,
  ridge and mountain the game would load there. That's ordinary references within two
  cells of the viewer's cell, and large references within five cells of any cell RNAM
  lists them under. It finds the highest silhouette in each degree of azimuth from eye
  height. Result in `docs/images/backdrop_horizon_before_after.png`:
  - **before:** only trees above the wall
  - **after:** ridges and snowy peaks above the treeline all round, from every point,
    with sky gaps that vary, and ridges and peaks through the gate
- Generator run twice: identical SHA256 `c2cf866d709c5210...` (596,614 bytes). Deployed
  byte-identical.
- Read back against the deployed plugin: 192 records added, **none removed or
  renumbered**. All 24 old mountains keep their FormIDs, now temporary in their cells. No
  persistent Full LOD reference is left. RNAM: 104 groups, 405 entries, 35 distinct
  references. Every entry matches its reference's cell as (Y, X); the own-cell key
  matches in 35; neighbours reach up to 2 cells, as vanilla's do.

### Performance

- 35 large references drawn at full detail within five cells. Vanilla mountain meshes
  are light, and the large-reference grid is vanilla's own streaming.
- 220 trees (was 99), 36 shrubs and 24 rocks, all within two cells of the wall. Trees
  have no LOD in this world, so each is a full tree model. That's still far less than a
  vanilla forest cell.
- No persistent Full LOD scenery is left.

### Known limits

- **The far side's trees:** from the east edge of the compound, the west wall's cell -2
  unloads, and the trees beyond about 2,000 out on that side go with it (the new
  treeline stays inside the loaded band). The large-reference ridges and mountains
  stay. Tree LOD would fix it.
- No terrain LOD: through the gate, the ground beyond two cells isn't drawn. The pinned
  ridges stand inside the drawn ground.
- The horizon model uses bounding boxes (a mountain's top as a peaked box, a tree's crown
  as half its bounds' width). It shows what loads and roughly how high it reaches, not
  how it looks. Barry's eye is the test.
- Weather and fog are unchanged.

### Follow-up: the rock behind the gate (Barry's screenshot)

Barry: the backdrop "does look better", but one rock behind the main gate shows a blank,
untextured side. It was the pinned `MountainCliffSlopePineForest03`. Measured from the
mesh (face area by outward normal): `MountainCliffSlope` and `MountainCliffSm01` are
finished on local +Y and nearly open on -Y (4 to 5% of their side area faces -Y, against
22 to 30% on +Y). The `MountainRidge01/02/03` pieces are finished both ways. The pinned
slope had yaw 95, so its open side partly faced the fair, and the random headings of the
other `CliffSm01` and `CliffSlope` pieces could do the same.

New `facesCentre` option on a mountain piece: its finished +Y turns to the compound
centre, within `faceJitter` (25 degrees). Set on every `MountainCliffSm01` and
`MountainCliffSlope` piece (10 placed), and the pinned slope is at yaw 6 (its bearing,
186, plus 180). Generator run twice: `fa26bb754bc2ad46...`; no record added, removed or
renumbered; deployed.

### Clear-weather test

In the console: **`fw 10a240`** forces `SkyrimClearTU` (clear, fog 0 to 40,000).
**`fw 10a243`** forces `SkyrimCloudyTU` (cloudy, fog 1,000 to 100,000). Compare the two
from the same spot. If a mountain appears or disappears as you walk across the
compound (not as the weather changes), that is loading; report where.

### Test for Barry

1. `cow SkyrimFairWorld 0 0`, then `fw 10a240`. From the centre, the market, Traders'
   Crossing, archery, the picnic tables, the stage square and the entrance: do ridges
   and mountains show above the treeline in every direction?
2. Walk across the compound: does any mountain pop in or out?
3. The treeline: natural, with gaps, not a wall? Any tree or rock through the palisade?
4. The gate: trees, then a ridge, then peaks?
5. Any ugly mesh base or floating edge on a ridge?
6. `fw 10a243` (cloudy): still reads, just hazier?
7. Frame rate at the stage square, compared with before.

## Previous pass: honey-style signs for Elven Goods, woodworker and archery; the archers' package restart (2026-09-23)

Barry's test of `6f0917f`: **the volume is right now, for all the audio.** The signs are
better, but the Elven Goods sign and the woodworker's still don't hang from their post,
nor does the archery sign. His ask: "duplicate the honey one and replace the sign". The
archers still don't shoot.

| Problem | Cause | Fix |
| --- | --- | --- |
| Elven Goods (the `sign_curios` group: elven, books, curios, fortune, religious) | a Solitude board hung from a separate stockade bar | **the honey group, another board**: Riften's `SignRTAlchemyShop01` (`0EA620`), which carries its own bar like the honey sign, placed as the other unturned signs are (turned 180, +49) |
| Woodworker (the `sign_trader` group: produce, woodworker, bard, cartographer, imports) | the Riverwood Trader board's own bar is 128 long, wider than the honey posts (92 apart) | the honey group with the generic `SignGeneralGoods01` (98, like honey's) |
| Archery sign | a Solitude board and a separate bar | the honey layout: posts 92 apart round y 105 (59 and 151), the Drunken Huntsman board (`096267`, hunter) turned 180 at the middle, no separate bar |
| Archers still idle | most likely the package resumes after a load where it stood (a `Travel` to the stand, which can't finish without navmesh), and `EvaluatePackage` keeps a package that's already running | a **hold package** above the training package: `SkyrimFairArcherHoldPackage`, a copy of `DefaultStayAtEditorLocation` live only while the new global `SkyrimFairArcherHold` is 1. On every arrival and load, the stage script sets each archer on their stand, sets the global and re-evaluates, so they switch to holding; 1.5 s later it clears the global and re-evaluates, so the training package starts from the top. It works in existing saves (an NPC's packages come from the plugin). Traces: "archer N on their stand, was running ..." and "archer N released, now running ..." |

**FormIDs:** removing the two bars would have moved every later record down by one,
the stage quest and all the actors included (caught in the read-back). New market piece
option **`"reserve": true`** takes a removed piece's FormID and places nothing; both
removed bars are reserves now.

**Verification:**
- Generator run twice: identical SHA256 `fbdc66277b56f95a...`. Scripts compile.
- Deployed: the plugin and 3 scripts, byte-identical.
- Read back against `6f0917f`'s plugin: the two bar references removed, the global
  (`001556`) and hold package (`001557`) added at the end, nothing else renumbered.
- The hold package's condition is `GetGlobalValue SkyrimFairArcherHold == 1`; both archer
  NPCs list the hold package, then the training package; the script has `ArcherHold`.
- The new sign groups read back with the honey sign's post layout; composed from the
  real meshes they hang as the honey one does.

**Test:**
- Elven Goods, woodworker and archery signs: hanging like the honey one?
- Archers: shooting within a few seconds of arriving, and after a reload?
- If not: `Papyrus.0.log` (the archer lines), and a quick console test: click an archer,
  type `resetai`. If that makes them shoot, the restart is the fix and needs to be
  stronger; if not, the problem is elsewhere.

## Previous pass: louder again, archers' trigger radius, the signs from the honey example (2026-09-23)

Barry's test of `2d03ba5`: the bards play. He wants the stage and the cheer "another
100%" and the murmur a little louder. The archers are still idle. The signs: the honey
vendor's is right. The Dwemer and mage signs hang at the wrong angle. The herbalist and
Elven Goods signs, and the archery sign, have no bar they visibly hang from.

| Problem | Cause | Fix |
| --- | --- | --- |
| Stage wants more | the files were already near their clean limit (-11 LUFS) | three more steps. (1) The stage output is now a copy of vanilla's **`SOMStereoRad10000`**, which plays the mono song at 100% in both front speakers (channel 0: L 100, R 100), where the HRTF model panned it. It still fades with distance, as vanilla's distant river loop does (up to +3 dB). (2) The limiter target goes to -4: the songs reach **-10 LUFS** (+0.8 dB), crest 9 dB. (3) The cheer's attenuation goes from 2 to 0 (+2 dB). About +4 dB on the songs and +6 on the cheer. The cost: the stage is no longer directional; it sounds like a PA |
| Murmur | 1 dB down | 0 dB, and the loop gets a plain +3 dB gain in the files (`loops[].gain`; it peaked at -5.5, now -2.5). The west field's quieter emitter stays 5 dB under the rest |
| Archers idle | **found in the package**: the `UseWeapon` template's shooting branch carries `GetWithinDistance` on its trigger ref (self) at the **trigger radius, 1,250**. Outside that, the tree drops into `Wait` with "max time 0 = forever". Load a save, or arrive, further than 1,250 from them and they wait for good. The last pass's `MoveTo` and `EvaluatePackage` didn't change that | **`SkyrimFairArcherTrainingPackage`**, a copy of `GuardSolitudeRangedTrainingPackage` with trigger radius **20,000** (`archery.triggerRadius`), which covers the whole world. It's created last, so no other FormID moves. The load-time reset stays, and now traces the archer's current package |
| Dwemer (pawn) and mage (alchemy) signs at the wrong angle | the working honey sign (`SignRTBeeandBarb01`) is the one Riften/Whiterun sign whose mesh is turned 180° at its root node. The others hang the other way round | every unturned Riften, Whiterun and generic sign is placed **turned 180° with its offset mirrored** (+49, the trader +64), so each has the honey sign's net transform exactly: pawn, alchemy, general goods (both), both blacksmiths, trader, and the centred hunter, mead and fishery signs |
| Solitude signs (herbalist, Elven Goods, archery) with no bar to hang from | the stockade bar was at z 178, 10 above the hooks' tops (168): the board hung in the air under it | the bar comes down through the hooks and rings: z **162** (scale 0.8) on the four Solitude vignettes, **160** (1.05) on the archery booth |

How the signs were checked: every sign mesh was measured with `nif_preview.py` (root
rotation and extents), and each group was composed from the real meshes at its config
transforms and rendered from both sides (a throwaway compositor in the session
scratchpad). The honey sign is the reference Barry confirmed.

**Verification:**
- Generator run twice: identical SHA256 `25bf700aee9b730c...`. Audio build deterministic.
- Deployed: the plugin, 9 sound files and 3 scripts, byte-identical.
- Read back against `2d03ba5`'s plugin: 1 record added (the archers' package, `001555`,
  radius 20,000, on both archer NPCs), none removed or renumbered.
- The stage output model: `DefinedSpeakerOutput`, 3,000 / 12,000, curve 100, 75, 50, 25, 0.
  Songs and cheer at 0 dB, murmur 0 (the west field 5).
- Placed sign groups read back as designed.

**Test:**
- Stage and cheer loud enough now? (Walk around the stage too: it no longer pans.)
- The murmur a touch louder?
- Archers: shooting after a reload, and after loading a save made far from the range?
- Signs: Dwemer and mage now hang like the honey sign? Herbalist, Elven Goods and archery
  hang from their bars? Also check the other stalls with signs (general goods, blacksmith,
  trader, hunter, mead, fish), which were turned the same way.

## Previous pass: concert-loud stage, bards that play, archers after a load, the archery sign (2026-09-23)

Barry's test of `c8c652a`: the stage music, cheer and running order all work, and the
pen horses are right. To fix: the music far too quiet ("like 100x"); the bards stand
without playing; the crowd a little louder; the archery sign still wrong; the archers
stop after a reload.

| Problem | Cause | Fix |
| --- | --- | --- |
| Stage music far too quiet | Barry's masters are about -19 LUFS; the speaker was full volume only within 1,500 on vanilla's steep curve (100, 50, 20, 5, 0); and `staticAttenuation` can't go below 0 (`SNDR` BNAM is unsigned) | three levers. (1) `build_audio.py` gains the songs and cheer to `stage.loudness` -7 under a look-ahead peak limiter at -1 dBFS: **-19 to about -11 LUFS, 8 dB up** (EBU R128 by ffmpeg), crest about 10 dB. (2) Full volume within **3,000**, silent at **12,000**. (3) A straight falloff, `stage.curve` 100, 75, 50, 25, 0. Together about 12 dB more in the square, more further out. 100x (40 dB) isn't reachable without clipping; this is as loud as the files go cleanly |
| Bards don't play | the copied Candlehearth package is `UseIdleMarker` at a *heading* marker for Luaffyn. It plays nothing. Vanilla's inn bards play from the `BardSongs` scene fragments: `Bard.GetActorRef().PlayIdle(IdleLuteStart)`, then `PlayIdle(IdleStop)` (read in `SF_BardSongsInstrumentalFlute_00095889.psc`) | the same method. The bards hold their spot with `DefaultStayAtEditorLocation`, and the stage script plays `IdleLuteStart` / `IdleDrumStart` / `IdleFluteStart` on each when a song starts and `IdleStop` when it ends (`Band`, `BandIdles`, `BandStop` properties; `fairWorld.audio.stage.band[].idle`). The bards are now persistent refs. The idle markers and package copies are gone; their 6 FormIDs are left unused, so every later record keeps its FormID |
| Crowd a bit quiet | 4 dB down | **1 dB** (Barry asked for 0 to 2) |
| Archery sign wrong | two faults. (1) The booth is placed as a *dressing group*, and `CommitDressing` ignored `exact`, so its posts, bar and sign each got a ±6-unit nudge and a ±4° twist: read back, the posts sat 3 and 9 off the sign's line. (2) The wreath on the north post pokes through the board's end | `CommitDressing` honours `exact`. The south post moves from y 60 to **20**, the bar to y **85** at scale **1.05** (it spans both posts), the sign to y **122.5**, so the board hangs from y 32.5 to 107.5: 3.5 clear of the south post and 12.5 clear of the wreath. Read back, all exact |
| Archers stop after a reload | the training package doesn't restart by itself after a load in a world with no navmesh. Castle Dour's archers have nothing extra (no script; one is persistent), but Solitude has navmesh | the archers are persistent refs, and the stage script, the fair's only controller, sets each back on their stand on every arrival and load: `MoveTo` the unkeyed linked ref (the `PatrolIdleMarker`), then `EvaluatePackage`. One not yet 3D-loaded is caught on a later update. Papyrus traces "archer N set on their stand" |

**A bounds gotcha found on the way:** `SignSFletcher`'s OBND says its board runs from
+15 to +90 along Y, but the SE mesh's root node is turned 180°, so it really hangs from
-90 to -15 (and down to z -100, not -81). Measure hanging signs with `tools/nif_preview.py`,
not the OBND.

**Verification:**
- Generator run twice: identical SHA256 `de58878c28d93ab1...` (576,330 bytes).
  `build_audio.py` run twice: identical files. All 3 scripts compile.
- Deployed with `deploy.py`: the plugin, 5 sound files and 3 scripts, byte-identical.
- Read back against the previous deployed plugin (`9c65b621...`): 6 records removed (the
  band's 3 idle markers and 3 packages), none added, every other FormID unchanged.
- The quest script's properties: the 17 old ones unchanged, plus `Band` (3 persistent
  refs), `BandIdles`, `BandStop` (`IdleStop` `0E4242`) and `Archers` (4 persistent refs).
- The stage output model is 3,000 / 12,000 with curve 100, 75, 50, 25, 0; the murmur's
  descriptors are at 1 dB, and the west field's quieter one at 6.
- Nothing band or archer is left in a temporary cell. No instrument marker is placed.

**Not verified:** anything in game.

**Test:**
- Is the stage loud enough now across the square? Does it carry, without distortion?
- Do the bards take up their instruments when a song starts, and put them away at the
  cheer?
- Save by the range, reload: do the archers start shooting again within a few seconds?
- Does the archery sign hang between the posts, clear of the wreath?
- Is the murmur about right at 1 dB?

If the bards still stand still, send `Papyrus.0.log` (with `bEnableLogging=1`): the
script traces "bard N plays: True/False" for each.

## Previous pass: no stage music, no band, quiet crowd (2026-09-23)

Barry: the crowd ambience plays but is very quiet; there is no music from the stage, and
no bards on the stage.

| Problem | Cause | Fix |
| --- | --- | --- |
| No stage music | Papyrus's `Sound` is the **sound marker** (`SOUN`), not the descriptor (`SNDR`). The script's `Songs` and `Cheers` pointed at descriptors, so they loaded as None and `Play` did nothing | a sound marker for each song and the cheer (`SkyrimFairAudioSong<Name>Marker`, `SkyrimFairAudioCheerCheerMarker`); the properties point at them (read back: all 5 are `SoundMarker`) |
| No bards | performers were never built | **the band**: three bards on the deck facing the square, lute (1840, 5380), drum (2048, 5480), flute (2256, 5380). Each stands at a vanilla instrument idle marker (`PlayLuteMarker`, `PlayDrumMarker`, `PlayFluteMarker`; the idle brings the instrument). Each has a copy of Candlehearth Hall's bard package (`UseIdleMarker` at its own marker, persistent). Bard and fine clothes; "Fair Bard" when looked at. `fairWorld.audio.stage.band` |
| Ambience very quiet | 10 dB static attenuation | 4 dB (about twice as loud) |

The stage script now writes `Debug.Trace` lines (started, loaded, each song and its
sound instance) for the Papyrus log.

- Generator run twice: identical SHA256 `9c65b621fa3e5af5...`.
- Deployed with the rebuilt scripts.

**Test**:
- Does the music start at the stage about 4 s after arriving?
- Do the bards play their instruments?
- Is the murmur at a good level now?

The bards play all the time; stopping them between songs is the performance cue step
(`docs/MUSIC.md` step 3).

## Hotfix: crash on boot from the sound descriptors (2026-09-23)

Barry's crash log: an access violation while the game loaded forms, on
`BGSSoundDescriptorForm` `SkyrimFair.esp` 0x14B2 (the first song).

- **Cause:** the sound descriptors had no `CNAM`, the descriptor's kind. Every vanilla
  one carries it (`0x1EEF540A`, standard), and the engine dereferences it at load.
  Mutagen leaves it out unless it's set.
- **Fix:** `Type = Standard` on every descriptor.
- **Also brought into line with vanilla's record layouts** (compared subrecord by
  subrecord):
  - the sound categories now carry `FULL` and `FNAM`
  - the quest carries `ANAM` (next alias ID) and form version 0
  - the player alias carries `FNAM` and `VTCK`, as vanilla's forced-player aliases do
- The output models, sound markers and globals already matched.
- Generator run twice: identical SHA256 `8bd235c4c39e5a41...`. Deployed byte-identical.

## Previous pass: festival audio, and the cobbles, sign bar and horses again (2026-09-23)

Barry's review of the last pass: the cobbles had gone completely; the sign bar didn't line
up with the posts; the horses stay in the pen but can be ridden; the worn ground now
varies. Then the big one: the passive festival audio with the stage music (his brief:
ambience, the stage set, the cheer, lifecycle, settings).

### The fixes

| Problem | Cause | Fix |
| --- | --- | --- |
| Cobbles gone | **the real cause**: the game treats each vertex's layer opacities as shares of the whole, and vanilla's add up to 1 at most (checked over 1.18 million Tamriel vertices: 98% at or under 1.0). The build wrote each layer's own coverage, so dirt, path and cobbles were all near 1 on the avenue and blended into mud. The last pass's extra wear raised the lower layers and buried the cobbles entirely; before that they showed only where the wear was low | the layers are converted to shares, top first: each keeps only what the layers above leave. Read back: the highest sum is 1.0 and no vertex is over it |
| Sign bar misaligned | module pieces (the archery booth) ignored `rotX`, so the lay-flat log stood upright; the booth's pieces are also nudged a few units at random | the bar is now `StockadeWoodbeamShort01`, a beam that lies flat and is centred as authored (144 long, scaled to 115), so no rotation is needed. New `exact` pieces skip the random nudge: the posts, sign, bar and wreath in every sign group. Modules now also honour `rotX`/`rotY` as vignettes do (the booth's arrow bundles, the leaning target, the pitchfork and broom) |
| Horses ridden off | vanilla horses with no owner | `SkyrimFairPenHorse` (Papyrus, `BlockActivation` on load) on each pen horse. New `script` on `crowds.animals` |

### Festival audio

Everything is in `docs/AUDIO.md`. In short:

- **Files audited:** 4 songs, one crowd murmur (loop), one cheer (one-shot). All are
  already mono 44.1 kHz 16-bit PCM. There are no other effects, so none were invented.
  `tools/build_audio.py` builds clean runtime WAVs:
  - songs: fades where Fiddle and Dragonborn-Approved stop dead
  - cheer: the leading silence trimmed, 11 s with a fade
  - murmur: a seamless 60.9 s loop in four copies, each starting a quarter further in
- **Ambience:** six positional sound markers, placed where the visitors cluster (food row,
  east lane, south-east stalls, entrance, crowd square, west field). Each plays its own
  copy, from full volume within 500 to silent by 3,000, 10 dB down. The engine starts and
  stops them with their cells, so they can't double up.
- **Stage set:** `SkyrimFairAudioQuest` (start-game-enabled) runs `SkyrimFairAudioScript`
  on a persistent speaker above the stage: song, cheer, 2 s, next song. It ducks the
  ambience to 75% under a song, stops everything when the player leaves the world, and
  restarts cleanly on every load through the player alias. The clock is game time, which
  stops in menus as the sounds do. Song lengths are measured from the built files.
- **Settings:** five globals, `SkyrimFairAudio{Ambience,Music}Enabled`,
  `{Ambience,Music,Cheer}Volume`, read live for a later MCM.
- **Game music:** the world's music type is vanilla's `MUSTavernSILENCE`.
- **Pipeline:**
  - `tools/build_papyrus.py` compiles `src/Papyrus/*.psc` with the Creation Kit's
    command-line compiler, against the CK's own sources, unpacked once. All 3 scripts
    compile.
  - `tools/deploy.py` copies the plugin, meshes, textures, `Sound\` and `Scripts\`,
    changed files only, and checks each copy.

**Verification:**
- Generator run twice: identical SHA256 `6d0cd3f43909e75e...` (574,508 bytes).
  `build_audio.py` run twice: identical files.
- Deployed with `deploy.py`: the plugin, 9 sound files and 3 scripts, all byte-identical.
- Read back from the plugin:
  - the quest's flags, player alias and alias script, and all 17 script properties
    (song lengths 149.6, 219.92, 202.76, 256.76; cheer 11)
  - 2 sound categories, 2 output models, 10 sound descriptors with their paths, 5 sound
    markers, 6 emitters and the speaker (persistent)
  - the 5 globals, the world's music type, and the 3 horses' scripts
  - no ground vertex over 1

**Not verified:** anything in game. The audio has only been checked as records and
files.

**Test** (the acceptance list is in `docs/AUDIO.md`):
- Are the cobbles back along the avenue?
- Does the sign hang from its bar, with the bar lying between the post tops?
- Can you still ride a horse? You shouldn't be able to.
- Balance the audio: murmur level, how far the band carries, the cheer's punch.

## Previous pass: ground wear, cobbles, archery sign, horses (2026-09-23)

Barry's review of the crowd and stable pass: vary the ground's wear (more worn in the
middle, less toward the palisade); the cobbles "don't pull through"; the archery sign
floats; no horses in the stable pen.

| Problem | Cause | Fix |
| --- | --- | --- |
| Cobbles break off in hard-edged wedges | two causes. (1) The game draws six textures a quadrant, the base and five alpha layers; the build allowed six alpha layers, so where the palisade's strip joined the four fair textures (the gate quadrants, the one by the stage), the sixth, the cobbles, was never drawn. (2) The cobbles faded out over 110 units, under the 128 between terrain vertices, and their edge wandered quickly, so the strip broke into the terrain's hard triangles | at most **five alpha layers** a quadrant: over the limit the faintest goes (3 wisps of tundra outside the gate). The cobbles' edge now fades over 300 (`ground.cobbleFeather`), wanders slowly (`cobbleRagged` 45, half-width 300), and the sunk patches are milder (`cobbleSunk` 0.35). Read back: one unbroken strip from the gate to the dance floor |
| Wear the same everywhere | wear depended only on what stood nearby | wear now fades from the middle of the fair (`centreDepth` 2000 inside the palisade) to 45% at the palisade (`edgeWear`), plus 0.2 all over the middle (`centreWear`); the patchy dirt-grass follows the same gradient, so the outskirts stay greener |
| Archery sign floats | the Solitude fletcher's sign hangs from rings at its top, and nothing ran between the posts to hang it from (the Whiterun signs carry their own bar) | a thin log crossbar (`WHIntWoodLogVerticalThinShort01` laid flat) between the posts, the rings hung from it. The same bar goes on the four Solitude-sign vignettes (fletcher, clothes, aromatics, curios) |
| No horses | the pen was left for later | three vanilla unsaddled horses (brown, grey, palomino: `EncHorse*`) stand in the pen, from the new `fairWorld.crowds.animals`, placed in the pen's own frame |

Verification: generator run twice, identical SHA256 `158de1d65cec1163...` (569,247 bytes),
deployed byte-identical; nothing new to deploy besides the ESP (all vanilla records).
Plan in `docs/images/lively_plan.png`.

**Test**:
- Do the cobbles run unbroken from the gate to the stage, with a soft edge?
- Does the ground look more trodden in the middle and greener at the edges?
- Does the archery sign hang from its bar? Check the bar doesn't sit backwards or tilted
  (it relies on the lay-flat rotation the arrow bundles use).
- Do the horses stay in the pen? Can the player ride them? They're vanilla horses with no
  owner, so riding one may take it for free.

## Previous pass: stage crowd area and stable corner (2026-09-23)

Barry's brief: "block out and dress the stage crowd area" (a dance area, a standing /
watching area, and a seating / social edge), and "the horse pens / stable corner" on the
west, support side. It stays an environment pass: no navmesh, inventories, band systems,
quests or MCM. The market and the approved layout are unchanged.

Stage geometry for reference: the deck is x 1173-2923, y 5088-6000, facing south; its steps
come down to a foot at y 4538 (x 1676-2420) and are walled; the avenue arrives at
(2048, 4308).

### A. Dance area

- New zone **`Dance`** (marker `SkyrimFairWorldDanceMarker`, at (2048, 4400)). It runs from
  y 4150 up to the deck, flanking the steps, 1,600 to 1,750 wide.
- It is a **market keep-out** (`DanceFloor`, plus `StageFront` over the deck and steps), so
  no dressing, seating or visitors can land on it. Read back: nothing on it but the stage's
  own steps and skirt.
- It is painted as **worn** ground (`ground.wornZones`), trodden dirt where the dancing
  will be.
- The old "stage approach" visitor group, which stood here, was removed. The floor is left
  open for dancers later.

### B. Standing / watching area

- New zone **`Watching`** (marker `SkyrimFairWorldWatchingMarker`, at (2048, 4060)). It is
  a U round the dance floor: a band across the south (y 3990-4150) and the two flanks
  (x 720-1173 and 2923-3380, up to y 5300).
- Its three parts are keep-outs (`WatchingSouth`, `WatchingWest`, `WatchingEast`), so the
  standing space stays readable. Only the existing light towers stand in the flanks.
- 11 watchers face the stage: **4 from the south, 4 from the west, 3 from the east.**

### C. Seating / social edge

Ten informal clusters round the outer edge of the square, asymmetric (more on the field
side). Each is placed by the collision-checked fitter, exempt only from the Crowd zone's
keep-out (new `dressing.exemptKeepOut`):

| Cluster | Pieces | Where |
| --- | --- | --- |
| `social_table` ×2 | rough table, bench, stool, crate for a seat, tankards, bread, cheese, jug, lantern | west (430, 4450); east (3720, 4880) |
| `social_hay` ×2 | three hay bales dragged round a crate, tankards, bread, basket | west (360, 4860); north-west corner (700, 5520) |
| `social_benches` ×2 | an L of two benches, stool, barrel table with tankards and a lantern | south-west (1000, 4020); east (3650, 4450) |
| `social_fire` ×2 | small campfire (**real light**), log seat, hay bale, stool with a tankard, firewood | west flank (520, 5250); east flank (3560, 5320) |
| `social_barrels` ×2 | two standing barrel tables with tankards, a drinking horn, mead | south-east (3130, 4000); north-east corner (3450, 5550) |
| `brazier_lit` ×2 | the fair's lit brazier (**real light**) | the square's back corners, beside the stage (900, 5600) and (3180, 5560) |

- 12 visitors round the clusters: 4 at the tables, 3 at the fires, 3 at the barrels, 2 on
  the hay.
- **The route from the avenue to the stage stays clear.** The avenue's corridor and the
  gate-to-stage sightline band are still keep-outs; only one festival pole stands at the
  corridor edge.

### Horse pens / stable corner

In the empty south of the west field, between the archery (y 700 up) and the gate, clear
of the avenue's wattle fence (x about 870) and the west wall. It is a new zone,
**`Stables`** (marker `SkyrimFairWorldStablesMarker`, at (300, 150), for horses later),
with worn ground.

| Piece | Contents | Where |
| --- | --- | --- |
| `horse_pen` | 768 × 512 pen of the **vanilla Whiterun farm fence** (`WRFenceStr01`, 9 rails on Whiterun stables' 256 grid), **a gate gap on the east side toward the fair**; hay scatter and mound, a hay bale, a water barrel with buckets (the watering corner; vanilla has no trough), a feed sack | centred (300, 150); interior left open for horses |
| `stable_shelter` | `StockadeLeanTo01` lean-to, stacked hay, feed sacks, a crate with leather strips, a bear pelt on a long crate (saddle blanket), a **pitchfork** (new prop) and a broom leant on it, a lantern | west of the pen |
| `feed_store` | stacked hay bales, sacks, a barrel | south-west |
| `hitching_rail` | three vanilla hitch posts in a row, a bucket, hay, a sack | south of the pen, on the gate side, where arrivals tie up |
| `stable_tack` | a saw horse carrying a pelt (tack stand), leather strips, a bucket, a stool | south |
| `micro_storage` | crate, sack, basket | north-west |

One stable hand stands by the shelter. The corner is modest: about 1,300 × 900 including
its support pieces.

**Numbers**: 86 dressing groups (from 68), 1,728 pieces, 57 visitors (59 keepers, 4
archers: 120 actors), 22 real lights. One new prop (`Pitchfork`, 174 in all).

**Verification**:
- Generator run twice: identical SHA256 `ea911c81e988c234...`. **Deployed byte-identical**,
  with the pitchfork.
- Read back from the ESP: dance floor clear of dressing and visitors; watching areas hold
  only watchers and the existing towers; no NPC inside the stage walls; avenue route clear.
- Renders in `docs/images/crowd_and_stables.png`: the pen closes with its gate gap, the
  shelter, hay and hitch posts round it; the square open in the middle.

**Visual review needed**:
- The **fence**: rails should meet at the corners (they overlap about 14 at each end by
  design), and the gate gap should read as a gate.
- The lean-to, pitchfork and saw horse sit at their measured heights; check nothing
  floats.
- Is the **dance floor** broad enough, and does the square feel open in the middle and
  social round the edges? The social edge is uneven on purpose; say if a side feels bare
  or crowded.
- **Performance** with 120 actors and 22 lights near the stage at night. Visitor groups
  are one number each in `fairWorld.crowds`.

## Previous pass: archery touch-ups (2026-09-23)

Barry: "you just nailed it", archers working, and two small things at the archery booth:
- **Fletcher sign hanging off one post**: every vanilla shop sign is modelled across its
  local Y, but the boards sit at different offsets from their origins:
  - centred: Riften Fishery, Honningbrew, Drunken Huntsman
  - from 0 to 98: general goods, alchemy, blacksmith, pawn (0 to 128 for the Riverwood
    trader)
  - about -90 to -15: the Solitude signs, including the fletcher
  - -98 to 0: Bee and Barb

  Hung at the posts' midpoint, the offset boards stuck out past one post. Every sign (15
  stall-sign vignettes and the booth's) is now shifted by its measured extent so it
  centres between the posts.
- **Floating arrows**: the arrow bundle is 59 long along -Y and is stood upright in its
  barrel, but its foot was on the rim. 10 arrow pieces (the booth, range storage, the
  fletcher's arrow barrel) now stand 36 lower, inside the 80-high barrels.

Generator run twice: identical SHA256 `c521ee9204d4bb75...`. **Deployed byte-identical.**

## Previous pass: review fixes and one trade per stall (2026-09-23)

Barry's in-game review (18 screenshots): props covering stall fronts, several stalls looking
like the same vendor (the drum and bowls), keepers too far to talk to (the long double
stall), the stage walkable, unused containers in front of stalls, two spit fires at the
meat stall, floating hay bales, and the parallax cobbles to include. Then: "could we now
determine which stall is which vendor", and a list of 39 stall identities to use.

| Problem | Cause | Fix |
| --- | --- | --- |
| Props covering stall fronts; empty containers in front | the original kits' `WRMarketDisplayShelf01/02/03` sat on the ground in front of counters (they read as empty boxes and steps); some kit "spill" spots were in front of counters; nothing kept later dressing out of a stall's frontage | shelves removed from every module; side spots moved to the stall ends; a **keep-clear frontage** (`market.frontageDepth`, 130) in front of every counter, which seating, dressing groups and visitors must stay out of, and poles must keep off the middle of. Read back: **0 of 33 stalls have anything low in front of the counter** |
| Keeper out of reach (the double stall) | `WRMarketStand01`'s table is 210 deep; its keepers stood 230 from the customer's side | `grand_double` is now two `WRMarketStand02` counters (124 deep) with a keeper each, just behind the counter (y -98). `whiterun_stand` and `canvas_wide` keepers moved to y -98 too |
| Same-looking vendors | kits shared goods (carver, timber and toys all had the drum and bowls; cooper and potter shared household goods); EastLane's right side restarted its theme list | **one trade per stall**, from Barry's list, each with its own kit; one theme sequence per lane across both sides |
| Two spit fires | both side spots of the roast kit picked the spit | no vignette twice on one stall |
| Floating hay bales | `HayBale01`'s origin is its base (z 0..85), but modules placed it at z 40 | z 0 everywhere, archery backstops included (16 bales, all at ground) |
| Stage walkable | nothing stopped the player | **29 invisible collision boxes** (vanilla `CollisionMarker` box primitives, default layer as vanilla's 897) round the deck and its steps, 15 outside every edge, 400 high; the performers stay inside |
| Cobbles | the cobble texture set pointed at vanilla `WRStoneFloor01`, whose `_p` only the Whiterun pack supplies, and installing the pack would change Whiterun city too | the pack's `wrstonefloor01` diffuse, normal and parallax maps copied to a **fair-only path**, `textures\SkyrimFair\Ground\Cobble01*.dds`, so only the fair's avenue uses them (git-ignored; redistribution permission unverified) |

**Which stall is which.** The fair fits 33 stalls; 33 of Barry's 39 trades are placed:
- **Avenue food row**: Fruit & Produce, Hot Pie, Mead & Ale, Cheese & Dairy, Bakery,
  Roast Meat, Spiced / Hot Drinks, Honey & Beekeeping, Spice & Imported Foods.
- **EastLane**: Iron & Steel Smith, Hunter & Leatherworker, Elven Goods, Dwemer Curios,
  Mage Supplies, Alchemy, Books & Scrolls, Jeweller, General Trinkets & Curios, Fur Trader,
  Herbalist / Apothecary, Fishmonger / Smoked Fish, Bowyer & Fletcher, plus the signature
  Imperial Armourer, Stormcloak / Nord Armourer and Sweetroll Stall.
- **East Wall Walk**: Clothing & Fine Fabrics, Woodworker / Carpenter, Pottery & Household
  Goods, Festival Toys & Gifts, Fortune Teller / Mystic Curios, Rare Goods / Travelling
  Merchant, Candle & Tallow Maker, Bard & Instrument Merchant.
- **Left out** (kits ready): Miner & Prospector, Saddler, Religious Charms, Cartographer,
  Festival Decorations and **Provincial Imports**, which removes the #27 / #39 overlap.

**Every keeper is now its own NPC record named for the trade** ("Cheese & Dairy Stall"),
so looking at a keeper says what the stall is (59 records, `SkyrimFairVendor<Theme><NN>`).
The build writes **`docs/STALLS.md`**:
- number, trade, theme id, lane, position, shell, counter goods, side and rear goods, sign
- each stall's marker for `player.moveto`
- the kits not yet placed

`docs/images/stalls_map.png` numbers the stalls on the plan.

**New goods** (25 more physics-free props, 173 in all): inkwell, quill, ores and an
orichalcum ingot, pickaxe, three staves, honeycomb, lavender and mountain flowers,
deathbell, nightshade, a flower basket, garnet, a gift satchel, troll skull, canopic jar,
a milk jug, a woodcutter's axe, a shovel. Also 37 new vignettes, among them dairy, plated
pies, pastries, hot drinks, honeycomb, spice sacks, staves, flowers, writing, cut gems,
trinkets, salted fish, arrow bundles, bows, carved tools, toys and gifts, mystic,
antiquities, songbooks, a standing lute, ores and mining tools, a small brazier, an arrow
barrel, flower baskets and a tools rack.

**Numbers**:
- 33 stalls, 1,566 pieces, 68 dressing groups, 24 festival crossings, 38 visitors,
  59 keepers.
- 5 stall lights: sweetroll, pies, mead, roast, hot drinks.
- 20 real lights in all.

**Verification**:
- Generator run twice: identical SHA256 `2a044aea1a73d744...` (558,669 bytes). **Deployed
  byte-identical**, with the 25 new props and `textures\SkyrimFair\Ground\`.
- Masters read back: `Skyrim.esm, Holidays.esp`.
- The cobble texture set reads the fair-only paths.
- 0 stall frontages obstructed; 0 floating hay bales.

**Test**:
- Can every keeper be reached and talked to, especially on the rebuilt double stalls?
- Do the stalls read as different trades from the path? Look at a keeper to see its name.
- Is the stage blocked at the steps and all round, with the performers still on it?
- Are the cobbles visible along the avenue, with parallax?
- Is the meat stall down to one fire?
- Any hay bale or prop still floating?

## Hotfix 2: the real cause, a BSA extractor bug (2026-09-23)

The same crash came back after the rigid-body change (same file, same instruction). Two
things settled it:
- Vanilla has its own `ElvenSwordForDisplay` STAT on the same `ElvenSword.nif`, so a
  weapon mesh as a static is fine.
- The crash was reading about 19 bytes from the end of the 69,369-byte file, past the
  64 KB first LZ4 block.

**Cause**: `tools/bsa_extract.py` decoded each LZ4 block of a frame on its own. SSE
archives link their blocks (frame flag "block independence" is 0), so matches in the
second block reach back into the first. Those read zeros, and **every file over 64 KB
came out corrupt past its first block** while keeping the right length. Five props were
affected: `ElvenSword`, `DrinkingHorn`, `HideCuirass`, `HuntingBow` and `ImperialBow`.
The first "hotfix" below was a wrong diagnosis. Its change (fixed rigid bodies instead
of unhooked links) is still the better way to make props static, so it stays.

**Fix**: the decoder now decodes into one output buffer with the whole frame as history,
and skips block checksums when the flag says they are there.
`tools/make_static_props.py` also checks each NIF's footer. That check only caught one of
the three old files it was tried on, so it is a guard, not the proof.

**Proof**: all 148 source meshes extracted by the fixed tool are **byte-identical to
Mutagen's own BSA reader** (`Mutagen.Bethesda.Archives`). Exactly the five props over
64 KB changed. The plugin is unchanged (`60d0a724b1b08410...`). **Meshes redeployed,
byte-identical.**

Earlier inspection work extracted meshes with the same tool: renders, and surface and
bound measurements. The measurements the fair relies on came from ESP bounds or from
meshes under 64 KB. Anything measured from a mesh over 64 KB before today should be
re-checked if it matters.

## Hotfix: the crash on entering the fair (2026-09-23)

Barry crashed entering the fair. CrashLogger: `EXCEPTION_ACCESS_VIOLATION` loading
`meshes\SkyrimFair\Props\ElvenSword.nif`, reading address `0xFFFFFFFF - 0x18`. That is
the "no collision" link (-1) the props tool had written into the mesh's root node, being
dereferenced as a pointer, with the rigid-body blocks left orphaned in the file. **130 of
the 148 props** carry their collision on the root node like the sword. The tower lantern
survived only because its collision is on a child node.

**Fix**: `tools/make_static_props.py` no longer touches links or BSX flags. It makes each
rigid body **fixed**, the way vanilla's unmoving barrels and crates are authored. Read
from `Barrel02.nif` and `CommonCrate01.nif` against `Bread01A.nif` and `ElvenSword.nif`:
- collision layer STATIC, in both filter copies (+4 and +36)
- inertia diagonal and mass 0 (+116, +136, +156, +180)
- motion system FIXED, deactivator, solver and quality FIXED: `05 01 01 00` at +224

Every block, link and flag is otherwise byte-identical to vanilla (the sword differs in
19 bytes, all inside its `bhkRigidBodyT`). All 148 props checked: every rigid body fixed
(136 with one body, 12 with two to four). The plugin is unchanged (`60d0a724b1b08410...`,
the props' bounds are the same). **Meshes redeployed, byte-identical.** Goods now have
solid, immovable collision.

**Test**: enter the fair again; goods should stay put when bumped.

## Previous pass: festival liveliness and density, worn ground (2026-09-23)

Barry's brief: "THE STRUCTURE IS GOOD, BUT THE FAIR STILL FEELS TOO BARE... MORE LIFE, NOT
MORE LAND", then "i have a mod called 'Holidays' can we try and use some of the
decorations", "audit & implement the whiterun and the parallax mod", and "the cobblestone
for like the center path, and then the dirt parallax textures for the outside areas, but
make them patchy... make it look like the festival is used and the ground is worn". The
layout is unchanged: same 33 stalls, same lanes, avenue kept clear.

### Numbers (read from the written ESP)

| | Before | After |
| --- | --- | --- |
| Market pieces | 301 | 1,922 (1,056 of them physics-free goods) |
| Dressing groups | 47 | 68 |
| Festival line crossings | 0 | 21 (42 poles, 42 rope halves, 75 hanging lanterns) |
| Visitors | 0 | 34 (plus 59 stall-keepers and 4 archers) |
| Real lights | 14 | 18 (3 stall lights, 1 range brazier) |
| Plugin | 423 KB | 561 KB, masters `Skyrim.esm, Holidays.esp` |

### Fixed on the way: sunk stalls and keepers inside tables

`WRMarketStand01` (the `grand_double` stalls: Imperial, Stormcloak and four more) has its
origin at its tabletop with legs 72 below (bounds minZ -72). Vanilla stands it about 75 up
(Carlotta's: her ground items sit 71-74 below the stand). We placed it at ground level, so
its table was at ankle height, its canopy at head height and its keepers stood inside the
table. It now stands at 75, and its keepers behind the table (y -145). The
`whiterun_stand` and `canvas_wide` keepers moved behind their `WRMarketStand02` counter
too (y -105 and -100). This is probably what Barry saw as the Dawi "breaking with the
vendor table".

### Goods: physics-free static props

The goods vanilla puts on its market stalls (cheese, bread, bottles, weapons, pelts) are
loose havok items: they fall, get knocked off and can be stolen, which is what happened to
the tower lantern. **`tools/make_static_props.py`** (replaces `make_tower_lantern.py`)
copies each vanilla mesh listed in `tools/static_props_sources.json` out of the BSAs with
its rigid bodies made fixed (see the hotfix above; the first build unhooked them and crashed), into `meshes\SkyrimFair\Props\` (**generated, git-ignored**, a
modified Bethesda mesh). It measures bounds into `tools/static_props.json` (committed),
and the generator makes a STAT `SkyrimFairProp<Name>` for each (`@Prop<Name>` in modules).
**148 props**:
- **Food**: bread, cheese, vegetables, sweetrolls, pies and treats.
- **Drink**: meads, wines, tankards and a drinking horn.
- **Tableware**: pottery, silver and baskets.
- **Arms and armour**: iron, steel, Imperial, Stormcloak, hide, elven and dwemer weapons,
  armour and shields.
- **Hunting**: bows, arrows and pelts, plus hanging game and hanging herbs.
- **Valuables and curios**: jewellery, soul gems, dwemer curios, potions, books and scrolls.
- **Instruments**: lute, drum and flute.
- **Sacks** and a food barrel.

11 skinned meshes (books, bows, hanging game) have fallback bounds.

### Stall individuality: five layers from theme kits

Each stall module now has **slots** in its own frame, measured from the shells:

| Slot | What it is |
| --- | --- |
| counter strips | Counter tops: Windhelm stalls at 77, `WRMarketStand02` at 76, `WRMarketStand01` table at 77 once raised, the display shelves' tiers, wooden tables at 62. |
| hang lines | Under the canopies. |
| side and rear spots | On the ground. |
| sign spot | Where the identity marker stands. |

A **stall kit** per theme (25 kits covering all 38 themes) fills those slots from a
library of **111 vignettes** (small scenes such as a cheese board, a sack pile, a barrel
table or a pelt rack):
1. **Structure**: the existing shell.
2. **Front display**: goods laid along every counter strip, cycling the kit's vignettes.
3. **Side clutter** and 4. **rear storage**: one vignette per ground spot, or none now and
   then.
5. **Identity**: a vanilla shop sign hung between two `SignWRPost01` posts, perpendicular
   to the street as vanilla hangs them (posts about 90 apart, sign at 168), usually with a
   snowberry wreath.

Hang lines carry herbs, game or coloured lanterns, depending on the kit.

Examples:
- **Sweetroll**: platters and plates of sweetrolls, a honey sign, coloured lanterns, a
  stall light, the biggest queue.
- **Food**: cabbages, potatoes, apples, gourds, garlic hanging, produce spilling onto the
  ground.
- **Drink**: meads and tankards, Honningbrew sign, mead barrels, a barrel table with
  stools.
- **Roast**: meats and fish, hanging game, a spit roast beside it.
- **Smith**: swords, axes and helmets, weapons standing in a barrel.
- **Imperial**: red rug with an Imperial helmet and shield. **Stormcloak**: pelts, a
  Stormcloak helmet, axes and a war horn.
- Also jewellery on a cloth, dwemer curios, potions, books and candles.

### Overhead: festival lines (Holidays)

At intervals along every lane, a pole stands each side, 35 inside the corridor edge. That
is the only room there is, because stalls line both sides, and a 16-wide pole does not
impede walking. A pennant rope is swagged between the poles:
- **Poles**: vanilla `WHIntWoodLogVerticalThin01` with a `WHIntWoodLogVerticalThinShort01`
  cap, 381 tall.
- **Ropes**: two mirrored halves of the vanilla Solitude festival line
  (`SRopefestivalLine01`, which starts 53 from its origin, runs 682 and rises 150), in
  **Holidays' colourways**: Whiterun, Saturalia, Riften and Windhelm, and **one Imperial
  and one Stormcloak half** meeting over the rival stalls. The low middle is at about
  330-350, just above the tallest canopy (281).
- **Lanterns**: Holidays' coloured animated lanterns follow the curve of most crossings.

Runs cover the Avenue, EastLane, EastWallWalk, EastCross and EastEntry: 21 crossings, 4
refused where a junction or table stood.

### Archery: a county-fair attraction

The keep-out is now just the four lanes, from the backstops to just behind the firing
line (x -760 to 260, y 860 to 1940), so the range can be dressed round its edges. Added:
- An **attendant booth**: a table of bows and arrows, a barrel of arrows, a lantern and
  Solitude's fletcher sign.
- A **prize booth**: a sweetroll platter, meads, a silver goblet, an amulet, a wolf pelt
  and a sign stand.
- **Spectator benches** on both long sides (benches, stools, hay bale, barrel with
  tankards).
- A **scoreboard** (sign stand, stool, parchment).
- **Range storage** (spare target leaning on arrows and crates).
- A **hay** cluster and a **brazier**.
- Three **spectators** behind the firing line.

Read back: **0 new references in any line of fire**.

### Hotspots and uneven crowds

Visitors are the stall-keepers' kind of NPC under the name "Fair Visitor": vanilla faces
from the fair's own face lists, the stay-at-location package. They are placed in loose,
facing groups round what draws people, and never inside a stall, dressing group or pole:

| Where | Visitors |
| --- | --- |
| Sweetroll queue | 4 |
| Mead / drink stall | 3 |
| Roast | 3 |
| Stage approach | 5 |
| Round braziers | 5 |
| Archery line | 2 |
| Traders' Crossing (EastLane x EastCross, 3780, 1000) | 2 |
| Picnic tables | 4 |
| Cook fires | 2 |
| Bakery, pies, jewellery | 1 each |
| Imperial rivals | 1 |

Group sizes vary by one either way, and not every table or fire gets a group, so the
density has a rhythm. A few groups got no one where their ground was already full (cheese,
Stormcloak, dwemer, smith).

### Micro-clusters, wall pockets, picnic

- **Micro-clusters**: 11 placed (barrel with tankards and stools, crate, sack and basket,
  delivery handcart, woodpile with chopping block, hay with basket and bread, bench with
  lantern, trader's cart). They fill gaps between stalls with the spiral fitter, which
  respects lanes, keep-outs and stalls.
- **Wall pockets**: 7 placed, irregular along the palisade (woodpiles, stacked crates,
  barrels, a camp).
- **Picnic**: the seating runs now mix `picnic` with `picnic_busy` (tankards, bread, cheese,
  a jug and a lantern on the table, a stool and a hay bale dragged in), `picnic_mixed`
  (round table, stools, a crate) and `picnic_hay` (hay bales round a crate).
- **The picnic lantern** was vanilla `Lantern`, a havok object that can fall. It's now the
  static copy.

### Worn ground and the cobbled avenue

Five fair-owned landscape textures (`SkyrimFairGround*`). Each copies a vanilla LTEX, so
grass, footsteps and friction carry over, and has its own texture set whose **height slot
names a parallax map**:
- **Grass** from `LFieldGrass01`: the base, keeps its grass.
- **Dirt-grass** from `LFieldDirtGrass01`: patchy almost everywhere, more where walked.
- **Dirt** from `LDirt02`: bare patches following wear, broken by noise at two scales.
- **Path** from `LDirtPath01`: trodden where traffic is heaviest.
- **Cobble** from `LSnowCobble01` (stone footsteps), retextured to
  `Architecture\Whiterun\WRStoneFloor01` with its `_n` and `_p` maps. It runs the length
  of the avenue, 500 wide, its edge wandering ±110, some stones sunk under dirt, and
  scuffed bare at the fringe.

**Wear** is a field painted *after* everything is placed. It rises round:
- stall fronts and floors, stall-keepers and visitors
- dressing groups (picnics, fires, camps)
- the archery firing line and target area
- every market lane's corridor
- the entrance forecourt and the crowd square

The old flat zone colours, only ever plan markers, are no longer painted. LAND FormIDs are
unchanged: heights are still built with the cells, and only the texture layers are added
at the end. At most 6 layers per quadrant.

**The two texture packs are not bundled.** They replace vanilla textures, so they are
optional, and Barry installs them in MO2:
- **Whiterun Mossy Wet Stonefloor – Grey 2k** (Nexus 99294) supplies
  `wrstonefloor01/02` with `_n` and `_p`. Without it the cobbles use vanilla
  `WRStoneFloor01`.
- **Terrain Parallax 1.5 – 4K2K** (Nexus 54860) supplies about 50 landscape textures with
  `_p` maps.

The modlist has Community Shaders with **Terrain Helper** (its DLL looks up terrain
parallax maps), Terrain Blending and Terrain Variation. The fair's texture sets name their
`_p` maps explicitly. Whether parallax shows in game is **unverified**.

### Holidays as a master

`Holidays.esp` (Nexus 1533, v2.20 Alpha 1, installed in MO2) is now a **master** of
SkyrimFair.esp. The fair places its records (rope colourways, lanterns, apple basket, mead
crate, platter, sign stand) and ships none of its files. In the "Still in Skyrim Plus"
profile it is enabled and loads at 141, before SkyrimFair.esp at 161. The generator now
writes against an explicit load order (Skyrim, Update, the DLCs, Holidays) instead of the
stock game's plugin list, which does not know Holidays.

### Verification

- Generator run twice: identical SHA256 `60d0a724b1b08410...` (560,509 bytes). **Deployed
  byte-identical**, with `meshes\SkyrimFair\Props\` (147) and `TowerLantern.nif`.
- Masters read back: `Skyrim.esm, Holidays.esp`.
- Ground texture sets read back with diffuse, normal and height paths; grass kept on the
  grass layer.
- **0** references in the archery lines of fire. **0** low references in the gate-to-stage
  sightline band (only rope lines and lanterns overhead).
- Plan drawn from the written ESP (`docs/images/lively_plan.png`: ground colour by texture,
  blue keepers, red visitors, yellow archers, black poles, pink ropes, orange lights): cobbled
  avenue, worn market, patchy field, lines across the lanes, visitors at the hotspots.
- Stall renders from the customer's side (`docs/images/lively_stalls.png`): goods on the counters, the raised
  `WRMarketStand01` table, sacks and crates spilling in front, the spit roast.

### Known and not done

- **Performance**: 97 actors in the fair (59 keepers, 34 visitors, 4 archers), 18 real
  lights, and about 1,900 small statics. `fairWorld.crowds.enabled` turns the visitors off.
- **Not seen in game**: counter heights, small item orientation (tilted leeks, lying
  swords and bows, the lute), hang heights, and how the cobble texture tiles at terrain
  scale (it's an architecture texture on landscape).
- Skinned props (bows, books, hanging game) use fallback bounds and may sit oddly.
- Holidays is Alpha; if a Holidays update renumbers records, the fair's references break.
  Pin the version.
- No navmesh yet; everything was placed with it in mind (poles at corridor edges, nothing
  in the lines of fire or the sightline).

### Test

1. **Walk the avenue** from the gate to the stage. Is it easy? Do the cobbles read as
   cobbles, and how big do the stones look on terrain?
2. **Stalls**: can you tell what each sells from the path? Are the goods on the counters
   (not floating or sunk)? Check the raised Imperial/Stormcloak tables and the keepers
   behind them.
3. **Festival lines**: height, sag and colours; the rival Imperial/Stormcloak crossing.
4. **Archery**: booth, prize table, benches. Do the archers now shoot? (The stand-marker
   fix is in this build.)
5. **Crowds**: does the sweetroll queue read as the most popular thing at the fair?
6. **Ground**: install the two texture packs in MO2 first, then check the worn patches,
   the parallax, and whether anything looks too uniform.
7. **Frame rate** in the market at night.

## Previous pass: tower fixes and working archery (2026-09-22, late)

Barry tested the towers and the range: "The lantern doesn't glow nor does it sit in the
tower top, can we use fireFX to just make the lanterns glow?", "The flags on the towers
seem to be going inside of the tower rather than hanging off naturally", and "the archery
doesn't seem to be doing its animation... people stood there with bows".

**1. The lantern fell to the ground.** Vanilla `CandleLanternwithCandle01` is havok
clutter (a `bhkRigidBody`, BSX `0x9b`). Spawned on the deck, inside the tower's collision,
it was pushed out and dropped. **Fix**: `tools/make_tower_lantern.py` extracts it from
the BSA and unhooks its physics (the one collision link set to none; BSX havok, complex
and articulated bits cleared, to `0x11`, which keeps the flicker animation and the candle
add-on). The result is `meshes\SkyrimFair\TowerLantern.nif`, record
`SkyrimFairTowerLantern`. It's a modified Bethesda mesh, so it is **generated, git-ignored
and shipped only in the built mod**. **Glow**: inside the lantern, a flame
(`FXfireWithEmbersLight` `033DA9` at 0.3), a soft halo round it (`FXGlowFillRoundMid`
`02EB0E`, the vanilla ambient glow sphere, placed 498 times in vanilla, at 0.45, about
115 across), and the `WRFireLightNS` light as before.

**2. The banners leaned into the tower.** Every one of the 33 vanilla placements of
`CityBannerWhiterun01InsideTall` is tilted 23 degrees back about the banner's local Y (Dragonsreach:
`(0,-23,0)`, `(0,23,180)`, `(-23,0,90)`, `(23,0,270)`), because the cloth leans in the
mesh. The towers now apply the same tilt (`bannerTiltDegrees`), computed per yaw; the
rotations written match those four vanilla combinations exactly. The banners hang 16
outside the deck edge (was 8), clear of the cross-braces.

**3. The archers never shot.** In Castle Dour **every target a trainee links to is a
persistent reference** (`0B2FE5`, `0B2FE7`; the one temporary target, `0B2FE6`, is linked
to nobody). Ours were temporary, and in a different cell from the archers (targets at x
-650 in cell -1, archers at x 300 in cell 0), so the `TrainingTarget` link had nothing to
resolve. **Fix**: the four `ArcheryTarget` references now go in the worldspace's
persistent cell with the persistent flag (`0x400`), and move in to x -450, so each archer
is **750** from its target (Solitude's trainees shoot from about 300-850). Read back from
the ESP: 4 persistent targets, 0 temporary, each archer linked to its own.

**Verification**: generator run twice, identical SHA256 `ef7aac9985a79042...` (423,273
bytes). **Deployed byte-identical**, with `meshes\SkyrimFair\TowerLantern.nif`.

**Test**:
- The lanterns stand on the deck, glow, and have a small flame (tune `fireScale`,
  `glowScale` if too big or small).
- Banners hang straight down the tower faces.
- The archers draw and shoot. A save made before this build may still have the old
  targets baked in; if they stand idle, try `coc` from a fresh cell or a save from
  before your first visit to the fair.

## Previous pass: festival light towers at the gate and the stage (2026-09-22, late)

Barry: "a new asset... barry_scaffold... festival watchtowers more than guard
watchtowers... two near the main gate and two near the stage... large whiterun banners
hanging from it", then "adding a light to the towers... the flame podium you used on the
stage, or just make a large lantern... where the guards normally stand".

**The tower** (`meshes\barry_scaffold\scaffold.nif`, BS 100, 2,552 triangles, box
collision; `scaffold_d.dds` 2048 DXT1, `scaffold_n.dds` a 4x4 flat normal map). Measured
from the mesh: 124 x 129 x 320, deck floor at 215, deck edge boards topping out at 234,
deck edge at +/-59, roof underside 306 over the centre, **ladder on the local -X face**.
Placed at **scale 2**: 640 tall, deck floor 430, edge boards 468, so it looks over the
palisade (350) and the gate (440).

**The light: a large lantern, not a brazier.** At scale 2 there is 182 of headroom
between the deck floor and the wooden roof, and a brazier's flames would lick the roof.
Instead each deck has vanilla `CandleLanternwithCandle01` (`02D847`, the common iron
lantern with a lit candle, placed 1,393 times in vanilla) at **3.5x, about 150 tall**,
standing in the middle of the deck where the guard would, 35 below the roof. Inside it is
**`WRFireLightNS`** (`0BBAE5`), Whiterun's warm street fire light: radius 768, flickering,
no shadows. That makes 4 more real lights, 14 in the fair.

**The banners**: vanilla `CityBannerWhiterun01InsideTall` (`0DEE54`), Dragonsreach's
long Whiterun banner (340 long, measured from its skin data), at 1.25x, about 425 long.
Each hangs from the top of the deck's edge boards, 8 outside the edge, and turned so the
cloth sways away from the tower. There are none on the ladder face or on faces toward the
wall.

| Tower | At | Yaw | Ladder faces | Banners face |
| --- | --- | --- | --- | --- |
| 01 gate west | (1500, -1790) | 0 | west | east (gate), north (fair) |
| 02 gate east | (2600, -1800) | 180 | east | west (gate), north (fair) |
| 03 stage west | (798, 5250) | 0 | west | east (stage), north, south (crowd) |
| 04 stage east | (3298, 5250) | 180 | east | west (stage), north, south (crowd) |

`fairWorld.towers` in the config, built by `FairTowers.cs`. Each tower's footprint plus
60 is a market keep-out; no market placement changed (33 stalls, 301 pieces, same
refusals).

**Clearances** (read from the written ESP): gate towers 40-65 inside the palisade and
out of the gate-to-stage sightline band; tower 02 about 110 from the gate signpost.
Stage towers about 245 from the stage skirt and 90 between a banner's swing and the
nearest stage post.

**Verification**: generator run twice, identical SHA256 `2e01805cfdde37fe...` (422,558
bytes). **Deployed byte-identical**, with `meshes\barry_scaffold\` and
`textures\barry_scaffold\` copied into the MO2 mod folder.

**Test**:
- Do the lanterns stand on the deck floor (not floating or sunk) and glow at night?
- Do the banners hang clear of the tower (no clipping through the edge boards or legs)
  and sway?
- Does the scale look right next to the gate and the stage? The scale, lantern size and
  banner size are one number each in the config.

## Previous pass: no Dawi stall-keepers, and outhouses by the walls (2026-09-22, late)

Barry: "i believe i have a dawi mod installed which has the dawi race. Could we make those
excempt from being vendors... their height breaks with the vendor table", and Astra's SE
conversion of Stroti's outdoor toilet for the outhouse groups asked for earlier.

**Dawi.** The vendors and archers take their faces from the vanilla commoner lists
`LCharBanditMeleeCommonerM` / `F` (`01A319` / `01A31E`). `Dawi_NPC_Encounters.esp`
overrides the male list and adds a seventh entry of its own (`04FF70:Dawi_NPC_Encounters.esp`),
so some male stall-keepers came out Dawi. The generator now copies each vanilla list
**from Skyrim.esm** into a leveled list of the fair's own, which no other mod edits:
- `SkyrimFairFacesLCharBanditMeleeCommonerM` (`000D49`): 6 entries, `EncBandit01`-`06Melee1HImperialM`, all Skyrim.esm.
- `SkyrimFairFacesLCharBanditMeleeCommonerF` (`000D4E`): 6 entries, the female counterparts, all Skyrim.esm.
- All 8 vendor records and both archer records template these lists (checked by reading
  the ESP). Other mods' face changes to those vanilla lists no longer reach the fair.

**Outhouses.** Two new project statics (`fairWorld.projectStatics`, used by modules as
`@EditorID`): `SkyrimFairOuthouse` (`000BF8`) and `SkyrimFairOuthouseDoor` (`000BF9`),
with the door placed shut in its frame. The module `outhouse_row` is three outhouses
side by side, and two `dressing` groups place it:

| Group | Outhouses at | Doors face |
| --- | --- | --- |
| West wall, north of the archery range | (-683, 2970), (-686, 2844), (-686, 2739) | east, into the field |
| East wall, north-east corner | (5332, 3662), (5299, 3767), (5252, 3874) | west-south-west, into the market |

To make room, the archery keep-out is narrowed to y 800-2,000 (the lanes run y 950-1,850),
and the east group is placed north of the East Wall Walk's end with a 900 search radius.

**Assets**: bundled in the deployed mod, but kept out of git because the resource's
permissions say "Do not upload to other sites" (see `CREDITS.md`).

**Verification**:
- Generator run twice: identical SHA256 `e0624b78d352730b...` (420,733 bytes). **Deployed
  byte-identical**, with `meshes\Stroti\` and `textures\Stroti\` copied into the MO2 mod
  folder.
- Market still 33 stalls, 59 stall-keepers, 4 archery lanes. The outhouses sit well
  north of the lines of fire (y 950-1,850).

**Test**:
- Stall-keepers: no Dawi behind any counter (walk the whole market, since faces are
  picked per spawn).
- The outhouses stand level against both walls, doors shut in their frames, and the
  textures load (no purple).

## Previous pass: props to bring the market to life (2026-09-22, night)

Barry: "audit some more props... the east side and the center looks a bit bare". The
audit of vanilla civilian props (Skyrim.esm, ranked by how often vanilla places them) led
to four groups, all approved.

| Group | Vanilla pieces | Where | Placed |
| --- | --- | --- | --- |
| **Lit braziers** | `WHfirebrazier01` + `FXfireWithEmbersHeavy` + **`LightCampFire01`** (`0AF8BA`, r 512, flickering) | down the East Lane's centre between the picnic islands; along the avenue's west edge | 5 + 2 |
| **Cook fires** | `Campfire01LandOff` + `FXfireWithEmbers01_lite` + `Spit01` roasting spit + `LightCampFire01`, two `TreePineForestCutLog01` log seats, hay scatter, firewood | on the field side of the avenue's eating row | 3 |
| **Whiterun banners** | `CivilWarBanner01` pole with `CityBannerWhiterun01` at +395 (the vanilla pairing) | the avenue's west edge | 3 |
| **Rival colours** | the same pole with `CivilWarBannerImp01` / `CivilWarBannerStorm01` | beside the Imperial and Stormcloak stalls (new `market.themeDressing`) | 2 |
| **Traders' camps** | `NorTentSmall`, `HandCart01`, firewood, hay bale | the market's north-east and south-east corners | 3 |
| **Gate clusters** | `HandCart02`, `HayBale01` + `HayMound01`, crate stacks | either side just inside the gate | 5 |
| **Field fence** | `FenceWoven01` wattle panels | between the avenue and the archery field, x of about 850-1,000, with a gap mid-way to walk through | 14 |
| **Gate sign** | `RoadSignPost` + `RoadSignWhiterun01` (board 230 up on the post, as vanilla mounts them) | inside the gate | 1 |

**10 real lights** (7 braziers, 3 cook fires), kept low for performance.

**How it's placed** (all config, `fairWorld.market`):
- `seating` runs now take several modules at random, a placement chance, and a wall
  margin of their own.
- `dressing` accepts whole modules that **spiral out up to 600 from their point until
  they fit**.
- `themeDressing` adds pieces to every stall of a theme.

Everything uses the stalls' collision rules (lanes, keep-outs, sightline, other pieces).
The archery keep-out now starts at x 700 (behind the archers), which opens the eating
row. **Dropped**: clutter along the stall lines, because the rows are packed too tight
for it (gaps of 20-45); and two camps, which found no room near the wall.

**Verification**:
- Generator run twice: identical SHA256 `cdb73a8a...` (419,242 bytes). **Deployed
  byte-identical.**
- All market and dressing meshes: 0 outside the compound (nearest 107 from the wall
  line); 0 in the crowd square, the stage zone and the gate-to-stage sightline band.
- **0 in the archery lines of fire**. The only pieces in the archery field are the
  range's own hay-bale backstops.
- Previews: `docs/images/market_plan.png`, and `market_views.png` (down the East Lane,
  and across the eating row).

**Test**:
- ~~Do the braziers and cook fires glow at night?~~ **Confirmed by Barry: the braziers glow.**
- Do the rival banners and the Whiterun banners hang right?
- Can you walk through the fence gap to the archery field?
- Frame rate with the 10 lights.

## Archery range on the west field (previous pass; now deployed)

Barry: "use the archery practise from solitude... replace the soldiers with standard
npc's. Have about 3-4 targets". Built as a copy of how **Castle Dour's practice yard**
works, read from Skyrim.esm:
- Solitude's bow trainees run **`GuardSolitudeRangedTrainingPackage`** (`0B4C54`), a
  UseWeapon package with no conditions and no schedule limits.
- Each trainee is linked to one **`ArcheryTarget`** (`066AF6`) by a linked reference
  with keyword **`TrainingTarget`** (`0B4C5A`).

The fair does the same with townsfolk.

| Piece | Value |
| --- | --- |
| Lanes | **4**. Archers on a firing line at x 300, targets at x -650 (950 away), at y 950 / 1,250 / 1,550 / 1,850 |
| Direction | shooting **west**, away from the avenue; the west wall is 500+ beyond the targets. A `HayBale01` backstop stands 140 behind each target |
| Targets | `SkyrimFairArcherTarget01-04`. The face is the mesh's local -X (checked by rendering it), turned to point at its archer (read back: face-to-archer 1.00 on all four) |
| Archers | 2 records, `SkyrimFairArcherMale` / `...Female`: Traits template from the vanilla commoner lists (as for the stall-keepers), `HunterClothesRND`, `HuntingBow`, 100 `IronArrow`, Citizen class, Unaggressive, Protected, and the Solitude package |

Config: `fairWorld.archery` (lanes, pieces, looks). The Activity zone marker (700, 1400)
already faces west.

Generator run twice: identical SHA256 `3535cd98...`. **Deployment pending**: the game
held the file open. Copy `dist/SkyrimFair.esp` once Skyrim is closed.

**Test**: do the four archers draw and shoot at their targets on a loop, with no one
wandering into the line of fire (the stall-keepers are all on the east side)?

**Parked**: outhouses (Toilets resource pack, "Toilet 2"): wooden, corrugated roof,
plinth and steps. They wait for Astra's LE-to-SE conversion; that pack's meshes, and
Stroti's, are all BS 83. Confirm the pack's Nexus permissions before bundling (the
page was not readable from here).

## Aligned rows and eating areas (previous pass; deployed)

From Barry's walk-through:
- "big gaps in the middle of the east stalls and the centre stalls... benches... eating
  areas"
- one stall near the stage "a bit misaligned with the others"

- **Misalignment fixed at the cause.** Back rows were set back to back behind whichever
  front stall they paired with, so their fronts wandered by the sum of two stall depths
  (up to about 200). Every visible row is now **lined up on its own street edge**:
  - Column A's back row is the **west side of the East Lane**, which moved to x 3,780.
  - Column B's back row faces a new **east-wall walkway** (x 5,100).
  - Back-to-back infill is switched off.

  Column A's front row still follows the avenue's gentle approved bend.
- **Eating areas: 9 picnic sets** (new module `picnic`): an `ExteriorWoodenTable01`
  with four `FarmBench01Static`, sometimes a `Lantern` on the table or a barrel at the
  end. They are set along lanes by the new `market.seating`, turned to run with the lane:
  - down the **middle of the East Lane** every 800, as islands with walking room either
    side
  - along the **avenue's open west edge** every 900, facing the food stalls

  Sets that would crowd a passage, a stall or a keep-out are skipped. The archery
  keep-out now starts at x 1,350.
- **Counts**: **33 stalls** (Avenue 9, East Lane 7 + 9, wall walk 8), 200 pieces,
  **59 stall-keepers**.
- Generator run twice: identical SHA256 `24dbe3a2...` (412,046 bytes). **Deployed
  byte-identical** (including the stall-keeper fixes the previous deploy missed).
- Market meshes: 0 outside the compound (nearest 519 from the wall); 0 in the crowd
  square, the stage zone, the archery field and the sightline band.

## Stall-keeper fixes (previous pass; now deployed)

Barry's test: the keepers showed up and "the faces look okay", but paired stalls had one
empty counter, and "they all look ready to wanna punch me".

- **Fists up, found and fixed.** The package I used, `DefaultHoldPositionCurrentLoc64`,
  carries the **WeaponDrawn** flag, so every keeper stood in the unarmed combat stance.
  It is now **`DefaultStayAtEditorLocation`** (`025BFC`), which 20 vanilla NPCs use,
  with no weapon flag. They stay at their spot and idle normally. (Lesson: check a
  vanilla package's flags, not just its name. The hold-position family all draw
  weapons.)
- **One keeper per counter.** Modules now carry `vendorSpots`, a list: `canvas_pair`
  and `canvas_wide` get one per structure, and the `grand_double` two across its double
  stand. **58 keepers** across the 34 stalls, where there were 34.
- Generator run twice: identical SHA256 `f661bcc4...` (408,300 bytes). Read back: all 8
  vendor records use the new package, and 58 are placed.
- **Deployment pending.** The copy to the MO2 mod folder was refused because the file
  was in use (the game or MO2 still running from Barry's test). The deployed plugin is
  still the previous build; copy `dist/SkyrimFair.esp` once Skyrim is closed.

## Placeholder stall-keepers (previous pass; package and counts superseded above)

Barry: "add NPCs to the stalls, but have them sell nothing for the time being... just to
help me envision it". **34 stall-keepers, one behind each stall's counter, facing the
street.** They are purely visual. They have no merchant setup, dialogue, faction, quest,
script or inventory, and nothing to steal.

| Record | Value |
| --- | --- |
| Vendor NPCs | **8 records** `SkyrimFairVendorMale01-04`, `SkyrimFairVendorFemale01-04`, all named "Fair Trader" |
| Faces | Traits template only, from vanilla `LCharBanditMeleeCommonerM` (`01A319`) / `...F` (`01A31E`), the "commoner" leveled lists (Imperial, Nord, Breton faces with vanilla FaceGen). Each reference rolls its own face, so there are no generated faces and no dark-face bug. Nothing else comes from the template: no bandit gear, factions or AI |
| Clothes | one record per vanilla outfit: `MerchantClothesOutfit01NoHat`, `FarmClothesRandom`, `FineClothesOutfit01`, `BarkeepClothes01` |
| Class, AI | `Citizen`; Unaggressive, Cowardly, helps nobody; **Protected** (only the player can kill them) |
| Package | `DefaultHoldPositionCurrentLoc64` (`0A6854`): stays within 64 of its spot, so **no navmesh is needed** |
| Placement | 34 ACHR, one per stall, at each stall type's `vendorSpot` (behind the counter), facing the stall's front. They are 200-240 behind each stall's front marker |

Config: `fairWorld.vendors` (looks, outfits, class, package) and each market module's
`vendorSpot`. Set `vendors.enabled` false to remove them all.

**Verification**:
- Generator run twice: identical SHA256 `0aa38d0d...` (406,764 bytes). Deployed
  byte-identical.
- Read back: 8 NPC records exactly as tabled, 34 placed, 0 filed in the wrong cell, 0
  merchant containers.
- **Not verified in game**:
  - how the faces and outfits look
  - whether any keeper spawns clipping a counter or stall post
  - that hold-position keeps them put without a navmesh

**What Barry should test**:
1. Walk the avenue and the East Lane: is there a keeper at every counter, facing you?
2. Any odd faces, or anyone stuck inside a stall or wandering off?
3. Talking to one should give only generic greetings, with no buy or sell option.

## East market, west games field (previous pass)

Still current; the vendor pass above adds NPCs only.

Barry: "could we have just an 'east market' and have the west for the archery and
anything else?" The market now fills the **east half** as two market streets built
from stall islands. The **west half is open for archery and games**.

```
            west wall                                   east wall
   ┌───────────────────────── crowd square ─── stage ────────────────┐
   │                          │ A-front ▌A-back │ East │B-front ▌B-back│
   │  ARCHERY / GAMES FIELD   │ (faces  ▌(faces │ Lane │(faces  ▌(faces│
   │  (open; shoots west)     │ avenue) ▌ lane) │      │ lane)  ▌ wall)│
   │        avenue (west side open) ──▶ stage │      │        ▌      │
   └──────────── gate ───────────────────────────────────────────────┘
```

| Column | Front row | Back row (back to back) | Total |
| --- | --- | --- | --- |
| **A**, along the avenue's east side | 9, facing the avenue | 6, facing the East Lane | 15 + the Imperial stall = **16** |
| **B**, along the East Lane's far side | 9, facing the lane | 9, facing the east-wall walkway | **18** |
| | | | **34 stalls, 141 pieces** |

- **Zones swapped**: the `Market` zone (worn-earth ground) now takes the east polygon,
  marker (3650, 1400). The `Activity` zone (grass-free ground) takes the west polygon,
  marker (700, 1400) **facing west**, so a future archery line shoots at the west wall,
  away from the avenue. Their ground paint follows.
- **Lanes**:
  - the avenue, lined on its east side only
  - the **East Lane**, x 3,700, y -800..3,700, 600 wide, lined on its far side
  - two stall-free openings: an **entrance passage** leaving the forecourt diagonally
    to the lane's south end, and a **mid crossing** at y 1,000
  - the crowd square joins both streets at the north
- **Keep-outs**: the archery range is now the west field (x < 1,500). The crowd square,
  the stage and the gate-to-stage sightline band are unchanged.
- **Stalls only** (Whiterun double and gabled stands, Windhelm canvas pairs, the wide
  mixed stall). Back rows now take the stall type closest in frontage to the one in
  front, so the islands line up.
- **Signature slots**: `SkyrimFairStallImperial01` and `...Stormcloak01` face each
  other across the East Lane, and `...Sweetroll01` is at the lane's north end by the
  crowd square. Every stall has a themed shell marker: food along the avenue,
  specialists and faction trades on the East Lane, trades on the back rows.
- **Count**: 34, against 40 when both avenue sides were lined, because both columns now
  share the east half at full stall size.

Previews: `docs/images/market_plan.png`, and `docs/images/market_views.png` (from the
gate; down the East Lane).

### Verification

- Generator run twice: identical SHA256 `b921f86d...` (402,644 bytes). Deployed
  byte-identical.
- Market meshes: 0 outside the compound (nearest 419 from the wall line); 0 in the
  crowd square, the stage zone, the west activity field or archery range, and the
  gate-to-stage sightline band.

### What Barry should test

1. From the gate: the market down the right, the open field on the left, the stage ahead.
2. The entrance passage and the mid crossing into the East Lane: are they easy to find?
3. Walk the East Lane: does it feel like a second market street?
4. Is the west field the right size and shape for archery and games?

### Stall kit (all vanilla Skyrim.esm, referenced, nothing copied)

Seven modules, each a main structure plus display, storage and dressing. Optional
pieces are dropped from about a third of copies, and every copy is randomly mirrored and
jittered (±5 in position, ±3 degrees), so no two stalls match:

| Module | Main structure | Frontage x depth | Dressing |
| --- | --- | --- | --- |
| `grand_double` | `WRMarketStand01` (`05B2A7`), Whiterun's canvas double stand | 560 x 400 | two `WRMarketDisplayShelf01/03`, barrels, stacked crates, banner post |
| `canvas_pair` | `WHMarketStall01` + `02` (`0F3C72/73`), merged canvas stalls | 440 x 300 | long crate, barrel, mead barrel |
| `whiterun_stand` | `WRMarketStand02` (`05B2E2`), gabled timber stall | 360 x 340 | `WRMarketDisplayShelf02` crate display, barrel, mead barrel, crate |
| `canvas_wide` | `WHMarketStall01` + `WRMarketStand02`, a merged mixed stall | 520 x 320 | display shelf, crates, barrel |
| `cask_bar` | `BreweryCaskLargeClosed01` (`07F871`) + `WHMarketStall02` | 460 x 360 | mead barrels, `FarmBench01Static` for drinkers |
| `open_trader` | `ExteriorWoodenTable01` (`03DE44`) + display shelf | 320 x 250 | crate, `HayBale01`, barrel, firewood |
| `trestle` | `ExteriorWoodenTable01` | 260 x 200 | crate, barrel; the filler for tight spots only |

Dressing: `Barrel02Static` `10C0E3`, `MeadBarrel01` `01E3A3`, `CrateSmall01-04`,
`CrateSmallLong01` `07A605`, `FirewoodPileMedium01` `0185B5`, and `FarmBannerPost01`
`1083D7`.

**Rejected**:
- `WRMarketStand03`, built for Whiterun's sloping market with 176 of legs below its
  floor
- Riften and Markarth stalls, off-style for a Whiterun fair
- Imperial tents, which read military
- Solitude's small counters, tiny one-table traders
- **Medieval Markets** (below), for now

### Layout machinery (the current config uses the avenue only)

- **Lanes are data.** Each has a centreline, a half-width profile that pinches and
  swells along it, and a gentle meander. The avenue uses the approved avenue line.
- **Stalls face their lane.** They stand behind its widest point across their whole
  frontage, with set-back up to 45 and turns up to ±7 degrees, and gaps of 10-70 with
  an occasional 280-420 browsing pocket.
- **A stall is placed only if it keeps clear of everything:**
  - every lane's corridor (modelled as square-cut slabs, so a pocket does not spill
    over the pinch next to it)
  - the wall (220), the crowd square, the stage, the entrance forecourt, the archery
    range (x > 3,250)
  - a 260-wide **gate-to-stage sightline band**
  - every other stall

  Refusals are what open the junction mouths and pockets. A tight spot tries smaller
  modules before giving up.
- **Priority order**: signature stalls, then the avenue, the branches, the back lanes,
  the outer lanes, and finally back-to-back infill behind the lane rows.
- **Deterministic**: fixed integer hashes throughout.
- Tuning is all in `fair.config.json`. `SKYRIMFAIR_TRACE=1` prints every refusal and
  its reason.

### Medieval Markets (external zip) audit

`external/Medieval Markets ESL-161479-1-1-1-1761815114.7z`: 37 meshes, an ESP, and
textures.

- **The ESP rearranges the vanilla city markets** (Riften, Whiterun and others), so the
  mod as a dependency would change players' cities. It is not a clean resource.
- **JJerem's stall pieces use vanilla textures only**:
  - `NewMarketStall01`: a four-post cloth-roofed stall with a front swag and side
    curtain, using vanilla `wrmarketstallroof01.dds`
  - `MarketStandLong01/02`: A-frame produce shelving
  - `SlantedShelf01`, `ShortFarmTable`, `fenceWovenTall01`
  - six weapon racks and an arrow pot
- **Produce crates and baskets** (17 + 7) use PraedythXVI's *Fruits and Veggies*
  textures, and some baskets Palpable Baskets (gooball60). They need those authors'
  permissions.
- `0TentNordLarge01` is an **unconverted Oldrim (LE, BS 83) mesh** with COTN
  (JPSteel2) textures. It is not usable as is.
- **Not used yet.** JJerem credits SMIM (Brumbek) as a source, and the files do not say
  which pieces derive from it. Before bundling, confirm with the mod page or JJerem
  which of the stall pieces above are wholly JJerem's; then they can be bundled with
  credit (no paid use). The cloth stall and the weapon racks would be the most useful:
  cloth variety, and the smithing rows.

## Condensed compound, stage fire pits, closed gate, open tundra (previous pass)

Still current. The market pass above adds records only.

Barry reviewed the stage and the area in game ("looks really good... the stage looks
great"), then asked for four changes. This pass makes those four changes and nothing else.

| Change | What was done |
| --- | --- |
| **Condense by about 30%** | Read as **30% less area**: the whole plan (perimeter, gate, avenue, every zone polygon and marker) was scaled by **0.837** about the centre of cell 0,0 (2048, 2048). The gate-to-stage axis stays on x 2048, and `cow SkyrimFairWorld 0 0` still lands mid-compound. The numbers were rewritten in `fair.config.json` (`scratchpad` tool `condense.py`), so the config stays the source of truth. If "30%" meant each dimension, rerun it with 0.7/0.837 on the new numbers. |
| **Fire pits on the stage** | **4 `WHfirebrazier01`** (`093A89`, the Windhelm fire basket on a stand) **with `FXfireWithEmbersHeavy`** (`033DA4`) above each. It is the same pair, at the same fire offset (-6, -2, +92), as Barry's braziers at the Tamriel stair foot. They stand on the deck at its front corners (u ±760, v 390) and rear corners (u ±760, v -380), with feet on the planks. They are in `fairWorld.stage.braziers`. |
| **Closed gate** | `SkyrimFairPalisadeGate` (`000B10`) now uses **`barry_palisades\viking_palisade_gate_closed.nif`**. It has the same 181 x 175 footprint and the same `viking_palisade_gate` textures. Deployed alongside the open version (SHA256 `329a9253...`). No texture changes. |
| **Fewer trees (Whiterun tundra, not forest)** | Forest `peakDensity` 0.8 -> **0.28**, `clearingThreshold` 0.34 -> **0.46** (bigger clearings): **99 trees** (was 518), in scattered stands. The mountains are unchanged. |

### Knock-on changes

- **Compound**: now **7,157 x 8,370** (X -1,384..5,773, Y -2,011..6,359). It has **89**
  palisade panels (was 106). The gate is at (2048, -1991).
- **Terrain**: the flat area and painted zones follow the smaller outline, so the LAND
  records changed. The terrain strategy is unchanged.
- **The stage keeps its approved size.** It needed `forwardOffset` 160 -> **270** so its
  rear timbers clear the nearer north wall (nearest above-ground stage geometry is now
  114 from the wall line). The deck centre is now (2048, 5544), with the step foot at
  (2048, 4538).
- **The audience area in front of the stage is now about 560 deep** (the step foot back
  to the crowd square's south edge, y 3973), where it was about 1,000. That is the cost
  of condensing round a stage that did not shrink. The Crowd marker moved from on the
  steps to (2048, 4290), in the audience space.

### Verification

- Build clean. Generator run twice: identical SHA256 **`092515db...`** (390,425 bytes).
- **Wall closure** re-proved on the new outline:
  - 3,235 points every 8 units all lie inside a wall or gate footprint.
  - 20,160 sightline rays from seven interior points (moved to the condensed layout)
    all cross a wall or gate.
  - The panels either side of the gate tuck 25 and 36 units into it.
- **Stage**: 0 mesh vertices outside the compound; nearest to the wall line 114.
- ESP and `viking_palisade_gate_closed.nif` deployed byte-identical.
- **Not verified in game**: the closed gate model, the braziers on the deck, and the
  tighter crowd square.

### What Barry should test

1. The closed gate, from inside and outside.
2. The braziers on the stage: lit, feet on the planks, not in the performers' way.
3. Is the condensed compound the right size, and is ~560 in front of the stage enough
   audience room?
4. Does the thinner tree cover read as Whiterun tundra?

## Main stage architecture (previous pass; approved by Barry)

Still current, except the stage now stands at (2048, 5544) after the condense, with `forwardOffset` 270, and carries four braziers.

The brief: an open, handmade, temporary Nordic timber pavilion at the north end of the
avenue, facing the gate, built from vanilla pieces. Barry's concept image was used as
loose art direction ("take the concept with a pinch of salt"). **Nothing else moved**:
read back against the mountain build (`b77160ca...`), 835 of 836 existing non-backdrop
records are identical. The one change is intended: `SkyrimFairWorldStageMarker` now
stands on the deck, at Z 134. The 133 stage references are new, and the trees and
mountains are unchanged in content, only renumbered. No NPCs, music, animation,
navmesh, stalls or dressing.

Preview renders from the written plugin (every placed mesh, rotated as the engine
does): `docs/images/stage_preview_views.png` (from the gate, and from the crowd square,
both at eye height 120), `stage_preview_side.png` and `stage_preview_plan.png`. The red
boxes are 128-tall figures for scale.

### Placement, derived from the config

- **Anchor**: the configured `Stage` zone's marker, (2048, 6548), heading **180**
  (south). The stage faces the marker's heading. The deck centre is the marker moved
  `forwardOffset` 160 toward the audience, to **(2048, 6388)**, which keeps the rear
  timbers clear of the wall.
- **On the sightline**: the main gate at (2048, -2777) already faces the Stage marker,
  so the deck, steps and frame are centred on **x 2048**, the gate-to-stage axis. No
  post, beam or clutter stands on that axis in front of the deck; the steps are on it by
  design.
- `fairWorld.stage` holds everything in stage-local coordinates (`u` across, `v`
  toward the audience), so the stage follows its zone if the zone ever moves. The
  marker must face along a world axis, and the generator refuses otherwise (see the
  rotation note below).

### Dimensions

| Part | Value |
| --- | --- |
| Deck | **1,750 wide x 912 deep**, top **134** above the crowd square (about head height), 7 x 4 porch sections |
| Steps | one central flight, **744 wide**, 5 treads, **22.3 risers**, 110 treads, **550 run** (about 14 degrees), foot at (2048, 5382) |
| Frame | posts **700** tall; header beams at 640 and 575, rafters at 690; overall about 1,900 x 1,560 on plan, **713** high |
| Audience space | the crowd square in front, from the step foot (y 5382) back to about 4,350: roughly 1,000 deep, **empty** |

### Vanilla assets used (all Skyrim.esm, referenced, nothing copied)

| Role | STAT | FormID | Count | Scale | Collision |
| --- | --- | --- | --- | --- | --- |
| Deck | `Walkway01` (farmhouse porch: plank deck on corner posts with knee braces) | `01C570` | 28 | 1.0 | compressed mesh, unscaled |
| Steps | `StockadeScaffoldTop0Sided01` (camp scaffold plank top) | `0533C0` | 15 | 1.0 | **box** |
| Risers | `StockadeWoodplanks01` | `0533D5` | 15 | 1.0 | compressed mesh |
| Deck skirt | `StockadeWoodplanks04` (250 x 143 plank panel) | `0533D8` | 22 | 1.0 | compressed mesh |
| Posts, beams, rafters, braces | `StockadeWoodbeam01` (bark log, 306 long) | `0533D0` | 6 + 20 + 21 + 6 | 1.55-2.34 | convex hull |

**133 references in total.**

- **Deck**: tiled at **250 x 228**, the size of the plank surface measured from the mesh.
  The object bounds say 272 x 256, which includes post ends; tiled at that size, the
  preview showed a slit at every joint. The porch's own posts carry it down to the
  ground, with a plank skirt on the front, sides and back, left open behind the steps.
- **Steps**: each tread rests on the one below. A board under each front edge closes
  the riser. There are no railings.
- **Frame**:
  - chunky round-log posts at the front corners, with four more along the rear
  - a doubled header at the front and a top beam and rail at the rear
  - side beams, and seven log rafters as a slatted partial canopy
  - the rear bays X-braced as a light timber backing
  - the sides and front left open
- **Handmade**: every log wanders up to 5 in height, 1 degree in yaw and 3% in size.
  Posts lean up to 0.8 degrees and sink up to 12, all from fixed integer hashes.

### Rejected, and why

| Candidate | Why not |
| --- | --- |
| `WalkwayStairs1/3/4/8/15` (farmhouse porch stairs) | steep narrow flights with newel posts, and `WalkwayREnd01` has a rope rail; the brief asks for broad and welcoming, no rails |
| `StockadeScaffoldStairs01` | 45 degrees with open ladder treads: defensive, not welcoming |
| `ShipStairs01` | small, awkward profile |
| `StockadeScaffoldBase*` as the deck | 192 tall, wants sinking, and reads as military scaffolding; the farmhouse porch reads rustic and civilian |
| Stepping with `Walkway01` pieces | their corner posts would stand in the treads, in the walking line |
| `ShackRoofMid01` / `Side01` as a canopy | **single-sided** planes: invisible from below, where the audience stands |
| `OrcAwning*` | bone and hide: reads orcish |
| `LargeNordicTent01`, `LargeImperialTent01` | a closed hide mound, and an Imperial military tent with the dragon emblem |
| `SMarketStallTop`, `MrkMarketStallRoof01` | a wooden slat pergola; a roof built onto its own small stall frame |
| Market-stall canvas (`whmarketstallroof01.dds`, the concept's cream-and-grey stripe) | **exists only baked into whole Windhelm and Riften stall meshes**, with their counters and frames |
| `StockadeFreewallBeam01` as posts | compressed-mesh collision, which the project knows scales unreliably; the log's convex hull scales |

**Cloth canopy: open question for Barry.** Vanilla has no standalone cloth awning, so
this pass uses the brief's "timber and/or cloth" allowance: log rafters give the
silhouette and leave the stage open. A true cloth awning like the concept's needs a
small project-owned mesh, either a draped sheet or a pair of swags, textured with
vanilla `whmarketstallroof01.dds` by path, not copied. The brief said not to author one
unless vanilla genuinely can't, and the audit above shows that vanilla can't. It is
left for Barry to approve.

### Rotation note (durable)

Skyrim applies a reference's rotations about the world axes, **Z first, then Y, then
X**. The approved pitched stair cheeks depend on it: they are yawed 90 degrees, then
pitched about world X. So every stage log is laid with a yaw, then at most one tilt
about the world axis across it, which is why the stage must face along a world axis.
Braces are crossing pairs about a shared centre, so they come out right whichever sign
a tilt takes. The preview renderer uses the same convention.

### Future attachment points (nothing placed yet)

These are all in `fairWorld.stage`, in stage-local coordinates:

| For | Where |
| --- | --- |
| Front banners and bunting | the header beams at 640 and 575, spanning u ±955 |
| Rear backdrop banners | the rear rail at 380 and the rear top beam at 640 (v -490) |
| Braziers at the front corners | on the deck, inside the posts at u ±905, v 490 |
| Performer markers | on the deck (top 134, 1,750 x 912); the Stage marker already stands on it |

### Collision observations (not yet tested in game)

- **Deck**: `Walkway01` at scale 1, its own compressed mesh, so it should walk like any
  farmhouse porch.
- **Steps**: box colliders at scale 1, with 22-unit risers, well within the player's
  step height.
- **Logs**: convex hulls at scale 1.55-2.34. They should scale with the mesh, but walk
  into a post to confirm.
- **Skirt boards**: compressed mesh, unscaled.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `158d7203...`.
- Read back against `b77160ca...`: 835 of 836 non-backdrop records identical, 1
  intended change (the Stage marker's Z), 133 new stage references. Trees (518) and
  mountains (24, all `0x10400`) are unchanged in content.
- Geometry checked against the written plugin's meshes:
  - 0 stage vertices outside the compound, and the nearest above-ground stage geometry
    is 100 from the wall line
  - 0 stage geometry in the audience area south of the step foot
  - nothing but the steps on the gate-to-stage axis in front of the deck
- Preview renders made from the plugin: the deck is continuous (after the 250 x 228
  fix), the risers are closed, and the frame reads from the gate.
- ESP deployed byte-identical. No new assets this pass.

### What Barry should test in game (this pass)

1. **From the gate** (`player.moveto SkyrimFairWorldEntranceMarker`, face north): do you
   immediately read the timber pavilion at the end of the avenue as the main stage?
2. **Walk up the avenue and the steps**: are the steps broad and easy, is the deck solid,
   and do any tiles show a seam?
3. **Walk round it**: how does it read from the sides and behind, and does anything float
   or poke through?
4. **Collision**: walk into a post and a skirt board.
5. **Scale**: the deck is at head height and the frame about 5.5 people tall. Does it
   hold the crowd square, or want to be bigger or smaller?
6. **The canopy**: are timber rafters enough for now, or do you want the small cloth
   awning mesh (see above)?

## Mountain backdrop and the view through the gate (previous pass; awaiting review)

Still current. The trees and mountains are renumbered by the stage pass, with their content unchanged.

Barry reviewed the palisade in game: "the gate looks incredible". Two things came back.
The gate model **is open** (the asset, not the placement). And through it, and over
the wall, the outside looked bare. So this pass adds vanilla mountains to the backdrop
and closes the forest behind a short clearing in front of the gate. **Nothing else
changed**: read back against the reviewed build (`da78301a...`), all **836** records
that are not trees or mountains are identical. That covers the wall, gate, terrain,
zones, markers, Tamriel and the sandbox.

### How the mountains draw in a world with no LOD

The world has no LOD, so an ordinary reference draws only while its cell is loaded. A
mountain 15,000 away would come and go as the player walked about. Vanilla solves this
in its small worlds, and it was audited before anything was placed. Skuldafn, Sovngarde,
Japhet's Folly and Tamriel itself keep their distant cloud meshes as references in the
worldspace's **persistent cell**, flagged **Persistent + Is Full LOD (`0x10400`)**, and
always inside the world's object bounds (Skuldafn's sit about 200,000 out, within its
bounds of cells 0..65). The mountains are placed exactly that way. Full LOD alone,
without Persistent, is not what vanilla does for distant scenery, so it was not tried.

### The mountains

**24 vanilla snow-covered mountain STATs** (the `_HeavySN` snow-shader variants, to match
the concept's snowy peaks), in two irregular rings round the compound centre (2223, 2198):

| Row | Radius | Count | Pieces |
| --- | --- | --- | --- |
| near | 13,000-15,500 | 11 | ridges and small cliffs: tops 1,970-4,840, 8-18 degrees above a standing player's eye at the centre |
| far | 17,500-20,500 | 13 | peaks and big cliffs: tops 5,420-8,220, 15-24 degrees |

| STAT | FormID | Placed | Scale |
| --- | --- | --- | --- |
| `MountainPeak01_HeavySN` | `043321` | 7 | 0.95-1.13 |
| `MountainCliffSm01_HeavySN` | `050DC0` | 6 | 0.90-1.14 |
| `MountainCliff04_HeavySN` | `027DDC` | 3 | 0.84-1.16 |
| `MountainRidge01_HeavySN` | `05205B` | 3 | 0.94-1.07 |
| `MountainCliff01_HeavySN` | `048DE0` | 2 | 1.18-1.19 |
| `MountainPeak02_HeavySN` | `046031` | 2 | 1.00-1.07 |
| `MountainRidge02_Heavy_SN` | `05304E` | 1 | 1.08 |

- Each row is evenly spaced in angle, with each piece wandering up to 30-35% of the
  spacing. The two rows are offset (start bearings 9 and 23) so near and far pieces do
  not line up, and every piece takes a hashed radius within its band, yaw and scale.
- **Sunk so no base ever shows.** Every mesh's lowest point is placed at Z -1,000, read
  from its own bounds in Skyrim.esm. At mountain distance the wall hides everything
  below about 500 from anywhere inside the compound. When the hill cells beyond are not
  loaded, a mountain therefore still reads as rising from behind the trees, not floating.
- Everything sits inside the world's object bounds (cells -5..6). The generator
  refuses a mountain outside them. The WRLD record is unchanged.
- Due south, straight out through the open gate, is a far `MountainCliff01` at bearing
  186 with near ridges at 164 and 211 either side.

Plan, 2,048 units per character, north up (`o` compound, `n` near row, `M` far row):

```text
    ......................
    ..............M.......
    .........M............
    .......M..............
    .......n...n..........
    ....M..........n......
    ..................M...
    ......................
    .....n................
    .........oooo....n....
    .........oooo.......M.
    ..M.n....oooo.........
    .........oooo.........
    .................n....
    ....n.............M...
    ...M..................
    .......n.......n......
    ...M........n...M.....
    ......................
    ...............M......
    .........M............
    ......................
```

### The view through the gate

The palisade pass kept every tree 1,800 from the gate and out of a 28-degree cone
straight out of it. That was for an approach from outside, which this isolated world
will never have, and it is why the open gate showed bare ground. Now the rules are:

- no tree within 700 of the gate;
- an 18-degree clearing straight out, only for the first 2,200, which reads as a path
  leading away;
- beyond that the forest closes in.

Trees: **518** (was 487). The nearest to the gate is 936 away. 17 now stand in the view
straight out of the gate, the nearest 2,300 out, with the far mountain behind them. The
per-species spread is otherwise as before: `TreePineForest01` 77, `02` 142, `03` 12,
`04` 131, `05` 156.

The gate's own collision is unchanged. Barry's package gives it a closed-gate box, so the
opening should be solid even though it looks open. **Walk into it to confirm.** The
teleport pass will decide what the open gateway shows.

### Records changed

| Type | Change |
| --- | --- |
| REFR, persistent cell | +24 mountains (`0x10400`) |
| REFR, cells | trees 487 -> 518, and all renumbered (they are placed last; nothing references them) |
| Everything else | identical |

### Verification of this pass

- Release build clean; generator run twice, identical SHA256 `b77160ca...`.
- Read back against `da78301a...`: 836 of 836 non-backdrop records identical. The 24
  mountains are all `0x10400`, all vanilla STATs, all in the persistent cell, 0 outside
  the world's bounds, and every mesh bottom is at -1,000. The persistent cell still
  holds the five zone markers.
- ESP deployed byte-identical. No new assets this pass.
- **Not verified in game**: how the mountains look, their draw distance and fog, and
  frame rate.

### What Barry should test in game (this pass)

1. From the centre, turn round: snowy mountains above the treeline all the way round,
   with the far peaks behind the near ridges, and none floating.
2. At the gate, look out through it: a short path, then trees, then mountains.
3. Walk from one end of the compound to the other. The mountains should never pop in
   or out. If they do, the Full LOD approach is not working in this world. Say so.
4. Are they too big, too small or too close? Each row's radius and scales are config
   values.
5. Frame rate.

## Palisade compound, main gate and forest backdrop (previous pass; gate approved in game)

Still current, except the forest's gate clearing, which the mountain pass above shortened (518 trees now, renumbered).

Barry approved the isolated `SkyrimFairWorld` prototype in game. This pass replaces the
temporary banner-pole markers with his custom palisade, adds the Viking gate at the main
entrance, and rings the compound with vanilla conifers. **The approved layout is
unchanged**: the perimeter polygon, gate point, avenue, zones, terrain and ground
painting are the same config values as before. Read back from the written plugin, all
121 LAND records, every cell header, the WRLD header, the persistent cell and the five
zone markers are identical to the approved build. The Tamriel terrace, staircase,
embankment and sandbox are untouched: all 543 records outside the fair world's cells are
identical.

Not built, by instruction: the teleport / "Enter The Wanderer's Fair" interaction, the
Tamriel gate, stalls, NPCs, navmesh, stage systems, music, archery, clutter.

### Custom assets, bundled

Barry's Skyrim-ready conversions, both **CC BY 4.0** (attribution recorded in
`CREDITS.md`). The deployable files live in the repo at `assets/meshes/barry_palisades/`
and `assets/textures/barry_palisades/`, byte-identical to the `Data/` folder of Barry's
package `assets/Skyrim_Palisade_Assets/`. They are deployed to the MO2 mod at the same
relative paths:

| File | SHA256 | Deployed |
| --- | --- | --- |
| `meshes\barry_palisades\palisade.nif` (24,655 bytes) | `d1d92fc9...43b2` | identical |
| `meshes\barry_palisades\viking_palisade_gate.nif` (468,624 bytes) | `7f5bfd7f...8349` | identical |
| `textures\barry_palisades\palisade\material_00_{d,n}.dds` | | identical |
| `textures\barry_palisades\viking_palisade_gate\material_00..23_{d,n}.dds` (48 files) | | identical |

The NIFs reference their textures as `textures\barry_palisades\...`, which resolve to
those files (checked in the NIF strings). Measured, per the package's validation and
build scripts: the palisade is **138.97 wide (local X) x 11.29 deep x 140 tall**, and the
gate is **181.44 x 43.52 x 175.45**. Both have origins at bottom centre, fixed zero-mass
box collision on the Static layer, and no LOD meshes.

### New STAT records

| EditorID | FormID | MODL | OBND |
| --- | --- | --- | --- |
| `SkyrimFairPalisade` | `000B0F` | `barry_palisades\palisade.nif` | -70,-6,0 .. 70,6,140 |
| `SkyrimFairPalisadeGate` | `000B10` | `barry_palisades\viking_palisade_gate.nif` | -91,-22,0 .. 91,22,176 |

Bounds come from the measured sizes (the terrace kit STATs carry none).

### Palisade: placement strategy

- **106 panels** at scale **2.5**, so each is 347 wide and **350 tall** (about 5 m). The
  crest wanders between **316 and 364** because each panel's scale varies by up to 4% and
  it sinks by up to 20. That is well above a standing player's eye (about 120), so the
  empty ground beyond is never visible over the wall.
- Laid **edge by edge along the approved 17-point outline**, each panel turned to its
  edge. Each edge's run carries **48 past both vertices**, so neighbouring edges cross at
  every bend. Each run is filled with the fewest panels that still **overlap by 6%** (21
  units), spaced evenly so there is never a short filler piece.
- **Handmade, not plotted**: from a fixed integer hash (never a string hash), each panel
  wanders up to 1.2 degrees in yaw and 5 units off the line, varies up to ±4% in scale
  and sinks 0-20. About half are turned round so the same face does not repeat along the
  wall.
- **Blended into the gate**: the south edge's run is split at the gate, and the panel
  either side **tucks 31 and 36 units into the gate** (the config asks for 32).
- The wall stands on the painted perimeter strip, so the stony strip reads as its
  trodden footing.

### Main gate

- `SkyrimFairWorldMainGate` (`000B11`), a static `SkyrimFairPalisadeGate`, closed, with no
  script or animation. It stands at the approved gate point **(2048, -2777)**, where the
  avenue starts, at scale **2.5**: 454 wide and **439 tall**, so it rises about 90 above
  the wall.
- **It faces the Stage marker** (heading 0, due north). The view out of the forecourt
  therefore runs straight up the avenue to the stage at (2048, 6548). The avenue's own
  gentle bend stays as approved.
- The model's front and back were not identifiable from the data. If the gate turns out
  to be facing the wrong way, `fairWorld.gatePiece.yawOffsetDegrees: 180` turns it round.
- The teleport and activation come in a later pass.

### Forest backdrop: placement strategy

**487 vanilla trees**, all Skyrim.esm TREE records, scenery only:

| Tree | FormID | Count | Scale | Distance beyond the wall |
| --- | --- | --- | --- | --- |
| `TreePineForest01` (2,498 tall at 1.0) | `01306D` | 71 | 0.80-1.20 | 574-4,608 |
| `TreePineForest02` (2,501) | `018A02` | 132 | 0.80-1.25 | 482-5,188 |
| `TreePineForest04` (2,085 above origin) | `04FBB0` | 121 | 0.80-1.25 | 556-4,826 |
| `TreePineForest05` (1,465) | `051126` | 151 | 0.86-1.35 | 424-5,191 |
| `TreePineForest03` (860, understory, within 1,800 only) | `04B016` | 12 | 0.94-1.48 | 382-1,762 |

These are the Falkreath and Rift pine-forest conifers, and in Tamriel they are placed at
0.35-1.67. Snow and dead variants are excluded.

- **Candidates** come from a 440-unit jittered grid over the band from 320 to 5,200
  beyond the wall. Each is kept by chance against a density that starts **sparse by the
  wall**, peaks from 700 to 2,600 out and thins toward 5,200, so the view has layers of
  trunks and canopy.
- **Clumps and clearings**: that density is multiplied by a low-frequency clustering
  field with a 1,700-unit period, and where the field is low there are no trees. The
  result is irregular stands with gaps of sky between them, not a ring. Because the
  ground rises beyond the flat margin, trees further out stand higher, which stacks the
  canopy.
- **Variation per tree**: species by weight, scale within its range, any yaw, and a lean
  of up to 1.5 degrees. Trunks are sunk 24.
- **Gate kept open**: no tree within 1,800 of the gate, and none in a 28-degree cone
  straight out of it, so a future approach from outside stays clear. From inside, the
  gate sightline runs north to the stage and past it into the northern stands.
- Canopies do not overhang the wall. Each species has a minimum distance (320 to 520)
  set to about its canopy radius.

Plan read back from the written plugin, 512 units per character, north up (`#` wall,
`G` gate, `^` a cell holding trees):

```text
                   ^
                 ^^      ^^
                ^  ^ ^ ^  ^ ^^^ ^
                  ^^  ^^^^ ^   ^^
          ^        ^^^^^^ ^^^  ^^^^
           ^       ^^^^^ ^^^^   ^ ^
             ^ ^^  ^   ^^ ^^^^ ^^^ ^
             ^^^^^^^        ^^^ ^ ^^^
            ^^ ^^^ ^         ^^^^ ^  ^^
         ^^^^^^^            ^^^  ^^^^ ^
        ^ ^   ^     #########^ ^ ^^  ^
         ^ ^^^^^  ###       ## ^^^^^^
        ^ ^^^    ##          ## ^^^^^^
           ^^^^^##            ##^^^ ^^ ^ ^
       ^        #              # ^^^ ^
        ^ ^   ^##              ##^^  ^ ^
      ^^^ ^ ^^^ #               #^ ^^^ ^^^
        ^^     ^#              ##^^ ^ ^   ^
        ^   ^ ^ #              # ^   ^ ^^^
       ^    ^ ^^#              # ^^^^^   ^
        ^    ^^ #              #^^ ^ ^^ ^
       ^ ^ ^^^^##              # ^^^   ^^
       ^  ^^^ ^#               # ^^^^^^
       ^^^^^ ^ #               ##^  ^^
             ^  #              # ^^^^^    ^
        ^^^^ ^^^#              #^ ^^^^
      ^  ^^^^ ^^##             #^^^^  ^
         ^^^^^ ^ ##          ### ^^^^
           ^^ ^   ###      ###  ^^^^^
        ^^  ^^^^^ ^ ###G#### ^^^ ^^^ ^^
         ^^^^   ^^ ^^       ^ ^ ^ ^
         ^ ^^  ^ ^^^^      ^^  ^^^^^^
           ^    ^^^^      ^^^  ^ ^  ^^
           ^^   ^ ^^^         ^^ ^^^^ ^
           ^^     ^^ ^       ^^    ^^
            ^      ^^         ^^
            ^   ^^ ^        ^^^
                  ^         ^ ^^
                              ^
```

**Loading.** The world has no LOD, so a tree only draws while its cell is loaded. **197**
of the 487 are in cells -1..1, which are loaded from anywhere in the compound at the
default `uGridsToLoad` of 5. The rest (cells ±2) load as the player nears that side, and
may pop in at the far side of the compound. The wall hides their bases, so what pops is
canopy. The lasting fix is tree LOD for this worldspace (DynDOLOD/xLODGen, which can use
vanilla pine billboards). There are no real distant mountains in this world; the
"mountains" in the gaps are the generated hills beyond the flat margin.

### Records changed

| Type | Change |
| --- | --- |
| STAT | +2 (`SkyrimFairPalisade`, `SkyrimFairPalisadeGate`) |
| REFR | -33 `FarmBannerPost01` scale posts; +106 palisade panels, +1 gate, +487 trees. SkyrimFairWorld now holds 599 references: 5 zone markers + 594 in its cells |
| CELL, LAND, WRLD, CLMT | content unchanged |

**One-time FormID shift.** The 121 exterior CELL and LAND records are now allocated
straight after the zone markers and before anything placed in them. They moved down by
33 FormIDs, the slots the posts had used, and their content is identical. From now on,
changing the wall or forest never moves them. Nothing references them. The WRLD
(`000A16`), climate (`000A15`), persistent cell (`000A17`) and zone markers
(`000A18`-`000A1C`) keep their IDs.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `da78301a...`.
- The previous approved build (`8b7eb313...`, the deployed copy) and the new one were
  read side by side with Mutagen:
  - **543 of 543** records outside the fair world's cells are identical (Tamriel,
    sandbox, kit STATs, climate, WRLD headers).
  - All 121 LAND records are identical, as are the cell headers apart from FormID, the
    persistent cell and the markers.
- **Wall closure**:
  - 3,863 points every 8 units along the outline all lie inside a wall or gate
    footprint (worst -6.8, i.e. inside).
  - **20,160 sightline rays** from seven interior points (centre, gate, stage, both
    sides, two corners) all cross a wall or gate centreline.
  - A first build had one ray slip between two centrelines at a shallow bend. Panel
    thickness blocked it, but the corner carry-over went from 24 to 48 so it no longer
    depends on thickness.
- **Usable space kept**: no wall or gate footprint enters a zone. The nearest is the
  gate's inner face meeting the entrance forecourt's paint, by design. 0 references
  filed in the wrong cell. 0 banner posts left.
- **Trees**:
  - the nearest is 382 outside the wall line, and the nearest to the gate is 1,862
  - 0 in the approach cone, and the closest pair is 174 apart
  - every tree is a TREE record in Skyrim.esm
- **Deployment**: the ESP is byte-identical in the mod folder, and so are all 52 asset
  files (2 NIF, 50 DDS).
- **Not verified in game**: how it looks, the gate's facing, collision against the wall,
  and performance with 487 full trees.

### What Barry should test in game (this pass)

1. `cow SkyrimFairWorld 0 0`, then turn round slowly. Check for a continuous wall with no
   gaps at the bends, trees above it on every side, and occasional sky between stands.
2. `player.moveto SkyrimFairWorldEntranceMarker` and look at the gate close up. Is it
   the right way round (if not, it's one config value)? Do the panels either side run
   into its posts cleanly? Then turn north: the view should run up the avenue to the
   stage.
3. **Height and scale**: does a 5 m wall with a 6 m gate feel right beside your
   character? Scale is one number each (`palisade.scale`, `gatePiece.scale`).
4. Walk into the wall and the gate: both should stop you.
5. Walk to each side of the compound and watch the far trees. Say if their popping in
   is distracting.
6. Frame rate with the forest in view.
7. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## The isolated worldspace (previous pass; approved by Barry in game)

Everything in this section still holds, except that the temporary posts are gone (replaced by the palisade above) and the cell and LAND FormIDs moved once (see "Records changed" above).

Barry changed direction: the full Wanderer's Fair will eventually live in its own
isolated outdoor worldspace, a large irregular palisade compound reached through a gate
in Tamriel. This pass builds **only the empty canvas**: a new exterior worldspace with
sky, weather, exterior lighting and generated flat ground, with the broad plan painted
into the ground. It is a **parallel prototype**. The raised Tamriel terrace, the
approved staircase and embankment, and the sandbox cell are untouched: all 541 records
of the previous plugin are present and field-for-field identical in the new one (checked
by reading both files, below).

Not built at that pass, by instruction: the Tamriel gate, the palisade itself, stalls, NPCs, archery,
stage systems, quests, navmesh, music, clutter.

### The worldspace

| Field | Value |
| --- | --- |
| EditorID | `SkyrimFairWorld` |
| FormID | `000A16:SkyrimFair.esp` (in game `xx000A16`, `xx` = Skyrim Fair's load-order index) |
| Name (FULL) | `The Wanderer's Fair` (loading screen / HUD) |
| Entry | **`cow SkyrimFairWorld 0 0`** |
| Exit | `cow Tamriel -2 -4` (the Tamriel fair site), or fast travel |
| Zone jumps | `player.moveto SkyrimFairWorldEntranceMarker` (also `...MarketMarker`, `...ActivityMarker`, `...CrowdMarker`, `...StageMarker`) |
| Parent | Tamriel (`00003C`), **Use Map Data only**, as vanilla sub-worlds do. Nothing else is inherited: not land, LOD, water, climate or image space |
| Flags | `NoLodWater`. Fast travel is allowed so the player can leave from the map |
| Climate | `SkyrimFairWorldClimate` (`000A15`), a copy of `SkyrimClimate` (`000812`: sun, glare, moons, sky model, sunrise 05:30-10:00, sunset 16:00-20:30) with its weather list replaced by `WeatherTundraNoPrecip`'s (`1046C9`): `SkyrimCloudyTU` 45, `SkyrimClearTU` 35, `SkyrimClearTU_A` 10, `SkyrimCloudyTU_A` 10. That is what the Whiterun plains get. Tamriel's own climate lists only `SkyrimCloudy` because Tamriel takes its weather from regions, which do not apply here |
| Lighting | Exterior, driven by the weathers (no LGTM on the world or its cells, same as Tamriel) |
| Water | None. No cell has Has Water; default water height -50,000 as a backstop; no WNAM |
| Land defaults | land 0, water -50,000 |
| Object bounds | cells -5,-5 to 6,6 |
| Music, location, encounter zone | none |
| Persistent cell | `000A17`, grid 0,0, Persistent flag set; holds the five zone markers |

### Cells and terrain

- **121 exterior cells, -5..5 on both axes** (45,056 units a side), each with a
  generated LAND record. Every cell is a new record; none is an override.
- **Terrain strategy: generated from rules, not sculpted.** Ground is exactly flat at
  **Z 0** inside the planned perimeter and for 1,024 units beyond it, so the palisade
  will stand on level ground wherever it is finally drawn. Past that it rises over
  8,192 units into low hills of about 1,536 (max 1,688 with a small deterministic
  undulation), closing the view where there is no wall yet. Undulation comes from an
  integer-hashed value noise, never a string hash, so it regenerates byte-identically.
- LAND carries heights (VHGT) and normals (VNML) computed across cell seams from the
  same function; DATA flags `0x1D`, exactly Sovngarde's; no vertex colours (Sovngarde
  has none either); zlib-compressed like vanilla. Checked by decoding the written
  file: **0 seam mismatches** across all 220 cell edges, **0 non-flat vertices** in the
  2,793 sampled across the compound's core.
- **The plan is painted into the ground** with vanilla landscape textures (referenced,
  never copied), at most 4 alpha layers in any quadrant (vanilla uses up to 5):

| Area | LTEX | Look in game |
| --- | --- | --- |
| Compound ground | `LFieldGrass01` `013428` | Whiterun field grass |
| Beyond the perimeter | `LTundra01` `024E30` | rougher tundra |
| Perimeter line, 256 wide | `LTundraRocks01NoRocks` `06DE8B` | stony strip, broken at the gate |
| Avenue, entrance forecourt, crowd square | `LDirtPath01` `0B424C` | bare path, no grass |
| Market side | `LFieldDirtGrass01` `0134B7` | worn earth |
| Activity side | `LFieldGrass01NoGrass` `024E46` | same field texture, no grass: an open range |
| Stage footprint | `LDirt02` `000C16` | darker bare earth |

### The broad plan

The compound is centred on the middle of cell 0,0 (world 2048, 2048), so `cow
SkyrimFairWorld 0 0` puts the player in the middle of it. The entrance is at the south
and the stage at the north, as in DESIGN.md's layout: **entrance, then a busy central
avenue, then the stage as the anchor**, with the market branching west and activities
east.

| Element | Where | Size |
| --- | --- | --- |
| Planned perimeter | irregular 17-point polygon, X -2,052..6,498, Y -2,802..7,198 | **8,550 x 10,000** (about 122 m x 143 m), roughly 3x the Tamriel terrace's span each way |
| Main gate | south wall at (2048, -2777), 640 wide | |
| Entrance forecourt | just inside the gate, widening to about 2,000 | |
| Central avenue | (2048,-2752) to (1898,48) to (2198,2748) to (2048,4748), a slight handmade bend | 800 wide, 7,500 long |
| Crowd square | north end, (2048, 5348) | about 5,300 x 2,000 |
| Stage | backs onto the north wall, faces south, (2048, 6548) | 2,150 x 600 footprint |
| Market | west of the avenue, (148, 1748) | about 2,500 x 5,500 |
| Activity / archery | east of the avenue, marker (3548, 1248) facing east, so an archery line shoots toward the east wall | about 3,000 x 5,600 |

Generator plan view, 512 units per character, north up (`#` perimeter strip, `E`
entrance, `=` avenue, `M` market, `A` activity, `C` crowd, `S` stage):

```text
           #######
        ###SSSSS..##
       ##.CCCCCCCC.##
      ##.CCCCCCCCC..##
      #..CCCCCCCCC...#
     #...CCCCCCCCC...#
      #..MMM.=.AAAAAA.#
      #.MMMM.=.AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     ##MMMMM==.AAAAAA#
      #.MMMM==.AAAAAA#
      #...MM.=.AAAAA.#
       #.....=..AAA.##
        ##..EEE...##
         ####EE###
              #
```

**Why the compound fits in cells -1..1.** The world has no LOD, so only the loaded grid
of cells renders (5 x 5 at the default `uGridsToLoad`). With the whole compound inside
a 3 x 3 block of cells, every cell of it is loaded from anywhere inside it, so the
future palisade will always draw and nothing beyond it needs to. Growing the compound
past that block means the far wall can drop out of view; it would then need the walls
flagged Full LOD, or generated LOD.

### Temporary scale markers

- The 33 temporary `FarmBannerPost01` scale posts that stood on the perimeter line
  were **removed in the palisade pass**. The wall now stands on that line.
- **Five persistent `XMarkerHeading`** (`000034`, invisible in game) with EditorIDs,
  for `player.moveto` and for the Creation Kit:

| EditorID | FormID | Position | Faces |
| --- | --- | --- | --- |
| `SkyrimFairWorldEntranceMarker` | `000A1A` | 2048, -2502 | north, up the avenue |
| `SkyrimFairWorldMarketMarker` | `000A18` | 148, 1748 | east, toward the avenue |
| `SkyrimFairWorldActivityMarker` | `000A19` | 3548, 1248 | east, down the range |
| `SkyrimFairWorldCrowdMarker` | `000A1B` | 2048, 5348 | north, at the stage |
| `SkyrimFairWorldStageMarker` | `000A1C` | 2048, 6548 | south, at the crowd |

### Records added (nothing removed or newly overridden)

| Type | Count |
| --- | --- |
| WRLD | 1 (`SkyrimFairWorld`) |
| CLMT | 1 (`SkyrimFairWorldClimate`) |
| CELL | 122 (121 exterior + the persistent cell) |
| LAND | 121 |
| REFR | 38 at that pass (33 posts + 5 markers); the posts are gone now, see the palisade pass |

Built last in the generator, so it only appends FormIDs: every Tamriel and sandbox
record keeps its previous ID. The exterior block / sub-block grouping was moved into a
shared `ExteriorCellGrid` used by both worldspaces; the Tamriel output is unchanged by
it (identical records, below).

### Verification of this pass

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 `8b7eb313...`.
- Baseline comparison: the previous audited build (`2eaeeeba...`, regenerated from the
  pre-pass tree first and matching this file's old hash) and the new build read side by
  side with Mutagen. **541 of 541 records identical** (WRLD and CELL compared on their
  own fields, every other record in full), 0 missing, 0 changed. Tamriel still has 6
  cells.
- New world decoded from the written ESP: WRLD fields as tabled; climate carries the
  four tundra weathers; 121 cells, 0 with water; 33 posts; five markers with the
  Persistent flag; heights 0..1,688; 0 seam mismatches; compound core flat; LAND
  `0,0` flags `0x1D`, compressed, flat normal `(0, 0, 127)`, two alpha layers per
  quadrant.
- **Not verified: loading in game.** Nothing here has been run in Skyrim. `cow
  SkyrimFairWorld 0 0` needs Barry's test.

### Technical limitations found

- **No LOD** of any kind (terrain, object, tree). Beyond the loaded cells the world
  is empty sky. Handled by the sizing above. xLODGen / DynDOLOD could generate LOD
  for this world later if a wider view is ever wanted.
- **No OFST offset table on the WRLD and no MHDT max-height data on its cells.** The
  Creation Kit normally writes both. Mutagen does not generate them. Vanilla has
  precedent for cells with no MHDT (`CWSiegeTestWorld`). Whether a missing OFST
  matters is **not yet confirmed in game**. It is the first thing to suspect if `cow`
  fails.
- **Grass**: the ground textures carry grass, but the world has no NGIO grass cache.
  If NGIO is set to load grass only from the cache, the fair ground will have no grass
  until the cache is regenerated. The layout reads either way.
- **Map**: the pause-menu map is Tamriel's (Use Map Data). The player's marker there
  means nothing. Fast travel out should work as it does from a city world. Not yet
  tested.
- **No navmesh**, so NPCs cannot path here yet. Nothing needs to until content goes in.
- Weather is climate-driven (no regions), so `fw <weather id>` is how to force one when
  testing.

### What Barry was asked to test (done: approved)

1. `cow SkyrimFairWorld 0 0`: does it load, with sky, sun and weather, and are you
   standing on grass in the middle of the avenue? If it hangs or drops you into a void,
   say so before anything else. That points at the missing OFST / MHDT data.
2. Walk south to the gate (or `player.moveto SkyrimFairWorldEntranceMarker`) and look
   north: the dirt avenue should run up to the big crowd square and the darker stage
   patch, with the worn-earth market on your left and the grass-free range on your right.
3. The posts show the planned wall line. Does the compound feel the right size? It is
   about 122 m x 143 m. Too big and it will feel empty; too small and it won't hold
   everything in DESIGN.md.
4. Look out over the posts: the ground should rise gently into hills and never stop in
   a visible edge.
5. Open the map and fast travel out, to check leaving works.
6. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## Tamriel terrace (unchanged this pass)

### Previous pass: the generator reproduces Barry's completed Creation Kit layout

**Late amendment (2026-09-22 evening):** after seeing the result in game Barry asked for
the big scaled walls to go entirely. At two to three times size the flat cut end of a
wall piece reads as a huge smooth slab, whichever way it is turned. The corner bastions
and the single walls on the short step faces are switched off (`bastions`,
`shortFaceWalls`), and the terrace rows now run through to every corner and along the
short faces too (`cornerStop` 0). No scaled-up Stonewall piece remains in the plugin;
the parapets (0.98), the west field wall (0.9), the cheeks (0.6) and Barry's two 1.06
cheek underpinnings are the only Stonewall01 left. Everything else below still holds.

**Sandbox cell added (2026-09-22, late):** the plugin now carries one interior CELL,
`SkyrimFairSandbox`, so pieces can be looked at in isolation. It is 25 fill bodies and
25 paving caps from the kit in a 5 x 5 square (5,120 units a side, floor at Z 0), a
`COCMarkerHeading` at the centre, and nothing else. Flags are Interior + Show Sky + Use
Sky Lighting (`DATA 0x0181`); every lighting value is inherited from
`DefaultLightingTemplate` (`XCLL` inherit `0x7FF`), the sky comes from
`WeatherTundraNoPrecip` and the image space is `DefaultImageSpaceExterior`. It shares the
foundation's two STAT records, so the STAT count stays at 9. It touches no vanilla record.
The six Tamriel cells and everything in them are unchanged from the row below.

Barry finished the perimeter by hand in the Creation Kit and saved it as
`reference/SkyrimFair.esp` (35,173 bytes, 2026-09-22 17:53). Per his instruction that
plugin is the **design source of truth**, and a **visual/layout reference only**: it is
not authoritative as a plugin and never replaces the generator's output. This pass reads
it, extracts every intentional change, and reproduces the layout from rules so the
generator's plugin lands on the same design.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 419,242 bytes (with dressing) |
| SHA256 | `cdb73a8a7140f6568807158f1b2c51e122fb69227764b1c156ddcd7db3b29fdf` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` (2026-09-22, night) |
| Sandbox cell | `SkyrimFairSandbox` (`0009E1:SkyrimFair.esp`), interior, 5 x 5 kit tiles, `coc SkyrimFairSandbox` in, `cow Tamriel -2 -4` out |
| Isolated worldspace | `SkyrimFairWorld` (`000A16:SkyrimFair.esp`), 121 cells, `cow SkyrimFairWorld 0 0` in; palisade, gate and forest per the palisade pass, mountains per the mountain pass, main stage per the current pass |
| Palisade assets | `meshes\barry_palisades\` (2 NIF) and `textures\barry_palisades\` (50 DDS), deployed byte-identical to `assets/` |
| Kit meshes | unchanged this pass; all 13 deployed NIFs match `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Tamriel cells | `-3,-4`, `-2,-4`, `-1,-4`, `-3,-3`, `-2,-3`, `-1,-3` (all byte-identical to vanilla) |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| **Footprint** | **26 cells** (was 33): see below |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN anywhere; 0 LAND in Tamriel. The 121 LAND records are new records in `SkyrimFairWorld`, not edits to any vanilla landscape |
| Floor material | `road01.dds` worn earth on the paving caps |

## What the reference contains, and what was taken from it

The reference was built from an earlier generated plugin, so a FormID diff against the
current one is meaningless; it was read as a complete layout instead, grouped by base
object, and every hand-placed group was classified.

### Footprint: Barry removed the east column and the south tongue

The reference paves 26 cells. Reconstructed from its floor caps, row by row from the
north: `..####.`, `.#####.`, `.#####.`, `.#####.`, `.#####.`, `.##....`. Against the
old 33-cell mask, column 6 (the far east strip) and the south tongue (row 5 columns 3
to 5, all of row 6) are gone, and the row-0 / column-5 notch is filled. His terrace walls
wrap this new outline (east rows at `X -4544` from row 0 to row 4, south rows at
`Y -14290` for columns 3 to 5 and `Y -14836` for columns 1 to 2), so it is deliberate.
**Adopted**: `footprint` in `fair.config.json` is the 26-cell mask. Because the north
row is now four tiles wide, the entrance column is pinned with `rampAlign: "offset:1"`
so the approved staircase stays at `X -5888` ("centre" would have moved it one tile
east). Three of his floor caps had no body under them (he deleted the 1024 fill they
replaced); the generator emits bodies and caps for all 26 cells.

### Perimeter: per-edge layer stacks, not one prototype

The north band is the prototype measured earlier. The other sides differ, and the
generator now carries **one layer stack per compass edge** (`terraceBand.edges`):

| Edge | Outward `StonewallTerrace01` | Inward `StonewallTerrace01` | `Stonewall01` parapet (0.98) | Extra |
| --- | --- | --- | --- | --- |
| N | origin +117, **on grade** | +111, **115 above** the outward piece: the grass slope | -15, floor -153 | |
| E | +64, fixed **floor -296** | +64, fixed floor -424: a grass plinth under the wall | +24, floor -149 | |
| S | +98, fixed floor -296 | +136, fixed floor -424 | +12, floor -149 | |
| W | +136, fixed floor -296 | +136, fixed floor -424 | +20, floor -149 | lower `Stonewall01` at 0.9 on grade, +397 |

Offsets are plan distances from the paving edge, positive outward, taken from his
pieces. On the shallower east, south and west he set the outward wall at one level
(`Z -5632`, crest at floor -121) and, on east and south, an inward piece 128 below it so
nothing floats. On the west he used a third, lower field wall on grade instead. His west
outward walls have no plinth and float 60 to 200 above ground in places; the generator
gives the west the same plinth as east and south. That is the one place it adds to his
layout rather than copying it, and it is flagged here.

Rows are laid along offsets of the outline with even spacing and flush ends as before,
**stop 96 short of every convex corner** (his rows end 78 to 167 short and the corner
is finished by the bastion), and split either side of the stairs. The outward and
inward rows run under the stair solid to the flank, as his do; the parapet runs to 20
past the cheeks' outer face.

### Short step faces: one big field wall

The three one-tile faces that end at a re-entrant corner (row 0's west face, row 1's
north face at column 1, row 5's east face) carry no rows in the reference. He walled
them with one or two `Stonewall01` or `StonewallEndL01` scaled 2.2 to 2.7 so the crest
reaches the floor. **Adopted** as a rule: a face of one tile touching a re-entrant corner
gets one `Stonewall01` on grade, 24 out, scaled to `(drop + 8) / 175` (2.2 to 2.5 here).

### Corners: bastions of scaled wall-ends, no knolls

The reference has **no** `StonewallTerraceCorner01` and no corner boulders. Every
convex corner is closed by `StonewallEndL01` scaled 2.2 to 3.2 so its crest is at floor
level, forming an L that projects outward along both face lines (north-east,
south-east, south-west), or a single wall where the other leg would have stood in front
of a neighbouring face's band (north-west of the stairs: the north-running wall only;
north-west of row 1: the west-running wall only; south-east of row 5: the south-running
wall only). **Adopted** as a rule: at each convex corner, two `StonewallEndL01` on grade,
16 inside their face line, starting 32 inside the corner and running outward `222 x
scale`, scale `(drop + 8) / 171` clamped 1.5 to 3.3, finished end outward; a leg whose
middle would lie within 560 of another edge's outward strip is not placed. That
reproduces his nine corner walls exactly: 9 generated, 9 in the reference.

### Entrance dressing, reproduced piece for piece

Placed relative to the stair head (`entranceDressing`), so the relationships hold if the
entrance ever moves. All positions equal the reference's to 0.1:

- two `WHfirebrazier01` on the level cheek ends at the foot, with
  `FXfireWithEmbersHeavy` fire above each;
- four `Stonewall01` at 0.6 as low wing walls flanking the foot, with a
  `StonewallEndL01` at 0.6 finishing each pair;
- two `FarmBannerPost01` mid-flight, 318 either side of the centreline, with two
  `MarkarthBanner01` on the west post and a `NightingaleBannerAnim02` on the east;
- three `Stonewall01` (1.06, 1.06, 0.6) under the west cheeks so they do not float.

`MarkarthBanner01` and `NightingaleBannerAnim02` are Barry's picks and are reproduced as
placed; they are Reach-city and Nightingale banners, so they read as placeholders for
fair banners that vanilla does not have.

### Ignored as accidental or noise

- Duplicate references at identical transforms (banner posts x4, several walls x2).
- One `StonewallTerrace01` at `Z 0.0` (`000FCF`), 5,300 units in the air.
- One `TreePineShrub01Snow`, a snow shrub in the tundra.
- One `StonewallTerrace02` (the rubble-apron variant) among 68 `Terrace01`.
- Scale rounding to two decimals and sub-unit position noise.
- A vanilla `LvlAnimalPlainsPrey` actor (`0DC5B7`) marked deleted in the CK file. The
  generator never touches vanilla actors; this stays out.
- Three of the generator's old 0.75-scale course walls he kept; they belong to the
  retired language.

### Kept from the generator although absent or fewer in the reference

- **Structural retaining bodies** (43) and both **entrance retaining wings**. He deleted
  48 of 53 retaining bodies and both wings. They are structural: they close the hollow
  under the floor slab and carry the edge collision. In this layout they are hidden
  behind his walls except for a strip of at most 25 units under the parapet, so keeping
  them changes nothing he saw and avoids a see-through gap.
- **The cheek caps** at their generated position; his sit 87 further out and 27 higher.
  The staircase is approved as generated.
- The map marker, the test stall, toe rocks, verge wedges and scrub, which the
  generator regenerates in equivalent positions.

## How close the result is

Nearest-piece match of the generated plugin against the reference, after removing his
duplicates:

| Group | Reference | Generated | Matched | Plan distance median / max |
| --- | --- | --- | --- | --- |
| `StonewallTerrace01` | 68 | 76 | 68 | 57 / 169 |
| `Stonewall01` (parapets, cheeks, big walls, wings) | 90 | 75 | 87 | 30 / 155 |
| `StonewallEndL01` (bastions, caps, foot ends) | 15 | 13 | 12 | 81 / 177 |
| Stairs, braziers, fire, posts, banners | 12 | 12 | 12 | 0 / 0 |

The extra generated terrace pieces are even spacing on runs where he left gaps; the
unmatched walls are his short-face wall-ends where the generator uses one `Stonewall01`
instead, and his three retired courses.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 11 / 11 (26 cells) |
| Kit stair flights | 3 |
| Cheek blocks, level ends, tapered caps, retaining wings | 18, 4, 2, 2 |
| Structural retaining courses / corners | 43 / 1 |
| Band: outward walls / inward pieces / parapets | 44 / 44 / 49 |
| Band: west lower field walls | 12 |
| Band: big walls on short faces / bastion wall-ends | 0 / 0 (switched off, see amendment) |
| Entrance dressing | 18 |
| Toe rocks / verge wedges / shrubs and scrub | 60 / 25 / 129 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **374** |
| Rejected as oversized / for blocking the entrance / for the market floor | 17 / 10 / 0 |

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json   # twice
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 (re-verified after the amendment).
- `footprint_audit.py` against the full load order: 0 vanilla references proud of the
  floor on the foundation.
- No Stonewall-family reference above scale 1.1 remains in the plugin (checked by
  reading the ESP).
- Independent read of the written ESP against the eroded market floor and the walkable
  stair width: **one hit, the deliberate `SMarketStall01` test stall.** Cheeks, parapet
  and the stair-foot dressing (which straddle those lines by design) are excluded and
  noted.
- Nearest-piece match against the reference as tabled above; stairs, braziers, fire,
  banner posts and banners at the reference positions exactly.
- **All six cell overrides byte-identical to vanilla.** WRLD deviation unchanged (RNAM
  dropped, FULL literal).
- Plan view drawn from the plugin: bands on every long face of the 26-cell outline,
  stairs at `X -5888`.
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

`SKYRIMFAIR_TRACE=1` prints cheek, bank and band decisions to stderr.

## Known and deliberately not done

- Corners are now simply where two rows meet, with the outward walls at their per-edge
  offsets, so a north/east corner has a 53-unit step between the two wall faces. No
  corner piece of any kind is placed; if a corner needs closing, it wants a new idea
  rather than a scaled wall.
- Shrubs are still planted at grade beyond the walls, not on the grass.
- Timber fence on the top edge, the cobbled spur from the road, bunting, pavilion,
  signpost text: not started. NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test on the terrace (from the previous pass)

1. **The whole perimeter**: each side should read as your own build did - terrace wall,
   grass, knee-high parapet. The big slabs are gone; look at the corners, where the two
   rows now just meet, and say whether they need closing with something else.
2. **The west side** now has the grass plinth under the wall that the east and south
   have. Say if you would rather it stayed as you left it.
3. **The stair foot**: braziers lit on the cheek ends, wing walls and banners where you
   put them.
4. **The retaining bodies you deleted are back** behind the walls. If any earth face
   shows where you had cleared it, tell me where.
5. Market floor clean, stairs climbable, nothing hollow.
