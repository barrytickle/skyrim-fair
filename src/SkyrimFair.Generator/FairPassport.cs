using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The Fair Passport (<see cref="PassportConfig"/>): a side quest Claudius Vale hands out when
/// first spoken to. Its objectives are the stamps (a whole song, Garrick, Claudius, five visitors
/// talked to, the roof horse seen); a full card handed back to him earns Claudius's Seal of
/// Approval (a gold necklace, Fortify Barter) and the Deed to the Roof Horse (SkyrimFairPassport.psc).
///
/// Built last of all in its own FormID range, after the cameos, the talk perk and the seat swap:
/// it hangs its two lines on the inspector's Hello topic (at the top, so they win over his random
/// greetings while their stage condition holds) and gives the stage script, the cameos' lines and
/// the talk perk a Passport property. Records copied from vanilla keep vanilla's subrecords: the
/// notes from MS07JareeRaNote, the amulet from JewelryNecklaceGold, the enchantment from
/// EnchArmorFortifySpeechcraftBase; the quest follows MS07 (SideQuest) and FreeformRiften07 (an
/// objective counting a text-display global).
/// </summary>
internal static class FairPassport
{
    public static string Build(SkyrimMod mod, FairWorldConfig fw, ISkyrimModGetter master)
    {
        var config = fw.Passport;
        var cameos = fw.Cameos;
        var stage = mod.Quests.First(q => q.VirtualMachineAdapter?.Scripts.Any(s => s.Name == "SkyrimFairAudioScript") == true);
        var inspector = mod.Npcs.First(n => n.EditorID == $"{cameos.EditorIdPrefix}{config.Inspector}");
        var fairWs = mod.Worldspaces.First(w => w.EditorID == fw.EditorId);
        var horse = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedNpc>())
            .FirstOrDefault(n => n.EditorID == $"{cameos.EditorIdPrefix}RoofHorseRef");
        var duck = mod.Globals.First(g => g.EditorID == $"{cameos.EditorIdPrefix}DuckUntil");
        bool IsHim(IConditionGetter c, FormKey npc) => c.Data is IGetIsIDConditionDataGetter id && id.Object.Link.FormKey == npc;
        var topic = mod.DialogTopics.First(t => t.Responses.Any(r => r.Conditions.Any(c => IsHim(c, inspector.FormKey))));
        ScriptObjectProperty Obj(string name, FormKey key) => new() { Name = name, Object = new FormLink<ISkyrimMajorRecordGetter>(key) };

        // ---- the items --------------------------------------------------------------------------
        var chats = new GlobalShort(mod) { EditorID = $"{config.EditorIdPrefix}Chats", Data = 0 };
        mod.Globals.Add(chats);
        var chatted = new FormList(mod) { EditorID = $"{config.EditorIdPrefix}Chatted" };
        mod.FormLists.Add(chatted);

        Book Note(string from, string id, string name, string text)
        {
            var book = master.Books.First(b => b.FormKey == FormKeyHelper.Parse(from)).Duplicate(mod.GetNextFormKey());
            book.EditorID = $"{config.EditorIdPrefix}{id}";
            book.Name = name;
            book.BookText = text;
            mod.Books.Add(book);
            return book;
        }

        var passport = Note(config.PassportFrom, "Note", config.PassportName, config.PassportText);
        var deed = Note(config.NoteFrom, "Deed", config.DeedName, config.DeedText);

        var enchantment = master.ObjectEffects.First(e => e.FormKey == FormKeyHelper.Parse(config.SealEnchantmentFrom)).Duplicate(mod.GetNextFormKey());
        enchantment.EditorID = $"{config.EditorIdPrefix}SealEnchantment";
        foreach (var effect in enchantment.Effects)
        {
            effect.Data!.Magnitude = config.SealBarter;
        }

