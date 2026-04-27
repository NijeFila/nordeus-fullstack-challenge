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

        [Header("Run Map (optional)")]
        [Tooltip("Used when RunConfig.mapNodes is present. Leave empty to keep the linear list.")]
        [SerializeField] private Transform mapContainer;
        [SerializeField] private RunMapNodeView mapNodePrefab;
        [SerializeField] private GameObject mapLinePrefab;
        [SerializeField] private TMP_Text mapTitleText;
        [SerializeField] private TMP_Text runVictoryText;
        [SerializeField] private float mapVerticalSpacing = 180f;
        [SerializeField] private float mapHorizontalSpacing = 120f;

        private Dictionary<string, RunMapNodeView> _nodeViews = new Dictionary<string, RunMapNodeView>();

        [Header("Endless Mode (optional)")]
        [Tooltip("Root container for endless-mode UI. Hidden in standard runs.")]
        [SerializeField] private GameObject endlessPanel;
        [SerializeField] private TMP_Text endlessTitleText;
        [SerializeField] private TMP_Text endlessFloorText;
        [SerializeField] private TMP_Text endlessNextEnemyText;
        [SerializeField] private TMP_Text endlessShopText;
        [SerializeField] private TMP_Text endlessBestFloorText;
        [SerializeField] private TMP_Text endlessDefeatText;
        [SerializeField] private Button endlessStartFloorButton;
        [SerializeField] private TMP_Text endlessStartFloorButtonLabel;
        [SerializeField] private Button endlessReturnToMenuButton;

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

            // Endless Mode takes precedence over the standard map / linear
            // encounter list, since none of those apply to a generated floor.
            if (GameSession.Instance.CurrentRunMode == RunMode.Endless)
            {
                ClearEncountersContainer();
                ClearMapContainer();
                if (mapTitleText != null) mapTitleText.gameObject.SetActive(false);
                if (runVictoryText != null) runVictoryText.gameObject.SetActive(false);
                RenderEndless();
            }
            else if (GameSession.Instance.HasMap && mapContainer != null && mapNodePrefab != null)
            {
                ClearEncountersContainer();
                if (endlessPanel != null) endlessPanel.SetActive(false);
                RenderMap();
            }
            else
            {
                ClearMapContainer();
                if (mapTitleText != null) mapTitleText.gameObject.SetActive(false);
                if (runVictoryText != null) runVictoryText.gameObject.SetActive(false);
                if (endlessPanel != null) endlessPanel.SetActive(false);
                RenderEncounters(run);
            }
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

        // ---------- Branching map rendering ----------

        private void RenderMap()
        {
            ClearMapContainer();
            _nodeViews.Clear();

            if (mapTitleText != null)
            {
                mapTitleText.gameObject.SetActive(true);
                mapTitleText.text = Loc.Tr("ui.map.title", "Run Map");
            }

            var session = GameSession.Instance;
            var run = session.CurrentRun;

            if (runVictoryText != null)
            {
                bool show = session.RunCompleted;
                runVictoryText.gameObject.SetActive(show);
                if (show)
                {
                    runVictoryText.text = Loc.Tr("ui.map.run_victory", "Run complete! Dragon defeated.");
                }
            }

            if (mapContainer != null)
            {
                var lg = mapContainer.GetComponent<LayoutGroup>();
                if (lg != null) lg.enabled = false;
                var csf = mapContainer.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;

                // Set container anchors to bottom center to scroll up
                var rect = mapContainer.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
            }

            // 1. Spawn all nodes
            foreach (var node in run.mapNodes)
            {
                if (node == null) continue;
                var view = SpawnMapNode(node);
                _nodeViews[node.id] = view;
            }

            // 2. Position nodes
            int maxDepth = 0;
            foreach (var node in run.mapNodes)
            {
                if (node.depth > maxDepth) maxDepth = node.depth;
                if (!_nodeViews.TryGetValue(node.id, out var view)) continue;

                var rt = view.GetComponent<RectTransform>();
                float y = node.depth * mapVerticalSpacing + (mapVerticalSpacing * 0.5f);
                float x = (node.position - 2.5f) * mapHorizontalSpacing; // Assume ~6 lanes, center at 2.5
                rt.anchoredPosition = new Vector2(x, y);
            }

            // 3. Draw Connections
            if (mapLinePrefab != null)
            {
                foreach (var node in run.mapNodes)
                {
                    if (node.connectedTo == null) continue;
                    if (!_nodeViews.TryGetValue(node.id, out var startView)) continue;

                    foreach (var targetId in node.connectedTo)
                    {
                        if (!_nodeViews.TryGetValue(targetId, out var endView)) continue;
                        DrawConnection(startView.GetComponent<RectTransform>(), endView.GetComponent<RectTransform>());
                    }
                }
            }

            // Resize container
            if (mapContainer != null)
            {
                var rect = mapContainer.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(mapHorizontalSpacing * 6, (maxDepth + 1) * mapVerticalSpacing + 100);
            }
        }

        private void DrawConnection(RectTransform start, RectTransform end)
        {
            var lineObj = Instantiate(mapLinePrefab, mapContainer);
            lineObj.transform.SetAsFirstSibling(); // Lines behind nodes
            var rt = lineObj.GetComponent<RectTransform>();

            Vector2 dir = end.anchoredPosition - start.anchoredPosition;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = start.anchoredPosition + dir * 0.5f;
            rt.sizeDelta = new Vector2(distance, 4f); // 4 pixels thick
            rt.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private RunMapNodeView SpawnMapNode(RunMapNodeDto node)
        {
            var session = GameSession.Instance;

            bool cleared = session.IsMapNodeCleared(node.id);
            bool available = session.IsMapNodeAvailable(node.id);
            bool interactable = available && !cleared && !session.RunCompleted;

            string typeText = LocalizeNodeType(node.type);
            string statusText = cleared
                ? Loc.Tr("ui.map.status.cleared", "Cleared")
                : (available ? Loc.Tr("ui.map.status.available", "Available")
                             : Loc.Tr("ui.map.status.locked", "Locked"));
            string label = BuildMapNodeLabel(node);

            bool isCurrent = session.SelectedMapNodeId == node.id;

            var view = Instantiate(mapNodePrefab, mapContainer);
            view.Bind(node.id, label, typeText, statusText, node.type, interactable, isCurrent, OnMapNodeSelected);
            return view;
            }

        private string BuildMapNodeLabel(RunMapNodeDto node)
        {
            if (node == null) return string.Empty;

            // Shop nodes have no encounter data — just show type + a depth cue.
            if (node.encounterIndex < 0)
            {
                return $"D{node.depth}. {LocalizeNodeType(node.type)}";
            }

            var session = GameSession.Instance;
            var encounter = session.GetEncounterByIndex(node.encounterIndex);
            if (encounter == null)
            {
                return $"D{node.depth}. {LocalizeNodeType(node.type)}";
            }

            var monster = session.GetMonsterById(encounter.monsterId);
            string monsterName = monster != null ? LocalizedNamesProxy(monster) : encounter.monsterId;
            string envName = string.Empty;
            if (!string.IsNullOrEmpty(encounter.environmentId))
            {
                var env = session.GetEnvironmentById(encounter.environmentId);
                if (env != null) envName = LocalizedNamesProxy(env);
            }

            string lvShort = Loc.Tr("label.level_short", "Lv");
            return string.IsNullOrEmpty(envName)
                ? $"D{node.depth}. {monsterName} ({lvShort} {encounter.level})"
                : $"D{node.depth}. {monsterName} ({lvShort} {encounter.level}) — {envName}";
        }

        private static string LocalizeNodeType(string type)
        {
            switch (type)
            {
                case "Battle": return Loc.Tr("ui.map.node.battle", "Battle");
                case "Elite":  return Loc.Tr("ui.map.node.elite",  "Elite");
                case "Shop":   return Loc.Tr("ui.map.node.shop",   "Shop");
                case "Boss":   return Loc.Tr("ui.map.node.boss",   "Boss");
                default:       return type ?? string.Empty;
            }
        }

        // Small wrappers to avoid pulling extra namespaces into the file.
        private static string LocalizedNamesProxy(MonsterDto m) =>
            NordeusChallenge.Client.Localization.LocalizedNames.Name(m);

        private static string LocalizedNamesProxy(BattleEnvironmentDto e) =>
            NordeusChallenge.Client.Localization.LocalizedNames.Name(e);

        private void OnMapNodeSelected(string nodeId)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            var node = session.GetMapNodeById(nodeId);
            if (node == null) return;
            if (!session.IsMapNodeAvailable(nodeId)) return;

            session.SetSelectedMapNode(nodeId);

            if (node.type == "Shop")
            {
                SceneManager.LoadScene(SceneNames.Shop);
                return;
            }

            // Battle / Elite / Boss all flow through the same battle pipeline.
            SceneManager.LoadScene(SceneNames.Battle);
        }

        private void ClearMapContainer()
        {
            if (mapContainer == null) return;
            for (int i = mapContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(mapContainer.GetChild(i).gameObject);
            }
        }

        // ---------- Endless Mode rendering ----------

        private void RenderEndless()
        {
            if (endlessPanel != null) endlessPanel.SetActive(true);

            var session = GameSession.Instance;
            var cfg = session.CurrentRun != null ? session.CurrentRun.endlessMode : null;

            if (endlessTitleText != null)
            {
                endlessTitleText.text = Loc.Tr("ui.endless.title", "Endless Mode");
            }

            if (endlessFloorText != null)
            {
                endlessFloorText.text = string.Format(Loc.Tr("ui.endless.floor", "Floor {0}"), session.EndlessFloor);
            }

            if (endlessBestFloorText != null)
            {
                if (session.EndlessBestFloor > 0)
                {
                    endlessBestFloorText.gameObject.SetActive(true);
                    endlessBestFloorText.text = string.Format(Loc.Tr("ui.endless.best_floor", "Best floor: {0}"), session.EndlessBestFloor);
                }
                else
                {
                    endlessBestFloorText.gameObject.SetActive(false);
                }
            }

            // Endless run is over (defeat). Show the summary, hide the start button.
            if (session.EndlessRunOver)
            {
                if (endlessNextEnemyText != null) endlessNextEnemyText.text = string.Empty;
                if (endlessShopText != null) endlessShopText.gameObject.SetActive(false);
                if (endlessStartFloorButton != null) endlessStartFloorButton.gameObject.SetActive(false);
                if (endlessDefeatText != null)
                {
                    endlessDefeatText.gameObject.SetActive(true);
                    endlessDefeatText.text = string.Format(
                        Loc.Tr("ui.endless.defeat_summary", "Endless run ended on floor {0}."),
                        session.EndlessBestFloor);
                }
                if (endlessReturnToMenuButton != null)
                {
                    endlessReturnToMenuButton.gameObject.SetActive(true);
                    endlessReturnToMenuButton.onClick.RemoveAllListeners();
                    endlessReturnToMenuButton.onClick.AddListener(OnEndlessReturnToMenu);
                }
                return;
            }

            if (endlessDefeatText != null) endlessDefeatText.gameObject.SetActive(false);

            string floorType = session.EndlessFloorType ?? string.Empty;
            bool isShopFloor = floorType == "Shop";

            if (endlessShopText != null)
            {
                endlessShopText.gameObject.SetActive(isShopFloor);
                if (isShopFloor)
                {
                    endlessShopText.text = Loc.Tr("ui.endless.shop_available", "Shop floor — gear up before the next battle.");
                }
            }

            if (endlessNextEnemyText != null)
            {
                endlessNextEnemyText.text = isShopFloor
                    ? string.Empty
                    : BuildEndlessEnemyLine(session);
            }

            if (endlessStartFloorButton != null)
            {
                endlessStartFloorButton.gameObject.SetActive(true);
                endlessStartFloorButton.onClick.RemoveAllListeners();
                endlessStartFloorButton.onClick.AddListener(OnEndlessStartFloor);
            }

            if (endlessStartFloorButtonLabel != null)
            {
                endlessStartFloorButtonLabel.text = isShopFloor
                    ? Loc.Tr("ui.endless.continue", "Visit Shop")
                    : Loc.Tr("ui.endless.start_floor", "Start Floor");
            }

            if (endlessReturnToMenuButton != null)
            {
                endlessReturnToMenuButton.gameObject.SetActive(false);
            }

            // Cfg is currently unused for rendering (all numbers come from
            // session state) but keeping the local makes future tweaks obvious.
            _ = cfg;
        }

        private string BuildEndlessEnemyLine(GameSession session)
        {
            var encounter = session.ResolveCurrentEncounter();
            if (encounter == null) return string.Empty;

            var monster = session.GetMonsterById(encounter.monsterId);
            string monsterName = monster != null ? LocalizedNamesProxy(monster) : encounter.monsterId;

            string envLine = string.Empty;
            if (!string.IsNullOrEmpty(encounter.environmentId))
            {
                var env = session.GetEnvironmentById(encounter.environmentId);
                if (env != null) envLine = LocalizedNamesProxy(env);
            }

            string lvShort = Loc.Tr("label.level_short", "Lv");
            string typeLabel = LocalizeNodeType(session.EndlessFloorType);

            string baseLine = string.Format(
                Loc.Tr("ui.endless.next_enemy", "Next: {0} ({1} {2})"),
                monsterName, lvShort, encounter.level);

            if (!string.IsNullOrEmpty(envLine))
            {
                baseLine += $" — {envLine}";
            }
            if (!string.IsNullOrEmpty(typeLabel))
            {
                baseLine = $"[{typeLabel}] {baseLine}";
            }
            return baseLine;
        }

        private void OnEndlessStartFloor()
        {
            var session = GameSession.Instance;
            if (session == null) return;
            if (session.EndlessRunOver) return;

            if (session.IsEndlessShopFloor)
            {
                SceneManager.LoadScene(SceneNames.Shop);
                return;
            }

            // Battle / Elite / Boss all flow through the standard battle scene.
            SceneManager.LoadScene(SceneNames.Battle);
        }

        private void OnEndlessReturnToMenu()
        {
            if (GameSession.Instance != null) GameSession.Instance.ClearCurrentRun();
            SceneManager.LoadScene(SceneNames.MainMenu);
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
