using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastItemSpawner : MonoBehaviour
    {
        [SerializeField] private BlastItem blastItemPrefab;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BoardMatcher boardMatcher;
        [SerializeField] private BoardGravity boardGravity;
        [SerializeField] private LevelController levelController;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private float itemSpacing = 1.2f;
        [SerializeField] private float boardBottomOffset = 1.25f;
        [SerializeField] private float itemScale = 0.75f;

        private readonly List<BlastItem> spawnedItems = new List<BlastItem>();

        public void SpawnItems(IReadOnlyList<BlastItemConfig> itemConfigs)
        {
            ResolveDependencies();
            ClearItems();

            if (itemConfigs == null || itemConfigs.Count == 0)
            {
                Debug.LogWarning("BlastItemSpawner received no item configs.");
                return;
            }

            for (int i = 0; i < itemConfigs.Count; i++)
            {
                BlastItemConfig config = itemConfigs[i];
                SpawnItem(config.Color, config.Power, i, itemConfigs.Count);
            }
        }

        public void ClearItems()
        {
            for (int i = spawnedItems.Count - 1; i >= 0; i--)
            {
                BlastItem item = spawnedItems[i];
                if (item == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(item.gameObject);
                }
                else
                {
                    DestroyImmediate(item.gameObject);
                }
            }

            spawnedItems.Clear();
        }

        private void SpawnItem(BlockColor color, int power, int index, int total)
        {
            if (blastItemPrefab == null || boardController == null)
            {
                Debug.LogError("BlastItemSpawner is missing required references.");
                return;
            }

            Transform parent = spawnRoot != null ? spawnRoot : transform;
            Vector3 position = GetItemPosition(index, total);

            BlastItem item = Instantiate(blastItemPrefab, position, Quaternion.identity, parent);
            item.name = $"BlastItem_{color}_{power}";
            item.transform.localScale = Vector3.one * itemScale;
            item.Init(color, power);

            BlastDragController dragController = item.GetComponent<BlastDragController>();
            if (dragController != null)
            {
                dragController.Configure(boardController, boardMatcher, boardGravity, levelController);
            }

            spawnedItems.Add(item);
        }

        private Vector3 GetItemPosition(int index, int total)
        {
            float boardCenterX = boardController.OriginPosition.x
                + (boardController.Width - 1) * boardController.CellSize * 0.5f;
            float boardBottomZ = boardController.OriginPosition.z - boardController.CellSize * 0.5f;
            float firstX = boardCenterX - (total - 1) * itemSpacing * 0.5f;

            return new Vector3(
                firstX + index * itemSpacing,
                boardController.BoardPlaneY,
                boardBottomZ - boardBottomOffset);
        }

        private void ResolveDependencies()
        {
            if (boardController == null)
            {
                boardController = FindFirstObjectByType<BoardController>();
            }

            if (boardMatcher == null)
            {
                boardMatcher = FindFirstObjectByType<BoardMatcher>();
            }

            if (boardGravity == null)
            {
                boardGravity = FindFirstObjectByType<BoardGravity>();
            }

            if (levelController == null)
            {
                levelController = FindFirstObjectByType<LevelController>();
            }
        }
    }
}
