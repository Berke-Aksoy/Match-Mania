using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.UIElements;
using UnityEngine.UI;

public sealed class BoardCreator : MonoBehaviour
{
    [SerializeField] private LevelBoardData _levelBoardData; // To Do: Get this level from levelManager or somewhere else
    private Block[,] _board;
    private List<BlockGroup> _blockGroups;

    [Header("Thresholds")]
    [SerializeField] private int minBlastableBlockCount = 2;
    [SerializeField] private int firstThreshold = 4;  // Condition A
    [SerializeField] private int secondThreshold = 6; // Condition B
    [SerializeField] private int thirdThreshold = 8;  // Condition C

    private void Start()
    {
        CreateBoard(_levelBoardData.MaxRowCount, _levelBoardData.MaxColumnCount);
        FindGroups();
        SetGroupIDsAndChangeIcons();
    }

    private void CreateBoard(int rowCount, int colCount)
    {
        _board = new Block[colCount, rowCount];
        _blockGroups = new List<BlockGroup>();
        BlockPool poolSingleton = BlockPool.Singleton;

        for(int i = 0; i < colCount; i++)
        {
            for(int j = 0; j < rowCount; j++)
            {
                if (Random.Range(0, 100) < 90) // Create colored blocks
                {
                    GameObject newObj = Instantiate(_levelBoardData.UsedColoredBlocks[Random.Range(0, _levelBoardData.UsedColoredBlocks.Length)].gameObject, new Vector3(i, j, 0), Quaternion.identity, transform);
                    ColoredBlock newColoredBlock = newObj.GetComponent<ColoredBlock>();
                    _board[i, j] = newColoredBlock;
                    poolSingleton.StoreColoredBlockToPool(newColoredBlock);
                }
                else // Create obstacle blocks
                {
                    GameObject newObj = Instantiate(_levelBoardData.UsedObstacleBlocks[Random.Range(0, _levelBoardData.UsedObstacleBlocks.Length)].gameObject, new Vector3(i, j, 0), Quaternion.identity, transform);
                    ObstacleBlock newObstacleBlock  = newObj.GetComponent<ObstacleBlock>();
                    _board[i, j] = newObstacleBlock;
                }
            }
        }

        poolSingleton.SortColoredBlockPool();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            ReCreate();
        }
    }

    private void ReCreate()
    {
        foreach(Block block in _board)
        {
            Destroy(block);
        }

        CreateBoard(_levelBoardData.MaxRowCount, _levelBoardData.MaxColumnCount);
    }

    private void FindGroups()
    {
        List<Vector2Int> candidateBlockList = new List<Vector2Int>();
        List<BlockGroup> blockGroups = new List<BlockGroup>();
        BlockGroup.ResetIDCounter();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        int totalRows = _levelBoardData.MaxRowCount;
        int totalCols = _levelBoardData.MaxColumnCount;

        bool[,] visited = new bool[totalCols, totalRows];
        BlockData.COLORTYPE targetColorType;

        for (int i = 0; i < totalCols; i++)
        {
            for (int j = 0; j < totalRows; j++)
            {
                if (visited[i, j]) { continue; }
                if (_board[i, j] == null) { visited[i, j] = true; continue; }

                targetColorType = _board[i, j].Data.ColorType;
                if(targetColorType == BlockData.COLORTYPE.None) { visited[i, j] = true; continue; }

                queue.Enqueue(new Vector2Int(i, j));
                visited[i, j] = true;

                while (queue.Count > 0)
                {
                    Vector2Int currentBlock = queue.Dequeue();
                    candidateBlockList.Add(currentBlock);

                    foreach (Vector2Int direction in NeighborDirections)
                    {
                        int newRowIndex = currentBlock.x + direction.x;
                        int newColIndex = currentBlock.y + direction.y;

                        if(IsWithinBounds(newRowIndex, newColIndex) && !visited[newRowIndex, newColIndex] && _board[newRowIndex, newColIndex] != null && _board[newRowIndex, newColIndex].Data.ColorType == targetColorType)
                        {
                            queue.Enqueue(new Vector2Int(newRowIndex, newColIndex));
                            visited[newRowIndex, newColIndex] = true;
                        }
                    }
                }

                int blockCount = candidateBlockList.Count;

                if (blockCount >= minBlastableBlockCount)
                {
                    BlockGroup newGroup = new BlockGroup(candidateBlockList, targetColorType);
                    blockGroups.Add(newGroup);
                }
                else
                {
                    foreach (Vector2Int blockLoc in candidateBlockList) // Change the sprites and groupIDs of blocks that are not on a group to default sprite
                    {
                        _board[blockLoc.x, blockLoc.y].GetComponent<ColoredBlock>().GroupID = -1;
                        ChangeSprite(_board[blockLoc.x, blockLoc.y], blockCount);
                    }
                }

                candidateBlockList.Clear(); // Reuse the group for the next group
            }
        }

        _blockGroups = blockGroups;
    }

    private void SetGroupIDsAndChangeIcons()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            int groupSize = blockGroup.Locations.Count;

            for (int i = 0; i < groupSize; i++)
            {
                Vector2Int blockLoc = blockGroup.Locations[i];

                ColoredBlock coloredBlock = _board[blockLoc.x, blockLoc.y].GetComponent<ColoredBlock>();

                if (coloredBlock != null)
                {
                    coloredBlock.GroupID = blockGroup.GroupID;
                    ChangeSprite(coloredBlock, groupSize);
                }
            }
        }
    }

    private void ChangeSprite(Block block, int groupSize)
    {
        int spriteIndex = 3; // Default sprite index

        if (groupSize >= thirdThreshold)
        {
            spriteIndex = 2; // Condition C sprite index
        }
        else if (groupSize >= secondThreshold)
        {
            spriteIndex = 1; // Condition B sprite index
        }
        else if (groupSize >= firstThreshold)
        {
            spriteIndex = 0; // Condition A sprite index
        }

        block.GetComponent<SpriteRenderer>().sprite = block.Data.IconSprites[spriteIndex] as Sprite; // To Do: Blocks can have sprite variable
    }

    public void BlastGroup(int groupID)
    {
        Debug.Log("Blast " + groupID);
        BlockGroup g = _blockGroups[groupID]; // The list is sequencial because we reset the ID counter in FindGroups method.

        foreach (Vector2Int loc in g.Locations)
        {
            Destroy(_board[loc.x, loc.y].gameObject);
            _board[loc.x, loc.y] = null;
        }

        // Get the blast initiator block's location and create the powerup (if powerup will ever be implemented)
        ApplyGravity();
        FindGroups();
        SetGroupIDsAndChangeIcons();
    }

    #region Gravity Mechanic

    private void ApplyGravity()
    {
        int totalRows = _levelBoardData.MaxRowCount;
        int totalCols = _levelBoardData.MaxColumnCount;

        for (int x = 0; x < totalCols; x++)
        {
            for (int y = totalRows - 1; y >= 0; y--)
            {
                Block currentBlock = _board[x, y];

                if (currentBlock != null && currentBlock.Data.BlockType != BlockData.BLOCKTYPE.Obstacle)
                {
                    int lowestAvailableRow = FindLowestAvailableRow(x, y); // Find the lowest available position in this column

                    // Move the block to the lowest available position if it's different from the current position
                    if (lowestAvailableRow != y)
                    {
                        _board[x, lowestAvailableRow] = currentBlock;
                        _board[x, y] = null;

                        currentBlock.transform.position = new Vector3(x, lowestAvailableRow, 0); // Update the block's visual position
                    }
                }
            }
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
                // Update the lowest available row to this empty cell
                lowestAvailableRow = row;
            }
            else if (_board[col, row].Data.BlockType == BlockData.BLOCKTYPE.Obstacle)
            {
                // Stop the search if an obstacle is encountered
                break;
            }
        }

        return lowestAvailableRow;
    }

    #endregion

    #region Helpers

    struct BlockGroup // Basically, a group is a set of positions which corresponds to blocks with color type
    {
        private static int _idCount;
        private int _groupID;
        public int GroupID { get { return _groupID; } }

        public List<Vector2Int> Locations;
        public BlockData.COLORTYPE ColorType;

        public BlockGroup(List<Vector2Int> locations, BlockData.COLORTYPE colorType)
        {
            _groupID = _idCount;
            _idCount++;
            Locations = new List<Vector2Int>();
            Locations.AddRange(locations);
            ColorType = colorType;
        }

        public static void ResetIDCounter() { _idCount = 0; }

        public string PrintGroupInfo()
        {
            string groupDesc = "Group ID: " + _groupID + " " + ColorType.ToString() + " location list count: " + Locations.Count;
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
