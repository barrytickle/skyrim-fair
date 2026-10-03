# The stage singers and livelier bards

Barry's brief (2026-09-23):
- **Lip sync:** option 1, generic singing lips, chosen over word-accurate sync (see "Why
  option 1").
- **Singers:** three, since the songs have several voices.
- **Crowd answers:** the singers needn't mouth them.
- **The band:** instruments should get intense where the songs do, and the bards should
  move about the stage.

**Status:**
- **Built:** the audio side for Fiddle and Dragonborn-Approved (below).
- **Built (2026-09-23), awaiting Barry's test:** the generator side (singers, records,
  script), as in "Generator side" below; details in `docs/AUDIT.md`. The naming rule is
  checked against vanilla. The singers can't be Traits-templated (Traits carries the voice
  type), so each copies a vanilla NPC's face and FaceGen files.
- **Not started:** the animation side.

## How vanilla bards sing (the method we copy)

Vanilla bard songs are **dialogue**. `BardSongs` has 172 voice files, and each `.fuz` is an
xWMA audio clip with a `.lip` lip track, one per sung line and voice type. A scene plays
them line by line while the bard holds the lute idle.

The Special Edition Creation Kit install has every tool for this, all command-line, so
they fit `AGENTS.md`:
- `Tools\LipGen\LipGenerator\LipGenerator.exe` (FaceFX)
- `Tools\LipGen\LipFuzer\LIPFuzer.exe`
- `Tools\Audio\xwmaencode.exe`

## Why option 1 (audio-driven lips), not word-accurate

Both were tested on Fiddle:
- **Audio-driven** (no text): LipGenerator follows the vocal stem's syllables, pauses and
  loudness. It works on anything from a single line to the whole 3:23 song.
- **With lyrics:** LipGenerator **crashes (segfault) above roughly 60–70 words** (about
  20 s of singing). It would need the lyrics cut into timed lines.
  - The vocal is sung smoothly, with reverb, so where each line starts can only be guessed.
  - The crowd's answers in the bridge ("NOBODY!") sit inside the vocal stem, with no gap
    to cut at.

Wrong line timing looks worse than good generic movement, so Barry chose option 1.
Word-accurate sync stays possible later, with timed lyrics. One side effect: the singers'
lips also move on the crowd's shouts. They're only a few seconds, and it reads as the band
joining in.

## The stems (audit of Fiddle)

- `music/stem/<Song> Stems.zip` holds two MP3s, **both named "Lead Vocal"**. Which is
  which is detected by bass share:
  - the vocal has 0% of its energy below 150 Hz
  - the other is the **complete instrumental**, drums, bass and fiddle (61% below 150 Hz)
- **Length:** both match the finished mix, 202.8 s. The MP3 headers claim otherwise, but
  they're wrong.
- **Offset:** both are 23 ms late, which is LAME's encoder delay. It's trimmed.
- **Level:** unmastered, about 4 dB under the mix. The pair gets one shared gain, so the
  balance stays the mix's.
- **Song map from the vocal:**

  | Time | Section |
  |---|---|
  | 10.8–22 s | spoken intro |
  | 22–36 s | instrumental |
  | 36–77 s | Verse 1, Verse 2, Chorus |
  | 82–124 s | Verse 3, Verse 4, Chorus |
  | **124–136 s** | **the fiddle and hurdy-gurdy duel** |
  | 136–182 s | bridge and final chorus |
  | 184–189 s | outro |

## What the audio tool makes

```
"C:/Blender/blender-3.6.23-windows-x64/blender-3.6.23-windows-x64/3.6/python/bin/python.exe" tools/bards/build_vocals.py [song ...]
```

It uses Blender's bundled Python for numpy, plus ffmpeg. The songs are listed in
`tools/bards/songs.json`. The output goes in `build/bards/<song>/`, which is git-ignored:

- **`voice/<singer>/<song>_Lnn_<singer>.fuz`:** one voice file per sung line per singer.
  - The singers are `lead`, `left` and `right`.
  - Every line has the same timing for all three, but a different gesture exaggeration
    (1.0, 1.3, 0.8), so the three faces don't move identically.
  - **All three are lip-only: their audio is silence.** The stage speaker keeps playing
    the full mix, so the vocal can't drift from the band. A Papyrus timer that's 0.2 s off
    is invisible on lips but audible on a voice.
  - To move the vocal into the lead's mouth later, set `voiced: true` on him in
    `songs.json` and play `instrumental.wav` from the speaker instead of the mix.
