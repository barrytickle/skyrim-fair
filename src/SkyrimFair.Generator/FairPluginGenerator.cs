using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace SkyrimFair.Generator;

internal static class FairPluginGenerator
{
    public static string Generate(FairConfig config)
    {
        config.Validate();

        var outputDirectory = Path.GetFullPath(config.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var modKey = ModKey.FromFileName(config.PluginName);
        var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);

        var outputPath = Path.Combine(outputDirectory, mod.ModKey.FileName);

        mod.BeginWrite
            .ToPath(outputPath)
            .WithDefaultLoadOrder()
            .Write();

        return outputPath;
    }
}
