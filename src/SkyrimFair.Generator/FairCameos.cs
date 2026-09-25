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
        string npcKeyword, Action<PlacedNpc> putPersistent, Action<PlacedObject> put, Action<PlacedObject> putPersistentObject,
        IReadOnlyList<WallPanel> panels, PalisadeConfig wall, float[] gate, (float X, float Y) centre,
        IReadOnlyList<(float X, float Y)> banners, Func<FormKey, PlacedNpc?> findActor)
    {
        var sandbox = master.Packages.First(p => p.FormKey == FormKeyHelper.Parse(config.Package));
        var keyword = mod.Keywords.FirstOrDefault(k => k.EditorID == npcKeyword);
        var placed = new List<PlacedNpc>();
        var npcs = new List<Npc>();
        var idles = new List<FormKey>();
        var holds = new List<float>();
        var stops = new List<FormKey>();
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
            var place = new Placement { Position = new P3Float(at[0], at[1], at[2] + 2f), Rotation = new P3Float(0f, 0f, at[3] * MathF.PI / 180f) };
            if (c.Replaces.Length > 0 && findActor(FormKeyHelper.Parse(c.Replaces)) is { } visitor)
            {
                // He takes a visitor's place: the visitor is disabled (keeping its FormID), and the
                // culling leaves disabled actors alone.
                visitor.MajorRecordFlagsRaw |= 0x800;
                place = visitor.Placement!.DeepCopy();
            }

            var reference = new PlacedNpc(mod)
            {
                EditorID = $"{npc.EditorID}Ref",
                Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                Placement = place,
            };
            putPersistent(reference);
            placed.Add(reference);

            first.Add(idles.Count);
            count.Add(c.Idles.Count);
            foreach (var idle in c.Idles)
            {
                idles.Add(FormKeyHelper.Parse(idle.Idle));
                holds.Add(idle.Hold);
                stops.Add(idle.Stop.Length > 0 ? FormKeyHelper.Parse(idle.Stop) : FormKey.Null);
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
            horse.VirtualMachineAdapter.Scripts[0].Properties.Add(new ScriptFloatProperty { Name = "HomeX", Data = roof.At[0] });
            horse.VirtualMachineAdapter.Scripts[0].Properties.Add(new ScriptFloatProperty { Name = "HomeY", Data = roof.At[1] });
            horse.VirtualMachineAdapter.Scripts[0].Properties.Add(new ScriptFloatProperty { Name = "HomeZ", Data = roof.At[2] });
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
        var spoken = new List<(DialogResponses Info, float Seconds)>();
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
                // DNAM 01 (Allow Default Dialog), as every vanilla NPC voice type (MaleYoungEager
                // 013AD1): without it he couldn't be talked to (2026-09-25, in game).
                var voice = new VoiceType(mod) { EditorID = member.VoiceType, Flags = VoiceType.Flag.AllowDefaultDialog };
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
                    spoken.Add((info, line.TryGetProperty("seconds", out var len) ? len.GetSingle() : 6f));

                    var name = $"{config.QuestEditorId}__{info.FormKey.ID:x8}_1.fuz".ToLowerInvariant();
                    var dst = Path.Combine(voiceRoot, voice.EditorID!, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    File.Copy(Path.Combine(cues, line.GetProperty("fuz").GetString()!), dst, overwrite: true);
                }

                mod.DialogTopics.Add(topic);
            }
        }

        // ---- their rounds: a spot global each, and a small sandbox at each spot ----------------------
        // A single wide sandbox left them standing (2026-09-25). Now each has a few spots, a
        // sandbox package at each (a copy of the same vanilla sandbox, round an XMarker, radius
        // SpotRadius, energy 100), each on the condition that his spot global holds its number.
        // The stage script moves the global on every so often and re-evaluates his package, so
        // he walks across the fair to the next spot. The wide sandbox stays last, as a fallback.
        var spotGlobals = new List<FormKey>();
        var spotCounts = new List<int>();
        for (var i = 0; i < config.Members.Count; i++)
        {
            var c = config.Members[i];
            if (c.Spots.Count < 2)
            {
                if (c.Stand)
                {
                    npcs[i].Packages.Insert(0, new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.StandPackage)));
                }

                spotGlobals.Add(FormKey.Null);
                spotCounts.Add(0);
                continue;
            }

            var global = new GlobalShort(mod) { EditorID = $"{config.EditorIdPrefix}{c.Id}Spot", Data = 0 };
            mod.Globals.Add(global);
            var own = new List<Package>();
            for (var k = 0; k < c.Spots.Count; k++)
            {
                var at = c.Spots[k];
                var marker = new PlacedObject(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{c.Id}Spot{k + 1}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse("0000003B:Skyrim.esm")),
                    Placement = new Placement { Position = new P3Float(at[0], at[1], at.Length > 2 ? at[2] : 0f), Rotation = new P3Float(0f, 0f, 0f) },
                };
                putPersistentObject(marker);

                var package = sandbox.Duplicate(mod.GetNextFormKey());
                package.EditorID = $"{config.EditorIdPrefix}{c.Id}Spot{k + 1}Sandbox";
                if (package.Data.Values.OfType<PackageDataLocation>().FirstOrDefault() is { } where)
                {
                    where.Location = new LocationTargetRadius
                    {
                        Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(marker.FormKey) },
                        Radius = (uint)c.SpotRadius,
                    };
                }

                foreach (var energy in package.Data.Values.OfType<PackageDataFloat>())
                {
                    energy.Data = 100f;
                }

                var here = new GetGlobalValueConditionData();
                here.Global.Link.SetTo(global.FormKey);
                package.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = k, Data = here });
                mod.Packages.Add(package);
                own.Add(package);
            }

            if (c.Stand)
            {
                // He stands where he's put (his spot records are kept, so nothing renumbers).
                npcs[i].Packages.Insert(0, new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.StandPackage)));
                spotGlobals.Add(FormKey.Null);
                spotCounts.Add(0);
                continue;
            }

            // His spot packages first (the first whose condition holds runs), the wide sandbox after.
            for (var k = own.Count - 1; k >= 0; k--)
            {
                npcs[i].Packages.Insert(0, new FormLink<IPackageGetter>(own[k].FormKey));
            }

            spotGlobals.Add(global.FormKey);
            spotCounts.Add(c.Spots.Count);
        }

        // ---- the Fair Inspector's posters, on the palisade's inner face ------------------------------
        var posters = config.Posters;
        var postersHung = 0;
        if (posters.Enabled && posters.Designs.Count > 0 && panels.Count > 0)
        {
            var designs = posters.Designs.ToDictionary(d => d.Id, d =>
            {
                var s = FairWorld.AddStatic(mod, new ProjectStaticConfig
                {
                    EditorId = $"{config.EditorIdPrefix}Poster{d.Id}",
                    Model = d.Model,
                    Width = d.Width,
                    Depth = d.Depth,
                    Height = d.Height,
                    MinZ = -d.Height,
                });
                return s.FormKey;
            });

            // Panels clear of the gate and of the banners, their inward normal and a spot on them.
            var spots = new List<(int Index, float X, float Y, float Z, float Yaw)>();
            for (var i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                if (MathF.Sqrt((p.X - gate[0]) * (p.X - gate[0]) + (p.Y - gate[1]) * (p.Y - gate[1])) < posters.GateClear)
                {
                    continue;
                }

                if (banners.Any(b => MathF.Sqrt((b.X - p.X) * (b.X - p.X) + (b.Y - p.Y) * (b.Y - p.Y)) < posters.BannerClear))
                {
                    continue;
                }

                var h = p.Heading * MathF.PI / 180f;
                var (ux, uy) = (MathF.Cos(h), -MathF.Sin(h));
                var (nx, ny) = (-uy, ux);
                if ((centre.X - p.X) * nx + (centre.Y - p.Y) * ny < 0f)
                {
                    (nx, ny) = (-nx, -ny);
                }

                // The paper's front is its local -Y: turned so that faces the fair.
                spots.Add((i, p.X + nx * posters.Out, p.Y + ny * posters.Out, p.Z + posters.Height, MathF.Atan2(-nx, -ny)));
            }

            // The feature poster (Garrick's statement) once, on the free panel nearest its point.
            var used = new HashSet<int>();
            void Hang(string design, (int Index, float X, float Y, float Z, float Yaw) at, float scale, int n)
            {
                put(new PlacedObject(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}Poster{design}{n}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(designs[design]),
                    Scale = scale,
                    Placement = new Placement { Position = new P3Float(at.X, at.Y, at.Z), Rotation = new P3Float(0f, 0f, at.Yaw) },
                });
                used.Add(at.Index);
                postersHung++;
            }

            if (posters.Feature.Length > 0 && posters.FeatureNear.Length == 2 && spots.Count > 0)
            {
                var near = spots.OrderBy(s => (s.X - posters.FeatureNear[0]) * (s.X - posters.FeatureNear[0]) + (s.Y - posters.FeatureNear[1]) * (s.Y - posters.FeatureNear[1])).First();
                Hang(posters.Feature, near, posters.FeatureScale, 1);
            }

            // The rest, every Every-th free panel round the wall, the designs in turn.
            var cycle = posters.Designs.Select(d => d.Id).Where(id => id != posters.Feature).ToList();
            var k = 0;
            foreach (var at in spots.Where(s => !used.Contains(s.Index)).Where((_, j) => j % posters.Every == 0))
            {
                if (spots.Any(s => used.Contains(s.Index) && Math.Abs(s.Index - at.Index) <= 1))
                {
                    continue;  // not right beside the feature poster
                }

                var design = cycle[k % cycle.Count];
                Hang(design, at, posters.Scale, k / cycle.Count + 1);
                k++;
            }
        }

        // ---- the music ducked while a cameo speaks: a begin fragment on each line --------------
        // A greeting from an NPC with no topics never opens the dialogue menu, so the stage script
        // can't see it. Each line's fragment (SkyrimFairCameoLine.psc) sets DuckUntil to the real
        // time its line ends; the stage script keeps the music down until then. Vanilla's
        // fragment VMAD (INFO 0684FF): version 5, object format 2, the script Local, extra bind
        // data 2 (the fragment itself 1), OnBegin Fragment_0.
        var duck = new GlobalFloat(mod) { EditorID = $"{config.EditorIdPrefix}DuckUntil", Data = 0f };
        mod.Globals.Add(duck);
        foreach (var (info, seconds) in spoken)
        {
            var entry = new ScriptEntry { Name = config.LineScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(new ScriptObjectProperty { Name = "DuckUntil", Object = new FormLink<ISkyrimMajorRecordGetter>(duck.FormKey) });
            entry.Properties.Add(new ScriptFloatProperty { Name = "Seconds", Data = seconds + 0.5f });
            info.VirtualMachineAdapter = new DialogResponsesAdapter
            {
                Version = 5,
                ObjectFormat = 2,
                ScriptFragments = new ScriptFragments
                {
                    ExtraBindDataVersion = 2,
                    FileName = config.LineScript,
                    OnBegin = new ScriptFragment { ExtraBindDataVersion = 1, ScriptName = config.LineScript, FragmentName = "Fragment_0" },
                },
            };
            info.VirtualMachineAdapter.Scripts.Add(entry);
        }

        // ---- the stage script's schedule for their idles
        var script = mod.Quests.First(q => q.FormKey == stageQuest).VirtualMachineAdapter!.Scripts[0];
        ScriptObjectProperty Obj(FormKey key) => new() { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        script.Properties.Add(new ScriptObjectListProperty { Name = "Cameos", Objects = placed.Select(p => Obj(p.FormKey)).ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "CameoIdles2", Objects = idles.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoHolds2", Data = holds.ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "CameoStops2", Objects = stops.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "CameoFirstIdle2", Data = first.ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "CameoIdleCount2", Data = count.ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoEveryMin2", Data = config.Members.Select(m => m.Every[0]).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoEveryMax2", Data = config.Members.Select(m => m.Every[1]).ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "CameoSpot", Objects = spotGlobals.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "CameoSpotCount", Data = spotCounts.ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoMoveMin", Data = config.Members.Select(m => m.Move[0]).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "CameoMoveMax", Data = config.Members.Select(m => m.Move[1]).ToExtendedList() });
        script.Properties.Add(new ScriptObjectProperty { Name = "CameoDuckUntil", Object = new FormLink<ISkyrimMajorRecordGetter>(duck.FormKey) });
        Console.WriteLine($"  cameo rounds: {string.Join(", ", config.Members.Select((m, i) => $"{m.Id} {spotCounts[i]} spots"))}; posters: {postersHung} on the palisade");
        return placed.Count;
    }
}
