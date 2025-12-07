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
        [SerializeField] private bool skipRuntimeBuildIfCanvasExists = true;
        [SerializeField] private string rootCanvasName = "TitleCanvas";

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
            // 에디터에서 시각 편집을 위해 사용자가 직접 Canvas를 배치했다면 런타임 자동 생성은 생략
            bool hasAnyCanvas = GetComponentInChildren<Canvas>(true) != null;
            if (skipRuntimeBuildIfCanvasExists && (hasAnyCanvas || transform.Find(rootCanvasName) != null))
            {
                return;
            }
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas(rootCanvasName, transform);
            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "TitlePanel", new Vector2(640, 720));

            RuntimeUIBuilder.CreateText(panel.transform, "GameTitle", gameTitle, 48, new Vector2(560, 80), new Vector2(0, 260));
            RuntimeUIBuilder.CreateText(panel.transform, "Subtitle", subtitle, 26, new Vector2(560, 40), new Vector2(0, 210));

            var testerToggle = RuntimeUIBuilder.CreateToggle(panel.transform, "TesterToggle", "Tester Mode (Unlock All Levels)", new Vector2(520, 60), new Vector2(0, 80));
            testerToggle.isOn = GameManager.Instance != null && GameManager.Instance.IsTester;
            testerToggle.onValueChanged.AddListener(OnTesterToggled);

            var startButton = RuntimeUIBuilder.CreateButton(panel.transform, "StartButton", "Start Game", new Vector2(420, 70), new Vector2(0, 10));
            startButton.onClick.AddListener(_titleManager.StartGame);

            var resetButton = RuntimeUIBuilder.CreateButton(panel.transform, "ResetButton", "Reset Progress", new Vector2(420, 60), new Vector2(0, -90));
            resetButton.onClick.AddListener(_titleManager.ResetProgress);

            var quitButton = RuntimeUIBuilder.CreateButton(panel.transform, "QuitButton", "End Game", new Vector2(420, 60), new Vector2(0, -170));
            quitButton.onClick.AddListener(_titleManager.QuitGame);

            // Audio controls
            RuntimeUIBuilder.CreateText(panel.transform, "VolumeLabel", "Master Volume", 22, new Vector2(420, 40), new Vector2(0, -240));
            var volumeSlider = RuntimeUIBuilder.CreateSlider(panel.transform, "VolumeSlider", new Vector2(420, 40), new Vector2(0, -280));
            if (AudioManager.Instance != null)
            {
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            }
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            var muteToggle = RuntimeUIBuilder.CreateToggle(panel.transform, "MuteToggle", "Mute", new Vector2(420, 50), new Vector2(0, -340));
            if (AudioManager.Instance != null)
            {
                muteToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMuted);
            }
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }

#if UNITY_EDITOR
        [ContextMenu("Build Title UI (Editor)")]
        private void BuildUIInEditor()
        {
            // 에디터에서 수동으로 UI를 생성하여 씬 뷰에서 배치/수정 가능하게 함
            if (transform.Find(rootCanvasName) != null)
            {
                Debug.Log("[TitleSceneBootstrap] 이미 캔버스가 존재합니다. 필요 시 'Clear Built Title UI' 사용 후 다시 생성하세요.");
                return;
            }

            // 에디터에서는 EventSystem이 필수는 아니므로 생략(원하면 이후 수동 추가)
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas(rootCanvasName, transform);
            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "TitlePanel", new Vector2(640, 720));

            RuntimeUIBuilder.CreateText(panel.transform, "GameTitle", gameTitle, 48, new Vector2(560, 80), new Vector2(0, 260));
            RuntimeUIBuilder.CreateText(panel.transform, "Subtitle", subtitle, 26, new Vector2(560, 40), new Vector2(0, 210));

            RuntimeUIBuilder.CreateToggle(panel.transform, "TesterToggle", "Tester Mode (Unlock All Levels)", new Vector2(520, 60), new Vector2(0, 80));
            RuntimeUIBuilder.CreateButton(panel.transform, "StartButton", "Start Game", new Vector2(420, 70), new Vector2(0, 10));
            RuntimeUIBuilder.CreateButton(panel.transform, "ResetButton", "Reset Progress", new Vector2(420, 60), new Vector2(0, -90));
            RuntimeUIBuilder.CreateButton(panel.transform, "QuitButton", "End Game", new Vector2(420, 60), new Vector2(0, -170));

            RuntimeUIBuilder.CreateText(panel.transform, "VolumeLabel", "Master Volume", 22, new Vector2(420, 40), new Vector2(0, -240));
            RuntimeUIBuilder.CreateSlider(panel.transform, "VolumeSlider", new Vector2(420, 40), new Vector2(0, -280));
            RuntimeUIBuilder.CreateToggle(panel.transform, "MuteToggle", "Mute", new Vector2(420, 50), new Vector2(0, -340));

            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        [ContextMenu("Clear Built Title UI")]
        private void ClearBuiltUI()
        {
            var existing = transform.Find(rootCanvasName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
        }
#endif

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
