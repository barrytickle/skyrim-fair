using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The crowd switched off where it can't be seen (<see cref="CrowdCullingConfig"/>).
///
/// Every placed object is laid into a voxel grid by its navmesh footprint (16-unit cells,
/// 16-unit height bands, tools/make_footprints.py), turned and scaled as the navmesh cuts
/// it. From each standing spot of a grid over the fair, rays run from the eye heights to
/// every switchable actor at the target heights; an actor is seen if any ray is clear. A
/// spot's table entry is every actor seen from any standing spot within the margin, so an
/// actor is on before it can come into view.
///
/// The switchable actors are made persistent (a script names them) and lose their crowd
/// layer's enable parent (a reference with one can't be enabled by script): the script
/// applies the layers itself. FormIDs don't move; only script properties and one global
/// are added.
/// </summary>
internal static class FairVisibility
{
    private const int BitsPerWord = 31;  // Papyrus has no bitwise operators: bits are read by division, so words stay positive
    private const float StartClear = 32f;   // the eye's own surroundings
    private const float EndClear = 40f;     // the actor's own seat or counter
    private const float Step = 12f;

    public static CullingResult Build(
        SkyrimMod mod, FairWorldConfig world, Worldspace worldspace,
        IReadOnlyDictionary<(int X, int Y), Cell> cells, Cell persistentCell, int persistentFlag,
        (float MinX, float MinY, float MaxX, float MaxY) area, Func<float, float, float> outside,
        ISkyrimModGetter master, Quest stageQuest)
    {
        var cfg = world.CrowdCulling;
        var nav = world.Navmesh;

        // ---- the switchable actors, in FormID order ---------------------------------------
        var layerScript = stageQuest.VirtualMachineAdapter!.Scripts[0];
        var layerMarkers = (layerScript.Properties.FirstOrDefault(p => p.Name == "CrowdLayers") as ScriptObjectListProperty)?.Objects
            .Select(o => o.Object.FormKey).ToList() ?? new List<FormKey>();
        var npcs = mod.Npcs.ToDictionary(n => n.FormKey);
        bool Switchable(PlacedNpc n) =>
            (n.MajorRecordFlagsRaw & 0x800) == 0
            && npcs.TryGetValue(n.Base.FormKey, out var npc)
            && !cfg.AlwaysOn.Any(prefix => (npc.EditorID ?? "").StartsWith(prefix, StringComparison.Ordinal));
        var actors = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedNpc>())
            .Concat(persistentCell.Persistent.OfType<PlacedNpc>())
            .Where(Switchable)
            .OrderBy(n => n.FormKey.ID)
            .ToList();
        var layers = actors.Select(n => n.EnableParent is { } ep ? layerMarkers.IndexOf(ep.Reference.FormKey) : -1).ToList();
        if (actors.Where((n, i) => n.EnableParent is not null && layers[i] < 0).FirstOrDefault() is { } odd)
        {
            throw new InvalidOperationException($"crowdCulling: actor {odd.FormKey} has an enable parent that isn't a crowd layer marker");
        }

        // ---- the voxel grid of everything placed ----------------------------------------------
        var footprints = FairNavmesh.LoadFootprints(nav.Footprints);
        var extras = nav.ExtraMasters
            .Select(p => Path.IsPathRooted(p) ? p : Path.Combine(FairPaths.ConfigDirectory, p))
            .Where(File.Exists)
            .Select(p => (ISkyrimModGetter)SkyrimMod.CreateFromBinaryOverlay(p, SkyrimRelease.SkyrimSE))
            .ToList();
        var link = new[] { master }.Concat(extras).Append(mod).ToImmutableLinkCache();

        var cell = footprints.CellSize;
        var (x0, y0) = (area.MinX - cfg.Margin, area.MinY - cfg.Margin);
        var (nx, ny) = ((int)MathF.Ceiling((area.MaxX - area.MinX + 2f * cfg.Margin) / cell), (int)MathF.Ceiling((area.MaxY - area.MinY + 2f * cfg.Margin) / cell));
        var solid = new uint[nx, ny];
        int Band(float z) => Math.Clamp((int)MathF.Floor((z - footprints.BandBase) / footprints.BandSize), 0, 31);

