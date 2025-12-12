using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    [DisallowMultipleComponent]
    public class TitleSceneAutoWire : MonoBehaviour
    {
        [SerializeField] private Transform uiRoot; // optional: set your TitleCanvas or panel root

        private void Awake()
        {
            if (uiRoot == null) uiRoot = transform;

            // Ensure TitleScreenManager exists
            var titleMgr = GetComponent<TitleScreenManager>();
            if (titleMgr == null) titleMgr = gameObject.AddComponent<TitleScreenManager>();

            // Buttons
            TryBindButton("StartButton", titleMgr.StartGame);
            TryBindButton("ResetButton", titleMgr.ResetProgress);
            TryBindButton("QuitButton", titleMgr.QuitGame);

            // Toggles
            TryBindToggle("TesterToggle", isOn => {
                if (GameManager.Instance != null) GameManager.Instance.IsTester = isOn;
                titleMgr.SetTesterMode(isOn);
            });
            TryBindToggle("MuteToggle", isOn => AudioManager.Instance?.ToggleMute(isOn));

            // Slider
            TryBindSlider("VolumeSlider", v => AudioManager.Instance?.SetVolume(v),
                initializeWith: AudioManager.Instance != null ? (float?)AudioManager.Instance.MasterVolume : null);

            // Optional labels
            TrySetText("GameTitle", "Shadow Escape");
            TrySetText("Subtitle", "Shadowmatic-inspired Puzzle");
        }

        private void TryBindButton(string name, Action onClick)
        {
            var t = FindByName(name);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn == null) btn = t.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private void TryBindToggle(string name, Action<bool> onChanged)
        {
            var t = FindByName(name);
            if (t == null) return;
            var toggle = t.GetComponent<Toggle>();
            if (toggle == null) toggle = t.gameObject.AddComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        }

        private void TryBindSlider(string name, Action<float> onChanged, float? initializeWith)
        {
            var t = FindByName(name);
            if (t == null) return;
            var slider = t.GetComponent<Slider>();
            if (slider == null) slider = t.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            if (initializeWith.HasValue) slider.SetValueWithoutNotify(initializeWith.Value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        }

        private void TrySetText(string name, string value)
        {
            var t = FindByName(name);
            if (t == null) return;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private Transform FindByName(string name)
        {
            if (uiRoot == null) return null;
            var child = uiRoot.Find(name);
            if (child != null) return child;
            // broader search
            foreach (var t in uiRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals(name)) return t;
            }
            return null;
        }
    }
}