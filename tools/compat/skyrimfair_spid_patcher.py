"""The Wanderer's Fair: SPID compatibility patcher.

Some mods give every NPC in Skyrim a spell through SPID (the Spell Perk Item Distributor).
The fair has a big crowd, and with that many NPCs their spells can flood the game's script
engine: about a minute after you arrive, the game freezes. The fix is one word per line,
-SkyrimFairNPC, which tells SPID to skip anyone at the fair. This script makes that edit for
you, in these mods' own files:

    Stealth Detection Fixes    StealthKillDetectionFix_Attack_DISTR.ini
                               StealthKillDetectionFix_DISTR.ini
                               StealthKillDetectionFix_Killmove_DISTR.ini
    Maximum Destruction        MaximumDestruction_DISTR.ini
    Strange Runes              StrangeRunes_DISTR.ini

How to use it (needs Python 3.8 or newer, from python.org):

    Double-click it, and paste the folder your mods are in when it asks:
      Mod Organizer 2: your MO2 "mods" folder (for example C:\\Modlists\\MyList\\mods)
      Vortex or manual installs: Skyrim's "Data" folder

    Or from a command prompt:
      python skyrimfair_spid_patcher.py "C:\\path\\to\\mods"            make the edits
      python skyrimfair_spid_patcher.py "C:\\path\\to\\mods" --dry-run  show what would change
      python skyrimfair_spid_patcher.py "C:\\path\\to\\mods" --undo     put the originals back

It only changes the exact lines listed on the fair's mod page (Compatibility), keeps a backup
of each file it changes (<file>.skyrimfair-backup), and leaves everything else alone. If a mod
has changed its line since, it says so and doesn't touch it: check the mod page for an update.
Run it again after updating one of these mods. Running it twice is harmless.
"""
import argparse
import os
import sys

KEYWORD = "-SkyrimFairNPC"
BACKUP = ".skyrimfair-backup"

# Each file, and the SPID lines in it (by their first field) that give every NPC a spell.
TARGETS = {
    "stealthkilldetectionfix_attack_distr.ini": ["0x817~StealthKillDetectionFix.esp"],
    "stealthkilldetectionfix_distr.ini": ["0x80B~StealthKillDetectionFix.esp"],
    "stealthkilldetectionfix_killmove_distr.ini": ["0x819~StealthKillDetectionFix.esp"],
    "maximumdestruction_distr.ini": ["0x8E6289~MaximumDestruction.esp"],
    "strangerunes_distr.ini": ["0x68855~StrangeRunes.esp"],
}


def find_files(root):
    """Every target file under root (a mods folder, or Data), skipping our own backups."""
    found = []
    for folder, _dirs, files in os.walk(root):
        for name in files:
            if name.lower() in TARGETS:
                found.append(os.path.join(folder, name))
    return sorted(found)


def patch_line(line, forms):
    """The line with -SkyrimFairNPC added to its second field, or None if it isn't a target.

    Returns (new_line, state): state is 'patched', 'already' or None (not a target line)."""
    body = line.rstrip("\r\n")
    ending = line[len(body):]
    stripped = body.strip()
    if stripped.startswith(";") or "=" not in stripped:
        return line, None
    key, _, value = body.partition("=")
    if key.strip().lower() != "spell":
        return line, None
    fields = value.split("|")
    if fields[0].strip().lower() not in (f.lower() for f in forms):
        return line, None
    if len(fields) == 1:
        fields.append(KEYWORD)
    else:
        names = [n.strip() for n in fields[1].split(",")]
        if any(n.lower() == KEYWORD.lower() for n in names):
            return line, "already"
        if fields[1].strip().upper() in ("", "NONE"):
            fields[1] = KEYWORD
        else:
            fields[1] = fields[1].rstrip() + "," + KEYWORD
    return key + "=" + "|".join(fields) + ending, "patched"


def read_text(path):
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw[3:].decode("utf-8") if bom else raw.decode("utf-8", errors="surrogateescape")
    return text, bom


def write_text(path, text, bom):
    data = text.encode("utf-8", errors="surrogateescape")
    with open(path, "wb") as f:
        f.write((b"\xef\xbb\xbf" if bom else b"") + data)


def patch(path, dry_run):
    forms = TARGETS[os.path.basename(path).lower()]
    text, bom = read_text(path)
    lines = text.splitlines(keepends=True)
    changed, already, seen = 0, 0, set()
    out = []
    for line in lines:
        new, state = patch_line(line, forms)
        if state:
            seen.add(line.split("=", 1)[1].split("|")[0].strip().lower())
        if state == "patched":
            changed += 1
            print(f"    {line.strip()}\n  ->{new.strip()}")
        elif state == "already":
            already += 1
        out.append(new)
    missing = [f for f in forms if f.lower() not in seen]
    if changed and not dry_run:
        backup = path + BACKUP
        if not os.path.exists(backup):
            write_text(backup, text, bom)
        write_text(path, "".join(out), bom)
    return changed, already, missing


def undo(path):
    backup = path + BACKUP
    if not os.path.exists(backup):
        return False
    os.replace(backup, path)
    return True


def main():
    ap = argparse.ArgumentParser(description="Add -SkyrimFairNPC to the SPID lines that freeze the Wanderer's Fair.")
    ap.add_argument("folder", nargs="?", help="your MO2 mods folder, or Skyrim's Data folder")
    ap.add_argument("--dry-run", action="store_true", help="show what would change, without changing anything")
    ap.add_argument("--undo", action="store_true", help="put back the original files from the backups")
    args = ap.parse_args()
    interactive = args.folder is None
    folder = args.folder
    if interactive:
        print(__doc__.split("How to use it")[0].strip())
        print()
        here = os.path.dirname(os.path.abspath(__file__))
        try:
            typed = input(f"Folder to search (MO2 'mods' or Skyrim 'Data'), or Enter for {here}:\n> ").strip().strip('"')
        except EOFError:
            typed = ""
        folder = typed or here
    if not os.path.isdir(folder):
        print(f"Not a folder: {folder}")
        return finish(interactive, 1)

    files = find_files(folder)
    if not files:
        print(f"None of the files were found under {folder}. Nothing to do.")
        print("If you use these mods, point this at the folder that holds them (MO2's 'mods', or 'Data').")
        return finish(interactive, 0)

    status = 0
    for path in files:
        rel = os.path.relpath(path, folder)
        if args.undo:
            print(f"{rel}: {'restored the original' if undo(path) else 'no backup here, left as it is'}")
            continue
        print(f"{rel}:")
        changed, already, missing = patch(path, args.dry_run)
        if changed:
            print(f"  {'would add' if args.dry_run else 'added'} {KEYWORD} to {changed} line(s)"
                  + ("" if args.dry_run else f"; backup: {os.path.basename(path)}{BACKUP}"))
        if already:
            print(f"  already done ({already} line(s))")
        for form in missing:
            print(f"  NOT FOUND: the line for {form}. The mod may have changed it: check the fair's mod page.")
            status = 2
    print()
    print("Done." if not args.dry_run else "Dry run: nothing was changed.")
    return finish(interactive, status)


def finish(interactive, status):
    if interactive:
        try:
            input("\nPress Enter to close.")
        except EOFError:
            pass
    return status


if __name__ == "__main__":
    sys.exit(main())