        // What the navmesh ignores (plants, cloth, smoke) doesn't hide anyone either.
        var seeThrough = nav.IgnoreBases.Select(FormKeyHelper.Parse).ToHashSet();
        var objects = cells.Values.SelectMany(c => c.Temporary).Concat(persistentCell.Persistent).OfType<PlacedObject>()
            .Where(o => o.Primitive is null && (o.MajorRecordFlagsRaw & 0x800) == 0 && !seeThrough.Contains(o.Base.FormKey))
            .OrderBy(o => o.FormKey.ID);
        var painted = 0;
        foreach (var o in objects)
        {
            if (!link.TryResolve(o.Base.FormKey, typeof(ISkyrimMajorRecordGetter), out var baseRecord)
                || FairNavmesh.ModelKey((baseRecord as IModeledGetter)?.Model?.File.DataRelativePath.ToString()) is not { } model
                || !footprints.Cells.TryGetValue(model, out var fp))
            {
                continue;
            }

            painted++;
            var p = o.Placement!;
            var scale = o.Scale ?? 1f;
            var (c, s) = (MathF.Cos(p.Rotation.Z), MathF.Sin(p.Rotation.Z));
            var sub = Math.Max(1, (int)MathF.Ceiling(scale / 0.7f));
            foreach (var (fx, fy, bands) in fp)
            {
                // The local bands, scaled and lifted into world bands.
                var world_ = 0u;
                for (var b = 0; b < 32; b++)
                {
                    if ((bands & (1u << b)) == 0)
                    {
                        continue;
                    }

                    var lo = Band(p.Position.Z + scale * (footprints.BandBase + footprints.BandSize * b));
                    var hi = Band(p.Position.Z + scale * (footprints.BandBase + footprints.BandSize * (b + 1)) - 0.01f);
                    for (var wb = lo; wb <= hi; wb++)
                    {
                        world_ |= 1u << wb;
                    }
                }

                for (var a = 0; a < sub; a++)
                {
                    for (var bb = 0; bb < sub; bb++)
                    {
                        var (lu, lv) = ((fx + (a + 0.5f) / sub) * cell * scale, (fy + (bb + 0.5f) / sub) * cell * scale);
                        var (wx, wy) = (p.Position.X + lu * c + lv * s, p.Position.Y - lu * s + lv * c);
                        var (gi, gj) = ((int)MathF.Floor((wx - x0) / cell), (int)MathF.Floor((wy - y0) / cell));
                        if (gi >= 0 && gi < nx && gj >= 0 && gj < ny)
                        {
                            solid[gi, gj] |= world_;
                        }
                    }
                }
            }
        }

        bool Blocked(float x, float y, float z)
        {
            var (gi, gj) = ((int)MathF.Floor((x - x0) / cell), (int)MathF.Floor((y - y0) / cell));
            return gi >= 0 && gi < nx && gj >= 0 && gj < ny && (solid[gi, gj] & (1u << Band(z))) != 0;
        }

        // ---- the spots, and what each standing one sees -----------------------------------------
        var spot = cfg.Spot;
        var (ox, oy) = (MathF.Floor(area.MinX / spot) * spot, MathF.Floor(area.MinY / spot) * spot);
        var columns = (int)MathF.Ceiling((area.MaxX - ox) / spot);
        var rows = (int)MathF.Ceiling((area.MaxY - oy) / spot);
        var words = (actors.Count + BitsPerWord - 1) / BitsPerWord;
        var ax = actors.Select(n => n.Placement!.Position.X).ToArray();
        var ay = actors.Select(n => n.Placement!.Position.Y).ToArray();
        var az = actors.Select(n => n.Placement!.Position.Z).ToArray();

        var standing = new List<(float X, float Y, bool[] Seen)>();
        for (var j = 0; j < rows; j++)
        {
            for (var i = 0; i < columns; i++)
            {
                var (x, y) = (ox + (i + 0.5f) * spot, oy + (j + 0.5f) * spot);
                if (outside(x, y) > -64f || Blocked(x, y, 10f) || Blocked(x, y, 80f))
                {
                    continue;
                }

                standing.Add((x, y, new bool[actors.Count]));
            }
        }

        Parallel.For(0, standing.Count, k =>
        {
            var (ex, ey, seen) = standing[k];
            for (var t = 0; t < actors.Count; t++)
            {
                var (dx, dy) = (ax[t] - ex, ay[t] - ey);
                var length = MathF.Sqrt(dx * dx + dy * dy);
                foreach (var eye in cfg.EyeHeights)
                {
                    foreach (var target in cfg.TargetHeights)
                    {
                        var clear = true;
                        for (var d = StartClear; d < length - EndClear && clear; d += Step)
                        {
                            var f = d / length;
                            clear = !Blocked(ex + dx * f, ey + dy * f, eye + (az[t] + target - eye) * f);
                        }

                        if (clear)
                        {
                            seen[t] = true;
                            goto next;
                        }
                    }
                }

            next:;
            }
        });

