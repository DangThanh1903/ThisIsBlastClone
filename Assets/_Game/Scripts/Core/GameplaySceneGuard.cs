using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThisIsBlast.Gameplay
{
    public static class GameplaySceneGuard
    {
        private const string GameplaySceneName = "Gameplay";
        private const string EmptyGameSceneName = "Game";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameplaySceneWhenPlaying()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (Object.FindFirstObjectByType<LevelController>() != null)
            {
                Debug.Log($"Gameplay scene ready: {activeScene.path}");
                return;
            }

            if (activeScene.name != EmptyGameSceneName)
            {
                Debug.LogWarning($"No LevelController found in active scene '{activeScene.name}'. Open Assets/_Game/Scenes/Gameplay.unity to run the prototype.");
                return;
            }

            Debug.LogWarning("Assets/_Game/Scenes/Game.unity has no gameplay setup. Loading Assets/_Game/Scenes/Gameplay.unity instead.");
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
