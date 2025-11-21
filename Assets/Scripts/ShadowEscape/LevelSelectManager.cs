using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadowEscape
{
    // 레벨 선택 화면 로직
    // UI 버튼은 LoadLevel(index)를 호출하도록 설정
    // 잠긴 레벨은 interactable=false 또는 별도 표시를 통해 처리 예정
    public class LevelSelectManager : MonoBehaviour
    {
        [Header("Level Scenes")]
        [Tooltip("플레이 가능한 레벨 씬 이름 목록. 인덱스 = levelIndex")] public List<string> levelSceneNames = new List<string>();

        [Header("Optional Auto Build")]
        [Tooltip("씬 로드시 잠금 여부 검사")]
        public bool enforceLockState = true;

        private void Awake()
        {
            _ = GameManager.Instance; // Ensure GameManager exists

            // If no scene names supplied in inspector, try to auto-populate from Assets/Scenes
            if (levelSceneNames == null || levelSceneNames.Count == 0)
            {
                try
                {
                    var sceneFiles = Directory.Exists("Assets/Scenes")
                        ? Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly)
                        : new string[0];

                    var names = sceneFiles
                        .Select(Path.GetFileNameWithoutExtension)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .OrderBy(n => n)
                        .ToList();

                    if (names.Count > 0)
                    {
                        levelSceneNames = names;
                        Debug.Log($"[LevelSelectManager] Auto-populated {names.Count} level scene names from Assets/Scenes.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Failed to auto-populate levelSceneNames: " + ex.Message);
                }
            }
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (GameManager.Instance == null) return false;
            return GameManager.Instance.IsLevelUnlocked(levelIndex);
        }

        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levelSceneNames.Count)
            {
                Debug.LogWarning($"잘못된 레벨 인덱스: {levelIndex}");
                return;
            }

            if (enforceLockState && !IsLevelUnlocked(levelIndex))
            {
                Debug.LogWarning($"레벨 {levelIndex} 은(는) 잠겨 있습니다.");
                return;
            }

            string sceneName = levelSceneNames[levelIndex];
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"레벨 {levelIndex} 의 씬 이름이 비어있습니다.");
                return;
            }

            GameManager.Instance.LoadScene(sceneName);
        }

        // 에디터에서 테스트용으로 현재 씬을 재시작할 수 있는 헬퍼
        public void RestartCurrent()
        {
            var active = SceneManager.GetActiveScene();
            GameManager.Instance.LoadScene(active.name);
        }
    }
}
