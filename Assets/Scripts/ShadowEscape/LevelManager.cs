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
    [Tooltip("Stars granted upon first completion.")]
    public int starsToGrant = 1;
    private bool levelCompletionTriggered = false;

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
    [Tooltip("If true, use the subject-required input mapping: LMB drag = horizontal, LMB+Ctrl = vertical, LMB+Shift = move")]
    [SerializeField] private bool useSubjectInputScheme = true;

    [Tooltip("Minimum interval (seconds) between automatic validation runs; validation is also run when interaction ends")]
    [SerializeField] private float validationInterval = 0.1f;
    private float lastValidationTime = 0f;

    [Header("UI Hooks")]
    [SerializeField] private LevelHintDisplay hintDisplay;

    private LevelMetadata _metadata; // 난이도/힌트 데이터
    private DifficultyTier _effectiveDifficulty = DifficultyTier.Hard; // 기본값 (제약 없음)

        private void Start()
        {
            mainCamera = Camera.main;

            if (hintDisplay == null)
            {
                hintDisplay = UnityObject.FindFirstObjectByType<LevelHintDisplay>(FindObjectsInactive.Include);
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
        }

        private void ApplyMetadata(LevelMetadata metadata)
        {
            _effectiveDifficulty = metadata.difficulty;
            levelIndex = metadata.levelIndex;
            Debug.Log($"[LevelManager] Metadata applied (difficulty={_effectiveDifficulty}, levelIndex={levelIndex})");

            InjectUnifiedTolerances(metadata.positionTolerance, metadata.rotationTolerance);

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

        private void HandleInput()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }
            if (GetMouseButtonDown(0))
            {
                lastMousePos = GetMousePosition();
                TrySelectPieceUnderMouse();
            }

            // 드래그 중
            if (GetMouseButton(0) && selectedPiece != null)
            {
                Vector3 mouseDelta = GetMousePosition() - lastMousePos;

                if (useSubjectInputScheme)
                {
                    // 난이도별 허용 동작 결정
                    bool allowMove = _effectiveDifficulty == DifficultyTier.Hard; // Hard만 이동 허용
                    bool allowVertical = _effectiveDifficulty == DifficultyTier.Medium || _effectiveDifficulty == DifficultyTier.Hard; // Medium 이상에서 수직 회전 허용
                    bool allowHorizontal = true; // 모든 난이도에서 수평 회전 허용

                    if (IsShiftPressed())
                    {
                        if (allowMove)
                        {
                            float dx = mouseDelta.x * moveSpeed;
                            float dy = mouseDelta.y * moveSpeed;
                            selectedPiece.Move(dx, dy);
                        }
                        // 이동 불허 난이도면 무시
                    }
                    else if (IsCtrlPressed())
                    {
                        if (allowVertical)
                        {
                            float ry = -1 * mouseDelta.y * rotateSpeed * Time.deltaTime;
                            selectedPiece.Rotate(0f, ry);
                        }
                        else
                        {
                            // 수직 회전 불허 시 무시하고(혹은 수평으로 대체 가능) 아무 것도 하지 않음
                        }
                    }
                    else
                    {
                        if (allowHorizontal)
                        {
                            float rx = -1 * mouseDelta.x * rotateSpeed * Time.deltaTime;
                            selectedPiece.Rotate(rx, 0f);
                        }
                    }
                }
                else
                {
                    // 이전 호환 모드: 난이도 적용 (Easy면 수평만, Medium은 수평+수직, Hard는 이동 제외한 여기서는 회전 전부)
                    bool allowVertical = _effectiveDifficulty != DifficultyTier.Easy;
                    float rx = -1 * mouseDelta.x * rotateSpeed * Time.deltaTime;
                    float ry = -1 * mouseDelta.y * rotateSpeed * Time.deltaTime;
                    if (!allowVertical)
                    {
                        selectedPiece.Rotate(rx, 0f);
                    }
                    else
                    {
                        selectedPiece.Rotate(rx, ry);
                    }
                }

                lastMousePos = GetMousePosition();
            }

            if (GetMouseButtonUp(0))
            {
                // interaction ended -> run validation immediately
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
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair.piece == null || pair.target == null) { allCorrect = false; continue; }
                bool wasCorrect = pair.piece.IsCorrect;
                bool nowCorrect = pair.target.CheckIsCorrect(pair.piece.transform);
                pair.piece.IsCorrect = nowCorrect;
                if (!nowCorrect) allCorrect = false;

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

            if (allCorrect && !levelCompletionTriggered)
            {
                levelCompletionTriggered = true;
                completionMessage = "Level Complete!";
                completionMessageTime = Time.time;
                Debug.Log($"[LevelManager] All pieces correct — signaling completion (levelIndex={levelIndex}, stars={starsToGrant}).");
                HandleLevelCompletion();
            }
        }

        private void HandleLevelCompletion()
        {
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.OnLevelCompleted(starsToGrant);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(levelIndex, starsToGrant);
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
            GUILayout.BeginArea(new Rect(padding, padding, 320, 160), GUI.skin.box);
            GUILayout.Label("[ShadowEscape Debug]");
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
