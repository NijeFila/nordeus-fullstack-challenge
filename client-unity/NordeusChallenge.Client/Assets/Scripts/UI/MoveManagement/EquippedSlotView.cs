using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.MoveManagement
{
    public class EquippedSlotView : MonoBehaviour
    {
        [SerializeField] private Button assignButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        private int _slotIndex;
        private Action<int> _onAssign;
        private Action<int> _onClear;

        public void Bind(
            int slotIndex,
            string labelText,
            Sprite iconSprite,
            bool hasMove,
            bool assignEnabled,
            Action<int> onAssign,
            Action<int> onClear)
        {
            _slotIndex = slotIndex;
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
                clearButton.interactable = hasMove;
                clearButton.onClick.AddListener(OnClearClicked);
            }
        }

        private void OnAssignClicked()
        {
            _onAssign?.Invoke(_slotIndex);
        }

        private void OnClearClicked()
        {
            _onClear?.Invoke(_slotIndex);
        }
    }
}
