using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Visitors described by <see cref="CrowdsConfig"/>: small groups gathered unevenly round
/// the fair's attractions (the sweetroll stall, the mead bar, the stage approach, the
/// archery line, the picnic tables), each facing what drew them there. They are the
/// stall-keepers' kind of NPC (vanilla faces from the fair's own face lists, town clothes,
/// the stay-at-location package) under a visitor's name, and stand where they are placed,
/// so no navmesh is needed; standing near each other they trade idle chatter. Nobody is
/// placed inside a stall, a dressing group or a pole.
/// </summary>
internal static class FairCrowds
{
    private const float Deg = MathF.PI / 180f;

    public static CrowdsResult Build(
        SkyrimMod mod, CrowdsConfig config, VendorsConfig looksFrom, MarketResult? market,
        Func<float, float, float> ground, Action<PlacedNpc> put, Func<string, FormKey> faceList)
    {
        var looks = new List<Npc>();
        foreach (var look in looksFrom.Looks)
        {
            var i = 0;
            foreach (var outfit in look.Outfits)
            {
                var npc = new Npc(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{look.Name}{++i:00}",
                    Name = config.Name,
                    Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(looksFrom.Race)),
                    Template = new FormLinkNullable<INpcSpawnGetter>(faceList(look.Template)),
                    Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(looksFrom.Class)),
                    DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(outfit)),
                    Configuration = new NpcConfiguration
                    {
                        Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
                            | (look.Female ? NpcConfiguration.Flag.Female : 0),
                        TemplateFlags = NpcConfiguration.TemplateFlag.Traits,
                        Level = new NpcLevel { Level = looksFrom.Level },
                        CalcMinLevel = looksFrom.Level,
                        CalcMaxLevel = looksFrom.Level,
                        SpeedMultiplier = 100,
                    },
                    AIData = new AIData
                    {
                        Aggression = Aggression.Unaggressive,
                        Confidence = Confidence.Cowardly,
                        Responsibility = Responsibility.NoCrime,
                        Assistance = Assistance.HelpsNobody,
                        Mood = Mood.Neutral,
                        EnergyLevel = 40,
                    },
                    ObjectBounds = new ObjectBounds { First = new P3Int16(-22, -14, 0), Second = new P3Int16(22, 14, 128) },
                    Height = 1f,
                    Weight = 50f,
                };
                npc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(looksFrom.Package)));
                mod.Npcs.Add(npc);
                looks.Add(npc);
            }
        }

        var blocked = market?.Footprints ?? Array.Empty<MarketFootprint>();
        var stood = new List<(float X, float Y)>();
        var groups = PlaceGroups(mod, config.Groups, 0, looks, blocked, market, stood, ground, put);

        // Animals stand where they are put, in their module's frame.
        var animals = 0;
        foreach (var animal in config.Animals)
        {
            var home = blocked.FirstOrDefault(b => b.Kind == animal.Near)
                ?? throw new InvalidOperationException($"fairWorld.crowds.animals {animal.Name}: no '{animal.Near}' was placed.");
            var (u, v) = (animal.At[0], animal.At[1]);
            var (c, s) = (MathF.Cos(home.Yaw * Deg), MathF.Sin(home.Yaw * Deg));
            var (x, y) = (home.X + u * c + v * s, home.Y - u * s + v * c);
            var placed = new PlacedNpc(mod)
            {
                Base = new FormLinkNullable<INpcGetter>(FormKeyHelper.Parse(animal.Base)),
                Placement = new Placement
                {
                    Position = new P3Float(x, y, ground(x, y) + 2f),
                    Rotation = new P3Float(0f, 0f, (home.Yaw + (animal.At.Length > 2 ? animal.At[2] : 0f)) * Deg),
                },
            };
            if (animal.Script.Length > 0)
            {
                placed.VirtualMachineAdapter = new VirtualMachineAdapter();
                placed.VirtualMachineAdapter.Scripts.Add(new ScriptEntry { Name = animal.Script });
            }

            put(placed);
            animals++;
        }

        return new CrowdsResult(looks.Count, stood, groups, animals, looks, blocked, market);
    }

    /// <summary>
    /// Places visitor groups round their focus (a stall theme's front, a dressing group, a
    /// point), clear of the market's footprints and of everyone already standing.
    /// <paramref name="salt"/> keeps a later set's hash streams apart from the first's.
    /// </summary>
    private static List<(string Name, int Placed)> PlaceGroups(
        SkyrimMod mod, IReadOnlyList<CrowdGroup> groupsConfig, int salt, IReadOnlyList<Npc> looks,
        IReadOnlyList<MarketFootprint> blocked, MarketResult? market, List<(float X, float Y)> stood,
        Func<float, float, float> ground, Action<PlacedNpc> put)
    {
        var groups = new List<(string Name, int Placed)>();
        for (var gi0 = 0; gi0 < groupsConfig.Count; gi0++)
        {
            var gi = gi0 + salt;
            var group = groupsConfig[gi0];
            var focus = new List<(float X, float Y, float Facing, float Inner)>();
            if (group.Theme.Length > 0)
            {
                // In front of every stall of the theme, facing its counter.
                foreach (var stall in market?.Stalls.Where(s => s.Theme == group.Theme) ?? Enumerable.Empty<MarketStall>())
                {
                    var (fx, fy) = (MathF.Sin(stall.Yaw * Deg), MathF.Cos(stall.Yaw * Deg));
                    var reach = stall.Depth / 2f + group.FrontOffset;
                    focus.Add((stall.X + fx * reach, stall.Y + fy * reach, stall.Yaw + 180f, 0f));
                }
            }
            else if (group.Near.Length > 0)
            {
                // Round each dressing group of the module, seen from all sides.
                // Measured from the group's edge, not its middle, so people stand round it.
                foreach (var f in blocked.Where(b => b.Kind == group.Near))
                {
                    focus.Add((f.X, f.Y, f.Yaw, MathF.Max(f.HalfW, f.HalfD)));
                }
            }
            else if (group.At.Length >= 2)
            {
                focus.Add((group.At[0], group.At[1], group.At.Length > 2 ? group.At[2] : 0f, 0f));
            }

            var placedHere = 0;
            var fi = 0;
            foreach (var (cx, cy, facing, inner) in focus)
            {
                fi++;
                if (FairHash.Hash3(gi * 31 + fi, 5, 41) >= group.Chance)
                {
                    continue;
                }

                // One more or one fewer here and there, so no two groups match.
                var count = Math.Max(1, group.Count + (int)MathF.Round(FairHash.Signed(gi * 31 + fi, 6, 41) * 0.7f));
                for (var k = 0; k < count; k++)
                {
                    // Spread over the arc at uneven radii, a few tries each to find clear ground.
                    for (var attempt = 0; attempt < 8; attempt++)
                    {
                        var seed = gi * 1000 + fi * 97 + k * 17 + attempt;
                        var t = count == 1 ? 0.5f : (k + 0.5f + FairHash.Signed(seed, 1, 40) * 0.35f) / count;
                        var angle = (facing + 180f + (t - 0.5f) * 2f * group.Arc) * Deg;
                        var r = inner + group.Radius * (0.45f + 0.55f * FairHash.Hash3(seed, 2, 40));
                        var (x, y) = (cx + MathF.Sin(angle) * r, cy + MathF.Cos(angle) * r);
                        if (blocked.Any(b => b.Contains(x, y, 25f)) || stood.Any(s => (s.X - x) * (s.X - x) + (s.Y - y) * (s.Y - y) < 55f * 55f))
                        {
                            continue;
                        }

                        // Face the focus, give or take, as people in a loose crowd do.
                        var yaw = MathF.Atan2(cx - x, cy - y) / Deg + FairHash.Signed(seed, 3, 40) * 25f;
                        var npc = looks[(int)(FairHash.Hash3(seed, 4, 40) * looks.Count) % looks.Count];
                        put(new PlacedNpc(mod)
                        {
                            Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                            Placement = new Placement
                            {
                                Position = new P3Float(x, y, ground(x, y) + 2f),
                                Rotation = new P3Float(0f, 0f, yaw * Deg),
                            },
                        });
                        stood.Add((x, y));
                        placedHere++;
                        break;
                    }
                }
            }

            groups.Add((group.Name, placedHere));
        }

        return groups;
    }

    /// <summary>
    /// Extra crowd tiers (<see cref="CrowdsConfig.Tiers"/>), built after every other record
    /// so no FormID before them moves. Each tier's visitors hang off one enable-parent marker
    /// (persistent), which the stage script enables up to <c>SkyrimFairCrowdTier</c>, so the
    /// crowd can be thinned or thickened live. A tier may give its own package (the
    /// wanderers' sandbox), in which case it gets its own visitor records.
    /// </summary>
    public static CrowdTiersResult BuildTiers(
        SkyrimMod mod, CrowdsConfig config, VendorsConfig looksFrom, CrowdsResult first,
        Func<float, float, float> ground, Action<PlacedObject> putMarker, Action<PlacedNpc> put, Func<string, FormKey> faceList,
        (float X, float Y, float Z) markerAt)
    {
        var markers = new List<FormKey>();
        var placed = new List<(string Tier, int Count)>();
        var stood = first.Positions.ToList();
        for (var ti = 0; ti < config.Tiers.Count; ti++)
        {
            var tier = config.Tiers[ti];
            var marker = new PlacedObject(mod)
            {
                EditorID = $"{config.EditorIdPrefix}Tier{ti + 1}Marker",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse("00003B:Skyrim.esm")),
                Placement = new Placement { Position = new P3Float(markerAt.X, markerAt.Y, markerAt.Z + 64f * ti), Rotation = new P3Float(0f, 0f, 0f) },
            };
            putMarker(marker);
            markers.Add(marker.FormKey);

            IReadOnlyList<Npc> looks = first.Looks;
            if (tier.Package.Length > 0)
            {
                // Their own records, the same looks with the tier's package.
                looks = first.Looks.Select((l, i) =>
                {
                    var copy = l.Duplicate(mod.GetNextFormKey());
                    copy.EditorID = $"{l.EditorID}{tier.Suffix}";
                    copy.Packages.Clear();
                    copy.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(tier.Package)));
                    mod.Npcs.Add(copy);
                    return copy;
                }).ToList();
            }

            var count = 0;
            void PutChild(PlacedNpc npc)
            {
                npc.EnableParent = new EnableParent { Reference = new FormLink<IPlacedGetter>(marker.FormKey) };
                put(npc);
                count++;
            }

            PlaceGroups(mod, tier.Groups, 5000 * (ti + 1), looks, first.Blocked, first.Market, stood, ground, PutChild);
            placed.Add((tier.Name, count));
        }

        return new CrowdTiersResult(markers, placed, stood.Skip(first.Positions.Count).ToList());
    }
}

internal sealed record CrowdTiersResult(IReadOnlyList<FormKey> Markers, IReadOnlyList<(string Tier, int Count)> Tiers, IReadOnlyList<(float X, float Y)> Positions);

internal sealed record CrowdsResult(int Records, IReadOnlyList<(float X, float Y)> Positions, IReadOnlyList<(string Name, int Placed)> Groups, int Animals,
    IReadOnlyList<Npc> Looks, IReadOnlyList<MarketFootprint> Blocked, MarketResult? Market);