        mod.ObjectEffects.Add(enchantment);
        var seal = master.Armors.First(a => a.FormKey == FormKeyHelper.Parse(config.SealFrom)).Duplicate(mod.GetNextFormKey());
        seal.EditorID = $"{config.EditorIdPrefix}Seal";
        seal.Name = config.SealName;
        seal.ObjectEffect = new FormLinkNullable<IObjectEffectGetter>(enchantment.FormKey);
        seal.Value = config.SealValue;
        seal.TemplateArmor = new FormLinkNullable<IArmorGetter>(FormKeyHelper.Parse(config.SealFrom));  // TNAM, as vanilla's enchanted necklaces
        seal.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
        seal.Keywords.Add(new FormLink<IKeywordGetter>(FormKeyHelper.Parse(config.NoDisenchant)));
        mod.Armors.Add(seal);

        // ---- the quest: stages 0, 10 (issued), 15 (full), 20 (returned); objectives 10-60 -------
        var quest = new Quest(mod)
        {
            EditorID = config.EditorIdPrefix,
            Name = config.QuestName,
            Flags = Quest.Flag.StartGameEnabled | Quest.Flag.RunOnce,
            Priority = 60,
            Type = Quest.TypeEnum.SideQuest,
            Filter = "Misc\\SkyrimFair\\",
            NextAliasID = 1,
        };

