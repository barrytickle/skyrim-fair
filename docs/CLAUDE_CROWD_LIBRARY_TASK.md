# Claude task: wire the static crowd library into the plugin

Work on `feat/bootstrap-generator`. Read `docs/CROWD.md` first, especially "The library".

**Barry confirmed the prototype (`Clapping01`) works in game (2026-09-23).** The library
uses the same shaders, tint approach and placement convention, so go ahead at scale. If a
particular skin tone or hair colour looks off in the next test, retune `skins` or
`hairColours` in `tools/crowd/library.json` and run the build again. Placement doesn't change.

## Implemented (2026-09-23)

Done as briefed: 51 copies of 36 figures, deployed. See `docs/AUDIT.md`, "the crowd
library placed". What differs from the brief, and why:

- **Placements are their own append-only list, `crowdPlacements`, not `places` on each
  figure.** The brief asks for placements that can be added append-safely "in whatever
  way your builder orders records". With `places` per figure, adding a copy of an earlier
  figure would renumber every later figure's records. In one flat list, each figure's
  STAT is made at its first use, so any append only adds records at the end.
- **Seats are made non-sittable.** A seated figure sits on a real bench's marker, so an NPC
  could sit down inside him. Each seat given to a figure becomes its vanilla static twin
  (the same mesh), and the generator refuses a seat a real sitter is linked to.
- **Leaners use the horse pen's fence.** It's the only rail near the figures' 64–80 hand
  height: its lower rail is at 77–85. The hitch posts have no bar, and the woven fences
  are solid panels.
- **Not placed yet:** Standing03 and HandsBehind02. No zone needed them, and they're free
  for the next pass.

## What exists

- 37 new NIFs, plus the prototype, under `assets/meshes/SkyrimFair/Crowd/`. They're build
  output and git-ignored.
- `tools/crowd/figures.json`, one row per figure:
  - `editorId`, `model`, and STAT `width` / `depth` / `height`, measured from the NIF the
    same way as the prototype's entry
  - the pose, character and outfit
  - a `placement` note where one is needed (seated, leaning)
- The gallery, to see what each figure looks like: `build/crowd/gallery_front.jpg` and
  `gallery_side.jpg`.

Rebuild and check if needed:

```
blender --background --factory-startup --python-exit-code 1 --python tools/crowd/build_crowd.py -- --all
python tools/crowd/verify_crowd.py --all      # must end "ALL PASS 38" and rewrite figures.json
```

`blender` here means the 3.6.23 build: `C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe`.

## What to do

1. **Add the figures to `fairWorld.crowdFigures`** (your late-built list, so no FormID moves).
   - Use `figures.json` for the STAT entries.
   - **Append new figures after the existing Clapping01 entry, and always at the end from
     now on.** Otherwise earlier records get renumbered.
   - Keep adding `places` to a figure append-safe, in whatever way your builder orders
     records.
2. **Collision:** give each copy the `collision` box you gave the clapper, sized from its
   row, or a smaller body box.
3. **Placement** (Barry's design rule):
   - Static figures go in the background and midground, mixed with real NPCs. Don't put
     them right beside paths the player walks.
   - Vary the heading of every copy, and don't place the same figure twice close together.
   - **Seated figures** (`Seated01–03`): origin at the seat marker. Put each on a bench or
     chair at the same position and heading, such as the spectator benches or seated tables.
     Standing figures have their origin between the feet.
   - **Leaning figures** (`Leaning01–02`): they need a rail or fence about 64–80 units up
     and 30–38 units in front of them.
   - **Archery range first**, per the original brief: `LookFar01–02`, `Pointing01–02`,
     `Clapping02–04`, `Cheering01–03` and `ArmsCrossed01–02` behind the real spectators.
     Then the stage crowd: `ClappingHigh`, `Cheering`, `Tankard`, `Toast`, `Waving` and
     `Laughing` behind the real audience.
4. **Navmesh:** run the footprints again, as for the prototype.
5. **Build and check:**
   - The generator must be deterministic.
   - Read the plugin back with Mutagen: one STAT per figure with the expected MODL and
     OBND, and only appended records.
   - Deploy with `deploy.py`, and confirm all 38 NIFs arrive.
6. **Docs:** update `docs/AUDIT.md`, `CODEX_HANDOVER.md` (the append-only rule for crowd
   figures) and `CREDITS.md`, which lists vanilla geometry and AnimObject props, and
   PyNifly as a build tool only.

## Ask Barry to check in game

- Skin tones: the Nord, Breton, Imperial and Redguard tints.
- Hair and beard colours.
- The props: does the tankard sit in the hand, and the goblet in the toasting hand?
- Do the seated figures sit on the bench, not in it or floating above it?
- Do the leaning figures line up with their rail?
- How repetitive the faces look from 5, 15 and 30 m.
- Frame rate with the figures placed.
