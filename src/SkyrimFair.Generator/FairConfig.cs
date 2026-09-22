namespace SkyrimFair.Generator;

internal sealed record FairConfig
{
    public string PluginName { get; init; } = "SkyrimFair.esp";

    public string OutputDirectory { get; init; } = "dist";

    public FairIdentity Identity { get; init; } = new();

    public StagePrototype Stage { get; init; } = new();

    public PrototypeSite Site { get; init; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PluginName))
        {
            throw new InvalidOperationException("PluginName cannot be empty.");
        }

        if (!PluginName.EndsWith(".esp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The prototype currently expects PluginName to end in .esp.");
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new InvalidOperationException("OutputDirectory cannot be empty.");
        }

        Site.Validate();
    }
}

internal sealed record FairIdentity
{
    public string Name { get; init; } = "Skyrim Fair";

    public string WorkingLocation { get; init; } = "Whiterun tundra";

    /// <summary>Written to the plugin's TES4 header as CNAM.</summary>
    public string Author { get; init; } = "BarryRim Event Planner";
}

internal sealed record StagePrototype
{
    public bool Enabled { get; init; } = true;

    public int DancerCount { get; init; } = 2;

    public int BardCount { get; init; } = 2;

    public string Track { get; init; } = "Round the Green";
}

/// <summary>
/// The audited exterior test site. Every FormKey and coordinate the generator
/// places lives here so implementation code never carries raw world data.
/// Values come from docs/AUDIT.md and were read out of Skyrim.esm directly.
/// </summary>
internal sealed record PrototypeSite
{
    /// <summary>
    /// Skyrim's Data folder, read-only, used to copy the real WRLD/CELL records we
    /// override so nothing vanilla is stripped. Optional: leave null (as CI does) and
    /// the generator still emits a structurally valid plugin, but it must not be loaded.
    /// The SKYRIM_DATA_PATH environment variable takes precedence over this value.
    /// </summary>
    public string? SkyrimDataPath { get; init; }

    /// <summary>Tamriel worldspace.</summary>
    public string Worldspace { get; init; } = "0000003C:Skyrim.esm";

    /// <summary>Tamriel's persistent cell, where vanilla exterior map markers live.</summary>
    public string PersistentCell { get; init; } = "00000D74:Skyrim.esm";

    /// <summary>The exterior cell that receives the test object.</summary>
    public string Cell { get; init; } = "00009A28:Skyrim.esm";

    public int CellGridX { get; init; } = -2;

    public int CellGridY { get; init; } = -4;

    public FairPlacement Placement { get; init; } = new();

    /// <summary>
    /// Planned floor height for the future flat market platform (Plan B): the terrain
    /// maximum across the 3072-unit core, so the platform is pure fill with no cut and
    /// needs no LAND edits. Recorded here for the platform milestone; the current
    /// prototype places objects on native ground instead, via Placement.
    /// </summary>
    public float PlannedPlatformFloorZ { get; init; } = -5672f;

    public FairTestObject TestObject { get; init; } = new();

    public FairMapMarker MapMarker { get; init; } = new();

    public FoundationConfig Foundation { get; init; } = new();

    public void Validate()
    {
        RequireFormKey(Worldspace, nameof(Worldspace));
        RequireFormKey(PersistentCell, nameof(PersistentCell));
        RequireFormKey(Cell, nameof(Cell));
        RequireFormKey(TestObject.FormKey, "TestObject.FormKey");
        RequireFormKey(MapMarker.BaseObject, "MapMarker.BaseObject");

        if (string.IsNullOrWhiteSpace(MapMarker.Name))
        {
            throw new InvalidOperationException("MapMarker.Name cannot be empty.");
        }

        Foundation.Validate();
    }

    private static void RequireFormKey(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Site.{field} cannot be empty.");
        }

        if (!value.Contains(':'))
        {
            throw new InvalidOperationException(
                $"Site.{field} must be a FormKey in 'FormID:Plugin.esm' form, but was '{value}'.");
        }
    }
}

internal sealed record FairPlacement
{
    public float X { get; init; } = -5632f;

    public float Y { get; init; } = -12800f;

    /// <summary>Native terrain height at X/Y, so prototype objects sit on the ground.</summary>
    public float Z { get; init; } = -5720f;
}

internal sealed record FairTestObject
{
    public string EditorId { get; init; } = "SMarketStall01";

