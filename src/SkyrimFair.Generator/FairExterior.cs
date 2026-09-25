using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Skyrim.Assets;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The compound's exterior in Tamriel (<see cref="ExteriorConfig"/>): the fair's own outline,
/// scaled and turned, walled with the same palisade on Tamriel's ground and decorated on its
/// outer face, with a load door in its gate to the fair's worldspace (and the fair's gate made
/// the door back). Inside, only what shows over the wall or from the hills: the stage, the light
/// towers and the cook fires, copied from the built fair. A script there plays the fair's music
/// faintly and sends fireworks up at night while the player is near.
///
/// It replaces the old raised-terrace prototype (Barry, 2026-09-24): every earlier Tamriel cell
/// override is dropped, so the terrace, the test stall and the clutter it disabled are gone,
/// with no FormID reused. Built last, from its own FormID range, so nothing else renumbers.
/// </summary>
internal static class FairExterior
{
    private const int PersistentFlag = 0x400;

    private const int InitiallyDisabledFlag = 0x800;

    private const float CellSize = 4096f;

    private static readonly FormKey XMarker = FormKey.Factory("00003B:Skyrim.esm");

    private static readonly FormKey XMarkerHeading = FormKey.Factory("000034:Skyrim.esm");

    private static readonly FormKey XMarkerActivator = FormKey.Factory("06CD3D:Skyrim.esm");

    public static ExteriorResult Build(
        SkyrimMod mod, FairConfig config, ISkyrimModGetter master, IWorldspaceGetter vanilla,
        Worldspace tamriel, Cell tamrielPersistent, PlacedObject mapMarker)
    {
        var ext = config.Exterior;
        var fw = config.FairWorld;
        var result = new ExteriorResult();

        var saved = mod.ModHeader.Stats.NextFormID;
        if (saved >= ext.FormIdBase)
        {
            throw new InvalidOperationException($"FormIDs reached the exterior's range (0x{ext.FormIdBase:X}): raise exterior.formIdBase");
        }

        mod.ModHeader.Stats.NextFormID = ext.FormIdBase;

        // ---- the frame: the fair's plan, scaled about its gate and turned onto Tamriel -------------
        if (ext.RotationDegrees is not (0f or 180f))
        {
            throw new InvalidOperationException("exterior.rotationDegrees must be 0 or 180 (tilted pieces are only turned exactly for those).");
        }

        var flip = ext.RotationDegrees == 180f ? -1f : 1f;
        var turn = ext.RotationDegrees * MathF.PI / 180f;
        var (px0, py0) = (fw.Gate[0], fw.Gate[1]);
        var (gx, gy) = (ext.Gate[0], ext.Gate[1]);
        (float X, float Y) Scaled(float x, float y) => (gx + flip * ext.Scale * (x - px0), gy + flip * ext.Scale * (y - py0));
        (float X, float Y) Offset(float u, float v) => (gx + flip * u, gy + flip * v);

        var terrain = new TerrainSampler(vanilla);
        float Ground(float x, float y) => terrain.Sample(x, y)
            ?? throw new InvalidOperationException($"exterior: no Tamriel ground at ({x:0}, {y:0})");

        // A wall piece stands on the lowest ground under it, so on a slope its downhill end
        // doesn't float (a panel is ~520 wide, and the ground here falls up to 0.4 across one).
        float LowestGround(float x, float y, float r)
        {
            var z = Ground(x, y);
            for (var k = 0; k < 8; k++)
            {
                z = MathF.Min(z, Ground(x + r * MathF.Cos(k * MathF.PI / 4f), y + r * MathF.Sin(k * MathF.PI / 4f)));
            }

            return z;
        }

        var outline = fw.Perimeter.Select(p => Scaled(p[0], p[1])).ToList();
        var (ox, oy) = (outline.Average(p => p.X), outline.Average(p => p.Y));

        // ---- knock down the old prototype: every Tamriel cell override goes ----------------------
        tamriel.SubCells.Clear();
        var cells = new Dictionary<(int X, int Y), Cell>();
        Cell CellAt(int cx, int cy)
        {
            if (!cells.TryGetValue((cx, cy), out var cell))
            {
                cells[(cx, cy)] = cell = FairPluginGenerator.BuildExteriorCell(vanilla, cx, cy);
            }

            return cell;
        }

        void PutObject(PlacedObject o)
        {
            var pos = o.Placement!.Position;
            CellAt((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize)).Temporary.Add(o);
        }

        PlacedObject Persistent(PlacedObject o)
        {
            o.MajorRecordFlagsRaw |= PersistentFlag;
            tamrielPersistent.Persistent.Add(o);
            return o;
        }

        // ---- the wall, as the fair's is laid, on Tamriel's ground ------------------------------
        var extPlan = fw with { Perimeter = outline.Select(p => new[] { p.X, p.Y }).ToList(), Gate = new[] { gx, gy } };
        var panelHalf = fw.Palisade.Width * fw.Palisade.Scale / 2f;
        var panels = FairWorld.WallPanelsOn(extPlan, (x, y) => LowestGround(x, y, panelHalf));
        var panelStatic = mod.Statics.First(s => s.EditorID == fw.Palisade.EditorId);
        foreach (var p in panels)
        {
            PutObject(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(panelStatic.FormKey),
                Scale = p.Scale,
                Placement = new Placement { Position = new P3Float(p.X, p.Y, p.Z), Rotation = new P3Float(0f, 0f, p.Heading * MathF.PI / 180f) },
            });
        }

