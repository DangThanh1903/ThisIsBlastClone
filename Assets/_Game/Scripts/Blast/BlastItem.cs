using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastItem : MonoBehaviour
    {
        [SerializeField] private BlockColor color;
        [SerializeField] private int power;
        [SerializeField] private BlastItemView itemView;

        public BlockColor Color => color;
        public int Power => power;

        private void Awake()
        {
            if (itemView == null)
            {
                itemView = GetComponent<BlastItemView>();
            }
        }

        public void Init(BlockColor itemColor, int itemPower)
        {
            color = itemColor;
            power = itemPower;

            if (itemView != null)
            {
                itemView.SetData(color, power);
            }
        }
    }
}
