using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The fair's face lists, filled from a pool of vanilla faces (<see cref="FacePoolConfig"/>).
///
/// Visitors, stall-keepers and archers take their looks (Traits) from the fair's own copy
/// of a vanilla leveled list. The copies used to hold that list's entries: the commoner
/// lists it names are LCharBanditMeleeCommonerM/F, six Imperial bandits a sex, warpaint
/// and all. Now each list is refilled with the generic (not unique) vanilla NPCs of every
/// playable race that have their own face: no warpaint, little dirt, no scars, and their
/// FaceGen head and tint in the game's archives, so none renders dark. Races are weighted
/// by repeating entries. The list records stay where they were, so no FormID moves.
/// </summary>
internal static class FairFaces
{
    public static (List<FormKey> Entries, IReadOnlyDictionary<string, int> PerRace, int Distinct, IReadOnlyDictionary<string, int> Dropped) Pool(
        ISkyrimModGetter master, FacePoolConfig cfg, bool female, IReadOnlyList<string> archiveDirs)
    {
        var (byRace, dropped) = Candidates(master, cfg, female, archiveDirs);
        return Share(cfg, byRace, dropped);
    }

    /// <summary>
    /// A fixed face for each NPC record whose EditorID starts with a <see cref="FixedFace"/>
    /// prefix, in place of the list: the band and the folk pair, whose animations want a
    /// human skeleton and (the pair) matching heights. Faces are dealt out in turn from the
    /// allowed races' candidates, spread through them, so each record gets its own.
    /// </summary>
    public static int Fixed(ISkyrimModGetter master, FacePoolConfig cfg, IEnumerable<Npc> npcs)
    {
        var height = master.Npcs.ToDictionary(n => n.FormKey, n => n.Height);
        var changed = 0;
        foreach (var rule in cfg.Fixed)
        {
            var records = npcs.Where(n => (n.EditorID ?? "").StartsWith(rule.Prefix, StringComparison.Ordinal)).OrderBy(n => n.FormKey.ID).ToList();
            foreach (var female in new[] { false, true })
            {
                var mine = records.Where(n => IsFemale(n) == female).ToList();
                if (mine.Count == 0)
                {
                    continue;
                }

                var (byRace, _) = Candidates(master, cfg, female, cfg.Archives);
                var faces = byRace.Where(kv => rule.Races.Contains(kv.Key)).SelectMany(kv => kv.Value)
                    .Where(k => MathF.Abs(height[k] - 1f) <= rule.HeightTolerance)
                    .OrderBy(k => k.ID).ToList();
                if (faces.Count == 0)
                {
                    throw new InvalidOperationException($"fairWorld.faces.fixed {rule.Prefix}: no {(female ? "female" : "male")} face in {string.Join("/", rule.Races)}");
                }

                for (var i = 0; i < mine.Count; i++)
                {
                    mine[i].Template = new FormLinkNullable<INpcSpawnGetter>(faces[(int)((long)(i * 2 + 1) * faces.Count / (mine.Count * 2))]);
                    changed++;
                }
            }
        }

        return changed;

        // The record's own sex decides which faces it gets (its female flag, as the lists' do).
        static bool IsFemale(Npc n) => n.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
    }

