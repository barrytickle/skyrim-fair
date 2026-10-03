# Riverwood Has Charm and Walls: wooden wall and gate asset audit

Audit of the local download `external/Riverwood Has Charm and Walls-146520-1-4-1-1746134441.7z`
(Nexus mod 146520, author J3w3ls, FOMOD version 1.4.1, uploaded 2025-05-01), carried out
2026-09-22 to answer one question: can Skyrim Fair borrow the wooden palisade walls and
gates for its own perimeter?

Everything below was read from the archive, the plugin inside it, and the Nexus page.
Nothing was copied into the repository or into the Skyrim install.

## Verdict

**Technically easy, but not permitted yet.** The wall and gate meshes are clean,
self-contained SSE statics that would drop into the generator with two custom textures.
The blocker is licensing: J3w3ls requires permission before any asset from this file is
used elsewhere, and says the assets will be released as a free modder's resource "eventually".
Until either of those happens, treat the mod as a visual reference only and do not copy,
convert or redistribute any of its files.

## Permissions (Nexus page, read 2026-09-22)

| Permission | J3w3ls's setting |
| --- | --- |
| Other user's assets | "All the assets in this file belong to the author, or are from free-to-use modder's resources" |
| Upload to other sites | Not allowed under any circumstances |
| Modification | Must get permission first |
| Conversion to other games | Not allowed |
| **Asset use** | **"You must get permission from me before you are allowed to use any of the assets in this file"** |
| Asset use in sold mods | Not allowed |
| Donation points | Not allowed for mods that use these assets |

Author notes, verbatim: "You may not use the assets of this mod in your own without
discussing it with me. Know that those assets will be included in a modder's resource
eventually, free to use there. You may not port my mods to Bethesda.net nor Skyrim LE nexus."

Third-party provenance inside the mod (from the file credits): LucidAPs (High Poly Project
smelter rocks), riton67000 (Farmhouse Parallax II, source of the WoodPost02 textures),
Vermunds (Bells of Skyrim bell), Wenderer (FOMOD tool), RavenKZP (xEdit script),
MaskedRPGFan (MCM settings loader). None of these touch the wall or gate meshes except
that the wall posts *reference* vanilla `woodpost02` paths, so a riton67000 retexture would
show through if the user has one installed. The bell in `J3_RW_BellTower.nif` is Vermunds's
and would need its own permission check.

## Two routes forward

1. **Ask J3w3ls.** Send a message on Nexus describing Skyrim Fair (free, non-commercial,
   credited) and listing the exact files below. Record the reply in `CREDITS.md` before
   copying anything.
2. **Wait for the modder's resource.** The author has said one is coming. If it appears,
   its own permissions apply and this audit's file list still tells us what to take.

A third option needs no permission: keep the fair on vanilla `Stonewall`/`Stockade` pieces
and treat Riverwood Has Walls as a look-and-feel reference only.

## What is in the archive

422 entries, 193.6 MB compressed. 271 NIFs, 19 DDS, 4 ESPs, 5 Papyrus scripts, FOMOD.
The meshes come in three shader variants of the same geometry, selected by the FOMOD:

| Folder | Shader on wall/gate shapes | Notes |
| --- | --- | --- |
| `complex/` | EnvMap (complex parallax material) | needs `_p` height maps and Community Shaders / ENB complex parallax |
| `parallax/` | Parallax | classic parallax, needs `_p` maps |
| `noparallax/` | Default | **the one to take**: no parallax, no `_p` textures needed |

Each variant has `1. Overhaul` (buildings), `2. Fortifications` (walls, gates, towers),
`4. Less Nordic` and `9. Vanilla` sub-folders. Only `2. Fortifications` is relevant here.
All NIFs are Gamebryo 20.2.0.7, BS version 100 (Skyrim SE), stamped "Optimized with SSE NIF
Optimizer v3.2.0". Every fortification piece carries `bhkCollisionObject` collision
(compressed-mesh MOPP on statics, `bhkBoxShape` on the animated gates) and `BSXFlags`.