        // The passport's alias: filled by the script (ForceRefTo) when Claudius hands it over, and
        // a Quest Object, so the passport can't leave the player's inventory (vanilla CR12 'Totem':
        // Optional, Quest Object, no fill).
        quest.Aliases.Add(new QuestAlias
        {
            ID = 0,
            Type = QuestAlias.TypeEnum.Reference,
            Name = "PassportItem",
            Flags = QuestAlias.Flag.Optional | QuestAlias.Flag.QuestObject,
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),  // VTCK 0, as vanilla's
        });
        quest.TextDisplayGlobals.Add(new FormLink<IGlobalGetter>(chats.FormKey));
        QuestStage Stage(ushort index, string? log, bool complete = false)
        {
            // Every vanilla stage has a log entry (QSDT), if only an empty one.
            var s = new QuestStage { Index = index };
            s.LogEntries.Add(new QuestLogEntry { Flags = complete ? QuestLogEntry.Flag.CompleteQuest : 0, Entry = log });
            return s;
        }

        string Log(int i) => i < config.Log.Count ? config.Log[i] : string.Empty;
        quest.Stages.Add(Stage(0, null));
        quest.Stages.Add(Stage(10, Log(0)));
        quest.Stages.Add(Stage(15, Log(1)));
        quest.Stages.Add(Stage(20, Log(2), complete: true));
        for (var i = 0; i < config.Objectives.Count; i++)
        {
            // An empty entry is a retired objective: its index stays unused, so the others keep theirs.
            if (string.IsNullOrEmpty(config.Objectives[i]))
            {
                continue;
            }

            var text = config.Objectives[i]
                .Replace("{count}", $"<Global={chats.EditorID}>", StringComparison.Ordinal)
                .Replace("{total}", config.Chats.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
            quest.Objectives.Add(new QuestObjective { Index = (ushort)((i + 1) * 10), Flags = 0, DisplayText = text });  // FNAM, as vanilla's
        }

        var script = new ScriptEntry { Name = config.Script };
        script.Properties.Add(Obj("PassportNote", passport.FormKey));
        script.Properties.Add(new ScriptObjectProperty { Name = "PassportItem", Object = new FormLink<ISkyrimMajorRecordGetter>(quest.FormKey), Alias = 0 });
        script.Properties.Add(Obj("HorseDeed", deed.FormKey));
        script.Properties.Add(Obj("Seal", seal.FormKey));
        script.Properties.Add(Obj("Chats", chats.FormKey));
        script.Properties.Add(new ScriptIntProperty { Name = "ChatsNeeded", Data = config.Chats });
        script.Properties.Add(Obj("Chatted", chatted.FormKey));
        if (horse is not null)
        {
            script.Properties.Add(Obj("Horse", horse.FormKey));
        }

        script.Properties.Add(new ScriptFloatProperty { Name = "HorseRange", Data = config.HorseRange });
        script.Properties.Add(Obj("Fair", fairWs.FormKey));
        quest.VirtualMachineAdapter = new QuestAdapter();
        quest.VirtualMachineAdapter.Scripts.Add(script);
        mod.Quests.Add(quest);

        // ---- his two lines, at the top of his Hello topic -----------------------------------------
        var cues = Path.Combine(FairPaths.ConfigDirectory, cameos.VoiceBuildDir, "Passport");
        var recorded = File.Exists(Path.Combine(cues, "lines.json"))
            ? System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(cues, "lines.json"))).RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
            : new List<System.Text.Json.JsonElement>();
        var voice = mod.VoiceTypes.First(v => v.FormKey == inspector.Voice.FormKey);
        var voiceRoot = Path.Combine(Path.IsPathRooted(fw.Singers.VoiceOut) ? fw.Singers.VoiceOut : Path.Combine(FairPaths.ConfigDirectory, fw.Singers.VoiceOut), mod.ModKey.FileName);
        var emotion = Enum.Parse<Emotion>(cameos.Members.First(m => m.Id == config.Inspector).Emotion);
        var lines = new[] { (Step: 1, Stage: 0f, Text: config.Give), (Step: 2, Stage: 15f, Text: config.Done) };
        for (var k = 0; k < lines.Length; k++)
        {
            var (step, atStage, fallback) = lines[k];
            var cue = k < recorded.Count ? recorded[k] : (System.Text.Json.JsonElement?)null;
            var text = cue?.GetProperty("text").GetString() ?? fallback;
            var info = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };  // ENAM, as vanilla's
            info.Responses.Add(new DialogResponse
            {
                Emotion = emotion,
                EmotionValue = 50,
                ResponseNumber = 1,
                Flags = DialogResponse.Flag.UseEmotionAnimation,
                Text = text,
                ScriptNotes = string.Empty,
                Edits = string.Empty,
            });
            var him = new GetIsIDConditionData { RunOnType = Condition.RunOnType.Subject };
            him.Object.Link.SetTo(inspector.FormKey);
            info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = him });
            var at = new GetStageConditionData { RunOnType = Condition.RunOnType.Subject };
            at.Quest.Link.SetTo(quest.FormKey);
            info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = atStage, Data = at });
            topic.Responses.Insert(k, info);

            // Unrecorded, the line is a subtitle only, on screen about as long as it takes to read.
            var seconds = cue is { } c && c.TryGetProperty("seconds", out var len) ? len.GetSingle() : 1.5f + text.Length / 15f;
            var entry = new ScriptEntry { Name = config.LineScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(Obj("DuckUntil", duck.FormKey));
            entry.Properties.Add(new ScriptFloatProperty { Name = "Seconds", Data = seconds + 0.5f });
            entry.Properties.Add(Obj("Passport", quest.FormKey));
            entry.Properties.Add(new ScriptIntProperty { Name = "Step", Data = step });
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

            if (cue is { } rec)
            {
                Voice(info.FormKey, 1, rec);
            }
        }

        // A recorded line's voice, under the name the engine looks for (the topics are the cameos
        // quest's, and have no EditorID).
        void Voice(FormKey info, int response, System.Text.Json.JsonElement rec)
        {
            var dst = Path.Combine(voiceRoot, voice.EditorID!, $"{cameos.QuestEditorId}__{info.ID:x8}_{response}.fuz".ToLowerInvariant());
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            var fuz = File.ReadAllBytes(Path.Combine(cues, rec.GetProperty("fuz").GetString()!));
            if (cameos.LooseLip)
            {
                // As FairCameos: the .fuz unpacked into the .lip and .xwm plain Skyrim reads.
                var lipSize = BitConverter.ToInt32(fuz, 8);
                File.WriteAllBytes(Path.ChangeExtension(dst, ".lip"), fuz[12..(12 + lipSize)]);
                File.WriteAllBytes(Path.ChangeExtension(dst, ".xwm"), fuz[(12 + lipSize)..]);
            }
            else
            {
                File.WriteAllBytes(dst, fuz);
            }
        }

        // ---- the stamps' senders get the quest ---------------------------------------------------
        stage.VirtualMachineAdapter!.Scripts.First(s => s.Name == "SkyrimFairAudioScript").Properties.Add(Obj("Passport", quest.FormKey));
        var stampOf = new Dictionary<FormKey, int>();
        foreach (var m in cameos.Members)
        {
            var npc = mod.Npcs.FirstOrDefault(n => n.EditorID == $"{cameos.EditorIdPrefix}{m.Id}");
            if (npc is not null)
            {
                // The Inspector's stamp (30) is retired: he hands the passport over, so talking to him
                // was done the moment it began (Barry, 2026-09-26). His lines stamp nothing.
                stampOf[npc.FormKey] = m.Id == config.Inspector ? 0 : 20;
            }
        }

        var stamped = 0;
        foreach (var info in mod.DialogTopics.SelectMany(t => t.Responses))
        {
            var line = info.VirtualMachineAdapter?.Scripts.FirstOrDefault(s => s.Name == cameos.LineScript);
            var speaker = info.Conditions.Select(c => c.Data).OfType<IGetIsIDConditionDataGetter>().Select(d => d.Object.Link.FormKey).FirstOrDefault(stampOf.ContainsKey);
            if (line is null || speaker.IsNull)
            {
                continue;
            }

            line.Properties.Add(Obj("Passport", quest.FormKey));
            line.Properties.Add(new ScriptIntProperty { Name = "Stamp", Data = stampOf[speaker] });
            stamped++;
        }

        var talk = mod.Perks.SelectMany(p => p.VirtualMachineAdapter?.Scripts ?? Enumerable.Empty<ScriptEntry>())
            .FirstOrDefault(s => s.Name == fw.TalkDuck.Script);
        talk?.Properties.Add(Obj("Passport", quest.FormKey));

        // ---- his run-up (Barry, 2026-09-26: a nod to Oblivion's guards) ----------------------------
        // Built last, so its records take the range's next IDs and nothing before them moves. While
        // the passport isn't issued and the player is in the fair, Claudius runs to them (a copy of
        // a vanilla force greet whose every location is the player) and opens a blocking branch whose
        // one line is two responses, the stop then the hand-over, as Ancano's run-up in MG03 is.
        var runUp = string.Empty;
        if (!string.IsNullOrEmpty(config.Stop))
        {
            var cameoQuest = topic.Quest.FormKey;
            var greetTopic = new DialogTopic(mod)
            {
                Quest = new FormLinkNullable<IQuestGetter>(cameoQuest),
                Priority = 50f,
                TopicFlags = 0,
                Category = DialogTopic.CategoryEnum.Topic,
                Subtype = DialogTopic.SubtypeEnum.Custom,
                // SNAM, the subtype's code: Mutagen writes it from this, not from Subtype, and left it
                // blank, which crashed the game at startup (2026-09-26). Vanilla's say CUST.
                SubtypeName = new Mutagen.Bethesda.Plugins.RecordType("CUST"),
            };
            var branch = new DialogBranch(mod)
            {
                EditorID = $"{config.EditorIdPrefix}StopBranch",
                Quest = new FormLink<IQuestGetter>(cameoQuest),
                Category = DialogBranch.CategoryType.Player,
                Flags = DialogBranch.Flag.Blocking,
                StartingTopic = new FormLinkNullable<IDialogTopicGetter>(greetTopic.FormKey),
            };
            greetTopic.Branch = new FormLinkNullable<IDialogBranchGetter>(branch.FormKey);

            var stopCue = recorded.Count >= 3 ? recorded[2] : (System.Text.Json.JsonElement?)null;
            var giveCue = recorded.Count >= 1 ? recorded[0] : (System.Text.Json.JsonElement?)null;
            var stopText = stopCue?.GetProperty("text").GetString() ?? config.Stop;
            var giveText = giveCue?.GetProperty("text").GetString() ?? config.Give;
            var info = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };  // ENAM, as vanilla's
            byte n = 1;
            foreach (var text in new[] { stopText, giveText })
            {
                info.Responses.Add(new DialogResponse
                {
                    Emotion = emotion,
                    EmotionValue = 50,
                    ResponseNumber = n++,
                    Flags = DialogResponse.Flag.UseEmotionAnimation,
                    Text = text,
                    ScriptNotes = string.Empty,
                    Edits = string.Empty,
                });
            }

            var him = new GetIsIDConditionData { RunOnType = Condition.RunOnType.Subject };
            him.Object.Link.SetTo(inspector.FormKey);
            info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = him });
            var notIssued = new GetStageConditionData { RunOnType = Condition.RunOnType.Subject };
            notIssued.Quest.Link.SetTo(quest.FormKey);
            info.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 0f, Data = notIssued });
            greetTopic.Responses.Add(info);

            float Seconds(System.Text.Json.JsonElement? cue, string text) =>
                cue is { } c && c.TryGetProperty("seconds", out var len) ? len.GetSingle() : 1.5f + text.Length / 15f;
            var entry = new ScriptEntry { Name = config.LineScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(Obj("DuckUntil", duck.FormKey));
            entry.Properties.Add(new ScriptFloatProperty { Name = "Seconds", Data = Seconds(stopCue, stopText) + Seconds(giveCue, giveText) + 0.5f });
            entry.Properties.Add(Obj("Passport", quest.FormKey));
            entry.Properties.Add(new ScriptIntProperty { Name = "Step", Data = 1 });
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
            if (stopCue is { } s1)
            {
                Voice(info.FormKey, 1, s1);
            }

            if (giveCue is { } s2)
            {
                Voice(info.FormKey, 2, s2);
            }

            mod.DialogTopics.Add(greetTopic);
            mod.DialogBranches.Add(branch);

            // The package: the vanilla one with this topic, on these conditions. Only while the player
            // is in the fair: elsewhere he'd set out across Skyrim after them.
            var greet = master.Packages.First(p => p.FormKey == FormKeyHelper.Parse(config.GreetFrom)).Duplicate(mod.GetNextFormKey());
            greet.EditorID = $"{config.EditorIdPrefix}RunUp";
            greet.VirtualMachineAdapter = null;
            // Wait (8) and trigger (62) near himself, as Ancano's; the greet distance (75) stays on
            // the player, so once triggered he runs to them wherever they are.
            var nearSelf = master.Packages.First(p => p.FormKey == FormKeyHelper.Parse(config.GreetTriggerFrom)).Data[62];
            var waitHere = (PackageDataLocation)nearSelf.DeepCopy();
            waitHere.Location.Radius = 128;
            greet.Data[8] = waitHere;
            var trigger = (PackageDataLocation)nearSelf.DeepCopy();
            trigger.Location.Radius = (uint)config.GreetRange;
            greet.Data[62] = trigger;
            var topicInput = greet.Data.Values.OfType<PackageDataTopic>().Single();
            topicInput.Topics.Clear();
            topicInput.Topics.Add(new TopicReference { Reference = new FormLink<IDialogTopicGetter>(greetTopic.FormKey) });
            greet.Conditions.Clear();
            var stillNot = new GetStageConditionData { RunOnType = Condition.RunOnType.Subject };
            stillNot.Quest.Link.SetTo(quest.FormKey);
            greet.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 0f, Data = stillNot });
            var inFair = new GetInWorldspaceConditionData { RunOnType = Condition.RunOnType.Reference, Reference = new FormLink<ISkyrimMajorRecordGetter>(FormKeyHelper.Parse("00000014:Skyrim.esm")) };
            inFair.WorldspaceOrList.Link.SetTo(fairWs.FormKey);
            greet.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = inFair });
            mod.Packages.Add(greet);
            inspector.Packages.Insert(0, new FormLink<IPackageGetter>(greet.FormKey));
            stage.VirtualMachineAdapter!.Scripts.First(s => s.Name == "SkyrimFairAudioScript").Properties.Add(Obj("PassportGreet", greet.FormKey));
            runUp = $", run-up {(stopCue is null ? "subtitled" : "voiced")}";
        }

        return $"{quest.EditorID}: {quest.Objectives.Count} objectives, {stamped} cameo lines stamp, "
            + $"lines {(recorded.Count >= 2 ? "voiced" : "subtitles only")}, horse {(horse is null ? "missing" : "found")}, talk perk {(talk is null ? "missing" : "hooked")}{runUp}";
    }
}
