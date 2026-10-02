using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The voiced fairgoers (Barry, 2026-09-26 and 2026-10-02): the visitors become ten characters,
/// each a voice type of its own with one line (Barry's recordings), said only when talked to.
/// <list type="bullet">
/// <item>A character's faces are vanilla NPCs' (Barry's picks), each copied onto a fair NPC with
/// the character's voice type (its "face carrier", FaceGen and all); one face list a character.
/// The visitors' bases take Traits from it, so each copy of a character rolls one of its faces.
/// A Traits template carries the voice too, which is why the faces need copying at all.</item>
/// <item>A base a character and a package (standing, seated, wandering): copies of the visitors'
/// packages without "Hellos to player", so they don't greet the player walking past.</item>
/// <item>Every visitor ref keeps its FormID and place; only its base changes. A few are given a
/// character on purpose (the nearest to a point), the rest round the characters in turn.</item>
/// <item>Their lines: a Hello topic in a quest of their own, an INFO a line for its voice type
/// (GetIsVoiceType), with the cameos' fragment, so the music ducks for the whole line.</item>
/// </list>
/// Built last of all, in its own FormID range (<see cref="FairgoersConfig.FormIdBase"/>).
/// </summary>
internal static class FairFairgoers
{
    private const int InitiallyDisabledFlag = 0x800;

    public static string Build(SkyrimMod mod, FairWorldConfig fw, ISkyrimModGetter master)
    {
        // Refs whose crowd layer is off by default (beyond crowds.tierDefault) aren't placed on purpose.
        var layersOn = fw.Crowds.TierDefault > 0 ? fw.Crowds.TierDefault : fw.Crowds.Tiers.Count;
        var offMarkers = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedObjectGetter>())
            .Where(o => o.EditorID is { } e && Enumerable.Range(layersOn + 1, Math.Max(0, fw.Crowds.Tiers.Count - layersOn)).Any(n => e == $"{fw.Crowds.EditorIdPrefix}Tier{n}Marker"))
            .Select(o => o.FormKey).ToHashSet();
        var config = fw.Fairgoers;
        var faces = fw.Singers;
        var keyword = fw.NpcKeyword.Length > 0 ? mod.Keywords.FirstOrDefault(k => k.EditorID == fw.NpcKeyword) : null;
        var stage = mod.Quests.First(q => q.VirtualMachineAdapter?.Scripts.Any(s => s.Name == "SkyrimFairAudioScript") == true);
        var stageScript = stage.VirtualMachineAdapter!.Scripts.First(s => s.Name == "SkyrimFairAudioScript");
        var duck = stageScript.Properties.OfType<ScriptObjectProperty>().First(p => p.Name == "CameoDuckUntil").Object.FormKey;

