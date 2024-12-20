using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InactivityChecker : MonoBehaviour
{
    [SerializeField] private float inactivityThreshold = 5f; // 5 seconds threshold
    private float inactivityTimer = 0f;
    private bool isHighlighting = false;

    private void Update()
    {
        TrackInactivity();
    }

    private void TrackInactivity()
    {
        inactivityTimer += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            ResetInactivityTimer();
        }

        if (inactivityTimer >= inactivityThreshold && !isHighlighting)
        {
            StartCoroutine(HighlightRandomBlockGroup());
        }
    }

    public void ResetInactivityTimer()
    {
        inactivityTimer = 0f;
        if (isHighlighting)
        {
            StopAllCoroutines();
            isHighlighting = false;
        }
    }

    private IEnumerator HighlightRandomBlockGroup()
    {
        isHighlighting = true;
        int totalGroupCount = BoardCreator.Singleton.BlockGroups.Count;

        if (totalGroupCount > 0)
        {
            Block[] blocks = BoardCreator.Singleton.BlockGroups[Random.Range(0, totalGroupCount)].Blocks.ToArray();
            Tween[] tweens = new Tween[blocks.Length];

            for(int i = 0; i < blocks.Length; i++)
            {
                tweens[i] = Highlighter.HighlightBlock(blocks[i]);
            }

            yield return new WaitForSeconds(4f); // Keep the highlight for 4 seconds
            ClearHighlight(blocks, tweens);
        }

        isHighlighting = false;
        ResetInactivityTimer();
    }

    private void ClearHighlight(Block[] blocks, Tween[] tweens)
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            Highlighter.StopHighlight(blocks[i], tweens[i]);
        }
    }
}
