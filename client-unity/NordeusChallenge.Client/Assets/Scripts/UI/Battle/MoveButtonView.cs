using System;
using NordeusChallenge.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    public class MoveButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private MoveDto _move;
        private Action<MoveDto> _onClick;

        public void Bind(MoveDto move, Action<MoveDto> onClick)
        {
            _move = move;
            _onClick = onClick;

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

        private void OnClicked()
        {
            _onClick?.Invoke(_move);
        }
    }
}
