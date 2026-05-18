using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoveMetro.UI
{
    internal sealed class UnityMenuSceneActions : IMenuSceneActions
    {
        public static readonly UnityMenuSceneActions Instance = new UnityMenuSceneActions();

        private UnityMenuSceneActions()
        {
        }

        public void LoadGameScene(string fallbackSceneName)
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.LoadGameScene();
                return;
            }

            if (!string.IsNullOrEmpty(fallbackSceneName))
            {
                SceneManager.LoadScene(fallbackSceneName);
                return;
            }

            Debug.LogError("MenuManager: game scene name is not configured.");
        }

        public void QuitGame()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.QuitGame();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
