# Credits and third-party assets

Skyrim Fair is built with help from the Skyrim modding community. This file records every
third-party mod, asset, author, licence and redistribution condition the project uses or
has evaluated. It was last checked against the deployed mod, file by file, on 2026-09-25
(the method is at the end).

This document is intentionally conservative. No asset is copied into Skyrim Fair until its
original author and its redistribution permission are confirmed.

## At a glance

| What | Whose | How the fair uses it | Status |
| --- | --- | --- | --- |
| Palisade wall | adam127 (Sketchfab) | bundled, converted | CC BY 4.0: credit ships |
| Viking Palisade gate | Sereib (Sketchfab) | bundled, converted | CC BY 4.0: credit ships |
| Outhouse | Strifey7 (Sketchfab) | bundled, converted | CC BY 4.0: credit ships |
| Scaffold light towers | Js_TuruokaJunpei (Sketchfab) | bundled, converted | CC BY 4.0: credit ships |
| Stage music, crowd sounds, singers' voices | Barry, made with Suno (Pro) | bundled | Barry owns them (Suno Pro) |
| Paired folk dance | Barry, made with ChatGPT ("Astra") | bundled (re-based clips) | the project's own |
| Holidays | Nexus 1533 | a master; its records are placed | dependency, nothing copied |
| Open Animation Replacer | (Nexus) | swaps the folk-dance and tempo clips in | dependency, nothing copied |
| Professional Dancer | contentcat and davidgilbertking | its dances, when installed | optional, nothing copied |
| SPID exclusions | Maximum Destruction, Stealth Detection Fixes, Strange Runes | players edit one line of each mod's own file (`docs/COMPATIBILITY.md`) | nothing of theirs ships |
| Terrain Parallax 1.5 | Nexus 54860 | recommended replacer | not bundled |
| Props, crowd figures, tempo clips, cobbles, singers' faces | Bethesda | built from vanilla files | vanilla-derived, as mods routinely ship |
| Scripts, parallax map, terrace kit, stage collision | the project | its own work | ours |

Looked at, but **nothing used or shipped**: Stroti's Outdoor Toilet (used until 2026-09-25), Medieval Markets, Whiterun Stone Stairs,
Riverwood Has Charm and Walls, Whiterun Mossy Wet Stonefloor (used until 2026-09-25), Crowded
Streets, Diverse Archery Targets, Fireworks (183953), Incaendo's Banner Resource 2.

## Bundled third-party assets

### Palisade and Viking Palisade gate (Sketchfab, CC BY 4.0)

