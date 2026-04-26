using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Shop
{
    // A single shop row. Click selects the offer; the controller handles the
    // actual purchase via a separate Buy button so accidental clicks do not
    // spend gold.
    public class ShopOfferView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private GameObject selectedMarker;
        [SerializeField] private GameObject ownedMarker;

        private string _offerId;
        private Action<string> _onSelect;

        public void Bind(
            string offerId,
            string labelText,
            string priceLabel,
            bool isSelected,
            bool isOwned,
            bool affordable,
            Action<string> onSelect)
        {
            _offerId = offerId;
            _onSelect = onSelect;

            if (label != null) label.text = labelText;
            if (priceText != null) priceText.text = priceLabel;

            if (selectedMarker != null) selectedMarker.SetActive(isSelected);
            if (ownedMarker != null) ownedMarker.SetActive(isOwned);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.interactable = !isOwned && affordable || isSelected || (!isOwned);
                selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        private void OnSelectClicked()
        {
            _onSelect?.Invoke(_offerId);
        }
    }
}
