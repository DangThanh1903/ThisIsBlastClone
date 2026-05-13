using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BoardMatcher : MonoBehaviour
    {
        private static readonly GridPosition[] Directions =
        {
            new GridPosition(1, 0),
            new GridPosition(-1, 0),
            new GridPosition(0, 1),
            new GridPosition(0, -1)
        };

        public List<Block> GetConnectedBlocks(BoardController board, int startX, int startY, BlockColor color)
        {
            List<Block> result = new List<Block>();

            if (board == null || !board.IsInsideGrid(startX, startY))
            {
                return result;
            }

            Block startBlock = board.GetBlock(startX, startY);
            if (startBlock == null || startBlock.Color != color)
            {
                return result;
            }

            bool[,] visited = new bool[board.Width, board.Height];
            Queue<GridPosition> queue = new Queue<GridPosition>();

            visited[startX, startY] = true;
            queue.Enqueue(new GridPosition(startX, startY));

            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();
                Block block = board.GetBlock(current.X, current.Y);

                if (block == null || block.Color != color)
                {
                    continue;
                }

                result.Add(block);

                foreach (GridPosition direction in Directions)
                {
                    int nextX = current.X + direction.X;
                    int nextY = current.Y + direction.Y;

                    if (!board.IsInsideGrid(nextX, nextY) || visited[nextX, nextY])
                    {
                        continue;
                    }

                    Block neighbor = board.GetBlock(nextX, nextY);
                    if (neighbor == null || neighbor.Color != color)
                    {
                        continue;
                    }

                    visited[nextX, nextY] = true;
                    queue.Enqueue(new GridPosition(nextX, nextY));
                }
            }

            return result;
        }
    }
}
