# MCM Helper: planning note

Written 2026-09-22 so later passes build toward an in-game settings menu. Nothing here is
implemented yet.

## What it is

[MCM Helper](https://www.nexusmods.com/skyrimspecialedition/mods/53000) lets a mod
define its Mod Configuration Menu with a `config.json` layout and a small Papyrus
script, rather than hand-written SkyUI MCM code. It needs **SKSE**, **SkyUI** and
**MCM Helper** installed; those become optional requirements of Skyrim Fair.

The pieces are:
- a quest, start-game-enabled, carrying a script that extends `MCM_ConfigBase`
- `Interface\MCM\Config\SkyrimFair\config.json`, the menu layout: pages, toggles,
  sliders, dropdowns
- `settings.ini` defaults, which MCM Helper saves per user
- the compile step for the script, the same one the music playlist will need (see
  `docs/MUSIC.md`)

## The rule to design by now

**Only runtime state can be an MCM setting.** The generator bakes the fair's layout
into the plugin, so wall shape, stall layout, tree count and terrain are generation-time
and stay in `fair.config.json`. The menu can switch and tune what already exists; it
cannot rebuild the layout.

So every feature that should be switchable in game should be generated as a group the
menu can flip in one go:
- **Enable-parent groups.** Give each toggleable set one persistent, initially enabled
  `XMarker` as its enable parent, so a script turns the whole set on or off with one
  `Enable()` / `Disable()` call. Candidates: the vendors, stage braziers and fire, market
  dressing, the forest backdrop, the mountains, performers.
- **Globals for values.** Anything tunable at runtime is a `GLOB` record the generator
  creates, such as music volume, the stage-show on/off, crowd density later, or the
  vendors' chance of being present. Conditions, packages and scripts read the global,
  and the MCM writes it.
- Keep these names stable (`SkyrimFair<Feature>Enabled`, `SkyrimFair<Feature>Parent`) so
  the MCM config and scripts don't break when the layout regenerates.

## Likely first settings

| Setting | Kind | Drives |
| --- | --- | --- |
| Stall-keepers present | toggle | the vendors' enable parent |
| Stage fires lit | toggle | the braziers' fire FX enable parent |
| Stage music / performance | toggle | the playlist controller (`docs/MUSIC.md`) |
| Music volume | slider | a global read by the controller, or the sound category |
| Forest and mountain backdrop | toggles | their enable parents, for performance on weaker machines |
| Travel to the fair | button | a teleport to the gate marker, for testing, until the Tamriel gate exists |

## When to build it

After the first scripted feature (the stage playlist) introduces the Papyrus compile
step. Until then, the only action is to generate new switchable features under an
enable parent. The vendors are already one group in `fairWorld.vendors` and are the
first candidate.
