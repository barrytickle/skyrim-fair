# Static crowd figures

Posed vanilla Skyrim characters baked into ordinary STAT scenery, to make the fair look
busy without the AI, package and navmesh cost of more actors. Real NPCs stay in the
foreground and wherever behaviour matters. Static figures fill the background and
midground.

**Status (2026-09-23): one prototype, `SkyrimFairCrowd_Clapping01`, built and checked in
Blender and against the NIF format. It's in the plugin and deployed: one reference at
(580, 1640), yaw 270, east of the archery spectators, facing the range. See
`fairWorld.crowdFigures` and `docs/CLAUDE_CROWD_TASK.md` for what changed from the brief.
Not yet tested in game. Stop here for Barry's review before making more.**

## The prototype

| | |
|---|---|
| NIF | `assets/meshes/SkyrimFair/Crowd/SkyrimFairCrowd_Clapping01.nif` (in game: `meshes\SkyrimFair\Crowd\SkyrimFairCrowd_Clapping01.nif`) |
| Figure | Nord man in Farm Clothes 01 and the matching cloth cap, clapping at chest height |
| Size | 131.9 units tall with the cap (about 1.88 m); footprint x −20.8…25.4, y −12.0…34.9 |
| Cost | **9 BSTriShapes, 9 materials, 6,393 triangles, 4,117 vertices, 158 KB** |
| Blocks | `BSFadeNode`, `BSTriShape` ×9, `BSLightingShaderProperty` ×9, `BSShaderTextureSet` ×9, `NiAlphaProperty` ×3. Nothing else. |
| Collision | None |
| New textures | None. Every path is a vanilla one. |

For scale, one full actor carries the same meshes plus a 99-bone skeleton, skinning,
the behaviour graph, AI packages and pathing.

### Independence, explicitly

- **Runtime skeleton: fully independent.** There are no bones or NiNodes other than the root. It needs no `skeleton.nif`.
- **Actor AI: fully independent.** It's a plain STAT model, with no actor, package or behaviour graph.
- **Skinning data: none left.** There's no `NiSkinInstance`, `BSDismemberSkinInstance`, `NiSkinData` or `NiSkinPartition`. The vertex format is `0x1b` (position, UV, normal, tangent), the same as vanilla `clutter\barrel01.nif`, with the skinned bit (`0x40`) cleared. The shader's Skinned flag is off on every shape.
- **Animation data: none left.** There are no controllers, interpolators, sequences, text keys or Havok blocks.

## Sources

