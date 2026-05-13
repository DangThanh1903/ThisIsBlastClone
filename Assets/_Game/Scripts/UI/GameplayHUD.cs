using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        [SerializeField] private string levelLabel = "Level 1";

        private bool showWin;
        private int remainingBlockCount;

        public void ShowLevel(int levelNumber)
        {
            levelLabel = $"Level {levelNumber}";
        }

        public void SetRemainingBlockCount(int count)
        {
            remainingBlockCount = Mathf.Max(0, count);
        }

        public void ShowWin()
        {
            showWin = true;
        }

        public void HideWin()
        {
            showWin = false;
        }

        private void OnGUI()
        {
            const int margin = 20;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(margin, margin, 220, 40), levelLabel, labelStyle);
            GUI.Label(new Rect(margin, margin + 38, 260, 40), $"Blocks: {remainingBlockCount}", labelStyle);

            if (!showWin)
            {
                return;
            }

            GUIStyle winStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 52,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 90), "WIN", winStyle);
        }
    }
}
