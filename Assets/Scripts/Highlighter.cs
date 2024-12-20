using DG.Tweening;
using UnityEngine;

public static class Highlighter
{
    public static Tween HighlightBlock(Block block)
    {
        return block.transform.DOScale(Vector3.one * 1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad); // Infinite loop with Yoyo (back and forth)
    }

    public static void StopHighlight(Block block, Tween tween)
    {
        tween?.Kill();
        if (block != null) { block.transform.localScale = Vector3.one; }
    }

}
