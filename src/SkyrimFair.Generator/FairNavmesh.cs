using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The fair's navmesh, generated from what the generator placed (see docs/NAVMESH.md):
/// <list type="number">
/// <item>the ground inside the palisade, rasterised;</item>
/// <item>every placed object that stands between the ground and head height cut out, by
/// its object bounds turned and scaled, padded by an actor's radius;</item>
/// <item>the largest connected area kept, split at the cell lines, and merged into
/// rectangles;</item>
/// <item>each rectangle's edge split at every other rectangle's corner on it, so no edge
/// has a T-junction, then triangulated counter-clockwise (seen from above, as every
/// vanilla navmesh is);</item>
/// <item>one NAVM a cell, with internal edge links, links across the cell lines, and the
/// lookup grid; and the navmesh info map's entries for them, written as the other
/// navmesh plugins in Barry's load order write theirs.</item>
/// </list>
/// Everything here is allocated after every other record, so no FormID moves.
/// </summary>
internal static class FairNavmesh
{
    private const float CellSize = 4096f;

    /// <summary>
    /// The value every navmesh and navmesh-info entry in Fertility Adventures (a mod with
    /// its own worldspace and 730 navmeshes) carries as its "CRC": a constant.
    /// </summary>
    private const uint NavmeshCrc = 0xA5E9A03C;

    private static readonly FormKey XMarker = FormKey.Factory("00003B:Skyrim.esm");
    private static readonly FormKey XMarkerHeading = FormKey.Factory("000034:Skyrim.esm");

    public static NavmeshResult Build(
        SkyrimMod mod, NavmeshConfig config, Worldspace world,
        IReadOnlyDictionary<(int X, int Y), Cell> cells, Cell persistentCell,
        (float MinX, float MinY, float MaxX, float MaxY) area,
        Func<float, float, float> outside, Func<float, float, float> height,
        ISkyrimModGetter master)
    {
        var res = config.Resolution;
        if (CellSize % res != 0f)
        {
            throw new InvalidOperationException($"fairWorld.navmesh.resolution {res} must divide the cell size, {CellSize}.");
        }

        // ---- the bases' bounds: Skyrim.esm, the fair's own, and any extra masters ----------
        var extras = new List<ISkyrimModDisposableGetter>();
        foreach (var path in config.ExtraMasters)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(FairPaths.ConfigDirectory, path);
            if (File.Exists(full))
            {
                extras.Add(SkyrimMod.CreateFromBinaryOverlay(full, SkyrimRelease.SkyrimSE));
            }
        }

