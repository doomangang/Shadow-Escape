using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
// Simplified dual input handling via compile-time directives instead of reflection.

namespace ShadowEscape
{
    // 씬별 퍼즐 관리자
    // 입력 처리(LMB 드래그: 회전, Shift+LMB 드래그: 이동)와 완료 판정 담당
    public class LevelManager : MonoBehaviour
    {
        [System.Serializable]
        public struct PieceTargetPair
        {
            public Piece piece;
            public TargetPiece target;
        }

        public List<PieceTargetPair> pairs = new List<PieceTargetPair>();

    private Camera mainCamera;
    private Piece selectedPiece;
    private Vector3 lastMousePos;

    [Header("Level Progression")]
    [Tooltip("Index used when reporting completion to GameManager.")]
    public int levelIndex = 0;
    private bool levelCompletionTriggered = false;
    
    [Header("Star Grading (Time-based) - Difficulty Scaling")]
    [Tooltip("Use difficulty-based time limits instead of manual settings")]
    [SerializeField] private bool useDifficultyBasedTime = true;
    
    [Header("Manual Time Settings (if not using difficulty-based)")]
    [Tooltip("Time limit for 3 stars (fast clear)")]
    [SerializeField] private float time3Stars = 30f;
    [Tooltip("Time limit for 2 stars (normal clear)")]
    [SerializeField] private float time2Stars = 60f;
    // 1 star: any time over time2Stars
    
    private float levelStartTime;
    private float _calculatedTime3Stars;
    private float _calculatedTime2Stars;

    [Header("Debug / UI")]
    [Tooltip("Show debug overlay in Editor. Disabled by default for grading builds.")]
    [SerializeField] private bool showDebugOverlay = false;
    private string completionMessage;
    private float completionMessageTime;
    public float completionMessageDuration = 3f;

        // 감도 설정
        public float rotateSpeed = 15f;
        public float moveSpeed = 0.01f;

    [Header("Input / Validation")]
    [Tooltip("Minimum interval (seconds) between automatic validation runs; validation is also run when interaction ends")]
    [SerializeField] private float validationInterval = 0.1f;
    private float lastValidationTime = 0f;

    [Header("UI Hooks")]
    [SerializeField] private LevelHintDisplay hintDisplay;
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private UI.HintUI hintUI;

    [Header("Visual Feedback")]
    [Tooltip("Spotlight that changes color based on accuracy (red→yellow→green)")]
    [SerializeField] private Light feedbackSpotlight;
    [Tooltip("Enable color feedback on spotlight")]
    [SerializeField] private bool enableSpotlightFeedback = true;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color partialColor = Color.yellow;
    [SerializeField] private Color correctColor = Color.green;

    private LevelMetadata _metadata; // 난이도/힌트 데이터
    private DifficultyTier _effectiveDifficulty = DifficultyTier.Hard; // 기본값 (제약 없음)

        private void Awake()
        {
            if (hintDisplay == null)
            {
                hintDisplay = GetComponent<LevelHintDisplay>() ?? GetComponentInChildren<LevelHintDisplay>(true);
            }

            // Spotlight 자동 탐색
            if (feedbackSpotlight == null && enableSpotlightFeedback)
            {
                feedbackSpotlight = UnityObject.FindFirstObjectByType<Light>();
                if (feedbackSpotlight != null)
                {
                    Debug.Log($"[LevelManager] Auto-found spotlight: {feedbackSpotlight.name}");
                }
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;

            if (hintDisplay == null)
            {
                hintDisplay = UnityObject.FindFirstObjectByType<LevelHintDisplay>(FindObjectsInactive.Include);
            }

            // HintUI 자동 탐색
            if (hintUI == null)
            {
                hintUI = UnityObject.FindFirstObjectByType<UI.HintUI>(FindObjectsInactive.Include);
            }

            // LevelMetadata 자동 탐색
            _metadata = UnityObject.FindFirstObjectByType<LevelMetadata>(FindObjectsInactive.Exclude);
            if (_metadata != null)
            {
                ApplyMetadata(_metadata);
            }
            else
            {
                Debug.LogWarning("[LevelManager] LevelMetadata 없음 - Hard로 간주");
                hintDisplay?.ClearHint();
            }

            // 자동 수집: 페어가 비어있을 때는 씬의 Piece/Target을 찾아 자동으로 매칭 시도
            if (pairs.Count == 0)
            {
                var scenePieces = UnityObject.FindObjectsByType<Piece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var sceneTargets = UnityObject.FindObjectsByType<TargetPiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                int count = Mathf.Min(scenePieces.Length, sceneTargets.Length);
                for (int i = 0; i < count; i++)
                {
                    pairs.Add(new PieceTargetPair { piece = scenePieces[i], target = sceneTargets[i] });
                }
            }

            // 난이도별 시간 계산
            CalculateTimeLimits();
            
            // 게임 시작 전 힌트 UI 표시
            ShowHintUI();
        }

        private void ApplyMetadata(LevelMetadata metadata)
        {
            _effectiveDifficulty = metadata.difficulty;
            levelIndex = metadata.levelIndex;
            Debug.Log($"[LevelManager] Metadata applied (difficulty={_effectiveDifficulty}, levelIndex={levelIndex})");

            InjectUnifiedTolerances(metadata.positionTolerance, metadata.rotationTolerance);
            
            // 난이도 적용 후 시간 재계산
            CalculateTimeLimits();

            if (!string.IsNullOrWhiteSpace(metadata.titleHint))
            {
                hintDisplay?.ShowHint(metadata.titleHint);
            }
            else
            {
                hintDisplay?.ClearHint();
            }
        }

        // 모든 TargetPiece에 메타데이터 tolerance 주입 (씬 내 editor 값 무시, 중앙관리)
        private void InjectUnifiedTolerances(float posTol, float rotTol)
        {
            int updated = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].target != null)
                {
                    pairs[i].target.positionTolerance = posTol;
                    pairs[i].target.rotationTolerance = rotTol;
                    updated++;
                }
            }

