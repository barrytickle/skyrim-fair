# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Hotfix 2: the real cause, a BSA extractor bug (2026-09-23)

The same crash came back after the rigid-body change (same file, same instruction). Two
things settled it:
- Vanilla has its own `ElvenSwordForDisplay` STAT on the same `ElvenSword.nif`, so a
  weapon mesh as a static is fine.
- The crash was reading about 19 bytes from the end of the 69,369-byte file, past the
  64 KB first LZ4 block.

**Cause**: `tools/bsa_extract.py` decoded each LZ4 block of a frame on its own. SSE
archives link their blocks (frame flag "block independence" is 0), so matches in the
second block reach back into the first. Those read zeros, and **every file over 64 KB
came out corrupt past its first block** while keeping the right length. Five props were
affected: `ElvenSword`, `DrinkingHorn`, `HideCuirass`, `HuntingBow` and `ImperialBow`.
The first "hotfix" below was a wrong diagnosis. Its change (fixed rigid bodies instead
of unhooked links) is still the better way to make props static, so it stays.

**Fix**: the decoder now decodes into one output buffer with the whole frame as history,
and skips block checksums when the flag says they are there.
`tools/make_static_props.py` also checks each NIF's footer. That check only caught one of
the three old files it was tried on, so it is a guard, not the proof.

**Proof**: all 148 source meshes extracted by the fixed tool are **byte-identical to
Mutagen's own BSA reader** (`Mutagen.Bethesda.Archives`). Exactly the five props over
64 KB changed. The plugin is unchanged (`60d0a724b1b08410...`). **Meshes redeployed,
byte-identical.**

Earlier inspection work extracted meshes with the same tool: renders, and surface and
bound measurements. The measurements the fair relies on came from ESP bounds or from
meshes under 64 KB. Anything measured from a mesh over 64 KB before today should be
re-checked if it matters.

## Hotfix: the crash on entering the fair (2026-09-23)

Barry crashed entering the fair. CrashLogger: `EXCEPTION_ACCESS_VIOLATION` loading
`meshes\SkyrimFair\Props\ElvenSword.nif`, reading address `0xFFFFFFFF - 0x18`. That is
the "no collision" link (-1) the props tool had written into the mesh's root node, being
dereferenced as a pointer, with the rigid-body blocks left orphaned in the file. **130 of
the 148 props** carry their collision on the root node like the sword. The tower lantern
survived only because its collision is on a child node.

**Fix**: `tools/make_static_props.py` no longer touches links or BSX flags. It makes each
rigid body **fixed**, the way vanilla's unmoving barrels and crates are authored. Read
from `Barrel02.nif` and `CommonCrate01.nif` against `Bread01A.nif` and `ElvenSword.nif`:
- collision layer STATIC, in both filter copies (+4 and +36)
- inertia diagonal and mass 0 (+116, +136, +156, +180)
- motion system FIXED, deactivator, solver and quality FIXED: `05 01 01 00` at +224

