using MatchMania.Blocks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BlockData;
using Random = UnityEngine.Random;

public class BoardManager : BaseSingleton<BoardManager>
{
    private LevelBoardData _levelBoardData;

    private Block[,] _board;
    public Block[,] Board { get => _board; }

    private Dictionary<Vector2Int, ColoredBlock> _coloredBlocks = new Dictionary<Vector2Int, ColoredBlock>();
    public Dictionary<Vector2Int, ColoredBlock> ColoredBlocks { get => _coloredBlocks; }
    private Dictionary<Vector2Int, ObstacleBlock> _obstacleBlocks = new Dictionary<Vector2Int, ObstacleBlock>();
    public Dictionary<Vector2Int, ObstacleBlock> ObstacleBlocks { get => _obstacleBlocks; }

    public static event Action OnBoardComplete;

    protected override void Awake()
    {
        base.Awake();
        _levelBoardData = LevelManager.Instance.GetLevelData();
    }

    private void Start()
    {
        int[] dimensions = GetBoardDimensions();
        CreateBoard(dimensions[0], dimensions[1]);
        GroupManager groupManager = GroupManager.Instance; // This code ensures that GroupManager exists in the game.
        OnBoardComplete?.Invoke();
    }

    private void CreateBoard(int rowCount, int colCount)
    {
        _board = new Block[colCount, rowCount];

        for (int i = 0; i < colCount; i++)
        {
            for (int j = 0; j < rowCount; j++)
            {
                if (Random.Range(0, 100) < GetBlockChances()[0]) { CreateBlock(i, j, BLOCKTYPE.Colored, true, true); }
                else { CreateBlock(i, j, BLOCKTYPE.Obstacle, true, true); }
            }
        }
    }

    public Block CreateBlock(int x, int y, BLOCKTYPE blockType, bool setLoc = true, bool isInit = false)
    {
        Vector2Int loc = new Vector2Int(x, y);
        GameObject newObj;
        Block newBlock = null;
        Block[] blocks;

        switch (blockType)
        {
            case BLOCKTYPE.Colored:
                blocks = _levelBoardData.UsedColoredBlocks;
                break;
            case BLOCKTYPE.Obstacle:
                blocks = _levelBoardData.UsedObstacleBlocks;
                break;
            default:
                blocks = null;
                break;
        }

        if (blocks == null) { return null; }

        if (!isInit)
        {
            newBlock = BlockPool.Instance.TryGetPooledBlock();
        }

        if (newBlock == null)
        {
            newObj = Instantiate(blocks[Random.Range(0, blocks.Length)].gameObject, new Vector3(x, y, 0), Quaternion.identity, transform);
            newBlock = newObj.GetComponent<Block>();
        }
        else
        {
            newBlock.transform.position = new Vector3(x, y, 0);
        }

        if (setLoc) { SetBlockLoc(newBlock, loc); }

        return newBlock;
    }

    public void SetBlockLoc(Block block, Vector2Int newLoc, bool removeOld = false, bool doesSwap = false) // Does fall variable is just used for blocks that are already in the board and will fall under. Do not set it to true in case of creation of missing blocks
    {
        if (!Helpers.IsWithinBounds(newLoc) || block == null) { return; }

        BLOCKTYPE blockType = block.Data.BlockType;
        Vector2Int oldLoc = block.Location;

        switch (blockType)
        {
            case BLOCKTYPE.Colored:
                if (removeOld) { _coloredBlocks.Remove(oldLoc); }
                else if (doesSwap) { _coloredBlocks.Remove(newLoc); }
                _coloredBlocks.Add(newLoc, (ColoredBlock)block); break;
            case BLOCKTYPE.Obstacle:
                if (removeOld) { _obstacleBlocks.Remove(oldLoc); }
                else if (doesSwap) { _obstacleBlocks.Remove(newLoc); }
                _obstacleBlocks.Add(newLoc, (ObstacleBlock)block); break;
        }

        if (removeOld) { _board[oldLoc.x, oldLoc.y] = null; }
        block.Location = newLoc;
        _board[newLoc.x, newLoc.y] = block;
    }

    public void RemoveBlock(Vector2Int loc, BLOCKTYPE blockType)
    {
        switch (blockType)
        {
            case BLOCKTYPE.Colored:
                _coloredBlocks.Remove(loc);
                break;
            case BLOCKTYPE.Obstacle:
                _obstacleBlocks.Remove(loc);
                break;
        }

        _board[loc.x, loc.y] = null;
    }

