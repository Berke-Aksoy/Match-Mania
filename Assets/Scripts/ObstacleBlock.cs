using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ObstacleBlock : Block
{
    [SerializeField] [Range(-1, 10)] private int _health;

    public bool? TakeDamage()
    {
        _health--;
        PlayBlastSound(0.4f);
        return CheckHealth();
    }

    private bool? CheckHealth()
    {
        if(_health <= 0)
        {
            Destroy(gameObject);
            return true;
        }
        else
        {
            ShakeBlock();
            _spriteRenderer.sprite = Data.IconSprites[_health - 1];
            return false;
        }
    }
}
