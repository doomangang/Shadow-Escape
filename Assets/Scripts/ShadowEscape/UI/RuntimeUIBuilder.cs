using System.Collections.Generic;
using TMPro;
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
        private static Sprite _defaultSprite;
        private static Sprite _backgroundSprite;
        private static Sprite _checkmarkSprite;
        private static Sprite _knobSprite;
    private static readonly Dictionary<string, Sprite> _fallbackSprites = new();
    private static readonly HashSet<string> _missingSpriteLogs = new();

        // In player builds, built-in UGUI sprites might not be available via Resources.GetBuiltinResource.
        // Fall back to null sprites and solid-color UI so build never fails.
        private static Sprite DefaultSprite => _defaultSprite ??= TryGetBuiltinSprite("UI/Skin/UISprite.psd");
        private static Sprite BackgroundSprite => _backgroundSprite ??= TryGetBuiltinSprite("UI/Skin/Background.psd");
        private static Sprite CheckmarkSprite => _checkmarkSprite ??= TryGetBuiltinSprite("UI/Skin/Checkmark.psd");
        private static Sprite KnobSprite => _knobSprite ??= TryGetBuiltinSprite("UI/Skin/Knob.psd");

        private static Sprite TryGetBuiltinSprite(string path)
        {
            Sprite sprite = null;
            try
            {
                sprite = Resources.GetBuiltinResource<Sprite>(path);
            }
            catch (System.Exception ex)
            {
                LogMissingSprite(path, ex);
            }

            if (sprite != null)
            {
                return sprite;
            }

            LogMissingSprite(path);
            return GetFallbackSolidSprite(path);
        }

        private static void LogMissingSprite(string path, System.Exception exception = null)
        {
            if (!_missingSpriteLogs.Add(path))
            {
                return;
            }

            if (exception != null)
            {
                Debug.LogWarning($"Failed to load builtin sprite: {path}. Using fallback sprite. {exception.Message}");
            }
            else
            {
                Debug.LogWarning($"Failed to load builtin sprite: {path}. Using fallback sprite.");
            }
        }

        private static Sprite GetFallbackSolidSprite(string key)
        {
            if (_fallbackSprites.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = $"{key}_FallbackTexture"
            };

            var white = Color.white;
            tex.SetPixels(new[] { white, white, white, white });
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sprite.name = $"{key}_FallbackSprite";

#if UNITY_EDITOR
            tex.hideFlags = HideFlags.HideAndDontSave;
            sprite.hideFlags = HideFlags.HideAndDontSave;
#endif

            _fallbackSprites[key] = sprite;
            return sprite;
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
            // Include inactive objects to avoid duplicating when an EventSystem exists but is disabled
            var existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            // Prefer new Input System UI module when available (try both assembly names)
            var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.UI")
                                        ?? System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                go.AddComponent(inputSystemModuleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
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
            if (BackgroundSprite != null)
            {
                image.sprite = BackgroundSprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }
            image.color = new Color(0f, 0f, 0f, 0.75f);

            return panelGO;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
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
            if (DefaultSprite != null)
            {
                image.sprite = DefaultSprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }
            image.color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28;
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
            if (DefaultSprite != null)
            {
                bgImage.sprite = DefaultSprite;
                bgImage.type = Image.Type.Sliced;
            }
            else
            {
                bgImage.sprite = null;
                bgImage.type = Image.Type.Simple;
            }
            bgImage.color = Color.white;

            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGO.transform.SetParent(bgGO.transform, false);
            var ckRect = checkmarkGO.GetComponent<RectTransform>();
            ckRect.anchorMin = new Vector2(0.1f, 0.1f);
            ckRect.anchorMax = new Vector2(0.9f, 0.9f);
            ckRect.offsetMin = Vector2.zero;
            ckRect.offsetMax = Vector2.zero;

            var ckImage = checkmarkGO.GetComponent<Image>();
            ckImage.sprite = CheckmarkSprite; // may be null; still fine with solid color
            ckImage.color = Color.green;

            var label = CreateText(go.transform, "Label", labelText, 26, new Vector2(size.x - 40, size.y), new Vector2((size.x - 40) / 2 + 20, 0));
            label.alignment = TextAlignmentOptions.MidlineLeft;

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
            var bgImage = background.GetComponent<Image>();
            if (BackgroundSprite != null)
            {
                bgImage.sprite = BackgroundSprite;
                bgImage.type = Image.Type.Sliced;
            }
            else
            {
                bgImage.sprite = null;
                bgImage.type = Image.Type.Simple;
            }
            bgImage.color = new Color(1f, 1f, 1f, 0.3f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10, 0);
            fillAreaRect.offsetMax = new Vector2(-10, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.GetComponent<Image>();
            if (DefaultSprite != null)
            {
                fillImage.sprite = DefaultSprite;
                fillImage.type = Image.Type.Sliced;
            }
            else
            {
                fillImage.sprite = null;
                fillImage.type = Image.Type.Simple;
            }
            fillImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);

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
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = KnobSprite; // may be null; still visible via color
            handleImage.color = Color.white;
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
