using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastItemSpawner : MonoBehaviour
    {
        private const string DefaultBlastItemPrefabPath = "Assets/_Game/Prefabs/Blast/BlastItem.prefab";

        [SerializeField] private BlastItem blastItemPrefab;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BoardMatcher boardMatcher;
        [SerializeField] private BoardGravity boardGravity;
        [SerializeField] private LevelController levelController;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private float itemSpacing = 1.2f;
        [SerializeField] private float boardBottomOffset = 1.25f;
        [SerializeField] private float launcherSlotOffset = 0.55f;
        [SerializeField] private float itemScale = 0.75f;
        [SerializeField] private int visibleItemCount = 3;
        [SerializeField] private float fireInterval = 0.08f;
        [SerializeField] private bool logSpawnSummary = true;

        private readonly Queue<BlastItemConfig> pendingItems = new Queue<BlastItemConfig>();
        private readonly List<BlastItem> spawnedItems = new List<BlastItem>();
        private readonly List<BlastItem> visibleItems = new List<BlastItem>();
        private bool isFiring;

        public void SpawnItems(IReadOnlyList<BlastItemConfig> itemConfigs)
        {
            ResolveDependencies();
            ClearItems();
            pendingItems.Clear();

            if (itemConfigs == null || itemConfigs.Count == 0)
            {
                Debug.LogWarning("BlastItemSpawner received no item configs.");
                return;
            }

            for (int i = 0; i < itemConfigs.Count; i++)
            {
                pendingItems.Enqueue(itemConfigs[i]);
            }

            FillVisibleItems();

            if (logSpawnSummary)
            {
                Debug.Log($"Blast queue loaded {itemConfigs.Count} items, visible now: {visibleItems.Count}.");
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
            visibleItems.Clear();
            pendingItems.Clear();
            isFiring = false;
        }

        public void ActivateItem(BlastItem item)
        {
            if (item == null || isFiring || levelController == null || levelController.State != GameState.Playing)
            {
                return;
            }

            int itemIndex = visibleItems.IndexOf(item);
            if (itemIndex < 0)
            {
                return;
            }

            visibleItems.RemoveAt(itemIndex);
            RepositionVisibleItems();
            FillVisibleItems();

            item.transform.position = GetLauncherSlotPosition(itemIndex);
            StartCoroutine(FireItemRoutine(item));
        }

        private void SpawnItem(BlockColor color, int power, int index, int total)
        {
            if (blastItemPrefab == null || boardController == null)
            {
                if (blastItemPrefab == null)
                {
                    blastItemPrefab = LoadDefaultBlastItemPrefabInEditor();
                }

                if (blastItemPrefab == null || boardController == null)
                {
                    Debug.LogError("BlastItemSpawner is missing required references.");
                    return;
                }
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
                dragController.Configure(this, levelController);
            }

            spawnedItems.Add(item);
            visibleItems.Add(item);
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

        private Vector3 GetLauncherSlotPosition(int index)
        {
            Vector3 bottomPosition = GetItemPosition(index, Mathf.Max(visibleItemCount, 1));
            bottomPosition.z += launcherSlotOffset;
            return bottomPosition;
        }

        private void FillVisibleItems()
        {
            while (visibleItems.Count < visibleItemCount && pendingItems.Count > 0)
            {
                BlastItemConfig config = pendingItems.Dequeue();
                SpawnItem(config.Color, config.Power, visibleItems.Count, Mathf.Max(visibleItemCount, 1));
            }
        }

        private void RepositionVisibleItems()
        {
            for (int i = 0; i < visibleItems.Count; i++)
            {
                BlastItem item = visibleItems[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.position = GetItemPosition(i, Mathf.Max(visibleItemCount, 1));
            }
        }

        private IEnumerator FireItemRoutine(BlastItem item)
        {
            isFiring = true;

            while (item != null && item.Power > 0 && levelController != null && levelController.State == GameState.Playing)
            {
                Block target = boardController.FindLowestBlockOfColor(item.Color, item.transform.position);
                if (target == null)
                {
                    break;
                }

                boardController.RemoveBlock(target);
                item.TryConsumePower();
                boardGravity.ApplyGravity(boardController);
                levelController.CheckWinCondition();

                yield return new WaitForSeconds(fireInterval);
            }

            spawnedItems.Remove(item);

            if (item != null)
            {
                Destroy(item.gameObject);
            }

            isFiring = false;
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

        private BlastItem LoadDefaultBlastItemPrefabInEditor()
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBlastItemPrefabPath);
            return prefab != null ? prefab.GetComponent<BlastItem>() : null;
#else
            return null;
#endif
        }
    }
}