    public string FormKey { get; init; } = "00064B87:Skyrim.esm";
}

internal sealed record FairMapMarker
{
    public string BaseObject { get; init; } = "00000010:Skyrim.esm";

    public string Name { get; init; } = "The Wanderer's Fair";

    /// <summary>
    /// Name of a Mutagen MapMarker.MarkerType value. "Pass" is raw TNAM 0x18,
    /// the icon vanilla uses for its border-pass crossings.
    /// </summary>
    public string Type { get; init; } = "Pass";

    public bool Visible { get; init; } = true;

    public bool CanTravelTo { get; init; } = true;

    /// <summary>
    /// XRDS marker radius. 1800 matches WhiterunWatchtowerMapMarker, the nearest
    /// vanilla marker to the fair site, and suits a 3072-unit market core.
    /// </summary>
    public float Radius { get; init; } = 1800f;

    /// <summary>
    /// XLRT location reference type. Vanilla uses MapMarkerRefType on 333 of its
    /// 347 Tamriel map markers.
    /// </summary>
    public string LocationRefType { get; init; } = "0010F63C:Skyrim.esm";

    /// <summary>
    /// Where the marker sits, if it should not sit at Site.Placement. Once the
    /// foundation covers the site centre the marker must move off it, or fast travel
    /// drops the player into the gap between native ground and the paving slab.
    /// This puts arrival on open ground just beyond the foot of the entrance ramp.
    /// </summary>
    public FairPlacement? Position { get; init; }
}

