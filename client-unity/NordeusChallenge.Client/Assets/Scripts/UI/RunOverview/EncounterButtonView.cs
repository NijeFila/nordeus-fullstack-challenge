using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.RunOverview
{
    public class EncounterButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private int _encounterIndex;
        private Action<int> _onClick;

        public void Bind(int encounterIndex, string labelText, bool interactable, Action<int> onClick)
        {
            _encounterIndex = encounterIndex;
            _onClick = onClick;

            if (label != null)
            {
                label.text = labelText;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = interactable;
                if (interactable)
                {
                    button.onClick.AddListener(OnClicked);
                }
            }
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_encounterIndex);
        }
    }
}
