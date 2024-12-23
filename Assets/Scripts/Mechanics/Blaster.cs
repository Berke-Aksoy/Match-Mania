using MatchMania.Blocks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BlockData;
using static GroupManager;

public static class Blaster
{
    public static event Action OnBlastComplete;

    public static bool BlastGroup(int groupID)
    {
        BoardManager boardManager = BoardManager.Instance;
        GroupManager groupManager = GroupManager.Instance;

        BlockGroup blastGroup = groupManager.BlockGroups[groupID]; // The list is sequencial because we reset the ID counter in FindGroups method.

        if (blastGroup.HasMovingPiece) // If the group known as having a moving piece, check current condition of the group
        {
            if (!IsBlastable(blastGroup)) { return false; }
        }

        DamageAdjacentObstacles(boardManager.FindAdjacentObstacles(blastGroup.Blocks));
        foreach (Block block in blastGroup.Blocks)
        {
            boardManager.DestroyBlock(block);
        }

        OnBlastComplete?.Invoke();

        return true;
    }

    private static bool IsBlastable(BlockGroup blastGroup) // Updates the status of the group variable HasMovingPiece
    {
        foreach (Block block in blastGroup.Blocks)
        {
            if (block.IsMoving) { return false; }
        }

        return true;
    }

    private static void DamageAdjacentObstacles(Dictionary<Vector2Int, ObstacleBlock> obstaclesToDamage)
    {
        BoardManager boardManager = BoardManager.Instance;
        Dictionary<Vector2Int, ObstacleBlock>.Enumerator pointer = obstaclesToDamage.GetEnumerator();

        while (pointer.MoveNext())
        {
            bool? isDestroyed = pointer.Current.Value?.TakeDamage();
            if (isDestroyed == true) { boardManager.RemoveBlock(pointer.Current.Key, BLOCKTYPE.Obstacle); }
        }
    }
}
