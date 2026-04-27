using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.Localization
{
    // Connects either a TMP_Dropdown or a pair of buttons to the localization
    // service. Either path is optional — assign whichever fits the Main Menu
    // layout in the editor.
    public class LanguageSelector : MonoBehaviour
    {
        [Header("Dropdown (optional)")]
        [SerializeField] private TMP_Dropdown dropdown;

        [Header("Buttons (optional)")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button serbianLatinButton;

        // Order must match Loc.SupportedLanguages — index 0 = English, index 1 = Serbian Latin.
        private static readonly string[] OrderedCodes = { Loc.English, Loc.SerbianLatin };
        private static readonly string[] OrderedLabels = { "English", "Srpski (latinica)" };

        private void Start()
        {
            SetupDropdown();
            SetupButtons();
        }

        private void OnDestroy()
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }
            if (englishButton != null)
            {
                englishButton.onClick.RemoveListener(OnEnglishClicked);
            }
            if (serbianLatinButton != null)
            {
                serbianLatinButton.onClick.RemoveListener(OnSerbianClicked);
            }
        }

        private void SetupDropdown()
        {
            if (dropdown == null) return;

            dropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(OrderedLabels.Length);
            for (int i = 0; i < OrderedLabels.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(OrderedLabels[i]));
            }
            dropdown.AddOptions(options);

            int currentIndex = IndexOf(Loc.CurrentLanguage);
            dropdown.SetValueWithoutNotify(currentIndex);
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void SetupButtons()
        {
            if (englishButton != null)
            {
                englishButton.onClick.AddListener(OnEnglishClicked);
            }
            if (serbianLatinButton != null)
            {
                serbianLatinButton.onClick.AddListener(OnSerbianClicked);
            }
        }

        private void OnDropdownChanged(int index)
        {
            if (index < 0 || index >= OrderedCodes.Length) return;
            Loc.SetLanguage(OrderedCodes[index]);
        }

        private void OnEnglishClicked() => Loc.SetLanguage(Loc.English);
        private void OnSerbianClicked() => Loc.SetLanguage(Loc.SerbianLatin);

        private static int IndexOf(string code)
        {
            for (int i = 0; i < OrderedCodes.Length; i++)
            {
                if (OrderedCodes[i] == code) return i;
            }
            return 0;
        }
    }
}
