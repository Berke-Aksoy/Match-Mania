using UnityEngine;
using DG.Tweening;

public class ColoredBlock : Block, IInteractable
{
    [SerializeField] private static int _condA = 4;  // Condition A
    [SerializeField] private static int _condB = 6; // Condition B
    [SerializeField] private static int _condC = 8;  // Condition C
    public void OnInteract()
    {
        if(_groupID == -1) { ShakeBlock(); return; }

        BoardCreator boardCreator = GetComponentInParent<BoardCreator>();
        if (boardCreator.BlastGroup(_groupID))
        {
            PlayBlastSound();
        }
        
    }

    public override void SetGroupID(int groupID, int groupSize)
    {
        base.SetGroupID(groupID, groupSize);
        _spriteRenderer.sprite = Data.IconSprites[GetIconIndex(groupSize)];
    }

    private int GetIconIndex(int groupSize)
    {
        if (groupSize >= _condC) return 2;
        if (groupSize >= _condB) return 1;
        if (groupSize >= _condA) return 0;
        return 3;
    }

    /*
    private void PlayLandingParticles(Block block)
    {
        if (landingParticlePrefab != null)
        {
            ParticleSystem particles = Instantiate(landingParticlePrefab, block.transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
    }
    */

    private void OnMouseDown()
    {
        OnInteract();
    }
}
