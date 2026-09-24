"""Seed each stage song's timelines (drums, crowd) in songs.config.json from its stems.

    "<Blender 3.6>/3.6/python/bin/python.exe" tools/bards/build_sections.py [--write] [--force] [song ...]

(Blender's bundled Python, for numpy; ffmpeg on the path.) For each song in
songs.config.json (fair.config.json's fairWorld.audio.stage.songsFile), unpacks music/stem/<title> Stems.zip, tells the instrumental
from the vocal by its bass share (as build_vocals.py does), and reads the drums'
intensity from the bass band (build_vocals.intensity: calm / normal / intense, 2.5 s
steps, blips under 5 s merged).

Each song gets two timelines, [second, what], an entry where it changes:
- drums: rest (whoever plays the drum rests), play or intense
- crowd: dance, clap or cheer (the dancers' idles)
The seed rests the drums and claps where the drums are calm, and plays and dances
elsewhere. The timelines are Barry's to tune by hand: --write adds them only to songs that
have none, --force replaces them. Without either, they're printed.

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


def timelines(inst, defaults):
    """(drums, crowd), each [[second, what], ...] with an entry only where it changes."""
    level = {'calm': 'rest', 'normal': 'play', 'intense': 'intense'}
    drums, crowd = [], []
    for s in bv.intensity(inst, defaults)['drums']:
        d, c = level[s['level']], 'clap' if s['level'] == 'calm' else 'dance'
        if not drums or drums[-1][1] != d:
            drums.append([s['start'], d])
        if not crowd or crowd[-1][1] != c:
            crowd.append([s['start'], c])
    return drums, crowd


def write_songs(doc):
    """songs.config.json as Barry edits it: a song a block and a section a line, then the
    performers (instruments, a musician a line, the crowd's moves). Any other key is kept."""
    j = json.dumps
    lines = ['{', f'  "about": {j(doc["about"])},', '  "songs": [']
    for n, song in enumerate(doc['songs']):
        lines.append('    {')
        for k in [k for k in song if k not in ('drums', 'crowd')]:
            lines.append(f'      {j(k)}: {j(song[k], ensure_ascii=False)},')
        for name in ('drums', 'crowd'):
            entries = song.get(name, [])
            lines.append(f'      {j(name)}: [')
            lines += [f'        [{j(t)}, {j(v)}]' + (',' if i < len(entries) - 1 else '') for i, (t, v) in enumerate(entries)]
            lines.append('      ]' + (',' if name == 'drums' else ''))
        lines.append('    }' + (',' if n < len(doc['songs']) - 1 else ''))
    lines.append('  ]')
    rest = [k for k in doc if k not in ('about', 'songs')]
    for k in rest:
        lines[-1] += ','
        v = doc[k]
        if k == 'instruments':
            items = list(v.items())
            lines.append(f'  {j(k)}: {{')
            lines += [f'    {j(a)}: {j(b)}' + (',' if i < len(items) - 1 else '') for i, (a, b) in enumerate(items)]
            lines.append('  }')
        elif k in ('band', 'orchestra'):
            lines.append(f'  {j(k)}: [')
            lines += ['    ' + j(m, ensure_ascii=False) + (',' if i < len(v) - 1 else '') for i, m in enumerate(v)]
            lines.append('  ]')
        elif k == 'crowdMoves':
            moves = list(v.items())
            lines.append(f'  {j(k)}: {{')
            for i, (kind, move) in enumerate(moves):
                lines.append(f'    {j(kind)}: {{')
                fields = list(move.items())
                lines += [f'      {j(a)}: [' + ', '.join(j(x) for x in b) + ']' + (',' if n2 < len(fields) - 1 else '')
                          for n2, (a, b) in enumerate(fields)]
                lines.append('    }' + (',' if i < len(moves) - 1 else ''))
            lines.append('  }')
        else:
            lines.append(f'  {j(k)}: {j(v, ensure_ascii=False)}')
    lines += ['}', '']
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
        drums, crowd = timelines(instrumental(title, WORK / song['name']), defaults)
        shutil.rmtree(WORK / song['name'])
        print(f"{song['name']}: drums {json.dumps(drums)}; crowd {json.dumps(crowd)}")
        if (args.write and 'drums' not in song and 'crowd' not in song) or args.force:
            song['drums'], song['crowd'] = drums, crowd
            changed += 1

    if changed:
        text = write_songs(doc)
        json.loads(text)
        SONGS.write_text(text, encoding='utf-8')
        print(f'wrote timelines for {changed} song(s) to {SONGS.name}')


if __name__ == '__main__':
    main()
