# Codex handover

This is the living agent handover for Skyrim Fair. It should stay usable in both directions: Codex can take over from Claude, and Claude can later take over from Codex without relying on chat history.

The active implementation agent should treat the repository and the latest local audit as authoritative. Do not rely on chat memory when the repo says something different.

## First things to read

Before changing anything, read these in order:

1. `docs/AUDIT.md` — current verified local/build/deployment state
2. `docs/DESIGN.md` — design north star and approved visual direction
3. `docs/CLAUDE_AFTER_SITE_TEST.md` — historical foundation milestone brief; useful context, but many phases are now complete
4. `fair.config.json`
5. generator source under `src/SkyrimFair.Generator`
6. asset build script under `assets/blender/build_foundation_kit.py`

The active branch is `feat/bootstrap-generator`. Do not merge the open PR unless Barry explicitly asks.

## Project goal

The Wanderer's Fair should feel like a UK Christmas market translated into medieval Skyrim: warm, dense, handmade, lively, slightly chaotic, and recognisably Skyrim.

The current architecture is a raised, flat, irregular market terrace in the Tamriel worldspace near Whiterun, connected to an existing road by a broad ramp. The terrace is intentionally substantial rather than hidden at native grade. Its perimeter is meant to read as a natural rocky landform / prepared fairground, not as a rectangular platform.

The current site, footprint, height and entrance are approved unless Barry explicitly changes them.

## Current approved foundation state

Read `docs/AUDIT.md` for exact current values and hashes. At handover time the important state is:

- centre `X -5888, Y -13440` (pushed back 512 from the road on 2026-09-22)
- floor `Z -5336` (raised 2026-09-22 to make the terrace tall, per concept reference)
- the platform was deliberately raised by +192 from the earlier test
- irregular 22-tile paving footprint
- broad ramp connects toward the existing road
- the ramp seam to the paving is geometrically continuous
- ramp slab depth was increased to remove visible daylight beneath the head
- perimeter naturalisation uses large vanilla tundra rocks, toe rocks, rough-earth verge wedges and vegetation
- paving and ramp have a material pipeline; the current deployed test references vanilla
  `WRStoneFloor02`, while the structural retaining geometry is hidden by rock/cliff dressing
- **foundation construction method is settled** (see below): structural body carries
  collision and has no upward face, a separate zero-thickness cap carries the paving
- every kit piece now has a material; the retaining faces, corner and verge wedge
  previously had none and rendered as flat lavender default surfaces
- a paving exclusion guard prevents generated dressing from protruding through the
  usable market floor
- no LAND edits
- no NAVM edits
- no NPCs, quests, packages, music or animation records yet
- selected safe vanilla environment references are disabled where they physically conflict with the terrace
- scripted / enable-parented / unsafe references are intentionally left alone
- NGIO grass cache has not yet been regenerated, so grass may still appear through paving
- the ramp foot's eastern corner is partially covered by a walkable native bank; this is currently accepted pending in-game visual review
- the Tamriel WRLD override currently rewrites the FULL value as literal English `Skyrim`; this is known technical cleanup because vanilla stores a localised string ID

## Current gate

Do not immediately add stalls, NPCs, stage content, navmesh, final textures, or expand the platform.

A foundation polish pass has just corrected five faults Barry reported from the first
textured in-game test. The single biggest one: **every face of every piece in the kit was
wound inward**, so the whole kit rendered inside-out. That had been true since the kit was
first authored and was invisible on untextured grey geometry. See `docs/AUDIT.md`.

Barry reviewed that build and it reads much better, but still blocky. That review
produced the direction decisions recorded below, and the next implementation step is the
**approach from the road**, not further edge polish.

The project-owned procedural cobble candidate is kept as a reproducible comparison, but
it is not the current NIF material. Keep the wall/cliff decision separate from the
floor-material decision.

Likely near-term directions, only after approval:

- minor naturalisation cleanup if any rocks float, repeat too obviously, intrude too far, or clutter the clear core
- improve / texture the project-owned paving, retaining and ramp kit
- decide whether the current usable terrace needs expansion
- rough-zone the terrace for stage, market lanes, food, crowd space and specialist stalls
- only after the geometry/layout is accepted, perform a dedicated navmesh milestone