Paths in the extracted archives are under `C:\Users\Barry\Downloads\Skyrim Clothing\meshes\`.

| Role | Mesh | Notes |
|---|---|---|
| Tunic, plus the body skin at the neck and cuffs | `clothes\farmclothes01\torsom_0.nif` + `torsom_1.nif` | Shapes `upperbody` and `MaleUnderwearBodyOutfit` |
| Boots | `clothes\farmclothes01\bootsm_0.nif` + `bootsm_1.nif` | |
| Cap | `clothes\farmclothes01\hatm.nif` | Body slot 31 (Hair, partition 131) |
| Hands | `actors\character\character assets\malehands_0.nif` + `malehands_1.nif` | |
| Head | `actors\character\character assets\malehead.nif` | Generic human male head, no FaceGen |
| Eyes | `actors\character\character assets\eyesmale.nif` | |
| Brows | `actors\character\character assets\faceparts\malebrows.nif` | |
| Mouth | `actors\character\character assets\mouth\mouthhuman.nif` | Teeth and tongue |

- **Body weight:** the `_0` and `_1` variants are blended 50/50, which is Skyrim's own
  weight lerp for an NPC of weight 50. The runtime morph isn't kept.
- **Hair:** none. The cap takes the Hair slot, so the game would hide the hair headpart
  under it anyway. An early build that included `hairshorthumanm.nif` showed strands
  poking through the cap.
- **Skeleton:** `C:\Users\Barry\Downloads\Skyrim Actors\meshes\actors\character\character assets\skeleton.hkx`,
  the vanilla 99-bone humanoid (SHA-256 `2a0bfeb030b9f1ff…`). No bones were added or
  changed. Every shape's skin binding matched its reference pose to within 0.00006 units.
- **Animation:** `...\actors\character\animations\npc_applaud2.hkx` (SHA-256 `6e5d6f8b34174efc…`),
  vanilla's applause idle: 221 frames, 7.33 s, 30 fps, 99 tracks in skeleton order.
- **Pose: frame 90, t = 3.00 s.** It claps steadily at chest height, and in this frame the
  palms are about 8 units apart, just before contact. That reads as clapping better than
  palms pressed together. Frames 79, 118, 135, 165 and 194 were also rendered for comparison.
  - `spectatorclap.hkx` was tried first and rejected: its hands never come closer than
    about 20 units. It's more of a gesturing spectator.
  - `npc_applaud3/4/5` also clap, but higher up, near the face or overhead.
  - They're good candidates for "raised-arm cheer" and "applauding" variants later.

### Textures (all vanilla, all found in the stock SE BSAs)

| Shape | Textures |
|---|---|
| upperbody | `textures\clothes\farmclothes01\torsom_d.dds`, `torsom_n.dds` |
| MaleUnderwearBodyOutfit | `textures\actors\character\male\MaleBody_1.dds`, `textures\default_n.dds`, `MaleBody_1_sk.dds`, `MaleBody_1_S.dds` |
| shoes | `textures\clothes\farmclothes01\shoem_d.dds`, `shoem_n.dds` |
| hat | `textures\clothes\farmclothes01\hatf_d.dds`, `hatf_n.dds` |
| HandMaleBig3rd | `textures\actors\character\male\MaleHands_1.dds`, `textures\default_n.dds`, `MaleHands_1_sk.dds`, `MaleHands_1_S.dds` |
| MaleHeadIMF | `textures\actors\character\male\MaleHead.dds`, `MaleHead_msn.dds`, `MaleHead_sk.dds`, `MaleHead_S.dds` |
| EyesMale | `textures\actors\character\eyes\EyeBrown.dds`, `EyeBrown_n.dds`, `EyeBrown_sk.dds`, `textures\cubemaps\EyeCubeMap.dds`, `...\eyes\EyeEnvironmentMask_M.dds` |
| MaleHeadBrows_ | `textures\actors\character\malebrows\MaleBrow_1.dds`, `MaleBrow1_n.dds` |
| MouthHuman | `textures\actors\character\mouth\MouthHuman.dds`, `MouthHuman_N.dds` |

- Textures are extracted to `build/crowd/Clapping01/preview_resources/` for the Blender
  preview only. Git ignores that folder and nothing from it is shipped.
- No texture was missing.

### Shader changes from vanilla

| Change | Shapes | Why |
|---|---|---|
| SLSF1 Skinned flag cleared | all | The geometry is no longer skinned |
| FaceTint → SkinTint; FaceGen detail map (slot 6) and its flag dropped | head | FaceGen tint masks exist only for named NPCs |
| Skin tint (0.92, 0.80, 0.70) | head, hands, body | The vanilla NIFs store white; an actor normally gets its race's tint at runtime |
| Hair tint (0.36, 0.25, 0.16) | brows | Same reason |
| Model-space normal map → `textures\default_n.dds`, MSN flag cleared | hands (posed 119° from bind), body skin (116°) | See below |

**Why the normal map changes.** Skin in Skyrim uses *model-space* normal maps. They're
only correct while each surface keeps its bind-pose orientation, and the engine rotates
them with the skinning at runtime. Once the pose is baked there's no skinning, so a hand
turned 119° would be lit from the wrong side. Any skin shape turned more than 20° from
bind (`MSN_MAX_ROTATION`) falls back to vanilla's flat tangent-space normal. That keeps
the lighting right and loses fine skin detail, which is invisible at crowd distance. The
head is turned 17°, so it keeps its model-space map with a small, acceptable error.
Clothing uses tangent-space maps, which stay correct after posing.

## Placement convention (every crowd figure)

- Origin between the feet, on the ground: the lowest boot vertex is at exactly Z = 0, and
  X/Y is the midpoint of the two foot bones.
- **The figure faces +Y**, as actors and furniture do. Z is up.
- Scale 1.0 is vanilla human scale. All transforms are baked into the vertices, and the
  root and every shape have identity transforms.
- For `fairWorld.projectStatics` (the generator makes the STAT with these bounds):

```json
{ "editorId": "SkyrimFairCrowdClapping01", "model": "SkyrimFair\\Crowd\\SkyrimFairCrowd_Clapping01.nif",
  "width": 51, "depth": 70, "height": 132 }