## The wooden wall and gate pieces

Sizes are the visible geometry's bounding box in game units, 1 m = 70.03 units. `zmin`
below zero is buried post length, which is how the pieces sit on sloping ground.

| File | Record | Tris | X (m) | Y (m) | Z (m) | zmin | zmax | Used in Riverwood | LOD mesh |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `J3_RW_WallStraightShort.nif` | STAT | 5,246 | 4.1 | 2.6 | 4.9 | -189 | 151 | 29 | no |
| `J3_RW_WallStraightMid.nif` | STAT | 10,904 | 9.3 | 2.7 | 4.9 | -189 | 154 | 3 | yes |
| `J3_RW_WallStraightMidUp.nif` | STAT | 10,904 | 9.3 | 2.7 | 6.3 | -188 | 252 | 7 | yes |
| `J3_RW_WallStraightMidUpReversed.nif` | STAT | 10,904 | 9.3 | 2.7 | 6.3 | -188 | 252 | 3 | yes |
| `J3_RW_WallJunction.nif` | STAT | 2,981 | 1.6 | 1.6 | 7.3 | -211 | 300 | 44 (at scale 1.05) | yes |
| `J3_RW_GateNoFrame.nif` | **DOOR** (animated) | 9,240 | 6.9 | 1.0 | 4.9 | -20 | 319 | 3 | no |
| `J3_RW_GateWay.nif` | STAT | 8,596 | 16.5 | 6.1 | 9.7 | -7 | 675 | 1 | yes |
| `J3_RW_WaterGate01.nif` | **DOOR** (animated) | 26,680 | 17.4 | 2.8 | 6.2 | -52 | 379 | 2 | no |
| `J3_RW_GateWayNorth.nif` | STAT | 31,538 | 24.2 | 16.5 | 20.4 | -18 | 1412 | 1 | yes |
| `J3_RW_GateWaySouth.nif` | STAT | 27,740 | 21.8 | 12.6 | 14.4 | -40 | 965 | 1 | yes |
| `J3_RW_Watchtower.nif` | STAT | 14,876 | 9.5 | 10.1 | 14.4 | -40 | 965 | 2 | yes |
| `J3_RW_GuardHouse.nif` | STAT | 4,872 | 8.8 | 9.8 | 6.9 | -40 | 444 | 1 | yes |
| `J3_RW_CoveredBridge_Full.nif` | STAT | 26,952 | 28.2 | 6.5 | 12.8 | -492 | 407 | 2 | yes |
| `J3_RW_BellTower.nif` | STAT | 4,233 | 3.3 | 3.2 | 5.4 | 0 | 376 | 3 (scale 0.8) | no |

Not listed: `_HalfBuilt` construction-stage variants, `BuildingMaterials_*` clutter piles,
`CoveredBridge_Short` and `BellTower_NoBell` (unused in the plugin). The plugin also defines
`J3_RW_WallStraightLong` but its NIF is not in the archive and no reference uses it.

The Riverwood palisade is assembled from short (4.1 m) wall runs between 44 junction posts,
with the 9.3 m pieces used sparingly. The `MidUp` pieces step the wall up a slope.

The two gates are `DOOR` records with `NiControllerManager` open/close animation and
door sounds. `J3_RW_GateNoFrame` is the leaf only; `J3_RW_GateWay` is the static frame it
sits in. Placing them from the generator would need a `Door` record, not a `Static`.

## Texture dependencies

Wall and gate shapes reference these texture sets (diffuse, `_n`, `_m` in each case):

