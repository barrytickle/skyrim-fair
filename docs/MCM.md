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

## Planned for 2.0.2: the fair comes and goes (Barry, 2026-09-26)

Players asked for the fair not to be a permanent fixture: "one that randomly appears or
appears only at certain times of the month/year/holidays. Just to keep it fresh". Build after
2.0.1.3 is tested and uploaded.

- **The schedule**, one setting (a global): **Always here** (the default; Barry: keep it, so
  no one's fair vanishes on update), **Festival days** (a few days around the lore
  festivals; check the dates against UESP), **Fair week** (the first days of each in-game
  month), **Wandering** (a weekly chance). Papyrus reads the vanilla `GameDay` / `GameMonth`
  globals, so no SKSE is needed.
- **One switch for everything outside the gate:** a persistent, initially enabled `XMarker`
  as enable parent of the exterior (gate, wall, silhouettes, outside show). The vanilla refs
  the fair disables take the same parent with "opposite", in place of the initially-disabled
  flag, so they come back when the fair leaves. The sunk large refs stay sunk (their LOD);
  the site reads as a cleared field. The Tamriel navmesh stays.
- **When it's away: Claudius's campsite** (Barry's idea, in place of a notice board): a small
  camp by the road at the gate's spot (tent, bedroll, campfire, a table with his ledger),
  enabled opposite the fair's marker. Claudius is one actor: when the fair leaves, he is
  moved to the camp and a package conditioned on the global sandboxes him there; when it
  comes back, he returns to his rounds.
- **Garrick camps with him** (Barry): moved to the camp the same way, sat by the fire playing
  his lute (the lute idle, as at the fair). No song and no audio (Barry, 2026-09-26: the
  ElevenLabs singing was too inconsistent).
- **The roof horse camps too** (Barry): moved to the camp and perched on a big flat-topped
  boulder beside it, standing and grazing up there (horses have no sit idle; a tent's slope
  would drop him). A short patrol or hold package on the rock's top, like the roof's `deck`,
  which needs a flat collision top. The Passport's horse stamp (in reach and in sight) works
  there as well.
- **Changing it:** ask Claudius, at the camp or at the fair. Asking for the fair
  back at the camp fades the screen to black (`Game.FadeOutGame`, vanilla), swaps the
  state, and fades in on the fair. Otherwise the fair only arrives or leaves while the
  player is in another worldspace or well away from it, never in view, and never while
  the player is inside.
- **No MCM** (Barry, 2026-09-26): Claudius's dialogue is the whole interface, so the fair
  keeps no requirements beyond Skyrim.esm. His lines for it: `claudius_camp` in the lines file (the 9 are recorded, in
  `cameos/claudius/mono/`, 2026-09-26).
- The Passport pauses while the fair is away; its stamps keep.
- **A courier letter when it's back** (Barry: always, no option to stop them): when the
  schedule brings the fair back, the script hands a letter from Claudius to the vanilla courier
  (`WICourier`, `WICourierScript.addItemToContainer(letter)`, no SKSE; vanilla quests use it for
  inheritance letters), who finds the player in the next town. Not sent when the player asks
  Claudius to bring it back at the camp. The letter is a book record, e.g.: "The Wanderer's Fair
  has returned to the road west of Whiterun. I have inspected it. It is present. C. Vale, Fair
  Inspector. P.S. The horse is back on the roof." One letter record, handed over each return.
