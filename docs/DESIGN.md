# Skyrim Fair design brief

## North star

**The Wanderer's Fair should feel like a UK Christmas market translated into medieval Skyrim.**

The goal is not a conventional city market, military camp, or modern theme park. It should feel like a temporary festival that has grown into a small, dense village: warm, handmade, slightly chaotic, crowded with specialist traders, food, drink, music, lights, games, and little pockets of personality.

## Spatial character

Prefer:

- narrow, busy market lanes rather than huge empty spaces
- clusters of wooden stalls and tents
- banners and hanging decorations overhead
- lanterns, braziers and warm evening lighting
- benches, hay bales, barrels, crates and food displays filling dead space
- side streets branching from the main avenue
- a larger performance square around the stage
- activities such as archery and future jousting toward the outskirts
- enough ambient NPCs that the fair feels genuinely attended

The site should remain readable and navigable even when visually dense.

## Proposed layout

The entrance leads north along the main fair avenue toward the performance square and stage.

Around the middle of the avenue, a broad cross-street forms the main trading district. Specialist armour, magic and curiosity stalls can extend horizontally to the east and west, allowing the fair to grow without turning the entire site into one long corridor.

Conceptual layout:

```text
                              NORTH
                                ↑

                    ┌─────────────────────┐
                    │     MAIN STAGE      │
                    │ singer • bards      │
                    │ dancers • lights    │
                    └─────────────────────┘

                       DANCE AREA
                  crowd • cheering • drink
                       CROWD SQUARE

      MEAD / ALE                           SWEETS
      HOT PIES          MAIN LANE          FOOD
          │                │                 │
══════════╪════════════════╪═════════════════╪══════════
          │          TRADERS' CROSSING       │
══════════╪════════════════╪═════════════════╪══════════
          │                │                 │
      IMPERIAL         landmark         STORMCLOAK
       ARMOUR                            ARMOUR
          │                                │
     STEEL / IRON                       HUNTER
      BLACKSMITH                         LEATHER
          │                                │

      WEST TRADING ROW              EAST TRADING ROW

      MAGE GOODS                     ELVEN GOODS
      DWEMER CURIOS                  JEWELLERY
      BOOKS / SCROLLS                ALCHEMY

                       │
                       │ MAIN LANE
                       │

                    FAIR GAMES
               archery • targets

                    STABLES

               FUTURE JOUSTING
                   outer field

                     ENTRANCE
             THE WANDERER'S FAIR
```

This is a direction, not a rigid final blueprint. Terrain and in-game sightlines should win over diagram purity.

## Performance square

The stage is one of the main anchors of the fair.

Planned atmosphere:

- lead singer using fake/general mouth movement rather than full phoneme-perfect lip sync
- bards providing the visual performance
- stage dancers using Professional Dancer
- a small group of NPCs dancing immediately in front of the stage
- additional spectators clapping, cheering, drinking, talking and watching
- positional music centred on the stage
- `Round the Green` as the prototype signature performance
- multiple songs in the stage repertoire rather than one endlessly looping track

Not every audience member should dance. Mixing dancers with ordinary spectators should make the crowd feel natural rather than choreographed.

### Stage set / playlist behaviour

The main stage should feel like a live festival set rather than a jukebox.

Target sequence between songs:

1. current song ends
2. dancers stop / return to a neutral performance idle
3. audience performs a short applause / cheer reaction
4. brief pause of roughly **2 seconds**
5. next song starts
6. performers settle into the new track while the crowd continues watching
7. roughly **5 seconds into the new song**, stage dancers and selected crowd dancers begin dancing again

This timing is a design target rather than a frame-perfect requirement. The goal is to create a believable breath between performances and make each new track feel like a separate live number.

The stage system should support a **playlist of multiple project-owned songs**. Tracks may eventually have their own preferred dance set, crowd-reaction intensity, or performer arrangement, but the first implementation can use one shared transition sequence.

## Food and drink lane

Specialist food stalls are encouraged because they make the market feel more like a real UK Christmas market.

Initial ideas:

- sweetroll-only stall
- pie stall
- mead / ale stall or larger drinking tent
- general baked-goods stall
- cheese stall
- roast-meat stall
- fruit / produce stall
- warm or spiced drink stall where suitable

### The sweetroll stall

The sweetroll-only stall should be played completely straight.

It sells sweetrolls. That is the concept.

It can be disproportionately popular, with several ambient NPCs nearby, while more exotic merchants receive less attention. The joke should mostly be environmental rather than explained through quests or lore dumps.

## Specialist trading rows

Side streets off the central crossing should contain themed merchants and displays.

Candidates include:

- iron and steel equipment
- Imperial gear
- Stormcloak / Nord war gear
- leather and hunting equipment
- Dwemer curiosities
- Elven goods
- mage robes, staves and magical trinkets
- alchemy
- jewellery
- books and scrolls

### Imperial and Stormcloak rivalry

