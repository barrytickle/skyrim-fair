namespace SkyrimFair.Generator;

internal sealed record FairConfig
{
    public string PluginName { get; init; } = "SkyrimFair.esp";

    /// <summary>SPID exclusion patches written to dist/spid (see <see cref="FairSpidPatches"/>).</summary>
    public List<SpidPatch> SpidPatches { get; init; } = new();

    public string OutputDirectory { get; init; } = "dist";

    public FairIdentity Identity { get; init; } = new();

    public StagePrototype Stage { get; init; } = new();

    public PrototypeSite Site { get; init; } = new();

    public SandboxConfig Sandbox { get; init; } = new();

    public FairWorldConfig FairWorld { get; init; } = new();

    /// <summary>The compound's exterior in Tamriel, with the gate into the fair (<see cref="ExteriorConfig"/>).</summary>
    public ExteriorConfig Exterior { get; init; } = new();

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
        Sandbox.Validate();
        FairWorld.Validate();
    }
}

/// <summary>
/// The isolated festival worldspace: a parallel prototype to the Tamriel terrace, not
/// a replacement for it. A flat grass canvas with sky, weather and exterior lighting,
/// its own generated landscape, and the broad plan (perimeter line, entrance, avenue,
/// crowd square, stage, market side, activity side) painted into the ground so it can
/// be walked in game. Reached with <c>cow &lt;EditorId&gt; 0 0</c>.
///
/// Every coordinate is in world units in the new worldspace. Points are <c>[x, y]</c>.
/// </summary>
internal sealed record FairWorldConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Typed after <c>cow</c>. Must be unique across the load order.</summary>
    public string EditorId { get; init; } = "SkyrimFairWorld";

    /// <summary>Shown on the loading screen and in the HUD on arrival.</summary>
    public string Name { get; init; } = "The Wanderer's Fair";

    /// <summary>
    /// Parent worldspace, used for its map only (the pause-menu map and fast travel out
    /// behave as they do in a city). Nothing else is inherited. Tamriel.
    /// </summary>
    public string ParentWorldspace { get; init; } = "0000003C:Skyrim.esm";

    /// <summary>CLMT copied for sun, moons, sky model and day timings. SkyrimClimate.</summary>
    public string BaseClimate { get; init; } = "00000812:Skyrim.esm";

    /// <summary>REGN whose weather list becomes the new climate's. WeatherTundraNoPrecip.</summary>
    public string WeatherRegion { get; init; } = "001046C9:Skyrim.esm";

    public string ClimateEditorId { get; init; } = "SkyrimFairWorldClimate";

    /// <summary>Landscape is generated for cells -CellRadius..+CellRadius on both axes.</summary>
    public int CellRadius { get; init; } = 5;

    /// <summary>Height of the flat festival ground.</summary>
    public float FloorZ { get; init; } = 0f;

    public FairWorldTerrain Terrain { get; init; } = new();

    public FairWorldTextures Textures { get; init; } = new();

    /// <summary>
    /// Planned palisade line, one closed irregular polygon. Only painted and staked for
    /// now: the final wall is not built in this pass.
    /// </summary>
    public List<float[]> Perimeter { get; init; } = new();

    /// <summary>Width of the painted strip marking the perimeter line.</summary>
    public float PerimeterStripWidth { get; init; } = 256f;

    /// <summary>
    /// <c>[x, y]</c> on the perimeter where the main gate stands, centred on the avenue.
    /// </summary>
    public float[] Gate { get; init; } = Array.Empty<float>();

    /// <summary>Width of the break in the painted perimeter strip at the gate.</summary>
    public float GateWidth { get; init; } = 640f;

    /// <summary>
    /// Zone whose marker the gate faces, so the view through it runs up the avenue to
    /// the stage.
    /// </summary>
    public string GateFacesZone { get; init; } = "Stage";

    /// <summary>The palisade wall laid along the perimeter.</summary>
    public PalisadeConfig Palisade { get; init; } = new();

    /// <summary>The closed main gate.</summary>
    public GatePieceConfig GatePiece { get; init; } = new();

    /// <summary>The ring of vanilla conifers outside the wall.</summary>
    public ForestConfig Forest { get; init; } = new();

    /// <summary>
    /// A second, denser band of trees, shrubs and rocks close outside the wall, generated
    /// after everything else (so the records before it keep their FormIDs). Kept within
    /// about 2,000 of the wall: that band is inside the loaded grid wherever the player
    /// stands in the compound.
    /// </summary>
    public ForestConfig Treeline { get; init; } = new() { Enabled = false };

    /// <summary>The generated navmesh (docs/NAVMESH.md).</summary>
    public NavmeshConfig Navmesh { get; init; } = new();

    /// <summary>More life in the fair (docs/AUDIT.md, 2026-09-24): see <see cref="LifeConfig"/>.</summary>
    public LifeConfig Life { get; init; } = new();

    /// <summary>The faces the fair's face lists are filled with (see <see cref="FacePoolConfig"/>).</summary>
    public FacePoolConfig Faces { get; init; } = new();

    /// <summary>
    /// The invisible collision wall solid <see cref="CollisionWall"/>s are built from
    /// (tools/make_collision_wall.py: a box 256 x 16 x 400, centred, long along local X).
    /// Its STAT is made late, so nothing renumbers.
    /// </summary>
    public ProjectStaticConfig SolidWall { get; init; } = new();

    /// <summary>Unseen crowd switched off where the player is (see <see cref="CrowdCullingConfig"/>).</summary>
    public CrowdCullingConfig CrowdCulling { get; init; } = new();

    /// <summary>
    /// Crowd figures take FormIDs from here up, in placement order, not from the mod's
    /// counter: appending figures then never renumbers anything built after them (the
    /// singers, the globals), and nothing built later renumbers them.
    /// </summary>
    public uint CrowdFormIdBase { get; init; } = 0x10000;

    /// <summary>
    /// A keyword on every fair NPC record, for SPID exclusions in other mods' distribution
    /// lines (<c>-SkyrimFairNPC</c>): docs/RELEASE.md.
    /// </summary>
    public string NpcKeyword { get; init; } = "SkyrimFairNPC";

    /// <summary>The stage singers (docs/BARDS.md, "Generator side").</summary>
    public SingersConfig Singers { get; init; } = new();

    /// <summary>Named ambient characters, the creators' cameos (FairCameos.cs).</summary>
    public CameosConfig Cameos { get; init; } = new();

    /// <summary>Bringing the player's companions through the gate (FairWorld, the stage script).</summary>
    public CompanionsConfig Companions { get; init; } = new();

    /// <summary>Glow at the lanterns (FairLights.cs).</summary>
    public LightsConfig Lights { get; init; } = new();

    /// <summary>The stalls as shops (FairShops.cs).</summary>
    public ShopsConfig Shops { get; init; } = new();

    /// <summary>A global the stage script sets to 1 while the player is at the fair (read by the compatibility patches).</summary>
    public string AtFairGlobal { get; init; } = "SkyrimFairAtFair";

    /// <summary>
    /// Static crowd figures (docs/CROWD.md): posed people baked into STATs. These are the
    /// definitions; <see cref="CrowdPlacements"/> places them. A figure's STAT is made at its
    /// first placement, so the definitions' order doesn't matter.
    /// </summary>
    public List<CrowdFigure> CrowdFigures { get; init; } = new();

    /// <summary>
    /// Every crowd figure copy, in record order. **Append only**: each entry adds its records
    /// (the figure's STAT on first use, the reference, its collision box) after the previous
    /// entry's, and all of them after everything but the navmesh, so appending never moves a
    /// FormID. Written by tools/place_crowd.py, then kept as it is.
    /// </summary>
    public List<CrowdPlacement> CrowdPlacements { get; init; } = new();

    /// <summary>
    /// False leaves every crowd figure out (their FormIDs are their own range, so nothing
    /// else moves) and the benches they'd sit on stay sittable. The definitions and
    /// placements stay in the config for later.
    /// </summary>
    public bool CrowdFiguresEnabled { get; init; } = true;

    /// <summary>Seat markers of each furniture base (from their NIFs), for seated figures.</summary>
    public List<SeatMarkers> SeatMarkers { get; init; } = new();

    /// <summary>Where the generator writes the benches, rails and actors tools/place_crowd.py reads.</summary>
    public string CrowdSitesDump { get; init; } = string.Empty;

    /// <summary>What every fair NPC gets: invulnerability, and other mods' spells taken off.</summary>
    public NpcGuardConfig NpcGuard { get; init; } = new();

    /// <summary>Astra's paired folk dance on two dancers, through Open Animation Replacer.</summary>
    public FolkDanceConfig FolkDance { get; init; } = new();

    /// <summary>Distant vanilla mountains, always drawn.</summary>
    public MountainsConfig Mountains { get; init; } = new();

    /// <summary>The main stage, placed from its zone.</summary>
    public StageConfig Stage { get; init; } = new();

    /// <summary>The market avenue, Traders' Crossing and trading rows.</summary>
    public MarketConfig Market { get; init; } = new();

    /// <summary>Placeholder stall-keepers, one per stall.</summary>
    public VendorsConfig Vendors { get; init; } = new();

    /// <summary>
    /// Bundled third-party or project meshes given STAT records, so market modules can use
    /// them by writing <c>@EditorID</c> as a piece.
    /// </summary>
    public List<ProjectStaticConfig> ProjectStatics { get; init; } = new();

    /// <summary>Festival light towers: Barry's scaffold, a large lantern, Whiterun banners.</summary>
    public TowersConfig Towers { get; init; } = new();

    /// <summary>
    /// Physics-free copies of vanilla item meshes, listed with their bounds in the manifest
    /// <c>tools/make_static_props.py</c> writes (relative to the config file). Each becomes a
    /// STAT named <c>SkyrimFairProp&lt;Name&gt;</c>, used in modules as <c>@Prop&lt;Name&gt;</c>.
    /// </summary>
    public string PropManifest { get; init; } = string.Empty;

    /// <summary>The worn, patchy festival ground: fair-owned landscape textures and wear.</summary>
    public GroundConfig Ground { get; init; } = new();

    /// <summary>Overhead festival lines: pennant ropes and lanterns strung between poles.</summary>
    public List<OverheadRun> Overhead { get; init; } = new();

    /// <summary>Visitors gathered unevenly round the fair's attractions.</summary>
    public CrowdsConfig Crowds { get; init; } = new();

    /// <summary>
    /// Invisible walls: vanilla CollisionMarker box primitives laid along segments, as the
    /// game's own invisible walls are (keeps visitors off the stage).
    /// </summary>
    public List<CollisionWall> CollisionWalls { get; init; } = new();

    /// <summary>Where the generated stall directory is written, relative to the config (empty: none).</summary>
    public string StallDirectory { get; init; } = string.Empty;

    /// <summary>The archery range: townsfolk practising at targets, Solitude-style.</summary>
    public ArcheryConfig Archery { get; init; } = new();

    /// <summary>The fair's sound: the stage set, the cheer and the crowd ambience.</summary>
    public AudioConfig Audio { get; init; } = new();

    /// <summary>Centreline of the central avenue, entrance first.</summary>
    public List<float[]> Avenue { get; init; } = new();

    public float AvenueWidth { get; init; } = 800f;

    /// <summary>Painted zones, drawn in list order (later zones paint over earlier ones).</summary>
    public List<FairWorldZone> Zones { get; init; } = new();

    /// <summary>
    /// Named persistent heading markers, one per zone, so each can be reached with
    /// <c>player.moveto &lt;EditorId&gt;</c>. XMarkerHeading: invisible in game.
    /// </summary>
    public string ZoneMarker { get; init; } = "00000034:Skyrim.esm";

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditorId) || EditorId.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                "FairWorld.EditorId must be a single word: it is typed into the console after 'cow'.");
        }

        if (CellRadius < 2 || CellRadius > 16)
        {
            throw new InvalidOperationException("FairWorld.CellRadius must be between 2 and 16.");
        }

        if (Perimeter.Count < 3 || Perimeter.Any(p => p.Length != 2))
        {
            throw new InvalidOperationException("FairWorld.Perimeter needs at least three [x, y] points.");
        }

        if (Avenue.Count < 2 || Avenue.Any(p => p.Length != 2))
        {
            throw new InvalidOperationException("FairWorld.Avenue needs at least two [x, y] points.");
        }

        if (Palisade.Width <= 0f || GatePiece.Width <= 0f || Palisade.Scale <= 0f || GatePiece.Scale <= 0f)
        {
            throw new InvalidOperationException("FairWorld palisade and gate need a positive width and scale.");
        }

        if (Gate is not { Length: 2 })
        {
            throw new InvalidOperationException("FairWorld.Gate needs an [x, y] point on the perimeter.");
        }

        if (!Zones.Any(z => z.Name == GateFacesZone))
        {
            throw new InvalidOperationException($"FairWorld.GateFacesZone '{GateFacesZone}' is not a zone.");
        }

        if (Mountains.Enabled && Mountains.Rows.Any(r => r.Count < 1 || r.Pieces.Count == 0 || r.Pieces.Any(p => p.Weight <= 0f)
                || r.MinRadius <= 0f || r.MaxRadius < r.MinRadius))
        {
            throw new InvalidOperationException(
                "Every FairWorld mountain row needs a count, radii, and pieces with positive weights.");
        }

        if (Stage.Enabled)
        {
            var stageZone = Zones.FirstOrDefault(z => z.Name == Stage.Zone)
                ?? throw new InvalidOperationException($"FairWorld.Stage.Zone '{Stage.Zone}' is not a zone.");
            var turn = ((stageZone.Marker.Length == 3 ? stageZone.Marker[2] : 0f) % 90f + 90f) % 90f;
            if (turn > 0.01f && turn < 89.99f)
            {
                throw new InvalidOperationException(
                    "The stage zone's marker must face along a world axis (a multiple of 90 degrees): " +
                    "every stage log is laid with one tilt about a world axis.");
            }

            if (Stage.Deck.Columns < 1 || Stage.Deck.Rows < 1 || Stage.Steps.Treads < 1 || Stage.Steps.Across < 1)
            {
                throw new InvalidOperationException("The stage needs at least one deck piece and one tread.");
            }

            if (Stage.Beams.Concat<object>(Stage.Braces).Any(b => b is StageBeam { From.Length: not 2 } or StageBeam { To.Length: not 2 }
                    or StageBrace { From.Length: not 2 } or StageBrace { To.Length: not 2 })
                || Stage.Posts.Any(p => p.Length != 2))
            {
                throw new InvalidOperationException("Stage posts, beams and braces need [u, v] points.");
            }
        }

        if (Forest.Enabled && (Forest.Trees.Count == 0 || Forest.Trees.Any(t => t.Weight <= 0f)))
        {
            throw new InvalidOperationException("FairWorld.Forest needs at least one tree, each with a positive weight.");
        }

        foreach (var zone in Zones)
        {
            if (string.IsNullOrWhiteSpace(zone.Name) || zone.Name.Any(char.IsWhiteSpace))
            {
                throw new InvalidOperationException("Every FairWorld zone needs a single-word Name.");
            }

            if (zone.Polygon.Count < 3 || zone.Polygon.Any(p => p.Length != 2))
            {
                throw new InvalidOperationException($"FairWorld zone '{zone.Name}' needs at least three [x, y] points.");
            }

            if (zone.Marker is not { Length: 3 })
            {
                throw new InvalidOperationException($"FairWorld zone '{zone.Name}' needs a Marker [x, y, headingDegrees].");
            }
        }

        // Land must run at least two whole cells past the planned wall, or the player can
        // see the edge of the generated ground from inside the compound.
        var min = (-CellRadius + 2) * 4096f;
        var max = (CellRadius - 1) * 4096f;
        if (Perimeter.Any(p => p.Any(v => v < min || v > max)))
        {
            throw new InvalidOperationException(
                "FairWorld.Perimeter reaches too close to the edge of the generated land; raise CellRadius.");
        }
    }
}

/// <summary>
/// A project static: one NIF under <c>meshes\</c>, measured once so the generator can
/// lay it by its real size. Width runs along local X, depth along local Y, and the
/// origin is at the bottom centre.
/// </summary>
internal sealed record SingersConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairSinger";

    public string Name { get; init; } = "Singer";

    /// <summary>The singers' quest; short, so voice file names are never truncated.</summary>
    public string QuestEditorId { get; init; } = "SkyrimFairSingers";

    public string Package { get; init; } = "00025BFC:Skyrim.esm";

    public int EmotionValue { get; init; } = 50;

    /// <summary>tools/bards/build_vocals.py's output, one folder per song.</summary>
    public string CuesDir { get; init; } = "build/bards";

    /// <summary>Stage song name (audio.stage.songs[].name) to its cues folder.</summary>
    public Dictionary<string, string> Songs { get; init; } = new();

    /// <summary>Folders searched for the game's archives, for the singers' FaceGen files.</summary>
    public List<string> FaceArchives { get; init; } = new();

    public string MeshesOut { get; init; } = "assets/meshes";

    public string TexturesOut { get; init; } = "assets/textures";

    /// <summary>Where the voice files go (<c>Sound\Voice</c> in the game).</summary>
    public string VoiceOut { get; init; } = "assets/sound/Voice";

    public List<SingerMember> Members { get; init; } = new();

    /// <summary>Their own cheer (a retargeted Mixamo clip) in place of IdleCivilWarCheer's, through OAR.</summary>
    public SingerCheerOar CheerOar { get; init; } = new();

    /// <summary>What the singers keep their offset from as they step (songs.config.json singerSteps).</summary>
    public SingerAnchor Anchor { get; init; } = new();
}

