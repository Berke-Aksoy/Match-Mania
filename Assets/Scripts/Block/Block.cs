using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace MatchMania.Blocks
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public abstract class Block : MonoBehaviour
    {
        [SerializeField] protected BlockData _data;
        public BlockData Data { get => _data; }

        protected int _groupID = -1;
        public int GroupID { get => _groupID; }
        protected SpriteRenderer _spriteRenderer;
        protected BoxCollider2D _boxCollider2D;
        protected Vector2Int _location;
        public Vector2Int Location { get => _location; set => _location = value; }
        private bool _isMoving;
        public bool IsMoving { get => _isMoving; }
        private Tween _tween;
        public Tween Tween { get=>_tween; set => _tween = value; }

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
            AudioManager.Instance.PlaySound(Data.BlastSound[Random.Range(0, Data.BlastSound.Length)], volume);
        }

        public void OnOffCollider(bool value)
        {
            _boxCollider2D.enabled = value;
        }

        public void Moving(float moveDuration)
        {
            _isMoving = true;
            OnOffCollider(false);
            StartCoroutine(ResetMoving(moveDuration));
        }

        IEnumerator ResetMoving(float moveDuration)
        {
            yield return new WaitForSeconds(moveDuration);
            _isMoving = false;
            OnOffCollider(true);
        }

        private void OnDestroy()
        {
            Tween.Kill();
        }
    }
}
