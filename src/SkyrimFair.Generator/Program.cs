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

        Console.WriteLine($"  {foundation.DressingCount,3} x vanilla rock / shrub / scrub dressing");
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
