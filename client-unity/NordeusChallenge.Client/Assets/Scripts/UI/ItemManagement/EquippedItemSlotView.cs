using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ItemManagement
{
    // A single equipped item slot row. Mirrors MoveManagement's EquippedSlotView:
    // the slot has an Assign button (active when a compatible inventory item is
    // selected) and a Clear button (active when the slot holds an item).
    public class EquippedItemSlotView : MonoBehaviour
    {
        [SerializeField] private Button assignButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        // Composite slot id, e.g. "weapon", "armor", "trinket:0", "trinket:1".
        private string _slotKey;
        private Action<string> _onAssign;
        private Action<string> _onClear;

        public void Bind(
            string slotKey,
            string labelText,
            Sprite iconSprite,
            bool hasItem,
            bool assignEnabled,
            Action<string> onAssign,
            Action<string> onClear)
        {
            _slotKey = slotKey;
            _onAssign = onAssign;
            _onClear = onClear;

            if (label != null)
            {
                label.text = labelText;
            }

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (assignButton != null)
            {
                assignButton.onClick.RemoveAllListeners();
                assignButton.interactable = assignEnabled;
                assignButton.onClick.AddListener(OnAssignClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.interactable = hasItem;
                clearButton.onClick.AddListener(OnClearClicked);
            }
        }

        private void OnAssignClicked()
        {
            _onAssign?.Invoke(_slotKey);
        }

        private void OnClearClicked()
        {
            _onClear?.Invoke(_slotKey);
        }
    }
}
