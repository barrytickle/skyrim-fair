using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Lays out the market described by <see cref="MarketConfig"/>: stall shells of vanilla
/// pieces along lanes that pinch and swell, facing the lane, irregular in set-back,
/// angle and spacing. A stall is only placed where it keeps clear of every lane, every
/// keep-out area, the wall and every other stall, so junctions, pockets and the
/// gate-to-stage sightline open up by themselves. Everything varies from fixed integer
/// hashes, so the layout regenerates byte-identically.
/// </summary>
internal static class FairMarket
{
    private const float Deg = MathF.PI / 180f;

    /// <summary>Spacing of the samples that stand for a lane's corridor.</summary>
    private const float SampleStep = 40f;

    /// <summary>How far to slide along the lane after a stall is refused.</summary>
    private const float RetryStep = 30f;

    /// <summary>How far late dressing keeps from anyone already standing there.</summary>
    private const float ActorClearance = 40f;

    private sealed record Lane(
        MarketLane Config, int Index, (float X, float Y)[] Points, float[] Arc, float Length, float Phase,
        List<(float X, float Y, float Tx, float Ty, float Half)> Samples);

    private sealed record Placed(float X, float Y, float Yaw, float HalfW, float HalfD);

    public static MarketResult Build(
        SkyrimMod mod, FairWorldConfig world, Func<float, float, float> outside, Func<float, float, float> ground,
        Action<PlacedObject> put, Cell persistentCell, int persistentFlag, Func<string, FormKey> resolve,
        IReadOnlyList<(float X, float Y)[]> extraKeepOut)
    {
        var market = world.Market;
        var modules = market.Modules.ToDictionary(m => m.Name);
        var lanes = market.Lanes.Select((l, i) => BuildLane(l, i, world)).ToList();
        var keepOut = world.Zones
            .Where(z => market.KeepOutZones.Contains(z.Name))
            .Select(z => z.Polygon.Select(p => (p[0], p[1])).ToArray())
            .Concat(market.KeepOut.Select(a => a.Polygon.Select(p => (p[0], p[1])).ToArray()))
            .Concat(extraKeepOut)
            .ToList();
        var keepOutNames = world.Zones.Where(z => market.KeepOutZones.Contains(z.Name)).Select(z => z.Name)
            .Concat(market.KeepOut.Select(a => a.Name))
            .Concat(extraKeepOut.Select(_ => "tower"))
            .ToList();
        IReadOnlyCollection<string> exempt = Array.Empty<string>();

        var placed = new List<Placed>();
        var frontages = new List<Placed>();
        var stalls = new List<MarketStall>();
        var themeCounts = new Dictionary<string, int>();
        var pieceCount = 0;
        var refused = 0;
        var reasons = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var markerBase = FormKeyHelper.Parse(market.ShellMarker);
        var lights = 0;

        // Null when the stall fits; otherwise what it collides with.
        string? WhyNot(MarketModule m, float x, float y, float yaw, Lane? ignore = null, float? wallMargin = null, bool keepFrontages = false)
        {
            var (hw, hd) = (m.Width / 2f, m.Depth / 2f);
            var (rx, ry) = (MathF.Cos(yaw * Deg), -MathF.Sin(yaw * Deg));
            var (fx, fy) = (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg));
            for (var i = 0; i <= 4; i++)
            {
                for (var j = 0; j <= 2; j++)
                {
                    var u = -hw + i * hw / 2f;
                    var v = -hd + j * hd;
                    var px = x + u * rx + v * fx;
                    var py = y + u * ry + v * fy;
                    if (outside(px, py) > -(wallMargin ?? market.WallMargin)) return $"the wall at ({px:0}, {py:0})";
                    for (var k = 0; k < keepOut.Count; k++)
                    {
                        if (!exempt.Contains(keepOutNames[k]) && FairGeometry.Inside(keepOut[k], px, py)) return $"keep-out area {keepOutNames[k]} at ({px:0}, {py:0})";
                    }
                    foreach (var lane in lanes)
                    {
                        if (ReferenceEquals(lane, ignore)) continue;

                        // The corridor is a run of thin slabs cut square across the lane, so a
                        // pocket's width does not spill back over the pinch beside it.
                        foreach (var sample in lane.Samples)
                        {
                            var dx = px - sample.X;
                            var dy = py - sample.Y;
                            var along = dx * sample.Tx + dy * sample.Ty;
                            var across = MathF.Abs(dx * sample.Ty - dy * sample.Tx);
                            if (MathF.Abs(along) <= SampleStep * 0.6f && across < sample.Half + market.Clearance)
                            {
                                return $"lane {lane.Config.Name} at ({px:0}, {py:0}) sample ({sample.X:0}, {sample.Y:0}) t ({sample.Tx:0.00}, {sample.Ty:0.00}) half {sample.Half:0}";
                            }
                        }
                    }
                }
            }

            var candidate = new Placed(x, y, yaw, hw + market.Clearance / 2f, hd + market.Clearance / 2f);
            if (placed.Any(p => Overlap(p, candidate))) return "another stall";
            return keepFrontages && frontages.Any(f => Overlap(f, new Placed(x, y, yaw, hw, hd))) ? "a stall's frontage" : null;
        }

        var vignettes = market.Vignettes.ToDictionary(v => v.Name);
        var kitDressed = 0;
        var footprints = new List<MarketFootprint>();

