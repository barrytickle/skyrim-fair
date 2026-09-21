using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Lays out the landscaped foundation prototype from the project-owned tile kit.
///
/// The paving stays on a clean grid so it can be generated seamlessly, but the
/// visible outline is driven by an irregular mask so the player never sees a
/// rectangle. See docs/DESIGN.md - the safe build envelope is a constraint, not
/// the shape.
/// </summary>
internal static class FairFoundation
{
    /// <summary>Outward +Y rotated onto each compass direction, radians about Z.</summary>
    private static readonly Dictionary<string, float> OutwardRotation = new()
    {
        ["N"] = 0f,
        ["W"] = MathF.PI / 2f,
        ["S"] = MathF.PI,
        ["E"] = 3f * MathF.PI / 2f,
    };

    private static readonly (string Name, int Dc, int Dr)[] Directions =
    {
        ("N", 0, -1),
        ("S", 0, 1),
        ("E", 1, 0),
        ("W", -1, 0),
    };

    public static FoundationResult Build(
        SkyrimMod mod,
        FairConfig config,
        Func<float, float, float?> sampleTerrain,
        Action<IPlacedObjectGetter, float, float> place)
    {
        var f = config.Site.Foundation;
        var site = config.Site;
        var tile = f.TileSize;

        var paved = ParseFootprint(f.Footprint);
        var cols = f.Footprint[0].Length;
        var rows = f.Footprint.Count;

        // Centre the mask on the site.
        (float X, float Y) Centre(int col, int row) => (
            site.Placement.X + (col - (cols - 1) / 2f) * tile,
            site.Placement.Y + ((rows - 1) / 2f - row) * tile);

        var statics = new Dictionary<string, Static>();
        Static StaticFor(string role)
        {
            if (statics.TryGetValue(role, out var existing))
            {
                return existing;
            }

            var piece = f.Pieces[role];
            var record = new Static(mod)
            {
                EditorID = piece.EditorId,
                Model = new Model { File = piece.Model },
            };
            statics[role] = record;
            return record;
        }

        var result = new FoundationResult();

        void Put(string role, float x, float y, float z, float rotZ)
        {
            var baseRecord = StaticFor(role);
            var placed = new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(baseRecord.FormKey),
                Placement = new Placement
                {
                    Position = new P3Float(x, y, z),
                    Rotation = new P3Float(0f, 0f, rotZ),
                },
            };
            place(placed, x, y);
            result.Counts[role] = result.Counts.GetValueOrDefault(role) + 1;
        }

