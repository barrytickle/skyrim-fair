"""Seed each stage song's sections (drums, crowd) in fair.config.json from its stems.

    "<Blender 3.6>/3.6/python/bin/python.exe" tools/bards/build_sections.py [--write] [--force] [song ...]

(Blender's bundled Python, for numpy; ffmpeg on the path.) For each song in
fairWorld.audio.stage.songs, unpacks music/stem/<title> Stems.zip, tells the instrumental
from the vocal by its bass share (as build_vocals.py does), and reads the drums'
intensity from the bass band (build_vocals.intensity: calm / normal / intense, 2.5 s
steps, blips under 5 s merged).

Each section is [start seconds, drums, crowd]:
- drums: calm (the drummers rest), normal or intense (they play)
- crowd: dance, clap or cheer (the dancers' idles)
The seed claps in the calm sections and dances elsewhere. The lists are Barry's to tune
by hand: --write adds them only to songs that have none, --force replaces them. Without
either, they're printed.

Only the stems are read; no singer lines or voice files are made (that's build_vocals.py).
"""
import argparse
import json
import shutil
import sys
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_vocals as bv  # noqa: E402

ROOT = bv.ROOT
CONFIG = ROOT / 'fair.config.json'
WORK = ROOT / 'build' / 'bards' / 'sections'
NL = chr(10)


def instrumental(title, work):
    """The instrumental stem of music/stem/<title> Stems.zip, as mono samples."""
    if work.exists():
        shutil.rmtree(work)
    work.mkdir(parents=True)
    zipfile.ZipFile(ROOT / 'music' / 'stem' / f'{title} Stems.zip').extractall(work)
    mp3s = sorted(work.rglob('*.mp3'))
    if len(mp3s) != 2:
        raise ValueError(f'{title}: expected 2 stems, found {len(mp3s)}')
    probe = []
    for k, p in enumerate(mp3s):
        wav = work / f'stem{k}.wav'
        bv.decode(p, wav)
        x = bv.load(wav)
        probe.append((bv.band_energy(x, 20, 150).sum() / bv.band_energy(x, 20, 20000).sum(), x))
    probe.sort(key=lambda t: t[0])
    if not (probe[0][0] < 0.05 < probe[1][0]):
        raise ValueError(f'{title}: cannot tell the vocal from the instrumental ({probe[0][0]:.2f}, {probe[1][0]:.2f})')
    return probe[1][1]


def sections(inst, defaults):
    drums = bv.intensity(inst, defaults)['drums']
    return [[s['start'], s['level'], 'clap' if s['level'] == 'calm' else 'dance'] for s in drums]


def write_songs(text, songs):
    """The config with its stage songs array rewritten, a song a line; the rest untouched."""
    start = text.index('"songs": [', text.index('"hardEndFade"'))
    depth = 0
    end = None
    for k in range(start + len('"songs": '), len(text)):
        depth += {'[': 1, ']': -1}.get(text[k], 0)
        if depth == 0:
            end = k + 1
            break
    indent = ' ' * (start - text.rindex(NL, 0, start) - 1)
    body = (',' + NL).join(indent + '  ' + json.dumps(song, ensure_ascii=False) for song in songs)
    return text[:start] + '"songs": [' + NL + body + NL + indent + ']' + text[end:]


def main():
    ap = argparse.ArgumentParser(description=__doc__.split(NL)[0])
    ap.add_argument('songs', nargs='*', help='song names (fairWorld.audio.stage.songs[].name); all by default')
    ap.add_argument('--write', action='store_true', help='add sections to songs that have none')
    ap.add_argument('--force', action='store_true', help="replace every chosen song's sections")
    args = ap.parse_args()

    defaults = json.loads((ROOT / 'tools' / 'bards' / 'songs.json').read_text(encoding='utf-8'))['defaults']
    text = CONFIG.read_text(encoding='utf-8')
    config = json.loads(text)
    songs = config['fairWorld']['audio']['stage']['songs']
    changed = 0
    for song in songs:
        if args.songs and song['name'] not in args.songs:
            continue
        title = Path(song['source']).stem
        seeded = sections(instrumental(title, WORK / song['name']), defaults)
        shutil.rmtree(WORK / song['name'])
        print(f"{song['name']}: {len(seeded)} sections, drums rest in "
              f"{sum(1 for s in seeded if s[1] == 'calm')}: {json.dumps(seeded)}")
        if (args.write and 'sections' not in song) or args.force:
            song['sections'] = seeded
            changed += 1

    if changed:
        text = write_songs(text, songs)
        json.loads(text)
        CONFIG.write_text(text, encoding='utf-8')
        print(f'wrote sections for {changed} song(s) to {CONFIG.name}')


if __name__ == '__main__':
    main()
