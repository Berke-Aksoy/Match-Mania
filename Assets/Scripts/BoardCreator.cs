using System.Collections.Generic;
using UnityEngine;
using MatchMania.Blocks;
using static BlockData;
using UnityEngine.InputSystem.Android;

public sealed class BoardCreator : BaseSingleton<BoardCreator>
{
    private static BoardCreator _instance;
    public static BoardCreator Singleton { get => _instance; }

    private LevelBoardData _levelBoardData;
    [Header("Threshold")]
    [SerializeField] private int minBlastableBlockCount = 2;

    private Block[,] _board;
    private List<BlockGroup> _blockGroups = new List<BlockGroup>();
    public List<BlockGroup> BlockGroups { get => _blockGroups; }

    private Dictionary<Vector2Int, ColoredBlock> _coloredBlocks = new Dictionary<Vector2Int, ColoredBlock>();
    private Dictionary<Vector2Int, ObstacleBlock> _obstacleBlocks = new Dictionary<Vector2Int, ObstacleBlock>();
    private float[] missingTimers;

    private void Start()
    {
        _levelBoardData = LevelManager.Instance.GetLevelData();
        missingTimers = new float[_levelBoardData.MaxColumnCount];
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
                if (Random.Range(0, 100) < _levelBoardData.coloredBlockChance) { CreateBlock(i, j, BLOCKTYPE.Colored); }
                else { CreateBlock(i, j, BLOCKTYPE.Obstacle); }
            }
        }

        poolSingleton.SortColoredBlockPool();
    }

    private Block CreateBlock(int x, int y, BLOCKTYPE blockType, bool setLoc = true)
    {
        Vector2Int loc = new Vector2Int(x, y);
        GameObject newObj;
        Block newBlock;
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

        newObj = Instantiate(blocks[Random.Range(0, blocks.Length)].gameObject, new Vector3(x, y, 0), Quaternion.identity, transform);
        newBlock = newObj.GetComponent<Block>();
        if (setLoc) { SetBlockLoc(newBlock, loc); }
        return newBlock;
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
        bool hasMovingPiece = false;

        bool[,] visited = new bool[totalCols, totalRows];
        COLORTYPE targetColorType;

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
                if (!hasMovingPiece) { hasMovingPiece = currentBlock.IsMoving; }

                foreach (Vector2Int direction in NeighborDirections)
                {
                    Vector2Int adjacentLoc = currentBlock.Location + direction;

                    if (IsWithinBounds(adjacentLoc) && !visited[adjacentLoc.x, adjacentLoc.y] && _board[adjacentLoc.x, adjacentLoc.y] != null && _board[adjacentLoc.x, adjacentLoc.y].Data.ColorType == targetColorType)
                    {
                        queue.Enqueue(_board[adjacentLoc.x, adjacentLoc.y]);
                        visited[adjacentLoc.x, adjacentLoc.y] = true;
                    }
                }
            }

            int blockCount = candidateBlockList.Count;

            if (blockCount >= minBlastableBlockCount)
            {
                BlockGroup newGroup = new BlockGroup(candidateBlockList, hasMovingPiece);
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
        
        if (blockGroups.Count == 0) // && PowerBlockCount = 0 // Check whether there are no groups and enough colored blocks to blast
        {
            StartCoroutine(WaitFallingBlocks()); // Handle edge ases
        }
    }

    private System.Collections.IEnumerator WaitFallingBlocks()
    {
        float waitTime = BlockAnimator.OneStepTime * (_levelBoardData.MaxColumnCount + 1);
        yield return new WaitForSeconds(waitTime); // To Do: test this more
        IntentionalShuffle();
        FindColoredBlockGroups();
        AssignGroupIDs();
    }

    private void AssignGroupIDs()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            foreach(Block block in blockGroup.Blocks)
            {
                block.SetGroupID(blockGroup.GroupID, blockGroup.Blocks.Count);
            }
        }
    }

    #endregion

    #region Shuffle Mechanic
    
    private bool IntentionalShuffle() // Returns true if the shuffle is succesful.
    {
        int tryCount = 0;
        List<List<ColoredBlock>> groupedBlocks = GroupBlocksByColor();
        int uniqueColorCount = groupedBlocks.Count;

        if(uniqueColorCount < 1)
        {   
            if(uniqueColorCount == 0) { WipeOutObstacles(); return false; } // In this case the map is full of obstacles that's why we need to wipe them out
            return CreateNewBlockAndReplaceOne(groupedBlocks, uniqueColorCount); // If there is one type of color and we couldn't make them group that means they are surrounded by obstacles
        }

        while (tryCount < uniqueColorCount)
        {
            int selGroupCount = groupedBlocks[tryCount].Count;

            if (selGroupCount < minBlastableBlockCount) {
                tryCount++;
                continue;
            }

            for (int pivotIndex = 0; pivotIndex < selGroupCount; pivotIndex++)
            {
                Vector2Int? availableLoc = null;
                availableLoc = FindValidAdjacentLoc(groupedBlocks[tryCount][pivotIndex].Location); // Find a valid spot for pivot

                if (availableLoc.HasValue)
                {
                    int index = (pivotIndex + 1) % selGroupCount;
                    Vector2Int moverBlockLoc = groupedBlocks[tryCount][index].Location; // Move the next block in the group to adjacent location of pivot block
                    SwapBlocks(availableLoc.Value, moverBlockLoc);
                    return true;
                }
            }

            tryCount++;
        }

        return CreateNewBlockAndReplaceOne(groupedBlocks, uniqueColorCount);
    }

    private bool CreateNewBlockAndReplaceOne(List<List<ColoredBlock>> groupedBlocks, int uniqueColorCount)
    {
        foreach (KeyValuePair<Vector2Int, ColoredBlock> kvp in _coloredBlocks)
        {
            Vector2Int? validLoc = FindValidAdjacentLoc(kvp.Key); // Step 1: Find a valid adjacent location to place the new block around the blockToMakeGroup
            if (validLoc.HasValue) // Step 2: Create a new block that is same with the current kvp.Value and replace the block on the validLoc
            {
                Block temp = _coloredBlocks.GetValueOrDefault(validLoc.Value);
                RemoveBlock(temp.Location, BLOCKTYPE.Colored);
                Destroy(temp.gameObject);

                GameObject newObj = Instantiate(kvp.Value.gameObject, new Vector3(validLoc.Value.x, validLoc.Value.y, 0), Quaternion.identity, transform);
                Block newBlock = newObj.GetComponent<Block>();
                SetBlockLoc(newBlock, validLoc.Value);
                BlockAnimator.ShakeBlock(newBlock);

                return true;
            }
        }

        // Step 3: If previous steps fail, handle edge cases (e.g., deadlock due to obstacles)
        WipeOutObstacles();
        return false;
    }

    private void WipeOutObstacles()
    {
        int obstacleCount = _obstacleBlocks.Count;
        List<ObstacleBlock> obstacles = new List<ObstacleBlock>();
        obstacles.AddRange(_obstacleBlocks.Values);

        for(int i = 0; i < obstacleCount; i++)
        {
            RemoveBlock(obstacles[i].Location, BLOCKTYPE.Obstacle);  
            obstacles[i].TakeDamage(true);
        }

        ApplyGravity();
    }

    private List<List<ColoredBlock>> GroupBlocksByColor()
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

    private Vector2Int? FindValidAdjacentLoc(Vector2Int locToCheck)
    {
        List<Vector2Int> validLocs = new List<Vector2Int>();

        foreach (Vector2Int dir in NeighborDirections)
        {
            Vector2Int adjacentLoc = locToCheck + dir;

            if (IsWithinBounds(adjacentLoc)) // Obstacle blocks are not swappable
            {
                Block block = _board[adjacentLoc.x, adjacentLoc.y];
                if (block == null) // Check this if block not to place the block in mid air
                {
                    Vector2Int underAdjacentLoc = adjacentLoc + NeighborDirections[1]; // Look under adjacent location
                    if (IsWithinBounds(underAdjacentLoc) && _board[underAdjacentLoc.x, underAdjacentLoc.y] == null) { continue; } // If adjacent location is in mid air, it is not a valid location
                }
                else if(block.Data.BlockType == BLOCKTYPE.Obstacle)
                {
                    continue;
                }

                validLocs.Add(adjacentLoc);
            }
        }

        if (validLocs.Count == 0) { return null; }
        return validLocs[Random.Range(0, validLocs.Count)];
    }

    private void SwapBlocks(Vector2Int pos1, Vector2Int pos2)
    {
        Block adjacent = _board[pos1.x, pos1.y];
        Block mover = _board[pos2.x, pos2.y];

        SetBlockLoc(adjacent, pos2, false, true);
        SetBlockLoc(mover, pos1, false, true);

        BlockAnimator.AnimateBlockLocationChange(adjacent, pos1);
        BlockAnimator.AnimateBlockLocationChange(mover, pos2);
    }

    #endregion

    #region Blast Mechanic

    public bool BlastGroup(int groupID)
    {
        BlockGroup blastGroup = _blockGroups[groupID]; // The list is sequencial because we reset the ID counter in FindGroups method.
        if (blastGroup.HasMovingPiece) // If the group known as having a moving piece, check current condition of the group
        {
            if (!IsBlastable(blastGroup)) { return false; }
        }

        DamageAdjacentObstacles(FindAdjacentObstacles(blastGroup));
        foreach (Block block in blastGroup.Blocks)
        {
            DestroyBlock(block);
        }
        // Get the blast initiator block's location and create the powerup (if powerup will ever be implemented)
        ApplyGravity();
        FindColoredBlockGroups();
        AssignGroupIDs();

        return true;
    }

    private void DestroyBlock(Block blockToDestroy)
    {
        RemoveBlock(blockToDestroy.Location, blockToDestroy.Data.BlockType);
        Destroy(blockToDestroy.gameObject);
    }

    private bool IsBlastable(BlockGroup blastGroup) // Updates the status of the group variable HasMovingPiece
    {
        foreach (Block block in blastGroup.Blocks)
        {
            if(block.IsMoving) { return false; }
        }

        return true;
    }

    private Dictionary<Vector2Int, ObstacleBlock> FindAdjacentObstacles(BlockGroup blockGroup)
    {
        Dictionary<Vector2Int, ObstacleBlock> obstaclesToDamage = new Dictionary<Vector2Int, ObstacleBlock>();

        foreach (Block block in blockGroup.Blocks)
        {
            foreach (Vector2Int dir in NeighborDirections)
            {
                Vector2Int adjacentLoc = block.Location + dir;

                if (IsWithinBounds(adjacentLoc) && _board[adjacentLoc.x, adjacentLoc.y] != null)
                {
                    Block adjacentBlock = _board[adjacentLoc.x, adjacentLoc.y];

                    if (adjacentBlock.Data.BlockType == BLOCKTYPE.Obstacle)
                    {
                        if (!obstaclesToDamage.ContainsKey(adjacentLoc))
                        {
                            obstaclesToDamage.Add(adjacentLoc, (ObstacleBlock)adjacentBlock);
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
            if (isDestroyed == true) { RemoveBlock(pointer.Current.Key, BLOCKTYPE.Obstacle); }
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

                if (currentBlock != null && currentBlock.Data.BlockType != BLOCKTYPE.Obstacle) // To Do: _canMove bool variable must be added to Block script
                {
                    int lowestAvailableRow = FindLowestAvailableRow(x, y); // Find the lowest available position in this column

                    // Move the block to the lowest available position if it's different from the current position
                    if (lowestAvailableRow != y)
                    {
                        Vector2Int newLoc = new Vector2Int(x, lowestAvailableRow);

                        SetBlockLoc(currentBlock, newLoc, true);
                        BlockAnimator.AnimateBlockLocationChange(currentBlock, new Vector2Int(x, y));
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
            else if (_board[col, row].Data.BlockType == BLOCKTYPE.Obstacle)
            {
                break; // Stop the search if an obstacle is encountered
            }
        }

        return lowestAvailableRow;
    }

    private void CreateMissingBlocksOnColumn(int column)
    {
        int totalRows = _levelBoardData.MaxRowCount;
        int extraSteps = 2;
        int spawnOffset = totalRows + extraSteps;
        int missingBlockCount = 0;

        // Count missing blocks starting from the topmost empty row
        for (int y = totalRows - 1; y >= 0; y--)
        {
            if (_board[column, y] == null)
            {
                missingBlockCount++;
            }
            else if (_board[column, y].Data.BlockType == BLOCKTYPE.Obstacle)
            {
                break; // Stop counting if an obstacle is encountered
            }
        }

        if (missingTimers[column] > 0) { spawnOffset += Mathf.RoundToInt(missingTimers[column] / BlockAnimator.OneStepTime); }

        for (int i = 0; i < missingBlockCount; i++)
        {
            int spawnRow = spawnOffset + i;
            int targetRow = totalRows - missingBlockCount + i;
            Vector2Int targetLoc = new Vector2Int(column, targetRow);

            // To Do: Bottom two lines will be changed after pool
            Block newBlock = CreateBlock(column, spawnRow, BLOCKTYPE.Colored, false);
            SetBlockLoc(newBlock, targetLoc);

            BlockAnimator.AnimateBlockCreation(newBlock);
            BlockAnimator.AnimateBlockLocationChange(newBlock, new Vector2Int(column, spawnRow));
        }

        missingTimers[column] = BlockAnimator.OneStepTime * missingBlockCount;
    }

    private void FixedUpdate()
    {
        for(int i=0; i<missingTimers.Length; i++)
        {
            float timer = missingTimers[i];
            if (timer > 0)
                missingTimers[i] = timer - Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Helpers

    private void SetBlockLoc(Block block, Vector2Int newLoc, bool removeOld = false, bool doesSwap = false) // Does fall variable is just used for blocks that are already in the board and will fall under. Do not set it to true in case of creation of missing blocks
    {
        if (!IsWithinBounds(newLoc)) { return; }

        BLOCKTYPE blockType = block.Data.BlockType;
        Vector2Int oldLoc = block.Location;

        switch (blockType)
        {
            case BLOCKTYPE.Colored:
                if (removeOld) { _coloredBlocks.Remove(oldLoc); }
                else if (doesSwap) { _coloredBlocks.Remove(newLoc); }
                _coloredBlocks.Add(newLoc, (ColoredBlock)block); break;
            case BLOCKTYPE.Obstacle:
                if(removeOld) { _obstacleBlocks.Remove(oldLoc); }
                else if(doesSwap) { _obstacleBlocks.Remove(newLoc); }
                _obstacleBlocks.Add(newLoc, (ObstacleBlock)block); break;
        }

        if (removeOld) { _board[oldLoc.x, oldLoc.y] = null; }
        block.Location = newLoc;
        _board[newLoc.x, newLoc.y] = block;
    }

    private void RemoveBlock(Vector2Int loc, BLOCKTYPE blockType)
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

    

    public struct BlockGroup // Basically, a group is a list of blocks with a groupID
    {
        private static int _idCounter;
        private int _groupID;
        public int GroupID { get { return _groupID; } }
        private bool _hasMovingPiece;
        public bool HasMovingPiece { get { return _hasMovingPiece; } }

        private List<Block> _blocks;
        public List<Block> Blocks { get { return _blocks; } }

        public BlockGroup(List<Block> blocks, bool hasMovingPiece)
        {
            _groupID = _idCounter;
            _idCounter++;
            _hasMovingPiece = hasMovingPiece;
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

    private bool IsWithinBounds(Vector2Int location)
    {
        int rowIndex = location.x, colIndex = location.y;
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
