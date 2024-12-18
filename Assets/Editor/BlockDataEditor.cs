using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockData))]
public class BlockDataEditor : Editor
{
    SerializedProperty blockTypeProperty;
    SerializedProperty colorTypeProperty;
    SerializedProperty iconSpritesProperty;
    SerializedProperty blastSoundProperty;

    private void OnEnable()
    {
        blockTypeProperty = serializedObject.FindProperty("BlockType");
        colorTypeProperty = serializedObject.FindProperty("ColorType");
        iconSpritesProperty = serializedObject.FindProperty("IconSprites");
        blastSoundProperty = serializedObject.FindProperty("BlastSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(blockTypeProperty);
        EditorGUILayout.PropertyField(colorTypeProperty);
        EditorGUILayout.PropertyField(blastSoundProperty);

        // Check if BlockType is 'Power' to conditionally draw IconSprites
        bool isPowerBlock = (BlockData.BLOCKTYPE)blockTypeProperty.enumValueIndex == BlockData.BLOCKTYPE.Power;

        // Disable the IconSprites field if BlockType is 'Power'
        EditorGUI.BeginDisabledGroup(isPowerBlock);
        EditorGUILayout.PropertyField(iconSpritesProperty, new GUIContent("Icon Sprites"), true);
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
