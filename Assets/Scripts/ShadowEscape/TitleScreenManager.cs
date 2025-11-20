using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowEscape
{
    // Title 화면에서 버튼에 연결될 로직 담당
    // 예상 UI 요소:
    //  - Start Game 버튼
    //  - Tester 모드 토글 (IsTester)
    //  - Reset Save 버튼
    //  - Quit 버튼
    public class TitleScreenManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [Tooltip("Level Select 씬 이름")] public string levelSelectSceneName = "01_LevelSelect";

        private void Awake()
        {
            _ = GameManager.Instance;
        }

        public void StartGame()
        {
            if (string.IsNullOrEmpty(levelSelectSceneName))
            {
                Debug.LogWarning("LevelSelect 씬 이름이 설정되지 않았습니다.");
                return;
            }
            GameManager.Instance.LoadScene(levelSelectSceneName);
        }

        public void SetTesterMode(bool isTester)
        {
            GameManager.Instance.IsTester = isTester;
            Debug.Log($"Tester Mode set to: {isTester}");
        }

        public void ResetProgress()
        {
            GameManager.Instance.ResetSaveData();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
