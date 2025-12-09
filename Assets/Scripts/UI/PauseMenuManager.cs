using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowEscape
{
    /// <summary>
    /// Handles showing the pause overlay, syncing tester-required audio options,
    /// and exposing core navigation buttons.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
    [SerializeField] private GameObject root;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private bool enableToggleHotkey = true;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.Escape;

    private bool warnedMissingRoot;

        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            RegisterListeners();
            SyncAudioControls();
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        private void Update()
        {
            if (!enableToggleHotkey)
            {
                return;
            }

            if (WasTogglePressedThisFrame())
            {
                ToggleMenu();
            }
        }

        public bool HasRoot => root != null;

        public void AssignRuntimeReferences(GameObject rootObject, Slider slider, Toggle toggle)
        {
            UnregisterListeners();

            root = rootObject;
            volumeSlider = slider;
            muteToggle = toggle;

            if (root != null)
            {
                root.SetActive(false);
            }

            RegisterListeners();
            SyncAudioControls();
        }

        private void RegisterListeners()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(OnMuteToggled);
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            }
        }

        private void UnregisterListeners()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(OnMuteToggled);
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            GameManager.Instance?.SetPaused(true);
            SyncAudioControls();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            GameManager.Instance?.SetPaused(false);
        }

        public void ToggleMenu()
        {
            if (root == null)
            {
                WarnMissingRoot();
                return;
            }

            if (root.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void OnRestart()
        {
            Hide();
            SceneFlowManager.Instance?.ReloadCurrent();
        }

        public void OnReturnToMenu()
        {
            Hide();
            SceneFlowManager.Instance?.LoadLevelSelect();
        }

        public void OnQuit()
        {
            Hide();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.SetVolume(value);
            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMuted);
            }
        }

        public void OnMuteToggled(bool isMuted)
        {
            AudioManager.Instance?.ToggleMute(isMuted);
            if (volumeSlider != null && AudioManager.Instance != null && isMuted)
            {
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            }
        }

        private void SyncAudioControls()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            }

            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMuted);
            }
        }

        private bool WasTogglePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacyToggleKey);
#else
            return false;
#endif
        }

        private void WarnMissingRoot()
        {
            if (warnedMissingRoot)
            {
                return;
            }

            warnedMissingRoot = true;
            Debug.LogWarning("[PauseMenuManager] Toggle requested but no root GameObject is assigned. Attach PauseMenuBootstrap or assign references via inspector.", this);
        }
    }
}
