# Skyrim Fair

A code-first Skyrim Special Edition fairground mod.

The long-term idea is a dedicated fairground with live music, generic bards and dancers, stalls, games, archery, scheduled performances, and eventually a safely-faked jousting tournament.

The first milestone is intentionally much less glamorous: prove that the repository can generate a valid Skyrim plugin from C#.

## Current milestone

`SkyrimFair.Generator` currently:

- reads `fair.config.json`
- creates a new Skyrim SE plugin with Mutagen
- writes `dist/SkyrimFair.esp`
- keeps the future stage prototype in config so later milestones can grow without throwing the scaffold away

No Creation Kit is required for this milestone.

## Requirements

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

See [docs/ROADMAP.md](docs/ROADMAP.md) for the current milestone list.
