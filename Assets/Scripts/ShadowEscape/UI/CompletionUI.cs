using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace ShadowEscape
{
    /// <summary>
    /// Lightweight completion screen controller. Displays earned stars
    /// and wires button callbacks to SceneFlowManager actions.
    /// </summary>
    public class CompletionUI : MonoBehaviour
    {
    [SerializeField] private GameObject root;
	[SerializeField] private TMP_Text headerText;
	[SerializeField] private TMP_Text starsText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        public void AssignReferences(GameObject rootObject, TMP_Text header, TMP_Text stars, Button next, Button retry, Button menu)
        {
            root = rootObject != null ? rootObject : gameObject;
            headerText = header;
            starsText = stars;
            nextButton = next;
            retryButton = retry;
            menuButton = menu;
        }

        public void Show(int stars, Action onNext, Action onRetry, Action onMenu, bool showNextButton = true)
        {
            Debug.Log($"[CompletionUI] Show called with {stars} stars, showNext={showNextButton}, root={(root != null ? root.name : "NULL")}");
            
            if (root != null)
            {
                root.SetActive(true);
                Debug.Log($"[CompletionUI] Root activated: {root.name}");
            }
            else
            {
                Debug.LogError("[CompletionUI] Root is NULL! Cannot show UI.");
            }

            if (headerText != null)
            {
                headerText.text = "Puzzle Complete";
            }

            if (starsText != null)
            {
                starsText.text = new string('\u2605', Mathf.Clamp(stars, 0, 3));
            }

            // Next 버튼 표시/숨김 (마지막 레벨에서는 숨김)
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton);
            }

            WireButton(nextButton, showNextButton ? onNext : null);
            WireButton(retryButton, onRetry);
            WireButton(menuButton, onMenu);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            WireButton(nextButton, null);
            WireButton(retryButton, null);
        }

        private static void WireButton(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(() => action.Invoke());
            }
        }
    }
}
