using UnityEngine;

namespace MatchMania.Blocks
{
    public class ObstacleBlock : Block
    {
        [SerializeField][Range(-1, 10)] private int _health;

        public bool? TakeDamage(bool kill = false, int damage = 1)
        {
            if (kill) { _health = 0; }
            else { _health -= damage; }

            PlayBlastSound(0.4f);
            return CheckHealth();
        }

        private bool? CheckHealth()
        {
            if (_health <= 0)
            {
                Destroy(gameObject);
                return true;
            }
            else
            {
                BlockAnimator.ShakeBlock(this);
                _spriteRenderer.sprite = Data.IconSprites[_health - 1];
                return false;
            }
        }
    }
}
