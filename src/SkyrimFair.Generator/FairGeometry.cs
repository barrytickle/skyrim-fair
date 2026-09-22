namespace SkyrimFair.Generator;

/// <summary>Plan geometry shared by the world, stage and market builders.</summary>
internal static class FairGeometry
{
    public static float SignedDistance((float X, float Y)[] polygon, float x, float y)
    {
        var nearest = float.MaxValue;
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var a = polygon[j];
            var b = polygon[i];
            nearest = MathF.Min(nearest, SegmentDistance(a, b, x, y).Distance);
            if ((b.Y > y) != (a.Y > y) && x < (a.X - b.X) * (y - b.Y) / (a.Y - b.Y) + b.X)
            {
                inside = !inside;
            }
        }

        return inside ? -nearest : nearest;
    }

    public static float PolylineDistance((float X, float Y)[] line, float x, float y)
    {
        var nearest = float.MaxValue;
        for (var i = 0; i + 1 < line.Length; i++)
        {
            nearest = MathF.Min(nearest, SegmentDistance(line[i], line[i + 1], x, y).Distance);
        }

        return nearest;
    }

    public static (float Distance, float T) SegmentDistance((float X, float Y) a, (float X, float Y) b, float x, float y)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var lengthSquared = abx * abx + aby * aby;
        var t = lengthSquared == 0f ? 0f : Math.Clamp(((x - a.X) * abx + (y - a.Y) * aby) / lengthSquared, 0f, 1f);
        var px = a.X + abx * t - x;
        var py = a.Y + aby * t - y;
        return (MathF.Sqrt(px * px + py * py), t);
    }

    /// <summary>True inside the polygon (even-odd rule).</summary>
    public static bool Inside((float X, float Y)[] polygon, float x, float y) => SignedDistance(polygon, x, y) <= 0f;
}
