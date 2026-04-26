using System.Collections.Generic;
using System.Text;
using NordeusChallenge.Client.Core;
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

            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null)
            {
                SetText(heroText, "No active run.");
                SetText(equippedMovesText, string.Empty);
                SetText(equippedItemsText, string.Empty);
                SetText(inventoryText, string.Empty);
                ClearEncountersContainer();
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            var hero = GameSession.Instance.CurrentHero ?? run.hero;

            RenderHero(hero);
            RenderEquippedMoves(hero);
            RenderEquippedItems();
            RenderInventory();
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
        }

        private void RenderHero(HeroDto hero)
        {
            if (hero == null)
            {
                SetText(heroText, "No hero data.");
                return;
            }

            var bonuses = GameSession.Instance.GetEquippedItemStatBonuses();
            var sb = new StringBuilder();
            sb.AppendLine($"{hero.name} (Lv {hero.level})");
            if (hero.stats != null)
            {
                sb.AppendLine(
                    $"HP {hero.stats.maxHealth}{FormatBonus(bonuses.maxHealth)} | " +
                    $"ATK {hero.stats.attack}{FormatBonus(bonuses.attack)} | " +
                    $"DEF {hero.stats.defense}{FormatBonus(bonuses.defense)} | " +
                    $"MAG {hero.stats.magic}{FormatBonus(bonuses.magic)}");
            }
            sb.Append($"XP {hero.xp}");
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
                SetText(equippedMovesText, "No equipped moves.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Equipped Moves:");
            for (int i = 0; i < hero.equippedMoves.Count; i++)
            {
                string moveId = hero.equippedMoves[i];
                var move = GameSession.Instance.GetMoveById(moveId);
                if (move == null) { sb.AppendLine($"- {moveId}"); continue; }
                sb.AppendLine($"- {move.name} ({move.category}, Pow {move.power})");
            }
            SetText(equippedMovesText, sb.ToString().TrimEnd());
        }

        private void RenderEquippedItems()
        {
            if (equippedItemsText == null) return;

            var session = GameSession.Instance;
            var sb = new StringBuilder();
            sb.AppendLine("Equipped Items:");
            bool anyEquipped = false;

            for (int s = 0; s < ItemSlotOrder.Length; s++)
            {
                string slot = ItemSlotOrder[s];
                var equipped = session.GetEquippedItemIds(slot);
                int cap = session.GetEquippedSlotCap(slot);

                if (equipped == null || equipped.Count == 0)
                {
                    sb.AppendLine($"- {Capitalize(slot)} (0/{cap}): Empty");
                    continue;
                }

                anyEquipped = true;
                sb.Append($"- {Capitalize(slot)} ({equipped.Count}/{cap}): ");
                for (int i = 0; i < equipped.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var item = session.GetItemById(equipped[i]);
                    sb.Append(item != null ? item.name : equipped[i]);
                }
                sb.AppendLine();
            }

            if (!anyEquipped)
            {
                SetText(equippedItemsText, "Equipped Items: none.");
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
                SetText(inventoryText, "Inventory: empty.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Inventory ({inventory.Count}):");
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = session.GetItemById(inventory[i]);
                string name = item != null ? item.name : inventory[i];
                string equippedNote = session.IsItemEquipped(inventory[i]) ? " [equipped]" : string.Empty;
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

            for (int i = 0; i < run.encounters.Count; i++)
            {
                var encounter = run.encounters[i];
                var monster = session.GetMonsterById(encounter.monsterId);
                string monsterName = monster != null ? monster.name : encounter.monsterId;

                bool unlocked = session.IsEncounterUnlocked(encounter.index);
                bool cleared = session.IsEncounterCleared(encounter.index);

                string status = !unlocked ? "Locked" : (cleared ? "Cleared" : "Available");
                var environment = session.GetEnvironmentById(encounter.environmentId);
                string environmentName = environment != null ? environment.name : null;

                string label = string.IsNullOrEmpty(environmentName)
                    ? $"{encounter.index + 1}. {monsterName} (Lv {encounter.level}) - {status}"
                    : $"{encounter.index + 1}. {monsterName} (Lv {encounter.level}) - {environmentName} - {status}";

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
