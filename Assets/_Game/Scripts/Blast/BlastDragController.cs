using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class BlastDragController : MonoBehaviour
    {
        [SerializeField] private BlastItem blastItem;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BoardMatcher boardMatcher;
        [SerializeField] private BoardGravity boardGravity;
        [SerializeField] private LevelController levelController;

        private Camera mainCamera;
        private Vector3 startPosition;
        private Vector3 dragOffset;
        private bool isDragging;

        private void Awake()
        {
            if (blastItem == null)
            {
                blastItem = GetComponent<BlastItem>();
            }

            ResolveDependencies();
            mainCamera = Camera.main;
        }

        public void Configure(
            BoardController board,
            BoardMatcher matcher,
            BoardGravity gravity,
            LevelController level)
        {
            boardController = board;
            boardMatcher = matcher;
            boardGravity = gravity;
            levelController = level;
        }

        private void OnMouseDown()
        {
            if (!CanDrag())
            {
                return;
            }

            isDragging = true;
            startPosition = transform.position;

            Vector3 pointerWorldPosition = CameraUtils.GetPointerWorldPositionOnPlane(mainCamera, boardController.BoardPlaneY);
            dragOffset = transform.position - pointerWorldPosition;
        }

        private void OnMouseDrag()
        {
            if (!isDragging)
            {
                return;
            }

            Vector3 pointerWorldPosition = CameraUtils.GetPointerWorldPositionOnPlane(mainCamera, boardController.BoardPlaneY);
            Vector3 targetPosition = pointerWorldPosition + dragOffset;
            targetPosition.y = startPosition.y;
            transform.position = targetPosition;
        }

        private void OnMouseUp()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            TryDropOnBoard();
        }

        private bool CanDrag()
        {
            ResolveDependencies();
            return blastItem != null
                && boardController != null
                && boardMatcher != null
                && boardGravity != null
                && levelController != null
                && levelController.State == GameState.Playing;
        }

        private void TryDropOnBoard()
        {
            GridPosition gridPosition = boardController.WorldToGrid(transform.position);
            if (!boardController.IsInsideGrid(gridPosition))
            {
                ResetToStartPosition();
                return;
            }

            Block targetBlock = boardController.GetBlock(gridPosition.X, gridPosition.Y);
            if (targetBlock == null || targetBlock.Color != blastItem.Color)
            {
                ResetToStartPosition();
                return;
            }

            List<Block> connectedBlocks = boardMatcher.GetConnectedBlocks(
                boardController,
                gridPosition.X,
                gridPosition.Y,
                blastItem.Color);

            if (connectedBlocks.Count == 0)
            {
                ResetToStartPosition();
                return;
            }

            boardController.RemoveBlocks(connectedBlocks);
            boardGravity.ApplyGravity(boardController);
            levelController.CheckWinCondition();
            Destroy(gameObject);
        }

        private void ResetToStartPosition()
        {
            transform.position = startPosition;
        }

        private void ResolveDependencies()
        {
            if (boardController == null)
            {
                boardController = FindFirstObjectByType<BoardController>();
            }

            if (boardMatcher == null)
            {
                boardMatcher = FindFirstObjectByType<BoardMatcher>();
            }

            if (boardGravity == null)
            {
                boardGravity = FindFirstObjectByType<BoardGravity>();
            }

            if (levelController == null)
            {
                levelController = FindFirstObjectByType<LevelController>();
            }
        }
    }
}
