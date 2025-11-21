using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ShadowEscape
{
    // 테스트 씬에서 Play 시 동적으로 Level Select UI를 만들어서
    // 01_LevelSelect 씬이 없어도 테스트가 가능하도록 돕는 헬퍼
    [DisallowMultipleComponent]
    public class TestSceneLevelSelectHelper : MonoBehaviour
    {
        [Tooltip("최상단에 생성할 Canvas의 이름")]
        public string canvasName = "TestScene_LevelSelectCanvas";

        [Tooltip("버튼의 최대 열 개수 (레이아웃용)")]
        public int columns = 3;

        private void Awake()
        {
            // Editor/Play 모드에서만 동작. 씬에서 중복 생성 방지
            if (GameObject.Find(canvasName) != null) return;

            var canvasGO = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // 배경 패널
            var panelGO = new GameObject("LevelSelectPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRt = panelGO.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(600, 400);
            panelRt.anchoredPosition = new Vector2(0, 0);
            var img = panelGO.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);

            // 타이틀 텍스트
            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -30);
            titleRt.sizeDelta = new Vector2(400, 40);
            var titleText = titleGO.AddComponent<UnityEngine.UI.Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "Level Select (Play-mode helper)";
            titleText.color = Color.white;

            // 버튼 컨테이너
            var container = new GameObject("Buttons", typeof(RectTransform));
            container.transform.SetParent(panelGO.transform, false);
            var contRt = container.GetComponent<RectTransform>();
            contRt.sizeDelta = new Vector2(560, 320);
            contRt.anchoredPosition = new Vector2(0, -20);

            // Obtain scene list from LevelSelectManager or Assets/Scenes fallback
            string[] sceneNames = new string[0];
            var lsm = FindObjectOfType<LevelSelectManager>();
            if (lsm != null && lsm.levelSceneNames != null && lsm.levelSceneNames.Count > 0)
            {
                sceneNames = lsm.levelSceneNames.ToArray();
            }
            else
            {
                // fallback: try to find any .unity in Assets/Scenes that are committed
                try
                {
                    var files = System.IO.Directory.Exists("Assets/Scenes") ? System.IO.Directory.GetFiles("Assets/Scenes", "*.unity") : new string[0];
                    sceneNames = files.Select(System.IO.Path.GetFileNameWithoutExtension).ToArray();
                }
                catch { sceneNames = new string[0]; }
            }

            // Create a button for each scene found (limit to 12 for layout)
            int limit = Mathf.Min(12, sceneNames.Length);
            for (int i = 0; i < limit; i++)
            {
                int idx = i;
                var btn = CreateButton(container.transform, new Vector2(-180 + (i % columns) * 180, 120 - (i / columns) * 90), 160, 60, sceneNames[i]);
                btn.onClick.AddListener(() => { LoadSceneByName(sceneNames[idx]); });
                // Lock overlay based on GameManager
                bool unlocked = GameManager.Instance == null ? true : GameManager.Instance.IsLevelUnlocked(idx);
                if (GameManager.Instance != null && GameManager.Instance.IsTester) unlocked = true;
                if (!unlocked)
                {
                    // dim button
                    var imgComp = btn.GetComponent<Image>();
                    imgComp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    btn.interactable = false;
                }
            }

            // Bottom controls: Reload and Toggle Tester
            var reloadBtn = CreateButton(panelGO.transform, new Vector2(-120, -170), 200, 40, "Reload Current Level");
            reloadBtn.onClick.AddListener(() => { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });

            var toggleBtn = CreateButton(panelGO.transform, new Vector2(120, -170), 200, 40, "Toggle Tester Mode");
            toggleBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) { GameManager.Instance.IsTester = !GameManager.Instance.IsTester; Debug.Log($"IsTester = {GameManager.Instance.IsTester}"); }
            });
        }

        private Button CreateButton(Transform parent, Vector2 anchoredPos, float w, float h, string label)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn = go.GetComponent<Button>();

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<UnityEngine.UI.Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            return btn;
        }

        private void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            Debug.Log($"[TestSceneLevelSelectHelper] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
