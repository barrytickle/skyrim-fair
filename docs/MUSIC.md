# Music and sound: feasibility and plan

Written 2026-09-22 as a reality check on how hard stage music is in Skyrim, before any
of it is built. **Update 2026-09-23:** steps 1 (ambience) and 2 (the playlist, with the
compile step) are built. See `docs/AUDIO.md`. Steps 3 (performer cues) and 4 (the gate)
are still to do.

## Summary

| Goal | Difficulty | Needs |
| --- | --- | --- |
| One song looping from the stage, fading with distance | **Easy** | generator records only, no scripts |
| Crowd murmur, cheers, music heard from outside the Tamriel gate | **Easy** | the same records |
| A playlist of several songs with pauses between them | **Moderate** | Papyrus scripting and a compile step |
| The choreographed set (cheer, pause, song, dancers resume about 5 s in) | **Hard** | scripted animation cues, a lot of in-game tuning |
| A singer whose mouth moves with the song | **Hard** | real lip-sync is not practical; use a generic talking or singing loop |

## How sound works in Skyrim

- **A sound placed in the world** is three record types:
  - a Sound Descriptor (`SNDR`) that points at the audio files;
  - a Sound Output Model (`SOPM`) that sets how it fades with distance;
  - a Sound Marker (`SOUN`) placed as a reference, which loops it from that spot.

  Mutagen writes all three, so this stays generator-owned like everything else. No
  scripts are involved.
- **Game music** (`MUSC` music types and `MUST` tracks) is the non-positional
  background music system. It picks tracks by location and state, ducks and fades,
  stops for combat, and has no precise timing. It suits ambience, not a live set.
- **Positional (3D) sound must be mono.** Skyrim does not place stereo files in 3D
  space, so a stereo song would not seem to come from the stage.
- **Formats**: 16-bit PCM `.wav`, or `.xwm` (xWMA, compressed, about 10x smaller).
  **MP3 is not supported.**
- Give every sound a sound category, so the player's volume sliders apply to it.

## Why the playlist and the set are harder

- **No "sound finished" event.** A script starts a song and gets an instance ID
  back, but it has to know each song's length and wait on timers. Papyrus timers drift
  by a second or so, and more under script load. Treat the DESIGN.md timings (2 s
  pause, dancers about 5 s in) as targets, not guarantees; DESIGN.md already says so.
- **Sounds stop on save/load and when the cell unloads.** The controller script must
  notice (when the stage cell loads, and on game load) and restart the set cleanly.
- **Compile step**: scripts are `.psc` source compiled to `.pex`. The Creation Kit's
  `PapyrusCompiler.exe` runs from the command line, which the project rules allow; it
  needs the vanilla script sources unpacked from the CK install. The pipeline does not
  do this yet.
- **Dancers**: stopping and restarting them on cues means sending animation events or
  switching AI packages from the controller script, and working with Professional
  Dancer's own system. Expect trial and error in game.
- **Singer lip-sync**: `.lip` files only drive dialogue lines, and producing them needs
  CK or FaceFX tooling. A four-minute song as a dialogue line is impractical. Use a
  generic talking or singing idle loop.

## Current files (checked 2026-09-22)

| File | Format | Length | Needs |
| --- | --- | --- | --- |
| `music/Dragonborn-Approved.wav` | 49 MB, stereo, 48 kHz, 16-bit PCM | about 4:17 | mono downmix for stage playback; convert to `.xwm` |
| `music/Hey-Ho, Skyrim.wav` | 42 MB, stereo, 48 kHz, 16-bit PCM | about 3:40 | the same |
| `sound-effects/crowd.mp3` | MP3 | | convert to WAV or `.xwm`; mono if positional |
| `sound-effects/crowd-cheer.mp3` | MP3 | | the same |

Both folders are untracked in git on purpose. Before any of this ships:

- **Provenance and rights.** `docs/AUDIT.md` still records the music as provenance
  unknown. If the tracks came from an AI music service, check that service's terms
  for redistribution in a free mod, and record the result in `CREDITS.md`.
- **Sample rate.** Vanilla sound is mostly 44.1 kHz. 48 kHz should play, but test it,
  or resample to 44.1 kHz during conversion.
- **Conversion tooling.** `xWMAEncode.exe` (it ships with the Creation Kit tools) and
  ffmpeg for downmixing and MP3 decoding are both command-line tools. The conversion
  can be a reproducible script like the texture and mesh builds.
- **Game music overlap.** `SkyrimFairWorld` has no music type assigned, so Skyrim's
  default exploration music may play over the stage. Give the world a silent or
  custom music type when the stage audio goes in.

## Plan, in order

1. **Ambient pass (small).** Convert the files as above. Add a looping mono stage track
   and a crowd murmur as generated `SNDR`, `SOPM` and `SOUN` records, placed at the
   stage and in the crowd square. Silence the world's game music. Tune the falloff in
   game so the music carries across the compound but not beyond the wall.
2. **Playlist pass (moderate).** Add the Papyrus compile step and a small stage
   controller script. Keep the track list, each track's length and the gap timings in
   `fair.config.json`, with the generator writing the script's properties. Play a
   cheer, pause, then the next song. Restart cleanly on load and on cell reload.
3. **Performance pass (hard, last).** Add the dancer and crowd cues from the same
   controller: dancers stop at the song's end and resume about 5 s into the next. Tune
   it by feel in game. Give the singer a generic singing or talking idle, not lip-sync.
4. **The Tamriel gate.** A mono loop of the stage music, quieter and muffled, at the
   closed fair gate so the player hears the fair from outside. Same records as step 1.

Steps 1 and 4 can be done at any time. Steps 2 and 3 should wait until the stage and
the performers exist.
