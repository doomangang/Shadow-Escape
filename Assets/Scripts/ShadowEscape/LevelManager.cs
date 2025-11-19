using System.Collections.Generic;
using UnityEngine;
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
    public bool showDebugOverlay = true;
    private string completionMessage;
    private float completionMessageTime;
    public float completionMessageDuration = 3f;

        // 감도 설정
        public float rotateSpeed = 15f;
        public float moveSpeed = 0.01f;

        private void Start()
        {
            mainCamera = Camera.main;

            // 자동 수집: 페어가 비어있을 때는 씬의 Piece/Target을 찾아 자동으로 매칭 시도
            if (pairs.Count == 0)
            {
                var scenePieces = Object.FindObjectsByType<Piece>(FindObjectsSortMode.None);
                var sceneTargets = Object.FindObjectsByType<TargetPiece>(FindObjectsSortMode.None);

                int count = Mathf.Min(scenePieces.Length, sceneTargets.Length);
                for (int i = 0; i < count; i++)
                {
                    pairs.Add(new PieceTargetPair { piece = scenePieces[i], target = sceneTargets[i] });
                }
            }
        }

        private void Update()
        {
            HandleInput();
            // 매 프레임 정답 여부 검사 (성능 문제가 생기면 조절)
            ValidateAllPieces();
        }

        private void HandleInput()
        {
            if (GetMouseButtonDown(0))
            {
                lastMousePos = GetMousePosition();
                TrySelectPieceUnderMouse();
            }

            // 드래그 중
            if (GetMouseButton(0) && selectedPiece != null)
            {
                Vector3 mouseDelta = GetMousePosition() - lastMousePos;

                if (IsShiftPressed())
                {
                    float dx = mouseDelta.x * moveSpeed;
                    float dy = mouseDelta.y * moveSpeed;
                    selectedPiece.Move(dx, dy);
                }
                else
                {
                    float rx = -1 * mouseDelta.x * rotateSpeed * Time.deltaTime;
                    float ry = -1 * mouseDelta.y * rotateSpeed * Time.deltaTime;
                    selectedPiece.Rotate(rx, ry);
                }

                lastMousePos = GetMousePosition();
            }

            if (GetMouseButtonUp(0))
            {
                selectedPiece = null;
            }
        }

        private bool GetMouseButtonDown(int button)
        {
        }

        private bool GetMouseButton(int button)
        {
        }

        private bool GetMouseButtonUp(int button)
        {
        }

        private Vector3 GetMousePosition()
        {
        }

        private bool IsShiftPressed()
        {
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
                pair.piece.IsCorrect = pair.target.CheckIsCorrect(pair.piece.transform);
                if (!pair.piece.IsCorrect) allCorrect = false;
            }

            if (allCorrect && !levelCompletionTriggered)
            {
                levelCompletionTriggered = true;
                completionMessage = "Level Complete!";
                completionMessageTime = Time.time;
                // Report completion to GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CompleteLevel(levelIndex, starsToGrant);
                }
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