            // 자동 수집된 pair 외 추가 TargetPiece (만약 존재)도 커버
            var allTargets = UnityObject.FindObjectsByType<TargetPiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in allTargets)
            {
                // 위에서 이미 설정된 경우라도 재설정 무해; 단순 동기화
                t.positionTolerance = posTol;
                t.rotationTolerance = rotTol;
            }

            Debug.Log($"[LevelManager] Injected unified tolerances (pos={posTol:F3}, rot={rotTol:F1}) into {allTargets.Length} TargetPiece(s). (pairs updated={updated})");
        }

        private void Update()
        {
            // ESC 키로 Pause 토글
            if (GetEscapeKeyDown())
            {
                if (pauseMenuManager != null)
                {
                    pauseMenuManager.ToggleMenu();
                }
                else
                {
                    Debug.LogWarning("[LevelManager] PauseMenuManager not assigned in Inspector!");
                }
            }

            // Pause 중에는 입력/검증을 중단
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }
            HandleInput();

            // Periodic validation to avoid running a heavy check every frame.
            if (Time.time - lastValidationTime >= validationInterval)
            {
                ValidateAllPieces();
                lastValidationTime = Time.time;
            }
        }

        private bool GetEscapeKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void HandleInput()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }

            // 레벨 완료 후 입력 차단
            if (levelCompletionTriggered)
            {
                return;
            }

            // LMB 클릭 시작 - 조각 선택
            if (GetMouseButtonDown(0))
            {
                lastMousePos = GetMousePosition();
                TrySelectPieceUnderMouse();
            }

            // LMB 드래그 중
            if (GetMouseButton(0) && selectedPiece != null)
            {
                Vector3 mouseDelta = GetMousePosition() - lastMousePos;

                // 난이도별 허용 동작 결정
                bool allowMove = _effectiveDifficulty == DifficultyTier.Hard;
                bool allowVertical = _effectiveDifficulty == DifficultyTier.Medium || _effectiveDifficulty == DifficultyTier.Hard;

                // Subject 입력 매핑: Click=horizontal, Ctrl+Click=vertical, Shift+Click=move
                if (IsShiftPressed() && allowMove)
                {
                    // Shift + LMB = 이동 (Hard만)
                    Vector3 rightMove = mainCamera.transform.right * mouseDelta.x * moveSpeed;
                    Vector3 upMove = mainCamera.transform.up * mouseDelta.y * moveSpeed;
                    selectedPiece.Move(rightMove + upMove);
                }
                else if (IsCtrlPressed() && allowVertical)
                {
                    // Ctrl + LMB = 수직 회전 (Medium, Hard)
                    float ry = -1 * mouseDelta.y * rotateSpeed * Time.deltaTime;
                    selectedPiece.Rotate(0f, ry);
                }
                else
                {
                    // LMB = 수평 회전 (모든 난이도)
                    float rx = -1 * mouseDelta.x * rotateSpeed * Time.deltaTime;
                    selectedPiece.Rotate(rx, 0f);
                }

                lastMousePos = GetMousePosition();
            }

            // LMB 버튼 뗌
            if (GetMouseButtonUp(0))
            {
                selectedPiece = null;
                ValidateAllPieces();
                lastValidationTime = Time.time;
            }
        }

        private bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (button == 0)
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return false;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        private bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (button == 0)
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
            return false;
#else
            return Input.GetMouseButton(button);
