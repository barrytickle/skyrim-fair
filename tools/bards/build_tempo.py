"""Fast and held copies of the vanilla instrument loops, for a song's "fast" and "rest" stretches.

    python tools/bards/build_tempo.py --data "E:/Modlists/Still In Skyrim/stock/Data"

For each instrument in songs.config.json's "fast" ({"lute": 1.3, ...}: how many times
faster), extracts that instrument's vanilla loop(s) from the archives and writes a copy
that plays that much faster: every frame kept, the frame time (and so the clip's length and
its blocks') divided by the speed. The loops carry no annotations, so nothing else moves.

It also writes a held copy of every instrument's loop, for "rest": the loop's first frame
(the pose it starts from, straight from taking the instrument up) on every frame, the same
length, so the player stands holding the instrument, still, instead of putting it away.

Written to assets/meshes/actors/character/animations/OpenAnimationReplacer/SkyrimFairTempo/
<Instrument>Fast/ and <Instrument>Rest/, where Open Animation Replacer swaps them in for the
fair's musicians while the stage script holds that instrument's tempo global at 2 (fast) or
0 (rest); the generator writes the submods' config.json with those conditions. Derived from vanilla files, so git-ignored and
shipped only in the built mod, as the static props are.

Plain Python: the vendored PyNifly HKX codec (GPL-3.0, character-actors/folk-dance/vendor)
is imported, not bundled.
"""
import argparse
import hashlib
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
CODEC = ROOT / 'character-actors' / 'folk-dance'
OUT = ROOT / 'assets' / 'meshes' / 'actors' / 'character' / 'animations' / 'OpenAnimationReplacer' / 'SkyrimFairTempo'
WORK = ROOT / 'build' / 'tempo'

# Each instrument's vanilla loop clips (the bard idles' behaviour plays these).
CLIPS = {
    'lute': ['animobjectluteloop.hkx'],
    'drum': ['animobjectdrumloop.hkx'],
    'flute': ['animobjectflutelong.hkx', 'animobjectfluteshort.hkx'],
}


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--data', required=True, help='the Skyrim Data folder holding the vanilla archives')
    args = ap.parse_args()

    sys.path.insert(0, str(CODEC / 'vendor' / 'hkx'))
    sys.path.insert(0, str(CODEC))
    import hkx_block_adapter
    hkx_block_adapter.install()
    from anim_skyrim import load_skyrim_animation, write_skyrim_animation

    show = json.loads((ROOT / 'songs.config.json').read_text(encoding='utf-8'))
    speeds = show.get('fast', {})
    def source(clip):
        src = WORK / clip
        if not src.exists():
            subprocess.run([sys.executable, str(ROOT / 'tools' / 'bsa_extract.py'), '--data', args.data,
                            '--extract', f'meshes/actors/character/animations/{clip}', '--out', str(WORK)],
                           check=True, capture_output=True)
        return src

    # Held copies, for "rest": the first frame on every frame.
    for instrument, clips in CLIPS.items():
        sub = OUT / f'{instrument.capitalize()}Rest'
        sub.mkdir(parents=True, exist_ok=True)
        for clip in clips:
            src = source(clip)
            anim = load_skyrim_animation(str(src))
            for t in anim.tracks:
                t.translations = [t.translations[0]] * len(t.translations)
                t.rotations = [t.rotations[0]] * len(t.rotations)
                t.scales = [t.scales[0]] * len(t.scales)
            target = sub / clip
            write_skyrim_animation(str(target), anim, ptr_size=src.read_bytes()[16])
            back = load_skyrim_animation(str(target))
            moved = max(abs(a - b) for t in back.tracks for f in t.rotations for a, b in zip(f, t.rotations[0]))
            sha = hashlib.sha256(target.read_bytes()).hexdigest()
            print(f'{instrument} rest: {clip} held on its first frame, {back.duration:.3f} s, '
                  f'largest movement {moved:.5f}; sha256 {sha[:16]}')

    for instrument, speed in speeds.items():
        if instrument not in CLIPS:
            sys.exit(f'songs.config.json fast: no loop known for "{instrument}" (known: {", ".join(CLIPS)})')
        if not 1.0 < speed <= 3.0:
            sys.exit(f'songs.config.json fast.{instrument}: {speed} should be between 1 and 3 (times faster)')
        sub = OUT / f'{instrument.capitalize()}Fast'
        sub.mkdir(parents=True, exist_ok=True)
        for clip in CLIPS[instrument]:
            src = source(clip)
            anim = load_skyrim_animation(str(src))
            if anim.annotations:
                sys.exit(f'{clip}: has annotations, which a retime would have to move too')
            before = anim.duration
            anim.frame_duration /= speed
            anim.duration /= speed
            anim.block_duration /= speed
            target = sub / clip
            write_skyrim_animation(str(target), anim, ptr_size=src.read_bytes()[16])
            back = load_skyrim_animation(str(target))
            sha = hashlib.sha256(target.read_bytes()).hexdigest()
            print(f'{instrument} x{speed}: {clip} {before:.3f} s -> {back.duration:.3f} s, '
                  f'{back.num_frames} frames; sha256 {sha[:16]}')


if __name__ == '__main__':
    main()
