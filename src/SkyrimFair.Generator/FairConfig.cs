namespace SkyrimFair.Generator;

internal sealed record FairConfig
{
    public string PluginName { get; init; } = "SkyrimFair.esp";

    public string OutputDirectory { get; init; } = "dist";

    public FairIdentity Identity { get; init; } = new();

    public StagePrototype Stage { get; init; } = new();

    public PrototypeSite Site { get; init; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PluginName))
        {
            throw new InvalidOperationException("PluginName cannot be empty.");
        }

        if (!PluginName.EndsWith(".esp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The prototype currently expects PluginName to end in .esp.");
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new InvalidOperationException("OutputDirectory cannot be empty.");
        }

        Site.Validate();
    }
}

internal sealed record FairIdentity
{
    public string Name { get; init; } = "Skyrim Fair";

    public string WorkingLocation { get; init; } = "Whiterun tundra";

    /// <summary>Written to the plugin's TES4 header as CNAM.</summary>
    public string Author { get; init; } = "BarryRim Event Planner";
}

internal sealed record StagePrototype
{
    public bool Enabled { get; init; } = true;

    public int DancerCount { get; init; } = 2;

    public int BardCount { get; init; } = 2;

    public string Track { get; init; } = "Round the Green";
}

/// <summary>
/// The audited exterior test site. Every FormKey and coordinate the generator
/// places lives here so implementation code never carries raw world data.
/// Values come from docs/AUDIT.md and were read out of Skyrim.esm directly.
/// </summary>
internal sealed record PrototypeSite
{
    /// <summary>
    /// Skyrim's Data folder, read-only, used to copy the real WRLD/CELL records we
    /// override so nothing vanilla is stripped. Optional: leave null (as CI does) and
    /// the generator still emits a structurally valid plugin, but it must not be loaded.
    /// The SKYRIM_DATA_PATH environment variable takes precedence over this value.
    /// </summary>
    public string? SkyrimDataPath { get; init; }

    /// <summary>Tamriel worldspace.</summary>
    public string Worldspace { get; init; } = "0000003C:Skyrim.esm";

    /// <summary>Tamriel's persistent cell, where vanilla exterior map markers live.</summary>
    public string PersistentCell { get; init; } = "00000D74:Skyrim.esm";

    /// <summary>The exterior cell that receives the test object.</summary>
    public string Cell { get; init; } = "00009A28:Skyrim.esm";

    public int CellGridX { get; init; } = -2;

    public int CellGridY { get; init; } = -4;

    public FairPlacement Placement { get; init; } = new();

    /// <summary>
    /// Planned floor height for the future flat market platform (Plan B): the terrain
    /// maximum across the 3072-unit core, so the platform is pure fill with no cut and
    /// needs no LAND edits. Recorded here for the platform milestone; the current
    /// prototype places objects on native ground instead, via Placement.
    /// </summary>
    public float PlannedPlatformFloorZ { get; init; } = -5672f;

    public FairTestObject TestObject { get; init; } = new();

    public FairMapMarker MapMarker { get; init; } = new();

    public FoundationConfig Foundation { get; init; } = new();

    public void Validate()
    {
        RequireFormKey(Worldspace, nameof(Worldspace));
        RequireFormKey(PersistentCell, nameof(PersistentCell));
        RequireFormKey(Cell, nameof(Cell));
        RequireFormKey(TestObject.FormKey, "TestObject.FormKey");
        RequireFormKey(MapMarker.BaseObject, "MapMarker.BaseObject");

        if (string.IsNullOrWhiteSpace(MapMarker.Name))
        {
            throw new InvalidOperationException("MapMarker.Name cannot be empty.");
        }

        Foundation.Validate();
    }

    private static void RequireFormKey(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Site.{field} cannot be empty.");
        }

        if (!value.Contains(':'))
        {
            throw new InvalidOperationException(
                $"Site.{field} must be a FormKey in 'FormID:Plugin.esm' form, but was '{value}'.");
        }
    }
}

