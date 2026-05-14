using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelController levelController;

        private void Start()
        {
            if (levelController == null)
            {
                Debug.LogError("GameBootstrap is missing a LevelController reference.");
                return;
            }

            levelController.StartLevel();
        }
    }
}