/// <summary>
/// The landscaped foundation prototype: an irregular paved area built from the
/// project-owned tile kit in assets/blender/build_foundation_kit.py.
/// </summary>
internal sealed record FoundationConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Grid step, matching the kit's 512 edge tile.</summary>
    public int TileSize { get; init; } = 512;

    /// <summary>
    /// Platform floor height. Set to the terrain maximum across the footprint so
    /// the platform is pure fill with zero cut and needs no LAND edits.
    /// </summary>
    public float FloorZ { get; init; } = -5672f;

    /// <summary>
    /// The visible outline, one character per TileSize cell, '#' paved. Deliberately
    /// irregular: the player must never see a rectangle or the L-shaped safe envelope.
    /// First row is north.
    /// </summary>
    public IReadOnlyList<string> Footprint { get; init; } = new[]
    {
        ".###..",
        ".#####",
        "######",
        ".#####",
        "..###.",
    };

    /// <summary>Compass edge carrying the entrance ramp: N, S, E or W.</summary>
    public string RampEdge { get; init; } = "S";

    /// <summary>Ramp tiles chained outward; each drops RampRise.</summary>
    public int RampTiles { get; init; } = 2;

    /// <summary>How many perimeter segments wide the entrance is.</summary>
    public int RampWidth { get; init; } = 2;

    /// <summary>
    /// Where along the chosen edge the ramp sits: "centre", "start" or "end", ordered
    /// west-to-east on N/S edges and south-to-north on E/W edges.
    ///
    /// This matters more than it looks. A ramp centred on the north edge aims into a
    /// dip where the road lies 402 units below the platform - a 1:4 drop. From the
    /// eastern end the road has climbed and the ground is flatter, which is what makes
    /// a 1:8 connection possible at all.
    /// </summary>
    public string RampAlign { get; init; } = "centre";

    /// <summary>Fall per ramp tile, matching the kit's 1:8 grade over 512 units.</summary>
    public float RampRise { get; init; } = 64f;

    /// <summary>
    /// Largest floor-to-ground step that still gets a shoulder wedge. Beyond this the
    /// edge is a faced wall and a thin verge just floats against it.
    /// </summary>
    public float ShoulderMaxDrop { get; init; } = 112f;

    /// <summary>Height of one retaining piece. Deeper edges stack several courses.</summary>
    public float RetainHeight { get; init; } = 256f;

    /// <summary>
    /// Bulk radius-based clearing. Deliberately OFF. The footprint audit in
    /// docs/AUDIT.md lists everything that intersects, and references are disabled
    /// only by explicit FormKey via DisableReferences below.
    /// </summary>
    public bool ClearClutter { get; init; }

    /// <summary>
    /// Individual placed references to override as Initially Disabled, named
    /// explicitly after review. Each is checked before being touched: it must exist
    /// in the master, its base must be scenery (STAT/TREE/FLOR), and it must carry no
    /// script, link, owner, enable parent or persistent flag. Anything failing those
    /// checks is refused and reported rather than disabled.
    /// </summary>
    public IReadOnlyList<string> DisableReferences { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Extra margin added to each object's own mesh radius when deciding whether it
    /// intrudes on the paving.
    /// </summary>
    public float ClearMargin { get; init; } = 160f;

    /// <summary>
    /// How far beyond the paving to look for intruding clutter. Big tundra boulders
    /// have mesh radii up to ~1770 units, so their origins can be a whole cell away.
    /// </summary>
    public float ClearSearchRadius { get; init; } = 2048f;

    /// <summary>
    /// Objects with a mesh radius above this are never disabled: they are
    /// landscape-scale cliffs and mountains, and removing one would tear a hole in
    /// the surrounding world.
    /// </summary>
    public float ClearMaxRadius { get; init; } = 2000f;

    /// <summary>Gap between the retaining face and the shoulder wedge.</summary>
    public float ShoulderOffset { get; init; } = 64f;

    /// <summary>
    /// Minimum step between floor and native ground before a retaining face is worth
    /// placing. Below this the paving simply meets grade, which is what happens on the
    /// east side of the site where the tundra rises to meet the platform.
    /// </summary>
    public float MinExposure { get; init; } = 16f;

    public Dictionary<string, FoundationPiece> Pieces { get; init; } = new()
    {
        ["floorFill"] = new() { EditorId = "SkyrimFairFloorFill1024", Model = @"SkyrimFair\SkyrimFair_FloorFill_1024.nif" },
        ["floorEdge"] = new() { EditorId = "SkyrimFairFloorEdge512", Model = @"SkyrimFair\SkyrimFair_FloorEdge_512.nif" },
        ["retain"] = new() { EditorId = "SkyrimFairRetain512", Model = @"SkyrimFair\SkyrimFair_Retain_512.nif" },
        ["retainCorner"] = new() { EditorId = "SkyrimFairRetainCorner128", Model = @"SkyrimFair\SkyrimFair_RetainCorner_128.nif" },
        ["ramp"] = new() { EditorId = "SkyrimFairRamp512", Model = @"SkyrimFair\SkyrimFair_Ramp_512.nif" },
        ["shoulder"] = new() { EditorId = "SkyrimFairShoulder512", Model = @"SkyrimFair\SkyrimFair_Shoulder_512.nif" },
        // Visual paving caps: the only upward-facing surfaces on the terrace, so
        // adjacent tiles cannot show a vertical face between them. No collision.
        ["paveCapFill"] = new() { EditorId = "SkyrimFairPaveCap1024", Model = @"SkyrimFair\SkyrimFair_PaveCap_1024.nif" },
        ["paveCapEdge"] = new() { EditorId = "SkyrimFairPaveCap512", Model = @"SkyrimFair\SkyrimFair_PaveCap_512.nif" },
        ["rampCap"] = new() { EditorId = "SkyrimFairRampCap512", Model = @"SkyrimFair\SkyrimFair_RampCap_512.nif" },
    };

    public EntranceConfig Entrance { get; init; } = new();

    public DressingConfig Dressing { get; init; } = new();

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Footprint.Count == 0 || Footprint.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Foundation.Footprint must have at least one non-empty row.");
        }

        if (Footprint.Select(r => r.Length).Distinct().Count() != 1)
        {
            throw new InvalidOperationException("Foundation.Footprint rows must all be the same length.");
        }

        if (!Footprint.Any(r => r.Contains('#')))
        {
            throw new InvalidOperationException("Foundation.Footprint has no paved cells.");
        }

        // Phase variants are optional: they are only built when the paving material
        // has a period that does not divide the tile grid, and PhasedRole falls back
        // to the base role when they are absent.
        foreach (var role in new[]
        {
            "floorFill", "floorEdge", "retain", "retainCorner", "ramp", "shoulder",
            "paveCapFill", "paveCapEdge", "rampCap",
        })
        {
            if (!Pieces.ContainsKey(role))
            {
                throw new InvalidOperationException($"Foundation.Pieces is missing '{role}'.");
            }
        }

        if (!new[] { "N", "S", "E", "W" }.Contains(RampEdge.ToUpperInvariant()))
        {
            throw new InvalidOperationException($"Foundation.RampEdge must be N, S, E or W, but was '{RampEdge}'.");
        }

        if (Dressing.CliffMinRunSegments < 2
            || Dressing.CliffMaxRunSegments < Dressing.CliffMinRunSegments)
        {
            throw new InvalidOperationException(
                "Foundation.Dressing cliff run limits must be at least 2 and max must be >= min.");
        }

        if (Dressing.CliffMeshLength <= 0f || Dressing.CliffMeshDepth <= 0f)
        {
            throw new InvalidOperationException(
                "Foundation.Dressing cliff mesh dimensions must be positive.");
        }
    }
}