/// <summary>
/// An invisible NPC under the deck, facing north with its AI off. The stage script has each
/// singer keep an offset from it (KeepOffsetFromActor), and moves the offsets sideways, so
/// the line steps across the deck still facing the crowd. It has its own FormID range.
/// </summary>
internal sealed record SingerAnchor
{
    /// <summary><c>[x, y, z]</c>; heading 0.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>Small, so it fits under the deck.</summary>
    public float Scale { get; init; } = 0.5f;

    public string Race { get; init; } = "00013746:Skyrim.esm";

    public uint FormIdBase { get; init; } = 0x40000;
}

/// <summary>songs.config.json "singerSteps": the singers' line stepping across the deck.</summary>
internal sealed record SingerSteps
{
    /// <summary>The line's sideways offsets, in turn, one a step; the first is where each song starts. None: they stand.</summary>
    public List<float> Offsets { get; init; } = new();

    public float Every { get; init; } = 8f;

    /// <summary>Seconds a step takes; gestures wait for it.</summary>
    public float StepSeconds { get; init; } = 3f;

    public float FirstStep { get; init; } = 6f;

    public float CatchUpRadius { get; init; } = 1000f;

    public float FollowRadius { get; init; } = 12f;
}

/// <summary>
/// The player's companions, brought through the fair's gate. Followers walk through a load
/// door only by navmesh door links, which neither the fair's generated navmesh nor Tamriel's
/// has at the new gate. So a quest of optional aliases finds the player's companions anywhere
/// (their persistent references, not only the loaded area): teammates or in
/// CurrentFollowerFaction, alive, not told to wait. The stage script restarts it on arriving
/// at the fair and on leaving, and moves each one it finds to the player.
/// </summary>
internal sealed record ShopsConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairShop";

    public uint FormIdBase { get; init; } = 0x90000;

    /// <summary>The interior cell the merchant chests stand in.</summary>
    public string HoldingCell { get; init; } = "SkyrimFairSandbox";

    /// <summary>JobMerchantFaction: DialogueGeneric's barter line is for its members.</summary>
    public string MerchantJobFaction { get; init; } = "00051596:Skyrim.esm";

    /// <summary>The vanilla vendor faction whose crime values ours take (a Khajiit caravan's).</summary>
    public string FactionTemplate { get; init; } = "ServicesThievesGuildCaravanZaynabi";

    /// <summary>The vanilla merchant chest whose model and bounds ours take.</summary>
    public string ChestTemplate { get; init; } = "MerchantWhiterunBelethorsGoodsChest";

    /// <summary>The gold item, and how much each keeper has for buying ("sell only": a little).</summary>
    public string Gold { get; init; } = "Gold001";

    public int GoldEach { get; init; } = 100;

    /// <summary>Theme (as the stalls': cheese, smith...) to its shop.</summary>
    public Dictionary<string, ShopTrade> Trades { get; init; } = new();
}

internal sealed record ShopTrade
{
    /// <summary>What it sells: items or leveled lists, by EditorID, with counts.</summary>
    public List<ShopItem> Stock { get; init; } = new();

    /// <summary>What it buys back: item keywords, by EditorID (VendorItemFood...).</summary>
    public List<string> Buys { get; init; } = new();

    /// <summary>Its own gold, if not the default.</summary>
    public int Gold { get; init; }
}

internal sealed record ShopItem
{
    public string Item { get; init; } = string.Empty;

    public int Count { get; init; } = 1;
}

internal sealed record LightsConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairGlow";

    public uint FormIdBase { get; init; } = 0x80000;

    /// <summary>WRFireLightNS: Whiterun's no-shadow firelight, radius 768.</summary>
    public string WallLight { get; init; } = "000BBAE5:Skyrim.esm";

    /// <summary>WRLightFireStreet01: Whiterun's street light, radius 512.</summary>
    public string LaneLight { get; init; } = "0008278A:Skyrim.esm";

    /// <summary>Lanterns within this of the wall line get the wall light.</summary>
    public float WallBand { get; init; } = 350f;

    public float WallSpacing { get; init; } = 800f;

    public float LaneSpacing { get; init; } = 700f;

    public int MaxWall { get; init; } = 16;

    public int MaxLanes { get; init; } = 14;

    /// <summary>How far under the lantern the light hangs.</summary>
    public float Below { get; init; } = 40f;
}

internal sealed record CompanionsConfig
{
    public bool Enabled { get; init; }

    public string QuestEditorId { get; init; } = "SkyrimFairCompanions";

    /// <summary>How many companions it brings at most.</summary>
    public int Slots { get; init; } = 6;

    public string FollowerFaction { get; init; } = "0005C84E:Skyrim.esm";

    /// <summary>Its own FormID range: one quest record.</summary>
    public uint FormIdBase { get; init; } = 0x70000;
}

internal sealed record CameosConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairCameo";

    /// <summary>Their own FormID range, so nothing renumbers.</summary>
    public uint FormIdBase { get; init; } = 0x60000;

    /// <summary>The music's volume, as a share, while one of them speaks.</summary>
    public float DuckLevel { get; init; } = 0.2f;

    /// <summary>The begin fragment on each of their lines (it ducks the music).</summary>
    public string LineScript { get; init; } = "SkyrimFairCameoLine";

    /// <summary>What a standing cameo runs: DefaultStayAtEditorLocation.</summary>
    public string StandPackage { get; init; } = "00025BFC:Skyrim.esm";

    /// <summary>The cameos' dialogue quest; short, so voice file names are never truncated.</summary>
    public string QuestEditorId { get; init; } = "SkyrimFairCameos";

    /// <summary>tools/cameos/build_voices.py's output, one folder per cameo id.</summary>
    public string VoiceBuildDir { get; init; } = "build/cameos";

    /// <summary>Barry's lines: {"garrick_sol_v": [{"file", "text"}, ...]} (read by build_voices.py).</summary>
    public string VoiceLinesFile { get; init; } = "cameos/skyrim_fair_voicelines.json";

    /// <summary>The sandbox each gets a wider copy of: DefaultSandboxEditorLocation1024NoConv.</summary>
    public string Package { get; init; } = "0010F587:Skyrim.esm";

    public List<CameoMember> Members { get; init; } = new();

    /// <summary>A horse on the stage roof (the recurring "horse on a roof").</summary>
    public RoofHorseConfig RoofHorse { get; init; } = new();

    /// <summary>The Fair Inspector's notices on the palisade's inner face.</summary>
    public PostersConfig Posters { get; init; } = new();
}

internal sealed record PostersConfig
{
    public bool Enabled { get; init; }

    public List<PosterDesign> Designs { get; init; } = new();

    /// <summary>The design hung once (Garrick's statement), on the free panel nearest <see cref="FeatureNear"/>.</summary>
    public string Feature { get; init; } = string.Empty;

    public float[] FeatureNear { get; init; } = Array.Empty<float>();

    public float FeatureScale { get; init; } = 3f;

    /// <summary>The meshes are 19 x 26 (converted at 40 units a metre); 2.5 makes them about 48 x 65.</summary>
    public float Scale { get; init; } = 2.5f;

    /// <summary>From the panel's centre line to the paper's back, on the fair's side.</summary>
    public float Out { get; init; } = 24f;

    /// <summary>The paper's centre above the panel's foot.</summary>
    public float Height { get; init; } = 150f;

    /// <summary>One poster every this many free panels.</summary>
    public int Every { get; init; } = 2;

    public float GateClear { get; init; } = 600f;

    /// <summary>No poster on a panel within this of a banner.</summary>
    public float BannerClear { get; init; } = 100f;
}

internal sealed record PosterDesign
{
    public string Id { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public float Width { get; init; } = 20f;

    public float Depth { get; init; } = 2f;

    public float Height { get; init; } = 13f;
}

internal sealed record RoofHorseConfig
{
    public bool Enabled { get; init; }

    /// <summary>The vanilla horse copied: its looks, with the fair's package and script.</summary>
    public string Horse { get; init; } = "00068D5B:Skyrim.esm";

    /// <summary>DefaultStayAtEditorLocation; the script also holds it still (SetDontMove).</summary>
    public string Package { get; init; } = "00025BFC:Skyrim.esm";

    public string Script { get; init; } = "SkyrimFairRoofHorse";

    /// <summary><c>[x, y, z, yaw]</c>, world: where it stands, on the platform.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>The platform's planks: StockadeScaffoldTop0Sided01, 248 x 262, 19 thick.</summary>
    public string DeckPiece { get; init; } = "000533C0:Skyrim.esm";

    /// <summary>Each plank's <c>[x, y, z, yaw]</c>, world.</summary>
    public List<float[]> Deck { get; init; } = new();
}

internal sealed record CameoMember
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ShortName { get; init; } = string.Empty;

    /// <summary>The vanilla NPC whose face, race and voice he takes (a Traits template).</summary>
    public string Template { get; init; } = string.Empty;

    public string Outfit { get; init; } = string.Empty;

    /// <summary><c>[x, y, z, yaw]</c>: where he starts, and the middle of his sandbox.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>How far from <see cref="At"/> he wanders.</summary>
    public float Radius { get; init; } = 1500f;

    public int Energy { get; init; } = 50;

    /// <summary>His key in the lines file; empty: he says nothing of his own.</summary>
    public string VoiceLines { get; init; } = string.Empty;

    /// <summary>His recordings (build_voices.py reads them).</summary>
    public string VoiceDir { get; init; } = string.Empty;

    /// <summary>His own voice type, so only his lines play for him.</summary>
    public string VoiceType { get; init; } = string.Empty;

    /// <summary>The emotion his lines play with (Happy, Neutral, Puzzled...).</summary>
    public string Emotion { get; init; } = "Neutral";

    /// <summary>His idles, in turn: each played, then stopped after its hold (seconds).</summary>
    public List<CameoIdle> Idles { get; init; } = new();

    /// <summary>Seconds between idles: <c>[min, max]</c>.</summary>
    public float[] Every { get; init; } = { 40f, 90f };

    /// <summary>He stands where he's put (his round kept, not walked).</summary>
    public bool Stand { get; init; }

    /// <summary>A visitor's reference he takes the place of (it's disabled): he stands there.</summary>
    public string Replaces { get; init; } = string.Empty;

    /// <summary>His round: <c>[x, y]</c> spots he walks between, sandboxing at each (2 or more).</summary>
    public List<float[]> Spots { get; init; } = new();

    public float SpotRadius { get; init; } = 350f;

    /// <summary>Seconds at a spot before he moves on: <c>[min, max]</c>.</summary>
    public float[] Move { get; init; } = { 60f, 120f };
}

internal sealed record CameoIdle
{
    public string Idle { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public float Hold { get; init; } = 10f;

    /// <summary>The idle that ends it (a paired exit, as Hadvar's ledger has); empty: the band's stop.</summary>
    public string Stop { get; init; } = string.Empty;
}

/// <summary>
/// An OAR submod that swaps a vanilla clip for the singers only: the clip copied from
/// <see cref="Source"/> into <see cref="Folder"/>/<see cref="Submod"/> under <see cref="Clip"/>'s
/// name, and its config written with the singers' own records (IsActorBase: the singers
/// aren't templated, so their base records are theirs in game).
/// </summary>
internal sealed record SingerCheerOar
{
    public bool Enabled { get; init; }

    public string Source { get; init; } = string.Empty;

    public string Folder { get; init; } = "assets/meshes/actors/character/animations/OpenAnimationReplacer/SkyrimFairBardSinger";

    public string Submod { get; init; } = "Singers";

    /// <summary>The vanilla clip it replaces: IdleCivilWarCheer's, the singers' "sing" move.</summary>
    public string Clip { get; init; } = "special_civilwarcheer.hkx";

    public int Priority { get; init; } = 1900000100;
}

internal sealed record SingerMember
{
    /// <summary>The singer's id in cues.json (lead, left, right).</summary>
    public string Id { get; init; } = string.Empty;

    public string VoiceType { get; init; } = string.Empty;

    /// <summary>The vanilla NPC whose face (record data and FaceGen files) he gets.</summary>
    public string Face { get; init; } = string.Empty;

    public string Outfit { get; init; } = string.Empty;

    /// <summary><c>[x, y, z, yaw]</c>.</summary>
    public float[] At { get; init; } = Array.Empty<float>();
}

internal sealed record CrowdPlacement
{
    public string Figure { get; init; } = string.Empty;

    /// <summary><c>[x, y, yaw]</c> on the ground (standing and leaning figures).</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>Or on a seat: the furniture reference nearest <c>[x, y]</c>, and which of its markers.</summary>
    public float[] Seat { get; init; } = Array.Empty<float>();

    public int Marker { get; init; }
}

/// <summary>
/// A furniture base's seat markers, <c>[x, y, heading]</c> in its own frame (the heading the
/// sitter faces, degrees clockwise from the furniture's +Y), and its non-sittable twin: a
/// seat given to a figure becomes the twin, so no NPC sits down inside the figure.
/// </summary>
internal sealed record SeatMarkers
{
    public string Furniture { get; init; } = string.Empty;

    public string StaticTwin { get; init; } = string.Empty;

    public List<float[]> Markers { get; init; } = new();
}

internal sealed record CrowdFigure : ProjectStaticConfig
{
    /// <summary>Where copies stand: <c>[x, y, yaw]</c> each, on the ground. The figure faces +Y at yaw 0.</summary>
    public List<float[]> Places { get; init; } = new();

    /// <summary>
    /// An invisible collision box round each copy, in the figure's frame:
    /// <c>[minX, minY, maxX, maxY, height]</c>. Empty: 80% of the bounds, full height.
    /// </summary>
    public float[] Collision { get; init; } = Array.Empty<float>();

    /// <summary>False leaves the figure walk-through (a background figure nobody reaches).</summary>
    public bool Solid { get; init; } = true;
}

/// <summary>
/// Protects the fair's NPCs from damage, as vanilla children are, and has a script take
/// other mods' spells off them as they load. Some mods give every NPC a cloak or a
/// per-effect script; at the fair's density that floods Papyrus (docs/CODEX_HANDOVER.md).
/// </summary>
/// <summary>A copy of another mod's _DISTR.ini with -SkyrimFairNPC added to the named spells' lines.</summary>
internal sealed record SpidPatch
{
    /// <summary>The mod's current _DISTR.ini, read on every build.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The spells' lines to exclude the fair's NPCs from, as they're written in the ini (<c>0x817~Plugin.esp</c>).</summary>
    public List<string> Spells { get; init; } = new();
}

internal sealed record NpcGuardConfig
{
    public bool Enabled { get; init; }

    public bool Invulnerable { get; init; } = true;

    /// <summary>The Actor script put on every fair NPC record.</summary>
    public string Script { get; init; } = "SkyrimFairNpcGuard";

    /// <summary>Spells taken off, as <c>plugin|hex id</c>; a plugin that isn't loaded is skipped.</summary>
    public List<string> StripSpells { get; init; } = new();
}

/// <summary>
/// A pair of dancers who play Astra's folk dance (tools/folk/rebase_folk.py). Each clip
/// replaces vanilla's Cicero dance for that one NPC through an Open Animation Replacer
/// condition the generator writes, and the stage script starts both together in songs.
/// </summary>
internal sealed record FolkDanceConfig
{
    public bool Enabled { get; init; }

    /// <summary>The pair's centre, <c>[x, y]</c>, and heading (degrees clockwise from +Y).</summary>
    public float[] Centre { get; init; } = Array.Empty<float>();

    public float Heading { get; init; }

    /// <summary>The idle the clips replace (its animation file is what OAR swaps).</summary>
    public string Idle { get; init; } = "000F7C8A:Skyrim.esm";

    /// <summary>The clip's length in seconds: the script restarts the pair this often.</summary>
    public float Length { get; init; } = 9.6f;

    public string Name { get; init; } = "Folk Dancer";

    /// <summary>The OAR mod folder the generator writes the conditions into (the clips are put there by the tool).</summary>
    public string OarFolder { get; init; } = string.Empty;

    public List<FolkDancer> Dancers { get; init; } = new();
}

internal sealed record FolkDancer
{
    /// <summary>The OAR submod folder holding this dancer's clip.</summary>
    public string Submod { get; init; } = string.Empty;

