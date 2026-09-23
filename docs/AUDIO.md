# Fair audio: the stage set and the crowd ambience

Built 2026-09-23. It is steps 1 and 2 of `docs/MUSIC.md`: the ambient pass and the playlist
pass. The performers' cues (step 3) and the Tamriel gate (step 4) are still to do.

## The files

Barry's converted audio, checked by `tools/build_audio.py` on every build. All of it is
mono, 44,100 Hz, 16-bit PCM, so nothing is reconverted.

| Source | Length | Role | Runtime file (under `Sound\`) |
| --- | --- | --- | --- |
| `music/mono/Round the Green.wav` | 2:29.6 | stage song 1, the signature number | `SkyrimFair\Music\Stage\RoundTheGreen.wav` |
| `music/mono/Hey-Ho, Skyrim.wav` | 3:39.9 | stage song 2 | `SkyrimFair\Music\Stage\HeyHoSkyrim.wav` |
| `music/mono/Fiddle.wav` | 3:22.8 | stage song 3. It stops dead, so it gets a 1.5 s fade | `SkyrimFair\Music\Stage\Fiddle.wav` |
| `music/mono/Dragonborn-Approved.wav` | 4:16.8 | stage song 4. It also ends hot, so it gets a fade | `SkyrimFair\Music\Stage\DragonbornApproved.wav` |
| `sound-effects/mono/crowd-cheer.wav` | 22.1 | **one-shot** cheer: 1.3 s of silence, a peak from about 1.5 to 7 s, then a long tail | `SkyrimFair\Effects\CrowdCheer01.wav`: the silence trimmed, 11 s kept, a 3.5 s fade-out |
| `sound-effects/mono/crowd.wav` | 63.9 | **looping ambience**: a steady crowd murmur (level even throughout, a few laughs), not cheering | `SkyrimFair\Ambience\CrowdMurmur01-04.wav`: a seamless 60.9 s loop (the last 3 s crossfaded into the start), in four copies each starting a quarter further round |

There are no other effects in `sound-effects/mono/`, so there is no market, tavern,
archery, horse or fire audio yet. None was invented. `music/original/` and
`sound-effects/original/` are the unconverted sources and are not used.

The files the build writes are plain WAVs: only a `fmt` and a `data` chunk, without the
LIST tag ffmpeg adds. They are git-ignored, like the sources.

## Crowd ambience

**Six positional emitters**, placed where the visitors actually cluster. The spots come
from grouping the placed NPCs:

| Emitter | Where | Copy |
| --- | --- | --- |
| FoodRow | the avenue's food stalls (2200, 2000) | 1 |
| EastLane | the east lane's middle (4150, 2650) | 2 |
| EastWallWalk | the south-east stalls (4300, 350) | 3 |
| GateFood | the entrance food row (2350, -300) | 4 |
| StageSquare | the crowd square (1950, 4650) | 3 |
| WestField | picnics, archery and stables (600, 1700), 5 dB quieter | 2 |

- Each emitter is a vanilla-style placed sound marker (`SOUN`) looping its own rotated
  copy. Copies heard together are 15 to 45 s apart, so the loop never lines up with itself.
- Each is full volume within 500 and silent by 3,000, so moving round the fair crosses
  from one to the next. There's no single recording following the player.
- The ambience plays 4 dB down (`staticAttenuation`) in its own category,
  `SkyrimFairAudioAmbienceCategory`. That category sits under vanilla's ambient category,
  so the player's Effects slider applies.
- **Lifecycle:** the engine runs placed markers itself. They play while their cell is
  loaded and stop when it unloads, whether leaving, fast travel or loading a save. They
  can't double up, and no script is needed.

## Stage set

`SkyrimFairAudioQuest` is start-game-enabled, so it also starts in an existing save. It
runs `SkyrimFairAudioScript` (`src/Papyrus/`). The generator writes every property from
`fairWorld.audio`, including each song's length, measured from the built files.

- The songs and the cheer play from `SkyrimFairAudioStageSpeaker`, a persistent marker
  above the stage (2048, 5450, 320). They're full volume within 1,500, which covers the
  whole square, and fade to nothing at 7,500, about the gate.
- The sequence is: 4 s after arriving, **song**, then the **cheer** (one-shot, 11 s),
  then a **2 s pause**, then the **next song**. It goes round the playlist in config order.
- Each song names its cheer (`"cheer": "cheer"`). New cheers (small applause, a big
  one, a song's own reaction) are more entries in `stage.cheers`, with no code change.
  The cheer is never looped, and it isn't part of the song files.
- **Ducking:** while a song plays, the ambience category drops to 75%
  (`duckAmbience`). Between songs it's back to 100%. During the cheer it's also 100%,
  and the cheer is louder than the murmur anyway.
- **Clock:** the script counts game time, converted with TimeScale. Game time stops in
  menus, and so do the fair's sounds, because their category pauses during menus. So a
  menu doesn't throw the timing out. Waiting or sleeping skips on to the next song.
- **Leaving:** every 2 s the script checks the player is still in `SkyrimFairWorld`.
  Outside it, the song and cheer are stopped. When the player comes back, the current
  song starts again after 4 s.
- **Save and load:** sounds don't survive a load. The quest's player alias
  (`SkyrimFairAudioPlayerAlias`) calls `Recover()` on every load: the old sound IDs are
  forgotten and the current song restarts after 4 s. Only one song and one cheer are ever
  tracked, and each is stopped before the next starts, so nothing accumulates.
- The world's music type is `MUSTavernSILENCE`, the silent type vanilla uses while bards
  play, so Skyrim's exploration music stays out of the fair.

## The band

Three bards on the deck, each at a vanilla instrument idle marker (lute, drum, flute) with
a copy of Candlehearth Hall's bard package: `UseIdleMarker` at that one marker, which is
persistent. The idle brings the instrument. They play continuously for now.

**Papyrus's `Sound` type is the sound marker (`SOUN`)**, so each song and the cheer has
one for the script; the descriptors (`SNDR`) behind them hold the files.

## Settings (for a later MCM)

Runtime globals, read by the script every 2 s, so a change applies live:

| Global | Default | Does |
| --- | --- | --- |
| `SkyrimFairAudioAmbienceEnabled` | 1 | 0 silences the ambience category |
| `SkyrimFairAudioAmbienceVolume` | 1.0 | ambience level (before ducking) |
| `SkyrimFairAudioMusicEnabled` | 1 | 0 stops the set; 1 restarts it after 4 s |
| `SkyrimFairAudioMusicVolume` | 1.0 | the song's instance volume |
| `SkyrimFairAudioCheerVolume` | 1.0 | the cheer's instance volume |

In the console: `set SkyrimFairAudioMusicVolume to 0.6`. Distances, attenuation, the
pause and the ducking level are generator values in `fairWorld.audio`.

## Build

```
python tools/build_audio.py                    # music/ + sound-effects/ -> assets/sound/
python tools/build_papyrus.py --ck "C:/Program Files (x86)/Steam/steamapps/common/skyrim"
dotnet run -c Release --project src/SkyrimFair.Generator -- fair.config.json
python tools/deploy.py --to "E:/Modlists/Still In Skyrim/mods/Skyrim Fair"
```

- **Papyrus:** `build_papyrus.py` uses the Creation Kit's command-line `PapyrusCompiler.exe`.
  It unpacks the vanilla sources from the CK's `Data\Scripts.rar` once, with Windows'
  own `tar`, into `build/papyrus/`. It never opens the Creation Kit.
- **Order:** the generator reads the built sound files' lengths, so build the audio
  first.
- **Deploy:** `deploy.py` copies the plugin, meshes, textures, `Sound\` and `Scripts\`,
  only the files that changed, and checks each copy is byte-identical.

## Acceptance test

1. Enter the fair: a low murmur, with no music for the first 4 s.
2. Walk the market: the murmur rises and falls between stalls and never stops.
3. Walk to the stage: the song takes over, with the murmur still just under it.
4. At the song's end: the cheer, a breath, the next song.
5. Walk to the gate: the music fades out behind you.
6. Leave through the gate, or `coc` elsewhere: nothing keeps playing.
7. Save mid-song, load: the song starts again after about 4 s, playing once, not twice.
8. `set SkyrimFairAudioMusicEnabled to 0`: the set stops within 2 s. Set it back to 1
   and it restarts.

To tune: `fairWorld.audio.ambience.staticAttenuation` (4) for the murmur's level;
`stage.minDistance` and `maxDistance` (1,500 and 7,500) for how far the band carries;
`stage.duckAmbience` (0.75).

## Rights

The songs and crowd files are Barry's. `docs/AUDIT.md` still records their provenance as
unknown. Confirm the terms before any public release; see `CREDITS.md`.
