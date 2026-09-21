# Skyrim Fair project-owned assets

Everything in this folder is authored for Skyrim Fair. No third-party mesh or texture data is copied or referenced here.

## Layout

```text
assets/
  blender/
    build_foundation_kit.py      source of truth - generates the whole kit
    fair_foundation_kit.blend    build output, committed for convenience
  fbx/
    SkyrimFair_*.fbx             BSFBX export, input to AssetWatcher
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

**AssetWatcher is a Qt GUI application with no command-line interface**, and Bethesda's guide says to run it as administrator. This step cannot be automated from here, so it is Barry's to run:

1. launch `...\Skyrim Special Edition\Tools\AssetWatcher\AssetWatcher.exe` as administrator
2. point it at `assets\fbx\` as the watched folder
3. let it convert the six `.fbx` files to `.nif`
4. place the resulting NIFs under a `meshes\SkyrimFair\` path so the plugin can reference them

Until that runs there are no NIFs, so no `STAT` records can usefully be generated yet.

## The kit

All dimensions in Skyrim units. Built 1:1 in Blender units and exported with no unit scaling, so **scale must be confirmed on first Creation Kit import**.

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

### Unverified

- **Scale.** Built 1:1 in Blender units with no unit scaling on export. Confirm on first CK import.
- **Collider `type` / `layer` / `material` enums.** These are populated by a UI callback and cannot be enumerated in headless Blender, so they remain at BGS defaults (`type='Box'`, `layer='1'`). Review them in the Blender UI before final use.
- **Material and texture.** No texture is assigned yet. The geometry proof comes first, per the brief. Cobblestone character still needs a material pass.
- **The ramp's child collider** is correct by construction but has not been seen in the Creation Kit or in game.