The Imperial and Stormcloak gear stalls should face directly across the main trading street from each other.

The visual contrast should sell the rivalry:

- Imperial stall: orderly, polished, red accents, neatly arranged shields and weapons
- Stormcloak stall: rugged, blue accents, furs, axes and rougher presentation

They should feel like cheeky rival traders rather than active military recruitment posts.

Do not use quest-critical versions of named NPCs such as Hadvar or Ralof for the actual vendors. Use safe original NPCs or generic faction-themed traders instead.

Ambient banter between the rival stalls is a possible later polish feature.

## Games and activities

Keep noisy or projectile-based activities away from the densest market lanes.

Planned or possible activities:

- archery range
- simple throwing games
- drinking game
- prize counter
- future tournament events
- future jousting field outside the main fair footprint

The initial archery implementation can use looping vanilla target-practice behaviour without scoring or schedules.

## Visual density

Density should come from both gameplay objects and non-interactive dressing.

Useful dressing includes:

- stalls and tents
- banners
- lanterns
- tables and counters
- baskets and crates
- armour and weapon displays
- food displays
- barrels and tankards
- hay bales
- benches
- fences
- braziers / fires
- stable clutter
- signs

Not everything needs to be physically interactive. Static display objects are preferred where Havok clutter would create noise or performance problems.

## Asset philosophy

Prefer, in order:

1. vanilla Skyrim assets
2. project-owned assets and music
3. clearly licensed modder resources
4. external dependencies when redistribution provenance is uncertain

Current external/resource candidates include:

- Professional Dancer for dancing
- Medieval Markets for market structures and dressing
- Incaendo's Banner Resource 2 for hanging banners

Avoid expanding the dependency list simply for convenience. The fair should remain relatively lightweight and easy to understand.

## Recurring ambient easter-egg NPCs

Two named background NPCs should appear around the fair as subtle creator cameos. They do not need quests, dialogue, follower functionality, or player interaction.

### Garrick Tallow

- Bosmer
- cheerful wandering festival bard / entertainer
- moves around the fair rather than being tied permanently to the main stage
- uses suitable vanilla bard / lute idles
- likely routes include the main lane, food stalls, trading rows and stage square
- visually warm, friendly and relaxed
- should feel like someone who is simply delighted that the fair exists
- may occasionally linger near the sweetroll stall

### Claudius Vale

- Imperial
- reserved fair inspector / records keeper
- neat, practical Imperial-flavoured clothing
- uses a vanilla ledger-writing / checklist idle where practical, inspired by the Helgen prisoner-list sequence
- tends to remain around the entrance, trader crossroads or other places that look like they require paperwork
- should visually read as methodical, observant and quietly judgemental

### Pair dynamic

Their contrast is intentional:

- Garrick = roaming, sociable, musical, upbeat
- Claudius = purposeful, administrative, exacting

They should read as a playful cat-and-dog pairing without needing dialogue to explain it.

## Tone

The Wanderer's Fair should feel:

- cosy
- lively
- crowded
- handmade
- temporary
- slightly chaotic
- humorous in small environmental ways
- recognisably Skyrim

Humour should come from the world itself, such as the inexplicably successful sweetroll stall or petty rival merchants, rather than turning the location into a parody.


## Map marker preference

For the final fair marker, use Skyrim's vanilla **Pass** map icon rather than the current Town/Village icon.

- intended marker name: `The Wanderer's Fair`
- preferred icon: `Pass`
- keep the current prototype marker behaviour only as needed for testing
- when the fast-travel issue is fixed, carry this icon preference into the implementation unless the audit identifies a technical blocker


## Concept references and the agreed visual direction

Two concept images were supplied as inspiration. Both read as concept renders rather
than in-game screenshots, so they are mood and language, not blueprints. What matters is
which parts of each we are actually taking.

### Reference A — the grand terraced approach

A broad cobbled stair climbing between stepped, curved drystone terraces, with an
open-sided timber market hall at the top, tents either side, bunting, heraldic banner
poles and braziers. Whiterun's walls behind.

**Taken from it:** the spatial idea that the fair is *ascended into* rather than walked
onto, and the dressing language — bunting, banner poles, braziers, awnings, plants
spilling over walls.

**Not taken:** the scale of the stone terracing. Measurement showed why (below).

### Reference B — the fair on a rocky rise beside the road

A signposted junction on the cobbled main road, a dirt spur curving off it, and a low
rocky knoll carrying a timber pavilion and a few tents. Stone steps up the knoll. The
edges are natural rock outcrop and grass with drystone only here and there.

**Taken from it:** the whole edge language, and the approach. This is the closer target.

### The agreed synthesis

Reference B is lower, smaller and more modest than Reference A, and dressing a grand
terraced complex to look like B would have the two fighting each other. The resolution:

1. **Content scale from A.** The footprint has to be large enough for a stage, market
   lanes, a food lane, games and specialist rows. Reference B has no room for any of it.
