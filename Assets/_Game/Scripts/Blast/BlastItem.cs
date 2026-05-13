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
            SetPower(itemPower);
        }

        public void SetPower(int value)
        {
            power = Mathf.Max(0, value);

            if (itemView != null)
            {
                itemView.SetData(color, power);
            }
        }

        public bool TryConsumePower(int amount = 1)
        {
            if (power <= 0)
            {
                return false;
            }

            SetPower(power - Mathf.Max(1, amount));
            return true;
        }
    }
}
