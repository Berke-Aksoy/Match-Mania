
using MatchMania.Blocks;
using System.Collections.Generic;
using UnityEngine;
using static BlockData;

public static class Helpers
{
    public static readonly Vector2Int[] NeighborDirections =
    {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
    };

    public static bool IsWithinBounds(Vector2Int location)
    {
        int rowIndex = location.x, colIndex = location.y;
        int[] dimensions = BoardManager.Instance.GetBoardDimensions();

        return rowIndex >= 0 && rowIndex < dimensions[1] && colIndex >= 0 && colIndex < dimensions[0];
    }

    
}
