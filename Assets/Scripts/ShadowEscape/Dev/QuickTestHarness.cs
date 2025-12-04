using UnityEngine;

namespace ShadowEscape
{
    /// <summary>
    /// Small runtime test harness to exercise common flows without building UI.
    /// Attach to any GameObject in a scene (for example Title scene) and press the
    /// mapped keys in Play mode to trigger actions.
    ///
    /// Keys:
    /// F5 - Load configured test level (testLevelIndex)
    /// F6 - Simulate current level completion (uses starsToGrant if available)
    /// F7 - Toggle Tester Mode (GameManager.IsTester)
    /// F8 - Toggle Pause (GameManager.SetPaused)
    /// F9 - Reload current scene
    /// </summary>
    public class QuickTestHarness : MonoBehaviour
    {
        [Tooltip("Level index used when pressing F5 to load a test level")]
        public int testLevelIndex = 0;

        [Tooltip("Stars to grant when simulating completion with F6")]
        public int testStars = 1;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (SceneFlowManager.Instance != null)
                {
                    Debug.Log($"[QuickTestHarness] Loading level {testLevelIndex}");
                    SceneFlowManager.Instance.LoadLevel(testLevelIndex);
                }
                else Debug.LogWarning("SceneFlowManager not found");
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("[QuickTestHarness] Simulating level completion");
                if (SceneFlowManager.Instance != null)
                {
                    SceneFlowManager.Instance.OnLevelCompleted(testStars);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.CompleteLevel(testLevelIndex, testStars);
                }
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.IsTester = !GameManager.Instance.IsTester;
                    Debug.Log($"[QuickTestHarness] Tester mode = {GameManager.Instance.IsTester}");
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPaused(!GameManager.Instance.IsPaused);
                    Debug.Log($"[QuickTestHarness] Pause toggled = {GameManager.Instance.IsPaused}");
                }
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (SceneFlowManager.Instance != null)
                {
                    SceneFlowManager.Instance.ReloadCurrent();
                    Debug.Log("[QuickTestHarness] Reload current scene");
                }
            }
        }
    }
}
