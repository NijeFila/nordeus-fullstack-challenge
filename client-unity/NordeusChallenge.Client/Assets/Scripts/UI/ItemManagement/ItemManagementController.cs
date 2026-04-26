using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ItemManagement
{
    // Minimal management screen: lists equipped items per slot (with Unequip)
    // and the inventory (with Equip). No drag/drop, no shop, no persistence.
    public class ItemManagementController : MonoBehaviour
    {
        [Header("Equipped")]
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private ItemListItemView equippedRowPrefab;

        [Header("Inventory")]
        [SerializeField] private Transform inventoryContainer;
        [SerializeField] private ItemListItemView inventoryRowPrefab;

        [Header("Selected Item")]
        [SerializeField] private TMP_Text selectedItemText;

        [Header("UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text heroSummaryText;
        [SerializeField] private Button backButton;

        private static readonly string[] SlotOrder = { "weapon", "armor", "trinket" };

        private string _selectedItemId;

        private void Start()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }

        private void Refresh()
        {
            if (GameSession.Instance == null
                || GameSession.Instance.CurrentRun == null
                || GameSession.Instance.CurrentHero == null)
            {
                SetStatus("No active run.");
                ClearContainer(equippedContainer);
                ClearContainer(inventoryContainer);
                UpdateSelectedItem();
                UpdateHeroSummary();
                return;
            }

            RenderEquipped();
            RenderInventory();
            UpdateSelectedItem();
            UpdateHeroSummary();
        }

        private void RenderEquipped()
        {
            ClearContainer(equippedContainer);
            if (equippedContainer == null || equippedRowPrefab == null) return;

            var session = GameSession.Instance;

            for (int s = 0; s < SlotOrder.Length; s++)
            {
                string slot = SlotOrder[s];
                var equipped = session.GetEquippedItemIds(slot);
                int cap = session.GetEquippedSlotCap(slot);

                if (equipped == null || equipped.Count == 0)
                {
                    var emptyRow = Instantiate(equippedRowPrefab, equippedContainer);
                    emptyRow.Bind(null, $"{Capitalize(slot)} (0/{cap}): Empty", string.Empty, false, null);
                    continue;
                }

                for (int i = 0; i < equipped.Count; i++)
                {
                    string itemId = equipped[i];
                    var item = session.GetItemById(itemId);
                    string label = $"{Capitalize(slot)} ({i + 1}/{cap}): {FormatItemLine(item, itemId)}";
                    var row = Instantiate(equippedRowPrefab, equippedContainer);
                    row.Bind(itemId, label, "Unequip", true, OnUnequipClicked);
                }
            }
        }

        private void RenderInventory()
        {
            ClearContainer(inventoryContainer);
            if (inventoryContainer == null || inventoryRowPrefab == null) return;

            var session = GameSession.Instance;
            var inventory = session.InventoryItemIds;
            if (inventory == null || inventory.Count == 0) return;

            for (int i = 0; i < inventory.Count; i++)
            {
                string itemId = inventory[i];
                var item = session.GetItemById(itemId);
                bool isEquipped = session.IsItemEquipped(itemId);
                string label = FormatItemLine(item, itemId) + (isEquipped ? "  [equipped]" : string.Empty);
                string action = isEquipped ? "Selected" : "Equip";

                var row = Instantiate(inventoryRowPrefab, inventoryContainer);
                row.Bind(itemId, label, action, !isEquipped, OnEquipClicked);
            }
        }

        private void OnEquipClicked(string itemId)
        {
            _selectedItemId = itemId;
            if (GameSession.Instance.EquipItem(itemId))
            {
                var item = GameSession.Instance.GetItemById(itemId);
                SetStatus(item != null ? $"Equipped {item.name}." : "Equipped.");
            }
            else
            {
                SetStatus("Could not equip item.");
            }
            Refresh();
        }

        private void OnUnequipClicked(string itemId)
        {
            _selectedItemId = itemId;
            if (GameSession.Instance.UnequipItem(itemId))
            {
                var item = GameSession.Instance.GetItemById(itemId);
                SetStatus(item != null ? $"Unequipped {item.name}." : "Unequipped.");
            }
            else
            {
                SetStatus("Could not unequip item.");
            }
            Refresh();
        }

        private void UpdateSelectedItem()
        {
            if (selectedItemText == null) return;

            var item = !string.IsNullOrEmpty(_selectedItemId)
                ? GameSession.Instance != null ? GameSession.Instance.GetItemById(_selectedItemId) : null
                : null;

            if (item == null)
            {
                selectedItemText.text = "Select an item to see details.";
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"<b>{item.name}</b>  ({Capitalize(item.slot)}");
            if (!string.IsNullOrEmpty(item.rarity)) sb.Append($", {item.rarity}");
            sb.Append(")");
            if (!string.IsNullOrEmpty(item.description))
            {
                sb.AppendLine();
                sb.Append(item.description);
            }
            if (item.statBonuses != null && item.statBonuses.Count > 0)
            {
                sb.AppendLine();
                for (int i = 0; i < item.statBonuses.Count; i++)
                {
                    var b = item.statBonuses[i];
                    if (b == null) continue;
                    if (i > 0) sb.Append("  ");
                    sb.Append(b.amount >= 0 ? $"+{b.amount} {b.stat}" : $"{b.amount} {b.stat}");
                }
            }
            selectedItemText.text = sb.ToString();
        }

        private void UpdateHeroSummary()
        {
            if (heroSummaryText == null) return;

            var hero = GameSession.Instance != null ? GameSession.Instance.CurrentHero : null;
            if (hero == null || hero.stats == null)
            {
                heroSummaryText.text = string.Empty;
                return;
            }

            var bonuses = GameSession.Instance.GetEquippedItemStatBonuses();
            heroSummaryText.text =
                $"{hero.name} (Lv {hero.level})\n" +
                $"HP {hero.stats.maxHealth}{Bonus(bonuses.maxHealth)}  |  " +
                $"MP {hero.stats.maxMana}{Bonus(bonuses.maxMana)}\n" +
                $"ATK {hero.stats.attack}{Bonus(bonuses.attack)}  |  " +
                $"DEF {hero.stats.defense}{Bonus(bonuses.defense)}  |  " +
                $"MAG {hero.stats.magic}{Bonus(bonuses.magic)}";
        }

        private static string Bonus(int amount)
        {
            if (amount == 0) return string.Empty;
            return amount > 0 ? $" (+{amount})" : $" ({amount})";
        }

        private static string FormatItemLine(ItemDto item, string fallbackId)
        {
            if (item == null) return fallbackId;
            return string.IsNullOrEmpty(item.rarity) ? item.name : $"{item.name} [{item.rarity}]";
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
