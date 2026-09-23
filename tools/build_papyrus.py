"""Compile the fair's Papyrus scripts (src/Papyrus/*.psc) to assets/scripts/*.pex.

Uses the Creation Kit's command-line compiler and the vanilla script sources it ships in
Data/Scripts.rar, which are unpacked once into build/papyrus/vanilla (git-ignored).
Command-line only: nothing opens the Creation Kit.

    python tools/build_papyrus.py --ck "C:/Program Files (x86)/Steam/steamapps/common/skyrim"
"""
import argparse
import pathlib
import shutil
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = ROOT / "src" / "Papyrus"
OUT = ROOT / "assets" / "scripts"
VANILLA = ROOT / "build" / "papyrus" / "vanilla"


def unpack(ck: pathlib.Path) -> pathlib.Path:
    """The vanilla sources and the flags file, unpacked from the CK's Scripts.rar once."""
    source = VANILLA / "Scripts" / "Source"
    if (source / "TESV_Papyrus_Flags.flg").exists():
        return source
    archive = ck / "Data" / "Scripts.rar"
    if not archive.exists():
        sys.exit(f"no {archive}: point --ck at the Creation Kit's folder")
    VANILLA.mkdir(parents=True, exist_ok=True)
    # Windows' own tar (bsdtar) reads RAR.
    tar = shutil.which("tar", path=r"C:\Windows\System32") or "tar"
    subprocess.run([tar, "-xf", str(archive), "-C", str(VANILLA), "Scripts/Source"], check=True)
    if not (source / "TESV_Papyrus_Flags.flg").exists():
        sys.exit(f"unpacked {archive} but found no TESV_Papyrus_Flags.flg under {source}")
    return source


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--ck", required=True, help="the Creation Kit's install folder")
    args = ap.parse_args()
    ck = pathlib.Path(args.ck)
    compiler = ck / "Papyrus Compiler" / "PapyrusCompiler.exe"
    if not compiler.exists():
        sys.exit(f"no {compiler}")
    vanilla = unpack(ck)
    OUT.mkdir(parents=True, exist_ok=True)
    scripts = sorted(SOURCE.glob("*.psc"))
    failed = []
    for psc in scripts:
        result = subprocess.run(
            [str(compiler), psc.name,
             f"-f={vanilla / 'TESV_Papyrus_Flags.flg'}",
             f"-i={SOURCE};{vanilla}",
             f"-o={OUT}"],
            cwd=SOURCE, capture_output=True, text=True)
        text = (result.stdout + result.stderr).strip()
        ok = result.returncode == 0 and (OUT / (psc.stem + ".pex")).exists()
        print(f"{'ok  ' if ok else 'FAIL'} {psc.name}")
        if not ok:
            print(text)
            failed.append(psc.name)
    if failed:
        sys.exit(f"{len(failed)} script(s) failed to compile")
    print(f"{len(scripts)} scripts -> {OUT}")


if __name__ == "__main__":
    main()
