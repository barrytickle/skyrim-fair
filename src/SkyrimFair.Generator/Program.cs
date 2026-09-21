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

    var outputPath = FairPluginGenerator.Generate(config);

    Console.WriteLine();
    Console.WriteLine($"Generated: {outputPath}");
    Console.WriteLine("Milestone unlocked: C# -> Mutagen -> Skyrim plugin.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
