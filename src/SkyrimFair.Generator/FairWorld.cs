using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Builds the isolated festival worldspace described by <see cref="FairWorldConfig"/>:
/// a new WRLD with its own climate, a persistent cell of named zone markers, and a
/// square of exterior cells each carrying a generated LAND record.
///
/// This is a parallel prototype. It adds records only; nothing in Tamriel, and nothing
/// the terrace build writes, is read or changed here.
///
/// The ground is generated from rules rather than painted by hand, so it regenerates
/// byte-identically: flat inside the planned perimeter, rising into low hills beyond
/// it, and textured so the broad plan can be walked in game before anything is built.
/// </summary>
internal static class FairWorld
{
    /// <summary>The SkyrimFairAtFair global built last run, for the compatibility patches.</summary>
    public static FormKey? AtFairGlobal { get; private set; }

    private const int CellSize = 4096;

    /// <summary>LAND vertices per side. Adjacent cells share their edge row.</summary>
    private const int Points = 33;

    private const float Step = CellSize / (Points - 1f);

    /// <summary>Vertices per side of one texture quadrant (a quarter cell, edges shared).</summary>
    private const int QuadPoints = 17;

    /// <summary>VHGT stores heights in units of eight.</summary>
    private const float HeightUnit = 8f;

    /// <summary>The Persistent record flag; Mutagen does not write it from group membership.</summary>
    private const int PersistentRecordFlag = 0x400;

    /// <summary>
    /// Is Full LOD: never fades and draws beyond the loaded cells. Vanilla gives it, with
    /// Persistent, to the distant scenery in its small worlds.
    /// </summary>
    private const int FullLodRecordFlag = 0x10000;

    /// <summary>
    /// DATA flags on LAND, written raw as 0x1D: normals + heights (0x01), layers (0x04),
    /// and the 0x08 and 0x10 bits every vanilla LAND carries. Sovngarde's LAND, which
    /// also has no vertex colours, is exactly this value. Not built from Mutagen's names:
    /// its <c>MPCD</c> member is 0x400, not the 0x08 bit.
    /// </summary>
    private const Landscape.Flag LandFlags = (Landscape.Flag)0x1D;

    /// <summary>BTXT's layer field is always -1.</summary>
    private const ushort BaseLayerNumber = 0xFFFF;

    /// <summary>
    /// Most alpha layers one quadrant may carry. The game draws six textures a quadrant, the
    /// base and five alpha layers (vanilla never uses more); a sixth alpha layer is simply
    /// not drawn, which cut the cobbles off at a quadrant edge.
    /// </summary>
    private const int MaxAlphaLayers = 5;

    /// <summary>Faint layers left out of quadrants over the limit, in the last build.</summary>
    public static int DroppedLayers => droppedLayers;

    private static int droppedLayers;

    /// <summary>Water is switched off in every cell; this is belt and braces for the defaults.</summary>
    private const float NoWaterHeight = -50000f;

    private const float Deg = MathF.PI / 180f;

    /// <summary>Soft edge of every painted zone.</summary>
    private const float ZoneFeather = 192f;

    private const float StripFeather = 96f;

