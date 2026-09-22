using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: SkyrimFair.StaticAudit <Skyrim.esm> [search terms ...]");
    return 1;
}

using var master = SkyrimMod.CreateFromBinaryOverlay(args[0], SkyrimRelease.SkyrimSE);
var customTerms = args.Length > 1;
var terms = customTerms ? args[1..] : new[] { "cliff", "rock", "tundra", "shelf" };

foreach (var record in master.Statics
    .Where(r => terms.Any(term =>
        (r.EditorID?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        || (r.Model?.File.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)))
    .OrderBy(r => r.EditorID))
{
    var b = record.ObjectBounds;
    if (b is null)
    {
        continue;
    }

    var x = b.Second.X - b.First.X;
    var y = b.Second.Y - b.First.Y;
    var z = b.Second.Z - b.First.Z;
    if (!customTerms && (Math.Max(x, y) < 450 || z < 100))
    {
        continue;
    }

    Console.WriteLine(
        $"{record.FormKey.ID:X6}\t{record.EditorID}\t{x}x{y}x{z}\t" +
        $"[{b.First.X},{b.First.Y},{b.First.Z}]..[{b.Second.X},{b.Second.Y},{b.Second.Z}]\t" +
        $"{record.Model?.File}");
}

return 0;
