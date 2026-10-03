using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// 2.0.2, the fair comes and goes (<see cref="CampConfig"/>, docs/CAMP.md, docs/MCM.md).
///
/// One switch for everything outside in Tamriel: a persistent marker is the enable parent of
/// every fair reference there, and of the vanilla references the fair disables ("opposite", so
/// they come back while it's away). In the fair's place, Claudius's camp by the road, enabled
/// opposite the marker. Claudius is moved between the fair's entrance and the camp, and a sandbox
/// at the camp heads his packages while the fair is away (<c>SkyrimFairAway</c>).
///
/// His talk, in the cameos' quest: the camp's greetings, "when will it be back", "bring it back
/// now" (the fade), and the schedule (every day, or only after the main story). The controller
/// quest (SkyrimFairCamp.psc) swaps the fair and the camp only out of the player's sight.
///
/// Built last of all in its own FormID range: the exterior, the cameos and the Passport are all
/// built first, and this only adds to them (enable parents, conditions, a package at the top).
/// </summary>
internal static class FairCamp
{
    private const int PersistentFlag = 0x400;

    private const int InitiallyDisabledFlag = 0x800;

    private const float CellSize = 4096f;

    private static readonly FormKey XMarker = FormKey.Factory("00003B:Skyrim.esm");

    private static readonly FormKey XMarkerHeading = FormKey.Factory("000034:Skyrim.esm");

    private static readonly FormKey Player = FormKey.Factory("000014:Skyrim.esm");

