using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

internal static class FairPluginGenerator
{
    private const int CellSize = 4096;

    /// <summary>Exterior cells are grouped 32x32 per block and 8x8 per sub-block.</summary>
    private const int ExteriorBlockSize = 32;

    private const int ExteriorSubBlockSize = 8;

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
            foundation = FairFoundation.Build(mod, config, terrain.Sample, PlaceAt);
            foreach (var record in foundation.Statics.Values)
            {
                mod.Statics.Add(record);
            }
        }

        // Nest each touched cell under its exterior block / sub-block.
        var blocks = new Dictionary<(int X, int Y), WorldspaceBlock>();
        var subBlocks = new Dictionary<(int X, int Y), WorldspaceSubBlock>();

        foreach (var ((cx, cy), placedObjects) in byCell.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
        {
            var cell = BuildExteriorCell(vanillaWorldspace, cx, cy);

            // Statics belong in the temporary child group, matching vanilla clutter.
            foreach (var placed in placedObjects)
            {
                cell.Temporary.Add(placed);
            }

            var blockKey = (FloorDivide(cx, ExteriorBlockSize), FloorDivide(cy, ExteriorBlockSize));
            var subKey = (FloorDivide(cx, ExteriorSubBlockSize), FloorDivide(cy, ExteriorSubBlockSize));

            if (!blocks.TryGetValue(blockKey, out var block))
            {
                block = new WorldspaceBlock
                {
                    BlockNumberX = (short)blockKey.Item1,
                    BlockNumberY = (short)blockKey.Item2,
                    GroupType = GroupTypeEnum.ExteriorCellBlock,
                };
                blocks[blockKey] = block;
                worldspace.SubCells.Add(block);
            }

            if (!subBlocks.TryGetValue(subKey, out var subBlock))
            {
                subBlock = new WorldspaceSubBlock
                {
                    BlockNumberX = (short)subKey.Item1,
                    BlockNumberY = (short)subKey.Item2,
                    GroupType = GroupTypeEnum.ExteriorCellSubBlock,
                };
                subBlocks[subKey] = subBlock;
                block.Items.Add(subBlock);
            }

            subBlock.Items.Add(cell);
        }

        mod.Worldspaces.Add(worldspace);

        var outputPath = Path.Combine(outputDirectory, mod.ModKey.FileName);

        mod.BeginWrite
            .ToPath(outputPath)
            .WithDefaultLoadOrder()
            .Write();

        return new FairBuildResult(
            outputPath,
            new FileInfo(outputPath).Length,
            testObject.FormKey,
            mapMarker.FormKey,
            master is not null,
            byCell.Keys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList(),
            foundation);
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

    private static Cell BuildExteriorCell(IWorldspaceGetter? vanillaWorldspace, int cx, int cy)
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

    /// <summary>Floor division, so negative cell coordinates land in the correct block.</summary>
    private static int FloorDivide(int value, int divisor)
        => (int)Math.Floor(value / (double)divisor);
}

internal sealed record FairBuildResult(
    string OutputPath,
    long SizeInBytes,
    FormKey TestObjectFormKey,
    FormKey MapMarkerFormKey,
    bool CopiedMasterRecords,
    IReadOnlyList<(int X, int Y)> CellsTouched,
    FoundationResult? Foundation);
