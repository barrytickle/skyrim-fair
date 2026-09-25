"""Build the release: a production folder and the archive to upload to Nexus Mods.

    python tools/package.py --version 1.0.0

Build the assets first, as for a deploy (make_static_props, build_audio, build_tempo,
build_voices, build_papyrus; see CLAUDE.md). This script then:

1. Builds a release plugin from fair.config.json with the release switches (below), twice,
   and checks the two builds are byte-identical. It goes to build/release/plugin, so the
   development plugin in dist/ is untouched.
2. Copies the game files deploy.py would, less what mustn't ship (EXCLUDE), into
   dist/release/SkyrimFair-<version>/SkyrimFair-<version>/, laid out as the game's Data
   folder (what MO2 and Vortex expect at an archive's top level).
3. Checks every mesh the plugin names from the fair's own folders is in the package, and
   that nothing names an excluded folder.
4. Writes the upload archive (SkyrimFair-<version>.zip) and, beside it, NEXUS_PAGE.md (the
   mod page's text, compatibility and credits) and RELEASE_TODO.md (what's still open).

Release switches (a copy of the config, fair.release.json, written next to it for the
config's relative paths and removed afterwards): none at the moment. The welcome sign was
reserved until Barry cleared it (2026-09-25); `exterior.fairSign.reserve` still exists for a
piece that must stay out of a release without moving any FormID.
"""
import argparse
import hashlib
import json
import pathlib
import re
import shutil
import struct
import subprocess
import sys
import zlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
import deploy  # noqa: E402  (the same trees and game-file types as a deploy)

PLUGIN = "SkyrimFair.esp"
# The plugin may need only these (2026-09-25: Holidays was replaced by the fair's own lanterns,
# bunting and props). A release stops if anything else becomes a master.
MASTERS = ["Skyrim.esm"]
RELEASE_CONFIG = ROOT / "fair.release.json"
RELEASE_PLUGIN_DIR = "build/release/plugin"

# Paths under the Data folder (lowercase, "/"), left out of the package: prefixes.
EXCLUDE = {
    "meshes/stroti/": "Stroti's outhouse: can't be re-uploaded, and replaced (CREDITS.md)",
    "textures/stroti/": "Stroti's outhouse textures",
    "meshes/skyrimfair/crowd/": "the static crowd figures: switched off, the plugin places none",
    "textures/skyrimfair/skyrimfair_cobble01": "the old procedural cobble: unused",
    "scripts/skyrimfairnpcguard.pex": "the retired guard script: nothing uses it",
}

# Still open before a public upload (docs/RELEASE.md); listed in RELEASE_TODO.md.
OPEN = [
    "Check the SPID lines in NEXUS_PAGE.md against the current versions of Maximum Destruction, "
    "Stealth Detection Fixes and Strange Runes before each upload (the instructions quote their lines).",
]


def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def excluded(rel: str) -> str | None:
    low = rel.replace("\\", "/").lower()
    return next((why for prefix, why in EXCLUDE.items() if low.startswith(prefix)), None)


def build_plugin() -> pathlib.Path:
    config = json.loads((ROOT / "fair.config.json").read_text(encoding="utf-8"))
    config["outputDirectory"] = RELEASE_PLUGIN_DIR
    RELEASE_CONFIG.write_text(json.dumps(config, ensure_ascii=False, indent=2), encoding="utf-8")
    out = ROOT / RELEASE_PLUGIN_DIR / PLUGIN
    try:
        hashes = []
        for _ in range(2):
            run = subprocess.run(
                ["dotnet", "run", "-c", "Release", "--project", "src/SkyrimFair.Generator", "--", RELEASE_CONFIG.name],
                cwd=ROOT, capture_output=True, text=True)
            if run.returncode != 0 or not out.exists():
                sys.exit(f"the release build failed:\n{run.stdout[-3000:]}\n{run.stderr[-3000:]}")
            hashes.append(sha(out))
        if hashes[0] != hashes[1]:
            sys.exit("the release plugin isn't deterministic: two builds differ")
    finally:
        RELEASE_CONFIG.unlink(missing_ok=True)
    print(f"release plugin {out.relative_to(ROOT)}: sha256 {hashes[0][:16]} (twice)")
    return out


def masters(plugin: pathlib.Path) -> list[str]:
    """The plugin header's MAST entries, in order."""
    data = plugin.read_bytes()
    size = struct.unpack_from("<I", data, 4)[0]
    body, p, out = data[24:24 + size], 0, []
    while p + 6 <= len(body):
        t, n = body[p:p + 4], struct.unpack_from("<H", body, p + 4)[0]
        if t == b"MAST":
            out.append(body[p + 6:p + 6 + n].rstrip(b"\x00").decode("latin1"))
        p += 6 + n
    return out


