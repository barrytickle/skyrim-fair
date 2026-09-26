# Compatibility: mods that give every NPC a spell (for the mod page)

Ready to paste onto the Nexus page. Skyrim Fair ships none of these mods' files. Players
make a one-word edit themselves, which needs nobody's permission. The generator's
`spidPatches` (`dist/spid/`) builds the same edits for Barry's own game only; they are
never packaged (`docs/RELEASE.md`).

To check a line, compare `dist/spid/` with the mod's own file. After any of these mods
updates, check its line again: the instructions quote it whole.

---

## Using Stealth Detection Fixes, Maximum Destruction or Strange Runes? Read this

These mods give **every NPC** in the game a spell through SPID. The fair has a big crowd,
and with that many NPCs their spells flood the game's script engine. **About a minute after
you arrive, the game freezes:** the music stops and the performers stand still.

The fix is one word per line: `-SkyrimFairNPC`. It tells SPID to skip anyone at the fair
(every fair NPC carries the `SkyrimFairNPC` keyword). Nothing changes anywhere else in
your game.

**The easy way:** download the optional **Compatibility** file (these instructions and the SPID Patcher), and run
`skyrimfair_spid_patcher.py` (it needs [Python](https://www.python.org/downloads/) 3.8 or newer).
Point it at your MO2 `mods` folder, or at Skyrim's `Data` folder for Vortex. It makes the edits
below for you, keeps a backup of each file, and can put them back with `--undo`.

**By hand:** open each file below in a text editor (it's in the mod's folder; in Mod Organizer
2, right-click the mod and choose "Open in Explorer"). Find the line, and change it as
shown. Save the file. If you update the mod later, make the edit again.

### Stealth Detection Fixes (three files)

`StealthKillDetectionFix_Attack_DISTR.ini`, change

```
Spell = 0x817~StealthKillDetectionFix.esp|NONE|NONE|NONE|NONE|NONE|NONE
```

to

```
Spell = 0x817~StealthKillDetectionFix.esp|-SkyrimFairNPC|NONE|NONE|NONE|NONE|NONE
```

`StealthKillDetectionFix_DISTR.ini`, change

```
Spell = 0x80B~StealthKillDetectionFix.esp|ActorTypeNPC|NONE|NONE|NONE|NONE|NONE
```

to

```
Spell = 0x80B~StealthKillDetectionFix.esp|ActorTypeNPC,-SkyrimFairNPC|NONE|NONE|NONE|NONE|NONE
```

`StealthKillDetectionFix_Killmove_DISTR.ini`, change

```
Spell = 0x819~StealthKillDetectionFix.esp|NONE|NONE|NONE|NONE|NONE|NONE
```

to

```
Spell = 0x819~StealthKillDetectionFix.esp|-SkyrimFairNPC|NONE|NONE|NONE|NONE|NONE
```

### Maximum Destruction (one file)

`MaximumDestruction_DISTR.ini`, the line starting `Spell = 0x8E6289`. Add
`,-SkyrimFairNPC` after `Estormo`:

```
Spell = 0x8E6289~MaximumDestruction.esp|ActorTypeNPC,Charmed Vigilant,Spellsword,Arch-Curate Vyrthur,Estormo,-SkyrimFairNPC|NONE|NONE|NONE|NONE|100
```

### Strange Runes (one file)

`StrangeRunes_DISTR.ini`, change

```
Spell = 0x68855~StrangeRunes.esp|ActorTypeNPC
```

to

```
Spell = 0x68855~StrangeRunes.esp|ActorTypeNPC,-SkyrimFairNPC
```

### Other mods like these

Any mod whose SPID file gives *every* NPC a cloak or a script can do the same. Add
`-SkyrimFairNPC` to the second field of its `Spell =` line (after a comma if the field
already has something in it, or in place of `NONE`).

**Mod authors:** adding `-SkyrimFairNPC` to your own line is harmless for players without
the fair, and saves them the edit.

---

## Checked: compatible, no patch needed

Answers given to players, each checked plugin against plugin (the fair's Tamriel footprint is
cells (-3,-4), (-3,-3), (-2,-4), (-2,-3), with no landscape edits):

- **Elysium Estate**, **Whiterun Manor**, **JG Pondside Cottage**
- **JK's Whiterun Outskirts** (1.6.2, checked 2026-09-26): all its edits are around Whiterun
  (cells 1..9, -5..3), about 14,000 units from the fair; no shared references. The only
  records both touch are the Tamriel worldspace and its persistent cell (identical to vanilla
  in both, each only adds its own references) and the navmesh info map (`012FB4`), whose
  entries the game merges across plugins; neither changes an entry the other does. Any load
  order.