#endif
        }

        private bool GetMouseButtonUp(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (button == 0)
                return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            return false;
#else
            return Input.GetMouseButtonUp(button);
#endif
        }

        private Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var pos = mouse.position.ReadValue();
                return new Vector3(pos.x, pos.y, 0f);
            }
            return Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        private bool IsShiftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

    private bool IsCtrlPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
    }

        private void TrySelectPieceUnderMouse()
        {
            if (mainCamera == null) return;
            // Use the input abstraction to support both input systems
            Vector3 mousePos = GetMousePosition();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Piece p = hit.collider.GetComponentInParent<Piece>();
                if (p != null)
                {
                    selectedPiece = p;
                }
            }
        }

        // 모든 조각을 검사해서 IsCorrect 플래그를 업데이트
        private void ValidateAllPieces()
        {
            bool allCorrect = true;
            float totalAccuracy = 0f;
            int validPairs = 0;

            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair.piece == null || pair.target == null) { allCorrect = false; continue; }
                bool wasCorrect = pair.piece.IsCorrect;
                bool nowCorrect = pair.target.CheckIsCorrect(pair.piece.transform);
                pair.piece.IsCorrect = nowCorrect;
                if (!nowCorrect) allCorrect = false;

                // 정확도 누적 (Spotlight 피드백용)
                // Hard 난이도만 위치 포함, Easy/Medium은 회전만
                bool includePosition = (_effectiveDifficulty == DifficultyTier.Hard);
                totalAccuracy += pair.target.CalculateAccuracy(pair.piece.transform, includePosition);
                validPairs++;

                if (!wasCorrect && nowCorrect)
                {
                    float posDist = Vector3.Distance(pair.piece.transform.position, pair.target.transform.position);
                    float rotAngle = Quaternion.Angle(pair.piece.transform.rotation, pair.target.transform.rotation);
                    Debug.Log(
                        $"[LevelManager] Piece matched: {pair.piece.name} -> Target {pair.target.name}\n" +
                        $"Distance={posDist:F4} (tol {pair.target.positionTolerance:F4}), Angle={rotAngle:F2} (tol {pair.target.rotationTolerance:F2})\n" +
                        $"PiecePos={pair.piece.transform.position:F3} TargetPos={pair.target.transform.position:F3}\n" +
                        $"PieceRot={pair.piece.transform.rotation.eulerAngles:F1} TargetRot={pair.target.transform.rotation.eulerAngles:F1}");
                }
            }

            // Spotlight 색상 피드백 업데이트
            if (enableSpotlightFeedback && feedbackSpotlight != null && validPairs > 0)
            {
                float avgAccuracy = totalAccuracy / validPairs;
                UpdateSpotlightColor(avgAccuracy);
            }

            if (allCorrect && !levelCompletionTriggered)
            {
                levelCompletionTriggered = true;
                
                // 클리어 시간 계산 및 별 등급 결정
                float clearTime = Time.time - levelStartTime;
                int starsEarned = CalculateStars(clearTime);
                
                completionMessage = $"Level Complete! {starsEarned}⭐";
                completionMessageTime = Time.time;
                
                Debug.Log($"[LevelManager] All pieces correct — blocking input and waiting 1 second before showing completion UI (levelIndex={levelIndex}, stars={starsEarned}, time={clearTime:F1}s).");
                StartCoroutine(ShowCompletionUIAfterDelay(starsEarned, 1f));
            }
        }

        private void CalculateTimeLimits()
        {
            if (useDifficultyBasedTime)
            {
                // 난이도별 시간 설정
                // Easy: 1분 (30초/1분), Medium: 1분30초 (45초/1분30초), Hard: 2분 (60초/2분)
                switch (_effectiveDifficulty)
                {
                    case DifficultyTier.Easy:
                        _calculatedTime3Stars = 30f;
                        _calculatedTime2Stars = 60f;
                        break;
                    case DifficultyTier.Medium:
                        _calculatedTime3Stars = 45f;
                        _calculatedTime2Stars = 90f;
                        break;
                    case DifficultyTier.Hard:
                        _calculatedTime3Stars = 60f;
                        _calculatedTime2Stars = 120f;
                        break;
                    default:
                        _calculatedTime3Stars = time3Stars;
                        _calculatedTime2Stars = time2Stars;
                        break;
                }
                Debug.Log($"[LevelManager] Time limits set for {_effectiveDifficulty}: 3⭐={_calculatedTime3Stars}s, 2⭐={_calculatedTime2Stars}s");
            }
            else
            {
                // 수동 설정 사용
                _calculatedTime3Stars = time3Stars;
                _calculatedTime2Stars = time2Stars;
            }
        }

        // Spotlight 색상 업데이트 (정확도 기반: 0~1)
        private void UpdateSpotlightColor(float accuracy)
        {
            if (feedbackSpotlight == null) return;

            Color targetColor;
            
            if (accuracy < 0.5f)
            {
                // 0~0.5: 빨강 → 노랑
                targetColor = Color.Lerp(incorrectColor, partialColor, accuracy * 2f);
            }
            else
            {
                // 0.5~1: 노랑 → 초록
                targetColor = Color.Lerp(partialColor, correctColor, (accuracy - 0.5f) * 2f);
            }

            feedbackSpotlight.color = targetColor;
        }

        private int CalculateStars(float clearTime)
        {
            if (clearTime <= _calculatedTime3Stars)
                return 3;
            else if (clearTime <= _calculatedTime2Stars)
                return 2;
            else
                return 1;
        }

        private System.Collections.IEnumerator ShowCompletionUIAfterDelay(int starsEarned, float delay)
        {
            yield return new WaitForSeconds(delay);
            HandleLevelCompletion(starsEarned);
        }

        private void ShowHintUI()
        {
            if (hintUI == null)
            {
                Debug.LogWarning("[LevelManager] HintUI not found, starting game immediately.");
                StartGame();
                return;
            }

            // 게임 일시정지 (힌트 UI 표시 중)
            Time.timeScale = 0f;

            // 레벨 정보 수집
            string hint = _metadata != null ? _metadata.titleHint : "Find the shadow!";
            string levelName = _metadata != null ? $"Level {_metadata.levelIndex + 1} - {_effectiveDifficulty}" : $"Level {levelIndex}";
            
            // 조작 방법 안내 (난이도별)
            string guide = GetControlGuide();

            // 힌트 UI 표시 (3초 후 자동 사라짐)
            hintUI.Show(hint, _calculatedTime3Stars, levelName, guide, 3f, StartGame);
        }

        private string GetControlGuide()
        {
            switch (_effectiveDifficulty)
            {
                case DifficultyTier.Easy:
                    return "Controls: Click & Drag - Horizontal Rotation";
                
                case DifficultyTier.Medium:
                    return "Controls:\n• Click & Drag - Horizontal Rotation\n• Ctrl + Drag - Vertical Rotation";
                
                case DifficultyTier.Hard:
                    return "Controls:\n• Click & Drag - Horizontal Rotation\n• Ctrl + Drag - Vertical Rotation\n• Shift + Drag - Move";
                
                default:
                    return "Controls: Use mouse to interact";
            }
        }

        private void StartGame()
        {
            Debug.Log("[LevelManager] Game started!");
            // 타이머 시작
            levelStartTime = Time.time;
            // 게임 재개
            Time.timeScale = 1f;
        }

        private void HandleLevelCompletion(int starsEarned)
        {
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.OnLevelCompleted(starsEarned);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(levelIndex, starsEarned);
            }
        }

        // 완료 판정 (모든 piece.IsCorrect가 true인지)
        public bool IsLevelComplete()
        {
                foreach (var pair in pairs)
                {
                    if (pair.piece == null || !pair.piece.IsCorrect) return false;
                }
                return true;
        }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            const int padding = 10;
            GUILayout.BeginArea(new Rect(padding, padding, 320, 200), GUI.skin.box);
            GUILayout.Label("[ShadowEscape Debug]");
            
            // 타이머 및 별 등급 표시
            if (!levelCompletionTriggered)
            {
                float elapsedTime = Time.time - levelStartTime;
                int currentStars = CalculateStars(elapsedTime);
                GUILayout.Label($"Time: {elapsedTime:F1}s ({currentStars}⭐)");
            }
            
            GUILayout.Label("Selected Piece: " + (selectedPiece ? selectedPiece.name : "(none)"));
            if (selectedPiece != null)
            {
                // Find matching target
                TargetPiece target = null;
                foreach (var pair in pairs)
                {
                    if (pair.piece == selectedPiece) { target = pair.target; break; }
                }
                if (target != null)
                {
                    float posDiff = Vector3.Distance(selectedPiece.transform.position, target.transform.position);
                    float rotDiff = Quaternion.Angle(selectedPiece.transform.rotation, target.transform.rotation);
                    GUILayout.Label($"Pos Diff: {posDiff:F3} (tol {target.positionTolerance:F3})");
                    GUILayout.Label($"Rot Diff: {rotDiff:F2} (tol {target.rotationTolerance:F2})");
                    GUILayout.Label("IsCorrect: " + (selectedPiece.IsCorrect ? "YES" : "NO"));
                }
            }
            GUILayout.EndArea();

            if (levelCompletionTriggered && Time.time - completionMessageTime <= completionMessageDuration)
            {
                var msgRect = new Rect(Screen.width / 2f - 150, 40, 300, 40);
                GUI.Box(msgRect, completionMessage);
            }
        }
    }
}