These two models form the wall and the main gate, inside the fair's worldspace and on its
exterior in Tamriel. They **are bundled** as `meshes\barry_palisades\` and
`textures\barry_palisades\`. Both are licensed CC BY 4.0, which allows redistribution and
modification with attribution.

| Model | Author | Source | Licence |
| --- | --- | --- | --- |
| Palisade | adam127 (https://sketchfab.com/adam127) | https://sketchfab.com/3d-models/palisade-c88f57eb0d734484b010c3f170e48d4a | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |
| Viking Palisade gate | Sereib (https://sketchfab.com/Sereib) | https://sketchfab.com/3d-models/viking-palisade-gate-9c87007db87c41c9b161dae827ced741 | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |

**Changes made:** Barry converted both for Skyrim SE / AE with Blender 3.6.23, the BGS
exporter and AssetWatcher. The package is `assets/Skyrim_Palisade_Assets/`, with its own
README and CREDITS.
- The palisade was uniformly enlarged to 3.5 m and turned to span its X axis. The gate keeps
  its original size.
- Both were re-origined to bottom centre, given simplified fixed box collision and
  re-materialised as opaque, double-sided matte wood. Their textures were re-encoded as DDS.
- The palisade's normal map has its green channel inverted. The gate shipped diffuse maps
  only, so it was given neutral normal maps.
- In the plugin, the palisade stands at scale 4 and the gate at 3.6. The exterior in Tamriel
  uses both at 60% of that.

Attribution:

> "Palisade" by adam127, licensed under CC BY 4.0. "Viking Palisade gate" by Sereib,
> licensed under CC BY 4.0. Converted and modified for Skyrim.

### Stroti's Outdoor Toilet (modder's resource): no longer used

**Replaced on 2026-09-25** by Strifey7's Outhouse (above). The plugin no longer refers to
it, so `meshes\Stroti\` and `textures\Stroti\` stay out of any package. Until then, the
outhouses were `meshes\Stroti\Outdoor Toilet\` (`OutdoorToilet.nif`, `ToiletDoor.nif`) and
`textures\Stroti\` (six custom DDS files).

| Role | Who | Source |
| --- | --- | --- |
| Original model (Oblivion) | Stroti | http://oblivion.nexusmods.com/mods/37634 |
| Skyrim conversion | Tamira | https://www.nexusmods.com/skyrimspecialedition/mods/4086 |
| SE / AE format conversion (stream 83 NiTriShape to stream 100 BSTriShape) | Barry, with ChatGPT ("Astra"), for Skyrim Fair | `external/Astra_Stroti_Outdoor_Toilet_Modern_SE.zip` |

The original readme's permissions: *"This is a modder's resource. You may use the meshes and
textures for your own mods as long as you give credit and you do not charge money for it. Do
not upload to other sites."* So:
- Skyrim Fair must stay free, and this credit must ship with it.
- **"Do not upload to other sites"** rules out re-uploading it inside the mod on Nexus,
  which is why it was replaced.
- The files are **not committed to this repository**, so git never uploads them anywhere.
  `.gitignore` excludes `assets/meshes/Stroti/` and `assets/textures/Stroti/`. To restore
  them, unzip the `meshes/` and `textures/` folders of Astra's zip into `assets/`.
- The resource also uses six vanilla textures (Whiterun floorboards, Solitude roof slate, a
  torn page). Skyrim supplies them, and they're not redistributed.


Attribution:

> Outdoor Toilet by Stroti, converted for Skyrim by Tamira, SE conversion by Astra.

### Scaffold (Sketchfab, CC BY 4.0)

`meshes\barry_scaffold\scaffold.nif`, with `textures\barry_scaffold\scaffold_d.dds` and
`scaffold_n.dds`, is the fair's light towers: four inside the fair, and four on the exterior.
Barry converted it for Skyrim.

| Model | Author | Source | Licence |
| --- | --- | --- | --- |
| scaffold | Js_TuruokaJunpei (https://sketchfab.com/Js_TuruokaJunpei) | https://sketchfab.com/3d-models/scaffold-90789439e18c4f3cbdb5c51b6c43b531 | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |

**Changes made** (Barry, with ChatGPT): "Modified for Skyrim SE/AE: converted from GLB to
NIF, scaled to approximately 8 metres tall, centred the origin at ground level, added solid
static collision, and adapted the materials and textures to Skyrim's shader and DDS formats."

Attribution:

> "scaffold" by Js_TuruokaJunpei, licensed under CC BY 4.0. Converted and modified for Skyrim.

### Outhouse (Sketchfab, CC BY 4.0)

The fair's six outhouses (two rows of three), **bundled** as `meshes\barry_outhouse\outhouse.nif`
and `textures\barry_outhouse\` (`outhouse_d.dds`, `outhouse_n.dds`, `outhouse_gloss_n.dds`).
It replaced Stroti's Outdoor Toilet on 2026-09-25, because Stroti's can't be re-uploaded.
Barry's package, with the Blender file, FBX, source textures and build scripts, is
`assets/Skyrim_Outhouse_Assets/`.

**Changes made** (Barry, with ChatGPT; the package's CREDITS): "scale and origin adjusted,
materials adapted for Skyrim, normal-map convention converted, DDS mipmaps generated, static
collision added". In detail, from its README:
- converted from GLB to an SE NIF, with all 1,118 triangles kept
- the origin centred at ground level
- one fixed box collider
- the PBR materials adapted to Skyrim's shader, the normal map's green channel inverted
- DDS with mipmaps

The door is part of the model, and doesn't open. The plugin places it at scale 2.2, about
192 units tall, like Skyrim's doors.

| Model | Author | Source | Licence |
| --- | --- | --- | --- |
| Outhouse | Strifey7 (https://sketchfab.com/Strifey7) | https://sketchfab.com/3d-models/outhouse-545355086dbb4b789a9cfe09d0ad5efe | CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/) |

Attribution:

> "Outhouse" by Strifey7, licensed under CC BY 4.0. Converted and modified for Skyrim.

## Barry's audio

**Files:**
- the stage songs: Round the Green; Hey-Ho, Skyrim; Fiddle; Dragonborn-Approved
  (`music/mono/`, and their stems in `music/stem/`)
- the crowd murmur and cheer (`sound-effects/mono/`)

Barry supplied them. `tools/build_audio.py` converts them to mono 44.1 kHz 16-bit WAV
under `Sound\SkyrimFair\`.

**The singers' voice files** (`Sound\Voice\SkyrimFair.esp\`, 147 `.fuz`) come from the same
songs' vocal stems. `tools/bards/build_vocals.py` splits them into lines. The lead's
lines carry the vocal, and the other two singers' lines carry silence with the same lip
track.

**Provenance: all made with Suno** (Barry, 2026-09-25): the songs, their stems and the
crowd sounds. Suno's terms (https://help.suno.com/en/articles/2746945) depend on the plan
they were made on:
- **Free (Basic) plan:** Suno owns them, and "you are allowed to use the songs for
  non-commercial purposes". A free mod is non-commercial. To be safe, opt the mod out of
  Nexus Donation Points and credit Suno.
- **Pro or Premier plan:** Barry owns them, and may use them commercially.

**Made on the Pro plan** (Barry, 2026-09-25), so Barry owns them, and the mod may ship
them, Donation Points included. Suno also notes that wholly AI-made music may not qualify
for copyright protection. The sources and the built files are kept out of git.

Attribution:

> Music and crowd sounds made with Suno.

## Built from Bethesda's files

All of these are Bethesda Game Studios' assets, rebuilt or copied for the fair. Anything
copied must come from **Bethesda's own archives** (the Steam install), not from a modlist:
Barry's modlist's `Skyrim - Textures*.bsa` are Vanilla Remastered - The New Normal's
(Nexus 153879).

- **Market goods (174 props):** `meshes\SkyrimFair\Props\*.nif` and `TowerLantern.nif`,
  from `tools/make_static_props.py`. Vanilla item meshes with their rigid bodies made
  fixed, so they stay put. The source meshes are Bethesda's (the modlist's stock folder,
  another official game version than Steam's: 8 bytes of Havok data differ). Every texture
  path is a vanilla one, and no texture is shipped.
- **Static crowd figures** (`meshes\SkyrimFair\Crowd\`, `docs/CROWD.md`; switched off in the
  plugin): vanilla body, clothes, hair, head, hand and foot meshes, posed on the vanilla
  skeletons with vanilla idle and furniture animations, holding Bethesda's props (the
  AnimObject tankard and the MQ201 goblet). Every texture path is a vanilla one.
- **The instrument tempo clips**
  (`...\OpenAnimationReplacer\SkyrimFairTempo\`, `tools/bards/build_tempo.py`): faster and
  held copies of vanilla's lute, drum and flute loops.
- **The avenue's cobbles:** `textures\SkyrimFair\Ground\Cobble01.dds` and `Cobble01_n.dds`
  are Bethesda's `textures\architecture\whiterun\wrstonefloor01.dds` and `_n.dds`,
  unchanged. They sit under the fair's own path, so they always match the fair's own
  parallax map (below). Barry chose that over pointing at vanilla's paths: a Whiterun
  replacer lays its stones out differently.
- **The stage singers' faces:** the three singers copy vanilla face-template NPCs' faces
  (`039CF6`, `039CFF`, `039D17`). Their FaceGen head meshes and tint textures are copied
  under the singers' FormIDs, all byte-identical to Bethesda's (rebuilt from the Steam
  install on 2026-09-25; the tints had come from Vanilla Remastered's archives).

The Elder Scrolls V: Skyrim Special Edition and its original game assets are the property of
Bethesda Game Studios / Bethesda Softworks. Skyrim Fair is an unofficial fan-made mod, not
affiliated with or endorsed by Bethesda.

## The project's own work

- **The plugin** (`SkyrimFair.esp`), made by the project's generator
  (`src/SkyrimFair.Generator/`).
- **The scripts** (`src/Papyrus/`). They compile against the vanilla Papyrus sources in the
  Creation Kit, which aren't redistributed. `SkyrimFairNpcGuard.pex` is retired (it called
  Papyrus Extender); leave it out of any package.
- **The cobbles' parallax map** (`textures\SkyrimFair\Ground\Cobble01_p.dds`), computed from
  the vanilla normal map by `tools/make_cobble.py`.
- **The terrace kit** (`meshes\SkyrimFair\SkyrimFair_*.nif`), made in Blender by
  `assets/blender/build_foundation_kit.py`, on vanilla texture paths. The plugin keeps its
  records, but no reference places them now.
- **The stage's invisible wall** (`meshes\SkyrimFair\Collision\StageWall.nif`), made by
  `tools/make_collision_wall.py`.
- An old procedural cobble (`textures\SkyrimFair\SkyrimFair_Cobble01*`, 2026-09-21) is
  project-made, but the plugin doesn't use it. Leave it out of any package.

### Paired folk dance (made with ChatGPT)

**Animation:** `folk_turn_A.hkx` and `folk_turn_B.hkx`, made for Skyrim Fair by Barry with
ChatGPT ("Astra", a ChatGPT model, not a person). OpenAI's terms assign the output to the
user, so there's no one else to ask.
They're authored in Blender 3.6 on the vanilla male and female skeletons (no bones added),
and encoded with PyNifly's standalone HKX codec (a build tool only).
- `tools/folk/rebase_folk.py` rewrites only the root track, so each dancer's clip starts at
  their own feet.
- The two clips replace the Cicero dance's animation for the fair's two folk dancers only,
  through Open Animation Replacer.
- The sources stay in Barry's untracked `character-actors/`, and the derived clips aren't
  committed.

## Dependencies (nothing of theirs is copied)

### Holidays (required master)

**Mod:** Holidays, Nexus Mods (Skyrim SE) 1533, version 2.20 Alpha 1.

Skyrim Fair uses Holidays as a **master**. The fair's festival rope lines are Holidays' own
records, placed by reference: its Whiterun, Saturalia, Riften, Windhelm, Imperial and
Stormcloak colourways of the vanilla Solitude festival line. So are its coloured hanging
lanterns, and its apple basket, mead crate, silver platter and sign stand. The lanterns also
hang from the palisade's pennant ropes and the lantern posts. **No Holidays file is copied or
redistributed.** Players need Holidays installed and loaded before SkyrimFair.esp.

Attribution:

> Festival decorations from Holidays (Nexus Mods, Skyrim Special Edition, mod 1533).

### Open Animation Replacer (required)

The fair ships OAR submod folders (a `config.json` and its clips) for the folk dance and the
instrument tempo. OAR itself, and SKSE, which it needs, are the player's to install.
Without OAR, the folk pair and the band fall back to vanilla animations.

### Professional Dancer (optional, recommended)

**Mod:** Professional Dancer, https://www.nexusmods.com/skyrimspecialedition/mods/124608  
**Created by:** contentcat and davidgilbertking  
**Referenced version:** 1.5.0  
**Licence stated by the authors:** CC BY-NC 4.0. The mod page credits the source dance
animations to creators whose animations were found on Mixamo.

**Detected at runtime** (since 2026-09-24). When `Dance.esp` is loaded, the stage script
plays five of its dances on the fair's dancers through its own animation events: `Dance1`,
`Dance3`, `Dance4`, `Dance8` and `Dance9`. Those exist once the player has run Pandora or
Nemesis with the mod installed. Without it, the vanilla dances play.
- **No file of Professional Dancer is copied, bundled or redistributed.** The fair only sends
  its event names.
- An earlier build that copied its clips into an OAR folder was removed for that reason.
- If any of its files are ever bundled, their use must follow CC BY-NC 4.0, and the
  underlying animations' provenance must be checked separately.

Attribution:

> Professional Dancer by contentcat and davidgilbertking.

## Compatibility patches (Maximum Destruction, Stealth Detection Fixes, Strange Runes)

These mods' SPID lines give every NPC a cloak, an ability or a script, which freezes the
game at the fair's crowd size. The fix adds `-SkyrimFairNPC` to those lines. **Players make
the edit themselves**, from the instructions on the mod page (`docs/COMPATIBILITY.md`), so
**nothing of these mods ships**. The generator's copies of their `_DISTR.ini` files
(`dist/spid/`, `spidPatches`) are for Barry's own game only, and are never packaged:
- Maximum Destruction: Nexus Mods, Skyrim SE (`MaximumDestruction_DISTR.ini`)
- Stealth Detection Fixes: Nexus Mods, Skyrim SE, mod 145336
  (`StealthKillDetectionFix_Attack_DISTR.ini`, `StealthKillDetectionFix_DISTR.ini`,
  `StealthKillDetectionFix_Killmove_DISTR.ini`)
- Strange Runes: Nexus Mods, Skyrim SE (`StrangeRunes_DISTR.ini`)

Asking the authors to add `-SkyrimFairNPC` upstream would save players the edit. The earlier ESP
condition patches (`SkyrimFair - Maximum Destruction Patch.esp`,
`SkyrimFair - Stealth Detection Fixes Patch.esp`) didn't work, and aren't shipped.

## Recommended texture replacer (not bundled)

| Mod | Nexus (SE) | What it does for the fair |
| --- | --- | --- |
| Terrain Parallax 1.5 – 4K2K | 54860 | parallax on the fair's grass, dirt and path ground: the fair's landscape texture sets name vanilla landscape paths with `_p` slots, which it fills |

Skyrim Fair ships none of its files. Installing it is optional and improves the look.

## Evaluated, not used

Nothing of these ships. Their permissions are kept here in case one is taken up later.

- **Medieval Markets** (JJerem, https://www.nexusmods.com/skyrimspecialedition/mods/161479,
  1.1 / 1.1.1). JJerem's own assets may be used with credit, in a mod that isn't sold. The
  mod also contains other authors' assets, under *their* permissions:
  - PraedythXVI (*Fruits and Veggies - modder's resource*)
  - Brumbek (*SMIM*)
  - gooball60 (*Palpable Baskets*)
  - JPSteel2 (*Northern Roads* tents)

  The 2026-09-22 audit (`docs/AUDIT.md`) found:
  - the stall pieces use vanilla textures only
  - the produce uses PraedythXVI's textures
  - the Nord tent is an unconverted LE mesh with COTN textures

  Before using any piece: identify the file, its real author and that author's
  permission, and record them here.
- **Whiterun Stone Stairs** (Chiselsky, https://www.nexusmods.com/skyrimspecialedition/mods/147164,
  1.2). Its assets may not be reused without Chiselsky's permission, except in
  compatibility patches with it. It credits Arthmoor for the USSEP meshes it builds on. The
  fair uses vanilla Whiterun paths, which it replaces if a player has it installed.
- **Riverwood Has Charm and Walls** (J3w3ls, https://www.nexusmods.com/skyrimspecialedition/mods/146520,
  1.4.1).
  - It may not be used or modified without J3w3ls's permission, uploaded elsewhere,
    converted, sold, or earn Donation Points.
  - J3w3ls plans a modder's resource.
  - It credits riton67000, LucidAPs and Vermunds (the bell).
  - Its file-level audit is in `docs/RIVERWOOD_WALLS_AUDIT.md`. It was a visual reference
    only; the fair's wall is the Sketchfab palisade.
- **Whiterun Mossy Wet Stonefloor – Grey 2k** (Nexus 99294). The avenue's cobbles were its
  `wrstonefloor01` textures until 2026-09-25. Now they're vanilla, with the fair's own
  parallax map, and installing it no longer changes the fair.
- **Crowded Streets** (Nexus 127723): its method was read, for the fair's crowd layers.
- **Diverse Archery Targets** (Nexus 98142): not used.
- **Fireworks** (Nexus 183953): read. The fair's fireworks are vanilla effects.
- **Incaendo's Banner Resource 2** (Nexus 94919): considered for banners. The fair's banners
  are vanilla's and Holidays'.
- **Vanilla Remastered - The New Normal** (Nexus 153879): Barry's modlist's texture
  archives. The singers' face tints were taken from it by mistake, and were rebuilt from
  Bethesda's archives on 2026-09-25.

## Build tools (none of them ship)

- **Mutagen** (GPL-3.0): the plugin generator reads and writes records with it.
- **PyNifly** (by Bad Dog, GPL-3.0; the licence is vendored as
  `character-actors/folk-dance/vendor/LICENSE-PyNifly`): writes the crowd NIFs and encodes
  the HKX clips (the folk dance, the tempo clips).
- **The Creation Kit's tools:** the Papyrus compiler, LipGenerator, LIPFuzer, xwmaencode.
- **Blender** (the terrace kit, the palisade conversion), **ffmpeg** (the audio), and
  **numpy** and **Pillow** (the cobbles' parallax map, the vocal lines).

## How this was checked (2026-09-25)

Every file in the deployed mod folder was traced to its source:
- **Meshes:** every texture path in every shipped mesh was looked up in Bethesda's archives
  (the Steam install). All are vanilla, apart from the credited assets above.
- **The plugin:** its texture paths are vanilla, apart from the cobbles. Its masters are
  Skyrim.esm and Holidays.esp only.
- **Byte comparisons:**
  - the props' 174 source meshes: stock folder against Steam
  - the singers' FaceGen files and the cobbles: against Bethesda's archives

  The stock folder has no loose files.

## Project asset ledger

| Skyrim Fair use | File(s) | Original project | Original author | Permission | Bundled or dependency |
| --- | --- | --- | --- | --- | --- |
| Palisade wall | `meshes\barry_palisades\palisade.nif`, `textures\barry_palisades\palisade\*.dds` | Palisade (Sketchfab) | adam127 | CC BY 4.0, with attribution | Bundled |
| Main gate | `meshes\barry_palisades\viking_palisade_gate_closed.nif` (placed), `viking_palisade_gate.nif` (open, not placed), `textures\barry_palisades\viking_palisade_gate\*.dds` | Viking Palisade gate (Sketchfab) | Sereib | CC BY 4.0, with attribution | Bundled |
| Outhouses (today) | `meshes\Stroti\Outdoor Toilet\*.nif`, `textures\Stroti\*.dds` | Stroti's Outdoor Toilet resource | Stroti; Tamira; Barry with ChatGPT | Credit, free, not re-uploaded | **Being replaced** by Strifey7's Outhouse |
| Outhouses (replacement) | to come | Outhouse (Sketchfab) | Strifey7 | CC BY 4.0, with attribution | To be bundled |
| Light towers | `meshes\barry_scaffold\scaffold.nif`, `textures\barry_scaffold\*.dds` | scaffold (Sketchfab) | Js_TuruokaJunpei | CC BY 4.0, with attribution | Bundled |
| Stage music, crowd sounds, voices | `Sound\SkyrimFair\`, `Sound\Voice\SkyrimFair.esp\` | made with Suno (Pro) | Barry | Barry owns them (Suno Pro plan) | Built mod only |
| Folk dance | `...\OpenAnimationReplacer\SkyrimFairFolk\` | the folk turn | Barry, made with ChatGPT | The project's own | Built mod only |
| Market goods (174 props) | `meshes\SkyrimFair\Props\*.nif`, `TowerLantern.nif` | Skyrim | Bethesda | Vanilla meshes, rigid bodies fixed | Built mod only |
| Crowd figures (off) | `meshes\SkyrimFair\Crowd\*.nif` | Skyrim | Bethesda | Vanilla meshes, posed | Built mod only |
| Instrument tempo clips | `...\OpenAnimationReplacer\SkyrimFairTempo\` | Skyrim | Bethesda | Vanilla loops, retimed | Built mod only |
| Avenue cobbles | `textures\SkyrimFair\Ground\Cobble01.dds`, `_n.dds` (vanilla), `_p.dds` (ours) | Skyrim; the project | Bethesda; the project | Vanilla copies; our own height map | Built mod only |
| Singers' faces | `...\FaceGenData\FaceGeom\SkyrimFair.esp\*.nif`, `...\FaceTint\SkyrimFair.esp\*.dds` | Skyrim | Bethesda | Vanilla copies (from Bethesda's archives since 2026-09-25) | Built mod only |
| Festival lines, lanterns, festive props | Holidays.esp records | Holidays | (Nexus 1533) | A master, nothing copied | Dependency |
| Folk dance and tempo swaps | OAR submods | Open Animation Replacer | (Nexus) | Nothing copied | Dependency |
| Stage dances | Dance.esp's events | Professional Dancer | contentcat and davidgilbertking | CC BY-NC 4.0; nothing copied | Optional dependency |
| SPID exclusions | instructions (`docs/COMPATIBILITY.md`) | Maximum Destruction; Stealth Detection Fixes; Strange Runes | their authors | Nothing of theirs ships | Players edit their own files |