    /// <summary>The visitor record whose looks are copied.</summary>
    public string Look { get; init; } = string.Empty;

    /// <summary>Where the dancer's clip starts, from the pair's centre: <c>[x, y, heading]</c>.</summary>
    public float[] At { get; init; } = Array.Empty<float>();
}

internal record ProjectStaticConfig
{
    public string EditorId { get; init; } = string.Empty;

    /// <summary>Path under <c>meshes\</c>, as the STAT's MODL stores it.</summary>
    public string Model { get; init; } = string.Empty;

    public float Width { get; init; }

    public float Depth { get; init; }

    public float Height { get; init; }

    /// <summary>Lowest point below the origin (props that hang or sit round their origin).</summary>
    public float MinZ { get; init; }

    public float Scale { get; init; } = 1f;
}

/// <summary>
/// The perimeter wall: repeated palisade panels laid edge by edge along the planned
/// outline, turned to each edge, overlapped so nothing shows through, and jittered a
/// little so the line looks built by hand rather than plotted.
/// </summary>
internal sealed record PalisadeConfig : ProjectStaticConfig
{
    /// <summary>Fraction of a panel's width each panel overlaps the next.</summary>
    public float Overlap { get; init; } = 0.06f;

    /// <summary>How far each edge's run carries past the vertex, closing the corner.</summary>
    public float CornerExtension { get; init; } = 48f;

    /// <summary>How far the panels either side of the gate tuck into its posts.</summary>
    public float GateTuck { get; init; } = 32f;

    public float YawJitterDegrees { get; init; } = 1.2f;

    /// <summary>Largest sideways wander off the outline.</summary>
    public float OffsetJitter { get; init; } = 5f;

    /// <summary>Largest fractional change of scale per panel, so the crest line varies.</summary>
    public float ScaleJitter { get; init; } = 0.04f;

    /// <summary>Largest sink into the ground per panel.</summary>
    public float SinkMax { get; init; } = 20f;

    /// <summary>Chance a panel is turned round, so the same face does not repeat along the wall.</summary>
    public float FlipChance { get; init; } = 0.5f;

    /// <summary>
    /// FormIDs the wall holds. Bigger panels need fewer; the rest are left unused, so a
    /// change of scale or gate width never renumbers the records built after the wall.
    /// 89 is the count at scale 2.5. 0 reserves nothing.
    /// </summary>
    public int ReservedPanels { get; init; }
}

internal sealed record GatePieceConfig : ProjectStaticConfig
{
    /// <summary>Added to the facing: 180 turns the gate round if its front is on the other side.</summary>
    public float YawOffsetDegrees { get; init; }

    public float Sink { get; init; } = 4f;
}

/// <summary>
/// Scenery conifers outside the wall. Candidates come from a jittered grid over the
/// band beyond the perimeter and are kept by a density that falls with distance and is
/// broken up by a low-frequency clustering field, so the trees stand in clumps with
/// clearings between them rather than in a ring.
/// </summary>
internal sealed record ForestConfig
{
    public bool Enabled { get; init; } = true;

    public List<ForestTree> Trees { get; init; } = new();

    /// <summary>Candidate spacing; each candidate wanders up to <see cref="Jitter"/> of it.</summary>
    public float GridSpacing { get; init; } = 440f;

    public float Jitter { get; init; } = 0.35f;

    /// <summary>Distance beyond the wall where the forest is densest.</summary>
    public float DenseFrom { get; init; } = 700f;

    public float DenseTo { get; init; } = 2600f;

    /// <summary>Beyond this distance no tree is placed.</summary>
    public float OuterDistance { get; init; } = 5200f;

    /// <summary>Chance a candidate is kept in the densest band, before clustering.</summary>
    public float PeakDensity { get; init; } = 0.8f;

    /// <summary>Size of the clumps and clearings.</summary>
    public float ClusterPeriod { get; init; } = 1700f;

    /// <summary>Clustering values below this are clearings: sky between the clumps.</summary>
    public float ClearingThreshold { get; init; } = 0.34f;

    /// <summary>No tree within this distance of the gate.</summary>
    public float GateClearRadius { get; init; } = 700f;

    /// <summary>
    /// Half-angle of the clearing kept straight out of the gate, like a path leading
    /// away. It runs only <see cref="GateApproachLength"/> out, so the forest closes
    /// behind it and the view through the open gate is of trees and mountains.
    /// </summary>
    public float GateApproachDegrees { get; init; } = 18f;

    public float GateApproachLength { get; init; } = 2200f;

    /// <summary>Largest lean off vertical, degrees.</summary>
    public float LeanDegrees { get; init; } = 1.5f;

    /// <summary>How far each trunk is sunk below the ground at its position.</summary>
    public float Sink { get; init; } = 24f;

    /// <summary>Varies the hash streams, so a second layer doesn't repeat the first's grid.</summary>
    public int Salt { get; init; }
}

/// <summary>
/// Vanilla mountain and ridge meshes beyond the forest, row by row round the compound
/// centre; pieces are sunk so their lowest point sits at <see cref="BaseZ"/>, well below
/// anything the wall lets the player see.
///
/// They are **large references**, as vanilla's mountains are: ordinary references in
/// their own cells, listed in the world's large-reference table (RNAM), which the game
/// loads within uLargeRefLODGridSize (11: five cells round the player) rather than
/// uGridsToLoad (5: two cells). The first build placed them Persistent and Is Full LOD
/// in the persistent cell instead; that doesn't load a reference whose cell is outside
/// the grid, so from the middle of the fair only the nearest showed.
/// </summary>
internal sealed record MountainsConfig
{
    public bool Enabled { get; init; } = true;

    public float BaseZ { get; init; } = -1000f;

    /// <summary>Place them as large references (see above); false for the old persistent Full LOD refs.</summary>
    public bool LargeReferences { get; init; } = true;

    /// <summary>
    /// Every large reference must lie within this many cells of the world's origin, so it
    /// is inside the large-reference grid from anywhere in the compound (cells -1..1).
    /// </summary>
    public int LargeReferenceCellLimit { get; init; } = 4;

    public List<MountainRow> Rows { get; init; } = new();
}

internal sealed record MountainRow
{
    public string Name { get; init; } = string.Empty;

    public int Count { get; init; }

    public float MinRadius { get; init; }

    public float MaxRadius { get; init; }

    /// <summary>Fraction of the even angular spacing each piece may wander.</summary>
    public float AngleJitter { get; init; } = 0.35f;

    /// <summary>Turns the row's pattern so its pieces do not line up with the other row's.</summary>
    public float StartDegrees { get; init; }

    public List<MountainPiece> Pieces { get; init; } = new();

    /// <summary>Pieces in each group, from Min to Max: overlapping clumps instead of a ring of single pieces.</summary>
    public int ClusterMin { get; init; } = 1;

    public int ClusterMax { get; init; } = 1;

    /// <summary>How far a group's pieces spread round from its angle (degrees) and in radius.</summary>
    public float ClusterSpreadDegrees { get; init; }

    public float ClusterRadiusSpread { get; init; }

    /// <summary>Chance a group is left out: sky between the clumps.</summary>
    public float GapChance { get; init; }

    /// <summary>The row's own base Z; null uses <see cref="MountainsConfig.BaseZ"/>.</summary>
    public float? BaseZ { get; init; }

    /// <summary>
    /// Generated after everything else in the world, so a new row doesn't move the FormIDs
    /// of the records after the first rows (the archers' packages).
    /// </summary>
    public bool PlaceLast { get; init; }

    /// <summary>Pieces placed by hand, as angle and radius round the compound centre (the gate view).</summary>
    public List<PinnedMountain> Pinned { get; init; } = new();
}

internal sealed record PinnedMountain
{
    public string Name { get; init; } = string.Empty;

    public string FormKey { get; init; } = string.Empty;

    /// <summary>Degrees clockwise from north, round the compound centre.</summary>
    public float Angle { get; init; }

    public float Radius { get; init; }

    public float Scale { get; init; } = 1f;

    public float Yaw { get; init; }
}

/// <summary>The fair's generated navmesh (FairNavmesh.cs, docs/NAVMESH.md).</summary>
internal sealed record NavmeshConfig
{
    public bool Enabled { get; init; }

    /// <summary>Raster size; it must divide the cell size (4,096).</summary>
    public float Resolution { get; init; } = 32f;

    /// <summary>How far inside the palisade the mesh stops.</summary>
    public float WallMargin { get; init; } = 48f;

    /// <summary>Padding round every obstacle: an actor's radius.</summary>
    public float ActorRadius { get; init; } = 32f;

    /// <summary>An obstacle's top must be this far above the ground, and its bottom below head height.</summary>
    public float MinObstacleHeight { get; init; } = 12f;

    public float HeadHeight { get; init; } = 160f;

    /// <summary>Smaller things (clutter) don't block.</summary>
    public float MinFootprint { get; init; } = 12f;

    /// <summary>Radius opened again under every actor's feet (0: none).</summary>
    public float ActorClearance { get; init; } = 24f;

    /// <summary>Smallest separate island kept (an actor's pocket, the stage), in raster cells.</summary>
    public int MinIslandCells { get; init; } = 4;

    /// <summary>Largest rectangle side, in raster cells.</summary>
    public int MaxRectangle { get; init; } = 16;

    /// <summary>
    /// Per-model footprints from tools/make_footprints.py: occupied cells in height bands,
    /// so an object blocks only where its geometry is (posts, counters, rails), not its
    /// whole bounding box. Models not in it fall back to their bounds.
    /// </summary>
    public string Footprints { get; init; } = "tools/navmesh_footprints.json";

    /// <summary>Optional: a text dump of the raster (region, free, kept), for debugging.</summary>
    public string DebugRaster { get; init; } = string.Empty;

    /// <summary>Where the build lists the models it cut obstacles for, for make_footprints.py.</summary>
    public string ModelList { get; init; } = "build/navmesh_models.txt";

    /// <summary>How far in from a platform's edges (the stage deck, its ramp) the mesh stops.</summary>
    public float PlatformMargin { get; init; } = 32f;

    /// <summary>On the ramp up the steps, how far above it a tread may stand without blocking.</summary>
    public float StepTolerance { get; init; } = 45f;

    /// <summary>Plugins whose bases' bounds are needed (Holidays.esp); missing files are skipped.</summary>
    public List<string> ExtraMasters { get; init; } = new();

    /// <summary>Footprint for a base whose bounds can't be read.</summary>
    public float UnknownHalfWidth { get; init; } = 30f;

    public float UnknownHeight { get; init; } = 120f;

    /// <summary>Bases that never block (FormKeys).</summary>
    public List<string> IgnoreBases { get; init; } = new();

    /// <summary>Vanilla's navmesh info map, overridden with the fair's entries.</summary>
    public string InfoMap { get; init; } = "00012FB4:Skyrim.esm";

    /// <summary>
    /// The vanilla entries every navmesh plugin in the load order copies into its override
    /// (Holidays and Fertility Adventures carry the same ten).
    /// </summary>
    public List<string> InfoMapSeeds { get; init; } = new();
}

internal sealed record MountainPiece
{
    public string Name { get; init; } = string.Empty;

    public string FormKey { get; init; } = string.Empty;

    public float Weight { get; init; } = 1f;

    public float MinScale { get; init; } = 1f;

    public float MaxScale { get; init; } = 1f;

    /// <summary>
    /// For one-sided meshes (MountainCliffSlope, MountainCliffSm01: finished on local +Y,
    /// open on -Y): turn the finished side to the compound, within
    /// <see cref="FaceJitter"/> degrees, instead of a random heading.
    /// </summary>
    public bool FacesCentre { get; init; }

    public float FaceJitter { get; init; } = 25f;
}

internal sealed record ForestTree
{
    public string FormKey { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>Relative frequency.</summary>
    public float Weight { get; init; } = 1f;

    public float MinScale { get; init; } = 0.8f;

    public float MaxScale { get; init; } = 1.2f;

    /// <summary>Nearest the wall this tree may stand, so its canopy does not hang over.</summary>
    public float MinDistance { get; init; } = 450f;

    /// <summary>Furthest from the wall this tree may stand.</summary>
    public float MaxDistance { get; init; } = float.MaxValue;

    /// <summary>Its own sink below the ground (rocks bed deeper than trunks); null uses the layer's.</summary>
    public float? Sink { get; init; }
}

/// <summary>
/// The ground: exactly flat inside the perimeter and for <see cref="FlatMargin"/>
/// beyond it, so the palisade will stand on level ground wherever it is finally drawn,
/// then rising gently into low hills that close the view where the wall is not yet.
/// </summary>
internal sealed record FairWorldTerrain
{
    public float FlatMargin { get; init; } = 1024f;

    /// <summary>Distance over which the ground climbs from the floor to its full rise.</summary>
    public float RiseDistance { get; init; } = 8192f;

    public float RiseHeight { get; init; } = 1536f;

    /// <summary>Undulation added outside the flat area, scaled by how far up the rise it is.</summary>
    public float NoiseAmplitude { get; init; } = 160f;

    public float NoisePeriod { get; init; } = 2048f;
}

/// <summary>Vanilla LTEX records painted into the landscape. Referenced, never copied.</summary>
internal sealed record FairWorldTextures
{
    /// <summary>Inside the compound. LFieldGrass01, the Whiterun tundra field with grass.</summary>
    public string Ground { get; init; } = "00013428:Skyrim.esm";

    /// <summary>Beyond the perimeter. LTundra01, rougher tundra with grass.</summary>
    public string Outside { get; init; } = "00024E30:Skyrim.esm";

    /// <summary>The perimeter strip. LTundraRocks01NoRocks.</summary>
    public string Perimeter { get; init; } = "0006DE8B:Skyrim.esm";

    /// <summary>The avenue. LDirtPath01, bare path with no grass.</summary>
    public string Avenue { get; init; } = "000B424C:Skyrim.esm";
}

internal sealed record FairWorldZone
{
    public string Name { get; init; } = string.Empty;

    /// <summary>LTEX painted over the zone.</summary>
    public string Texture { get; init; } = string.Empty;

    public List<float[]> Polygon { get; init; } = new();

    /// <summary><c>[x, y, headingDegrees]</c> for the zone's named marker.</summary>
    public float[] Marker { get; init; } = Array.Empty<float>();

    /// <summary>Height of the marker above the floor, when it stands on something (the stage deck).</summary>
    public float MarkerHeight { get; init; }
}

/// <summary>
/// The main stage: an open timber pavilion built from vanilla pieces, placed from the
/// configured stage zone. Everything below is in the stage's own frame: <c>u</c> runs
/// across the stage (positive to the performers' left, the audience's right), <c>v</c> runs
/// toward the audience, and the origin is the deck centre on the ground. The deck centre
/// is the zone marker moved <see cref="ForwardOffset"/> toward the audience, and the stage
/// faces the marker's heading.
/// </summary>
internal sealed record StageConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Zone whose marker places and turns the stage.</summary>
    public string Zone { get; init; } = "Stage";

    public float ForwardOffset { get; init; } = 60f;

    /// <summary>Top of the performance deck above the ground.</summary>
    public float DeckHeight { get; init; } = 134f;

    public StageDeck Deck { get; init; } = new();

    public StageSkirt Skirt { get; init; } = new();

    public StageSteps Steps { get; init; } = new();

    /// <summary>The log used for every post, beam, rafter and brace. Its length runs along local Y, centred.</summary>
    public StageLog Log { get; init; } = new();

    /// <summary>Upright posts, <c>[u, v]</c>, standing on the ground.</summary>
    public List<float[]> Posts { get; init; } = new();

    public float PostHeight { get; init; } = 700f;

    /// <summary>Horizontal beams: straight runs between two plan points at a height.</summary>
    public List<StageBeam> Beams { get; init; } = new();

    public StageRafters Rafters { get; init; } = new();

    /// <summary>X-braced bays: two crossing diagonals in the vertical plane between two plan points.</summary>
    public List<StageBrace> Braces { get; init; } = new();

    /// <summary>Hand-built wander: largest height change of a log.</summary>
    public float ZJitter { get; init; } = 5f;

    public float YawJitterDegrees { get; init; } = 1f;

    public float ScaleJitter { get; init; } = 0.03f;

    /// <summary>Largest lean of a post, degrees.</summary>
    public float PostLeanDegrees { get; init; } = 0.8f;

    /// <summary>
    /// Fire braziers standing on the deck, <c>[u, v]</c>: the same Windhelm fire basket on a
    /// stand, with the same fire above it, as Barry placed at the Tamriel stair foot.
    /// </summary>
    public List<float[]> Braziers { get; init; } = new();

    public string BrazierPiece { get; init; } = "00093A89:Skyrim.esm";

    /// <summary>How far the brazier's feet reach below its origin (its OBND Z min).</summary>
    public float BrazierFeet { get; init; } = 33f;

