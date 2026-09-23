# Navmesh for SkyrimFairWorld: the plan

Status: **plan only, nothing built** (2026-09-23). Written after the archers proved the
need: their training package runs after a load but stalls in a step that needs a path,
and there is no navmesh anywhere in the fair's world. Every NPC plan after this (vendors
walking to stalls, the crowd, dancers, quests) needs it too.

## What was read (vanilla and Barry's load order, with Mutagen)

**A navmesh (`NAVM`)** belongs to one exterior cell. Mutagen reads and writes every part:

- vertices (world coordinates) and triangles (three vertex indices, three edge links, flags, cover)
- **winding: counter-clockwise seen from above**, without exception (54,982 vanilla
  triangles and 4,394 in Fertility Adventures, sampled)
- **edge links:** each triangle edge holds the index of the triangle across it in the same
  navmesh, or -1 for a boundary. If the edge's `EdgeLink_*` flag is set, the value is
  instead an index into the navmesh's `EdgeLinks` list (another navmesh plus a triangle),
  which is how meshes join across cell borders
- flags: `Found` on nearly every triangle; `Water` where relevant
- a **lookup grid**: `divisor x divisor` cells over the mesh's bounds (`MaxDistanceX/Y` =
  size / divisor), each a uint32 count and that many uint16 triangle indices (read in
  Fertility Adventures: divisor 5, 3,968 wide, 793.6 per cell)
- `Min`/`Max` bounds, `NavmeshVersion` 12, the parent (worldspace and cell coordinates),
  record flag `0x40000`, form version 44
- the "CRC" is **the constant `A5E9A03C`** in every navmesh in Fertility Adventures, and
  in its NAVI entries, so it isn't computed per mesh

**The navmesh info map (`NAVI`, `012FB4:Skyrim.esm`)** has one entry per navmesh: its
parent, a point inside it, merge and preferred-merge lists, linked doors, optional
"island" data. **Plugins don't copy the whole map.** Every plugin in Barry's load order
that adds navmesh overrides NAVI with only a short list: its own entries plus about ten
vanilla entries copied unchanged, and vanilla's preferred-pathing block. That's what the
Creation Kit writes; the game merges the lists. Examples:

- `Holidays.esp` (already a master): 11 entries, 1 its own
- **`Fertility Adventures.esp`**: a new small worldspace, `FMAHuntingGrounds`, with 730
  navmeshes, the direct precedent for ours. Its entries for its own world have merge
  flag 0 and empty merge lists. Island data appears only on tiny isolated meshes with no
  links

## The approach: generated, like everything else

A new generator stage, `FairNavmesh.cs`, runs after every placement and writes the
navmesh from the same data that placed the fair. No Creation Kit, no hand editing.
Deterministic, and diffed like every other pass.

1. **Walkable area.** Inside the palisade polygon, on the flat floor (Z 0, the ground
   function for anything off it), rasterised at 32 units.
2. **Obstacles cut out** from everything the generator placed, using each base's object
   bounds, rotated and scaled, padded by an actor radius (about 35):
   - blocks: anything whose world Z range overlaps the ground to head height (about 5 to
     150) with a footprint over about 15 units: counters, tables, barrels, crates, stall
     frames, sign posts, towers, braziers, benches, the stage, the pen fences, the invisible
     collision walls, the archery targets and backstops
   - ignored: things on tables or overhead (goods, lanterns, festival lines, banners),
     ground decals, and plants without collision
   - the space behind stall counters stays walkable where it's open, so keepers stand on
     the mesh
3. **Triangulation without T-junctions**, written in the generator (no new dependency):
   the free raster cells are merged into rectangles per exterior cell. Each rectangle's
   edge is split at every neighbouring rectangle's corner, and it's fanned from a centre
   vertex. So every edge is shared exactly by the two triangles either side, none is
   degenerate, and adjacency is exact.
4. **Cell borders:** rectangles are cut at the 4,096 cell lines, so both sides of a border
   produce the same edge segments. Those edges become external links in both directions.
5. **Records:** one `NAVM` per exterior cell with walkable ground (the compound's cells
   -1..1, about 9), counter-clockwise triangles, `Found` flags, the lookup grid, bounds,
   version 12, the constant CRC. A NAVI override of vanilla's record carrying our entries
   (a point on each mesh, no merges, no islands) plus the same vanilla entries and
   preferred-pathing block the other plugins copy.
6. **FormIDs:** navmeshes and the NAVI entries are allocated after everything else, so
   no existing record moves.

## Checking it without the game

A validator (a scratchpad Mutagen program, as for every read-back) will check:

- every index in range, and every triangle counter-clockwise and not degenerate
- internal links symmetric, and external links reciprocal across each border
- every triangle in the lookup grid
- the mesh is one connected island inside the compound
- no triangle under an obstacle's padded footprint, and every NPC's standing point on the mesh

It will also render the navmesh over the fair plan (a PNG for Barry), and compare record
layouts with Fertility Adventures' subrecord by subrecord.

Optional, Barry's choice: open `SkyrimFair.esp` in the Creation Kit **read-only** and
look at it in navmesh view. Never save it there.

## Phases

1. **Ground navmesh** for the compound interior, with obstacles, and the NAVI entries.
   Test in game: do the archers shoot after a reload? Do vendors hold their spots? Does
   an NPC told to follow the player walk round the stalls?
2. **Tuning from Barry's test:** obstacle padding, gaps between stalls, the pen, the
   archery lanes, and the gate area.
3. **Later, as the content needs it:** the stage deck and steps (performers walking up,
   dancers), the pen gate for horses, door links for the Tamriel gate, and cover.

## Risks

- **A malformed navmesh can crash the game** or leave NPCs frozen. That's why the
  validator comes before the first in-game test, and why a new record type is diffed with
  a working example.
- Performance: a fine raster makes many triangles. Target a few hundred to about 1,500 a
  cell (vanilla towns run to about 1,000); the rectangle merge keeps open ground cheap.
- The NAVI merge behaviour is inferred from how every navmesh plugin in the load order
  is built, not from documentation.
- The mesh will be flat and obstacle-cut, not hand-tuned. Hand-placed details (NPCs
  squeezing between stalls) come from tuning, not from the first build.
