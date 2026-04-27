using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.RunOverview
{
    // One button on the branching run map. Rebuilt for a polished token-like design.
    public class RunMapNodeView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image typeIcon;
        [SerializeField] private Image highlight;

        [Header("State Visuals")]
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color clearedColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        [Header("Icons")]
        [SerializeField] private Sprite battleSprite;
        [SerializeField] private Sprite eliteSprite;
        [SerializeField] private Sprite shopSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite treasureSprite;
        [SerializeField] private Sprite eventSprite;
        [SerializeField] private Sprite restSprite;

        private string _nodeId;
        private Action<string> _onClick;

        public void Bind(
            string nodeId,
            string labelText,
            string typeText,
            string statusText,
            string nodeType,
            bool interactable,
            bool isCurrent,
            Action<string> onClick)
        {
            _nodeId = nodeId;
            _onClick = onClick;

            if (label != null)
            {
                label.text = labelText;
                label.gameObject.SetActive(false); // Hide label for the new design
            }

            // Set Sprite based on type
            if (typeIcon != null)
            {
                typeIcon.sprite = GetSpriteForType(nodeType);
                bool cleared = statusText == "Cleared";
                
                if (cleared)
                {
                    typeIcon.color = clearedColor;
                }
                else if (interactable)
                {
                    typeIcon.color = availableColor;
                }
                else
                {
                    typeIcon.color = lockedColor;
                }
            }

            // Highlighting
            if (highlight != null)
            {
                if (isCurrent)
                {
                    highlight.gameObject.SetActive(true);
                    highlight.color = new Color(0.2f, 0.8f, 1f, 0.8f); // Cyan for current
                }
                else if (interactable)
                {
                    highlight.gameObject.SetActive(true);
                    highlight.color = new Color(1f, 0.9f, 0.5f, 0.6f); // Yellow for reachable
                }
                else if (nodeType == "Boss")
                {
                    highlight.gameObject.SetActive(true);
                    highlight.color = new Color(1f, 0.2f, 0.2f, 0.4f); // Red glow for boss
                }
                else
                {
                    highlight.gameObject.SetActive(false);
                }
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

        private Sprite GetSpriteForType(string type)
        {
            switch (type)
            {
                case "Battle":   return battleSprite;
                case "Elite":    return eliteSprite;
                case "Shop":     return shopSprite;
                case "Boss":     return bossSprite;
                case "Treasure": return treasureSprite;
                case "Event":    return eventSprite;
                case "Rest":     return restSprite;
                default:         return battleSprite;
            }
        }

        private void OnClicked()
        {
            _onClick?.Invoke(_nodeId);
        }
    }
}