## Direction decided 2026-09-21: terraced in-world, approach as hero

Barry reviewed the first fully textured build in game and supplied two concept
references. The following are **decisions**, not suggestions. See
`docs/DESIGN.md` for the art-direction reasoning and the measurements behind them.

### Decided

1. **Stay in the Tamriel worldspace at the approved site.** Three alternatives were
   put up and rejected: an interior-cell instance, our own exterior worldspace behind
   a city door, and re-siting toward Whiterun for the backdrop. The project's north
   star is a fair worked into the landscape, and the site's placement, fast travel,
   road connection and clutter conflicts are already solved.
2. **The platform must be stepped, not just bigger.** A single flat level at the wanted
   3,482 span needs a 444-unit wall on its deep side. Two or three levels bring that to
   ~204-221. Stepping is what makes the size affordable.
3. **Footprint grew to 33 cells, 8.65 M sq units, single level, floor -5592.**
   Built. Two earlier intentions did not survive measurement and were dropped:
   - *three levels*: the ground under the footprint sits almost entirely in one
     128-unit band, so levels banded by terrain collapsed into one, and levels built
     up on flat ground made the outer walls worse (402-422u). What stops it reading
     as a plinth is that the wall now **varies 120-222** instead of a uniform
     192-288 band - the variation comes from the terrain, not from architecture.
   - *moving the site 512 south*: first rejected, because the only measurable gain was
     the deepest corner dropping 311 -> 274. **Then done anyway on 2026-09-22**, for a
     reason measurement had not been asked about: in game the terrace felt too close to
     the road. Pushing back 512 also lengthened the run to the road from 1,141 to 1,653,
     which relaxed the ramp from 1:3.8 to 1:4.3. **Centre is now X -5888, Y -13440.**
4. **Edge language follows the second concept, not the first**: low, irregular, mostly
   natural rock outcrop. **Confirmed by Barry:** rock is the *default* edge treatment;
   drystone terrace walling appears only on the long straight runs that are genuinely
   retaining something, and around the entrance. The terrace should read as a rocky rise
   someone levelled off, not as a constructed terrace.
5. **The approach from the road is the priority piece of work.** A signposted junction
   on the existing vanilla road, a dirt spur curving off it, steps for the final climb.
   It should read as a path worn to somewhere new.
6. **The Whiterun backdrop is out of scope.** Measured: the Western Watchtower is 5,888
   units away, Whiterun's outer cells 18,176, the city proper 38,810. Barry has
   explicitly ruled it out rather than re-site. Do not re-open it unprompted.

### Open

Nothing is blocking. The one remaining judgement is whether three levels survive
contact with the terrain: the site only falls about 100 units on its mean trend, so
levels that step *down* quickly end up below grade. Building levels *up* from the
current floor is the workable direction. This is a measurement question, not a decision
for Barry.

## Perimeter construction language (Barry's brief, 2026-09-22)

The fair must not read as a rectangular block with paving on top. From inside it is a
clean flat market floor; from outside the player should see a shaped, terraced hillside.
The structural platform stays artificial - the player just must not be able to tell.

Rules, in force for all future perimeter work:

- **No continuous wall.** Masonry goes in short runs with gaps, and every run dies into
  a part-buried rock rather than stopping in mid-air.
- **Battered, not vertical.** Courses step outward as they descend. The slant is the
  step-back, never a rotation.
- **Layers, with widths that vary constantly**: paving, rough verge, low stone
  retaining where needed, sloped earth and embedded rock, native tundra. No uniform
  border.
- **Tiers, not one drop.** Break the height into two or more shorter visual levels.
  They need not be walkable.
- **Asymmetry.** Per-edge masonry bias, different run lengths, one side more built than
  the other. The player must not be able to trace a rectangle.
- **Rocks embedded, never dropped on.** Sunk below the crest, outside the market floor.
- **Interior stays clean.** Naturalisation belongs on the perimeter; never scatter
  clutter across the usable paving.
- **Entrance cut into the bank**, framed by low field walls beside the player rather
  than cliff faces against it.
