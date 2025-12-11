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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCoreSystems()
        {
            _ = GameManager.Instance;
            _ = SceneFlowManager.Instance;
            // AudioManager는 씬에 배치된 것을 사용하므로 나중에 초기화
            _ = AudioManager.Instance;
        }
    }
}