Every block, link and flag is otherwise byte-identical to vanilla (the sword differs in
19 bytes, all inside its `bhkRigidBodyT`). All 148 props checked: every rigid body fixed
(136 with one body, 12 with two to four). The plugin is unchanged (`60d0a724b1b08410...`,
the props' bounds are the same). **Meshes redeployed, byte-identical.** Goods now have
solid, immovable collision.

**Test**: enter the fair again; goods should stay put when bumped.

## Current pass: festival liveliness and density, worn ground (2026-09-23)

Barry's brief: "THE STRUCTURE IS GOOD, BUT THE FAIR STILL FEELS TOO BARE... MORE LIFE, NOT
MORE LAND", then "i have a mod called 'Holidays' can we try and use some of the
decorations", "audit & implement the whiterun and the parallax mod", and "the cobblestone
for like the center path, and then the dirt parallax textures for the outside areas, but
make them patchy... make it look like the festival is used and the ground is worn". The
layout is unchanged: same 33 stalls, same lanes, avenue kept clear.

### Numbers (read from the written ESP)

| | Before | After |
| --- | --- | --- |
| Market pieces | 301 | 1,922 (1,056 of them physics-free goods) |
| Dressing groups | 47 | 68 |
| Festival line crossings | 0 | 21 (42 poles, 42 rope halves, 75 hanging lanterns) |
| Visitors | 0 | 34 (plus 59 stall-keepers and 4 archers) |
| Real lights | 14 | 18 (3 stall lights, 1 range brazier) |
| Plugin | 423 KB | 561 KB, masters `Skyrim.esm, Holidays.esp` |

### Fixed on the way: sunk stalls and keepers inside tables

`WRMarketStand01` (the `grand_double` stalls: Imperial, Stormcloak and four more) has its
origin at its tabletop with legs 72 below (bounds minZ -72). Vanilla stands it about 75 up
(Carlotta's: her ground items sit 71-74 below the stand). We placed it at ground level, so
its table was at ankle height, its canopy at head height and its keepers stood inside the
table. It now stands at 75, and its keepers behind the table (y -145). The
`whiterun_stand` and `canvas_wide` keepers moved behind their `WRMarketStand02` counter
too (y -105 and -100). This is probably what Barry saw as the Dawi "breaking with the
vendor table".

### Goods: physics-free static props

The goods vanilla puts on its market stalls (cheese, bread, bottles, weapons, pelts) are
loose havok items: they fall, get knocked off and can be stolen, which is what happened to
the tower lantern. **`tools/make_static_props.py`** (replaces `make_tower_lantern.py`)
copies each vanilla mesh listed in `tools/static_props_sources.json` out of the BSAs with
its rigid bodies made fixed (see the hotfix above; the first build unhooked them and crashed), into `meshes\SkyrimFair\Props\` (**generated, git-ignored**, a
modified Bethesda mesh). It measures bounds into `tools/static_props.json` (committed),
and the generator makes a STAT `SkyrimFairProp<Name>` for each (`@Prop<Name>` in modules).
**148 props**:
- **Food**: bread, cheese, vegetables, sweetrolls, pies and treats.
- **Drink**: meads, wines, tankards and a drinking horn.
- **Tableware**: pottery, silver and baskets.
- **Arms and armour**: iron, steel, Imperial, Stormcloak, hide, elven and dwemer weapons,
  armour and shields.
- **Hunting**: bows, arrows and pelts, plus hanging game and hanging herbs.
- **Valuables and curios**: jewellery, soul gems, dwemer curios, potions, books and scrolls.
- **Instruments**: lute, drum and flute.
- **Sacks** and a food barrel.

11 skinned meshes (books, bows, hanging game) have fallback bounds.

### Stall individuality: five layers from theme kits

Each stall module now has **slots** in its own frame, measured from the shells:

| Slot | What it is |
| --- | --- |
| counter strips | Counter tops: Windhelm stalls at 77, `WRMarketStand02` at 76, `WRMarketStand01` table at 77 once raised, the display shelves' tiers, wooden tables at 62. |
| hang lines | Under the canopies. |
| side and rear spots | On the ground. |
| sign spot | Where the identity marker stands. |

A **stall kit** per theme (25 kits covering all 38 themes) fills those slots from a
library of **111 vignettes** (small scenes such as a cheese board, a sack pile, a barrel
table or a pelt rack):
1. **Structure**: the existing shell.
2. **Front display**: goods laid along every counter strip, cycling the kit's vignettes.
3. **Side clutter** and 4. **rear storage**: one vignette per ground spot, or none now and
   then.
5. **Identity**: a vanilla shop sign hung between two `SignWRPost01` posts, perpendicular
   to the street as vanilla hangs them (posts about 90 apart, sign at 168), usually with a
   snowberry wreath.

Hang lines carry herbs, game or coloured lanterns, depending on the kit.

Examples:
- **Sweetroll**: platters and plates of sweetrolls, a honey sign, coloured lanterns, a
  stall light, the biggest queue.
- **Food**: cabbages, potatoes, apples, gourds, garlic hanging, produce spilling onto the
  ground.
- **Drink**: meads and tankards, Honningbrew sign, mead barrels, a barrel table with
  stools.
- **Roast**: meats and fish, hanging game, a spit roast beside it.
- **Smith**: swords, axes and helmets, weapons standing in a barrel.
- **Imperial**: red rug with an Imperial helmet and shield. **Stormcloak**: pelts, a
  Stormcloak helmet, axes and a war horn.
- Also jewellery on a cloth, dwemer curios, potions, books and candles.

### Overhead: festival lines (Holidays)

At intervals along every lane, a pole stands each side, 35 inside the corridor edge. That
is the only room there is, because stalls line both sides, and a 16-wide pole does not
impede walking. A pennant rope is swagged between the poles:
- **Poles**: vanilla `WHIntWoodLogVerticalThin01` with a `WHIntWoodLogVerticalThinShort01`
  cap, 381 tall.
- **Ropes**: two mirrored halves of the vanilla Solitude festival line
  (`SRopefestivalLine01`, which starts 53 from its origin, runs 682 and rises 150), in
  **Holidays' colourways**: Whiterun, Saturalia, Riften and Windhelm, and **one Imperial
  and one Stormcloak half** meeting over the rival stalls. The low middle is at about
  330-350, just above the tallest canopy (281).
- **Lanterns**: Holidays' coloured animated lanterns follow the curve of most crossings.

Runs cover the Avenue, EastLane, EastWallWalk, EastCross and EastEntry: 21 crossings, 4
refused where a junction or table stood.

### Archery: a county-fair attraction

The keep-out is now just the four lanes, from the backstops to just behind the firing
line (x -760 to 260, y 860 to 1940), so the range can be dressed round its edges. Added:
- An **attendant booth**: a table of bows and arrows, a barrel of arrows, a lantern and
  Solitude's fletcher sign.
- A **prize booth**: a sweetroll platter, meads, a silver goblet, an amulet, a wolf pelt
  and a sign stand.
- **Spectator benches** on both long sides (benches, stools, hay bale, barrel with
  tankards).
- A **scoreboard** (sign stand, stool, parchment).
- **Range storage** (spare target leaning on arrows and crates).
- A **hay** cluster and a **brazier**.
- Three **spectators** behind the firing line.

Read back: **0 new references in any line of fire**.

### Hotspots and uneven crowds

Visitors are the stall-keepers' kind of NPC under the name "Fair Visitor": vanilla faces
from the fair's own face lists, the stay-at-location package. They are placed in loose,
facing groups round what draws people, and never inside a stall, dressing group or pole:

| Where | Visitors |
| --- | --- |
| Sweetroll queue | 4 |
| Mead / drink stall | 3 |
| Roast | 3 |
| Stage approach | 5 |
| Round braziers | 5 |
| Archery line | 2 |
| Traders' Crossing (EastLane x EastCross, 3780, 1000) | 2 |
| Picnic tables | 4 |
| Cook fires | 2 |
| Bakery, pies, jewellery | 1 each |
| Imperial rivals | 1 |

Group sizes vary by one either way, and not every table or fire gets a group, so the
density has a rhythm. A few groups got no one where their ground was already full (cheese,
Stormcloak, dwemer, smith).

### Micro-clusters, wall pockets, picnic

- **Micro-clusters**: 11 placed (barrel with tankards and stools, crate, sack and basket,
  delivery handcart, woodpile with chopping block, hay with basket and bread, bench with
  lantern, trader's cart). They fill gaps between stalls with the spiral fitter, which
  respects lanes, keep-outs and stalls.
- **Wall pockets**: 7 placed, irregular along the palisade (woodpiles, stacked crates,
  barrels, a camp).
- **Picnic**: the seating runs now mix `picnic` with `picnic_busy` (tankards, bread, cheese,
  a jug and a lantern on the table, a stool and a hay bale dragged in), `picnic_mixed`
  (round table, stools, a crate) and `picnic_hay` (hay bales round a crate).
- **The picnic lantern** was vanilla `Lantern`, a havok object that can fall. It's now the
  static copy.

### Worn ground and the cobbled avenue

Five fair-owned landscape textures (`SkyrimFairGround*`). Each copies a vanilla LTEX, so
grass, footsteps and friction carry over, and has its own texture set whose **height slot
names a parallax map**:
- **Grass** from `LFieldGrass01`: the base, keeps its grass.
- **Dirt-grass** from `LFieldDirtGrass01`: patchy almost everywhere, more where walked.
- **Dirt** from `LDirt02`: bare patches following wear, broken by noise at two scales.
- **Path** from `LDirtPath01`: trodden where traffic is heaviest.
- **Cobble** from `LSnowCobble01` (stone footsteps), retextured to
  `Architecture\Whiterun\WRStoneFloor01` with its `_n` and `_p` maps. It runs the length
  of the avenue, 500 wide, its edge wandering ±110, some stones sunk under dirt, and
  scuffed bare at the fringe.

**Wear** is a field painted *after* everything is placed. It rises round:
- stall fronts and floors, stall-keepers and visitors
- dressing groups (picnics, fires, camps)
- the archery firing line and target area
- every market lane's corridor
- the entrance forecourt and the crowd square

The old flat zone colours, only ever plan markers, are no longer painted. LAND FormIDs are
unchanged: heights are still built with the cells, and only the texture layers are added
at the end. At most 6 layers per quadrant.

**The two texture packs are not bundled.** They replace vanilla textures, so they are
optional, and Barry installs them in MO2:
- **Whiterun Mossy Wet Stonefloor – Grey 2k** (Nexus 99294) supplies
  `wrstonefloor01/02` with `_n` and `_p`. Without it the cobbles use vanilla
  `WRStoneFloor01`.
- **Terrain Parallax 1.5 – 4K2K** (Nexus 54860) supplies about 50 landscape textures with
  `_p` maps.

The modlist has Community Shaders with **Terrain Helper** (its DLL looks up terrain
parallax maps), Terrain Blending and Terrain Variation. The fair's texture sets name their
`_p` maps explicitly. Whether parallax shows in game is **unverified**.

### Holidays as a master

`Holidays.esp` (Nexus 1533, v2.20 Alpha 1, installed in MO2) is now a **master** of
SkyrimFair.esp. The fair places its records (rope colourways, lanterns, apple basket, mead
crate, platter, sign stand) and ships none of its files. In the "Still in Skyrim Plus"
profile it is enabled and loads at 141, before SkyrimFair.esp at 161. The generator now
writes against an explicit load order (Skyrim, Update, the DLCs, Holidays) instead of the
stock game's plugin list, which does not know Holidays.

### Verification

- Generator run twice: identical SHA256 `60d0a724b1b08410...` (560,509 bytes). **Deployed
  byte-identical**, with `meshes\SkyrimFair\Props\` (147) and `TowerLantern.nif`.
- Masters read back: `Skyrim.esm, Holidays.esp`.
- Ground texture sets read back with diffuse, normal and height paths; grass kept on the
  grass layer.
- **0** references in the archery lines of fire. **0** low references in the gate-to-stage
  sightline band (only rope lines and lanterns overhead).
- Plan drawn from the written ESP (`docs/images/lively_plan.png`: ground colour by texture,
  blue keepers, red visitors, yellow archers, black poles, pink ropes, orange lights): cobbled
  avenue, worn market, patchy field, lines across the lanes, visitors at the hotspots.
- Stall renders from the customer's side (`docs/images/lively_stalls.png`): goods on the counters, the raised
  `WRMarketStand01` table, sacks and crates spilling in front, the spit roast.

### Known and not done

- **Performance**: 97 actors in the fair (59 keepers, 34 visitors, 4 archers), 18 real
  lights, and about 1,900 small statics. `fairWorld.crowds.enabled` turns the visitors off.
- **Not seen in game**: counter heights, small item orientation (tilted leeks, lying
  swords and bows, the lute), hang heights, and how the cobble texture tiles at terrain
  scale (it's an architecture texture on landscape).
- Skinned props (bows, books, hanging game) use fallback bounds and may sit oddly.
- Holidays is Alpha; if a Holidays update renumbers records, the fair's references break.
  Pin the version.
- No navmesh yet; everything was placed with it in mind (poles at corridor edges, nothing
  in the lines of fire or the sightline).

### Test

1. **Walk the avenue** from the gate to the stage. Is it easy? Do the cobbles read as
   cobbles, and how big do the stones look on terrain?
2. **Stalls**: can you tell what each sells from the path? Are the goods on the counters
   (not floating or sunk)? Check the raised Imperial/Stormcloak tables and the keepers
   behind them.
3. **Festival lines**: height, sag and colours; the rival Imperial/Stormcloak crossing.
4. **Archery**: booth, prize table, benches. Do the archers now shoot? (The stand-marker
   fix is in this build.)
5. **Crowds**: does the sweetroll queue read as the most popular thing at the fair?
6. **Ground**: install the two texture packs in MO2 first, then check the worn patches,
   the parallax, and whether anything looks too uniform.
7. **Frame rate** in the market at night.

## Previous pass: tower fixes and working archery (2026-09-22, late)

Barry tested the towers and the range: "The lantern doesn't glow nor does it sit in the
tower top, can we use fireFX to just make the lanterns glow?", "The flags on the towers
seem to be going inside of the tower rather than hanging off naturally", and "the archery
doesn't seem to be doing its animation... people stood there with bows".

**1. The lantern fell to the ground.** Vanilla `CandleLanternwithCandle01` is havok
clutter (a `bhkRigidBody`, BSX `0x9b`). Spawned on the deck, inside the tower's collision,
it was pushed out and dropped. **Fix**: `tools/make_tower_lantern.py` extracts it from
the BSA and unhooks its physics (the one collision link set to none; BSX havok, complex
and articulated bits cleared, to `0x11`, which keeps the flicker animation and the candle
add-on). The result is `meshes\SkyrimFair\TowerLantern.nif`, record
`SkyrimFairTowerLantern`. It's a modified Bethesda mesh, so it is **generated, git-ignored
and shipped only in the built mod**. **Glow**: inside the lantern, a flame
(`FXfireWithEmbersLight` `033DA9` at 0.3), a soft halo round it (`FXGlowFillRoundMid`
`02EB0E`, the vanilla ambient glow sphere, placed 498 times in vanilla, at 0.45, about
115 across), and the `WRFireLightNS` light as before.

**2. The banners leaned into the tower.** Every one of the 33 vanilla placements of
`CityBannerWhiterun01InsideTall` is tilted 23 degrees back about the banner's local Y (Dragonsreach:
`(0,-23,0)`, `(0,23,180)`, `(-23,0,90)`, `(23,0,270)`), because the cloth leans in the
mesh. The towers now apply the same tilt (`bannerTiltDegrees`), computed per yaw; the
rotations written match those four vanilla combinations exactly. The banners hang 16
outside the deck edge (was 8), clear of the cross-braces.

**3. The archers never shot.** In Castle Dour **every target a trainee links to is a
persistent reference** (`0B2FE5`, `0B2FE7`; the one temporary target, `0B2FE6`, is linked
to nobody). Ours were temporary, and in a different cell from the archers (targets at x
-650 in cell -1, archers at x 300 in cell 0), so the `TrainingTarget` link had nothing to
resolve. **Fix**: the four `ArcheryTarget` references now go in the worldspace's
persistent cell with the persistent flag (`0x400`), and move in to x -450, so each archer
is **750** from its target (Solitude's trainees shoot from about 300-850). Read back from
the ESP: 4 persistent targets, 0 temporary, each archer linked to its own.

**Verification**: generator run twice, identical SHA256 `ef7aac9985a79042...` (423,273
bytes). **Deployed byte-identical**, with `meshes\SkyrimFair\TowerLantern.nif`.

**Test**:
- The lanterns stand on the deck, glow, and have a small flame (tune `fireScale`,
  `glowScale` if too big or small).
- Banners hang straight down the tower faces.
- The archers draw and shoot. A save made before this build may still have the old
  targets baked in; if they stand idle, try `coc` from a fresh cell or a save from
  before your first visit to the fair.

## Previous pass: festival light towers at the gate and the stage (2026-09-22, late)

Barry: "a new asset... barry_scaffold... festival watchtowers more than guard
watchtowers... two near the main gate and two near the stage... large whiterun banners
hanging from it", then "adding a light to the towers... the flame podium you used on the
stage, or just make a large lantern... where the guards normally stand".

**The tower** (`meshes\barry_scaffold\scaffold.nif`, BS 100, 2,552 triangles, box
collision; `scaffold_d.dds` 2048 DXT1, `scaffold_n.dds` a 4x4 flat normal map). Measured
from the mesh: 124 x 129 x 320, deck floor at 215, deck edge boards topping out at 234,
deck edge at +/-59, roof underside 306 over the centre, **ladder on the local -X face**.
Placed at **scale 2**: 640 tall, deck floor 430, edge boards 468, so it looks over the
palisade (350) and the gate (440).

**The light: a large lantern, not a brazier.** At scale 2 there is 182 of headroom
between the deck floor and the wooden roof, and a brazier's flames would lick the roof.
Instead each deck has vanilla `CandleLanternwithCandle01` (`02D847`, the common iron
lantern with a lit candle, placed 1,393 times in vanilla) at **3.5x, about 150 tall**,
standing in the middle of the deck where the guard would, 35 below the roof. Inside it is
**`WRFireLightNS`** (`0BBAE5`), Whiterun's warm street fire light: radius 768, flickering,
no shadows. That makes 4 more real lights, 14 in the fair.

**The banners**: vanilla `CityBannerWhiterun01InsideTall` (`0DEE54`), Dragonsreach's
long Whiterun banner (340 long, measured from its skin data), at 1.25x, about 425 long.
Each hangs from the top of the deck's edge boards, 8 outside the edge, and turned so the
cloth sways away from the tower. There are none on the ladder face or on faces toward the
wall.

| Tower | At | Yaw | Ladder faces | Banners face |
| --- | --- | --- | --- | --- |
| 01 gate west | (1500, -1790) | 0 | west | east (gate), north (fair) |
| 02 gate east | (2600, -1800) | 180 | east | west (gate), north (fair) |
| 03 stage west | (798, 5250) | 0 | west | east (stage), north, south (crowd) |
| 04 stage east | (3298, 5250) | 180 | east | west (stage), north, south (crowd) |

`fairWorld.towers` in the config, built by `FairTowers.cs`. Each tower's footprint plus
60 is a market keep-out; no market placement changed (33 stalls, 301 pieces, same
refusals).

**Clearances** (read from the written ESP): gate towers 40-65 inside the palisade and
out of the gate-to-stage sightline band; tower 02 about 110 from the gate signpost.
Stage towers about 245 from the stage skirt and 90 between a banner's swing and the
nearest stage post.

**Verification**: generator run twice, identical SHA256 `2e01805cfdde37fe...` (422,558
bytes). **Deployed byte-identical**, with `meshes\barry_scaffold\` and
`textures\barry_scaffold\` copied into the MO2 mod folder.

**Test**:
- Do the lanterns stand on the deck floor (not floating or sunk) and glow at night?
- Do the banners hang clear of the tower (no clipping through the edge boards or legs)
  and sway?
- Does the scale look right next to the gate and the stage? The scale, lantern size and
  banner size are one number each in the config.

## Previous pass: no Dawi stall-keepers, and outhouses by the walls (2026-09-22, late)

Barry: "i believe i have a dawi mod installed which has the dawi race. Could we make those
excempt from being vendors... their height breaks with the vendor table", and Astra's SE
conversion of Stroti's outdoor toilet for the outhouse groups asked for earlier.

**Dawi.** The vendors and archers take their faces from the vanilla commoner lists
`LCharBanditMeleeCommonerM` / `F` (`01A319` / `01A31E`). `Dawi_NPC_Encounters.esp`
overrides the male list and adds a seventh entry of its own (`04FF70:Dawi_NPC_Encounters.esp`),
so some male stall-keepers came out Dawi. The generator now copies each vanilla list
**from Skyrim.esm** into a leveled list of the fair's own, which no other mod edits:
- `SkyrimFairFacesLCharBanditMeleeCommonerM` (`000D49`): 6 entries, `EncBandit01`-`06Melee1HImperialM`, all Skyrim.esm.
- `SkyrimFairFacesLCharBanditMeleeCommonerF` (`000D4E`): 6 entries, the female counterparts, all Skyrim.esm.
- All 8 vendor records and both archer records template these lists (checked by reading
  the ESP). Other mods' face changes to those vanilla lists no longer reach the fair.

**Outhouses.** Two new project statics (`fairWorld.projectStatics`, used by modules as
`@EditorID`): `SkyrimFairOuthouse` (`000BF8`) and `SkyrimFairOuthouseDoor` (`000BF9`),
with the door placed shut in its frame. The module `outhouse_row` is three outhouses
side by side, and two `dressing` groups place it:

| Group | Outhouses at | Doors face |
| --- | --- | --- |
| West wall, north of the archery range | (-683, 2970), (-686, 2844), (-686, 2739) | east, into the field |
| East wall, north-east corner | (5332, 3662), (5299, 3767), (5252, 3874) | west-south-west, into the market |

To make room, the archery keep-out is narrowed to y 800-2,000 (the lanes run y 950-1,850),
and the east group is placed north of the East Wall Walk's end with a 900 search radius.

**Assets**: bundled in the deployed mod, but kept out of git because the resource's
permissions say "Do not upload to other sites" (see `CREDITS.md`).

**Verification**:
- Generator run twice: identical SHA256 `e0624b78d352730b...` (420,733 bytes). **Deployed
  byte-identical**, with `meshes\Stroti\` and `textures\Stroti\` copied into the MO2 mod
  folder.
- Market still 33 stalls, 59 stall-keepers, 4 archery lanes. The outhouses sit well
  north of the lines of fire (y 950-1,850).

**Test**:
- Stall-keepers: no Dawi behind any counter (walk the whole market, since faces are
  picked per spawn).
- The outhouses stand level against both walls, doors shut in their frames, and the
  textures load (no purple).

## Previous pass: props to bring the market to life (2026-09-22, night)

Barry: "audit some more props... the east side and the center looks a bit bare". The
audit of vanilla civilian props (Skyrim.esm, ranked by how often vanilla places them) led
to four groups, all approved.

| Group | Vanilla pieces | Where | Placed |
| --- | --- | --- | --- |
| **Lit braziers** | `WHfirebrazier01` + `FXfireWithEmbersHeavy` + **`LightCampFire01`** (`0AF8BA`, r 512, flickering) | down the East Lane's centre between the picnic islands; along the avenue's west edge | 5 + 2 |
| **Cook fires** | `Campfire01LandOff` + `FXfireWithEmbers01_lite` + `Spit01` roasting spit + `LightCampFire01`, two `TreePineForestCutLog01` log seats, hay scatter, firewood | on the field side of the avenue's eating row | 3 |
| **Whiterun banners** | `CivilWarBanner01` pole with `CityBannerWhiterun01` at +395 (the vanilla pairing) | the avenue's west edge | 3 |
| **Rival colours** | the same pole with `CivilWarBannerImp01` / `CivilWarBannerStorm01` | beside the Imperial and Stormcloak stalls (new `market.themeDressing`) | 2 |
| **Traders' camps** | `NorTentSmall`, `HandCart01`, firewood, hay bale | the market's north-east and south-east corners | 3 |
| **Gate clusters** | `HandCart02`, `HayBale01` + `HayMound01`, crate stacks | either side just inside the gate | 5 |
| **Field fence** | `FenceWoven01` wattle panels | between the avenue and the archery field, x of about 850-1,000, with a gap mid-way to walk through | 14 |
| **Gate sign** | `RoadSignPost` + `RoadSignWhiterun01` (board 230 up on the post, as vanilla mounts them) | inside the gate | 1 |

**10 real lights** (7 braziers, 3 cook fires), kept low for performance.

**How it's placed** (all config, `fairWorld.market`):
- `seating` runs now take several modules at random, a placement chance, and a wall
  margin of their own.
- `dressing` accepts whole modules that **spiral out up to 600 from their point until
  they fit**.
- `themeDressing` adds pieces to every stall of a theme.

Everything uses the stalls' collision rules (lanes, keep-outs, sightline, other pieces).
The archery keep-out now starts at x 700 (behind the archers), which opens the eating
row. **Dropped**: clutter along the stall lines, because the rows are packed too tight
for it (gaps of 20-45); and two camps, which found no room near the wall.

**Verification**:
- Generator run twice: identical SHA256 `cdb73a8a...` (419,242 bytes). **Deployed
  byte-identical.**
- All market and dressing meshes: 0 outside the compound (nearest 107 from the wall
  line); 0 in the crowd square, the stage zone and the gate-to-stage sightline band.
- **0 in the archery lines of fire**. The only pieces in the archery field are the
  range's own hay-bale backstops.
- Previews: `docs/images/market_plan.png`, and `market_views.png` (down the East Lane,
  and across the eating row).

**Test**:
- ~~Do the braziers and cook fires glow at night?~~ **Confirmed by Barry: the braziers glow.**
- Do the rival banners and the Whiterun banners hang right?
- Can you walk through the fence gap to the archery field?
- Frame rate with the 10 lights.

## Archery range on the west field (previous pass; now deployed)

Barry: "use the archery practise from solitude... replace the soldiers with standard
npc's. Have about 3-4 targets". Built as a copy of how **Castle Dour's practice yard**
works, read from Skyrim.esm:
- Solitude's bow trainees run **`GuardSolitudeRangedTrainingPackage`** (`0B4C54`), a
  UseWeapon package with no conditions and no schedule limits.
- Each trainee is linked to one **`ArcheryTarget`** (`066AF6`) by a linked reference
  with keyword **`TrainingTarget`** (`0B4C5A`).

The fair does the same with townsfolk.

| Piece | Value |
| --- | --- |
| Lanes | **4**. Archers on a firing line at x 300, targets at x -650 (950 away), at y 950 / 1,250 / 1,550 / 1,850 |
| Direction | shooting **west**, away from the avenue; the west wall is 500+ beyond the targets. A `HayBale01` backstop stands 140 behind each target |
| Targets | `SkyrimFairArcherTarget01-04`. The face is the mesh's local -X (checked by rendering it), turned to point at its archer (read back: face-to-archer 1.00 on all four) |
| Archers | 2 records, `SkyrimFairArcherMale` / `...Female`: Traits template from the vanilla commoner lists (as for the stall-keepers), `HunterClothesRND`, `HuntingBow`, 100 `IronArrow`, Citizen class, Unaggressive, Protected, and the Solitude package |

Config: `fairWorld.archery` (lanes, pieces, looks). The Activity zone marker (700, 1400)
already faces west.

Generator run twice: identical SHA256 `3535cd98...`. **Deployment pending**: the game
held the file open. Copy `dist/SkyrimFair.esp` once Skyrim is closed.

**Test**: do the four archers draw and shoot at their targets on a loop, with no one
wandering into the line of fire (the stall-keepers are all on the east side)?

**Parked**: outhouses (Toilets resource pack, "Toilet 2"): wooden, corrugated roof,
plinth and steps. They wait for Astra's LE-to-SE conversion; that pack's meshes, and
Stroti's, are all BS 83. Confirm the pack's Nexus permissions before bundling (the
page was not readable from here).

## Aligned rows and eating areas (previous pass; deployed)

From Barry's walk-through:
- "big gaps in the middle of the east stalls and the centre stalls... benches... eating
  areas"
- one stall near the stage "a bit misaligned with the others"

- **Misalignment fixed at the cause.** Back rows were set back to back behind whichever
  front stall they paired with, so their fronts wandered by the sum of two stall depths
  (up to about 200). Every visible row is now **lined up on its own street edge**:
  - Column A's back row is the **west side of the East Lane**, which moved to x 3,780.
  - Column B's back row faces a new **east-wall walkway** (x 5,100).
  - Back-to-back infill is switched off.

  Column A's front row still follows the avenue's gentle approved bend.
- **Eating areas: 9 picnic sets** (new module `picnic`): an `ExteriorWoodenTable01`
  with four `FarmBench01Static`, sometimes a `Lantern` on the table or a barrel at the
  end. They are set along lanes by the new `market.seating`, turned to run with the lane:
  - down the **middle of the East Lane** every 800, as islands with walking room either
    side
  - along the **avenue's open west edge** every 900, facing the food stalls

  Sets that would crowd a passage, a stall or a keep-out are skipped. The archery
  keep-out now starts at x 1,350.
- **Counts**: **33 stalls** (Avenue 9, East Lane 7 + 9, wall walk 8), 200 pieces,
  **59 stall-keepers**.
- Generator run twice: identical SHA256 `24dbe3a2...` (412,046 bytes). **Deployed
  byte-identical** (including the stall-keeper fixes the previous deploy missed).
- Market meshes: 0 outside the compound (nearest 519 from the wall); 0 in the crowd
  square, the stage zone, the archery field and the sightline band.

## Stall-keeper fixes (previous pass; now deployed)

Barry's test: the keepers showed up and "the faces look okay", but paired stalls had one
empty counter, and "they all look ready to wanna punch me".

- **Fists up, found and fixed.** The package I used, `DefaultHoldPositionCurrentLoc64`,
  carries the **WeaponDrawn** flag, so every keeper stood in the unarmed combat stance.
  It is now **`DefaultStayAtEditorLocation`** (`025BFC`), which 20 vanilla NPCs use,
  with no weapon flag. They stay at their spot and idle normally. (Lesson: check a
  vanilla package's flags, not just its name. The hold-position family all draw
  weapons.)
- **One keeper per counter.** Modules now carry `vendorSpots`, a list: `canvas_pair`
  and `canvas_wide` get one per structure, and the `grand_double` two across its double
  stand. **58 keepers** across the 34 stalls, where there were 34.
- Generator run twice: identical SHA256 `f661bcc4...` (408,300 bytes). Read back: all 8
  vendor records use the new package, and 58 are placed.
- **Deployment pending.** The copy to the MO2 mod folder was refused because the file
  was in use (the game or MO2 still running from Barry's test). The deployed plugin is
  still the previous build; copy `dist/SkyrimFair.esp` once Skyrim is closed.

## Placeholder stall-keepers (previous pass; package and counts superseded above)

Barry: "add NPCs to the stalls, but have them sell nothing for the time being... just to
help me envision it". **34 stall-keepers, one behind each stall's counter, facing the
street.** They are purely visual. They have no merchant setup, dialogue, faction, quest,
script or inventory, and nothing to steal.

| Record | Value |
| --- | --- |
| Vendor NPCs | **8 records** `SkyrimFairVendorMale01-04`, `SkyrimFairVendorFemale01-04`, all named "Fair Trader" |
| Faces | Traits template only, from vanilla `LCharBanditMeleeCommonerM` (`01A319`) / `...F` (`01A31E`), the "commoner" leveled lists (Imperial, Nord, Breton faces with vanilla FaceGen). Each reference rolls its own face, so there are no generated faces and no dark-face bug. Nothing else comes from the template: no bandit gear, factions or AI |
| Clothes | one record per vanilla outfit: `MerchantClothesOutfit01NoHat`, `FarmClothesRandom`, `FineClothesOutfit01`, `BarkeepClothes01` |
| Class, AI | `Citizen`; Unaggressive, Cowardly, helps nobody; **Protected** (only the player can kill them) |
| Package | `DefaultHoldPositionCurrentLoc64` (`0A6854`): stays within 64 of its spot, so **no navmesh is needed** |
| Placement | 34 ACHR, one per stall, at each stall type's `vendorSpot` (behind the counter), facing the stall's front. They are 200-240 behind each stall's front marker |

Config: `fairWorld.vendors` (looks, outfits, class, package) and each market module's
`vendorSpot`. Set `vendors.enabled` false to remove them all.

**Verification**:
- Generator run twice: identical SHA256 `0aa38d0d...` (406,764 bytes). Deployed
  byte-identical.
- Read back: 8 NPC records exactly as tabled, 34 placed, 0 filed in the wrong cell, 0
  merchant containers.
- **Not verified in game**:
  - how the faces and outfits look
  - whether any keeper spawns clipping a counter or stall post
  - that hold-position keeps them put without a navmesh

**What Barry should test**:
1. Walk the avenue and the East Lane: is there a keeper at every counter, facing you?
2. Any odd faces, or anyone stuck inside a stall or wandering off?
3. Talking to one should give only generic greetings, with no buy or sell option.

## East market, west games field (previous pass)

Still current; the vendor pass above adds NPCs only.

Barry: "could we have just an 'east market' and have the west for the archery and
anything else?" The market now fills the **east half** as two market streets built
from stall islands. The **west half is open for archery and games**.

```
            west wall                                   east wall
   ┌───────────────────────── crowd square ─── stage ────────────────┐
   │                          │ A-front ▌A-back │ East │B-front ▌B-back│
   │  ARCHERY / GAMES FIELD   │ (faces  ▌(faces │ Lane │(faces  ▌(faces│
   │  (open; shoots west)     │ avenue) ▌ lane) │      │ lane)  ▌ wall)│
   │        avenue (west side open) ──▶ stage │      │        ▌      │
   └──────────── gate ───────────────────────────────────────────────┘
```

| Column | Front row | Back row (back to back) | Total |
| --- | --- | --- | --- |
| **A**, along the avenue's east side | 9, facing the avenue | 6, facing the East Lane | 15 + the Imperial stall = **16** |
| **B**, along the East Lane's far side | 9, facing the lane | 9, facing the east-wall walkway | **18** |
| | | | **34 stalls, 141 pieces** |

- **Zones swapped**: the `Market` zone (worn-earth ground) now takes the east polygon,
  marker (3650, 1400). The `Activity` zone (grass-free ground) takes the west polygon,
  marker (700, 1400) **facing west**, so a future archery line shoots at the west wall,
  away from the avenue. Their ground paint follows.
- **Lanes**:
  - the avenue, lined on its east side only
  - the **East Lane**, x 3,700, y -800..3,700, 600 wide, lined on its far side
  - two stall-free openings: an **entrance passage** leaving the forecourt diagonally
    to the lane's south end, and a **mid crossing** at y 1,000
  - the crowd square joins both streets at the north
- **Keep-outs**: the archery range is now the west field (x < 1,500). The crowd square,
  the stage and the gate-to-stage sightline band are unchanged.
- **Stalls only** (Whiterun double and gabled stands, Windhelm canvas pairs, the wide
  mixed stall). Back rows now take the stall type closest in frontage to the one in
  front, so the islands line up.
- **Signature slots**: `SkyrimFairStallImperial01` and `...Stormcloak01` face each
  other across the East Lane, and `...Sweetroll01` is at the lane's north end by the
  crowd square. Every stall has a themed shell marker: food along the avenue,
  specialists and faction trades on the East Lane, trades on the back rows.
- **Count**: 34, against 40 when both avenue sides were lined, because both columns now
  share the east half at full stall size.

Previews: `docs/images/market_plan.png`, and `docs/images/market_views.png` (from the
gate; down the East Lane).

### Verification

- Generator run twice: identical SHA256 `b921f86d...` (402,644 bytes). Deployed
  byte-identical.
- Market meshes: 0 outside the compound (nearest 419 from the wall line); 0 in the
  crowd square, the stage zone, the west activity field or archery range, and the
  gate-to-stage sightline band.

### What Barry should test

1. From the gate: the market down the right, the open field on the left, the stage ahead.
2. The entrance passage and the mid crossing into the East Lane: are they easy to find?
3. Walk the East Lane: does it feel like a second market street?
4. Is the west field the right size and shape for archery and games?

### Stall kit (all vanilla Skyrim.esm, referenced, nothing copied)

Seven modules, each a main structure plus display, storage and dressing. Optional
pieces are dropped from about a third of copies, and every copy is randomly mirrored and
jittered (±5 in position, ±3 degrees), so no two stalls match:

| Module | Main structure | Frontage x depth | Dressing |
| --- | --- | --- | --- |
| `grand_double` | `WRMarketStand01` (`05B2A7`), Whiterun's canvas double stand | 560 x 400 | two `WRMarketDisplayShelf01/03`, barrels, stacked crates, banner post |
| `canvas_pair` | `WHMarketStall01` + `02` (`0F3C72/73`), merged canvas stalls | 440 x 300 | long crate, barrel, mead barrel |
| `whiterun_stand` | `WRMarketStand02` (`05B2E2`), gabled timber stall | 360 x 340 | `WRMarketDisplayShelf02` crate display, barrel, mead barrel, crate |
| `canvas_wide` | `WHMarketStall01` + `WRMarketStand02`, a merged mixed stall | 520 x 320 | display shelf, crates, barrel |
| `cask_bar` | `BreweryCaskLargeClosed01` (`07F871`) + `WHMarketStall02` | 460 x 360 | mead barrels, `FarmBench01Static` for drinkers |
| `open_trader` | `ExteriorWoodenTable01` (`03DE44`) + display shelf | 320 x 250 | crate, `HayBale01`, barrel, firewood |
| `trestle` | `ExteriorWoodenTable01` | 260 x 200 | crate, barrel; the filler for tight spots only |

Dressing: `Barrel02Static` `10C0E3`, `MeadBarrel01` `01E3A3`, `CrateSmall01-04`,
`CrateSmallLong01` `07A605`, `FirewoodPileMedium01` `0185B5`, and `FarmBannerPost01`
`1083D7`.

**Rejected**:
- `WRMarketStand03`, built for Whiterun's sloping market with 176 of legs below its
  floor
- Riften and Markarth stalls, off-style for a Whiterun fair
- Imperial tents, which read military
- Solitude's small counters, tiny one-table traders
- **Medieval Markets** (below), for now

### Layout machinery (the current config uses the avenue only)

- **Lanes are data.** Each has a centreline, a half-width profile that pinches and
  swells along it, and a gentle meander. The avenue uses the approved avenue line.
- **Stalls face their lane.** They stand behind its widest point across their whole
  frontage, with set-back up to 45 and turns up to ±7 degrees, and gaps of 10-70 with
  an occasional 280-420 browsing pocket.
- **A stall is placed only if it keeps clear of everything:**
  - every lane's corridor (modelled as square-cut slabs, so a pocket does not spill
    over the pinch next to it)
  - the wall (220), the crowd square, the stage, the entrance forecourt, the archery
    range (x > 3,250)
  - a 260-wide **gate-to-stage sightline band**
  - every other stall

  Refusals are what open the junction mouths and pockets. A tight spot tries smaller
  modules before giving up.
- **Priority order**: signature stalls, then the avenue, the branches, the back lanes,
  the outer lanes, and finally back-to-back infill behind the lane rows.
- **Deterministic**: fixed integer hashes throughout.
- Tuning is all in `fair.config.json`. `SKYRIMFAIR_TRACE=1` prints every refusal and
  its reason.

### Medieval Markets (external zip) audit

`external/Medieval Markets ESL-161479-1-1-1-1761815114.7z`: 37 meshes, an ESP, and
textures.

- **The ESP rearranges the vanilla city markets** (Riften, Whiterun and others), so the
  mod as a dependency would change players' cities. It is not a clean resource.
- **JJerem's stall pieces use vanilla textures only**:
  - `NewMarketStall01`: a four-post cloth-roofed stall with a front swag and side
    curtain, using vanilla `wrmarketstallroof01.dds`
  - `MarketStandLong01/02`: A-frame produce shelving
  - `SlantedShelf01`, `ShortFarmTable`, `fenceWovenTall01`
  - six weapon racks and an arrow pot
- **Produce crates and baskets** (17 + 7) use PraedythXVI's *Fruits and Veggies*
  textures, and some baskets Palpable Baskets (gooball60). They need those authors'
  permissions.
- `0TentNordLarge01` is an **unconverted Oldrim (LE, BS 83) mesh** with COTN
  (JPSteel2) textures. It is not usable as is.
- **Not used yet.** JJerem credits SMIM (Brumbek) as a source, and the files do not say
  which pieces derive from it. Before bundling, confirm with the mod page or JJerem
  which of the stall pieces above are wholly JJerem's; then they can be bundled with
  credit (no paid use). The cloth stall and the weapon racks would be the most useful:
  cloth variety, and the smithing rows.

## Condensed compound, stage fire pits, closed gate, open tundra (previous pass)

Still current. The market pass above adds records only.

Barry reviewed the stage and the area in game ("looks really good... the stage looks
great"), then asked for four changes. This pass makes those four changes and nothing else.

| Change | What was done |
| --- | --- |
| **Condense by about 30%** | Read as **30% less area**: the whole plan (perimeter, gate, avenue, every zone polygon and marker) was scaled by **0.837** about the centre of cell 0,0 (2048, 2048). The gate-to-stage axis stays on x 2048, and `cow SkyrimFairWorld 0 0` still lands mid-compound. The numbers were rewritten in `fair.config.json` (`scratchpad` tool `condense.py`), so the config stays the source of truth. If "30%" meant each dimension, rerun it with 0.7/0.837 on the new numbers. |
| **Fire pits on the stage** | **4 `WHfirebrazier01`** (`093A89`, the Windhelm fire basket on a stand) **with `FXfireWithEmbersHeavy`** (`033DA4`) above each. It is the same pair, at the same fire offset (-6, -2, +92), as Barry's braziers at the Tamriel stair foot. They stand on the deck at its front corners (u ±760, v 390) and rear corners (u ±760, v -380), with feet on the planks. They are in `fairWorld.stage.braziers`. |
| **Closed gate** | `SkyrimFairPalisadeGate` (`000B10`) now uses **`barry_palisades\viking_palisade_gate_closed.nif`**. It has the same 181 x 175 footprint and the same `viking_palisade_gate` textures. Deployed alongside the open version (SHA256 `329a9253...`). No texture changes. |
| **Fewer trees (Whiterun tundra, not forest)** | Forest `peakDensity` 0.8 -> **0.28**, `clearingThreshold` 0.34 -> **0.46** (bigger clearings): **99 trees** (was 518), in scattered stands. The mountains are unchanged. |

### Knock-on changes

- **Compound**: now **7,157 x 8,370** (X -1,384..5,773, Y -2,011..6,359). It has **89**
  palisade panels (was 106). The gate is at (2048, -1991).
- **Terrain**: the flat area and painted zones follow the smaller outline, so the LAND
  records changed. The terrain strategy is unchanged.
- **The stage keeps its approved size.** It needed `forwardOffset` 160 -> **270** so its
  rear timbers clear the nearer north wall (nearest above-ground stage geometry is now
  114 from the wall line). The deck centre is now (2048, 5544), with the step foot at
  (2048, 4538).
- **The audience area in front of the stage is now about 560 deep** (the step foot back
  to the crowd square's south edge, y 3973), where it was about 1,000. That is the cost
  of condensing round a stage that did not shrink. The Crowd marker moved from on the
  steps to (2048, 4290), in the audience space.

### Verification

- Build clean. Generator run twice: identical SHA256 **`092515db...`** (390,425 bytes).
- **Wall closure** re-proved on the new outline:
  - 3,235 points every 8 units all lie inside a wall or gate footprint.
  - 20,160 sightline rays from seven interior points (moved to the condensed layout)
    all cross a wall or gate.
  - The panels either side of the gate tuck 25 and 36 units into it.
- **Stage**: 0 mesh vertices outside the compound; nearest to the wall line 114.
- ESP and `viking_palisade_gate_closed.nif` deployed byte-identical.
- **Not verified in game**: the closed gate model, the braziers on the deck, and the
  tighter crowd square.

### What Barry should test

1. The closed gate, from inside and outside.
2. The braziers on the stage: lit, feet on the planks, not in the performers' way.
3. Is the condensed compound the right size, and is ~560 in front of the stage enough
   audience room?
4. Does the thinner tree cover read as Whiterun tundra?

## Main stage architecture (previous pass; approved by Barry)

Still current, except the stage now stands at (2048, 5544) after the condense, with `forwardOffset` 270, and carries four braziers.

The brief: an open, handmade, temporary Nordic timber pavilion at the north end of the
avenue, facing the gate, built from vanilla pieces. Barry's concept image was used as
loose art direction ("take the concept with a pinch of salt"). **Nothing else moved**:
read back against the mountain build (`b77160ca...`), 835 of 836 existing non-backdrop
records are identical. The one change is intended: `SkyrimFairWorldStageMarker` now
stands on the deck, at Z 134. The 133 stage references are new, and the trees and
mountains are unchanged in content, only renumbered. No NPCs, music, animation,
navmesh, stalls or dressing.

Preview renders from the written plugin (every placed mesh, rotated as the engine
does): `docs/images/stage_preview_views.png` (from the gate, and from the crowd square,
both at eye height 120), `stage_preview_side.png` and `stage_preview_plan.png`. The red
boxes are 128-tall figures for scale.

### Placement, derived from the config

- **Anchor**: the configured `Stage` zone's marker, (2048, 6548), heading **180**
  (south). The stage faces the marker's heading. The deck centre is the marker moved
  `forwardOffset` 160 toward the audience, to **(2048, 6388)**, which keeps the rear
  timbers clear of the wall.
- **On the sightline**: the main gate at (2048, -2777) already faces the Stage marker,
  so the deck, steps and frame are centred on **x 2048**, the gate-to-stage axis. No
  post, beam or clutter stands on that axis in front of the deck; the steps are on it by
  design.
- `fairWorld.stage` holds everything in stage-local coordinates (`u` across, `v`
  toward the audience), so the stage follows its zone if the zone ever moves. The
  marker must face along a world axis, and the generator refuses otherwise (see the
  rotation note below).

### Dimensions

| Part | Value |
| --- | --- |
| Deck | **1,750 wide x 912 deep**, top **134** above the crowd square (about head height), 7 x 4 porch sections |
| Steps | one central flight, **744 wide**, 5 treads, **22.3 risers**, 110 treads, **550 run** (about 14 degrees), foot at (2048, 5382) |
| Frame | posts **700** tall; header beams at 640 and 575, rafters at 690; overall about 1,900 x 1,560 on plan, **713** high |
| Audience space | the crowd square in front, from the step foot (y 5382) back to about 4,350: roughly 1,000 deep, **empty** |

### Vanilla assets used (all Skyrim.esm, referenced, nothing copied)

| Role | STAT | FormID | Count | Scale | Collision |
| --- | --- | --- | --- | --- | --- |
| Deck | `Walkway01` (farmhouse porch: plank deck on corner posts with knee braces) | `01C570` | 28 | 1.0 | compressed mesh, unscaled |
| Steps | `StockadeScaffoldTop0Sided01` (camp scaffold plank top) | `0533C0` | 15 | 1.0 | **box** |
| Risers | `StockadeWoodplanks01` | `0533D5` | 15 | 1.0 | compressed mesh |
| Deck skirt | `StockadeWoodplanks04` (250 x 143 plank panel) | `0533D8` | 22 | 1.0 | compressed mesh |
| Posts, beams, rafters, braces | `StockadeWoodbeam01` (bark log, 306 long) | `0533D0` | 6 + 20 + 21 + 6 | 1.55-2.34 | convex hull |

**133 references in total.**

- **Deck**: tiled at **250 x 228**, the size of the plank surface measured from the mesh.
  The object bounds say 272 x 256, which includes post ends; tiled at that size, the
  preview showed a slit at every joint. The porch's own posts carry it down to the
  ground, with a plank skirt on the front, sides and back, left open behind the steps.
- **Steps**: each tread rests on the one below. A board under each front edge closes
  the riser. There are no railings.
- **Frame**:
  - chunky round-log posts at the front corners, with four more along the rear
  - a doubled header at the front and a top beam and rail at the rear
  - side beams, and seven log rafters as a slatted partial canopy
  - the rear bays X-braced as a light timber backing
  - the sides and front left open
- **Handmade**: every log wanders up to 5 in height, 1 degree in yaw and 3% in size.
  Posts lean up to 0.8 degrees and sink up to 12, all from fixed integer hashes.

### Rejected, and why

| Candidate | Why not |
| --- | --- |
| `WalkwayStairs1/3/4/8/15` (farmhouse porch stairs) | steep narrow flights with newel posts, and `WalkwayREnd01` has a rope rail; the brief asks for broad and welcoming, no rails |
| `StockadeScaffoldStairs01` | 45 degrees with open ladder treads: defensive, not welcoming |
| `ShipStairs01` | small, awkward profile |
| `StockadeScaffoldBase*` as the deck | 192 tall, wants sinking, and reads as military scaffolding; the farmhouse porch reads rustic and civilian |
| Stepping with `Walkway01` pieces | their corner posts would stand in the treads, in the walking line |
| `ShackRoofMid01` / `Side01` as a canopy | **single-sided** planes: invisible from below, where the audience stands |
| `OrcAwning*` | bone and hide: reads orcish |
| `LargeNordicTent01`, `LargeImperialTent01` | a closed hide mound, and an Imperial military tent with the dragon emblem |
| `SMarketStallTop`, `MrkMarketStallRoof01` | a wooden slat pergola; a roof built onto its own small stall frame |
| Market-stall canvas (`whmarketstallroof01.dds`, the concept's cream-and-grey stripe) | **exists only baked into whole Windhelm and Riften stall meshes**, with their counters and frames |
| `StockadeFreewallBeam01` as posts | compressed-mesh collision, which the project knows scales unreliably; the log's convex hull scales |

**Cloth canopy: open question for Barry.** Vanilla has no standalone cloth awning, so
this pass uses the brief's "timber and/or cloth" allowance: log rafters give the
silhouette and leave the stage open. A true cloth awning like the concept's needs a
small project-owned mesh, either a draped sheet or a pair of swags, textured with
vanilla `whmarketstallroof01.dds` by path, not copied. The brief said not to author one
unless vanilla genuinely can't, and the audit above shows that vanilla can't. It is
left for Barry to approve.

### Rotation note (durable)

Skyrim applies a reference's rotations about the world axes, **Z first, then Y, then
X**. The approved pitched stair cheeks depend on it: they are yawed 90 degrees, then
pitched about world X. So every stage log is laid with a yaw, then at most one tilt
about the world axis across it, which is why the stage must face along a world axis.
Braces are crossing pairs about a shared centre, so they come out right whichever sign
a tilt takes. The preview renderer uses the same convention.

### Future attachment points (nothing placed yet)

These are all in `fairWorld.stage`, in stage-local coordinates:

| For | Where |
| --- | --- |
| Front banners and bunting | the header beams at 640 and 575, spanning u ±955 |
| Rear backdrop banners | the rear rail at 380 and the rear top beam at 640 (v -490) |
| Braziers at the front corners | on the deck, inside the posts at u ±905, v 490 |
| Performer markers | on the deck (top 134, 1,750 x 912); the Stage marker already stands on it |

### Collision observations (not yet tested in game)

- **Deck**: `Walkway01` at scale 1, its own compressed mesh, so it should walk like any
  farmhouse porch.
- **Steps**: box colliders at scale 1, with 22-unit risers, well within the player's
  step height.
- **Logs**: convex hulls at scale 1.55-2.34. They should scale with the mesh, but walk
  into a post to confirm.
- **Skirt boards**: compressed mesh, unscaled.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `158d7203...`.
- Read back against `b77160ca...`: 835 of 836 non-backdrop records identical, 1
  intended change (the Stage marker's Z), 133 new stage references. Trees (518) and
  mountains (24, all `0x10400`) are unchanged in content.
- Geometry checked against the written plugin's meshes:
  - 0 stage vertices outside the compound, and the nearest above-ground stage geometry
    is 100 from the wall line
  - 0 stage geometry in the audience area south of the step foot
  - nothing but the steps on the gate-to-stage axis in front of the deck
- Preview renders made from the plugin: the deck is continuous (after the 250 x 228
  fix), the risers are closed, and the frame reads from the gate.
- ESP deployed byte-identical. No new assets this pass.

### What Barry should test in game (this pass)

1. **From the gate** (`player.moveto SkyrimFairWorldEntranceMarker`, face north): do you
   immediately read the timber pavilion at the end of the avenue as the main stage?
2. **Walk up the avenue and the steps**: are the steps broad and easy, is the deck solid,
   and do any tiles show a seam?
3. **Walk round it**: how does it read from the sides and behind, and does anything float
   or poke through?
4. **Collision**: walk into a post and a skirt board.
5. **Scale**: the deck is at head height and the frame about 5.5 people tall. Does it
   hold the crowd square, or want to be bigger or smaller?
6. **The canopy**: are timber rafters enough for now, or do you want the small cloth
   awning mesh (see above)?

## Mountain backdrop and the view through the gate (previous pass; awaiting review)

Still current. The trees and mountains are renumbered by the stage pass, with their content unchanged.

Barry reviewed the palisade in game: "the gate looks incredible". Two things came back.
The gate model **is open** (the asset, not the placement). And through it, and over
the wall, the outside looked bare. So this pass adds vanilla mountains to the backdrop
and closes the forest behind a short clearing in front of the gate. **Nothing else
changed**: read back against the reviewed build (`da78301a...`), all **836** records
that are not trees or mountains are identical. That covers the wall, gate, terrain,
zones, markers, Tamriel and the sandbox.

### How the mountains draw in a world with no LOD

The world has no LOD, so an ordinary reference draws only while its cell is loaded. A
mountain 15,000 away would come and go as the player walked about. Vanilla solves this
in its small worlds, and it was audited before anything was placed. Skuldafn, Sovngarde,
Japhet's Folly and Tamriel itself keep their distant cloud meshes as references in the
worldspace's **persistent cell**, flagged **Persistent + Is Full LOD (`0x10400`)**, and
always inside the world's object bounds (Skuldafn's sit about 200,000 out, within its
bounds of cells 0..65). The mountains are placed exactly that way. Full LOD alone,
without Persistent, is not what vanilla does for distant scenery, so it was not tried.

### The mountains

**24 vanilla snow-covered mountain STATs** (the `_HeavySN` snow-shader variants, to match
the concept's snowy peaks), in two irregular rings round the compound centre (2223, 2198):

| Row | Radius | Count | Pieces |
| --- | --- | --- | --- |
| near | 13,000-15,500 | 11 | ridges and small cliffs: tops 1,970-4,840, 8-18 degrees above a standing player's eye at the centre |
| far | 17,500-20,500 | 13 | peaks and big cliffs: tops 5,420-8,220, 15-24 degrees |

| STAT | FormID | Placed | Scale |
| --- | --- | --- | --- |
| `MountainPeak01_HeavySN` | `043321` | 7 | 0.95-1.13 |
| `MountainCliffSm01_HeavySN` | `050DC0` | 6 | 0.90-1.14 |
| `MountainCliff04_HeavySN` | `027DDC` | 3 | 0.84-1.16 |
| `MountainRidge01_HeavySN` | `05205B` | 3 | 0.94-1.07 |
| `MountainCliff01_HeavySN` | `048DE0` | 2 | 1.18-1.19 |
| `MountainPeak02_HeavySN` | `046031` | 2 | 1.00-1.07 |
| `MountainRidge02_Heavy_SN` | `05304E` | 1 | 1.08 |

- Each row is evenly spaced in angle, with each piece wandering up to 30-35% of the
  spacing. The two rows are offset (start bearings 9 and 23) so near and far pieces do
  not line up, and every piece takes a hashed radius within its band, yaw and scale.
- **Sunk so no base ever shows.** Every mesh's lowest point is placed at Z -1,000, read
  from its own bounds in Skyrim.esm. At mountain distance the wall hides everything
  below about 500 from anywhere inside the compound. When the hill cells beyond are not
  loaded, a mountain therefore still reads as rising from behind the trees, not floating.
- Everything sits inside the world's object bounds (cells -5..6). The generator
  refuses a mountain outside them. The WRLD record is unchanged.
- Due south, straight out through the open gate, is a far `MountainCliff01` at bearing
  186 with near ridges at 164 and 211 either side.

Plan, 2,048 units per character, north up (`o` compound, `n` near row, `M` far row):

```text
    ......................
    ..............M.......
    .........M............
    .......M..............
    .......n...n..........
    ....M..........n......
    ..................M...
    ......................
    .....n................
    .........oooo....n....
    .........oooo.......M.
    ..M.n....oooo.........
    .........oooo.........
    .................n....
    ....n.............M...
    ...M..................
    .......n.......n......
    ...M........n...M.....
    ......................
    ...............M......
    .........M............
    ......................
```

### The view through the gate

The palisade pass kept every tree 1,800 from the gate and out of a 28-degree cone
straight out of it. That was for an approach from outside, which this isolated world
will never have, and it is why the open gate showed bare ground. Now the rules are:

- no tree within 700 of the gate;
- an 18-degree clearing straight out, only for the first 2,200, which reads as a path
  leading away;
- beyond that the forest closes in.

Trees: **518** (was 487). The nearest to the gate is 936 away. 17 now stand in the view
straight out of the gate, the nearest 2,300 out, with the far mountain behind them. The
per-species spread is otherwise as before: `TreePineForest01` 77, `02` 142, `03` 12,
`04` 131, `05` 156.

The gate's own collision is unchanged. Barry's package gives it a closed-gate box, so the
opening should be solid even though it looks open. **Walk into it to confirm.** The
teleport pass will decide what the open gateway shows.

### Records changed

| Type | Change |
| --- | --- |
| REFR, persistent cell | +24 mountains (`0x10400`) |
| REFR, cells | trees 487 -> 518, and all renumbered (they are placed last; nothing references them) |
| Everything else | identical |

### Verification of this pass

- Release build clean; generator run twice, identical SHA256 `b77160ca...`.
- Read back against `da78301a...`: 836 of 836 non-backdrop records identical. The 24
  mountains are all `0x10400`, all vanilla STATs, all in the persistent cell, 0 outside
  the world's bounds, and every mesh bottom is at -1,000. The persistent cell still
  holds the five zone markers.
- ESP deployed byte-identical. No new assets this pass.
- **Not verified in game**: how the mountains look, their draw distance and fog, and
  frame rate.

### What Barry should test in game (this pass)

1. From the centre, turn round: snowy mountains above the treeline all the way round,
   with the far peaks behind the near ridges, and none floating.
2. At the gate, look out through it: a short path, then trees, then mountains.
3. Walk from one end of the compound to the other. The mountains should never pop in
   or out. If they do, the Full LOD approach is not working in this world. Say so.
4. Are they too big, too small or too close? Each row's radius and scales are config
   values.
5. Frame rate.

## Palisade compound, main gate and forest backdrop (previous pass; gate approved in game)

Still current, except the forest's gate clearing, which the mountain pass above shortened (518 trees now, renumbered).

Barry approved the isolated `SkyrimFairWorld` prototype in game. This pass replaces the
temporary banner-pole markers with his custom palisade, adds the Viking gate at the main
entrance, and rings the compound with vanilla conifers. **The approved layout is
unchanged**: the perimeter polygon, gate point, avenue, zones, terrain and ground
painting are the same config values as before. Read back from the written plugin, all
121 LAND records, every cell header, the WRLD header, the persistent cell and the five
zone markers are identical to the approved build. The Tamriel terrace, staircase,
embankment and sandbox are untouched: all 543 records outside the fair world's cells are
identical.

Not built, by instruction: the teleport / "Enter The Wanderer's Fair" interaction, the
Tamriel gate, stalls, NPCs, navmesh, stage systems, music, archery, clutter.

### Custom assets, bundled

Barry's Skyrim-ready conversions, both **CC BY 4.0** (attribution recorded in
`CREDITS.md`). The deployable files live in the repo at `assets/meshes/barry_palisades/`
and `assets/textures/barry_palisades/`, byte-identical to the `Data/` folder of Barry's
package `assets/Skyrim_Palisade_Assets/`. They are deployed to the MO2 mod at the same
relative paths:

| File | SHA256 | Deployed |
| --- | --- | --- |
| `meshes\barry_palisades\palisade.nif` (24,655 bytes) | `d1d92fc9...43b2` | identical |
| `meshes\barry_palisades\viking_palisade_gate.nif` (468,624 bytes) | `7f5bfd7f...8349` | identical |
| `textures\barry_palisades\palisade\material_00_{d,n}.dds` | | identical |
| `textures\barry_palisades\viking_palisade_gate\material_00..23_{d,n}.dds` (48 files) | | identical |

The NIFs reference their textures as `textures\barry_palisades\...`, which resolve to
those files (checked in the NIF strings). Measured, per the package's validation and
build scripts: the palisade is **138.97 wide (local X) x 11.29 deep x 140 tall**, and the
gate is **181.44 x 43.52 x 175.45**. Both have origins at bottom centre, fixed zero-mass
box collision on the Static layer, and no LOD meshes.

### New STAT records

| EditorID | FormID | MODL | OBND |
| --- | --- | --- | --- |
| `SkyrimFairPalisade` | `000B0F` | `barry_palisades\palisade.nif` | -70,-6,0 .. 70,6,140 |
| `SkyrimFairPalisadeGate` | `000B10` | `barry_palisades\viking_palisade_gate.nif` | -91,-22,0 .. 91,22,176 |

Bounds come from the measured sizes (the terrace kit STATs carry none).

### Palisade: placement strategy

- **106 panels** at scale **2.5**, so each is 347 wide and **350 tall** (about 5 m). The
  crest wanders between **316 and 364** because each panel's scale varies by up to 4% and
  it sinks by up to 20. That is well above a standing player's eye (about 120), so the
  empty ground beyond is never visible over the wall.
- Laid **edge by edge along the approved 17-point outline**, each panel turned to its
  edge. Each edge's run carries **48 past both vertices**, so neighbouring edges cross at
  every bend. Each run is filled with the fewest panels that still **overlap by 6%** (21
  units), spaced evenly so there is never a short filler piece.
- **Handmade, not plotted**: from a fixed integer hash (never a string hash), each panel
  wanders up to 1.2 degrees in yaw and 5 units off the line, varies up to ±4% in scale
  and sinks 0-20. About half are turned round so the same face does not repeat along the
  wall.
- **Blended into the gate**: the south edge's run is split at the gate, and the panel
  either side **tucks 31 and 36 units into the gate** (the config asks for 32).
- The wall stands on the painted perimeter strip, so the stony strip reads as its
  trodden footing.

### Main gate

- `SkyrimFairWorldMainGate` (`000B11`), a static `SkyrimFairPalisadeGate`, closed, with no
  script or animation. It stands at the approved gate point **(2048, -2777)**, where the
  avenue starts, at scale **2.5**: 454 wide and **439 tall**, so it rises about 90 above
  the wall.
- **It faces the Stage marker** (heading 0, due north). The view out of the forecourt
  therefore runs straight up the avenue to the stage at (2048, 6548). The avenue's own
  gentle bend stays as approved.
- The model's front and back were not identifiable from the data. If the gate turns out
  to be facing the wrong way, `fairWorld.gatePiece.yawOffsetDegrees: 180` turns it round.
- The teleport and activation come in a later pass.

### Forest backdrop: placement strategy

**487 vanilla trees**, all Skyrim.esm TREE records, scenery only:

| Tree | FormID | Count | Scale | Distance beyond the wall |
| --- | --- | --- | --- | --- |
| `TreePineForest01` (2,498 tall at 1.0) | `01306D` | 71 | 0.80-1.20 | 574-4,608 |
| `TreePineForest02` (2,501) | `018A02` | 132 | 0.80-1.25 | 482-5,188 |
| `TreePineForest04` (2,085 above origin) | `04FBB0` | 121 | 0.80-1.25 | 556-4,826 |
| `TreePineForest05` (1,465) | `051126` | 151 | 0.86-1.35 | 424-5,191 |
| `TreePineForest03` (860, understory, within 1,800 only) | `04B016` | 12 | 0.94-1.48 | 382-1,762 |

These are the Falkreath and Rift pine-forest conifers, and in Tamriel they are placed at
0.35-1.67. Snow and dead variants are excluded.

- **Candidates** come from a 440-unit jittered grid over the band from 320 to 5,200
  beyond the wall. Each is kept by chance against a density that starts **sparse by the
  wall**, peaks from 700 to 2,600 out and thins toward 5,200, so the view has layers of
  trunks and canopy.
- **Clumps and clearings**: that density is multiplied by a low-frequency clustering
  field with a 1,700-unit period, and where the field is low there are no trees. The
  result is irregular stands with gaps of sky between them, not a ring. Because the
  ground rises beyond the flat margin, trees further out stand higher, which stacks the
  canopy.
- **Variation per tree**: species by weight, scale within its range, any yaw, and a lean
  of up to 1.5 degrees. Trunks are sunk 24.
- **Gate kept open**: no tree within 1,800 of the gate, and none in a 28-degree cone
  straight out of it, so a future approach from outside stays clear. From inside, the
  gate sightline runs north to the stage and past it into the northern stands.
- Canopies do not overhang the wall. Each species has a minimum distance (320 to 520)
  set to about its canopy radius.

Plan read back from the written plugin, 512 units per character, north up (`#` wall,
`G` gate, `^` a cell holding trees):

```text
                   ^
                 ^^      ^^
                ^  ^ ^ ^  ^ ^^^ ^
                  ^^  ^^^^ ^   ^^
          ^        ^^^^^^ ^^^  ^^^^
           ^       ^^^^^ ^^^^   ^ ^
             ^ ^^  ^   ^^ ^^^^ ^^^ ^
             ^^^^^^^        ^^^ ^ ^^^
            ^^ ^^^ ^         ^^^^ ^  ^^
         ^^^^^^^            ^^^  ^^^^ ^
        ^ ^   ^     #########^ ^ ^^  ^
         ^ ^^^^^  ###       ## ^^^^^^
        ^ ^^^    ##          ## ^^^^^^
           ^^^^^##            ##^^^ ^^ ^ ^
       ^        #              # ^^^ ^
        ^ ^   ^##              ##^^  ^ ^
      ^^^ ^ ^^^ #               #^ ^^^ ^^^
        ^^     ^#              ##^^ ^ ^   ^
        ^   ^ ^ #              # ^   ^ ^^^
       ^    ^ ^^#              # ^^^^^   ^
        ^    ^^ #              #^^ ^ ^^ ^
       ^ ^ ^^^^##              # ^^^   ^^
       ^  ^^^ ^#               # ^^^^^^
       ^^^^^ ^ #               ##^  ^^
             ^  #              # ^^^^^    ^
        ^^^^ ^^^#              #^ ^^^^
      ^  ^^^^ ^^##             #^^^^  ^
         ^^^^^ ^ ##          ### ^^^^
           ^^ ^   ###      ###  ^^^^^
        ^^  ^^^^^ ^ ###G#### ^^^ ^^^ ^^
         ^^^^   ^^ ^^       ^ ^ ^ ^
         ^ ^^  ^ ^^^^      ^^  ^^^^^^
           ^    ^^^^      ^^^  ^ ^  ^^
           ^^   ^ ^^^         ^^ ^^^^ ^
           ^^     ^^ ^       ^^    ^^
            ^      ^^         ^^
            ^   ^^ ^        ^^^
                  ^         ^ ^^
                              ^
```

**Loading.** The world has no LOD, so a tree only draws while its cell is loaded. **197**
of the 487 are in cells -1..1, which are loaded from anywhere in the compound at the
default `uGridsToLoad` of 5. The rest (cells ±2) load as the player nears that side, and
may pop in at the far side of the compound. The wall hides their bases, so what pops is
canopy. The lasting fix is tree LOD for this worldspace (DynDOLOD/xLODGen, which can use
vanilla pine billboards). There are no real distant mountains in this world; the
"mountains" in the gaps are the generated hills beyond the flat margin.

### Records changed

| Type | Change |
| --- | --- |
| STAT | +2 (`SkyrimFairPalisade`, `SkyrimFairPalisadeGate`) |
| REFR | -33 `FarmBannerPost01` scale posts; +106 palisade panels, +1 gate, +487 trees. SkyrimFairWorld now holds 599 references: 5 zone markers + 594 in its cells |
| CELL, LAND, WRLD, CLMT | content unchanged |

**One-time FormID shift.** The 121 exterior CELL and LAND records are now allocated
straight after the zone markers and before anything placed in them. They moved down by
33 FormIDs, the slots the posts had used, and their content is identical. From now on,
changing the wall or forest never moves them. Nothing references them. The WRLD
(`000A16`), climate (`000A15`), persistent cell (`000A17`) and zone markers
(`000A18`-`000A1C`) keep their IDs.

### Verification of this pass

- Release build: zero warnings, zero errors. Generator run twice: identical SHA256
  `da78301a...`.
- The previous approved build (`8b7eb313...`, the deployed copy) and the new one were
  read side by side with Mutagen:
  - **543 of 543** records outside the fair world's cells are identical (Tamriel,
    sandbox, kit STATs, climate, WRLD headers).
  - All 121 LAND records are identical, as are the cell headers apart from FormID, the
    persistent cell and the markers.
