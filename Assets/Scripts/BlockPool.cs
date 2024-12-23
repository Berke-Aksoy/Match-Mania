using System.Collections.Generic;
using UnityEngine;
using MatchMania.Blocks;
using static BlockData;
using System.Linq;

public class BlockPool : BaseSingleton<BlockPool>
{
    private static Dictionary<COLORTYPE, List<ColoredBlock>> _blocksGroupedByColor = new Dictionary<COLORTYPE, List<ColoredBlock>>();

    private void OnEnable()
    {
        BoardManager.OnBoardComplete += StoreColoredBlocksToPool;
    }

    private void OnDisable()
    {
        BoardManager.OnBoardComplete -= StoreColoredBlocksToPool;
    }

    public void StoreColoredBlocksToPool()
    {
        List<List<ColoredBlock>> _coloredPool = BoardManager.Instance.GroupBlocksByColor();
        _blocksGroupedByColor = _coloredPool.Select((coloredBlocks, index) => new { ColorType = (COLORTYPE)index, ColoredBlocks = coloredBlocks })
                                            .ToDictionary(item => item.ColorType, item => item.ColoredBlocks);
    }

    public ColoredBlock GetPooledBlock(COLORTYPE color)
    {
        List<ColoredBlock> blocks = _blocksGroupedByColor.GetValueOrDefault(color);
        if (blocks != null)
        {
            ColoredBlock poppedBlock = blocks[blocks.Count - 1];
            blocks.RemoveAt(blocks.Count - 1);
            return poppedBlock;
        }
        else
        {
            return null;
        }
    }

    public void BlockToPool(Block block)
    {
        _blocksGroupedByColor.GetValueOrDefault(block.Data.ColorType)?.Add((ColoredBlock)block);
    }

}