        // The visitors' bases, and the refs on them.
        var visitorBases = mod.Npcs
            .Where(n => n.EditorID is { } e && e.StartsWith(config.VisitorPrefix, StringComparison.Ordinal)
                && !e.StartsWith(config.ChildPrefix, StringComparison.Ordinal))
            .ToDictionary(n => n.FormKey);
        var refs = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedNpc>())
            .Where(r => visitorBases.ContainsKey(r.Base.FormKey))
            .OrderBy(r => r.FormKey.ID)
            .ToList();

        // The lines, a list a character, in the lines file's order.
        var lines = new Dictionary<string, List<System.Text.Json.JsonElement>>();
        foreach (var c in config.Characters)
        {
            var cues = Path.Combine(FairPaths.ConfigDirectory, config.VoiceBuildDir, c.Voice, "lines.json");
            if (!File.Exists(cues))
            {
                throw new InvalidOperationException($"fairgoers {c.Voice}: no {cues}; run tools/cameos/build_voices.py");
            }

            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(cues));
            lines[c.Voice] = doc.RootElement.EnumerateArray().Select(l => l.Clone()).ToList();
        }

        // ---- the voice types, face carriers and face lists ---------------------------------------
        var voices = new Dictionary<string, VoiceType>();
        var faceLists = new Dictionary<string, LeveledNpc>();
        var carriers = 0;
        foreach (var c in config.Characters)
        {
            // DNAM 01 (Allow Default Dialog), as the cameos' and every vanilla NPC voice type.
            var voice = new VoiceType(mod) { EditorID = $"{config.EditorIdPrefix}{c.Id}Voice", Flags = VoiceType.Flag.AllowDefaultDialog };
            mod.VoiceTypes.Add(voice);
            voices[c.Voice] = voice;
            var list = new LeveledNpc(mod) { EditorID = $"{config.EditorIdPrefix}{c.Id}Faces", Flags = LeveledNpc.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer | LeveledNpc.Flag.CalculateForEachItemInCount };
            for (var i = 0; i < c.Faces.Count; i++)
            {
                var donor = master.Npcs.First(n => n.FormKey == FormKeyHelper.Parse(c.Faces[i]));
                var carrier = Carrier(mod, donor, voice, $"{config.EditorIdPrefix}{c.Id}Face{i + 1:00}", fw.Crowds.Name);
                FairSingers.CopyFace(faces, master, donor.FormKey, mod, carrier.FormKey);
                if (keyword is not null)
                {
                    carrier.Keywords = new ExtendedList<IFormLinkGetter<IKeywordGetter>> { new FormLink<IKeywordGetter>(keyword.FormKey) };
                }

                mod.Npcs.Add(carrier);
                list.Entries ??= new ExtendedList<LeveledNpcEntry>();
                list.Entries.Add(new LeveledNpcEntry { Data = new LeveledNpcEntryData { Level = 1, Count = 1, Reference = new FormLink<INpcSpawnGetter>(carrier.FormKey) } });
                carriers++;
            }

            mod.LeveledNpcs.Add(list);
            faceLists[c.Voice] = list;
        }

        // ---- packages without "Hellos to player", one a visitor package ----------------------------
        var quiet = new Dictionary<FormKey, FormKey>();
        foreach (var key in visitorBases.Values.Select(PackageOf).Distinct().OrderBy(k => k.ID))
        {
            var source = (IPackageGetter?)mod.Packages.FirstOrDefault(p => p.FormKey == key) ?? master.Packages.First(p => p.FormKey == key);
            var copy = (Package)source.Duplicate(mod.GetNextFormKey());
            copy.EditorID = $"{config.EditorIdPrefix}{source.EditorID!.Replace("SkyrimFair", string.Empty, StringComparison.Ordinal)}";
            copy.InterruptFlags &= ~Package.InterruptFlag.HellosToPlayer;
            mod.Packages.Add(copy);
            quiet[key] = copy.FormKey;
        }

        // ---- a base a character and package ----------------------------------------------------------
        var template = visitorBases.Values.OrderBy(n => n.FormKey.ID).First();
        var bases = new Dictionary<(string Voice, FormKey Package), Npc>();
        Npc BaseFor(FairgoerCharacter c, FormKey package)
        {
            if (bases.TryGetValue((c.Voice, package), out var made))
            {
                return made;
            }

            var npc = template.Duplicate(mod.GetNextFormKey());
            var kind = visitorBases.Values.First(v => PackageOf(v) == package).EditorID!;
            var suffix = kind.EndsWith("Sitter", StringComparison.Ordinal) ? "Sitter" : kind.EndsWith("Wanderer", StringComparison.Ordinal) ? "Wanderer" : string.Empty;
            npc.EditorID = $"{config.EditorIdPrefix}{c.Id}{suffix}";
            npc.Template = new FormLinkNullable<INpcSpawnGetter>(faceLists[c.Voice].FormKey);
            npc.DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(c.Outfit));
            npc.Voice = new FormLinkNullable<IVoiceTypeGetter>(voices[c.Voice].FormKey);
            // The sex its faces have (the Traits template's), so nothing reads the base's as other.
            var female = master.Npcs.First(n => n.FormKey == FormKeyHelper.Parse(c.Faces[0])).Configuration.Flags & NpcConfiguration.Flag.Female;
            npc.Configuration.Flags = (npc.Configuration.Flags & ~NpcConfiguration.Flag.Female) | female;
            npc.Packages.Clear();
            npc.Packages.Add(new FormLink<IPackageGetter>(quiet[package]));
            if (keyword is not null && !(npc.Keywords?.Any(k => k.FormKey == keyword.FormKey) ?? false))
            {
                npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            mod.Npcs.Add(npc);
            return bases[(c.Voice, package)] = npc;
        }

        // ---- who plays whom: a few on purpose, the rest in turn -------------------------------------
        var byVoice = config.Characters.ToDictionary(c => c.Voice);
        var cast = new Dictionary<FormKey, FairgoerCharacter>();
        bool On(IPlacedNpc r) => (r.MajorRecordFlagsRaw & InitiallyDisabledFlag) == 0
            && !(r.EnableParent is { } parent && offMarkers.Contains(parent.Reference.FormKey));
        var woken = 0;
        foreach (var p in config.Placed)
        {
            // Wake: a retired visitor (placed but initially disabled) may be switched back on for
            // it, where no visitor stands any more (the Mead & Ale stall's group is retired).
            var nearest = refs.Where(r => (On(r) || (p.Wake && (r.MajorRecordFlagsRaw & InitiallyDisabledFlag) != 0 && r.EnableParent is null)) && !cast.ContainsKey(r.FormKey))
                .OrderBy(r => Dist(r, p.At)).ThenBy(r => r.FormKey.ID).First();
            if ((nearest.MajorRecordFlagsRaw & InitiallyDisabledFlag) != 0)
            {
                nearest.MajorRecordFlagsRaw &= ~InitiallyDisabledFlag;
                woken++;
            }

            cast[nearest.FormKey] = byVoice[p.Voice];
        }

        var counts = config.Characters.ToDictionary(c => c.Voice, c => cast.Values.Count(v => v.Voice == c.Voice));
        foreach (var r in refs.Where(r => !cast.ContainsKey(r.FormKey)))
        {
            var c = config.Characters.OrderBy(x => counts[x.Voice]).First();
            cast[r.FormKey] = c;
            counts[c.Voice]++;
        }

        foreach (var r in refs)
        {
            r.Base = new FormLinkNullable<INpcGetter>(BaseFor(cast[r.FormKey], PackageOf(visitorBases[r.Base.FormKey])).FormKey);
        }

        // ---- their lines: a Hello a voice type --------------------------------------------------------
        var quest = new Quest(mod)
        {
            EditorID = config.EditorIdPrefix + "s",
            Name = "Fairgoers",
            Flags = Quest.Flag.StartGameEnabled,
            Priority = 0,
            NextAliasID = 0,
        };
        mod.Quests.Add(quest);
        var topic = new DialogTopic(mod)
        {
            Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = (DialogTopic.SubtypeEnum)0x4F,
            SubtypeName = new RecordType("HELO"),
            Priority = 50f,
        };
        var voiceRoot = Path.Combine(Path.IsPathRooted(faces.VoiceOut) ? faces.VoiceOut : Path.Combine(FairPaths.ConfigDirectory, faces.VoiceOut), mod.ModKey.FileName);
        var spoken = 0;
        foreach (var c in config.Characters)
        {
            var cues = Path.Combine(FairPaths.ConfigDirectory, config.VoiceBuildDir, c.Voice);
            foreach (var line in lines[c.Voice])
            {
                var info = new DialogResponses(mod)
                {
                    Flags = new DialogResponseFlags { Flags = DialogResponses.Flag.Random },
                    FavorLevel = FavorLevel.None,
                };
                info.Responses.Add(new DialogResponse
                {
                    Emotion = Emotion.Happy,
                    EmotionValue = 50,
                    ResponseNumber = 1,
                    Flags = DialogResponse.Flag.UseEmotionAnimation,
                    Text = line.GetProperty("text").GetString()!,
                    ScriptNotes = string.Empty,
                    Edits = string.Empty,
                });
                var theirs = new GetIsVoiceTypeConditionData { RunOnType = Condition.RunOnType.Subject };
                theirs.VoiceTypeOrList.Link.SetTo(voices[c.Voice].FormKey);
                info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = theirs });
                var seconds = line.TryGetProperty("seconds", out var len) ? len.GetSingle() : 6f;
                var entry = new ScriptEntry { Name = fw.Cameos.LineScript, Flags = ScriptEntry.Flag.Local };
                entry.Properties.Add(new ScriptObjectProperty { Name = "DuckUntil", Object = new FormLink<ISkyrimMajorRecordGetter>(duck) });
                entry.Properties.Add(new ScriptFloatProperty { Name = "Seconds", Data = seconds + 0.5f });
                info.VirtualMachineAdapter = new DialogResponsesAdapter
                {
                    Version = 5,
                    ObjectFormat = 2,
                    ScriptFragments = new ScriptFragments
                    {
                        ExtraBindDataVersion = 2,
                        FileName = fw.Cameos.LineScript,
                        OnBegin = new ScriptFragment { ExtraBindDataVersion = 1, ScriptName = fw.Cameos.LineScript, FragmentName = "Fragment_0" },
                    },
                };
                info.VirtualMachineAdapter.Scripts.Add(entry);
                topic.Responses.Add(info);

                // Sound\Voice\<plugin>\<voice type>\<quest>__<INFO id>_1, lowercase; the .fuz
                // unpacked into .lip and .xwm, as the cameos' (plain Skyrim reads that layout).
                var dst = Path.Combine(voiceRoot, voices[c.Voice].EditorID!, $"{quest.EditorID}__{info.FormKey.ID:x8}_1.fuz".ToLowerInvariant());
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                var fuz = File.ReadAllBytes(Path.Combine(cues, line.GetProperty("fuz").GetString()!));
                if (fw.Cameos.LooseLip)
                {
                    var lipSize = BitConverter.ToInt32(fuz, 8);
                    File.WriteAllBytes(Path.ChangeExtension(dst, ".lip"), fuz[12..(12 + lipSize)]);
                    File.WriteAllBytes(Path.ChangeExtension(dst, ".xwm"), fuz[(12 + lipSize)..]);
                }
                else
                {
                    File.WriteAllBytes(dst, fuz);
                }

                spoken++;
            }
        }

        mod.DialogTopics.Add(topic);
        var perCharacter = string.Join(", ", config.Characters.Select(c => $"{c.Id} {counts[c.Voice]}"));
        return $"{refs.Count} visitors as {config.Characters.Count} characters ({perCharacter}); "
            + $"{carriers} faces, {bases.Count} bases, {quiet.Count} packages, {spoken} lines, {woken} retired visitors woken";
    }

    private static FormKey PackageOf(INpcGetter npc) => npc.Packages.Count > 0 ? npc.Packages[0].FormKey : FormKey.Null;

    private static float Dist(IPlacedNpc r, float[] at)
    {
        var p = r.Placement!.Position;
        return MathF.Sqrt((p.X - at[0]) * (p.X - at[0]) + (p.Y - at[1]) * (p.Y - at[1]));
    }

    /// <summary>A vanilla NPC's face, race, sex and build on a fair NPC with the character's voice.</summary>
    private static Npc Carrier(SkyrimMod mod, INpcGetter face, VoiceType voice, string editorId, string name) => new Npc(mod)
    {
        EditorID = editorId,
        Name = name,
        Race = new FormLink<IRaceGetter>(face.Race.FormKey),
        Voice = new FormLinkNullable<IVoiceTypeGetter>(voice.FormKey),
        Class = new FormLink<IClassGetter>(face.Class.FormKey),
        HeadTexture = new FormLinkNullable<ITextureSetGetter>(face.HeadTexture.FormKey),
        HairColor = new FormLinkNullable<IColorRecordGetter>(face.HairColor.FormKey),
        TextureLighting = face.TextureLighting,
        FaceMorph = face.FaceMorph?.DeepCopy(),
        FaceParts = face.FaceParts?.DeepCopy(),
        HeadParts = face.HeadParts.Select(p => (IFormLinkGetter<IHeadPartGetter>)new FormLink<IHeadPartGetter>(p.FormKey)).ToExtendedList(),
        TintLayers = face.TintLayers.Select(t => t.DeepCopy()).ToExtendedList(),
        Configuration = new NpcConfiguration
        {
            Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
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
            EnergyLevel = 40,
        },
        ObjectBounds = new ObjectBounds { First = new P3Int16(-22, -14, 0), Second = new P3Int16(22, 14, 128) },
        PlayerSkills = new PlayerSkills(),
        Height = face.Height,
        Weight = face.Weight,
    };
}
