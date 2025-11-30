using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Generates a functional Title Screen UI at runtime so designers can focus on gameplay scenes first.
    /// Relies on TitleScreenManager for the actual button behaviors.
    /// </summary>
    [DisallowMultipleComponent]
    public class TitleSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string gameTitle = "Shadow Escape";
        [SerializeField] private string subtitle = "Shadowmatic-inspired Puzzle";

        private TitleScreenManager _titleManager;

        private void Awake()
        {
            _titleManager = GetComponent<TitleScreenManager>();
            if (_titleManager == null)
            {
                _titleManager = gameObject.AddComponent<TitleScreenManager>();
            }

            if (string.IsNullOrWhiteSpace(_titleManager.levelSelectSceneName) && SceneFlowManager.Instance != null)
            {
                _titleManager.levelSelectSceneName = SceneFlowManager.Instance.levelSelectSceneName;
            }
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            RuntimeUIBuilder.EnsureEventSystemExists();
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas("TitleCanvas", transform);
            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "TitlePanel", new Vector2(640, 720));

            RuntimeUIBuilder.CreateText(panel.transform, "GameTitle", gameTitle, 48, new Vector2(560, 80), new Vector2(0, 260));
            RuntimeUIBuilder.CreateText(panel.transform, "Subtitle", subtitle, 26, new Vector2(560, 40), new Vector2(0, 210));

            var testerToggle = RuntimeUIBuilder.CreateToggle(panel.transform, "TesterToggle", "Tester Mode (모든 레벨 해제)", new Vector2(520, 60), new Vector2(0, 80));
            testerToggle.isOn = GameManager.Instance != null && GameManager.Instance.IsTester;
            testerToggle.onValueChanged.AddListener(OnTesterToggled);

            var startButton = RuntimeUIBuilder.CreateButton(panel.transform, "StartButton", "게임 시작", new Vector2(420, 70), new Vector2(0, 10));
            startButton.onClick.AddListener(_titleManager.StartGame);

            var resetButton = RuntimeUIBuilder.CreateButton(panel.transform, "ResetButton", "진행도 초기화", new Vector2(420, 60), new Vector2(0, -90));
            resetButton.onClick.AddListener(_titleManager.ResetProgress);

            var quitButton = RuntimeUIBuilder.CreateButton(panel.transform, "QuitButton", "게임 종료", new Vector2(420, 60), new Vector2(0, -170));
            quitButton.onClick.AddListener(_titleManager.QuitGame);

            // Audio controls
            RuntimeUIBuilder.CreateText(panel.transform, "VolumeLabel", "마스터 볼륨", 22, new Vector2(420, 40), new Vector2(0, -240));
            var volumeSlider = RuntimeUIBuilder.CreateSlider(panel.transform, "VolumeSlider", new Vector2(420, 40), new Vector2(0, -280));
            if (AudioManager.Instance != null)
            {
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            }
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            var muteToggle = RuntimeUIBuilder.CreateToggle(panel.transform, "MuteToggle", "음소거", new Vector2(420, 50), new Vector2(0, -340));
            if (AudioManager.Instance != null)
            {
                muteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMuted);
            }
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }

        private void OnTesterToggled(bool isTester)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IsTester = isTester;
            }
            _titleManager.SetTesterMode(isTester);
        }

        private void OnVolumeChanged(float value)
        {
            AudioManager.Instance?.SetVolume(value);
        }

        private void OnMuteToggled(bool isMuted)
        {
            AudioManager.Instance?.ToggleMute(isMuted);
        }
    }
}
