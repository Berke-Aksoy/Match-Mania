using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
    private static BlockPool _instance;
    public static BlockPool Singleton { get => _instance; }

    private static Dictionary<BlockData.COLORTYPE, Queue<ColoredBlock>> _blockPoolByColor = new Dictionary<BlockData.COLORTYPE, Queue<ColoredBlock>>();
    private static List<ColoredBlock> _coloredBlockPool = new List<ColoredBlock>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StoreColoredBlockToPool(ColoredBlock coloredBlock)
    {
        if(coloredBlock == null) { return; }

        _coloredBlockPool.Add(coloredBlock);
    }

    public void SortColoredBlockPool()
    {
        Queue<ColoredBlock> blueBlocks = new Queue<ColoredBlock>();
        Queue<ColoredBlock> greenBlocks = new Queue<ColoredBlock>();
        Queue<ColoredBlock> pinkBlocks = new Queue<ColoredBlock>();
        Queue<ColoredBlock> purpleBlocks = new Queue<ColoredBlock>();
        Queue<ColoredBlock> redBlocks = new Queue<ColoredBlock>();
        Queue<ColoredBlock> yellowBlocks = new Queue<ColoredBlock>();

        foreach (ColoredBlock block in _coloredBlockPool)
        {
            switch(block.Data.ColorType)
            {
                case BlockData.COLORTYPE.Blue: blueBlocks.Enqueue(block); break;
                case BlockData.COLORTYPE.Green: greenBlocks.Enqueue(block);break;
                case BlockData.COLORTYPE.Pink: pinkBlocks.Enqueue(block); break;
                case BlockData.COLORTYPE.Purple: purpleBlocks.Enqueue(block); break;
                case BlockData.COLORTYPE.Red: redBlocks.Enqueue(block); break;
                case BlockData.COLORTYPE.Yellow: yellowBlocks.Enqueue(block); break;
            }
        }

        _blockPoolByColor.Add(BlockData.COLORTYPE.Blue, blueBlocks);
        _blockPoolByColor.Add(BlockData.COLORTYPE.Green, greenBlocks);
        _blockPoolByColor.Add(BlockData.COLORTYPE.Pink, pinkBlocks);
        _blockPoolByColor.Add(BlockData.COLORTYPE.Purple, purpleBlocks);
        _blockPoolByColor.Add(BlockData.COLORTYPE.Red, redBlocks);
        _blockPoolByColor.Add(BlockData.COLORTYPE.Yellow, yellowBlocks);

    }

    public GameObject GetPooledBlock(BlockData.COLORTYPE color)
    {
        //blockPoolByColor.GetValueOrDefault(color);
        return null;
    }

    public void ClearPools()
    {
        
    }
}