def records(data: bytes, start: int, end: int):
    """(signature, body) of every record in a plugin (GRUPs walked, compressed bodies inflated)."""
    p = start
    while p < end:
        sig = data[p:p + 4]
        size = struct.unpack_from("<I", data, p + 4)[0]
        if sig == b"GRUP":
            yield from records(data, p + 24, p + size)
            p += size
        else:
            flags = struct.unpack_from("<I", data, p + 8)[0]
            body = data[p + 24:p + 24 + size]
            if flags & 0x40000:
                body = zlib.decompress(body[4:])
            yield sig, body
            p += 24 + size


def model_paths(plugin: pathlib.Path) -> set[str]:
    """Every model path the plugin names (MODL, MOD2..MOD5 and the like), lowercase, under meshes/."""
    data = plugin.read_bytes()
    hdr = struct.unpack_from("<I", data, 4)[0]
    paths = set()
    for _, body in records(data, 24 + hdr, len(data)):
        p = 0
        while p + 6 <= len(body):
            t = body[p:p + 4]
            n = struct.unpack_from("<H", body, p + 4)[0]
            d = body[p + 6:p + 6 + n]
            if t in (b"MODL", b"MOD2", b"MOD3", b"MOD4", b"MOD5", b"DMDL") and d.endswith(b"\x00") and d[:-1].lower().endswith(b".nif"):
                path = d[:-1].decode("latin1").replace("\\", "/").lower()
                paths.add(path[len("meshes/"):] if path.startswith("meshes/") else path)
            p += 6 + n
    return paths


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--version", required=True, help="the release's version, e.g. 1.0.0")
    args = ap.parse_args()
    name = f"SkyrimFair-{args.version}"
    top = ROOT / "dist" / "release" / name
    data_dir = top / name
    if top.exists():
        shutil.rmtree(top)
    data_dir.mkdir(parents=True)

    plugin = build_plugin()
    found = masters(plugin)
    if found != MASTERS:
        sys.exit(f"the release plugin's masters are {found}, not {MASTERS}")
    print(f"masters: {', '.join(found)}")

    # The game files, as deploy.py gathers them, less the exclusions.
    files = [(plugin, PLUGIN)]
    files += [(f, f"Seq/{f.name}") for f in sorted((ROOT / RELEASE_PLUGIN_DIR / "Seq").glob("*.seq"))]
    if not any(rel.startswith("Seq/") for _, rel in files):
        sys.exit("no Seq file next to the release plugin")
    for src, dst, skip in deploy.TREES:
        base = ROOT / src
        for f in sorted(base.rglob("*")):
            rel = f.relative_to(base)
            oar = f.name == "config.json" and "OpenAnimationReplacer" in rel.parts
            if f.is_file() and (f.suffix.lower() in deploy.GAME_FILES or oar) and not (rel.parts and rel.parts[0] in skip):
                files.append((f, f"{dst}/{rel.as_posix()}"))
    left_out: dict[str, int] = {}
    shipped = []
    for src, rel in files:
        why = excluded(rel)
        if why:
            left_out[why] = left_out.get(why, 0) + 1
            continue
        dst = data_dir / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
        shipped.append(rel.lower())
    credits = ROOT / "CREDITS.md"
    shutil.copyfile(credits, data_dir / "SkyrimFair - Credits.md")

    # Every mesh the plugin names from the fair's own folders must ship; none from an excluded one.
    own_tops = {p.name.lower() for p in (ROOT / "assets" / "meshes").iterdir() if p.is_dir()} - {"actors"}
    shipped_meshes = {r[len("meshes/"):] for r in shipped if r.startswith("meshes/")}
    missing, banned = [], []
    for path in sorted(model_paths(plugin)):
        if excluded("meshes/" + path):
            banned.append(path)
        elif path.split("/")[0] in own_tops and path not in shipped_meshes:
            missing.append(path)
    if banned or missing:
        sys.exit("the plugin names meshes the package lacks:\n  "
                 + "\n  ".join([f"excluded: {p}" for p in banned] + [f"missing: {p}" for p in missing]))

    # The upload archive: the Data folder's contents at its top level.
    archive = shutil.make_archive(str(top / name), "zip", root_dir=data_dir)

    # The optional SPID Patcher (tools/compat): the player's patcher and a readme, zipped on
    # their own, for the mod page's optional files. Not a mod: nothing to install in MO2.
    patcher_dir = top / "SPID-Patcher"
    patcher_dir.mkdir()
    patcher = ROOT / "tools" / "compat" / "skyrimfair_spid_patcher.py"
    sys.path.insert(0, str(patcher.parent))
    import skyrimfair_spid_patcher as spid  # noqa: E402
    stale = [form for forms in spid.TARGETS.values() for form in forms if form not in compat]
    if stale:
        sys.exit(f"the SPID Patcher targets lines the Compatibility page doesn't list: {stale}")
    shutil.copyfile(patcher, patcher_dir / patcher.name)
    doc = patcher.read_text(encoding="utf-8").split('"""', 2)[1].strip()
    (patcher_dir / "README.txt").write_text(doc.replace("\\\\", "\\") + "\n", encoding="utf-8")
    patcher_zip = shutil.make_archive(str(top / f"{name}-SPID-Patcher"), "zip", root_dir=patcher_dir)
    print(f"optional SPID Patcher {pathlib.Path(patcher_zip).relative_to(ROOT)}")
    size = sum(f.stat().st_size for f in data_dir.rglob("*") if f.is_file())

    # The mod page and what's still open.
    # COMPATIBILITY.md's first part is a note for us; the page takes what follows its first rule.
    compat = (ROOT / "docs" / "COMPATIBILITY.md").read_text(encoding="utf-8").split("\n---\n", 1)[1].strip()
    page = PAGE.format(version=args.version) + "\n\n" + compat + "\n\n---\n\n" + credits.read_text(encoding="utf-8")
    (top / "NEXUS_PAGE.md").write_text(page, encoding="utf-8")
    # The Nexus form's sections (Description, Installation, Features, Requirements, Shout outs),
    # with the compatibility instructions at the bottom: they must match COMPATIBILITY.md's (the
    # one to keep current), headings one level down, or the page would quote stale SPID lines.
    description = (ROOT / "docs" / "NEXUS_DESCRIPTION.md").read_text(encoding="utf-8")
    if "\n## Compatibility\n" not in description:
        sys.exit("docs/NEXUS_DESCRIPTION.md has no Compatibility section")
    embedded = description.split("\n## Compatibility\n", 1)[1].strip()
    embedded = re.sub(r"^#(#{2,5}) ", r"\1 ", embedded, flags=re.M)
    if embedded != compat:
        sys.exit("docs/NEXUS_DESCRIPTION.md's Compatibility section differs from docs/COMPATIBILITY.md: copy it across")
    (top / "NEXUS_DESCRIPTION.md").write_text(description, encoding="utf-8")
    todo = [f"# {name}: still open before uploading", ""]
    todo += [f"- [ ] {item}" for item in OPEN]
    todo += ["", "## Left out of the package", ""]
    todo += [f"- {why} ({n} files)" for why, n in left_out.items()]
    (top / "RELEASE_TODO.md").write_text("\n".join(todo) + "\n", encoding="utf-8")

    print(f"{len(shipped)} game files ({size / 1e6:.0f} MB) in {data_dir.relative_to(ROOT)}")
    for why, n in left_out.items():
        print(f"  left out {n}: {why}")
    print(f"archive {pathlib.Path(archive).relative_to(ROOT)} ({pathlib.Path(archive).stat().st_size / 1e6:.0f} MB)")
    print(f"mod page text {(top / 'NEXUS_PAGE.md').relative_to(ROOT)}; open items {(top / 'RELEASE_TODO.md').relative_to(ROOT)}")


