using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    public class LevelHintDisplay : MonoBehaviour
    {
        [Tooltip("Optional root that will be toggled when showing/clearing hints. Defaults to the GameObject this script is on.")]
        [SerializeField] private GameObject container;

        [Tooltip("UI Text element that will show the hint message.")]
        [SerializeField] private Text hintText;

        private void Awake()
        {
            if (container == null)
            {
                container = gameObject;
            }

            if (hintText == null)
            {
                hintText = GetComponentInChildren<Text>();
            }

            ClearHint();
        }

        public void ShowHint(string text)
        {
            if (hintText != null)
            {
                hintText.text = text;
            }

            if (container != null)
            {
                container.SetActive(true);
            }
        }

        public void ClearHint()
        {
            if (hintText != null)
            {
                hintText.text = string.Empty;
            }

            if (container != null)
            {
                container.SetActive(false);
            }
        }
    }
}
