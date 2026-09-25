using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Assets;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The fair's sound, from <see cref="AudioConfig"/> (see docs/AUDIO.md):
/// <list type="bullet">
/// <item>the crowd ambience: looping murmur from sound markers placed round the fair's
/// busy places, each playing its own rotated copy of the loop so none line up. Placed
/// markers play by themselves while their cell is loaded and stop when it unloads, so
/// the engine keeps them from ever doubling up;</item>
/// <item>the stage set: a start-game-enabled quest whose script plays the songs from a
/// speaker marker on the stage, the cheer after each, a breath, then the next, and
/// ducks the ambience (its own sound category) under a song;</item>
/// <item>the runtime globals a later MCM can write: ambience and music on/off, volumes.</item>
/// </list>
/// The runtime files come from tools/build_audio.py; their lengths are read here, so the
/// script's timings always match the files deployed.
/// </summary>
/// <summary>
/// The added songs' FormID range: records made through <see cref="Build"/> take the range's
/// next FormIDs, and the mod's counter is put back after, so a song added to the playlist
/// renumbers nothing. The stage and the exterior share it, in build order.
/// </summary>
internal static class FairAddedSongs
{
    private static uint baseId;
    private static uint next;

    public static void Reset(uint formIdBase) => (baseId, next) = (formIdBase, formIdBase);

    public static T Build<T>(SkyrimMod mod, Func<T> make)
    {
        var saved = mod.ModHeader.Stats.NextFormID;
        if (saved >= baseId)
        {
            throw new InvalidOperationException($"FormIDs reached the added songs' range (0x{baseId:X}): raise audio.stage.addedSongsFormIdBase");
        }

        mod.ModHeader.Stats.NextFormID = next;
        var made = make();
        next = mod.ModHeader.Stats.NextFormID;
        mod.ModHeader.Stats.NextFormID = saved;
        return made;
    }
}

internal static class FairAudio
{
    private static readonly FormKey XMarker = FormKey.Factory("00003B:Skyrim.esm");
    private static readonly FormKey PlayerRef = FormKey.Factory("000014:Skyrim.esm");

    // Vanilla sound categories the fair's hang under, so the player's sliders apply.
    // Paused during menus with a fade, as the controller's clock (game time) is.
    private static readonly FormKey PausedDuringMenuFade = FormKey.Factory("09F254:Skyrim.esm");
    private static readonly FormKey AmbientCategory = FormKey.Factory("07F80B:Skyrim.esm");

    // SOMMono06000_dry: a plain mono 3D output model, copied and given the fair's distances,
    // unless a config names another (the stage's is StereoRad, both speakers at full).
    private static readonly FormKey MonoOutputModel = FormKey.Factory("10C2ED:Skyrim.esm");