PAGE = """# The Wanderer's Fair

A travelling festival in its own walled compound, with a stage show, a market of 33 trades,
archery, fireworks and a crowd. Version {version}.

## Requirements

- Skyrim Special Edition or Anniversary Edition. Nothing else: the plugin needs only `Skyrim.esm`.

## Recommended

- **Open Animation Replacer** (needs SKSE): the folk dance, the instruments' fast and held loops,
  and the singers' own cheer. Without it, those fall back to vanilla animations.
- **Professional Dancer** (optional): with `Dance.esp` enabled and Pandora or Nemesis run after
  installing, the crowd's dancers take five of its dances. Nothing of it ships.
- **Terrain Parallax**: the ground's parallax. A visual replacer only.

## Installing

Install with Mod Organizer 2 or Vortex ("Mod Manager Download"), and enable `SkyrimFair.esp`.

**Grass caches:** if you use a grass cache (NGIO / Grass Cache Helper NG), the fair's ground has
no grass until you regenerate your cache with the fair installed; it's then included.

**Updating:** some updates change the stage show's data, which a save keeps. If an update's notes
say so, start a new game or visit the fair from a save made before you first entered it.

## Prices

The fair's armour and gear stalls charge 20 times the usual price, so the fair is a treat, not a
shortcut. Everything else sells at normal prices, and selling to the keepers is unchanged.

## Compatibility"""


if __name__ == "__main__":
    main()
