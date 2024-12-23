using MatchMania.Blocks;
using System;
using UnityEngine;
using static BlockData;

public class GravityMechanic : MonoBehaviour
{
    private float[] missingTimers;

    public static event Action OnApplyGravity;

    private void OnEnable()
    {
        Blaster.OnBlastComplete += ApplyGravity;
        ShuffleMechanic.OnShuffleComplete += ApplyGravity;
    }

    private void OnDisable()
    {
        ShuffleMechanic.OnShuffleComplete -= ApplyGravity;
        Blaster.OnBlastComplete -= ApplyGravity;
    }

    private void Start()
    {
        missingTimers = new float[BoardManager.Instance.GetBoardDimensions()[1]];
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < missingTimers.Length; i++) // 50Hz is more than enough to update timers that are used for missing block creation
        {
            float timer = missingTimers[i];
            if (timer > 0)
                missingTimers[i] = timer - Time.fixedDeltaTime;
        }
    }

    public void ApplyGravity()
    {
        BoardManager boardManager = BoardManager.Instance;
        int[] dimensions = boardManager.GetBoardDimensions();
        Block[,] board = boardManager.Board;

        for (int x = 0; x < dimensions[1]; x++)
        {
            for (int y = 0; y < dimensions[0]; y++)
            {
                Block currentBlock = board[x, y];

                if (currentBlock != null && currentBlock.Data.UseGravity)
                {
                    int lowestAvailableRow = boardManager.FindLowestAvailableRow(x, y); // Find the lowest available position in this column

                    // Move the block to the lowest available position if it's different from the current position
                    if (lowestAvailableRow != y)
                    {
                        Vector2Int newLoc = new Vector2Int(x, lowestAvailableRow);

                        BoardManager.Instance.SetBlockLoc(currentBlock, newLoc, true);
                        BlockAnimator.AnimateBlockLocationChange(currentBlock, new Vector2Int(x, y));
                    }
                }
            }

            CreateMissingBlocksOnColumn(x);
        }

        OnApplyGravity?.Invoke();
    }

    private void CreateMissingBlocksOnColumn(int column)
    {
        BoardManager boardManager = BoardManager.Instance;
        Block[,] board = boardManager.Board;

        int totalRows = boardManager.GetBoardDimensions()[0];
        int extraSteps = 2;
        int spawnOffset = totalRows + extraSteps;
        int missingBlockCount = 0;

        // Count missing blocks starting from the topmost empty row
        for (int y = totalRows - 1; y >= 0; y--)
        {
            if (board[column, y] == null)
            {
                missingBlockCount++;
            }
            else if (board[column, y].Data.BlockType == BLOCKTYPE.Obstacle)
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
            Block newBlock = boardManager.CreateBlock(column, spawnRow, BLOCKTYPE.Colored, false);
            boardManager.SetBlockLoc(newBlock, targetLoc);

            BlockAnimator.AnimateBlockCreation(newBlock);
            BlockAnimator.AnimateBlockLocationChange(newBlock, new Vector2Int(column, spawnRow));
        }

        missingTimers[column] = BlockAnimator.OneStepTime * missingBlockCount;
    }
}
