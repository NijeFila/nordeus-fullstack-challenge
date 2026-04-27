using TMPro;
using UnityEngine;

namespace NordeusChallenge.Client.Localization
{
    // Drop-in component for static labels. Attach to any GameObject that has a
    // TextMeshProUGUI; the component fills in the translated text on enable
    // and refreshes whenever the active language changes.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Translation key, e.g. ui.main_menu.start")]
        [SerializeField] private string key;

        [Tooltip("Optional fallback. If empty, the key itself is used when no translation exists.")]
        [TextArea]
        [SerializeField] private string fallback;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Apply();
            Loc.LanguageChanged += Apply;
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= Apply;
        }

        public void SetKey(string newKey, string newFallback = null)
        {
            key = newKey;
            if (newFallback != null)
            {
                fallback = newFallback;
            }
            Apply();
        }

        private void Apply()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }
            if (_text == null) return;
            _text.text = Loc.Tr(key, fallback);
        }
    }
}
