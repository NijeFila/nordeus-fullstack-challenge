using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Shop
{
    // Shop screen: select an offer from the list, see its details, click Buy.
    // No selling, no refresh, no random prices — the catalog comes straight
    // from RunConfigResponseDto.shopOffers and is static for the run.
    public class ShopController : MonoBehaviour
    {
        [Header("Offers")]
        [SerializeField] private Transform offersContainer;
        [SerializeField] private ShopOfferView offerPrefab;

        [Header("Selected Offer")]
        [SerializeField] private TMP_Text selectedOfferText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonLabel;

        [Header("UI")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button backButton;

        private string _selectedOfferId;

        private void Start()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);

            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        private void Refresh()
        {
            if (GameSession.Instance == null
                || GameSession.Instance.CurrentRun == null
                || GameSession.Instance.CurrentHero == null)
            {
                SetStatus("No active run.");
                ClearOffers();
                UpdateGoldText();
                UpdateSelectedOffer();
                return;
            }

            UpdateGoldText();
            RenderOffers();
            UpdateSelectedOffer();
        }

        private void RenderOffers()
        {
            ClearOffers();
            if (offersContainer == null || offerPrefab == null) return;

            var session = GameSession.Instance;
            var offers = session.CurrentRun.shopOffers;
            if (offers == null || offers.Count == 0) return;

            for (int i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                if (offer == null) continue;

                bool owned = session.IsShopOfferAlreadyOwned(offer);
                bool affordable = session.CanAffordShopOffer(offer);
                bool selected = offer.id == _selectedOfferId;

                string headline = $"{offer.name} ({offer.type})";
                string priceLabel = $"{offer.price}g";

                var view = Instantiate(offerPrefab, offersContainer);
                view.Bind(offer.id, headline, priceLabel, selected, owned, affordable, OnOfferSelected);
            }
        }

        private void OnOfferSelected(string offerId)
        {
            _selectedOfferId = offerId;
            SetStatus(string.Empty);
            Refresh();
        }

        private void OnBuyClicked()
        {
            var offer = FindSelectedOffer();
            if (offer == null)
            {
                SetStatus("Select an offer first.");
                return;
            }

            if (GameSession.Instance.PurchaseShopOffer(offer, out string reason))
            {
                SetStatus(BuildPurchaseMessage(offer));
            }
            else
            {
                SetStatus(string.IsNullOrEmpty(reason) ? "Could not buy this offer." : reason);
            }

            Refresh();
        }

        private static string BuildPurchaseMessage(ShopOfferDto offer)
        {
            switch (offer.type)
            {
                case "Item":
                    return $"Bought {offer.name}. Added to inventory.";
                case "StatUpgrade":
                    return $"Bought {offer.name}. {FormatStatUpgrade(offer.stat, offer.amount)}";
                default:
                    return $"Bought {offer.name}.";
            }
        }

        private static string FormatStatUpgrade(string stat, int amount)
        {
            string sign = amount >= 0 ? "+" : string.Empty;
            switch (stat)
            {
                case "maxHealth": return $"Max Health {sign}{amount}.";
                case "maxMana":   return $"Max Mana {sign}{amount}.";
                case "attack":    return $"Attack {sign}{amount}.";
                case "defense":   return $"Defense {sign}{amount}.";
                case "magic":     return $"Magic {sign}{amount}.";
                default:          return $"{stat} {sign}{amount}.";
            }
        }

        private void UpdateSelectedOffer()
        {
            var offer = FindSelectedOffer();

            if (selectedOfferText != null)
            {
                selectedOfferText.text = BuildOfferDetails(offer);
            }

            if (buyButton != null)
            {
                bool canBuy = false;
                string label = "Buy";
                if (offer != null && GameSession.Instance != null)
                {
                    if (GameSession.Instance.IsShopOfferAlreadyOwned(offer))
                    {
                        label = "Owned";
                    }
                    else if (!GameSession.Instance.CanAffordShopOffer(offer))
                    {
                        label = $"Need {offer.price}g";
                    }
                    else
                    {
                        canBuy = true;
                        label = $"Buy ({offer.price}g)";
                    }
                }
                buyButton.interactable = canBuy;
                if (buyButtonLabel != null) buyButtonLabel.text = label;
            }
        }

        private string BuildOfferDetails(ShopOfferDto offer)
        {
            if (offer == null)
            {
                return "Select an offer to see details.";
            }

            var sb = new StringBuilder();
            sb.Append($"<b>{offer.name}</b>");
            sb.AppendLine();
            sb.Append($"Price: {offer.price}g  |  Type: {offer.type}");

            if (offer.type == "Item" && !string.IsNullOrEmpty(offer.itemId))
            {
                var item = GameSession.Instance != null
                    ? GameSession.Instance.GetItemById(offer.itemId)
                    : null;
                if (item != null)
                {
                    sb.AppendLine();
                    sb.Append($"Item: {item.name}");
                    if (!string.IsNullOrEmpty(item.slot)) sb.Append($"  ({item.slot})");
                    if (item.statBonuses != null && item.statBonuses.Count > 0)
                    {
                        sb.AppendLine();
                        for (int i = 0; i < item.statBonuses.Count; i++)
                        {
                            var b = item.statBonuses[i];
                            if (b == null) continue;
                            if (i > 0) sb.Append("  ");
                            string sign = b.amount >= 0 ? "+" : string.Empty;
                            sb.Append($"{sign}{b.amount} {b.stat}");
                        }
                    }
                }
            }
            else if (offer.type == "StatUpgrade" && !string.IsNullOrEmpty(offer.stat))
            {
                sb.AppendLine();
                sb.Append(FormatStatUpgrade(offer.stat, offer.amount));
            }

            if (!string.IsNullOrEmpty(offer.description))
            {
                sb.AppendLine();
                sb.Append(offer.description);
            }

            return sb.ToString();
        }

        private ShopOfferDto FindSelectedOffer()
        {
            if (string.IsNullOrEmpty(_selectedOfferId)) return null;
            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null) return null;

            var offers = GameSession.Instance.CurrentRun.shopOffers;
            if (offers == null) return null;

            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] != null && offers[i].id == _selectedOfferId) return offers[i];
            }
            return null;
        }

        private void UpdateGoldText()
        {
            if (goldText == null) return;
            int gold = GameSession.Instance != null ? GameSession.Instance.CurrentGold : 0;
            goldText.text = $"Gold: {gold}";
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private void ClearOffers()
        {
            if (offersContainer == null) return;
            for (int i = offersContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(offersContainer.GetChild(i).gameObject);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }
    }
}
