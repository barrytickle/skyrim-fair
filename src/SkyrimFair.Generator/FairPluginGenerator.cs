using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

internal static class FairPluginGenerator
{
    private const int CellSize = 4096;

    /// <summary>
    /// The Persistent record flag. Every one of the 347 vanilla map markers in Tamriel
    /// carries it, and without it the engine will not resolve the reference as a
    /// fast-travel destination: the marker draws on the map but travelling to it
    /// returns the player to where they already are. Mutagen models persistence only
    /// through Cell.Persistent membership and does not write this flag, so it is set
    /// explicitly here.
    /// </summary>
    private const int PersistentRecordFlag = 0x400;

    /// <summary>
    /// Copies a worldspace's own fields without dragging in its cells.
    /// LargeReferences (the RNAM lists) are deliberately dropped: the Creation Kit omits
    /// them on every WRLD override, and so does every mod in the audited load order.
    /// DynDOLOD regenerates and owns that data from a later slot, so carrying ~1.4MB of
    /// stale RNAM here would only clobber it.
    /// </summary>
    private static readonly Worldspace.TranslationMask WorldspaceHeaderOnly = new(defaultOn: true)
    {
        SubCells = false,
        TopCell = false,
        LargeReferences = false,
    };

    /// <summary>
    /// Initially Disabled. Vanilla rocks and shrubs standing inside the paved area are
    /// overridden with this flag rather than deleted: non-destructive, reversible, and
    /// it leaves the original records intact for any other mod that references them.
    /// </summary>
    private const int InitiallyDisabledFlag = 0x800;

    /// <summary>Copies a cell's own fields without dragging in its existing contents.</summary>
    private static readonly Cell.TranslationMask CellHeaderOnly = new(defaultOn: true)
    {
        Persistent = false,
        Temporary = false,
        Landscape = false,
        NavigationMeshes = false,
    };

    public static FairBuildResult Generate(FairConfig config)
    {
        config.Validate();

        var outputDirectory = Path.GetFullPath(config.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var modKey = ModKey.FromFileName(config.PluginName);
        var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);
        mod.ModHeader.Author = config.Identity.Author;

        var site = config.Site;
        var worldspaceKey = FormKeyHelper.Parse(site.Worldspace);
        var persistentCellKey = FormKeyHelper.Parse(site.PersistentCell);

        // Overriding a vanilla WRLD/CELL means replacing it wholesale: whatever we omit
        // is lost in game. So when Skyrim.esm is reachable we copy the real records and
        // only add our references. Without it we can still emit a structurally correct
        // plugin, but it would strip regions, water height and map data, so we say so.
        using var master = TryOpenMaster(config, worldspaceKey.ModKey);
        var vanillaWorldspace = master is null ? null : FindWorldspace(master, worldspaceKey);

        var worldspace = vanillaWorldspace is not null
            ? vanillaWorldspace.DeepCopy(WorldspaceHeaderOnly)
            : new Worldspace(worldspaceKey, SkyrimRelease.SkyrimSE);

        // ---- persistent cell: the map marker ------------------------------
        var mapMarker = BuildMapMarker(mod, site);
        var persistentCell = BuildPersistentCell(vanillaWorldspace, persistentCellKey, worldspaceKey);
        persistentCell.Persistent.Add(mapMarker);
        worldspace.TopCell = persistentCell;

        // ---- exterior cells: everything placed in the world ---------------
        var byCell = new Dictionary<(int X, int Y), List<PlacedObject>>();

        void PlaceAt(IPlacedObjectGetter placed, float x, float y)
        {
            var key = ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));
            if (!byCell.TryGetValue(key, out var list))
            {
                list = new List<PlacedObject>();
                byCell[key] = list;
            }

