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

        private string _moveId;
        private Action<string> _onSelect;

        public void Bind(string moveId, string labelText, bool isSelected, Action<string> onSelect)
        {
            _moveId = moveId;
            _onSelect = onSelect;

            if (label != null)
            {
                label.text = isSelected ? $"> {labelText}" : labelText;
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
