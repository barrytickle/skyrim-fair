# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: close the stair seams, equalise the cheeks, lift collision

The in-game front view confirmed three final entrance faults: a dark seam remained
between each stair flank and its cheek wall, the wall on screen-right was lower than the
one on screen-left, and the actor's feet sank through the flat treads. This pass moves
both cheek runs 12 units inward, raises the lower run to match the taller one, and lifts
the stair's smooth collision plane by one complete riser. The tapered upper caps and
all other entrance geometry remain in place.

The approved site, footprint, elevation, stair dimensions and content scope did not
change. The closed retaining wings added around the stair head remain, because they fix
the independently reported missing rear face.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 86,114 bytes |
| SHA256 | `2173a925428814ed270a4ba35b768d270da0a07387ddbbda90de6d0e5bd0d93d` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | 13 NIFs under `meshes\SkyrimFair\`; rebuilt through Blender / AssetWatcher |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps |

## Diagonal original cheek blocks and upper caps

The entrance uses eighteen vanilla Stonewall-family references at scale 0.6: seven
pitched `Stonewall01` blocks plus a level `Stonewall01` at the bottom of each side, and
a compact `StonewallEndL01` cap at each upper landing.

The only visual change from that original is rotation:

- every block remains aligned along the stair with Z rotation 90 degrees;
- fourteen diagonal blocks have X rotation **+30.3 degrees**, matching
  `atan2(112 stair rise, 192 stair run)`;
- four end references have zero X/Y pitch: two ordinary lower blocks and two tapered
  upper caps;
- all blocks remain lowered 48 units without changing the diagonal angle. Both crests
  now sit 32 units above the nosing, matching the taller screen-left run;
- both wall centrelines moved 12 units toward the steps. Their irregular inner edges
  now overlap the authored stair footprint by about 16.7 units rather than relying on
  a nominal four-unit overlap that still exposed a dark seam;
- the three flights are treated as one 748.8-unit run. Seven diagonal pieces per side
  are evenly spaced 102.7 apart; their 132.8-unit projected lengths overlap by about
  30.1, including across both former flight joins;
- each lower level block overlaps the diagonal run by 16 units;
- each upper cap is the matching vanilla `StonewallEndL01` (`0000099E:Skyrim.esm`),
  222 units long rather than 256 before scaling. At the current transform it reaches
  only about 107 units onto the terrace side while overlapping the diagonal run by
  about 26 units; it replaces the former full top block rather than extending it;
- both upper caps use the same end variant because both wall runs share the same local
  direction: the finished left end faces uphill and the continuing edge faces downhill;
- the generator applies the pitch from the entrance direction, so the same logic remains
  correct if the entrance edge changes later.

The written ESP was read independently after generation: exactly 16 references use
`Stonewall01` at scale 0.6—14 serialize `[+30.3, 0, 90]` degrees and two serialize
`[0, 0, 90]`. Exactly two references use `StonewallEndL01` at scale 0.6 and serialize
`[0, 0, 90]`.

World placements:

| Side | X | diagonal Y centres | lower block Y | upper cap Y |
| --- | --- | --- | --- | --- |
| W | -6022 | -11582, -11479, -11376, -11274, -11171, -11068, -10966 | -10838 | -11699 |
| E | -5754 | same | -10838 | -11699 |

The project-authored `SkyrimFair_StairCheek_192` remains in the reproducible asset kit as
an unused comparison/prototype, but it has no STAT record and no placed reference in the
current plugin.

## Closed retaining face around the stair head

Two `SkyrimFair_EntranceRetainWing_144` references remain at
`(-6072, -11648, -5336)` and `(-5704, -11648, -5336)`. They close the omitted structural
face on either side of the entrance while leaving a 224-unit central opening around the
217.1-unit stair. They do not bridge the walking route visually or with collision.

## Foundation and entrance retained unchanged

- 33-cell irregular footprint, emitted as 12 structural paving bodies and 12 caps.
- Three `SkyrimFair_Stair_192` flights at scale 1.3, eight steps per flight, with built-in
  smooth box collision. The collision plane is lifted one 14-unit source riser (18.2
  world units at scale 1.3), so it descends from one riser above each tread to flush at
  its downhill edge and never falls below the visible stone.
- Stair heads remain `(-5888, -11648 / -11398 / -11149)`, at
  `Z -5336 / -5482 / -5627` (rounded display values).
- Entrance banks use closed boulders on the west and closed grassy piles on the east.
- No dirt-cliff mesh is placed anywhere; `cliffMinRunSegments` remains 99.
- All-four-edge layered perimeter remains enabled with `Stonewall01` at scale 0.75.

## What is on the site

| Element | Count |
| --- | --- |
| Paving bodies / visual caps | 12 / 12 |
| Kit stair flights | 3 |
| Tilted vanilla cheek blocks | 14 |
| Level lower cheek blocks | 2 |
| Tapered upper masonry caps | 2 |
| Entrance retaining wings | 2 |
| Entrance bank pieces | 10 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces (all edges) | 57 |
| Part-buried rocks ending a run | 12 |
| Embankment rocks | 84 |
| Corner stones | 8 |
| Toe rocks | 68 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 143 |
| Cliff pieces | 0 |
| Vanilla references placed | 400 |
| Project-kit references placed | 108 |
| Test stall and map marker | 2 |
| STAT records created | 9 |
| Rejected as oversized | 18 |
| Rejected for blocking the entrance | 74 |
| Rejected for protruding through the market floor | 45 |

Relative to the prior plugin, the same eighteen cheek references move inward and the
west run rises 24 units. The tighter wall envelope deterministically rejects one entrance
bank boulder and admits one additional shrub, leaving 10 bank pieces and 143 shrubs.
Tamriel WRLD and the same four exterior CELL records remain overridden.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json  # twice
python tools\footprint_audit.py --data <stock Data> \
    --profile <Still in Skyrim Plus profile> --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical 86,114-byte output and identical SHA256.
- Blender 3.6.23 rebuilt all 13 FBX files with zero face-orientation assertion failures;
  AssetWatcher converted all 13 NIFs.
- Before deployment, binary comparison against the old deployed meshes found the child
  collision translation in both `SkyrimFair_Stair_192.nif` and fallback
  `SkyrimFair_StairCollision.nif` increased by `0.200025` Havok metres—the converted
  representation of the intended 14 Skyrim-unit lift.
- The local static-audit utility now accepts explicit search terms. It confirmed vanilla
  `StonewallEndL01` (`00099E`) is 222 x 138 x 171 and that the active Nordic Stonewalls
  mod supplies its matching replacement mesh.
- Direct ESP read confirmed 14 pitched `Stonewall01`, two level lower `Stonewall01`, and
  exactly two level `StonewallEndL01` upper caps, all at scale 0.6.
- Full-load-order audit: 40 vanilla references intersect the footprint/margin, with zero
  references standing proud of floor `-5336`.
- Generated ESP copied to the MO2 mod and compared byte-for-byte by SHA256.
- The complete reproducible kit was rebuilt and redeployed; visual stair geometry and
  every non-stair source dimension remain unchanged.
- `modlist.txt`, `plugins.txt`, `loadorder.txt`, saves, grass cache and generated LOD were
  not touched.

## Known and deliberately not done

- This deliberately restores terrain-dependent vanilla wall meshes. Pitch and placement
  are verified in the plugin, but only the in-game retest can confirm that their backs
  and undersides are acceptably buried from all approach angles.
- Timber fence, braziers and the cobbled road spur remain unstarted pending entrance
  approval.
- NGIO grass cache has not been regenerated. No navmesh exists.
- The Tamriel WRLD override still writes literal English `Skyrim` and drops RNAM.
- The deliberate `SMarketStall01` test stall remains on the paving.
- Untracked `music/` WAV files remain untouched and outside git.

## What Barry should test in game

1. Face uphill and confirm both cheek crests have the same height and silhouette.
2. Check the complete diagonal run from both sides. There should be no seam between the
   step edges and walls, no gaps between blocks, and no gaps at the former flight joins.
3. Check that each tapered upper cap reads as a compact finished masonry pier, not as
   another full wall segment, and that no natural rock intrudes onto the terrace top.
4. Look beneath and behind the tilted blocks for exposed open mesh; the separate rear
   retaining wings should still close the terrace at the stair head.
5. Walk and pause on several treads. Boots should no longer sink into the stone; also
   confirm the one-riser lift does not make the actor visibly hover or snag at joins.
