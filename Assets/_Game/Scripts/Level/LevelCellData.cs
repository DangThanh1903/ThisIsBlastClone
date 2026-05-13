using System;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [Serializable]
    public struct LevelCellData
    {
        [SerializeField] private CellType cellType;
        [SerializeField] private BlockColor color;
        [SerializeField] private int hp;
        [SerializeField] private BlockSpecialType specialType;

        public CellType CellType => cellType;
        public BlockColor Color => color;
        public int Hp => Mathf.Max(1, hp);
        public BlockSpecialType SpecialType => specialType;
        public bool HasBlock => cellType == CellType.Block;

        public LevelCellData(CellType cellType, BlockColor color, int hp, BlockSpecialType specialType)
        {
            this.cellType = cellType;
            this.color = color;
            this.hp = Mathf.Max(1, hp);
            this.specialType = specialType;
        }

        public static LevelCellData Block(BlockColor color, int hp = 1, BlockSpecialType specialType = BlockSpecialType.None)
        {
            return new LevelCellData(CellType.Block, color, hp, specialType);
        }

        public static LevelCellData Empty()
        {
            return new LevelCellData(CellType.Empty, BlockColor.Yellow, 1, BlockSpecialType.None);
        }
    }
}
