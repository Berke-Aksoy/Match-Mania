using UnityEngine;

public class ObstacleBlock : Block
{
    [SerializeField] [Range(-1, 10)] private int _health;
    [SerializeField] private GameObject _lowerHealthVersion;
}
