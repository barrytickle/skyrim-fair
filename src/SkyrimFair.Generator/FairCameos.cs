using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Named ambient characters, the creators' cameos (<see cref="CameosConfig"/>): Garrick Sol V,
/// a Bosmer bard who wanders the fair and now and then strikes up his lute, and Claudius Vale,
/// a Breton inspector who wanders the market, reading his notes and examining things. No
/// quest or dialogue.
///
/// Each takes a vanilla NPC's face, race and voice through a Traits template (so its FaceGen
/// is vanilla's), and keeps his own name, clothes and AI: a copy of vanilla's no-conversation
/// sandbox with a wider radius. Their idles are the stage script's (<c>Cameos</c> and the
/// properties after it): every so often, when a cameo is free, it plays one of his idles and
/// stops it after its hold.
///
/// Built in its own FormID range (<see cref="CameosConfig.FormIdBase"/>), after the life pass
/// and before the navmesh and the culling, so nothing renumbers.
/// </summary>
internal static class FairCameos
{
    public static int Build(SkyrimMod mod, CameosConfig config, ISkyrimModGetter master, FormKey stageQuest,
        string npcKeyword, Action<PlacedNpc> putPersistent)
    {
        var sandbox = master.Packages.First(p => p.FormKey == FormKeyHelper.Parse(config.Package));
        var keyword = mod.Keywords.FirstOrDefault(k => k.EditorID == npcKeyword);
        var placed = new List<PlacedNpc>();
        var idles = new List<FormKey>();
        var holds = new List<float>();
        var first = new List<int>();
        var count = new List<int>();
        foreach (var c in config.Members)
        {
            // A wider copy of the sandbox: its location (data input 0) is round the editor location.
            var package = sandbox.Duplicate(mod.GetNextFormKey());
            package.EditorID = $"{config.EditorIdPrefix}{c.Id}Sandbox";
            if (package.Data.Values.OfType<PackageDataLocation>().FirstOrDefault() is { } where)
            {
                where.Location.Radius = (uint)c.Radius;
            }

            mod.Packages.Add(package);

            var template = master.Npcs.First(n => n.FormKey == FormKeyHelper.Parse(c.Template));
            var npc = new Npc(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{c.Id}",
                Name = c.Name,
                ShortName = c.ShortName.Length > 0 ? c.ShortName : null,
                Race = new FormLink<IRaceGetter>(template.Race.FormKey),
                Template = new FormLinkNullable<INpcSpawnGetter>(template.FormKey),
                Class = new FormLink<IClassGetter>(template.Class.FormKey),
                DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(c.Outfit)),
                Configuration = new NpcConfiguration
                {
                    Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
                        | NpcConfiguration.Flag.Invulnerable | NpcConfiguration.Flag.Unique,
                    TemplateFlags = NpcConfiguration.TemplateFlag.Traits,
                    Level = new NpcLevel { Level = 5 },
                    CalcMinLevel = 5,
                    CalcMaxLevel = 5,
                    SpeedMultiplier = 100,
                },
                AIData = new AIData
                {
                    Aggression = Aggression.Unaggressive,
                    Confidence = Confidence.Cowardly,
                    Responsibility = Responsibility.NoCrime,
                    Assistance = Assistance.HelpsNobody,
                    Mood = Mood.Happy,
                    EnergyLevel = (byte)c.Energy,
                },
                ObjectBounds = new ObjectBounds { First = new P3Int16(-22, -14, 0), Second = new P3Int16(22, 14, 128) },
                // DNAM: every vanilla NPC has it (Mutagen leaves it out unless set).
                PlayerSkills = new PlayerSkills(),
                Height = 1f,
                Weight = 50f,
            };
            npc.Packages.Add(new FormLink<IPackageGetter>(package.FormKey));
            if (keyword is not null)
            {
                npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            mod.Npcs.Add(npc);

            var at = c.At;
            var reference = new PlacedNpc(mod)
            {
                EditorID = $"{npc.EditorID}Ref",
                Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                Placement = new Placement { Position = new P3Float(at[0], at[1], at[2] + 2f), Rotation = new P3Float(0f, 0f, at[3] * MathF.PI / 180f) },
            };
            putPersistent(reference);
            placed.Add(reference);

            first.Add(idles.Count);
            count.Add(c.Idles.Count);
            foreach (var idle in c.Idles)
            {
                idles.Add(FormKeyHelper.Parse(idle.Idle));
                holds.Add(idle.Hold);
            }
        }

        // ---- the stage script's schedule for their idles
        var script = mod.Quests.First(q => q.FormKey == stageQuest).VirtualMachineAdapter!.Scripts[0];
        ScriptObjectProperty Obj(FormKey key) => new() { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        script.Properties.Add(new ScriptObjectListProperty { Name = "Cameos", Objects = placed.Select(p => Obj(p.FormKey)).ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "CameoIdles", Objects = idles.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoHolds", Data = holds.ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "CameoFirstIdle", Data = first.ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "CameoIdleCount", Data = count.ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoEveryMin", Data = config.Members.Select(m => m.Every[0]).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoEveryMax", Data = config.Members.Select(m => m.Every[1]).ToExtendedList() });
        return placed.Count;
    }
}
