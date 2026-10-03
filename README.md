# Skyrim Fair

A code-first Skyrim Special Edition fairground mod.

The long-term idea is a dedicated fairground with live music, generic bards and dancers, stalls, games, archery, scheduled performances, and eventually a safely-faked jousting tournament. The design target is a **UK Christmas market translated into medieval Skyrim**: dense lanes, specialist food and gear stalls, warm lighting, side streets, crowds and a lively performance square.

The first milestone is intentionally much less glamorous: prove that the repository can generate a valid Skyrim plugin from C#.

## Current milestone

`SkyrimFair.Generator` currently:

- reads `fair.config.json`
- creates a new Skyrim SE plugin with Mutagen
- writes `dist/SkyrimFair.esp`
- keeps the future stage prototype in config so later milestones can grow without throwing the scaffold away

No Creation Kit is required for this milestone.

## Playing it

What a player needs: the requirements, the recommended mods, and the **SPID
exclusion needed for Maximum Destruction and Stealth Detection Fixes users**. See [`docs/RELEASE.md`](docs/RELEASE.md).

## Requirements (to build)

- .NET 10 SDK
- Skyrim Special Edition / Anniversary Edition installed on the machine that runs the generator
- a normal Skyrim load order that Mutagen can discover

The project currently uses `Mutagen.Bethesda.Skyrim 0.54.4`.

## Build

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
```

## Generate the smoke-test plugin

From the repository root:

```powershell
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

Expected output:

```text
Building Skyrim Fair...
Working location: Whiterun tundra
Prototype stage: 2 bard(s), 2 dancer(s), track 'Round the Green'

Generated: ...\dist\SkyrimFair.esp
Milestone unlocked: C# -> Mutagen -> Skyrim plugin.
```

The generated ESP is deliberately empty for now. The test is whether Skyrim accepts the generated plugin. Once that is confirmed, the next commit will place one unmistakable vanilla object in a known exterior cell.

## Sandbox cell

The plugin also carries a private interior cell, `SkyrimFairSandbox`, paved with a
5 x 5 square of the foundation kit's 1024 tiles (5,120 units, about 73 m, a side) and
open to a tundra sky. It has no doors and touches nothing in the world; it exists so
pieces can be looked at in isolation before they go anywhere near the site.

Console commands (open the console with `~`):

```text
coc SkyrimFairSandbox        go in
cow Tamriel -2 -4            come out, at the fair site
```

The `sandbox` block in `fair.config.json` controls its name, size, floor height,
lighting template and sky weather. Set `"enabled": false` to leave it out of the plugin.

## Fair worldspace (prototype)

`SkyrimFairWorld` is an isolated outdoor worldspace that will eventually hold the whole
fair. It has 121 cells of generated ground, flat inside the compound and rising into low
hills beyond it, under a tundra sky. The plan is painted into the ground (avenue,
entrance, crowd square and stage, market side, activity side). A custom wooden palisade
encloses it, with a closed Viking gate at the south end of the avenue, and a loose
forest of vanilla pines stands outside the wall. There are no stalls or NPCs yet. The
Tamriel terrace is unaffected.

The palisade and gate meshes are bundled from `assets/meshes/barry_palisades/` and
`assets/textures/barry_palisades/`. They are CC BY 4.0 models by adam127 and Sereib;
see `CREDITS.md`.

```text
cow SkyrimFairWorld 0 0                     go in, to the middle of the compound
player.moveto SkyrimFairWorldEntranceMarker the gate (also Market, Activity, Crowd, Stage)
cow Tamriel -2 -4                           come out, at the Tamriel fair site
```

The `fairWorld` block in `fair.config.json` holds the perimeter, gate, avenue and zone
geometry, the ground textures, the terrain shape, and the palisade, gate and forest
settings. `docs/AUDIT.md` has the details.

## Prototype config

```json
{
  "pluginName": "SkyrimFair.esp",
  "outputDirectory": "dist",
  "identity": {
    "name": "Skyrim Fair",
    "workingLocation": "Whiterun tundra"
  },
  "stage": {
    "enabled": true,
    "dancerCount": 2,
    "bardCount": 2,
    "track": "Round the Green"
  }
}
```

## Development approach

1. Prove plugin generation.
2. Inspect the actual local Skyrim install and active load order.
3. Place one test object.
4. Build the stage.
5. Add generic performers.
6. Integrate dance animations and custom stage music.
7. Expand into the fairground.
8. Only introduce SKSE or Papyrus where a feature actually needs runtime logic.
9. Attempt jousting after we have demonstrated basic survival instincts.

See [docs/ROADMAP.md](docs/ROADMAP.md) for the current milestone list and [docs/DESIGN.md](docs/DESIGN.md) for the fair layout, atmosphere and content direction.
