"""Copy the built mod into the MO2 mod folder: the plugin, meshes, textures, sound, scripts.

    python tools/deploy.py --to "E:/Modlists/Still In Skyrim/mods/Skyrim Fair"

Build first: the generator (dist/SkyrimFair.esp), tools/make_static_props.py,
tools/build_audio.py and tools/build_papyrus.py. Only changed files are copied; every
copy is checked byte-identical afterwards. Nothing is deleted from the mod folder.
"""
import argparse
import filecmp
import hashlib
import pathlib
import shutil
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# (source under the repo, destination under the mod folder, subfolders left out)
TREES = [
    ("assets/meshes", "meshes", set()),
    ("assets/textures", "textures", {"source"}),
    ("assets/sound", "Sound", set()),
    ("assets/scripts", "Scripts", set()),
]


# Only game files: the asset folders also hold build scripts and PNG sources. Open Animation
# Replacer's config.json files (conditions, under OpenAnimationReplacer/) are game files too.
GAME_FILES = {".nif", ".dds", ".wav", ".xwm", ".fuz", ".lip", ".pex", ".hkx", ".tri", ".bgsm", ".bgem"}


def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--to", required=True, help="the MO2 mod folder")
    args = ap.parse_args()
    dest = pathlib.Path(args.to)
    if not dest.is_dir():
        sys.exit(f"no mod folder {dest}")

    files = [(ROOT / "dist" / "SkyrimFair.esp", dest / "SkyrimFair.esp")]
    # SPID exclusion patches: other mods' _DISTR.ini files with -SkyrimFairNPC added. At the
    # mod folder's root they override the originals while Skyrim Fair has the higher MO2 priority.
    files += [(f, dest / f.name) for f in sorted((ROOT / "dist" / "spid").glob("*_DISTR.ini"))]
    for src, dst, skip in TREES:
        base = ROOT / src
        if not base.is_dir():
            print(f"  (no {src} yet)")
            continue
        for f in sorted(base.rglob("*")):
            rel = f.relative_to(base)
            oar = f.name == "config.json" and "OpenAnimationReplacer" in rel.parts
            if f.is_file() and (f.suffix.lower() in GAME_FILES or oar) and not (rel.parts and rel.parts[0] in skip):
                files.append((f, dest / dst / rel))

    copied = 0
    for src, dst in files:
        if not src.exists():
            sys.exit(f"missing {src}: build it first")
        if dst.exists() and dst.stat().st_size == src.stat().st_size and filecmp.cmp(src, dst, shallow=False):
            continue
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
        if sha(src) != sha(dst):
            sys.exit(f"copy of {src} is not byte-identical")
        copied += 1
        print(f"  copied {dst.relative_to(dest)}")
    print(f"{copied} of {len(files)} files copied; plugin sha256 {sha(dest / 'SkyrimFair.esp')[:16]}")


if __name__ == "__main__":
    main()
