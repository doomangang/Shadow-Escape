using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowEscape
{    public class PauseMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle muteToggle;
        
        [Header("Navigation Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
            
            // Wire buttons
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinue);
            }
            
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestart);
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnReturnToMenu);
            }
        }

        private void OnEnable()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            }
        }

        private void OnDisable()
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

        public void OnContinue()
        {
            Hide();
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
    }
}
