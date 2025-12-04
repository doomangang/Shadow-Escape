using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using UnityObject = UnityEngine.Object;

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
                    _instance = UnityObject.FindFirstObjectByType<SceneFlowManager>(FindObjectsInactive.Exclude);
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

    [Header("UI References")]
    [Tooltip("Optional prefab used when a CompletionUI cannot be found in the loaded scene. Will be instantiated once and persisted across scenes.")]
    [SerializeField] private CompletionUI completionUIPrefab;

    private CompletionUI _completionUI;

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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        public void InitializeSceneList()
        {
            if (levelSceneNames != null && levelSceneNames.Count > 0) return; // 이미 설정됨
            try
            {
                var names = new List<string>();

                // 우선 BuildSettings에 등록된 씬을 사용 (빌드 환경에서 안전)
                int buildCount = SceneManager.sceneCountInBuildSettings;
                if (buildCount > 0)
                {
                    for (int i = 0; i < buildCount; i++)
                    {
                        var path = SceneUtility.GetScenePathByBuildIndex(i);
                        if (string.IsNullOrEmpty(path)) continue;
                        var name = Path.GetFileNameWithoutExtension(path);
                        if (!string.IsNullOrEmpty(name)) names.Add(name);
                    }

                    // 타이틀/레벨선택 씬 제외한 퍼즐 레벨만 추출
                    levelSceneNames = names.Where(n => n != titleSceneName && n != levelSelectSceneName).ToList();
                    Debug.Log($"[SceneFlowManager] 빌드 설정에서 레벨 목록 채움: {levelSceneNames.Count}개 (BuildSettings count={buildCount})");
                    return;
                }

                // 폴백: (에디터에서) Assets/Scenes 폴더를 읽어 자동 채움
                var pathDir = "Assets/Scenes";
                if (Directory.Exists(pathDir))
                {
                    var files = Directory.GetFiles(pathDir, "*.unity", SearchOption.TopDirectoryOnly);
                    var fileNames = files.Select(Path.GetFileNameWithoutExtension)
                                         .Where(n => !string.IsNullOrEmpty(n))
                                         .OrderBy(n => n)
                                         .ToList();
                    levelSceneNames = fileNames.Where(n => n != titleSceneName && n != levelSelectSceneName).ToList();
                    Debug.Log($"[SceneFlowManager] 디스크(Assets/Scenes)에서 레벨 목록 채움: {levelSceneNames.Count}개");
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
            _completionUI = null;
            AudioManager.Instance?.PlayBGMForScene(sceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneName = scene.name;
            _completionUI = null;
            AudioManager.Instance?.PlayBGMForScene(scene.name);
        }

        private CompletionUI GetCompletionUI()
        {
            if (_completionUI == null)
            {
                _completionUI = UnityObject.FindFirstObjectByType<CompletionUI>(FindObjectsInactive.Include);
                if (_completionUI == null && completionUIPrefab != null)
                {
                    _completionUI = Instantiate(completionUIPrefab);
                    DontDestroyOnLoad(_completionUI.gameObject);
                    _completionUI.Hide();
                }
                else if (_completionUI == null)
                {
                    _completionUI = BuildFallbackCompletionUI();
                }
            }

            return _completionUI;
        }

        private CompletionUI BuildFallbackCompletionUI()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGO = new GameObject("RuntimeCompletionUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasGO);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var root = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(500f, 260f);
            rootRect.anchoredPosition = Vector2.zero;
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.7f);

            Text header = CreateText("Header", root.transform, defaultFont, 36, new Vector2(0, 60));
            Text stars = CreateText("Stars", root.transform, defaultFont, 30, new Vector2(0, 10));

            Button nextButton = CreateButton("NextButton", root.transform, defaultFont, "Next", new Vector2(-100, -70));
            Button retryButton = CreateButton("RetryButton", root.transform, defaultFont, "Retry", new Vector2(100, -70));

            var completion = canvasGO.AddComponent<CompletionUI>();
            completion.AssignReferences(root, header, stars, nextButton, retryButton);
            completion.Hide();
            return completion;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(440f, 60f);
            rect.anchoredPosition = anchoredPos;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 50f);
            rect.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var rectText = textGO.GetComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.offsetMin = Vector2.zero;
            rectText.offsetMax = Vector2.zero;
            var text = textGO.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return go.GetComponent<Button>();
        }

        // 레벨 완료 후 후속 처리 (Completion UI 연동)
        public void OnLevelCompleted(int stars)
        {
            if (CurrentLevelIndex >= 0 && GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(CurrentLevelIndex, stars);
            }

            var completionUI = GetCompletionUI();
            if (completionUI != null)
            {
                completionUI.Show(
                    stars,
                    () =>
                    {
                        completionUI.Hide();
                        HandleCompletionNext();
                    },
                    () =>
                    {
                        completionUI.Hide();
                        HandleCompletionRetry();
                    });
            }
            else
            {
                Debug.LogWarning("[SceneFlowManager] CompletionUI not found. Returning to Level Select by default.");
                HandleCompletionNext();
            }
        }

        private void HandleCompletionNext()
        {
            int nextIndex = CurrentLevelIndex + 1;
            if (levelSceneNames != null && nextIndex >= 0 && nextIndex < levelSceneNames.Count)
            {
                LoadLevel(nextIndex);
            }
            else
            {
                LoadLevelSelect();
            }
        }

        private void HandleCompletionRetry()
        {
            ReloadCurrent();
        }
    }
}
