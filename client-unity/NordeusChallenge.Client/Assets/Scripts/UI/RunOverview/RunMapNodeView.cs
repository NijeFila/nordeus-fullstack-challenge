using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.RunOverview
{
    // One button on the branching run map. Mirrors EncounterButtonView's
    // single-label/single-button shape but carries a string node id and a
    // status badge so Locked / Available / Cleared can be styled cheaply.
    public class RunMapNodeView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text typeBadge;
        [SerializeField] private TMP_Text statusBadge;
        [SerializeField] private GameObject bossHighlight;
        [SerializeField] private GameObject eliteHighlight;
        [SerializeField] private GameObject shopHighlight;

        private string _nodeId;
        private Action<string> _onClick;

        public void Bind(
            string nodeId,
            string labelText,
            string typeText,
            string statusText,
            string nodeType,
            bool interactable,
            Action<string> onClick)
        {
            _nodeId = nodeId;
            _onClick = onClick;

            if (label != null) label.text = labelText;
            if (typeBadge != null) typeBadge.text = typeText;
            if (statusBadge != null) statusBadge.text = statusText;

            if (bossHighlight != null) bossHighlight.SetActive(nodeType == "Boss");
            if (eliteHighlight != null) eliteHighlight.SetActive(nodeType == "Elite");
            if (shopHighlight != null) shopHighlight.SetActive(nodeType == "Shop");

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
            _onClick?.Invoke(_nodeId);
        }
    }
}