        try
        {
            var link = new ISkyrimModGetter[] { master }.Concat(extras).Append(mod).ToImmutableLinkCache();
            var ignored = config.IgnoreBases.Select(FormKeyHelper.Parse).ToHashSet();

            // ---- the raster: inside the wall, less the obstacles ------------------------
            var ix0 = (int)MathF.Floor(area.MinX / res) - 1;
            var iy0 = (int)MathF.Floor(area.MinY / res) - 1;
            var ix1 = (int)MathF.Ceiling(area.MaxX / res) + 1;
            var iy1 = (int)MathF.Ceiling(area.MaxY / res) + 1;
            var (w, h) = (ix1 - ix0, iy1 - iy0);
            var free = new bool[w, h];
            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    var (x, y) = ((ix0 + i + 0.5f) * res, (iy0 + j + 0.5f) * res);
                    free[i, j] = outside(x, y) <= -config.WallMargin;
                }
            }

            var obstacles = 0;
            var unknown = new SortedSet<string>(StringComparer.Ordinal);
            var refs = cells.Values.SelectMany(c => c.Temporary).Concat(persistentCell.Persistent).OfType<PlacedObject>();
            foreach (var r in refs)
            {
                var p = r.Placement!.Position;
                if (p.X < area.MinX - 200f || p.X > area.MaxX + 200f || p.Y < area.MinY - 200f || p.Y > area.MaxY + 200f)
                {
                    continue;
                }

                var scale = r.Scale ?? 1f;
                float lx0, ly0, lz0, lx1, ly1, lz1;
                if (r.Primitive is { } prim)
                {
                    // Invisible walls: a box primitive, stored as half-extents.
                    (lx0, ly0, lz0, lx1, ly1, lz1) = (-prim.Bounds.X, -prim.Bounds.Y, -prim.Bounds.Z, prim.Bounds.X, prim.Bounds.Y, prim.Bounds.Z);
                }
                else
                {
                    var key = r.Base.FormKey;
                    if (key == XMarker || key == XMarkerHeading || ignored.Contains(key))
                    {
                        continue;
                    }

                    if (!link.TryResolve(key, typeof(ISkyrimMajorRecordGetter), out var baseRecord))
                    {
                        unknown.Add(key.ToString());
                        var d = config.UnknownHalfWidth;
                        (lx0, ly0, lz0, lx1, ly1, lz1) = (-d, -d, 0f, d, d, config.UnknownHeight);
                    }
                    else
                    {
                        if (baseRecord is not (IStaticGetter or IMoveableStaticGetter or IFurnitureGetter or IContainerGetter
                            or IActivatorGetter or IDoorGetter))
                        {
                            continue;  // lights, sounds, markers, plants, grass, trees
                        }

                        var b = ((IObjectBoundedGetter)baseRecord).ObjectBounds;
                        (lx0, ly0, lz0, lx1, ly1, lz1) = (b.First.X, b.First.Y, b.First.Z, b.Second.X, b.Second.Y, b.Second.Z);
                    }
                }

                var ground = height(p.X, p.Y);
                var (bottom, top) = (p.Z + lz0 * scale - ground, p.Z + lz1 * scale - ground);
                if (top < config.MinObstacleHeight || bottom > config.HeadHeight)
                {
                    continue;  // flat on the ground, or overhead
                }

                if (MathF.Max(lx1 - lx0, ly1 - ly0) * scale < config.MinFootprint)
                {
                    continue;  // small clutter an actor steps round
                }

                if (Cut(free, ix0, iy0, res, p.X, p.Y, r.Placement.Rotation.Z,
                    lx0 * scale - config.ActorRadius, ly0 * scale - config.ActorRadius,
                    lx1 * scale + config.ActorRadius, ly1 * scale + config.ActorRadius))
                {
                    obstacles++;
                }
            }

            // ---- keep the largest connected area --------------------------------------------
            var (kept, islands) = KeepLargest(free, w, h);

            // ---- rectangles, never crossing a cell line ------------------------------------------
            var rects = new List<(int X0, int Y0, int X1, int Y1, (int, int) Cell)>();
            var used = new bool[w, h];
            var perCell = (int)(CellSize / res);
            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    if (!kept[i, j] || used[i, j])
                    {
                        continue;
                    }

                    var (gx, gy) = (ix0 + i, iy0 + j);
                    var cell = (FloorDiv(gx, perCell), FloorDiv(gy, perCell));
                    var cellX1 = (cell.Item1 + 1) * perCell - ix0;
                    var cellY1 = (cell.Item2 + 1) * perCell - iy0;
                    var rw = 1;
                    while (i + rw < w && i + rw < cellX1 && rw < config.MaxRectangle && kept[i + rw, j] && !used[i + rw, j])
                    {
                        rw++;
                    }

                    var rh = 1;
                    while (j + rh < h && j + rh < cellY1 && rh < config.MaxRectangle && Row(kept, used, i, i + rw, j + rh))
                    {
                        rh++;
                    }

                    for (var b = j; b < j + rh; b++)
                    {
                        for (var a = i; a < i + rw; a++)
                        {
                            used[a, b] = true;
                        }
                    }

                    rects.Add((gx, gy, gx + rw, gy + rh, cell));
                }
            }

            // ---- triangles: each rectangle's boundary split at every corner on it -----------------
            // Vertex keys are doubled raster coordinates, so a rectangle's centre is an integer.
            var corners = new HashSet<(int, int)>();
            foreach (var r in rects)
            {
                corners.Add((r.X0 * 2, r.Y0 * 2));
                corners.Add((r.X1 * 2, r.Y0 * 2));
                corners.Add((r.X1 * 2, r.Y1 * 2));
                corners.Add((r.X0 * 2, r.Y1 * 2));
            }

            var meshes = new SortedDictionary<(int X, int Y), MeshBuilder>();
            foreach (var r in rects)
            {
                if (!meshes.TryGetValue(r.Cell, out var mb))
                {
                    meshes[r.Cell] = mb = new MeshBuilder();
                }

                var ring = new List<(int, int)>();
                for (var x = r.X0; x < r.X1; x++) Add(ring, corners, (x * 2, r.Y0 * 2), x == r.X0);   // south, west to east
                for (var y = r.Y0; y < r.Y1; y++) Add(ring, corners, (r.X1 * 2, y * 2), y == r.Y0);   // east, south to north
                for (var x = r.X1; x > r.X0; x--) Add(ring, corners, (x * 2, r.Y1 * 2), x == r.X1);   // north, east to west
                for (var y = r.Y1; y > r.Y0; y--) Add(ring, corners, (r.X0 * 2, y * 2), y == r.Y1);   // west, north to south

                if (ring.Count == 4)
                {
                    mb.Triangle(ring[0], ring[1], ring[2]);
                    mb.Triangle(ring[0], ring[2], ring[3]);
                }
                else
                {
                    var centre = (r.X0 + r.X1, r.Y0 + r.Y1);
                    for (var k = 0; k < ring.Count; k++)
                    {
                        mb.Triangle(centre, ring[k], ring[(k + 1) % ring.Count]);
                    }
                }
            }

            // ---- the NAVM records ---------------------------------------------------------------
            var navmeshes = new SortedDictionary<(int X, int Y), NavigationMesh>();
            foreach (var cellKey in meshes.Keys)
            {
                if (!cells.ContainsKey(cellKey))
                {
                    throw new InvalidOperationException($"The navmesh reaches cell {cellKey}, which the world doesn't have.");
                }

                navmeshes[cellKey] = new NavigationMesh(mod);
            }

            // Directed edges across all meshes, for the links over the cell lines.
            var edgeOwner = new Dictionary<((int, int) A, (int, int) B), ((int, int) Cell, int Tri)>();
            foreach (var (cellKey, mb) in meshes)
            {
                for (var t = 0; t < mb.Tris.Count; t++)
                {
                    var (a, b, c) = mb.Tris[t];
                    edgeOwner[(a, b)] = (cellKey, t);
                    edgeOwner[(b, c)] = (cellKey, t);
                    edgeOwner[(c, a)] = (cellKey, t);
                }
            }

            var totalTris = 0;
            var externalLinks = 0;
            var summary = new List<(int X, int Y, int Vertices, int Triangles, int Links)>();
            foreach (var (cellKey, mb) in meshes)
            {
                var nav = navmeshes[cellKey];
                var index = new Dictionary<(int, int), short>();
                var vertices = new ExtendedList<P3Float>();
                short V((int, int) key)
                {
                    if (!index.TryGetValue(key, out var v))
                    {
                        var (x, y) = (key.Item1 * res / 2f, key.Item2 * res / 2f);
                        v = (short)vertices.Count;
                        vertices.Add(new P3Float(x, y, height(x, y)));
                        index[key] = v;
                    }

                    return v;
                }

                var links = new ExtendedList<EdgeLink>();
                var triangles = new ExtendedList<NavmeshTriangle>();
                for (var t = 0; t < mb.Tris.Count; t++)
                {
                    var (a, b, c) = mb.Tris[t];
                    var flags = NavmeshTriangle.Flag.Found;
                    short Edge((int, int) from, (int, int) to, NavmeshTriangle.Flag external)
                    {
                        if (!edgeOwner.TryGetValue((to, from), out var other))
                        {
                            return -1;
                        }

                        if (other.Cell == cellKey)
                        {
                            return (short)other.Tri;
                        }

                        flags |= external;
                        links.Add(new EdgeLink
                        {
                            Mesh = new FormLink<INavigationMeshGetter>(navmeshes[other.Cell].FormKey),
                            TriangleIndex = (short)other.Tri,
                        });
                        externalLinks++;
                        return (short)(links.Count - 1);
                    }

                    var e01 = Edge(a, b, NavmeshTriangle.Flag.EdgeLink_0_1);
                    var e12 = Edge(b, c, NavmeshTriangle.Flag.EdgeLink_1_2);
                    var e20 = Edge(c, a, NavmeshTriangle.Flag.EdgeLink_2_0);
                    triangles.Add(new NavmeshTriangle
                    {
                        Vertices = new P3Int16(V(a), V(b), V(c)),
                        EdgeLink_0_1 = e01,
                        EdgeLink_1_2 = e12,
                        EdgeLink_2_0 = e20,
                        Flags = flags,
                    });
                }

                var min = new P3Float(vertices.Min(v => v.X), vertices.Min(v => v.Y), vertices.Min(v => v.Z));
                var max = new P3Float(vertices.Max(v => v.X), vertices.Max(v => v.Y), vertices.Max(v => v.Z));
                var divisor = (uint)Math.Clamp((int)Math.Ceiling(triangles.Count / 46.0), 1, 12);
                var (dx, dy) = ((max.X - min.X) / divisor, (max.Y - min.Y) / divisor);
                nav.Data = new NavigationMeshData
                {
                    NavmeshVersion = 12,
                    CrcHash = NavmeshCrc,
                    Parent = new WorldspaceNavmeshParent
                    {
                        Parent = new FormLink<IWorldspaceGetter>(world.FormKey),
                        // (Y, X) in Mutagen's naming, as every vanilla navmesh reads.
                        Coordinates = new P2Int16((short)cellKey.Y, (short)cellKey.X),
                    },
                    Vertices = vertices,
                    Triangles = triangles,
                    EdgeLinks = links,
                    NavmeshGridDivisor = divisor,
                    MaxDistanceX = dx,
                    MaxDistanceY = dy,
                    Min = min,
                    Max = max,
                    NavmeshGrid = Grid(vertices, triangles, min, dx, dy, (int)divisor),
                };
                nav.IsCompressed = true;
                cells[cellKey].NavigationMeshes.Add(nav);
                totalTris += triangles.Count;
                summary.Add((cellKey.X, cellKey.Y, vertices.Count, triangles.Count, links.Count));
            }

            // ---- the navmesh info map ---------------------------------------------------------------
            var source = master.NavigationMeshInfoMaps.FirstOrDefault(n => n.FormKey == FormKeyHelper.Parse(config.InfoMap))
                ?? throw new InvalidOperationException($"No navmesh info map {config.InfoMap} in Skyrim.esm.");
            var map = source.DeepCopy();
            // Form version 44, as the navmesh plugins' overrides carry (vanilla's own is 40).
            map.FormVersion = 44;
            var seeds = config.InfoMapSeeds.Select(FormKeyHelper.Parse).ToHashSet();
            var kept2 = map.MapInfos.Where(i => seeds.Contains(i.NavigationMesh.FormKey)).ToList();
            map.MapInfos.Clear();
            map.MapInfos.AddRange(kept2);
            foreach (var (cellKey, nav) in navmeshes)
            {
                var d = nav.Data!;
                var t0 = d.Triangles[0];
                var point = new P3Float(
                    (d.Vertices[t0.Vertices.X].X + d.Vertices[t0.Vertices.Y].X + d.Vertices[t0.Vertices.Z].X) / 3f,
                    (d.Vertices[t0.Vertices.X].Y + d.Vertices[t0.Vertices.Y].Y + d.Vertices[t0.Vertices.Z].Y) / 3f,
                    (d.Vertices[t0.Vertices.X].Z + d.Vertices[t0.Vertices.Y].Z + d.Vertices[t0.Vertices.Z].Z) / 3f);
                map.MapInfos.Add(new NavigationMapInfo
                {
                    NavigationMesh = new FormLink<INavigationMeshGetter>(nav.FormKey),
                    Unknown = 0,
                    Point = point,
                    PreferredMergesFlag = 0,
                    Unknown2 = unchecked((int)NavmeshCrc),
                    Parent = new NavigationMapInfoWorldParent
                    {
                        ParentWorldspace = new FormLink<IWorldspaceGetter>(world.FormKey),
                        ParentWorldspaceCoord = new P2Int16((short)cellKey.Y, (short)cellKey.X),
                    },
                });
            }

            mod.NavigationMeshInfoMaps.Add(map);

            // ---- where the actors stand, against the mesh -------------------------------------------
            var actors = cells.Values.SelectMany(c => c.Temporary).Concat(persistentCell.Persistent).OfType<PlacedNpc>()
                .Select(a => a.Placement!.Position)
                .Where(p => p.X >= area.MinX && p.X <= area.MaxX && p.Y >= area.MinY && p.Y <= area.MaxY)
                .ToList();
            var onMesh = actors.Count(p =>
            {
                var (i, j) = ((int)MathF.Floor(p.X / res) - ix0, (int)MathF.Floor(p.Y / res) - iy0);
                return i >= 0 && j >= 0 && i < w && j < h && kept[i, j];
            });

            return new NavmeshResult(navmeshes.Count, totalTris, externalLinks, obstacles, islands,
                actors.Count, onMesh, unknown.ToList(), summary, kept, ix0, iy0, res);
        }
        finally
        {
            foreach (var e in extras)
            {
                e.Dispose();
            }
        }
    }

    /// <summary>Marks the raster cells whose centres fall in a turned rectangle as blocked.</summary>
    private static bool Cut(bool[,] free, int ix0, int iy0, float res, float px, float py, float yaw,
        float x0, float y0, float x1, float y1)
    {
        var (c, s) = (MathF.Cos(yaw), MathF.Sin(yaw));
        var reach = MathF.Sqrt(MathF.Max(x0 * x0, x1 * x1) + MathF.Max(y0 * y0, y1 * y1));
        var (i0, i1) = ((int)MathF.Floor((px - reach) / res) - ix0, (int)MathF.Ceiling((px + reach) / res) - ix0);
        var (j0, j1) = ((int)MathF.Floor((py - reach) / res) - iy0, (int)MathF.Ceiling((py + reach) / res) - iy0);
        var any = false;
        for (var j = Math.Max(0, j0); j <= Math.Min(free.GetLength(1) - 1, j1); j++)
        {
            for (var i = Math.Max(0, i0); i <= Math.Min(free.GetLength(0) - 1, i1); i++)
            {
                var (dx, dy) = ((ix0 + i + 0.5f) * res - px, (iy0 + j + 0.5f) * res - py);
                // World to local: the inverse of local (u, v) -> (u cos + v sin, -u sin + v cos).
                var u = dx * c - dy * s;
                var v = dx * s + dy * c;
                if (u >= x0 && u <= x1 && v >= y0 && v <= y1)
                {
                    free[i, j] = false;
                    any = true;
                }
            }
        }

        return any;
    }

    private static (bool[,] Kept, int Islands) KeepLargest(bool[,] free, int w, int h)
    {
        var label = new int[w, h];
        var sizes = new List<int> { 0 };
        var stack = new Stack<(int, int)>();
        for (var j = 0; j < h; j++)
        {
            for (var i = 0; i < w; i++)
            {
                if (!free[i, j] || label[i, j] != 0)
                {
                    continue;
                }

                var id = sizes.Count;
                sizes.Add(0);
                stack.Push((i, j));
                label[i, j] = id;
                while (stack.Count > 0)
                {
                    var (a, b) = stack.Pop();
                    sizes[id]++;
                    foreach (var (na, nb) in new[] { (a + 1, b), (a - 1, b), (a, b + 1), (a, b - 1) })
                    {
                        if (na >= 0 && nb >= 0 && na < w && nb < h && free[na, nb] && label[na, nb] == 0)
                        {
                            label[na, nb] = id;
                            stack.Push((na, nb));
                        }
                    }
                }
            }
        }

        var best = 1;
        for (var k = 2; k < sizes.Count; k++)
        {
            if (sizes[k] > sizes[best])
            {
                best = k;
            }
        }

        var kept = new bool[w, h];
        for (var j = 0; j < h; j++)
        {
            for (var i = 0; i < w; i++)
            {
                kept[i, j] = sizes.Count > 1 && label[i, j] == best;
            }
        }

        return (kept, sizes.Count - 1);
    }

    private static bool Row(bool[,] kept, bool[,] used, int i0, int i1, int j)
    {
        for (var i = i0; i < i1; i++)
        {
            if (!kept[i, j] || used[i, j])
            {
                return false;
            }
        }

        return true;
    }

    private static void Add(List<(int, int)> ring, HashSet<(int, int)> corners, (int, int) point, bool isCorner)
    {
        if (isCorner || corners.Contains(point))
        {
            ring.Add(point);
        }
    }

    private static int FloorDiv(int a, int b) => (int)Math.Floor(a / (double)b);

    /// <summary>
    /// The lookup grid, as vanilla stores it: divisor x divisor cells, row by row
    /// (index = y * divisor + x), each a uint32 count and that many uint16 triangle indices.
    /// </summary>
    private static byte[] Grid(IReadOnlyList<P3Float> v, IReadOnlyList<NavmeshTriangle> tris, P3Float min, float dx, float dy, int divisor)
    {
        var cells = Enumerable.Range(0, divisor * divisor).Select(_ => new List<ushort>()).ToArray();
        for (var t = 0; t < tris.Count; t++)
        {
            var (a, b, c) = (v[tris[t].Vertices.X], v[tris[t].Vertices.Y], v[tris[t].Vertices.Z]);
            int G(float value, float origin, float step) => Math.Clamp((int)MathF.Floor((value - origin) / step), 0, divisor - 1);
            var (gx0, gx1) = (G(MathF.Min(a.X, MathF.Min(b.X, c.X)), min.X, dx), G(MathF.Max(a.X, MathF.Max(b.X, c.X)), min.X, dx));
            var (gy0, gy1) = (G(MathF.Min(a.Y, MathF.Min(b.Y, c.Y)), min.Y, dy), G(MathF.Max(a.Y, MathF.Max(b.Y, c.Y)), min.Y, dy));
            for (var gy = gy0; gy <= gy1; gy++)
            {
                for (var gx = gx0; gx <= gx1; gx++)
                {
                    cells[gy * divisor + gx].Add((ushort)t);
                }
            }
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        foreach (var list in cells)
        {
            bw.Write(list.Count);
            foreach (var t in list)
            {
                bw.Write(t);
            }
        }

        bw.Flush();
        return ms.ToArray();
    }

    private sealed class MeshBuilder
    {
        public List<((int, int) A, (int, int) B, (int, int) C)> Tris { get; } = new();

        public void Triangle((int, int) a, (int, int) b, (int, int) c) => Tris.Add((a, b, c));
    }
}

internal sealed record NavmeshResult(
    int Meshes, int Triangles, int ExternalLinks, int Obstacles, int Islands,
    int Actors, int ActorsOnMesh, IReadOnlyList<string> UnknownBases,
    IReadOnlyList<(int X, int Y, int Vertices, int Triangles, int Links)> Cells,
    bool[,] Walkable, int OriginX, int OriginY, float Resolution);
