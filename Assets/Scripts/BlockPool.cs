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
        List<List<ColoredBlock>> copiedColoredPool = new List<List<ColoredBlock>>();

        foreach (var blockList in _coloredPool)
        {
            // Create a new list for each sublist
            List<ColoredBlock> copiedBlockList = new List<ColoredBlock>();

            foreach (var block in blockList)
            {
                ColoredBlock newBlock = Instantiate(block, transform);
                copiedBlockList.Add(newBlock);
                newBlock.gameObject.SetActive(false);
            }
            copiedColoredPool.Add(copiedBlockList);
        }

        _blocksGroupedByColor = copiedColoredPool.Select((coloredBlocks, index) => new { ColorType = (COLORTYPE)index, ColoredBlocks = coloredBlocks })
                                            .ToDictionary(item => item.ColorType, item => item.ColoredBlocks);
    }

    public ColoredBlock TryGetPooledBlock(COLORTYPE color)
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

    public ColoredBlock TryGetPooledBlock() // Tries to get a block that has a random color.
    {
        int colorCount = _blocksGroupedByColor.Count;
        COLORTYPE colorType = (COLORTYPE) Random.Range(0, colorCount);
        List<ColoredBlock> blocks = null;

        if (!_blocksGroupedByColor.ContainsKey(colorType))
        {
            return null;
        }
        else
        {
            blocks = _blocksGroupedByColor[colorType];
        }

        if (blocks != null && blocks.Count != 0 )
        {
            ColoredBlock poppedBlock = blocks[blocks.Count - 1];
            _blocksGroupedByColor[colorType].RemoveAt(blocks.Count - 1);
            poppedBlock.gameObject.SetActive(true);
            return poppedBlock;
        }
        else
        {
            return null;
        }
    }

    public void BlockToPool(Block block)
    {
        block.SetGroupID(-1, 1);

        if (_blocksGroupedByColor.GetValueOrDefault(block.Data.ColorType) != null)
        {
            _blocksGroupedByColor.GetValueOrDefault(block.Data.ColorType).Add((ColoredBlock)block);
            block.transform.SetParent(transform);
        }
        else
        {
            List<ColoredBlock> blocks = new List<ColoredBlock>
            {
                (ColoredBlock)block
            };

            _blocksGroupedByColor.Add(block.Data.ColorType, blocks);
            block.transform.SetParent(transform);
        }

        block.gameObject.SetActive(false);
    }

}