        // One piece in a frame at (ox, oy, oz) turned by frameYaw, mirrored across its X.
        // Tilts are about the piece's own axes, split into the world-axis X and Y rotations
        // Skyrim applies after Z (as the Dragonsreach banners showed).
        void PutPiece(MarketPiece piece, float ox, float oy, float oz, float frameYaw, float mirror, float jitter, int seed)
        {
            if (piece.Reserve)
            {
                mod.GetNextFormKey();
                return;
            }

            var (rx, ry) = (MathF.Cos(frameYaw * Deg), -MathF.Sin(frameYaw * Deg));
            var (fx, fy) = (MathF.Sin(frameYaw * Deg), MathF.Cos(frameYaw * Deg));
            jitter = piece.Exact ? 0f : jitter;
            var u = mirror * piece.X + FairHash.Signed(seed, 11, 90) * jitter;
            var v = piece.Y + FairHash.Signed(seed, 12, 90) * jitter;
            var px = ox + u * rx + v * fx;
            var py = oy + u * ry + v * fy;
            var yaw = (frameYaw + mirror * piece.Yaw + FairHash.Signed(seed, 13, 90) * jitter * 1.5f) * Deg;
            var (a, t) = (piece.RotX * Deg, mirror * piece.RotY * Deg);
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(piece.Piece)),
                Scale = piece.Scale == 1f ? null : piece.Scale,
                Placement = new Placement
                {
                    Position = new P3Float(px, py, oz + piece.Z),
                    Rotation = new P3Float(a * MathF.Cos(yaw) + t * MathF.Sin(yaw), -a * MathF.Sin(yaw) + t * MathF.Cos(yaw), yaw),
                },
            });
            pieceCount++;
        }

        // A vignette's pieces round a point in the stall's frame.
        void PutVignette(string name, float x, float y, float z, float yaw, float mirror, int seed)
        {
            if (!vignettes.TryGetValue(name, out var vignette))
            {
                throw new InvalidOperationException($"Stall kit names vignette '{name}', which fairWorld.market.vignettes does not define.");
            }

            var k = 0;
            foreach (var piece in vignette.Pieces)
            {
                k++;
                if (piece.Optional && FairHash.Hash3(seed, k, 91) < 0.4f)
                {
                    continue;
                }

                PutPiece(piece, x, y, z, yaw, mirror, 2f, seed * 37 + k);
            }
        }

        // Dress a committed stall with its theme's kit, slot by slot.
        void Dress(MarketModule m, float x, float y, float yaw, float mirror, string theme, int seed)
        {
            var kit = market.StallKits.FirstOrDefault(k => k.Themes.Contains(theme));
            if (kit is null)
            {
                return;
            }

            kitDressed++;
            var (rx, ry) = (MathF.Cos(yaw * Deg), -MathF.Sin(yaw * Deg));
            var (fx, fy) = (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg));
            (float X, float Y) At(float u, float v) => (x + mirror * u * rx + v * fx, y + mirror * u * ry + v * fy);
            var n = 0;

            // Counter strips: vignettes laid left to right, cycling the kit's list.
            var pick = (int)(FairHash.Hash3(seed, 1, 92) * 97);
            foreach (var strip in m.Slots.Counter.Where(c => c.Length == 4))
            {
                var (x0, x1, sv, sz) = (strip[0], strip[1], strip[2], strip[3]);
                var cursor = x0;
                while (kit.Counter.Count > 0)
                {
                    var vig = vignettes[kit.Counter[pick++ % kit.Counter.Count]];
                    if (cursor + vig.Width > x1 + 4f) break;
                    var (px, py) = At(cursor + vig.Width / 2f, sv);
                    PutVignette(vig.Name, px, py, ground(px, py) + sz, yaw, mirror, seed * 53 + n++);
                    cursor += vig.Width + 6f;
                }
            }

            // Hang lines: goods every HangSpacing, a little uneven.
            foreach (var strip in m.Slots.Hang.Where(c => c.Length == 4))
            {
                if (kit.Hang.Count == 0) break;
                var (x0, x1, sv, sz) = (strip[0], strip[1], strip[2], strip[3]);
                for (var u = x0; u <= x1; u += kit.HangSpacing)
                {
                    var uu = u + FairHash.Signed(seed, n, 93) * kit.HangSpacing * 0.2f;
                    var (px, py) = At(uu, sv);
                    PutVignette(kit.Hang[(pick++) % kit.Hang.Count], px, py, ground(px, py) + sz, yaw, mirror, seed * 53 + n++);
                }
            }

            // Ground spots: one vignette each, or none now and then.
            var used = new HashSet<string>();
            void Spots(List<float[]> spots, List<string> choices, int salt)
            {
                foreach (var spot in spots.Where(sp => sp.Length >= 2))
                {
                    n++;
                    if (choices.Count == 0 || FairHash.Hash3(seed, n, salt) < kit.EmptyChance) continue;
                    var (px, py) = At(spot[0], spot[1]);

                    // Each ground vignette once per stall (one spit fire, not two).
                    var start = (int)(FairHash.Hash3(seed, n, salt + 1) * choices.Count) % choices.Count;
                    var name = Enumerable.Range(0, choices.Count).Select(k => choices[(start + k) % choices.Count]).FirstOrDefault(c => !used.Contains(c));
                    if (name is null) continue;
                    used.Add(name);
                    PutVignette(name, px, py, ground(px, py), yaw + mirror * (spot.Length > 2 ? spot[2] : 0f), mirror, seed * 53 + n);
                }
            }

            Spots(m.Slots.Side, kit.Side, 94);
            Spots(m.Slots.Rear, kit.Rear, 96);
            if (kit.Sign.Count > 0 && m.Slots.Sign.Count > 0)
            {
                var spot = m.Slots.Sign[(int)(FairHash.Hash3(seed, 3, 98) * m.Slots.Sign.Count) % m.Slots.Sign.Count];
                var (px, py) = At(spot[0], spot[1]);
                var name = kit.Sign[(int)(FairHash.Hash3(seed, 4, 98) * kit.Sign.Count) % kit.Sign.Count];
                PutVignette(name, px, py, ground(px, py), yaw + mirror * (spot.Length > 2 ? spot[2] : 0f), mirror, seed * 53 + 999);
            }

            if (kit.Light.Length > 0)
            {
                var (lx, ly) = At(kit.LightAt[0], kit.LightAt[1]);
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(kit.Light)),
                    Placement = new Placement { Position = new P3Float(lx, ly, ground(lx, ly) + kit.LightAt[2]), Rotation = new P3Float(0f, 0f, 0f) },
                });
                lights++;
            }
        }

        void Commit(MarketModule m, float x, float y, float yaw, string laneName, string theme, int seedA, int seedB)
        {
            var mirror = FairHash.Hash3(seedA, seedB, 61) < 0.5f ? -1f : 1f;
            var (rx, ry) = (MathF.Cos(yaw * Deg), -MathF.Sin(yaw * Deg));
            var (fx, fy) = (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg));
            var k = 0;
            foreach (var piece in m.Pieces)
            {
                k++;
                if (piece.Optional && FairHash.Hash3(seedA * 31 + k, seedB, 62) < 0.35f)
                {
                    continue;
                }

                if (piece.Reserve)
                {
                    mod.GetNextFormKey();
                    continue;
                }

                var wobble = piece.Exact ? 0f : 1f;
                var u = mirror * piece.X + FairHash.Signed(seedA * 31 + k, seedB, 63) * 5f * wobble;
                var v = piece.Y + FairHash.Signed(seedA * 31 + k, seedB, 64) * 5f * wobble;
                var px = x + u * rx + v * fx;
                var py = y + u * ry + v * fy;
                var pieceYaw = (yaw + mirror * piece.Yaw + FairHash.Signed(seedA * 31 + k, seedB, 65) * 3f * wobble) * Deg;
                var (a, t) = (piece.RotX * Deg, mirror * piece.RotY * Deg);
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(piece.Piece)),
                    Scale = piece.Scale == 1f ? null : piece.Scale,
                    Placement = new Placement
                    {
                        Position = new P3Float(px, py, ground(px, py) + piece.Z),
                        Rotation = new P3Float(a * MathF.Cos(pieceYaw) + t * MathF.Sin(pieceYaw), -a * MathF.Sin(pieceYaw) + t * MathF.Cos(pieceYaw), pieceYaw),
                    },
                });
                pieceCount++;
            }

            Dress(m, x, y, yaw, mirror, theme, seedA * 131 + seedB);

            foreach (var extra in market.ThemeDressing.Where(t => t.Theme == theme).SelectMany(t => t.Pieces))
            {
                var px = x + mirror * extra.X * rx + extra.Y * fx;
                var py = y + mirror * extra.X * ry + extra.Y * fy;
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(extra.Piece)),
                    Placement = new Placement
                    {
                        Position = new P3Float(px, py, ground(px, py) + extra.Z),
                        Rotation = new P3Float(0f, 0f, (yaw + extra.Yaw) * Deg),
                    },
                });
                pieceCount++;
            }

            placed.Add(new Placed(x, y, yaw, m.Width / 2f + market.Clearance / 2f, m.Depth / 2f + market.Clearance / 2f));
            footprints.Add(new MarketFootprint("stall", x, y, yaw, m.Width / 2f, m.Depth / 2f));

            // Keep the ground in front of the counter clear, so its keeper can be reached.
            var reach = m.Depth / 2f + market.FrontageDepth / 2f;
            var (sfx, sfy) = (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg));
            frontages.Add(new Placed(x + sfx * reach, y + sfy * reach, yaw, m.Width / 2f - 10f, market.FrontageDepth / 2f));
            footprints.Add(new MarketFootprint("frontage", x + sfx * reach, y + sfy * reach, yaw, m.Width / 2f - 10f, market.FrontageDepth / 2f));

            // The shell marker stands at the stall's front edge, facing into it.
            var number = themeCounts[theme] = themeCounts.GetValueOrDefault(theme) + 1;
            var mx = x + fx * (m.Depth / 2f - 20f);
            var my = y + fy * (m.Depth / 2f - 20f);
            var marker = new PlacedObject(mod)
            {
                EditorID = $"{market.ShellMarkerPrefix}{Capitalise(theme)}{number:00}",
                MajorRecordFlagsRaw = persistentFlag,
                Base = new FormLinkNullable<IPlaceableObjectGetter>(markerBase),
                Placement = new Placement
                {
                    Position = new P3Float(mx, my, ground(mx, my)),
                    Rotation = new P3Float(0f, 0f, (yaw + 180f) * Deg),
                },
            };
            persistentCell.Persistent.Add(marker);
            var vendors = m.VendorSpots
                .Where(v => v.Length == 2)
                .Select(v => (X: x + mirror * v[0] * rx + v[1] * fx, Y: y + mirror * v[0] * ry + v[1] * fy))
                .ToList();

            stalls.Add(new MarketStall(laneName, m.Name, theme, marker.EditorID, x, y, yaw, m.Width, m.Depth, vendors));
        }

        // Dressing (seating): the module's pieces and footprint, but no stall or shell marker.
        void CommitDressing(MarketModule m, float x, float y, float yaw, int seedA, int seedB)
        {
            var (rx, ry) = (MathF.Cos(yaw * Deg), -MathF.Sin(yaw * Deg));
            var (fx, fy) = (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg));
            var k = 0;
            foreach (var piece in m.Pieces)
            {
                k++;
                if (piece.Optional && FairHash.Hash3(seedA * 31 + k, seedB, 62) < 0.4f)
                {
                    continue;
                }

                if (piece.Reserve)
                {
                    mod.GetNextFormKey();
                    continue;
                }

                // Exact pieces (a sign, its posts and bar, which must meet) skip the nudge here
                // too: the archery booth is a dressing group, and its sign drifted off its bar.
                var wobble = piece.Exact ? 0f : 1f;
                var u = piece.X + FairHash.Signed(seedA * 31 + k, seedB, 63) * 6f * wobble;
                var v = piece.Y + FairHash.Signed(seedA * 31 + k, seedB, 64) * 6f * wobble;
                var px = x + u * rx + v * fx;
                var py = y + u * ry + v * fy;
                var pieceYaw = (yaw + piece.Yaw + FairHash.Signed(seedA * 31 + k, seedB, 65) * 4f * wobble) * Deg;
                var (a, t) = (piece.RotX * Deg, piece.RotY * Deg);
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(piece.Piece)),
                    Scale = piece.Scale == 1f ? null : piece.Scale,
                    Placement = new Placement
                    {
                        Position = new P3Float(px, py, ground(px, py) + piece.Z),
                        Rotation = new P3Float(a * MathF.Cos(pieceYaw) + t * MathF.Sin(pieceYaw), -a * MathF.Sin(pieceYaw) + t * MathF.Cos(pieceYaw), pieceYaw),
                    },
                });
                pieceCount++;
            }

            placed.Add(new Placed(x, y, yaw, m.Width / 2f + market.Clearance / 2f, m.Depth / 2f + market.Clearance / 2f));
            footprints.Add(new MarketFootprint(m.Name, x, y, yaw, m.Width / 2f, m.Depth / 2f));
        }

        // A stall beside a lane at a station, pushed back behind the lane edge, facing it.
        (float X, float Y, float Yaw) Beside(Lane lane, float s, float side, MarketModule m, float setBack, float turn)
        {
            var (cx, cy, tx, ty, _) = Station(lane, s);
            var (nx, ny) = (-ty * side, tx * side);  // side = +1 left, -1 right

            // Stand behind the lane's widest point across the stall's whole frontage, and
            // behind its meander there too, so a stall never juts into a pocket beside it.
            var half = 0f;
            for (var k = -2; k <= 2; k++)
            {
                var (sx, sy, _, _, sh) = Station(lane, s + k * m.Width / 4f);
                half = MathF.Max(half, sh + ((sx - cx) * nx + (sy - cy) * ny));
            }

            var off = half + m.Depth / 2f + setBack + 25f;
            var x = cx + nx * off;
            var y = cy + ny * off;
            var yaw = MathF.Atan2(-nx, -ny) / Deg + turn;
            return (x, y, yaw);
        }

        // ---- signature stalls first ---------------------------------------------------
        for (var i = 0; i < market.Fixed.Count; i++)
        {
            var f = market.Fixed[i];
            var lane = lanes.First(l => l.Config.Name == f.Lane);
            var m = modules[f.Module];
            var side = f.Side == "left" ? 1f : -1f;
            var (x, y, yaw) = Beside(lane, f.At, side, m, market.Clearance, 0f);
            if (WhyNot(m, x, y, yaw) is { } reason)
            {
                throw new InvalidOperationException(
                    $"Signature stall '{f.Theme}' at {f.Lane} {f.At} {f.Side} ({x:0}, {y:0}) collides with {reason}; " +
                    "move it in fairWorld.market.fixed.");
            }

            Commit(m, x, y, yaw, $"{lane.Config.Name} {SideName(side)}", f.Theme, 900 + i, 1);
        }

        // ---- every lane, side by side ------------------------------------------------
        foreach (var lane in lanes)
        {
            var c = lane.Config;
            if (c.Mix.Count == 0)
            {
                continue;  // a passage only: it keeps its corridor clear, but lines up no stalls
            }

            var mix = c.Mix.Select(x => (Module: modules[x.Module], x.Weight)).ToList();
            var total = mix.Sum(x => x.Weight);
            var sides = c.Sides switch
            {
                "left" => new[] { 1f },
                "right" => new[] { -1f },
                _ => new[] { 1f, -1f },
            };

            var themeIndex = 0;  // one sequence per lane, left side then right, so no theme repeats
            foreach (var side in sides)
            {
                var seedA = lane.Index * 2 + (side > 0 ? 0 : 1);
                var s = MathF.Max(0f, c.From) + FairHash.Hash3(seedA, 0, 66) * c.GapMax;
                var end = MathF.Min(c.To, lane.Length);
                var n = 0;
                while (s < end)
                {
                    n++;
                    var pick = FairHash.Hash3(seedA, n, 67) * total;
                    var m = mix[^1].Module;
                    foreach (var (module, weight) in mix)
                    {
                        pick -= weight;
                        if (pick < 0f)
                        {
                            m = module;
                            break;
                        }
                    }

                    if (s + m.Width > end)
                    {
                        // Near the end of the lane, finish with the smallest stall that fits, or stop.
                        m = mix.Select(x => x.Module).OrderBy(x => x.Width).First();
                        if (s + m.Width > end)
                        {
                            break;
                        }
                    }

                    var setBack = market.Clearance + FairHash.Hash3(seedA, n, 68) * c.SetBack;
                    var turn = FairHash.Signed(seedA, n, 69) * c.AngleJitter;
                    var (x, y, yaw) = Beside(lane, s + m.Width / 2f, side, m, setBack, turn);
                    var why = WhyNot(m, x, y, yaw);

                    // A tight spot takes a smaller stall before it is given up on.
                    foreach (var smaller in mix.Select(x => x.Module).Where(x => x.Width < m.Width).OrderByDescending(x => x.Width))
                    {
                        if (why is null) break;
                        var (sx, sy, syaw) = Beside(lane, s + smaller.Width / 2f, side, smaller, setBack, turn);
                        if (WhyNot(smaller, sx, sy, syaw) is null)
                        {
                            (m, x, y, yaw, why) = (smaller, sx, sy, syaw, null);
                        }
                    }

                    if (why is not null)
                    {
                        refused++;
                        var reason = why.Split(" at ")[0];
                        if (Environment.GetEnvironmentVariable("SKYRIMFAIR_TRACE") is { Length: > 0 })
                        {
                            Console.Error.WriteLine($"market refuse {c.Name} side {side} s {s:0} {m.Name} at ({x:0}, {y:0}): {why}");
                        }
                        reasons[$"{c.Name}: {reason}"] = reasons.GetValueOrDefault($"{c.Name}: {reason}") + 1;
                        s += RetryStep;
                        continue;
                    }

                    var theme = c.Themes.Count == 0 ? "stall" : c.Themes[themeIndex++ % c.Themes.Count];
                    Commit(m, x, y, yaw, $"{c.Name} {SideName(side)}", theme, seedA, n);
                    s += m.Width + (FairHash.Hash3(seedA, n, 70) < c.PocketChance
                        ? c.PocketMin + FairHash.Hash3(seedA, n, 71) * (c.PocketMax - c.PocketMin)
                        : c.GapMin + FairHash.Hash3(seedA, n, 72) * (c.GapMax - c.GapMin));
                }
            }
        }

        // ---- seating: picnic sets along lanes ---------------------------------------
        var seats = 0;
        var dressingRuns = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var dressingRefusals = new SortedDictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < market.Seating.Count; i++)
        {
            var seating = market.Seating[i];
            var lane = lanes.First(l => l.Config.Name == seating.Lane);
            var choices = (seating.Modules.Count > 0 ? seating.Modules : new List<string> { seating.Module })
                .Select(n => modules[n]).ToList();
            var k = 0;
            for (var s = seating.From; s <= MathF.Min(seating.To, lane.Length); s += seating.Spacing)
            {
                k++;
                if (FairHash.Hash3(500 + i, k, 76) >= seating.Chance)
                {
                    continue;
                }

                var m = choices[(int)(FairHash.Hash3(500 + i, k, 77) * choices.Count) % choices.Count];
                var (cx, cy, tx, ty, _) = Station(lane, s);
                var x = cx - ty * seating.Offset;
                var y = cy + tx * seating.Offset;
                // The module's long side (its local X) runs along the lane.
                var yaw = MathF.Atan2(tx, ty) / Deg - 90f + FairHash.Signed(500 + i, k, 75) * seating.AngleJitter;
                var why = WhyNot(m, x, y, yaw, ignore: seating.Offset == 0f ? lane : null, wallMargin: seating.WallMargin, keepFrontages: true);
                var runName = $"{seating.Lane} {(seating.Modules.Count > 0 ? string.Join("/", seating.Modules) : seating.Module)} @{seating.Offset:0}";
                if (why is null)
                {
                    CommitDressing(m, x, y, yaw, 500 + i, k);
                    seats++;
                    dressingRuns[runName] = dressingRuns.GetValueOrDefault(runName) + 1;
                }
                else
                {
                    var reason = $"{runName}: {why.Split(" at ")[0]}";
                    dressingRefusals[reason] = dressingRefusals.GetValueOrDefault(reason) + 1;
                }
            }
        }

        // ---- hand-placed dressing that marks the lane structure ---------------------
        // The late list (built at the end, FairWorld) comes through here too, also kept clear
        // of the NPCs placed by then; it returns where the group stood, or null if refused.
        (float X, float Y)? PlaceDressing(MarketDressing d, int seed, IReadOnlyList<(float X, float Y)> actors)
        {
            if (d.Module.Length > 0)
            {
                exempt = d.ExemptKeepOut;
                var group = modules[d.Module];
                string? Fits(float x, float y)
                {
                    if (WhyNot(group, x, y, d.Yaw, wallMargin: 60f, keepFrontages: true) is { } why) return why;
                    var body = new Placed(x, y, d.Yaw, group.Width / 2f + ActorClearance, group.Depth / 2f + ActorClearance);
                    return actors.Any(a => Overlap(body, new Placed(a.X, a.Y, 0f, 1f, 1f))) ? "an NPC" : null;
                }

                var (gx, gy) = (d.X, d.Y);
                var why = d.Force ? null : Fits(gx, gy);

                // Spiral out from the requested point until the group fits.
                for (var r = 60f; why is not null && r <= d.SearchRadius; r += 60f)
                {
                    for (var a = 0; a < 360 && why is not null; a += 30)
                    {
                        var (tx, ty) = (d.X + r * MathF.Sin(a * Deg), d.Y + r * MathF.Cos(a * Deg));
                        if (Fits(tx, ty) is null)
                        {
                            (gx, gy, why) = (tx, ty, null);
                        }
                    }
                }

                exempt = Array.Empty<string>();
                if (why is null)
                {
                    CommitDressing(group, gx, gy, d.Yaw, seed, 1);
                    seats++;
                    dressingRuns[$"group {d.Module}"] = dressingRuns.GetValueOrDefault($"group {d.Module}") + 1;
                    return (gx, gy);
                }

                dressingRefusals[$"group {d.Module} at ({d.X:0}, {d.Y:0}): {why.Split(" at ")[0]}"] = 1;
                return null;
            }

            if (d.Reserve)
            {
                mod.GetNextFormKey();
                return null;
            }

            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(d.Piece)),
                Placement = new Placement
                {
                    Position = new P3Float(d.X, d.Y, ground(d.X, d.Y) + d.Z),
                    Rotation = new P3Float(0f, 0f, d.Yaw * Deg),
                },
            });
            pieceCount++;
            return (d.X, d.Y);
        }

        for (var di = 0; di < market.Dressing.Count; di++)
        {
            PlaceDressing(market.Dressing[di], 800 + di, Array.Empty<(float X, float Y)>());
        }

        // ---- back-to-back infill ------------------------------------------------------
        if (market.BackFill.Count > 0)
        {
            var fronts = stalls.ToList();
            var theme = 0;
            for (var i = 0; i < fronts.Count; i++)
            {
                var front = fronts[i];
                var (fx, fy) = (MathF.Sin(front.Yaw * Deg), MathF.Cos(front.Yaw * Deg));
                // Prefer the stall closest in frontage to the one in front, so the back row
                // lines up with it instead of overlapping its neighbours.
                var candidates = market.BackFill
                    .Select(n => modules[n])
                    .OrderBy(x => x.Width > front.Width + 20f ? 1 : 0)
                    .ThenBy(x => MathF.Abs(x.Width - front.Width))
                    .ToList();
                foreach (var m in candidates)
                {
                    var name = m.Name;
                    var back = front.Depth / 2f + m.Depth / 2f + market.Clearance + 40f;
                    var (x, y) = (front.X - fx * back, front.Y - fy * back);
                    var yaw = front.Yaw + 180f + FairHash.Signed(i, 3, 74) * 5f;
                    var whyBack = WhyNot(m, x, y, yaw);
                    if (whyBack is not null && Environment.GetEnvironmentVariable("SKYRIMFAIR_TRACE") is { Length: > 0 })
                    {
                        Console.Error.WriteLine($"market backfill refuse behind {front.Lane} ({front.X:0}, {front.Y:0}) {name} at ({x:0}, {y:0}): {whyBack}");
                    }

                    if (whyBack is null)
                    {
                        var t = market.BackFillThemes.Count == 0 ? "stall" : market.BackFillThemes[theme++ % market.BackFillThemes.Count];
                        Commit(m, x, y, yaw, front.Lane + " (behind)", t, 700 + i, 5);
                        break;
                    }
                }
            }
        }

        // ---- overhead festival lines ----------------------------------------------------
        var crossings = 0;
        var poles = new List<(float X, float Y)>();
        var poleModule = new MarketModule { Name = "pole", Width = 40f, Depth = 40f };
        for (var ri = 0; ri < world.Overhead.Count; ri++)
        {
            var run = world.Overhead[ri];
            var lane = lanes.First(l => l.Config.Name == run.Lane);
            var ropes = run.Ropes.Count > 0 ? run.Ropes : new List<string> { run.Rope };
            var k = 0;
            for (var at = run.From; at <= MathF.Min(run.To, lane.Length); at += run.Spacing)
            {
                k++;
                if (FairHash.Hash3(600 + ri, k, 99) >= run.Chance)
                {
                    continue;
                }

                var (cx, cy, tx, ty, half) = Station(lane, at);
                var ends = new List<(float X, float Y)>();
                foreach (var side in new[] { 1f, -1f })
                {
                    var (nx, ny) = (-ty * side, tx * side);
                    (float X, float Y)? found = null;
                    foreach (var slide in new[] { 0f, 40f, -40f, 80f, -80f, 120f, -120f, 160f, -160f, 200f, -200f, 240f, -240f })
                    {
                        var off = half + market.Clearance + run.PoleMargin;
                        var (px, py) = (cx + nx * off + tx * slide, cy + ny * off + ty * slide);

                        // A pole may stand at the end of a stall's frontage but not across the
                        // middle of its counter.
                        var core = new Placed(px, py, 0f, 12f, 12f);
                        if (frontages.Any(f => Overlap(f with { HalfW = f.HalfW * 0.6f }, core))) continue;
                        if (WhyNot(poleModule, px, py, 0f, ignore: lane, wallMargin: 60f) is null && !poles.Any(q => MathF.Abs(q.X - px) < 60f && MathF.Abs(q.Y - py) < 60f))
                        {
                            found = (px, py);
                            break;
                        }
                    }

                    if (found is { } f) ends.Add(f);
                }

                if (ends.Count < 2)
                {
                    dressingRefusals[$"overhead {run.Lane} at {at:0}: no room for a pole"] = 1;
                    continue;
                }


                var pole = run.Pole.Length > 0 ? resolve(run.Pole) : FormKey.Null;
                var cap = run.PoleCap.Length > 0 ? resolve(run.PoleCap) : FormKey.Null;
                var top = run.PoleHeight * run.PoleStack + (cap.IsNull ? 0f : run.PoleCapHeight);
                foreach (var e in ends)
                {
                    poles.Add(e);
                    placed.Add(new Placed(e.X, e.Y, 0f, 25f, 25f));
                    footprints.Add(new MarketFootprint("pole", e.X, e.Y, 0f, 12f, 12f));
                    for (var i = 0; i < run.PoleStack && !pole.IsNull; i++)
                    {
                        put(new PlacedObject(mod)
                        {
                            Base = new FormLinkNullable<IPlaceableObjectGetter>(pole),
                            Placement = new Placement
                            {
                                Position = new P3Float(e.X, e.Y, ground(e.X, e.Y) - 6f + i * run.PoleHeight),
                                Rotation = new P3Float(0f, 0f, FairHash.Hash3(ri, k * 7 + i, 88) * 6.28f),
                            },
                        });
                        pieceCount++;
                    }

                    if (!cap.IsNull)
                    {
                        put(new PlacedObject(mod)
                        {
                            Base = new FormLinkNullable<IPlaceableObjectGetter>(cap),
                            Placement = new Placement
                            {
                                Position = new P3Float(e.X, e.Y, ground(e.X, e.Y) - 6f + run.PoleStack * run.PoleHeight),
                                Rotation = new P3Float(0f, 0f, FairHash.Hash3(ri, k * 7 + 5, 88) * 6.28f),
                            },
                        });
                        pieceCount++;
                    }
                }

                // Two mirrored halves of the festival line meet at the low middle: each half
                // starts 53 from its origin, runs 682 along its local (+X, -Y) diagonal (heading
                // 134.7) and rises 150, all times its scale.
                var (mx, my) = ((ends[0].X + ends[1].X) / 2f, (ends[0].Y + ends[1].Y) / 2f);
                var topZ = MathF.Max(ground(ends[0].X, ends[0].Y), ground(ends[1].X, ends[1].Y)) + top - 8f;
                var li = 0;
                foreach (var e in ends)
                {
                    var (dx, dy) = (e.X - mx, e.Y - my);
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    var scale = d / 682f;
                    var (ux, uy) = (dx / d, dy / d);
                    var heading = MathF.Atan2(ux, uy) / Deg;
                    var (ox, oy) = (mx - ux * 53f * scale, my - uy * 53f * scale);
                    var oz = topZ - 150f * scale;
                    var rope = resolve(ropes[(k - 1 + (e == ends[0] ? 0 : 1)) % ropes.Count]);
                    put(new PlacedObject(mod)
                    {
                        Base = new FormLinkNullable<IPlaceableObjectGetter>(rope),
                        Scale = scale,
                        Placement = new Placement
                        {
                            Position = new P3Float(ox, oy, oz),
                            Rotation = new P3Float(0f, 0f, (heading - 134.7f) * Deg),
                        },
                    });
                    pieceCount++;

                    // Lanterns along the half, following the rope's rise (about quadratic).
                    for (var along = run.LanternSpacing * 0.5f; along < d - 30f && run.Lanterns.Count > 0; along += run.LanternSpacing)
                    {
                        var frac = along / d;
                        var (lx, ly) = (mx + ux * along, my + uy * along);
                        put(new PlacedObject(mod)
                        {
                            Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(run.Lanterns[(k + li++) % run.Lanterns.Count])),
                            Placement = new Placement
                            {
                                Position = new P3Float(lx, ly, oz + 150f * scale * frac * frac - 4f),
                                Rotation = new P3Float(0f, 0f, heading * Deg),
                            },
                        });
                        pieceCount++;
                    }
                }

                crossings++;
            }
        }

        return new MarketResult(stalls, pieceCount, refused, reasons, seats, dressingRuns, dressingRefusals)
        {
            KitDressed = kitDressed,
            Lights = lights,
            Crossings = crossings,
            Footprints = footprints,
            PlaceLate = actors =>
            {
                var at = new List<(string Module, float X, float Y)?>();
                for (var li = 0; li < market.LateDressing.Count; li++)
                {
                    var d = market.LateDressing[li];
                    at.Add(PlaceDressing(d, 1800 + li, actors) is { } p ? (d.Module.Length > 0 ? d.Module : d.Piece, p.X, p.Y) : null);
                }

                return at;
            },
            PlaceMore = (list, seedBase, actors) =>
                list.Select((d, i) => PlaceDressing(d, seedBase + i, actors) is { } p ? ((d.Module.Length > 0 ? d.Module : d.Piece), p.X, p.Y) : ((string, float, float)?)null).ToList(),
            PlacePlants = (plants, wallSpots, actors) =>
            {
                // Candidates: the strip inside the wall (given), then each lane's edges.
                var spots = new List<(float X, float Y, int Seed)>();
                for (var i = 0; i < wallSpots.Count; i++)
                {
                    if (FairHash.Hash3(900, i, 1) < plants.WallChance) spots.Add((wallSpots[i].X, wallSpots[i].Y, 10000 + i));
                }

                for (var li = 0; li < lanes.Count; li++)
                {
                    var lane = lanes[li];
                    var k = 0;
                    for (var at = plants.LaneSpacing / 2f; at < lane.Length; at += plants.LaneSpacing)
                    {
                        var (cx, cy, tx, ty, half) = Station(lane, at);
                        foreach (var side in new[] { 1f, -1f })
                        {
                            k++;
                            if (FairHash.Hash3(901 + li, k, 2) >= plants.LaneChance) continue;
                            var off = half + market.Clearance + plants.LaneBeyond + FairHash.Hash3(901 + li, k, 3) * 60f;
                            var slide = FairHash.Signed(901 + li, k, 4) * plants.LaneSpacing * 0.3f;
                            spots.Add((cx - ty * side * off + tx * slide, cy + tx * side * off + ty * slide, 20000 + li * 1000 + k));
                        }
                    }
                }

                var plant = new MarketModule { Name = "plant", Width = plants.Size, Depth = plants.Size };
                var count = 0;
                foreach (var (x, y, seed) in spots)
                {
                    if (WhyNot(plant, x, y, 0f, wallMargin: 40f, keepFrontages: true) is not null) continue;
                    var clear = plants.ActorClearance;
                    if (actors.Any(a => MathF.Abs(a.X - x) < clear && MathF.Abs(a.Y - y) < clear)) continue;
                    var flower = plants.Flowers.Count > 0 && FairHash.Hash3(seed, 1, 5) < plants.FlowerShare;
                    var pool = flower ? plants.Flowers : plants.Pieces;
                    var piece = pool[(int)(FairHash.Hash3(seed, 2, 5) * pool.Count) % pool.Count];
                    var scale = plants.Scale[0] + FairHash.Hash3(seed, 3, 5) * (plants.Scale[1] - plants.Scale[0]);
                    put(new PlacedObject(mod)
                    {
                        Base = new FormLinkNullable<IPlaceableObjectGetter>(resolve(piece)),
                        Scale = scale,
                        Placement = new Placement
                        {
                            Position = new P3Float(x, y, ground(x, y) - plants.Sink),
                            Rotation = new P3Float(0f, 0f, FairHash.Hash3(seed, 4, 5) * MathF.PI * 2f),
                        },
                    });
                    placed.Add(new Placed(x, y, 0f, plants.Size / 2f, plants.Size / 2f));
                    pieceCount++;
                    count++;
                }

                return count;
            },
        };
    }

    private static Lane BuildLane(MarketLane c, int index, FairWorldConfig world)
    {
        var points = (c.UseAvenue ? world.Avenue : c.Points).Select(p => (p[0], p[1])).ToArray();
        var arc = new float[points.Length];
        for (var i = 1; i < points.Length; i++)
        {
            var dx = points[i].Item1 - points[i - 1].Item1;
            var dy = points[i].Item2 - points[i - 1].Item2;
            arc[i] = arc[i - 1] + MathF.Sqrt(dx * dx + dy * dy);
        }

        var lane = new Lane(c, index, points, arc, arc[^1], FairHash.Hash3(index, 7, 73) * MathF.PI * 2f,
            new List<(float, float, float, float, float)>());
        for (var s = 0f; s <= lane.Length; s += SampleStep)
        {
            lane.Samples.Add(Station(lane, s));
        }

        // Round the lane's ends so nothing stands right across its mouth.
        foreach (var s in new[] { 0f, lane.Length })
        {
            var (x, y, tx, ty, half) = Station(lane, s);
            for (var k = 1; k <= (int)(half / SampleStep); k++)
            {
                var d = (s == 0f ? -1f : 1f) * k * SampleStep;
                lane.Samples.Add((x + tx * d, y + ty * d, tx, ty, MathF.Sqrt(MathF.Max(0f, half * half - d * d))));
            }
        }

        return lane;
    }

    /// <summary>Lane centre (with its meander), direction and half-width at distance <paramref name="s"/>.</summary>
    private static (float X, float Y, float Tx, float Ty, float Half) Station(Lane lane, float s)
    {
        s = Math.Clamp(s, 0f, lane.Length);
        var i = 1;
        while (i < lane.Points.Length - 1 && lane.Arc[i] < s) i++;
        var a = lane.Points[i - 1];
        var b = lane.Points[i];
        var segment = lane.Arc[i] - lane.Arc[i - 1];
        var t = segment <= 0f ? 0f : (s - lane.Arc[i - 1]) / segment;
        var (tx, ty) = ((b.X - a.X) / segment, (b.Y - a.Y) / segment);
        var (amp, period) = (lane.Config.Meander[0], MathF.Max(1f, lane.Config.Meander[1]));
        var wander = amp * MathF.Sin(2f * MathF.PI * s / period + lane.Phase);
        var x = a.X + (b.X - a.X) * t - ty * wander;
        var y = a.Y + (b.Y - a.Y) * t + tx * wander;
        return (x, y, tx, ty, HalfWidth(lane.Config.HalfWidths, s));
    }

    private static float HalfWidth(List<float[]> stations, float s)
    {
        if (stations.Count == 0) return 250f;
        if (s <= stations[0][0]) return stations[0][1];
        for (var i = 1; i < stations.Count; i++)
        {
            if (s <= stations[i][0])
            {
                var (s0, h0, s1, h1) = (stations[i - 1][0], stations[i - 1][1], stations[i][0], stations[i][1]);
                return h0 + (h1 - h0) * (s - s0) / MathF.Max(1f, s1 - s0);
            }
        }

        return stations[^1][1];
    }

    /// <summary>Separating-axis test for two rotated rectangles.</summary>
    private static bool Overlap(Placed a, Placed b)
    {
        foreach (var yaw in new[] { a.Yaw, b.Yaw })
        {
            foreach (var axis in new[] { (MathF.Cos(yaw * Deg), -MathF.Sin(yaw * Deg)), (MathF.Sin(yaw * Deg), MathF.Cos(yaw * Deg)) })
            {
                float Extent(Placed p)
                {
                    var (rx, ry) = (MathF.Cos(p.Yaw * Deg), -MathF.Sin(p.Yaw * Deg));
                    var (fx, fy) = (MathF.Sin(p.Yaw * Deg), MathF.Cos(p.Yaw * Deg));
                    return p.HalfW * MathF.Abs(rx * axis.Item1 + ry * axis.Item2)
                        + p.HalfD * MathF.Abs(fx * axis.Item1 + fy * axis.Item2);
                }

                var gap = MathF.Abs((b.X - a.X) * axis.Item1 + (b.Y - a.Y) * axis.Item2);
                if (gap > Extent(a) + Extent(b))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Which side of the direction of travel: left is +1.</summary>
    private static string SideName(float side) => side > 0 ? "left" : "right";

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}

internal sealed record MarketStall(
    string Lane, string Module, string Theme, string MarkerEditorId, float X, float Y, float Yaw, float Width, float Depth,
    IReadOnlyList<(float X, float Y)> Vendors);

internal sealed record MarketResult(
    IReadOnlyList<MarketStall> Stalls, int Pieces, int Refused, IReadOnlyDictionary<string, int> Reasons, int Seating,
    IReadOnlyDictionary<string, int> DressingRuns, IReadOnlyDictionary<string, int> DressingRefusals)
{
    public int KitDressed { get; init; }

    public int Lights { get; init; }

    public int Crossings { get; init; }

    /// <summary>Every stall, dressing group and pole footprint, for the crowds and the ground's wear.</summary>
    public IReadOnlyList<MarketFootprint> Footprints { get; init; } = Array.Empty<MarketFootprint>();

    /// <summary>
    /// Places <see cref="MarketConfig.LateDressing"/>, clear of the given NPC positions, with the
    /// market's own fit checks. Called once, at the end of the build: where each entry stood, or null.
    /// </summary>
    public Func<IReadOnlyList<(float X, float Y)>, IReadOnlyList<(string Module, float X, float Y)?>> PlaceLate { get; init; } =
        _ => Array.Empty<(string, float, float)?>();

    /// <summary>As <see cref="PlaceLate"/>, for another list (the life dressing), with its own seeds from the given base.</summary>
    public Func<IReadOnlyList<MarketDressing>, int, IReadOnlyList<(float X, float Y)>, IReadOnlyList<(string Module, float X, float Y)?>> PlaceMore { get; init; } =
        (_, _, _) => Array.Empty<(string, float, float)?>();

    /// <summary>
    /// Plants at the given spots inside the wall and along the lanes' edges, each only where
    /// the market's fit check passes and clear of the NPCs: how many were placed.
    /// </summary>
    public Func<LifePlants, IReadOnlyList<(float X, float Y)>, IReadOnlyList<(float X, float Y)>, int> PlacePlants { get; init; } =
        (_, _, _) => 0;
}

/// <summary>A placed rectangle: half-extents along the thing's own X (width) and Y (depth).</summary>
internal sealed record MarketFootprint(string Kind, float X, float Y, float Yaw, float HalfW, float HalfD)
{
    private const float Deg = MathF.PI / 180f;

    public bool Contains(float px, float py, float margin)
    {
        var (dx, dy) = (px - X, py - Y);
        var u = dx * MathF.Cos(Yaw * Deg) - dy * MathF.Sin(Yaw * Deg);
        var v = dx * MathF.Sin(Yaw * Deg) + dy * MathF.Cos(Yaw * Deg);
        return MathF.Abs(u) <= HalfW + margin && MathF.Abs(v) <= HalfD + margin;
    }
}
