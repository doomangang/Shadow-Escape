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

        [Header("Star UI Objects (child of completion panel)")]
        [SerializeField] private GameObject[] starObjects;
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

            UpdateStarObjects(stars);

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

        private void UpdateStarObjects(int stars)
        {
            if (starObjects == null || starObjects.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[CompletionUI] No star objects assigned!");
                return;
            }

            // 0~3 범위로 클램프
            int clampedStars = Mathf.Clamp(stars, 0, starObjects.Length);

            // 먼저 전부 끄기
            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].SetActive(false);
                }
            }

            // 0개면 아무것도 안 켬
            if (clampedStars <= 0)
            {
                return;
            }

            // 예: clampedStars = 1 → index 0
            //     clampedStars = 2 → index 1
            //     clampedStars = 3 → index 2
            int activeIndex = clampedStars - 1;

            if (activeIndex >= 0 && activeIndex < starObjects.Length && starObjects[activeIndex] != null)
            {
                for(int i = 0; i <= activeIndex; i++)
                {
                    if(starObjects[i] != null)
                    {
                        starObjects[i].SetActive(true);
                        UnityEngine.Debug.Log($"[CompletionUI] Activated star object at index {i}: {starObjects[i].name}");
                    }
                }
            }
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
