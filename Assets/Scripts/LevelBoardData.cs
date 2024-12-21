using UnityEngine;

[CreateAssetMenu(fileName = "levelData", menuName = "Level Board Data")]
public sealed class LevelBoardData : ScriptableObject
{
    [Range (2, 10)]
    public int MaxRowCount, MaxColumnCount;
    [Range(0, 100)]
    public int obstacleBlockChance, coloredBlockChance, powerBlockChance;
    public ColoredBlock[] UsedColoredBlocks;
    public ObstacleBlock[] UsedObstacleBlocks;
}
