using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockData))]
public class BlockDataEditor : Editor
{
    SerializedProperty blockTypeProperty;
    SerializedProperty colorTypeProperty;
    SerializedProperty iconSpritesProperty;

    private void OnEnable()
    {
        blockTypeProperty = serializedObject.FindProperty("BlockType");
        colorTypeProperty = serializedObject.FindProperty("ColorType");
        iconSpritesProperty = serializedObject.FindProperty("IconSprites");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the BlockType field
        EditorGUILayout.PropertyField(blockTypeProperty);

        // Draw the ColorType field
        EditorGUILayout.PropertyField(colorTypeProperty);

        // Check if BlockType is 'colored' to conditionally draw IconSprites
        bool isColoredBlock = (BlockData.BLOCKTYPE)blockTypeProperty.enumValueIndex == BlockData.BLOCKTYPE.colored;

        // Disable the IconSprites field if BlockType is not 'colored'
        EditorGUI.BeginDisabledGroup(!isColoredBlock);
        EditorGUILayout.PropertyField(iconSpritesProperty, new GUIContent("Icon Sprites"), true);
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
