using System.Globalization;
using Mutagen.Bethesda.Plugins;

namespace SkyrimFair.Generator;

internal static class FormKeyHelper
{
    /// <summary>
    /// Accepts the eight-digit FormKey notation used across the project docs
    /// ("00064B87:Skyrim.esm") and hands Mutagen the six-digit form it expects.
    /// </summary>
    public static FormKey Parse(string value)
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
