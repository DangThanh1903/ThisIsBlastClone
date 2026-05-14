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

        private Vector3 targetWorldPosition;
        private bool isMoving;
        private bool isShotted;

        public int X => x;
        public int Y => y;
        public BlockColor Color => color;
        public int Hp => hp;
        public BlockView View => blockView;
        public bool IsMoving => isMoving;
        public bool IsShotted => isShotted;

        private void Awake()
        {
            if (blockView == null)
            {
                blockView = GetComponent<BlockView>();
            }

            targetWorldPosition = transform.position;
        }

        public void Init(int gridX, int gridY, BlockColor blockColor, int blockHp = 1)
        {
            color = blockColor;
            hp = Mathf.Max(1, blockHp);
            isShotted = false;
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

        public void MarkShotted()
        {
            isShotted = true;
        }

        public void ClearShotted()
        {
            isShotted = false;
        }

        public void BeginMove(Vector3 worldPosition)
        {
            targetWorldPosition = worldPosition;
            isMoving = true;
        }

        public void MoveStep(float maxDistance)
        {
            if (!isMoving)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, maxDistance);

            if ((transform.position - targetWorldPosition).sqrMagnitude <= 0.0001f)
            {
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }

        public bool IsAtWorldPosition(Vector3 worldPosition)
        {
            return (transform.position - worldPosition).sqrMagnitude <= 0.0001f;
        }
    }
}
