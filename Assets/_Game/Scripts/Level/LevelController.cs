using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private LevelData levelData;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BlastItemSpawner blastItemSpawner;
        [SerializeField] private GameplayHUD gameplayHUD;

        public GameState State { get; private set; } = GameState.NotStarted;

        public void StartLevel()
        {
            if (State != GameState.NotStarted)
            {
                return;
            }

            ResolveDependencies();

            if (levelData == null || boardController == null)
            {
                Debug.LogError("LevelController is missing LevelData or BoardController.");
                return;
            }

            State = GameState.Playing;
            boardController.SpawnBoard(levelData);
            blastItemSpawner?.SpawnItems(levelData.StartingBlastItems);

            if (gameplayHUD != null)
            {
                levelNumber = levelData.LevelNumber;
                gameplayHUD.ShowLevel(levelNumber);
                gameplayHUD.SetRemainingBlockCount(boardController.CountBlocks());
                gameplayHUD.HideWin();
            }
        }

        public void CheckWinCondition()
        {
            if (State != GameState.Playing || boardController == null)
            {
                return;
            }

            int remainingBlocks = boardController.CountBlocks();
            gameplayHUD?.SetRemainingBlockCount(remainingBlocks);

            if (!IsWinConditionMet())
            {
                return;
            }

            State = GameState.Won;
            gameplayHUD?.ShowWin();
        }

        public void LoseLevel()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            State = GameState.Lost;
            gameplayHUD?.ShowLose();
        }

        private bool IsWinConditionMet()
        {
            if (levelData == null)
            {
                return false;
            }

            switch (levelData.WinCondition)
            {
                case WinConditionType.ClearAllBlocks:
                    return boardController.IsCleared();
                default:
                    return false;
            }
        }

        private void ResolveDependencies()
        {
            if (boardController == null)
            {
                boardController = FindFirstObjectByType<BoardController>();
            }

            if (blastItemSpawner == null)
            {
                blastItemSpawner = FindFirstObjectByType<BlastItemSpawner>();
            }

            if (gameplayHUD == null)
            {
                gameplayHUD = FindFirstObjectByType<GameplayHUD>();
            }
        }
    }
}
