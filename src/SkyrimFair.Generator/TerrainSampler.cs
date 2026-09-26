using Mutagen.Bethesda.Skyrim;

namespace SkyrimFair.Generator;

/// <summary>
/// Reads vanilla LAND heightmaps so generated references can sit on the ground.
///
/// Only Skyrim.esm is read. That is correct for the fair site: the audit confirmed
/// cells -2,-4 and -2,-3 have no mod LAND overrides, so vanilla is the winning
/// record. If the fair ever moves to a cell a landscape mod edits, this needs to
/// resolve the winner through the load order instead.
/// </summary>
internal sealed class TerrainSampler
{
    private const int CellSize = 4096;

    private const int Step = 128;

    private const int Points = 33;

    private readonly IWorldspaceGetter? worldspace;

    private readonly Dictionary<(int, int), float[]?> cache = new();

    public TerrainSampler(IWorldspaceGetter? worldspace) => this.worldspace = worldspace;

    /// <summary>Bilinearly sampled ground height, or null if no LAND is available.</summary>
    public float? Sample(float x, float y)
    {
        var cx = (int)MathF.Floor(x / CellSize);
        var cy = (int)MathF.Floor(y / CellSize);
        var heights = HeightsFor(cx, cy);
        if (heights is null)
        {
            return null;
        }

        var fx = (x - cx * CellSize) / Step;
        var fy = (y - cy * CellSize) / Step;
        var i = Math.Clamp((int)MathF.Floor(fx), 0, Points - 2);
        var j = Math.Clamp((int)MathF.Floor(fy), 0, Points - 2);
        var tx = Math.Clamp(fx - i, 0f, 1f);
        var ty = Math.Clamp(fy - j, 0f, 1f);

        float At(int px, int py) => heights[py * Points + px];

        var top = At(i, j) * (1 - tx) + At(i + 1, j) * tx;
        var bottom = At(i, j + 1) * (1 - tx) + At(i + 1, j + 1) * tx;
        return top * (1 - ty) + bottom * ty;
    }

    private float[]? HeightsFor(int cx, int cy)
    {
        if (cache.TryGetValue((cx, cy), out var cached))
        {
            return cached;
        }

        float[]? heights = null;
        var cell = FindCell(cx, cy);
        var map = cell?.Landscape?.VertexHeightMap;
        if (map is not null)
        {
            heights = new float[Points * Points];
            var rowStart = map.Offset;
            for (var row = 0; row < Points; row++)
            {
                rowStart += map.HeightMap[0, row];
                var running = rowStart;
                heights[row * Points] = running;
                for (var col = 1; col < Points; col++)
                {
                    running += map.HeightMap[col, row];
                    heights[row * Points + col] = running;
                }
            }

            for (var k = 0; k < heights.Length; k++)
            {
                heights[k] *= 8f;
            }
        }

        cache[(cx, cy)] = heights;
        return heights;
    }

    private ICellGetter? FindCell(int cx, int cy)
    {
        if (worldspace is null)
        {
            return null;
        }

        foreach (var block in worldspace.SubCells)
        {
            foreach (var subBlock in block.Items)
            {
                foreach (var cell in subBlock.Items)
                {
                    var grid = cell.Grid?.Point;
                    if (grid is not null && grid.Value.X == cx && grid.Value.Y == cy)
                    {
                        return cell;
                    }
                }
            }
        }

        return null;
    }
}
