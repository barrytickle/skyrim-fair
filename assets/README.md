# Skyrim Fair project-owned assets

Everything in this folder is authored for Skyrim Fair. No third-party mesh or texture data is copied or referenced here.

## Layout

```text
assets/
  blender/
    build_foundation_kit.py      source of truth - generates the whole kit
    fair_foundation_kit.blend    build output, committed for convenience
  fbx/
    SkyrimFair/
      SkyrimFair_*.fbx         BSFBX export, input to AssetWatcher
  nif/
    SkyrimFair/
      SkyrimFair_*.nif         AssetWatcher output, ready for the plugin
```

The **script is the source of truth**, matching the rest of the project: the `.blend` and `.fbx` files are build outputs and can be regenerated at any time.

## Toolchain

Uses Bethesda's official pipeline, which ships with Creation Kit 2.0. Nothing third-party is required.

| Component | Path |
| --- | --- |
| Blender 3.6.23 (portable) | `C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe` |
| BGS Art Tools [Skyrim] 1.0.0 | installed from `...\Skyrim Special Edition\Tools\ArtTools\Blender\bgs_skyrim_tools.zip` |
| BGS FBX Exporter [Skyrim] 1.0.0 | installed from `...\Tools\ArtTools\Blender\io_scene_bsfbx_skyrim.zip` |
| AssetWatcher (FBX to NIF) | `...\Skyrim Special Edition\Tools\AssetWatcher\AssetWatcher.exe` |
| Creation Kit 2.0 v1.7.99 | `E:\SteamLibrary\steamapps\common\Skyrim Special Edition\CreationKit.exe` |

Addons are installed into Blender **3.6's own config** (`%APPDATA%\Blender Foundation\Blender\3.6\scripts\addons`), so Barry's Blender 5.2 install is untouched.

Blender 5.2 cannot be used: both Bethesda addons declare `"blender": (3, 6, 0)` via the legacy `bl_info` system and bundle a 3.6-era copy of Blender's FBX exporter.

## Rebuilding the kit

```powershell
& "C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe" --background --python assets\blender\build_foundation_kit.py
```

That regenerates the `.blend` and all six `.fbx` files, applies collision and prints a full geometry and collision report.

## Remaining manual step: FBX to NIF

**AssetWatcher is a Qt GUI application with no command-line interface**, and Bethesda's guide says to run it as administrator. This step cannot be automated, so it has to be done by hand — once. After the project is saved, AssetWatcher converts automatically on every future export.

Launch:

```text
E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Tools\AssetWatcher\AssetWatcher.exe
```

Settings tab → **Create New Project**, then in the Watch Settings window:

| Field | Value |
| --- | --- |
| File Types | enable **Meshes** |
| Platform | ensure **PC** is enabled |
| **Source folder** | `E:\html\skyrim-fair\skyrim-fair\assets\fbx` |
| **Output Folder** | `E:\html\skyrim-fair\skyrim-fair\assets\nif` |

Save the project, then click **Save** again in AssetWatcher to persist settings. Check the eye icon shows the project **ON**.

### Why those two paths

AssetWatcher **mirrors the Source folder's subfolder structure into the Output folder**. The FBX files live in `assets\fbx\SkyrimFair\`, so the conversion lands them at:

```text
assets\nif\SkyrimFair\SkyrimFair_FloorFill_1024.nif
                     \SkyrimFair_FloorEdge_512.nif
                     \SkyrimFair_Retain_512.nif
                     \SkyrimFair_RetainCorner_128.nif
                     \SkyrimFair_Ramp_512.nif
                     \SkyrimFair_Shoulder_512.nif
```

That `SkyrimFair` folder level is what gives the plugin its mesh paths, which are **relative to `Data`**:

```text
meshes\SkyrimFair\SkyrimFair_FloorFill_1024.nif
```

Set Source to `assets\fbx` — **not** `assets\fbx\SkyrimFair` — or that folder level is lost and the NIFs land loose.

Output goes to `assets\nif` inside the repo rather than a game `Data\Meshes`, which keeps every project-owned artefact in one place and writes into neither the Steam install nor the MO2 game. Deployment into the MO2 mod folder is a separate step, exactly like the ESP. Bethesda's guide suggests pointing Output at `Data\Meshes` instead, which is fine if you want the Creation Kit to find the NIFs automatically for preview.

### Triggering the conversion

AssetWatcher converts on file change, so with the project ON, re-run the build script and the six FBX files will be rewritten and converted:

```powershell
& "C:\Blender\blender-3.6.23-windows-x64\blender-3.6.23-windows-x64\blender.exe" --background --python assets\blender\build_foundation_kit.py
```

### Deployment target

Once NIFs exist, they belong in the MO2 mod alongside the plugin:

