using System.Text;
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
                categoryText.text = BuildCategoryLine(move);
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

        private static string BuildCategoryLine(MoveDto move)
        {
            var sb = new StringBuilder();
            sb.Append(move.category);
            if (move.power > 0)
            {
                sb.Append($" · Power {move.power}");
            }
            if (move.manaCost > 0)
            {
                sb.Append($" · {move.manaCost} MP");
            }
            if (move.hpCost > 0)
            {
                sb.Append($" · {move.hpCost} HP");
            }
            if (move.effect != null && !string.IsNullOrEmpty(move.effect.kind))
            {
                sb.Append($" · {move.effect.kind}");
                if (move.effect.amount != 0)
                {
                    sb.Append($" {move.effect.amount}");
                }
                if (move.effect.durationTurns > 0 && move.effect.kind != "Heal")
                {
                    sb.Append($" / {move.effect.durationTurns}t");
                }
            }
            return sb.ToString();
        }
    }
}
