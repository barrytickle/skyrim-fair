using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Builds the isolated sandbox cell described by <see cref="SandboxConfig"/>.
///
/// It is an interior cell so that it needs no landscape, no LOD and no worldspace of
/// its own, but it carries the Show Sky and Use Sky Lighting flags so pieces are lit
/// and seen under a real tundra sky rather than an interior lighting rig. Every
/// lighting value is inherited from the template, so the XCLL block is present (the
/// engine expects one on interiors) but contributes nothing of its own.
///
/// The floor reuses the foundation kit's 1024 fill body and paving cap, one on top of
/// the other exactly as the fair lays them, so anything tested here stands at the same
/// height above the same surface it will stand on at the site.
/// </summary>
internal static class FairSandbox
{
    private const float TileSize = 1024f;

    /// <summary>All eleven XCLL inherit bits: take everything from the lighting template.</summary>
    private const int InheritEverything = 0x7FF;

    /// <summary>Lift the player marker clear of the paving so nothing spawns inside it.</summary>
    private const float MarkerLift = 8f;

    public static SandboxResult Build(SkyrimMod mod, FairConfig config, Func<string, Static> staticFor)
    {
        var s = config.Sandbox;

        var cell = new Cell(mod)
        {
            EditorID = s.EditorId,
            Name = s.Name,
            Flags = Cell.Flag.IsInteriorCell | Cell.Flag.ShowSky | Cell.Flag.UseSkyLighting,
            Lighting = new CellLighting { Inherits = (CellLighting.Inherit)InheritEverything },
            LightingTemplate = new FormLinkNullable<ILightingTemplateGetter>(
                FormKeyHelper.Parse(s.LightingTemplate)),
            SkyAndWeatherFromRegion = new FormLinkNullable<IRegionGetter>(
                FormKeyHelper.Parse(s.WeatherRegion)),
            ImageSpace = new FormLinkNullable<IImageSpaceGetter>(
                FormKeyHelper.Parse(s.ImageSpace)),
        };

        // ---- floor: an n x n square of kit tiles centred on the origin ----------
        var floorFill = staticFor("floorFill");
        var paveCap = staticFor("paveCapFill");
        var half = (s.Tiles - 1) / 2f;
        var placed = 0;

        for (var row = 0; row < s.Tiles; row++)
        {
            for (var col = 0; col < s.Tiles; col++)
            {
                var x = (col - half) * TileSize;
                var y = (half - row) * TileSize;
                cell.Temporary.Add(Place(mod, floorFill.FormKey, x, y, s.FloorZ));
                cell.Temporary.Add(Place(mod, paveCap.FormKey, x, y, s.FloorZ));
                placed += 2;
            }
        }

        // ---- coc marker: where the console drops the player ---------------------
        var marker = Place(mod, FormKeyHelper.Parse(s.CocMarker), 0f, 0f, s.FloorZ + MarkerLift);
        marker.EditorID = s.EditorId + "COCMarker";
        // Mutagen doesn't write the Persistent flag from group membership; xEdit
        // reports a ref in the Persistent group without it as an error.
        marker.MajorRecordFlagsRaw = 0x400;
        cell.Persistent.Add(marker);

        // Interior cells are filed by the decimal digits of their FormID: the last
        // digit picks the block, the one before it the sub-block.
        var id = cell.FormKey.ID;
        var subBlock = new CellSubBlock
        {
            BlockNumber = (int)(id / 10 % 10),
            GroupType = GroupTypeEnum.InteriorCellSubBlock,
        };
        subBlock.Cells.Add(cell);

        var block = new CellBlock
        {
            BlockNumber = (int)(id % 10),
            GroupType = GroupTypeEnum.InteriorCellBlock,
        };
        block.SubBlocks.Add(subBlock);
        mod.Cells.Records.Add(block);

        return new SandboxResult(cell.FormKey, s.EditorId, s.Tiles, placed, s.Tiles * TileSize);
    }

    private static PlacedObject Place(SkyrimMod mod, FormKey baseKey, float x, float y, float z)
        => new(mod)
        {
            Base = new FormLinkNullable<IPlaceableObjectGetter>(baseKey),
            Placement = new Placement
            {
                Position = new P3Float(x, y, z),
                Rotation = new P3Float(0f, 0f, 0f),
            },
        };
}

internal sealed record SandboxResult(
    FormKey CellFormKey,
    string EditorId,
    int Tiles,
    int FloorPiecesPlaced,
    float SideLength);
