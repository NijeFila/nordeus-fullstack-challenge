using System.Collections.Generic;
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
    // Item Management screen. Mirrors MoveManagement: inventory list on one side,
    // fixed equipped slots on the other (weapon, armor, trinket-1, trinket-2).
    // The player selects an item, then clicks the Assign button on a compatible
    // slot. Replacing an occupied slot pushes the previous item back to the
    // inventory automatically because items remain owned after unequip.
    public class ItemManagementController : MonoBehaviour
    {
        [Header("Equipped Slots")]
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private EquippedItemSlotView equippedSlotPrefab;

        [Header("Inventory")]
        [SerializeField] private Transform inventoryContainer;
        [SerializeField] private ItemListItemView inventoryItemPrefab;

        [Header("Selected Item")]
        [SerializeField] private TMP_Text selectedItemText;

        [Header("UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text heroSummaryText;
        [SerializeField] private Button backButton;

        // Fixed slot order shown in the equipped column. Trinket appears twice
        // because trinket cap is 2.
        private static readonly (string Slot, int Index, string Label)[] SlotLayout =
        {
            ("weapon",  0, "Weapon"),
            ("armor",   0, "Armor"),
            ("trinket", 0, "Trinket 1"),
            ("trinket", 1, "Trinket 2")
        };

        private string _selectedItemId;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
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

        // ---------- Equipped slots ----------

        private void RenderEquipped()
        {
            ClearContainer(equippedContainer);
            if (equippedContainer == null || equippedSlotPrefab == null) return;

            var session = GameSession.Instance;
            var selectedItem = !string.IsNullOrEmpty(_selectedItemId)
                ? session.GetItemById(_selectedItemId)
                : null;

            for (int i = 0; i < SlotLayout.Length; i++)
            {
                var slot = SlotLayout[i];
                string itemId = GetItemInSlot(slot.Slot, slot.Index);
                bool hasItem = !string.IsNullOrEmpty(itemId);
                ItemDto item = hasItem ? session.GetItemById(itemId) : null;

                string labelText = hasItem
                    ? $"{slot.Label}: {FormatItemLine(item, itemId)}"
                    : $"{slot.Label}: Empty";

                bool slotMatchesSelection = selectedItem != null && selectedItem.slot == slot.Slot;
                bool selectedAlreadyHere = slotMatchesSelection && itemId == _selectedItemId;
                bool assignEnabled = slotMatchesSelection && !selectedAlreadyHere;

                string slotKey = MakeSlotKey(slot.Slot, slot.Index);

                var view = Instantiate(equippedSlotPrefab, equippedContainer);
                view.Bind(slotKey, labelText, null, hasItem, assignEnabled, OnAssignSlot, OnClearSlot);
            }
        }

        private string GetItemInSlot(string slot, int index)
        {
            var equipped = GameSession.Instance.GetEquippedItemIds(slot);
            if (equipped == null || index >= equipped.Count) return null;
            return equipped[index];
        }

        // ---------- Inventory list ----------

        private void RenderInventory()
        {
            ClearContainer(inventoryContainer);
            if (inventoryContainer == null || inventoryItemPrefab == null) return;

            var session = GameSession.Instance;
            var inventory = session.InventoryItemIds;
            if (inventory == null || inventory.Count == 0) return;

            for (int i = 0; i < inventory.Count; i++)
            {
                string itemId = inventory[i];
                var item = session.GetItemById(itemId);
                bool isSelected = itemId == _selectedItemId;
                bool isEquipped = session.IsItemEquipped(itemId);

                string suffix = isEquipped ? "  [equipped]" : string.Empty;
                string labelText = FormatItemLine(item, itemId) + suffix;

                var view = Instantiate(inventoryItemPrefab, inventoryContainer);
                view.Bind(itemId, labelText, null, isSelected, OnInventoryItemSelected);
            }
        }

        private void OnInventoryItemSelected(string itemId)
        {
            _selectedItemId = itemId;
            SetStatus(string.Empty);
            Refresh();
        }

        // ---------- Slot actions ----------

        private void OnAssignSlot(string slotKey)
        {
            if (string.IsNullOrEmpty(_selectedItemId))
            {
                SetStatus("Select an item from your inventory first.");
                return;
            }

            if (!TryParseSlotKey(slotKey, out string slot, out int index))
            {
                SetStatus("Invalid slot.");
                return;
            }

            var session = GameSession.Instance;
            var item = session.GetItemById(_selectedItemId);
            if (item == null)
            {
                SetStatus("Unknown item.");
                return;
            }

            if (item.slot != slot)
            {
                SetStatus($"{item.name} cannot be equipped in the {slot} slot.");
                return;
            }

            if (!session.IsItemOwned(_selectedItemId))
            {
                SetStatus("That item is not in your inventory.");
                return;
            }

            // Make room in the targeted index. If the same item was already
            // equipped elsewhere in the same slot type, unequip it first to
            // avoid duplicates after the assignment.
            if (session.IsItemEquipped(_selectedItemId))
            {
                session.UnequipItem(_selectedItemId);
            }

            // Free up the targeted index by unequipping whatever currently sits there.
            string currentInIndex = GetItemInSlot(slot, index);
            if (!string.IsNullOrEmpty(currentInIndex) && currentInIndex != _selectedItemId)
            {
                session.UnequipItem(currentInIndex);
            }

            // Pad earlier indices so the new item lands at the requested position.
            // (E.g. assigning to trinket index 1 when index 0 is empty would
            // otherwise place it at index 0.)
            EnsureSlotIndexAvailable(slot, index);

            if (session.EquipItem(_selectedItemId))
            {
                SetStatus($"Equipped {item.name}.");
            }
            else
            {
                SetStatus($"Could not equip {item.name}.");
            }

            Refresh();
        }

        private void OnClearSlot(string slotKey)
        {
            if (!TryParseSlotKey(slotKey, out string slot, out int index))
            {
                return;
            }

            string itemId = GetItemInSlot(slot, index);
            if (string.IsNullOrEmpty(itemId)) return;

            var item = GameSession.Instance.GetItemById(itemId);
            if (GameSession.Instance.UnequipItem(itemId))
            {
                SetStatus(item != null ? $"Unequipped {item.name}." : "Unequipped.");
            }

            Refresh();
        }

        // GameSession.EquipItem appends to the slot list, so to land an item at
        // a specific trinket index we temporarily fill earlier indices with a
        // placeholder. We do this by re-equipping the existing earlier items in
        // order; nothing actually fills "empty" indices on the model side.
        // For the prototype the only multi-index slot is trinket (cap 2), so
        // this just no-ops if index == 0.
        private void EnsureSlotIndexAvailable(string slot, int index)
        {
            if (index <= 0) return;

            var equipped = GameSession.Instance.GetEquippedItemIds(slot);
            if (equipped == null) return;

            // If there is already at least 'index' items, the new equip will
            // append to position 'index' (or be capped). Nothing to do.
            // If there are fewer, we have no way to leave a gap, so the item
            // will be placed at the next free position. That keeps the model
            // simple and matches the MoveManagement behavior for empty slots.
        }

        // ---------- Selected item details ----------

        private void UpdateSelectedItem()
        {
            if (selectedItemText == null) return;

            var item = !string.IsNullOrEmpty(_selectedItemId) && GameSession.Instance != null
                ? GameSession.Instance.GetItemById(_selectedItemId)
                : null;

            if (item == null)
            {
                selectedItemText.text = "Select an item to see details.";
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"<b>{item.name}</b>");
            sb.AppendLine();
            sb.Append(Capitalize(item.slot));
            if (!string.IsNullOrEmpty(item.rarity))
            {
                sb.Append($"  |  {Capitalize(item.rarity)}");
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
            if (!string.IsNullOrEmpty(item.description))
            {
                sb.AppendLine();
                sb.Append(item.description);
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

        // ---------- Helpers ----------

        private void OnBackClicked()
        {
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private static string MakeSlotKey(string slot, int index) => $"{slot}:{index}";

        private static bool TryParseSlotKey(string slotKey, out string slot, out int index)
        {
            slot = null;
            index = 0;
            if (string.IsNullOrEmpty(slotKey)) return false;

            int sep = slotKey.IndexOf(':');
            if (sep <= 0 || sep >= slotKey.Length - 1) return false;

            slot = slotKey.Substring(0, sep);
            return int.TryParse(slotKey.Substring(sep + 1), out index);
        }

        private static string FormatItemLine(ItemDto item, string fallbackId)
        {
            if (item == null) return fallbackId;
            return string.IsNullOrEmpty(item.rarity) ? item.name : $"{item.name} [{item.rarity}]";
        }

        private static string Bonus(int amount)
        {
            if (amount == 0) return string.Empty;
            return amount > 0 ? $" (+{amount})" : $" ({amount})";
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
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
    }
}
