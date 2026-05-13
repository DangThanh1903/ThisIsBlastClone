using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BoardGravity : MonoBehaviour
    {
        public void ApplyGravity(BoardController board)
        {
            if (board == null)
            {
                return;
            }

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
                        block.transform.position = board.GridToWorld(x, writeY);
                    }

                    writeY++;
                }
            }
        }
    }
}