- **Wall closure**:
  - 3,863 points every 8 units along the outline all lie inside a wall or gate
    footprint (worst -6.8, i.e. inside).
  - **20,160 sightline rays** from seven interior points (centre, gate, stage, both
    sides, two corners) all cross a wall or gate centreline.
  - A first build had one ray slip between two centrelines at a shallow bend. Panel
    thickness blocked it, but the corner carry-over went from 24 to 48 so it no longer
    depends on thickness.
- **Usable space kept**: no wall or gate footprint enters a zone. The nearest is the
  gate's inner face meeting the entrance forecourt's paint, by design. 0 references
  filed in the wrong cell. 0 banner posts left.
- **Trees**:
  - the nearest is 382 outside the wall line, and the nearest to the gate is 1,862
  - 0 in the approach cone, and the closest pair is 174 apart
  - every tree is a TREE record in Skyrim.esm
- **Deployment**: the ESP is byte-identical in the mod folder, and so are all 52 asset
  files (2 NIF, 50 DDS).
- **Not verified in game**: how it looks, the gate's facing, collision against the wall,
  and performance with 487 full trees.

### What Barry should test in game (this pass)

1. `cow SkyrimFairWorld 0 0`, then turn round slowly. Check for a continuous wall with no
   gaps at the bends, trees above it on every side, and occasional sky between stands.