        // ---- the table: every actor seen from within the margin, 31 to a word ----------------------
        var table = new int[columns * rows * words];
        var active = new int[columns * rows];
        var margin2 = cfg.Margin * cfg.Margin;
        for (var j = 0; j < rows; j++)
        {
            for (var i = 0; i < columns; i++)
            {
                var (x, y) = (ox + (i + 0.5f) * spot, oy + (j + 0.5f) * spot);
                var on = new bool[actors.Count];
                var any = false;
                foreach (var (sx, sy, seen) in standing)
                {
                    if ((sx - x) * (sx - x) + (sy - y) * (sy - y) > margin2)
                    {
                        continue;
                    }

                    any = true;
                    for (var t = 0; t < actors.Count; t++)
                    {
                        on[t] |= seen[t];
                    }
                }

                // Nowhere to stand near it (outside the wall, deep in a stall): everyone on.
                for (var t = 0; t < actors.Count; t++)
                {
                    if (!any || on[t])
                    {
                        var index = (j * columns + i) * words + t / BitsPerWord;
                        table[index] |= 1 << (t % BitsPerWord);
                        active[j * columns + i]++;
                    }
                }
            }
        }

        // ---- the actors: persistent, and out of their layer's enable parent ------------------------
        foreach (var n in actors)
        {
            n.EnableParent = null;
            if ((n.MajorRecordFlagsRaw & persistentFlag) == 0)
            {
                foreach (var c in cells.Values)
                {
                    c.Temporary.Remove(n);
                }

                n.MajorRecordFlagsRaw |= persistentFlag;
                persistentCell.Persistent.Add(n);
            }
        }

        // ---- the script and its switch --------------------------------------------------------------
        var global = new GlobalFloat(mod) { EditorID = cfg.Global, Data = 1f };
        mod.Globals.Add(global);
        var layerGlobal = (layerScript.Properties.FirstOrDefault(p => p.Name == "CrowdLayer") as ScriptObjectProperty)?.Object.FormKey;
        ScriptObjectProperty Obj(string name, FormKey key) => new() { Name = name, Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
        var script = new ScriptEntry
        {
            Name = cfg.Script,
            Properties =
            {
                Obj("FairWorld", worldspace.FormKey),
                new ScriptObjectListProperty
                {
                    Name = "Actors",
                    Objects = actors.Select(n => Obj("", n.FormKey)).ToExtendedList(),
                },
                new ScriptIntListProperty { Name = "Layers", Data = layers.ToExtendedList() },
                new ScriptIntListProperty { Name = "Table", Data = table.ToExtendedList() },
                new ScriptFloatProperty { Name = "OriginX", Data = ox },
                new ScriptFloatProperty { Name = "OriginY", Data = oy },
                new ScriptFloatProperty { Name = "Spot", Data = spot },
                new ScriptIntProperty { Name = "Columns", Data = columns },
                new ScriptIntProperty { Name = "Rows", Data = rows },
                new ScriptIntProperty { Name = "Words", Data = words },
                Obj("Culling", global.FormKey),
                new ScriptFloatProperty { Name = "Poll", Data = cfg.Poll },
                new ScriptFloatProperty { Name = "IdlePoll", Data = cfg.IdlePoll },
            },
        };
        if (layerGlobal is { } lg)
        {
            script.Properties.Add(Obj("CrowdLayer", lg));
        }

        stageQuest.VirtualMachineAdapter.Scripts.Add(script);

        int ActiveAt(float x, float y) =>
            active[Math.Clamp((int)MathF.Floor((y - oy) / spot), 0, rows - 1) * columns + Math.Clamp((int)MathF.Floor((x - ox) / spot), 0, columns - 1)];
        var inside = standing.Select(v => ActiveAt(v.X, v.Y)).OrderBy(a => a).ToList();
        return new CullingResult(
            actors.Count, painted, standing.Count, columns, rows, words, table.Length,
            inside.Count == 0 ? 0 : (float)inside.Average(), inside.Count == 0 ? 0 : inside[inside.Count / 2], inside.Count == 0 ? 0 : inside[^1],
            cfg.ReportAt.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (kv.Key, ActiveAt(kv.Value[0], kv.Value[1]))).ToList());
    }
}

internal sealed record CullingResult(
    int Actors, int Objects, int StandingSpots, int Columns, int Rows, int Words, int TableLength,
    float MeanOn, int MedianOn, int MaxOn, IReadOnlyList<(string Name, int On)> At);
