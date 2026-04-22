using System;
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

        private MoveDto _move;
        private Action<MoveDto> _onClick;
        private Action<MoveDto> _onHover;

        public void Bind(MoveDto move, Action<MoveDto> onClick, Action<MoveDto> onHover = null)
        {
            _move = move;
            _onClick = onClick;
            _onHover = onHover;

            if (label != null)
            {
                label.text = move != null ? move.name : string.Empty;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
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
