"""Give the cameos' lines vanilla lip tracks of the same length, so they lip-sync on plain Skyrim.

    python tools/cameos/borrow_lips.py --data "E:/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"

Run after tools/cameos/build_voices.py (it rewrites that step's .fuz files).

Plain Skyrim SE skips lip files made by today's generators: its lip function runs four checks
that they fail (SSE Engine Fixes' bLipSync fix bypasses them). Vanilla's own lip files, from the
2011 pipeline, pass. Neither generator on hand writes that format from the command line (the SE
LipGenerator and the SE Creation Kit write the new one; the Oldrim Creation Kit has no
command-line lip generation). So each line borrows the lip track of a vanilla line of the same
length (Barry, 2026-09-25: "I honestly didn't notice it was out of sync"), from a voice that
suits the character: the mouth moves for exactly as long as the voice plays.

For each cameo (cameos.members[].lipsFrom, a vanilla voice type folder), every vanilla line's
length is read from its xWMA header (no decoding), and each of the cameo's lines takes the
unused vanilla line closest in length. The .fuz keeps its own audio; only its lip track changes.
lines.json records each line's source ("lipFrom") and both lengths.
"""
import argparse
import json
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import bsa_extract  # noqa: E402


def fuz_parts(fuz: bytes):
    assert fuz[:4] == b"FUZE", "not a .fuz"
    lip_size = struct.unpack_from("<I", fuz, 8)[0]
    return fuz[12:12 + lip_size], fuz[12 + lip_size:]


def xwm_seconds(xwm: bytes) -> float:
    """An xWMA file's length: its last 'dpds' entry (decoded bytes) over 16-bit PCM's byte rate."""
    assert xwm[:4] == b"RIFF" and xwm[8:12] == b"XWMA", "not xWMA"
    p, rate, channels, dpds = 12, None, None, None
    while p + 8 <= len(xwm):
        tag, size = xwm[p:p + 4], struct.unpack_from("<I", xwm, p + 4)[0]
        body = xwm[p + 8:p + 8 + size]
        if tag == b"fmt ":
            _fmt, channels, rate = struct.unpack_from("<HHI", body, 0)
        elif tag == b"dpds":
            dpds = struct.unpack_from(f"<{size // 4}I", body, 0)
        p += 8 + size + (size & 1)
    return dpds[-1] / (rate * channels * 2)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", required=True, help="a Skyrim SE Data folder with Bethesda's voice archives")
    args = ap.parse_args()
    config = json.loads((ROOT / "fair.config.json").read_text(encoding="utf-8"))
    cameos = config["fairWorld"]["cameos"]
    build = ROOT / cameos.get("voiceBuildDir", "build/cameos")
    index = bsa_extract.build_index(args.data)

    for member in cameos["members"]:
        voice = member.get("lipsFrom")
        work = build / member["id"]
        if not voice or not (work / "lines.json").exists():
            continue
        prefix = f"sound/voice/skyrim.esm/{voice.lower()}/"
        pool = []
        # Spoken lines only: the bards' sung songs (bardsongs_*) are performance scenes, and
        # their lips didn't animate a cameo on plain SE (2.0.1.3, 2026-09-25).
        exclude = [e.lower() for e in member.get("lipsExclude", ["bardsongs"])]
        for key in sorted(k for k in index if k.startswith(prefix) and k.endswith(".fuz")
                          and not any(e in k.rsplit("/", 1)[-1] for e in exclude)):
            try:
                lip, xwm = fuz_parts(bsa_extract.extract(index[key]))
                if len(lip) >= 100:
                    pool.append((xwm_seconds(xwm), key, lip))
            except (AssertionError, struct.error, TypeError, ZeroDivisionError):
                continue
        if not pool:
            sys.exit(f"{member['id']}: no vanilla lines under {prefix}")
        lines = json.loads((work / "lines.json").read_text(encoding="utf-8"))
        used = set()
        worst = 0.0
        # The share of each line's length the lip should run: plain SE seems to skip a lip that
        # lasts as long as its sound (a bard's 4.7 s lip animated a 6.2 s line; lips matched to
        # the full length didn't). cameos.members[].lipShare, 0.95 by default.
        share = float(member.get("lipShare", 0.95))
        # The longest lines first, as the pool thins out at the long end.
        for line in sorted(lines, key=lambda x: -x["seconds"]):
            fuz_path = work / line["fuz"]
            _lip, audio = fuz_parts(fuz_path.read_bytes())
            ours = xwm_seconds(audio)
            target = ours * share
            seconds, key, lip = min((c for c in pool if c[1] not in used), key=lambda c: abs(c[0] - target))
            used.add(key)
            worst = max(worst, abs(seconds - target))
            fuz_path.write_bytes(b"FUZE" + struct.pack("<II", 1, len(lip)) + lip + audio)
            line["lipFrom"] = key
            line["lipSeconds"] = round(seconds, 3)
            line["audioSeconds"] = round(ours, 3)
            line["lipShare"] = share
        (work / "lines.json").write_text(json.dumps(lines, indent=1, ensure_ascii=False), encoding="utf-8")
        print(f"  {member['id']}: {len(lines)} lip tracks from {voice} ({len(pool)} candidates) at "
              f"{share:.0%} of each line's length, within {worst * 1000:.0f} ms")


if __name__ == "__main__":
    main()
