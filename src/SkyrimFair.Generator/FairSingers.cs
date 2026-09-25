using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The stage singers (docs/BARDS.md, "Generator side"): three men at the front of the deck
/// whose lips follow the songs, the way vanilla bards sing. Vanilla sings in dialogue:
/// one line per sung phrase, a voice file per line and voice type, and the engine picks
/// the file by the speaker's voice type. So here:
/// - each singer has his own voice type
/// - each sung line is a topic with one INFO, no text and so no subtitle, and Happy 50
/// - the voice files from tools/bards/build_vocals.py (build/bards/&lt;song&gt;/) are copied
///   to the names the engine looks for, which are checked against vanilla's BardSongs:
///   <c>Sound\Voice\SkyrimFair.esp\&lt;voice type&gt;\&lt;quest&gt;_&lt;topic&gt;_&lt;INFO id, 8 hex&gt;_&lt;response&gt;.fuz</c>.
///   The topics have no EditorID, as BardSongs' songs, and the quest's is short, so the
///   CK's truncation (past 25 characters) never applies.
///
/// The singers can't use the fair's Traits templates for their faces: Traits carries the
/// voice type too. Each is a new NPC with a vanilla NPC's face data copied field by field,
/// and that NPC's FaceGen head and tint copied from the game's archives under his FormID.
///
/// The stage script says each line on all three at the line's start, timed from the song's
/// own start (properties Singers, SingerTopics, SingerStarts, SongFirstLine, SongLineCount).
/// </summary>
internal static class FairSingers
{
    public static (int Singers, int Lines, int Files) Build(
        SkyrimMod mod, SingersConfig config, AudioConfig audio, VendorsConfig looksFrom, ISkyrimModGetter master,
        FormKey stageQuest, Action<PlacedNpc> putPersistent)
    {
        // ---- voice types, and the singers with their own faces
        var archives = config.FaceArchives
            .Select(d => Path.IsPathRooted(d) ? d : Path.Combine(FairPaths.ConfigDirectory, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.bsa").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var singers = new List<(SingerMember Member, VoiceType Voice, PlacedNpc Ref)>();
        foreach (var member in config.Members)
        {
            var voice = new VoiceType(mod) { EditorID = member.VoiceType };
            mod.VoiceTypes.Add(voice);

            var face = master.Npcs.FirstOrDefault(n => n.FormKey == FormKeyHelper.Parse(member.Face))
                ?? throw new InvalidOperationException($"singers {member.Id}: no NPC {member.Face} in {master.ModKey}");
            var npc = new Npc(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{member.Id}",
                Name = config.Name,
                Race = new FormLink<IRaceGetter>(face.Race.FormKey),
                Voice = new FormLinkNullable<IVoiceTypeGetter>(voice.FormKey),
                Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(looksFrom.Class)),
                DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(member.Outfit)),
                Configuration = new NpcConfiguration
                {
                    Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected | NpcConfiguration.Flag.Invulnerable
                        | (face.Configuration.Flags & NpcConfiguration.Flag.Female),
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
                    Mood = Mood.Happy,
                    EnergyLevel = 50,
                },
                ObjectBounds = new ObjectBounds { First = new P3Int16(-22, -14, 0), Second = new P3Int16(22, 14, 128) },
                // The face, exactly as the vanilla NPC's, so his FaceGen files fit.
                HeadTexture = new FormLinkNullable<ITextureSetGetter>(face.HeadTexture.FormKey),
                HairColor = new FormLinkNullable<IColorRecordGetter>(face.HairColor.FormKey),
                TextureLighting = face.TextureLighting,
                FaceMorph = face.FaceMorph?.DeepCopy(),
                FaceParts = face.FaceParts?.DeepCopy(),
                Height = face.Height,
                Weight = face.Weight,
                // DNAM: every vanilla NPC has it, and these have no template to fill it.
                PlayerSkills = new PlayerSkills(),
            };
            foreach (var part in face.HeadParts)
            {
                npc.HeadParts.Add(new FormLink<IHeadPartGetter>(part.FormKey));
            }

            foreach (var tint in face.TintLayers)
            {
                npc.TintLayers.Add(tint.DeepCopy());
            }

            npc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.Package)));
            mod.Npcs.Add(npc);

            // His FaceGen head and tint: the vanilla NPC's files, under his own FormID.
            var from = $"{face.FormKey.ID:x8}";
            var to = $"{npc.FormKey.ID:X8}";
            CopyFromArchives(archives, $@"meshes\actors\character\facegendata\facegeom\{master.ModKey.FileName.String.ToLowerInvariant()}\{from}.nif",
                Out(config.MeshesOut, $@"actors\character\FaceGenData\FaceGeom\{mod.ModKey.FileName}\{to}.nif"));
            CopyFromArchives(archives, $@"textures\actors\character\facegendata\facetint\{master.ModKey.FileName.String.ToLowerInvariant()}\{from}.dds",
                Out(config.TexturesOut, $@"actors\character\FaceGenData\FaceTint\{mod.ModKey.FileName}\{to}.dds"));

            var at = member.At;
            var placed = new PlacedNpc(mod)
            {
                EditorID = $"{npc.EditorID}Ref",
                Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                Placement = new Placement { Position = new P3Float(at[0], at[1], at[2] + 2f), Rotation = new P3Float(0f, 0f, at[3] * MathF.PI / 180f) },
            };
            putPersistent(placed);
            singers.Add((member, voice, placed));
        }

        // ---- the quest and one topic per sung line
        var quest = new Quest(mod)
        {
            EditorID = config.QuestEditorId,
            Name = "Stage singers",
            Flags = Quest.Flag.StartGameEnabled,
            Priority = 0,
            // Mutagen leaves these out unless set; every vanilla quest has ANAM, every INFO CNAM.
            NextAliasID = 0,
        };
        mod.Quests.Add(quest);

        var voiceRoot = Path.Combine(Out(config.VoiceOut, ""), mod.ModKey.FileName);
        if (Directory.Exists(voiceRoot))
        {
            Directory.Delete(voiceRoot, recursive: true);  // stale lines from earlier FormIDs
        }

        // Only the singers' voice types may use these lines (the subtype is also idle chatter's).
        var voices = new FormList(mod) { EditorID = $"{config.EditorIdPrefix}Voices" };
        foreach (var (_, voice, _) in singers)
        {
            voices.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(voice.FormKey));
        }

        mod.FormLists.Add(voices);

        var topics = new List<FormKey>();
        var starts = new List<float>();
        var firstLine = new List<int>();
        var lineCount = new List<int>();
        var files = 0;
        foreach (var song in audio.Stage.Songs)
        {
            var cues = config.Songs.TryGetValue(song.Name, out var folder)
                ? Path.Combine(Path.IsPathRooted(config.CuesDir) ? config.CuesDir : Path.Combine(FairPaths.ConfigDirectory, config.CuesDir), folder)
                : null;
            var cuesFile = cues is null ? null : Path.Combine(cues, "cues.json");
            if (cuesFile is null || !File.Exists(cuesFile))
            {
                firstLine.Add(-1);
                lineCount.Add(0);
                continue;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(cuesFile));
            var lines = doc.RootElement.GetProperty("lines").EnumerateArray().ToList();
            firstLine.Add(topics.Count);
            lineCount.Add(lines.Count);
            foreach (var line in lines)
            {
                // Exactly BardSongs' song topics: Misc, subtype IDAT, no branch (DATA 00 07 0054,
                // SNAM "IDAT"). A Topic-category DIAL with no branch, and an empty SNAM,
                // crashed the game at startup: every vanilla Topic-category DIAL has a branch.
                var topic = new DialogTopic(mod)
                {
                    Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
                    Category = (DialogTopic.CategoryEnum)7,
                    Subtype = (DialogTopic.SubtypeEnum)0x54,
                    SubtypeName = new RecordType("IDAT"),
                    Priority = 50f,
                };
                var info = new DialogResponses(mod)
                {
                    Flags = new DialogResponseFlags(),
                    FavorLevel = FavorLevel.None,
                };
                info.Responses.Add(new DialogResponse
                {
                    Emotion = Emotion.Happy,
                    EmotionValue = (uint)config.EmotionValue,
                    ResponseNumber = 1,
                    Flags = DialogResponse.Flag.UseEmotionAnimation,
                    Text = string.Empty,
                    ScriptNotes = string.Empty,
                    Edits = string.Empty,
                });
                var singerVoice = new GetIsVoiceTypeConditionData { RunOnType = Condition.RunOnType.Subject };
                singerVoice.VoiceTypeOrList.Link.SetTo(voices.FormKey);
                info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = singerVoice });
                topic.Responses.Add(info);
                mod.DialogTopics.Add(topic);
                topics.Add(topic.FormKey);
                starts.Add(line.GetProperty("start").GetSingle());

                // The engine's name for this line's voice file, for each singer's voice type.
                var name = $"{config.QuestEditorId}__{info.FormKey.ID:x8}_1.fuz".ToLowerInvariant();
                foreach (var (member, voice, _) in singers)
                {
                    var src = Path.Combine(cues!, line.GetProperty("files").GetProperty(member.Id).GetString()!.Replace('/', Path.DirectorySeparatorChar));
                    var dst = Path.Combine(voiceRoot, voice.EditorID!, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    File.Copy(src, dst, overwrite: true);
                    files++;
                }
            }
        }

        // ---- the stage script's schedule
        var script = mod.Quests.First(q => q.FormKey == stageQuest).VirtualMachineAdapter!.Scripts[0];
        ScriptObjectProperty Obj(FormKey key) => new() { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        script.Properties.Add(new ScriptObjectListProperty { Name = "Singers", Objects = singers.Select(s => Obj(s.Ref.FormKey)).ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "SingerTopics", Objects = topics.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptFloatListProperty { Name = "SingerStarts", Data = starts.ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "SongFirstLine", Data = firstLine.ToExtendedList() });
        script.Properties.Add(new ScriptIntListProperty { Name = "SongLineCount", Data = lineCount.ToExtendedList() });

        // ---- the line's steps: an anchor to keep an offset from, in its own FormID range
        var steps = audio.Stage.SingerSteps;
        if (steps.Offsets.Count > 0)
        {
            var a = config.Anchor;
            if (a.At.Length != 3)
            {
                throw new InvalidOperationException("singers.anchor.at: [x, y, z] is needed for songs.config.json singerSteps");
            }

            var saved = mod.ModHeader.Stats.NextFormID;
            if (saved >= a.FormIdBase)
            {
                throw new InvalidOperationException($"FormIDs reached the singers' anchor range (0x{a.FormIdBase:X}): raise singers.anchor.formIdBase");
            }

            mod.ModHeader.Stats.NextFormID = a.FormIdBase;
            var anchorNpc = new Npc(mod)
            {
                EditorID = $"{config.EditorIdPrefix}Anchor",
                Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(a.Race)),
                Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(looksFrom.Class)),
                Configuration = new NpcConfiguration
                {
                    Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Invulnerable,
                    Level = new NpcLevel { Level = 1 },
                    CalcMinLevel = 1,
                    CalcMaxLevel = 1,
                    SpeedMultiplier = 100,
                },
                AIData = new AIData
                {
                    Aggression = Aggression.Unaggressive,
                    Confidence = Confidence.Cowardly,
                    Responsibility = Responsibility.NoCrime,
                    Assistance = Assistance.HelpsNobody,
                },
                ObjectBounds = new ObjectBounds { First = new P3Int16(-22, -14, 0), Second = new P3Int16(22, 14, 128) },
                PlayerSkills = new PlayerSkills(),
            };
            anchorNpc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.Package)));
            mod.Npcs.Add(anchorNpc);
            var anchorRef = new PlacedNpc(mod)
            {
                EditorID = $"{anchorNpc.EditorID}Ref",
                Base = new FormLinkNullable<INpcGetter>(anchorNpc.FormKey),
                Placement = new Placement { Position = new P3Float(a.At[0], a.At[1], a.At[2]), Rotation = new P3Float(0f, 0f, 0f) },
                Scale = a.Scale,
            };
            putPersistent(anchorRef);
            mod.ModHeader.Stats.NextFormID = saved;

            // Each mark as an offset from the anchor; the anchor faces north, so its frame is the world's.
            var members = singers.Select(s => s.Member.At).ToList();
            script.Properties.Add(new ScriptObjectProperty { Name = "SingerAnchor", Object = new FormLink<ISkyrimMajorRecordGetter>(anchorRef.FormKey) });
            script.Properties.Add(new ScriptFloatListProperty { Name = "SingerHomeX", Data = members.Select(m => m[0] - a.At[0]).ToExtendedList() });
            script.Properties.Add(new ScriptFloatListProperty { Name = "SingerHomeY", Data = members.Select(m => m[1] - a.At[1]).ToExtendedList() });
            script.Properties.Add(new ScriptFloatListProperty { Name = "SingerHomeZ", Data = members.Select(m => m[2] + 2f - a.At[2]).ToExtendedList() });
            script.Properties.Add(new ScriptFloatListProperty { Name = "SingerFacing", Data = members.Select(m => m[3]).ToExtendedList() });
            script.Properties.Add(new ScriptFloatListProperty { Name = "SingerStepOffsets", Data = steps.Offsets.ToExtendedList() });
            script.Properties.Add(new ScriptFloatProperty { Name = "SingerStepEvery", Data = steps.Every });
            script.Properties.Add(new ScriptFloatProperty { Name = "SingerStepSeconds", Data = steps.StepSeconds });
            script.Properties.Add(new ScriptFloatProperty { Name = "SingerFirstStep", Data = steps.FirstStep });
            script.Properties.Add(new ScriptFloatProperty { Name = "SingerCatchUp", Data = steps.CatchUpRadius });
            script.Properties.Add(new ScriptFloatProperty { Name = "SingerFollow", Data = steps.FollowRadius });
            Console.WriteLine($"  singer steps: offsets [{string.Join(", ", steps.Offsets)}] every {steps.Every} s; anchor {anchorRef.FormKey.ID:X6} at ({a.At[0]}, {a.At[1]}, {a.At[2]})");
        }

        return (singers.Count, topics.Count, files);
    }

    /// <summary>
    /// A vanilla NPC's FaceGen head and tint, copied from the game's archives
    /// (<see cref="SingersConfig.FaceArchives"/>) under another NPC's FormID.
    /// </summary>
    internal static void CopyFace(SingersConfig config, ISkyrimModGetter master, FormKey from, SkyrimMod mod, FormKey to)
    {
        var archives = config.FaceArchives
            .Select(d => Path.IsPathRooted(d) ? d : Path.Combine(FairPaths.ConfigDirectory, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.bsa").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var (f, t) = ($"{from.ID:x8}", $"{to.ID:X8}");
        CopyFromArchives(archives, $@"meshes\actors\character\facegendata\facegeom\{master.ModKey.FileName.String.ToLowerInvariant()}\{f}.nif",
            Out(config.MeshesOut, $@"actors\character\FaceGenData\FaceGeom\{mod.ModKey.FileName}\{t}.nif"));
        CopyFromArchives(archives, $@"textures\actors\character\facegendata\facetint\{master.ModKey.FileName.String.ToLowerInvariant()}\{f}.dds",
            Out(config.TexturesOut, $@"actors\character\FaceGenData\FaceTint\{mod.ModKey.FileName}\{t}.dds"));
    }

    private static string Out(string root, string rel)
    {
        var baseDir = Path.IsPathRooted(root) ? root : Path.Combine(FairPaths.ConfigDirectory, root);
        return Path.Combine(baseDir, rel);
    }

    private static void CopyFromArchives(IReadOnlyList<string> archives, string path, string dst)
    {
        foreach (var bsa in archives)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, bsa);
            var file = reader.Files.FirstOrDefault(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (file is null)
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.WriteAllBytes(dst, file.GetBytes());
            return;
        }

        throw new InvalidOperationException($"singers: {path} is in none of the archives in singers.faceArchives");
    }
}
