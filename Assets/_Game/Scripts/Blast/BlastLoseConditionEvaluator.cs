using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastLoseConditionEvaluator : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private LevelController levelController;

        public void Configure(BoardController board, LevelController level)
        {
            boardController = board;
            levelController = level;
        }

        public bool ShouldLose(IReadOnlyList<BlastItem> activeItems, int activeSlotCount)
        {
            if (levelController == null
                || boardController == null
                || activeItems == null
                || levelController.State != GameState.Playing
                || activeItems.Count < activeSlotCount)
            {
                return false;
            }

            for (int i = 0; i < activeItems.Count; i++)
            {
                BlastItem item = activeItems[i];
                if (item != null
                    && item.Power > 0
                    && boardController.HasBottomBlockOfColor(item.Color))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