- **The existing road is the approach.** Do not build a separate path aimed at the fair.
- **Vanilla first.** Audit Whiterun and tundra assets before authoring anything.

Implemented as a **prototype on the north and west edges only**
(`PerimeterWall.PrototypeEdges`), so it can be judged against the older treatment on
south and east before being rolled out. See `docs/AUDIT.md` for the verified state.

## External assets: replacers need no dependency

Whiterun Stone Stairs (Nexus 147164 v1.2) was inspected and **not adopted**: it is a
pure replacer with no plugin and no new assets, so there is nothing to depend on. This
is the general rule - where a mod replaces a vanilla mesh or texture at its own path,
Skyrim Fair keeps referencing the vanilla path and the user's replacer wins at runtime.
That is already how Blended Roads improves `road01.dds` and Nordic Stonewalls improves
`Stonewall01`. Never copy a third-party asset into the project to get that effect.

## Perimeter treatment, in four layers

Barry's spec, 2026-09-22: "a slightly slanted stone wall, with shrubbery, and then a
grass hill with shrubbery and rocks". From the paving outward and down:

1. **Battered drystone wall.** Courses of `Stonewall01` (`0000099B`), the ordinary
   256-wide, 175-tall field wall, each course stepped `CourseBatter` further out than
   the one above. The slant is the step-back, not a rotation - that is how drystone
   retaining is actually built, and it keeps the face made of small repeated pieces.
   Up to three courses, covering about 525.
2. **Shrubbery** on and against it.
3. **Grass and earth bank.** `DirtCliffs02FieldGrass01` and
   `DirtCliffsIsland01FieldGrass01` are in the embankment pool - earth cliffs with
   grass tops, which is the "grass hill" layer rather than more bare rock.
4. **Shrubbery and rock** on the bank.

This replaced relying on the stair piece's own wall for the look. Scaling the stair to
2.0 had scaled that wall to 1024 x 344, which read as masonry slabs rather than farm
terracing; at 1.3 it is 666 x 224 and the coursed field walls carry the perimeter.

## Vanilla asset palette for the fair

Researched against `Skyrim.esm` so a later pass does not repeat it. All are referenced
by FormID and game path; nothing vanilla is ever copied into the project or the mod.

### Drystone terracing kit — 512 grid, 172u crest

A complete purpose-built set, already on the same grid as our paving, with tundra
`FieldGrass01` variants. Measured from the shipped meshes: the retaining face is on
**local -Y**, the crest sits at `Y -256` reaching `Z 172`, and the ground behind falls
gently to 96 by `Y +256`. It is a wall plus the field behind it, not a thin wall.

| FormID | EditorID | Size | Use |
| --- | --- | --- | --- |
| `0009CC` | `StonewallTerraceLong01` | 512 x 581 x 174 | straight run |
| `0009C6` / `000A6C` | `StonewallTerrace01` / `02` | 256 deep | short run |
| `000A74` | `StonewallTerraceCorner01` | 583 x 581 | corner |
| `000A70` / `000A73` | `StonewallTerraceEndL01` / `EndR01` | 512 x 678 | wall ends |
| `0009D0` | `StonewallTerraceStairs01` | 512 x 578 | **stairs through the wall** |
| `000A72` / `000A71` | `StonewallTerraceRampUp01` / `Down01` | 512 x 597 x 235 | ramp through the wall |

Note: `Nordic Stonewalls` in Barry's load order replaces the plain `Stonewall01/02/End`
meshes but **not** the terrace pieces, so those come from the vanilla BSA.

### Fair dressing

| FormID | EditorID | Notes |
| --- | --- | --- |
| `1083D7` | `FarmBannerPost01` | 422u tall banner post, rural vernacular; also a bunting anchor |
| `0D2025`-`0D2028` | `CityBannerWhiterun01`-`04` | MSTT, animated cloth, Whiterun horse heraldry |
| `01ED86` | `BannerAnchor01` | what a banner hangs from |
| `0F3C72` / `0F3C73` | `WHMarketStall01` / `02` | 216u Whiterun market stalls |
| `0BE2A3` / `0BE2A2` | `ImperialTentLarge` / `Small` | 318u / 187u canvas tents |
| `03AF9D` | `RoadSignPost` | 256u signpost upright |
| `0D8D9D`-`0D8DAB` etc. | `RoadSign<City>01L/R` | 28 sign boards, city names baked in |
| `093A89` | `WHfirebrazier01` | 167u Whiterun brazier |
| `08278D` | `WRBrazier01` | 121u |
| `0F491C` | `Campfire01LandOffRocks01` | firepit bedded in rocks |

