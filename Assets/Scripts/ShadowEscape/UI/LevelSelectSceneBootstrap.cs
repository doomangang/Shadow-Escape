using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    [DisallowMultipleComponent]
    public class LevelSelectSceneBootstrap : MonoBehaviour
    {
        [Header("Text Configuration")]
        [SerializeField] private string headerText = "Select Level";
        [SerializeField] private string infoDefaultText = "Select a level with the left button";
    [SerializeField] private string backButtonText = "Back to Title";
    [SerializeField] private string reloadButtonText = "Reload Scene";

        [Header("UI References")]
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text infoLabel;
    [SerializeField] private TMP_Text backButtonLabel;
    [SerializeField] private TMP_Text reloadButtonLabel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private LevelSelectButtonView levelButtonPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private Button reloadButton;

        [Header("Visual Settings")]
        [SerializeField] private Color unlockedButtonColor = Color.white;
        [SerializeField] private Color lockedButtonColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private bool verboseLogging;

        private readonly List<LevelSelectButtonView> spawnedButtons = new List<LevelSelectButtonView>();

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => SceneFlowManager.Instance?.LoadTitle());
            }

            if (reloadButton != null)
            {
                reloadButton.onClick.RemoveAllListeners();
                reloadButton.onClick.AddListener(() => SceneFlowManager.Instance?.LoadLevelSelect());
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            if (infoLabel != null)
            {
                infoLabel.text = infoDefaultText;
            }

            if (backButtonLabel != null)
            {
                backButtonLabel.text = backButtonText;
            }

            if (reloadButtonLabel != null)
            {
                reloadButtonLabel.text = reloadButtonText;
            }

            if (contentRoot == null || levelButtonPrefab == null)
            {
                Debug.LogWarning("LevelSelectSceneBootstrap: UI 참조가 비어있습니다. 인스펙터에서 할당해주세요.");
                return;
            }

            ClearButtons();

            var sfm = SceneFlowManager.Instance;
            var gm = GameManager.Instance;

            List<string> levelNames = sfm?.levelSceneNames ?? new List<string>();
            if (levelNames.Count == 0 && infoLabel != null)
            {
                infoLabel.text = "Level list not found. Please populate SceneFlowManager.levelSceneNames.";
            }

            for (int i = 0; i < levelNames.Count; i++)
            {
                SpawnLevelButton(i, levelNames[i], gm, sfm);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        private void SpawnLevelButton(int levelIndex, string sceneName, GameManager gm, SceneFlowManager sfm)
        {
            var view = Instantiate(levelButtonPrefab, contentRoot);
            view.gameObject.name = $"LevelButton_{levelIndex}";

            if (!view.EnsureBindings())
            {
                LogVerbose($"{view.name} 프리팹 참조가 비어 있어 파괴합니다.");
                Destroy(view.gameObject);
                return;
            }

            var button = view.Button;
            if (button == null)
            {
                Debug.LogWarning("LevelSelectButtonView prefab에는 Button 컴포넌트가 필요합니다.");
                Destroy(view.gameObject);
                return;
            }

            bool unlocked = gm == null || gm.IsTester || gm.IsLevelUnlocked(levelIndex);
            button.interactable = unlocked;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = unlocked ? unlockedButtonColor : lockedButtonColor;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => sfm?.LoadLevel(levelIndex));

            if (view.TitleLabel != null)
            {
                view.TitleLabel.text = sceneName;
            }

            if (view.StatusLabel != null)
            {
                view.StatusLabel.text = unlocked ? "Unlocked" : "Locked";
            }

            if (view.StarLabel != null)
            {
                view.StarLabel.text = "S" + GetStarCount(levelIndex, gm);
            }

            LogVerbose($"{sceneName} 버튼 생성 완료 - Status='{view.StatusLabel?.text}', Stars='{view.StarLabel?.text}'");
            spawnedButtons.Add(view);
        }

        private void ClearButtons()
        {
            for (int i = spawnedButtons.Count - 1; i >= 0; i--)
            {
                if (spawnedButtons[i] == null) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(spawnedButtons[i].gameObject);
                }
                else
#endif
                {
                    Destroy(spawnedButtons[i].gameObject);
                }
            }

            spawnedButtons.Clear();
        }

        private string GetStarCount(int levelIndex, GameManager gm)
        {
            if (gm?.CurrentSave == null || gm.CurrentSave.starsEarnedAtLevel == null)
            {
                return "0";
            }

            if (levelIndex < 0 || levelIndex >= gm.CurrentSave.starsEarnedAtLevel.Length)
            {
                return "0";
            }

            int stars = gm.CurrentSave.starsEarnedAtLevel[levelIndex];
            return Mathf.Clamp(stars, 0, 3).ToString();
        }

        private void LogVerbose(string message)
        {
            if (!verboseLogging) return;
            Debug.Log($"[LevelSelectSceneBootstrap] {message}", this);
        }
    }
}
