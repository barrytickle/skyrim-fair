using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// More life in the fair (<see cref="LifeConfig"/>): banners and pennant ropes on the
/// palisade, market dressing (drying lines, lantern posts, tools, produce, a goat pen),
/// shrubs and flowers, smoke over the fires, and animals of the fair's own. Called with
/// the mod's counter on the life range, so its records renumber nothing else; within the
/// range they are made in this order, so a new kind of thing goes at the end.
/// </summary>
internal static class FairLife
{
    private const float Deg = MathF.PI / 180f;

    public static LifeResult Build(
        SkyrimMod mod, FairWorldConfig world, ISkyrimModGetter master, IReadOnlyList<WallPanel> panels,
        IReadOnlyList<(float X, float Y)> perimeter, Func<float, float, float> ground,
        IReadOnlyDictionary<(int X, int Y), Cell> cells, MarketResult? market, IReadOnlyList<(float X, float Y)> actors,
        Action<PlacedObject> put, Action<PlacedNpc> putNpc)
    {
        var life = world.Life;
        var result = new LifeResult();
        var (cx, cy) = (perimeter.Average(p => p.X), perimeter.Average(p => p.Y));

        // ---- the palisade: banners, and pennant ropes swagged between them -----------------------
        if (life.Palisade.Enabled && life.Palisade.Banners.Count > 0)
        {
            (result.Banners, result.Ropes, result.Lanterns) = DecoratePalisade(mod, life.Palisade, world.Palisade, world.Gate, panels, (cx, cy), false, put);
        }

        // ---- market dressing: drying lines, lantern posts, tools, produce, the goat pen ----------
        if (market is not null && life.Dressing.Count > 0)
        {
            var at = market.PlaceMore(life.Dressing, 2600, actors);
            result.Dressing = at.Count(p => p is not null);
            result.Refused = at.Select((p, i) => p is null ? $"{life.Dressing[i].Module} at ({life.Dressing[i].X:0}, {life.Dressing[i].Y:0})" : null)
                .OfType<string>().ToList();
        }

        // ---- plants: a strip inside the wall, and the lanes' edges -----------------------------------
        if (market is not null && life.Plants.Enabled && life.Plants.Pieces.Count > 0)
        {
            var pl = life.Plants;
            var spots = new List<(float X, float Y)>();
            for (var i = 0; i < perimeter.Count; i++)
            {
                var (a, b) = (perimeter[i], perimeter[(i + 1) % perimeter.Count]);
                var length = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var (ux, uy) = ((b.X - a.X) / length, (b.Y - a.Y) / length);
                var (nx, ny) = (-uy, ux);
                if ((cx - a.X) * nx + (cy - a.Y) * ny < 0f)
                {
                    (nx, ny) = (-nx, -ny);
                }

                var k = 0;
                for (var s = pl.WallSpacing / 2f; s < length; s += pl.WallSpacing)
                {
                    k++;
                    var inset = pl.WallInset[0] + FairHash.Hash3(950 + i, k, 1) * (pl.WallInset[1] - pl.WallInset[0]);
                    var along = s + FairHash.Signed(950 + i, k, 2) * pl.WallSpacing * 0.3f;
                    spots.Add((a.X + ux * along + nx * inset, a.Y + uy * along + ny * inset));
                }
            }

            result.Plants = market.PlacePlants(pl, spots, actors);
        }

        // ---- smoke over the fires ------------------------------------------------------------------
        if (life.Smoke.Enabled && life.Smoke.Piece.Length > 0)
        {
            var fires = life.Smoke.Fires.Select(FormKeyHelper.Parse).ToHashSet();
            var smoke = FormKeyHelper.Parse(life.Smoke.Piece);
            var at = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedObject>())
                .Where(o => fires.Contains(o.Base.FormKey))
                .OrderBy(o => o.FormKey.ID)
                .Select(o => o.Placement!.Position)
                .ToList();
            foreach (var p in at)
            {
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(smoke),
                    Scale = life.Smoke.Scale == 1f ? null : life.Smoke.Scale,
                    Placement = new Placement
                    {
                        Position = new P3Float(p.X, p.Y, p.Z + life.Smoke.Z),
                        Rotation = new P3Float(0f, 0f, 0f),
                    },
                });
                result.Smokes++;
            }
        }

        // ---- animals of the fair's own ------------------------------------------------------------
        var keyword = mod.Keywords.FirstOrDefault(k => k.EditorID == world.NpcKeyword);
        foreach (var animal in life.Animals)
        {
            var source = master.Npcs.FirstOrDefault(n => n.FormKey == FormKeyHelper.Parse(animal.Base))
                ?? throw new InvalidOperationException($"fairWorld.life.animals {animal.EditorId}: {animal.Base} is not an NPC in Skyrim.esm.");
            var npc = source.Duplicate(mod.GetNextFormKey());
            npc.EditorID = animal.EditorId;
            npc.Configuration.Flags |= NpcConfiguration.Flag.Invulnerable;
            if (keyword is not null)
            {
                npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            if (animal.Unaggressive)
            {
                npc.AIData.Aggression = Aggression.Unaggressive;
            }

            if (animal.Factions is { } factions)
            {
                npc.Factions.Clear();
                npc.Factions.AddRange(factions.Select(f => new RankPlacement { Faction = new FormLink<IFactionGetter>(FormKeyHelper.Parse(f)), Rank = 0 }));
            }

            if (animal.Packages is { } packages)
            {
                npc.Packages.Clear();
                npc.Packages.AddRange(packages.Select(p => new FormLink<IPackageGetter>(FormKeyHelper.Parse(p))));
            }

            mod.Npcs.Add(npc);

            var home = animal.Near.Length == 0
                ? null
                : market?.Footprints.FirstOrDefault(f => f.Kind == animal.Near)
                    ?? throw new InvalidOperationException($"fairWorld.life.animals {animal.EditorId}: no '{animal.Near}' was placed.");
            foreach (var at in animal.At)
            {
                var (x, y, yaw) = (at[0], at[1], at.Length > 2 ? at[2] : 0f);
                if (home is not null)
                {
                    var (c, s) = (MathF.Cos(home.Yaw * Deg), MathF.Sin(home.Yaw * Deg));
                    (x, y, yaw) = (home.X + x * c + y * s, home.Y - x * s + y * c, home.Yaw + yaw);
                }

                putNpc(new PlacedNpc(mod)
                {
                    Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                    Placement = new Placement
                    {
                        Position = new P3Float(x, y, ground(x, y) + 2f),
                        Rotation = new P3Float(0f, 0f, yaw * Deg),
                    },
                });
                result.Animals++;
            }
        }

        return result;
    }

    /// <summary>
    /// Banners on a palisade's face (the inner one, or the outer when <paramref name="outward"/>),
    /// every few panels, and pennant ropes swagged between them: how many of each were placed.
    /// </summary>
    internal static (int Banners, int Ropes, int Lanterns) DecoratePalisade(
        SkyrimMod mod, LifePalisade pal, PalisadeConfig wall, float[] gate, IReadOnlyList<WallPanel> panels,
        (float X, float Y) centre, bool outward, Action<PlacedObject> put)
    {
        var (cx, cy) = centre;
        var ropes = 0;
        var lanterns = 0;
        var pitch = wall.Width * wall.Scale * (1f - wall.Overlap);

        // Straight runs of panels, broken at corners and at the gate.
        var runs = new List<List<WallPanel>>();
        WallPanel? last = null;
        foreach (var p in panels)
        {
            var nearGate = MathF.Sqrt((p.X - gate[0]) * (p.X - gate[0]) + (p.Y - gate[1]) * (p.Y - gate[1])) < pal.GateClear;
            if (nearGate)
            {
                last = null;
                continue;
            }

            var turn = last is null ? 0f : MathF.Abs(((p.Heading - last.Heading) % 180f + 270f) % 180f - 90f);
            var gap = last is null ? 0f : MathF.Sqrt((p.X - last.X) * (p.X - last.X) + (p.Y - last.Y) * (p.Y - last.Y));
            if (last is null || turn > 4f || gap > pitch * 1.35f)
            {
                runs.Add(new List<WallPanel>());
            }

            runs[^1].Add(p);
            last = p;
        }

        var banner = 0;
        var rope = 0;
        foreach (var run in runs)
        {
            // The panel's local X runs along the wall; the banners face the fair.
            var h = run[0].Heading * Deg;
            var (ux, uy) = (MathF.Cos(h), -MathF.Sin(h));
            var (nx, ny) = (-uy, ux);
            if (((cx - run[0].X) * nx + (cy - run[0].Y) * ny < 0f) != outward)
            {
                (nx, ny) = (-nx, -ny);
            }

            var first = (run.Count - 1) % pal.Every / 2;
            var anchors = new List<(float X, float Y, float Top)>();
            for (var i = first; i < run.Count; i += pal.Every)
            {
                var p = run[i];
                var top = p.Z + wall.Height * p.Scale;
                var (ax, ay) = (p.X + nx * pal.Out, p.Y + ny * pal.Out);
                anchors.Add((ax, ay, top));
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(pal.Banners[banner % pal.Banners.Count])),
                    Scale = pal.BannerScale == 1f ? null : pal.BannerScale,
                    Placement = new Placement
                    {
                        Position = new P3Float(ax, ay, top - pal.BannerDrop),
                        Rotation = new P3Float(0f, 0f, MathF.Atan2(ny, -nx)),
                    },
                });
                banner++;
            }

            // Two mirrored halves of the festival line meet at the low middle, as the lane
            // crossings are hung (FairMarket): each half starts 53 from its origin, runs 682
            // along its local (+X, -Y) diagonal (heading 134.7) and rises 150, times its scale.
            for (var a = 0; a + 1 < anchors.Count && pal.Ropes.Count > 0; a++)
            {
                var ends = new[] { anchors[a], anchors[a + 1] };
                var (mx, my) = ((ends[0].X + ends[1].X) / 2f, (ends[0].Y + ends[1].Y) / 2f);
                var topZ = MathF.Min(ends[0].Top, ends[1].Top) - pal.RopeDrop;
                foreach (var e in ends)
                {
                    var (dx, dy) = (e.X - mx, e.Y - my);
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    var scale = d / 682f;
                    var (ex, ey) = (dx / d, dy / d);
                    var heading = MathF.Atan2(ex, ey) / Deg;
                    var oz = topZ - 150f * scale;
                    put(new PlacedObject(mod)
                    {
                        Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(pal.Ropes[rope % pal.Ropes.Count])),
                        Scale = scale,
                        Placement = new Placement
                        {
                            Position = new P3Float(mx - ex * 53f * scale, my - ey * 53f * scale, oz),
                            Rotation = new P3Float(0f, 0f, (heading - 134.7f) * Deg),
                        },
                    });
                    ropes++;

                    for (var along = pal.LanternSpacing * 0.5f; along < d - 30f && pal.Lanterns.Count > 0; along += pal.LanternSpacing)
                    {
                        var frac = along / d;
                        put(new PlacedObject(mod)
                        {
                            Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(pal.Lanterns[lanterns % pal.Lanterns.Count])),
                            Placement = new Placement
                            {
                                Position = new P3Float(mx + ex * along, my + ey * along, oz + 150f * scale * frac * frac - 4f),
                                Rotation = new P3Float(0f, 0f, heading * Deg),
                            },
                        });
                        lanterns++;
                    }
                }

                rope++;
            }
        }

        return (banner, ropes, lanterns);
    }
}

internal sealed class LifeResult
{
    public int Banners { get; set; }

    public int Ropes { get; set; }

    public int Lanterns { get; set; }

    public int Dressing { get; set; }

    public List<string> Refused { get; set; } = new();

    public int Plants { get; set; }

    public int Smokes { get; set; }

    public int Animals { get; set; }
}
