"""Seed each stage song's sections (drums, crowd) in songs.config.json from its stems.

    "<Blender 3.6>/3.6/python/bin/python.exe" tools/bards/build_sections.py [--write] [--force] [song ...]

(Blender's bundled Python, for numpy; ffmpeg on the path.) For each song in
songs.config.json (fair.config.json's fairWorld.audio.stage.songsFile), unpacks music/stem/<title> Stems.zip, tells the instrumental
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
SONGS = ROOT / 'songs.config.json'
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


def write_songs(doc):
    """songs.config.json as Barry edits it: a song a block, a section a line."""
    lines = ['{', f'  "about": {json.dumps(doc["about"])},', '  "songs": [']
    for n, song in enumerate(doc['songs']):
        lines.append('    {')
        for k in [k for k in song if k != 'sections']:
            lines.append(f'      {json.dumps(k)}: {json.dumps(song[k], ensure_ascii=False)},')
        lines.append('      "sections": [')
        sections = song.get('sections', [])
        for i, (t, drums, crowd) in enumerate(sections):
            lines.append(f'        [{json.dumps(t)}, {json.dumps(drums)}, {json.dumps(crowd)}]' + (',' if i < len(sections) - 1 else ''))
        lines.append('      ]')
        lines.append('    }' + (',' if n < len(doc['songs']) - 1 else ''))
    lines += ['  ]', '}', '']
    return NL.join(lines)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split(NL)[0])
    ap.add_argument('songs', nargs='*', help='song names (songs.config.json); all by default')
    ap.add_argument('--write', action='store_true', help='add sections to songs that have none')
    ap.add_argument('--force', action='store_true', help="replace every chosen song's sections")
    args = ap.parse_args()

    defaults = json.loads((ROOT / 'tools' / 'bards' / 'songs.json').read_text(encoding='utf-8'))['defaults']
    doc = json.loads(SONGS.read_text(encoding='utf-8'))
    songs = doc['songs']
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
        text = write_songs(doc)
        json.loads(text)
        SONGS.write_text(text, encoding='utf-8')
        print(f'wrote sections for {changed} song(s) to {SONGS.name}')


if __name__ == '__main__':
    main()
