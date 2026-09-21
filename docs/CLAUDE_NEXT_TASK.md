# Claude next task: re-site the fair near Western Watchtower and diagnose fast travel

Work from the latest `feat/bootstrap-generator` branch.

Read `docs/AUDIT.md` first. Treat it as the current source of truth for Barry's local setup.

Barry has now performed the first in-game test.

## In-game findings

The first visible prototype loaded successfully.

Observed:

- `The Wanderer's Fair` appears on the map.
- Attempting to fast travel to it does **not** move the player to the fair. The game redirects Barry back to his current location instead.
- Barry manually walked to the marker and confirmed the placed vanilla market stall/table exists in-world.
- The current site around cell `2, -2` is much hillier in actual gameplay than desired for a dense Christmas-market-style fair.
- Barry reports the terrain near the **Western Watchtower** looks substantially flatter.

Treat the successful in-world object placement as confirmation that the generator pipeline itself works.

## Goal

Find a better, flatter fairground site in the broader Western Watchtower / Whiterun tundra area without colliding with quest, dragon, civil-war, navmesh, road, settlement, or major exterior-overhaul content.

Also diagnose the prototype map-marker fast-travel failure and determine the correct implementation.

Do not modify Skyrim, MO2, Barry's saves, load order, Pandora output, DynDOLOD or Occlusion during this audit.

Do not move the generated fair yet unless Barry explicitly asks after reviewing the audit.

## 1. Audit flatter terrain near Western Watchtower

Start from the vanilla Western Watchtower area and inspect surrounding exterior cells.

Important prior finding:

- the actual Western Watchtower area and nearby cells may participate in scripted dragon / civil-war content
- previous audit specifically warned about Western Watchtower cells around grid `0,-4` / `1,-4`

Therefore:

- do **not** assume the tower cell itself is safe
- search outward from the tower for nearby flat tundra that visually feels connected to the area but avoids scripted conflict zones
- prioritise a site still reasonably close to Whiterun and roads, but with enough open space for the full fair layout

For each viable candidate report:

- Tamriel cell FormKey
- grid X/Y
- exact world coordinates for a proposed fair centre
- approximate usable flat footprint in Skyrim units
- terrain relief across the proposed footprint
- nearest road / landmark / map marker
- distance from Western Watchtower
- nearest vanilla references
- whether the candidate cell has LAND edits in the active load order
- whether it has navmesh edits
- whether it has placed-reference conflicts
- whether it is touched by civil-war, dragon, encounter, settlement, farm, road or quest content
- whether Landscape and Water Fixes, Majestic Mountains, GreatWarSkyrim, Jobs, Occlusion or other active exterior mods touch it
- any visible terrain seam / rock / slope / obstruction concerns

Rank **three** candidate areas by suitability, but do not make a final placement change.

The target footprint should be large enough for the current design direction:

- main entrance avenue
- central traders' crossing
- east/west specialist trading rows
- food/drink lane
- stage and crowd square
- games area
- outer stable / future jousting space

Prefer one broad, naturally flat area over several smaller terraces.

### Hard requirement: the market core must be flat

The fair's commercial core should be genuinely level in game. Do not recommend a site that only looks acceptable numerically if the actual footprint has enough slope to make rows of stalls, tables, crowds or stage placement look awkward.

If no naturally flat candidate is good enough, report that clearly rather than forcing the design onto unsuitable terrain.

### Plan B: purpose-built flat fairground platform

If natural terrain cannot provide a sufficiently large, safe, flat site, assess a purpose-built raised/levelled fairground as the fallback.

The intended concept is:

- a broad **perfectly flat cobblestone / stone-paved market floor**
- the central fair, trading rows and stage square sit on this level surface
- the outer edges transition back into native tundra through deliberate blending rather than a visible rectangular slab
- possible edge treatments include shallow ramps, packed-earth embankments, retaining stone, rocks, fences, hay, shrubs, steps and stall placement that hides transitions
- archery, stable and future jousting areas may remain on more natural ground outside the paved core if that improves visual integration

For this fallback, report:

- a sensible approximate platform footprint for the current fair layout
- likely platform elevation at each candidate site
- maximum height difference between the proposed flat floor and surrounding terrain
- where ramps / slopes / steps would be needed
- whether vanilla Skyrim statics can plausibly create a cobblestone or stone-paved surface
- candidate vanilla floor / road / courtyard / stone platform meshes or modular pieces, with exact EditorIDs/FormKeys where identifiable
- whether a custom static mesh would be substantially cleaner than assembling vanilla pieces
- collision implications
- navmesh implications for merchants, crowds, Garrick, Claudius and performers
- how the platform could connect safely to existing exterior navmesh
- whether the solution can avoid LAND edits
- expected compatibility implications compared with directly flattening the landscape

Do **not** build the platform during this audit.

The preferred fallback is a static/platform solution rather than editing LAND, provided it can be made visually convincing and navmeshed safely. The goal is to preserve compatibility while giving the fair a reliable flat foundation.

## 2. Check current site against the in-game observation

Revisit the current prototype site:

- cell `000095FE:Skyrim.esm`
- grid `2,-2`
- centre near `10880,-7552,-4616`

Explain why the previous numeric relief check understated the visible hilliness, if that can be determined from the heightmap sampling method or footprint size.

This is important so future terrain audits use a more representative method.

If useful, compare:

- local 512x512 relief
- larger fair-sized footprint relief
- slope gradients / elevation change over the full candidate footprint

## 3. Diagnose the map-marker fast-travel failure

The marker currently:

- appears on the world map
- is named `The Wanderer's Fair`
- uses the MapMarker base
- is in Tamriel's persistent cell
- has visible + can-travel flags set
- uses the town/village icon
- visually selects correctly on the map
- but choosing fast travel returns Barry to his current location instead of moving him

Inspect vanilla exterior fast-travel markers and compare our generated record structure.

Determine whether the marker also needs one or more of:

- a linked location
- a persistent reference flag
- a specific reference flag / record flag
- a teleport / arrival marker relationship
- a different placement cell or persistent-cell structure
- additional map-marker fields
- a valid landing coordinate convention
- a navmesh-accessible destination
- another field omitted by the current generator

Use Skyrim.esm examples close to Whiterun where possible.

Do not guess. Report the exact structural difference between our marker and a known-working vanilla marker.

If the issue can be proven and the fix is low-risk, document the exact Mutagen-side change required, but do not commit the implementation in this audit unless explicitly asked.

## 4. Record the in-game milestone

Update `docs/AUDIT.md` to record that:

- the generated plugin has now been loaded in game successfully
- the marker renders on the map
- the placed stall renders in-world
- current site is rejected for fair layout because it is too hilly in practice
- fast travel to the custom marker is currently broken
- the next site search is focused around Western Watchtower while avoiding its scripted conflict cells
- a flat cobblestone platform is the approved fallback if no suitable natural site exists

## 5. Git workflow

After the audit:

1. overwrite `docs/AUDIT.md` with the latest verified state,
2. include the three ranked site candidates and the fast-travel diagnosis,
3. include the Plan B platform assessment if no natural candidate meets the flat-floor requirement,
4. commit and push to `feat/bootstrap-generator`,
5. do not modify generator placement coordinates yet,
6. do not deploy a new ESP into MO2,
7. tell Barry the audit is pushed and highlight the recommended candidate plus the fast-travel cause.

Suggested commit message:

```text
docs: audit flatter fair sites near watchtower
```

The Git history is the audit history. `docs/AUDIT.md` should describe only the latest known state.
