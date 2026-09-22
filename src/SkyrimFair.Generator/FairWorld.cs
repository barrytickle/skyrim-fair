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
    /// Is Full LOD: never fades and draws beyond the loaded cells. Vanilla gives it, with
    /// Persistent, to the distant scenery in its small worlds.
    /// </summary>
    private const int FullLodRecordFlag = 0x10000;

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
            var marker = Place(mod, markerBase, x, y, config.FloorZ + zone.MarkerHeight, heading);
            marker.EditorID = $"{config.EditorId}{zone.Name}Marker";
            marker.MajorRecordFlagsRaw = PersistentRecordFlag;
            topCell.Persistent.Add(marker);
            markers.Add(new FairWorldMarker(marker.EditorID, marker.FormKey, x, y, heading));
        }

        worldspace.TopCell = topCell;

        // ---- exterior cells, each with its own landscape --------------------------
        // Cells and LAND are allocated before anything placed in them, so their
        // FormIDs do not move whenever the wall or forest changes.
        var grid = new ExteriorCellGrid(worldspace);
        var cells = new Dictionary<(int X, int Y), Cell>();
        var textures = plan.PaintTextures();
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
                grid.Add(cell, cx, cy);
                cells[(cx, cy)] = cell;
            }
        }

        void Put(PlacedObject placed)
        {
            var pos = placed.Placement!.Position;
            var key = ((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize));
            if (!cells.TryGetValue(key, out var cell))
            {
                throw new InvalidOperationException(
                    $"FairWorld reference at {pos.X:0}, {pos.Y:0} falls outside the generated cells; raise CellRadius.");
            }

            cell.Temporary.Add(placed);
        }

        // ---- the palisade and its gate ---------------------------------------------
        var panelStatic = AddStatic(mod, config.Palisade);
        var gateStatic = AddStatic(mod, config.GatePiece);

        var gatePiece = config.GatePiece;
        var facing = config.Zones.First(z => z.Name == config.GateFacesZone).Marker;
        var gateHeading = MathF.Atan2(facing[0] - config.Gate[0], facing[1] - config.Gate[1]) * 180f / MathF.PI
            + gatePiece.YawOffsetDegrees;
        var gate = Place(
            mod, gateStatic.FormKey, config.Gate[0], config.Gate[1],
            plan.Height(config.Gate[0], config.Gate[1]) - gatePiece.Sink, gateHeading);
        gate.EditorID = $"{config.EditorId}MainGate";
        gate.Scale = gatePiece.Scale;
        Put(gate);

        var panels = plan.WallPanels(config.Palisade, gatePiece.Width * gatePiece.Scale / 2f).ToList();
        foreach (var panel in panels)
        {
            var placed = Place(mod, panelStatic.FormKey, panel.X, panel.Y, panel.Z, panel.Heading);
            placed.Scale = panel.Scale;
            Put(placed);
        }

        // ---- the main stage ---------------------------------------------------------
        var stage = config.Stage.Enabled ? FairStage.Build(mod, config, plan.Height, Put) : null;

        // ---- project statics, used by market modules as @EditorID -----------------------
        var projectStatics = config.ProjectStatics.ToDictionary(ps => ps.EditorId, ps => AddStatic(mod, ps).FormKey);
        FormKey Resolve(string piece) => piece.StartsWith('@')
            ? projectStatics.TryGetValue(piece[1..], out var key)
                ? key
                : throw new InvalidOperationException($"Market piece {piece} is not a configured project static.")
            : FormKeyHelper.Parse(piece);

        // ---- the fair's own face lists -------------------------------------------------
        // Vendors and archers take their looks from vanilla commoner leveled lists. Other
        // mods edit those lists (Dawi NPC Encounters adds its race to the male one), so the
        // fair copies each list's Skyrim.esm entries into a record of its own, which no
        // other mod touches.
        var faceLists = new Dictionary<string, FormKey>();
        FormKey FaceList(string template)
        {
            if (faceLists.TryGetValue(template, out var own))
            {
                return own;
            }

            var key = FormKeyHelper.Parse(template);
            var vanilla = master?.LeveledNpcs.FirstOrDefault(l => l.FormKey == key);
            if (vanilla is null)
            {
                return faceLists[template] = key;
            }

            var copy = vanilla.Duplicate(mod.GetNextFormKey());
            copy.EditorID = $"SkyrimFairFaces{vanilla.EditorID}";
            mod.LeveledNpcs.Add(copy);
            return faceLists[template] = copy.FormKey;
        }

        // ---- the market ---------------------------------------------------------------
        var market = config.Market.Enabled
            ? FairMarket.Build(mod, config, plan.Outside, plan.Height, Put, topCell, PersistentRecordFlag, Resolve)
            : null;

        // ---- stall-keepers ------------------------------------------------------------
        VendorsResult? vendors = null;
        if (market is not null && config.Vendors.Enabled)
        {
            vendors = FairVendors.Build(mod, config.Vendors, market.Stalls, plan.Height, npc =>
            {
                var pos = npc.Placement!.Position;
                cells[((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize))].Temporary.Add(npc);
            }, FaceList);
        }

        // ---- archery range ------------------------------------------------------------
        ArcheryResult? archery = null;
        if (config.Archery.Enabled)
        {
            archery = FairArchery.Build(mod, config.Archery, plan.Height, Put, npc =>
            {
                var pos = npc.Placement!.Position;
                cells[((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize))].Temporary.Add(npc);
            }, FaceList);
        }

        // ---- distant mountains ------------------------------------------------------
        var mountains = new List<MountainPlacement>();
        if (config.Mountains.Enabled)
        {
            if (master is null)
            {
                throw new InvalidOperationException(
                    "FairWorld mountains are sunk by their mesh bounds, which are read from Skyrim.esm; " +
                    "set Site.SkyrimDataPath or disable fairWorld.mountains.");
            }

            var lowest = master.Statics.ToDictionary(r => r.FormKey, r => (float)r.ObjectBounds.First.Z);
            var worldMin = config.CellRadius * -CellSize;
            var worldMax = (config.CellRadius + 1) * CellSize;
            mountains = plan.Mountains(config.Mountains, key => lowest.TryGetValue(key, out var z)
                ? z
                : throw new InvalidOperationException($"Mountain {key} is not a STAT in Skyrim.esm.")).ToList();

            foreach (var mountain in mountains)
            {
                // Vanilla keeps its always-drawn scenery inside the world's object bounds.
                if (mountain.X < worldMin || mountain.X >= worldMax || mountain.Y < worldMin || mountain.Y >= worldMax)
                {
                    throw new InvalidOperationException(
                        $"Mountain at {mountain.X:0}, {mountain.Y:0} falls outside the world's bounds; reduce its row radius.");
                }

                var placed = Place(mod, mountain.Base, mountain.X, mountain.Y, mountain.Z, mountain.Heading);
                placed.Scale = mountain.Scale;
                placed.MajorRecordFlagsRaw = PersistentRecordFlag | FullLodRecordFlag;
                topCell.Persistent.Add(placed);
            }
        }

        // ---- forest backdrop ---------------------------------------------------------
        var trees = config.Forest.Enabled ? plan.ForestTrees(config.Forest, gateHeading).ToList() : new List<TreePlacement>();
        foreach (var tree in trees)
        {
            var placed = Place(mod, tree.Base, tree.X, tree.Y, tree.Z, tree.Heading);
            placed.Placement!.Rotation = new P3Float(
                tree.LeanX * MathF.PI / 180f, tree.LeanY * MathF.PI / 180f, tree.Heading * MathF.PI / 180f);
            placed.Scale = tree.Scale;
            Put(placed);
        }

        mod.Worldspaces.Add(worldspace);

        return new FairWorldResult(
            worldspace.FormKey,
            config.EditorId,
            climateKey,
            weatherCount,
            cells.Count,
            config.CellRadius,
            maxLayers,
            markers,
            plan.Bounds,
            new FairWorldWall(
                panelStatic.FormKey, gateStatic.FormKey, gate.FormKey, panels.Count,
                config.Palisade.Width * config.Palisade.Scale, config.Palisade.Height * config.Palisade.Scale,
                gatePiece.Height * gatePiece.Scale, gateHeading),
            trees
                .GroupBy(t => t.Name)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new FairWorldTreeCount(
                    g.Key, g.Count(), g.Min(t => t.Scale), g.Max(t => t.Scale),
                    g.Min(t => t.Distance), g.Max(t => t.Distance)))
                .ToList(),
            mountains,
            stage,
            market,
            vendors,
            archery,
            plan.RenderPlan(512f, 0f, Array.Empty<TreePlacement>()),
            plan.RenderPlan(1024f, config.Forest.OuterDistance, trees),
            plan.RenderMountains(mountains, 2048f));
    }

    /// <summary>
    /// A STAT for a project mesh, with object bounds taken from its measured size so the
    /// engine culls it correctly.
    /// </summary>
    private static Static AddStatic(SkyrimMod mod, ProjectStaticConfig piece)
    {
        var halfWidth = (short)MathF.Ceiling(piece.Width / 2f);
        var halfDepth = (short)MathF.Ceiling(piece.Depth / 2f);
        var record = new Static(mod)
        {
            EditorID = piece.EditorId,
            Model = new Model { File = piece.Model },
            ObjectBounds = new ObjectBounds
            {
                First = new P3Int16((short)-halfWidth, (short)-halfDepth, 0),
                Second = new P3Int16(halfWidth, halfDepth, (short)MathF.Ceiling(piece.Height)),
            },
        };
        mod.Statics.Add(record);
        return record;
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
        /// Palisade panels, edge by edge round the outline. Each edge's run carries a
        /// little past both vertices so corners close, is split either side of the gate
        /// with its ends tucked into the gate posts, and is filled with the fewest panels
        /// that still overlap by <see cref="PalisadeConfig.Overlap"/>, spread evenly so no
        /// short filler panel is needed. Every panel then wanders slightly in yaw, line,
        /// scale and sink, and some are turned round, all from a fixed integer hash.
        /// </summary>
        public IEnumerable<WallPanel> WallPanels(PalisadeConfig wall, float gateHalfWidth)
        {
            var width = wall.Width * wall.Scale;
            var pitch = width * (1f - wall.Overlap);

            for (var i = 0; i < perimeter.Length; i++)
            {
                var a = perimeter[i];
                var b = perimeter[(i + 1) % perimeter.Length];
                var length = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var (ux, uy) = ((b.X - a.X) / length, (b.Y - a.Y) / length);
                var edgeHeading = MathF.Atan2(ux, uy) * 180f / MathF.PI;

                var runs = new List<(float From, float To)>();
                var (gateDistance, gateT) = SegmentDistance(a, b, config.Gate[0], config.Gate[1]);
                if (gateDistance < 1f)
                {
                    var at = gateT * length;
                    runs.Add((-wall.CornerExtension, at - gateHalfWidth + wall.GateTuck));
                    runs.Add((at + gateHalfWidth - wall.GateTuck, length + wall.CornerExtension));
                }
                else
                {
                    runs.Add((-wall.CornerExtension, length + wall.CornerExtension));
                }

                for (var r = 0; r < runs.Count; r++)
                {
                    var (from, to) = runs[r];
                    var span = to - from;
                    var count = span <= width ? 1 : (int)MathF.Ceiling((span - width) / pitch) + 1;
                    var run = i * 16 + r;
                    for (var k = 0; k < count; k++)
                    {
                        var along = count == 1 ? (from + to) / 2f : from + width / 2f + k * (span - width) / (count - 1);
                        var offset = (Hash3(run, k, 1) * 2f - 1f) * wall.OffsetJitter;
                        var x = a.X + ux * along + uy * offset;
                        var y = a.Y + uy * along - ux * offset;
                        var flip = Hash3(run, k, 5) < wall.FlipChance ? 180f : 0f;

                        // The panel spans local X, so it turns a quarter from the edge heading.
                        yield return new WallPanel(
                            x, y,
                            Height(x, y) - Hash3(run, k, 4) * wall.SinkMax,
                            edgeHeading - 90f + flip + (Hash3(run, k, 2) * 2f - 1f) * wall.YawJitterDegrees,
                            wall.Scale * (1f + (Hash3(run, k, 3) * 2f - 1f) * wall.ScaleJitter));
                    }
                }
            }
        }

        /// <summary>
        /// Scenery conifers in the band outside the wall: a jittered grid of candidates,
        /// each kept by chance against a density that rises from sparse by the wall to
        /// dense a little way out and thins toward the outer edge, multiplied by a
        /// clustering field whose low values are clearings. Nothing stands near the gate
        /// or in the open approach in front of it.
        /// </summary>
        public IEnumerable<TreePlacement> ForestTrees(ForestConfig forest, float gateHeading)
        {
            var species = forest.Trees
                .Select(t => (Tree: t, Key: FormKeyHelper.Parse(t.FormKey)))
                .ToList();
            var nearest = forest.Trees.Min(t => t.MinDistance);
            var (minX, minY, maxX, maxY) = Bounds;
            var reach = forest.OuterDistance;

            // The approach runs out of the gate, away from what it faces.
            var outward = (gateHeading + 180f) * MathF.PI / 180f;
            var (ox, oy) = (MathF.Sin(outward), MathF.Cos(outward));
            var coneCos = MathF.Cos(forest.GateApproachDegrees * MathF.PI / 180f);

            var gx0 = (int)MathF.Floor((minX - reach) / forest.GridSpacing);
            var gx1 = (int)MathF.Ceiling((maxX + reach) / forest.GridSpacing);
            var gy0 = (int)MathF.Floor((minY - reach) / forest.GridSpacing);
            var gy1 = (int)MathF.Ceiling((maxY + reach) / forest.GridSpacing);

            for (var gy = gy0; gy <= gy1; gy++)
            {
                for (var gx = gx0; gx <= gx1; gx++)
                {
                    var x = (gx + 0.5f + (Hash3(gx, gy, 11) * 2f - 1f) * forest.Jitter) * forest.GridSpacing;
                    var y = (gy + 0.5f + (Hash3(gx, gy, 12) * 2f - 1f) * forest.Jitter) * forest.GridSpacing;
                    var distance = Outside(x, y);
                    if (distance < nearest || distance > forest.OuterDistance)
                    {
                        continue;
                    }

                    var gdx = x - config.Gate[0];
                    var gdy = y - config.Gate[1];
                    var fromGate = MathF.Sqrt(gdx * gdx + gdy * gdy);
                    if (fromGate < forest.GateClearRadius
                        || (fromGate < forest.GateApproachLength && (gdx * ox + gdy * oy) / fromGate > coneCos))
                    {
                        continue;
                    }

                    var cluster = ValueNoise(x / forest.ClusterPeriod + 41.7f, y / forest.ClusterPeriod - 23.9f) * 0.7f
                        + ValueNoise(x / (forest.ClusterPeriod * 0.45f) - 8.2f, y / (forest.ClusterPeriod * 0.45f) + 3.3f) * 0.3f;
                    if (cluster < forest.ClearingThreshold)
                    {
                        continue;
                    }

                    var rampIn = Smooth((distance - nearest) / MathF.Max(1f, forest.DenseFrom - nearest));
                    var fadeOut = 1f - 0.75f * Smooth((distance - forest.DenseTo) / MathF.Max(1f, forest.OuterDistance - forest.DenseTo));
                    var clump = Smooth((cluster - forest.ClearingThreshold) / 0.22f);
                    var chance = forest.PeakDensity * (0.25f + 0.75f * rampIn) * fadeOut * clump;
                    if (Hash3(gx, gy, 13) >= chance)
                    {
                        continue;
                    }

                    var allowed = species
                        .Where(s => distance >= s.Tree.MinDistance && distance <= s.Tree.MaxDistance)
                        .ToList();
                    if (allowed.Count == 0)
                    {
                        continue;
                    }

                    var pick = Hash3(gx, gy, 14) * allowed.Sum(s => s.Tree.Weight);
                    var chosen = allowed[^1];
                    foreach (var s in allowed)
                    {
                        pick -= s.Tree.Weight;
                        if (pick < 0f)
                        {
                            chosen = s;
                            break;
                        }
                    }

                    var tree = chosen.Tree;
                    yield return new TreePlacement(
                        chosen.Key, tree.Name, x, y,
                        Height(x, y) - forest.Sink,
                        Hash3(gx, gy, 15) * 360f,
                        (Hash3(gx, gy, 16) * 2f - 1f) * forest.LeanDegrees,
                        (Hash3(gx, gy, 17) * 2f - 1f) * forest.LeanDegrees,
                        tree.MinScale + Hash3(gx, gy, 18) * (tree.MaxScale - tree.MinScale),
                        distance);
                }
            }
        }

        /// <summary>
        /// Distant mountains, row by row, round the compound centre: evenly spaced in
        /// angle with each piece wandering by up to <see cref="MountainRow.AngleJitter"/> of
        /// the spacing, at a radius anywhere in the row's band, turned at random, and
        /// sunk so the mesh's lowest point lands on <see cref="MountainsConfig.BaseZ"/>.
        /// </summary>
        public IEnumerable<MountainPlacement> Mountains(MountainsConfig mountains, Func<FormKey, float> lowestPoint)
        {
            var (minX, minY, maxX, maxY) = Bounds;
            var (cx, cy) = ((minX + maxX) / 2f, (minY + maxY) / 2f);

            for (var r = 0; r < mountains.Rows.Count; r++)
            {
                var row = mountains.Rows[r];
                var pieces = row.Pieces.Select(p => (Piece: p, Key: FormKeyHelper.Parse(p.FormKey))).ToList();
                var totalWeight = pieces.Sum(p => p.Piece.Weight);
                var spacing = 360f / row.Count;

                for (var k = 0; k < row.Count; k++)
                {
                    var angle = row.StartDegrees + (k + (Hash3(r, k, 21) * 2f - 1f) * row.AngleJitter) * spacing;
                    var radius = row.MinRadius + Hash3(r, k, 22) * (row.MaxRadius - row.MinRadius);
                    var rad = angle * MathF.PI / 180f;
                    var x = cx + MathF.Sin(rad) * radius;
                    var y = cy + MathF.Cos(rad) * radius;

                    var pick = Hash3(r, k, 23) * totalWeight;
                    var chosen = pieces[^1];
                    foreach (var p in pieces)
                    {
                        pick -= p.Piece.Weight;
                        if (pick < 0f)
                        {
                            chosen = p;
                            break;
                        }
                    }

                    var scale = chosen.Piece.MinScale + Hash3(r, k, 24) * (chosen.Piece.MaxScale - chosen.Piece.MinScale);
                    yield return new MountainPlacement(
                        chosen.Key, chosen.Piece.Name, row.Name, x, y,
                        mountains.BaseZ - lowestPoint(chosen.Key) * scale,
                        Hash3(r, k, 25) * 360f, scale, angle, radius);
                }
            }
        }

        /// <summary>Mountains on a coarse plan, north up: <c>n</c> near row, <c>M</c> far row, <c>o</c> the compound.</summary>
        public string RenderMountains(IReadOnlyList<MountainPlacement> mountains, float cell)
        {
            if (mountains.Count == 0)
            {
                return string.Empty;
            }

            var (minX, minY, maxX, maxY) = Bounds;
            var reach = mountains.Max(m => m.Radius) + cell;
            var (cx, cy) = ((minX + maxX) / 2f, (minY + maxY) / 2f);
            var size = (int)MathF.Ceiling(reach * 2f / cell);
            var marks = new Dictionary<(int, int), char>();
            foreach (var m in mountains)
            {
                marks[((int)((m.X - cx + reach) / cell), (int)((cy + reach - m.Y) / cell))] =
                    m.Row == mountains[0].Row ? 'n' : 'M';
            }

            var sb = new StringBuilder();
            for (var row = 0; row < size; row++)
            {
                sb.Append("    ");
                for (var col = 0; col < size; col++)
                {
                    var x = cx - reach + (col + 0.5f) * cell;
                    var y = cy + reach - (row + 0.5f) * cell;
                    sb.Append(marks.TryGetValue((col, row), out var c) ? c : Outside(x, y) <= 0f ? 'o' : '.');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Plan view, north up, one character per <paramref name="cell"/> units, padded by
        /// <paramref name="margin"/>. <c>^</c> marks a character holding at least one tree.
        /// </summary>
        public string RenderPlan(float cell, float margin, IReadOnlyList<TreePlacement> trees)
        {
            var (minX, minY, maxX, maxY) = Bounds;
            minX -= margin + cell;
            minY -= margin + cell;
            maxX += margin + cell;
            maxY += margin + cell;
            var columns = (int)MathF.Floor((maxX - minX) / cell) + 1;
            var rows = (int)MathF.Floor((maxY - minY) / cell) + 1;
            var wooded = trees
                .Select(t => ((int)MathF.Floor((t.X - minX) / cell), (int)MathF.Floor((maxY - t.Y) / cell)))
                .ToHashSet();

            var sb = new StringBuilder();
            for (var row = 0; row < rows; row++)
            {
                sb.Append("    ");
                for (var col = 0; col < columns; col++)
                {
                    var glyph = Glyph(minX + (col + 0.5f) * cell, maxY - (row + 0.5f) * cell);
                    sb.Append(glyph == ' ' && wooded.Contains((col, row)) ? '^' : glyph);
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        private char Glyph(float x, float y)
        {
            var outside = Outside(x, y);
            if (GateDistance(x, y) <= config.GateWidth / 2f)
            {
                return 'G';
            }

            if (MathF.Abs(outside) <= config.PerimeterStripWidth)
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
            => FairGeometry.SignedDistance(polygon, x, y);

        private static float PolylineDistance((float X, float Y)[] line, float x, float y)
            => FairGeometry.PolylineDistance(line, x, y);

        private static (float Distance, float T) SegmentDistance((float X, float Y) a, (float X, float Y) b, float x, float y)
            => FairGeometry.SegmentDistance(a, b, x, y);

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

        private static float Hash3(int a, int b, int salt) => FairHash.Hash3(a, b, salt);

        private static float Hash(int x, int y) => FairHash.Hash(x, y);
    }
}

internal sealed record FairWorldMarker(string EditorId, FormKey FormKey, float X, float Y, float Heading);

internal sealed record WallPanel(float X, float Y, float Z, float Heading, float Scale);

internal sealed record TreePlacement(
    FormKey Base, string Name, float X, float Y, float Z,
    float Heading, float LeanX, float LeanY, float Scale, float Distance);

internal sealed record FairWorldWall(
    FormKey PanelStatic,
    FormKey GateStatic,
    FormKey GateReference,
    int PanelCount,
    float PanelWidth,
    float PanelHeight,
    float GateHeight,
    float GateHeading);

internal sealed record MountainPlacement(
    FormKey Base, string Name, string Row, float X, float Y, float Z,
    float Heading, float Scale, float Bearing, float Radius);

internal sealed record FairWorldTreeCount(
    string Name, int Count, float MinScale, float MaxScale, float MinDistance, float MaxDistance);

internal sealed record FairWorldResult(
    FormKey WorldspaceFormKey,
    string EditorId,
    FormKey ClimateFormKey,
    int WeatherCount,
    int CellCount,
    int CellRadius,
    int MaxAlphaLayers,
    IReadOnlyList<FairWorldMarker> Markers,
    (float MinX, float MinY, float MaxX, float MaxY) CompoundBounds,
    FairWorldWall Wall,
    IReadOnlyList<FairWorldTreeCount> Trees,
    IReadOnlyList<MountainPlacement> Mountains,
    StageResult? Stage,
    MarketResult? Market,
    VendorsResult? Vendors,
    ArcheryResult? Archery,
    string Plan,
    string ForestPlan,
    string MountainPlan);
