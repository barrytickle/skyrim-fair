namespace SkyrimFair.Generator;

/// <summary>
/// Fixed integer hashes for everything the generator varies "by hand". Never seeded from
/// a string hash, which .NET randomises per process: the same config must always give
/// the same plugin.
/// </summary>
internal static class FairHash
{
    /// <summary>Hash of two integers in 0..1.</summary>
    public static float Hash(int x, int y)
    {
        unchecked
        {
            var h = (uint)x * 374761393u + (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }

    /// <summary>Hash of three integers in 0..1: a position and a salt naming what it decides.</summary>
    public static float Hash3(int a, int b, int salt)
        => Hash(unchecked(a * 73856093 ^ salt * 83492791), unchecked(b * 19349663 + salt));

    /// <summary>-1..1 from <see cref="Hash3"/>.</summary>
    public static float Signed(int a, int b, int salt) => Hash3(a, b, salt) * 2f - 1f;
}
