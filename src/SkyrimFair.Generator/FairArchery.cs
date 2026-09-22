using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The archery range described by <see cref="ArcheryConfig"/>, set up as Solitude's
/// Castle Dour practice yard is: each archer stands on the firing line and is linked to
/// one <c>ArcheryTarget</c> by a linked reference with the <c>TrainingTarget</c> keyword,
/// and runs vanilla <c>GuardSolitudeRangedTrainingPackage</c>, which has no conditions and
/// shoots at that linked target all day. The archers are ordinary townsfolk rather than
/// soldiers: their looks come, as the stall-keepers' do, from a Traits template on
/// vanilla commoner leveled lists, with hunter clothes, a hunting bow and arrows.
/// </summary>
internal static class FairArchery
{
    private const float Deg = MathF.PI / 180f;

    public static ArcheryResult Build(
        SkyrimMod mod, ArcheryConfig config, Func<float, float, float> ground,
        Action<PlacedObject> putObject, Action<PlacedNpc> putNpc, Func<string, FormKey> faceList)
    {
        var archers = new List<Npc>();
        foreach (var look in config.Looks)
        {
            var npc = new Npc(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{look.Name}",
                Name = config.Name,
                Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(config.Race)),
                Template = new FormLinkNullable<INpcSpawnGetter>(faceList(look.Template)),
                Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(config.Class)),
                DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(config.Outfit)),
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
                    Confidence = Confidence.Average,
                    Responsibility = Responsibility.NoCrime,
                    Assistance = Assistance.HelpsNobody,
                    Mood = Mood.Neutral,
                    EnergyLevel = 50,
                },
                ObjectBounds = new ObjectBounds
                {
                    First = new P3Int16(-22, -14, 0),
                    Second = new P3Int16(22, 14, 128),
                },
                Height = 1f,
                Weight = 50f,
                Items = new ExtendedList<ContainerEntry>
                {
                    new() { Item = new ContainerItem { Item = new FormLink<IItemGetter>(FormKeyHelper.Parse(config.Bow)), Count = 1 } },
                    new() { Item = new ContainerItem { Item = new FormLink<IItemGetter>(FormKeyHelper.Parse(config.Arrows)), Count = config.ArrowCount } },
                },
            };
            npc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.Package)));
            mod.Npcs.Add(npc);
            archers.Add(npc);
        }

        var targetBase = FormKeyHelper.Parse(config.Target);
        var backstopBase = FormKeyHelper.Parse(config.Backstop);
        var keyword = FormKeyHelper.Parse(config.TargetKeyword);
        var lanes = 0;
        for (var i = 0; i < config.Lanes.Count; i++)
        {
            var lane = config.Lanes[i];
            var (ax, ay, tx, ty) = (lane.Archer[0], lane.Archer[1], lane.Target[0], lane.Target[1]);
            var shotHeading = MathF.Atan2(tx - ax, ty - ay) / Deg;
            var (dx, dy) = ((tx - ax) / Distance(ax, ay, tx, ty), (ty - ay) / Distance(ax, ay, tx, ty));

            // The target's face is its local -X. Turn it so -X points back at the archer.
            var awayX = -dx;
            var awayY = -dy;
            var targetYaw = MathF.Atan2(awayY, -awayX) / Deg;  // -X = (-cos t, sin t)
            var target = new PlacedObject(mod)
            {
                EditorID = $"{config.EditorIdPrefix}Target{i + 1:00}",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(targetBase),
                Placement = new Placement
                {
                    Position = new P3Float(tx, ty, ground(tx, ty)),
                    Rotation = new P3Float(0f, 0f, targetYaw * Deg),
                },
            };
            putObject(target);

            // A hay bale behind the target stops the stray arrows.
            var (bx, by) = (tx + dx * config.BackstopDistance, ty + dy * config.BackstopDistance);
            putObject(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(backstopBase),
                Placement = new Placement
                {
                    Position = new P3Float(bx, by, ground(bx, by) + config.BackstopZ),
                    Rotation = new P3Float(0f, 0f, (shotHeading + 90f) * Deg),
                },
            });

            var archer = new PlacedNpc(mod)
            {
                Base = new FormLinkNullable<INpcGetter>(archers[i % archers.Count].FormKey),
                Placement = new Placement
                {
                    Position = new P3Float(ax, ay, ground(ax, ay) + 2f),
                    Rotation = new P3Float(0f, 0f, shotHeading * Deg),
                },
            };
            archer.LinkedReferences.Add(new LinkedReferences
            {
                KeywordOrReference = new FormLink<IKeywordLinkedReferenceGetter>(keyword),
                Reference = new FormLink<IPlacedGetter>(target.FormKey),
            });
            putNpc(archer);
            lanes++;
        }

        return new ArcheryResult(archers.Count, lanes);
    }

    private static float Distance(float ax, float ay, float bx, float by)
        => MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
}

internal sealed record ArcheryResult(int Records, int Lanes);
