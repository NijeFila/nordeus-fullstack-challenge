using NordeusChallenge.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Common
{
    public class MoveInfoPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private string emptyMessage = "Select a move to see details.";
        [SerializeField] private TMP_Text emptyMessageText;

        private void Awake()
        {
            if (emptyMessageText != null && !string.IsNullOrEmpty(emptyMessage))
            {
                emptyMessageText.text = emptyMessage;
            }
        }

        public void Show(MoveDto move, Sprite iconSprite)
        {
            if (move == null)
            {
                Clear();
                return;
            }

            if (contentRoot != null) contentRoot.SetActive(true);
            if (emptyState != null) emptyState.SetActive(false);

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (nameText != null)
            {
                nameText.text = move.name;
            }

            if (categoryText != null)
            {
                categoryText.text = move.power > 0
                    ? $"{move.category} · Power {move.power}"
                    : move.category;
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.IsNullOrEmpty(move.description) ? string.Empty : move.description;
            }
        }

        public void Clear()
        {
            if (contentRoot != null) contentRoot.SetActive(false);
            if (emptyState != null) emptyState.SetActive(true);

            if (icon != null)
            {
                icon.enabled = false;
                icon.sprite = null;
            }
            if (nameText != null) nameText.text = string.Empty;
            if (categoryText != null) categoryText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
        }
    }
}