    public static FairWorldResult Build(SkyrimMod mod, FairWorldConfig config, ISkyrimModGetter? master)
    {
        var plan = new Plan(config);

        // ---- climate: vanilla sky and sun, the tundra region's weather ------------
        var climateKey = BuildClimate(mod, config, master, out var weatherCount);

        // ---- the worldspace itself ------------------------------------------------
        var worldspace = new Worldspace(mod)
        {
            EditorID = config.EditorId,
            Name = config.Name,
            Parent = new WorldspaceParent
            {
                Worldspace = new FormLink<IWorldspaceGetter>(FormKeyHelper.Parse(config.ParentWorldspace)),
                Flags = WorldspaceParent.Flag.UseMapData,
            },
            Climate = new FormLinkNullable<IClimateGetter>(climateKey),
            LandDefaults = new WorldspaceLandDefaults
            {
                DefaultLandHeight = config.FloorZ,
                DefaultWaterHeight = NoWaterHeight,
            },
            Flags = Worldspace.Flag.NoLodWater,
            ObjectBoundsMin = new P2Float(-config.CellRadius, -config.CellRadius),
            ObjectBoundsMax = new P2Float(config.CellRadius + 1, config.CellRadius + 1),
            DistantLodMultiplier = 1f,
        };

        // ---- persistent cell: one named marker per zone ---------------------------
        var topCell = new Cell(mod)
        {
            MajorRecordFlagsRaw = PersistentRecordFlag,
            Grid = new CellGrid { Point = new P2Int(0, 0) },
        };

        var markers = new List<FairWorldMarker>();
        var markerBase = FormKeyHelper.Parse(config.ZoneMarker);
        foreach (var zone in config.Zones)
        {
            var (x, y, heading) = (zone.Marker[0], zone.Marker[1], zone.Marker[2]);
            var marker = Place(mod, markerBase, x, y, config.FloorZ + zone.MarkerHeight, heading);
            marker.EditorID = $"{config.EditorId}{zone.Name}Marker";
            marker.MajorRecordFlagsRaw = PersistentRecordFlag;
            topCell.Persistent.Add(marker);
            markers.Add(new FairWorldMarker(marker.EditorID, marker.FormKey, x, y, heading));
        }

        worldspace.TopCell = topCell;

        // ---- exterior cells, each with its own landscape --------------------------
        // Cells and LAND are allocated before anything placed in them, so their
        // FormIDs do not move whenever the wall or forest changes.
        var grid = new ExteriorCellGrid(worldspace);
        var cells = new Dictionary<(int X, int Y), Cell>();
        var groundTextures = config.Ground.Enabled ? AddGroundTextures(mod, config.Ground, master) : null;
        if (groundTextures is not null)
        {
            plan.UseGround(groundTextures);
        }

        var textures = groundTextures is null ? plan.PaintTextures() : null;
        var maxLayers = 0;
        droppedLayers = 0;

        for (var cy = -config.CellRadius; cy <= config.CellRadius; cy++)
        {
            for (var cx = -config.CellRadius; cx <= config.CellRadius; cx++)
            {
                // No Has Water flag: this world has no water anywhere.
                var cell = new Cell(mod)
                {
                    Grid = new CellGrid { Point = new P2Int(cx, cy) },
                    Flags = 0,
                };

                cell.Landscape = BuildLandscape(mod, plan, textures, cx, cy, ref maxLayers);
                grid.Add(cell, cx, cy);
                cells[(cx, cy)] = cell;
            }
        }

        void Put(PlacedObject placed)
        {
            var pos = placed.Placement!.Position;
            var key = ((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize));
            if (!cells.TryGetValue(key, out var cell))
            {
                throw new InvalidOperationException(
                    $"FairWorld reference at {pos.X:0}, {pos.Y:0} falls outside the generated cells; raise CellRadius.");
            }

            cell.Temporary.Add(placed);
        }

        // ---- the palisade and its gate ---------------------------------------------
        var panelStatic = AddStatic(mod, config.Palisade);
        var gateStatic = AddStatic(mod, config.GatePiece);

        var gatePiece = config.GatePiece;
        var facing = config.Zones.First(z => z.Name == config.GateFacesZone).Marker;
        var gateHeading = MathF.Atan2(facing[0] - config.Gate[0], facing[1] - config.Gate[1]) * 180f / MathF.PI
            + gatePiece.YawOffsetDegrees;
        var gate = Place(
            mod, gateStatic.FormKey, config.Gate[0], config.Gate[1],
            plan.Height(config.Gate[0], config.Gate[1]) - gatePiece.Sink, gateHeading);
        gate.EditorID = $"{config.EditorId}MainGate";
        gate.Scale = gatePiece.Scale;
        Put(gate);

        var panels = plan.WallPanels(config.Palisade, gatePiece.Width * gatePiece.Scale / 2f).ToList();
        foreach (var panel in panels)
        {
            var placed = Place(mod, panelStatic.FormKey, panel.X, panel.Y, panel.Z, panel.Heading);
            placed.Scale = panel.Scale;
            Put(placed);
        }

        if (config.Palisade.ReservedPanels > 0)
        {
            if (panels.Count > config.Palisade.ReservedPanels)
            {
                throw new InvalidOperationException(
                    $"The palisade needs {panels.Count} panels, more than its {config.Palisade.ReservedPanels} reserved FormIDs; every later record would renumber.");
            }

            for (var i = panels.Count; i < config.Palisade.ReservedPanels; i++)
            {
                mod.GetNextFormKey();
            }
        }

        // ---- the main stage ---------------------------------------------------------
        var stage = config.Stage.Enabled ? FairStage.Build(mod, config, plan.Height, Put) : null;

        // ---- project statics, used by market modules as @EditorID -----------------------
        var projectStatics = config.ProjectStatics.ToDictionary(ps => ps.EditorId, ps => AddStatic(mod, ps).FormKey);
        var props = 0;
        if (config.PropManifest.Length > 0)
        {
            foreach (var (name, prop) in LoadPropManifest(config.PropManifest))
            {
                projectStatics[$"Prop{name}"] = AddStatic(mod, prop).FormKey;
                props++;
            }
        }
        FormKey Resolve(string piece) => piece.StartsWith('@')
            ? projectStatics.TryGetValue(piece[1..], out var key)
                ? key
                : throw new InvalidOperationException($"Market piece {piece} is not a configured project static.")
            : FormKeyHelper.Parse(piece);

        // ---- the fair's own face lists -------------------------------------------------
        // Vendors and archers take their looks from vanilla commoner leveled lists. Other
        // mods edit those lists (Dawi NPC Encounters adds its race to the male one), so the
        // fair copies each list's Skyrim.esm entries into a record of its own, which no
        // other mod touches.
        var faceLists = new Dictionary<string, FormKey>();
        FormKey FaceList(string template)
        {
            if (faceLists.TryGetValue(template, out var own))
            {
                return own;
            }

            var key = FormKeyHelper.Parse(template);
            var vanilla = master?.LeveledNpcs.FirstOrDefault(l => l.FormKey == key);
            if (vanilla is null)
            {
                return faceLists[template] = key;
            }

            var copy = vanilla.Duplicate(mod.GetNextFormKey());
            copy.EditorID = $"SkyrimFairFaces{vanilla.EditorID}";

            // Refilled from the face pool: the vanilla "commoner" lists are six bandits a sex.
            if (config.Faces.Enabled && config.Faces.Lists.TryGetValue(template, out var poolId))
            {
                var female = poolId.EndsWith("Female", StringComparison.Ordinal);
                var (entries, perRace, distinct, dropped) = FairFaces.Pool(master!, config.Faces, female, config.Faces.Archives);
                copy.EditorID = poolId;
                copy.Entries = entries.Select(e => new LeveledNpcEntry
                {
                    Data = new LeveledNpcEntryData { Level = 1, Count = 1, Reference = new FormLink<INpcSpawnGetter>(e) },
                }).ToExtendedList();
                Console.WriteLine($"  faces {poolId}: {entries.Count} entries, {distinct} different faces: "
                    + string.Join(", ", perRace.Select(kv => $"{kv.Key} {kv.Value}"))
                    + "; left out: " + string.Join(", ", dropped.Select(kv => $"{kv.Key} {kv.Value}")));
            }

            mod.LeveledNpcs.Add(copy);
            return faceLists[template] = copy.FormKey;
        }

        // ---- festival light towers ------------------------------------------------------
        TowersResult? towers = null;
        if (config.Towers.Enabled && config.Towers.Towers.Count > 0)
        {
            towers = FairTowers.Build(
                mod, config.Towers, AddStatic(mod, config.Towers.Tower), AddStatic(mod, config.Towers.Lantern), plan.Height, Put);
        }

        // ---- the market ---------------------------------------------------------------
        var market = config.Market.Enabled
            ? FairMarket.Build(mod, config, plan.Outside, plan.Height, Put, topCell, PersistentRecordFlag, Resolve,
                towers?.Footprints ?? Array.Empty<(float X, float Y)[]>())
            : null;

        // ---- stall-keepers ------------------------------------------------------------
        VendorsResult? vendors = null;
        if (market is not null && config.Vendors.Enabled)
        {
            vendors = FairVendors.Build(mod, config.Vendors, market.Stalls, plan.Height, npc =>
            {
                var pos = npc.Placement!.Position;
                cells[((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize))].Temporary.Add(npc);
            }, FaceList);
        }

        // ---- archery range ------------------------------------------------------------
        ArcheryResult? archery = null;
        if (config.Archery.Enabled)
        {
            // Targets are persistent, as Castle Dour's are: an archer's linked target in
            // another cell only resolves when the target is a persistent reference.
            void PutPersistent(PlacedObject placed)
            {
                placed.MajorRecordFlagsRaw |= PersistentRecordFlag;
                topCell.Persistent.Add(placed);
            }

            archery = FairArchery.Build(mod, config.Archery, plan.Height, Put, PutPersistent, npc =>
            {
                npc.MajorRecordFlagsRaw |= PersistentRecordFlag;
                topCell.Persistent.Add(npc);
            }, FaceList);
        }

        // ---- invisible walls ------------------------------------------------------------
        var wallBoxes = 0;
        foreach (var wall in config.CollisionWalls)
        {
            var (ax, ay, bx, by) = (wall.From[0], wall.From[1], wall.To[0], wall.To[1]);
            var length = MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            var pieces = Math.Max(1, (int)MathF.Ceiling(length / wall.PieceLength));
            var heading = MathF.Atan2(bx - ax, by - ay);  // along the wall; the box's local Y runs along it
            for (var i = 0; i < pieces; i++)
            {
                var t = (i + 0.5f) / pieces;
                var (cx, cy) = (ax + (bx - ax) * t, ay + (by - ay) * t);
                Put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse("00000021:Skyrim.esm")),
                    Primitive = new PlacedPrimitive
                    {
                        // Half-extents: across, along, up.
                        Bounds = new P3Float(wall.Thickness / 2f, length / pieces / 2f + 4f, wall.Height / 2f),
                        Color = System.Drawing.Color.FromArgb(0, 255, 255, 0),
                        Unknown = 0.15f,
                        Type = PlacedPrimitive.TypeEnum.Box,
                    },
                    Placement = new Placement
                    {
                        Position = new P3Float(cx, cy, plan.Height(cx, cy) + wall.Height / 2f),
                        Rotation = new P3Float(0f, 0f, heading),
                    },
                });
                wallBoxes++;
            }
        }

        // ---- visitors -------------------------------------------------------------------
        CrowdsResult? crowds = null;
        if (config.Crowds.Enabled)
        {
            crowds = FairCrowds.Build(mod, config.Crowds, config.Vendors, market, plan.Height, npc =>
            {
                var pos = npc.Placement!.Position;
                cells[((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize))].Temporary.Add(npc);
            }, FaceList);
        }

        // ---- sound: the stage set and the crowd ambience ------------------------------------
        AudioResult? audio = null;
        if (config.Audio.Enabled)
        {
            // The bards are persistent, as the script plays their instruments.
            audio = FairAudio.Build(mod, config.Audio, master!, worldspace, placed =>
            {
                placed.MajorRecordFlagsRaw |= PersistentRecordFlag;
                topCell.Persistent.Add(placed);
            }, Put, npc =>
            {
                npc.MajorRecordFlagsRaw |= PersistentRecordFlag;
                topCell.Persistent.Add(npc);
            }, config.Vendors, FaceList, archery?.Archers ?? Array.Empty<FormKey>());
        }

        // ---- the worn festival ground ------------------------------------------------------
        // Painted last, from where everything now stands: the LAND records were allocated
        // with the cells (their FormIDs stay put) and only their texture layers are added here.
        if (groundTextures is not null)
        {
            var wear = new List<(float X, float Y, float Radius, float Strength)>();
            foreach (var stall in market?.Stalls ?? Array.Empty<MarketStall>())
            {
                var (fx, fy) = (MathF.Sin(stall.Yaw * Deg), MathF.Cos(stall.Yaw * Deg));
                var front = stall.Depth / 2f + 60f;
                wear.Add((stall.X + fx * front, stall.Y + fy * front, config.Ground.StallRadius + stall.Width * 0.25f, 0.95f));
                wear.Add((stall.X, stall.Y, stall.Width * 0.45f, 0.55f));
                foreach (var (vx, vy) in stall.Vendors)
                {
                    wear.Add((vx, vy, config.Ground.NpcRadius, 0.8f));
                }
            }

            foreach (var f in market?.Footprints.Where(f => f.Kind is not "stall" and not "pole" and not "frontage") ?? Enumerable.Empty<MarketFootprint>())
            {
                wear.Add((f.X, f.Y, MathF.Max(f.HalfW, f.HalfD) + config.Ground.DressingRadius * 0.5f, 0.7f));
            }

            foreach (var (vx, vy) in crowds?.Positions ?? Array.Empty<(float X, float Y)>())
            {
                wear.Add((vx, vy, config.Ground.NpcRadius, 0.9f));
            }

            foreach (var lane in config.Archery.Enabled ? config.Archery.Lanes : new List<ArcheryLane>())
            {
                wear.Add((lane.Archer[0], lane.Archer[1], 170f, 1f));
                wear.Add((lane.Target[0], lane.Target[1], 130f, 0.55f));
            }

            plan.SetWear(wear, config.Market.Lanes, config.Ground.WornZones);
            var painted = plan.PaintTextures();
            foreach (var ((cx, cy), cell) in cells.OrderBy(c => c.Key.Y).ThenBy(c => c.Key.X))
            {
                PaintLayers(cell.Landscape!, plan, painted, cx, cy, ref maxLayers);
            }
        }

        // ---- distant mountains ------------------------------------------------------
        var mountains = new List<MountainPlacement>();
        var lowest = new Dictionary<FormKey, float>();
        var largeReferences = new List<(PlacedObject Ref, MountainPlacement Mountain)>();
        float LowestPoint(FormKey key) => lowest.TryGetValue(key, out var z)
            ? z
            : throw new InvalidOperationException($"Mountain {key} is not a STAT in Skyrim.esm.");

        // Large references, as vanilla's mountains are: ordinary references in their own
        // cells, listed in the world's RNAM so they load five cells out (see
        // MountainsConfig). Each must stay inside the large-reference grid from anywhere in
        // the compound.
        void PlaceMountains(IEnumerable<MountainPlacement> placements)
        {
            var worldMin = config.CellRadius * -CellSize;
            var worldMax = (config.CellRadius + 1) * CellSize;
            var limit = config.Mountains.LargeReferenceCellLimit;
            foreach (var mountain in placements)
            {
                // Vanilla keeps its always-drawn scenery inside the world's object bounds.
                if (mountain.X < worldMin || mountain.X >= worldMax || mountain.Y < worldMin || mountain.Y >= worldMax)
                {
                    throw new InvalidOperationException(
                        $"Mountain at {mountain.X:0}, {mountain.Y:0} falls outside the world's bounds; reduce its row radius.");
                }

                var placed = Place(mod, mountain.Base, mountain.X, mountain.Y, mountain.Z, mountain.Heading);
                placed.Scale = mountain.Scale;
                if (config.Mountains.LargeReferences)
                {
                    var (mx, my) = ((int)MathF.Floor(mountain.X / CellSize), (int)MathF.Floor(mountain.Y / CellSize));
                    if (Math.Abs(mx) > limit || Math.Abs(my) > limit)
                    {
                        throw new InvalidOperationException(
                            $"Mountain {mountain.Name} ({mountain.Row}) at {mountain.X:0}, {mountain.Y:0} is in cell {mx}, {my}, " +
                            $"beyond the large-reference grid's reach (cells -{limit}..{limit}); reduce its row radius.");
                    }

                    Put(placed);
                    largeReferences.Add((placed, mountain));
                }
                else
                {
                    placed.MajorRecordFlagsRaw = PersistentRecordFlag | FullLodRecordFlag;
                    topCell.Persistent.Add(placed);
                }
            }
        }

        if (config.Mountains.Enabled)
        {
            if (master is null)
            {
                throw new InvalidOperationException(
                    "FairWorld mountains are sunk by their mesh bounds, which are read from Skyrim.esm; " +
                    "set Site.SkyrimDataPath or disable fairWorld.mountains.");
            }

            lowest = master.Statics.ToDictionary(r => r.FormKey, r => (float)r.ObjectBounds.First.Z);
            mountains = plan.Mountains(config.Mountains, LowestPoint, placeLast: false).ToList();
            PlaceMountains(mountains);
        }

        // ---- forest backdrop ---------------------------------------------------------
        var trees = config.Forest.Enabled ? plan.ForestTrees(config.Forest, gateHeading).ToList() : new List<TreePlacement>();
        foreach (var tree in trees)
        {
            var placed = Place(mod, tree.Base, tree.X, tree.Y, tree.Z, tree.Heading);
            placed.Placement!.Rotation = new P3Float(
                tree.LeanX * MathF.PI / 180f, tree.LeanY * MathF.PI / 180f, tree.Heading * MathF.PI / 180f);
            placed.Scale = tree.Scale;
            Put(placed);
        }

        // ---- the archers' own training package ---------------------------------------------
        // Vanilla's shoots only while the player is within its trigger radius (1,250) of the
        // archer; otherwise it drops into a Wait that never ends, so an archer loaded with
        // the player further off stands idle for good. The fair's copy covers the whole
        // world. Made last so no other record's FormID moves.
        if (archery is not null && config.Archery.TriggerRadius > 0)
        {
            var source = master?.Packages.FirstOrDefault(x => x.FormKey == FormKeyHelper.Parse(config.Archery.Package))
                ?? throw new InvalidOperationException("fairWorld.archery.triggerRadius needs Skyrim.esm to copy the training package from.");
            var package = source.Duplicate(mod.GetNextFormKey());
            package.EditorID = $"{config.Archery.EditorIdPrefix}TrainingPackage";
            ((PackageDataInt)package.Data[config.Archery.TriggerRadiusInput]).Data = (uint)config.Archery.TriggerRadius;

            // "Use Weapon Location": vanilla's is near the unkeyed linked ref (the stand),
            // within 32, and the package's first step is a Travel there. After a load the
            // package resumes in that Travel, which needs a path; with no navmesh it never
            // arrives, so the archer never shoots (read in game: the package is running,
            // nothing happens). Near self, as the package's own weapon search location is,
            // the archer is always there, so the Travel completes with or without navmesh.
            var useAt = (PackageDataLocation)package.Data[config.Archery.UseWeaponLocationInput];
            var nearSelf = ((PackageDataLocation)package.Data[config.Archery.SearchLocationInput]).Location!.DeepCopy();
            nearSelf.Radius = (uint)config.Archery.UseWeaponRadius;
            useAt.Location = nearSelf;
            mod.Packages.Add(package);

            // A hold package above it, live only while SkyrimFairArcherHold is 1. After a
            // load the training package resumes wherever it stood (a Travel the archer
            // can't finish without navmesh), and EvaluatePackage keeps a package that is
            // already running. So the stage script sets the global, lets the archers switch
            // to holding, then clears it: the training package starts again from the top.
            var hold = new GlobalFloat(mod) { EditorID = $"{config.Archery.EditorIdPrefix}Hold", Data = 0f };
            mod.Globals.Add(hold);
            var holdSource = master!.Packages.First(x => x.FormKey == FormKeyHelper.Parse(config.Archery.HoldPackage));
            var holdPackage = holdSource.Duplicate(mod.GetNextFormKey());
            holdPackage.EditorID = $"{config.Archery.EditorIdPrefix}HoldPackage";
            var onHold = new GetGlobalValueConditionData();
            onHold.Global.Link.SetTo(hold.FormKey);
            holdPackage.Conditions.Add(new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = onHold });
            mod.Packages.Add(holdPackage);

            // The hold package is kept (its FormID, and the global's, are in saves) but no
            // longer given to the archers: switching to it and back didn't restart the
            // training package (read in game: they stayed on the hold package).
            foreach (var npc in archery.ArcherRecords)
            {
                npc.Packages.Clear();
                npc.Packages.Add(new FormLink<IPackageGetter>(package.FormKey));
            }

            if (audio is not null)
            {
                var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
                script.Properties.Add(new ScriptObjectProperty { Name = "ArcherHold", Object = new FormLink<ISkyrimMajorRecordGetter>(hold.FormKey) });
            }
        }

        // ---- the backdrop's later layers ----------------------------------------------------
        // The treeline and the rows marked PlaceLast come after every other record, so
        // adding or retuning them never moves the FormIDs before them.
        if (config.Mountains.Enabled && master is not null)
        {
            var late = plan.Mountains(config.Mountains, LowestPoint, placeLast: true).ToList();
            PlaceMountains(late);
            mountains.AddRange(late);
        }

        if (config.Treeline.Enabled)
        {
            var treeline = plan.ForestTrees(config.Treeline, gateHeading).ToList();
            foreach (var tree in treeline)
            {
                var placed = Place(mod, tree.Base, tree.X, tree.Y, tree.Z, tree.Heading);
                placed.Placement!.Rotation = new P3Float(
                    tree.LeanX * MathF.PI / 180f, tree.LeanY * MathF.PI / 180f, tree.Heading * MathF.PI / 180f);
                placed.Scale = tree.Scale;
                Put(placed);
            }

            trees.AddRange(treeline);
        }

        // ---- the rest of the orchestra and the crowd tiers, after everything else -----------------
        void PutPersistentNpc(PlacedNpc npc)
        {
            npc.MajorRecordFlagsRaw |= PersistentRecordFlag;
            topCell.Persistent.Add(npc);
        }

        void PutTemporaryNpc(PlacedNpc npc)
        {
            var pos = npc.Placement!.Position;
            cells[((int)MathF.Floor(pos.X / CellSize), (int)MathF.Floor(pos.Y / CellSize))].Temporary.Add(npc);
        }

        if (audio is not null && config.Audio.Stage.Orchestra.Count > 0)
        {
            var players = FairAudio.BuildOrchestra(mod, config.Audio, config.Vendors, FaceList, PutPersistentNpc, audio.Quest);
            Console.WriteLine($"  orchestra: {config.Audio.Stage.Band.Count + players} bards on the deck");
        }

        if (crowds is not null && config.Crowds.Tiers.Count > 0 && audio is not null)
        {
            var crowdMarker = config.Zones.First(z => z.Name == "Crowd").Marker;

            // The seats: every placed bench and stool the crowds may use, by the dressing
            // module it stands in (the first footprint of a seated kind containing it).
            var seatBases = config.Crowds.Seats.ToDictionary(kv => FormKeyHelper.Parse(kv.Key), kv => kv.Value);
            var seatKinds = config.Crowds.Tiers.SelectMany(t => t.Seats).Select(g => g.Near).ToHashSet(StringComparer.Ordinal);
            var seats = new List<CrowdSeat>();
            foreach (var cell in cells.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y))
            {
                foreach (var seat in cell.Value.Temporary.OfType<PlacedObject>())
                {
                    if (!seatBases.TryGetValue(seat.Base.FormKey, out var n))
                    {
                        continue;
                    }

                    var sp = seat.Placement!.Position;
                    for (var fi = 0; fi < crowds.Blocked.Count; fi++)
                    {
                        var f = crowds.Blocked[fi];
                        if (seatKinds.Contains(f.Kind) && f.Contains(sp.X, sp.Y, 10f))
                        {
                            seats.Add(new CrowdSeat(seat, n, fi, f));
                            break;
                        }
                    }
                }
            }

            var quietSource = config.Crowds.QuietPackage.Length > 0
                ? master?.Packages.FirstOrDefault(x => x.FormKey == FormKeyHelper.Parse(config.Crowds.QuietPackage))
                : null;
            var tiers = FairCrowds.BuildTiers(mod, config.Crowds, config.Vendors, crowds, plan.Height, placed =>
            {
                placed.MajorRecordFlagsRaw |= PersistentRecordFlag;
                topCell.Persistent.Add(placed);
            }, PutTemporaryNpc, PutPersistentNpc, (crowdMarker[0], crowdMarker[1], plan.Height(crowdMarker[0], crowdMarker[1]) - 200f),
                seats, quietSource, FolkKeepClear());
            var tierGlobal = new GlobalFloat(mod) { EditorID = config.Crowds.TierGlobal, Data = config.Crowds.Tiers.Count };
            mod.Globals.Add(tierGlobal);
            var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
            ScriptObjectProperty Obj(FormKey key) => new() { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(key) };
            script.Properties.Add(new ScriptObjectListProperty { Name = "CrowdLayers", Objects = tiers.Markers.Select(Obj).ToExtendedList() });
            script.Properties.Add(new ScriptObjectProperty { Name = "CrowdLayer", Object = new FormLink<ISkyrimMajorRecordGetter>(tierGlobal.FormKey) });
            script.Properties.Add(new ScriptObjectListProperty { Name = "Dancers", Objects = tiers.Dancers.Select(Obj).ToExtendedList() });
            script.Properties.Add(new ScriptObjectListProperty
            {
                Name = "DanceIdles",
                Objects = config.Crowds.DanceIdles.Select(i => Obj(FormKeyHelper.Parse(i))).ToExtendedList(),
            });
            script.Properties.Add(new ScriptFloatListProperty { Name = "DanceLengths", Data = config.Crowds.DanceLengths.ToExtendedList() });
            script.Properties.Add(new ScriptObjectListProperty
            {
                Name = "CheerIdles",
                Objects = config.Crowds.CheerIdles.Select(i => Obj(FormKeyHelper.Parse(i))).ToExtendedList(),
            });
            Console.WriteLine($"  crowd layers ({config.Crowds.TierGlobal}, all on; {seats.Count} seats, {tiers.Dancers.Count} dancers): "
                + string.Join(", ", tiers.Tiers.Select(t => $"{t.Tier} {t.Count}")));
        }

        // ---- the large-reference table (RNAM) -------------------------------------------------
        // Laid out as vanilla Tamriel's (read back with Mutagen): each reference is listed
        // under every cell its footprint overlaps, and both the group's key and the entry
        // hold a cell as (Y, X) in Mutagen's naming.
        if (largeReferences.Count > 0)
        {
            var bounds = master!.Statics.ToDictionary(r => r.FormKey, r => r.ObjectBounds);
            var groups = new SortedDictionary<(int Y, int X), List<(uint Id, FormKey Key, int CellX, int CellY)>>();
            foreach (var (placed, m) in largeReferences)
            {
                var b = bounds[m.Base];
                var reach = m.Scale * MathF.Sqrt(
                    MathF.Max(b.First.X * b.First.X, b.Second.X * b.Second.X) + MathF.Max(b.First.Y * b.First.Y, b.Second.Y * b.Second.Y));
                var (cellX, cellY) = ((int)MathF.Floor(m.X / CellSize), (int)MathF.Floor(m.Y / CellSize));
                for (var gx = (int)MathF.Floor((m.X - reach) / CellSize); gx <= (int)MathF.Floor((m.X + reach) / CellSize); gx++)
                {
                    for (var gy = (int)MathF.Floor((m.Y - reach) / CellSize); gy <= (int)MathF.Floor((m.Y + reach) / CellSize); gy++)
                    {
                        if (Math.Abs(gx) > config.CellRadius || Math.Abs(gy) > config.CellRadius)
                        {
                            continue;
                        }

                        if (!groups.TryGetValue((gy, gx), out var list))
                        {
                            groups[(gy, gx)] = list = new();
                        }

                        list.Add((placed.FormKey.ID, placed.FormKey, cellX, cellY));
                    }
                }
            }

            worldspace.LargeReferences.Clear();
            foreach (var ((gy, gx), list) in groups)
            {
                var group = new WorldspaceGridReference { GridPosition = new P2Int16((short)gy, (short)gx) };
                foreach (var (_, key, cellX, cellY) in list.OrderBy(e => e.Id))
                {
                    group.References.Add(new WorldspaceReference
                    {
                        Reference = new FormLink<IPlacedObjectGetter>(key),
                        Position = new P2Int16((short)cellY, (short)cellX),
                    });
                }

                worldspace.LargeReferences.Add(group);
            }
        }

        // The folk pair's circle, kept clear of the crowd layers: its centre and a ring round it
        // (the placement keeps 55 from anyone standing, so this clears about 115 round the centre).
        List<(float X, float Y)> FolkKeepClear()
        {
            var clear = new List<(float X, float Y)>();
            if (config.FolkDance.Enabled && config.FolkDance.Centre.Length >= 2)
            {
                var (cx, cy) = (config.FolkDance.Centre[0], config.FolkDance.Centre[1]);
                clear.Add((cx, cy));
                for (var k = 0; k < 8; k++)
                {
                    clear.Add((cx + 60f * MathF.Sin(k * MathF.PI / 4f), cy + 60f * MathF.Cos(k * MathF.PI / 4f)));
                }
            }

            return clear;
        }

        var folkOar = new List<(Npc Npc, string Submod, string Folder)>();

        // ---- Astra's folk dance: a pair, each with its own clip through OAR ---------------------
        // Made before the guard, which then covers them like every other fair NPC.
        if (config.FolkDance.Enabled && audio is not null)
        {
            var folk = config.FolkDance;
            var folkDancers = new List<FormKey>();
            var oar = Path.IsPathRooted(folk.OarFolder) ? folk.OarFolder : Path.Combine(FairPaths.ConfigDirectory, folk.OarFolder);
            var json = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            Directory.CreateDirectory(oar);
            File.WriteAllText(Path.Combine(oar, "config.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                name = "Skyrim Fair folk dance",
                author = "Astra (animation), Skyrim Fair",
                description = "The fair's folk dancers only: each plays its half of the paired dance in place of the Cicero dance.",
            }, json));
            var turn = folk.Heading * MathF.PI / 180f;
            foreach (var dancer in folk.Dancers)
            {
                var look = mod.Npcs.First(n => n.EditorID == dancer.Look);
                var npc = look.Duplicate(mod.GetNextFormKey());
                npc.EditorID = $"SkyrimFairFolkDancer{dancer.Submod}";
                npc.Name = folk.Name;
                mod.Npcs.Add(npc);

                var (u, v) = (dancer.At[0], dancer.At[1]);
                var (x, y) = (folk.Centre[0] + u * MathF.Cos(turn) + v * MathF.Sin(turn), folk.Centre[1] - u * MathF.Sin(turn) + v * MathF.Cos(turn));
                var placed = new PlacedNpc(mod)
                {
                    EditorID = $"{npc.EditorID}Ref",
                    Base = new FormLinkNullable<INpcGetter>(npc.FormKey),
                    Placement = new Placement
                    {
                        Position = new P3Float(x, y, plan.Height(x, y) + 2f),
                        Rotation = new P3Float(0f, 0f, (folk.Heading + dancer.At[2]) * MathF.PI / 180f),
                    },
                };
                PutPersistentNpc(placed);
                folkDancers.Add(placed.FormKey);

                // OAR's condition is written at the end, with this dancer's own keyword.
                folkOar.Add((npc, dancer.Submod, oar));
            }

            var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
            script.Properties.Add(new ScriptObjectListProperty
            {
                Name = "FolkDancers",
                Objects = folkDancers.Select(k => new ScriptObjectProperty { Name = "", Object = new FormLink<ISkyrimMajorRecordGetter>(k) }).ToExtendedList(),
            });
            script.Properties.Add(new ScriptObjectProperty { Name = "FolkIdle", Object = new FormLink<ISkyrimMajorRecordGetter>(FormKeyHelper.Parse(folk.Idle)) });
            script.Properties.Add(new ScriptFloatProperty { Name = "FolkClipLength", Data = folk.Length });
            Console.WriteLine($"  folk dance: {folkDancers.Count} dancers at ({folk.Centre[0]}, {folk.Centre[1]}); OAR conditions in {folk.OarFolder}");
        }

        // ---- the NPC guard: invulnerable, and other mods' spells taken off ----------------------
        // Records are only changed here, except the one FormList, so nothing earlier moves.
        if (config.NpcGuard.Enabled && audio is not null)
        {
            var strip = new FormList(mod) { EditorID = "SkyrimFairStripSpells" };
            mod.FormLists.Add(strip);
            foreach (var npc in mod.Npcs)
            {
                if (config.NpcGuard.Invulnerable)
                {
                    npc.Configuration.Flags |= NpcConfiguration.Flag.Invulnerable;
                }

                if (config.NpcGuard.Script.Length == 0)
                {
                    continue;
                }

                npc.VirtualMachineAdapter ??= new VirtualMachineAdapter();
                npc.VirtualMachineAdapter.Scripts.Add(new ScriptEntry
                {
                    Name = config.NpcGuard.Script,
                    Properties = { new ScriptObjectProperty { Name = "StripSpells", Object = new FormLink<ISkyrimMajorRecordGetter>(strip.FormKey) } },
                });
            }

            // The stage quest looks the spells up by plugin (no master needed) and fills the list.
            var guard = config.NpcGuard.StripSpells.Select(s => s.Split('|')).ToList();
            var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
            script.Properties.Add(new ScriptObjectProperty { Name = "StripSpells", Object = new FormLink<ISkyrimMajorRecordGetter>(strip.FormKey) });
            script.Properties.Add(new ScriptStringListProperty { Name = "StripPlugins", Data = guard.Select(g => g[0]).ToExtendedList() });
            script.Properties.Add(new ScriptIntListProperty { Name = "StripIds", Data = guard.Select(g => Convert.ToInt32(g[1], 16)).ToExtendedList() });
            Console.WriteLine($"  NPC guard: {mod.Npcs.Count} NPC records{(config.NpcGuard.Invulnerable ? " invulnerable" : "")}, "
                + $"{guard.Count} spells stripped when their plugin is loaded");
        }

        var seatDefs = config.SeatMarkers.ToDictionary(m => FormKeyHelper.Parse(m.Furniture));

        // ---- the sites tools/place_crowd.py plans the crowd figures from ------------------------
        if (config.CrowdSitesDump.Length > 0)
        {
            WriteCrowdSites();
        }

        // ---- static crowd figures (docs/CROWD.md), after everything but the navmesh ------------------
        // In placement order: a figure's STAT at its first placement, then each copy followed by
        // its invisible collision box (the invisible walls' kind). Append-only, so nothing moves.
        var figureDefs = config.CrowdFigures.ToDictionary(f => f.EditorId, StringComparer.Ordinal);
        var figureStats = new Dictionary<string, Static>(StringComparer.Ordinal);
        var figureCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var nextFigureId = config.CrowdFormIdBase;
        FormKey FigureKey() => new(mod.ModKey, nextFigureId++);
        var legacy = config.CrowdFigures.SelectMany(f => f.Places.Select(at => new CrowdPlacement { Figure = f.EditorId, At = at }));
        foreach (var placement in config.CrowdFiguresEnabled ? legacy.Concat(config.CrowdPlacements) : Enumerable.Empty<CrowdPlacement>())
        {
            var figure = figureDefs.TryGetValue(placement.Figure, out var def)
                ? def
                : throw new InvalidOperationException($"crowdPlacements: no crowdFigures entry {placement.Figure}");
            if (!figureStats.TryGetValue(figure.EditorId, out var stat))
            {
                figureStats[figure.EditorId] = stat = AddStatic(mod, figure, FigureKey());
            }

            var at = placement.At;
            if (placement.Seat.Length >= 2)
            {
                at = SeatPlace(placement);
            }

            var ground = plan.Height(at[0], at[1]);
            Put(new PlacedObject(FigureKey(), SkyrimRelease.SkyrimSE)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(stat.FormKey),
                Placement = new Placement { Position = new P3Float(at[0], at[1], ground), Rotation = new P3Float(0f, 0f, at[2] * MathF.PI / 180f) },
            });
            figureCounts[figure.EditorId] = figureCounts.GetValueOrDefault(figure.EditorId) + 1;
            if (!figure.Solid)
            {
                continue;
            }

            var box = figure.Collision.Length == 5
                ? figure.Collision
                : new[] { -0.4f * figure.Width, -0.4f * figure.Depth, 0.4f * figure.Width, 0.4f * figure.Depth, figure.Height };
            // The box's middle, turned into the world as the figure is (yaw clockwise from +Y).
            var (u, v) = ((box[0] + box[2]) / 2f, (box[1] + box[3]) / 2f);
            var yaw = at[2] * MathF.PI / 180f;
            var (x, y) = (at[0] + u * MathF.Cos(yaw) + v * MathF.Sin(yaw), at[1] - u * MathF.Sin(yaw) + v * MathF.Cos(yaw));
            Put(new PlacedObject(FigureKey(), SkyrimRelease.SkyrimSE)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse("00000021:Skyrim.esm")),
                Primitive = new PlacedPrimitive
                {
                    // Half-extents: across, along (the figure's +Y), up.
                    Bounds = new P3Float((box[2] - box[0]) / 2f, (box[3] - box[1]) / 2f, box[4] / 2f),
                    Color = System.Drawing.Color.FromArgb(0, 255, 255, 0),
                    Unknown = 0.15f,
                    Type = PlacedPrimitive.TypeEnum.Box,
                },
                Placement = new Placement
                {
                    Position = new P3Float(x, y, ground + box[4] / 2f),
                    Rotation = new P3Float(0f, 0f, yaw),
                },
            });
        }

        if (figureCounts.Count > 0)
        {
            Console.WriteLine($"  crowd figures: {figureCounts.Values.Sum()} placed, {figureCounts.Count} figures: "
                + string.Join(", ", figureCounts.Select(kv => $"{kv.Key["SkyrimFairCrowd".Length..]} {kv.Value}")));
        }

        // A seated figure sits on a seat marker of the furniture reference nearest its seat,
        // facing as the sitter would; the furniture becomes its non-sittable twin.
        float[] SeatPlace(CrowdPlacement placement)
        {
            var (sx, sy) = (placement.Seat[0], placement.Seat[1]);
            var seat = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedObject>())
                .Where(o => seatDefs.ContainsKey(o.Base.FormKey))
                .OrderBy(o => (o.Placement!.Position.X - sx) * (o.Placement.Position.X - sx) + (o.Placement.Position.Y - sy) * (o.Placement.Position.Y - sy))
                .First();
            var sp = seat.Placement!;
            var d = MathF.Sqrt((sp.Position.X - sx) * (sp.Position.X - sx) + (sp.Position.Y - sy) * (sp.Position.Y - sy));
            if (d > 40f)
            {
                throw new InvalidOperationException($"crowdPlacements {placement.Figure}: no seat within 40 of ({sx}, {sy}); the nearest is {d:0} away");
            }

            var sitters = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedNpc>())
                .Concat(topCell.Persistent.OfType<PlacedNpc>())
                .Count(n => n.LinkedReferences.Any(l => l.Reference.FormKey == seat.FormKey));
            if (sitters > 0)
            {
                throw new InvalidOperationException($"crowdPlacements {placement.Figure}: the seat at ({sx}, {sy}) is a real sitter's");
            }

            var markers = seatDefs[seat.Base.FormKey];
            var m = markers.Markers[placement.Marker];
            var syaw = sp.Rotation.Z;
            var (mx, my) = (sp.Position.X + m[0] * MathF.Cos(syaw) + m[1] * MathF.Sin(syaw), sp.Position.Y - m[0] * MathF.Sin(syaw) + m[1] * MathF.Cos(syaw));
            seat.Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(markers.StaticTwin));
            return new[] { mx, my, (syaw * 180f / MathF.PI + m[2] + 360f) % 360f };
        }

        void WriteCrowdSites()
        {
            var linked = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedNpc>()).Concat(topCell.Persistent.OfType<PlacedNpc>())
                .SelectMany(n => n.LinkedReferences.Select(l => l.Reference.FormKey)).ToHashSet();
            var objects = cells.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y).SelectMany(c => c.Value.Temporary.OfType<PlacedObject>()).ToList();
            var seatsOut = objects.Where(o => seatDefs.ContainsKey(o.Base.FormKey)).Select(o => new
            {
                furniture = o.Base.FormKey.ToString(),
                x = o.Placement!.Position.X, y = o.Placement.Position.Y, yaw = o.Placement.Rotation.Z * 180f / MathF.PI,
                taken = linked.Contains(o.FormKey),
            });
            var rails = objects.Where(o => o.Base.FormKey == FormKeyHelper.Parse("0006EAAD:Skyrim.esm")).Select(o => new
            {
                x = o.Placement!.Position.X, y = o.Placement.Position.Y, yaw = o.Placement.Rotation.Z * 180f / MathF.PI,
            });
            var actors = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedNpc>()).Concat(topCell.Persistent.OfType<PlacedNpc>())
                .Where(n => (n.MajorRecordFlagsRaw & 0x800) == 0)
                .Select(n => new[] { n.Placement!.Position.X, n.Placement.Position.Y })
                .OrderBy(p => p[0]).ThenBy(p => p[1]);
            var path = Path.IsPathRooted(config.CrowdSitesDump) ? config.CrowdSitesDump : Path.Combine(FairPaths.ConfigDirectory, config.CrowdSitesDump);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new { seats = seatsOut, rails, actors }));
        }

        // ---- SkyrimFairAtFair: 1 while the player is at the fair ----------------------------------
        // The stage script keeps it; the compatibility patches switch other mods' per-NPC
        // spells off while it's 1. It's saved, so a save loaded at the fair starts with it on.
        if (audio is not null && config.AtFairGlobal.Length > 0)
        {
            var atFair = new GlobalFloat(mod) { EditorID = config.AtFairGlobal, Data = 0f };
            mod.Globals.Add(atFair);
            AtFairGlobal = atFair.FormKey;
            var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
            script.Properties.Add(new ScriptObjectProperty { Name = "AtFair", Object = new FormLink<ISkyrimMajorRecordGetter>(atFair.FormKey) });
        }

        // ---- the stage singers (docs/BARDS.md) --------------------------------------------------
        if (config.Singers.Enabled && audio is not null && master is not null)
        {
            var (singers, lines, files) = FairSingers.Build(mod, config.Singers, config.Audio, config.Vendors, master, audio.Quest, PutPersistentNpc);
            Console.WriteLine($"  singers: {singers} on the deck, {lines} sung lines, {files} voice files");
        }

        // ---- fixed faces for the band and the folk pair (their animations want a human skeleton) ----
        // Only the records' template changes, so nothing renumbers.
        if (config.Faces.Enabled && config.Faces.Fixed.Count > 0 && master is not null)
        {
            Console.WriteLine($"  faces: {FairFaces.Fixed(master, config.Faces, mod.Npcs)} records given a fixed face "
                + $"({string.Join(", ", config.Faces.Fixed.Select(f => f.Prefix))})");
        }

        // ---- SkyrimFairNPC: a keyword on every fair NPC, for other mods' SPID exclusions ----------
        // SPID gives spells to the runtime copies of templated NPCs (FF...), which don't belong to
        // SkyrimFair.esp, so a plugin filter misses them; the keyword is carried over. Built last.
        if (config.NpcKeyword.Length > 0)
        {
            var keyword = new Keyword(mod) { EditorID = config.NpcKeyword };
            mod.Keywords.Add(keyword);
            foreach (var npc in mod.Npcs)
            {
                npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
                npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            }

            Console.WriteLine($"  keyword {config.NpcKeyword} on {mod.Npcs.Count} NPC records");
        }

        // ---- SkyrimFairAudioFirstTrack: a test switch for the song to start with -------------
        if (audio is not null)
        {
            var first = new GlobalFloat(mod) { EditorID = "SkyrimFairAudioFirstTrack", Data = -1f };
            mod.Globals.Add(first);
            var script = mod.Quests.First(q => q.FormKey == audio.Quest).VirtualMachineAdapter!.Scripts[0];
            script.Properties.Add(new ScriptObjectProperty { Name = "FirstTrack", Object = new FormLink<ISkyrimMajorRecordGetter>(first.FormKey) });
        }

        // ---- the folk dancers' OAR conditions, on a keyword each ------------------------------
        // Not IsActorBase: the folk dancers are templated, so in game they run on runtime
        // copies (FF...) of their records, and a base-record condition never matched (SPID's
        // log shows their bases as FF0021C1 and FF0012B0). The copies keep their record's
        // keywords. Built last, so nothing renumbers.
        foreach (var (npc, submod, folder) in folkOar)
        {
            var keyword = new Keyword(mod) { EditorID = $"{npc.EditorID}" };
            mod.Keywords.Add(keyword);
            npc.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            npc.Keywords.Add(new FormLink<IKeywordGetter>(keyword.FormKey));
            var sub = Path.Combine(folder, submod);
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "config.json"), System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["name"] = $"Folk dancer ({submod})",
                ["description"] = $"{npc.EditorID}: Astra's folk dance in place of the Cicero dance.",
                ["priority"] = 1900000000,
                ["conditions"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["condition"] = "HasKeyword",
                        ["requiredVersion"] = "1.0.0.0",
                        ["Keyword"] = new Dictionary<string, object>
                        {
                            ["form"] = new Dictionary<string, string>
                            {
                                ["pluginName"] = mod.ModKey.FileName,
                                ["formID"] = keyword.FormKey.ID.ToString("X"),
                            },
                        },
                    },
                },
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        // ---- late dressing: after every other record, so nothing before it renumbers -------------
        if (market is not null && config.Market.LateDressing.Count > 0)
        {
            var actors = cells.Values.SelectMany(c => c.Temporary.OfType<PlacedNpc>())
                .Concat(topCell.Persistent.OfType<PlacedNpc>())
                .Select(n => (n.Placement!.Position.X, n.Placement.Position.Y))
                .ToList();
            var late = market.PlaceLate(actors);
            Console.WriteLine($"  late dressing: {late.Count(p => p is not null)} of {late.Count} placed"
                + string.Concat(late.Select((p, i) => p is { } q ? "" : $"; refused {config.Market.LateDressing[i].Module} at ({config.Market.LateDressing[i].X:0}, {config.Market.LateDressing[i].Y:0})")));
        }

        // The mod's own counter must stay below the crowd figures' range.
        var counter = mod.ModHeader.Stats.NextFormID;
        if (counter >= config.CrowdFormIdBase)
        {
            throw new InvalidOperationException($"FormIDs reached the crowd figures' range (0x{config.CrowdFormIdBase:X}): raise crowdFormIdBase");
        }

        // ---- the navmesh, last of all ------------------------------------------------------------
        if (config.Navmesh.Enabled && master is not null)
        {
            // The stage's deck and the ramp up its steps are raised navmesh; its own deck,
            // treads, risers and skirt carry them rather than blocking them.
            var platforms = new List<NavPlatform>();
            var platformPieces = new HashSet<FormKey>();
            if (stage is not null)
            {
                var (fx, fy) = (stage.DeckFront.X - stage.CentreX, stage.DeckFront.Y - stage.CentreY);
                var fl = MathF.Sqrt(fx * fx + fy * fy);
                var f = (X: fx / fl, Y: fy / fl);
                var r = (X: f.Y, Y: -f.X);
                var m = config.Navmesh.PlatformMargin;
                var (hw, hd) = (stage.DeckWidth / 2f, stage.DeckDepth / 2f);
                var deckTop = stage.GroundZ + stage.DeckHeight;
                var treads = Math.Max(1, (int)MathF.Round(stage.DeckHeight / stage.Riser) - 1);
                var ramp = stage.StairRun / treads * (treads + 1);
                var (scx, scy) = (stage.CentreX, stage.CentreY);
                platforms.Add(new NavPlatform("deck", scx, scy, r, f, -hw + m, hw - m, -hd + m, hd - m,
                    (_, _) => deckTop, config.Navmesh.MinObstacleHeight));
                platforms.Add(new NavPlatform("steps", scx, scy, r, f, -stage.StairWidth / 2f + m, stage.StairWidth / 2f - m, hd - m, hd + ramp,
                    (x, y) =>
                    {
                        var v = (x - scx) * f.X + (y - scy) * f.Y;
                        return stage.GroundZ + stage.DeckHeight * Math.Clamp(1f - (v - hd) / ramp, 0f, 1f);
                    },
                    config.Navmesh.StepTolerance));
                foreach (var piece in new[] { config.Stage.Deck.Piece, config.Stage.Steps.Piece, config.Stage.Steps.RiserPiece, config.Stage.Skirt.Piece })
                {
                    platformPieces.Add(FormKeyHelper.Parse(piece));
                }
            }

            var nav = FairNavmesh.Build(mod, config.Navmesh, worldspace, cells, topCell, plan.Bounds, plan.Outside, plan.Height, master,
                platforms, platformPieces);
            Console.WriteLine($"  navmesh: {nav.FromFootprints} obstacles cut by their model's footprint, the rest by bounds ({nav.Models} models listed)");
            Console.WriteLine($"  navmesh: {nav.Meshes} meshes, {nav.Triangles} triangles, {nav.ExternalLinks} links across cell lines; " +
                $"{nav.Obstacles} obstacles cut; {nav.Islands} areas, {nav.KeptIslands} kept (the main one, actors' pockets, the stage); actors on the mesh {nav.ActorsOnMesh} of {nav.Actors}");
            foreach (var c in nav.Cells)
            {
                Console.WriteLine($"    cell {c.X,2}, {c.Y,2}: {c.Vertices,5} vertices, {c.Triangles,5} triangles, {c.Links,4} links out");
            }

            if (nav.UnknownBases.Count > 0)
            {
                Console.WriteLine($"    bases without readable bounds (default footprint): {string.Join(", ", nav.UnknownBases)}");
            }
        }

        // ---- the crowd switched off where it can't be seen, after everything is placed ----------
        // Adds only script properties and one global; the actors keep their FormIDs.
        if (config.CrowdCulling.Enabled && audio is not null && master is not null)
        {
            var cull = FairVisibility.Build(mod, config, worldspace, cells, topCell, PersistentRecordFlag, plan.Bounds, plan.Outside,
                master, mod.Quests.First(q => q.FormKey == audio.Quest));
            Console.WriteLine($"  crowd culling: {cull.Actors} actors switchable, {cull.Objects} objects block sight, {cull.StandingSpots} standing spots; "
                + $"table {cull.Columns} x {cull.Rows} x {cull.Words} = {cull.TableLength}; on at a spot: mean {cull.MeanOn:0}, median {cull.MedianOn}, most {cull.MaxOn}");
            foreach (var (name, on) in cull.At)
            {
                Console.WriteLine($"    {name}: {on} of {cull.Actors} on");
            }
        }

        mod.Worldspaces.Add(worldspace);

        return new FairWorldResult(
            worldspace.FormKey,
            config.EditorId,
            climateKey,
            weatherCount,
            cells.Count,
            config.CellRadius,
            maxLayers,
            markers,
            plan.Bounds,
            new FairWorldWall(
                panelStatic.FormKey, gateStatic.FormKey, gate.FormKey, panels.Count,
                config.Palisade.Width * config.Palisade.Scale, config.Palisade.Height * config.Palisade.Scale,
                gatePiece.Height * gatePiece.Scale, gateHeading),
            trees
                .GroupBy(t => t.Name)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new FairWorldTreeCount(
                    g.Key, g.Count(), g.Min(t => t.Scale), g.Max(t => t.Scale),
                    g.Min(t => t.Distance), g.Max(t => t.Distance)))
                .ToList(),
            mountains,
            stage,
            market,
            vendors,
            archery,
            towers,
            crowds,
            audio,
            wallBoxes,
            props,
            groundTextures?.Count ?? 0,
            plan.RenderPlan(512f, 0f, Array.Empty<TreePlacement>()),
            plan.RenderPlan(1024f, config.Forest.OuterDistance, trees),
            plan.RenderMountains(mountains, 2048f));
    }

    /// <summary>
    /// A STAT for a project mesh, with object bounds taken from its measured size so the
    /// engine culls it correctly.
    /// </summary>
    private static Static AddStatic(SkyrimMod mod, ProjectStaticConfig piece, FormKey? key = null)
    {
        var halfWidth = (short)MathF.Ceiling(piece.Width / 2f);
        var halfDepth = (short)MathF.Ceiling(piece.Depth / 2f);
        var record = new Static(key ?? mod.GetNextFormKey(), SkyrimRelease.SkyrimSE)
        {
            EditorID = piece.EditorId,
            Model = new Model { File = piece.Model },
            ObjectBounds = new ObjectBounds
            {
                First = new P3Int16((short)-halfWidth, (short)-halfDepth, (short)MathF.Floor(piece.MinZ)),
                Second = new P3Int16(halfWidth, halfDepth, (short)MathF.Ceiling(piece.Height)),
            },
        };
        mod.Statics.Add(record);
        return record;
    }

    // ------------------------------------------------------------------------
    // Climate
    // ------------------------------------------------------------------------

    /// <summary>
    /// Tamriel's weather comes from regions, and its own climate lists a single weather,
    /// so inheriting Tamriel's climate would give one sky for ever. Instead the base
    /// climate is copied (sun, moons, sky model, day timings) and handed the tundra
    /// region's weather list, which is what the player sees on the plains at the site.
    /// </summary>
    private static FormKey BuildClimate(
        SkyrimMod mod, FairWorldConfig config, ISkyrimModGetter? master, out int weatherCount)
    {
        var baseKey = FormKeyHelper.Parse(config.BaseClimate);
        var regionKey = FormKeyHelper.Parse(config.WeatherRegion);
        var baseClimate = master?.Climates.FirstOrDefault(c => c.FormKey == baseKey);
        var region = master?.Regions.FirstOrDefault(r => r.FormKey == regionKey);

        if (baseClimate is null || region?.Weather is null)
        {
            // Without the master the vanilla climate is referenced as it stands.
            weatherCount = 0;
            return baseKey;
        }

        var climate = baseClimate.Duplicate(mod.GetNextFormKey());
        climate.EditorID = config.ClimateEditorId;
        climate.WeatherTypes = new ExtendedList<WeatherType>(
            (region.Weather.Weathers ?? Array.Empty<IWeatherTypeGetter>()).Select(w => new WeatherType
            {
                Weather = new FormLink<IWeatherGetter>(w.Weather.FormKey),
                Chance = (int)w.Chance,
                Global = new FormLink<IGlobalGetter>(w.Global.FormKey),
            }));
        mod.Climates.Add(climate);
        weatherCount = climate.WeatherTypes.Count;
        return climate.FormKey;
    }

    // ------------------------------------------------------------------------
    // Landscape
    // ------------------------------------------------------------------------

    private static Landscape BuildLandscape(
        SkyrimMod mod, Plan plan, IReadOnlyList<PaintTexture>? textures, int cx, int cy, ref int maxLayers)
    {
        var originX = cx * CellSize;
        var originY = cy * CellSize;

        // Quantised heights with a one-vertex border, so normals at the cell edge are
        // taken across the seam from the same global function the neighbour uses.
        var q = new int[Points + 2, Points + 2];
        for (var row = -1; row <= Points; row++)
        {
            for (var col = -1; col <= Points; col++)
            {
                q[col + 1, row + 1] = (int)MathF.Round(plan.Height(originX + col * Step, originY + row * Step) / HeightUnit);
            }
        }

        var heightMap = new Array2d<sbyte>(new P2Int(Points, Points), 0);
        var normals = new Array2d<P3UInt8>(new P2Int(Points, Points), default(P3UInt8));
        for (var row = 0; row < Points; row++)
        {
            for (var col = 0; col < Points; col++)
            {
                var here = q[col + 1, row + 1];
                var delta = col == 0
                    ? (row == 0 ? 0 : here - q[1, row])
                    : here - q[col, row + 1];
                if (delta is < sbyte.MinValue or > sbyte.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"FairWorld terrain is too steep at cell {cx},{cy} vertex {col},{row}.");
                }

                heightMap[col, row] = (sbyte)delta;

                var dx = (q[col + 2, row + 1] - q[col, row + 1]) * HeightUnit / (2 * Step);
                var dy = (q[col + 1, row + 2] - q[col + 1, row]) * HeightUnit / (2 * Step);
                var length = MathF.Sqrt(dx * dx + dy * dy + 1f);
                normals[col, row] = new P3UInt8(
                    SignedByte(-dx / length), SignedByte(-dy / length), SignedByte(1f / length));
            }
        }

        var land = new Landscape(mod)
        {
            Flags = LandFlags,
            IsCompressed = true,
            VertexNormals = normals,
            VertexHeightMap = new LandscapeVertexHeightMap
            {
                Offset = q[1, 1],
                HeightMap = heightMap,
            },
        };

        if (textures is not null)
        {
            PaintLayers(land, plan, textures, cx, cy, ref maxLayers);
        }

        return land;
    }

    /// <summary>Base and alpha texture layers of one LAND, quadrant by quadrant.</summary>
    private static void PaintLayers(Landscape land, Plan plan, IReadOnlyList<PaintTexture> textures, int cx, int cy, ref int maxLayers)
    {
        var originX = cx * CellSize;
        var originY = cy * CellSize;

        // Four quadrants, 17 x 17 vertices each, row 0 at the south edge.
        foreach (var (quadrant, qx, qy) in Quadrants)
        {
            land.Layers.Add(new BaseLayer
            {
                Header = new LayerHeader
                {
                    Texture = new FormLink<ILandscapeTextureGetter>(plan.GroundTexture),
                    Quadrant = quadrant,
                    LayerNumber = BaseLayerNumber,
                },
            });

            // Each texture's own coverage, painted bottom to top, turned into the shares the
            // game blends: at every vertex the layers' opacities are portions of the whole
            // and add up to at most 1 (as vanilla's do), the rest showing the base. A layer
            // covers what lies under it, so each keeps only what the layers above leave.
            var alphas = new float[textures.Count, QuadPoints * QuadPoints];
            for (var row = 0; row < QuadPoints; row++)
            {
                for (var col = 0; col < QuadPoints; col++)
                {
                    var x = originX + (qx * (QuadPoints - 1) + col) * Step;
                    var y = originY + (qy * (QuadPoints - 1) + row) * Step;
                    var left = 1f;
                    for (var i = textures.Count - 1; i >= 0; i--)
                    {
                        var alpha = Math.Clamp(textures[i].Alpha(x, y), 0f, 1f);
                        alphas[i, row * QuadPoints + col] = alpha * left;
                        left *= 1f - alpha;
                    }
                }
            }

            var painted = new List<(FormKey Texture, ExtendedList<AlphaLayerData> Data, float Weight)>();
            for (var i = 0; i < textures.Count; i++)
            {
                var data = new ExtendedList<AlphaLayerData>();
                for (var pos = 0; pos < QuadPoints * QuadPoints; pos++)
                {
                    var share = MathF.Floor(alphas[i, pos] * 255f) / 255f;
                    if (share >= 1f / 255f)
                    {
                        data.Add(new AlphaLayerData { Position = (ushort)pos, Opacity = share });
                    }
                }

                if (data.Count > 0)
                {
                    painted.Add((textures[i].Texture, data, data.Sum(d => d.Opacity)));
                }
            }

            // Over the limit, the faintest layers go (a wisp of grass-dirt at a palisade
            // corner), never the order of the rest.
            while (painted.Count > MaxAlphaLayers)
            {
                var faintest = painted.Select((l, i) => (l.Weight, i)).Min().i;
                painted.RemoveAt(faintest);
                droppedLayers++;
            }

            var layerNumber = 0;
            foreach (var (texture, data, _) in painted)
            {
                land.Layers.Add(new AlphaLayer
                {
                    Header = new LayerHeader
                    {
                        Texture = new FormLink<ILandscapeTextureGetter>(texture),
                        Quadrant = quadrant,
                        LayerNumber = (ushort)layerNumber++,
                    },
                    AlphaLayerData = data,
                });
            }

            maxLayers = Math.Max(maxLayers, layerNumber);
        }
    }

    // ------------------------------------------------------------------------
    // Ground textures and props
    // ------------------------------------------------------------------------

    /// <summary>
    /// The fair's own landscape textures: copies of vanilla LTEX records (grass, footsteps,
    /// friction carry over) with their own texture sets, whose height slot names a parallax
    /// map for Terrain Parallax under Community Shaders' Terrain Helper.
    /// </summary>
    private static Dictionary<string, FormKey> AddGroundTextures(SkyrimMod mod, GroundConfig ground, ISkyrimModGetter? master)
    {
        if (master is null)
        {
            throw new InvalidOperationException("fairWorld.ground copies vanilla landscape textures from Skyrim.esm; set Site.SkyrimDataPath.");
        }

        var result = new Dictionary<string, FormKey>();
        foreach (var t in ground.Textures)
        {
            var key = FormKeyHelper.Parse(t.Source);
            var ltex = master.LandscapeTextures.FirstOrDefault(l => l.FormKey == key)
                ?? throw new InvalidOperationException($"Ground texture {t.Role}: {t.Source} is not an LTEX in Skyrim.esm.");
            var sourceSet = master.TextureSets.First(x => x.FormKey == ltex.TextureSet.FormKey);
            var set = sourceSet.Duplicate(mod.GetNextFormKey());
            var name = char.ToUpperInvariant(t.Role[0]) + t.Role[1..];
            set.EditorID = $"{ground.EditorIdPrefix}{name}Set";
            if (t.Diffuse.Length > 0) set.Diffuse = t.Diffuse;
            if (t.Normal.Length > 0) set.NormalOrGloss = t.Normal;
            var diffuse = set.Diffuse!.GivenPath;
            set.Height = t.Height.Length > 0
                ? t.Height
                : System.Text.RegularExpressions.Regex.Replace(diffuse, @"\.dds$", "_p.dds", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            mod.TextureSets.Add(set);

            var copy = ltex.Duplicate(mod.GetNextFormKey());
            copy.EditorID = $"{ground.EditorIdPrefix}{name}";
            copy.TextureSet.SetTo(set.FormKey);
            mod.LandscapeTextures.Add(copy);
            result[t.Role] = copy.FormKey;
        }

        foreach (var role in new[] { "grass", "dirtGrass", "dirt", "path", "cobble" })
        {
            if (!result.ContainsKey(role))
            {
                throw new InvalidOperationException($"fairWorld.ground.textures has no '{role}' texture.");
            }
        }

        return result;
    }

    /// <summary>The props manifest written by tools/make_static_props.py, in name order.</summary>
    private static IEnumerable<(string Name, ProjectStaticConfig Prop)> LoadPropManifest(string path)
    {
        var full = Path.IsPathRooted(path) ? path : Path.Combine(FairPaths.ConfigDirectory, path);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(full));
        foreach (var entry in doc.RootElement.GetProperty("props").EnumerateObject().OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var lo = entry.Value.GetProperty("min").EnumerateArray().Select(v => v.GetSingle()).ToArray();
            var hi = entry.Value.GetProperty("max").EnumerateArray().Select(v => v.GetSingle()).ToArray();
            yield return (entry.Name, new ProjectStaticConfig
            {
                EditorId = $"SkyrimFairProp{entry.Name}",
                Model = entry.Value.GetProperty("model").GetString()!,
                Width = MathF.Max(2f, 2f * MathF.Max(MathF.Abs(lo[0]), MathF.Abs(hi[0]))),
                Depth = MathF.Max(2f, 2f * MathF.Max(MathF.Abs(lo[1]), MathF.Abs(hi[1]))),
                Height = MathF.Max(2f, hi[2]),
                MinZ = lo[2],
            });
        }
    }

    private static readonly (Quadrant Quadrant, int X, int Y)[] Quadrants =
    {
        (Quadrant.BottomLeft, 0, 0),
        (Quadrant.BottomRight, 1, 0),
        (Quadrant.TopLeft, 0, 1),
        (Quadrant.TopRight, 1, 1),
    };

    private static byte SignedByte(float unit) => unchecked((byte)(sbyte)MathF.Round(unit * 127f));

    private static PlacedObject Place(SkyrimMod mod, FormKey baseKey, float x, float y, float z, float headingDegrees)
        => new(mod)
        {
            Base = new FormLinkNullable<IPlaceableObjectGetter>(baseKey),
            Placement = new Placement
            {
                Position = new P3Float(x, y, z),
                Rotation = new P3Float(0f, 0f, headingDegrees * MathF.PI / 180f),
            },
        };

    // ------------------------------------------------------------------------
    // The plan: perimeter, avenue and zones as distance fields
    // ------------------------------------------------------------------------

    private sealed record PaintTexture(FormKey Texture, Func<float, float, float> Alpha);

    private sealed class Plan
    {
        private readonly FairWorldConfig config;

        private readonly (float X, float Y)[] perimeter;

        private readonly (float X, float Y)[] avenue;

        private readonly List<(FairWorldZone Zone, (float X, float Y)[] Polygon)> zones;

        public Plan(FairWorldConfig config)
        {
            this.config = config;
            perimeter = config.Perimeter.Select(p => (p[0], p[1])).ToArray();
            avenue = config.Avenue.Select(p => (p[0], p[1])).ToArray();
            zones = config.Zones.Select(z => (z, z.Polygon.Select(p => (p[0], p[1])).ToArray())).ToList();
            GroundTexture = FormKeyHelper.Parse(config.Textures.Ground);
        }

        public FormKey GroundTexture { get; private set; }

        private Dictionary<string, FormKey>? ground;

        private List<(float X, float Y, float Radius, float Strength)> wear = new();

        private List<((float X, float Y)[] Points, float Half)> wornLanes = new();

        private List<(float X, float Y)[]> wornZones = new();

        // Wear sources bucketed by 512-unit tile, so each vertex only visits its neighbours.
        private Dictionary<(int, int), List<(float X, float Y, float Radius, float Strength)>> wearTiles = new();

        public void UseGround(Dictionary<string, FormKey> textures)
        {
            ground = textures;
            GroundTexture = textures["grass"];
        }

        public void SetWear(
            List<(float X, float Y, float Radius, float Strength)> sources, List<MarketLane> lanes, List<string> zoneNames)
        {
            wear = sources;
            wearTiles = new();
            foreach (var w in sources)
            {
                for (var tx = (int)MathF.Floor((w.X - w.Radius) / 512f); tx <= (int)MathF.Floor((w.X + w.Radius) / 512f); tx++)
                {
                    for (var ty = (int)MathF.Floor((w.Y - w.Radius) / 512f); ty <= (int)MathF.Floor((w.Y + w.Radius) / 512f); ty++)
                    {
                        if (!wearTiles.TryGetValue((tx, ty), out var list)) wearTiles[(tx, ty)] = list = new();
                        list.Add(w);
                    }
                }
            }

            wornLanes = lanes
                .Where(l => l.UseAvenue || l.Points.Count >= 2)
                .Select(l => ((l.UseAvenue ? avenue : l.Points.Select(p => (p[0], p[1])).ToArray()),
                    l.HalfWidths.Count == 0 ? 250f : l.HalfWidths.Average(h => h[1])))
                .ToList();
            wornZones = zones.Where(z => zoneNames.Contains(z.Zone.Name)).Select(z => z.Polygon).ToList();
        }

        /// <summary>How trodden the ground is, 0 (untouched) to 1 (bare), before noise.</summary>
        private float Wear(float x, float y)
        {
            var w = 0f;
            if (wearTiles.TryGetValue(((int)MathF.Floor(x / 512f), (int)MathF.Floor(y / 512f)), out var near))
            {
                foreach (var s in near)
                {
                    var d = MathF.Sqrt((x - s.X) * (x - s.X) + (y - s.Y) * (y - s.Y));
                    if (d < s.Radius) w = MathF.Max(w, s.Strength * Smooth(1f - d / s.Radius));
                }
            }

            foreach (var (points, half) in wornLanes)
            {
                w = MathF.Max(w, config.Ground.LaneWear * Fill(PolylineDistance(points, x, y) - half, 220f));
            }

            foreach (var polygon in wornZones)
            {
                w = MathF.Max(w, 0.85f * Fill(SignedDistance(polygon, x, y), 260f));
            }

            // The cobbles' fringe is scuffed to bare dirt by the traffic on and off them.
            var edge = MathF.Abs(PolylineDistance(avenue, x, y) - config.Ground.CobbleHalfWidth);
            w = MathF.Max(w, 0.8f * Fill(edge - 90f, 140f));

            // Trodden hardest in the middle of the fair, less out toward the palisade.
            var gc = config.Ground;
            var middle = Smooth(-Outside(x, y) / gc.CentreDepth);
            return w * (gc.EdgeWear + (1f - gc.EdgeWear) * middle) + gc.CentreWear * middle;
        }

        /// <summary>Distance along the avenue of the point on it nearest (x, y).</summary>
        private float AvenueStation(float x, float y)
        {
            var (best, bestS, s) = (float.MaxValue, 0f, 0f);
            for (var i = 1; i < avenue.Length; i++)
            {
                var (a, b) = (avenue[i - 1], avenue[i]);
                var (dx, dy) = (b.X - a.X, b.Y - a.Y);
                var len = MathF.Sqrt(dx * dx + dy * dy);
                var t = Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / (len * len), 0f, 1f);
                var (px, py) = (a.X + dx * t, a.Y + dy * t);
                var d = (x - px) * (x - px) + (y - py) * (y - py);
                if (d < best) (best, bestS) = (d, s + t * len);
                s += len;
            }

            return bestS;
        }

        private IReadOnlyList<PaintTexture> GroundLayers()
        {
            var gc = config.Ground;
            var p = gc.NoisePeriod;
            float Inside(float x, float y) => Fill(Outside(x, y) + 40f, 160f);
            float N1(float x, float y) => ValueNoise(x / p + 3.1f, y / p - 7.7f);
            float N2(float x, float y) => ValueNoise(x / (p * 0.6f) - 11.3f, y / (p * 0.6f) + 2.9f);
            float N3(float x, float y) => ValueNoise(x / (p * 0.3f) + 19.9f, y / (p * 0.3f) + 13.1f);
            var length = 0f;
            for (var i = 1; i < avenue.Length; i++)
            {
                length += MathF.Sqrt((avenue[i].X - avenue[i - 1].X) * (avenue[i].X - avenue[i - 1].X) + (avenue[i].Y - avenue[i - 1].Y) * (avenue[i].Y - avenue[i - 1].Y));
            }

            return new List<PaintTexture>
            {
                new(FormKeyHelper.Parse(config.Textures.Outside), (x, y) => Smooth((Outside(x, y) - 128f) / 512f)),
                new(FormKeyHelper.Parse(config.Textures.Perimeter),
                    (x, y) => Fill(MathF.Abs(Outside(x, y)) - config.PerimeterStripWidth / 2f, StripFeather)
                        * (1f - Fill(GateDistance(x, y) - config.GateWidth / 2f, StripFeather))),

                // Grass giving way: patchy dirt-grass, most in the middle of the fair and where it is walked.
                new(ground!["dirtGrass"], (x, y) => Inside(x, y) * Smooth((0.16f + 0.24f * Smooth(-Outside(x, y) / gc.CentreDepth) + 0.8f * Wear(x, y) + (N1(x, y) - 0.5f) * 1.3f - 0.3f) / 0.35f)),

                // Bare dirt in patches, following the wear but broken by noise at two scales.
                new(ground["dirt"], (x, y) => Inside(x, y) * Smooth((0.1f + Wear(x, y) * 1.05f + (N2(x, y) - 0.5f) * 1.0f + (N3(x, y) - 0.5f) * 0.4f - 0.5f) / 0.25f)),

                // Trodden earth where the traffic is heaviest.
                new(ground["path"], (x, y) => Inside(x, y) * Smooth((Wear(x, y) - 0.66f + (N3(x, y) - 0.5f) * 0.5f) / 0.2f)),

                // The cobbled avenue, its edge wandering and the stones giving out at the ends.
                new(ground["cobble"], (x, y) =>
                {
                    var along = AvenueStation(x, y);
                    if (along < gc.CobbleFrom - 200f || along > MathF.Min(gc.CobbleTo, length) + 200f) return 0f;
                    // The edge wanders slowly: a quick wander at this terrain resolution breaks it up.
                    var half = gc.CobbleHalfWidth + gc.CobbleRagged * (N1(x, y) * 2f - 1f);
                    var ends = Fill(gc.CobbleFrom - along, 300f) * Fill(along - MathF.Min(gc.CobbleTo, length), 300f);

                    // Here and there the stones are sunk under trodden dirt.
                    var sunk = 1f - gc.CobbleSunk * Smooth((N2(x, y) - 0.62f) / 0.2f);
                    return Fill(PolylineDistance(avenue, x, y) - half, gc.CobbleFeather) * ends * sunk;
                }),
            };
        }

        public (float MinX, float MinY, float MaxX, float MaxY) Bounds =>
            (perimeter.Min(p => p.X), perimeter.Min(p => p.Y), perimeter.Max(p => p.X), perimeter.Max(p => p.Y));

        /// <summary>Distance outside the perimeter; negative inside.</summary>
        public float Outside(float x, float y) => SignedDistance(perimeter, x, y);

        public float Height(float x, float y)
        {
            var t = config.Terrain;
            var beyond = Outside(x, y) - t.FlatMargin;
            if (beyond <= 0f)
            {
                return config.FloorZ;
            }

            var rise = Smooth(beyond / t.RiseDistance);
            var noise = ValueNoise(x / t.NoisePeriod, y / t.NoisePeriod) * 0.65f
                + ValueNoise(x / (t.NoisePeriod * 0.5f) + 17.3f, y / (t.NoisePeriod * 0.5f) - 5.1f) * 0.35f;
            return config.FloorZ + rise * (t.RiseHeight + t.NoiseAmplitude * (noise * 2f - 1f));
        }

        /// <summary>
        /// Every painted texture in draw order, with its alpha field. Zones sharing a
        /// texture are merged into one layer so a quadrant never carries a texture twice.
        /// </summary>
        public IReadOnlyList<PaintTexture> PaintTextures()
        {
            if (ground is not null)
            {
                return GroundLayers();
            }

            var ops = new List<(FormKey Texture, Func<float, float, float> Alpha)>
            {
                (FormKeyHelper.Parse(config.Textures.Outside), (x, y) => Smooth((Outside(x, y) - 128f) / 512f)),
                (FormKeyHelper.Parse(config.Textures.Perimeter),
                    (x, y) => Fill(MathF.Abs(Outside(x, y)) - config.PerimeterStripWidth / 2f, StripFeather)
                        * (1f - Fill(GateDistance(x, y) - config.GateWidth / 2f, StripFeather))),
                (FormKeyHelper.Parse(config.Textures.Avenue),
                    (x, y) => Fill(PolylineDistance(avenue, x, y) - config.AvenueWidth / 2f, ZoneFeather)),
            };

            foreach (var (zone, polygon) in zones)
            {
                ops.Add((FormKeyHelper.Parse(zone.Texture), (x, y) => Fill(SignedDistance(polygon, x, y), ZoneFeather)));
            }

            return ops
                .GroupBy(o => o.Texture)
                .Select(g =>
                {
                    var fields = g.Select(o => o.Alpha).ToArray();
                    return new PaintTexture(g.Key, (x, y) => fields.Max(f => f(x, y)));
                })
                .ToList();
        }

        /// <summary>Distance to the gate point; infinite when no gate is configured.</summary>
        private float GateDistance(float x, float y)
        {
            if (config.Gate is not { Length: 2 } gate)
            {
                return float.MaxValue;
            }

            var dx = x - gate[0];
            var dy = y - gate[1];
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Palisade panels, edge by edge round the outline. Each edge's run carries a
        /// little past both vertices so corners close, is split either side of the gate
        /// with its ends tucked into the gate posts, and is filled with the fewest panels
        /// that still overlap by <see cref="PalisadeConfig.Overlap"/>, spread evenly so no
        /// short filler panel is needed. Every panel then wanders slightly in yaw, line,
        /// scale and sink, and some are turned round, all from a fixed integer hash.
        /// </summary>
        public IEnumerable<WallPanel> WallPanels(PalisadeConfig wall, float gateHalfWidth)
        {
            var width = wall.Width * wall.Scale;
            var pitch = width * (1f - wall.Overlap);

            for (var i = 0; i < perimeter.Length; i++)
            {
                var a = perimeter[i];
                var b = perimeter[(i + 1) % perimeter.Length];
                var length = MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                var (ux, uy) = ((b.X - a.X) / length, (b.Y - a.Y) / length);
                var edgeHeading = MathF.Atan2(ux, uy) * 180f / MathF.PI;

                var runs = new List<(float From, float To)>();
                var (gateDistance, gateT) = SegmentDistance(a, b, config.Gate[0], config.Gate[1]);
                if (gateDistance < 1f)
                {
                    var at = gateT * length;
                    runs.Add((-wall.CornerExtension, at - gateHalfWidth + wall.GateTuck));
                    runs.Add((at + gateHalfWidth - wall.GateTuck, length + wall.CornerExtension));
                }
                else
                {
                    runs.Add((-wall.CornerExtension, length + wall.CornerExtension));
                }

                for (var r = 0; r < runs.Count; r++)
                {
                    var (from, to) = runs[r];
                    var span = to - from;
                    var count = span <= width ? 1 : (int)MathF.Ceiling((span - width) / pitch) + 1;
                    var run = i * 16 + r;
                    for (var k = 0; k < count; k++)
                    {
                        var along = count == 1 ? (from + to) / 2f : from + width / 2f + k * (span - width) / (count - 1);
                        var offset = (Hash3(run, k, 1) * 2f - 1f) * wall.OffsetJitter;
                        var x = a.X + ux * along + uy * offset;
                        var y = a.Y + uy * along - ux * offset;
                        var flip = Hash3(run, k, 5) < wall.FlipChance ? 180f : 0f;

                        // The panel spans local X, so it turns a quarter from the edge heading.
                        yield return new WallPanel(
                            x, y,
                            Height(x, y) - Hash3(run, k, 4) * wall.SinkMax,
                            edgeHeading - 90f + flip + (Hash3(run, k, 2) * 2f - 1f) * wall.YawJitterDegrees,
                            wall.Scale * (1f + (Hash3(run, k, 3) * 2f - 1f) * wall.ScaleJitter));
                    }
                }
            }
        }

        /// <summary>
        /// Scenery conifers in the band outside the wall: a jittered grid of candidates,
        /// each kept by chance against a density that rises from sparse by the wall to
        /// dense a little way out and thins toward the outer edge, multiplied by a
        /// clustering field whose low values are clearings. Nothing stands near the gate
        /// or in the open approach in front of it.
        /// </summary>
        public IEnumerable<TreePlacement> ForestTrees(ForestConfig forest, float gateHeading)
        {
            // A second layer (the treeline) salts the hash streams, so its grid doesn't
            // repeat the first's; salt 0 is the first layer's own stream.
            float H(int x, int y, int k) => Hash3(x + forest.Salt * 7919, y - forest.Salt * 104729, k);
            var species = forest.Trees
                .Select(t => (Tree: t, Key: FormKeyHelper.Parse(t.FormKey)))
                .ToList();
            var nearest = forest.Trees.Min(t => t.MinDistance);
            var (minX, minY, maxX, maxY) = Bounds;
            var reach = forest.OuterDistance;

            // The approach runs out of the gate, away from what it faces.
            var outward = (gateHeading + 180f) * MathF.PI / 180f;
            var (ox, oy) = (MathF.Sin(outward), MathF.Cos(outward));
            var coneCos = MathF.Cos(forest.GateApproachDegrees * MathF.PI / 180f);

            var gx0 = (int)MathF.Floor((minX - reach) / forest.GridSpacing);
            var gx1 = (int)MathF.Ceiling((maxX + reach) / forest.GridSpacing);
            var gy0 = (int)MathF.Floor((minY - reach) / forest.GridSpacing);
            var gy1 = (int)MathF.Ceiling((maxY + reach) / forest.GridSpacing);

            for (var gy = gy0; gy <= gy1; gy++)
            {
                for (var gx = gx0; gx <= gx1; gx++)
                {
                    var x = (gx + 0.5f + (H(gx, gy, 11) * 2f - 1f) * forest.Jitter) * forest.GridSpacing;
                    var y = (gy + 0.5f + (H(gx, gy, 12) * 2f - 1f) * forest.Jitter) * forest.GridSpacing;
                    var distance = Outside(x, y);
                    if (distance < nearest || distance > forest.OuterDistance)
                    {
                        continue;
                    }

                    var gdx = x - config.Gate[0];
                    var gdy = y - config.Gate[1];
                    var fromGate = MathF.Sqrt(gdx * gdx + gdy * gdy);
                    if (fromGate < forest.GateClearRadius
                        || (fromGate < forest.GateApproachLength && (gdx * ox + gdy * oy) / fromGate > coneCos))
                    {
                        continue;
                    }

                    var cluster = ValueNoise(x / forest.ClusterPeriod + 41.7f, y / forest.ClusterPeriod - 23.9f) * 0.7f
                        + ValueNoise(x / (forest.ClusterPeriod * 0.45f) - 8.2f, y / (forest.ClusterPeriod * 0.45f) + 3.3f) * 0.3f;
                    if (cluster < forest.ClearingThreshold)
                    {
                        continue;
                    }

                    var rampIn = Smooth((distance - nearest) / MathF.Max(1f, forest.DenseFrom - nearest));
                    var fadeOut = 1f - 0.75f * Smooth((distance - forest.DenseTo) / MathF.Max(1f, forest.OuterDistance - forest.DenseTo));
                    var clump = Smooth((cluster - forest.ClearingThreshold) / 0.22f);
                    var chance = forest.PeakDensity * (0.25f + 0.75f * rampIn) * fadeOut * clump;
                    if (H(gx, gy, 13) >= chance)
                    {
                        continue;
                    }

                    var allowed = species
                        .Where(s => distance >= s.Tree.MinDistance && distance <= s.Tree.MaxDistance)
                        .ToList();
                    if (allowed.Count == 0)
                    {
                        continue;
                    }

                    var pick = H(gx, gy, 14) * allowed.Sum(s => s.Tree.Weight);
                    var chosen = allowed[^1];
                    foreach (var s in allowed)
                    {
                        pick -= s.Tree.Weight;
                        if (pick < 0f)
                        {
                            chosen = s;
                            break;
                        }
                    }

                    var tree = chosen.Tree;
                    yield return new TreePlacement(
                        chosen.Key, tree.Name, x, y,
                        Height(x, y) - (tree.Sink ?? forest.Sink),
                        H(gx, gy, 15) * 360f,
                        (H(gx, gy, 16) * 2f - 1f) * forest.LeanDegrees,
                        (H(gx, gy, 17) * 2f - 1f) * forest.LeanDegrees,
                        tree.MinScale + H(gx, gy, 18) * (tree.MaxScale - tree.MinScale),
                        distance);
                }
            }
        }

        /// <summary>
        /// Distant mountains, row by row, round the compound centre: evenly spaced in
        /// angle with each piece wandering by up to <see cref="MountainRow.AngleJitter"/> of
        /// the spacing, at a radius anywhere in the row's band, turned at random, and
        /// sunk so the mesh's lowest point lands on <see cref="MountainsConfig.BaseZ"/>.
        /// </summary>
        public IEnumerable<MountainPlacement> Mountains(MountainsConfig mountains, Func<FormKey, float> lowestPoint, bool placeLast)
        {
            var (minX, minY, maxX, maxY) = Bounds;
            var (cx, cy) = ((minX + maxX) / 2f, (minY + maxY) / 2f);

            for (var r = 0; r < mountains.Rows.Count; r++)
            {
                var row = mountains.Rows[r];
                if (row.PlaceLast != placeLast)
                {
                    continue;
                }

                var baseZ = row.BaseZ ?? mountains.BaseZ;
                var pieces = row.Pieces.Select(p => (Piece: p, Key: FormKeyHelper.Parse(p.FormKey))).ToList();
                var totalWeight = pieces.Sum(p => p.Piece.Weight);
                var spacing = 360f / Math.Max(1, row.Count);

                for (var k = 0; k < row.Count; k++)
                {
                    if (row.GapChance > 0f && Hash3(r, k, 26) < row.GapChance)
                    {
                        continue;
                    }

                    var groupAngle = row.StartDegrees + (k + (Hash3(r, k, 21) * 2f - 1f) * row.AngleJitter) * spacing;
                    var groupRadius = row.MinRadius + Hash3(r, k, 22) * (row.MaxRadius - row.MinRadius);
                    var size = row.ClusterMin + (int)(Hash3(r, k, 27) * (row.ClusterMax - row.ClusterMin + 1));
                    size = Math.Clamp(size, row.ClusterMin, Math.Max(row.ClusterMin, row.ClusterMax));

                    for (var j = 0; j < size; j++)
                    {
                        // The group's first piece keeps the row's original hash streams, so a
                        // row of single pieces places exactly as before.
                        var (hk, salt) = j == 0 ? (k, 0) : (k * 16 + j, 1000);
                        var angle = groupAngle + (j == 0 ? 0f : (Hash3(r, hk, salt + 28) * 2f - 1f) * row.ClusterSpreadDegrees);
                        var radius = groupRadius + (j == 0 ? 0f : (Hash3(r, hk, salt + 29) * 2f - 1f) * row.ClusterRadiusSpread);
                        var rad = angle * MathF.PI / 180f;
                        var x = cx + MathF.Sin(rad) * radius;
                        var y = cy + MathF.Cos(rad) * radius;

                        var pick = Hash3(r, hk, salt + 23) * totalWeight;
                        var chosen = pieces[^1];
                        foreach (var p in pieces)
                        {
                            pick -= p.Piece.Weight;
                            if (pick < 0f)
                            {
                                chosen = p;
                                break;
                            }
                        }

                        var scale = chosen.Piece.MinScale + Hash3(r, hk, salt + 24) * (chosen.Piece.MaxScale - chosen.Piece.MinScale);
                        // A one-sided piece turns its finished local +Y to the compound.
                        var heading = chosen.Piece.FacesCentre
                            ? MathF.Atan2(cx - x, cy - y) * 180f / MathF.PI + (Hash3(r, hk, salt + 25) * 2f - 1f) * chosen.Piece.FaceJitter
                            : Hash3(r, hk, salt + 25) * 360f;
                        yield return new MountainPlacement(
                            chosen.Key, chosen.Piece.Name, row.Name, x, y,
                            baseZ - lowestPoint(chosen.Key) * scale,
                            (heading % 360f + 360f) % 360f, scale, angle, radius);
                    }
                }

                foreach (var pin in row.Pinned)
                {
                    var key = FormKeyHelper.Parse(pin.FormKey);
                    var rad = pin.Angle * MathF.PI / 180f;
                    yield return new MountainPlacement(
                        key, pin.Name, row.Name, cx + MathF.Sin(rad) * pin.Radius, cy + MathF.Cos(rad) * pin.Radius,
                        baseZ - lowestPoint(key) * pin.Scale, pin.Yaw, pin.Scale, pin.Angle, pin.Radius);
                }
            }
        }

        /// <summary>Mountains on a coarse plan, north up: <c>n</c> near row, <c>M</c> far row, <c>o</c> the compound.</summary>
        public string RenderMountains(IReadOnlyList<MountainPlacement> mountains, float cell)
        {
            if (mountains.Count == 0)
            {
                return string.Empty;
            }

            var (minX, minY, maxX, maxY) = Bounds;
            var reach = mountains.Max(m => m.Radius) + cell;
            var (cx, cy) = ((minX + maxX) / 2f, (minY + maxY) / 2f);
            var size = (int)MathF.Ceiling(reach * 2f / cell);
            var marks = new Dictionary<(int, int), char>();
            foreach (var m in mountains)
            {
                marks[((int)((m.X - cx + reach) / cell), (int)((cy + reach - m.Y) / cell))] =
                    m.Row == mountains[0].Row ? 'n' : 'M';
            }

            var sb = new StringBuilder();
            for (var row = 0; row < size; row++)
            {
                sb.Append("    ");
                for (var col = 0; col < size; col++)
                {
                    var x = cx - reach + (col + 0.5f) * cell;
                    var y = cy + reach - (row + 0.5f) * cell;
                    sb.Append(marks.TryGetValue((col, row), out var c) ? c : Outside(x, y) <= 0f ? 'o' : '.');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Plan view, north up, one character per <paramref name="cell"/> units, padded by
        /// <paramref name="margin"/>. <c>^</c> marks a character holding at least one tree.
        /// </summary>
        public string RenderPlan(float cell, float margin, IReadOnlyList<TreePlacement> trees)
        {
            var (minX, minY, maxX, maxY) = Bounds;
            minX -= margin + cell;
            minY -= margin + cell;
            maxX += margin + cell;
            maxY += margin + cell;
            var columns = (int)MathF.Floor((maxX - minX) / cell) + 1;
            var rows = (int)MathF.Floor((maxY - minY) / cell) + 1;
            var wooded = trees
                .Select(t => ((int)MathF.Floor((t.X - minX) / cell), (int)MathF.Floor((maxY - t.Y) / cell)))
                .ToHashSet();

            var sb = new StringBuilder();
            for (var row = 0; row < rows; row++)
            {
                sb.Append("    ");
                for (var col = 0; col < columns; col++)
                {
                    var glyph = Glyph(minX + (col + 0.5f) * cell, maxY - (row + 0.5f) * cell);
                    sb.Append(glyph == ' ' && wooded.Contains((col, row)) ? '^' : glyph);
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        private char Glyph(float x, float y)
        {
            var outside = Outside(x, y);
            if (GateDistance(x, y) <= config.GateWidth / 2f)
            {
                return 'G';
            }

            if (MathF.Abs(outside) <= config.PerimeterStripWidth)
            {
                return '#';
            }

            if (outside > 0f)
            {
                return ' ';
            }

            for (var i = zones.Count - 1; i >= 0; i--)
            {
                if (SignedDistance(zones[i].Polygon, x, y) <= 0f)
                {
                    return zones[i].Zone.Name[0];
                }
            }

            return PolylineDistance(avenue, x, y) <= config.AvenueWidth / 2f ? '=' : '.';
        }

        private static float SignedDistance((float X, float Y)[] polygon, float x, float y)
            => FairGeometry.SignedDistance(polygon, x, y);

        private static float PolylineDistance((float X, float Y)[] line, float x, float y)
            => FairGeometry.PolylineDistance(line, x, y);

        private static (float Distance, float T) SegmentDistance((float X, float Y) a, (float X, float Y) b, float x, float y)
            => FairGeometry.SegmentDistance(a, b, x, y);

        /// <summary>1 well inside, 0.5 on the edge, 0 well outside, over <paramref name="feather"/>.</summary>
        private static float Fill(float signedDistance, float feather) => Smooth(0.5f - signedDistance / feather);

        private static float Smooth(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Smooth value noise in 0..1 from a fixed integer hash. Never a string hash.</summary>
        private static float ValueNoise(float x, float y)
        {
            var ix = (int)MathF.Floor(x);
            var iy = (int)MathF.Floor(y);
            var fx = Smooth(x - ix);
            var fy = Smooth(y - iy);
            var top = Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx);
            var bottom = Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx);
            return Lerp(top, bottom, fy);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Hash3(int a, int b, int salt) => FairHash.Hash3(a, b, salt);

        private static float Hash(int x, int y) => FairHash.Hash(x, y);
    }
}

internal sealed record FairWorldMarker(string EditorId, FormKey FormKey, float X, float Y, float Heading);

internal sealed record WallPanel(float X, float Y, float Z, float Heading, float Scale);

internal sealed record TreePlacement(
    FormKey Base, string Name, float X, float Y, float Z,
    float Heading, float LeanX, float LeanY, float Scale, float Distance);

internal sealed record FairWorldWall(
    FormKey PanelStatic,
    FormKey GateStatic,
    FormKey GateReference,
    int PanelCount,
    float PanelWidth,
    float PanelHeight,
    float GateHeight,
    float GateHeading);

internal sealed record MountainPlacement(
    FormKey Base, string Name, string Row, float X, float Y, float Z,
    float Heading, float Scale, float Bearing, float Radius);

internal sealed record FairWorldTreeCount(
    string Name, int Count, float MinScale, float MaxScale, float MinDistance, float MaxDistance);

internal sealed record FairWorldResult(
    FormKey WorldspaceFormKey,
    string EditorId,
    FormKey ClimateFormKey,
    int WeatherCount,
    int CellCount,
    int CellRadius,
    int MaxAlphaLayers,
    IReadOnlyList<FairWorldMarker> Markers,
    (float MinX, float MinY, float MaxX, float MaxY) CompoundBounds,
    FairWorldWall Wall,
    IReadOnlyList<FairWorldTreeCount> Trees,
    IReadOnlyList<MountainPlacement> Mountains,
    StageResult? Stage,
    MarketResult? Market,
    VendorsResult? Vendors,
    ArcheryResult? Archery,
    TowersResult? Towers,
    CrowdsResult? Crowds,
    AudioResult? Audio,
    int WallBoxes,
    int Props,
    int GroundTextures,
    string Plan,
    string ForestPlan,
    string MountainPlan);
