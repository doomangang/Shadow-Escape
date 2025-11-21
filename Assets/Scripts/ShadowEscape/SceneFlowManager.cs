using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShadowEscape
{
    // 씬/내비게이션 전담 매니저: Title ↔ LevelSelect ↔ Level 흐름 관리
    // GameManager는 저장/진행만 담당하도록 역할 분리
    public class SceneFlowManager : MonoBehaviour
    {
        private static SceneFlowManager _instance;
        public static SceneFlowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneFlowManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SceneFlowManager");
                        _instance = go.AddComponent<SceneFlowManager>();
                    }
                }
                return _instance;
            }
        }

        [Tooltip("레벨 씬 이름 목록 (자동 채움 가능)")] public List<string> levelSceneNames = new List<string>();
        [Tooltip("타이틀 씬 이름")] public string titleSceneName = "00_Title";
        [Tooltip("레벨 선택 씬 이름")] public string levelSelectSceneName = "01_LevelSelect";

        public string CurrentSceneName { get; private set; }
        public int CurrentLevelIndex { get; private set; } = -1;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSceneList();
        }

        public void InitializeSceneList()
        {
            if (levelSceneNames != null && levelSceneNames.Count > 0) return; // 이미 설정됨

            try
            {
                var path = "Assets/Scenes";
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.unity", SearchOption.TopDirectoryOnly);
                    var names = files.Select(Path.GetFileNameWithoutExtension)
                                     .Where(n => !string.IsNullOrEmpty(n))
                                     .OrderBy(n => n)
                                     .ToList();
                    // 타이틀/레벨선택 씬 제외한 퍼즐 레벨만 추출 (규칙: 앞 두 씬 명은 title/level select)
                    levelSceneNames = names.Where(n => n != titleSceneName && n != levelSelectSceneName).ToList();
                    Debug.Log($"[SceneFlowManager] 자동 레벨 목록 채움: {levelSceneNames.Count}개");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Scene list 초기화 실패: " + ex.Message);
            }
        }

        // --- 씬 로드 진입점 ---
        public void LoadTitle()
        {
            CurrentLevelIndex = -1;
            LoadScene(titleSceneName);
        }

        public void LoadLevelSelect()
        {
            CurrentLevelIndex = -1;
            LoadScene(levelSelectSceneName);
        }

        public void LoadLevel(int levelIndex)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager 없음 - 레벨 로드 불가");
                return;
            }
            if (!GameManager.Instance.IsTester && !GameManager.Instance.IsLevelUnlocked(levelIndex))
            {
                Debug.LogWarning($"레벨 {levelIndex} 잠김");
                return;
            }
            if (levelSceneNames == null || levelSceneNames.Count == 0)
            {
                Debug.LogWarning("레벨 씬 목록이 비어 있음");
                return;
            }
            if (levelIndex < 0 || levelIndex >= levelSceneNames.Count)
            {
                Debug.LogWarning("잘못된 레벨 인덱스: " + levelIndex);
                return;
            }
            CurrentLevelIndex = levelIndex;
            LoadScene(levelSceneNames[levelIndex]);
        }

        public void ReloadCurrent()
        {
            if (string.IsNullOrEmpty(CurrentSceneName)) return;
            LoadScene(CurrentSceneName);
        }

        public void ReturnToTitle()
        {
            LoadTitle();
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("sceneName 비어 있음");
                return;
            }
            SceneManager.LoadScene(sceneName);
            CurrentSceneName = sceneName;
        }

        // 레벨 완료 후 후속 처리 (Completion UI 연동 예정)
        public void OnLevelCompleted(int stars)
        {
            if (GameManager.Instance != null && CurrentLevelIndex >= 0)
            {
                GameManager.Instance.CompleteLevel(CurrentLevelIndex, stars);
            }
            // 이후: CompletionUI.Show(...) / 다음 레벨 자동 로드 등 확장 가능
        }
    }
}