        void PutVanilla(string formKey, float x, float y, float z, float rotZ, float scale)
        {
            var placed = new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(formKey)),
                Scale = scale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, z),
                    Rotation = new P3Float(0f, 0f, rotZ),
                },
            };
            place(placed, x, y);
            result.DressingCount++;
        }

        // ---- paving -------------------------------------------------------
        // Greedy 2x2 blocks become 1024 fill tiles; whatever is left over gets a
        // 512 edge tile. Both are multiples of the same grid so nothing overlaps.
        var used = new HashSet<(int, int)>();
        for (var row = 0; row < rows - 1; row++)
        {
            for (var col = 0; col < cols - 1; col++)
            {
                var block = new[] { (col, row), (col + 1, row), (col, row + 1), (col + 1, row + 1) };
                if (block.Any(c => !paved.Contains(c) || used.Contains(c)))
                {
                    continue;
                }

                foreach (var c in block)
                {
                    used.Add(c);
                }

                var a = Centre(col, row);
                var b = Centre(col + 1, row + 1);
                Put("floorFill", (a.X + b.X) / 2f, (a.Y + b.Y) / 2f, f.FloorZ, 0f);
            }
        }

        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            if (used.Contains((col, row)))
            {
                continue;
            }

            var (x, y) = Centre(col, row);
            Put("floorEdge", x, y, f.FloorZ, 0f);
        }

        // ---- perimeter ----------------------------------------------------
        // Ramp segments are reserved first so no retaining wall blocks the way in.
        var rampSegments = ChooseRampSegments(paved, f, cols, rows);

        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            foreach (var (name, dc, dr) in Directions)
            {
                if (paved.Contains((col + dc, row + dr)))
                {
                    continue;
                }

                var (cx, cy) = Centre(col, row);
                var ex = cx + dc * tile / 2f;
                var ey = cy - dr * tile / 2f;
                var rot = OutwardRotation[name];

                if (rampSegments.Contains((col, row, name)))
                {
                    // Chain of ramp tiles stepping down and outward from the paving.
                    for (var i = 0; i < f.RampTiles; i++)
                    {
                        var ox = ex + dc * tile * i;
                        var oy = ey - dr * tile * i;
                        Put("ramp", ox, oy, f.FloorZ - f.RampRise * i, rot);
                    }

                    continue;
                }

                // Where native ground already reaches the floor there is no step to
                // hide, so a retaining face would sit entirely buried and a shoulder
                // would perch above the paving. Skip both and let the paving meet grade.
                var edgeGround = sampleTerrain(ex, ey);
                var exposed = !edgeGround.HasValue || edgeGround.Value < f.FloorZ - f.MinExposure;
                if (!exposed)
                {
                    continue;
                }

                // Retaining face hangs below the floor plane; surplus buries.
                Put("retain", ex, ey, f.FloorZ, rot);

                // Shoulder sits on native ground just beyond the retaining face.
                var sx = ex + dc * (tile / 2f + f.ShoulderOffset);
                var sy = ey - dr * (tile / 2f + f.ShoulderOffset);
                var ground = sampleTerrain(sx, sy);
                if (ground.HasValue && ground.Value < f.FloorZ)
                {
                    Put("shoulder", sx, sy, ground.Value, rot);
                }
            }
        }

        // ---- outer corners -------------------------------------------------
        // Only the north-east style corner is placed, at rotation 0, so the piece
        // is represented without relying on a rotation convention this prototype
        // has not yet confirmed in game. Remaining corners are hidden by rocks.
        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            if (paved.Contains((col, row - 1)) || paved.Contains((col + 1, row)))
            {
                continue;
            }

            var (cx, cy) = Centre(col, row);
            Put("retainCorner", cx + tile / 2f, cy + tile / 2f, f.FloorZ, 0f);
        }

        // ---- dressing ------------------------------------------------------
        var rng = new Random(f.Dressing.Seed);
        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            foreach (var (name, dc, dr) in Directions)
            {
                if (paved.Contains((col + dc, row + dr)))
                {
                    continue;
                }

                var (cx, cy) = Centre(col, row);
                for (var i = 0; i < f.Dressing.PerEdgeSegment; i++)
                {
                    var along = (float)(rng.NextDouble() - 0.5) * tile;
                    var out_ = tile / 2f + f.Dressing.MinOffset
                        + (float)rng.NextDouble() * f.Dressing.Spread;

                    var px = cx + dc * out_ + (dc == 0 ? along : 0f);
                    var py = cy - dr * out_ + (dr == 0 ? along : 0f);

                    var ground = sampleTerrain(px, py);
                    if (!ground.HasValue)
                    {
                        continue;
                    }

                    // Rocks hug the retaining face and break its silhouette;
                    // shrubs and scrub sit slightly further out in the grass.
                    var pool = i == 0 ? f.Dressing.Rocks
                        : (rng.NextDouble() < 0.5 ? f.Dressing.Shrubs : f.Dressing.Scrub);
                    var pick = pool[rng.Next(pool.Count)];
                    var rot = (float)(rng.NextDouble() * Math.PI * 2.0);
                    var scale = f.Dressing.MinScale
                        + (float)rng.NextDouble() * (f.Dressing.MaxScale - f.Dressing.MinScale);

                    // Sink rocks slightly so they read as bedded into the ground.
                    var z = ground.Value - (i == 0 ? f.Dressing.RockSink : 0f);
                    PutVanilla(pick, px, py, z, rot, scale);
                }
            }
        }

        result.Statics = statics;
        return result;
    }

    private static HashSet<(int, int)> ParseFootprint(IReadOnlyList<string> mask)
    {
        var paved = new HashSet<(int, int)>();
        for (var row = 0; row < mask.Count; row++)
        {
            for (var col = 0; col < mask[row].Length; col++)
            {
                if (mask[row][col] == '#')
                {
                    paved.Add((col, row));
                }
            }
        }

        return paved;
    }

    /// <summary>
    /// Picks the middle run of perimeter segments on the configured ramp edge, so
    /// the entrance reads as one deliberate approach rather than scattered ramps.
    /// </summary>
    private static HashSet<(int, int, string)> ChooseRampSegments(
        HashSet<(int, int)> paved, FoundationConfig f, int cols, int rows)
    {
        var edge = f.RampEdge.ToUpperInvariant();
        var (dc, dr) = Directions.First(d => d.Name == edge) switch { var d => (d.Dc, d.Dr) };

        var candidates = paved
            .Where(c => !paved.Contains((c.Item1 + dc, c.Item2 + dr)))
            .OrderBy(c => dc == 0 ? c.Item1 : c.Item2)
            .ToList();

        var chosen = new HashSet<(int, int, string)>();
        if (candidates.Count == 0)
        {
            return chosen;
        }

        var mid = candidates.Count / 2;
        var start = Math.Max(0, mid - f.RampWidth / 2);
        for (var i = start; i < Math.Min(candidates.Count, start + f.RampWidth); i++)
        {
            chosen.Add((candidates[i].Item1, candidates[i].Item2, edge));
        }

        return chosen;
    }
}

internal sealed class FoundationResult
{
    public Dictionary<string, Static> Statics { get; set; } = new();

    public Dictionary<string, int> Counts { get; } = new();

    public int DressingCount { get; set; }
}