    public string FirePiece { get; init; } = "00033DA4:Skyrim.esm";

    /// <summary>The fire's offset from the brazier, <c>[x, y, z]</c>, as at the stair foot.</summary>
    public float[] FireOffset { get; init; } = { -6f, -2f, 92f };
}

internal sealed record StageDeck
{
    public string Piece { get; init; } = "0001C570:Skyrim.esm";

    public string Name { get; init; } = "Walkway01";

    /// <summary>Along the piece's local X (the stage's width).</summary>
    public float PieceWidth { get; init; } = 272f;

    /// <summary>Along the piece's local Y (the stage's depth).</summary>
    public float PieceDepth { get; init; } = 256f;

    /// <summary>Where the piece's plank surface is centred on its local X (measured from the mesh).</summary>
    public float PieceCentreX { get; init; } = -15f;

    /// <summary>Where the piece's plank surface is centred on its local Y.</summary>
    public float PieceCentreY { get; init; } = -5f;

    /// <summary>Height of the walking surface above the piece's origin.</summary>
    public float PieceTopZ { get; init; } = 6f;

    public int Columns { get; init; } = 6;

    public int Rows { get; init; } = 3;
}

internal sealed record StageSkirt
{
    public bool Enabled { get; init; } = true;

    public string Piece { get; init; } = "000533D8:Skyrim.esm";

    public string Name { get; init; } = "StockadeWoodplanks04";

    /// <summary>Along the panel's local Y.</summary>
    public float Length { get; init; } = 250f;

    /// <summary>Height of the panel's top above its origin.</summary>
    public float TopZ { get; init; } = 79f;

    /// <summary>How far below the deck surface the panel's top sits.</summary>
    public float BelowDeck { get; init; } = 4f;

    /// <summary>Distance outside the deck edge.</summary>
    public float Gap { get; init; } = 5f;

    /// <summary>Faces to board: any of <c>front</c>, <c>back</c>, <c>left</c>, <c>right</c>.</summary>
    public List<string> Faces { get; init; } = new() { "front", "left", "right", "back" };
}

internal sealed record StageSteps
{
    public string Piece { get; init; } = "000533C0:Skyrim.esm";

    public string Name { get; init; } = "StockadeScaffoldTop0Sided01";

    public float PieceWidth { get; init; } = 248f;

    public float PieceDepth { get; init; } = 262f;

    public float PieceTopZ { get; init; } = 4f;

    /// <summary>Treads between the ground and the deck; the risers are equal.</summary>
    public int Treads { get; init; } = 5;

    /// <summary>Pieces side by side across each tread.</summary>
    public int Across { get; init; } = 3;

    /// <summary>Visible depth of each tread; the rest runs under the tread above.</summary>
    public float Exposed { get; init; } = 110f;

    /// <summary>Board closing each riser, standing at the tread's front edge. Length along its local Y.</summary>
    public string RiserPiece { get; init; } = "000533D5:Skyrim.esm";

    public string RiserName { get; init; } = "StockadeWoodplanks01";

    public float RiserLength { get; init; } = 250f;

    /// <summary>Height of the board's top above its origin.</summary>
    public float RiserTopZ { get; init; } = 36f;
}

internal sealed record StageLog
{
    public string Piece { get; init; } = "000533D0:Skyrim.esm";

    public string Name { get; init; } = "StockadeWoodbeam01";

    public float Length { get; init; } = 306f;

    /// <summary>Scale for horizontal beams: sets their thickness. Runs are filled with as many as needed.</summary>
    public float BeamScale { get; init; } = 2f;

    /// <summary>Overlap between consecutive logs in a run.</summary>
    public float Overlap { get; init; } = 30f;
}

internal sealed record StageBeam
{
    public float[] From { get; init; } = Array.Empty<float>();

    public float[] To { get; init; } = Array.Empty<float>();

    public float Z { get; init; }
}

internal sealed record StageRafters
{
    /// <summary>Across-stage positions of the rafters.</summary>
    public List<float> U { get; init; } = new();

    public float FromV { get; init; } = -470f;

    public float ToV { get; init; } = 470f;

    public float Z { get; init; } = 700f;

    public float Scale { get; init; } = 1.6f;
}

internal sealed record StageBrace
{
    public float[] From { get; init; } = Array.Empty<float>();

    public float[] To { get; init; } = Array.Empty<float>();

    public float ZLow { get; init; }

    public float ZHigh { get; init; }
}


/// <summary>
/// A private interior cell, paved with the project's own floor kit and open to the
/// sky, for looking at pieces in isolation. It is reached with the console:
/// <c>coc &lt;EditorId&gt;</c> to go in, <c>cow Tamriel &lt;x&gt; &lt;y&gt;</c> to come back
/// out at the fair site. It is not part of the fair and has no doors into the world.
/// </summary>
internal sealed record SandboxConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>The name typed after <c>coc</c>. Must be unique across the load order.</summary>
    public string EditorId { get; init; } = "SkyrimFairSandbox";

    /// <summary>Shown on the loading screen and in the HUD when entering.</summary>
    public string Name { get; init; } = "Skyrim Fair Sandbox";

    /// <summary>Floor tiles per side. Each tile is the 1024-unit fill from the foundation kit.</summary>
    public int Tiles { get; init; } = 5;

    /// <summary>Top surface of the paving. Everything in the sandbox stands on this.</summary>
    public float FloorZ { get; init; } = 0f;

    /// <summary>LGTM the cell inherits all its lighting from. DefaultLightingTemplate.</summary>
    public string LightingTemplate { get; init; } = "000300E2:Skyrim.esm";

    /// <summary>REGN whose weather list drives the visible sky. WeatherTundraNoPrecip.</summary>
    public string WeatherRegion { get; init; } = "001046C9:Skyrim.esm";

    /// <summary>IMGS applied in the cell. DefaultImageSpaceExterior.</summary>
    public string ImageSpace { get; init; } = "00000161:Skyrim.esm";

    /// <summary>STAT placed at the centre so <c>coc</c> has somewhere to put the player. COCMarkerHeading.</summary>
    public string CocMarker { get; init; } = "00000032:Skyrim.esm";

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditorId) || EditorId.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                "Sandbox.EditorId must be a single word: it is typed into the console after 'coc'.");
        }

        if (Tiles < 1 || Tiles > 16)
        {
            throw new InvalidOperationException("Sandbox.Tiles must be between 1 and 16.");
        }
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
        // Hidden box-collider slope under the vanilla staircase, one per flight.
        ["stairCollision"] = new() { EditorId = "SkyrimFairStairCollision", Model = @"SkyrimFair\SkyrimFair_StairCollision.nif" },
        // Project-authored flight of steps with its own box collider; no wall.
        ["stair"] = new() { EditorId = "SkyrimFairStair192", Model = @"SkyrimFair\SkyrimFair_Stair_192.nif" },
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
            "paveCapFill", "paveCapEdge", "rampCap", "stairCollision", "stair",
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
    /// An exposed cliff-skin end is tucked this far inside the corner it would
    /// otherwise show past. The corner stone and the retaining body then hide it.
    /// </summary>
    public float CliffEndInset { get; init; } = 96f;

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

    public PerimeterWallConfig PerimeterWall { get; init; } = new();

    public EntranceBankConfig EntranceBank { get; init; } = new();

    public EntranceCheekConfig EntranceCheeks { get; init; } = new();

    public TerraceBandConfig TerraceBand { get; init; } = new();

    public IReadOnlyList<EntranceDressingItem> EntranceDressing { get; init; } = new List<EntranceDressingItem>();

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

    /// <summary>
    /// Use the project-authored flight (steps only, collider built in) instead of
    /// the vanilla StonewallTerraceStairs01, whose 666-wide wall read as a gate.
    /// </summary>
    public bool KitStair { get; init; } = true;

    /// <summary>Half the walking width of one flight at scale 1: 167 on both the vanilla and the kit flight.</summary>
    public float StairHalfWidth { get; init; } = 83.5f;

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

    /// <summary>Project kit role for each closed retaining wing beside the stair head.</summary>
    public string RetainWingRole { get; init; } = "entranceRetainWing";

    /// <summary>Lateral centre of each 144-wide wing in the 512-wide entrance segment.</summary>
    public float RetainWingOffset { get; init; } = 184f;
}


/// <summary>
/// The stone wall at the top of the perimeter: ordinary drystone field walls, stacked
/// in courses that step outward as they go down so the face is battered rather than
/// vertical. Small repeated pieces, not one slab.
/// </summary>
internal sealed record PerimeterWallConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>`Stonewall01`, the 256-wide, 175-tall drystone field wall.</summary>
    public string Piece { get; init; } = "0000099B:Skyrim.esm";

    /// <summary>Fallback height if the piece has no usable bounds.</summary>
    public float CourseHeight { get; init; } = 175f;

    /// <summary>How far each course steps out from the one above. This is the batter.</summary>
    public float CourseBatter { get; init; } = 48f;

    /// <summary>Courses deep edges may stack. Three covers about 525 units.</summary>
    public int MaxCourses { get; init; } = 3;

    /// <summary>
    /// Compass edges this language is prototyped on. Empty means the whole perimeter.
    /// Kept to one edge first so the new treatment can be compared against the old on
    /// the same site before it is rolled out.
    /// </summary>
    public IReadOnlyList<string> PrototypeEdges { get; init; } = new[] { "N" };

    /// <summary>Shortest and longest run of segments one masonry stretch covers.</summary>
    public int MinStretch { get; init; } = 2;

    public int MaxStretch { get; init; } = 3;

    /// <summary>
    /// Chance a stretch gets a second, lower course. Not every stretch does, which is
    /// what breaks the height into varying numbers of visual tiers.
    /// </summary>
    public double SecondCourseChance { get; init; } = 0.55;

    /// <summary>How far a whole stretch may be pushed out, so no two line up.</summary>
    public float OffsetJitter { get; init; } = 96f;

    /// <summary>Sideways wander within a segment, so the run is not a ruled line.</summary>
    public float AlongJitter { get; init; } = 64f;

    /// <summary>
    /// Per-edge chance that any given stretch is masonry rather than left to rock and
    /// planting. Deliberately uneven: the fair should read as more built on one side.
    /// </summary>
    public Dictionary<string, float> MasonryBias { get; init; } = new()
    {
        ["N"] = 0.7f, ["W"] = 0.5f, ["S"] = 0.35f, ["E"] = 0.25f,
    };

    /// <summary>Share of the local drop the rock ending a run is sized to.</summary>
    public float TerminalRockShare { get; init; } = 0.8f;

    /// <summary>How far below the crest that rock is sunk, so it reads part-buried.</summary>
    public float TerminalRockSink { get; init; } = 64f;

    /// <summary>
    /// On the entrance edge, no masonry within this distance of the stair centreline.
    /// The flights bring their own walls; more beside them reads as a gatehouse.
    /// </summary>
    public float EntranceClear { get; init; } = 900f;

    /// <summary>Scale applied to every course piece. Below 1 gives the lower walls of the concept.</summary>
    public float PieceScale { get; init; } = 1f;

    /// <summary>The piece's length along the wall at scale 1; Stonewall01 is 256.</summary>
    public float PieceLength { get; init; } = 256f;
}

/// <summary>
/// Earth and part-buried rock laid against the outer faces of the stair walls, so the
/// entrance reads as steps cut into a bank rather than a gate between two walls. The
/// two sides are different on purpose.
/// </summary>
internal sealed record EntranceBankConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Which side gets rock; the other gets earth and scrub.</summary>
    public bool RockOnLeft { get; init; } = true;

    public int RockSide { get; init; } = 2;

    public int EarthSide { get; init; } = 3;

    /// <summary>
    /// Closed boulders for the rock side. Only pieces modelled all the way round
    /// belong here: the bank is seen from the steps AND from the approach.
    /// </summary>
    public IReadOnlyList<string> RockPool { get; init; } = new[]
    {
        "0001819A:Skyrim.esm", // RockL02
        "0001A6E2:Skyrim.esm", // RockL04
        "0001B0A8:Skyrim.esm", // RockL05
        "000332C7:Skyrim.esm", // RockPileL01TundraRocks
    };

    /// <summary>
    /// Earth pieces with grass tops for the softer side. DirtCliffsIsland01 is
    /// modelled all round, unlike the DirtCliffs01/02 strips, which are open-backed
    /// and must never be free-spun beside a walking route.
    /// </summary>
    public IReadOnlyList<string> EarthPool { get; init; } = new[]
    {
        "00042A88:Skyrim.esm", // DirtCliffsIsland01FieldGrass01 (closed grassy hump)
        "00024E7B:Skyrim.esm", // RockPileL02FieldGrass01Moss (broad low grassy pile)
        "00024E8F:Skyrim.esm", // RockPileM02FieldGrass01Moss (low, grassy)
        "000332C7:Skyrim.esm", // RockPileL01TundraRocks
    };

    /// <summary>A piece is drawn only if its height at MaxScale reaches this share of the target.</summary>
    public float ReachShare { get; init; } = 0.7f;

    /// <summary>No bank piece starts further out than the wall's end plus this.</summary>
    public float Extent { get; init; } = 320f;

    /// <summary>Clearance kept between a bank piece and the edge of the stair gap.</summary>
    public float GapMargin { get; init; } = 32f;

    /// <summary>Share of the local drop each piece is sized to.</summary>
    public float Share { get; init; } = 0.9f;

    /// <summary>Smallest drop a piece is sized against, so the lowest flight still gets a bank.</summary>
    public float MinDrop { get; init; } = 160f;

    /// <summary>How much of its own radius a piece stands off the wall face. Below 1 it overlaps the wall.</summary>
    public float Lean { get; init; } = 0.55f;

    /// <summary>Largest scale a bank piece may take; the island cliff is huge at 1.0 already.</summary>
    public float MaxScale { get; init; } = 1.2f;

    public float AlongJitter { get; init; } = 160f;

    /// <summary>Crown sits this far below the wall crest, so it reads part-buried.</summary>
    public float Sink { get; init; } = 48f;

    /// <summary>Base sits this far below grade, so the piece reads as bedded in.</summary>
    public float Bury { get; init; } = 40f;
}

/// <summary>
/// Low drystone walls stepping down either side of the steps: the stair cheeks in the
/// concept. This is the only masonry laid at the entrance.
/// </summary>
internal sealed record EntranceCheekConfig
{
    public bool Enabled { get; init; } = true;

    public string Piece { get; init; } = "0000099B:Skyrim.esm"; // Stonewall01

    /// <summary>
    /// `StonewallEndL01`: the matching tapered end whose open run faces downhill.
    /// It replaces the full top block so the terrace landing ends in masonry without
    /// growing another wall segment.
    /// </summary>
    public string TopPiece { get; init; } = "0000099E:Skyrim.esm";

    /// <summary>Scale of the wall piece. 0.6 makes the 175-tall field wall waist high.</summary>
    public float Scale { get; init; } = 0.6f;

    public float PieceLength { get; init; } = 256f;

    public float TopPieceLength { get; init; } = 222f;

    public float PieceDepth { get; init; } = 138f;

    /// <summary>Lower every cheek crest by this amount while preserving its angle.</summary>
    public float Sink { get; init; } = 48f;

    /// <summary>Minimum projected overlap between consecutive tilted blocks.</summary>
    public float Overlap { get; init; } = 24f;

    /// <summary>Overlap of each level end block into the diagonal run.</summary>
    public float EndOverlap { get; init; } = 16f;

    /// <summary>
    /// Clearance between the stair flank and the wall's inner face. Negative values
    /// deliberately tuck the irregular wall edge under the stair so no dark seam opens.
    /// </summary>
    public float Gap { get; init; } = -16f;

    /// <summary>Crest height above the nosing line; both sides match.</summary>
    public float RiseLeft { get; init; } = 80f;

    public float RiseRight { get; init; } = 80f;
}

