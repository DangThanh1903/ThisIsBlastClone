using System;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [Serializable]
    public struct LevelCellData
    {
        [SerializeField] private CellType cellType;
        [SerializeField] private BlockColor color;
        [SerializeField] private BlockSpecialType specialType;

        public CellType CellType => cellType;
        public BlockColor Color => color;
        public BlockSpecialType SpecialType => specialType;
        public bool HasBlock => cellType == CellType.Block;

        public LevelCellData(CellType cellType, BlockColor color, BlockSpecialType specialType)
        {
            this.cellType = cellType;
            this.color = color;
            this.specialType = specialType;
        }

        public static LevelCellData Block(BlockColor color, BlockSpecialType specialType = BlockSpecialType.None)
        {
            return new LevelCellData(CellType.Block, color, specialType);
        }

        public static LevelCellData Empty()
        {
            return new LevelCellData(CellType.Empty, BlockColor.Yellow, BlockSpecialType.None);
        }
    }
}