    public void DestroyBlock(Block blockToDestroy)
    {
        RemoveBlock(blockToDestroy.Location, blockToDestroy.Data.BlockType);
        BlockPool.Instance.BlockToPool(blockToDestroy);
    }

    public void SwapBlocks(Vector2Int pos1, Vector2Int pos2)
    {
        Block adjacent = _board[pos1.x, pos1.y];
        Block mover = _board[pos2.x, pos2.y];

        SetBlockLoc(adjacent, pos2, false, true);
        SetBlockLoc(mover, pos1, false, true);

        BlockAnimator.AnimateBlockLocationChange(adjacent, pos1);
        BlockAnimator.AnimateBlockLocationChange(mover, pos2);
    }

    public List<List<ColoredBlock>> GroupBlocksByColor()
    {
        Dictionary<COLORTYPE, List<ColoredBlock>> blocksByColorDict = new Dictionary<COLORTYPE, List<ColoredBlock>>();

        foreach (ColoredBlock block in _coloredBlocks.Values)
        {
            COLORTYPE colorType = block.Data.ColorType;

            if (!blocksByColorDict.ContainsKey(colorType))
            {
                blocksByColorDict[colorType] = new List<ColoredBlock>();
            }

            blocksByColorDict[colorType].Add(block);
        }

        List<List<ColoredBlock>> groupedBlocks = new List<List<ColoredBlock>>(blocksByColorDict.Values);

        return groupedBlocks;
    }

    public Dictionary<Vector2Int, ObstacleBlock> FindAdjacentObstacles(List<Block> blocksToSearch)
    {
        Dictionary<Vector2Int, ObstacleBlock> adjacentObstacles = new Dictionary<Vector2Int, ObstacleBlock>();

        foreach (Block block in blocksToSearch)
        {
            foreach (Vector2Int dir in Helpers.NeighborDirections)
            {
                Vector2Int adjacentLoc = block.Location + dir;
                ObstacleBlock obstacle = _obstacleBlocks.GetValueOrDefault(adjacentLoc);

                if (Helpers.IsWithinBounds(adjacentLoc) && obstacle != null)
                {
                    if (!adjacentObstacles.ContainsKey(adjacentLoc))
                    {
                        adjacentObstacles.Add(adjacentLoc, obstacle);
                    }
                }
            }
        }

        return adjacentObstacles;
    }

    public Vector2Int? FindValidAdjacentLoc(Vector2Int locToCheck) // Returns a swapable adjacent location which holds a colored block according to given location
    {
        List<Vector2Int> validLocs = new List<Vector2Int>();

        foreach (Vector2Int dir in Helpers.NeighborDirections)
        {
            Vector2Int adjacentLoc = locToCheck + dir;

            if (Helpers.IsWithinBounds(adjacentLoc))
            {
                Block block = _board[adjacentLoc.x, adjacentLoc.y];
                if (block == null || block.Data.BlockType == BLOCKTYPE.Obstacle) { continue; } // Obstacle blocks are not swappable

                validLocs.Add(adjacentLoc);
            }
        }

        if (validLocs.Count == 0) { return null; }
        return validLocs[UnityEngine.Random.Range(0, validLocs.Count)];
    }

    public int FindLowestAvailableRow(int col, int startRow) // Finds the lowest available row in the specified column, starting from the given row.
    {
        int lowestAvailableRow = startRow;

        // Start checking from the current row down to row 0
        for (int row = startRow - 1; row >= 0; row--)
        {
            if (_board[col, row] == null)
            {
                lowestAvailableRow = row; // Update the lowest available row to this empty cell
            }
            else if (_board[col, row].Data.BlockType == BLOCKTYPE.Obstacle)
            {
                break; // Stop the search if an obstacle is encountered
            }
        }

        return lowestAvailableRow;
    }

    public int[] GetBoardDimensions()
    {
        int[] dimensions = {_levelBoardData.MaxRowCount, _levelBoardData.MaxColumnCount};
        return dimensions;
    }

    public int[] GetBlockChances()
    {
        int[] chances = { _levelBoardData.coloredBlockChance, _levelBoardData.obstacleBlockChance, _levelBoardData.powerBlockChance};
        return chances;
    }
}
