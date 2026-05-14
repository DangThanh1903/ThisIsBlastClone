using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BoardGravity : MonoBehaviour
    {
        [SerializeField] private float fallSpeed = 6f;

        private readonly List<Block> movingBlocks = new List<Block>();

        private void Update()
        {
            float maxDistance = fallSpeed * Time.deltaTime;

            for (int i = movingBlocks.Count - 1; i >= 0; i--)
            {
                Block block = movingBlocks[i];
                if (block == null)
                {
                    movingBlocks.RemoveAt(i);
                    continue;
                }

                block.MoveStep(maxDistance);
                if (!block.IsMoving)
                {
                    movingBlocks.RemoveAt(i);
                }
            }
        }

        public void ApplyGravity(BoardController board)
        {
            List<Block> movedBlocks = CompactBoard(board);
            for (int i = 0; i < movedBlocks.Count; i++)
            {
                TrackMovingBlock(movedBlocks[i]);
            }
        }

        public IEnumerator ApplyGravityRoutine(BoardController board)
        {
            List<Block> movedBlocks = CompactBoard(board);
            if (movedBlocks.Count == 0)
            {
                yield break;
            }

            bool hasMovingBlocks;
            do
            {
                hasMovingBlocks = false;
                float maxDistance = fallSpeed * Time.deltaTime;

                for (int i = 0; i < movedBlocks.Count; i++)
                {
                    Block block = movedBlocks[i];
                    if (block == null || !block.IsMoving)
                    {
                        continue;
                    }

                    block.MoveStep(maxDistance);
                    hasMovingBlocks |= block.IsMoving;
                }

                yield return null;
            }
            while (hasMovingBlocks);
        }

        private List<Block> CompactBoard(BoardController board)
        {
            if (board == null)
            {
                return new List<Block>();
            }

            List<Block> movedBlocks = new List<Block>();

            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;

                // Scan bottom to top. Every surviving block is compacted to the lowest empty row.
                for (int readY = 0; readY < board.Height; readY++)
                {
                    Block block = board.GetBlock(x, readY);
                    if (block == null)
                    {
                        continue;
                    }

                    if (readY != writeY)
                    {
                        board.SetBlockAt(x, readY, null);
                        board.SetBlockAt(x, writeY, block);
                        block.SetGridPosition(x, writeY);
                        block.BeginMove(board.GridToWorld(x, writeY));
                        movedBlocks.Add(block);
                    }

                    writeY++;
                }
            }

            return movedBlocks;
        }

        private void TrackMovingBlock(Block block)
        {
            if (block == null || movingBlocks.Contains(block))
            {
                return;
            }

            movingBlocks.Add(block);
        }
    }
}
