using UnityEngine;
using UnityEngine.Audio;

public abstract class Block : MonoBehaviour
{
    [SerializeField] protected BlockData _data;
    public BlockData Data { get => _data; }

    protected SpriteRenderer _spriteRenderer;
    protected Collider2D _collider2D;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<Collider2D>();
    }

    public virtual void PlayBlastSound()
    {
        AudioManager.Singleton.PlaySound(Data.BlastSound[Random.Range(0, Data.BlastSound.Length)]);
    }

    public void ColliderOnOff(bool isOn)
    {
        _collider2D.enabled = isOn;
    }
}
