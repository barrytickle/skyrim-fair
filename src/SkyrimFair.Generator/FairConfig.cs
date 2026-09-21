namespace SkyrimFair.Generator;

internal sealed record FairConfig
{
    public string PluginName { get; init; } = "SkyrimFair.esp";

    public string OutputDirectory { get; init; } = "dist";

    public FairIdentity Identity { get; init; } = new();

    public StagePrototype Stage { get; init; } = new();

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
    }
}

internal sealed record FairIdentity
{
    public string Name { get; init; } = "Skyrim Fair";

    public string WorkingLocation { get; init; } = "Whiterun tundra";
}

internal sealed record StagePrototype
{
    public bool Enabled { get; init; } = true;

    public int DancerCount { get; init; } = 2;

    public int BardCount { get; init; } = 2;

    public string Track { get; init; } = "Round the Green";
}
