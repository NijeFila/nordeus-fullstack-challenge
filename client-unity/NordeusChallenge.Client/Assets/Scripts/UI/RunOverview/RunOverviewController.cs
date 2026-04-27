using System.Collections.Generic;
using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.RunOverview
{
    public class RunOverviewController : MonoBehaviour
    {
        [Header("Hero / Moves")]
        [SerializeField] private TMP_Text heroText;
        [SerializeField] private TMP_Text equippedMovesText;
        [SerializeField] private Button moveManagementButton;

        [Header("Items")]
        [SerializeField] private TMP_Text equippedItemsText;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private Button itemManagementButton;

        [Header("Shop")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Button shopButton;

        [Header("Encounters")]
        [SerializeField] private Transform encountersContainer;
        [SerializeField] private EncounterButtonView encounterButtonPrefab;

        private static readonly string[] ItemSlotOrder = { "weapon", "armor", "trinket" };

        private void Start()
        {
            if (moveManagementButton != null)
            {
                moveManagementButton.onClick.AddListener(OnMoveManagementClicked);
            }
            if (itemManagementButton != null)
            {
                itemManagementButton.onClick.AddListener(OnItemManagementClicked);
            }
            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopClicked);
            }

            // Defensive fallback: if the player skipped the class picker for
            // any reason, seed the hero from defaultHeroClassId before render.
            if (GameSession.Instance != null) GameSession.Instance.EnsureHeroInitialized();

            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null)
            {
                SetText(heroText, Loc.Tr("ui.common.no_active_run", "No active run."));
                SetText(equippedMovesText, string.Empty);
                SetText(equippedItemsText, string.Empty);
                SetText(inventoryText, string.Empty);
                SetText(goldText, string.Empty);
                ClearEncountersContainer();
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            var hero = GameSession.Instance.CurrentHero ?? run.hero;

            RenderHero(hero);
            RenderEquippedMoves(hero);
            RenderEquippedItems();
            RenderInventory();
            RenderGold();
            RenderEncounters(run);
        }

        private void OnDestroy()
        {
            if (moveManagementButton != null)
            {
                moveManagementButton.onClick.RemoveListener(OnMoveManagementClicked);
            }
            if (itemManagementButton != null)
            {
                itemManagementButton.onClick.RemoveListener(OnItemManagementClicked);
            }
            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OnShopClicked);
            }
        }

        private void RenderGold()
        {
            if (goldText == null) return;
            int gold = GameSession.Instance != null ? GameSession.Instance.CurrentGold : 0;
            goldText.text = string.Format(Loc.Tr("ui.run.gold", "Gold: {0}"), gold);
        }

        private void OnShopClicked()
        {
            SceneManager.LoadScene(SceneNames.Shop);
        }

        private void RenderHero(HeroDto hero)
        {
            if (hero == null)
            {
                SetText(heroText, Loc.Tr("ui.common.no_hero_data", "No hero data."));
                return;
            }

            var bonuses = GameSession.Instance.GetEquippedItemStatBonuses();
            string lv = Loc.Tr("label.level_short", "Lv");
            string hp = Loc.Tr("label.hp", "HP");
            string atk = Loc.Tr("label.atk", "ATK");
            string def = Loc.Tr("label.def", "DEF");
            string mag = Loc.Tr("label.mag", "MAG");
            string xp = Loc.Tr("label.xp", "XP");
            var sb = new StringBuilder();
            sb.AppendLine($"{LocalizedNames.Name(hero)} ({lv} {hero.level})");
            if (hero.stats != null)
            {
                sb.AppendLine(
                    $"{hp} {hero.stats.maxHealth}{FormatBonus(bonuses.maxHealth)} | " +
                    $"{atk} {hero.stats.attack}{FormatBonus(bonuses.attack)} | " +
                    $"{def} {hero.stats.defense}{FormatBonus(bonuses.defense)} | " +
                    $"{mag} {hero.stats.magic}{FormatBonus(bonuses.magic)}");
            }
            sb.Append($"{xp} {hero.xp}");
            SetText(heroText, sb.ToString());
        }

        private static string FormatBonus(int amount)
        {
            if (amount == 0) return string.Empty;
            return amount > 0 ? $" (+{amount})" : $" ({amount})";
        }

        private void RenderEquippedMoves(HeroDto hero)
        {
            if (hero == null || hero.equippedMoves == null || hero.equippedMoves.Count == 0)
            {
                SetText(equippedMovesText, Loc.Tr("ui.run.no_equipped_moves", "No equipped moves."));
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(Loc.Tr("ui.run.equipped_moves", "Equipped Moves:"));
            string powLabel = Loc.Tr("label.power", "Pow");
            for (int i = 0; i < hero.equippedMoves.Count; i++)
            {
                string moveId = hero.equippedMoves[i];
                var move = GameSession.Instance.GetMoveById(moveId);
                if (move == null) { sb.AppendLine($"- {moveId}"); continue; }
                sb.AppendLine($"- {LocalizedNames.Name(move)} ({LocalizedNames.Category(move.category)}, {powLabel} {move.power})");
            }
            SetText(equippedMovesText, sb.ToString().TrimEnd());
        }

        private void RenderEquippedItems()
        {
            if (equippedItemsText == null) return;

            var session = GameSession.Instance;
            var sb = new StringBuilder();
            sb.AppendLine(Loc.Tr("ui.run.equipped_items", "Equipped Items:"));
            string emptyWord = Loc.Tr("ui.common.empty", "Empty");
            bool anyEquipped = false;

            for (int s = 0; s < ItemSlotOrder.Length; s++)
            {
                string slot = ItemSlotOrder[s];
                var equipped = session.GetEquippedItemIds(slot);
                int cap = session.GetEquippedSlotCap(slot);

                if (equipped == null || equipped.Count == 0)
                {
                    sb.AppendLine($"- {LocalizedNames.Slot(slot)} (0/{cap}): {emptyWord}");
                    continue;
                }

                anyEquipped = true;
                sb.Append($"- {LocalizedNames.Slot(slot)} ({equipped.Count}/{cap}): ");
                for (int i = 0; i < equipped.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var item = session.GetItemById(equipped[i]);
                    sb.Append(item != null ? LocalizedNames.Name(item) : equipped[i]);
                }
                sb.AppendLine();
            }

            if (!anyEquipped)
            {
                SetText(equippedItemsText, Loc.Tr("ui.run.equipped_items_none", "Equipped Items: none."));
                return;
            }

            SetText(equippedItemsText, sb.ToString().TrimEnd());
        }

        private void RenderInventory()
        {
            if (inventoryText == null) return;

            var session = GameSession.Instance;
            var inventory = session.InventoryItemIds;

            if (inventory == null || inventory.Count == 0)
            {
                SetText(inventoryText, Loc.Tr("ui.run.inventory_empty", "Inventory: empty."));
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(Loc.Tr("ui.run.inventory", "Inventory ({0}):"), inventory.Count));
            string equippedTag = " " + Loc.Tr("ui.run.equipped_tag", "[equipped]");
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = session.GetItemById(inventory[i]);
                string name = item != null ? LocalizedNames.Name(item) : inventory[i];
                string equippedNote = session.IsItemEquipped(inventory[i]) ? equippedTag : string.Empty;
                sb.AppendLine($"- {name}{equippedNote}");
            }
            SetText(inventoryText, sb.ToString().TrimEnd());
        }

        private void RenderEncounters(RunConfigResponseDto run)
        {
            ClearEncountersContainer();

            if (encountersContainer == null || encounterButtonPrefab == null) return;
            if (run.encounters == null || run.encounters.Count == 0) return;

            var session = GameSession.Instance;

            string lvShort = Loc.Tr("label.level_short", "Lv");
            for (int i = 0; i < run.encounters.Count; i++)
            {
                var encounter = run.encounters[i];
                var monster = session.GetMonsterById(encounter.monsterId);
                string monsterName = monster != null ? LocalizedNames.Name(monster) : encounter.monsterId;

                bool unlocked = session.IsEncounterUnlocked(encounter.index);
                bool cleared = session.IsEncounterCleared(encounter.index);

                string status = !unlocked
                    ? Loc.Tr("status.locked", "Locked")
                    : (cleared ? Loc.Tr("status.cleared", "Cleared") : Loc.Tr("status.available", "Available"));
                var environment = session.GetEnvironmentById(encounter.environmentId);
                string environmentName = environment != null ? LocalizedNames.Name(environment) : null;

                string label = string.IsNullOrEmpty(environmentName)
                    ? $"{encounter.index + 1}. {monsterName} ({lvShort} {encounter.level}) - {status}"
                    : $"{encounter.index + 1}. {monsterName} ({lvShort} {encounter.level}) - {environmentName} - {status}";

                var view = Instantiate(encounterButtonPrefab, encountersContainer);
                view.Bind(encounter.index, label, unlocked, OnEncounterSelected);
            }
        }

        private void OnMoveManagementClicked()
        {
            SceneManager.LoadScene(SceneNames.MoveManagement);
        }

        private void OnItemManagementClicked()
        {
            SceneManager.LoadScene(SceneNames.ItemManagement);
        }

        private void OnEncounterSelected(int encounterIndex)
        {
            if (GameSession.Instance == null) return;
            if (!GameSession.Instance.IsEncounterUnlocked(encounterIndex)) return;

            GameSession.Instance.SetSelectedEncounterIndex(encounterIndex);
            SceneManager.LoadScene(SceneNames.Battle);
        }

        private void ClearEncountersContainer()
        {
            if (encountersContainer == null) return;
            for (int i = encountersContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(encountersContainer.GetChild(i).gameObject);
            }
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
