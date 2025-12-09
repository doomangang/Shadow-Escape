using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    // Runtime helper for the Level Select scene.
    // Expects a container GameObject with child Buttons (in order: level 0, level 1, ...).
    public class LevelSelectUIController : MonoBehaviour
    {
        [Tooltip("Parent that contains level buttons as children (in level index order)")]
        public RectTransform buttonsContainer;

        [Tooltip("Reference to LevelSelectManager that will be used to load levels")]
        public LevelSelectManager levelSelectManager;

        [Tooltip("Optional: if true, will show star count in child Text named 'Stars' under each button")]
        public bool showStars = true;

        private void Start()
        {
            if (buttonsContainer == null)
            {
                Debug.LogWarning("LevelSelectUIController: buttonsContainer not set.");
                return;
            }

            if (levelSelectManager == null)
            {
                Debug.LogWarning("LevelSelectUIController: levelSelectManager not set.");
            }

            var buttons = buttonsContainer.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                int idx = i; // capture
                var btn = buttons[i];
                btn.onClick.RemoveAllListeners();
                if (levelSelectManager != null)
                {
                    btn.onClick.AddListener(() => levelSelectManager.LoadLevel(idx));
                }

                bool unlocked = true;
                if (GameManager.Instance != null)
                {
                    unlocked = GameManager.Instance.IsLevelUnlocked(idx);
                }

                btn.interactable = unlocked || (GameManager.Instance != null && GameManager.Instance.IsTester);

                // Try to find a child UI element named "LockOverlay" and toggle it
                var lockTf = btn.transform.Find("LockOverlay");
                if (lockTf != null)
                {
                    lockTf.gameObject.SetActive(!btn.interactable);
                }

                if (showStars && GameManager.Instance != null && GameManager.Instance.CurrentSave != null)
                {
                    var starsTf = btn.transform.Find("Stars");
                    if (starsTf != null)
                    {
                        var txt = starsTf.GetComponent<UnityEngine.UI.Text>();
                        if (txt != null)
                        {
                            int stars = 0;
                            if (idx >= 0 && idx < GameManager.Instance.CurrentSave.starsEarnedAtLevel.Length)
                                stars = GameManager.Instance.CurrentSave.starsEarnedAtLevel[idx];
                            txt.text = new string('\u2605', Mathf.Clamp(stars, 0, 3));
                        }
                    }
                }
            }
        }
    }
}