/// <summary>
/// Barry's completed Creation Kit perimeter, reproduced per compass edge. Offsets are
/// plan distances from the paving edge, positive outward. Drops are below the floor.
/// </summary>
internal sealed record TerraceBandConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>StonewallTerrace01: 256 long, wall face on local -Y at 325, crest 175, grass falling to 96 at +256.</summary>
    public string Piece { get; init; } = "000009C6:Skyrim.esm";

    public float PieceLength { get; init; } = 256f;

    /// <summary>Stonewall01: parapet at 0.98, west field wall at 0.9, short-face walls and (scaled) nothing else.</summary>
    public string ParapetPiece { get; init; } = "0000099B:Skyrim.esm";

    public float ParapetLength { get; init; } = 256f;

    public float ParapetScale { get; init; } = 0.98f;

    public float FieldWallScale { get; init; } = 0.9f;

    /// <summary>StonewallEndL01, the bastion wall-end: local X -94..128, 171 tall.</summary>
    public string EndPiece { get; init; } = "0000099E:Skyrim.esm";

    public float EndPieceLength { get; init; } = 222f;

    /// <summary>Distance from the piece origin to its +X (unfinished) end at scale 1.</summary>
    public float EndPieceFar { get; init; } = 128f;

    public Dictionary<string, BandEdgeConfig> Edges { get; init; } = new()
    {
        ["N"] = new() { OuterOffset = 117f, OuterOnGround = true, Filler = true, FillerOffset = 111f, FillerLift = 115f, ParapetOffset = -15f, ParapetDrop = 153f },
        ["E"] = new() { OuterOffset = 64f, OuterOnGround = false, OuterDrop = 296f, Filler = true, FillerOffset = 64f, FillerDrop = 424f, ParapetOffset = 24f, ParapetDrop = 149f },
        ["S"] = new() { OuterOffset = 98f, OuterOnGround = false, OuterDrop = 296f, Filler = true, FillerOffset = 136f, FillerDrop = 424f, ParapetOffset = 12f, ParapetDrop = 149f },
        ["W"] = new() { OuterOffset = 136f, OuterOnGround = false, OuterDrop = 296f, Filler = true, FillerOffset = 136f, FillerDrop = 424f, ParapetOffset = 20f, ParapetDrop = 149f, FieldWall = true, FieldWallOffset = 397f },
    };

    /// <summary>Slope top height above the inward piece's origin where it meets the retaining face.</summary>
    public float MiddleTopAtFace { get; init; } = 148f;

    public float LowerSink { get; init; } = 6f;

    /// <summary>No terrace crest closer than this to the floor plane.</summary>
    public float CrestClear { get; init; } = 48f;

    /// <summary>Rows stop this far before a convex corner; the bastion finishes it.</summary>
    public float CornerStop { get; init; } = 96f;

    /// <summary>The parapet runs this far past the cheek's outer face toward the steps.</summary>
    public float ParapetStairOverlap { get; init; } = 20f;

    /// <summary>Scaled-up wall-ends at convex corners. Off: the rows run through to the corner.</summary>
    public bool Bastions { get; init; } = false;

    /// <summary>One scaled-up field wall on each short step face. Off: the rows run there too.</summary>
    public bool ShortFaceWalls { get; init; } = false;

    /// <summary>A face this long or shorter, touching a re-entrant corner, gets one big wall.</summary>
    public float ShortFaceMax { get; init; } = 512f;

    public float ShortFaceOffset { get; init; } = 24f;

    /// <summary>Big walls and bastions are scaled so their crest clears the floor by this.</summary>
    public float BigWallOvershoot { get; init; } = 8f;

    public float BigWallMinScale { get; init; } = 1.5f;

    public float BigWallMaxScale { get; init; } = 3.3f;

    /// <summary>Bastion walls sit this far inside their face line.</summary>
    public float BastionInset { get; init; } = 16f;

    /// <summary>Bastion walls start this far inside the corner along the other edge.</summary>
    public float BastionStart { get; init; } = 32f;

    /// <summary>A bastion leg whose middle comes closer than this to another paved cell is not placed.</summary>
    public float BastionLegClear { get; init; } = 560f;
}

/// <summary>One compass edge's layer stack.</summary>
internal sealed record BandEdgeConfig
{
    public float OuterOffset { get; init; } = 100f;

    /// <summary>True: outward wall on grade. False: at floor minus OuterDrop.</summary>
    public bool OuterOnGround { get; init; } = false;

    public float OuterDrop { get; init; } = 296f;

    public bool Filler { get; init; } = true;

    public float FillerOffset { get; init; } = 100f;

    /// <summary>When set, the inward piece sits this far above the outward wall (the grass slope).</summary>
    public float? FillerLift { get; init; }

    /// <summary>Otherwise it sits at floor minus this, a plinth under the outward wall.</summary>
    public float FillerDrop { get; init; } = 424f;

    public bool Parapet { get; init; } = true;

    public float ParapetOffset { get; init; } = 0f;

    public float ParapetDrop { get; init; } = 149f;

    /// <summary>A third, lower Stonewall01 on grade in front of the band (west face).</summary>
    public bool FieldWall { get; init; } = false;

    public float FieldWallOffset { get; init; } = 397f;
}

/// <summary>One hand-placed object relative to the stair head: Dx to the right looking out, Dy outward, Dz from the floor.</summary>
internal sealed record EntranceDressingItem
{
    public string Base { get; init; } = "";

    public float Dx { get; init; }

    public float Dy { get; init; }

    public float Dz { get; init; }

    public float RotDeg { get; init; }

    public float Scale { get; init; } = 1f;
}

/// <summary>
/// The market: stall shells laid along lanes, as a controlled maze. Each lane has a
/// centreline and a half-width profile that pinches and swells along it, and a gentle
/// meander, so the street alternates between tight passages and browsing pockets.
/// Stall modules (a main structure plus display, storage and dressing pieces) line one
/// or both sides, facing the lane, jittered in set-back, angle and gap, and any module
/// that would stand in another lane, a keep-out zone or another module is skipped,
/// which is what opens the junctions and pockets. Each module also gets a named,
/// themed shell marker so merchants can be layered on later.
/// </summary>
internal sealed record MarketConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>Stall corners must stay this far inside the palisade line.</summary>
    public float WallMargin { get; init; } = 220f;

    /// <summary>Clearance kept between neighbouring modules and from lane edges.</summary>
    public float Clearance { get; init; } = 20f;

    /// <summary>
    /// A keep-clear strip this deep in front of every stall's counter: later dressing,
    /// festival poles and visitors stay out of it, so every keeper can be reached.
    /// </summary>
    public float FrontageDepth { get; init; } = 130f;

    /// <summary>Zones no stall may stand in (the crowd square, the stage, the entrance forecourt).</summary>
    public List<string> KeepOutZones { get; init; } = new();

    /// <summary>Extra no-stall areas, such as the archery range.</summary>
    public List<MarketArea> KeepOut { get; init; } = new();

    public List<MarketModule> Modules { get; init; } = new();

    public List<MarketLane> Lanes { get; init; } = new();

    /// <summary>Signature stalls placed first at a lane station, such as the rival faction pair.</summary>
    public List<MarketFixed> Fixed { get; init; } = new();

    /// <summary>
    /// Back-to-back infill: after the lanes are lined, each stall tries to take one of
    /// these modules directly behind it, facing the other way, as real market rows do.
    /// </summary>
    public List<string> BackFill { get; init; } = new();

    public List<string> BackFillThemes { get; init; } = new();

    /// <summary>
    /// Seating: modules (picnic tables and benches) set out at intervals along a lane,
    /// either down its middle or offset to one side, turned to run along it.
    /// </summary>
    public List<MarketSeating> Seating { get; init; } = new();

    /// <summary>Extra pieces placed with every stall of a theme, in the stall's frame (the rival banners).</summary>
    public List<MarketThemeDressing> ThemeDressing { get; init; } = new();

    /// <summary>Hand-placed dressing that marks the lane structure, such as banner posts at the crossing.</summary>
    public List<MarketDressing> Dressing { get; init; } = new();

    /// <summary>
    /// Dressing built after every other record but the navmesh, so adding to it never moves
    /// another FormID. Placed as <see cref="Dressing"/> is, and also kept clear of every
    /// placed NPC. Append only: an entry's position in the list is part of its seed.
    /// </summary>
    public List<MarketDressing> LateDressing { get; init; } = new();

    /// <summary>Small reusable scenes (a cheese board, a sack pile, a sign on its post) that kits place.</summary>
    public List<MarketVignette> Vignettes { get; init; } = new();

    /// <summary>
    /// What each business shows: per theme, vignettes for the stall's counters, hang line,
    /// sides, rear and identity marker, placed at the module's <see cref="MarketModule.Slots"/>.
    /// </summary>
    public List<StallKit> StallKits { get; init; } = new();

    /// <summary>Shell markers: one persistent heading marker per stall, named by theme.</summary>
    public string ShellMarker { get; init; } = "00000034:Skyrim.esm";

    public string ShellMarkerPrefix { get; init; } = "SkyrimFairStall";
}

internal sealed record MarketArea
{
    public string Name { get; init; } = string.Empty;

    public List<float[]> Polygon { get; init; } = new();
}

/// <summary>
/// One stall kit. Local frame: +Y is the front (toward the lane), the footprint is
/// <see cref="Width"/> x <see cref="Depth"/> centred on the origin.
/// </summary>
internal sealed record MarketModule
{
    public string Name { get; init; } = string.Empty;

    public float Width { get; init; }

    public float Depth { get; init; }

    public List<MarketPiece> Pieces { get; init; } = new();

    /// <summary>
    /// <c>[x, y]</c> in the module frame where stall-keepers stand, facing the front: one
    /// per counter, so a merged pair of stalls gets two.
    /// </summary>
    public List<float[]> VendorSpots { get; init; } = new();

    /// <summary>Where a stall kit dresses this module, in the module frame.</summary>
    public ModuleSlots Slots { get; init; } = new();
}

/// <summary>
/// Dressing slots of a stall module. Strips are <c>[x0, x1, y, z]</c>: a counter top or
/// shelf that vignettes are laid along, or the line under a canopy that goods hang from
/// (<c>z</c> is the hanging point). Spots are <c>[x, y, yaw]</c> on the ground.
/// </summary>
internal sealed record ModuleSlots
{
    public List<float[]> Counter { get; init; } = new();

    public List<float[]> Hang { get; init; } = new();

    public List<float[]> Side { get; init; } = new();

    public List<float[]> Rear { get; init; } = new();

    public List<float[]> Sign { get; init; } = new();
}

internal sealed record MarketPiece
{
    public string Piece { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public float Z { get; init; }

    public float Yaw { get; init; }

    /// <summary>Dressing that some copies of the module leave out, so no two look alike.</summary>
    public bool Optional { get; init; }

    /// <summary>Tilts in degrees, applied as world X and Y rotations after the yaw (as vanilla).</summary>
    public float RotX { get; init; }

    public float RotY { get; init; }

    public float Scale { get; init; } = 1f;

    /// <summary>
    /// Placed exactly, without the small hand-placed wobble: for pieces that must meet
    /// (a sign, its posts and the bar it hangs from).
    /// </summary>
    public bool Exact { get; init; }

    /// <summary>
    /// Places nothing, but takes the FormID a removed piece had, so every record after it
    /// keeps its FormID in existing saves (the stage quest, the actors).
    /// </summary>
    public bool Reserve { get; init; }
}

internal sealed record MarketLane
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Use the fair world's avenue as this lane's centreline.</summary>
    public bool UseAvenue { get; init; }

    public List<float[]> Points { get; init; } = new();

    /// <summary><c>[distance along the lane, half-width]</c> stations, interpolated.</summary>
    public List<float[]> HalfWidths { get; init; } = new();

    /// <summary><c>[amplitude, period]</c> of the sideways wander of the lane's centre.</summary>
    public float[] Meander { get; init; } = { 0f, 1000f };

    /// <summary>Stalls from and to this distance along the lane.</summary>
    public float From { get; init; }

    public float To { get; init; } = float.MaxValue;

    /// <summary><c>both</c>, <c>left</c> or <c>right</c> of the direction of travel.</summary>
    public string Sides { get; init; } = "both";

    /// <summary>Which modules this lane uses, and how often.</summary>
    public List<MarketMix> Mix { get; init; } = new();

    /// <summary>Theme slots handed out to this lane's stalls in turn.</summary>
    public List<string> Themes { get; init; } = new();

    public float GapMin { get; init; } = 30f;

    public float GapMax { get; init; } = 140f;

    /// <summary>Chance of leaving a browsing pocket instead of the next stall.</summary>
    public float PocketChance { get; init; } = 0.12f;

    public float PocketMin { get; init; } = 280f;

    public float PocketMax { get; init; } = 420f;

    /// <summary>Largest extra set-back behind the lane edge.</summary>
    public float SetBack { get; init; } = 70f;

    /// <summary>Largest turn away from facing the lane squarely, degrees.</summary>
    public float AngleJitter { get; init; } = 7f;
}

internal sealed record MarketFixed
{
    public string Lane { get; init; } = string.Empty;

    public float At { get; init; }

    public string Side { get; init; } = "left";

    public string Module { get; init; } = string.Empty;

    public string Theme { get; init; } = string.Empty;
}

internal sealed record MarketMix
{
    public string Module { get; init; } = string.Empty;

    public float Weight { get; init; } = 1f;
}

internal sealed record MarketDressing
{
    /// <summary>A whole module (a camp, a cart cluster) instead of one piece; placed only if it fits.</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>For a module: how far from its point to search for a spot that fits.</summary>
    public float SearchRadius { get; init; }

    /// <summary>
    /// Keep-out zones this group may stand in (the performance square's social edge sits in
    /// the Crowd zone, which keeps stalls out); every other keep-out still applies.
    /// </summary>
    public List<string> ExemptKeepOut { get; init; } = new();

    public string Piece { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public float Z { get; init; }

    public float Yaw { get; init; }

    /// <summary>
    /// Place the module exactly at its point, with no fit check or search: for dressing
    /// fixed to a structure (the stage roof's lanterns), where keep-outs don't apply.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>A single piece that places nothing but keeps the FormID it had (the road sign, moved outside).</summary>
    public bool Reserve { get; init; }
}

/// <summary>
/// Placeholder stall-keepers: they stand at their counters so the market can be pictured.
/// They sell nothing and have no dialogue, faction or quest of their own.
/// </summary>
internal sealed record VendorsConfig
{
    public bool Enabled { get; init; } = true;

    public string EditorIdPrefix { get; init; } = "SkyrimFairVendor";

    public string Name { get; init; } = "Fair Trader";

    /// <summary>Placeholder race; the Traits template replaces it with the template's.</summary>
    public string Race { get; init; } = "00013746:Skyrim.esm";

    public string Class { get; init; } = "0001326B:Skyrim.esm";

    /// <summary>
    /// Keeps them where they are placed, with no navmesh needed and no weapons out:
    /// DefaultStayAtEditorLocation. (The hold-position packages draw weapons.)
    /// </summary>
    public string Package { get; init; } = "00025BFC:Skyrim.esm";

    public short Level { get; init; } = 5;

    public List<VendorLook> Looks { get; init; } = new();

    /// <summary>
    /// What each stall's keeper is called, by the stall's theme ("cheese" -> "Cheese Seller"),
    /// so the stall's trade shows when looking at its keeper. Themes not listed keep
    /// <see cref="Name"/>.
    /// </summary>
    public Dictionary<string, string> ThemeNames { get; init; } = new();
}

internal sealed record VendorLook
{
    public string Name { get; init; } = string.Empty;

    /// <summary>A vanilla leveled NPC list whose Traits (face, race, sex, voice) the vendor takes.</summary>
    public string Template { get; init; } = string.Empty;

    public bool Female { get; init; }

    /// <summary>Vanilla outfits; one vendor record is made per outfit.</summary>
    public List<string> Outfits { get; init; } = new();
}

internal sealed record MarketSeating
{
    public string Lane { get; init; } = string.Empty;

    public string Module { get; init; } = string.Empty;

    /// <summary>Several modules to pick from at random, instead of <see cref="Module"/>.</summary>
    public List<string> Modules { get; init; } = new();

    /// <summary>Chance of trying a placement at each station, so runs do not read as a grid.</summary>
    public float Chance { get; init; } = 1f;

    /// <summary>Overrides the market's wall margin, for dressing that belongs against the wall.</summary>
    public float? WallMargin { get; init; }

    /// <summary>Sideways from the lane centre, positive to the left of travel; 0 is down the middle.</summary>
    public float Offset { get; init; }

    public float From { get; init; }

    public float To { get; init; } = float.MaxValue;

    public float Spacing { get; init; } = 900f;

    public float AngleJitter { get; init; } = 6f;
}

/// <summary>
/// The archery range, set up as Solitude's Castle Dour practice yard: each archer is
/// linked to one target by the <c>TrainingTarget</c> keyword and runs vanilla
/// <c>GuardSolitudeRangedTrainingPackage</c>. The archers are townsfolk, not soldiers.
/// </summary>
internal sealed record AudioConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairAudio";

    /// <summary>Where tools/build_audio.py writes the runtime files, relative to the config.</summary>
    public string SoundRoot { get; init; } = "assets/sound";

    /// <summary>The world's music type: a silent one, so the game's own music stays out of the fair.</summary>
    public string WorldMusic { get; init; } = string.Empty;

    /// <summary>Vanilla's TimeScale global, for the controller's clock.</summary>
    public string TimeScaleGlobal { get; init; } = "0000003A:Skyrim.esm";

    /// <summary>Start values of the runtime globals (a later MCM writes them).</summary>
    public AudioGlobals Globals { get; init; } = new();

    public StageAudioConfig Stage { get; init; } = new();

    public AmbienceConfig Ambience { get; init; } = new();
}

