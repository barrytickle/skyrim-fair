using System.Text.Json;
using SkyrimFair.Generator;

const string defaultConfigPath = "fair.config.json";

var configPath = args.Length > 0 ? args[0] : defaultConfigPath;

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Config file not found: {Path.GetFullPath(configPath)}");
    return 1;
}

try
{
    var json = await File.ReadAllTextAsync(configPath);

    var config = JsonSerializer.Deserialize<FairConfig>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    if (config is null)
    {
        Console.Error.WriteLine("The fair config could not be parsed.");
        return 1;
    }

    Console.WriteLine($"Building {config.Identity.Name}...");
    Console.WriteLine($"Working location: {config.Identity.WorkingLocation}");
    Console.WriteLine(
        $"Prototype stage: {config.Stage.BardCount} bard(s), " +
        $"{config.Stage.DancerCount} dancer(s), track '{config.Stage.Track}'");

    var result = FairPluginGenerator.Generate(config);

    var site = config.Site;
    Console.WriteLine();
    Console.WriteLine(
        $"Fair site: Tamriel cell {site.CellGridX}, {site.CellGridY} " +
        $"at ({site.Placement.X}, {site.Placement.Y}, {site.Placement.Z})");
    Console.WriteLine(
        $"  {site.TestObject.EditorId} placed as {result.TestObjectFormKey}");
    Console.WriteLine(
        $"  map marker '{site.MapMarker.Name}' placed as {result.MapMarkerFormKey}");

    if (result.Foundation is { } foundation)
    {
        Console.WriteLine();
        Console.WriteLine($"Foundation prototype at floor Z {site.Foundation.FloorZ}:");
        foreach (var (role, count) in foundation.Counts.OrderBy(p => p.Key))
        {
            Console.WriteLine($"  {count,3} x {role}");
        }

        Console.WriteLine();
        Console.WriteLine("  Terrain integration:");
        Console.WriteLine($"  {foundation.CliffFaces,3} x tundra cliff face skinning a straight wall run");
        Console.WriteLine($"  {foundation.CliffCoveredSegments,3} x straight wall segment covered by cliff skin");
        Console.WriteLine($"  {foundation.CliffEndSkipped,3} x cliff skin refused for an open end past a corner");
        Console.WriteLine($"  {foundation.WallRocks,3} x embankment rock facing an exposed edge");
        Console.WriteLine($"  {foundation.EntrancePieces,3} x vanilla stair-through-a-wall entrance");
        Console.WriteLine($"  {foundation.WallCourses,3} x battered drystone field-wall course");
        Console.WriteLine($"  {foundation.TerminalRocks,3} x part-buried rock ending a masonry run");
        Console.WriteLine($"  {foundation.EntranceBankPieces,3} x earth or rock burying a stair wall");
        Console.WriteLine($"  {foundation.CheekWalls,3} x low drystone cheek beside the steps");
        Console.WriteLine($"  {foundation.CornerStones,3} x corner stone breaking the outline");
        Console.WriteLine($"  {foundation.ToeRocks,3} x toe rock bedded into native grade");
        Console.WriteLine($"  {foundation.VergeWedges,3} x rough-earth verge wedge");
        Console.WriteLine($"  {foundation.VergePlants,3} x shrub / scrub in the verge");
        Console.WriteLine($"  {foundation.DressingCount,3} x vanilla references placed in total");
        Console.WriteLine($"  {foundation.OversizedSkipped,3} x rejected as oversized");
        Console.WriteLine($"  {foundation.ChannelSkipped,3} x rejected for blocking the entrance channel");
        Console.WriteLine($"  {foundation.PavingGuardSkipped,3} x rejected for protruding through the market floor");
        Console.WriteLine($"  {foundation.DisabledCount,3} x named reference disabled (Initially Disabled)");
        foreach (var refusal in foundation.Refused)
        {
            Console.WriteLine($"      REFUSED {refusal}");
        }
        Console.WriteLine($"  {foundation.Statics.Count} STAT records created");
    }

    Console.WriteLine();
    Console.WriteLine($"Cells touched: {string.Join(", ", result.CellsTouched.Select(c => $"{c.X},{c.Y}"))}");

    Console.WriteLine();
    if (result.CopiedMasterRecords)
    {
        Console.WriteLine("Overridden Tamriel worldspace and cell records copied from Skyrim.esm.");
    }
    else
    {
        Console.WriteLine(
            "WARNING: Skyrim.esm was not read, so the overridden worldspace and cell records " +
            "are stubs. They would strip vanilla regions, water height and map data. " +
            "Set Site.SkyrimDataPath (or SKYRIM_DATA_PATH) before loading this plugin in game.");
    }

    Console.WriteLine($"Generated: {result.OutputPath} ({result.SizeInBytes} bytes)");
    Console.WriteLine("Milestone unlocked: C# -> Mutagen -> Skyrim plugin.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
