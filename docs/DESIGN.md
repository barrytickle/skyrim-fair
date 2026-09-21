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

Not every audience member should dance. Mixing dancers with ordinary spectators should make the crowd feel natural rather than choreographed.

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
