# Claude task: add the static crowd prototype to the plugin

Work on `feat/bootstrap-generator`. Read `docs/CROWD.md` first: it explains what the figure
is and how it was made. This task only wires the one existing prototype into the plugin
so Barry can test it in game.

**Scope: one STAT and one test reference by the archery range. Stop for Barry's in-game
review before adding more figures or copies.**

## Implemented (2026-09-23): what changed from this brief, and why

The prototype is wired in and deployed as the brief asks: one STAT and one reference by
the archery range, then a stop for Barry's review. Three steps were done differently,
because as written they would have gone wrong:

1. **Not `market.fixed`, and not a market module at all.** `market.fixed` only takes lane
   stalls (`lane`, `at`, `side`, `theme`), so it can't hold a free-standing x/y/yaw piece.
   Free-standing modules live in `market.dressing`. But see fix 3: the market was the
   wrong place anyway.
2. **The spot moved from (240, 1560) to (580, 1640), yaw 270.**
   - The archers stand at x 100 and shoot west, at targets at x −450.
   - The real spectators are placed on the *east* side of their focus (240, 1400):
     roughly x 310–500, y 1200–1600.
   - So (240, 1560) would have put the figure between the spectators and the range,
     140 units behind lane 3's archer: in front of them, not behind.
   - (580, 1640) is east of the spectators, between lanes 3 and 4, clear of the hay pile
     (x 490–630, y 1290–1510).
   - The nearest person is 258 units away, and the nearest visible object is the hay bale
     at 239.
   - Yaw 270 was right: the market frame's forward is (sin yaw, cos yaw), so yaw 0 faces
     +Y and 270 faces west, toward the targets.
