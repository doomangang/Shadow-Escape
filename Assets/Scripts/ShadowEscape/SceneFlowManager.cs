using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using UnityObject = UnityEngine.Object;
using TMPro;

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
    [Tooltip("자동 정렬 시 사용할 난이도 키워드 순서")] [SerializeField]
    private string[] difficultyKeywordOrder = new[] { "easy", "medium", "hard" };
    [Tooltip("난이도 키워드 기반 정렬 강제 적용 여부")] [SerializeField]
    private bool enforceKeywordOrdering = true;
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
                    ApplyPreferredOrdering();
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
                    ApplyPreferredOrdering();
                    Debug.Log($"[SceneFlowManager] 디스크(Assets/Scenes)에서 레벨 목록 채움: {levelSceneNames.Count}개");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Scene list 초기화 실패: " + ex.Message);
            }
        }

        private void ApplyPreferredOrdering()
        {
            if (!enforceKeywordOrdering || levelSceneNames == null || levelSceneNames.Count <= 1)
            {
                return;
            }

            levelSceneNames = levelSceneNames
                .OrderBy(name => GetKeywordSortValue(name))
                .ThenBy(name => name)
                .ToList();
        }

        private int GetKeywordSortValue(string sceneName)
        {
            if (difficultyKeywordOrder == null || difficultyKeywordOrder.Length == 0 || string.IsNullOrEmpty(sceneName))
            {
                return int.MaxValue;
            }

            string lower = sceneName.ToLowerInvariant();
            for (int i = 0; i < difficultyKeywordOrder.Length; i++)
            {
                var keyword = difficultyKeywordOrder[i];
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (lower.Contains(keyword.ToLowerInvariant()))
                {
                    return i;
                }
            }

            return difficultyKeywordOrder.Length;
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
            Debug.Log($"[SceneFlowManager] ReloadCurrent called. CurrentSceneName='{CurrentSceneName}', ActiveScene='{SceneManager.GetActiveScene().name}'");
            if (string.IsNullOrEmpty(CurrentSceneName))
            {
                Debug.LogWarning("[SceneFlowManager] ReloadCurrent: CurrentSceneName is empty, aborting.");
                return;
            }
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
                Debug.LogWarning("[SceneFlowManager] LoadScene: sceneName 비어 있음");
                return;
            }

            Debug.Log($"[SceneFlowManager] Loading scene '{sceneName}'. ActiveScene before load: '{SceneManager.GetActiveScene().name}'");

            // Use explicit Single mode reload to ensure a full scene reload
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            CurrentSceneName = sceneName;
            _completionUI = null;
            AudioManager.Instance?.PlayBGMForScene(sceneName);
            Debug.Log($"[SceneFlowManager] LoadScene call completed for '{sceneName}'. ActiveScene now: '{SceneManager.GetActiveScene().name}'");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneName = scene.name;
            _completionUI = null;
            AudioManager.Instance?.PlayBGMForScene(scene.name);

            // UI 이벤트가 동작하지 않는 케이스 방지: EventSystem 자동 생성
            EnsureEventSystemExists();
        }

        private void EnsureEventSystemExists()
        {
            var systems = UnityObject.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            UnityEngine.EventSystems.EventSystem activeSystem = null;

            if (systems != null && systems.Length > 0)
            {
                activeSystem = SelectPrimaryEventSystem(systems);

                for (int i = 0; i < systems.Length; i++)
                {
                    var candidate = systems[i];
                    if (candidate == null || candidate == activeSystem) continue;

                    Debug.LogWarning($"[SceneFlowManager] Scene '{candidate.gameObject.scene.name}'에서 중복 EventSystem '{candidate.name}'을 제거했습니다.");
                    Destroy(candidate.gameObject);
                }
            }

            if (activeSystem == null)
            {
                activeSystem = CreateEventSystem();
                Debug.Log("[SceneFlowManager] EventSystem이 없어 자동 생성했습니다.");
            }

            EnsureEventSystemHasInputModule(activeSystem);
        }

        private UnityEngine.EventSystems.EventSystem SelectPrimaryEventSystem(UnityEngine.EventSystems.EventSystem[] systems)
        {
            if (systems == null || systems.Length == 0) return null;

            // 선호 순서: DontDestroyOnLoad 씬에 있는 객체 → 나머지 중 첫 번째
            var persistent = systems.FirstOrDefault(s => s != null && s.gameObject.scene.name == "DontDestroyOnLoad");
            var primary = persistent ?? systems.FirstOrDefault(s => s != null);

            if (primary != null && primary.gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(primary.gameObject);
            }

            return primary;
        }

        private UnityEngine.EventSystems.EventSystem CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            DontDestroyOnLoad(go);
            return go.GetComponent<UnityEngine.EventSystems.EventSystem>();
        }

        private void EnsureEventSystemHasInputModule(UnityEngine.EventSystems.EventSystem target)
        {
            if (target == null) return;

            var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.UI")
                                        ?? System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModuleType != null)
            {
                var hasInputSystemModule = target.GetComponent(inputSystemModuleType) != null;
                var legacy = target.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (!hasInputSystemModule)
                {
                    target.gameObject.AddComponent(inputSystemModuleType);
                }
                if (legacy != null)
                {
                    Destroy(legacy);
                }
            }
            else if (target.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
            {
                target.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private CompletionUI GetCompletionUI()
        {
            if (_completionUI == null)
            {
                Debug.Log("[SceneFlowManager] Searching for CompletionUI...");
                _completionUI = UnityObject.FindFirstObjectByType<CompletionUI>(FindObjectsInactive.Include);
                
                if (_completionUI == null && completionUIPrefab != null)
                {
                    Debug.Log("[SceneFlowManager] Instantiating CompletionUI from prefab");
                    _completionUI = Instantiate(completionUIPrefab);
                    DontDestroyOnLoad(_completionUI.gameObject);
                    _completionUI.Hide();
                }
                else if (_completionUI == null)
                {
                    Debug.Log("[SceneFlowManager] Building fallback CompletionUI");
                    _completionUI = BuildFallbackCompletionUI();
                }
                else
                {
                    Debug.Log($"[SceneFlowManager] Found existing CompletionUI: {_completionUI.name}");
                }
            }

            return _completionUI;
        }

        private CompletionUI BuildFallbackCompletionUI()
        {
            // Unity 2023+ uses LegacyRuntime.ttf instead of Arial.ttf
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                // Fallback to Arial for older Unity versions
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

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
            rootRect.sizeDelta = new Vector2(800f, 400f); // 더 크게
            rootRect.anchoredPosition = Vector2.zero;
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.9f); // 더 진하게

            TMP_Text header = CreateTMPText("Header", root.transform, 36, new Vector2(0, 60));
            TMP_Text stars = CreateTMPText("Stars", root.transform, 30, new Vector2(0, 10));

            Button nextButton = CreateButton("NextButton", root.transform, defaultFont, "Next", new Vector2(-150, -70));
            Button retryButton = CreateButton("RetryButton", root.transform, defaultFont, "Retry", new Vector2(0, -70));
            Button menuButton = CreateButton("MenuButton", root.transform, defaultFont, "Menu", new Vector2(150, -70));

            var completion = canvasGO.AddComponent<CompletionUI>();
            completion.AssignReferences(root, header, stars, nextButton, retryButton, menuButton);
            completion.Hide();
            return completion;
        }

        private static TMP_Text CreateTMPText(string name, Transform parent, int fontSize, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(440f, 60f);
            rect.anchoredPosition = anchoredPos;
            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TMPro.TextAlignmentOptions.Center;
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
            Debug.Log($"[SceneFlowManager] OnLevelCompleted called with {stars} stars");
            
            if (CurrentLevelIndex >= 0 && GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(CurrentLevelIndex, stars);
            }

            var completionUI = GetCompletionUI();
            Debug.Log($"[SceneFlowManager] CompletionUI obtained: {(completionUI != null ? completionUI.name : "NULL")}");
            
            if (completionUI != null)
            {
                // 마지막 레벨인지 확인
                // Prefer using the actual scene list length if available, otherwise fall back to GameManager.TotalLevels
                bool isLastLevel = false;
                if (levelSceneNames != null && levelSceneNames.Count > 0)
                {
                    isLastLevel = (CurrentLevelIndex >= levelSceneNames.Count - 1);
                }
                else if (GameManager.Instance != null)
                {
                    isLastLevel = (CurrentLevelIndex >= GameManager.Instance.TotalLevels - 1);
                }
                
                Debug.Log($"[SceneFlowManager] Calling completionUI.Show() - CurrentLevel={CurrentLevelIndex}, IsLastLevel={isLastLevel}");
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
                    },
                    () =>
                    {
                        completionUI.Hide();
                        LoadLevelSelect();
                    },
                    showNextButton: !isLastLevel); // 마지막 레벨이면 Next 버튼 숨김
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