internal sealed record FairPlacement
{
    public float X { get; init; } = -5632f;

    public float Y { get; init; } = -12800f;

    /// <summary>Native terrain height at X/Y, so prototype objects sit on the ground.</summary>
    public float Z { get; init; } = -5720f;
}

internal sealed record FairTestObject
{
    public string EditorId { get; init; } = "SMarketStall01";

    public string FormKey { get; init; } = "00064B87:Skyrim.esm";
}

internal sealed record FairMapMarker
{
    public string BaseObject { get; init; } = "00000010:Skyrim.esm";

    public string Name { get; init; } = "The Wanderer's Fair";

    /// <summary>
    /// Name of a Mutagen MapMarker.MarkerType value. "Pass" is raw TNAM 0x18,
    /// the icon vanilla uses for its border-pass crossings.
    /// </summary>
    public string Type { get; init; } = "Pass";

    public bool Visible { get; init; } = true;

    public bool CanTravelTo { get; init; } = true;

    /// <summary>
    /// XRDS marker radius. 1800 matches WhiterunWatchtowerMapMarker, the nearest
    /// vanilla marker to the fair site, and suits a 3072-unit market core.
    /// </summary>
    public float Radius { get; init; } = 1800f;

    /// <summary>
    /// XLRT location reference type. Vanilla uses MapMarkerRefType on 333 of its
    /// 347 Tamriel map markers.
    /// </summary>
    public string LocationRefType { get; init; } = "0010F63C:Skyrim.esm";

    /// <summary>
    /// Where the marker sits, if it should not sit at Site.Placement. Once the
    /// foundation covers the site centre the marker must move off it, or fast travel
    /// drops the player into the gap between native ground and the paving slab.
    /// This puts arrival on open ground just beyond the foot of the entrance ramp.
    /// </summary>
    public FairPlacement? Position { get; init; }
}

/// <summary>
/// The landscaped foundation prototype: an irregular paved area built from the
/// project-owned tile kit in assets/blender/build_foundation_kit.py.
/// </summary>
internal sealed record FoundationConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Grid step, matching the kit's 512 edge tile.</summary>
    public int TileSize { get; init; } = 512;

    /// <summary>
    /// Platform floor height. Set to the terrain maximum across the footprint so
    /// the platform is pure fill with zero cut and needs no LAND edits.
    /// </summary>
    public float FloorZ { get; init; } = -5672f;

    /// <summary>
    /// The visible outline, one character per TileSize cell, '#' paved. Deliberately
    /// irregular: the player must never see a rectangle or the L-shaped safe envelope.
    /// First row is north.
    /// </summary>
    public IReadOnlyList<string> Footprint { get; init; } = new[]
    {
        ".###..",
        ".#####",
        "######",
        ".#####",
        "..###.",
    };

    /// <summary>Compass edge carrying the entrance ramp: N, S, E or W.</summary>
    public string RampEdge { get; init; } = "S";

    /// <summary>Ramp tiles chained outward; each drops RampRise.</summary>
    public int RampTiles { get; init; } = 2;

    /// <summary>How many perimeter segments wide the entrance is.</summary>
    public int RampWidth { get; init; } = 2;

    /// <summary>Fall per ramp tile, matching the kit's 1:8 grade over 512 units.</summary>
    public float RampRise { get; init; } = 64f;

    /// <summary>Gap between the retaining face and the shoulder wedge.</summary>
    public float ShoulderOffset { get; init; } = 64f;

    /// <summary>
    /// Minimum step between floor and native ground before a retaining face is worth
    /// placing. Below this the paving simply meets grade, which is what happens on the
    /// east side of the site where the tundra rises to meet the platform.
    /// </summary>
    public float MinExposure { get; init; } = 16f;

    public Dictionary<string, FoundationPiece> Pieces { get; init; } = new()
    {
        ["floorFill"] = new() { EditorId = "SkyrimFairFloorFill1024", Model = @"SkyrimFair\SkyrimFair_FloorFill_1024.nif" },
        ["floorEdge"] = new() { EditorId = "SkyrimFairFloorEdge512", Model = @"SkyrimFair\SkyrimFair_FloorEdge_512.nif" },
        ["retain"] = new() { EditorId = "SkyrimFairRetain512", Model = @"SkyrimFair\SkyrimFair_Retain_512.nif" },
        ["retainCorner"] = new() { EditorId = "SkyrimFairRetainCorner128", Model = @"SkyrimFair\SkyrimFair_RetainCorner_128.nif" },
        ["ramp"] = new() { EditorId = "SkyrimFairRamp512", Model = @"SkyrimFair\SkyrimFair_Ramp_512.nif" },
        ["shoulder"] = new() { EditorId = "SkyrimFairShoulder512", Model = @"SkyrimFair\SkyrimFair_Shoulder_512.nif" },
    };

    public DressingConfig Dressing { get; init; } = new();

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Footprint.Count == 0 || Footprint.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Foundation.Footprint must have at least one non-empty row.");
        }

        if (Footprint.Select(r => r.Length).Distinct().Count() != 1)
        {
            throw new InvalidOperationException("Foundation.Footprint rows must all be the same length.");
        }

        if (!Footprint.Any(r => r.Contains('#')))
        {
            throw new InvalidOperationException("Foundation.Footprint has no paved cells.");
        }

        foreach (var role in new[] { "floorFill", "floorEdge", "retain", "retainCorner", "ramp", "shoulder" })
        {
            if (!Pieces.ContainsKey(role))
            {
                throw new InvalidOperationException($"Foundation.Pieces is missing '{role}'.");
            }
        }

        if (!new[] { "N", "S", "E", "W" }.Contains(RampEdge.ToUpperInvariant()))
        {
            throw new InvalidOperationException($"Foundation.RampEdge must be N, S, E or W, but was '{RampEdge}'.");
        }
    }
}

