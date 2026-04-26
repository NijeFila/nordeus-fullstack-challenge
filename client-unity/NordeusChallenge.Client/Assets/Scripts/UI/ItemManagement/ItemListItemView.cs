using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ItemManagement
{
    // Single inventory row. Click selects the item; the controller then assigns
    // it to a compatible equipped slot. Mirrors MoveListItemView's style.
    public class ItemListItemView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedMarker;

        private string _itemId;
        private Action<string> _onSelect;

        public void Bind(string itemId, string labelText, Sprite iconSprite, bool isSelected, Action<string> onSelect)
        {
            _itemId = itemId;
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
            _onSelect?.Invoke(_itemId);
        }
    }
}
