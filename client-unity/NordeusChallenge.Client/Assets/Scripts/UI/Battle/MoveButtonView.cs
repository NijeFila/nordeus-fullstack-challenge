using System;
using System.Text;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    public class MoveButtonView : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private Image icon;

        private MoveDto _move;
        private Action<MoveDto> _onClick;
        private Action<MoveDto> _onHover;
        private bool _affordable = true;
        private bool _externallyEnabled = true;

        public MoveDto Move => _move;

        public void Bind(MoveDto move, Sprite iconSprite, Action<MoveDto> onClick, Action<MoveDto> onHover = null)
        {
            _move = move;
            _onClick = onClick;
            _onHover = onHover;

            if (label != null)
            {
                label.text = LocalizedNames.Name(move);
            }

            UpdateCostLabel();

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        public void SetAffordable(bool affordable)
        {
            _affordable = affordable;
            ApplyInteractable();
        }

        public void SetInteractableExternal(bool enabled)
        {
            _externallyEnabled = enabled;
            ApplyInteractable();
        }

        private void ApplyInteractable()
        {
            if (button != null)
            {
                button.interactable = _externallyEnabled && _affordable;
            }
        }

        private void UpdateCostLabel()
        {
            if (costLabel == null) return;
            if (_move == null)
            {
                costLabel.text = string.Empty;
                return;
            }

            var sb = new StringBuilder();
            if (_move.manaCost > 0) sb.Append($"{_move.manaCost} {Loc.Tr("label.mp", "MP")}");
            if (_move.hpCost > 0)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append($"{_move.hpCost} {Loc.Tr("label.hp", "HP")}");
            }
            costLabel.text = sb.ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHover?.Invoke(_move);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _onHover?.Invoke(_move);
        }

        private void OnClicked()
        {
            _onHover?.Invoke(_move);
            _onClick?.Invoke(_move);
        }
    }
}
