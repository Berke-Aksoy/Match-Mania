using UnityEngine;
using MatchMania.Blocks;

[CreateAssetMenu(fileName = "levelData", menuName = "Level Board Data")]
public sealed class LevelBoardData : ScriptableObject
{
    [Range (2, 10)]
    public int MaxRowCount, MaxColumnCount;
    [Range(0, 100)]
    public int obstacleBlockChance, coloredBlockChance, powerBlockChance;
    public ColoredBlock[] UsedColoredBlocks;
    public ObstacleBlock[] UsedObstacleBlocks;

    public void InitializeDefaults()
    {
        MaxRowCount = 10;
        MaxColumnCount = 10;
        obstacleBlockChance = 15;
        coloredBlockChance = 85;
        powerBlockChance = 0;
        UsedColoredBlocks = Resources.LoadAll<ColoredBlock>("Blocks/Color Blocks");
        UsedObstacleBlocks = Resources.LoadAll<ObstacleBlock>("Blocks/Obstacle Blocks");
    }
}
