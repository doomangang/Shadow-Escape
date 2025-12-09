using UnityEngine;

namespace ShadowEscape
{
    /// <summary>
    /// Ensures critical singleton managers are alive before the first scene loads so that
    /// Audio/Pause/UI flows described in the notes exist even if the scene forgets to
    /// place the prefabs manually.
    /// </summary>
    public static class CoreBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureCoreSystems()
        {
            _ = GameManager.Instance;
            _ = SceneFlowManager.Instance;
            _ = AudioManager.Instance;
            EnsurePauseMenu();
        }

        private static void EnsurePauseMenu()
        {
            var existing = Object.FindFirstObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return;
            }

            var pauseRoot = new GameObject("RuntimePauseMenu");
            Object.DontDestroyOnLoad(pauseRoot);
            pauseRoot.AddComponent<PauseMenuBootstrap>();
        }
    }
}