        result.Panels = panels.Count;
        if (ext.Decorate && fw.Life.Palisade.Banners.Count > 0)
        {
            // The wall is at the exterior's scale, so its banners are too.
            var outerFace = fw.Life.Palisade with
            {
                BannerScale = fw.Life.Palisade.BannerScale * ext.Scale,
                BannerDrop = fw.Life.Palisade.BannerDrop * ext.Scale,
            };
            (result.Banners, result.Ropes, result.Lanterns) = FairLife.DecoratePalisade(
                mod, outerFace, fw.Palisade, new[] { gx, gy }, panels, (ox, oy), true, PutObject);
        }

        // ---- the gate: a load door each side ------------------------------------------------------
        var fairWorld = mod.Worldspaces.First(w => w.EditorID == fw.EditorId);
        var fairCells = fairWorld.SubCells.SelectMany(b => b.Items).SelectMany(sb => sb.Items).ToList();
        var inner = fairCells.SelectMany(c => c.Temporary.OfType<PlacedObject>().Select(o => (Cell: c, Ref: o)))
            .First(x => x.Ref.EditorID == $"{fw.EditorId}MainGate");
        var template = master.Doors.First(d => d.FormKey == FormKeyHelper.Parse(ext.DoorTemplate));
        Door DoorRecord(string editorId, string name)
        {
            var d = template.Duplicate(mod.GetNextFormKey());
            d.EditorID = editorId;
            d.Name = name;
            d.Model = new Model { File = fw.GatePiece.Model };
            mod.Doors.Add(d);
            return d;
        }

        var outDoor = DoorRecord("SkyrimFairExteriorGate", ext.GateName);
        var inDoor = DoorRecord("SkyrimFairExitGate", ext.ExitName);

        var innerYaw = inner.Ref.Placement!.Rotation.Z;
        var outer = Persistent(new PlacedObject(mod)
        {
            EditorID = "SkyrimFairExteriorGateRef",
            Base = new FormLinkNullable<IPlaceableObjectGetter>(outDoor.FormKey),
            Scale = fw.GatePiece.Scale,
            Placement = new Placement
            {
                Position = new P3Float(gx, gy, LowestGround(gx, gy, fw.GatePiece.Width * fw.GatePiece.Scale / 2f) - fw.GatePiece.Sink),
                Rotation = new P3Float(0f, 0f, innerYaw + turn),
            },
        });

        // The fair's own gate becomes the door out: same FormID, a door base, persistent.
        inner.Cell.Temporary.Remove(inner.Ref);
        inner.Ref.Base = new FormLinkNullable<IPlaceableObjectGetter>(inDoor.FormKey);
        inner.Ref.MajorRecordFlagsRaw |= PersistentFlag;
        fairWorld.TopCell!.Persistent.Add(inner.Ref);

