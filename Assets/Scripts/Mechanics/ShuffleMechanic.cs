using MatchMania.Blocks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BlockData;

public class ShuffleMechanic : MonoBehaviour
{
    public static event Action OnShuffleComplete;

    private void OnEnable()
    {
        GroupManager.OnEdgeCase += IntentionalShuffle;
    }

    private void OnDisable()
    {
        GroupManager.OnEdgeCase -= IntentionalShuffle;
    }

    private void IntentionalShuffle(int minBlastableBlockCount) // Returns true if the shuffle is succesful.
    {
        BoardManager boardManager = BoardManager.Instance;
        int tryCount = 0;
        List<List<ColoredBlock>> groupedBlocks = boardManager.GroupBlocksByColor();
        int uniqueColorCount = groupedBlocks.Count;

        if (uniqueColorCount < 1)
        {
            if (uniqueColorCount == 0) // In this case the map is full of obstacles that's why we need to wipe them out
            { 
                WipeOutObstacles();
                OnShuffleComplete?.Invoke();
                return;
            }

            CreateNewBlockAndReplaceOne(); // If there is one type of color and we couldn't make them group that means they are surrounded by obstacles
            OnShuffleComplete?.Invoke();
            return;
        }

        while (tryCount < uniqueColorCount)
        {
            int selGroupCount = groupedBlocks[tryCount].Count;

            if (selGroupCount < minBlastableBlockCount)
            {
                tryCount++;
                continue;
            }

            for (int pivotIndex = 0; pivotIndex < selGroupCount; pivotIndex++)
            {
                Vector2Int? availableLoc = null;
                availableLoc = boardManager.FindValidAdjacentLoc(groupedBlocks[tryCount][pivotIndex].Location); // Find a valid spot for pivot

                if (availableLoc.HasValue)
                {
                    int index = (pivotIndex + 1) % selGroupCount;
                    Vector2Int moverBlockLoc = groupedBlocks[tryCount][index].Location; // Move the next block in the group to adjacent location of pivot block
                    boardManager.SwapBlocks(availableLoc.Value, moverBlockLoc);
                    OnShuffleComplete?.Invoke();
                    return;
                }
            }

            tryCount++;
        }

        CreateNewBlockAndReplaceOne();
        OnShuffleComplete?.Invoke();
        return;
    }

    private bool CreateNewBlockAndReplaceOne()
    {
        BoardManager boardManager = BoardManager.Instance;
        Dictionary<Vector2Int, ColoredBlock> coloredBlocks = boardManager.ColoredBlocks;

        foreach (KeyValuePair<Vector2Int, ColoredBlock> kvp in coloredBlocks)
        {
            Vector2Int? validLoc = boardManager.FindValidAdjacentLoc(kvp.Key); // Step 1: Find a valid adjacent location to place the new block around the blockToMakeGroup
            if (validLoc.HasValue) // Step 2: Create a new block that is same with the current kvp.Value and replace the block on the validLoc
            {
                Block temp = coloredBlocks.GetValueOrDefault(validLoc.Value);
                boardManager.RemoveBlock(temp.Location, BLOCKTYPE.Colored);
                Destroy(temp.gameObject);

                GameObject newObj = Instantiate(kvp.Value.gameObject, new Vector3(validLoc.Value.x, validLoc.Value.y, 0), Quaternion.identity, transform);
                Block newBlock = newObj.GetComponent<Block>();
                boardManager.SetBlockLoc(newBlock, validLoc.Value);
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
        BoardManager boardManager = BoardManager.Instance;
        List<ObstacleBlock> obstacles = new List<ObstacleBlock>();
        obstacles.AddRange(boardManager.ObstacleBlocks.Values);
        int obstacleCount = obstacles.Count;

        for (int i = 0; i < obstacleCount; i++)
        {
            boardManager.RemoveBlock(obstacles[i].Location, BLOCKTYPE.Obstacle);
            obstacles[i].TakeDamage(true);
        }
    }

}
