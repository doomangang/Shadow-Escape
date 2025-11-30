using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Helper utilities to generate simple runtime UI without needing prefab authoring.
    /// Keeps scene bootstrap scripts short and readable.
    /// </summary>
    public static class RuntimeUIBuilder
    {
        private static Font _defaultFont;

        private static Font DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _defaultFont;
            }
        }

        public static Canvas CreateFullScreenCanvas(string name, Transform parent = null)
        {
            var canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
            {
                canvasGO.transform.SetParent(parent, false);
            }

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            return canvas;
        }

        public static void EnsureEventSystemExists()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2? anchoredPos = null)
        {
            var panelGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            var rect = panelGO.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos ?? Vector2.zero;

            var image = panelGO.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);

            return panelGO;
        }

        public static Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.font = DefaultFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;

            return label;
        }

        public static Button CreateButton(Transform parent, string name, string labelText, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.font = DefaultFont;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.color = Color.white;
            text.text = labelText;

            return go.GetComponent<Button>();
        }

        public static Toggle CreateToggle(Transform parent, string name, string labelText, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(go.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(32, 32);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0, 0);

            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = Color.white;

            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGO.transform.SetParent(bgGO.transform, false);
            var ckRect = checkmarkGO.GetComponent<RectTransform>();
            ckRect.anchorMin = new Vector2(0.1f, 0.1f);
            ckRect.anchorMax = new Vector2(0.9f, 0.9f);
            ckRect.offsetMin = Vector2.zero;
            ckRect.offsetMax = Vector2.zero;

            var ckImage = checkmarkGO.GetComponent<Image>();
            ckImage.color = Color.green;

            var label = CreateText(go.transform, "Label", labelText, 22, new Vector2(size.x - 40, size.y), new Vector2((size.x - 40) / 2 + 20, 0));
            label.alignment = TextAnchor.MiddleLeft;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = ckImage;

            return toggle;
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10, 0);
            fillAreaRect.offsetMax = new Vector2(-10, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.4f, 1f);

            var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(go.transform, false);
            var handleRect = handleSlideArea.GetComponent<RectTransform>();
            handleRect.sizeDelta = size;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(1f, 1f);
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            handle.GetComponent<Image>().color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(28, 60);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }
    }
}
