using DG.Tweening;
using UnityEngine;

public static class Highlighter
{
    public static Tween HighlightBlock(Transform transform)
    {
        return transform.DOScale(Vector3.one * 1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad); // Infinite loop with Yoyo (back and forth)
    }

    public static void StopHighlight(Transform transform, Tween tween)
    {
        tween?.Kill();
        if (transform != null) { transform.localScale = Vector3.one; }
    }

}
