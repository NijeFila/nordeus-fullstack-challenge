using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ItemManagement
{
    public class ItemListItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text actionText;

        private string _itemId;
        private Action<string> _onClicked;

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

        public void Bind(string itemId, string label, string action, bool interactable, Action<string> onClicked)
        {
            _itemId = itemId;
            _onClicked = onClicked;

            if (labelText != null) labelText.text = label;
            if (actionText != null) actionText.text = action ?? string.Empty;
            if (button != null) button.interactable = interactable;
        }

        private void OnClicked()
        {
            if (_onClicked != null && !string.IsNullOrEmpty(_itemId)) _onClicked(_itemId);
        }
    }
}
