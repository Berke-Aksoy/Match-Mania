using DG.Tweening;
using UnityEngine;
using MatchMania.Blocks;

public static class BlockAnimator
{
    public static void ShakeBlock(Block block, float duration = 0.5f)
    {
        block.Moving(duration);
        block.transform.DOShakePosition(duration, strength: new Vector3(0.15f, 0.1f, 0), vibrato: 10, randomness: 10, fadeOut: true);
    }

    public static void AnimateBlockCreation(Block block)
    {
        block.transform.localScale = Vector3.zero;
        block.transform.DOScale(Vector3.one, 0.3f);
    }

    public static void AnimateBlockLocationChange(Block block, Vector2 targetPosition, float duration = 0.5f, Ease ease = Ease.OutBounce)
    {
        block.Moving(duration);
        block.transform.DOMove(targetPosition, duration).SetEase(ease);
    }
}