        // Arrive a little in from each gate: facing into the fair, or away from it on the road.
        var ip = inner.Ref.Placement.Position;
        var (ifx, ify) = (MathF.Sin(innerYaw), MathF.Cos(innerYaw));
        var (ax, ay) = (gx - flip * ifx * ext.Arrive, gy - flip * ify * ext.Arrive);
        outer.TeleportDestination = new TeleportDestination
        {
            Door = new FormLink<IPlacedObjectGetter>(inner.Ref.FormKey),
            Position = new P3Float(ip.X + ifx * ext.Arrive, ip.Y + ify * ext.Arrive, fw.FloorZ + 10f),
            Rotation = new P3Float(0f, 0f, innerYaw),
        };
        inner.Ref.TeleportDestination = new TeleportDestination
        {
            Door = new FormLink<IPlacedObjectGetter>(outer.FormKey),
            Position = new P3Float(ax, ay, Ground(ax, ay) + 10f),
            Rotation = new P3Float(0f, 0f, innerYaw + turn + MathF.PI),
        };

        // The map marker (same FormID) moves to the road in front of the gate.
        var (mx, my) = (gx - flip * ifx * ext.MapMarkerOut, gy - flip * ify * ext.MapMarkerOut);
        mapMarker.Placement!.Position = new P3Float(mx, my, Ground(mx, my));