internal sealed record AudioGlobals
{
    public float AmbienceEnabled { get; init; } = 1f;

    public float AmbienceVolume { get; init; } = 1f;

    public float MusicEnabled { get; init; } = 1f;

    public float MusicVolume { get; init; } = 1f;

    public float CheerVolume { get; init; } = 1f;
}

internal sealed record StageAudioConfig
{
    /// <summary>The stage speaker: <c>[x, y, z]</c>, where the music and the cheer come from.</summary>
    public float[] Speaker { get; init; } = Array.Empty<float>();

    /// <summary>Full volume within this distance of the speaker, silent beyond the maximum.</summary>
    public float MinDistance { get; init; } = 1500f;

    public float MaxDistance { get; init; } = 7500f;

    /// <summary>
    /// The output model's falloff: five volumes (percent) from the minimum distance to the
    /// maximum. Empty keeps the copied vanilla curve (100, 50, 20, 5, 0).
    /// </summary>
    public int[] Curve { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The vanilla output model the stage's is copied from; empty for SOMMono06000_dry.
    /// SOMStereoRad10000 plays the mono songs at full level in both front speakers (the
    /// HRTF model pans them), still fading with distance, as vanilla's distant river does.
    /// </summary>
    public string OutputModel { get; init; } = string.Empty;

    /// <summary>
    /// Decibels taken off the songs and the cheer. It can't go below 0 (the record stores an
    /// unsigned value), so loudness comes from the files: see <see cref="Loudness"/>.
    /// </summary>
    public float StaticAttenuation { get; init; }

    /// <summary>For tools/build_audio.py: the gated level (dBFS) songs and cheers are brought to.</summary>
    public float? Loudness { get; init; }

    /// <summary>For tools/build_audio.py: the limiter's ceiling, dBFS.</summary>
    public float Ceiling { get; init; } = -1f;

    public float CheerStaticAttenuation { get; init; }

    public float FirstSongDelay { get; init; } = 4f;

    public float PauseAfterCheer { get; init; } = 2f;

    /// <summary>Seconds before a song's end its cheer starts; the song finishes under it.</summary>
    public float CheerLead { get; init; } = 1f;

    /// <summary>
    /// The instruments with timelines, by name (lute, drum, flute), as their idles: a musician
    /// whose idle is one follows that instrument's timeline in each song.
    /// </summary>
    public Dictionary<string, string> Instruments { get; init; } = new();

    /// <summary>
    /// How many times faster each instrument's loop plays in a "fast" stretch (songs.config.json
    /// "fast"; tools/bards/build_tempo.py makes the clips). The generator writes an OAR submod
    /// per instrument, swapping the fast clip in while that instrument's tempo global is 2.
    /// </summary>
    public Dictionary<string, float> Fast { get; init; } = new();

    /// <summary>The singers' moves (from songs.config.json): while singing, while resting, at a song's end.</summary>
    public CrowdMove SingerSing { get; init; } = new();

    public CrowdMove SingerRest { get; init; } = new();

    /// <summary>A "cheer" stretch of the singers' timeline.</summary>
    public CrowdMove SingerCheer { get; init; } = new();

    /// <summary>The move each singer makes at a song's end.</summary>
    public CrowdMove SingerEnd { get; init; } = new();

    public float SingerGap { get; init; } = 2f;

    public SingerSteps SingerSteps { get; init; } = new();

    public List<MoreDance> MoreDances { get; init; } = new();

    public string MoreDancesPlugin { get; init; } = "Dance.esp";

    public string MoreDancesCheck { get; init; } = "000803";

    public FireworksShow Fireworks { get; init; } = new();

    /// <summary>Where the fast clips and their OAR submods live.</summary>
    public string TempoOarFolder { get; init; } = "assets/meshes/actors/character/animations/OpenAnimationReplacer/SkyrimFairTempo";

    /// <summary>The ambience's level during a song, as a share of normal.</summary>
    public float DuckAmbience { get; init; } = 0.75f;

    /// <summary>For tools/build_audio.py: a track louder than this in its last 50 ms gets a fade.</summary>
    public float HardEndLevel { get; init; } = 300f;

    public float HardEndFade { get; init; } = 1.5f;

    public List<StageSong> Songs { get; init; } = new();

    /// <summary>Where the added songs' records go (<see cref="StageSong.Added"/>).</summary>
    public uint AddedSongsFormIdBase { get; init; } = 0x50000;

    /// <summary>
    /// The stage show's own file (songs.config.json, next to the fair config): the songs and
    /// their timelines, the instruments, the musicians (<see cref="Band"/>, <see cref="Orchestra"/>)
    /// and the crowd's moves, so they're easy to find and edit. Program.cs reads it.
    /// </summary>
    public string SongsFile { get; init; } = string.Empty;


    public List<StageCheer> Cheers { get; init; } = new();

    /// <summary>
    /// What keeps the bards on their spots: DefaultStayAtEditorLocation, as vanilla's bards
    /// hold still (DefaultStayAtCurrentLocation) while their scene plays the instrument.
    /// </summary>
    public string BandPackage { get; init; } = "00025BFC:Skyrim.esm";

    /// <summary>The idle that puts an instrument away (vanilla <c>IdleStop</c>).</summary>
    public string BandStop { get; init; } = "000E4242:Skyrim.esm";

    /// <summary>The bards playing on the stage.</summary>
    public List<BandMember> Band { get; init; } = new();

    /// <summary>
    /// The rest of the orchestra, built last (after every other record) and given to the
    /// stage script as its own properties, so existing saves pick them up.
    /// </summary>
    public List<BandMember> Orchestra { get; init; } = new();
}

internal sealed record BandMember
{
    public string Name { get; init; } = string.Empty;

    /// <summary>What looking at them says.</summary>
    public string Title { get; init; } = "Fair Bard";

    /// <summary>
    /// The idle the stage script plays on them when a song starts (IdleLuteStart,
    /// IdleDrumStart, IdleFluteStart): it brings out the instrument and plays it, as
    /// vanilla's bard scenes do with <c>PlayIdle</c>.
    /// </summary>
    public string Idle { get; init; } = string.Empty;

    /// <summary><c>[x, y, z, heading]</c> on the stage.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>A <see cref="VendorLook.Name"/>: the face list they are drawn from.</summary>
    public string Look { get; init; } = "Male";

    public string Outfit { get; init; } = string.Empty;
}

internal sealed record StageSong
{
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Added after the first playlist: its sound records (and the exterior's quieter copy)
    /// are built in the added-songs range (<see cref="FairAddedSongs"/>), so a song can go
    /// anywhere in the playlist without renumbering anything.
    /// </summary>
    public bool Added { get; init; }

    /// <summary>Barry's converted file, for tools/build_audio.py.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The runtime file under <c>Sound\</c>.</summary>
    public string File { get; init; } = string.Empty;

    /// <summary>The cheer after it (a <see cref="StageCheer.Name"/>); empty for none.</summary>
    public string Cheer { get; init; } = string.Empty;

    /// <summary>
    /// Each instrument's timeline: [second, "rest" | "normal" | "fast" | "away"], from the song's
    /// start, an entry where it changes. Whoever plays that instrument follows it (a
    /// resting instrument is put away). None: it plays throughout.
    /// </summary>
    public List<System.Text.Json.JsonElement[]> Lute { get; init; } = new();

    public List<System.Text.Json.JsonElement[]> Drum { get; init; } = new();

    public List<System.Text.Json.JsonElement[]> Flute { get; init; } = new();

    /// <summary>The singers' timeline: [second, "sing" | "rest"]; resting, their lines are skipped (and they clap). None: they sing.</summary>
    public List<System.Text.Json.JsonElement[]> Singers { get; init; } = new();

    /// <summary>The crowd's timeline: [second, "dance" | "clap" | "cheer"]. None: they dance throughout.</summary>
    public List<System.Text.Json.JsonElement[]> Crowd { get; init; } = new();
}

internal sealed record StageCheer
{
    public string Name { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string File { get; init; } = string.Empty;

    /// <summary>For tools/build_audio.py: seconds kept after the leading silence, and the fade.</summary>
    public float Length { get; init; } = 11f;

    public float FadeOut { get; init; } = 3f;

    public float Threshold { get; init; } = 600f;
}

internal sealed record AmbienceConfig
{
    public float MinDistance { get; init; } = 500f;

    public float MaxDistance { get; init; } = 3000f;

    public float StaticAttenuation { get; init; } = 11f;

    public List<AmbienceLoop> Loops { get; init; } = new();

    public List<AmbienceEmitter> Emitters { get; init; } = new();
}

internal sealed record AmbienceLoop
{
    public string Name { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public float Crossfade { get; init; } = 3f;

    /// <summary>For tools/build_audio.py: a plain gain in dB applied to the loop.</summary>
    public float Gain { get; init; }

    /// <summary>Copies of the loop, each rotated to start further in, for emitters heard together.</summary>
    public List<string> Files { get; init; } = new();
}

internal sealed record AmbienceEmitter
{
    public string Name { get; init; } = string.Empty;

    /// <summary><c>[x, y, z]</c> in the world.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    public string Loop { get; init; } = string.Empty;

    /// <summary>Which rotated copy of the loop it plays.</summary>
    public int Copy { get; init; }

    /// <summary>Decibels quieter than the other emitters.</summary>
    public float ExtraAttenuation { get; init; }
}

internal sealed record ArcheryConfig
{
    public bool Enabled { get; init; } = true;

    public string EditorIdPrefix { get; init; } = "SkyrimFairArcher";

    public string Name { get; init; } = "Fair Archer";

    public string Race { get; init; } = "00013746:Skyrim.esm";

    public string Class { get; init; } = "0001326B:Skyrim.esm";

    /// <summary>HunterClothesRND.</summary>
    public string Outfit { get; init; } = "00073FC7:Skyrim.esm";

    /// <summary>HuntingBow.</summary>
    public string Bow { get; init; } = "00013985:Skyrim.esm";

    /// <summary>IronArrow.</summary>
    public string Arrows { get; init; } = "0001397D:Skyrim.esm";

    public int ArrowCount { get; init; } = 100;

    /// <summary>
    /// The trigger radius of the fair's copy of the training package: the archers shoot
    /// only while the player is this close. Vanilla's is 1,250; 0 uses vanilla's package.
    /// </summary>
    public int TriggerRadius { get; init; }

    /// <summary>What the archers hold with while their training package restarts: DefaultStayAtEditorLocation.</summary>
    public string HoldPackage { get; init; } = "00025BFC:Skyrim.esm";

    /// <summary>UseWeapon's "Use Weapon Location" input, and "Search for Weapon Location" (near self).</summary>
    public sbyte UseWeaponLocationInput { get; init; } = 3;

    public sbyte SearchLocationInput { get; init; } = 0;

    /// <summary>The use-weapon location's radius, round the archer itself.</summary>
    public int UseWeaponRadius { get; init; } = 64;

    /// <summary>The package data input holding the trigger radius (UseWeapon's "Trigger Radius").</summary>
    public sbyte TriggerRadiusInput { get; init; } = 30;

    /// <summary>GuardSolitudeRangedTrainingPackage: shoot at the TrainingTarget linked ref, all day.</summary>
    public string Package { get; init; } = "000B4C54:Skyrim.esm";

    /// <summary>TrainingTarget, the keyword the package reads the target from.</summary>
    public string TargetKeyword { get; init; } = "000B4C5A:Skyrim.esm";

    /// <summary>ArcheryTarget: its face is its local -X, turned toward the archer.</summary>
    public string Target { get; init; } = "00066AF6:Skyrim.esm";

    /// <summary>
    /// PatrolIdleMarker: where the archer stands to shoot. The package's "Use Weapon
    /// Location" is the archer's linked reference with no keyword, within 32; without
    /// it the package never starts and the archer stands idle.
    /// </summary>
    public string StandMarker { get; init; } = "000140BD:Skyrim.esm";

    /// <summary>HayBale01 behind each target.</summary>
    public string Backstop { get; init; } = "0005B198:Skyrim.esm";

    public float BackstopDistance { get; init; } = 140f;

    public float BackstopZ { get; init; } = 40f;

    public short Level { get; init; } = 10;

    public List<VendorLook> Looks { get; init; } = new();

    public List<ArcheryLane> Lanes { get; init; } = new();
}

internal sealed record ArcheryLane
{
    public float[] Archer { get; init; } = Array.Empty<float>();

    public float[] Target { get; init; } = Array.Empty<float>();
}

internal sealed record MarketThemeDressing
{
    public string Theme { get; init; } = string.Empty;

    public List<MarketPiece> Pieces { get; init; } = new();
}

/// <summary>
/// Festival light towers: Barry's scaffold watchtower with no guard, a large lantern on
/// the deck where the guard would stand, and tall Whiterun banners from the deck's edge.
/// The market treats each tower as a keep-out.
/// </summary>
internal sealed record TowersConfig
{
    public bool Enabled { get; init; } = true;

    public string EditorIdPrefix { get; init; } = "SkyrimFairLightTower";

    public ProjectStaticConfig Tower { get; init; } = new();

    /// <summary>Top of the deck's edge boards above the tower's origin, unscaled; banners hang from here.</summary>
    public float DeckZ { get; init; } = 234f;

    /// <summary>The deck floor inside the edge boards, unscaled; the lantern stands here.</summary>
    public float FloorZ { get; init; } = 215f;

    /// <summary>Half the deck's width, unscaled; banners hang just outside it.</summary>
    public float DeckHalf { get; init; } = 59f;

    public float Sink { get; init; } = 4f;

    /// <summary>CityBannerWhiterun01InsideTall, Dragonsreach's long banner.</summary>
    public string Banner { get; init; } = "000DEE54:Skyrim.esm";

    public float BannerScale { get; init; } = 1.25f;

    /// <summary>How far outside the deck edge each banner hangs.</summary>
    public float BannerOut { get; init; } = 8f;

    /// <summary>
    /// The tall banner's cloth leans in the mesh; Dragonsreach tilts every one back by 23
    /// degrees about its local Y so it hangs straight.
    /// </summary>
    public float BannerTiltDegrees { get; init; } = 23f;

    /// <summary>Banner origin (its top) relative to the deck top.</summary>
    public float BannerZ { get; init; }

    /// <summary>
    /// The lantern: vanilla CandleLanternwithCandle01 with its physics unhooked by
    /// <c>tools/make_tower_lantern.py</c>, since the vanilla one is havok clutter and falls.
    /// </summary>
    public ProjectStaticConfig Lantern { get; init; } = new();

    public float LanternScale { get; init; } = 3f;

    /// <summary>WRFireLightNS: Whiterun's warm street fire light, radius 768, no shadows.</summary>
    public string Light { get; init; } = "000BBAE5:Skyrim.esm";

    /// <summary>Light height above the deck floor, inside the lantern.</summary>
    public float LightZ { get; init; } = 60f;

    /// <summary>FXfireWithEmbersLight, shrunk to a flame inside the lantern.</summary>
    public string Fire { get; init; } = "00033DA9:Skyrim.esm";

    public float FireScale { get; init; } = 0.3f;

    /// <summary>Fire base above the deck floor.</summary>
    public float FireZ { get; init; } = 28f;

    /// <summary>FXGlowFillRoundMid, a soft halo so the lantern reads from across the fair.</summary>
    public string Glow { get; init; } = "0002EB0E:Skyrim.esm";

    public float GlowScale { get; init; } = 0.45f;

    /// <summary>Glow centre above the deck floor.</summary>
    public float GlowZ { get; init; } = 75f;

    public float KeepOutMargin { get; init; } = 60f;

    public List<TowerSpot> Towers { get; init; } = new();
}

internal sealed record TowerSpot
{
    public float X { get; init; }

    public float Y { get; init; }

    /// <summary>Yaw in degrees; the ladder is on the tower's local -X face.</summary>
    public float Yaw { get; init; }

    /// <summary>Local faces (+X, +Y, -Y, -X) that carry a banner.</summary>
    public List<string> Banners { get; init; } = new();
}

/// <summary>A small scene of pieces round a point, in the slot's frame (+Y toward the customer).</summary>
internal sealed record MarketVignette
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Frontage it takes along a strip.</summary>
    public float Width { get; init; } = 60f;

    public List<MarketPiece> Pieces { get; init; } = new();
}

/// <summary>
/// One kind of business: vignette names per slot kind. Counter strips are filled left to
/// right from <see cref="Counter"/>, hang strips repeat <see cref="Hang"/> at its spacing,
/// and each ground spot takes one of its list (or nothing, at <see cref="EmptyChance"/>).
/// </summary>
internal sealed record StallKit
{
    public List<string> Themes { get; init; } = new();

    public List<string> Counter { get; init; } = new();

    public List<string> Hang { get; init; } = new();

    public float HangSpacing { get; init; } = 45f;

    public List<string> Side { get; init; } = new();

