using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObstacleBlock))]
public class ObstacleBlockEditor : Editor
{
    SerializedProperty healthProperty;
    SerializedProperty lowerHealthVersionProperty;
    SerializedProperty dataProperty;

    private void OnEnable()
    {
        healthProperty = serializedObject.FindProperty("_health");
        lowerHealthVersionProperty = serializedObject.FindProperty("_lowerHealthVersion");
        dataProperty = serializedObject.FindProperty("_data");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the health field
        EditorGUILayout.PropertyField(healthProperty);
        EditorGUILayout.PropertyField(dataProperty);

        // Disable lowerHealthVersion if health <= 1
        bool isHealthGreaterThanOne = healthProperty.intValue > 1;

        EditorGUI.BeginDisabledGroup(!isHealthGreaterThanOne);
        EditorGUILayout.PropertyField(lowerHealthVersionProperty, new GUIContent("Lower Health Version"));
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
