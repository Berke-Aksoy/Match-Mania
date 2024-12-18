using UnityEngine;
using DG.Tweening;

public class ColoredBlock : Block, IInteractable
{
    private int _groupID = -1;
    public int GroupID { get => _groupID; }

    [SerializeField] private static int firstThreshold = 4;  // Condition A
    [SerializeField] private static int secondThreshold = 6; // Condition B
    [SerializeField] private static int thirdThreshold = 8;  // Condition C
    public void OnInteract()
    {
        if(_groupID == -1) { AnimateNotValid(); return; }

        BoardCreator boardCreator = GetComponentInParent<BoardCreator>();
        boardCreator.BlastGroup(_groupID);

        PlayBlastSound();
    }

    public void SetGroupIDandIcon(int groupID, int size)
    {
        _groupID = groupID;

        int spriteIndex = 3; // Default sprite index

        if (size >= thirdThreshold)
        {
            spriteIndex = 2; // Condition C sprite index
        }
        else if (size >= secondThreshold)
        {
            spriteIndex = 1; // Condition B sprite index
        }
        else if (size >= firstThreshold)
        {
            spriteIndex = 0; // Condition A sprite index
        }

        _spriteRenderer.sprite = Data.IconSprites[spriteIndex];
    }

    private void AnimateNotValid()
    {
        _collider2D.enabled = false;
        transform.DOShakePosition(0.5f, strength: new Vector3(0.15f, 0.1f, 0), vibrato: 10, randomness: 10, fadeOut: true).OnComplete(() =>
        {
            _collider2D.enabled = true;
        });
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
        Debug.Log(name + " clicked on " + _groupID);
        OnInteract();
    }
}
