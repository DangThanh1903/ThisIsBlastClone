using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [CreateAssetMenu(menuName = "This Is Blast/Level Data", fileName = "LevelData")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private string levelId = "level_01";
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 8;
        [SerializeField] private LevelCellData[] cells;
        [SerializeField] private BlastItemConfig[] startingBlastItems =
        {
            new BlastItemConfig(BlockColor.Yellow, 44),
            new BlastItemConfig(BlockColor.Red, 60)
        };
        [SerializeField] private WinConditionType winCondition = WinConditionType.ClearAllBlocks;

        public string LevelId => levelId;
        public int LevelNumber => levelNumber;
        public int Width => width;
        public int Height => height;
        public WinConditionType WinCondition => winCondition;
        public IReadOnlyList<BlastItemConfig> StartingBlastItems => startingBlastItems;

        public LevelCellData GetCell(int x, int y)
        {
            if (!IsInside(x, y))
            {
                Debug.LogError($"LevelData cell lookup outside bounds: ({x}, {y}).");
                return LevelCellData.Empty();
            }

            EnsureCellArray();
            return cells[ToIndex(x, y)];
        }

        public BlockColor GetColor(int x, int y)
        {
            return GetCell(x, y).Color;
        }

        public void SetLevelInfo(string id, int number)
        {
            levelId = string.IsNullOrWhiteSpace(id) ? $"level_{number:00}" : id;
            levelNumber = Mathf.Max(1, number);
        }

        public void Initialize(int levelWidth, int levelHeight, BlockColor defaultColor)
        {
            width = Mathf.Max(1, levelWidth);
            height = Mathf.Max(1, levelHeight);
            cells = new LevelCellData[width * height];

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = LevelCellData.Block(defaultColor);
            }
        }

        public void SetColor(int x, int y, BlockColor color)
        {
            SetBlock(x, y, color);
        }

        public void SetBlock(
            int x,
            int y,
            BlockColor color,
            BlockSpecialType specialType = BlockSpecialType.None)
        {
            if (!IsInside(x, y))
            {
                return;
            }

            EnsureCellArray();
            cells[ToIndex(x, y)] = LevelCellData.Block(color, specialType);
        }

        public void SetEmpty(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return;
            }

            EnsureCellArray();
            cells[ToIndex(x, y)] = LevelCellData.Empty();
        }

        public void SetStartingBlastItems(params BlastItemConfig[] items)
        {
            startingBlastItems = items == null || items.Length == 0
                ? new[] { new BlastItemConfig(BlockColor.Yellow, 44), new BlastItemConfig(BlockColor.Red, 60) }
                : items;
        }

        public void SetWinCondition(WinConditionType condition)
        {
            winCondition = condition;
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            levelNumber = Mathf.Max(1, levelNumber);
            EnsureCellArray();

            if (startingBlastItems == null || startingBlastItems.Length == 0)
            {
                SetStartingBlastItems();
            }
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private int ToIndex(int x, int y)
        {
            return y * width + x;
        }

        private void EnsureCellArray()
        {
            int expectedLength = width * height;

            if (cells != null && cells.Length == expectedLength)
            {
                return;
            }

            LevelCellData[] resized = new LevelCellData[expectedLength];
            if (cells != null)
            {
                int copyLength = Mathf.Min(cells.Length, resized.Length);
                for (int i = 0; i < copyLength; i++)
                {
                    resized[i] = cells[i];
                }
            }

            for (int i = cells != null ? Mathf.Min(cells.Length, resized.Length) : 0; i < resized.Length; i++)
            {
                resized[i] = LevelCellData.Block(BlockColor.Yellow);
            }

            cells = resized;
        }
    }
}