    public List<string> Rear { get; init; } = new();

    public List<string> Sign { get; init; } = new();

    public float EmptyChance { get; init; } = 0.15f;

    /// <summary>A warm light at the stall (a LIGH FormKey), for the busy stalls only.</summary>
    public string Light { get; init; } = string.Empty;

    public float[] LightAt { get; init; } = { 0f, 40f, 170f };
}

/// <summary>
/// The worn ground: each fair texture is a copy of a vanilla landscape texture (so grass,
/// footsteps and friction carry over) whose texture set also names a parallax height map,
/// for Terrain Parallax under Community Shaders' Terrain Helper. Wear rises round the
/// things people walk to, broken by noise into patches.
/// </summary>
internal sealed record GroundConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairGround";

    public List<GroundTexture> Textures { get; init; } = new();

    /// <summary>Half-width of the cobbled core of the avenue.</summary>
    public float CobbleHalfWidth { get; init; } = 250f;

    /// <summary>How far the cobbles' edge wanders in and out.</summary>
    public float CobbleRagged { get; init; } = 90f;

    /// <summary>Where the cobbles stop short of the avenue's ends.</summary>
    public float CobbleFrom { get; init; } = 150f;

    public float CobbleTo { get; init; } = float.MaxValue;

    /// <summary>
    /// How far the cobbles' edge takes to fade out. Keep it well over the 128 between
    /// terrain vertices, or the edge breaks up into the terrain's hard triangles.
    /// </summary>
    public float CobbleFeather { get; init; } = 300f;

    /// <summary>How much of the cobbled core is sunk under trodden dirt here and there (0..1).</summary>
    public float CobbleSunk { get; init; } = 0.35f;

    /// <summary>
    /// How deep inside the palisade the ground is fully trodden. Wear fades from the middle
    /// of the fair to <see cref="EdgeWear"/> of itself at the palisade.
    /// </summary>
    public float CentreDepth { get; init; } = 2000f;

    public float EdgeWear { get; init; } = 0.45f;

    /// <summary>Wear added all over the middle of the fair, fading to none at the palisade.</summary>
    public float CentreWear { get; init; } = 0.2f;

    public float NoisePeriod { get; init; } = 520f;

    /// <summary>Wear radius and strength round each kind of source.</summary>
    public float StallRadius { get; init; } = 200f;

    public float NpcRadius { get; init; } = 150f;

    public float DressingRadius { get; init; } = 220f;

    /// <summary>Wear along every market lane's corridor.</summary>
    public float LaneWear { get; init; } = 0.6f;

    /// <summary>Zones worn all over (the entrance forecourt, the crowd square).</summary>
    public List<string> WornZones { get; init; } = new();
}

internal sealed record GroundTexture
{
    /// <summary>grass, dirtGrass, dirt, path or cobble.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>The vanilla LTEX copied.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Replacement texture paths under <c>textures\</c>; empty keeps the source's.</summary>
    public string Diffuse { get; init; } = string.Empty;

    public string Normal { get; init; } = string.Empty;

    /// <summary>The parallax height map; empty derives <c>&lt;diffuse&gt;_p.dds</c>.</summary>
    public string Height { get; init; } = string.Empty;

    /// <summary>Grass (GRAS) added to the copy's own list, after the source's.</summary>
    public List<string> Grasses { get; init; } = new();
}

/// <summary>
/// A run of festival lines along a lane: at intervals, a pole each side just outside the
/// corridor and a pennant rope swagged across between them (two mirrored halves of the
/// Solitude festival line meeting at the low middle), optionally hung with lanterns.
/// </summary>
internal sealed record OverheadRun
{
    public string Lane { get; init; } = string.Empty;

    public float From { get; init; }

    public float To { get; init; } = float.MaxValue;

    public float Spacing { get; init; } = 700f;

    /// <summary>The rope line STAT; halves are placed mirrored, high end at each pole top.</summary>
    public string Rope { get; init; } = "000FA22B:Skyrim.esm";

    /// <summary>Alternative ropes cycled along the run (colourways), overriding <see cref="Rope"/>.</summary>
    public List<string> Ropes { get; init; } = new();

    /// <summary>The pole piece, stacked <see cref="PoleStack"/> high.</summary>
    public string Pole { get; init; } = string.Empty;

    public float PoleHeight { get; init; } = 254f;

    public int PoleStack { get; init; } = 2;

    /// <summary>A shorter piece stood on top of the stack (optional), and its height.</summary>
    public string PoleCap { get; init; } = string.Empty;

    public float PoleCapHeight { get; init; }

    /// <summary>
    /// Where the poles stand relative to the corridor edge plus clearance; negative is
    /// inside the corridor, which is the only room there is where stalls line both sides.
    /// </summary>
    public float PoleMargin { get; init; } = 40f;

    /// <summary>Lanterns hung along the rope (MSTTs cycled), every <see cref="LanternSpacing"/>.</summary>
    public List<string> Lanterns { get; init; } = new();

    public float LanternSpacing { get; init; } = 110f;

    /// <summary>Fewer crossings where it is <see cref="Chance"/> below 1.</summary>
    public float Chance { get; init; } = 1f;
}

/// <summary>Visitor groups: each gathers round a point, facing it, unevenly.</summary>
internal sealed record CrowdsConfig
{
    public bool Enabled { get; init; }

    public string EditorIdPrefix { get; init; } = "SkyrimFairVisitor";

    public string Name { get; init; } = "Fair Visitor";

    public List<CrowdGroup> Groups { get; init; } = new();

    /// <summary>Animals placed as they are (the horses in the stable pen).</summary>
    public List<CrowdAnimal> Animals { get; init; } = new();

    /// <summary>Extra layers of visitors, built last and switched live (see <see cref="CrowdTier"/>).</summary>
    public List<CrowdTier> Tiers { get; init; } = new();

    /// <summary>The global the stage script reads: how many layers are on.</summary>
    public string TierGlobal { get; init; } = "SkyrimFairCrowdLayers";

    /// <summary>How many layers are on to begin with; 0 or less: all of them.</summary>
    public int TierDefault { get; init; }

    /// <summary>
    /// The visitors' package swapped, late, for a copy of this one that only greets the
    /// player (no chatter between visitors, no world interactions): fewer AI checks.
    /// Empty leaves them as they are.
    /// </summary>
    public string QuietPackage { get; init; } = string.Empty;

    /// <summary>What seated visitors run: sit in the (unkeyed) linked furniture.</summary>
    public string SitPackage { get; init; } = "000ECEEB:Skyrim.esm";

    /// <summary>Furniture bases people can be seated on, and how many each seats.</summary>
    public Dictionary<string, int> Seats { get; init; } = new();

    /// <summary>What the stage script plays on the dancers during a song, and at the cheer.</summary>
    public List<string> DanceIdles { get; init; } = new();

    public List<string> CheerIdles { get; init; } = new();

    /// <summary>Each dance idle's clip length in seconds (vanilla idles play once; the script replays them).</summary>
    public List<float> DanceLengths { get; init; } = new();

    /// <summary>
    /// A song section's "clap": these idles, replayed as each ends (clip lengths read from
    /// the archive). A "cheer" section plays <see cref="CheerIdles"/> the same way, with
    /// <see cref="CheerLengths"/>.
    /// </summary>
    public List<string> ClapIdles { get; init; } = new();

    public List<float> ClapLengths { get; init; } = new();

    public List<float> CheerLengths { get; init; } = new();

    public ChildrenConfig Children { get; init; } = new();
}

/// <summary>The fair's children: vanilla children's faces (Traits templates) in child clothes.</summary>
internal sealed record ChildrenConfig
{
    public string Name { get; init; } = "Fair Child";

    public string Race { get; init; } = "0002C65B:Skyrim.esm";

    public List<string> Templates { get; init; } = new();

    public List<string> Outfits { get; init; } = new();
}

internal sealed record CrowdAnimal
{
    public string Name { get; init; } = string.Empty;

    /// <summary>A vanilla actor base, placed as it is.</summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>The market dressing module it stands in (the first placed one).</summary>
    public string Near { get; init; } = string.Empty;

    /// <summary><c>[x, y, yaw]</c> in that module's frame.</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>A Papyrus script put on the reference (the pen horses': not to be ridden off).</summary>
    public string Script { get; init; } = string.Empty;
}

/// <summary>
/// An extra layer of visitors on one enable-parent marker, switched live by the stage
/// script from the <see cref="CrowdsConfig.TierGlobal"/> global (tiers 1..n on).
/// </summary>
internal sealed record CrowdTier
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Their own package (a sandbox for wanderers); empty keeps the visitors' stay-put package.</summary>
    public string Package { get; init; } = string.Empty;

    /// <summary>EditorID suffix for the tier's own visitor records, when it has a package.</summary>
    public string Suffix { get; init; } = string.Empty;

    /// <summary><c>dancer</c>: persistent, and danced by the stage script during songs.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>The groups are the fair's children rather than adult visitors.</summary>
    public bool Children { get; init; }

    /// <summary>
    /// They stand where they're put: the tier's own records keep its package's place in the
    /// FormIDs, but run the visitors' stay-put package (Barry traded the walkers for the cameos).
    /// </summary>
    public bool Stand { get; init; }

    public List<CrowdGroup> Groups { get; init; } = new();

    /// <summary>People seated on the furniture of market dressing modules.</summary>
    public List<SeatGroup> Seats { get; init; } = new();
}

/// <summary>Seated visitors: up to <see cref="PerModule"/> on the seats of each module of a kind.</summary>
internal sealed record SeatGroup
{
    public string Near { get; init; } = string.Empty;

    public int PerModule { get; init; } = 2;

    public float Chance { get; init; } = 1f;
}

internal sealed record CrowdGroup
{
    public string Name { get; init; } = string.Empty;

    /// <summary>What they gather round: <c>[x, y]</c>, or a stall theme's front (<see cref="Theme"/>).</summary>
    public float[] At { get; init; } = Array.Empty<float>();

    public string Theme { get; init; } = string.Empty;

    /// <summary>Or round every market dressing group of this module (the picnic sets, the fires).</summary>
    public string Near { get; init; } = string.Empty;

    /// <summary>Chance each focus gets a group at all, so not every table is taken.</summary>
    public float Chance { get; init; } = 1f;

    /// <summary>For a theme: how far in front of the stall's counter they stand.</summary>
    public float FrontOffset { get; init; } = 150f;

    public int Count { get; init; } = 3;

    /// <summary>How far round the point they spread.</summary>
    public float Radius { get; init; } = 160f;

    /// <summary>Arc they fill, degrees either side of facing the point from the front (360 = all round).</summary>
    public float Arc { get; init; } = 90f;

    /// <summary>
    /// Placed as before but initially disabled: the group is replaced (by seated people or
    /// a layer) and keeps its references, so no later FormID moves.
    /// </summary>
    public bool Retired { get; init; }
}

internal sealed record CollisionWall
{
    public string Name { get; init; } = string.Empty;

    public float[] From { get; init; } = Array.Empty<float>();

    public float[] To { get; init; } = Array.Empty<float>();

    /// <summary>Height above the ground.</summary>
    public float Height { get; init; } = 400f;

    public float Thickness { get; init; } = 16f;

    /// <summary>Longest single box; longer segments are split.</summary>
    public float PieceLength { get; init; } = 256f;

    /// <summary>The primitive box's collision layer index, or none for the default.</summary>
    public uint? Layer { get; init; }

    /// <summary>
    /// Built from <see cref="FairWorldConfig.SolidWall"/>, an invisible static with a real
    /// box collider, instead of a CollisionMarker primitive: in game the primitives didn't
    /// stop the player (2026-09-24). Each piece takes the static's full length (256), so
    /// shorter pieces overlap their neighbours and reach a little past the segment's ends.
    /// </summary>
    public bool Solid { get; init; }
}

/// <summary>
/// The crowd switched off where it can't be seen (docs/AUDIT.md, 2026-09-24). The fair has
/// no occlusion, so the game draws actors hidden behind stalls. The generator works out,
/// for each spot of a grid over the fair, which actors can be seen from anywhere within
/// <see cref="Margin"/> of it, and the stage quest's <see cref="Script"/> enables only those
/// as the player moves. Every placed object blocks sight by its navmesh footprint.
/// </summary>
internal sealed record CrowdCullingConfig
{
    public bool Enabled { get; init; }

    /// <summary>The script added to the stage quest, and the global that switches it (1 on).</summary>
    public string Script { get; init; } = "SkyrimFairCrowdCull";

    public string Global { get; init; } = "SkyrimFairCrowdCulling";

    /// <summary>The grid's spacing: the player's spot is looked up in it.</summary>
    public float Spot { get; init; } = 256f;

    /// <summary>
    /// An actor is on at a spot if it's seen from anywhere this close, so it's on before it
    /// can come into view: a sprint covers about this in the script's poll and a 3D load.
    /// </summary>
    public float Margin { get; init; } = 768f;

    /// <summary>Eye heights sight is checked from: standing, and a raised third-person camera.</summary>
    public List<float> EyeHeights { get; init; } = new() { 150f, 210f };

    /// <summary>Heights on each actor sight is checked to; any clear ray sees it.</summary>
    public List<float> TargetHeights { get; init; } = new() { 40f, 100f, 160f };

    /// <summary>Seconds between the script's checks at the fair, and elsewhere.</summary>
    public float Poll { get; init; } = 0.5f;

    public float IdlePoll { get; init; } = 5f;

    /// <summary>
    /// Actors never switched: NPC records whose EditorID starts with one of these (the
    /// band, singers, archers and folk pair are script-driven), and every actor whose base
    /// isn't the fair's own (the pen's horses).
    /// </summary>
    public List<string> AlwaysOn { get; init; } = new();

    /// <summary>Named spots reported at build time, as [x, y].</summary>
    public Dictionary<string, float[]> ReportAt { get; init; } = new();
}

/// <summary>
/// The pool the fair's face lists are filled from (<see cref="FairFaces"/>): generic vanilla
/// NPCs of the weighted races, with their own face and FaceGen, no warpaint or scars.
/// </summary>
internal sealed record FacePoolConfig
{
    public bool Enabled { get; init; }

    /// <summary>
    /// The vanilla lists whose fair copies are refilled, as template FormKey to the copy's
    /// EditorID; a copy is female if its EditorID ends in "Female".
    /// </summary>
    public Dictionary<string, string> Lists { get; init; } = new();

    /// <summary>Relative share of each race's entries, by race EditorID.</summary>
    public Dictionary<string, float> RaceWeights { get; init; } = new();

    /// <summary>Entries a list gets in all (255 at most): the weights are shares of these.</summary>
    public int Entries { get; init; } = 200;

    public float MaxPaint { get; init; } = 0.05f;

    public float MaxDirt { get; init; } = 0.3f;

    /// <summary>Scars are ordinary on Skyrim's townsfolk; warpaint is what reads as a bandit.</summary>
    public bool AllowScars { get; init; } = true;

    /// <summary>Most entries one face gets: a race with few faces gets fewer entries, not look-alikes.</summary>
    public int MaxRepeat { get; init; } = 2;

    /// <summary>EditorID prefixes left out: quest, dungeon and DLC characters.</summary>
    public List<string> ExcludePrefixes { get; init; } = new();

    /// <summary>Left out when the name or EditorID contains one of these (ghosts, corpses).</summary>
    public List<string> ExcludeNames { get; init; } = new();

    /// <summary>Folders whose BSAs hold the vanilla FaceGen; a face without its files is left out.</summary>
    public List<string> Archives { get; init; } = new();

    /// <summary>Records given one fixed face instead of a list (see <see cref="FairFaces.Fixed"/>).</summary>
    public List<FixedFace> Fixed { get; init; } = new();
}

internal sealed record FixedFace
{
    /// <summary>NPC records whose EditorID starts with this.</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>The races their faces come from.</summary>
    public List<string> Races { get; init; } = new();

    /// <summary>How far the face's height may be from 1 (the folk pair's clips need a normal height).</summary>
    public float HeightTolerance { get; init; } = 1f;
}

/// <summary>songs.config.json: the stage show (see <see cref="StageAudioConfig.SongsFile"/>).</summary>
internal sealed record StageShowFile
{
    /// <summary>The songs, in playlist order.</summary>
    public List<StageSong> Songs { get; init; } = new();

    /// <summary>Named idles and forms: lute, drum, flute (a musician's idle may be one of these names), putAway, holdPackage.</summary>
    public Dictionary<string, string> Instruments { get; init; } = new();

    public List<BandMember> Band { get; init; } = new();

    public List<BandMember> Orchestra { get; init; } = new();

    /// <summary>dance, clap and cheer: the idles the dancers play in a section of that kind.</summary>
    public Dictionary<string, CrowdMove> CrowdMoves { get; init; } = new();

    /// <summary>How many times faster each instrument's loop plays in a "fast" stretch.</summary>
    public Dictionary<string, float> Fast { get; init; } = new();

