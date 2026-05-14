using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastShotController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private BoardGravity boardGravity;
        [SerializeField] private LevelController levelController;
        [SerializeField] private float fireInterval = 0.04f;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileScale = 0.18f;

        private readonly Dictionary<BlockColor, int> nextFireColumnsByColor = new Dictionary<BlockColor, int>();
        private int inFlightProjectileCount;
        private Coroutine firingRoutine;

        public bool IsFiring => firingRoutine != null;

        public void Configure(BoardController board, BoardGravity gravity, LevelController level)
        {
            boardController = board;
            boardGravity = gravity;
            levelController = level;
        }

        public void ResetShots()
        {
            if (firingRoutine != null)
            {
                StopCoroutine(firingRoutine);
                firingRoutine = null;
            }

            nextFireColumnsByColor.Clear();
            inFlightProjectileCount = 0;
        }

        public void EnsureColorCursor(BlockColor color)
        {
            if (!nextFireColumnsByColor.ContainsKey(color))
            {
                nextFireColumnsByColor[color] = 0;
            }
        }

        public void StartFiring(
            IList<BlastItem> activeItems,
            Action<BlastItem> consumeActiveItem,
            Action firingCompleted)
        {
            if (firingRoutine != null)
            {
                return;
            }

            firingRoutine = StartCoroutine(FireActiveItemsRoutine(activeItems, consumeActiveItem, firingCompleted));
        }

        private IEnumerator FireActiveItemsRoutine(
            IList<BlastItem> activeItems,
            Action<BlastItem> consumeActiveItem,
            Action firingCompleted)
        {
            inFlightProjectileCount = 0;

            while (levelController != null && levelController.State == GameState.Playing)
            {
                if (inFlightProjectileCount <= 0)
                {
                    ConsumeSpentActiveItems(activeItems, consumeActiveItem);
                }

                bool firedAny = ScheduleAvailableShots(activeItems);
                if (firedAny)
                {
                    yield return new WaitForSeconds(fireInterval);
                    continue;
                }

                if (inFlightProjectileCount > 0)
                {
                    yield return null;
                    continue;
                }

                break;
            }

            ConsumeSpentActiveItems(activeItems, consumeActiveItem);
            firingRoutine = null;
            firingCompleted?.Invoke();
        }

        private bool ScheduleAvailableShots(IList<BlastItem> activeItems)
        {
            if (activeItems == null
                || boardController == null
                || boardController.Width <= 0
                || boardGravity == null
                || levelController == null)
            {
                return false;
            }

            bool firedAny = false;

            for (int i = 0; i < activeItems.Count && levelController.State == GameState.Playing; i++)
            {
                BlastItem item = activeItems[i];
                if (item == null || item.Power <= 0)
                {
                    continue;
                }

                nextFireColumnsByColor.TryGetValue(item.Color, out int nextColumn);
                if (!boardController.TryGetBottomBlockOfColor(
                    item.Color,
                    nextColumn,
                    out Block targetBlock,
                    out int targetColumn))
                {
                    continue;
                }

                LaunchShot(item, targetBlock, targetColumn);
                firedAny = true;
            }

            return firedAny;
        }

        private void LaunchShot(BlastItem item, Block targetBlock, int targetColumn)
        {
            if (item == null || targetBlock == null || item.Power <= 0)
            {
                return;
            }

            targetBlock.MarkShotted();
            nextFireColumnsByColor[item.Color] = (targetColumn + 1) % boardController.Width;
            FaceTarget(item, targetBlock.transform.position);

            if (!item.TryConsumePower())
            {
                targetBlock.ClearShotted();
                return;
            }

            inFlightProjectileCount++;
            ProjectileController.Create(
                item.Color,
                item.transform.position,
                projectileScale,
                projectileSpeed,
                targetBlock,
                levelController,
                HandleProjectileFinished);
        }

        private void HandleProjectileFinished(Block targetBlock, bool hitTarget)
        {
            if (targetBlock != null
                && hitTarget
                && boardController != null
                && boardGravity != null
                && levelController != null
                && levelController.State == GameState.Playing)
            {
                boardController.RemoveBlock(targetBlock);
                boardGravity.ApplyGravity(boardController);
                levelController.CheckWinCondition();
            }
            else if (targetBlock != null)
            {
                targetBlock.ClearShotted();
            }

            inFlightProjectileCount = Mathf.Max(0, inFlightProjectileCount - 1);
        }

        private void FaceTarget(BlastItem item, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - item.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            item.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ConsumeSpentActiveItems(
            IList<BlastItem> activeItems,
            Action<BlastItem> consumeActiveItem)
        {
            if (activeItems == null)
            {
                return;
            }

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                BlastItem item = activeItems[i];
                if (item == null)
                {
                    activeItems.RemoveAt(i);
                    continue;
                }

                if (item.Power <= 0)
                {
                    consumeActiveItem?.Invoke(item);
                }
            }
        }
    }
}