- **`cues.json`:**
  - `lines`: each line's `id`, `start` and `end` in seconds from the song's start, never
    overlapping, 2–15 s each, plus its files.
  - `intensity`: calm, normal and intense sections for `drums` (bass band) and `strings`
    (mid band), at 2.5 s resolution.
  - `length`, matching the mix to the millisecond.
- **`instrumental.wav`:** the instrumental stem at the shared gain, used only if a singer is
  made `voiced`.

**Built:**
- Fiddle: 19 lines (161 s sung of 203 s)
- Dragonborn-Approved: 30 lines (218 s of 257 s); its instrumental break is 153–178 s

The tool checks for overlaps, and checks that every `.fuz` holds a lip track.
LipGenerator sometimes exits 1 on a file it accepts on a rerun, so the tool retries.

**Waiting on stems:** Round the Green and Hey-Ho, Skyrim. Songs without stems simply have no
singer lines.

## Generator side (brief for the other agent)

Keep it FormID-safe: build everything in the late section, appended, as the orchestra and
the crowd figures are.

1. **Three singer NPCs**, "Singer", men as the songs are, at the front of the stage deck.
   - Persistent, and given to the stage script as new properties (`Singers`).
   - Face the square; hold them with `DefaultStayAtEditorLocation`, as the bards are.
2. **Three voice types**, one each: `SkyrimFairSingerLead`, `SkyrimFairSingerLeft`,
   `SkyrimFairSingerRight`. One dialogue line then plays a different file per singer,
   because the engine picks the file by the speaker's voice type.
3. **One topic per sung line** (a `DIAL` with one `INFO`), in a new quest or the stage quest.
   - Empty response text, so there are no subtitles.
   - The response's emotion can be Happy 50, for a smile.
4. **The voice files go to `Sound\Voice\SkyrimFair.esp\<VoiceType EditorID>\<name>.fuz`**,
   named as vanilla names them.
   - Read that rule from vanilla before relying on it: `bardsongs__00074773_1.fuz` looks
     like quest EditorID, then topic EditorID (empty), then the INFO's FormID (8 hex, no
     load-order byte), then the response number. The CK truncates the EditorID parts.
   - Check it against `Skyrim.esm`'s BardSongs INFO records with Mutagen, then have the
     generator write a copy manifest from `cues.json` to those names. `deploy.py` copies them.
5. **The stage script** (`SkyrimFairAudioScript`):
   - When a song with cues starts, call `Say(topic)` on each singer at each line's `start`.
     Schedule each line from the song's own start time, not from the previous line, so
     timer error never adds up.
   - Stop at the cheer, or when the player leaves.
   - On load, `Recover()` restarts the song and the line schedule together.
   - Write the lines' start times into script properties from `cues.json`.
6. **Test in game:**
   - Do the three singers' mouths move with the song, and stop in the pauses?
   - How far behind the music are the lips, if at all?
   - Do subtitles stay off?
   - Is there no audible doubling, since the singers' audio is silent?
   - Does it stay in sync over the whole song, and after a save and load mid-song?

## Animation side (next, not started)

- **Vanilla has one loop per instrument:** `animobjectluteloop`, `animobjectdrumloop`,
  `animobjectflutelong`, `animobjectfluteshort`. There are no intense variants and no
  moving ones.
- **Plan:**
  1. Variants of those loops, generated in Blender from the vanilla loops (as the folk
     dance was), or authored by Astra:
     - **intense:** bigger strums, leaning back, head-bobbing, faster drumming
     - **lively:** steps, sway and turns, covering part of the deck and returning to the
       start, so the actor's position never has to move and no pathing is involved
  2. Swap them in for the fair's bards only, through Open Animation Replacer, conditioned
     on a global the stage script sets from `cues.json`'s `intensity` sections. Re-send the
     idle when the level changes, so OAR re-evaluates.
- **The dependency: Astra's folk dance.** It's the first custom clip through OAR. Its
  in-game test shows whether the replacement works, and whether movement inside an idle
  clip displays. Do that before building bard variants on it.
- **Singing faces:** vanilla `SetExpressionOverride` (Happy or Surprise at 40–60) on the
  singers and bards during songs is a free extra.