internal sealed record FoundationPiece
{
    public string EditorId { get; init; } = string.Empty;

    /// <summary>Mesh path relative to Data\meshes\.</summary>
    public string Model { get; init; } = string.Empty;
}

/// <summary>Vanilla rocks and plants used to break up the hard tile boundary.</summary>
internal sealed record DressingConfig
{
    public int Seed { get; init; } = 20260921;

    public int PerEdgeSegment { get; init; } = 2;

    /// <summary>
    /// Elongated vanilla tundra cliff face used as the visible skin over long,
    /// project-owned structural retaining runs. This mesh has real shallow relief;
    /// no parallax feature is required for its silhouette.
    /// </summary>
    public string CliffFace { get; init; } = "00097065:Skyrim.esm";

    /// <summary>Shortest contiguous straight run replaced by a cliff face.</summary>
    public int CliffMinRunSegments { get; init; } = 2;

    /// <summary>Maximum 512-unit wall segments covered by one cliff reference.</summary>
    public int CliffMaxRunSegments { get; init; } = 4;

    /// <summary>Measured long axis of DirtCliffs01Tundra01.</summary>
    public float CliffMeshLength { get; init; } = 1944f;

    /// <summary>Measured shallow axis of DirtCliffs01Tundra01.</summary>
    public float CliffMeshDepth { get; init; } = 465f;

    /// <summary>
    /// How far the cliff origin sits INWARD of the wall face. The mesh is an open
    /// shell whose face is on its local -Y side, so once it is turned to face
    /// outward the body extends inward and this is what buries the missing back
    /// wall inside the structural slab. It replaced an outward offset, which had
    /// pushed the open back into plain view.
    /// </summary>
    public float CliffInset { get; init; } = 48f;

    /// <summary>
    /// How far below the floor plane the cliff's top is placed, so its broad grassy
    /// top cap is hidden beneath the paving instead of shelving across it.
    /// </summary>
    public float CliffTopSink { get; init; } = 40f;

    /// <summary>
    /// How far beyond the paving edge dressing starts. Vanilla rocks have large
    /// meshes, so placing them close to the edge spills them onto the paved surface.
    /// </summary>
    public float MinOffset { get; init; } = 192f;

    public float Spread { get; init; } = 384f;

    public float MinScale { get; init; } = 0.7f;

    public float MaxScale { get; init; } = 1.4f;

    /// <summary>Rocks are sunk slightly so they read as bedded into the ground.</summary>
    public float RockSink { get; init; } = 24f;

    /// <summary>
    /// Hard ceiling on a dressing piece's mesh radius. Landscape-scale rocks such as
    /// RockTundraLand02Tundra01 (radius 1767) will bury the entire platform if placed
    /// as edge dressing, so they are rejected outright.
    /// </summary>
    public float MaxRadius { get; init; } = 500f;

    /// <summary>
    /// How far a dressing piece is allowed to reach onto the paving, so rocks break
    /// the edge silhouette instead of sitting in a tidy line beside it.
    /// </summary>
    public float EdgeOverlap { get; init; } = 64f;

    /// <summary>
    /// Toe rocks: low piles laid at native ground where the embankment meets grass.
    /// Deliberately small and medium piles only - the big RockTundraLand landscape
    /// slabs are 1400-1770 units across and are not dressing, they are terrain
    /// features. These are too short to face a wall; that is what Wall is for.
    /// </summary>
    public IReadOnlyList<string> Rocks { get; init; } = new[]
    {
        "0001BFB0:Skyrim.esm", // RockPileM01FieldGrass01Moss, r=404 h= 92
        "00024E8F:Skyrim.esm", // RockPileM02FieldGrass01Moss, r=305 h=102
        "00021E70:Skyrim.esm", // RockPileS01FieldGrass01,     r=180 h= 59
        "000674BB:Skyrim.esm", // RockPileS02FieldGrass01,     r=177 h= 78
        "0003554E:Skyrim.esm", // RockShelf01FieldGrass01,     r=660 h= 54
    };

