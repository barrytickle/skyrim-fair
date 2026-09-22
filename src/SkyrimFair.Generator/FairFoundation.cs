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

    /// <summary>Fixed per-edge seed offsets, so each edge draws its own reproducible stream.</summary>
    private static readonly Dictionary<string, int> EdgeSalt = new()
    {
        ["N"] = 101,
        ["S"] = 211,
        ["E"] = 307,
        ["W"] = 419,
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
        Func<FormKey, PieceBounds> boundsOf,
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

        // UV phase variants only exist to stop a small texture period stamping
        // visibly. The vanilla Whiterun floor repeats every 256 units, which divides
        // the 512 grid exactly, so phasing it would break continuity instead of
        // helping and the variants are not built. Fall back to the base role rather
        // than requiring every mode to declare all four.
        string PhasedRole(string baseRole, int col, int row)
        {
            var u = ((col % 2) + 2) % 2;
            var v = ((row % 2) + 2) % 2;
            var candidate = (u, v) switch
            {
                (1, 0) => baseRole + "U1",
                (0, 1) => baseRole + "V1",
                (1, 1) => baseRole + "U1V1",
                _ => baseRole,
            };
            return f.Pieces.ContainsKey(candidate) ? candidate : baseRole;
        }

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

        foreach (var (col, row) in paved)
        {
            var (px, py) = Centre(col, row);
            result.PavedRects.Add((px - tile / 2f, py - tile / 2f, px + tile / 2f, py + tile / 2f));
        }

        void Put(string role, float x, float y, float z, float rotZ, float scale = 1f)
        {
            var baseRecord = StaticFor(role);
            var placed = new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(baseRecord.FormKey),
                Scale = MathF.Abs(scale - 1f) < 1e-4f ? null : scale,
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
                var fx = (a.X + b.X) / 2f;
                var fy = (a.Y + b.Y) / 2f;
                Put("floorFill", fx, fy, f.FloorZ, 0f);
                Put("paveCapFill", fx, fy, f.FloorZ, 0f);
            }
        }

        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            if (used.Contains((col, row)))
            {
                continue;
            }

            var (x, y) = Centre(col, row);
            Put(PhasedRole("floorEdge", col, row), x, y, f.FloorZ, 0f);
            Put(PhasedRole("paveCapEdge", col, row), x, y, f.FloorZ, 0f);
        }

        // ---- perimeter ----------------------------------------------------
        // Ramp segments are reserved first so no retaining wall blocks the way in.
        var rampSegments = ChooseRampSegments(paved, f, cols, rows);

        // Every placed ramp tile, kept so the naturalisation pass can face the ramp's
        // flanks and keep its walking channel clear.
        var rampTiles = new List<(float X, float Y, float Z, int Dc, int Dr, bool Stair)>();

        // Perimeter segments that got a retaining face, with the exposure measured at
        // the face. The embankment pass works from these rather than re-deriving them.
        var faced = new List<(float Ex, float Ey, int Dc, int Dr, float Rot, float SurfaceZ, float Drop)>();

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
                    var e = f.Entrance;
                    var slot0Z = f.FloorZ;
                    var firstRamp = 0;

                    if (e.UseStairs)
                    {
                        // A chain of vanilla stair flights, nose to tail, which reads as
                        // one long staircase climbing the terrace rather than a ramp.
                        //
                        // Measured off the shipped mesh: the treads run from local
                        // Y -256 at Z 18 up to Y -64 at Z 130, so one flight is 112 of
                        // rise over 192 of run, about 30 degrees. Turning the piece to
                        // face outward and stepping each flight one run further out and
                        // one drop lower puts the top tread of each on the bottom tread
                        // of the one above, with no landing needed.
                        //
                        // Each flight brings its own 512-wide drystone wall, so the
                        // chain also builds the stepped retaining tiers either side of
                        // the steps.
                        var stairDrop = e.StairDrop * e.StairScale;
                        var stairRun = e.StairRun * e.StairScale;
                        for (var i = 0; i < e.StairFlights; i++)
                        {
                            if (e.KitStair)
                            {
                                // The project-authored flight: steps only, no wall, box
                                // collider on the nosing line built in. Its origin IS the
                                // top tread, it descends in local +Y, so it takes the
                                // plain outward rotation and no inset. Nothing else is
                                // needed for it to be climbable.
                                Put("stair",
                                    ex + dc * (stairRun * i),
                                    ey - dr * (stairRun * i),
                                    f.FloorZ - stairDrop * i,
                                    rot,
                                    e.StairScale);
                                result.EntrancePieces++;
                                rampTiles.Add((ex + dc * (stairRun * i), ey - dr * (stairRun * i),
                                               f.FloorZ - stairDrop * i, dc, dr, true));
                                continue;
                            }

                            var outward = stairRun * i - e.StairInset * e.StairScale;
                            PutVanilla(e.Stair,
                                ex + dc * outward,
                                ey - dr * outward,
                                f.FloorZ - e.StairTopOffset * e.StairScale - stairDrop * i,
                                rot + MathF.PI,
                                e.StairScale);
                            result.EntrancePieces++;

                            // Hidden smooth collision under this flight. The vanilla
                            // stair's bhkCompressedMeshShape does not scale reliably
                            // with XSCL - the flight was climbable at scale 1.0 and not
                            // once scaled - so a box-collider slope is laid on the tread
                            // line instead. The mesh's top tread is 64 in front of its
                            // origin, which is exactly StairInset, so the two offsets
                            // cancel and the slab's top lands at stairRun * i on the
                            // floor plane minus i drops. It descends in local +Y, so it
                            // takes the plain outward rotation, not the stair's turn.
                            Put("stairCollision",
                                ex + dc * (stairRun * i),
                                ey - dr * (stairRun * i),
                                f.FloorZ - stairDrop * i,
                                rot,
                                e.StairScale);

                            // Register each flight with the ramp-tile list so the
                            // entrance channel and the paving guard cover the stairs,
                            // and so the flank treatment dresses their sides. Without
                            // this the channel disappears with the ramp and dressing is
                            // free to land on the steps.
                            rampTiles.Add((ex + dc * outward, ey - dr * outward,
                                           f.FloorZ - stairDrop * i, dc, dr, true));
                        }

                        slot0Z = f.FloorZ - stairDrop * e.StairFlights;
                    }

                    // Any ramp tiles configured carry on below the stairs. With a full
                    // stair chain reaching grade this is usually zero.
                    for (var i = firstRamp; i < f.RampTiles; i++)
                    {
                        var ox = ex + dc * tile * i;
                        var oy = ey - dr * tile * i;
                        var oz = slot0Z - f.RampRise * i;
                        Put(PhasedRole("ramp", col + dc * i, row + dr * i), ox, oy, oz, rot);
                        Put(PhasedRole("rampCap", col + dc * i, row + dr * i), ox, oy, oz, rot);
                        rampTiles.Add((ox, oy, oz, dc, dr, false));
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

                // Retaining face hangs below the floor plane; surplus buries. One
                // piece only covers RetainHeight, so deep edges stack downward -
                // otherwise a steep side leaves a gap showing open terrain.
                var drop = edgeGround.HasValue ? f.FloorZ - edgeGround.Value : f.RetainHeight;
                faced.Add((ex, ey, dc, dr, rot, f.FloorZ, drop));
                var courses = Math.Max(1, (int)MathF.Ceiling(drop / f.RetainHeight));
                for (var c = 0; c < courses; c++)
                {
                    Put("retain", ex, ey, f.FloorZ - f.RetainHeight * c, rot);
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

        // ---- naturalisation: banded edge treatment -------------------------
        //
        // Every segment of the approved footprint is exposed between 192 and 288
        // units, so the whole perimeter is a wall and there is no gentle edge left to
        // soften. The treatment is therefore banded by measured exposure rather than
        // applied uniformly:
        //
        //   exposure > WallMinDrop   rock embankment, each piece scaled to the wall
        //                            it faces and placed so its crown reaches the
        //                            floor plane, some overshooting to break the
        //                            outline seen from on the terrace
        //   always                   a toe of low rock piles bedded into grade
        //   gentle local ground      the project-owned rough-earth verge wedge, plus
        //                            shrubs and scrub, so rock gives way to grass
        //
        // The ramp flanks are treated as segments too, and that is where the small
        // height differences actually live: the ramp stands 288 above grade at its
        // head and 24 at its foot, so it picks up rock high up and earth low down.
        var d = f.Dressing;
        var rng = new Random(d.Seed);

        // Clear walking channel down the middle of the entrance, continued past the
        // foot toward the road. Nothing is placed whose mesh reaches into it, which is
        // what lets rock hug both ramp flanks without the way in becoming an obstacle.
        (float MinX, float MinY, float MaxX, float MaxY)? channel = null;
        if (rampTiles.Count > 0)
        {
            var rdc = rampTiles[0].Dc;
            var rdr = rampTiles[0].Dr;
            var minX = rampTiles.Min(t => t.X) - tile / 2f;
            var maxX = rampTiles.Max(t => t.X) + tile / 2f;
            var minY = rampTiles.Min(t => t.Y) - tile / 2f;
            var maxY = rampTiles.Max(t => t.Y) + tile / 2f;

            if (rdc == 0)
            {
                var mid = (minX + maxX) / 2f;
                minX = mid - d.RampChannelWidth / 2f;
                maxX = mid + d.RampChannelWidth / 2f;
                if (rdr < 0) { maxY += d.RampLandingLength; } else { minY -= d.RampLandingLength; }
            }
            else
            {
                var mid = (minY + maxY) / 2f;
                minY = mid - d.RampChannelWidth / 2f;
                maxY = mid + d.RampChannelWidth / 2f;
                if (rdc > 0) { maxX += d.RampLandingLength; } else { minX -= d.RampLandingLength; }
            }

            channel = (minX, minY, maxX, maxY);
        }

        bool ChannelClear(float x, float y, float reach)
        {
            if (channel is not { } c)
            {
                return true;
            }

            var gx = MathF.Max(MathF.Max(c.MinX - x, x - c.MaxX), 0f);
            var gy = MathF.Max(MathF.Max(c.MinY - y, y - c.MaxY), 0f);
            return MathF.Sqrt(gx * gx + gy * gy) >= reach;
        }

        bool RectClearChannel(float minX, float minY, float maxX, float maxY)
        {
            if (channel is not { } c)
            {
                return true;
            }

            return maxX <= c.MinX || minX >= c.MaxX || maxY <= c.MinY || minY >= c.MaxY;
        }

        // ---- protected market floor ---------------------------------------
        // Rocks are meant to interrupt the RIM of the terrace, not to stand on it.
        // The protected region is the paved footprint eroded inward by the rim
        // allowance on OUTER edges only - shared edges between two paved cells are
        // not eroded, or the protection would be full of holes along every internal
        // boundary. Anything whose crown clears the floor plane is then refused if
        // its mesh reaches into that region, which is what stops a perimeter rock
        // from protruding through the usable market surface.
        var marketFloor = new List<(float MinX, float MinY, float MaxX, float MaxY)>();
        foreach (var (col, row) in paved)
        {
            var (cx, cy) = Centre(col, row);
            var minX = cx - tile / 2f + (paved.Contains((col - 1, row)) ? 0f : d.PavingRimAllowance);
            var maxX = cx + tile / 2f - (paved.Contains((col + 1, row)) ? 0f : d.PavingRimAllowance);
            var minY = cy - tile / 2f + (paved.Contains((col, row + 1)) ? 0f : d.PavingRimAllowance);
            var maxY = cy + tile / 2f - (paved.Contains((col, row - 1)) ? 0f : d.PavingRimAllowance);
            if (maxX > minX && maxY > minY)
            {
                marketFloor.Add((minX, minY, maxX, maxY));
            }
        }

        // Where a flight's walking surface starts relative to the tile it was
        // registered at. The vanilla mesh's top tread is one inset in front of its
        // origin; the kit flight's origin is its top tread.
        var stairInset = f.Entrance.KitStair ? 0f : f.Entrance.StairInset * f.Entrance.StairScale;

        bool ClearsMarketFloor(float x, float y, float reach, float crownZ)
        {
            if (crownZ > f.FloorZ + d.PavingClearance)
            {
                foreach (var r in marketFloor)
                {
                    var gx = MathF.Max(MathF.Max(r.MinX - x, x - r.MaxX), 0f);
                    var gy = MathF.Max(MathF.Max(r.MinY - y, y - r.MaxY), 0f);
                    if (MathF.Sqrt(gx * gx + gy * gy) < reach)
                    {
                        return false;
                    }
                }
            }

            // The ramp is a walking surface too, and it was not protected: seven pieces
            // ended up standing on it, one of them 100 units proud. Its surface falls
            // along the run, so the test is against the height at the piece's own
            // position rather than a single plane.
            foreach (var t in rampTiles)
            {
                // A ramp tile is a 512-wide walking surface. A stair flight is not: its
                // walking surface is the 167-wide gap between the two halves of the
                // wall, starting one inset in front of the origin and running one
                // flight. Guarding the whole tile for a flight would refuse the very
                // bank that is meant to bury its walls.
                var sc = f.Entrance.StairScale;
                var half = t.Stair
                    ? f.Entrance.StairHalfWidth * sc + d.RampRimAllowance
                    : tile / 2f - d.RampRimAllowance;
                var runStart = t.Stair ? stairInset : 0f;
                var runLen = t.Stair ? f.Entrance.StairRun * sc : tile;
                var runRise = t.Stair ? f.Entrance.StairDrop * sc : f.RampRise;

                var ax = t.X + t.Dc * runStart;
                var ay = t.Y - t.Dr * runStart;
                var bx = ax + t.Dc * runLen;
                var by = ay - t.Dr * runLen;
                var minX = MathF.Min(ax, bx) - (t.Dc == 0 ? half : 0f);
                var maxX = MathF.Max(ax, bx) + (t.Dc == 0 ? half : 0f);
                var minY = MathF.Min(ay, by) - (t.Dr == 0 ? half : 0f);
                var maxY = MathF.Max(ay, by) + (t.Dr == 0 ? half : 0f);

                var gx = MathF.Max(MathF.Max(minX - x, x - maxX), 0f);
                var gy = MathF.Max(MathF.Max(minY - y, y - maxY), 0f);
                if (MathF.Sqrt(gx * gx + gy * gy) >= reach)
                {
                    continue;
                }

                // How far along the tile's run the piece sits, and so how far the
                // sloping surface has dropped by the time it gets there.
                var along = t.Dc != 0
                    ? (x - ax) * t.Dc / runLen
                    : (y - ay) * -t.Dr / runLen;
                along = Math.Clamp(along, 0f, 1f);
                if (crownZ > t.Z - runRise * along + d.PavingClearance)
                {
                    return false;
                }
            }

            return true;
        }

        bool InPaving(float x, float y)
        {
            foreach (var r in result.PavedRects)
            {
                if (x >= r.MinX && x <= r.MaxX && y >= r.MinY && y <= r.MaxY)
                {
                    return true;
                }
            }

            return false;
        }

        float Spin() => (float)(rng.NextDouble() * Math.PI * 2.0);

        // ---- layered perimeter -------------------------------------------
        // The brief: from the paved surface outward the eye should read
        //
        //     paving -> rough verge -> low stone retaining WHERE NEEDED
        //            -> sloped earth and embedded rock -> native tundra
        //
        // with the widths changing constantly and no continuous wall anywhere. So this
        // does not place masonry per segment. It walks each straight run of the
        // outline and lays SHORT stretches of wall separated by gaps, and the gaps get
        // nothing but earth, rock and planting from the ordinary treatment. Every
        // stretch ends in a large part-buried rock, so masonry always dies into the
        // bank rather than stopping in mid-air.
        //
        // Two levers keep it from reading as a pattern: every length and offset is
        // jittered per stretch, and each compass edge is seeded differently and given
        // its own bias, so one side ends up more masonry and another more rock. The
        // player should not be able to trace a rectangle.
        void LayeredPerimeter()
        {
            var w = d.PerimeterWall;
            if (!w.Enabled || w.Piece.Length == 0)
            {
                return;
            }

            var bounds = boundsOf(FormKeyHelper.Parse(w.Piece));
            var height = (bounds.Height > 1f ? bounds.Height : w.CourseHeight) * w.PieceScale;
            var pieceLen = w.PieceLength * w.PieceScale;
            var perSegment = Math.Max(1, (int)MathF.Ceiling(tile / pieceLen));

            foreach (var direction in Directions)
            {
                if (w.PrototypeEdges.Count > 0
                    && !w.PrototypeEdges.Contains(direction.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // A separate stream per edge, so the two sides of the fair never come
                // out mirrored. The salt is a fixed table, NOT string.GetHashCode():
                // .NET randomises string hashes per process, and seeding from one made
                // the masonry come out differently on every run of the generator.
                var edgeRng = new Random(d.Seed + EdgeSalt[direction.Name]);
                var masonryBias = w.MasonryBias.TryGetValue(direction.Name, out var b) ? b : 0.5f;

                var lines = faced
                    .Where(s => s.Dc == direction.Dc && s.Dr == direction.Dr)
                    .GroupBy(s => direction.Dc == 0 ? s.Ey : s.Ex);

                foreach (var line in lines)
                {
                    // The stair flights bring their own walls. More field wall right
                    // beside them is what made the entrance read as a gate, so on the
                    // entrance edge the segments nearest the stairs get no masonry.
                    var ordered = line
                        .Where(s => channel is not { } ch
                            || !(s.Dc == direction.Dc && s.Dr == direction.Dr
                                 && direction.Name.Equals(f.RampEdge, StringComparison.OrdinalIgnoreCase))
                            || (direction.Dc == 0
                                ? MathF.Abs(s.Ex - (ch.MinX + ch.MaxX) / 2f)
                                : MathF.Abs(s.Ey - (ch.MinY + ch.MaxY) / 2f)) >= w.EntranceClear)
                        .OrderBy(s => direction.Dc == 0 ? s.Ex : s.Ey)
                        .ToList();

                    var i = 0;
                    while (i < ordered.Count)
                    {
                        // Wall for a few segments, then nothing for a few. Lengths are
                        // drawn fresh each time so no rhythm establishes itself.
                        var wanted = edgeRng.NextDouble() < masonryBias;
                        var span = w.MinStretch + edgeRng.Next(w.MaxStretch - w.MinStretch + 1);
                        span = Math.Min(span, ordered.Count - i);

                        if (!wanted)
                        {
                            i += span;
                            continue;
                        }

                        var stretch = ordered.Skip(i).Take(span).ToList();
                        var lift = (float)edgeRng.NextDouble() * w.OffsetJitter;
                        var courses = 1 + (edgeRng.NextDouble() < w.SecondCourseChance ? 1 : 0);

                        foreach (var seg in stretch)
                        {
                            for (var c = 0; c < courses; c++)
                            {
                                // Each course steps further out as it goes down, so the
                                // face is battered. The step itself is jittered, which
                                // is what stops the batter reading as a machined slope.
                                var batter = w.CourseBatter * (0.7f + (float)edgeRng.NextDouble() * 0.6f);
                                var outward = lift + batter * c;
                                var crown = seg.SurfaceZ - height * c;

                                for (var k = 0; k < perSegment; k++)
                                {
                                    var along = (k + 0.5f) * tile / perSegment - tile / 2f
                                        + (float)(edgeRng.NextDouble() - 0.5) * w.AlongJitter;
                                    var px = seg.Ex + seg.Dc * outward + (seg.Dc == 0 ? along : 0f);
                                    var py = seg.Ey - seg.Dr * outward + (seg.Dr == 0 ? along : 0f);
                                    if (!ChannelClear(px, py, pieceLen / 2f))
                                    {
                                        result.ChannelSkipped++;
                                        continue;
                                    }

                                    PutVanilla(w.Piece, px, py, crown - bounds.ZMax * w.PieceScale, seg.Rot, w.PieceScale);
                                    result.WallCourses++;
                                }
                            }
                        }

                        // Bury a large rock at each end so the masonry runs into the
                        // bank instead of stopping dead.
                        foreach (var end in new[] { stretch[0], stretch[^1] })
                        {
                            var pick = d.CornerStones[edgeRng.Next(d.CornerStones.Count)];
                            var rb = boundsOf(FormKeyHelper.Parse(pick));
                            if (rb.Height <= 1f)
                            {
                                continue;
                            }

                            var scale = Math.Clamp(
                                (end.Drop * w.TerminalRockShare) / rb.Height, d.MinScale, d.MaxScale);
                            var reach = rb.Radius * scale;
                            var out2 = MathF.Max(0f, reach - d.WallEdgeOverlap);
                            var rx = end.Ex + end.Dc * out2;
                            var ry = end.Ey - end.Dr * out2;
                            if (!ChannelClear(rx, ry, reach)
                                || !ClearsMarketFloor(rx, ry, reach, end.SurfaceZ))
                            {
                                result.ChannelSkipped++;
                                continue;
                            }

                            // Sunk below the crest so it reads as part-buried in the
                            // bank rather than dropped on top of it.
                            var crown = end.SurfaceZ - w.TerminalRockSink;
                            PutVanilla(pick, rx, ry, crown - rb.ZMax * scale, Spin(), scale);
                            result.TerminalRocks++;
                        }

                        i += span;
                    }
                }
            }
        }

        // One embankment rock, scaled to the wall it faces and bedded into the ground
        // beneath itself. Returns false when nothing was placed, so the caller can
        // retry with a slimmer piece.
        bool TryWallRock(string pick, float ex, float ey, int dc, int dr, float surfaceZ, float drop)
        {
            var b = boundsOf(FormKeyHelper.Parse(pick));
            if (b.Height <= 1f)
            {
                result.OversizedSkipped++;
                return false;
            }

            // Provisional scale, from the exposure at the face, purely to get the
            // stand-off distance right.
            var scale = Math.Clamp((drop + d.WallBedding) / b.Height, d.MinScale, d.MaxScale);
            var along = (float)(rng.NextDouble() - 0.5) * tile;
            var outward = MathF.Max(0f, b.Radius * scale - d.WallEdgeOverlap);
            var px = ex + dc * outward + (dc == 0 ? along : 0f);
            var py = ey - dr * outward + (dr == 0 ? along : 0f);

            // Re-bed against the ground under the rock itself. This matters: the
            // ground falls away from the platform, so a rock standing a couple of
            // hundred units out sits on ground well below the face it is meant to be
            // facing, and bedding it to the face measurement leaves it in mid-air.
            var rockGround = sampleTerrain(px, py);
            if (!rockGround.HasValue)
            {
                return false;
            }

            var localDrop = surfaceZ - rockGround.Value;
            scale = Math.Clamp((localDrop + d.WallBedding) / b.Height, d.MinScale, d.MaxScale);
            var radius = b.Radius * scale;
            if (radius > d.WallMaxRadius)
            {
                result.OversizedSkipped++;
                return false;
            }

            // Headroom is what the piece has spare once it has covered the wall.
            // Overshoot is capped by it, so the base is never lifted clear of the
            // ground in order to crown the edge.
            var headroom = b.Height * scale - localDrop;
            if (headroom <= 0f)
            {
                result.TooShortSkipped++;
                return false;
            }

            if (!ChannelClear(px, py, radius))
            {
                result.ChannelSkipped++;
                return false;
            }

            if (!ClearsMarketFloor(px, py, radius, surfaceZ + d.WallTopOvershoot))
            {
                result.PavingGuardSkipped++;
                return false;
            }

            // Solve Z from the piece's own bounds. A vanilla rock's origin sits near
            // its base, not its centre, so assuming a fixed offset is exactly what
            // leaves dressing either floating or sunk out of sight.
            var crown = surfaceZ
                + (float)rng.NextDouble() * MathF.Min(d.WallTopOvershoot, headroom);
            PutVanilla(pick, px, py, crown - b.ZMax * scale, Spin(), scale);
            result.WallRocks++;
            return true;
        }

        // Long, straight structural runs get one elongated tundra cliff skin rather
        // than three unrelated boulders per segment. Short runs and irregular joins
        // deliberately keep the established large-rock treatment.
        var cliffCovered = new HashSet<int>();
        var cliffBounds = boundsOf(FormKeyHelper.Parse(d.CliffFace));
        var indexedFaces = faced.Select((segment, index) => (segment, index)).ToList();
        foreach (var direction in Directions)
        {
            var candidates = indexedFaces
                .Where(v => v.segment.Dc == direction.Dc && v.segment.Dr == direction.Dr
                    && v.segment.Drop > d.WallMinDrop)
                .GroupBy(v => direction.Dc == 0 ? v.segment.Ey : v.segment.Ex);

            foreach (var line in candidates)
            {
                var ordered = line.OrderBy(v => direction.Dc == 0 ? v.segment.Ex : v.segment.Ey).ToList();
                var runs = new List<List<(int Index, float Ex, float Ey, int Dc, int Dr,
                    float Rot, float SurfaceZ, float Drop)>>();
                foreach (var item in ordered)
                {
                    var coordinate = direction.Dc == 0 ? item.segment.Ex : item.segment.Ey;
                    var runItem = (item.index, item.segment.Ex, item.segment.Ey,
                        item.segment.Dc, item.segment.Dr, item.segment.Rot,
                        item.segment.SurfaceZ, item.segment.Drop);
                    if (runs.Count == 0)
                    {
                        runs.Add(new() { runItem });
                        continue;
                    }

                    var previous = runs[^1][^1];
                    var previousCoordinate = direction.Dc == 0 ? previous.Ex : previous.Ey;
                    if (MathF.Abs(coordinate - previousCoordinate - tile) > 1f)
                    {
                        runs.Add(new());
                    }
                    runs[^1].Add(runItem);
                }

                foreach (var run in runs.Where(r => r.Count >= d.CliffMinRunSegments))
                {
                    for (var start = 0; start < run.Count; start += d.CliffMaxRunSegments)
                    {
                        var chunk = run.Skip(start).Take(d.CliffMaxRunSegments).ToList();
                        if (chunk.Count < d.CliffMinRunSegments)
                        {
                            continue;
                        }

                        var ex = chunk.Average(v => v.Ex);
                        var ey = chunk.Average(v => v.Ey);
                        var surfaceZ = chunk.Max(v => v.SurfaceZ);
                        var drop = chunk.Max(v => v.Drop);
                        var scale = Math.Clamp((drop + 24f) / cliffBounds.Height, 0.9f, 1.25f);
                        var length = d.CliffMeshLength * scale;
                        var depth = d.CliffMeshDepth * scale;
                        // The cliff mesh is an OPEN SHELL, not a solid. Measured from
                        // the shipped geometry: 82% of DirtCliffs01's face area points
                        // along local -Y, a separate cap facing +Z forms the grassy top,
                        // and the +Y side is simply absent - 176 boundary edges on the
                        // main face alone. Two consequences drive this placement.
                        //
                        // First, the piece has to be turned to face the player. The kit
                        // convention points local +Y outward, which aimed the cliff face
                        // INTO the platform and left the missing back wall pointing at
                        // the viewer, so the dressing vanished when seen from outside.
                        // Adding half a turn puts the real face outward.
                        //
                        // Second, with the face outward the body now extends inward, so
                        // the offset is inward too: the open back ends up buried inside
                        // the structural slab instead of hanging in the open air.
                        var px = ex - direction.Dc * d.CliffInset;
                        var py = ey + direction.Dr * d.CliffInset;

                        // Third: the shell has open ENDS as well as an open back, and
                        // the mesh is longer than the runs it covers, so each end
                        // overhangs. At a re-entrant corner the overhang runs into the
                        // body of the next tile and is buried. At a convex corner it
                        // sticks out past the corner into open air, and from the side
                        // face the player looks straight into the hollow shell - which
                        // is what the east face was doing. So: work out which ends are
                        // buried, slide the piece toward a buried end so the exposed
                        // one is tucked inside the corner, and if neither end can be
                        // buried, do not place the skin at all. Rocks cover that run.
                        var runLen = chunk.Count * tile;
                        var axisX = direction.Dc == 0 ? 1f : 0f;
                        var axisY = direction.Dc == 0 ? 0f : 1f;
                        var inX = -direction.Dc * tile / 2f;
                        var inY = direction.Dr * tile / 2f;
                        var lo = chunk[0];
                        var hi = chunk[^1];
                        var loBuried = InPaving(lo.Ex - axisX * tile + inX, lo.Ey - axisY * tile + inY);
                        var hiBuried = InPaving(hi.Ex + axisX * tile + inX, hi.Ey + axisY * tile + inY);
                        var overhang = (length - runLen) / 2f;
                        var shift = 0f;
                        if (!loBuried && !hiBuried)
                        {
                            if (overhang > -d.CliffEndInset)
                            {
                                result.CliffEndSkipped++;
                                continue;
                            }
                        }
                        else if (!loBuried)
                        {
                            shift = overhang + d.CliffEndInset;
                        }
                        else if (!hiBuried)
                        {
                            shift = -(overhang + d.CliffEndInset);
                        }

                        // The buried end must still stop inside the neighbouring tile,
                        // whose body runs one full tile along the axis.
                        if (shift != 0f && overhang + MathF.Abs(shift) > tile)
                        {
                            result.CliffEndSkipped++;
                            continue;
                        }

                        px += axisX * shift;
                        py += axisY * shift;

                        // Use the full measured depth as the normal-axis half-extent.
                        // The vanilla mesh origin is not centred in depth
                        // (local Y is -135..330), so this is deliberately conservative
                        // around the entrance rather than assuming a symmetric pivot.
                        var halfX = direction.Dc == 0 ? length / 2f : depth;
                        var halfY = direction.Dr == 0 ? length / 2f : depth;
                        if (!RectClearChannel(px - halfX, py - halfY, px + halfX, py + halfY))
                        {
                            result.ChannelSkipped++;
                            continue;
                        }

                        // Sink the cliff so its grassy top cap sits UNDER the paving
                        // rather than 24 units above it. The cap is a broad horizontal
                        // surface extending the full depth of the mesh, so any part of
                        // it left above the floor plane reads as a grass shelf lying
                        // across the market floor, which is exactly how it looked.
                        PutVanilla(d.CliffFace, px, py,
                            surfaceZ - d.CliffTopSink - cliffBounds.ZMax * scale,
                            OutwardRotation[direction.Name] + MathF.PI, scale);
                        result.CliffFaces++;
                        foreach (var item in chunk)
                        {
                            cliffCovered.Add(item.Index);
                            result.CliffCoveredSegments++;
                        }
                    }
                }
            }
        }

        // One edge segment: cliff/rock faced, toed into grade, then verged.
        void Treat(float ex, float ey, int dc, int dr, float rot, float surfaceZ, float drop,
            bool placeWallRocks = true)
        {
            // --- embankment ---------------------------------------------------
            if (placeWallRocks && drop > d.WallMinDrop)
            {
                // Draw only from pieces that can actually reach the top of this wall
                // at their permitted scale. Without this filter a 180-unit rock gets
                // asked to face a 288-unit edge, and the only way to put its crown on
                // the floor plane is to lift its base off the ground. Short pieces are
                // not wasted - they are exactly what the lower ramp flanks want.
                var usable = d.Wall
                    .Where(k => boundsOf(FormKeyHelper.Parse(k)).Height * d.MaxScale
                        >= drop + d.WallTopOvershoot)
                    .ToList();
                if (usable.Count == 0)
                {
                    usable = new List<string>
                    {
                        d.Wall.MaxBy(k => boundsOf(FormKeyHelper.Parse(k)).Height)!,
                    };
                }

                // The slimmest piece that can still reach the top. Kept as a retry for
                // segments beside the entrance: there a fat rock is rejected for
                // reaching into the walking channel, and without a second attempt the
                // most visible wall on the site - the west side of the ramp cutting -
                // would be the one left bare.
                var slimmest = usable.MinBy(k => boundsOf(FormKeyHelper.Parse(k)).Radius)!;

                for (var i = 0; i < d.WallPerSegment; i++)
                {
                    if (!TryWallRock(usable[rng.Next(usable.Count)], ex, ey, dc, dr, surfaceZ, drop))
                    {
                        TryWallRock(slimmest, ex, ey, dc, dr, surfaceZ, drop);
                    }
                }
            }

            // --- toe ----------------------------------------------------------
            for (var i = 0; i < d.ToePerSegment; i++)
            {
                var pick = d.Rocks[rng.Next(d.Rocks.Count)];
                var b = boundsOf(FormKeyHelper.Parse(pick));
                var scale = d.MinScale + (float)rng.NextDouble() * (d.MaxScale - d.MinScale);
                var radius = b.Radius * scale;
                if (radius > d.MaxRadius)
                {
                    result.OversizedSkipped++;
                    continue;
                }

                var along = (float)(rng.NextDouble() - 0.5) * tile;
                var outward = MathF.Max(d.MinOffset, radius - d.EdgeOverlap)
                    + (float)rng.NextDouble() * d.Spread;
                var px = ex + dc * outward + (dc == 0 ? along : 0f);
                var py = ey - dr * outward + (dr == 0 ? along : 0f);
                if (!ChannelClear(px, py, radius))
                {
                    result.ChannelSkipped++;
                    continue;
                }

                var ground = sampleTerrain(px, py);
                if (!ground.HasValue)
                {
                    continue;
                }

                if (!ClearsMarketFloor(px, py, radius,
                        ground.Value - d.RockSink + b.ZMax * scale))
                {
                    result.PavingGuardSkipped++;
                    continue;
                }

                PutVanilla(pick, px, py, ground.Value - d.RockSink, Spin(), scale);
                result.ToeRocks++;
            }

            // --- verge: rough earth, then plants ------------------------------
            // The wedge is a thin 32-unit skin of rough earth. It reads only where the
            // native ground it lies on is itself gentle, so it is gated on the local
            // step across the verge, not on the wall height standing behind it.
            var nearX = ex + dc * d.MinOffset;
            var nearY = ey - dr * d.MinOffset;
            var farX = nearX + dc * d.ShoulderRun;
            var farY = nearY - dr * d.ShoulderRun;
            var nearGround = sampleTerrain(nearX, nearY);
            var farGround = sampleTerrain(farX, farY);
            if (nearGround.HasValue && farGround.HasValue
                && nearGround.Value < surfaceZ
                && MathF.Abs(nearGround.Value - farGround.Value) <= d.VergeMaxLocalStep
                && ChannelClear(nearX, nearY, d.ShoulderRun))
            {
                Put("shoulder", nearX, nearY, nearGround.Value, rot);
                result.VergeWedges++;
            }

            for (var i = 0; i < d.VergePerSegment; i++)
            {
                var pool = rng.NextDouble() < 0.5 ? d.Shrubs : d.Scrub;
                var pick = pool[rng.Next(pool.Count)];
                var b = boundsOf(FormKeyHelper.Parse(pick));
                var scale = d.MinScale + (float)rng.NextDouble() * (d.MaxScale - d.MinScale);

                var along = (float)(rng.NextDouble() - 0.5) * tile;
                var outward = d.MinOffset + d.Spread * 0.5f
                    + (float)rng.NextDouble() * d.Spread;
                var px = ex + dc * outward + (dc == 0 ? along : 0f);
                var py = ey - dr * outward + (dr == 0 ? along : 0f);
                if (!ChannelClear(px, py, b.Radius * scale))
                {
                    result.ChannelSkipped++;
                    continue;
                }

                var ground = sampleTerrain(px, py);
                if (!ground.HasValue)
                {
                    continue;
                }

                if (!ClearsMarketFloor(px, py, b.Radius * scale,
                        ground.Value + b.ZMax * scale))
                {
                    result.PavingGuardSkipped++;
                    continue;
                }

                PutVanilla(pick, px, py, ground.Value, Spin(), scale);
                result.VergePlants++;
            }
        }

        for (var i = 0; i < faced.Count; i++)
        {
            var seg = faced[i];
            Treat(seg.Ex, seg.Ey, seg.Dc, seg.Dr, seg.Rot, seg.SurfaceZ, seg.Drop,
                !cliffCovered.Contains(i));
        }

        LayeredPerimeter();

        // ---- entrance bank ----------------------------------------------------
        // Each stair flight brings a 666-wide drystone wall with it, and three of them
        // stacked either side of the steps read as a gatehouse. The wall cannot be
        // removed from the mesh, so it is BURIED instead: earth and part-sunk rock laid
        // against the outer face of each flight's wall, so what the player sees beside
        // the steps is bank, not masonry. The stair width itself is untouched.
        //
        // The two sides are deliberately different. One gets rock, the other earth and
        // scrub, with different counts, so the approach cannot be read as a designed
        // pair. Which side is which is fixed by config, not chance, so it reproduces.
        // ---- cheek walls ------------------------------------------------------
        // Small drystone walls stepping down beside the steps, one line each side,
        // waist high - the concept's stair cheeks. Stonewall01 scaled down, laid along
        // the flight, each piece's crest set a little above the nosing line where it
        // stands. The two sides get different crest heights so the pair never reads
        // as designed symmetry. These, and only these, are the masonry at the
        // entrance.
        var trace = Environment.GetEnvironmentVariable("SKYRIMFAIR_TRACE") is { Length: > 0 };
        var stairHalf = f.Entrance.StairHalfWidth * f.Entrance.StairScale;
        var flightRun = f.Entrance.StairRun * f.Entrance.StairScale;
        var flightDrop = f.Entrance.StairDrop * f.Entrance.StairScale;
        var cheekDepth = 0f;
        var cheekRiseMax = 0f;
        if (f.Entrance.UseStairs && d.EntranceCheeks.Enabled && rampTiles.Count > 0)
        {
            var ck = d.EntranceCheeks;
            var cb = boundsOf(FormKeyHelper.Parse(ck.Piece));
            var len = ck.PieceLength * ck.Scale;
            cheekDepth = ck.PieceDepth * ck.Scale;
            cheekRiseMax = MathF.Max(ck.RiseLeft, ck.RiseRight);
            var alongRot = OutwardRotation[f.RampEdge.ToUpperInvariant()] + MathF.PI / 2f;

            foreach (var t in rampTiles.Where(t => t.Stair))
            {
                var n = Math.Max(1, (int)MathF.Ceiling(flightRun / len));
                for (var side = -1; side <= 1; side += 2)
                {
                    var rise = side < 0 ? ck.RiseLeft : ck.RiseRight;
                    var lateral = side * (stairHalf + ck.Gap + cheekDepth / 2f);
                    for (var j = 0; j < n; j++)
                    {
                        // First piece flush with the stair head so nothing pokes onto the
                        // paving; later pieces overlap toward the foot rather than overhang.
                        var along = stairInset + MathF.Min((j + 0.5f) * len, flightRun - len / 2f);
                        var nosing = t.Z - flightDrop * ((along - stairInset) / flightRun);
                        var px = t.X + t.Dc * along + (t.Dc == 0 ? lateral : 0f);
                        var py = t.Y - t.Dr * along + (t.Dr == 0 ? lateral : 0f);
                        var pz = nosing + rise - cb.ZMax * ck.Scale;
                        PutVanilla(ck.Piece, px, py, pz, alongRot, ck.Scale);
                        result.CheekWalls++;
                        if (trace) Console.Error.WriteLine($"cheek side {side} flight at ({t.X:F0},{t.Y:F0}) j {j} -> ({px:F0},{py:F0},{pz:F0}) crest {nosing + rise:F0}");
                    }
                }
            }
        }

        if (f.Entrance.UseStairs && d.EntranceBank.Enabled && rampTiles.Count > 0)
        {
            var eb = d.EntranceBank;
            var bankRng = new Random(d.Seed + 7919);

            // What the bank leans on: the outer face of the cheek wall if there is
            // one, else the vanilla flight's own 666-wide wall. And how high it may
            // rise: a little under that wall's crest at mid-flight.
            var wallHalf = cheekDepth > 0f
                ? stairHalf + d.EntranceCheeks.Gap + cheekDepth
                : 256f * f.Entrance.StairScale;
            var wallRise = cheekDepth > 0f
                ? cheekRiseMax - flightDrop / 2f
                : 40f * f.Entrance.StairScale;
            var flightInset = stairInset;

            // The walking route is the stair gap. Nothing here may reach into it.
            var gapHalf = stairHalf + eb.GapMargin;

            foreach (var t in rampTiles)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var rocky = (side < 0) == eb.RockOnLeft;
                    var pool = rocky ? eb.RockPool : eb.EarthPool;
                    var count = rocky ? eb.RockSide : eb.EarthSide;

                    // The wall crest is a little above the top tread of its flight.
                    var wallTop = t.Z + wallRise;

                    // Pieces are laid outward from the wall face, each overlapping
                    // the one before by a share of its own radius, so spacing follows
                    // the actual sizes drawn rather than a fixed step.
                    var edge = wallHalf;

                    for (var k = 0; k < count; k++)
                    {
                        // Alongside the MIDDLE of the flight. The tile records the
                        // stair origin, which is one inset behind the top tread, so
                        // the flight's wall runs from inset to inset + run.
                        var along = flightInset + flightRun / 2f
                            + (float)(bankRng.NextDouble() - 0.5) * eb.AlongJitter;
                        var footX = t.X + t.Dc * along + (t.Dc == 0 ? side * wallHalf : 0f);
                        var footY = t.Y - t.Dr * along + (t.Dr == 0 ? side * wallHalf : 0f);

                        // Sized to the wall face it hides: from below grade at the
                        // wall's foot up to just under its crest. Sizing it to the
                        // drop alone gave knee-high rocks against a chest-high wall.
                        var bankGround = sampleTerrain(footX, footY);
                        if (!bankGround.HasValue)
                        {
                            continue;
                        }

                        var drop = MathF.Max(wallTop - bankGround.Value, eb.MinDrop);
                        var target = drop - eb.Sink + eb.Bury;

                        // Draw from the pieces tall enough to do the job at their
                        // permitted scale; a low pile is not asked to hide a tall wall.
                        var usable = pool
                            .Where(kk => boundsOf(FormKeyHelper.Parse(kk)).Height * eb.MaxScale
                                >= target * eb.ReachShare)
                            .ToList();
                        if (usable.Count == 0)
                        {
                            usable = new List<string>
                            {
                                pool.MaxBy(kk => boundsOf(FormKeyHelper.Parse(kk)).Height)!,
                            };
                        }

                        var pick = usable[bankRng.Next(usable.Count)];
                        var pb = boundsOf(FormKeyHelper.Parse(pick));
                        if (pb.Height <= 1f)
                        {
                            continue;
                        }

                        var scale = Math.Clamp(target / pb.Height, d.MinScale, eb.MaxScale);
                        var reach = pb.Radius * scale;

                        // Leaning into whatever is inside it - the wall face first, then
                        // the previous piece - but never reaching into the steps: a
                        // piece too fat to lean on the wall is pushed out until its
                        // inner edge clears the gap.
                        var offset = MathF.Max(edge + reach * eb.Lean, gapHalf + reach);

                        // The bank is for the wall. Once the next piece would start
                        // beyond the wall's own extent it is embankment, not bank, and
                        // the ordinary perimeter treatment already covers that.
                        if (offset - reach > wallHalf + eb.Extent)
                        {
                            if (trace) Console.Error.WriteLine($"bank STOP        side {side} k {k}: inner edge {offset - reach:F0} beyond wall + extent");
                            break;
                        }

                        edge = offset + reach * eb.Lean;
                        var lateral = side * offset;
                        var px = t.X + t.Dc * along + (t.Dc == 0 ? lateral : 0f);
                        var py = t.Y - t.Dr * along + (t.Dr == 0 ? lateral : 0f);

                        // The stair walls themselves sit inside the ramp channel, so
                        // the channel guard cannot be the test here. The test is the
                        // steps: the piece's inner edge must stay outside the gap.
                        if (MathF.Abs(lateral) - reach < gapHalf)
                        {
                            if (trace) Console.Error.WriteLine($"bank REJECT gap  {pick} side {side} k {k} scale {scale:F2} reach {reach:F0} lateral {lateral:F0}");
                            result.ChannelSkipped++;
                            continue;
                        }

                        // Bedded: base below grade at the piece's own position, and if
                        // that would lift its crown above the crest, sunk further.
                        // Nothing here floats and nothing overtops the wall. The crown
                        // also stays under the floor plane: the top flight's wall stands
                        // above the paving, and a bank rising with it would be a rock
                        // standing on the market floor.
                        // A broad piece spans ground that is not level. Bedding it to
                        // the lowest sample under it means no edge is left in the air;
                        // the higher side just sits deeper, which reads as buried.
                        var pieceGround = bankGround.Value;
                        foreach (var (sx, sy) in new[]
                        {
                            (0f, 0f), (0.6f, 0f), (-0.6f, 0f), (0f, 0.6f), (0f, -0.6f),
                        })
                        {
                            var g = sampleTerrain(px + sx * reach, py + sy * reach);
                            if (g.HasValue && g.Value < pieceGround)
                            {
                                pieceGround = g.Value;
                            }
                        }
                        var pz = pieceGround - eb.Bury - pb.ZMin * scale;
                        var crownCap = MathF.Min(wallTop - eb.Sink, f.FloorZ - d.PavingClearance);
                        if (pz + pb.ZMax * scale > crownCap)
                        {
                            pz = crownCap - pb.ZMax * scale;
                        }

                        var crownZ = pz + pb.ZMax * scale;
                        if (!ClearsMarketFloor(px, py, reach, crownZ))
                        {
                            if (trace) Console.Error.WriteLine($"bank REJECT floor {pick} side {side} k {k} scale {scale:F2} reach {reach:F0} at ({px:F0},{py:F0}) crown {crownZ:F0}");
                            result.PavingGuardSkipped++;
                            continue;
                        }

                        if (trace) Console.Error.WriteLine($"bank PLACE       {pick} side {side} k {k} scale {scale:F2} reach {reach:F0} at ({px:F0},{py:F0},{pz:F0}) crown {pz + pb.ZMax * scale:F0} ground {pieceGround:F0}");
                        PutVanilla(pick, px, py, pz, Spin(), scale);
                        result.EntranceBankPieces++;
                    }
                }
            }
        }

        // ---- ramp flanks ---------------------------------------------------
        // Without this the ramp is a grey slab hanging in the air beside the terrace.
        // Each tile is treated on both sides against the exposure measured at that
        // tile, so the treatment fades from rock at the head to earth at the foot.
        foreach (var t in rampTiles)
        {
            var alongX = t.X + t.Dc * tile / 2f;
            var alongY = t.Y - t.Dr * tile / 2f;
            var rampSurfaceZ = t.Z - f.RampRise / 2f;

            foreach (var (name, cdc, cdr) in Directions)
            {
                // Only the two sides, never along the ramp's own run.
                if (cdc * t.Dc + cdr * t.Dr != 0)
                {
                    continue;
                }

                var ex = alongX + cdc * tile / 2f;
                var ey = alongY - cdr * tile / 2f;

                // The ramp leaves the terrace, so its first tile has paving alongside
                // it. Treating that side pushes rock and scrub outward onto the
                // platform, which is how two pieces ended up standing on the terrace.
                if (InPaving(ex + cdc * tile / 2f, ey - cdr * tile / 2f))
                {
                    continue;
                }

                var flankGround = sampleTerrain(ex, ey);
                if (!flankGround.HasValue)
                {
                    continue;
                }

                var drop = rampSurfaceZ - flankGround.Value;
                if (drop <= f.MinExposure)
                {
                    continue;
                }

                Treat(ex, ey, cdc, cdr, OutwardRotation[name], rampSurfaceZ, drop);
            }
        }

        // ---- corner stones -------------------------------------------------
        // A flat top edge reads as a built rectangle most obviously at its corners,
        // so every convex corner of the outline gets one larger stone set across it.
        foreach (var (col, row) in paved.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            foreach (var (vdc, vdr) in new[] { (0, -1), (0, 1) })
            {
                foreach (var (hdc, hdr) in new[] { (1, 0), (-1, 0) })
                {
                    if (paved.Contains((col + vdc, row + vdr)) || paved.Contains((col + hdc, row + hdr)))
                    {
                        continue;
                    }

                    var (cx, cy) = Centre(col, row);
                    var cornerX = cx + hdc * tile / 2f;
                    var cornerY = cy - vdr * tile / 2f;

                    var pick = d.CornerStones[rng.Next(d.CornerStones.Count)];
                    var b = boundsOf(FormKeyHelper.Parse(pick));
                    if (b.Height <= 1f)
                    {
                        continue;
                    }

                    var cornerGround = sampleTerrain(cornerX, cornerY);
                    var drop = cornerGround.HasValue ? f.FloorZ - cornerGround.Value : f.RetainHeight;
                    var scale = Math.Clamp((drop + d.WallBedding) / b.Height, d.MinScale, d.MaxScale);
                    var radius = b.Radius * scale;
                    if (radius > d.WallMaxRadius)
                    {
                        result.OversizedSkipped++;
                        continue;
                    }

                    var headroom = b.Height * scale - drop;
                    if (headroom <= 0f)
                    {
                        result.TooShortSkipped++;
                        continue;
                    }

                    // Set diagonally outward across the corner so it breaks both edges.
                    var outward = MathF.Max(0f, radius - d.WallEdgeOverlap) * 0.7071f;
                    var px = cornerX + hdc * outward;
                    var py = cornerY - vdr * outward;
                    if (!ChannelClear(px, py, radius))
                    {
                        result.ChannelSkipped++;
                        continue;
                    }

                    if (!ClearsMarketFloor(px, py, radius, f.FloorZ + d.WallTopOvershoot))
                    {
                        result.PavingGuardSkipped++;
                        continue;
                    }

                    var crown = f.FloorZ
                        + (float)rng.NextDouble() * MathF.Min(d.WallTopOvershoot, headroom);
                    PutVanilla(pick, px, py, crown - b.ZMax * scale, Spin(), scale);
                    result.CornerStones++;
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
    /// Picks one contiguous run of perimeter segments on the ramp edge.
    ///
    /// The run has to be contiguous AND share the same perpendicular coordinate, or
    /// the ramp lanes come out staggered: on an irregular outline the outermost
    /// segments of an edge are not all in the same row, and simply taking the last
    /// N candidates by column produced two lanes starting a whole tile apart.
    ///
    /// Where several rows offer a long enough run, the outermost one wins, so the
    /// ramp leaves from the edge of the outline rather than out of a notch in it.
    /// </summary>
    private static HashSet<(int, int, string)> ChooseRampSegments(
        HashSet<(int, int)> paved, FoundationConfig f, int cols, int rows)
    {
        var edge = f.RampEdge.ToUpperInvariant();
        var (dc, dr) = Directions.First(d => d.Name == edge) switch { var d => (d.Dc, d.Dr) };
        var chosen = new HashSet<(int, int, string)>();

        var candidates = paved
            .Where(c => !paved.Contains((c.Item1 + dc, c.Item2 + dr)))
            .ToList();
        if (candidates.Count == 0)
        {
            return chosen;
        }

        // Along = the axis the ramp is wide in; across = the one it descends along.
        static int Along((int Col, int Row) c, int dc) => dc == 0 ? c.Col : c.Row;
        static int Across((int Col, int Row) c, int dc) => dc == 0 ? c.Row : c.Col;

        var runs = new List<List<(int Col, int Row)>>();
        foreach (var line in candidates
            .Select(c => (Col: c.Item1, Row: c.Item2))
            .GroupBy(c => Across(c, dc)))
        {
            var ordered = line.OrderBy(c => Along(c, dc)).ToList();
            var run = new List<(int Col, int Row)> { ordered[0] };
            foreach (var cell in ordered.Skip(1))
            {
                if (Along(cell, dc) - Along(run[^1], dc) == 1)
                {
                    run.Add(cell);
                }
                else
                {
                    runs.Add(run);
                    run = new List<(int Col, int Row)> { cell };
                }
            }

            runs.Add(run);
        }

        var usable = runs.Where(r => r.Count >= f.RampWidth).ToList();
        if (usable.Count == 0)
        {
            // Nothing wide enough: fall back to the longest run there is, so the
            // entrance is still contiguous even if it ends up narrower than asked.
            usable = new List<List<(int Col, int Row)>> { runs.OrderByDescending(r => r.Count).First() };
        }

        // Outermost line first, so the ramp leaves the outline rather than a notch.
        //
        // "Outermost" is distance along the OUTWARD direction, which is what the sign
        // here encodes: going outward means row decreasing on a north edge, row
        // increasing on a south edge, and likewise for columns east and west. Getting
        // that backwards put a one-tile-wide entrance on the far side of the terrace.
        var pick = usable
            .OrderByDescending(r => Across(r[0], dc) * (dc != 0 ? dc : dr))
            .ThenByDescending(r => r.Count)
            .First();

        var width = Math.Min(f.RampWidth, pick.Count);
        var start = f.RampAlign.ToLowerInvariant() switch
        {
            "start" => 0,
            "end" => pick.Count - width,
            _ => Math.Max(0, pick.Count / 2 - width / 2),
        };
        for (var i = start; i < start + width; i++)
        {
            chosen.Add((pick[i].Col, pick[i].Row, edge));
        }

        return chosen;
    }
}

internal sealed class FoundationResult
{
    public Dictionary<string, Static> Statics { get; set; } = new();

    /// <summary>Paved tile bounds (minX, minY, maxX, maxY), for clearing vanilla clutter.</summary>
    public List<(float MinX, float MinY, float MaxX, float MaxY)> PavedRects { get; } = new();

    public int DisabledCount { get; set; }

    /// <summary>Dressing picks rejected for having an oversized mesh footprint.</summary>
    public int OversizedSkipped { get; set; }

    /// <summary>Picks dropped for reaching into the entrance walking channel.</summary>
    public int ChannelSkipped { get; set; }

    /// <summary>Picks dropped for being too short to face the edge without floating.</summary>
    public int TooShortSkipped { get; set; }

    /// <summary>Picks refused for protruding through the usable market floor.</summary>
    public int PavingGuardSkipped { get; set; }

    /// <summary>Vanilla entrance pieces, currently the stair-through-a-wall.</summary>
    public int EntrancePieces { get; set; }

    /// <summary>Drystone field-wall courses stepped back to batter the perimeter.</summary>
    public int WallCourses { get; set; }

    /// <summary>Part-buried rocks where a masonry run dies into the bank.</summary>
    public int TerminalRocks { get; set; }

    /// <summary>Earth and rock laid against the stair walls to bury them.</summary>
    public int EntranceBankPieces { get; set; }

    /// <summary>Low drystone pieces stepping down beside the steps.</summary>
    public int CheekWalls { get; set; }

    /// <summary>Tall rocks facing an exposed retaining edge.</summary>
    public int WallRocks { get; set; }

    /// <summary>Elongated vanilla tundra cliff faces skinning straight retaining runs.</summary>
    public int CliffFaces { get; set; }

    /// <summary>Structural wall segments whose boulder facing was replaced by cliff skin.</summary>
    public int CliffCoveredSegments { get; set; }

    /// <summary>Cliff skins refused because an open end would have shown past a corner.</summary>
    public int CliffEndSkipped { get; set; }

    /// <summary>Low rock piles bedded in at the foot of the embankment.</summary>
    public int ToeRocks { get; set; }

    /// <summary>Project-owned rough-earth verge wedges.</summary>
    public int VergeWedges { get; set; }

    /// <summary>Shrubs and scrub in the verge.</summary>
    public int VergePlants { get; set; }

    /// <summary>Larger stones set across the convex corners of the outline.</summary>
    public int CornerStones { get; set; }

    /// <summary>References named for disabling that were refused, with the reason.</summary>
    public List<string> Refused { get; } = new();

    public Dictionary<string, int> Counts { get; } = new();

    public int DressingCount { get; set; }
}

/// <summary>
/// A base object's mesh envelope, as recorded in its OBND.
///
/// <paramref name="Radius"/> is the horizontal half-diagonal, used to keep a piece
/// from swallowing the platform. <paramref name="ZMin"/> and <paramref name="ZMax"/>
/// are relative to the mesh origin, which for vanilla rocks sits near the base.
/// </summary>
internal readonly record struct PieceBounds(float Radius, float ZMin, float ZMax)
{
    public float Height => ZMax - ZMin;
}