internal sealed record FoundationPiece
{
    public string EditorId { get; init; } = string.Empty;

    /// <summary>Mesh path relative to Data\meshes\.</summary>
    public string Model { get; init; } = string.Empty;
}

/// <summary>Vanilla rocks and plants used to break up the hard tile boundary.</summary>
internal sealed record DressingConfig
{
    public int Seed { get; init; } = 20260921;

    public int PerEdgeSegment { get; init; } = 2;

    public float MinOffset { get; init; } = 32f;

    public float Spread { get; init; } = 224f;

    public float MinScale { get; init; } = 0.7f;

    public float MaxScale { get; init; } = 1.4f;

    /// <summary>Rocks are sunk slightly so they read as bedded into the ground.</summary>
    public float RockSink { get; init; } = 24f;

    public IReadOnlyList<string> Rocks { get; init; } = new[]
    {
        "00039224:Skyrim.esm", // RockTundraLand01Tundra01
        "0003925D:Skyrim.esm", // RockTundraLand02Tundra01
        "0001BFB0:Skyrim.esm", // RockPileM01FieldGrass01Moss
        "00024E8F:Skyrim.esm", // RockPileM02FieldGrass01Moss
        "00021E70:Skyrim.esm", // RockPileS01FieldGrass01
        "000674BB:Skyrim.esm", // RockPileS02FieldGrass01
    };

    public IReadOnlyList<string> Shrubs { get; init; } = new[]
    {
        "000AAE79:Skyrim.esm", // TreeTundraShrub01
        "000AAE7A:Skyrim.esm", // TreeTundraShrub02
        "000AAE7B:Skyrim.esm", // TreeTundraShrub03
        "000AAE7F:Skyrim.esm", // TreeTundraShrub04
        "000AAE81:Skyrim.esm", // TreeTundraShrub05
        "000AAE83:Skyrim.esm", // TreeTundraShrub06
    };

    public IReadOnlyList<string> Scrub { get; init; } = new[]
    {
        "0003A2BC:Skyrim.esm", // TundraScrub01
        "0003A2BD:Skyrim.esm", // TundraScrub02
        "0003A2BE:Skyrim.esm", // TundraScrub03
    };
}
