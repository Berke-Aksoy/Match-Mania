using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ObstacleBlock : Block
{
    [SerializeField] [Range(-1, 10)] private int _health;

    public bool? TakeDamage()
    {
        _health--;
        PlayBlastSound();
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
            ShakeBox();
            _spriteRenderer.sprite = Data.IconSprites[_health - 1];
            return false;
        }
    }

    private void ShakeBox()
    {
        transform.DOShakePosition(0.5f, strength: new Vector3(0.15f, 0.1f, 0), vibrato: 10, randomness: 10, fadeOut: true);
    }
}
