using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastItemSpawner : MonoBehaviour
    {
        private const string DefaultBlastItemPrefabPath = "Assets/_Game/Prefabs/Blast/BlastItem.prefab";

        [SerializeField] private BlastItem blastItemPrefab;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BoardGravity boardGravity;
        [SerializeField] private LevelController levelController;
        [SerializeField] private BlastShotController shotController;
        [SerializeField] private BlastLoseConditionEvaluator loseConditionEvaluator;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Transform activeSlotVisualRoot;
        [SerializeField] private float boardBottomOffset = 1.3f;
        [SerializeField] private float activeSlotSpacing = 0.85f;
        [SerializeField] private float queueColumnSpacing = 0.85f;
        [SerializeField] private float queueRowSpacing = 0.72f;
        [SerializeField] private float activeSlotBoardOffset = 0.35f;
        [SerializeField] private float itemScale = 0.75f;
        [FormerlySerializedAs("visibleItemCount")]
        [SerializeField] private int activeSlotCount = 5;
        [SerializeField] private int queueColumnCount = 3;
        [SerializeField] private int queueVisibleRows = 3;
        [SerializeField] private Color activeSlotColor = new Color(0.16f, 0.33f, 0.48f, 0.85f);
        [SerializeField] private bool logSpawnSummary = true;

        private readonly Queue<BlastItemConfig> pendingItems = new Queue<BlastItemConfig>();
        private readonly List<BlastItem> spawnedItems = new List<BlastItem>();
        private readonly List<BlastItem> queueItems = new List<BlastItem>();
        private readonly List<BlastItem> activeItems = new List<BlastItem>();
        private readonly Dictionary<BlastItem, int> itemColumns = new Dictionary<BlastItem, int>();
        private readonly Dictionary<BlastItem, int> itemRows = new Dictionary<BlastItem, int>();
        private readonly Dictionary<BlastItem, int> activeItemSlots = new Dictionary<BlastItem, int>();

        public void Configure(BoardController board, BoardGravity gravity, LevelController level)
        {
            boardController = board;
            boardGravity = gravity;
            levelController = level;

            EnsureLocalControllers();
            ConfigureLocalControllers();
        }

        public void SpawnItems(IReadOnlyList<BlastItemConfig> itemConfigs)
        {
            EnsureLocalControllers();
            ConfigureLocalControllers();
            ClearItems();
            pendingItems.Clear();
            CreateActiveSlotVisuals();

            if (itemConfigs == null || itemConfigs.Count == 0)
            {
                Debug.LogWarning("BlastItemSpawner received no item configs.");
                return;
            }

            for (int i = 0; i < itemConfigs.Count; i++)
            {
                pendingItems.Enqueue(itemConfigs[i]);
            }

            FillQueueColumns();

            if (logSpawnSummary)
            {
                Debug.Log($"Blast queue loaded {itemConfigs.Count} items, visible queue now: {queueItems.Count}.");
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

                item.transform.DOKill();

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
            queueItems.Clear();
            activeItems.Clear();
            itemColumns.Clear();
            itemRows.Clear();
            activeItemSlots.Clear();
            pendingItems.Clear();
            shotController?.ResetShots();
        }

        public void ActivateItem(BlastItem item)
        {
            EnsureLocalControllers();
            ConfigureLocalControllers();

            if (item == null
                || boardController == null
                || boardGravity == null
                || levelController == null
                || levelController.State != GameState.Playing
                || !queueItems.Contains(item)
                || !IsSelectableQueueItem(item))
            {
                return;
            }

            int activeSlot = FindFreeActiveSlot();
            if (activeSlot < 0)
            {
                CheckLoseCondition();
                return;
            }

            int sourceColumn = itemColumns[item];
            queueItems.Remove(item);
            itemColumns.Remove(item);
            itemRows.Remove(item);

            activeItems.Add(item);
            activeItemSlots[item] = activeSlot;
            shotController.EnsureColorCursor(item.Color);

            item.transform.DOKill();
            item.transform
                .DOMove(GetActiveSlotPosition(activeSlot), 0.18f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    AdvanceQueueColumn(sourceColumn);
                    StartFiringIfNeeded();
                });

        }

        private void StartFiringIfNeeded()
        {
            if (shotController == null || shotController.IsFiring)
            {
                return;
            }

            shotController.StartFiring(activeItems, ConsumeActiveItem, CheckLoseCondition);
        }

        private void FillQueueColumns()
        {
            for (int column = 0; column < queueColumnCount; column++)
            {
                FillQueueColumn(column);
            }
        }

        private void FillQueueColumn(int column)
        {
            for (int row = 0; row < queueVisibleRows && pendingItems.Count > 0; row++)
            {
                if (FindQueueItem(column, row) != null)
                {
                    continue;
                }

                BlastItemConfig config = pendingItems.Dequeue();
                SpawnQueueItem(config.Color, config.Power, column, row);
            }
        }

        private void SpawnQueueItem(BlockColor color, int power, int column, int row)
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
            Vector3 position = GetQueueItemPosition(column, row);

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
            queueItems.Add(item);
            itemColumns[item] = column;
            itemRows[item] = row;
        }

        private void CompactQueueColumn(int column)
        {
            for (int row = 1; row < queueVisibleRows; row++)
            {
                BlastItem item = FindQueueItem(column, row);
                if (item == null)
                {
                    continue;
                }

                int targetRow = row - 1;
                itemRows[item] = targetRow;
                item.transform.DOKill();
                item.transform.DOMove(GetQueueItemPosition(column, targetRow), 0.14f).SetEase(Ease.OutQuad);
            }
        }

        private void AdvanceQueueColumn(int column)
        {
            CompactQueueColumn(column);
            FillQueueColumn(column);
        }

        private BlastItem FindQueueItem(int column, int row)
        {
            for (int i = 0; i < queueItems.Count; i++)
            {
                BlastItem item = queueItems[i];
                if (item != null
                    && itemColumns.TryGetValue(item, out int itemColumn)
                    && itemRows.TryGetValue(item, out int itemRow)
                    && itemColumn == column
                    && itemRow == row)
                {
                    return item;
                }
            }

            return null;
        }

        private bool IsSelectableQueueItem(BlastItem item)
        {
            return itemRows.TryGetValue(item, out int row) && row == 0;
        }

        private int FindFreeActiveSlot()
        {
            for (int slot = 0; slot < activeSlotCount; slot++)
            {
                if (!IsActiveSlotOccupied(slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        private bool IsActiveSlotOccupied(int slot)
        {
            foreach (KeyValuePair<BlastItem, int> pair in activeItemSlots)
            {
                if (pair.Key != null && pair.Value == slot)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetQueueItemPosition(int column, int row)
        {
            float boardCenterX = GetBoardCenterX();
            float boardBottomZ = boardController.OriginPosition.z - boardController.CellSize * 0.5f;
            float firstX = boardCenterX - (queueColumnCount - 1) * queueColumnSpacing * 0.5f;

            return new Vector3(
                firstX + column * queueColumnSpacing,
                boardController.BoardPlaneY,
                boardBottomZ - boardBottomOffset - row * queueRowSpacing);
        }

        private Vector3 GetActiveSlotPosition(int index)
        {
            float boardCenterX = GetBoardCenterX();
            float boardBottomZ = boardController.OriginPosition.z - boardController.CellSize * 0.5f;
            float firstX = boardCenterX - (activeSlotCount - 1) * activeSlotSpacing * 0.5f;

            return new Vector3(
                firstX + index * activeSlotSpacing,
                boardController.BoardPlaneY,
                boardBottomZ - activeSlotBoardOffset);
        }

        private float GetBoardCenterX()
        {
            return boardController.OriginPosition.x
                + (boardController.Width - 1) * boardController.CellSize * 0.5f;
        }

        private void CreateActiveSlotVisuals()
        {
            if (boardController == null)
            {
                return;
            }

            Transform root = EnsureActiveSlotVisualRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            for (int i = 0; i < activeSlotCount; i++)
            {
                GameObject slotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slotObject.name = $"ActiveSlot_{i + 1}";
                slotObject.transform.SetParent(root, false);
                slotObject.transform.position = GetActiveSlotPosition(i) + new Vector3(0f, -0.05f, 0f);
                float slotSize = boardController.CellSize * 0.8f;
                slotObject.transform.localScale = new Vector3(slotSize, 0.05f, slotSize);

                Renderer renderer = slotObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"))
                    {
                        color = activeSlotColor
                    };
                }

                Collider collider = slotObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
        }

        private Transform EnsureActiveSlotVisualRoot()
        {
            if (activeSlotVisualRoot != null)
            {
                return activeSlotVisualRoot;
            }

            GameObject rootObject = new GameObject("ActiveSlotVisualsRoot");
            rootObject.transform.SetParent(transform, false);
            activeSlotVisualRoot = rootObject.transform;
            return activeSlotVisualRoot;
        }

        private void ConsumeActiveItem(BlastItem item)
        {
            spawnedItems.Remove(item);
            activeItems.Remove(item);
            activeItemSlots.Remove(item);

            if (item != null)
            {
                item.transform.DOKill();
                Destroy(item.gameObject);
            }

        }

        private void CheckLoseCondition()
        {
            if (loseConditionEvaluator == null || !loseConditionEvaluator.ShouldLose(activeItems, activeSlotCount))
            {
                return;
            }

            levelController.LoseLevel();
        }

        private void EnsureLocalControllers()
        {
            if (shotController == null)
            {
                shotController = GetComponent<BlastShotController>();
                if (shotController == null)
                {
                    shotController = gameObject.AddComponent<BlastShotController>();
                }
            }

            if (loseConditionEvaluator == null)
            {
                loseConditionEvaluator = GetComponent<BlastLoseConditionEvaluator>();
                if (loseConditionEvaluator == null)
                {
                    loseConditionEvaluator = gameObject.AddComponent<BlastLoseConditionEvaluator>();
                }
            }
        }

        private void ConfigureLocalControllers()
        {
            shotController?.Configure(boardController, boardGravity, levelController);
            loseConditionEvaluator?.Configure(boardController, levelController);
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
