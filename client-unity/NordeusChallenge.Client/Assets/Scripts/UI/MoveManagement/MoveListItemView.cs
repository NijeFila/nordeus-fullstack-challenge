using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.MoveManagement
{
    public class MoveListItemView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedMarker;

        private string _moveId;
        private Action<string> _onSelect;

        public void Bind(string moveId, string labelText, Sprite iconSprite, bool isSelected, Action<string> onSelect)
        {
            _moveId = moveId;
            _onSelect = onSelect;

            if (label != null)
            {
                label.text = labelText;
            }

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(isSelected);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        private void OnSelectClicked()
        {
            _onSelect?.Invoke(_moveId);
        }
    }
}
