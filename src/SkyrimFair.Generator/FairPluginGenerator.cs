using System.Globalization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

internal static class FairPluginGenerator
{
    /// <summary>Exterior cells are grouped 32x32 per block and 8x8 per sub-block.</summary>
    private const int ExteriorBlockSize = 32;

    private const int ExteriorSubBlockSize = 8;

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
        var site = config.Site;

        var worldspaceKey = ParseFormKey(site.Worldspace);
        var persistentCellKey = ParseFormKey(site.PersistentCell);
        var cellKey = ParseFormKey(site.Cell);

        // Overriding a vanilla WRLD/CELL means replacing it wholesale: whatever we omit
        // is lost in game. So when Skyrim.esm is reachable we copy the real records and
        // only add our references. Without it we can still emit a structurally correct
        // plugin, but it would strip regions, water height and map data, so we say so.
        using var master = TryOpenMaster(config, worldspaceKey.ModKey);

        Worldspace worldspace;
        Cell persistentCell;
        Cell cell;

        if (master is not null)
        {
            var vanillaWorldspace = FindWorldspace(master, worldspaceKey);
            worldspace = vanillaWorldspace.DeepCopy(WorldspaceHeaderOnly);

            var vanillaPersistentCell = vanillaWorldspace.TopCell
                ?? throw new InvalidOperationException(
                    $"{worldspaceKey} has no persistent cell in {master.ModKey.FileName}.");

            if (vanillaPersistentCell.FormKey != persistentCellKey)
            {
                throw new InvalidOperationException(
                    $"Configured persistent cell {persistentCellKey} does not match " +
                    $"{worldspaceKey}'s actual persistent cell {vanillaPersistentCell.FormKey}.");
            }

            persistentCell = vanillaPersistentCell.DeepCopy(CellHeaderOnly);
            cell = FindExteriorCell(vanillaWorldspace, cellKey, site).DeepCopy(CellHeaderOnly);
        }
        else
        {
            worldspace = new Worldspace(worldspaceKey, SkyrimRelease.SkyrimSE);
            persistentCell = new Cell(persistentCellKey, SkyrimRelease.SkyrimSE);
            cell = new Cell(cellKey, SkyrimRelease.SkyrimSE)
            {
                Grid = new CellGrid
                {
                    Point = new P2Int(site.CellGridX, site.CellGridY),
                },
            };
        }

        var mapMarker = BuildMapMarker(mod, site);
        persistentCell.Persistent.Add(mapMarker);
        worldspace.TopCell = persistentCell;

        // Statics belong in the temporary child group, matching vanilla exterior clutter.
        var testObject = BuildTestObject(mod, site);
        cell.Temporary.Add(testObject);

        worldspace.SubCells.Add(BuildBlockChain(site.CellGridX, site.CellGridY, cell));
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
            master is not null);
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
        if (!File.Exists(masterPath))
        {
            return null;
        }

        return SkyrimMod.CreateFromBinaryOverlay(masterPath, SkyrimRelease.SkyrimSE);
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

    private static ICellGetter FindExteriorCell(
        IWorldspaceGetter worldspace,
        FormKey key,
        PrototypeSite site)
    {
        var blockX = (short)FloorDivide(site.CellGridX, ExteriorBlockSize);
        var blockY = (short)FloorDivide(site.CellGridY, ExteriorBlockSize);
        var subBlockX = (short)FloorDivide(site.CellGridX, ExteriorSubBlockSize);
        var subBlockY = (short)FloorDivide(site.CellGridY, ExteriorSubBlockSize);

        foreach (var block in worldspace.SubCells)
        {
            if (block.BlockNumberX != blockX || block.BlockNumberY != blockY)
            {
                continue;
            }

            foreach (var subBlock in block.Items)
            {
                if (subBlock.BlockNumberX != subBlockX || subBlock.BlockNumberY != subBlockY)
                {
                    continue;
                }

                foreach (var candidate in subBlock.Items)
                {
                    if (candidate.FormKey == key)
                    {
                        return candidate;
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"Cell {key} was not found at grid {site.CellGridX}, {site.CellGridY} " +
            $"(block {blockX}, {blockY} / sub-block {subBlockX}, {subBlockY}).");
    }

    private static PlacedObject BuildTestObject(SkyrimMod mod, PrototypeSite site)
    {
        return new PlacedObject(mod)
        {
            EditorID = "FairTestMarketStall",
            Base = new FormLinkNullable<IPlaceableObjectGetter>(ParseFormKey(site.TestObject.FormKey)),
            Placement = new Placement
            {
                Position = new P3Float(site.Placement.X, site.Placement.Y, site.Placement.Z),
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

        return new PlacedObject(mod)
        {
            EditorID = "FairSiteMapMarker",
            Base = new FormLinkNullable<IPlaceableObjectGetter>(ParseFormKey(marker.BaseObject)),
            MapMarker = new MapMarker
            {
                Name = marker.Name,
                Type = markerType,
                Flags = flags,
            },
            Placement = new Placement
            {
                Position = new P3Float(site.Placement.X, site.Placement.Y, site.Placement.Z),
                Rotation = new P3Float(0f, 0f, 0f),
            },
        };
    }

    private static WorldspaceBlock BuildBlockChain(int cellX, int cellY, Cell cell)
    {
        var subBlock = new WorldspaceSubBlock
        {
            BlockNumberX = (short)FloorDivide(cellX, ExteriorSubBlockSize),
            BlockNumberY = (short)FloorDivide(cellY, ExteriorSubBlockSize),
            GroupType = GroupTypeEnum.ExteriorCellSubBlock,
        };
        subBlock.Items.Add(cell);

        var block = new WorldspaceBlock
        {
            BlockNumberX = (short)FloorDivide(cellX, ExteriorBlockSize),
            BlockNumberY = (short)FloorDivide(cellY, ExteriorBlockSize),
            GroupType = GroupTypeEnum.ExteriorCellBlock,
        };
        block.Items.Add(subBlock);

        return block;
    }

    /// <summary>Floor division, so negative cell coordinates land in the correct block.</summary>
    private static int FloorDivide(int value, int divisor)
        => (int)Math.Floor(value / (double)divisor);

    /// <summary>
    /// Accepts the eight-digit FormKey notation used across the project docs
    /// ("00064B87:Skyrim.esm") and hands Mutagen the six-digit form it expects.
    /// </summary>
    private static FormKey ParseFormKey(string value)
    {
        var parts = value.Split(':', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"'{value}' is not a FormKey in 'FormID:Plugin.esm' form.");
        }

        var rawId = parts[0].Trim();
        if (!uint.TryParse(rawId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
        {
            throw new InvalidOperationException($"'{rawId}' is not a hexadecimal FormID.");
        }

        // Strip the load-order byte; the plugin name carries that information.
        return FormKey.Factory($"{id & 0xFFFFFF:X6}:{parts[1].Trim()}");
    }
}

internal sealed record FairBuildResult(
    string OutputPath,
    long SizeInBytes,
    FormKey TestObjectFormKey,
    FormKey MapMarkerFormKey,
    bool CopiedMasterRecords);
