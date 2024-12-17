using UnityEngine;

[CreateAssetMenu(fileName = "BlockTypeData", menuName = "Block Type Data")]
public class BlockData : ScriptableObject
{
    // In future blocktype with color can be used for different features. For example an obstacle with a color type blue can allow blue blocks to pass.
    public enum BLOCKTYPE
    {
        colored, Obstacle, Power
    }

    public enum COLORTYPE
    {
        Blue, Green, Pink, Purple, Red, Yellow, None
    }


    public BLOCKTYPE BlockType;
    public COLORTYPE ColorType = COLORTYPE.None;

    [Tooltip("The order is important. 0:A, 1:B, 2:C, 3:Default")] public Sprite[] IconSprites;
}
