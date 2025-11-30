using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Lightweight completion screen controller. Displays earned stars
    /// and wires button callbacks to SceneFlowManager actions.
    /// </summary>
    public class CompletionUI : MonoBehaviour
    {
    [SerializeField] private GameObject root;
	[SerializeField] private Text headerText;
	[SerializeField] private Text starsText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        public void AssignReferences(GameObject rootObject, Text header, Text stars, Button next, Button retry)
        {
            root = rootObject != null ? rootObject : gameObject;
            headerText = header;
            starsText = stars;
            nextButton = next;
            retryButton = retry;
        }

        public void Show(int stars, Action onNext, Action onRetry)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (headerText != null)
            {
                headerText.text = "Puzzle Complete";
            }

            if (starsText != null)
            {
                starsText.text = new string('\u2605', Mathf.Clamp(stars, 0, 3));
            }

            WireButton(nextButton, onNext);
            WireButton(retryButton, onRetry);
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
