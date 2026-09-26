using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Stall-keepers described by <see cref="VendorsConfig"/>: one NPC standing behind every
/// counter (a merged pair of stalls has two), facing the street. For picturing the market only: they sell nothing,
/// say nothing of their own, and belong to no faction or quest.
///
/// Each keeper is its own record, named for its stall's trade (VendorsConfig.ThemeNames).
/// Faces come from vanilla: each vendor record takes only its Traits (race, sex, face,
/// voice, height) from a vanilla leveled list, so every reference is a different
/// vanilla face with its own FaceGen, and no generated face can come out dark. Clothes,
/// class and AI are the vendor's own: vanilla town clothes, the Citizen class, and a
/// stay-at-location package with no weapons out, so they stay put without a navmesh.
/// </summary>
internal static class FairVendors
{
    private const float Deg = MathF.PI / 180f;

    public static VendorsResult Build(
        SkyrimMod mod, VendorsConfig config, IReadOnlyList<MarketStall> stalls,
        Func<float, float, float> ground, Action<PlacedNpc> put, Func<string, FormKey> faceList)
    {
        // The looks (template x outfit) keepers are drawn from.
        var looks = config.Looks.SelectMany(look => look.Outfits.Select(outfit => (Look: look, Outfit: outfit))).ToList();

        // One record per keeper, named for the stall's trade, so looking at a keeper says
        // what the stall sells ("Cheese Seller") and each can be found by EditorID.
        var placed = 0;
        var records = 0;
        var perTheme = new Dictionary<string, int>();
        for (var i = 0; i < stalls.Count; i++)
        {
            var stall = stalls[i];
            var name = config.ThemeNames.TryGetValue(stall.Theme, out var named) ? named : config.Name;
            for (var k = 0; k < stall.Vendors.Count; k++)
            {
                var (x, y) = stall.Vendors[k];
                var (look, outfit) = looks[(int)(FairHash.Hash3(i, 9 + k, 81) * looks.Count) % looks.Count];
                var number = perTheme[stall.Theme] = perTheme.GetValueOrDefault(stall.Theme) + 1;
                var theme = stall.Theme.Length == 0 ? "Stall" : char.ToUpperInvariant(stall.Theme[0]) + stall.Theme[1..];
                var npc = new Npc(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{theme}{number:00}",
                    Name = name,
                    Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(config.Race)),
                    Template = new FormLinkNullable<INpcSpawnGetter>(faceList(look.Template)),
                    Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(config.Class)),
                    DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(outfit)),
                    Configuration = new NpcConfiguration
                    {
                        Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
                            | (look.Female ? NpcConfiguration.Flag.Female : 0),
                        TemplateFlags = NpcConfiguration.TemplateFlag.Traits,
                        Level = new NpcLevel { Level = config.Level },
                        CalcMinLevel = config.Level,
                        CalcMaxLevel = config.Level,
                        SpeedMultiplier = 100,
                    },
                    AIData = new AIData
                    {
                        Aggression = Aggression.Unaggressive,
                        Confidence = Confidence.Cowardly,
                        Responsibility = Responsibility.NoCrime,
                        Assistance = Assistance.HelpsNobody,
                        Mood = Mood.Neutral,
                        EnergyLevel = 20,
                    },
                    ObjectBounds = new ObjectBounds
                    {
                        First = new P3Int16(-22, -14, 0),
                        Second = new P3Int16(22, 14, 128),
                    },
                    Height = 1f,
                    Weight = 50f,
                };
                npc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.Package)));
                mod.Npcs.Add(npc);
                records++;
                put(new PlacedNpc(mod)
                {
                    Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                    Placement = new Placement
                    {
                        Position = new P3Float(x, y, ground(x, y) + 2f),
                        Rotation = new P3Float(0f, 0f, stall.Yaw * Deg),
                    },
                });
                placed++;
            }
        }

        return new VendorsResult(records, placed);
    }
}

internal sealed record VendorsResult(int Records, int Placed);