### Not in vanilla — must be project-authored

- **Bunting / triangular pennants.** Genuinely absent from Skyrim. A catenary strip of
  pennants is one mesh plus one texture and we own the Blender to NIF pipeline, so this
  is project-owned with no permission question.
- **Custom signpost text** ("Whiterun Fair"). Vanilla sign boards have their city names
  baked into the texture. A custom board is a plane plus a project-owned texture.
- **A large open-sided timber pavilion.** No single vanilla equivalent. Either assembled
  from vanilla timber, clustered `WHMarketStall01/02`, or Medieval Markets, which is
  already a permissioned candidate in `CREDITS.md`.

## Expansion rule

The current 22-cell footprint is **superseded**: Barry has approved growing it to roughly
37 cells on three authored levels (see the direction decisions above). The rules below
still govern how any expansion is done.

It may be expanded later if the festival layout needs more room. Preserve the modular approach:

- extend paving only where needed
- extend the rocky / earth / vegetation perimeter with it
- keep the visible outline irregular
- avoid obvious rectangles, L-shapes or grid silhouettes
- keep archery, stables and future jousting available as natural-ground activities outside the paved core where appropriate

Do not enlarge the platform simply because more space might be useful. First prove a need from the actual festival layout.

## Road and entrance

**The entrance is a CHAIN of vanilla stair flights, not a ramp** (built 2026-09-22, to match Barry's
second concept reference). `StonewallTerraceStairs01`, `000009D0:Skyrim.esm`, is a
512-wide drystone wall with a 167-wide staircase cut through it; its treads climb 112
units over a 192 run, about 30 degrees. Measured off the shipped mesh, not the LOD.

Flights chain nose to tail: each is stepped one `StairRun` further out and one
`StairDrop` lower, which puts the top tread of each on the bottom tread of the one
above, so no landings are needed and it reads as one long staircase. Each flight brings
its own 512-wide drystone wall, so the chain also builds the stepped retaining tiers
either side of the steps.

**Three flights at scale 1.3** carry the current floor of `-5336` down to native ground, landing 8
units into grade and stopping 885 short of the road. The piece scales well - 17 risers
of about 7 units - so at 2.0 the walkable stair is 334 wide and each flight brings a
1024-wide, 344-tall drystone wall, which is the broad staircase with chunky tiers the
concept shows, which leaves the last stretch to
the dirt path still to be built. Flights are registered with the ramp-tile list so the
entrance channel and the paving guard cover the steps and the flank treatment dresses
their sides. `Entrance.UseStairs = false` falls back to a plain ramp.

**Skyrim Fair now disables no vanilla references at all.** At this floor height the four
that were disabled sit 200 to 300 units below the paving, so they are invisible and go
back to vanilla untouched.



The existing road is part of the site's visual integration.

- never disable or alter the road reference just to simplify the ramp
- preserve useful roadside details unless they physically block access
- keep a clear walking channel from road to ramp to terrace
- the final entrance should feel like an existing Skyrim road naturally rises into the fair
- later decoration can add banners, lanterns, signage and festival framing around the approach

## Asset pipeline

Project-owned mesh work is code-first and reproducible.

- Blender 3.6.23 portable is used for the Skyrim asset pipeline
- BGS Art Tools [Skyrim] and BGS FBX Exporter [Skyrim] are installed and enabled
- `assets/blender/build_foundation_kit.py` is the reproducible source for the kit
- FBX is converted to NIF using Bethesda AssetWatcher
- conversion has a 40x scale behaviour; the build script compensates using the established Blender-to-Skyrim unit factor
- static world geometry must remain unyielding / mass 0
- ramp collision uses the verified rotated child box approach
- shoulder wedge intentionally has no collision
- do not save the authoritative ESP from Creation Kit

