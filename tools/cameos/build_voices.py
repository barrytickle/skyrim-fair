"""Voice files for the cameos' lines (Garrick Sol V, Claudius Vale), from Barry's recordings.

    python tools/cameos/build_voices.py

Reads cameos/skyrim_fair_voicelines.json ({"garrick_sol_v": [{"file": "01.wav", "text": ...}, ...]})
and each cameo's recordings in cameos/<folder>/mono/ (mono 44.1 kHz 16-bit), as fair.config.json's
cameos.members say (voiceLines: the json key, voiceDir: the folder). For each line:

- ffmpeg brings the line to the voices' loudness: a gentle speech compressor (3:1 over
  -24 dB), then loudnorm to `cameos.voiceLoudness` LUFS (-12; true peak -1; about -12.7 in
  practice): Barry's recordings sat at -17 to -19, 7-9 dB under the stage songs (-10)
- LipGenerator (Bethesda's, command line) writes the lip track from the audio and the line's
  text (the text improves the mouth shapes; curly quotes are straightened for it)
- xwmaencode makes the xWMA audio, and LIPFuzer packs both into a .fuz

Writes build/cameos/<id>/<nn>.fuz and build/cameos/<id>/lines.json (file, text, fuz, in the
json's order). The generator (FairCameos.cs) makes one Hello line per entry, subtitled with
its text, and copies each .fuz to the name the engine looks for.
"""
import json
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = Path(r'E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Tools')
LIPGEN = TOOLS / 'LipGen/LipGenerator/LipGenerator.exe'
LIPFUZER = TOOLS / 'LipGen/LipFuzer/LIPFuzer.exe'
XWMA = TOOLS / 'Audio/xwmaencode.exe'
OUT = ROOT / 'build' / 'cameos'


def run(*args):
    r = subprocess.run([str(a) for a in args], capture_output=True, text=True)
    if r.returncode != 0:
        raise RuntimeError(f"{Path(str(args[0])).name} failed ({r.returncode}): {r.stdout[-400:]} {r.stderr[-400:]}")
    return r


def plain(text):
    """The line for LipGenerator: straight quotes, no dashes it can't read."""
    return (text.replace('\u2019', "'").replace('\u2018', "'").replace('\u201c', '"').replace('\u201d', '"')
            .replace('\u2014', ', ').replace('\u2013', ', ').replace('\u2026', '...'))


def main():
    config = json.loads((ROOT / 'fair.config.json').read_text(encoding='utf-8'))
    cameos = config['fairWorld']['cameos']
    lines_file = ROOT / cameos.get('voiceLinesFile', 'cameos/skyrim_fair_voicelines.json')
    all_lines = json.loads(lines_file.read_text(encoding='utf-8'))
    for member in cameos['members']:
        key, src = member.get('voiceLines'), member.get('voiceDir')
        if not key or not src:
            continue
        entries = all_lines[key]
        work = OUT / member['id']
        staging = work / 'staging'
        if work.exists():
            shutil.rmtree(work)
        staging.mkdir(parents=True)
        made = []
        for k, entry in enumerate(entries, 1):
            wav = ROOT / src / entry['file']
            if not wav.exists():
                sys.exit(f"{member['id']}: {wav} is missing")
            base = f"{k:02d}"
            loud = cameos.get('voiceLoudness', -12)
            run('ffmpeg', '-v', 'error', '-y', '-i', wav, '-af', f'acompressor=threshold=-24dB:ratio=3:attack=5:release=80:makeup=6,loudnorm=I={loud}:TP=-1.0:LRA=7',
                '-ar', '44100', '-ac', '1', '-c:a', 'pcm_s16le', staging / f'{base}.wav')
            for attempt in range(3):  # LipGenerator now and then exits 1 on a file it accepts on a rerun
                try:
                    run(LIPGEN, staging / f'{base}.wav', plain(entry['text']), f'-OutputFileName:{staging / (base + ".lip")}')
                    break
                except RuntimeError:
                    if attempt == 2:
                        raise
            if not (staging / f'{base}.lip').exists():
                raise RuntimeError(f"LipGenerator wrote no lip for {member['id']} {base}")
            run(XWMA, staging / f'{base}.wav', staging / f'{base}.xwm')
            (staging / f'{base}.wav').unlink()
            made.append({'file': entry['file'], 'text': entry['text'], 'fuz': f'{base}.fuz'})
        run(LIPFUZER, '-s', staging, '-d', work, '--norec', '-v', 0)
        for m in made:
            fuz = (work / m['fuz']).read_bytes() if (work / m['fuz']).exists() else b''
            # A .fuz is 'FUZE', a version, the lip size, the lip data, then the xWMA audio.
            if fuz[:4] != b'FUZE' or int.from_bytes(fuz[8:12], 'little') < 100:
                raise RuntimeError(f"{member['id']} {m['fuz']}: missing, or without a lip track")
        shutil.rmtree(staging)
        (work / 'lines.json').write_text(json.dumps(made, indent=1, ensure_ascii=False), encoding='utf-8')
        print(f"  {member['id']}: {len(made)} lines -> {work}")


if __name__ == '__main__':
    main()
