using UnityEngine;
using TMPro;
using System;
using System.Collections;

namespace ShadowEscape.UI
{
    public class HintUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text clearTimeText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text guideText;
        
        [Header("Auto Hide Settings")]
        [SerializeField] private float displayDuration = 3f;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        public void Show(string hint, float threeStarTime, string levelName, string guide, float duration, Action onComplete)
        {
            Debug.Log($"[HintUI] Show called - Level: {levelName}, Hint: {hint}, Duration: {duration}s");
            
            if (root != null)
            {
                root.SetActive(true);
                Debug.Log($"[HintUI] Root activated: {root.name}");
            }
            else
            {
                Debug.LogError("[HintUI] Root is NULL!");
            }

            if (hintText != null)
            {
                hintText.text = hint;
            }

            if (clearTimeText != null)
            {
                clearTimeText.text = $"Clear Time: {threeStarTime:F0}s";
            }

            if (levelText != null)
            {
                levelText.text = levelName;
            }

            if (guideText != null)
            {
                guideText.text = guide;
            }

            StartCoroutine(AutoHideAfterDelay(duration, onComplete));
        }

        private IEnumerator AutoHideAfterDelay(float duration, Action onComplete)
        {
            yield return new WaitForSecondsRealtime(duration);
            Hide();
            onComplete?.Invoke();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