2. `player.moveto SkyrimFairWorldEntranceMarker` and look at the gate close up. Is it
   the right way round (if not, it's one config value)? Do the panels either side run
   into its posts cleanly? Then turn north: the view should run up the avenue to the
   stage.
3. **Height and scale**: does a 5 m wall with a 6 m gate feel right beside your
   character? Scale is one number each (`palisade.scale`, `gatePiece.scale`).
4. Walk into the wall and the gate: both should stop you.
5. Walk to each side of the compound and watch the far trees. Say if their popping in
   is distracting.
6. Frame rate with the forest in view.
7. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## The isolated worldspace (previous pass; approved by Barry in game)

Everything in this section still holds, except that the temporary posts are gone (replaced by the palisade above) and the cell and LAND FormIDs moved once (see "Records changed" above).

Barry changed direction: the full Wanderer's Fair will eventually live in its own
isolated outdoor worldspace, a large irregular palisade compound reached through a gate
in Tamriel. This pass builds **only the empty canvas**: a new exterior worldspace with
sky, weather, exterior lighting and generated flat ground, with the broad plan painted
into the ground. It is a **parallel prototype**. The raised Tamriel terrace, the
approved staircase and embankment, and the sandbox cell are untouched: all 541 records
of the previous plugin are present and field-for-field identical in the new one (checked
by reading both files, below).

Not built at that pass, by instruction: the Tamriel gate, the palisade itself, stalls, NPCs, archery,
stage systems, quests, navmesh, music, clutter.

### The worldspace

| Field | Value |
| --- | --- |
| EditorID | `SkyrimFairWorld` |
| FormID | `000A16:SkyrimFair.esp` (in game `xx000A16`, `xx` = Skyrim Fair's load-order index) |
| Name (FULL) | `The Wanderer's Fair` (loading screen / HUD) |
| Entry | **`cow SkyrimFairWorld 0 0`** |
| Exit | `cow Tamriel -2 -4` (the Tamriel fair site), or fast travel |
| Zone jumps | `player.moveto SkyrimFairWorldEntranceMarker` (also `...MarketMarker`, `...ActivityMarker`, `...CrowdMarker`, `...StageMarker`) |
| Parent | Tamriel (`00003C`), **Use Map Data only**, as vanilla sub-worlds do. Nothing else is inherited: not land, LOD, water, climate or image space |
| Flags | `NoLodWater`. Fast travel is allowed so the player can leave from the map |
| Climate | `SkyrimFairWorldClimate` (`000A15`), a copy of `SkyrimClimate` (`000812`: sun, glare, moons, sky model, sunrise 05:30-10:00, sunset 16:00-20:30) with its weather list replaced by `WeatherTundraNoPrecip`'s (`1046C9`): `SkyrimCloudyTU` 45, `SkyrimClearTU` 35, `SkyrimClearTU_A` 10, `SkyrimCloudyTU_A` 10. That is what the Whiterun plains get. Tamriel's own climate lists only `SkyrimCloudy` because Tamriel takes its weather from regions, which do not apply here |
| Lighting | Exterior, driven by the weathers (no LGTM on the world or its cells, same as Tamriel) |
| Water | None. No cell has Has Water; default water height -50,000 as a backstop; no WNAM |
| Land defaults | land 0, water -50,000 |
| Object bounds | cells -5,-5 to 6,6 |
| Music, location, encounter zone | none |
| Persistent cell | `000A17`, grid 0,0, Persistent flag set; holds the five zone markers |

### Cells and terrain

- **121 exterior cells, -5..5 on both axes** (45,056 units a side), each with a
  generated LAND record. Every cell is a new record; none is an override.
- **Terrain strategy: generated from rules, not sculpted.** Ground is exactly flat at
  **Z 0** inside the planned perimeter and for 1,024 units beyond it, so the palisade
  will stand on level ground wherever it is finally drawn. Past that it rises over
  8,192 units into low hills of about 1,536 (max 1,688 with a small deterministic
  undulation), closing the view where there is no wall yet. Undulation comes from an
  integer-hashed value noise, never a string hash, so it regenerates byte-identically.
- LAND carries heights (VHGT) and normals (VNML) computed across cell seams from the
  same function; DATA flags `0x1D`, exactly Sovngarde's; no vertex colours (Sovngarde
  has none either); zlib-compressed like vanilla. Checked by decoding the written
  file: **0 seam mismatches** across all 220 cell edges, **0 non-flat vertices** in the
  2,793 sampled across the compound's core.
- **The plan is painted into the ground** with vanilla landscape textures (referenced,
  never copied), at most 4 alpha layers in any quadrant (vanilla uses up to 5):

| Area | LTEX | Look in game |
| --- | --- | --- |
| Compound ground | `LFieldGrass01` `013428` | Whiterun field grass |
| Beyond the perimeter | `LTundra01` `024E30` | rougher tundra |
| Perimeter line, 256 wide | `LTundraRocks01NoRocks` `06DE8B` | stony strip, broken at the gate |
| Avenue, entrance forecourt, crowd square | `LDirtPath01` `0B424C` | bare path, no grass |
| Market side | `LFieldDirtGrass01` `0134B7` | worn earth |
| Activity side | `LFieldGrass01NoGrass` `024E46` | same field texture, no grass: an open range |
| Stage footprint | `LDirt02` `000C16` | darker bare earth |

### The broad plan

The compound is centred on the middle of cell 0,0 (world 2048, 2048), so `cow
SkyrimFairWorld 0 0` puts the player in the middle of it. The entrance is at the south
and the stage at the north, as in DESIGN.md's layout: **entrance, then a busy central
avenue, then the stage as the anchor**, with the market branching west and activities
east.

| Element | Where | Size |
| --- | --- | --- |
| Planned perimeter | irregular 17-point polygon, X -2,052..6,498, Y -2,802..7,198 | **8,550 x 10,000** (about 122 m x 143 m), roughly 3x the Tamriel terrace's span each way |
| Main gate | south wall at (2048, -2777), 640 wide | |
| Entrance forecourt | just inside the gate, widening to about 2,000 | |
| Central avenue | (2048,-2752) to (1898,48) to (2198,2748) to (2048,4748), a slight handmade bend | 800 wide, 7,500 long |
| Crowd square | north end, (2048, 5348) | about 5,300 x 2,000 |
| Stage | backs onto the north wall, faces south, (2048, 6548) | 2,150 x 600 footprint |
| Market | west of the avenue, (148, 1748) | about 2,500 x 5,500 |
| Activity / archery | east of the avenue, marker (3548, 1248) facing east, so an archery line shoots toward the east wall | about 3,000 x 5,600 |

Generator plan view, 512 units per character, north up (`#` perimeter strip, `E`
entrance, `=` avenue, `M` market, `A` activity, `C` crowd, `S` stage):

```text
           #######
        ###SSSSS..##
       ##.CCCCCCCC.##
      ##.CCCCCCCCC..##
      #..CCCCCCCCC...#
     #...CCCCCCCCC...#
      #..MMM.=.AAAAAA.#
      #.MMMM.=.AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.==AAAAAA#
      #MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     #.MMMMM.=.AAAAAA#
     ##MMMMM==.AAAAAA#
      #.MMMM==.AAAAAA#
      #...MM.=.AAAAA.#
       #.....=..AAA.##
        ##..EEE...##
         ####EE###
              #
```

**Why the compound fits in cells -1..1.** The world has no LOD, so only the loaded grid
of cells renders (5 x 5 at the default `uGridsToLoad`). With the whole compound inside
a 3 x 3 block of cells, every cell of it is loaded from anywhere inside it, so the
future palisade will always draw and nothing beyond it needs to. Growing the compound
past that block means the far wall can drop out of view; it would then need the walls
flagged Full LOD, or generated LOD.

### Temporary scale markers

- The 33 temporary `FarmBannerPost01` scale posts that stood on the perimeter line
  were **removed in the palisade pass**. The wall now stands on that line.
- **Five persistent `XMarkerHeading`** (`000034`, invisible in game) with EditorIDs,
  for `player.moveto` and for the Creation Kit:

| EditorID | FormID | Position | Faces |
| --- | --- | --- | --- |
| `SkyrimFairWorldEntranceMarker` | `000A1A` | 2048, -2502 | north, up the avenue |
| `SkyrimFairWorldMarketMarker` | `000A18` | 148, 1748 | east, toward the avenue |
| `SkyrimFairWorldActivityMarker` | `000A19` | 3548, 1248 | east, down the range |
| `SkyrimFairWorldCrowdMarker` | `000A1B` | 2048, 5348 | north, at the stage |
| `SkyrimFairWorldStageMarker` | `000A1C` | 2048, 6548 | south, at the crowd |

### Records added (nothing removed or newly overridden)

| Type | Count |
| --- | --- |
| WRLD | 1 (`SkyrimFairWorld`) |
| CLMT | 1 (`SkyrimFairWorldClimate`) |
| CELL | 122 (121 exterior + the persistent cell) |
| LAND | 121 |
| REFR | 38 at that pass (33 posts + 5 markers); the posts are gone now, see the palisade pass |

Built last in the generator, so it only appends FormIDs: every Tamriel and sandbox
record keeps its previous ID. The exterior block / sub-block grouping was moved into a
shared `ExteriorCellGrid` used by both worldspaces; the Tamriel output is unchanged by
it (identical records, below).

### Verification of this pass

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 `8b7eb313...`.
- Baseline comparison: the previous audited build (`2eaeeeba...`, regenerated from the
  pre-pass tree first and matching this file's old hash) and the new build read side by
  side with Mutagen. **541 of 541 records identical** (WRLD and CELL compared on their
  own fields, every other record in full), 0 missing, 0 changed. Tamriel still has 6
  cells.
- New world decoded from the written ESP: WRLD fields as tabled; climate carries the
  four tundra weathers; 121 cells, 0 with water; 33 posts; five markers with the
  Persistent flag; heights 0..1,688; 0 seam mismatches; compound core flat; LAND
  `0,0` flags `0x1D`, compressed, flat normal `(0, 0, 127)`, two alpha layers per
  quadrant.
- **Not verified: loading in game.** Nothing here has been run in Skyrim. `cow
  SkyrimFairWorld 0 0` needs Barry's test.

### Technical limitations found

- **No LOD** of any kind (terrain, object, tree). Beyond the loaded cells the world
  is empty sky. Handled by the sizing above. xLODGen / DynDOLOD could generate LOD
  for this world later if a wider view is ever wanted.
- **No OFST offset table on the WRLD and no MHDT max-height data on its cells.** The
  Creation Kit normally writes both. Mutagen does not generate them. Vanilla has
  precedent for cells with no MHDT (`CWSiegeTestWorld`). Whether a missing OFST
  matters is **not yet confirmed in game**. It is the first thing to suspect if `cow`
  fails.
- **Grass**: the ground textures carry grass, but the world has no NGIO grass cache.
  If NGIO is set to load grass only from the cache, the fair ground will have no grass
  until the cache is regenerated. The layout reads either way.
- **Map**: the pause-menu map is Tamriel's (Use Map Data). The player's marker there
  means nothing. Fast travel out should work as it does from a city world. Not yet
  tested.
- **No navmesh**, so NPCs cannot path here yet. Nothing needs to until content goes in.
- Weather is climate-driven (no regions), so `fw <weather id>` is how to force one when
  testing.

### What Barry was asked to test (done: approved)

1. `cow SkyrimFairWorld 0 0`: does it load, with sky, sun and weather, and are you
   standing on grass in the middle of the avenue? If it hangs or drops you into a void,
   say so before anything else. That points at the missing OFST / MHDT data.
2. Walk south to the gate (or `player.moveto SkyrimFairWorldEntranceMarker`) and look
   north: the dirt avenue should run up to the big crowd square and the darker stage
   patch, with the worn-earth market on your left and the grass-free range on your right.
3. The posts show the planned wall line. Does the compound feel the right size? It is
   about 122 m x 143 m. Too big and it will feel empty; too small and it won't hold
   everything in DESIGN.md.
4. Look out over the posts: the ground should rise gently into hills and never stop in
   a visible edge.
5. Open the map and fast travel out, to check leaving works.
6. `cow Tamriel -2 -4`: the terrace should be exactly as before.

## Tamriel terrace (unchanged this pass)

### Previous pass: the generator reproduces Barry's completed Creation Kit layout

**Late amendment (2026-09-22 evening):** after seeing the result in game Barry asked for
the big scaled walls to go entirely. At two to three times size the flat cut end of a
wall piece reads as a huge smooth slab, whichever way it is turned. The corner bastions
and the single walls on the short step faces are switched off (`bastions`,
`shortFaceWalls`), and the terrace rows now run through to every corner and along the
short faces too (`cornerStop` 0). No scaled-up Stonewall piece remains in the plugin;
the parapets (0.98), the west field wall (0.9), the cheeks (0.6) and Barry's two 1.06
cheek underpinnings are the only Stonewall01 left. Everything else below still holds.

**Sandbox cell added (2026-09-22, late):** the plugin now carries one interior CELL,
`SkyrimFairSandbox`, so pieces can be looked at in isolation. It is 25 fill bodies and
25 paving caps from the kit in a 5 x 5 square (5,120 units a side, floor at Z 0), a
`COCMarkerHeading` at the centre, and nothing else. Flags are Interior + Show Sky + Use
Sky Lighting (`DATA 0x0181`); every lighting value is inherited from
`DefaultLightingTemplate` (`XCLL` inherit `0x7FF`), the sky comes from
`WeatherTundraNoPrecip` and the image space is `DefaultImageSpaceExterior`. It shares the
foundation's two STAT records, so the STAT count stays at 9. It touches no vanilla record.
The six Tamriel cells and everything in them are unchanged from the row below.

Barry finished the perimeter by hand in the Creation Kit and saved it as
`reference/SkyrimFair.esp` (35,173 bytes, 2026-09-22 17:53). Per his instruction that
plugin is the **design source of truth**, and a **visual/layout reference only**: it is
not authoritative as a plugin and never replaces the generator's output. This pass reads
it, extracts every intentional change, and reproduces the layout from rules so the
generator's plugin lands on the same design.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 419,242 bytes (with dressing) |
| SHA256 | `cdb73a8a7140f6568807158f1b2c51e122fb69227764b1c156ddcd7db3b29fdf` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` (2026-09-22, night) |
| Sandbox cell | `SkyrimFairSandbox` (`0009E1:SkyrimFair.esp`), interior, 5 x 5 kit tiles, `coc SkyrimFairSandbox` in, `cow Tamriel -2 -4` out |
| Isolated worldspace | `SkyrimFairWorld` (`000A16:SkyrimFair.esp`), 121 cells, `cow SkyrimFairWorld 0 0` in; palisade, gate and forest per the palisade pass, mountains per the mountain pass, main stage per the current pass |
| Palisade assets | `meshes\barry_palisades\` (2 NIF) and `textures\barry_palisades\` (50 DDS), deployed byte-identical to `assets/` |
| Kit meshes | unchanged this pass; all 13 deployed NIFs match `assets/nif/SkyrimFair/` |
| Masters | `Skyrim.esm` only |
| Tamriel cells | `-3,-4`, `-2,-4`, `-1,-4`, `-3,-3`, `-2,-3`, `-1,-3` (all byte-identical to vanilla) |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| **Footprint** | **26 cells** (was 33): see below |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN anywhere; 0 LAND in Tamriel. The 121 LAND records are new records in `SkyrimFairWorld`, not edits to any vanilla landscape |
| Floor material | `road01.dds` worn earth on the paving caps |

## What the reference contains, and what was taken from it

The reference was built from an earlier generated plugin, so a FormID diff against the
current one is meaningless; it was read as a complete layout instead, grouped by base
object, and every hand-placed group was classified.

### Footprint: Barry removed the east column and the south tongue

The reference paves 26 cells. Reconstructed from its floor caps, row by row from the
north: `..####.`, `.#####.`, `.#####.`, `.#####.`, `.#####.`, `.##....`. Against the
old 33-cell mask, column 6 (the far east strip) and the south tongue (row 5 columns 3
to 5, all of row 6) are gone, and the row-0 / column-5 notch is filled. His terrace walls
wrap this new outline (east rows at `X -4544` from row 0 to row 4, south rows at
`Y -14290` for columns 3 to 5 and `Y -14836` for columns 1 to 2), so it is deliberate.
**Adopted**: `footprint` in `fair.config.json` is the 26-cell mask. Because the north
row is now four tiles wide, the entrance column is pinned with `rampAlign: "offset:1"`
so the approved staircase stays at `X -5888` ("centre" would have moved it one tile
east). Three of his floor caps had no body under them (he deleted the 1024 fill they
replaced); the generator emits bodies and caps for all 26 cells.

### Perimeter: per-edge layer stacks, not one prototype

The north band is the prototype measured earlier. The other sides differ, and the
generator now carries **one layer stack per compass edge** (`terraceBand.edges`):

| Edge | Outward `StonewallTerrace01` | Inward `StonewallTerrace01` | `Stonewall01` parapet (0.98) | Extra |
| --- | --- | --- | --- | --- |
| N | origin +117, **on grade** | +111, **115 above** the outward piece: the grass slope | -15, floor -153 | |
| E | +64, fixed **floor -296** | +64, fixed floor -424: a grass plinth under the wall | +24, floor -149 | |
| S | +98, fixed floor -296 | +136, fixed floor -424 | +12, floor -149 | |
| W | +136, fixed floor -296 | +136, fixed floor -424 | +20, floor -149 | lower `Stonewall01` at 0.9 on grade, +397 |

Offsets are plan distances from the paving edge, positive outward, taken from his
pieces. On the shallower east, south and west he set the outward wall at one level
(`Z -5632`, crest at floor -121) and, on east and south, an inward piece 128 below it so
nothing floats. On the west he used a third, lower field wall on grade instead. His west
outward walls have no plinth and float 60 to 200 above ground in places; the generator
gives the west the same plinth as east and south. That is the one place it adds to his
layout rather than copying it, and it is flagged here.

Rows are laid along offsets of the outline with even spacing and flush ends as before,
**stop 96 short of every convex corner** (his rows end 78 to 167 short and the corner
is finished by the bastion), and split either side of the stairs. The outward and
inward rows run under the stair solid to the flank, as his do; the parapet runs to 20
past the cheeks' outer face.

### Short step faces: one big field wall

The three one-tile faces that end at a re-entrant corner (row 0's west face, row 1's
north face at column 1, row 5's east face) carry no rows in the reference. He walled
them with one or two `Stonewall01` or `StonewallEndL01` scaled 2.2 to 2.7 so the crest
reaches the floor. **Adopted** as a rule: a face of one tile touching a re-entrant corner
gets one `Stonewall01` on grade, 24 out, scaled to `(drop + 8) / 175` (2.2 to 2.5 here).

### Corners: bastions of scaled wall-ends, no knolls

The reference has **no** `StonewallTerraceCorner01` and no corner boulders. Every
convex corner is closed by `StonewallEndL01` scaled 2.2 to 3.2 so its crest is at floor
level, forming an L that projects outward along both face lines (north-east,
south-east, south-west), or a single wall where the other leg would have stood in front
of a neighbouring face's band (north-west of the stairs: the north-running wall only;
north-west of row 1: the west-running wall only; south-east of row 5: the south-running
wall only). **Adopted** as a rule: at each convex corner, two `StonewallEndL01` on grade,
16 inside their face line, starting 32 inside the corner and running outward `222 x
scale`, scale `(drop + 8) / 171` clamped 1.5 to 3.3, finished end outward; a leg whose
middle would lie within 560 of another edge's outward strip is not placed. That
reproduces his nine corner walls exactly: 9 generated, 9 in the reference.

### Entrance dressing, reproduced piece for piece

Placed relative to the stair head (`entranceDressing`), so the relationships hold if the
entrance ever moves. All positions equal the reference's to 0.1:

- two `WHfirebrazier01` on the level cheek ends at the foot, with
  `FXfireWithEmbersHeavy` fire above each;
- four `Stonewall01` at 0.6 as low wing walls flanking the foot, with a
  `StonewallEndL01` at 0.6 finishing each pair;
- two `FarmBannerPost01` mid-flight, 318 either side of the centreline, with two
  `MarkarthBanner01` on the west post and a `NightingaleBannerAnim02` on the east;
- three `Stonewall01` (1.06, 1.06, 0.6) under the west cheeks so they do not float.

`MarkarthBanner01` and `NightingaleBannerAnim02` are Barry's picks and are reproduced as
placed; they are Reach-city and Nightingale banners, so they read as placeholders for
fair banners that vanilla does not have.

### Ignored as accidental or noise

- Duplicate references at identical transforms (banner posts x4, several walls x2).
- One `StonewallTerrace01` at `Z 0.0` (`000FCF`), 5,300 units in the air.
- One `TreePineShrub01Snow`, a snow shrub in the tundra.
- One `StonewallTerrace02` (the rubble-apron variant) among 68 `Terrace01`.
- Scale rounding to two decimals and sub-unit position noise.
- A vanilla `LvlAnimalPlainsPrey` actor (`0DC5B7`) marked deleted in the CK file. The
  generator never touches vanilla actors; this stays out.
- Three of the generator's old 0.75-scale course walls he kept; they belong to the
  retired language.

### Kept from the generator although absent or fewer in the reference

- **Structural retaining bodies** (43) and both **entrance retaining wings**. He deleted
  48 of 53 retaining bodies and both wings. They are structural: they close the hollow
  under the floor slab and carry the edge collision. In this layout they are hidden
  behind his walls except for a strip of at most 25 units under the parapet, so keeping
  them changes nothing he saw and avoids a see-through gap.
- **The cheek caps** at their generated position; his sit 87 further out and 27 higher.
  The staircase is approved as generated.
- The map marker, the test stall, toe rocks, verge wedges and scrub, which the
  generator regenerates in equivalent positions.

## How close the result is

Nearest-piece match of the generated plugin against the reference, after removing his
duplicates:

| Group | Reference | Generated | Matched | Plan distance median / max |
| --- | --- | --- | --- | --- |
| `StonewallTerrace01` | 68 | 76 | 68 | 57 / 169 |
| `Stonewall01` (parapets, cheeks, big walls, wings) | 90 | 75 | 87 | 30 / 155 |
| `StonewallEndL01` (bastions, caps, foot ends) | 15 | 13 | 12 | 81 / 177 |
| Stairs, braziers, fire, posts, banners | 12 | 12 | 12 | 0 / 0 |

The extra generated terrace pieces are even spacing on runs where he left gaps; the
unmatched walls are his short-face wall-ends where the generator uses one `Stonewall01`
instead, and his three retired courses.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 11 / 11 (26 cells) |
| Kit stair flights | 3 |
| Cheek blocks, level ends, tapered caps, retaining wings | 18, 4, 2, 2 |
| Structural retaining courses / corners | 43 / 1 |
| Band: outward walls / inward pieces / parapets | 44 / 44 / 49 |
| Band: west lower field walls | 12 |
| Band: big walls on short faces / bastion wall-ends | 0 / 0 (switched off, see amendment) |
| Entrance dressing | 18 |
| Toe rocks / verge wedges / shrubs and scrub | 60 / 25 / 129 |
| Cliff pieces | 0 |
| **Vanilla references placed** | **374** |
| Rejected as oversized / for blocking the entrance / for the market floor | 17 / 10 / 0 |

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json   # twice
python tools/footprint_audit.py --data <stock Data> --profile "Still in Skyrim Plus" --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical SHA256 (re-verified after the amendment).
- `footprint_audit.py` against the full load order: 0 vanilla references proud of the
  floor on the foundation.
- No Stonewall-family reference above scale 1.1 remains in the plugin (checked by
  reading the ESP).
- Independent read of the written ESP against the eroded market floor and the walkable
  stair width: **one hit, the deliberate `SMarketStall01` test stall.** Cheeks, parapet
  and the stair-foot dressing (which straddle those lines by design) are excluded and
  noted.
- Nearest-piece match against the reference as tabled above; stairs, braziers, fire,
  banner posts and banners at the reference positions exactly.
- **All six cell overrides byte-identical to vanilla.** WRLD deviation unchanged (RNAM
  dropped, FULL literal).
- Plan view drawn from the plugin: bands on every long face of the 26-cell outline,
  stairs at `X -5888`.
- `modlist.txt`, `plugins.txt` and `loadorder.txt` untouched.

`SKYRIMFAIR_TRACE=1` prints cheek, bank and band decisions to stderr.

## Known and deliberately not done

- Corners are now simply where two rows meet, with the outward walls at their per-edge
  offsets, so a north/east corner has a 53-unit step between the two wall faces. No
  corner piece of any kind is placed; if a corner needs closing, it wants a new idea
  rather than a scaled wall.
- Shrubs are still planted at grade beyond the walls, not on the grass.
- Timber fence on the top edge, the cobbled spur from the road, bunting, pavilion,
  signpost text: not started. NGIO grass cache not regenerated. No navmesh.
- Untracked `music/` folder left out of git; provenance unknown.

## What Barry should test on the terrace (from the previous pass)

1. **The whole perimeter**: each side should read as your own build did - terrace wall,
   grass, knee-high parapet. The big slabs are gone; look at the corners, where the two
   rows now just meet, and say whether they need closing with something else.
2. **The west side** now has the grass plinth under the wall that the east and south
   have. Say if you would rather it stayed as you left it.
3. **The stair foot**: braziers lit on the cheek ends, wing walls and banners where you
   put them.
4. **The retaining bodies you deleted are back** behind the walls. If any earth face
   shows where you had cleared it, tell me where.
5. Market floor clean, stairs climbable, nothing hollow.
