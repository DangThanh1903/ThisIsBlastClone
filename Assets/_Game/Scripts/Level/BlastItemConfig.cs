using System;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [Serializable]
    public struct BlastItemConfig
    {
        [SerializeField] private BlockColor color;
        [SerializeField] private int power;

        public BlockColor Color => color;
        public int Power => Mathf.Max(0, power);

        public BlastItemConfig(BlockColor color, int power)
        {
            this.color = color;
            this.power = Mathf.Max(0, power);
        }
    }
}
