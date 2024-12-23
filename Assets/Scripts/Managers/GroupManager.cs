using MatchMania.Blocks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BlockData;

public class GroupManager : BaseSingleton<GroupManager>
{
    [SerializeField] private int _minBlastableBlockCount = 2;
    private List<BlockGroup> _blockGroups = new List<BlockGroup>();
    public List<BlockGroup> BlockGroups { get => _blockGroups; }

    public static event Action<int> OnEdgeCase;

    private void OnEnable()
    {
        GravityMechanic.OnApplyGravity += HandleEventOrder;
        BoardManager.OnBoardComplete += HandleEventOrder;
    }

    private void OnDisable()
    {
        BoardManager.OnBoardComplete -= HandleEventOrder;
        GravityMechanic.OnApplyGravity -= HandleEventOrder;
    }

    private void HandleEventOrder()
    {
        FindColoredBlockGroups();
        AssignGroupIDs();
    }

    public void FindColoredBlockGroups()
    {
        BoardManager boardManager = BoardManager.Instance;
        Dictionary<Vector2Int, ColoredBlock> coloredBlocks = boardManager.ColoredBlocks;
        int[] dimensions = boardManager.GetBoardDimensions();

        List<Block> candidateBlockList = new List<Block>();
        List<BlockGroup> blockGroups = new List<BlockGroup>();
        BlockGroup.ResetIDCounter();

        Queue<Block> queue = new Queue<Block>();

        bool hasMovingPiece = false;

        bool[,] visited = new bool[dimensions[1], dimensions[0]];
        COLORTYPE targetColorType;

        Dictionary<Vector2Int, ColoredBlock>.Enumerator pointer = coloredBlocks.GetEnumerator();

        while (pointer.MoveNext())
        {
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

                foreach (Vector2Int direction in Helpers.NeighborDirections)
                {
                    Vector2Int adjacentLoc = currentBlock.Location + direction;
                    Block adjacentBlock = coloredBlocks.GetValueOrDefault(adjacentLoc);

                    if (Helpers.IsWithinBounds(adjacentLoc) && !visited[adjacentLoc.x, adjacentLoc.y] && adjacentBlock != null && adjacentBlock.Data.ColorType == targetColorType)
                    {
                        queue.Enqueue(adjacentBlock);
                        visited[adjacentLoc.x, adjacentLoc.y] = true;
                    }
                }
            }

            int blockCount = candidateBlockList.Count;

            if (blockCount >= _minBlastableBlockCount)
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

    public void AssignGroupIDs()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            foreach (Block block in blockGroup.Blocks)
            {
                block.SetGroupID(blockGroup.GroupID, blockGroup.Blocks.Count);
            }
        }
    }

    private System.Collections.IEnumerator WaitFallingBlocks()
    {
        float waitTime = Mathf.Max(1f, BlockAnimator.OneStepTime * (BoardManager.Instance.GetBoardDimensions()[1]));
        yield return new WaitForSeconds(waitTime);
        OnEdgeCase?.Invoke(_minBlastableBlockCount);
    }

    private void DebugBlockGroups()
    {
        foreach (BlockGroup blockGroup in _blockGroups)
        {
            Debug.Log(blockGroup.PrintGroupInfo());
        }
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

}
