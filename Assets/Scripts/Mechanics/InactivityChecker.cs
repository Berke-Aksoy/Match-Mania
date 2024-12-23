using DG.Tweening;
using System.Collections;
using UnityEngine;
using MatchMania.Blocks;
using System.Collections.Generic;

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
        List<GroupManager.BlockGroup> blockGroups = GroupManager.Instance.BlockGroups;
        int totalGroupCount = GroupManager.Instance.BlockGroups.Count;

        if (totalGroupCount > 0)
        {
            Block[] blocks = blockGroups[Random.Range(0, totalGroupCount)].Blocks.ToArray();
            Tween[] tweens = new Tween[blocks.Length];

            for(int i = 0; i < blocks.Length; i++)
            {
                tweens[i] = Highlighter.HighlightBlock(blocks[i].transform);
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
            Highlighter.StopHighlight(blocks[i].transform, tweens[i]);
        }
    }
}
