# Claude follow-up stage: prototype the landscaped fairground foundation

This task is intentionally queued **after** the current Site 1 / fast-travel retest.

Do not start this stage until Barry confirms that:

- fast travel works,
- the Pass icon is correct,
- Site 1 is acceptable as the surrounding landscape for the permanent fair.

Read the latest `docs/AUDIT.md` and `docs/DESIGN.md` before doing anything.

## Goal

Create the first visual proof of the **flat landscaped fairground foundation**.

This is a foundation-only milestone. It is not yet the full fair.

The prototype should prove that we can have:

- a perfectly flat central paved area,
- cobblestone / stone character in the centre,
- a grassy / rough-earth shoulder around the paving,
- rocky retaining edges or small cliff-like faces where the terrace sits above native ground,
- large stones and shrubs breaking up the perimeter,
- a broad natural-looking entrance transition,
- no obvious giant rectangular slab in the wilderness.

## Phase 1: inspect the local asset-authoring toolchain

Before creating assets, report what is actually available locally for Skyrim mesh work.

Check read-only for:

- Creation Kit
- NifSkope
- Blender
- Blender NIF / Skyrim export tooling if present
- Cathedral Assets Optimizer or other relevant mesh tooling
- any existing project-owned asset pipeline

Do not install new software automatically.

If the available tools are insufficient to author a Skyrim-compatible collision mesh safely, stop and document exactly what is missing rather than inventing a workflow.

## Phase 2: define a minimal project-owned tile kit

Preferred direction: a small reusable kit rather than one monolithic 3072 x 3072 mesh.

Conceptually assess:

- repeatable cobblestone floor tile
- straight edge / retaining piece
- inner / outer corner pieces if required
- broad entrance ramp / transition piece
- optional rough-earth / grass shoulder piece if that makes blending substantially cleaner

The pieces should be dimensioned on a clean Skyrim-friendly grid so they can be generated and placed from config.

Record proposed dimensions, pivots and intended collision behaviour before modelling.

## Phase 3: visual prototype

If the local toolchain supports it safely, create only enough project-owned geometry to test the concept in game.

Requirements:

- flat walkable top surface
- cobblestone or Skyrim-appropriate stone character
- no modern concrete appearance
- edge treatment should resemble rough stone / natural retaining work
- perimeter must be designed to accept rocks, shrubs, grass and clutter over the join
- do not attempt final texture art if a temporary project-owned test material is enough to prove geometry
- no LAND height edits

For the first visual test, a **small representative section** is acceptable. It does not need to cover the entire 3072 x 3072 fair immediately if a smaller section can prove the look, tiling, collision and terrain blending.

## Phase 4: placement prototype

If assets are successfully created:

- register them as project-owned statics in `SkyrimFair.esp`
- place a small test arrangement at Site 1
- include:
  - some flat paving,
  - at least one visible edge,
  - one transition / ramp if practical,
  - some vanilla rocks / shrubs / grass dressing around the edge to demonstrate the intended blend
- keep the existing fair marker available for testing
- do not add NPCs yet
- do not author or alter navmesh yet
- do not add stalls, stage, vendors, music or dancers yet

The goal is visual proof only.

## Phase 5: navmesh assessment

Do not create navmesh in this stage unless Barry explicitly approves it after seeing the visual foundation.

Instead, document:

- which exterior cells the final paved core would span
- where the existing exterior navmesh approaches the intended entrance
- where new navmesh would need to be authored
- likely join points
- whether the final platform geometry should be adjusted before navmesh work

Treat navmesh as the next dedicated milestone.

## Visual acceptance criteria

Barry should be able to stand at the site and say:

- the centre is clearly flat
- the paving feels like part of Skyrim
- the edge looks landscaped rather than artificial
- the raised portion reads like a low rocky terrace / prepared fairground
- grass and native terrain visually meet the foundation naturally
- there is no obvious "big block in a field" effect

If those conditions are not met, iterate on the foundation before adding fair content.

## Safety boundaries

Do not:

- edit LAND
- modify existing mods
- modify DynDOLOD or Occlusion
- alter Barry's saves
- touch Pandora
- add NPCs or navmesh
- commit third-party archives or assets
- use third-party meshes/textures unless their provenance and permission are explicitly confirmed

Project-owned test assets may be committed if they are genuinely created for Skyrim Fair and are safe to redistribute.

## Audit and Git workflow

When this stage is eventually run:

1. overwrite `docs/AUDIT.md` with the new verified state,
2. document the local authoring toolchain,
3. document every project-owned asset created and its path,
4. record exact dimensions / pivots / collision choices,
5. record every placed test reference and FormKey,
6. record any blocker before attempting navmesh,
7. commit and push to `feat/bootstrap-generator`,
8. do not merge the PR.

Suggested commit message:

```text
feat: prototype landscaped fair foundation
```


## Important pipeline rule: asset tools vs plugin records

The Creation Kit and Bethesda Art Tools live against Barry's separate Steam Skyrim install, while the active MO2 game uses the stock copy at `E:\Modlists\Still In Skyrim\stock`. Those two `Skyrim.esm` files are not byte-identical.

Therefore:

- use Blender 3.6 + BGS Art Tools + BGS FBX Exporter + AssetWatcher for **project-owned mesh/collision authoring only**
- use NifSkope / CAO for inspection or asset validation as needed
- do **not** save SkyrimFair.esp from the Creation Kit
- do **not** let the Creation Kit become the source of truth for plugin records
- continue creating / updating STAT and REFR records through the existing Mutagen generator against the stock Skyrim data path
- if the Creation Kit is opened for visual inspection later, treat it as a viewer/editor convenience only and document any risk from the differing master copy

This keeps the generated plugin aligned with the exact game data Barry actually loads in MO2.