### Permanent foundation construction method

Each paved or ramped position is built from two references at the same position,
rotation and Z:

```
structural body   collision + outer earth/rock faces, NO upward face
visual cap        zero-thickness upward polygon, paving material, NO collision
```

This is the settled method, not an experiment. It exists because a single textured box
puts paving material on vertical faces, and because adjacent 32-unit slabs show the rim
of their side faces at every tile boundary. With caps, the only upward-facing surface on
the terrace is a flat plane, so the floor reads continuous and paving cannot appear on
anything but a walking surface.

Rules that must not regress:

- **all faces wound outward.** `box()`, `sloped_box()` and `wedge()` do this, and the
  build script *asserts* it per piece. The whole kit was previously inside-out.
- **every piece carries a material.** A piece with none renders as flat lavender.
- **paving material only on upward faces.** Structural bodies use earth, not paving.
- **UV phase variants only when the texture period does not divide the tile grid.**
  Vanilla `WRStoneFloor02` repeats every 256 units, which divides 512 exactly, so phasing
  it breaks continuity. `PhasedRole` falls back to the base role when a variant is absent,
  so `project_cobble` mode can reintroduce them with no code change.
- **`DirtCliffs01` is an open shell**, face on local **-Y**, grassy cap on +Z, back absent.
  It must be rotated to face outward, offset *inward* so the missing back is buried, and
  sunk below the floor plane so its cap does not shelve across the market floor.
- **the paving exclusion guard stays.** Dressing whose crown clears the floor plane may
  not reach inside the paved footprint eroded by `pavingRimAllowance` on outer edges.
  `wallEdgeOverlap` must stay below `pavingRimAllowance`.

### Permanent material pipeline

- `assets/blender/build_foundation_kit.py` owns geometry, UVs and BGS material setup
- **default paving mode is `vanilla_road_dirt`**, using vanilla
  `textures\landscape\roads\road01.dds` and `_n.dds`. Barry chose worn earth over stone
  on 2026-09-22: WRStoneFloor02 is the material Bethesda uses for a stone CITY floor,
  and at fairground scale it made the terrace read as a slab of concrete. A fair
  pitched on prepared ground is earth; stone returns later only for the main avenue
  and stage square. `vanilla_whiterun_test` and `project_cobble` remain selectable.
- vanilla `WRStoneFloor02` is mapped at its measured native scale: one UV repeat per
  256 Skyrim units
- set environment variable `SKYRIM_FAIR_PAVING_MATERIAL=project_cobble` before the
  headless Blender build to regenerate the project-owned alternative
- the project candidate is built by `assets/textures/build_paving_material.py` and has
  diffuse, tangent-space normal/gloss and optional BC4 `_p` height outputs
- Community Shaders Extended Materials is active and supports the optional height path;
  ENB is not installed; materials must always remain convincing with height/parallax off
- physical mesh supplies large silhouette, normal maps supply surface detail, and
  optional parallax is restricted to medium/small cracks and relief
- structural bodies, retaining and corner use vanilla
  `textures\landscape\dirtcliffs\dirtcliffs01.dds`; the verge wedge uses
  `textures\landscape\fieldgrass02.dds`; both referenced by game path only
- never copy vanilla `WRStoneFloor`, dirt-cliff or grass DDS files into the project or
  mod; reference their game paths directly
- do not enable `vertex_colors_enabled` unless the mesh actually exports a vertex-colour
  layer; copying the flag from a vanilla shader that has one is a silent error
- do not adopt third-party Whiterun replacer assets without explicit reuse and
  redistribution permission

The Creation Kit and Bethesda art tools are against Barry's separate Steam Skyrim install, while the active modlist uses the stock Skyrim copy under MO2. Plugin records must continue to be generated against the stock data via Mutagen.

## Local environment

Current known paths include:

