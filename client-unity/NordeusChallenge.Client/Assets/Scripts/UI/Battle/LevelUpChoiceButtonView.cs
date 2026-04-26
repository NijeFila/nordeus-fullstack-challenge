using System;
using NordeusChallenge.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    public class LevelUpChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text descriptionText;

        private LevelUpChoiceDto _choice;
        private Action<LevelUpChoiceDto> _onClicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Bind(LevelUpChoiceDto choice, Action<LevelUpChoiceDto> onClicked)
        {
            _choice = choice;
            _onClicked = onClicked;

            if (labelText != null)
            {
                labelText.text = choice != null ? choice.name : string.Empty;
            }
            if (descriptionText != null)
            {
                descriptionText.text = choice != null ? choice.description : string.Empty;
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void OnClicked()
        {
            if (_onClicked != null && _choice != null)
            {
                _onClicked(_choice);
            }
        }
    }
}
