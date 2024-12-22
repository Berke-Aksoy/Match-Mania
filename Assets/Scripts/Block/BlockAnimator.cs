using DG.Tweening;
using UnityEngine;

namespace MatchMania.Blocks
{
    public static class BlockAnimator
    {
        [SerializeField] private static float _oneStepTime = 0.2f; // The duration to take one step
        public static float OneStepTime { get => _oneStepTime; }
        public static void ShakeBlock(Block block, float duration = 0.5f)
        {
            block.Moving(duration);
            block.Tween = block.transform.DOShakePosition(duration, strength: new Vector3(0.15f, 0.1f, 0), vibrato: 10, randomness: 10, fadeOut: true);
        }

        public static void AnimateBlockCreation(Block block)
        {
            block.transform.localScale = Vector3.zero;
            block.Tween = block.transform.DOScale(Vector3.one, 0.2f);
        }

        public static void AnimateBlockLocationChange(Block block, Vector2Int startPosition, Ease ease = Ease.InBack)
        {
            int stepCount = Mathf.Abs(startPosition.y - block.Location.y);
            float duration = stepCount * _oneStepTime;

            if (block.IsMoving) { duration += GetRemainingTime(block.Tween); }

            block.Moving(duration);
            block.Tween = block.transform.DOMove(new Vector3(block.Location.x, block.Location.y, 0), duration).SetEase(ease);
        }

        private static float GetRemainingTime(Tween moveTween)
        {
            float remainingTime = moveTween.Duration(false) - moveTween.Elapsed(false);
            moveTween.Kill();

            return remainingTime;
        }
    }
}