    public static string Build(SkyrimMod mod, FairConfig config, ISkyrimModGetter master, IWorldspaceGetter vanilla, Worldspace tamriel, ExteriorResult exterior)
    {
        var fw = config.FairWorld;
        var camp = fw.Camp;
        var ext = config.Exterior;
        var cameos = fw.Cameos;
        var passport = fw.Passport;
        ScriptObjectProperty Obj(string name, FormKey key) => new() { Name = name, Object = new FormLink<ISkyrimMajorRecordGetter>(key) };

        // ---- the globals ------------------------------------------------------------------------
        GlobalShort Global(string name)
        {
            var g = new GlobalShort(mod) { EditorID = $"SkyrimFair{name}", Data = 0 };
            mod.Globals.Add(g);
            return g;
        }

        var away = Global("Away");
        var schedule = Global("Schedule");
        var storyGate = Global("StoryGate");
        var storyDone = Global("StoryDone");

        // ---- the frame: the gate, and out from it toward the road --------------------------------
        var (gx, gy) = (ext.Gate[0], ext.Gate[1]);
        var (ofx, ofy) = ext.Approach.To is { Length: 2 } road ? (road[0] - gx, road[1] - gy) : (0f, 1f);
        var len = MathF.Sqrt(ofx * ofx + ofy * ofy);
        (ofx, ofy) = (ofx / len, ofy / len);
        var (rx, ry) = (ofy, -ofx);
        var facing = MathF.Atan2(ofx, ofy);
        var terrain = new TerrainSampler(vanilla);
        float Ground(float x, float y) => terrain.Sample(x, y) ?? throw new InvalidOperationException($"camp: no Tamriel ground at ({x:0}, {y:0})");
        (float X, float Y, float Yaw) At(ExteriorDecor d) =>
            (gx + rx * d.Side + ofx * d.Forward, gy + ry * d.Side + ofy * d.Forward, facing + d.Yaw * MathF.PI / 180f);

        // Tilted to the ground's slope, as FairExterior.OnGround.
        P3Float OnGround(float x, float y, float yaw, float e)
        {
            var (zx, zy) = ((Ground(x + e, y) - Ground(x - e, y)) / (2f * e), (Ground(x, y + e) - Ground(x, y - e)) / (2f * e));
            var l = MathF.Sqrt(zx * zx + zy * zy + 1f);
            var (nx, ny, nz) = (-zx / l, -zy / l, 1f / l);
            return new P3Float(MathF.Atan2(ny, nz), -MathF.Asin(nx), yaw);
        }

        // ---- the markers, persistent in Tamriel ---------------------------------------------------
        var top = tamriel.TopCell ?? throw new InvalidOperationException("camp: Tamriel has no persistent cell");
        PlacedObject Marker(string name, FormKey kind, float x, float y, float z, float yaw)
        {
            var o = new PlacedObject(mod)
            {
                EditorID = $"SkyrimFair{name}",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(kind),
                MajorRecordFlagsRaw = PersistentFlag,
                Placement = new Placement { Position = new P3Float(x, y, z), Rotation = new P3Float(0f, 0f, yaw) },
            };
            top.Persistent.Add(o);
            return o;
        }

        var present = Marker("PresentMarker", XMarker, gx, gy, Ground(gx, gy) + 64f, 0f);
        var (cx, cy, cyaw) = At(camp.Marker);
        var campMarker = Marker("CampMarker", XMarkerHeading, cx, cy, Ground(cx, cy) + 8f, cyaw);
        var (vx, vy, vyaw) = At(camp.View);
        var view = Marker("CampViewMarker", XMarkerHeading, vx, vy, Ground(vx, vy) + 8f, vyaw);

        // ---- the switch: the fair's references in Tamriel follow the marker, vanilla's the opposite --
        var cells = tamriel.SubCells.SelectMany(b => b.Items).SelectMany(sb => sb.Items).ToList();
        var large = vanilla.LargeReferences.SelectMany(l => l.References).Select(r => r.Reference.FormKey).ToHashSet();
        var (fair, back, kept) = (0, 0, 0);
        var placed = cells.SelectMany(c => c.Temporary.Concat(c.Persistent)).Concat(top.Persistent).OfType<IPlaced>().ToList();
        foreach (var r in placed)
        {
            if (r.FormKey == present.FormKey || r.FormKey == campMarker.FormKey || r.FormKey == view.FormKey
                || r is IPlacedObjectGetter { MapMarker: not null })
            {
                continue;
            }

            var ours = r.FormKey.ModKey == mod.ModKey;
            var off = (r.MajorRecordFlagsRaw & InitiallyDisabledFlag) != 0;
            if (ours)
            {
                // Already off for good (the tower banners by the wall, a reserved sign): left so.
                if (off)
                {
                    kept++;
                    continue;
                }

                SetParent(r, present.FormKey, false);
                fair++;
            }
            else if (off && !exterior.Approach.Contains(r.FormKey) && !large.Contains(r.FormKey))
            {
                // Vanilla, cleared for the fair: back while it's away. (The way in stays cleared,
                // and a sunk large reference stays sunk, or its LOD would show.)
                if (EnableParentOf(r) is not null)
                {
                    kept++;
                    continue;
                }

                r.MajorRecordFlagsRaw &= ~InitiallyDisabledFlag;
                SetParent(r, present.FormKey, true);
                back++;
            }
            else
            {
                kept++;
            }
        }

        // ---- the camp: vanilla pieces, there while the fair isn't --------------------------------
        Cell CellAt(float x, float y)
        {
            var (ix, iy) = ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));
            return cells.FirstOrDefault(c => c.Grid is { } g && g.Point.X == ix && g.Point.Y == iy)
                ?? throw new InvalidOperationException($"camp: Tamriel cell ({ix}, {iy}) isn't one the exterior overrides; move the camp");
        }

        foreach (var d in camp.Pieces)
        {
            var (x, y, yaw) = At(d);
            var o = new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(d.Piece)),
                Scale = d.Scale == 1f ? null : d.Scale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, Ground(x, y) + d.Z),
                    Rotation = d.Tilt ? OnGround(x, y, yaw, 60f) : new P3Float(0f, 0f, yaw),
                },
            };
            SetParent(o, present.FormKey, true);
            CellAt(x, y).Temporary.Add(o);
        }

        // ---- Claudius ---------------------------------------------------------------------------
        var inspector = mod.Npcs.First(n => n.EditorID == $"{cameos.EditorIdPrefix}{camp.Inspector}");
        var hisRef = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedNpc>()).Single(n => n.Base.FormKey == inspector.FormKey);
        var entrance = mod.Worldspaces.SelectMany(w => w.EnumerateMajorRecords<IPlacedObject>()).Single(o => o.EditorID == passport.EntranceMarker);
        ConditionFloat Is(ConditionData data, float value, bool or = false) => new()
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = value,
            Data = data,
            Flags = or ? Condition.Flag.OR : 0,
        };
        ConditionFloat GlobalIs(IGlobalGetter g, float value, bool or = false)
        {
            var data = new GetGlobalValueConditionData { RunOnType = Condition.RunOnType.Subject };
            data.Global.Link.SetTo(g.FormKey);
            return Is(data, value, or);
        }

        var mainQuest = FormKeyHelper.Parse(camp.MainQuest);
        ConditionFloat StoryOver(float value, bool or = false)
        {
            var data = new GetQuestCompletedConditionData { RunOnType = Condition.RunOnType.Subject };
            data.Quest.Link.SetTo(mainQuest);
            return Is(data, value, or);
        }

        ConditionFloat Him()
        {
            var data = new GetIsIDConditionData { RunOnType = Condition.RunOnType.Subject };
            data.Object.Link.SetTo(inspector.FormKey);
            return Is(data, 1f);
        }

        // His sandbox at the camp, on top of his packages while the fair is away: a copy of his wait
        // at the entrance, round the camp marker, sitting allowed (the stool by the fire).
        var atEntrance = mod.Packages.First(p => p.EditorID == $"{passport.EditorIdPrefix}AtEntrance");
        var atCamp = atEntrance.Duplicate(mod.GetNextFormKey());
        atCamp.EditorID = $"{camp.EditorIdPrefix}Sandbox";
        atCamp.Data.Values.OfType<PackageDataLocation>().Single().Location = new LocationTargetRadius
        {
            Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(campMarker.FormKey) },
            Radius = (uint)camp.Radius,
        };
        ((PackageDataBool)atCamp.Data[6]).Data = true;  // sitting
        atCamp.Conditions.Clear();
        atCamp.Conditions.Add(GlobalIs(away, 1f));
        mod.Packages.Add(atCamp);
        inspector.Packages.Insert(0, new FormLink<IPackageGetter>(atCamp.FormKey));

        // The Passport's run-up only at the fair: its force greet, and the stop line's branch.
        var force = mod.Packages.First(p => p.EditorID == $"{passport.EditorIdPrefix}ForceGreet");
        force.Conditions.Add(GlobalIs(away, 0f));
        var stopBranch = mod.DialogBranches.First(b => b.EditorID == $"{passport.EditorIdPrefix}StopBranch");
        foreach (var info in mod.DialogTopics.Where(t => t.Branch.FormKey == stopBranch.FormKey).SelectMany(t => t.Responses))
        {
            info.Conditions.Add(GlobalIs(away, 0f));
        }

        // His greetings: the fair's (and the Passport's) only at the fair.
        bool IsHim(IConditionGetter c) => c.Data is IGetIsIDConditionDataGetter id && id.Object.Link.FormKey == inspector.FormKey;
        var hello = mod.DialogTopics.First(t => t.SubtypeName.Type == "HELO" && t.Responses.Any(r => r.Conditions.Any(IsHim)));
        foreach (var info in hello.Responses.Where(r => r.Conditions.Any(IsHim)))
        {
            info.Conditions.Add(GlobalIs(away, 0f));
        }

        // ---- his lines ----------------------------------------------------------------------------
        var cues = Path.Combine(FairPaths.ConfigDirectory, cameos.VoiceBuildDir, "Camp");
        var cuesFile = Path.Combine(cues, "lines.json");
        if (!File.Exists(cuesFile))
        {
            throw new InvalidOperationException($"camp: no {cuesFile}; run tools/cameos/build_voices.py");
        }

        var recorded = System.Text.Json.JsonDocument.Parse(File.ReadAllText(cuesFile)).RootElement.EnumerateArray()
            .ToDictionary(e => e.GetProperty("file").GetString()!, e => e.Clone());
        var voice = mod.VoiceTypes.First(v => v.FormKey == inspector.Voice.FormKey);
        var voiceRoot = Path.Combine(Path.IsPathRooted(fw.Singers.VoiceOut) ? fw.Singers.VoiceOut : Path.Combine(FairPaths.ConfigDirectory, fw.Singers.VoiceOut), mod.ModKey.FileName);
        var emotion = Enum.Parse<Emotion>(cameos.Members.First(m => m.Id == camp.Inspector).Emotion);
        var duck = mod.Globals.First(g => g.EditorID == $"{cameos.EditorIdPrefix}DuckUntil");
        var cameoQuest = hello.Quest.FormKey;
        var quest = new Quest(mod)
        {
            EditorID = camp.EditorIdPrefix,
            Name = "The fair's calendar",
            Flags = Quest.Flag.StartGameEnabled,
            Priority = 0,
            NextAliasID = 0,
        };

        // Steps (SkyrimFairCamp.Said): 0 none, 1 bring it back, 2 the story skipped and bring it
        // back, 3 every day, 4 only after the story, 5 the story's end remarked on.
        DialogResponses Line(string file, int step, bool goodbye, params ConditionFloat[] conditions)
        {
            var rec = recorded.TryGetValue(file, out var r) ? r : throw new InvalidOperationException($"camp: {file} isn't in {cuesFile}");
            var text = rec.GetProperty("text").GetString()!;
            var info = new DialogResponses(mod)
            {
                Flags = new DialogResponseFlags { Flags = goodbye ? DialogResponses.Flag.Goodbye : 0 },
                FavorLevel = FavorLevel.None,  // CNAM, as vanilla's
            };
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
            info.Conditions.Add(Him());
            info.Conditions.AddRange(conditions);

            // The begin fragment, as the Passport's lines (VMAD v5, format 2, Local).
            var entry = new ScriptEntry { Name = camp.LineScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(Obj("DuckUntil", duck.FormKey));
            entry.Properties.Add(new ScriptFloatProperty { Name = "Seconds", Data = rec.GetProperty("seconds").GetSingle() + 0.5f });
            entry.Properties.Add(Obj("Camp", quest.FormKey));
            entry.Properties.Add(new ScriptIntProperty { Name = "Step", Data = step });
            info.VirtualMachineAdapter = new DialogResponsesAdapter
            {
                Version = 5,
                ObjectFormat = 2,
                ScriptFragments = new ScriptFragments
                {
                    ExtraBindDataVersion = 2,
                    FileName = camp.LineScript,
                    OnBegin = new ScriptFragment { ExtraBindDataVersion = 1, ScriptName = camp.LineScript, FragmentName = "Fragment_0" },
                },
            };
            info.VirtualMachineAdapter.Scripts.Add(entry);

            // The voice, under the name the engine looks for (a topic of the cameos' quest with no EditorID).
            var dst = Path.Combine(voiceRoot, voice.EditorID!, $"{cameos.QuestEditorId}__{info.FormKey.ID:x8}_1.fuz".ToLowerInvariant());
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

            return info;
        }

        // Greetings, at the top of his Hello topic: the camp's while away (the story's wait first),
        // and once at the fair when the story is over.
        var waiting = new[] { GlobalIs(away, 1f), GlobalIs(storyGate, 1f), StoryOver(0f) };
        var greetings = new List<DialogResponses>
        {
            Line("camp_story_wait.wav", 0, false, waiting),
            Line("camp_hello_01.wav", 0, false, GlobalIs(away, 1f)),
            Line("camp_hello_02.wav", 0, false, GlobalIs(away, 1f)),
            Line("story_done.wav", 5, false, GlobalIs(away, 0f), GlobalIs(storyGate, 1f), StoryOver(1f), GlobalIs(storyDone, 0f)),
        };
        greetings[1].Flags!.Flags = DialogResponses.Flag.Random;
        greetings[2].Flags!.Flags = DialogResponses.Flag.Random;
        hello.Responses.InsertRange(0, greetings);

        // The player's topics, each top-level in a branch of its own (as vanilla's: Player, Top-Level).
        DialogTopic Topic(string prompt, DialogBranch? branch)
        {
            var t = new DialogTopic(mod)
            {
                Quest = new FormLinkNullable<IQuestGetter>(cameoQuest),
                Name = prompt,
                Priority = 50f,
                TopicFlags = 0,
                Category = DialogTopic.CategoryEnum.Topic,
                Subtype = DialogTopic.SubtypeEnum.Custom,
                SubtypeName = new RecordType("CUST"),  // SNAM: a blank one crashed the game at startup
            };
            if (branch is null)
            {
                branch = new DialogBranch(mod)
                {
                    EditorID = $"{camp.EditorIdPrefix}Branch{mod.DialogBranches.Count(b => (b.EditorID ?? "").StartsWith(camp.EditorIdPrefix, StringComparison.Ordinal)) + 1:00}",
                    Quest = new FormLink<IQuestGetter>(cameoQuest),
                    Category = DialogBranch.CategoryType.Player,
                    Flags = DialogBranch.Flag.TopLevel,
                    StartingTopic = new FormLinkNullable<IDialogTopicGetter>(t.FormKey),
                };
                mod.DialogBranches.Add(branch);
            }

            t.Branch = new FormLinkNullable<IDialogBranchGetter>(branch.FormKey);
            mod.DialogTopics.Add(t);
            return t;
        }

        var when = Topic(camp.AskWhen, null);
        when.Responses.Add(Line("camp_when.wav", 0, false, GlobalIs(away, 1f)));
        var skip = Topic(camp.AskSkip, null);
        skip.Responses.Add(Line("story_skip.wav", 2, true, waiting));
        // Away, and not waiting on the story: the gate off, or the story over.
        var bring = Topic(camp.AskBring, null);
        bring.Responses.Add(Line("camp_bring_back.wav", 1, true, GlobalIs(away, 1f), GlobalIs(storyGate, 0f, or: true), StoryOver(1f)));
        var ask = Topic(camp.AskSchedule, null);
        var askLine = Line("schedule_ask.wav", 0, false);
        ask.Responses.Add(askLine);
        var askBranch = mod.DialogBranches.First(b => b.FormKey == ask.Branch.FormKey);
        var always = Topic(camp.ChooseAlways, askBranch);
        always.Responses.Add(Line("schedule_always.wav", 3, false));
        var story = Topic(camp.ChooseStory, askBranch);
        story.Responses.Add(Line("schedule_story.wav", 4, false, StoryOver(0f)));
        askLine.LinkTo.Add(new FormLink<IDialogTopicGetter>(always.FormKey));
        askLine.LinkTo.Add(new FormLink<IDialogTopicGetter>(story.FormKey));

        // ---- the controller ----------------------------------------------------------------------
        var script = new ScriptEntry { Name = camp.Script };
        script.Properties.Add(Obj("Away", away.FormKey));
        script.Properties.Add(Obj("Schedule", schedule.FormKey));
        script.Properties.Add(Obj("StoryGate", storyGate.FormKey));
        script.Properties.Add(Obj("StoryDone", storyDone.FormKey));
        script.Properties.Add(Obj("Present", present.FormKey));
        script.Properties.Add(Obj("CampMarker", campMarker.FormKey));
        script.Properties.Add(Obj("ViewMarker", view.FormKey));
        script.Properties.Add(Obj("EntranceMarker", entrance.FormKey));
        script.Properties.Add(Obj("Inspector", hisRef.FormKey));
        script.Properties.Add(Obj("FairWorld", mod.Worldspaces.First(w => w.EditorID == fw.EditorId).FormKey));
        script.Properties.Add(Obj("Tamriel", tamriel.FormKey));
        script.Properties.Add(Obj("MainQuest", mainQuest));
        script.Properties.Add(Obj("FadeOut", FormKeyHelper.Parse(camp.FadeOut)));
        script.Properties.Add(Obj("FadeHold", FormKeyHelper.Parse(camp.FadeHold)));
        script.Properties.Add(Obj("FadeBack", FormKeyHelper.Parse(camp.FadeBack)));
        script.Properties.Add(new ScriptFloatProperty { Name = "SwapDistance", Data = camp.SwapDistance });
        script.Properties.Add(new ScriptFloatProperty { Name = "Poll", Data = camp.Poll });
        quest.VirtualMachineAdapter = new QuestAdapter();
        quest.VirtualMachineAdapter.Scripts.Add(script);
        mod.Quests.Add(quest);

        return $"{fair} fair references switched, {back} vanilla ones back while away, {kept} left as they were; "
            + $"{camp.Pieces.Count} camp pieces, {greetings.Count + 6} lines";
    }

    private static IEnableParentGetter? EnableParentOf(IPlaced r) => r switch
    {
        IPlacedObjectGetter o => o.EnableParent,
        IPlacedNpcGetter n => n.EnableParent,
        _ => null,
    };

    private static void SetParent(IPlaced r, FormKey marker, bool opposite)
    {
        var parent = new EnableParent
        {
            Reference = new FormLink<IPlacedGetter>(marker),
            Flags = opposite ? EnableParent.Flag.SetEnableStateToOppositeOfParent : 0,
        };
        switch (r)
        {
            case PlacedObject o:
                o.EnableParent = parent;
                break;
            case PlacedNpc n:
                n.EnableParent = parent;
                break;
            default:
                throw new InvalidOperationException($"camp: can't switch {r.FormKey} ({r.GetType().Name})");
        }
    }
}
