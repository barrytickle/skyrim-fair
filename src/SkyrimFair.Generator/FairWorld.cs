using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Builds the isolated festival worldspace described by <see cref="FairWorldConfig"/>:
/// a new WRLD with its own climate, a persistent cell of named zone markers, and a
/// square of exterior cells each carrying a generated LAND record.
///
/// This is a parallel prototype. It adds records only; nothing in Tamriel, and nothing
/// the terrace build writes, is read or changed here.
///
/// The ground is generated from rules rather than painted by hand, so it regenerates
/// byte-identically: flat inside the planned perimeter, rising into low hills beyond
/// it, and textured so the broad plan can be walked in game before anything is built.
/// </summary>
internal static class FairWorld
{
    private const int CellSize = 4096;

    /// <summary>LAND vertices per side. Adjacent cells share their edge row.</summary>
    private const int Points = 33;

    private const float Step = CellSize / (Points - 1f);

    /// <summary>Vertices per side of one texture quadrant (a quarter cell, edges shared).</summary>
    private const int QuadPoints = 17;

    /// <summary>VHGT stores heights in units of eight.</summary>
    private const float HeightUnit = 8f;

    /// <summary>The Persistent record flag; Mutagen does not write it from group membership.</summary>
    private const int PersistentRecordFlag = 0x400;

    /// <summary>
    /// DATA flags on LAND, written raw as 0x1D: normals + heights (0x01), layers (0x04),
    /// and the 0x08 and 0x10 bits every vanilla LAND carries. Sovngarde's LAND, which
    /// also has no vertex colours, is exactly this value. Not built from Mutagen's names:
    /// its <c>MPCD</c> member is 0x400, not the 0x08 bit.
    /// </summary>
    private const Landscape.Flag LandFlags = (Landscape.Flag)0x1D;

    /// <summary>BTXT's layer field is always -1.</summary>
    private const ushort BaseLayerNumber = 0xFFFF;

    /// <summary>Most alpha layers any one quadrant may carry; vanilla quadrants use up to five.</summary>
    private const int MaxAlphaLayers = 6;

    /// <summary>Water is switched off in every cell; this is belt and braces for the defaults.</summary>
    private const float NoWaterHeight = -50000f;

    /// <summary>Soft edge of every painted zone.</summary>
    private const float ZoneFeather = 192f;

    private const float StripFeather = 96f;

    public static FairWorldResult Build(SkyrimMod mod, FairWorldConfig config, ISkyrimModGetter? master)
    {
        var plan = new Plan(config);

        // ---- climate: vanilla sky and sun, the tundra region's weather ------------
        var climateKey = BuildClimate(mod, config, master, out var weatherCount);

        // ---- the worldspace itself ------------------------------------------------
        var worldspace = new Worldspace(mod)
        {
            EditorID = config.EditorId,
            Name = config.Name,
            Parent = new WorldspaceParent
            {
                Worldspace = new FormLink<IWorldspaceGetter>(FormKeyHelper.Parse(config.ParentWorldspace)),
                Flags = WorldspaceParent.Flag.UseMapData,
            },
            Climate = new FormLinkNullable<IClimateGetter>(climateKey),
            LandDefaults = new WorldspaceLandDefaults
            {
                DefaultLandHeight = config.FloorZ,
                DefaultWaterHeight = NoWaterHeight,
            },
            Flags = Worldspace.Flag.NoLodWater,
            ObjectBoundsMin = new P2Float(-config.CellRadius, -config.CellRadius),
            ObjectBoundsMax = new P2Float(config.CellRadius + 1, config.CellRadius + 1),
            DistantLodMultiplier = 1f,
        };

        // ---- persistent cell: one named marker per zone ---------------------------
        var topCell = new Cell(mod)
        {
            MajorRecordFlagsRaw = PersistentRecordFlag,
            Grid = new CellGrid { Point = new P2Int(0, 0) },
        };

        var markers = new List<FairWorldMarker>();
        var markerBase = FormKeyHelper.Parse(config.ZoneMarker);
        foreach (var zone in config.Zones)
        {
            var (x, y, heading) = (zone.Marker[0], zone.Marker[1], zone.Marker[2]);
            var marker = Place(mod, markerBase, x, y, config.FloorZ, heading);
            marker.EditorID = $"{config.EditorId}{zone.Name}Marker";
            marker.MajorRecordFlagsRaw = PersistentRecordFlag;
            topCell.Persistent.Add(marker);
            markers.Add(new FairWorldMarker(marker.EditorID, marker.FormKey, x, y, heading));
        }

        worldspace.TopCell = topCell;

        // ---- temporary scale posts along the planned wall line --------------------
        var posts = new Dictionary<(int X, int Y), List<PlacedObject>>();
        var postBase = FormKeyHelper.Parse(config.PerimeterPost);
        var postCount = 0;
        foreach (var (x, y, heading) in plan.PostPositions(config.PostSpacing))
        {
            var key = ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));
            if (!posts.TryGetValue(key, out var list))
            {
                list = new List<PlacedObject>();
                posts[key] = list;
            }