2. **Edge language from B.** Low, irregular, mostly natural rock outcrop, with drystone
   walling only where something is genuinely being retained. This kills the engineered
   platform read far more effectively than neat terracing would.
3. **The approach is the hero.** A signposted junction on the existing vanilla road, a
   dirt spur curving off it, and steps for the final climb. The fair should read as a
   path that has been worn to somewhere new, not as a structure dropped beside the road.

### Stair retaining-edge rule

The cheek walls beside the approach steps use the original small vanilla drystone blocks,
not a continuous purpose-built slab. Keep their irregular, handmade silhouette, but pitch
each block to the same overall descent as the stair so they do not stand as vertical
towers. They remain low and partly embedded into the bank. Separately, the structural
terrace face must close cleanly around the stair-head opening without placing geometry
or collision across the walking route.

### Explicit non-goal: the backdrop

Both references put Whiterun's walls in the mid-distance. Measured from the approved
site, that is not available:

| Landmark | Distance |
| --- | --- |
| Western Watchtower | 5,888 units |
| Whiterun outer exterior cells | 18,176 units |
| Whiterun city proper | 38,810 units |

What the site actually offers is the **Western Watchtower** in the mid-distance with
Whiterun as a distant silhouette. Barry has ruled the backdrop out of scope rather than
re-siting for it. Do not re-open this without an explicit instruction.

### Why the platform must be stepped rather than simply bigger

This is the measurement that shapes the whole foundation, taken from vanilla LAND under
the approved centre:

| Footprint | Scheme | Deepest outer wall |
| --- | --- | --- |
| 2,867 span (21 cells) | one flat level | 301u |
| 3,482 span (37 cells) | one flat level | **444u** |
| 3,482 span (37 cells) | two levels | 221u |
| 3,482 span (37 cells) | three levels | 204u |

A single flat platform at the wanted size needs a 444-unit wall on its deep side, which
is a castle plinth and exactly the blockiness being designed out. Stepping halves it.
**Size and "not blocky" are not competing goals — stepping is what buys the size.**

The ground itself is gentler than either reference suggests: the mean trend across a
3,482 footprint is about 100 units west-to-east, rising eastward, with a crown around
`Y -13,200` falling away north and far south. So the terracing is **authored**, not
terrain-following. That is on-message: a fairground prepared and levelled over years is
exactly authored terracing.

Usefully, the high ground is south-east and the road is north, so the natural
arrangement is to arrive at the low side and climb south-east into the fair — which is
the arrangement both references show.

## Fairground foundation art direction

If Site 1 is approved after the next in-game retest, the fair should use a purpose-built flat market terrace rather than trying to fit stalls to Skyrim's natural slope.

The visual target is **not** a large exposed slab.

### Core surface

- central market core should be perfectly flat
- cobblestone / stone paving should dominate the centre
- main avenue, traders' crossing, trading rows and stage square should all sit on this level surface
- the paved core can be roughly 3072 x 3072 for the first full layout pass

### Perimeter treatment

The platform should visually disappear into the tundra.

Use a layered transition:

1. cobblestone market core
2. narrow grass / rough-earth shoulder
3. rocky retaining edge or low cliff-like face where elevation is exposed
4. large stones, shrubs, grass clumps, hay, fences and clutter to break up the silhouette
5. native tundra beyond

The perimeter should be irregular rather than reading as one perfect rectangle.

### Safe build envelope is not the visible shape

Terrain and conflict analysis produced a safe **L-shaped build envelope** around Site 1 (see `docs/AUDIT.md`): the site cell plus the cells due west and due south, avoiding the Western Watchtower cells to the east, the mountain to the south and Fort Greymoor's exterior cells on the south-west diagonal.

**That L-shape is a constraint on where building is permitted. It must never become the shape the player sees.**

Within the safe area, the visible fairground footprint should feel irregular and organic — like a natural rocky terrace or a patch of prepared market ground worked into the landscape over years. The perimeter should curve, taper and vary in width so it reads as a rock formation adapted for a fair, never as a geometric letter shape, rectangle or any other obviously authored outline.

Practical consequence for the tile kit: keep the **paving** on a clean grid so it can be generated and stays seamless, but drive the **outline** irregularly — step tiles in and out, vary how far the paving reaches on each side, and hide the resulting stepped silhouette under the shoulder, rocks, shrubs and stall placement. The grid should be invisible in the final read.

### Terrain logic

- high side should meet the native ground as naturally as possible
- low side can use a rocky retaining face / small cliff treatment
- main entrance should use a broad, believable ramp or sloped approach
- avoid clean exposed vertical platform walls
- avoid making the foundation read as concrete, modern paving, or a dropped-in block
- archery, stables and future jousting can remain on natural ground outside the paved core

The intended visual story is that this is a recurring fairground that has been deliberately prepared and levelled over time.
