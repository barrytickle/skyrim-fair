"""Build the fair's runtime sound files from Barry's converted audio.

Reads fairWorld.audio in fair.config.json. Every source is already mono 44.1 kHz 16-bit
PCM (music/mono, sound-effects/mono); this checks that rather than reconverting, then
writes clean WAVs (a fmt and a data chunk only, no ffmpeg LIST tags) under
assets/sound/, at the Data-relative paths the plugin's sound descriptors name:

- songs: kept whole, with a short fade where a track stops dead
- one-shots (the cheer): leading silence trimmed, cut to length with a fade-out
- ambience loops: a seamless loop (the end crossfaded into the start), and rotated
  copies starting at different points, so emitters playing at once never line up

Deterministic: the same sources give byte-identical files.

    python tools/build_audio.py
"""
import array
import json
import pathlib
import sys
import wave

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "assets" / "sound"
RATE = 44100


def read(source: str) -> array.array:
    path = ROOT / source
    with wave.open(str(path), "rb") as w:
        fmt = (w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getcomptype())
        if fmt != (1, 2, RATE, "NONE"):
            sys.exit(f"{source}: {fmt}; expected mono, 16-bit, {RATE} Hz PCM")
        samples = array.array("h")
        samples.frombytes(w.readframes(w.getnframes()))
    if sys.byteorder != "little":
        samples.byteswap()
    return samples


def write(file: str, samples: array.array) -> float:
    path = OUT / pathlib.PureWindowsPath(file).as_posix()
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(samples.tobytes())
    return len(samples) / RATE


def clip(v: float) -> int:
    return max(-32768, min(32767, int(round(v))))


def fade_out(samples: array.array, seconds: float) -> None:
    n = min(len(samples), int(seconds * RATE))
    start = len(samples) - n
    for i in range(n):
        samples[start + i] = clip(samples[start + i] * (1 - (i + 1) / n))


def fade_in(samples: array.array, seconds: float) -> None:
    n = min(len(samples), int(seconds * RATE))
    for i in range(n):
        samples[i] = clip(samples[i] * (i / n))


def tail_level(samples: array.array, seconds: float = 0.05) -> int:
    tail = samples[-int(seconds * RATE):]
    return int((sum(s * s for s in tail) / len(tail)) ** 0.5)


def first_sound(samples: array.array, threshold: int) -> int:
    for i, s in enumerate(samples):
        if abs(s) >= threshold:
            return i
    return 0


def seamless(samples: array.array, crossfade: float) -> array.array:
    """A loop whose last `crossfade` seconds blend into its first, so it wraps unheard."""
    x = int(crossfade * RATE)
    body = samples[x:]
    n = len(body)
    for i in range(x):
        t = (i + 0.5) / x
        # Equal-power blend: the tail fades out as the head (which the loop wraps to) fades in.
        body[n - x + i] = clip(samples[len(samples) - x + i] * (1 - t) ** 0.5 + samples[i] * t ** 0.5)
    return body


def main() -> None:
    cfg = json.loads((ROOT / "fair.config.json").read_text(encoding="utf-8"))
    audio = cfg["fairWorld"]["audio"]
    made = []

    for song in audio["stage"]["songs"]:
        s = read(song["source"])
        level = tail_level(s)
        # A track that stops dead (loud in its last 50 ms) gets a short fade.
        if level > audio["stage"].get("hardEndLevel", 300):
            fade_out(s, audio["stage"].get("hardEndFade", 1.5))
        made.append((song["file"], write(song["file"], s), f"tail {level}"))

    for cheer in audio["stage"]["cheers"]:
        s = read(cheer["source"])
        start = max(0, first_sound(s, cheer.get("threshold", 600)) - int(0.05 * RATE))
        s = s[start:start + int(cheer["length"] * RATE)]
        fade_in(s, 0.05)
        fade_out(s, cheer.get("fadeOut", 3.0))
        made.append((cheer["file"], write(cheer["file"], s), f"trimmed {start / RATE:.2f}s"))

    amb = audio["ambience"]
    for loop in amb["loops"]:
        base = seamless(read(loop["source"]), loop.get("crossfade", 3.0))
        for k, file in enumerate(loop["files"]):
            # Each copy starts a share further round the loop.
            r = int(len(base) * k / len(loop["files"]))
            made.append((file, write(file, base[r:] + base[:r]), f"rotated {r / RATE:.1f}s"))

    for file, seconds, note in made:
        print(f"{seconds:8.2f}s  {file}  ({note})")


if __name__ == "__main__":
    main()