            list.Add(Place(mod, postBase, x, y, plan.Height(x, y), heading));
            postCount++;
        }

        // ---- exterior cells, each with its own landscape --------------------------
        var grid = new ExteriorCellGrid(worldspace);
        var textures = plan.PaintTextures();
        var cellCount = 0;
        var maxLayers = 0;

        for (var cy = -config.CellRadius; cy <= config.CellRadius; cy++)
        {
            for (var cx = -config.CellRadius; cx <= config.CellRadius; cx++)
            {
                // No Has Water flag: this world has no water anywhere.
                var cell = new Cell(mod)
                {
                    Grid = new CellGrid { Point = new P2Int(cx, cy) },
                    Flags = 0,
                };

                cell.Landscape = BuildLandscape(mod, plan, textures, cx, cy, ref maxLayers);

                if (posts.TryGetValue((cx, cy), out var cellPosts))
                {
                    foreach (var post in cellPosts)
                    {
                        cell.Temporary.Add(post);
                    }
                }

                grid.Add(cell, cx, cy);
                cellCount++;
            }
        }

        mod.Worldspaces.Add(worldspace);

        return new FairWorldResult(
            worldspace.FormKey,
            config.EditorId,
            climateKey,
            weatherCount,
            cellCount,
            config.CellRadius,
            postCount,
            maxLayers,
            markers,
            plan.Bounds,
            plan.RenderPlan(512f));
    }

    // ------------------------------------------------------------------------
    // Climate
    // ------------------------------------------------------------------------

    /// <summary>
    /// Tamriel's weather comes from regions, and its own climate lists a single weather,
    /// so inheriting Tamriel's climate would give one sky for ever. Instead the base
    /// climate is copied (sun, moons, sky model, day timings) and handed the tundra
    /// region's weather list, which is what the player sees on the plains at the site.
    /// </summary>
    private static FormKey BuildClimate(
        SkyrimMod mod, FairWorldConfig config, ISkyrimModGetter? master, out int weatherCount)
    {
        var baseKey = FormKeyHelper.Parse(config.BaseClimate);
        var regionKey = FormKeyHelper.Parse(config.WeatherRegion);
        var baseClimate = master?.Climates.FirstOrDefault(c => c.FormKey == baseKey);
        var region = master?.Regions.FirstOrDefault(r => r.FormKey == regionKey);

        if (baseClimate is null || region?.Weather is null)
        {
            // Without the master the vanilla climate is referenced as it stands.
            weatherCount = 0;
            return baseKey;
        }

        var climate = baseClimate.Duplicate(mod.GetNextFormKey());
        climate.EditorID = config.ClimateEditorId;
        climate.WeatherTypes = new ExtendedList<WeatherType>(
            (region.Weather.Weathers ?? Array.Empty<IWeatherTypeGetter>()).Select(w => new WeatherType
            {
                Weather = new FormLink<IWeatherGetter>(w.Weather.FormKey),
                Chance = (int)w.Chance,
                Global = new FormLink<IGlobalGetter>(w.Global.FormKey),
            }));
        mod.Climates.Add(climate);
        weatherCount = climate.WeatherTypes.Count;
        return climate.FormKey;
    }

    // ------------------------------------------------------------------------
    // Landscape
    // ------------------------------------------------------------------------

    private static Landscape BuildLandscape(
        SkyrimMod mod, Plan plan, IReadOnlyList<PaintTexture> textures, int cx, int cy, ref int maxLayers)
    {
        var originX = cx * CellSize;
        var originY = cy * CellSize;

        // Quantised heights with a one-vertex border, so normals at the cell edge are
        // taken across the seam from the same global function the neighbour uses.
        var q = new int[Points + 2, Points + 2];
        for (var row = -1; row <= Points; row++)
        {
            for (var col = -1; col <= Points; col++)
            {
                q[col + 1, row + 1] = (int)MathF.Round(plan.Height(originX + col * Step, originY + row * Step) / HeightUnit);
            }
        }

        var heightMap = new Array2d<sbyte>(new P2Int(Points, Points), 0);
        var normals = new Array2d<P3UInt8>(new P2Int(Points, Points), default(P3UInt8));
        for (var row = 0; row < Points; row++)
        {
            for (var col = 0; col < Points; col++)
            {
                var here = q[col + 1, row + 1];
                var delta = col == 0
                    ? (row == 0 ? 0 : here - q[1, row])
                    : here - q[col, row + 1];
                if (delta is < sbyte.MinValue or > sbyte.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"FairWorld terrain is too steep at cell {cx},{cy} vertex {col},{row}.");
                }

                heightMap[col, row] = (sbyte)delta;

                var dx = (q[col + 2, row + 1] - q[col, row + 1]) * HeightUnit / (2 * Step);
                var dy = (q[col + 1, row + 2] - q[col + 1, row]) * HeightUnit / (2 * Step);
                var length = MathF.Sqrt(dx * dx + dy * dy + 1f);
                normals[col, row] = new P3UInt8(
                    SignedByte(-dx / length), SignedByte(-dy / length), SignedByte(1f / length));
            }
        }

        var land = new Landscape(mod)
        {
            Flags = LandFlags,
            IsCompressed = true,
            VertexNormals = normals,
            VertexHeightMap = new LandscapeVertexHeightMap
            {
                Offset = q[1, 1],
                HeightMap = heightMap,
            },
        };

        // Four quadrants, 17 x 17 vertices each, row 0 at the south edge.
        foreach (var (quadrant, qx, qy) in Quadrants)
        {
            land.Layers.Add(new BaseLayer
            {
                Header = new LayerHeader
                {
                    Texture = new FormLink<ILandscapeTextureGetter>(plan.GroundTexture),
                    Quadrant = quadrant,
                    LayerNumber = BaseLayerNumber,
                },
            });

            var layerNumber = 0;
            foreach (var texture in textures)
            {
                var data = new ExtendedList<AlphaLayerData>();
                for (var row = 0; row < QuadPoints; row++)
                {
                    for (var col = 0; col < QuadPoints; col++)
                    {
                        var x = originX + (qx * (QuadPoints - 1) + col) * Step;
                        var y = originY + (qy * (QuadPoints - 1) + row) * Step;
                        var alpha = texture.Alpha(x, y);
                        if (alpha >= 1f / 255f)
                        {
                            data.Add(new AlphaLayerData
                            {
                                Position = (ushort)(row * QuadPoints + col),
                                Opacity = MathF.Round(alpha * 255f) / 255f,
                            });
                        }
                    }
                }

                if (data.Count == 0)
                {
                    continue;
                }

                land.Layers.Add(new AlphaLayer
                {
                    Header = new LayerHeader
                    {
                        Texture = new FormLink<ILandscapeTextureGetter>(texture.Texture),
                        Quadrant = quadrant,
                        LayerNumber = (ushort)layerNumber,
                    },
                    AlphaLayerData = data,
                });
                layerNumber++;
            }

            if (layerNumber > MaxAlphaLayers)
            {
                throw new InvalidOperationException(
                    $"FairWorld cell {cx},{cy} {quadrant} needs {layerNumber} texture layers; the limit is {MaxAlphaLayers}.");
            }

            maxLayers = Math.Max(maxLayers, layerNumber);
        }

        return land;
    }

    private static readonly (Quadrant Quadrant, int X, int Y)[] Quadrants =
    {
        (Quadrant.BottomLeft, 0, 0),
        (Quadrant.BottomRight, 1, 0),
        (Quadrant.TopLeft, 0, 1),
        (Quadrant.TopRight, 1, 1),
    };

    private static byte SignedByte(float unit) => unchecked((byte)(sbyte)MathF.Round(unit * 127f));

    private static PlacedObject Place(SkyrimMod mod, FormKey baseKey, float x, float y, float z, float headingDegrees)
        => new(mod)
        {
            Base = new FormLinkNullable<IPlaceableObjectGetter>(baseKey),
            Placement = new Placement
            {
                Position = new P3Float(x, y, z),
                Rotation = new P3Float(0f, 0f, headingDegrees * MathF.PI / 180f),
            },
        };

    // ------------------------------------------------------------------------
    // The plan: perimeter, avenue and zones as distance fields
    // ------------------------------------------------------------------------

    private sealed record PaintTexture(FormKey Texture, Func<float, float, float> Alpha);

    private sealed class Plan
    {
        private readonly FairWorldConfig config;

        private readonly (float X, float Y)[] perimeter;

        private readonly (float X, float Y)[] avenue;

        private readonly List<(FairWorldZone Zone, (float X, float Y)[] Polygon)> zones;

        public Plan(FairWorldConfig config)
        {
            this.config = config;
            perimeter = config.Perimeter.Select(p => (p[0], p[1])).ToArray();
            avenue = config.Avenue.Select(p => (p[0], p[1])).ToArray();
            zones = config.Zones.Select(z => (z, z.Polygon.Select(p => (p[0], p[1])).ToArray())).ToList();
            GroundTexture = FormKeyHelper.Parse(config.Textures.Ground);
        }

        public FormKey GroundTexture { get; }

        public (float MinX, float MinY, float MaxX, float MaxY) Bounds =>
            (perimeter.Min(p => p.X), perimeter.Min(p => p.Y), perimeter.Max(p => p.X), perimeter.Max(p => p.Y));

        /// <summary>Distance outside the perimeter; negative inside.</summary>
        public float Outside(float x, float y) => SignedDistance(perimeter, x, y);

        public float Height(float x, float y)
        {
            var t = config.Terrain;
            var beyond = Outside(x, y) - t.FlatMargin;
            if (beyond <= 0f)
            {
                return config.FloorZ;
            }

            var rise = Smooth(beyond / t.RiseDistance);
            var noise = ValueNoise(x / t.NoisePeriod, y / t.NoisePeriod) * 0.65f
                + ValueNoise(x / (t.NoisePeriod * 0.5f) + 17.3f, y / (t.NoisePeriod * 0.5f) - 5.1f) * 0.35f;
            return config.FloorZ + rise * (t.RiseHeight + t.NoiseAmplitude * (noise * 2f - 1f));
        }

        /// <summary>
        /// Every painted texture in draw order, with its alpha field. Zones sharing a
        /// texture are merged into one layer so a quadrant never carries a texture twice.
        /// </summary>
        public IReadOnlyList<PaintTexture> PaintTextures()
        {
            var ops = new List<(FormKey Texture, Func<float, float, float> Alpha)>
            {
                (FormKeyHelper.Parse(config.Textures.Outside), (x, y) => Smooth((Outside(x, y) - 128f) / 512f)),
                (FormKeyHelper.Parse(config.Textures.Perimeter),
                    (x, y) => Fill(MathF.Abs(Outside(x, y)) - config.PerimeterStripWidth / 2f, StripFeather)
                        * (1f - Fill(GateDistance(x, y) - config.GateWidth / 2f, StripFeather))),
                (FormKeyHelper.Parse(config.Textures.Avenue),
                    (x, y) => Fill(PolylineDistance(avenue, x, y) - config.AvenueWidth / 2f, ZoneFeather)),
            };

            foreach (var (zone, polygon) in zones)
            {
                ops.Add((FormKeyHelper.Parse(zone.Texture), (x, y) => Fill(SignedDistance(polygon, x, y), ZoneFeather)));
            }

            return ops
                .GroupBy(o => o.Texture)
                .Select(g =>
                {
                    var fields = g.Select(o => o.Alpha).ToArray();
                    return new PaintTexture(g.Key, (x, y) => fields.Max(f => f(x, y)));
                })
                .ToList();
        }

        /// <summary>Distance to the gate point; infinite when no gate is configured.</summary>
        private float GateDistance(float x, float y)
        {
            if (config.Gate is not { Length: 2 } gate)
            {
                return float.MaxValue;
            }

            var dx = x - gate[0];
            var dy = y - gate[1];
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Posts at every perimeter vertex and at even spacing along each edge, except
        /// across the gate, which gets one post either side of its opening instead.
        /// </summary>
        public IEnumerable<(float X, float Y, float Heading)> PostPositions(float spacing)
        {
            var gate = config.Gate;
            var gateHalf = config.GateWidth / 2f;

            for (var i = 0; i < perimeter.Length; i++)
            {
                var a = perimeter[i];
                var b = perimeter[(i + 1) % perimeter.Length];
                var length = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var heading = MathF.Atan2(b.X - a.X, b.Y - a.Y) * 180f / MathF.PI;
                var steps = Math.Max(1, (int)MathF.Round(length / spacing));
                var gateOnEdge = false;
                var gateT = 0f;

                if (gate is { Length: 2 })
                {
                    var (d, t) = SegmentDistance(a, b, gate[0], gate[1]);
                    gateOnEdge = d < gateHalf;
                    gateT = t;
                }

                for (var s = 0; s < steps; s++)
                {
                    var t = s / (float)steps;
                    if (gateOnEdge && MathF.Abs(t - gateT) * length < gateHalf)
                    {
                        continue;
                    }

                    yield return (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, heading);
                }

                if (gateOnEdge)
                {
                    foreach (var side in new[] { -1f, 1f })
                    {
                        var t = gateT + side * gateHalf / length;
                        yield return (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, heading);
                    }
                }
            }
        }

        /// <summary>Plan view, north up, one character per <paramref name="cell"/> units.</summary>
        public string RenderPlan(float cell)
        {
            var (minX, minY, maxX, maxY) = Bounds;
            var sb = new StringBuilder();
            for (var y = maxY + cell; y >= minY - cell; y -= cell)
            {
                sb.Append("    ");
                for (var x = minX - cell; x <= maxX + cell; x += cell)
                {
                    sb.Append(Glyph(x, y));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private char Glyph(float x, float y)
        {
            var outside = Outside(x, y);
            if (MathF.Abs(outside) <= config.PerimeterStripWidth && GateDistance(x, y) > config.GateWidth / 2f)
            {
                return '#';
            }

            if (outside > 0f)
            {
                return ' ';
            }

            for (var i = zones.Count - 1; i >= 0; i--)
            {
                if (SignedDistance(zones[i].Polygon, x, y) <= 0f)
                {
                    return zones[i].Zone.Name[0];
                }
            }

            return PolylineDistance(avenue, x, y) <= config.AvenueWidth / 2f ? '=' : '.';
        }

        private static float SignedDistance((float X, float Y)[] polygon, float x, float y)
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

        private static float PolylineDistance((float X, float Y)[] line, float x, float y)
        {
            var nearest = float.MaxValue;
            for (var i = 0; i + 1 < line.Length; i++)
            {
                nearest = MathF.Min(nearest, SegmentDistance(line[i], line[i + 1], x, y).Distance);
            }

            return nearest;
        }

        private static (float Distance, float T) SegmentDistance((float X, float Y) a, (float X, float Y) b, float x, float y)
        {
            var abx = b.X - a.X;
            var aby = b.Y - a.Y;
            var lengthSquared = abx * abx + aby * aby;
            var t = lengthSquared == 0f ? 0f : Math.Clamp(((x - a.X) * abx + (y - a.Y) * aby) / lengthSquared, 0f, 1f);
            var px = a.X + abx * t - x;
            var py = a.Y + aby * t - y;
            return (MathF.Sqrt(px * px + py * py), t);
        }

        /// <summary>1 well inside, 0.5 on the edge, 0 well outside, over <paramref name="feather"/>.</summary>
        private static float Fill(float signedDistance, float feather) => Smooth(0.5f - signedDistance / feather);

        private static float Smooth(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Smooth value noise in 0..1 from a fixed integer hash. Never a string hash.</summary>
        private static float ValueNoise(float x, float y)
        {
            var ix = (int)MathF.Floor(x);
            var iy = (int)MathF.Floor(y);
            var fx = Smooth(x - ix);
            var fy = Smooth(y - iy);
            var top = Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx);
            var bottom = Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx);
            return Lerp(top, bottom, fy);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)x * 374761393u + (uint)y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}

internal sealed record FairWorldMarker(string EditorId, FormKey FormKey, float X, float Y, float Heading);

internal sealed record FairWorldResult(
    FormKey WorldspaceFormKey,
    string EditorId,
    FormKey ClimateFormKey,
    int WeatherCount,
    int CellCount,
    int CellRadius,
    int PostCount,
    int MaxAlphaLayers,
    IReadOnlyList<FairWorldMarker> Markers,
    (float MinX, float MinY, float MaxX, float MaxY) CompoundBounds,
    string Plan);