            list.Add((PlacedObject)placed);
        }

        // With the foundation in place the site centre is paved, so the stall stands
        // on the platform rather than being buried 48 units under it.
        var stallZ = site.Foundation.Enabled ? site.Foundation.FloorZ : site.Placement.Z;
        var testObject = BuildTestObject(mod, site, stallZ);
        PlaceAt(testObject, site.Placement.X, site.Placement.Y);

        FoundationResult? foundation = null;
        if (site.Foundation.Enabled)
        {
            var terrain = new TerrainSampler(vanillaWorldspace);
            var bounds = master is null
                ? new Dictionary<FormKey, PieceBounds>()
                : CollectSceneryBounds(master);
            foundation = FairFoundation.Build(
                mod, config, terrain.Sample,
                key => bounds.TryGetValue(key, out var b) ? b : default,
                PlaceAt);
            foreach (var record in foundation.Statics.Values)
            {
                mod.Statics.Add(record);
            }
        }

        // ---- sandbox: a private interior cell for looking at pieces ------
        // Shares the foundation's STAT records when they exist so the plugin carries
        // one definition of each kit piece, not one per place it is used.
        SandboxResult? sandbox = null;
        if (config.Sandbox.Enabled)
        {
            var kitStatics = foundation?.Statics ?? new Dictionary<string, Static>();
            sandbox = FairSandbox.Build(mod, config, role =>
            {
                if (kitStatics.TryGetValue(role, out var existing))
                {
                    return existing;
                }

                if (!site.Foundation.Pieces.TryGetValue(role, out var piece))
                {
                    throw new InvalidOperationException(
                        $"The sandbox floor needs foundation piece '{role}', which is not configured.");
                }

                var record = new Static(mod)
                {
                    EditorID = piece.EditorId,
                    Model = new Model { File = piece.Model },
                };
                kitStatics[role] = record;
                mod.Statics.Add(record);
                return record;
            });
        }

        // Clear vanilla clutter standing inside the paving, or rocks and shrubs poke
        // straight through the finished surface.
        if (site.Foundation.ClearClutter
            && foundation is not null && vanillaWorldspace is not null && foundation.PavedRects.Count > 0)
        {
            var clearable = CollectClearableBases(master!);
            var fnd = site.Foundation;

            // Search a ring of cells around the paving, not just the cells we place
            // into. A boulder's origin can sit a whole cell away and still cover the
            // paving with its mesh, so restricting the search to our own cells misses
            // exactly the biggest rocks.
            var minX = foundation.PavedRects.Min(r => r.MinX) - fnd.ClearSearchRadius;
            var maxX = foundation.PavedRects.Max(r => r.MaxX) + fnd.ClearSearchRadius;
            var minY = foundation.PavedRects.Min(r => r.MinY) - fnd.ClearSearchRadius;
            var maxY = foundation.PavedRects.Max(r => r.MaxY) + fnd.ClearSearchRadius;

            for (var cy = (int)MathF.Floor(minY / CellSize); cy <= (int)MathF.Floor(maxY / CellSize); cy++)
            {
                for (var cx = (int)MathF.Floor(minX / CellSize); cx <= (int)MathF.Floor(maxX / CellSize); cx++)
                {
                    var vanillaCell = FindVanillaCell(vanillaWorldspace, cx, cy);
                    if (vanillaCell is null)
                    {
                        continue;
                    }

                    foreach (var existing in vanillaCell.Temporary.OfType<IPlacedObjectGetter>())
                    {
                        var pos = existing.Placement?.Position;
                        if (pos is null ||
                            !clearable.TryGetValue(existing.Base.FormKey, out var radius))
                        {
                            continue;
                        }

                        // Landscape-scale meshes (cliffs, mountains) are left alone:
                        // disabling one would tear a hole in the surrounding world.
                        if (radius > fnd.ClearMaxRadius)
                        {
                            continue;
                        }

                        // Reach = the object's own mesh radius, scaled, plus a margin,
                        // so a big rock is cleared by how far it actually spreads.
                        var reach = radius * (existing.Scale ?? 1f) + fnd.ClearMargin;
                        var covered = foundation.PavedRects.Any(r =>
                        {
                            var dx = MathF.Max(0f, MathF.Max(r.MinX - pos.Value.X, pos.Value.X - r.MaxX));
                            var dy = MathF.Max(0f, MathF.Max(r.MinY - pos.Value.Y, pos.Value.Y - r.MaxY));
                            return MathF.Sqrt(dx * dx + dy * dy) <= reach;
                        });
                        if (!covered)
                        {
                            continue;
                        }

                        var disabled = existing.DeepCopy();
                        disabled.MajorRecordFlagsRaw |= InitiallyDisabledFlag;

                        var key = (cx, cy);
                        if (!byCell.TryGetValue(key, out var list))
                        {
                            list = new List<PlacedObject>();
                            byCell[key] = list;
                        }

                        list.Add(disabled);
                        foundation.DisabledCount++;
                    }
                }
            }
        }

        // Disable individually named references. Nothing is cleared in bulk: each
        // FormKey was reviewed in the footprint audit, and each is re-checked here
        // before being touched.
        if (foundation is not null && vanillaWorldspace is not null
            && site.Foundation.DisableReferences.Count > 0)
        {
            var scenery = CollectClearableBases(master!);
            var wanted = site.Foundation.DisableReferences
                .Select(FormKeyHelper.Parse).ToHashSet();
            var found = new HashSet<FormKey>();

            foreach (var (cx, cy) in EnumerateCellsAround(foundation, site.Foundation.ClearSearchRadius))
            {
                var vanillaCell = FindVanillaCell(vanillaWorldspace, cx, cy);
                if (vanillaCell is null)
                {
                    continue;
                }

                foreach (var existing in vanillaCell.Temporary.OfType<IPlacedObjectGetter>())
                {
                    if (!wanted.Contains(existing.FormKey))
                    {
                        continue;
                    }

                    found.Add(existing.FormKey);
                    var refusal = WhyUnsafeToDisable(existing, scenery);
                    if (refusal is not null)
                    {
                        foundation.Refused.Add($"{existing.FormKey}: {refusal}");
                        continue;
                    }

                    var disabled = existing.DeepCopy();
                    disabled.MajorRecordFlagsRaw |= InitiallyDisabledFlag;

                    var key = (cx, cy);
                    if (!byCell.TryGetValue(key, out var list))
                    {
                        list = new List<PlacedObject>();
                        byCell[key] = list;
                    }

                    list.Add(disabled);
                    foundation.DisabledCount++;
                }
            }

            foreach (var missing in wanted.Except(found))
            {
                foundation.Refused.Add($"{missing}: not found near the footprint");
            }
        }

        // Nest each touched cell under its exterior block / sub-block.
        var grid = new ExteriorCellGrid(worldspace);

        foreach (var ((cx, cy), placedObjects) in byCell.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
        {
            var cell = BuildExteriorCell(vanillaWorldspace, cx, cy);

            // Statics belong in the temporary child group, matching vanilla clutter.
            foreach (var placed in placedObjects)
            {
                cell.Temporary.Add(placed);
            }

            grid.Add(cell, cx, cy);
        }

        mod.Worldspaces.Add(worldspace);

        // ---- the isolated festival worldspace: a parallel prototype ------
        // Built last so it only ever appends FormIDs: every Tamriel and sandbox record
        // keeps the ID it had before this worldspace existed.
        FairWorldResult? fairWorld = config.FairWorld.Enabled
            ? FairWorld.Build(mod, config.FairWorld, master)
            : null;

        // ---- the compound's exterior in Tamriel: replaces the old terrace, built last ------
        if (config.Exterior.Enabled && master is not null && vanillaWorldspace is not null && fairWorld is not null)
        {
            var ext = FairExterior.Build(mod, config, master, vanillaWorldspace, worldspace, persistentCell, mapMarker);
            Console.WriteLine($"  exterior: {ext.Panels} wall panels, {ext.Banners} banners, {ext.Ropes} rope halves, {ext.Lanterns} lanterns; "
                + $"{ext.Silhouette} pieces of the fair inside; {ext.Disabled} vanilla references cleared ({ext.Sunk} large ones sunk, not disabled); {ext.Songs} songs outside, "
                + $"{ext.FireworkSites} firework sites; {ext.Path} path pieces, {ext.Flags} gate flag pieces, {ext.Signs} road sign pieces, {ext.Hidden} tower banners off the wall, {ext.Decor} entrance dressing; cells {string.Join(" ", ext.Cells.Select(c => $"({c.X},{c.Y})"))}; "
                + $"FormIDs 0x{ext.FormIds.From:X}-0x{ext.FormIds.To:X}");
        }

        // The fair's own paper lanterns in place of Holidays', in their own FormID range. Last of
        // all, after the exterior, so every placed lantern (the fair's and the exterior's in
        // Tamriel) changes base, and nothing built from them earlier sees a difference.
        if (config.FairWorld.Enabled && config.FairWorld.PaperLanterns.Enabled)
        {
            var pl = config.FairWorld.PaperLanterns;
            var saved = mod.ModHeader.Stats.NextFormID;
            mod.ModHeader.Stats.NextFormID = pl.FormIdBase;
            var swaps = FairPaperLanterns.Build(mod, pl, mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedObject>()));
            Console.WriteLine($"  paper lanterns: {FairPaperLanterns.Apply(swaps)} placed, {swaps.Select(s => s.Base).Distinct().Count()} kinds; "
                + $"FormIDs 0x{pl.FormIdBase:X}-0x{mod.ModHeader.Stats.NextFormID - 1:X}");

            // The rest of Holidays (the bunting and the props), appended in the same range.
            if (config.FairWorld.HolidaysFree.Enabled && master is not null)
            {
                var from = mod.ModHeader.Stats.NextFormID;
                var (bunting, props, added) = FairHolidaysFree.Build(mod, config.FairWorld.HolidaysFree, master);
                Console.WriteLine($"  holidays-free: {bunting} bunting lines, {props} props ({added} pieces added); FormIDs 0x{from:X}-0x{mod.ModHeader.Stats.NextFormID - 1:X}");
            }

            // The grass on the fair's ground: its own sparse, short copies, appended in the same range.
            var grass = config.FairWorld.Ground.GrassOverride;
            if (grass.Enabled && master is not null)
            {
                var from = mod.ModHeader.Stats.NextFormID;
                var role = char.ToUpperInvariant(grass.Role[0]) + grass.Role[1..];
                var ltex = mod.LandscapeTextures.First(l => l.EditorID == $"{config.FairWorld.Ground.EditorIdPrefix}{role}");
                ltex.Grasses.Clear();
                foreach (var g in grass.Grasses)
                {
                    var source = master.Grasses.First(x => x.FormKey == FormKeyHelper.Parse(g.From));
                    var copy = source.Duplicate(mod.GetNextFormKey());
                    copy.EditorID = $"{grass.EditorIdPrefix}{source.EditorID}";
                    copy.Density = g.Density;
                    mod.Grasses.Add(copy);
                    ltex.Grasses.Add(new FormLink<IGrassGetter>(copy.FormKey));
                }

                Console.WriteLine($"  ground grass: {ltex.EditorID} now grows {string.Join(", ", grass.Grasses.Select(g => $"{g.From} at {g.Density}"))}; FormIDs 0x{from:X}-0x{mod.ModHeader.Stats.NextFormID - 1:X}");
            }

            mod.ModHeader.Stats.NextFormID = saved;
        }

        // Bar stools become chairs facing their tables, last of all (their FormIDs stay).
        var seatSwap = config.FairWorld.SeatSwap;
        if (config.FairWorld.Enabled && seatSwap.Enabled && master is not null)
        {
            var from = FormKeyHelper.Parse(seatSwap.From);
            var to = FormKeyHelper.Parse(seatSwap.To);
            var tableBases = master.Statics.Where(s => (s.EditorID ?? "").Contains("Table", StringComparison.OrdinalIgnoreCase)).Select(s => s.FormKey)
                .Concat(mod.Statics.Where(s => (s.EditorID ?? "").Contains("Table", StringComparison.OrdinalIgnoreCase)).Select(s => s.FormKey))
                .ToHashSet();
            var placed = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedObject>()).Where(o => o.Placement is not null).ToList();
            var tables = placed.Where(o => tableBases.Contains(o.Base.FormKey)).ToList();
            var sitterYaw = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedNpc>())
                .Where(n => n.Placement is not null && n.LinkedReferences.Any(l => l.KeywordOrReference.IsNull))
                .GroupBy(n => n.LinkedReferences.First(l => l.KeywordOrReference.IsNull).Reference.FormKey)
                .ToDictionary(g => g.Key, g => g.First().Placement!.Rotation.Z);
            var (toTable, toSitter, kept) = (0, 0, 0);
            foreach (var seat in placed.Where(o => o.Base.FormKey == from).OrderBy(o => o.FormKey.ID))
            {
                var p = seat.Placement!.Position;
                var table = tables
                    .Select(t => (Ref: t, D: MathF.Sqrt((t.Placement!.Position.X - p.X) * (t.Placement.Position.X - p.X) + (t.Placement.Position.Y - p.Y) * (t.Placement.Position.Y - p.Y))))
                    .Where(t => t.D > 1f && t.D <= seatSwap.TableReach)
                    .OrderBy(t => t.D)
                    .FirstOrDefault();
                var r = seat.Placement.Rotation;
                float yaw;
                if (table.Ref is not null)
                {
                    // Its front (local +Y) toward the table: forward is (sin yaw, cos yaw).
                    yaw = MathF.Atan2(table.Ref.Placement!.Position.X - p.X, table.Ref.Placement.Position.Y - p.Y);
                    toTable++;
                }
                else if (sitterYaw.TryGetValue(seat.FormKey, out var sy))
                {
                    yaw = sy;
                    toSitter++;
                }
                else
                {
                    yaw = r.Z;
                    kept++;
                }

                seat.Base = new FormLinkNullable<IPlaceableObjectGetter>(to);
                seat.Placement.Rotation = new P3Float(0f, 0f, yaw);
            }

            Console.WriteLine($"  seats: {toTable + toSitter + kept} bar stools now chairs ({toTable} facing a table, {toSitter} their sitter's way, {kept} as they were)");
        }

        // Empty array properties can't be initialised from a plugin ("cannot be initialized because
        // the value is the incorrect type" in the log); left out, the script sees them empty anyway.
        foreach (var quest in mod.Quests.Where(q => q.VirtualMachineAdapter is not null))
        {
            foreach (var script in quest.VirtualMachineAdapter!.Scripts)
            {
                script.Properties.RemoveAll(p =>
                    (p is ScriptObjectListProperty o && o.Objects.Count == 0) || (p is ScriptFloatListProperty f && f.Data.Count == 0)
                    || (p is ScriptIntListProperty n && n.Data.Count == 0) || (p is ScriptStringListProperty s && s.Data.Count == 0));
            }
        }

        var outputPath = Path.Combine(outputDirectory, mod.ModKey.FileName);

        // Masters are sorted against this order; only the ones the plugin links to are written.
        // Holidays.esp (Nexus 1533) was a master until the fair's own lanterns, bunting and props
        // replaced its pieces (FairPaperLanterns, FairHolidaysFree): now only Skyrim.esm is.
        mod.BeginWrite
            .ToPath(outputPath)
            .WithLoadOrder(
                ModKey.FromFileName("Skyrim.esm"), ModKey.FromFileName("Update.esm"),
                ModKey.FromFileName("Dawnguard.esm"), ModKey.FromFileName("HearthFires.esm"),
                ModKey.FromFileName("Dragonborn.esm"), ModKey.FromFileName("Holidays.esp"))
            .WithNoDataFolder()
            .Write();

        // Seq\<plugin>.seq: the start-game-enabled quests that carry dialogue (as xEdit writes it,
        // each FormID as the file stores it, its own index after the masters). Without it their
        // dialogue can stay dead (the cameos couldn't be talked to, 2026-09-25).
        var dialogueQuests = mod.DialogTopics.Select(t => t.Quest.FormKey).ToHashSet();
        var seqQuests = mod.Quests
            .Where(q => q.Flags.HasFlag(Quest.Flag.StartGameEnabled) && dialogueQuests.Contains(q.FormKey))
            .OrderBy(q => q.FormKey.ID)
            .ToList();
        // The masters are known once the plugin is written: its own index follows them.
        var masterCount = (uint)SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE).ModHeader.MasterReferences.Count;
        var seqDir = Path.Combine(outputDirectory, "Seq");
        Directory.CreateDirectory(seqDir);
        var seqPath = Path.Combine(seqDir, Path.ChangeExtension(mod.ModKey.FileName.String, ".seq"));
        using (var seq = new BinaryWriter(File.Create(seqPath)))
        {
            foreach (var q in seqQuests)
            {
                seq.Write((masterCount << 24) | q.FormKey.ID);
            }
        }

        Console.WriteLine($"  seq: {seqQuests.Count} start-game quests with dialogue ({string.Join(", ", seqQuests.Select(q => q.EditorID))})");


        foreach (var (file, lines) in FairSpidPatches.Build(config, outputDirectory))
        {
            Console.WriteLine($"  SPID patch {file}: {lines} line(s) exclude -{config.FairWorld.NpcKeyword}");
        }

        return new FairBuildResult(
            outputPath,
            new FileInfo(outputPath).Length,
            testObject.FormKey,
            mapMarker.FormKey,
            master is not null,
            byCell.Keys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList(),
            foundation,
            sandbox,
            fairWorld);
    }

    private static IEnumerable<(int X, int Y)> EnumerateCellsAround(
        FoundationResult foundation, float pad)
    {
        var minX = foundation.PavedRects.Min(r => r.MinX) - pad;
        var maxX = foundation.PavedRects.Max(r => r.MaxX) + pad;
        var minY = foundation.PavedRects.Min(r => r.MinY) - pad;
        var maxY = foundation.PavedRects.Max(r => r.MaxY) + pad;
        for (var cy = (int)MathF.Floor(minY / CellSize); cy <= (int)MathF.Floor(maxY / CellSize); cy++)
        {
            for (var cx = (int)MathF.Floor(minX / CellSize); cx <= (int)MathF.Floor(maxX / CellSize); cx++)
            {
                yield return (cx, cy);
            }
        }
    }

    /// <summary>
    /// Refuses anything that is not plain scenery. Returns null when safe to disable.
    /// </summary>
    internal static string? WhyUnsafeToDisable(
        IPlacedObjectGetter placed, Dictionary<FormKey, float> scenery)
    {
        if (!scenery.ContainsKey(placed.Base.FormKey))
        {
            return "base object is not a static, tree or flora";
        }

        if ((placed.MajorRecordFlagsRaw & PersistentRecordFlag) != 0)
        {
            return "reference is persistent";
        }

        if (placed.VirtualMachineAdapter is not null) return "reference has a script";
        if (placed.EnableParent is not null) return "reference is enable-parented";
        if (placed.LinkedReferences.Count > 0) return "reference has linked references";
        if (placed.Owner.FormKey != FormKey.Null) return "reference is owned";
        if (placed.LocationRefTypes is { Count: > 0 }) return "reference has location ref types";
        if (placed.TeleportDestination is not null) return "reference is a teleport door";
        if (placed.EncounterZone.FormKey != FormKey.Null) return "reference has an encounter zone";
        return null;
    }

    /// <summary>
    /// Base objects safe to disable, mapped to the radius of their mesh footprint.
    ///
    /// The radius matters enormously. RockTundraLand02Tundra01 has an object bounds
    /// radius of 1767 units, so a boulder whose origin sits 500 units clear of the
    /// paving still blankets it. Testing reference origins alone leaves exactly those
    /// rocks poking through the finished surface.
    ///
    /// Scenery only. Activators, containers, doors, furniture and anything an NPC or
    /// quest might reference are deliberately excluded.
    /// </summary>
    internal static Dictionary<FormKey, float> CollectClearableBases(ISkyrimModGetter master)
        => CollectSceneryBounds(master).ToDictionary(p => p.Key, p => p.Value.Radius);

    /// <summary>
    /// Scenery bounds, keyed by base FormKey: mesh radius plus the vertical extent.
    ///
    /// The vertical extent is what lets a rock be placed to a target top height. A
    /// rock's origin sits near its base, not its centre - RockL01 runs from -59 to
    /// +229 - so hiding a 240-unit retaining face means solving for the reference Z
    /// from the piece's own ZMax, not guessing an offset.
    /// </summary>
    private static Dictionary<FormKey, PieceBounds> CollectSceneryBounds(ISkyrimModGetter master)
    {
        var bounds = new Dictionary<FormKey, PieceBounds>();

        void Add(FormKey key, IObjectBoundsGetter? b)
        {
            if (b is null)
            {
                bounds[key] = new PieceBounds(0f, 0f, 0f);
                return;
            }

            var hx = (b.Second.X - b.First.X) / 2f;
            var hy = (b.Second.Y - b.First.Y) / 2f;
            bounds[key] = new PieceBounds(MathF.Sqrt(hx * hx + hy * hy), b.First.Z, b.Second.Z);
        }

        foreach (var record in master.Statics) Add(record.FormKey, record.ObjectBounds);
        foreach (var record in master.Trees) Add(record.FormKey, record.ObjectBounds);
        foreach (var record in master.Florae) Add(record.FormKey, record.ObjectBounds);
        return bounds;
    }

    internal static ICellGetter? FindVanillaCell(IWorldspaceGetter worldspace, int cx, int cy)
    {
        foreach (var block in worldspace.SubCells)
        {
            foreach (var subBlock in block.Items)
            {
                foreach (var cell in subBlock.Items)
                {
                    var grid = cell.Grid?.Point;
                    if (grid is not null && grid.Value.X == cx && grid.Value.Y == cy)
                    {
                        return cell;
                    }
                }
            }
        }

        return null;
    }

    private static Cell BuildPersistentCell(
        IWorldspaceGetter? vanillaWorldspace, FormKey persistentCellKey, FormKey worldspaceKey)
    {
        if (vanillaWorldspace is null)
        {
            return new Cell(persistentCellKey, SkyrimRelease.SkyrimSE);
        }

        var vanilla = vanillaWorldspace.TopCell
            ?? throw new InvalidOperationException(
                $"{worldspaceKey} has no persistent cell in the master.");

        if (vanilla.FormKey != persistentCellKey)
        {
            throw new InvalidOperationException(
                $"Configured persistent cell {persistentCellKey} does not match " +
                $"{worldspaceKey}'s actual persistent cell {vanilla.FormKey}.");
        }

        return vanilla.DeepCopy(CellHeaderOnly);
    }

    internal static Cell BuildExteriorCell(IWorldspaceGetter? vanillaWorldspace, int cx, int cy)
    {
        if (vanillaWorldspace is not null)
        {
            foreach (var block in vanillaWorldspace.SubCells)
            {
                foreach (var subBlock in block.Items)
                {
                    foreach (var candidate in subBlock.Items)
                    {
                        var grid = candidate.Grid?.Point;
                        if (grid is not null && grid.Value.X == cx && grid.Value.Y == cy)
                        {
                            return candidate.DeepCopy(CellHeaderOnly);
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Cell at grid {cx}, {cy} was not found in the master.");
        }

        // Stub fallback: structurally valid, but not safe to load. Program.cs warns.
        return new Cell(FormKey.Null, SkyrimRelease.SkyrimSE)
        {
            Grid = new CellGrid { Point = new P2Int(cx, cy) },
        };
    }

    private static ISkyrimModDisposableGetter? TryOpenMaster(FairConfig config, ModKey masterKey)
    {
        var dataPath = Environment.GetEnvironmentVariable("SKYRIM_DATA_PATH");
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            dataPath = config.Site.SkyrimDataPath;
        }

        if (string.IsNullOrWhiteSpace(dataPath))
        {
            return null;
        }

        var masterPath = Path.Combine(dataPath, masterKey.FileName);
        return !File.Exists(masterPath)
            ? null
            : SkyrimMod.CreateFromBinaryOverlay(masterPath, SkyrimRelease.SkyrimSE);
    }

    private static IWorldspaceGetter FindWorldspace(ISkyrimModGetter master, FormKey key)
    {
        foreach (var worldspace in master.Worldspaces)
        {
            if (worldspace.FormKey == key)
            {
                return worldspace;
            }
        }

        throw new InvalidOperationException(
            $"Worldspace {key} was not found in {master.ModKey.FileName}.");
    }

    private static PlacedObject BuildTestObject(SkyrimMod mod, PrototypeSite site, float z)
    {
        return new PlacedObject(mod)
        {
            EditorID = "FairTestMarketStall",
            Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(site.TestObject.FormKey)),
            Placement = new Placement
            {
                Position = new P3Float(site.Placement.X, site.Placement.Y, z),
                Rotation = new P3Float(0f, 0f, 0f),
            },
        };
    }

    private static PlacedObject BuildMapMarker(SkyrimMod mod, PrototypeSite site)
    {
        var marker = site.MapMarker;

        var flags = default(MapMarker.Flag);
        if (marker.Visible)
        {
            flags |= MapMarker.Flag.Visible;
        }

        if (marker.CanTravelTo)
        {
            flags |= MapMarker.Flag.CanTravelTo;
        }

        if (!Enum.TryParse<MapMarker.MarkerType>(marker.Type, ignoreCase: true, out var markerType))
        {
            throw new InvalidOperationException(
                $"'{marker.Type}' is not a known map marker type. " +
                $"Expected one of: {string.Join(", ", Enum.GetNames<MapMarker.MarkerType>())}.");
        }

        var placed = new PlacedObject(mod)
        {
            EditorID = "FairSiteMapMarker",
            MajorRecordFlagsRaw = PersistentRecordFlag,
            Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(marker.BaseObject)),
            MapMarker = new MapMarker
            {
                Name = marker.Name,
                Type = markerType,
                Flags = flags,
            },
            Radius = marker.Radius,
            Placement = new Placement
            {
                Position = new P3Float(
                    marker.Position?.X ?? site.Placement.X,
                    marker.Position?.Y ?? site.Placement.Y,
                    marker.Position?.Z ?? site.Placement.Z),
                Rotation = new P3Float(0f, 0f, 0f),
            },
        };

        placed.LocationRefTypes = new ExtendedList<IFormLinkGetter<ILocationReferenceTypeGetter>>
        {
            new FormLink<ILocationReferenceTypeGetter>(FormKeyHelper.Parse(marker.LocationRefType)),
        };

        return placed;
    }
}

internal sealed record FairBuildResult(
    string OutputPath,
    long SizeInBytes,
    FormKey TestObjectFormKey,
    FormKey MapMarkerFormKey,
    bool CopiedMasterRecords,
    IReadOnlyList<(int X, int Y)> CellsTouched,
    FoundationResult? Foundation,
    SandboxResult? Sandbox,
    FairWorldResult? FairWorld);