    /// <summary>sing, rest, cheer and end: the singers' gestures while singing, while resting, in a cheer stretch, and at a song's end.</summary>
    public Dictionary<string, CrowdMove> SingerMoves { get; init; } = new();

    /// <summary>Seconds each singer stands between moves.</summary>
    public float SingerGap { get; init; } = 2f;

    /// <summary>The singers stepping across the deck as a line.</summary>
    public SingerSteps SingerSteps { get; init; } = new();

    /// <summary>
    /// Professional Dancer's dances (its animation events, which exist once the mod is installed and
    /// Pandora or Nemesis has run), used while dancing when its plugin is loaded; vanilla otherwise.
    /// </summary>
    public List<MoreDance> MoreDances { get; init; } = new();

    public string MoreDancesPlugin { get; init; } = "Dance.esp";

    /// <summary>A record in the plugin that proves it's loaded (DanceIdle1).</summary>
    public string MoreDancesCheck { get; init; } = "000803";

    /// <summary>The fair's own fireworks after each song (vanilla effects, no dependency).</summary>
    public FireworksShow Fireworks { get; init; } = new();
}

internal sealed record MoreDance
{
    /// <summary>The animation event (Professional Dancer's FNIS list: Dance1..Dance14).</summary>
    public string Event { get; init; } = string.Empty;

    /// <summary>Its clip's length in seconds (read from the mod's .hkx with the codec).</summary>
    public float Length { get; init; }
}

internal sealed record FireworksShow
{
    /// <summary>Shells sent up as each song ends, one per site in turn: gold, blue or violet.</summary>
    public List<string> AfterSong { get; init; } = new();

    /// <summary>More at night (20:00-05:00), from the middle site.</summary>
    public List<string> AtNight { get; init; } = new();

    /// <summary>Launch sites, [x, y]: in the open, away from people and trees.</summary>
    public List<float[]> Sites { get; init; } = new();

    /// <summary>Seconds between one shell and the next.</summary>
    public float Stagger { get; init; } = 0.6f;

    /// <summary>Seconds each colour's shell climbs before it bursts (at 1,500 units a second).</summary>
    public Dictionary<string, float> Climb { get; init; } = new();
}

internal sealed record CrowdMove
{
    public List<string> Idles { get; init; } = new();

    /// <summary>Labels for the reader; not used.</summary>
    public List<string> Names { get; init; } = new();

    /// <summary>Each idle's clip length, seconds.</summary>
    public List<float> Lengths { get; init; } = new();
}

/// <summary>
/// More life in the fair, built after the late dressing but from its own FormID range
/// (<see cref="FormIdBase"/> up, the mod's counter put back after), so every record built
/// later (the navmesh, the culling global, the solid walls, the tempo and fireworks
/// records) keeps its FormID, while the navmesh and the culling still see all of it.
/// </summary>
internal sealed record LifeConfig
{
    public bool Enabled { get; init; }

    public uint FormIdBase { get; init; } = 0x20000;

    public LifePalisade Palisade { get; init; } = new();

    public LifePlants Plants { get; init; } = new();

    /// <summary>Market dressing (modules), placed as the late dressing is, clear of the NPCs.</summary>
    public List<MarketDressing> Dressing { get; init; } = new();

    public LifeSmoke Smoke { get; init; } = new();

    public List<LifeAnimal> Animals { get; init; } = new();
}

/// <summary>
/// Banners on the palisade's inner face, every <see cref="Every"/> panels, and a pennant
/// rope swagged between each pair along a straight run (two mirrored halves of the festival
/// line meeting low in the middle, as the lane crossings are), hung with lanterns.
/// </summary>
internal sealed record LifePalisade
{
    public bool Enabled { get; init; } = true;

    /// <summary>Hanging banners (MSTTs), cycled; each hangs from its origin, its local -X out from the wall.</summary>
    public List<string> Banners { get; init; } = new();

    public float BannerScale { get; init; } = 1f;

    /// <summary>How far below the wall's top each banner's origin hangs.</summary>
    public float BannerDrop { get; init; } = 70f;

    /// <summary>How far in from the panel's centre line the banners and ropes hang.</summary>
    public float Out { get; init; } = 34f;

    public int Every { get; init; } = 2;

    /// <summary>No banner within this of the gate's centre.</summary>
    public float GateClear { get; init; } = 520f;

    /// <summary>Festival ropes cycled; empty for none.</summary>
    public List<string> Ropes { get; init; } = new();

    /// <summary>How far below the wall's top the ropes' high ends are.</summary>
    public float RopeDrop { get; init; } = 95f;

    public List<string> Lanterns { get; init; } = new();

    public float LanternSpacing { get; init; } = 150f;
}

/// <summary>Shrubs, ferns and a few flowers: a strip inside the wall, and along the lanes' edges.</summary>
internal sealed record LifePlants
{
    public bool Enabled { get; init; } = true;

    /// <summary>Shrubs and ferns (TREE or STAT bases), picked at random.</summary>
    public List<string> Pieces { get; init; } = new();

    /// <summary>Flowers (harvestable flora), for <see cref="FlowerShare"/> of the plants.</summary>
    public List<string> Flowers { get; init; } = new();

    public float FlowerShare { get; init; } = 0.2f;

    /// <summary>The strip inside the wall: how far in from the panels (min, max), and a candidate every <see cref="WallSpacing"/>.</summary>
    public float[] WallInset { get; init; } = { 110f, 190f };

    public float WallSpacing { get; init; } = 170f;

    public float WallChance { get; init; } = 0.75f;

    /// <summary>Along the lanes: a candidate each side every <see cref="LaneSpacing"/>, just past the corridor's edge.</summary>
    public float LaneSpacing { get; init; } = 300f;

    public float LaneBeyond { get; init; } = 40f;

    public float LaneChance { get; init; } = 0.45f;

    public float[] Scale { get; init; } = { 0.6f, 1.1f };

    /// <summary>Room each plant needs (its fit-check square), and from any NPC.</summary>
    public float Size { get; init; } = 70f;

    public float ActorClearance { get; init; } = 90f;

    public float Sink { get; init; } = 4f;
}

/// <summary>A smoke plume over every placed fire of the given bases.</summary>
internal sealed record LifeSmoke
{
    public bool Enabled { get; init; } = true;

    public string Piece { get; init; } = string.Empty;

    public List<string> Fires { get; init; } = new();

    public float Z { get; init; } = 30f;

    public float Scale { get; init; } = 1f;
}

/// <summary>
/// Animals of the fair's own: a copy of a vanilla creature, with the fair's NPC keyword
/// (so the SPID exclusions and the crowd culling take it), invulnerable, and optionally
/// calmed (unaggressive, its factions and packages replaced).
/// </summary>
internal sealed record LifeAnimal
{
    public string EditorId { get; init; } = string.Empty;

    public string Base { get; init; } = string.Empty;

    /// <summary>Replace the copy's factions with these (rank 0); null keeps the vanilla ones.</summary>
    public List<string>? Factions { get; init; }

    /// <summary>Replace the copy's packages with these; null keeps the vanilla ones.</summary>
    public List<string>? Packages { get; init; }

    public bool Unaggressive { get; init; }

    /// <summary>A market dressing module it stands in (the first placed one of that name); empty for world positions.</summary>
    public string Near { get; init; } = string.Empty;

    /// <summary><c>[x, y, yaw]</c> each, in the module's frame or the world's.</summary>
    public List<float[]> At { get; init; } = new();
}

/// <summary>
/// The fair compound's exterior in Tamriel (FairExterior.cs): the fair's outline scaled by
/// <see cref="Scale"/> about its gate, turned by <see cref="RotationDegrees"/> and put with its
/// gate at <see cref="Gate"/>; its own FormID range, built last.
/// </summary>
internal sealed record ExteriorConfig
{
    public bool Enabled { get; init; }

    public uint FormIdBase { get; init; } = 0x30000;

    /// <summary>Where the gate stands in Tamriel.</summary>
    public float[] Gate { get; init; } = { 0f, 0f };

    /// <summary>0 or 180.</summary>
    public float RotationDegrees { get; init; }

    public float Scale { get; init; } = 1f;

    /// <summary>The vanilla load door copied for both gates (its sounds and flags; the model is the fair's gate).</summary>
    public string DoorTemplate { get; init; } = "00050973:Skyrim.esm";

    public string GateName { get; init; } = "The Wanderer's Fair";

    public string ExitName { get; init; } = "Whiterun Hold";

    /// <summary>How far in from each gate the player arrives.</summary>
    public float Arrive { get; init; } = 300f;

    /// <summary>How far out in front of the gate the map marker stands.</summary>
    public float MapMarkerOut { get; init; } = 900f;

    /// <summary>Banners and pennant ropes on the wall's outer face (the fair's life.palisade settings).</summary>
    public bool Decorate { get; init; } = true;

    /// <summary>Vanilla scenery bigger than this (landscape rocks) is left alone.</summary>
    public float ClearMaxRadius { get; init; } = 900f;

    public float ClearMargin { get; init; } = 100f;

    /// <summary>Parts of the built fair copied inside: the stage, the towers, the fires.</summary>
    public List<ExteriorCluster> Clusters { get; init; } = new();

    public ExteriorShow Show { get; init; } = new();

    public ExteriorApproach Approach { get; init; } = new();

    public ExteriorGateFlags GateFlags { get; init; } = new();

    /// <summary>
    /// Copied pieces of these bases within <see cref="HideNearWall"/> of the wall line are copied
    /// disabled (FormIDs kept): the towers stand whole inside the smaller wall, so their outward
    /// banners hung in it (2026-09-25).
    /// </summary>
    public List<string> HideNearWallBases { get; init; } = new();

    /// <summary>
    /// Vanilla references the clearing leaves alone: the big slab (023362) that hides a dip
    /// where DynDOLOD's terrain underside shows through; the gate was moved clear of it instead.
    /// </summary>
    public List<string> Keep { get; init; } = new();

    public float HideNearWall { get; init; } = 120f;

    /// <summary>The Whiterun road sign beside the path, out from the gate.</summary>
    public ExteriorRoadSign RoadSign { get; init; } = new();

    /// <summary>More on the wall's outer face: banners between the life pass's, and lamp posts.</summary>
    public ExteriorWallDressing WallDressing { get; init; } = new();

    /// <summary>The festival entrance: braziers, banners, lamps and clutter by the gate and path.</summary>
    public List<ExteriorDecor> Decor { get; init; } = new();

    /// <summary>The fair's own welcome sign by the path.</summary>
    public ExteriorFairSign FairSign { get; init; } = new();

    /// <summary>
    /// Vanilla pieces over ground the clearing left bare where it shows through: <c>at</c> is
    /// <c>[x, y, yaw]</c>, world; set into the ground by <c>z</c> (negative sinks it). The dip the
    /// big slab covered showed DynDOLOD's terrain underside (2026-09-25).
    /// </summary>
    public List<MarketPiece> Cover { get; init; } = new();

    /// <summary>How far under the ground a cleared large reference is sunk (they aren't disabled: see FairExterior).</summary>
    public float SinkLarge { get; init; } = 3000f;
}

/// <summary>
/// The way in: from <see cref="Start"/> out of the gate to <see cref="To"/> (a point on the
/// road), vanilla scenery cleared within <see cref="ClearHalfWidth"/> (the road left), and
/// road chunks laid on the ground every <see cref="Spacing"/>: one across the middle and a
/// ragged edge piece each side.
/// </summary>
internal sealed record ExteriorApproach
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Lay the road chunks. Off (2026-09-25), the way stays cleared but bare: on this slope
    /// the wide, thin chunks stood out of the ground like rocks. Their FormIDs are still taken.
    /// </summary>
    public bool Pave { get; init; } = true;

    public float Start { get; init; } = 150f;

    public float[]? To { get; init; }

    public float ClearHalfWidth { get; init; } = 450f;

    public float Spacing { get; init; } = 170f;

    public float HalfWidth { get; init; } = 200f;

    public float Sink { get; init; } = 6f;

    public List<string> Pieces { get; init; } = new();

    public List<string> EdgePieces { get; init; } = new();
}

/// <summary>A flag either side of the gate, <see cref="Out"/> along the wall and <see cref="Forward"/> in front of it.</summary>
/// <summary>
/// The "Whiterun Fair" welcome sign beside the path out from the gate, facing arrivals. Its
/// static is built with the exterior, in the exterior's FormID range, so nothing renumbers.
/// </summary>
internal sealed record ExteriorFairSign : ProjectStaticConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>1: on the gate's right as you come out; -1: its left.</summary>
    public float Side { get; init; } = -1f;

    public float Out { get; init; } = 300f;

    public float Forward { get; init; } = 480f;

    /// <summary>How far the post's foot goes into the ground, for slopes.</summary>
    public float Sink { get; init; } = 6f;

    /// <summary>Degrees the face turns from straight out toward the path (+ toward it).</summary>
    public float TurnToPath { get; init; } = 15f;
}

/// <summary>
/// A piece of the exterior's dressing, placed in the gate's frame: <see cref="Side"/> along the
/// wall (+ is the right coming out), <see cref="Forward"/> out from it, on the ground plus
/// <see cref="Z"/>, turned <see cref="Yaw"/> degrees from facing out. <see cref="Tilt"/> lays it
/// on the slope (clutter); posts and lights stand upright.
/// </summary>
internal sealed record ExteriorWallDressing
{
    public bool Enabled { get; init; }

    /// <summary>A banner on every panel without one within <see cref="BannerClear"/>.</summary>
    public string Banner { get; init; } = "000DEE54:Skyrim.esm";

    public float BannerClear { get; init; } = 80f;

    /// <summary>A lamp post (arm toward the wall) every this many panels, with a light at its lamp.</summary>
    public int LampEvery { get; init; } = 3;

    public string Lamp { get; init; } = "001097E7:Skyrim.esm";

    public float LampOut { get; init; } = 110f;

    public string LampLight { get; init; } = "0008278A:Skyrim.esm";

    /// <summary>Nothing within this of the gate (the entrance has its own dressing).</summary>
    public float GateClear { get; init; } = 450f;
}

internal sealed record ExteriorDecor
{
    public string Name { get; init; } = string.Empty;

    public string Piece { get; init; } = string.Empty;

    public float Side { get; init; }

    public float Forward { get; init; }

    public float Z { get; init; }

    public float Yaw { get; init; }

    public float Scale { get; init; } = 1f;

    public bool Tilt { get; init; }
}

internal sealed record ExteriorRoadSign
{
    public bool Enabled { get; init; } = true;

    /// <summary>1: on the gate's right as you come out; -1: its left.</summary>
    public float Side { get; init; } = 1f;

    public float Out { get; init; } = 280f;

    public float Forward { get; init; } = 320f;

    /// <summary>The post and its arms: each piece's z above the ground, and its world yaw in degrees.</summary>
    public List<MarketPiece> Pieces { get; init; } = new();
}

internal sealed record ExteriorGateFlags
{
    public bool Enabled { get; init; } = true;

    public float Out { get; init; } = 450f;

    public float Forward { get; init; } = 110f;

    /// <summary>The flag's pieces, in its frame (y toward the road).</summary>
    public List<MarketPiece> Pieces { get; init; } = new();
}

/// <summary>
/// A part of the fair copied into the exterior, whole and unscaled: every placed object within
/// <see cref="Radius"/> of <see cref="Centre"/> (or inside <see cref="Rect"/>, [x0, y0, x1, y1]),
/// moved to <see cref="To"/> ([u, v] from the gate in the fair's frame, unscaled) or, without
/// it, to the centre's scaled place.
/// </summary>
internal sealed record ExteriorCluster
{
    public string Name { get; init; } = string.Empty;

    public float[] Centre { get; init; } = { 0f, 0f };

    public float Radius { get; init; } = 200f;

    public float[]? Rect { get; init; }

    public float[]? To { get; init; }
}

/// <summary>The fair heard and seen from outside: its songs, faintly, the crowd, and fireworks at night.</summary>
internal sealed record ExteriorShow
{
    public bool Enabled { get; init; } = true;

    /// <summary>The cluster the music and the fireworks come from (the stage).</summary>
    public string Cluster { get; init; } = "stage";

    public float SpeakerHeight { get; init; } = 300f;

    public float MusicMinDistance { get; init; } = 1500f;

    public float MusicMaxDistance { get; init; } = 9000f;

    /// <summary>Decibels quieter than the stage.</summary>
    public float MusicAttenuation { get; init; } = 12f;

    public float SongGap { get; init; } = 20f;

    public float CrowdMinDistance { get; init; } = 800f;

    public float CrowdMaxDistance { get; init; } = 4000f;

    public float CrowdAttenuation { get; init; } = 6f;

    /// <summary>Seconds between volleys at night (varied a little).</summary>
    public float FireworkEvery { get; init; } = 150f;
}