    private static (SortedDictionary<string, List<FormKey>> ByRace, SortedDictionary<string, int> Dropped) Candidates(
        ISkyrimModGetter master, FacePoolConfig cfg, bool female, IReadOnlyList<string> archiveDirs)
    {
        var faceFiles = FaceGenFiles(master, archiveDirs);
        var link = new[] { master }.ToImmutableLinkCache();
        var races = master.Races.Where(r => r.EditorID is { } id && cfg.RaceWeights.ContainsKey(id)).ToDictionary(r => r.FormKey);
        var scars = master.HeadParts.Where(h => h.Type == HeadPart.TypeEnum.Scars).Select(h => h.FormKey).ToHashSet();

        // Each race's tint masks by index, to tell warpaint and dirt from make-up.
        var masks = races.Values.ToDictionary(r => r.FormKey, r =>
            ((female ? r.HeadData?.Female : r.HeadData?.Male)?.TintMasks ?? new List<ITintAssetsGetter>())
                .Where(t => t.Index is not null)
                .GroupBy(t => (int)t.Index!.Value)
                .ToDictionary(g => g.Key, g => g.First().MaskType));

        var byRace = new SortedDictionary<string, List<FormKey>>(StringComparer.Ordinal);
        var dropped = new SortedDictionary<string, int>(StringComparer.Ordinal);
        void Drop(string why) => dropped[why] = dropped.GetValueOrDefault(why) + 1;
        foreach (var n in master.Npcs.OrderBy(n => n.FormKey.ID))
        {
            var flags = n.Configuration.Flags;
            if (!races.TryGetValue(n.Race.FormKey, out var race)
                || flags.HasFlag(NpcConfiguration.Flag.Female) != female
                || (!n.Template.IsNull && n.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits)))
            {
                continue;
            }

            if (flags.HasFlag(NpcConfiguration.Flag.Unique)) { Drop("unique"); continue; }
            if (cfg.ExcludePrefixes.Any(p => (n.EditorID ?? "").StartsWith(p, StringComparison.Ordinal))) { Drop("prefix"); continue; }
            if (cfg.ExcludeNames.Any(x => (n.Name?.String ?? "").Contains(x, StringComparison.OrdinalIgnoreCase)
                || (n.EditorID ?? "").Contains(x, StringComparison.OrdinalIgnoreCase))) { Drop("name"); continue; }

            var paint = 0f;
            var dirt = 0f;
            foreach (var t in n.TintLayers)
            {
                if (t.Index is ushort ix && masks[race.FormKey].TryGetValue(ix, out var type) && t.InterpolationValue is float v)
                {
                    if (type == TintAssets.TintMaskType.Paint) paint = MathF.Max(paint, v);
                    if (type == TintAssets.TintMaskType.Dirt) dirt = MathF.Max(dirt, v);
                }
            }

            if (paint > cfg.MaxPaint) { Drop("warpaint"); continue; }
            if (dirt > cfg.MaxDirt) { Drop("dirt"); continue; }
            if (!cfg.AllowScars && n.HeadParts.Any(h => scars.Contains(h.FormKey))) { Drop("scars"); continue; }

            var id = n.FormKey.ID.ToString("x8");
            if (!faceFiles.Contains($@"meshes\actors\character\facegendata\facegeom\skyrim.esm\{id}.nif")) { Drop("no FaceGen head"); continue; }
            if (!faceFiles.Contains($@"textures\actors\character\facegendata\facetint\skyrim.esm\{id}.dds")) { Drop("no FaceGen tint"); continue; }

            if (!byRace.TryGetValue(race.EditorID!, out var list))
            {
                byRace[race.EditorID!] = list = new List<FormKey>();
            }

            list.Add(n.FormKey);
        }

        return (byRace, dropped);
    }

    private static (List<FormKey> Entries, IReadOnlyDictionary<string, int> PerRace, int Distinct, IReadOnlyDictionary<string, int> Dropped) Share(
        FacePoolConfig cfg, SortedDictionary<string, List<FormKey>> byRace, SortedDictionary<string, int> dropped)
    {

        // Share the entries out by weight among the races that have faces, then fill each
        // race's share by walking its faces in turn (a race with fewer faces repeats them).
        var present = cfg.RaceWeights.Where(kv => byRace.ContainsKey(kv.Key)).OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        var total = present.Sum(kv => kv.Value);
        var entries = new List<FormKey>();
        var perRace = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var distinct = 0;
        foreach (var (raceId, weight) in present)
        {
            var faces = byRace[raceId];
            var share = Math.Min(faces.Count * cfg.MaxRepeat, Math.Max(1, (int)MathF.Round(cfg.Entries * weight / total)));
            for (var k = 0; k < share; k++)
            {
                // Spread through the race's faces (they come in families by FormID), and
                // round again only once every face is in.
                entries.Add(share <= faces.Count ? faces[(int)((long)k * faces.Count / share)] : faces[k % faces.Count]);
            }

            perRace[$"{raceId} ({faces.Count} faces)"] = share;
            distinct += Math.Min(share, faces.Count);
        }

        if (entries.Count > 255)
        {
            throw new InvalidOperationException($"fairWorld.faces: {entries.Count} entries, more than a leveled list holds (255); lower entries");
        }

        return (entries, perRace, distinct, dropped);
    }

    private static HashSet<string>? cachedFiles;

    /// <summary>Every FaceGen path under Skyrim.esm in the archives, lower case.</summary>
    private static HashSet<string> FaceGenFiles(ISkyrimModGetter master, IReadOnlyList<string> archiveDirs)
    {
        if (cachedFiles is not null)
        {
            return cachedFiles;
        }

        cachedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bsa in archiveDirs
            .Select(d => Path.IsPathRooted(d) ? d : Path.Combine(FairPaths.ConfigDirectory, d))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.bsa").OrderBy(f => f, StringComparer.OrdinalIgnoreCase)))
        {
            foreach (var f in Archive.CreateReader(GameRelease.SkyrimSE, bsa).Files)
            {
                var path = f.Path.Replace('/', '\\');
                if (path.Contains(@"facegendata\", StringComparison.OrdinalIgnoreCase))
                {
                    cachedFiles.Add(path.ToLowerInvariant());
                }
            }
        }

        return cachedFiles;
    }
}
