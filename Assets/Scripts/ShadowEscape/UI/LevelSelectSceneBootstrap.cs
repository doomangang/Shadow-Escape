using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Runtime-generated Level Select UI that reads data from GameManager + SceneFlowManager.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelSelectSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string headerText = "레벨 선택";

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            RuntimeUIBuilder.EnsureEventSystemExists();
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas("LevelSelectCanvas", transform);
            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "LevelSelectPanel", new Vector2(900, 800));

            RuntimeUIBuilder.CreateText(panel.transform, "Header", headerText, 46, new Vector2(780, 80), new Vector2(0, 320));

            var infoText = RuntimeUIBuilder.CreateText(panel.transform, "Info", "왼쪽 버튼으로 레벨을 선택하세요", 24, new Vector2(780, 40), new Vector2(0, 260));

            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = new Vector2(780, 500);
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0, -40);
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            scrollGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0);

            var layout = contentGO.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(360, 120);
            layout.spacing = new Vector2(20, 20);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperCenter;

            scrollRect.content = contentRect;

            var sfm = SceneFlowManager.Instance;
            var gm = GameManager.Instance;

            List<string> levelNames = sfm?.levelSceneNames ?? new List<string>();
            if (levelNames.Count == 0)
            {
                infoText.text = "씬 리스트를 찾을 수 없습니다. SceneFlowManager.levelSceneNames를 채워주세요.";
            }

            for (int i = 0; i < levelNames.Count; i++)
            {
                CreateLevelButton(contentRect, i, levelNames[i], gm, sfm);
            }

            var backButton = RuntimeUIBuilder.CreateButton(panel.transform, "BackButton", "타이틀로", new Vector2(300, 60), new Vector2(-200, -340));
            backButton.onClick.AddListener(() => SceneFlowManager.Instance?.LoadTitle());

            var reloadButton = RuntimeUIBuilder.CreateButton(panel.transform, "ReloadButton", "씬 다시 불러오기", new Vector2(300, 60), new Vector2(200, -340));
            reloadButton.onClick.AddListener(() => SceneFlowManager.Instance?.LoadLevelSelect());
        }

        private void CreateLevelButton(Transform parent, int levelIndex, string sceneName, GameManager gm, SceneFlowManager sfm)
        {
            var button = RuntimeUIBuilder.CreateButton(parent, $"LevelButton_{levelIndex}", sceneName, new Vector2(360, 120), Vector2.zero);
            button.transform.localScale = Vector3.one;

            bool unlocked = gm == null || gm.IsTester || gm.IsLevelUnlocked(levelIndex);
            button.interactable = unlocked;

            if (!unlocked)
            {
                button.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            button.onClick.AddListener(() => sfm?.LoadLevel(levelIndex));

            var statusText = RuntimeUIBuilder.CreateText(button.transform, "Status", unlocked ? "Unlocked" : "Locked", 20, new Vector2(320, 30), new Vector2(0, -25));
            statusText.alignment = TextAnchor.MiddleLeft;

            var starText = RuntimeUIBuilder.CreateText(button.transform, "Stars", "★" + GetStarCount(levelIndex, gm), 24, new Vector2(320, 30), new Vector2(0, 25));
            starText.alignment = TextAnchor.MiddleLeft;
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
    }
}
