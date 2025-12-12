using UnityEngine;

namespace ShadowEscape
{

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