    /// <summary>
    /// Embankment rocks, for facing an exposed retaining edge.
    ///
    /// Height is the whole point of this pool and the reason the terrace previously
    /// read as a bare grey box: every piece in Rocks is a 54-102 unit pile, and the
    /// perimeter it was meant to hide is 192-288 units tall, so the dressing sat
    /// round the foot of the wall like gravel. These pieces are 180-418 tall and are
    /// scaled to the measured exposure of the segment they face.
    ///
    /// All of them are statics vanilla itself places within 6,000 units of this site,
    /// so the embankment reads as the same landform family as the surrounding tundra.
    /// </summary>
    public IReadOnlyList<string> Wall { get; init; } = new[]
    {
        "00018199:Skyrim.esm", // RockL01,                r=270 h=288, 9 nearby in vanilla
        "0001819A:Skyrim.esm", // RockL02,                r=399 h=263
        "00018BA5:Skyrim.esm", // RockL03,                r=289 h=180, 4 nearby in vanilla
        "0001A6E2:Skyrim.esm", // RockL04,                r=178 h=418, tall and narrow
        "0001B0A8:Skyrim.esm", // RockL05,                r=215 h=276
        "000332C7:Skyrim.esm", // RockPileL01TundraRocks, r=512 h=372, 2 nearby in vanilla
    };

    /// <summary>
    /// Bigger single stones dropped at the outer corners of the outline, where a flat
    /// top edge reads most obviously as a built rectangle.
    /// </summary>
    public IReadOnlyList<string> CornerStones { get; init; } = new[]
    {
        "0001819A:Skyrim.esm", // RockL02,                r=399 h=263
        "000332C7:Skyrim.esm", // RockPileL01TundraRocks, r=512 h=372
        "0001A6E2:Skyrim.esm", // RockL04,                r=178 h=418
    };

    /// <summary>Exposure above which a segment gets the tall embankment treatment.</summary>
    public float WallMinDrop { get; init; } = 140f;

    /// <summary>Embankment rocks per perimeter segment.</summary>
    public int WallPerSegment { get; init; } = 3;

    /// <summary>
    /// How far a rock's top is allowed to rise above the floor plane. This is what
    /// breaks the silhouette: seen from on the terrace, rock crowns interrupt the
    /// paving edge instead of it ending in a clean line.
    /// </summary>
    public float WallTopOvershoot { get; init; } = 48f;

    /// <summary>
    /// How far a rock's base is driven below the point its top has to reach, so the
    /// piece is sized to bed into the ground rather than perch on it. Must exceed
    /// WallTopOvershoot or a rock can end up standing on its own base.
    /// </summary>
    public float WallBedding { get; init; } = 80f;

    /// <summary>
    /// How far an embankment rock may reach onto the paving. Enough to interrupt the
    /// edge line, and no more: at 192 the rocks were standing well inside the market
    /// floor. Must stay below PavingRimAllowance or the guard will refuse the very
    /// rim rocks it is meant to permit.
    /// </summary>
    public float WallEdgeOverlap { get; init; } = 80f;

    /// <summary>
    /// Band just inside the paved edge where dressing may still protrude above the
    /// floor plane. Inside this band the terrace is protected: anything whose crown
    /// clears the paving is refused, so stalls always have a clean surface.
    /// </summary>
    public float PavingRimAllowance { get; init; } = 112f;

    /// <summary>
    /// How far above the floor plane a piece's crown may reach before the paving
    /// guard applies. Below this it is under the walking surface and harmless.
    /// </summary>
    public float PavingClearance { get; init; } = 8f;

    /// <summary>Ceiling on an embankment rock's scaled mesh radius.</summary>
    public float WallMaxRadius { get; init; } = 640f;

    /// <summary>Toe rocks per segment, laid on native ground beyond the embankment.</summary>
    public int ToePerSegment { get; init; } = 2;

    /// <summary>Shrubs and scrub per segment, out in the verge.</summary>
    public int VergePerSegment { get; init; } = 2;

