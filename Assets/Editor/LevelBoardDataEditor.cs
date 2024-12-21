using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelBoardData))]
public class LevelBoardDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelBoardData data = (LevelBoardData)target;

        // Draw default inspector elements
        DrawDefaultInspector();

        // Calculate total chances
        int totalChance = data.coloredBlockChance + data.obstacleBlockChance + data.powerBlockChance;

        // Show a warning if the total is not 100
        if (totalChance != 100)
        {
            EditorGUILayout.HelpBox("The total of Instantiation Chances must be 100. Adjust the values below.", MessageType.Warning);

            // Automatically balance the values to ensure total equals 100
            if (totalChance > 100)
            {
                int excess = totalChance - 100;

                // Prioritize reducing the highest value
                if (data.coloredBlockChance >= excess)
                {
                    data.coloredBlockChance -= excess;
                }
                else if (data.obstacleBlockChance >= (excess - data.coloredBlockChance))
                {
                    excess -= data.coloredBlockChance;
                    data.coloredBlockChance = 0;
                    data.obstacleBlockChance -= excess;
                }
                else
                {
                    excess -= (data.coloredBlockChance + data.obstacleBlockChance);
                    data.coloredBlockChance = 0;
                    data.obstacleBlockChance = 0;
                    data.powerBlockChance -= excess;
                }
            }
            else if (totalChance < 100)
            {
                int deficit = 100 - totalChance;

                // Prioritize adding to the smallest value
                if (data.coloredBlockChance + deficit <= 100)
                {
                    data.coloredBlockChance += deficit;
                }
                else if (data.obstacleBlockChance + (deficit - data.coloredBlockChance) <= 100)
                {
                    deficit -= (100 - data.coloredBlockChance);
                    data.coloredBlockChance = 100;
                    data.obstacleBlockChance += deficit;
                }
                else
                {
                    deficit -= (100 - (data.coloredBlockChance + data.obstacleBlockChance));
                    data.coloredBlockChance = 100;
                    data.obstacleBlockChance = 100;
                    data.powerBlockChance += deficit;
                }
            }

            // Mark the object as dirty to save changes
            EditorUtility.SetDirty(data);
        }
    }
}
