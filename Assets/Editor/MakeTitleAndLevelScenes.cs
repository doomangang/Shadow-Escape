using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Editor utility to create minimal 00_Title and 01_LevelSelect scenes with UI
// Run from menu: Tools/ShadowEscape/Create Title & LevelSelect Scenes
public static class MakeTitleAndLevelScenes
{
    [MenuItem("Tools/ShadowEscape/Create Title & LevelSelect Scenes")]
    public static void CreateScenes()
    {
        // Create Title scene
        var titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        titleScene.name = "00_Title";

        // Root manager
        var titleMgrGO = new GameObject("TitleManagers");
        var titleMgr = titleMgrGO.AddComponent<ShadowEscape.TitleScreenManager>();

        // Create Canvas
        var canvasGO = CreateBasicCanvas("TitleCanvas");

        // Play button
        var playBtn = CreateUIButton(canvasGO.transform, "PlayButton", "Play", new Vector2(0, 40));
        // Reset button
        var resetBtn = CreateUIButton(canvasGO.transform, "ResetButton", "Reset Progress", new Vector2(0, -20));
        // Quit button
        var quitBtn = CreateUIButton(canvasGO.transform, "QuitButton", "Quit", new Vector2(0, -80));

        // Hook buttons to TitleScreenManager methods (persistent listeners)
        UnityEventTools.AddPersistentListener(playBtn.onClick, new UnityAction(titleMgr.StartGame));
        UnityEventTools.AddPersistentListener(resetBtn.onClick, new UnityAction(titleMgr.ResetProgress));
        UnityEventTools.AddPersistentListener(quitBtn.onClick, new UnityAction(titleMgr.QuitGame));

        // Save Title scene
        string titlePath = "Assets/Scenes/00_Title.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(titleScene, titlePath);

        // Create LevelSelect scene
        var selectScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        selectScene.name = "01_LevelSelect";

        var selectMgrGO = new GameObject("LevelSelectManagers");
        var selectMgr = selectMgrGO.AddComponent<ShadowEscape.LevelSelectManager>();

        var selectCanvas = CreateBasicCanvas("LevelSelectCanvas");
        // Container for level buttons
        var container = new GameObject("LevelButtonsContainer", typeof(RectTransform));
        container.transform.SetParent(selectCanvas.transform, false);
        var rt = container.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 400);
        rt.anchoredPosition = Vector2.zero;

        // Create a sample grid of 9 level buttons
        int count = 9;
        int cols = 3;
        int rows = Mathf.CeilToInt((float)count / cols);
        float spacing = 140f;
        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            Vector2 pos = new Vector2((col - (cols - 1) / 2f) * spacing, (row - (rows - 1) / 2f) * -spacing);
            var btn = CreateUIButton(container.transform, "LevelButton_" + i, "Level " + (i + 1), pos);

            // Add a small 'LockOverlay' child (disabled by default)
            var lockGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockGO.transform.SetParent(btn.transform, false);
            var lockImg = lockGO.GetComponent<Image>();
            lockImg.color = new Color(0f, 0f, 0f, 0.6f);
            var lrt = lockGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lockGO.SetActive(false);

            // Add a Stars Text child
            var starsGO = new GameObject("Stars", typeof(RectTransform));
            starsGO.transform.SetParent(btn.transform, false);
            var txt = starsGO.AddComponent<UnityEngine.UI.Text>();
            txt.text = "";
            txt.alignment = TextAnchor.LowerCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var srt = starsGO.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -25);
            srt.sizeDelta = new Vector2(120, 24);
        }

        // Attach LevelSelectUIController to a manager GO and wire references
        var uiCtrlGO = new GameObject("LevelSelectUI");
        var uiCtrl = uiCtrlGO.AddComponent<ShadowEscape.LevelSelectUIController>();
        uiCtrl.buttonsContainer = container.GetComponent<RectTransform>();
        uiCtrl.levelSelectManager = selectMgr;

        // Save LevelSelect scene
        string selectPath = "Assets/Scenes/01_LevelSelect.unity";
        EditorSceneManager.SaveScene(selectScene, selectPath);

        EditorUtility.DisplayDialog("ShadowEscape", "Created 00_Title and 01_LevelSelect scenes in Assets/Scenes.", "OK");
    }

    private static GameObject CreateBasicCanvas(string name)
    {
        var canvasGO = new GameObject(name);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private static Button CreateUIButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);
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
}
#endif
