namespace SkyrimFair.Generator;

/// <summary>Where the config was read from, so paths inside it resolve beside it.</summary>
internal static class FairPaths
{
    public static string ConfigDirectory { get; set; } = Directory.GetCurrentDirectory();
}
