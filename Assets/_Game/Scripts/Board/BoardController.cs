using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BoardController : MonoBehaviour
    {
        private const string DefaultBlockPrefabPath = "Assets/_Game/Prefabs/Board/Block.prefab";

        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float cellSize = 0.6f;
        [SerializeField] private Vector3 originPosition;
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Transform boardRoot;
        [SerializeField, Range(0.1f, 1f)] private float blockFill = 0.92f;
        [SerializeField] private bool logSpawnSummary = true;

        private Block[,] grid;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 OriginPosition => originPosition;
        public float BoardPlaneY => originPosition.y;

        public void SpawnBoard(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("BoardController cannot spawn without LevelData.");
                return;
            }

            if (blockPrefab == null)
            {
                blockPrefab = LoadDefaultBlockPrefabInEditor();
                if (blockPrefab == null)
                {
                    Debug.LogError("BoardController is missing a Block prefab.");
                    return;
                }
            }

            ClearBoard();

            width = levelData.Width;
            height = levelData.Height;
            grid = new Block[width, height];

            Transform parent = boardRoot != null ? boardRoot : transform;
            int spawnedBlockCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    LevelCellData cell = levelData.GetCell(x, y);
                    if (!cell.HasBlock)
                    {
                        grid[x, y] = null;
                        continue;
                    }

                    Vector3 finalPosition = GridToWorld(x, y);
                    Block block = Instantiate(blockPrefab, finalPosition, Quaternion.identity, parent);
                    block.name = $"Block_{x}_{y}";
                    block.transform.localScale = Vector3.one * cellSize * blockFill;
                    block.Init(x, y, cell.Color, cell.Hp);

                    grid[x, y] = block;
                    spawnedBlockCount++;
                }
            }

            if (logSpawnSummary)
            {
                Debug.Log($"Board spawned {spawnedBlockCount} blocks from level '{levelData.LevelId}' ({width}x{height}).");
            }
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            float leftEdge = originPosition.x - cellSize * 0.5f;
            float bottomEdge = originPosition.z - cellSize * 0.5f;

            int gridX = Mathf.FloorToInt((worldPosition.x - leftEdge) / cellSize);
            int gridY = Mathf.FloorToInt((worldPosition.z - bottomEdge) / cellSize);

            return new GridPosition(gridX, gridY);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return originPosition + new Vector3(x * cellSize, 0f, y * cellSize);
        }

        public Block GetBlock(int x, int y)
        {
            if (!IsInsideGrid(x, y) || grid == null)
            {
                return null;
            }

            return grid[x, y];
        }

        public void RemoveBlocks(List<Block> blocks)
        {
            if (blocks == null)
            {
                return;
            }

            foreach (Block block in blocks)
            {
                if (block == null || !IsInsideGrid(block.X, block.Y))
                {
                    continue;
                }

                if (grid[block.X, block.Y] == block)
                {
                    grid[block.X, block.Y] = null;
                }

                Destroy(block.gameObject);
            }
        }

        public void RemoveBlock(Block block)
        {
            if (block == null || !IsInsideGrid(block.X, block.Y))
            {
                return;
            }

            if (grid[block.X, block.Y] == block)
            {
                grid[block.X, block.Y] = null;
            }

            Destroy(block.gameObject);
        }

        public bool TryRemoveBottomBlockOfColor(BlockColor color, int startColumn, out int removedColumn)
        {
            if (!TryGetBottomBlockOfColor(color, startColumn, out Block block, out removedColumn))
            {
                return false;
            }

            RemoveBlock(block);
            return true;
        }

        public bool TryGetBottomBlockOfColor(
            BlockColor color,
            int startColumn,
            out Block targetBlock,
            out int targetColumn)
        {
            targetBlock = null;
            targetColumn = -1;

            if (width <= 0)
            {
                return false;
            }

            int normalizedStartColumn = Mathf.Clamp(startColumn, 0, width - 1);

            for (int offset = 0; offset < width; offset++)
            {
                int x = (normalizedStartColumn + offset) % width;
                Block block = GetBlock(x, 0);
                if (block == null
                    || block.Y != 0
                    || block.IsShotted
                    || block.Color != color)
                {
                    continue;
                }

                targetBlock = block;
                targetColumn = x;
                return true;
            }

            return false;
        }

        public bool HasBottomBlockOfColor(BlockColor color)
        {
            for (int x = 0; x < width; x++)
            {
                Block block = GetBlock(x, 0);
                if (block != null
                    && block.Y == 0
                    && !block.IsShotted
                    && block.Color == color)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCleared()
        {
            if (grid == null)
            {
                return true;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int CountBlocks()
        {
            int count = 0;

            if (grid == null)
            {
                return count;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public bool IsInsideGrid(GridPosition position)
        {
            return IsInsideGrid(position.X, position.Y);
        }

        public bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public void ClearBoard()
        {
            if (boardRoot == null)
            {
                boardRoot = transform;
            }

            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = boardRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            grid = null;
        }

        internal void SetBlockAt(int x, int y, Block block)
        {
            if (!IsInsideGrid(x, y) || grid == null)
            {
                return;
            }

            grid[x, y] = block;
        }

        private Block LoadDefaultBlockPrefabInEditor()
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBlockPrefabPath);
            return prefab != null ? prefab.GetComponent<Block>() : null;
#else
            return null;
#endif
        }
    }
}
