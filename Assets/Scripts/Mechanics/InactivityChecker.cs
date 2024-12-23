using DG.Tweening;
using System.Collections;
using UnityEngine;
using MatchMania.Blocks;
using System.Collections.Generic;

namespace MatchMania.InactivityCheck
{
    public class InactivityChecker : MonoBehaviour
    {
        [SerializeField] private float inactivityThreshold = 5f;
        [SerializeField] private float highlightDuration = 3f;
        private float inactivityTimer = 0f;
        private bool isHighlighting = false;
        Tween[] tweens;
        Block[] blocks;

        private void OnEnable()
        {
            Blaster.OnBlastComplete += ResetInactivityTimer;
        }

        private void OnDisable()
        {
            Blaster.OnBlastComplete -= ResetInactivityTimer;
        }

        private void Update()
        {
            TrackInactivity();
        }

        private void TrackInactivity()
        {
            inactivityTimer += Time.deltaTime;

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
                StopHighlightingGroup(blocks, tweens);
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
                blocks = blockGroups[Random.Range(0, totalGroupCount)].Blocks.ToArray();
                tweens = new Tween[blocks.Length];

                for (int i = 0; i < blocks.Length; i++)
                {
                    tweens[i] = Highlighter.HighlightBlock(blocks[i].transform);
                }

                yield return new WaitForSeconds(highlightDuration); // Keep the highlight for 4 seconds

                StopHighlightingGroup(blocks, tweens);
            }

            isHighlighting = false;
            ResetInactivityTimer();
        }

        private void StopHighlightingGroup(Block[] blocks, Tween[] tweens)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                Highlighter.StopHighlight(blocks[i].transform, tweens[i]);
            }
        }

    }
}