    public static AudioResult Build(
        SkyrimMod mod, AudioConfig config, ISkyrimModGetter master, Worldspace world,
        Action<PlacedObject> putPersistent, Action<PlacedObject> put, Action<PlacedNpc> putNpc,
        VendorsConfig looksFrom, Func<string, FormKey> faceList, IReadOnlyList<FormKey> archers)
    {
        var p = config.EditorIdPrefix;
        var root = Path.IsPathRooted(config.SoundRoot) ? config.SoundRoot : Path.Combine(FairPaths.ConfigDirectory, config.SoundRoot);

        SoundCategory Category(string name, FormKey parent)
        {
            var c = new SoundCategory(mod)
            {
                EditorID = $"{p}{name}Category",
                // Vanilla categories all carry a name and flags (FULL, FNAM); so do these.
                Name = $"SkyrimFair{name}",
                Flags = SoundCategory.Flag.MuteWhenSubmerged,
                Parent = new FormLinkNullable<ISoundCategoryGetter>(parent),
                StaticVolumeMultiplier = 1f,
            };
            mod.SoundCategories.Add(c);
            return c;
        }

        var stageCategory = Category("Stage", PausedDuringMenuFade);
        var ambienceCategory = Category("Ambience", AmbientCategory);

        SoundOutputModel Output(string name, float min, float max, int[] curve, string model)
        {
            var sourceKey = model.Length > 0 ? FormKeyHelper.Parse(model) : MonoOutputModel;
            var source = master.SoundOutputModels.First(o => o.FormKey == sourceKey);
            var o = source.Duplicate(mod.GetNextFormKey());
            o.EditorID = $"{p}{name}Output";
            o.Attenuation!.MinDistance = (ushort)min;
            o.Attenuation.MaxDistance = (ushort)max;
            if (curve.Length > 0)
            {
                if (curve.Length != o.Attenuation.Curve.Length)
                {
                    throw new InvalidOperationException($"fairWorld.audio: an output curve has {o.Attenuation.Curve.Length} points, not {curve.Length}.");
                }

                o.Attenuation.Curve = new MemorySlice<byte>(curve.Select(v => (byte)v).ToArray());
            }

            mod.SoundOutputModels.Add(o);
            return o;
        }

        var stageOutput = Output("Stage", config.Stage.MinDistance, config.Stage.MaxDistance, config.Stage.Curve, config.Stage.OutputModel);
        var ambienceOutput = Output("Ambience", config.Ambience.MinDistance, config.Ambience.MaxDistance, Array.Empty<int>(), string.Empty);

        (SoundDescriptor Sound, float Seconds) Descriptor(string name, string file, SoundCategory category, SoundOutputModel output, bool loop, float attenuation)
        {
            var seconds = WavSeconds(Path.Combine(root, file.Replace('\\', Path.DirectorySeparatorChar)));
            var d = new SoundDescriptor(mod)
            {
                EditorID = $"{p}{name}",
                // CNAM, the descriptor's kind: vanilla's always carry it, and the engine
                // dereferences it while loading forms (without it the game crashes on boot).
                Type = SoundDescriptor.DescriptorType.Standard,
                Category = new FormLinkNullable<ISoundCategoryGetter>(category.FormKey),
                OutputModel = new FormLinkNullable<ISoundOutputModelGetter>(output.FormKey),
                LoopAndRumble = new SoundLoopAndRumble { Loop = loop ? SoundDescriptor.LoopType.Loop : SoundDescriptor.LoopType.None },
                Priority = 128,
                StaticAttenuation = attenuation,
            };
            d.SoundFiles.Add(new AssetLink<SkyrimSoundAssetType>(@"Data\Sound\" + file));
            mod.SoundDescriptors.Add(d);
            return (d, seconds);
        }

        // ---- globals ------------------------------------------------------------------
        GlobalFloat Global(string name, float value)
        {
            var g = new GlobalFloat(mod) { EditorID = $"{p}{name}", Data = value };
            mod.Globals.Add(g);
            return g;
        }

        var ambienceEnabled = Global("AmbienceEnabled", config.Globals.AmbienceEnabled);
        var ambienceVolume = Global("AmbienceVolume", config.Globals.AmbienceVolume);
        var musicEnabled = Global("MusicEnabled", config.Globals.MusicEnabled);
        var musicVolume = Global("MusicVolume", config.Globals.MusicVolume);
        var cheerVolume = Global("CheerVolume", config.Globals.CheerVolume);

        // ---- the stage set --------------------------------------------------------------
        FairAddedSongs.Reset(config.Stage.AddedSongsFormIdBase);
        var songs = config.Stage.Songs
            .Select(s => s.Added
                ? FairAddedSongs.Build(mod, () => Descriptor($"Song{s.Name}", s.File, stageCategory, stageOutput, false, config.Stage.StaticAttenuation))
                : Descriptor($"Song{s.Name}", s.File, stageCategory, stageOutput, false, config.Stage.StaticAttenuation))
            .ToList();
        var cheers = config.Stage.Cheers
            .Select(c => Descriptor($"Cheer{char.ToUpperInvariant(c.Name[0])}{c.Name[1..]}", c.File, stageCategory, stageOutput, false, config.Stage.CheerStaticAttenuation))
            .ToList();
        // Papyrus's Sound is the sound marker (SOUN), not the descriptor: a Sound property
        // pointed at an SNDR loads as None, and nothing plays. Each song and cheer gets one.
        SoundMarker Marker(string name, SoundDescriptor sound)
        {
            var s = new SoundMarker(mod)
            {
                EditorID = $"{p}{name}Marker",
                SoundDescriptor = new FormLinkNullable<ISoundDescriptorGetter>(sound.FormKey),
                ObjectBounds = new ObjectBounds { First = new P3Int16(-16, -16, -16), Second = new P3Int16(16, 16, 16) },
            };
            mod.SoundMarkers.Add(s);
            return s;
        }

        var songMarkers = config.Stage.Songs
            .Select((s, i) => s.Added ? FairAddedSongs.Build(mod, () => Marker($"Song{s.Name}", songs[i].Sound)) : Marker($"Song{s.Name}", songs[i].Sound))
            .ToList();
        var cheerMarkers = config.Stage.Cheers.Select((c, i) => Marker($"Cheer{char.ToUpperInvariant(c.Name[0])}{c.Name[1..]}", cheers[i].Sound)).ToList();

        var songCheers = config.Stage.Songs.Select(s =>
        {
            if (s.Cheer.Length == 0) return -1;
            var i = config.Stage.Cheers.FindIndex(c => c.Name == s.Cheer);
            return i >= 0 ? i : throw new InvalidOperationException($"fairWorld.audio song {s.Name}: no cheer '{s.Cheer}'.");
        }).ToList();

        var sp = config.Stage.Speaker;
        var speaker = new PlacedObject(mod)
        {
            EditorID = $"{p}StageSpeaker",
            Base = new FormLinkNullable<IPlaceableObjectGetter>(XMarker),
            Placement = new Placement { Position = new P3Float(sp[0], sp[1], sp[2]), Rotation = new P3Float(0f, 0f, MathF.PI) },
        };
        putPersistent(speaker);

        var quest = new Quest(mod)
        {
            EditorID = $"{p}Quest",
            Name = "Wanderer's Fair Stage",
            Flags = Quest.Flag.StartGameEnabled,
            Priority = 10,
            // ANAM, as vanilla quests with aliases carry it.
            NextAliasID = 1,
            QuestFormVersion = 0,
        };
        quest.Aliases.Add(new QuestAlias
        {
            ID = 0,
            Name = "Player",
            Type = QuestAlias.TypeEnum.Reference,
            ForcedReference = new FormLinkNullable<IPlacedGetter>(PlayerRef),
            // FNAM and VTCK, written as vanilla's own forced-player aliases have them.
            Flags = (QuestAlias.Flag)0,
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
        });


        // ---- the band ------------------------------------------------------------------------
        // Bards playing on the stage, as vanilla's inn bards do: a package holds each one on
        // their spot, and the stage script plays the instrument idle on them when a song
        // starts (PlayIdle(IdleLuteStart), as the BardSongs scenes do) and IdleStop when it
        // ends. They're persistent references, so the script's properties can name them.
        var bandPlaced = new List<FormKey>();
        var bandIdles = new List<FormKey>();
        foreach (var member in config.Stage.Band)
        {
            // Two FormIDs the band's first build used (an idle marker and a package copy),
            // kept unused so the records after it keep theirs in existing saves.
            mod.GetNextFormKey();
            mod.GetNextFormKey();

            var placed = Bard(mod, config, member, looksFrom, faceList);
            putNpc(placed);
            bandPlaced.Add(placed.FormKey);
            bandIdles.Add(FormKeyHelper.Parse(member.Idle));
        }

        // ---- the songs' sections: drums and crowd, from each song's start ------------------
        // The drums' and the crowd's timelines, merged: a section at every second either
        // changes, carrying the other's last value (drums play and the crowd dances before
        // their first entry).
        List<(float At, int Value)> Timeline(StageSong song, string what, List<System.Text.Json.JsonElement[]> entries, string[] words)
        {
            var list = new List<(float, int)>();
            var last = -1f;
            foreach (var entry in entries)
            {
                var at = entry[0].GetSingle();
                var value = Array.IndexOf(words, entry[1].GetString());
                if (at <= last)
                {
                    throw new InvalidOperationException($"songs {song.Name}: {what} must be in order ({at} after {last})");
                }

                if (value < 0)
                {
                    throw new InvalidOperationException($"songs {song.Name}: {what} at {at} must be {string.Join(", ", words)}");
                }

                list.Add((at, value));
                last = at;
            }

            return list;
        }

        // The instruments with timelines, in the order the script's InstrumentIdles has them.
        var instruments = new[] { "lute", "drum", "flute" };
        IEnumerable<(float Start, int[] Play, int Sing, int Crowd)> SongSections(StageSong song)
        {
            // 0 rest (holding it, still), 1 normal, 2 fast, 3 away (put away).
            var levels = new[] { "rest", "normal", "fast", "away" };
            var play = new[] { song.Lute, song.Drum, song.Flute }
                .Select((entries, k) => Timeline(song, instruments[k], entries, levels)).ToArray();
            var sing = Timeline(song, "singers", song.Singers, new[] { "rest", "sing" });
            var crowd = Timeline(song, "crowd", song.Crowd, new[] { "dance", "clap", "cheer" });
            var now = instruments.Select(_ => 1).ToArray();
            var (singing, mode) = (1, 0);
            var times = play.SelectMany(t => t.Select(x => x.At)).Concat(sing.Select(x => x.At)).Concat(crowd.Select(x => x.At));
            foreach (var at in times.Distinct().OrderBy(x => x))
            {
                for (var k = 0; k < play.Length; k++)
                {
                    foreach (var x in play[k].Where(x => x.At == at)) now[k] = x.Value;
                }

                foreach (var x in sing.Where(x => x.At == at)) singing = x.Value;
                foreach (var x in crowd.Where(x => x.At == at)) mode = x.Value;
                yield return (at, now.ToArray(), singing, mode);
            }
        }

        List<(float Start, int[] Play, int Sing, int Crowd)> Sections() => config.Stage.Songs.SelectMany(SongSections).ToList();
        List<(int First, int Count)> SectionIndex()
        {
            var index = new List<(int, int)>();
            var at = 0;
            foreach (var song in config.Stage.Songs)
            {
                var n = SongSections(song).Count();
                index.Add((n == 0 ? -1 : at, n));
                at += n;
            }

            return index;
        }

        // The singers' moves: idles and their clip lengths, checked to pair up.
        (ExtendedList<ScriptObjectProperty> Idles, ExtendedList<float> Lengths) Moves(CrowdMove move)
        {
            if (move.Idles.Count != move.Lengths.Count)
            {
                throw new InvalidOperationException($"songs.config.json singerMoves: {move.Idles.Count} idles but {move.Lengths.Count} lengths");
            }

            return (move.Idles.Select(i => new ScriptObjectProperty { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(FormKeyHelper.Parse(i)) }).ToExtendedList(),
                move.Lengths.ToExtendedList());
        }

        // ---- the stage script ------------------------------------------------------------
        ScriptObjectProperty Obj(string name, FormKey key) => new() { Name = name, Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        ScriptFloatProperty Float(string name, float value) => new() { Name = name, Data = value };
        var script = new ScriptEntry { Name = "SkyrimFairAudioScript" };
        script.Properties.AddRange(new ScriptProperty[]
        {
            Obj("FairWorld", world.FormKey),
            Obj("StageSpeaker", speaker.FormKey),
            new ScriptObjectListProperty { Name = "Songs", Objects = songMarkers.Select(s => Obj("", s.FormKey)).ToExtendedList() },
            new ScriptFloatListProperty { Name = "SongLengths", Data = songs.Select(s => s.Seconds).ToExtendedList() },
            new ScriptIntListProperty { Name = "SongCheers", Data = songCheers.ToExtendedList() },
            new ScriptObjectListProperty { Name = "Cheers", Objects = cheerMarkers.Select(c => Obj("", c.FormKey)).ToExtendedList() },
            new ScriptFloatListProperty { Name = "CheerLengths", Data = cheers.Select(c => c.Seconds).ToExtendedList() },
            Float("FirstSongDelay", config.Stage.FirstSongDelay),
            Float("PauseAfterCheer", config.Stage.PauseAfterCheer),
            Float("CheerLead", config.Stage.CheerLead),
            new ScriptObjectListProperty { Name = "SingerMoves", Objects = Moves(config.Stage.SingerSing).Idles },
            new ScriptFloatListProperty { Name = "SingerMoveLengths", Data = Moves(config.Stage.SingerSing).Lengths },
            new ScriptObjectListProperty { Name = "SingerRestMoves", Objects = Moves(config.Stage.SingerRest).Idles },
            new ScriptFloatListProperty { Name = "SingerRestLengths", Data = Moves(config.Stage.SingerRest).Lengths },
            Float("SingerGap", config.Stage.SingerGap),
            new ScriptObjectListProperty { Name = "SingerCheerMoves", Objects = Moves(config.Stage.SingerCheer).Idles },
            new ScriptFloatListProperty { Name = "SingerCheerLengths", Data = Moves(config.Stage.SingerCheer).Lengths },
            new ScriptFloatListProperty { Name = "SectionStarts", Data = Sections().Select(x => x.Start).ToExtendedList() },
            new ScriptIntListProperty { Name = "SectionPlay", Data = Sections().SelectMany(x => x.Play).ToExtendedList() },
            new ScriptIntListProperty { Name = "SectionSing", Data = Sections().Select(x => x.Sing).ToExtendedList() },
            new ScriptObjectListProperty
            {
                Name = "InstrumentIdles",
                Objects = instruments.Select(i => Obj("", FormKeyHelper.Parse(config.Stage.Instruments.TryGetValue(i, out var idle)
                    ? idle
                    : throw new InvalidOperationException($"songs.config.json: instruments has no \"{i}\"")))).ToExtendedList(),
            },
            new ScriptIntListProperty { Name = "SectionCrowd", Data = Sections().Select(x => x.Crowd).ToExtendedList() },
            new ScriptIntListProperty { Name = "SongFirstSection", Data = SectionIndex().Select(x => x.First).ToExtendedList() },
            new ScriptIntListProperty { Name = "SongSectionCount", Data = SectionIndex().Select(x => x.Count).ToExtendedList() },
            Float("DuckDuringSong", config.Stage.DuckAmbience),
            Obj("AmbienceCategory", ambienceCategory.FormKey),
            Obj("AmbienceEnabled", ambienceEnabled.FormKey),
            Obj("AmbienceVolume", ambienceVolume.FormKey),
            Obj("MusicEnabled", musicEnabled.FormKey),
            Obj("MusicVolume", musicVolume.FormKey),
            Obj("CheerVolume", cheerVolume.FormKey),
            Obj("TimeScale", FormKeyHelper.Parse(config.TimeScaleGlobal)),
            new ScriptObjectListProperty { Name = "Band", Objects = bandPlaced.Select(b => Obj("", b)).ToExtendedList() },
            new ScriptObjectListProperty { Name = "BandIdles", Objects = bandIdles.Select(i => Obj("", i)).ToExtendedList() },
            Obj("BandStop", FormKeyHelper.Parse(config.Stage.BandStop)),
            new ScriptObjectListProperty { Name = "Archers", Objects = archers.Select(a => Obj("", a)).ToExtendedList() },
        });
        if (config.Stage.SingerEnd.Idles.Count > 0)
        {
            script.Properties.Add(Obj("SingerEndMove", FormKeyHelper.Parse(config.Stage.SingerEnd.Idles[0])));
        }

        var adapter = new QuestAdapter();
        adapter.Scripts.Add(script);
        var alias = new QuestFragmentAlias { Property = new ScriptObjectProperty { Object = new FormLink<ISkyrimMajorRecordGetter>(quest.FormKey), Alias = 0 } };
        alias.Scripts.Add(new ScriptEntry { Name = "SkyrimFairAudioPlayerAlias" });
        adapter.Aliases.Add(alias);
        quest.VirtualMachineAdapter = adapter;
        mod.Quests.Add(quest);

        // ---- the crowd ambience -----------------------------------------------------------
        var markers = new Dictionary<(string Loop, int Copy, float Extra), SoundMarker>();
        var emitters = new List<(string Name, float X, float Y)>();
        var loopSeconds = 0f;
        foreach (var e in config.Ambience.Emitters)
        {
            var loop = config.Ambience.Loops.FirstOrDefault(l => l.Name == e.Loop)
                ?? throw new InvalidOperationException($"fairWorld.audio.ambience emitter {e.Name}: no loop '{e.Loop}'.");
            if (e.Copy < 0 || e.Copy >= loop.Files.Count)
            {
                throw new InvalidOperationException($"fairWorld.audio.ambience emitter {e.Name}: loop '{e.Loop}' has no copy {e.Copy}.");
            }

            var key = (e.Loop, e.Copy, e.ExtraAttenuation);
            if (!markers.TryGetValue(key, out var marker))
            {
                var name = $"{e.Loop}{e.Copy + 1:00}{(e.ExtraAttenuation > 0 ? $"Quiet{e.ExtraAttenuation:0}" : "")}";
                var (sound, seconds) = Descriptor($"Ambience{name}", loop.Files[e.Copy], ambienceCategory, ambienceOutput, true,
                    config.Ambience.StaticAttenuation + e.ExtraAttenuation);
                loopSeconds = seconds;
                marker = new SoundMarker(mod)
                {
                    EditorID = $"{p}Ambience{name}Marker",
                    SoundDescriptor = new FormLinkNullable<ISoundDescriptorGetter>(sound.FormKey),
                    ObjectBounds = new ObjectBounds { First = new P3Int16(-16, -16, -16), Second = new P3Int16(16, 16, 16) },
                };
                mod.SoundMarkers.Add(marker);
                markers[key] = marker;
            }

            put(new PlacedObject(mod)
            {
                EditorID = $"{p}Ambience{e.Name}",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(marker.FormKey),
                Placement = new Placement { Position = new P3Float(e.At[0], e.At[1], e.At[2]), Rotation = new P3Float(0f, 0f, 0f) },
            });
            emitters.Add((e.Name, e.At[0], e.At[1]));
        }

        // The game's own music stays out: the tavern "silence" music type bards use.
        if (config.WorldMusic.Length > 0)
        {
            world.Music = new FormLinkNullable<IMusicTypeGetter>(FormKeyHelper.Parse(config.WorldMusic));
        }

        return new AudioResult(
            quest.FormKey,
            config.Stage.Songs.Select((s, i) => (s.Name, songs[i].Seconds)).ToList(),
            cheers.Select(c => c.Seconds).ToList(),
            emitters,
            markers.Count,
            loopSeconds,
            config.Stage.Band.Select(b => b.Name).ToList());
    }

    /// <summary>
    /// One bard: a visitor-kind NPC (the fair's face lists, Traits template) in bard's
    /// clothes, held on their spot by <see cref="StageAudioConfig.BandPackage"/>, placed
    /// where the member says. The caller puts the reference (persistent).
    /// </summary>
    private static PlacedNpc Bard(SkyrimMod mod, AudioConfig config, BandMember member, VendorsConfig looksFrom, Func<string, FormKey> faceList)
    {
        var p = config.EditorIdPrefix;
        var at = member.At;
        var look = looksFrom.Looks.First(l => l.Name == member.Look);
        var npc = new Npc(mod)
        {
            EditorID = $"{p}Band{member.Name}",
            Name = member.Title,
            Race = new FormLink<IRaceGetter>(FormKeyHelper.Parse(looksFrom.Race)),
            Template = new FormLinkNullable<INpcSpawnGetter>(faceList(look.Template)),
            Class = new FormLink<IClassGetter>(FormKeyHelper.Parse(looksFrom.Class)),
            DefaultOutfit = new FormLinkNullable<IOutfitGetter>(FormKeyHelper.Parse(member.Outfit)),
            Configuration = new NpcConfiguration
            {
                Flags = NpcConfiguration.Flag.AutoCalcStats | NpcConfiguration.Flag.Protected
                    | (look.Female ? NpcConfiguration.Flag.Female : 0),
                TemplateFlags = NpcConfiguration.TemplateFlag.Traits,
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
            Height = 1f,
            Weight = 50f,
        };
        npc.Packages.Add(new FormLink<IPackageGetter>(FormKeyHelper.Parse(config.Stage.BandPackage)));
        mod.Npcs.Add(npc);

        return new PlacedNpc(mod)
        {
            EditorID = $"{p}Band{member.Name}Ref",
            Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
            Placement = new Placement { Position = new P3Float(at[0], at[1], at[2] + 2f), Rotation = new P3Float(0f, 0f, at[3] * MathF.PI / 180f) },
        };
    }

    /// <summary>
    /// The rest of the orchestra (<see cref="StageAudioConfig.Orchestra"/>), built after
    /// every other record so no FormID before it moves, and given to the stage script as
    /// new properties (<c>Orchestra</c>, <c>OrchestraIdles</c>), which an existing save
    /// picks up (a property already in a save keeps its saved value; a new one is filled).
    /// </summary>
    public static int BuildOrchestra(SkyrimMod mod, AudioConfig config, VendorsConfig looksFrom, Func<string, FormKey> faceList,
        Action<PlacedNpc> putNpc, FormKey quest)
    {
        var placed = new List<FormKey>();
        var idles = new List<FormKey>();
        foreach (var member in config.Stage.Orchestra)
        {
            var bard = Bard(mod, config, member, looksFrom, faceList);
            putNpc(bard);
            placed.Add(bard.FormKey);
            idles.Add(FormKeyHelper.Parse(member.Idle));
        }

        var script = mod.Quests.First(q => q.FormKey == quest).VirtualMachineAdapter!.Scripts[0];
        ScriptObjectProperty Obj(FormKey key) => new() { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        script.Properties.Add(new ScriptObjectListProperty { Name = "Orchestra", Objects = placed.Select(Obj).ToExtendedList() });
        script.Properties.Add(new ScriptObjectListProperty { Name = "OrchestraIdles", Objects = idles.Select(Obj).ToExtendedList() });
        return placed.Count;
    }

    /// <summary>Length of a PCM WAV from its header: data bytes over bytes a second.</summary>
    internal static float WavSeconds(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"No {path}: build the sound files first (python tools/build_audio.py).");
        }

        using var r = new BinaryReader(File.OpenRead(path));
        if (new string(r.ReadChars(4)) != "RIFF") throw new InvalidOperationException($"{path} is not a WAV.");
        r.ReadInt32();
        if (new string(r.ReadChars(4)) != "WAVE") throw new InvalidOperationException($"{path} is not a WAV.");
        var byteRate = 0;
        while (r.BaseStream.Position < r.BaseStream.Length)
        {
            var id = new string(r.ReadChars(4));
            var size = r.ReadInt32();
            if (id == "fmt ")
            {
                var format = r.ReadInt16();
                var channels = r.ReadInt16();
                var rate = r.ReadInt32();
                byteRate = r.ReadInt32();
                if (format != 1 || channels != 1)
                {
                    throw new InvalidOperationException($"{path}: format {format}, {channels} channels; positional sound must be mono PCM.");
                }

                r.BaseStream.Seek(size - 12, SeekOrigin.Current);
            }
            else if (id == "data")
            {
                return size / (float)byteRate;
            }
            else
            {
                r.BaseStream.Seek(size + (size & 1), SeekOrigin.Current);
            }
        }

        throw new InvalidOperationException($"{path} has no data chunk.");
    }
}

internal sealed record AudioResult(
    FormKey Quest,
    IReadOnlyList<(string Name, float Seconds)> Songs,
    IReadOnlyList<float> Cheers,
    IReadOnlyList<(string Name, float X, float Y)> Emitters,
    int AmbienceMarkers,
    float LoopSeconds,
    IReadOnlyList<string> Band);