        // ---- what shows over the wall: the stage, the towers, the fires, from the built fair -----------
        var skip = new HashSet<FormKey> { XMarker, XMarkerHeading, XMarkerActivator };
        skip.UnionWith(mod.SoundMarkers.Select(s => s.FormKey));
        skip.UnionWith(mod.IdleMarkers.Select(s => s.FormKey));
        skip.UnionWith(mod.Statics.Where(s => s.EditorID == fw.SolidWall.EditorId).Select(s => s.FormKey));
        var fairRefs = fairCells.SelectMany(c => c.Temporary.OfType<PlacedObject>())
            .Concat(fairWorld.TopCell!.Persistent.OfType<PlacedObject>())
            .Where(o => o.Primitive is null && o.EnableParent is null && (o.MajorRecordFlagsRaw & InitiallyDisabledFlag) == 0
                && o.TeleportDestination is null && !skip.Contains(o.Base.FormKey)
                && !(o.EditorID ?? "").StartsWith(fw.Cameos.EditorIdPrefix, StringComparison.Ordinal))
            .OrderBy(o => o.FormKey.ID)
            .ToList();
        var anchors = new Dictionary<string, ((float X, float Y) At, float Z, float Cx, float Cy)>();
        foreach (var cluster in ext.Clusters)
        {
            var (ccx, ccy) = (cluster.Centre[0], cluster.Centre[1]);
            var at = cluster.To is { Length: 2 } to ? Offset(to[0], to[1]) : Scaled(ccx, ccy);
            var r = cluster.Radius;
            bool In(P3Float p) => cluster.Rect is { Length: 4 } rect
                ? p.X >= rect[0] && p.X <= rect[2] && p.Y >= rect[1] && p.Y <= rect[3]
                : (p.X - ccx) * (p.X - ccx) + (p.Y - ccy) * (p.Y - ccy) <= r * r;

            // Never float: stand on the lowest ground under the cluster's reach.
            var reach = cluster.Rect is { Length: 4 } rc ? MathF.Max(rc[2] - rc[0], rc[3] - rc[1]) / 2f : r;
            var z = new[] { (0f, 0f), (1f, 1f), (1f, -1f), (-1f, 1f), (-1f, -1f) }
                .Min(d => Ground(at.X + d.Item1 * reach * 0.7f, at.Y + d.Item2 * reach * 0.7f));
            anchors[cluster.Name] = (at, z, ccx, ccy);
            foreach (var o in fairRefs.Where(o => In(o.Placement!.Position)))
            {
                var p = o.Placement!;
                var (x, y) = (at.X + flip * (p.Position.X - ccx), at.Y + flip * (p.Position.Y - ccy));
                PutObject(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(o.Base.FormKey),
                    Scale = o.Scale,
                    Placement = new Placement
                    {
                        Position = new P3Float(x, y, z + p.Position.Z - fw.FloorZ),
                        Rotation = new P3Float(flip * p.Rotation.X, flip * p.Rotation.Y, p.Rotation.Z + turn),
                    },
                });
                result.Silhouette++;
            }
        }

        // ---- vanilla scenery inside the walls or through them: disabled ---------------------------------
        var scenery = FairPluginGenerator.CollectClearableBases(master);
        var names = master.Statics.ToDictionary(st => st.FormKey, st => st.EditorID);

        // Large references (the worldspace's RNAM list) keep their LOD model in view while they
        // aren't loaded and showing, and a disabled one never is: its LOD stood in front of the
        // gate (2026-09-25). Those are left enabled and sunk out of sight instead, so the engine
        // loads them and hides the LOD.
        var large = vanilla.LargeReferences.SelectMany(l => l.References).Select(r => r.Reference.FormKey).ToHashSet();

        // The approach: from just out of the gate to the road, cleared and paved.
        var (ofx, ofy) = (-flip * MathF.Sin(innerYaw), -flip * MathF.Cos(innerYaw));
        var app = ext.Approach;
        var (sx0, sy0) = (gx + ofx * app.Start, gy + ofy * app.Start);
        var (sx1, sy1) = app.To is { Length: 2 } road ? (road[0], road[1]) : (sx0, sy0);
        bool InApproach(float x, float y)
        {
            if (!app.Enabled) return false;
            var (dx, dy) = (sx1 - sx0, sy1 - sy0);
            var len = MathF.Max(1f, MathF.Sqrt(dx * dx + dy * dy));
            var (tx0, ty0) = (x - sx0, y - sy0);
            var along = (tx0 * dx + ty0 * dy) / len;
            var across = MathF.Abs(tx0 * dy - ty0 * dx) / len;
            return along >= -app.Start && along <= len && across <= app.ClearHalfWidth;
        }
        var spawners = master.Activators.Where(a => (a.EditorID ?? "").StartsWith("critterSpawn", StringComparison.OrdinalIgnoreCase)).Select(a => a.FormKey).ToHashSet();
        var prey = master.LeveledNpcs.Where(l => (l.EditorID ?? "").StartsWith("LvlAnimal", StringComparison.OrdinalIgnoreCase)).Select(l => l.FormKey).ToHashSet();
        bool Inside(float x, float y)
        {
            var c = false;
            for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            {
                var (xi, yi, xj, yj) = (outline[i].X, outline[i].Y, outline[j].X, outline[j].Y);
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
            }

            return c;
        }

        float WallDistance(float x, float y)
        {
            var best = float.MaxValue;
            for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            {
                var (ax0, ay0, bx, by) = (outline[j].X, outline[j].Y, outline[i].X, outline[i].Y);
                var l2 = (bx - ax0) * (bx - ax0) + (by - ay0) * (by - ay0);
                var t = Math.Clamp(((x - ax0) * (bx - ax0) + (y - ay0) * (by - ay0)) / l2, 0f, 1f);
                var (dx, dy) = (x - (ax0 + t * (bx - ax0)), y - (ay0 + t * (by - ay0)));
                best = MathF.Min(best, MathF.Sqrt(dx * dx + dy * dy));
            }

            return best;
        }

        var (minX, maxX) = (outline.Min(p => p.X) - ext.ClearMaxRadius, outline.Max(p => p.X) + ext.ClearMaxRadius);
        var (minY, maxY) = (outline.Min(p => p.Y) - ext.ClearMaxRadius, outline.Max(p => p.Y) + ext.ClearMaxRadius);
        for (var cy = (int)MathF.Floor(minY / CellSize); cy <= (int)MathF.Floor(maxY / CellSize); cy++)
        {
            for (var cx = (int)MathF.Floor(minX / CellSize); cx <= (int)MathF.Floor(maxX / CellSize); cx++)
            {
                if (FairPluginGenerator.FindVanillaCell(vanilla, cx, cy) is not { } vc)
                {
                    continue;
                }

                foreach (var o in vc.Temporary.OfType<IPlacedObjectGetter>().OrderBy(o => o.FormKey.ID))
                {
                    if (o.Placement is not { } p) continue;
                    var (x, y) = (p.Position.X, p.Position.Y);
                    bool hit;
                    if (InApproach(x, y) && FairPluginGenerator.WhyUnsafeToDisable(o, scenery) is null
                        && !(names.GetValueOrDefault(o.Base.FormKey) ?? "").StartsWith("Road", StringComparison.Ordinal))
                    {
                        // The way in: everything but the road itself, landscape rocks too.
                        hit = true;
                    }
                    else if (spawners.Contains(o.Base.FormKey))
                    {
                        hit = Inside(x, y);
                    }
                    else if (scenery.TryGetValue(o.Base.FormKey, out var radius) && radius <= ext.ClearMaxRadius
                        && FairPluginGenerator.WhyUnsafeToDisable(o, scenery) is null)
                    {
                        hit = Inside(x, y) || WallDistance(x, y) <= radius * (o.Scale ?? 1f) + ext.ClearMargin;
                    }
                    else
                    {
                        continue;
                    }

                    if (!hit) continue;
                    var off = (PlacedObject)o.DeepCopy();
                    if (large.Contains(o.FormKey))
                    {
                        off.Placement!.Position = new P3Float(p.Position.X, p.Position.Y, p.Position.Z - ext.SinkLarge);
                        result.Sunk++;
                    }
                    else
                    {
                        off.MajorRecordFlagsRaw |= InitiallyDisabledFlag;
                    }

                    CellAt(cx, cy).Temporary.Add(off);
                    result.Disabled++;
                }

                foreach (var n in vc.Temporary.OfType<IPlacedNpcGetter>().OrderBy(n => n.FormKey.ID))
                {
                    if (n.Placement is not { } p || !prey.Contains(n.Base.FormKey) || !Inside(p.Position.X, p.Position.Y)) continue;
                    var off = (PlacedNpc)n.DeepCopy();
                    off.MajorRecordFlagsRaw |= InitiallyDisabledFlag;
                    CellAt(cx, cy).Temporary.Add(off);
                    result.Disabled++;
                }
            }
        }

        // ---- heard and seen from outside: faint music, the crowd, fireworks at night ----------------------
        if (ext.Show.Enabled && anchors.TryGetValue(ext.Show.Cluster, out var stageAt))
        {
            var show = ext.Show;
            var audio = fw.Audio;
            var p = audio.EditorIdPrefix;
            var root = Path.IsPathRooted(audio.SoundRoot) ? audio.SoundRoot : Path.Combine(FairPaths.ConfigDirectory, audio.SoundRoot);
            (float X, float Y) FromStage(float x, float y) => (stageAt.At.X + flip * (x - stageAt.Cx), stageAt.At.Y + flip * (y - stageAt.Cy));

            SoundOutputModel Output(string name, string source, float min, float max)
            {
                var o = master.SoundOutputModels.First(m => m.FormKey == FormKeyHelper.Parse(source)).Duplicate(mod.GetNextFormKey());
                o.EditorID = $"{p}{name}Output";
                o.Attenuation!.MinDistance = (ushort)min;
                o.Attenuation.MaxDistance = (ushort)max;
                mod.SoundOutputModels.Add(o);
                return o;
            }

            SoundMarker Sound(string name, string file, string category, SoundOutputModel output, bool loop, float attenuation)
            {
                var d = new SoundDescriptor(mod)
                {
                    EditorID = $"{p}{name}",
                    Type = SoundDescriptor.DescriptorType.Standard,
                    Category = new FormLinkNullable<ISoundCategoryGetter>(mod.SoundCategories.First(c => c.EditorID == $"{p}{category}Category").FormKey),
                    OutputModel = new FormLinkNullable<ISoundOutputModelGetter>(output.FormKey),
                    LoopAndRumble = new SoundLoopAndRumble { Loop = loop ? SoundDescriptor.LoopType.Loop : SoundDescriptor.LoopType.None },
                    Priority = 128,
                    StaticAttenuation = attenuation,
                };
                d.SoundFiles.Add(new AssetLink<SkyrimSoundAssetType>(@"Data\Sound\" + file));
                mod.SoundDescriptors.Add(d);
                var s = new SoundMarker(mod)
                {
                    EditorID = $"{p}{name}Marker",
                    SoundDescriptor = new FormLinkNullable<ISoundDescriptorGetter>(d.FormKey),
                    ObjectBounds = new ObjectBounds { First = new P3Int16(-16, -16, -16), Second = new P3Int16(16, 16, 16) },
                };
                mod.SoundMarkers.Add(s);
                return s;
            }

            var musicOut = Output("Outside", audio.Stage.OutputModel, show.MusicMinDistance, show.MusicMaxDistance);
            var songs = audio.Stage.Songs
                .Select(s => s.Added
                    ? FairAddedSongs.Build(mod, () => Sound($"OutsideSong{s.Name}", s.File, "Stage", musicOut, false, show.MusicAttenuation))
                    : Sound($"OutsideSong{s.Name}", s.File, "Stage", musicOut, false, show.MusicAttenuation))
                .ToList();
            var lengths = audio.Stage.Songs.Select(s => FairAudio.WavSeconds(Path.Combine(root, s.File.Replace('\\', Path.DirectorySeparatorChar)))).ToList();

            var speaker = Persistent(new PlacedObject(mod)
            {
                EditorID = "SkyrimFairOutsideSpeaker",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(XMarker),
                Placement = new Placement { Position = new P3Float(stageAt.At.X, stageAt.At.Y, stageAt.Z + show.SpeakerHeight), Rotation = new P3Float(0f, 0f, 0f) },
            });

            if (audio.Ambience.Loops.FirstOrDefault() is { Files.Count: > 0 } crowd)
            {
                var crowdOut = Output("OutsideCrowd", "0010C2ED:Skyrim.esm", show.CrowdMinDistance, show.CrowdMaxDistance);
                var murmur = Sound("OutsideCrowd", crowd.Files[0], "Ambience", crowdOut, true, show.CrowdAttenuation);
                PutObject(new PlacedObject(mod)
                {
                    EditorID = "SkyrimFairOutsideCrowd",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(murmur.FormKey),
                    Placement = new Placement { Position = new P3Float(ox, oy, Ground(ox, oy) + 150f), Rotation = new P3Float(0f, 0f, 0f) },
                });
            }

            // The fair's own launch sites, where they stand behind its stage.
            var sites = new List<PlacedObject>();
            var aims = new List<PlacedObject>();
            foreach (var s in fw.Audio.Stage.Fireworks.Sites)
            {
                var (x, y) = FromStage(s[0], s[1]);
                var z = Ground(x, y);
                var n = sites.Count + 1;
                sites.Add(Persistent(new PlacedObject(mod)
                {
                    EditorID = $"SkyrimFairOutsideFireworkSite{n:00}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(XMarkerActivator),
                    Placement = new Placement { Position = new P3Float(x, y, z + 20f), Rotation = new P3Float(0f, 0f, 0f) },
                }));
                aims.Add(Persistent(new PlacedObject(mod)
                {
                    EditorID = $"SkyrimFairOutsideFireworkAim{n:00}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(XMarker),
                    Placement = new Placement { Position = new P3Float(x, y, z + 1520f), Rotation = new P3Float(0f, 0f, 0f) },
                }));
            }

            // The shells the stage fires, every colour once.
            var stageScript = mod.Quests.SelectMany(q => q.VirtualMachineAdapter?.Scripts ?? new ExtendedList<ScriptEntry>())
                .First(s => s.Name == "SkyrimFairAudioScript");
            var shells = stageScript.Properties.OfType<ScriptObjectListProperty>()
                .Where(pr => pr.Name is "FireworkShells" or "FireworkNightShells")
                .SelectMany(pr => pr.Objects.Select(o => o.Object.FormKey))
                .Distinct()
                .ToList();
            FormKey Global(string editorId) => mod.Globals.FirstOrDefault(g => g.EditorID == editorId)?.FormKey ?? FormKey.Null;
            ScriptObjectProperty Obj(string name, FormKey key) => new() { Name = name, Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
            ScriptObjectListProperty List(string name, IEnumerable<FormKey> keys) => new()
            {
                Name = name,
                Objects = keys.Select(k => new ScriptObjectProperty { Object = new FormLink<ISkyrimMajorRecordGetter>(k) }).ToExtendedList(),
            };

            var controller = new PlacedObject(mod)
            {
                EditorID = "SkyrimFairOutsideShow",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(XMarkerActivator),
                Placement = new Placement { Position = new P3Float(stageAt.At.X, stageAt.At.Y, stageAt.Z + 40f), Rotation = new P3Float(0f, 0f, 0f) },
                VirtualMachineAdapter = new VirtualMachineAdapter(),
            };
            var script = new ScriptEntry { Name = "SkyrimFairOutsideShow" };
            script.Properties.Add(List("Songs", songs.Select(s => s.FormKey)));
            script.Properties.Add(new ScriptFloatListProperty { Name = "SongLengths", Data = lengths.ToExtendedList() });
            script.Properties.Add(new ScriptFloatProperty { Name = "SongGap", Data = show.SongGap });
            script.Properties.Add(Obj("Speaker", speaker.FormKey));
            script.Properties.Add(List("FireworkSites", sites.Select(s => s.FormKey)));
            script.Properties.Add(List("FireworkAims", aims.Select(s => s.FormKey)));
            script.Properties.Add(List("FireworkShells", shells));
            script.Properties.Add(new ScriptFloatProperty { Name = "FireworkStagger", Data = fw.Audio.Stage.Fireworks.Stagger });
            script.Properties.Add(new ScriptFloatProperty { Name = "FireworkEvery", Data = show.FireworkEvery });
            if (Global("SkyrimFairFireworks") is { IsNull: false } on) script.Properties.Add(Obj("FireworksOn", on));
            if (Global($"{p}MusicEnabled") is { IsNull: false } music) script.Properties.Add(Obj("MusicEnabled", music));
            controller.VirtualMachineAdapter.Scripts.Add(script);
            PutObject(controller);
            result.Songs = songs.Count;
            result.FireworkSites = sites.Count;
        }

        // ---- the approach: road chunks up to the gate, and a Whiterun flag each side of it --------------
        // Tilts follow the ground. Skyrim turns a reference about the world Z, then Y, then X,
        // clockwise (the vanilla road pieces fit that best), so local up is (-sin y, cos y sin x,
        // cos y cos x), and a piece lies on a slope of normal n at y = -asin(nx), x = atan2(ny, nz).
        P3Float OnGround(float x, float y, float yaw)
        {
            const float e = 16f;
            var (zx, zy) = ((Ground(x + e, y) - Ground(x - e, y)) / (2f * e), (Ground(x, y + e) - Ground(x, y - e)) / (2f * e));
            var len = MathF.Sqrt(zx * zx + zy * zy + 1f);
            var (nx, ny, nz) = (-zx / len, -zy / len, 1f / len);
            return new P3Float(MathF.Atan2(ny, nz), -MathF.Asin(nx), yaw);
        }

        if (app.Enabled && app.Pieces.Count > 0)
        {
            var (dx, dy) = (sx1 - sx0, sy1 - sy0);
            var length = MathF.Sqrt(dx * dx + dy * dy);
            var (ux, uy) = (dx / length, dy / length);
            var heading = MathF.Atan2(ux, uy);
            var k = 0;
            for (var at = 0f; at <= length; at += app.Spacing)
            {
                foreach (var side in new[] { 0f, -1f, 1f })
                {
                    k++;
                    var piece = side == 0f ? app.Pieces[k % app.Pieces.Count] : app.EdgePieces[k % app.EdgePieces.Count];
                    var off = side * app.HalfWidth + FairHash.Signed(4400, k, 1) * 30f;
                    var slide = FairHash.Signed(4400, k, 2) * app.Spacing * 0.25f;
                    var (x, y) = (sx0 + ux * (at + slide) + uy * off, sy0 + uy * (at + slide) - ux * off);
                    var yaw = heading + MathF.PI / 2f + FairHash.Signed(4400, k, 3) * 0.2f + (side == 0f ? 0f : FairHash.Hash3(4400, k, 4) * MathF.PI);
                    if (!app.Pave)
                    {
                        // Unpaved: the piece's FormID is kept, so everything after it keeps its own.
                        mod.GetNextFormKey();
                        continue;
                    }

                    PutObject(new PlacedObject(mod)
                    {
                        Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(piece)),
                        Placement = new Placement { Position = new P3Float(x, y, Ground(x, y) - app.Sink), Rotation = OnGround(x, y, yaw) },
                    });
                    result.Path++;
                }
            }
        }

        // The flags stand just out from the wall either side of the gate, facing the road.
        if (ext.GateFlags.Enabled && ext.GateFlags.Pieces.Count > 0)
        {
            var gf = ext.GateFlags;
            var (rx, ry) = (ofy, -ofx);
            var yaw = MathF.Atan2(ofx, ofy);
            foreach (var side in new[] { -1f, 1f })
            {
                var (fx0, fy0) = (gx + rx * side * gf.Out + ofx * gf.Forward, gy + ry * side * gf.Out + ofy * gf.Forward);
                var z = Ground(fx0, fy0);
                foreach (var piece in gf.Pieces)
                {
                    PutObject(new PlacedObject(mod)
                    {
                        Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(piece.Piece)),
                        Placement = new Placement
                        {
                            Position = new P3Float(fx0 + MathF.Sin(yaw) * piece.Y, fy0 + MathF.Cos(yaw) * piece.Y, z + piece.Z),
                            Rotation = new P3Float(0f, 0f, yaw),
                        },
                    });
                    result.Flags++;
                }
            }
        }

        // The Whiterun road sign, beside the path just out from the gate (it stood inside, 2026-09-25).
        if (ext.RoadSign.Enabled && ext.RoadSign.Pieces.Count > 0)
        {
            var rs = ext.RoadSign;
            var (rx, ry) = (ofy, -ofx);
            var (sx, sy) = (gx + rx * rs.Side * rs.Out + ofx * rs.Forward, gy + ry * rs.Side * rs.Out + ofy * rs.Forward);
            var z = Ground(sx, sy);
            foreach (var piece in rs.Pieces)
            {
                PutObject(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(piece.Piece)),
                    Placement = new Placement
                    {
                        Position = new P3Float(sx, sy, z + piece.Z),
                        Rotation = new P3Float(0f, 0f, piece.Yaw * MathF.PI / 180f),
                    },
                });
                result.Signs++;
            }
        }

        // The fair's welcome sign, on the path's other side, its lettered face (the model's -Y)
        // toward arrivals, turned a little toward the path.
        if (ext.FairSign.Enabled && ext.FairSign.Model.Length > 0)
        {
            var fs = ext.FairSign;
            var sign = FairWorld.AddStatic(mod, fs);
            var (rx, ry) = (ofy, -ofx);
            var (sx, sy) = (gx + rx * fs.Side * fs.Out + ofx * fs.Forward, gy + ry * fs.Side * fs.Out + ofy * fs.Forward);
            // Local +Y points back at the gate, so -Y (the face) points out along the path.
            var faceYaw = MathF.Atan2(-ofx, -ofy) - fs.Side * fs.TurnToPath * MathF.PI / 180f;
            PutObject(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(sign.FormKey),
                Scale = fs.Scale == 1f ? null : fs.Scale,
                Placement = new Placement
                {
                    Position = new P3Float(sx, sy, Ground(sx, sy) - fs.Sink),
                    Rotation = new P3Float(0f, 0f, faceYaw),
                },
            });
            result.Signs++;
        }

        // Cover for bare ground the clearing left showing (appended, at the end of the range).
        foreach (var c in ext.Cover)
        {
            PutObject(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(c.Piece)),
                Scale = c.Scale == 1f ? null : c.Scale,
                Placement = new Placement { Position = new P3Float(c.X, c.Y, Ground(c.X, c.Y) + c.Z), Rotation = OnGround(c.X, c.Y, c.Yaw * MathF.PI / 180f) },
            });
        }

        // ---- file the cells ------------------------------------------------------------------------
        var grid = new ExteriorCellGrid(tamriel);
        foreach (var ((cx, cy), cell) in cells.OrderBy(c => c.Key.Y).ThenBy(c => c.Key.X))
        {
            grid.Add(cell, cx, cy);
        }

        result.Cells = cells.Keys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
        result.FormIds = (ext.FormIdBase, mod.ModHeader.Stats.NextFormID - 1);
        mod.ModHeader.Stats.NextFormID = saved;
        return result;
    }
}

internal sealed class ExteriorResult
{
    public int Panels { get; set; }

    public int Banners { get; set; }

    public int Ropes { get; set; }

    public int Lanterns { get; set; }

    public int Silhouette { get; set; }

    public int Disabled { get; set; }

    public int Songs { get; set; }

    public int FireworkSites { get; set; }

    public int Path { get; set; }

    public int Flags { get; set; }

    public int Sunk { get; set; }

    public int Signs { get; set; }

    public List<(int X, int Y)> Cells { get; set; } = new();

    public (uint From, uint To) FormIds { get; set; }
}
