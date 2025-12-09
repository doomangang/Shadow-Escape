using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowEscape
{
    /// <summary>
    /// Helper component that exposes the important sub-elements of a level button prefab
    /// so that designers can wire everything up in the editor while the bootstrapper
    /// simply fills in data at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelSelectButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text starLabel;

        public Button Button => button;
        public TMP_Text TitleLabel => titleLabel;
        public TMP_Text StatusLabel => statusLabel;
        public TMP_Text StarLabel => starLabel;

        /// <summary>
        /// 에디터에서 참조가 비었을 때 자동으로 하위 컴포넌트를 찾아 채워준다.
        /// 모든 필드가 채워졌는지 여부를 반환.
        /// </summary>
        public bool EnsureBindings()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (titleLabel == null || statusLabel == null || starLabel == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);

                foreach (var text in texts)
                {
                    string lower = text.name.ToLowerInvariant();

                    if (titleLabel == null && lower.Contains("title"))
                    {
                        titleLabel = text;
                        continue;
                    }

                    if (statusLabel == null && lower.Contains("status"))
                    {
                        statusLabel = text;
                        continue;
                    }

                    if (starLabel == null && (lower.Contains("star") || lower.Contains("score")))
                    {
                        starLabel = text;
                        continue;
                    }
                }

                // 그래도 비어있으면 순서대로 채움
                if (titleLabel == null && texts.Length > 0)
                {
                    titleLabel = texts[0];
                }

                if (statusLabel == null && texts.Length > 1)
                {
                    statusLabel = texts[1];
                }

                if (starLabel == null && texts.Length > 2)
                {
                    starLabel = texts[2];
                }
            }

            bool isValid = button != null && titleLabel != null && statusLabel != null && starLabel != null;

            if (!isValid)
            {
                Debug.LogWarning($"LevelSelectButtonView: 일부 참조가 비어 있습니다. Button={button != null}, Title={titleLabel != null}, Status={statusLabel != null}, Star={starLabel != null}", this);
            }

            return isValid;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureBindings();
        }
#endif
    }
}