3. **Built last, so no FormID moves.**
   - A `projectStatics` entry is made before anything is placed, and a dressing entry is
     placed mid-market.
   - Either would have renumbered every later record: the vendors, bards, archers,
     dancers and crowd layers.
   - Saves hold references by FormID, and the project has avoided shifts all along
     (`reserve`, `placeLast`, the late orchestra and layers).
   - So there's a new **`fairWorld.crowdFigures`** list: each entry is a project static
     plus `places: [[x, y, yaw], ...]`. The generator builds it after everything but the
     navmesh.
   - Checked against the deployed plugin: 3 records added at the end (the STAT, the
     REFR, and the guard's FormList, below). The only other change is the navmeshes,
     which are always renumbered. Nothing else moved.

Everything else is as briefed:
- `.gitignore` gets `assets/meshes/SkyrimFair/Crowd/`.
- The STAT: OBND (−26, −35, 0) to (26, 35, 132), MODL
  `SkyrimFair\Crowd\SkyrimFairCrowd_Clapping01.nif`. Same layout as the outhouse, the
  project static already working in game.
- The footprints were regenerated, and the figure is cut from the navmesh
  (196 footprints, up from 195).
- The generator is deterministic.
- `deploy.py` copied the NIF (checksum `012524da…`).

**Added in the same pass (Barry's request, separate from this brief):** the NPC guard in
`fairWorld.npcGuard`.
- Every fair NPC is invulnerable, as children are.
- A script takes Stealth Detection Fixes' detection cloak and Maximum Destruction's
  per-effect script off each NPC as it loads.
- That clash froze the Papyrus VM at the fair's crowd density (`docs/CODEX_HANDOVER.md`).
- Without the guard the figure couldn't be tested in a game that stays up.

## What already exists

- The NIF: `assets/meshes/SkyrimFair/Crowd/SkyrimFairCrowd_Clapping01.nif`. It's static,
  unskinned, has no collision, faces +Y, and has its origin between the feet at Z = 0.
  It's 131.9 units tall; the footprint is x −20.8…25.4, y −12.0…34.9.
- The tools that build and check it: `tools/crowd/`. You don't need to change them.

If the NIF is missing (it's build output), rebuild and check it:

```
blender --background --factory-startup --python-exit-code 1 --python tools/crowd/build_crowd.py -- Clapping01
python tools/crowd/verify_crowd.py Clapping01        # must print "failures": [] and exit 0
```

`blender` here means `C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe`.
Use that 3.6 build, not Blender 5.2. The expected SHA-256 is `012524da3f79f851…`.

## 1. Keep the NIF out of git

It's built from Bethesda's geometry, as `Props/` is. Add this to `.gitignore`, next to
the `assets/meshes/SkyrimFair/Props/` line:

```
assets/meshes/SkyrimFair/Crowd/
```

Do commit `tools/crowd/` (except `__pycache__`), `docs/CROWD.md` and this file.

## 2. Add the STAT

Add this to `fairWorld.projectStatics` in `fair.config.json`:

```json
{ "editorId": "SkyrimFairCrowdClapping01", "model": "SkyrimFair\\Crowd\\SkyrimFairCrowd_Clapping01.nif",
  "width": 51, "depth": 70, "height": 132 }
```

`AddStatic` in `FairWorld.cs` turns this into a STAT with symmetric bounds. Width and depth
are twice the figure's largest |x| and |y|. `minZ` stays 0.

## 3. Place one test reference

Use the existing market-module mechanism, the same one the outhouse row uses
(`@EditorID` pieces):

- Add a module to `fairWorld.market.modules`:

```json
{
  "name": "crowd_test",
  "width": 60,
  "depth": 70,
  "vendorSpots": [],
  "pieces": [
    { "name": "CrowdClapping01", "piece": "@SkyrimFairCrowdClapping01", "x": 0, "y": 0, "yaw": 0 }
  ]
}
```

- Add a placement to `fairWorld.market.fixed`, just behind the real archery spectators.
  They're centred at `[240, 1400]`, facing 270. Something like:

```json
{ "module": "crowd_test", "x": 240, "y": 1560, "yaw": 270, "searchRadius": 200 }
```

**Turn it to face the targets.** The figure faces +Y at yaw 0. Check which way yaw turns
a module (compare the outhouse row's doors), then pick the yaw that has him looking the
same way as the real spectators. Keep him a little behind them, so real NPCs stay in front.

If a looser mechanism suits better (for example, a crowd-group entry that places statics),
use it, but keep this pass to one reference.

## 4. Navmesh

The figure has no collision, so without a navmesh cut, NPCs will walk through him.
Cutting him out of the navmesh is recommended:
1. After the layout change, run `tools/make_footprints.py` as `CLAUDE.md` describes.
2. Then run the generator again.

Check that his footprint shows up in the navmesh model list.

## 5. Build, check, deploy

1. Run the generator twice and confirm the SHA-256 matches (the generator must stay deterministic).
2. Read the plugin back with a throwaway Mutagen program and check:
   - the STAT `SkyrimFairCrowdClapping01` exists, its MODL is `SkyrimFair\Crowd\SkyrimFairCrowd_Clapping01.nif`,
     and its OBND is (−26, −35, 0) to (26, 35, 132)
   - exactly one REFR of it in `SkyrimFairWorld`, at about Z = ground level
   - compare the STAT subrecord by subrecord with vanilla's, per `CODEX_HANDOVER.md`
3. Deploy with `tools/deploy.py`, as usual. The NIF must reach
   `mods\Skyrim Fair\meshes\SkyrimFair\Crowd\`. Check that `deploy.py` picks up the new
   folder, since it's git-ignored like `Props/`.
4. Update `docs/AUDIT.md` (Current pass), `docs/CODEX_HANDOVER.md` (the placement convention:
   +Y forward, origin between the feet at Z = 0) and `CREDITS.md`:
   - The geometry is vanilla Skyrim.
   - PyNifly (GPL) is a build tool only, not bundled.

## 6. What to ask Barry to check in game

- **Where to find him:** behind the archery spectators.
- **Scale:** does he look the same height as the real NPCs?
- **Feet:** are they on the ground, not sunk and not floating?
- **Facing:** is he looking toward the range?
- **Skin colour:** do the face and hands look natural, or grey or oddly tinted? The skin
  tints are stored in the shader, which has never been tested on a static. The fallback
  is in `CROWD.md` under Limits.
- **Lighting:** is it right on the hands, and does the texture look correct everywhere?
- **Distance:** how convincing is he from 5, 15 and 30 m, next to the real spectators?
- **Frame rate:** any change? There shouldn't be one.

Don't claim anything works in game until Barry confirms it.
