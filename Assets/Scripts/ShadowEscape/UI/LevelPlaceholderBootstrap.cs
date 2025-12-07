using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Temporary placeholder for puzzle scenes so that SceneFlow can be verified end-to-end
    /// before real 3D content is authored.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelPlaceholderBootstrap : MonoBehaviour
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private DifficultyTier difficulty = DifficultyTier.Easy;
        [SerializeField] private int starsToGrant = 3;

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas("LevelPlaceholderCanvas", transform);
            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "LevelPanel", new Vector2(700, 400));

            RuntimeUIBuilder.CreateText(panel.transform, "Title", $"Level {levelIndex} ({difficulty})", 42, new Vector2(600, 60), new Vector2(0, 120));
            RuntimeUIBuilder.CreateText(panel.transform, "Body", "실제 퍼즐 대신 임시 패널입니다.\n오브젝트를 배치하기 전에 Scene Flow 동작을 확인하세요.", 22, new Vector2(600, 80), new Vector2(0, 40));

            var completeButton = RuntimeUIBuilder.CreateButton(panel.transform, "CompleteButton", "퍼즐 완료 처리", new Vector2(400, 70), new Vector2(0, -20));
            completeButton.onClick.AddListener(MarkCompleted);

            var levelSelectButton = RuntimeUIBuilder.CreateButton(panel.transform, "BackButton", "레벨 선택으로", new Vector2(400, 60), new Vector2(0, -120));
            levelSelectButton.onClick.AddListener(() => SceneFlowManager.Instance?.LoadLevelSelect());
        }

        private void MarkCompleted()
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
    }
}
