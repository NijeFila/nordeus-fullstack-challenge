using System;
using NordeusChallenge.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ClassSelection
{
    // One card per hero class on the picker screen. Mirrors the lightweight
    // *ListItemView styles used by Move/Item Management — a button bound to
    // an id plus a label.
    public class HeroClassCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private GameObject selectedIndicator;

        private string _classId;
        private Action<string> _onSelected;

        public void Bind(
            string classId,
            string name,
            string description,
            string statsLine,
            string movesLine,
            Sprite portraitSprite,
            bool isSelected,
            Action<string> onSelected)
        {
            _classId = classId;
            _onSelected = onSelected;

            if (nameLabel != null) nameLabel.text = name;
            if (descriptionLabel != null) descriptionLabel.text = description;
            if (statsLabel != null) statsLabel.text = statsLine;
            if (movesLabel != null) movesLabel.text = movesLine;

            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            _onSelected?.Invoke(_classId);
        }
    }
}
