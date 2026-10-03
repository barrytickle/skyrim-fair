using Mutagen.Bethesda.Skyrim;

namespace SkyrimFair.Generator;

/// <summary>
/// Files exterior cells into a worldspace's block / sub-block groups. Exterior cells are
/// grouped 32x32 per block and 8x8 per sub-block, by floor division so negative cell
/// coordinates land in the correct group.
/// </summary>
internal sealed class ExteriorCellGrid
{
    private const int BlockSize = 32;

    private const int SubBlockSize = 8;

    private readonly Worldspace worldspace;

    private readonly Dictionary<(int X, int Y), WorldspaceBlock> blocks = new();

    private readonly Dictionary<(int X, int Y), WorldspaceSubBlock> subBlocks = new();

    public ExteriorCellGrid(Worldspace worldspace) => this.worldspace = worldspace;

    public void Add(Cell cell, int cx, int cy)
    {
        var blockKey = (FloorDivide(cx, BlockSize), FloorDivide(cy, BlockSize));
        var subKey = (FloorDivide(cx, SubBlockSize), FloorDivide(cy, SubBlockSize));

        if (!blocks.TryGetValue(blockKey, out var block))
        {
            block = new WorldspaceBlock
            {
                BlockNumberX = (short)blockKey.Item1,
                BlockNumberY = (short)blockKey.Item2,
                GroupType = GroupTypeEnum.ExteriorCellBlock,
            };
            blocks[blockKey] = block;
            worldspace.SubCells.Add(block);
        }

        if (!subBlocks.TryGetValue(subKey, out var subBlock))
        {
            subBlock = new WorldspaceSubBlock
            {
                BlockNumberX = (short)subKey.Item1,
                BlockNumberY = (short)subKey.Item2,
                GroupType = GroupTypeEnum.ExteriorCellSubBlock,
            };
            subBlocks[subKey] = subBlock;
            block.Items.Add(subBlock);
        }

        subBlock.Items.Add(cell);
    }

    private static int FloorDivide(int value, int divisor)
        => (int)Math.Floor(value / (double)divisor);
}
