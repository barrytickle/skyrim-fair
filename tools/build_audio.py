"""Build the fair's runtime sound files from Barry's converted audio.

Reads fairWorld.audio in fair.config.json (the stage songs from songs.config.json). Every source is already mono 44.1 kHz 16-bit
PCM (music/mono, sound-effects/mono); this checks that rather than reconverting, then
writes clean WAVs (a fmt and a data chunk only, no ffmpeg LIST tags) under
assets/sound/, at the Data-relative paths the plugin's sound descriptors name:

- songs: kept whole, with a short fade where a track stops dead
- one-shots (the cheer): leading silence trimmed, cut to length with a fade-out
- songs and cheers are brought up to the stage's loudness (`stage.loudness`, gated
  dBFS) under a look-ahead peak limiter (`stage.ceiling`): Barry's masters sit near
  -19, far quieter than a live stage should be
- ambience loops: a seamless loop (the end crossfaded into the start), an optional plain
  `gain` in dB, and rotated copies starting at different points, so emitters playing at
  once never line up

Deterministic: the same sources give byte-identical files.

    python tools/build_audio.py
"""
import array
import collections
import json
import math
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


def db(v: float) -> float:
    return 20 * math.log10(max(v, 1e-9) / 32768)


def loudness(samples: array.array, block: float = 0.4) -> float:
    """Gated level in dBFS: the mean power of the 400 ms blocks within 10 dB of the loud
    ones, as loudness metering gates out the quiet passages (unweighted)."""
    n = int(block * RATE)
    powers = []
    for i in range(0, len(samples) - n + 1, n):
        chunk = samples[i:i + n]
        powers.append(sum(s * s for s in chunk) / n)
    powers = [p for p in powers if p > (32768 * 10 ** (-70 / 20)) ** 2] or [1.0]
    mean = sum(powers) / len(powers)
    kept = [p for p in powers if p >= mean / 10] or powers
    return db((sum(kept) / len(kept)) ** 0.5)


def louden(samples: array.array, target: float, ceiling: float, lookahead: float = 0.002, release: float = 0.12) -> tuple:
    """Gain the samples to `target` dBFS (gated), with a look-ahead peak limiter holding
    every sample under `ceiling` dBFS. The limiter's gain dips smoothly just before a
    peak (the minimum over the look-ahead window, averaged over the same window, so it is
    never above what the peak needs) and recovers over `release` seconds."""
    before = loudness(samples)
    gain = 10 ** ((target - before) / 20)
    limit = 32767 * 10 ** (ceiling / 20)
    n = len(samples)
    need = [min(1.0, limit / (abs(s) * gain)) if s else 1.0 for s in samples]
    width = max(1, int(lookahead * RATE))
    # Minimum of need over [i, i + width), by a monotonic queue.
    lows = [1.0] * n
    queue = collections.deque()
    for i in range(n - 1, -1, -1):
        while queue and need[queue[-1]] >= need[i]:
            queue.pop()
        queue.append(i)
        while queue[0] >= i + width:
            queue.popleft()
        lows[i] = need[queue[0]]
    # Release: the gain may fall at once but only climbs back slowly.
    rate = 1 - math.exp(-1 / (release * RATE))
    held = 1.0
    for i in range(n):
        held = min(lows[i], held + (1 - held) * rate)
        lows[i] = held
    # Average over the trailing window: each value is at most what sample i needs.
    out = array.array("h", bytes(2 * n))
    total = 0.0
    for i in range(n):
        total += lows[i]
        if i >= width:
            total -= lows[i - width]
        g = total / min(i + 1, width)
        out[i] = clip(samples[i] * gain * g)
    return out, before, loudness(out)


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
    if audio["stage"].get("songsFile"):  # the songs live in songs.config.json
        audio["stage"]["songs"] = json.loads((ROOT / audio["stage"]["songsFile"]).read_text(encoding="utf-8"))["songs"]
    made = []

    stage = audio["stage"]
    target, ceiling = stage.get("loudness"), stage.get("ceiling", -1.0)

    def loud(s: array.array) -> tuple:
        if target is None:
            return s, ""
        s, before, after = louden(s, target, ceiling)
        return s, f", {before:.1f} -> {after:.1f} dBFS"

    for song in stage["songs"]:
        source = read(song["source"])
        level = tail_level(source)
        s, gained = loud(source)
        # A track that stops dead (loud in its last 50 ms) gets a short fade.
        if level > audio["stage"].get("hardEndLevel", 300):
            fade_out(s, audio["stage"].get("hardEndFade", 1.5))
        made.append((song["file"], write(song["file"], s), f"tail {level}{gained}"))

    for cheer in audio["stage"]["cheers"]:
        s = read(cheer["source"])
        start = max(0, first_sound(s, cheer.get("threshold", 600)) - int(0.05 * RATE))
        s = s[start:start + int(cheer["length"] * RATE)]
        fade_in(s, 0.05)
        fade_out(s, cheer.get("fadeOut", 3.0))
        s, gained = loud(s)
        made.append((cheer["file"], write(cheer["file"], s), f"trimmed {start / RATE:.2f}s{gained}"))

    amb = audio["ambience"]
    for loop in amb["loops"]:
        base = seamless(read(loop["source"]), loop.get("crossfade", 3.0))
        # An optional plain gain in dB (the murmur has headroom: it peaks near -5.5 dBFS).
        g = 10 ** (loop.get("gain", 0) / 20)
        if g != 1:
            base = array.array("h", (clip(v * g) for v in base))
        for k, file in enumerate(loop["files"]):
            # Each copy starts a share further round the loop.
            r = int(len(base) * k / len(loop["files"]))
            made.append((file, write(file, base[r:] + base[:r]), f"rotated {r / RATE:.1f}s"))

    for file, seconds, note in made:
        print(f"{seconds:8.2f}s  {file}  ({note})")


if __name__ == "__main__":
    main()
