using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class Block : MonoBehaviour
    {
        [SerializeField] private BlockView blockView;

        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private BlockColor color;
        [SerializeField] private int hp = 1;

        public int X => x;
        public int Y => y;
        public BlockColor Color => color;
        public int Hp => hp;
        public BlockView View => blockView;

        private void Awake()
        {
            if (blockView == null)
            {
                blockView = GetComponent<BlockView>();
            }
        }

        public void Init(int gridX, int gridY, BlockColor blockColor, int blockHp = 1)
        {
            color = blockColor;
            hp = Mathf.Max(1, blockHp);
            SetGridPosition(gridX, gridY);

            if (blockView != null)
            {
                blockView.SetColor(color);
            }
        }

        public void SetGridPosition(int gridX, int gridY)
        {
            x = gridX;
            y = gridY;
        }
    }
}