- MO2 portable root: `E:\Modlists\Still In Skyrim`
- stock Skyrim Data: `E:\Modlists\Still In Skyrim\stock\Data`
- deployed mod: `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp`
- active MO2 profile: `Still in Skyrim Plus`
- portable Blender 3.6.23: `C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\`

Do not assume these paths if the latest `docs/AUDIT.md` says otherwise.

## Build commands

The normal generator flow is:

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

After generation, structurally verify the written ESP independently where practical, then deploy only the Skyrim Fair output to the existing MO2 mod directory.

Do not touch Barry's save files, Pandora output, DynDOLOD, Occlusion, load order, or unrelated mods unless Barry explicitly asks.

## Git and documentation workflow

Barry wants the active coding agent to document the work continuously, not just dump a summary at the end.

### Keep this handover current

`docs/CODEX_HANDOVER.md` is a living cross-agent handover, not a one-time onboarding note. Whenever a **meaningful project change** occurs, update this file in the same implementation pass so another agent can take over immediately.

Meaningful changes include, for example:

- approved site / footprint / elevation / entrance changes
- new or changed asset pipeline steps
- new meshes, materials, textures, collision rules or conversion quirks
- changes to generator architecture or config structure
- newly accepted technical debt, compatibility constraints or safety rules
- completion of a major milestone such as naturalisation, floor texturing, navmesh, stage systems or NPC systems
- any new local path / tool / dependency that a replacement agent would need
- a major design decision that materially changes what should be built next

Do **not** churn this file for tiny implementation details already captured in `docs/AUDIT.md`. Keep it concise enough to onboard another agent quickly, while preserving the durable decisions and current direction.

When handing work back to Claude, Claude should read `docs/CODEX_HANDOVER.md`, then `docs/AUDIT.md`, then `docs/DESIGN.md` before making changes.

For every meaningful implementation pass:

1. read the latest `docs/AUDIT.md` before making local-dependent assumptions
2. state the plan before destructive or broad changes
3. make the smallest coherent change
4. verify it
5. overwrite `docs/AUDIT.md` with the complete latest verified state
6. include:
   - what changed
   - why it changed
   - exact coordinates / FormKeys / paths / dimensions where relevant
   - commands run
   - generated ESP size and SHA256
   - deployed ESP SHA256 and whether it is byte-identical
   - records added / removed / overridden
   - assets created or regenerated
   - verification performed
   - known deviations / unresolved issues
   - what Barry should test in game
7. if the pass changes durable project state or direction, update `docs/CODEX_HANDOVER.md` in the same pass
8. commit with a descriptive message
9. push to `feat/bootstrap-generator`
10. do not merge

Do not append old audit reports inside `docs/AUDIT.md`. Git history is the history. The file should describe the latest state in full.

If a pass involves exploratory work that does not modify the plugin, still document the findings and commit the docs if they materially affect future decisions.

## Working style

Be conservative with Barry's live modlist.

- audit first, mutate second
- report unsafe / scripted / quest-linked references rather than touching them
- prefer config-driven values over hard-coded placement coordinates
- preserve the code-first reproducible pipeline
- do not silently change approved layout decisions
- when a request has visual ambiguity, measure and report before making a large layout move
- never claim something is verified unless it was actually checked against the generated/deployed output

If a tool or local dependency is missing, stop and document the blocker rather than inventing a workaround that risks the modlist.

## Design reminders for the later fair build

Once the foundation itself is approved, the intended fair includes:

- main stage and crowd square
- live multi-song stage set with short cheer / pause transitions
- dense market lanes
- specialist armour, magic and curiosity traders
- Imperial and Stormcloak rival stalls facing one another
- food and drink lane
- mandatory sweetroll-only stall, disproportionately popular for no explained reason
- archery and other games toward the outskirts
- future jousting on natural ground
- Garrick Tallow, a cheerful Bosmer wandering bard
- Claudius Vale, a reserved Imperial records-keeper / inspector

Do not add these before Barry approves the foundation and requests the next content milestone.

## Immediate handover behaviour

On taking over:

1. inspect the latest branch and `docs/AUDIT.md`
2. verify the working tree is clean or explain any local changes
3. do not rebuild/redeploy merely for the sake of taking over
4. summarise the current state to Barry in a short handover note
5. wait for Barry's in-game feedback or next explicit task
6. from then on, own the implementation and documentation workflow above