```text
E:\Modlists\Still In Skyrim\mods\Skyrim Fair\meshes\SkyrimFair\*.nif
```

The NIFs now exist and are verified, so `STAT` records can be generated. Their in-game mesh paths are `meshes\SkyrimFair\<name>.nif`.

## The kit

All dimensions in Skyrim units, and **verified exact** in the converted NIFs. See the scale note below.

| Piece | Footprint | Height | Collision | Purpose |
| --- | --- | --- | --- | --- |
| `SkyrimFair_FloorFill_1024` | 1024 x 1024 | 32 thick | self, Box | interior paving. 9 cover a 3072 core |
| `SkyrimFair_FloorEdge_512` | 512 x 512 | 32 thick | self, Box | outer ring, half size so the outline can step in 512u increments |
| `SkyrimFair_Retain_512` | 512 x 128 | 256 tall | self, Box | retaining face hanging below the floor plane |
| `SkyrimFair_RetainCorner_128` | 128 x 128 | 256 tall | self, Box | turns the stepped outline |
| `SkyrimFair_Ramp_512` | 512 x 512 | rises 64 | **child box rotated 7.13 deg** | 1:8 chainable ramp; 3 cover the ~160u west-edge fall |
| `SkyrimFair_Shoulder_512` | 512 x 256 | tapers 32 to 0 | **none, intentional** | rough-earth / grass transition outside the paving |

### Pivot conventions

Chosen so the Mutagen generator needs no offset arithmetic:

- **floor and ramp tiles** - centre of the **top** face, so placing a reference at the platform floor Z puts the walking surface exactly there
- **retaining pieces** - top **outer** edge, so the piece hangs below the floor plane and surplus height buries in terrain
- **retaining corner** - top outer corner
- **shoulder wedge** - thick end, top face

For retaining, ramp and shoulder pieces, **+Y points away from the platform centre** (outward / downhill).

### Collision notes

- Every piece is an **unyielding rigidbody with mass 0** — static world geometry, not a loose prop. The BGS default is mass 80 with unyielding off, which would have made the platform a physics object.
- The four box-shaped pieces use their own mesh as the collider. BGS defaults that to a bounding box, which is exactly right for a box.
- The **ramp** cannot use a bounding box: that would be a solid 512 x 512 x 256 block and would stop the player walking up it. It instead uses a separate box child collider rotated 7.13 degrees to lie along the slope — the "Adding Collision using Child Collider Meshes" method from Bethesda's guide. Verified present in the exported FBX as `SkyrimFair_Ramp_512_Collider`.
- The **shoulder** deliberately has no collider; it sits on native ground and would only create a snag lip.

### Scale — the one real trap

**1 Blender unit converts to exactly 40 Skyrim units.** Every dimension in the build script is written in readable Skyrim units and divided by `BLENDER_UNITS_PER_SKYRIM_UNIT` (1/40) at mesh-construction time.

This was found by parsing the converted NIFs, not assumed. The first build came out **exactly 40x too large** on every piece, on both the visual mesh and the Havok collision. Blender's scene unit settings make no difference — building with `system="NONE"` and with Bethesda's own recommended `IMPERIAL` / `INCHES` / `scale_length=1` produced byte-identical oversized output — so the factor lives in the FBX to NIF conversion itself, not in Blender.

If a future piece comes out the wrong size, check this constant first.

### Verified in the converted NIFs

Confirmed by parsing `assets/nif/SkyrimFair/*.nif` directly:

- all six are valid SSE NIFs — `Gamebryo File Format, Version 20.2.0.7`, userVersion 12, bsVersion 100
- **visual mesh scale is exact**: every bounding sphere matches its expected radius, ratio 1.000
- **collision half-extents are exact**: 512x512x32, 1024x1024x32, 512x128x256, 128x128x256 as specified
- the five collision-bearing pieces carry `bhkCollisionObject`, `bhkRigidBodyT`, `bhkBoxShape` and `bhkConvexTransformShape`
- the **ramp's rotated collider survived conversion** — its transform reads cos 0.992 / sin 0.124, i.e. 7.13 degrees, and its box is 512 x 516 x 64, the 516 being the slope length `hypot(512, 64)`
- the **shoulder has no collision blocks at all**, as intended
- all six carry `BSLightingShaderProperty` and `BSShaderTextureSet`, so a texture slot is ready

### Still unverified
- **Collider `type` / `layer` / `material` enums.** These are populated by a UI callback and cannot be enumerated in headless Blender, so they remain at BGS defaults (`type='Box'`, `layer='1'`). Review them in the Blender UI before final use.
- **Material and texture.** No texture is assigned yet. The geometry proof comes first, per the brief. Cobblestone character still needs a material pass.
- **The ramp's child collider** is correct by construction but has not been seen in the Creation Kit or in game.