| Texture | Origin | Shipped in archive |
| --- | --- | --- |
| `textures\j3w3ls\riverwood\riverwoodpost.dds` | **custom, J3w3ls** | yes: 2048 square, BC7, 5.6 MB each for `d`, `_n`, `_p` |
| `textures\j3w3ls\riverwood\riverwoodwoodplanks.dds` | **custom, J3w3ls** (author: "the plank texture is custom") | yes: 4096 square, BC7, 22.4 MB each for `d`, `_n`, `_p` |
| `textures\j3w3ls\riverwood\smelteriron01.dds` | custom, on gateways/towers/guardhouse only | yes: 4096 square, 22.4 MB each |
| `textures\clutter\stockade\stockadeextra01.dds` | vanilla | no (confirmed in `Skyrim - Textures0.bsa`) |
| `textures\architecture\farmhouse\rope01.dds` | vanilla | no (confirmed) |
| `textures\architecture\farmhouse\woodpost02.dds` | vanilla path | no |
| `textures\architecture\whiterun\wrlargewoodpillar01.dds` | vanilla | no (confirmed) |
| `textures\architecture\farmhouse\stonewall01.dds` | vanilla, guardhouse only | no |
| `textures\cubemaps\ore_iron_e.dds` | vanilla | no (confirmed) |
| `textures\dwemetaltiles03_gray.dds` | custom recolour of vanilla dwemer tiles, bell tower only | yes: 1024 square |

No `_m` environment-mask textures are shipped for the custom sets even though the
`complex` variant references them; the `noparallax` variant does not use env-mapping, which
is another reason to prefer it. The LOD meshes only use `riverwoodpost` and
`riverwoodwoodplanks`.

The 4K plank set is the weight: 67 MB for `d` + `_n` + `_p`, or 45 MB without the parallax
map. For a fair-sized perimeter a 2K downscale would be sensible, but that is a modification
and needs the author's agreement too.

## Minimum file set, if permission is granted

For the plain palisade (walls, junctions, one gate) in the no-parallax variant:

- `noparallax/2. Fortifications/meshes/J3w3ls/architecture/Riverwood/J3_RW_WallStraightShort.nif`
- same folder: `J3_RW_WallStraightMid.nif`, `J3_RW_WallStraightMidUp.nif`, `J3_RW_WallStraightMidUpReversed.nif`
- same folder: `J3_RW_WallJunction.nif`
- same folder: `J3_RW_GateNoFrame.nif` and `J3_RW_GateWay.nif` (or `J3_RW_WaterGate01.nif`)
- `LOD/meshes/LOD/J3w3ls/J3_RW_WallStraightMid*_LOD.nif`, `J3_RW_WallJunction_LOD.nif`, `J3_RW_GateWay_LOD*.nif`
- `textures/J3w3ls/Riverwood/RiverwoodPost.dds` and `RiverwoodPost_n.dds`
- `textures/J3w3ls/Riverwood/RiverwoodWoodPlanks.dds` and `RiverwoodWoodPlanks_n.dds`

Roughly 3.5 MB of meshes and 56 MB of textures. Everything else those meshes need is
vanilla and already in every player's install. Renaming the paths out of `J3w3ls\` would
avoid colliding with a user who also runs Riverwood Has Walls, but is again a modification.

## Generator impact

- Walls and junctions are ordinary `Static` records: same pattern as the `Stonewall01`
  pieces the generator already places. New `Static` records pointing at the copied NIF paths,
  then `PlacedObject` references along the outline.
- Gates need `Door` records (with the vanilla wooden gate open/close sounds) and, if they
  should be walk-through, a navmesh cut. `J3_RW_WallJunction.nif` already carries a
  `Navcut:WallJunction` node, so the posts cut navmesh automatically.
- The pieces bury 189 to 211 units of post below the origin, so on the fair's paved terrace
  they would need lifting to the paving surface or clipping through the caps, unlike the
  vanilla terrace walls which sit on their base.
- Piece lengths (284 and 651 units) do not divide the 1024-unit cell grid the fair is
  built on; runs would be laid at even spacing with a junction post at every joint, as
  Riverwood itself does.
