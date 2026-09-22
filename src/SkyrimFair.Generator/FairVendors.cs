using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Stall-keepers described by <see cref="VendorsConfig"/>: one NPC standing behind each
/// stall's counter, facing the street. For picturing the market only: they sell nothing,
/// say nothing of their own, and belong to no faction or quest.
///
/// Faces come from vanilla: each vendor record takes only its Traits (race, sex, face,
/// voice, height) from a vanilla leveled list, so every reference is a different
/// vanilla face with its own FaceGen, and no generated face can come out dark. Clothes,
/// class and AI are the vendor's own: vanilla town clothes, the Citizen class, and a
/// hold-position package so they stay put without a navmesh.
/// </summary>
internal static class FairVendors
{
    private const float Deg = MathF.PI / 180f;

    public static VendorsResult Build(
        SkyrimMod mod, VendorsConfig config, IReadOnlyList<MarketStall> stalls,
        Func<float, float, float> ground, Action<PlacedNpc> put)
    {
        // One vendor record per look (template x outfit), shared by many references.
        var looks = new List<Npc>();
        foreach (var look in config.Looks)
        {
            foreach (var outfit in look.Outfits)
            {
                var npc = new Npc(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{look.Name}{looks.Count(n => n.EditorID!.StartsWith($"{config.EditorIdPrefix}{look.Name}")) + 1:00}",
                    Name = config.Name,
                    Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(config.Race)),
                    Template = new FormLinkNullable<INpcSpawnGetter>(FormKeyHelper.Parse(look.Template)),
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
                looks.Add(npc);
            }
        }

        var placed = 0;
        for (var i = 0; i < stalls.Count; i++)
        {
            var stall = stalls[i];
            if (stall.VendorX is not { } x || stall.VendorY is not { } y)
            {
                continue;
            }

            var npc = looks[(int)(FairHash.Hash3(i, 9, 81) * looks.Count) % looks.Count];
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

        return new VendorsResult(looks.Count, placed);
    }
}

internal sealed record VendorsResult(int Records, int Placed);
