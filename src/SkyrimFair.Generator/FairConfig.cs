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
}
