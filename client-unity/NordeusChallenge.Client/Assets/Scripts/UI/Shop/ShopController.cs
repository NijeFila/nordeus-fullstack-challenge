using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
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
        [SerializeField] private NordeusChallenge.Client.Visual.VisualCatalog visualCatalog;

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
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
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

                string headline = $"{LocalizedNames.Name(offer)} ({LocalizedNames.OfferType(offer.type)})";
                string priceLabel = $"{offer.price}g";

                Sprite iconSprite = null;
                if (visualCatalog != null)
                {
                    if (offer.type == "Item") iconSprite = visualCatalog.GetItemIcon(offer.itemId);
                    else if (offer.type == "StatUpgrade") iconSprite = visualCatalog.GetStatIcon(offer.stat);
                }

                var view = Instantiate(offerPrefab, offersContainer);
                view.Bind(offer.id, headline, priceLabel, selected, owned, affordable, iconSprite, OnOfferSelected);
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
                SetStatus(Loc.Tr("shop.select_first", "Select an offer first."));
                return;
            }

            if (GameSession.Instance.PurchaseShopOffer(offer, out string reason))
            {
                SetStatus(BuildPurchaseMessage(offer));
            }
            else
            {
                SetStatus(string.IsNullOrEmpty(reason) ? Loc.Tr("shop.could_not_buy", "Could not buy this offer.") : reason);
            }

            Refresh();
        }

        private static string BuildPurchaseMessage(ShopOfferDto offer)
        {
            string offerName = LocalizedNames.Name(offer);
            switch (offer.type)
            {
                case "Item":
                    return string.Format(Loc.Tr("shop.bought_item", "Bought {0}. Added to inventory."), offerName);
                case "StatUpgrade":
                    return string.Format(Loc.Tr("shop.bought_stat", "Bought {0}. {1}"), offerName, FormatStatUpgrade(offer.stat, offer.amount));
                default:
                    return string.Format(Loc.Tr("shop.bought_generic", "Bought {0}."), offerName);
            }
        }

        private static string FormatStatUpgrade(string stat, int amount)
        {
            string statName = LocalizedNames.Stat(stat);
            return amount >= 0
                ? string.Format(Loc.Tr("shop.stat_upgrade", "{0} +{1}."), statName, amount)
                : string.Format(Loc.Tr("shop.stat_upgrade_neg", "{0} {1}."), statName, amount);
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
                string label = Loc.Tr("shop.buy", "Buy");
                if (offer != null && GameSession.Instance != null)
                {
                    if (GameSession.Instance.IsShopOfferAlreadyOwned(offer))
                    {
                        label = Loc.Tr("shop.owned", "Owned");
                    }
                    else if (!GameSession.Instance.CanAffordShopOffer(offer))
                    {
                        label = string.Format(Loc.Tr("shop.need_gold", "Need {0}g"), offer.price);
                    }
                    else
                    {
                        canBuy = true;
                        label = string.Format(Loc.Tr("shop.buy_with_price", "Buy ({0}g)"), offer.price);
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
                return Loc.Tr("shop.detail_hint", "Select an offer to see details.");
            }

            var sb = new StringBuilder();
            sb.Append($"<b>{LocalizedNames.Name(offer)}</b>");
            sb.AppendLine();
            sb.Append(string.Format(Loc.Tr("shop.price_label", "Price: {0}g  |  Type: {1}"), offer.price, LocalizedNames.OfferType(offer.type)));

            if (offer.type == "Item" && !string.IsNullOrEmpty(offer.itemId))
            {
                var item = GameSession.Instance != null
                    ? GameSession.Instance.GetItemById(offer.itemId)
                    : null;
                if (item != null)
                {
                    sb.AppendLine();
                    sb.Append($"{Loc.Tr("shop.type.Item", "Item")}: {LocalizedNames.Name(item)}");
                    if (!string.IsNullOrEmpty(item.slot)) sb.Append($"  ({LocalizedNames.Slot(item.slot)})");
                    if (item.statBonuses != null && item.statBonuses.Count > 0)
                    {
                        sb.AppendLine();
                        for (int i = 0; i < item.statBonuses.Count; i++)
                        {
                            var b = item.statBonuses[i];
                            if (b == null) continue;
                            if (i > 0) sb.Append("  ");
                            string sign = b.amount >= 0 ? "+" : string.Empty;
                            sb.Append($"{sign}{b.amount} {LocalizedNames.Stat(b.stat)}");
                        }
                    }
                }
            }
            else if (offer.type == "StatUpgrade" && !string.IsNullOrEmpty(offer.stat))
            {
                sb.AppendLine();
                sb.Append(FormatStatUpgrade(offer.stat, offer.amount));
            }

            string desc = LocalizedNames.Description(offer);
            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine();
                sb.Append(desc);
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
            goldText.text = string.Format(Loc.Tr("ui.run.gold", "Gold: {0}"), gold);
        }

        private void OnBackClicked()
        {
            var session = GameSession.Instance;

            // Endless shop floors advance one floor on exit so the next floor
            // (battle/elite/boss) is queued for RunOverview.
            if (session != null && session.CurrentRunMode == RunMode.Endless && session.IsEndlessShopFloor)
            {
                session.AdvanceEndlessShopFloor();
            }

            // If the player came from a Shop map node, clearing it on exit
            // unlocks the connected nodes. No-op when shop was reached from
            // the legacy "Shop" button on the run overview.
            if (session != null && session.HasMap && !string.IsNullOrEmpty(session.SelectedMapNodeId))
            {
                var node = session.GetMapNodeById(session.SelectedMapNodeId);
                if (node != null && node.type == "Shop" && !session.IsMapNodeCleared(node.id))
                {
                    session.MarkSelectedMapNodeCleared();
                }
            }

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
