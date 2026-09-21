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

        static string PhasedRole(string baseRole, int col, int row)
        {
            var u = ((col % 2) + 2) % 2;
            var v = ((row % 2) + 2) % 2;
            return (u, v) switch
            {
                (1, 0) => baseRole + "U1",
                (0, 1) => baseRole + "V1",
                (1, 1) => baseRole + "U1V1",
                _ => baseRole,
            };
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
            Put(PhasedRole("floorEdge", col, row), x, y, f.FloorZ, 0f);
        }

        // ---- perimeter ----------------------------------------------------
        // Ramp segments are reserved first so no retaining wall blocks the way in.
        var rampSegments = ChooseRampSegments(paved, f, cols, rows);

        // Every placed ramp tile, kept so the naturalisation pass can face the ramp's
        // flanks and keep its walking channel clear.
        var rampTiles = new List<(float X, float Y, float Z, int Dc, int Dr)>();

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
                    // Chain of ramp tiles stepping down and outward from the paving.
                    for (var i = 0; i < f.RampTiles; i++)
                    {
                        var ox = ex + dc * tile * i;
                        var oy = ey - dr * tile * i;
                        var oz = f.FloorZ - f.RampRise * i;
                        Put(PhasedRole("ramp", col + dc * i, row + dr * i), ox, oy, oz, rot);
                        rampTiles.Add((ox, oy, oz, dc, dr));
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
                        var px = ex + direction.Dc * d.CliffOutset;
                        var py = ey - direction.Dr * d.CliffOutset;
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

                        PutVanilla(d.CliffFace, px, py,
                            surfaceZ + 24f - cliffBounds.ZMax * scale,
                            OutwardRotation[direction.Name], scale);
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

        var start = f.RampAlign.ToLowerInvariant() switch
        {
            "start" => 0,
            "end" => Math.Max(0, candidates.Count - f.RampWidth),
            _ => Math.Max(0, candidates.Count / 2 - f.RampWidth / 2),
        };
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

    /// <summary>Paved tile bounds (minX, minY, maxX, maxY), for clearing vanilla clutter.</summary>
    public List<(float MinX, float MinY, float MaxX, float MaxY)> PavedRects { get; } = new();

    public int DisabledCount { get; set; }

    /// <summary>Dressing picks rejected for having an oversized mesh footprint.</summary>
    public int OversizedSkipped { get; set; }

    /// <summary>Picks dropped for reaching into the entrance walking channel.</summary>
    public int ChannelSkipped { get; set; }

    /// <summary>Picks dropped for being too short to face the edge without floating.</summary>
    public int TooShortSkipped { get; set; }

    /// <summary>Tall rocks facing an exposed retaining edge.</summary>
    public int WallRocks { get; set; }

    /// <summary>Elongated vanilla tundra cliff faces skinning straight retaining runs.</summary>
    public int CliffFaces { get; set; }

    /// <summary>Structural wall segments whose boulder facing was replaced by cliff skin.</summary>
    public int CliffCoveredSegments { get; set; }

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