    /// <summary>
    /// Largest local rise or fall of native ground across the verge that still reads
    /// as a soft earth transition. Beyond this the ground is doing its own thing and a
    /// verge wedge just floats.
    /// </summary>
    public float VergeMaxLocalStep { get; init; } = 112f;

    /// <summary>
    /// Clear walking width kept down the middle of the entrance ramp. Nothing is
    /// placed whose mesh reaches into this channel, which is what lets rock hug both
    /// ramp flanks without the entrance becoming an obstacle course.
    /// </summary>
    public float RampChannelWidth { get; init; } = 480f;

    /// <summary>How far past the ramp foot the clear channel continues, toward the road.</summary>
    public float RampLandingLength { get; init; } = 512f;

    /// <summary>
    /// Band at the ramp's own edge where dressing may still stand proud of its
    /// surface. Much tighter than PavingRimAllowance: a rock centred exactly on the
    /// ramp edge reaches half its width onto the walking surface, and at the head of
    /// the ramp that is a snag right where the player steps on.
    /// </summary>
    public float RampRimAllowance { get; init; } = 32f;

    /// <summary>
    /// Run of the project-owned shoulder wedge, matching SHOULDER_RUN in the kit
    /// script. The verge rule samples ground at both ends of the wedge, so this has
    /// to be the real length rather than an approximation.
    /// </summary>
    public float ShoulderRun { get; init; } = 256f;

    public IReadOnlyList<string> Shrubs { get; init; } = new[]
    {
        "000AAE79:Skyrim.esm", // TreeTundraShrub01
        "000AAE7A:Skyrim.esm", // TreeTundraShrub02
        "000AAE7B:Skyrim.esm", // TreeTundraShrub03
        "000AAE7F:Skyrim.esm", // TreeTundraShrub04
        "000AAE81:Skyrim.esm", // TreeTundraShrub05
        "000AAE83:Skyrim.esm", // TreeTundraShrub06
    };

    public IReadOnlyList<string> Scrub { get; init; } = new[]
    {
        "0003A2BC:Skyrim.esm", // TundraScrub01
        "0003A2BD:Skyrim.esm", // TundraScrub02
        "0003A2BE:Skyrim.esm", // TundraScrub03
    };
}


/// <summary>
/// The way in. A vanilla drystone wall with a staircase cut through it, rather than a
/// bare ramp: measured off the shipped mesh, the wall is 512 wide, the stair gap is 167,
/// and the treads climb 112 units over a 192 run at about 30 degrees.
/// </summary>
internal sealed record EntranceConfig
{
    /// <summary>False falls back to a plain ramp all the way up.</summary>
    public bool UseStairs { get; init; } = true;

    /// <summary>`StonewallTerraceStairs01`, the farm terrace stair.</summary>
    public string Stair { get; init; } = "000009D0:Skyrim.esm";

    /// <summary>
    /// Uniform scale on the stair piece. The mesh has 17 risers of only about 7 units,
    /// so it takes scaling well: at 2.0 the walkable gap goes 167 -> 334 and the steps
    /// are still a normal 13 each, while the drystone wall each flight carries grows to
    /// 1024 wide and 344 tall. That is what turns a narrow slot into the broad
    /// staircase with chunky tiers in the concept. Drop, run, inset and top offset all
    /// scale with it.
    /// </summary>
    public float StairScale { get; init; } = 2f;

    /// <summary>Height one flight climbs at scale 1. Also the step between flights.</summary>
    public float StairDrop { get; init; } = 112f;

    /// <summary>Run of one flight, so chained flights meet tread to tread.</summary>
    public float StairRun { get; init; } = 192f;

    /// <summary>
    /// Flights in the chain. At scale 2 each drops 224 over a 384 run, so two carry the
    /// floor at -5336 down the same 448 over the same 768 that four did at scale 1.
    /// </summary>
    public int StairFlights { get; init; } = 2;

    /// <summary>
    /// Local Z of the top tread. Placing the piece this far below the floor plane puts
    /// the top step on the floor and leaves the wall crest standing 40 above it.
    /// </summary>
    public float StairTopOffset { get; init; } = 130f;

    /// <summary>
    /// How far inside the paved edge the piece's origin sits, so the top tread lands on
    /// the edge itself. The mesh's top tread is 64 units in front of its origin.
    /// </summary>
    public float StairInset { get; init; } = 64f;
}
