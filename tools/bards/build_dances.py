"""The crowd's dance styles: Professional Dancer's clips, set up for Open Animation Replacer.

    python tools/bards/build_dances.py [--mods "E:/Modlists/Still In Skyrim/mods"]

For each style in songs.config.json's "danceStyles" ([{"name", "clip", "length"}, ...]),
copies that clip from Professional Dancer (Nexus 124608) to
assets/meshes/actors/character/animations/OpenAnimationReplacer/SkyrimFairDances/Style<NN>/
special_cicerodance1.hkx. OAR plays it in place of the vanilla Cicero dance for a fair NPC
whose Variable10 is NN, which the stage script sets before each dance (the generator writes
the submods' config.json). The clip's length is read back and checked against the file's.

The clips come from the installed mod (a folder under --mods whose name holds "Professional
Dancer"; its Dance.esp can stay disabled: only the animations are used), or else from its
archive in external/. They are Mixamo animations under the mod's CC BY-NC licence, which
doesn't allow handing the raw files on, so they are git-ignored and never shipped: a user
builds them from their own copy.

Plain Python (py7zr for the archive); the vendored PyNifly HKX codec reads the lengths.
"""
import argparse
import json
import pathlib
import shutil
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
CODEC = ROOT / 'character-actors' / 'folk-dance'
OUT = ROOT / 'assets' / 'meshes' / 'actors' / 'character' / 'animations' / 'OpenAnimationReplacer' / 'SkyrimFairDances'
WORK = ROOT / 'build' / 'dances'
ARCHIVE = 'Professional Dancer*.7z'
INSIDE = 'meshes/actors/character/animations/Dance/'


def source_dir(mods):
    """The folder holding the mod's clips: the installed mod, or the archive unpacked."""
    for d in sorted(pathlib.Path(mods).glob('*Professional Dancer*')) if mods else []:
        clips = d / 'meshes' / 'actors' / 'character' / 'animations' / 'Dance'
        if clips.is_dir():
            return clips, f'installed mod {d.name}'
    archives = sorted((ROOT / 'external').glob(ARCHIVE))
    if not archives:
        sys.exit('No Professional Dancer: install it in MO2 (its Dance.esp can stay off), or put its archive in external/')
    import py7zr
    if WORK.exists():
        shutil.rmtree(WORK)
    WORK.mkdir(parents=True)
    with py7zr.SevenZipFile(archives[0]) as z:
        names = [n for n in z.getnames() if INSIDE.lower() in n.replace('\\', '/').lower() and n.lower().endswith('.hkx')]
        z.extract(path=WORK, targets=names)
    found = next(WORK.rglob('Dance'), None)
    return found, f'archive {archives[0].name}'


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--mods', default='E:/Modlists/Still In Skyrim/mods', help="MO2's mods folder, to find the installed mod")
    args = ap.parse_args()

    sys.path.insert(0, str(CODEC / 'vendor' / 'hkx'))
    sys.path.insert(0, str(CODEC))
    import hkx_block_adapter
    hkx_block_adapter.install()
    from anim_skyrim import load_skyrim_animation

    styles = json.loads((ROOT / 'songs.config.json').read_text(encoding='utf-8')).get('danceStyles', [])
    src, where = source_dir(args.mods)
    print(f'clips from {where}')
    if OUT.exists():
        for old in OUT.glob('Style*'):
            shutil.rmtree(old)
    for n, style in enumerate(styles, 1):
        clip = src / style['clip']
        if not clip.exists():
            sys.exit(f'{style["clip"]}: not in {src}')
        sub = OUT / f'Style{n:02d}'
        sub.mkdir(parents=True, exist_ok=True)
        target = sub / 'special_cicerodance1.hkx'
        shutil.copyfile(clip, target)
        length = load_skyrim_animation(str(target)).duration
        ok = abs(length - style['length']) < 0.05
        print(f'Style{n:02d} {style["name"]}: {style["clip"]} {length:.3f} s' + ('' if ok else f'  (songs.config.json says {style["length"]}: fix it)'))
        if not ok:
            sys.exit(1)


if __name__ == '__main__':
    main()
