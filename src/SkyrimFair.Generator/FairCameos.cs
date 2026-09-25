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
    public static int Build(SkyrimMod mod, CameosConfig config, SingersConfig faces, ISkyrimModGetter master, FormKey stageQuest,
        string npcKeyword, Action<PlacedNpc> putPersistent, Action<PlacedObject> put)
    {
        var sandbox = master.Packages.First(p => p.FormKey == FormKeyHelper.Parse(config.Package));
        var keyword = mod.Keywords.FirstOrDefault(k => k.EditorID == npcKeyword);
        var placed = new List<PlacedNpc>();
        var npcs = new List<Npc>();
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

            // His face is the vanilla NPC's, copied field by field with its FaceGen head and tint
            // (as the singers'): a Traits template would show the template's name in game.
            var face = master.Npcs.First(n => n.FormKey == FormKeyHelper.Parse(c.Template));
            var npc = new Npc(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{c.Id}",
                Name = c.Name,
                ShortName = c.ShortName.Length > 0 ? c.ShortName : null,
                Race = new FormLink<IRaceGetter>(face.Race.FormKey),
                Voice = new FormLinkNullable<IVoiceTypeGetter>(face.Voice.FormKey),
                Class = new FormLink<IClassGetter>(face.Class.FormKey),
                DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(c.Outfit)),
                HeadTexture = new FormLinkNullable<ITextureSetGetter>(face.HeadTexture.FormKey),
                HairColor = new FormLinkNullable<IColorRecordGetter>(face.HairColor.FormKey),
                TextureLighting = face.TextureLighting,
                FaceMorph = face.FaceMorph?.DeepCopy(),
                FaceParts = face.FaceParts?.DeepCopy(),
                Configuration = new NpcConfiguration
                {
                    Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
                        | NpcConfiguration.Flag.Invulnerable | NpcConfiguration.Flag.Unique
                        | (face.Configuration.Flags & NpcConfiguration.Flag.Female),
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
                Height = face.Height,
                Weight = face.Weight,
            };
            foreach (var part in face.HeadParts)
            {
                npc.HeadParts.Add(new FormLink<IHeadPartGetter>(part.FormKey));
            }

            foreach (var tint in face.TintLayers)
            {
                npc.TintLayers.Add(tint.DeepCopy());
            }

            FairSingers.CopyFace(faces, master, face.FormKey, mod, npc.FormKey);
            npc.Packages.Add(new FormLink<IPackageGetter>(package.FormKey));
            if (keyword is not null)
            {
                npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            mod.Npcs.Add(npc);
            npcs.Add(npc);

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

        // ---- a horse on the stage roof, on a little plank platform across two rafters
        var roof = config.RoofHorse;
        if (roof.Enabled && roof.At.Length == 4)
        {
            var a = roof.At[3] * MathF.PI / 180f;
            var k = 0;
            foreach (var deck in roof.Deck)
            {
                put(new PlacedObject(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}RoofDeck{++k}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(roof.DeckPiece)),
                    Placement = new Placement
                    {
                        Position = new P3Float(deck[0], deck[1], deck[2]),
                        Rotation = new P3Float(0f, 0f, deck[3] * MathF.PI / 180f),
                    },
                });
            }

            var horseFrom = master.Npcs.First(n => n.FormKey == FormKeyHelper.Parse(roof.Horse));
            var horse = horseFrom.Duplicate(mod.GetNextFormKey());
            horse.EditorID = $"{config.EditorIdPrefix}RoofHorse";
            horse.Packages.Clear();
            horse.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(roof.Package)));
            horse.Configuration.Flags |= NpcConfiguration.Flag.Invulnerable | NpcConfiguration.Flag.Protected;
            horse.VirtualMachineAdapter = new VirtualMachineAdapter();
            horse.VirtualMachineAdapter.Scripts.Add(new ScriptEntry { Name = roof.Script });
            if (keyword is not null)
            {
                horse.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                horse.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            mod.Npcs.Add(horse);
            putPersistent(new PlacedNpc(mod)
            {
                EditorID = $"{horse.EditorID}Ref",
                Base = new FormLinkNullable<INpcGetter>(horse.FormKey),
                Placement = new Placement { Position = new P3Float(roof.At[0], roof.At[1], roof.At[2]), Rotation = new P3Float(0f, 0f, a) },
            });
        }

        // ---- their lines: a Hello (what an NPC says when you talk to him), one per recording ----
        // Each has his own voice type, so no vanilla line (they're filtered by voice type) is his,
        // and a Hello topic in the cameos' quest shaped as vanilla's DialogueGenericHello: Misc,
        // subtype 0x4F, SNAM HELO, priority 50, no branch; each INFO Random, for him only (GetIsID).
        // The voice files come from tools/cameos/build_voices.py (build/cameos/<id>/), copied to
        // Sound\Voice\<plugin>\<voice type>\<quest>__<INFO id>_1.fuz, lowercase (as the singers').
        var voiced = config.Members.Select((m, i) => (Member: m, Npc: npcs[i]))
            .Where(x => x.Member.VoiceLines.Length > 0 && x.Member.VoiceType.Length > 0)
            .ToList();
        if (voiced.Count > 0)
        {
            var quest = new Quest(mod)
            {
                EditorID = config.QuestEditorId,
                Name = "Fair cameos",
                Flags = Quest.Flag.StartGameEnabled,
                Priority = 0,
                // Mutagen leaves these out unless set; every vanilla quest has ANAM, every INFO CNAM.
                NextAliasID = 0,
            };
            mod.Quests.Add(quest);
            var voiceRoot = Path.Combine(faces.VoiceOut is { } vo && Path.IsPathRooted(vo) ? vo : Path.Combine(FairPaths.ConfigDirectory, faces.VoiceOut), mod.ModKey.FileName);
            foreach (var (member, npc) in voiced)
            {
                var voice = new VoiceType(mod) { EditorID = member.VoiceType };
                mod.VoiceTypes.Add(voice);
                npc.Voice = new FormLinkNullable<IVoiceTypeGetter>(voice.FormKey);

                var cues = Path.Combine(FairPaths.ConfigDirectory, config.VoiceBuildDir, member.Id);
                var cuesFile = Path.Combine(cues, "lines.json");
                if (!File.Exists(cuesFile))
                {
                    throw new InvalidOperationException($"cameos {member.Id}: no {cuesFile}; run tools/cameos/build_voices.py");
                }

                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(cuesFile));
                var topic = new DialogTopic(mod)
                {
                    Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
                    Category = DialogTopic.CategoryEnum.Misc,
                    Subtype = (DialogTopic.SubtypeEnum)0x4F,
                    SubtypeName = new RecordType("HELO"),
                    Priority = 50f,
                };
                var emotion = Enum.Parse<Emotion>(member.Emotion);
                foreach (var line in doc.RootElement.EnumerateArray())
                {
                    var info = new DialogResponses(mod)
                    {
                        Flags = new DialogResponseFlags { Flags = DialogResponses.Flag.Random },
                        FavorLevel = FavorLevel.None,
                    };
                    info.Responses.Add(new DialogResponse
                    {
                        Emotion = emotion,
                        EmotionValue = 50,
                        ResponseNumber = 1,
                        Flags = DialogResponse.Flag.UseEmotionAnimation,
                        Text = line.GetProperty("text").GetString()!,
                        ScriptNotes = string.Empty,
                        Edits = string.Empty,
                    });
                    var him = new GetIsIDConditionData { RunOnType = Condition.RunOnType.Subject };
                    him.Object.Link.SetTo(npc.FormKey);
                    info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = him });
                    topic.Responses.Add(info);

                    var name = $"{config.QuestEditorId}__{info.FormKey.ID:x8}_1.fuz".ToLowerInvariant();
                    var dst = Path.Combine(voiceRoot, voice.EditorID!, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    File.Copy(Path.Combine(cues, line.GetProperty("fuz").GetString()!), dst, overwrite: true);
                }

                mod.DialogTopics.Add(topic);
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