```

Figures go in `fairWorld.crowdFigures` (that entry, plus `places: [[x, y, yaw], ...]`),
which the generator builds after everything but the navmesh, so adding one never moves
another record's FormID. Don't use a market module or `projectStatics` for them: both are
built early and would renumber everything after them.

## Pipeline

**Tools:**
- Blender 3.6.23 (`C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe`),
  run from the command line.
- PyNifly 28.3's standalone NIF library (`pyn`, `NiflyDLL.dll`) and HKX codec (`hkx/anim_skyrim.py`),
  both from `C:\Users\Barry\Downloads\Skyrim Actors\crowd-tools\pynifly\io_scene_nifly`.
- PyNifly's Blender add-on is **not** installed or used, because it targets Blender 4.0.
  The HKX codec is the same file the folk-dance work vendors.

```
blender --background --factory-startup --python-exit-code 1 --python tools/crowd/build_crowd.py -- Clapping01
python tools/crowd/verify_crowd.py Clapping01
blender --background --factory-startup --python-exit-code 1 --python tools/crowd/render_nif.py -- Clapping01
# to choose a frame for a new recipe:
blender ... --python tools/crowd/build_crowd.py -- Clapping01 --sheet 79,90,118
```

`tools/crowd/build_crowd.py` (with `crowd_lib.py`, driven by `tools/crowd/recipes/Clapping01.json`) does this:

1. **Armature.** It reads `skeleton.hkx` and builds a Blender armature with all 99 bones at
   the HKX reference pose.
2. **Meshes.** It reads each vanilla NIF: vertices, triangles, UVs, normals, per-bone skin
   weights, skin-to-bone binds, shader properties and all nine texture slots. It blends
   `_0` and `_1` and creates Blender meshes in rest position (bone rest × skin-to-bone,
   checked to agree across every bone of each shape). Each mesh gets vertex groups and an
   Armature modifier (linear blend, as Skyrim skins).
3. **Pose.** It decodes frame 90 of `npc_applaud2.hkx` and sets each pose bone so its
   armature-space matrix equals the HKX frame's world matrix. The posed scene is saved as
   `build/crowd/Clapping01/Clapping01_posed.blend`.
4. **Bake.** It **applies every Armature modifier** and clears the parent, the vertex
   groups and the armature object. What's left is plain posed meshes, saved as
   `Clapping01_baked.blend`.
   - As a cross-check, the source data is skinned a second time without Blender. The baked
     vertices match to within 0.0032 units.
   - That second skinning also provides the posed normals (bind normals rotated by the same
     bone blend, as the engine does). This keeps vanilla's authored seam normals.
5. **Place.** It shifts the vertices so the origin sits between the feet at ground level.
6. **Export.** It writes a Skyrim SE NIF with PyNifly: a `BSFadeNode` root and one
   unskinned `BSTriShape` per shape. The vanilla shader is copied with the changes above,
   the vanilla texture paths are written slot by slot, and vanilla alpha properties are
   kept. Nifly computes the tangents.

**Why not FBX → AssetWatcher?** The BGS Blender exporter and AssetWatcher are installed,
but AssetWatcher is a GUI folder-watcher, and `AGENTS.md` rules out driving GUIs. PyNifly
writes the SE NIF directly from the command line, and the result is checked
independently. It stays a valid alternative route if you prefer the official converter.

The build produces the same file byte for byte each run (SHA-256 `012524da3f79f851…`).

### Removed on the way from actor mesh to static

- 99 skeleton bones.
- All skin instances, skin data, skin partitions and dismemberment partitions (skinned to
  36 bones in the tunic, 36 in the hands, 9 in the body skin, 6 in the boots, 3 in the cap,
  and 1–2 in the head parts).
- The per-vertex bone indices and weights, and the skinned vertex format.
- The shader Skinned flags and the FaceGen detail map.
- The hair mesh, which the cap hides in game.
- No hidden body geometry was cut. The only body skin in the outfit is 174 triangles, and
  a highlight render (`build/crowd/Clapping01/review_body_visibility.jpg`) shows it's what
  you see at the V-neck and the sleeve cuffs.

## Checks

`verify_crowd.py` reads the NIF back from disk. It parses the header block table with
its own code, not PyNifly, and fails on any of these:
- a non-SE version
- a root that isn't a `BSFadeNode`
- any skin, controller, interpolator, sequence, extra-data or Havok block
- a shape that isn't a `BSTriShape`, a skinned vertex format, or missing normals or tangents
- a shader Skinned flag
- bones
- a texture that isn't in the stock SE Data or BSAs
- a lowest point not at Z = 0
- a height outside the human range

It passes and writes `build/crowd/Clapping01/verify.json`.

`render_nif.py` renders the exported file, not the build scene. The review images are in
`build/crowd/Clapping01/`:
- `review_full.jpg`: front, three-quarter, sides and back
- `review_detail.jpg`: hands, head and feet on a Z = 0 plane
- `contact_sheet_applaud2.jpg`: the candidate frames

Checked in the renders:
- Shoulders, elbows, wrists and fingers are natural, and the clapping hands don't
  interpenetrate.
- Knees, ankles and boots are fine, with both feet flat on the ground.
- The neck and waist are fine, and the sleeves and tunic follow the arms.
- There are no collapsed joints, floating pieces or stretched textures.

The preview's skin shading is only approximate (Workbench, with tints multiplied in).

## Limits and open questions for the in-game test

1. **It hasn't been seen in game.** This covers the whole list below: shaders, lighting,
   scale next to real NPCs.
2. **Skin and brow tints on a STAT.** The tints are stored in the shader as a static has no
   actor to supply them. If faces or hands render grey or oddly coloured, the fallback is
   to switch skin shapes to the Default shader type.
3. **Hands and body skin use the flat normal**, so they have less surface detail than an
   actor's. The proper fix would be converting their model-space maps to tangent space.
   That needs new textures (under `textures\SkyrimFair\Crowd\`) and a DDS writer, and no
   `texconv` was found on this machine.
4. **Generic face**, with no FaceGen: the vanilla default male head.
5. **No LOD and no collision.** The player and NPCs walk through the figure. Place it
   where nobody walks, or add a simple box later.
6. ~~Not wired into the plugin yet.~~ Done: `fairWorld.crowdFigures`, one test reference.
7. **Tool paths live in Downloads.** PyNifly and the extracted meshes are read from
   `C:\Users\Barry\Downloads\...` (`crowd_lib.py` constants). The NIF itself needs none of
   them.
8. The output NIF holds Bethesda geometry, so like `Props/` it isn't committed
   (`.gitignore`: `assets/meshes/SkyrimFair/Crowd/`).

## Next, once the prototype is approved

- Figures, from the same recipe format:
  - neutral and relaxed standing, cheering, a raised-arm cheer (`npc_applaud5`), gesturing
    (`spectatortalking`, `spectatorcheer`)
  - holding a tankard (`animobjectcheer*` plus the tankard mesh)
  - seated, leaning, pointing
- Variation: female (`skeleton_female.hkx`, female meshes), more outfits and caps or hoods,
  and turning each copy. The archery range comes first.
