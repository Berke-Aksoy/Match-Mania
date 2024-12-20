using DG.Tweening;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public abstract class Block : MonoBehaviour
{
    [SerializeField] protected BlockData _data;
    public BlockData Data { get => _data; }

    protected int _groupID = -1;
    public int GroupID { get => _groupID; }
    protected SpriteRenderer _spriteRenderer;
    protected BoxCollider2D _boxCollider2D;
    protected Vector2Int _locationOnBoard;
    public Vector2Int LocationOnBoard { get => _locationOnBoard; set => _locationOnBoard = value; }
    private bool _isMoving;
    public bool IsMoving { get => _isMoving; }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
    }

    public virtual void SetGroupID(int groupID, int groupSize)
    {
        _groupID = groupID;
    }

    protected virtual void PlayBlastSound(float volume = 1f)
    {
        AudioManager.Singleton.PlaySound(Data.BlastSound[Random.Range(0, Data.BlastSound.Length)], volume);
    }

    protected void ShakeBlock(float shakeDuration = 0.5f)
    {
        _boxCollider2D.enabled = false;
        transform.DOShakePosition(shakeDuration, strength: new Vector3(0.15f, 0.1f, 0), vibrato: 10, randomness: 10, fadeOut: true).OnComplete(() =>
        {
            _boxCollider2D.enabled = true;
        });
    }

    public void Moving(float moveDuration)
    {
        _isMoving = true;
        _boxCollider2D.enabled = false;
        StartCoroutine(ResetMoving(moveDuration));
    }

    IEnumerator ResetMoving(float moveDuration)
    {
        yield return new WaitForSeconds(moveDuration);
        _isMoving = false;
        _boxCollider2D.enabled = true;
    }
}
