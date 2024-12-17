using UnityEngine;

public abstract class Block : MonoBehaviour
{
    [SerializeField] private BlockData _data;
    public BlockData Data { get => _data; }
}
