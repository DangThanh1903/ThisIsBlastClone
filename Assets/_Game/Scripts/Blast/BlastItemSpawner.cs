using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private float itemSpacing = 1.2f;
        [SerializeField] private float boardBottomOffset = 1.25f;
        [SerializeField] private int activeSlotCount = 5;
        [SerializeField] private float activeSlotSpacing = 0.85f;
        [SerializeField] private float activeSlotBoardOffset = 0.35f;
        [SerializeField] private float itemScale = 0.75f;
        [SerializeField] private int visibleItemCount = 3;
        [SerializeField] private Color activeSlotColor = new Color(0.16f, 0.33f, 0.48f, 0.85f);
        [SerializeField] private bool logSpawnSummary = true;

        private readonly Queue<BlastItemConfig> pendingItems = new Queue<BlastItemConfig>();
        private readonly List<BlastItem> spawnedItems = new List<BlastItem>();
        private readonly List<BlastItem> visibleItems = new List<BlastItem>();
        private readonly List<BlastItem> activeItems = new List<BlastItem>();

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
            activeItems.Clear();
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
                || !visibleItems.Contains(item))
            {
                return;
            }

            if (activeItems.Count >= activeSlotCount)
            {
                CheckLoseCondition();
                return;
            }

            visibleItems.Remove(item);
            activeItems.Add(item);
            shotController.EnsureColorCursor(item.Color);

            item.transform.position = GetActiveSlotPosition(activeItems.Count - 1);

            RepositionVisibleItems();
            FillVisibleItems();

            if (!shotController.IsFiring)
            {
                shotController.StartFiring(activeItems, ConsumeActiveItem, CheckLoseCondition);
            }
        }

        private void FillVisibleItems()
        {
            while (visibleItems.Count < visibleItemCount && pendingItems.Count > 0)
            {
                BlastItemConfig config = pendingItems.Dequeue();
                SpawnItem(config.Color, config.Power, visibleItems.Count, Mathf.Max(visibleItemCount, 1));
            }
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

        private Vector3 GetActiveSlotPosition(int index)
        {
            float boardCenterX = boardController.OriginPosition.x
                + (boardController.Width - 1) * boardController.CellSize * 0.5f;
            float boardBottomZ = boardController.OriginPosition.z - boardController.CellSize * 0.5f;
            float firstX = boardCenterX - (activeSlotCount - 1) * activeSlotSpacing * 0.5f;

            return new Vector3(
                firstX + index * activeSlotSpacing,
                boardController.BoardPlaneY,
                boardBottomZ - activeSlotBoardOffset);
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

        private void RepositionActiveItems()
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                BlastItem item = activeItems[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.position = GetActiveSlotPosition(i);
            }
        }

        private void ConsumeActiveItem(BlastItem item)
        {
            spawnedItems.Remove(item);
            activeItems.Remove(item);

            if (item != null)
            {
                Destroy(item.gameObject);
            }

            RepositionActiveItems();
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
