using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public sealed class BoardCreator : MonoBehaviour
{
    [SerializeField] private LevelBoardData _levelBoardData; // To Do: Get this level from levelManager or somewhere else
    private Block[,] _board;
    private List<BlockGroup> _blockGroups = new List<BlockGroup>();
    Dictionary<Vector2Int, ColoredBlock> _coloredBlocks = new Dictionary<Vector2Int, ColoredBlock>();
    Dictionary<Vector2Int, ObstacleBlock> _obstacleBlocks = new Dictionary<Vector2Int, ObstacleBlock>();

    [Header("Thresholds")]
    [SerializeField] private int minBlastableBlockCount = 2;

    private void Start()
    {
        CreateBoard(_levelBoardData.MaxRowCount, _levelBoardData.MaxColumnCount);
        FindColoredBlockGroups();
        AssignGroupIDs();
    }

    private void CreateBoard(int rowCount, int colCount)
    {
        _board = new Block[colCount, rowCount];
        BlockPool poolSingleton = BlockPool.Singleton;

        for(int i = 0; i < colCount; i++)
        {
            for(int j = 0; j < rowCount; j++)
            {
                GameObject newObj;
                Vector2Int loc = new Vector2Int(i, j);

                if (Random.Range(0, 100) < 90) // Create colored blocks
                {
                    newObj = Instantiate(_levelBoardData.UsedColoredBlocks[Random.Range(0, _levelBoardData.UsedColoredBlocks.Length)].gameObject, new Vector3(i, j, 0), Quaternion.identity, transform);
                    ColoredBlock newColoredBlock = newObj.GetComponent<ColoredBlock>();
                    SetBlockLoc(newColoredBlock, loc);
                }
                else // Create obstacle blocks
                {
                    newObj = Instantiate(_levelBoardData.UsedObstacleBlocks[Random.Range(0, _levelBoardData.UsedObstacleBlocks.Length)].gameObject, new Vector3(i, j, 0), Quaternion.identity, transform);
                    ObstacleBlock newObstacleBlock  = newObj.GetComponent<ObstacleBlock>();
                    SetBlockLoc(newObstacleBlock, loc);
                }
            }
        }

        poolSingleton.SortColoredBlockPool();
    }

    private void ReCreate()
    {
        foreach(Block block in _board)
        {
            Destroy(block);
        }

        CreateBoard(_levelBoardData.MaxRowCount, _levelBoardData.MaxColumnCount);
    }

    #region Grouping Mechanic

    private void FindColoredBlockGroups()
    {
        List<Block> candidateBlockList = new List<Block>();
        List<BlockGroup> blockGroups = new List<BlockGroup>();
        BlockGroup.ResetIDCounter();

        Queue<Block> queue = new Queue<Block>();

        int totalRows = _levelBoardData.MaxRowCount;
        int totalCols = _levelBoardData.MaxColumnCount;

        bool[,] visited = new bool[totalCols, totalRows];
        BlockData.COLORTYPE targetColorType;

        Dictionary<Vector2Int, ColoredBlock>.Enumerator pointer = _coloredBlocks.GetEnumerator();

        while (pointer.MoveNext()) {
            ColoredBlock coloredBlock = pointer.Current.Value;
            Vector2Int coloredBlockLoc = pointer.Current.Key;
            targetColorType = coloredBlock.Data.ColorType;

            queue.Enqueue(coloredBlock);
            visited[coloredBlockLoc.x, coloredBlockLoc.y] = true;

            while (queue.Count > 0)
            {
                Block currentBlock = queue.Dequeue();
                candidateBlockList.Add(currentBlock);

                foreach (Vector2Int direction in NeighborDirections)
                {
                    Vector2Int adjacentLoc = currentBlock.LocationOnBoard + direction;

                    if (IsWithinBounds(adjacentLoc.x, adjacentLoc.y) && !visited[adjacentLoc.x, adjacentLoc.y] && _board[adjacentLoc.x, adjacentLoc.y] != null && _board[adjacentLoc.x, adjacentLoc.y].Data.ColorType == targetColorType)
                    {
                        queue.Enqueue(_board[adjacentLoc.x, adjacentLoc.y]);
                        visited[adjacentLoc.x, adjacentLoc.y] = true;
                    }
                }
            }

            int blockCount = candidateBlockList.Count;

            if (blockCount >= minBlastableBlockCount)
            {
                BlockGroup newGroup = new BlockGroup(candidateBlockList);
                blockGroups.Add(newGroup);
            }
            else
            {
                foreach (Block block in candidateBlockList) // Change the sprites and groupIDs of blocks that are not on a group to default sprite
                {
                    block.SetGroupID(-1, blockCount);
                }
            }

            candidateBlockList.Clear(); // Reuse the list for the next group
        }

        _blockGroups = blockGroups;

        if (blockGroups.Count == 0 && _coloredBlocks.Count > minBlastableBlockCount) // && PowerBlockCount = 0 // Check whether there are no groups and enough colored blocks to blast
        {
            Debug.Log("Shuffle is available and needed");
            CountColors();
            //IntentionalShuffle();
            //FindColoredBlockGroups();
        }
    }

    private void AssignGroupIDs()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            Debug.Log(blockGroup.PrintGroupInfo());
            foreach(Block block in blockGroup.Blocks)
            {
                Debug.Log(block.LocationOnBoard);
                block.SetGroupID(blockGroup.GroupID, blockGroup.Blocks.Count);
            }
        }
    }

    #endregion

    #region Shuffle Mechanic
    
    private void IntentionalShuffle()
    {
        int usedColorTypeCount = _levelBoardData.UsedColoredBlocks.Length;
        int[] colorCounts = new int[usedColorTypeCount];
        List<ColoredBlock>[] coloredBlocksByType = new List<ColoredBlock>[usedColorTypeCount];
        int selectedColorIndex = CountColors(usedColorTypeCount, ref colorCounts, ref coloredBlocksByType); // At first selected color index points to the most used color
        int tryCount = 0;

        ColoredBlock pivotBlock = coloredBlocksByType[selectedColorIndex][Random.Range(0, colorCounts[selectedColorIndex])];  // Randomly select a colored block from the selected color type to be the "pivot" block

        while (tryCount < usedColorTypeCount)
        {
            if (colorCounts[selectedColorIndex] < minBlastableBlockCount) { return; }

            foreach (ColoredBlock candidateBlock in coloredBlocksByType[selectedColorIndex])  // Try to find a block to swap with, they are already in same color therefore, no need to check color
            {
                if (candidateBlock.LocationOnBoard == pivotBlock.LocationOnBoard) continue;

                /*
                if (TryMakeAdjacent(pivotPos, candidatePos)) // Attempt to swap the blocks to make them adjacent
                {
                    FindColoredBlockGroups(); // Re-run group detection
                    return;
                }
                */
            }
        }

        Debug.LogWarning("Could not find a suitable block to swap for creating a valid group. Try instantiating a new block.");
    }
    

    private int CountColors(int usedColorTypeCount, ref int[] colorCounts, ref List<ColoredBlock>[] coloredBlocksByType)
    {
        int mostUsedColor = 0;
        BlockData.COLORTYPE[] colorsToCompare = new BlockData.COLORTYPE[usedColorTypeCount];

        for (int i = 0; i < usedColorTypeCount; i++)
        {
            colorsToCompare[i] = _levelBoardData.UsedColoredBlocks[i].Data.ColorType;
        }

        Dictionary<Vector2Int, ColoredBlock>.Enumerator pointer = _coloredBlocks.GetEnumerator();

        while (pointer.MoveNext())
        {
            ColoredBlock coloredBlock = pointer.Current.Value;
            BlockData.COLORTYPE currentColorType = coloredBlock.Data.ColorType;

            for(int i = 0;i < colorsToCompare.Length; i++)
            {
                if (colorsToCompare[i] == currentColorType)
                {
                    coloredBlocksByType[i].Add(coloredBlock);
                    colorCounts[i]++;
                    break;
                }
            }
        }

        foreach(int value in colorCounts)
        {
            if(value > mostUsedColor) mostUsedColor = value;
        }

        return mostUsedColor;
    }

    private bool TryMakeAdjacent(Vector2Int pivotPos, Vector2Int candidatePos, bool reversed = false) // Check if candidatePos can be made adjacent to pivotPos by swapping
    {
        foreach (Vector2Int dir in NeighborDirections)
        {
            Vector2Int adjacentPos = pivotPos + dir;

            if (_board[adjacentPos.x, adjacentPos.y] != null && _board[adjacentPos.x, adjacentPos.y].Data.BlockType != BlockData.BLOCKTYPE.Obstacle)
            {
                SwapBlocks(adjacentPos, candidatePos);
                return true;
            }
        }

        if (!reversed)
        {
            TryMakeAdjacent(candidatePos, pivotPos, true);
        }

        return false;
    }

    private void SwapBlocks(Vector2Int pos1, Vector2Int pos2)
    {
        Block temp = _board[pos1.x, pos1.y];
        _board[pos1.x, pos1.y] = _board[pos2.x, pos2.y];
        _board[pos2.x, pos2.y] = temp;

        // Swap their world positions for visual effect
        Vector3 tempPosition = _board[pos1.x, pos1.y].transform.position;
        _board[pos1.x, pos1.y].transform.position = _board[pos2.x, pos2.y].transform.position;
        _board[pos2.x, pos2.y].transform.position = tempPosition;
    }

    #endregion

    #region Blast Mechanic

    public void BlastGroup(int groupID)
    {
        Debug.Log("Blasting group " + groupID);
        BlockGroup blastGroup = _blockGroups[groupID]; // The list is sequencial because we reset the ID counter in FindGroups method.

        DamageAdjacentObstacles(FindAdjacentObstacles(blastGroup));

        foreach (Block block in blastGroup.Blocks)
        {
            Vector2Int loc = block.LocationOnBoard;
            _coloredBlocks.Remove(loc);
            _board[loc.x, loc.y] = null;
            Destroy(block.gameObject);
        }

        // Get the blast initiator block's location and create the powerup (if powerup will ever be implemented)
        ApplyGravity();
        FindColoredBlockGroups();
        AssignGroupIDs();
    }

    private Dictionary<Vector2Int, ObstacleBlock> FindAdjacentObstacles(BlockGroup blockGroup)
    {
        Dictionary<Vector2Int, ObstacleBlock> obstaclesToDamage = new Dictionary<Vector2Int, ObstacleBlock>();

        foreach (Block block in blockGroup.Blocks)
        {
            foreach (Vector2Int dir in NeighborDirections)
            {
                Vector2Int adjacentPos = block.LocationOnBoard + dir;

                if (IsWithinBounds(adjacentPos.x, adjacentPos.y) && _board[adjacentPos.x, adjacentPos.y] != null)
                {
                    Block adjacentBlock = _board[adjacentPos.x, adjacentPos.y];

                    if (adjacentBlock.Data.BlockType == BlockData.BLOCKTYPE.Obstacle)
                    {
                        if (!obstaclesToDamage.ContainsKey(adjacentPos))
                        {
                            obstaclesToDamage.Add(adjacentPos, adjacentBlock.GetComponent<ObstacleBlock>());
                        }
                    }
                }
            }
        }

        return obstaclesToDamage;
    }

    private void DamageAdjacentObstacles(Dictionary<Vector2Int, ObstacleBlock> obstaclesToDamage)
    {
        Dictionary<Vector2Int, ObstacleBlock>.Enumerator pointer = obstaclesToDamage.GetEnumerator();

        while (pointer.MoveNext()) {
            bool? isDestroyed = pointer.Current.Value?.TakeDamage();
            if (isDestroyed == true) {
                Vector2Int blockLoc = pointer.Current.Key;
                _obstacleBlocks.Remove(blockLoc);
                _board[blockLoc.x, blockLoc.y] = null;
            }
        }
    }

    #endregion

    #region Gravity Mechanic

    private void ApplyGravity()
    {
        int totalRows = _levelBoardData.MaxRowCount;
        int totalCols = _levelBoardData.MaxColumnCount;

        for (int x = 0; x < totalCols; x++)
        {
            for (int y = 0; y < totalRows; y++)
            {
                Block currentBlock = _board[x, y];

                if (currentBlock != null && currentBlock.Data.BlockType != BlockData.BLOCKTYPE.Obstacle)
                {
                    int lowestAvailableRow = FindLowestAvailableRow(x, y); // Find the lowest available position in this column

                    // Move the block to the lowest available position if it's different from the current position
                    if (lowestAvailableRow != y)
                    {
                        if(currentBlock.Data.BlockType == BlockData.BLOCKTYPE.Colored) // Update KeyValuePair for _coloredBlocks
                        {
                            Vector2Int blockLoc = new Vector2Int(x, lowestAvailableRow);

                            UpdateBlockLoc(blockLoc, x, y);
                        }
                        else if(currentBlock.Data.BlockType == BlockData.BLOCKTYPE.Power)
                        {
                            // Same as above (if implemented)
                        }

                        AnimateBlockFall(currentBlock, new Vector2(x, lowestAvailableRow));
                    }
                }
            }

            CreateMissingBlocksOnColumn(x);
        }
    }

    private int FindLowestAvailableRow(int col, int startRow) // Finds the lowest available row in the specified column, starting from the given row.
    {
        int lowestAvailableRow = startRow;

        // Start checking from the current row down to row 0
        for (int row = startRow - 1; row >= 0; row--)
        {
            if (_board[col, row] == null)
            {
                lowestAvailableRow = row; // Update the lowest available row to this empty cell
            }
            else if (_board[col, row].Data.BlockType == BlockData.BLOCKTYPE.Obstacle)
            {
                break; // Stop the search if an obstacle is encountered
            }
        }

        return lowestAvailableRow;
    }

    private void CreateMissingBlocksOnColumn(int column)
    {
        int totalRows = _levelBoardData.MaxRowCount;
        int spawnRow = totalRows + 4;
        int missingBlockCount = 0;

        // Count missing blocks starting from the topmost empty row
        for (int y = totalRows - 1; y >= 0; y--)
        {
            if (_board[column, y] == null)
            {
                missingBlockCount++;
            }
            else if (_board[column, y].Data.BlockType == BlockData.BLOCKTYPE.Obstacle)
            {
                break; // Stop counting if an obstacle is encountered
            }
        }

        for (int i = 0; i < missingBlockCount; i++)
        {
            spawnRow = spawnRow + i;
            int targetRow = totalRows - missingBlockCount + i;
            Vector2Int targetLoc = new Vector2Int(column, targetRow);

            // To Do: Bottom two lines will be changed after pool
            GameObject newObj = Instantiate(_levelBoardData.UsedColoredBlocks[Random.Range(0, _levelBoardData.UsedColoredBlocks.Length)].gameObject, new Vector3(column, spawnRow, 0), Quaternion.identity, transform);
            ColoredBlock newColoredBlock = newObj.GetComponent<ColoredBlock>();
            SetBlockLoc(newColoredBlock, targetLoc);

            AnimateBlockCreation(newColoredBlock);
            AnimateBlockFall(newColoredBlock, new Vector2(column, targetRow), 0.8f);
        }
    }

    private void UpdateBlockLoc(Vector2Int newLoc, int oldX = -1, int oldY = -1)
    {
        Vector2Int oldLoc = new Vector2Int(oldX, oldY);

        ColoredBlock coloredBlock = _coloredBlocks.GetValueOrDefault(oldLoc);
        _coloredBlocks.Remove(oldLoc);
        _board[oldX, oldY] = null;

        SetBlockLoc(coloredBlock, newLoc);
    }

    private void SetBlockLoc(ColoredBlock block, Vector2Int blockLoc)
    {
        block.LocationOnBoard = blockLoc;
        _board[blockLoc.x, blockLoc.y] = block;
        _coloredBlocks.Add(blockLoc, block);
    }

    private void SetBlockLoc(ObstacleBlock block, Vector2Int blockLoc)
    {
        block.LocationOnBoard = blockLoc;
        _board[blockLoc.x, blockLoc.y] = block;
        _obstacleBlocks.Add(blockLoc, block);
    }

    private void AnimateBlockCreation(Block block)
    {
        block.transform.localScale = Vector3.zero;
        block.transform.DOScale(Vector3.one, 0.3f);
    }

    private void AnimateBlockFall(Block block, Vector2 targetPosition, float duration = 0.4f, Ease ease = Ease.OutBounce)
    {
        block.ColliderOnOff(false);
        block.transform.DOMove(targetPosition, duration).SetEase(ease).OnComplete(() =>
        {
            block.ColliderOnOff(true);
        });
    }

    #endregion

    #region Helpers

    struct BlockGroup // Basically, a group is a list of blocks with a groupID
    {
        private static int _idCounter;
        private int _groupID;
        public int GroupID { get { return _groupID; } }

        private List<Block> _blocks;
        public List<Block> Blocks { get { return _blocks; } }

        public BlockGroup(List<Block> blocks)
        {
            _groupID = _idCounter;
            _idCounter++;
            _blocks = new List<Block>();
            _blocks.AddRange(blocks);
        }

        public static void ResetIDCounter() { _idCounter = 0; }

        public string PrintGroupInfo()
        {
            string groupDesc = "Group ID: " + _groupID + " block count: " + _blocks.Count;
            return groupDesc;
        }
    }

    private bool IsWithinBounds(int rowIndex, int colIndex)
    {
        return rowIndex >= 0 && rowIndex < _levelBoardData.MaxColumnCount && colIndex >= 0 && colIndex < _levelBoardData.MaxRowCount;
    }

    private readonly Vector2Int[] NeighborDirections =
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
    };

    private void DebugBlockGroups()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            Debug.Log(blockGroup.PrintGroupInfo());
        }
    }

    #endregion

}
