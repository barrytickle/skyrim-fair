# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: original cheek blocks pitched downhill with the stairs

Barry compared the original small vanilla cheek blocks with the continuous
project-authored replacement in game. The replacement was structurally tidy but too
engineered. The chosen direction is the simpler original arrangement, with one change:
each block is rotated to follow the stair's overall descent. The first implementation
used Skyrim's opposite Euler sign and visibly leaned uphill; the corrected sign is now
verified directly in the written ESP.

The approved site, footprint, elevation, stair dimensions and content scope did not
change. The closed retaining wings added around the stair head remain, because they fix
the independently reported missing rear face.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 85,670 bytes |
| SHA256 | `2c5fc97600616b4ce05e2be288ce07e7e0b9a160fdbf6bae447ab6241bff365a` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | 13 NIFs under `meshes\SkyrimFair\`; unchanged in this ESP-only pass |
| Masters | `Skyrim.esm` only |
| Centre / floor | `X -5888, Y -13440`, floor `Z -5336` |
| Cells | `-2,-4`, `-1,-4`, `-2,-3`, `-1,-3` |
| Vanilla references disabled | 0 |
| Dirt-cliff pieces placed | 0 |
| Forbidden records | 0 LAND, NAVM, NPC_, QUST, PACK, DIAL, INFO, SCEN |
| Floor material | `road01.dds` worn earth on the paving caps |

## Diagonal original cheek blocks

The entrance again uses twelve vanilla `Stonewall01` references (`0000099B:Skyrim.esm`):
two per side per flight, at scale 0.6. Their spacing, asymmetry and chunky mesh are the
same as the original version Barry preferred.

The only visual change from that original is rotation:

- each block remains aligned along the stair with Z rotation 90 degrees;
- each block now has X rotation **+30.3 degrees**, matching
  `atan2(112 stair rise, 192 stair run)`;
- Y rotation is zero at the current north-facing entrance;
- the centre of each tilted crest is anchored to the same nosing-relative height as the
  upright version. West stays 56 above the nosing and east stays 80 above it;
- the generator applies the pitch from the entrance direction, so the same logic remains
  correct if the entrance edge changes later.

The written ESP was read independently after generation: exactly 12 references use
`Stonewall01` at scale 0.6, and every one serializes rotation `[+30.3, 0, 90]` degrees.

World placements:

| Side | X | Y | Z origins |
| --- | --- | --- | --- |
| W | -6034 | -11571, -11475, -11322, -11226, -11072, -10976 | -5415, -5471, -5561, -5617, -5706, -5762 |
| E | -5742 | same | -5391, -5447, -5537, -5593, -5682, -5738 |

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
  smooth box collision.
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
| Tilted vanilla cheek blocks | 12 |
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
| Vanilla references placed | 394 |
| Project-kit references placed | 108 |
| Test stall and map marker | 2 |
| STAT records created | 9 |
| Rejected as oversized | 18 |
| Rejected for blocking the entrance | 75 |
| Rejected for protruding through the market floor | 45 |

Relative to the prior plugin, six project cheek references and their STAT were removed,
and twelve rotated vanilla wall references were restored. Deterministic bank/plant
placement returned to 10 entrance-bank pieces and 143 shrubs. Tamriel WRLD and the same
four exterior CELL records remain overridden.

## Verification performed

```powershell
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json  # twice
python tools\footprint_audit.py --data <stock Data> \
    --profile <Still in Skyrim Plus profile> --mods <mods> \
    --esp dist/SkyrimFair.esp --floor=-5336
```

- Release build: zero warnings, zero errors.
- Generator run twice: identical 85,670-byte output and identical SHA256.
- The audit reader now exposes all three serialized rotation components. Direct ESP read
  confirmed 12 cheek references at scale 0.6 and pitch +30.3 degrees.
- Full-load-order audit: 40 vanilla references intersect the footprint/margin, with zero
  references standing proud of floor `-5336`.
- Generated ESP copied to the MO2 mod and compared byte-for-byte by SHA256.
- No FBX or NIF was rebuilt or redeployed in this pass.
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

1. Compare directly with the original: the same chunky blocks should now lean down the
   staircase rather than stand vertically.
2. Check whether the diagonal blocks remain low enough and whether their overlaps read
   naturally from both sides.
3. Look beneath and behind the tilted blocks for exposed open mesh; the separate rear
   retaining wings should still close the terrace at the stair head.
4. Walk up, down and brush both edges to confirm the rotated vanilla collision does not
   snag or intrude into the stair route.
