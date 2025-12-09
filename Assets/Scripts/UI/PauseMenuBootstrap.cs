using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Builds a lightweight Bootstrap-inspired pause menu at runtime so scenes work without hand-authored UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private string rootCanvasName = "PauseMenuCanvas";
        [SerializeField] private Vector2 panelSize = new Vector2(520f, 640f);
        [SerializeField] private int sortingOrder = 250;
        [SerializeField] private bool skipIfRootAssigned = true;
        [SerializeField] private PauseMenuManager pauseMenuManager;

        private void Awake()
        {
            RuntimeUIBuilder.EnsureEventSystemExists();

            if (pauseMenuManager == null)
            {
                pauseMenuManager = GetComponent<PauseMenuManager>();
            }

            if (pauseMenuManager == null)
            {
                pauseMenuManager = gameObject.AddComponent<PauseMenuManager>();
            }
        }

        private void Start()
        {
            if (pauseMenuManager == null)
            {
                Debug.LogWarning("[PauseMenuBootstrap] Missing PauseMenuManager reference.", this);
                return;
            }

            if (skipIfRootAssigned && pauseMenuManager.HasRoot)
            {
                return;
            }

            BuildRuntimeUI();
        }

        private void BuildRuntimeUI()
        {
            var canvas = RuntimeUIBuilder.CreateFullScreenCanvas(rootCanvasName, transform);
            canvas.sortingOrder = sortingOrder;
            var canvasGO = canvas.gameObject;

            var dimmer = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            dimmer.transform.SetParent(canvas.transform, false);
            var dimmerRect = dimmer.GetComponent<RectTransform>();
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;

            var dimmerImage = dimmer.GetComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.75f);
            var dimmerButton = dimmer.GetComponent<Button>();
            dimmerButton.targetGraphic = dimmerImage;
            dimmerButton.transition = Selectable.Transition.None;
            dimmerButton.onClick.AddListener(() => pauseMenuManager.Hide());

            var panel = RuntimeUIBuilder.CreatePanel(canvas.transform, "PausePanel", panelSize);
            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.09f, 0.11f, 0.18f, 0.94f);
            }

            RuntimeUIBuilder.CreateText(panel.transform, "Title", "Game Paused", 42, new Vector2(panelSize.x - 80f, 70f), new Vector2(0, panelSize.y / 2f - 90f));
            RuntimeUIBuilder.CreateText(panel.transform, "Subtitle", "Take a breath. Use the quick actions below.", 22, new Vector2(panelSize.x - 80f, 60f), new Vector2(0, panelSize.y / 2f - 150f));

            var resumeButton = RuntimeUIBuilder.CreateButton(panel.transform, "ResumeButton", "Resume", new Vector2(panelSize.x - 140f, 64f), new Vector2(0, 80f));
            resumeButton.onClick.AddListener(() => pauseMenuManager.Hide());

            var restartButton = RuntimeUIBuilder.CreateButton(panel.transform, "RestartButton", "Restart Level", new Vector2(panelSize.x - 140f, 60f), new Vector2(0, 10f));
            restartButton.onClick.AddListener(pauseMenuManager.OnRestart);

            var selectButton = RuntimeUIBuilder.CreateButton(panel.transform, "LevelSelectButton", "Level Select", new Vector2(panelSize.x - 140f, 60f), new Vector2(0, -60f));
            selectButton.onClick.AddListener(pauseMenuManager.OnReturnToMenu);

            var quitButton = RuntimeUIBuilder.CreateButton(panel.transform, "QuitButton", "Quit Game", new Vector2(panelSize.x - 140f, 60f), new Vector2(0, -130f));
            quitButton.onClick.AddListener(pauseMenuManager.OnQuit);

            RuntimeUIBuilder.CreateText(panel.transform, "AudioLabel", "Audio", 28, new Vector2(panelSize.x - 160f, 50f), new Vector2(0, -210f));
            var volumeSlider = RuntimeUIBuilder.CreateSlider(panel.transform, "VolumeSlider", new Vector2(panelSize.x - 140f, 40f), new Vector2(0, -260f));
            var muteToggle = RuntimeUIBuilder.CreateToggle(panel.transform, "MuteToggle", "Mute audio", new Vector2(panelSize.x - 140f, 50f), new Vector2(0, -320f));

            RuntimeUIBuilder.CreateText(panel.transform, "Hint", "Press ESC anytime to toggle the pause overlay.", 20, new Vector2(panelSize.x - 80f, 50f), new Vector2(0, -380f));

            pauseMenuManager.AssignRuntimeReferences(canvasGO, volumeSlider, muteToggle);
        }
    }
}
