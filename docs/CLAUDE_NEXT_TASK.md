# Claude next task: prepare the first MO2 test build

Work from the latest `feat/bootstrap-generator` branch.

Read `docs/AUDIT.md` first. Treat it as the current source of truth for Barry's local setup.

## Goal

Prepare the first generated Skyrim Fair plugin for an in-game test through Mod Organizer 2.

The generated ESP has already been structurally verified. This task is only to place it into a dedicated MO2 mod folder so Barry can enable it manually.

## Small easter egg

Before rebuilding, add one harmless metadata easter egg to the generated plugin:

- TES4 Author / CNAM: `BarryRim Event Planner`

Keep it subtle. Do not add gameplay content, records, scripts, messages, quests, or visible in-game jokes for this easter egg. Verify after generation that the author metadata is present in the written ESP.

## Safety boundary

You may create files and folders inside the MO2 `mods` directory for this dedicated project mod.

Do **not**:

- enable the mod in MO2
- enable the plugin in the right-hand plugin pane
- reorder the load order
- edit Barry's active profile
- launch Skyrim
- launch SKSE
- modify `stock\Data`
- modify any existing mod
- modify Pandora output
- run DynDOLOD/Occlusion
- touch Barry's saves

Barry will perform the enable-and-launch step manually.

## Known paths

From the latest audit:

- MO2 instance: `E:\Modlists\Still In Skyrim`
- MO2 mods directory: `E:\Modlists\Still In Skyrim\mods`
- project output: `dist\SkyrimFair.esp`

## Required work

1. Add the plugin author metadata described above, preserving the existing generator structure.

2. Rebuild the project from the repository root:

```powershell
dotnet restore SkyrimFair.sln
dotnet build SkyrimFair.sln -c Release
dotnet run --project src/SkyrimFair.Generator -- fair.config.json
```

3. Verify `dist\SkyrimFair.esp` exists and matches the expected first-visible-build structure from `docs/AUDIT.md`, including `BarryRim Event Planner` as the TES4 author metadata.

4. Create this dedicated MO2 mod folder if it does not already exist:

```text
E:\Modlists\Still In Skyrim\mods\Skyrim Fair\
```

5. Copy the generated plugin to:

```text
E:\Modlists\Still In Skyrim\mods\Skyrim Fair\SkyrimFair.esp
```

6. Verify the copied file exists and is byte-identical to `dist\SkyrimFair.esp`.

7. Do not add any other files unless MO2 itself requires metadata to recognise the folder. If metadata is created, document exactly what was added and why.

## Expected state when finished

The filesystem should contain:

```text
E:\Modlists\Still In Skyrim\mods\Skyrim Fair\
└── SkyrimFair.esp
```

The mod may appear in MO2's left pane after refresh/restart, but it must remain disabled.

The plugin must not be enabled in Barry's active load order.

## Git / audit workflow

After preparing the folder:

1. overwrite `docs/AUDIT.md` with the latest verified state,
2. record:
   - build result
   - confirmation that TES4 Author / CNAM is `BarryRim Event Planner`
   - generated ESP size/hash
   - deployed ESP size/hash
   - exact MO2 destination path
   - whether the mod folder already existed or was created
   - confirmation that the copied file is byte-identical
   - confirmation that the mod/plugin were **not enabled**
3. commit and push the generator change plus refreshed audit to `feat/bootstrap-generator`,
4. do not commit the generated ESP or any MO2 files to Git,
5. tell Barry only that the MO2 test build is prepared and ready for him to enable manually, plus any blocker if something went wrong.

Suggested commit message:

```text
feat: add BarryRim plugin easter egg
```

The Git history is the audit history. `docs/AUDIT.md` should describe only the latest known state.
