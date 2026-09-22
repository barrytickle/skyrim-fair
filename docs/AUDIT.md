# Local Audit

This is the current verified state of Skyrim Fair and Barry's local deployment. Git history holds older reports; this file is a complete current snapshot.

## Current pass: compact masonry caps at the terrace landing

Barry approved the low, overlapping diagonal cheek run and requested a more deliberate
upper termination: compact masonry end-caps on both sides, with no natural material at
the terrace landing and no additional full wall segment. Local `Skyrim.esm` inspection
found the matching `StonewallEndL01`; its tapered end faces the terrace while its open
run overlaps the first diagonal block downhill. The existing lower `Stonewall01`
terminations remain unchanged.

The approved site, footprint, elevation, stair dimensions and content scope did not
change. The closed retaining wings added around the stair head remain, because they fix
the independently reported missing rear face.

| Field | Verified value |
| --- | --- |
| Branch | `feat/bootstrap-generator` |
| Output | `dist/SkyrimFair.esp` |
| Size | 86,114 bytes |
| SHA256 | `ab0437f61ec182b6124123d6703f34be126cd53c81f870df4e3be7dadacd7876` |
| Deployed | byte-identical at `E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp` |
| Kit meshes deployed | 13 NIFs under `meshes\SkyrimFair\`; unchanged in this ESP-only pass |
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
- all blocks were lowered 48 units without changing the diagonal angle. The west crest
  is now 8 above the nosing and the east crest 32 above it;
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
| W | -6034 | -11582, -11479, -11376, -11274, -11171, -11068, -10966 | -10838 | -11699 |
| E | -5742 | same | -10838 | -11699 |

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
| Tilted vanilla cheek blocks | 14 |
| Level lower cheek blocks | 2 |
| Tapered upper masonry caps | 2 |
| Entrance retaining wings | 2 |
| Entrance bank pieces | 11 |
| Structural retaining courses | 53 |
| Drystone field-wall pieces (all edges) | 57 |
| Part-buried rocks ending a run | 12 |
| Embankment rocks | 84 |
| Corner stones | 8 |
| Toe rocks | 68 |
| Rough-earth verge wedges | 23 |
| Shrubs and scrub | 142 |
| Cliff pieces | 0 |
| Vanilla references placed | 400 |
| Project-kit references placed | 108 |
| Test stall and map marker | 2 |
| STAT records created | 9 |
| Rejected as oversized | 18 |
| Rejected for blocking the entrance | 74 |
| Rejected for protruding through the market floor | 45 |

Relative to the prior plugin, two full upper `Stonewall01` references were replaced
one-for-one with `StonewallEndL01`; reference counts and all deterministic dressing remain
unchanged. Tamriel WRLD and the same four exterior CELL records remain overridden.

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
- The local static-audit utility now accepts explicit search terms. It confirmed vanilla
  `StonewallEndL01` (`00099E`) is 222 x 138 x 171 and that the active Nordic Stonewalls
  mod supplies its matching replacement mesh.
- Direct ESP read confirmed 14 pitched `Stonewall01`, two level lower `Stonewall01`, and
  exactly two level `StonewallEndL01` upper caps, all at scale 0.6.
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

1. Confirm the cheek walls are now low enough: west should barely clear the nosing and
   east should remain only modestly higher.
2. Check the complete diagonal run from both sides. There should be no gaps at individual
   blocks or at the two former flight joins.
3. Check that each tapered upper cap reads as a compact finished masonry pier, not as
   another full wall segment, and that no natural rock intrudes onto the terrace top.
4. Look beneath and behind the tilted blocks for exposed open mesh; the separate rear
   retaining wings should still close the terrace at the stair head.
5. Walk up, down and brush both edges to confirm the rotated vanilla collision does not
   snag or intrude into the stair route.
