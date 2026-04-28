using System;
using System.Collections.Generic;
using NordeusChallenge.Client.Models;
using UnityEngine;

namespace NordeusChallenge.Client.Runtime
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public RunConfigResponseDto CurrentRun { get; private set; }

        public HeroDto CurrentHero { get; private set; }

        public string SelectedHeroClassId { get; private set; }

        public int SelectedEncounterIndex { get; private set; } = -1;

        public int HighestUnlockedEncounterIndex { get; private set; } = -1;

        private readonly HashSet<int> _clearedEncounters = new();

        // ---------- Run mode ----------

        public RunMode CurrentRunMode { get; private set; } = RunMode.Standard;

        public void SetRunMode(RunMode mode) => CurrentRunMode = mode;

        // ---------- Endless Mode state ----------

        public int EndlessFloor { get; private set; }
        public int EndlessBestFloor { get; private set; }
        public bool EndlessRunOver { get; private set; }

        // "Battle", "Elite", "Boss", or "Shop". Empty when no floor is queued.
        public string EndlessFloorType { get; private set; }

        // Synthesized encounter for the current endless floor. BattleController
        // resolves it through ResolveCurrentEncounter() so the rest of the
        // battle pipeline keeps using the same EncounterDto shape.
        private EncounterDto _endlessEncounter;

        public bool IsEndlessShopFloor =>
            CurrentRunMode == RunMode.Endless && EndlessFloorType == "Shop";

        // ---------- Branching map state ----------

        public string SelectedMapNodeId { get; private set; }

        // True once a Boss node has been cleared this run.
        public bool RunCompleted { get; private set; }

        private readonly HashSet<string> _clearedNodeIds = new();
        private readonly HashSet<string> _availableNodeIds = new();

        public IReadOnlyCollection<string> ClearedNodeIds => _clearedNodeIds;
        public IReadOnlyCollection<string> AvailableNodeIds => _availableNodeIds;

        public bool HasMap =>
            CurrentRun != null && CurrentRun.mapNodes != null && CurrentRun.mapNodes.Count > 0;

        private readonly System.Random _random = new();

        private int _pendingLevelUps;

        public int PendingLevelUps => _pendingLevelUps;

        public bool HasPendingLevelUp => _pendingLevelUps > 0;

        // ---------- Gold (run-scoped) ----------

        private int _currentGold;

        public int CurrentGold => _currentGold;

        // ---------- Items (inventory + equipped) ----------

        // All item ids the player owns this run (no duplicates).
        private readonly List<string> _inventoryItemIds = new();

        // Equipped item ids per slot ("weapon", "armor", "trinket"). Each entry
        // also exists in _inventoryItemIds.
        private readonly Dictionary<string, List<string>> _equippedItemIds = new();

        public IReadOnlyList<string> InventoryItemIds => _inventoryItemIds;

        // Per-slot caps. Defaults are used if rules.equippedItemSlots is not
        // present on the wire — JsonUtility cannot deserialize a Dictionary so
        // we treat the field as advisory and rely on these values instead.
        private const int WeaponSlots = 1;
        private const int ArmorSlots = 1;
        private const int TrinketSlots = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetCurrentRun(RunConfigResponseDto run)
        {
            CurrentRun = run;
            CurrentHero = CloneHero(run != null ? run.hero : null);
            SelectedHeroClassId = null;
            SelectedEncounterIndex = -1;
            HighestUnlockedEncounterIndex = (run != null && run.encounters != null && run.encounters.Count > 0) ? 0 : -1;
            _clearedEncounters.Clear();
            _pendingLevelUps = 0;
            _currentGold = 0;
            ResetInventory();
            ResetMapProgress();
            ResetEndlessState();
        }

        public void ClearCurrentRun()
        {
            CurrentRun = null;
            CurrentHero = null;
            SelectedHeroClassId = null;
            SelectedEncounterIndex = -1;
            HighestUnlockedEncounterIndex = -1;
            _clearedEncounters.Clear();
            _pendingLevelUps = 0;
            _currentGold = 0;
            ResetInventory();
            ResetMapProgress();
            ResetEndlessState();
            CurrentRunMode = RunMode.Standard;
        }

        // Initializes the active hero from the chosen class. Replaces whatever
        // hero the run was seeded with (typically the legacy default Knight).
        // Inventory and run progression are reset so the class choice is the
        // start of a clean run.
        public void InitializeHeroFromClass(HeroClassDto heroClass)
        {
            if (heroClass == null)
            {
                Debug.LogWarning("InitializeHeroFromClass called with null class.");
                return;
            }

            CurrentHero = HeroFromClass(heroClass);
            SelectedHeroClassId = heroClass.id;

            // Reset run-scoped progression so picking late doesn't carry stale state.
            SelectedEncounterIndex = -1;
            HighestUnlockedEncounterIndex = (CurrentRun != null && CurrentRun.encounters != null && CurrentRun.encounters.Count > 0) ? 0 : -1;
            _clearedEncounters.Clear();
            _pendingLevelUps = 0;
            _currentGold = 0;
            ResetInventory();
            ResetMapProgress();
            ResetEndlessState();

            // Endless runs need their first floor queued the moment the hero
            // is initialized so RunOverview has something to display.
            if (CurrentRunMode == RunMode.Endless)
            {
                GenerateNextEndlessFloor();
            }
        }

        // Resolves a hero class from CurrentRun by id, or falls back to
        // defaultHeroClassId when no id is supplied. Returns null only if the
        // run has no class catalog at all.
        public HeroClassDto GetHeroClassByIdOrDefault(string heroClassId)
        {
            if (CurrentRun == null || CurrentRun.heroClasses == null || CurrentRun.heroClasses.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(heroClassId))
            {
                for (int i = 0; i < CurrentRun.heroClasses.Count; i++)
                {
                    if (CurrentRun.heroClasses[i] != null && CurrentRun.heroClasses[i].id == heroClassId)
                    {
                        return CurrentRun.heroClasses[i];
                    }
                }
            }

            string defaultId = CurrentRun.defaultHeroClassId;
            if (!string.IsNullOrEmpty(defaultId))
            {
                for (int i = 0; i < CurrentRun.heroClasses.Count; i++)
                {
                    if (CurrentRun.heroClasses[i] != null && CurrentRun.heroClasses[i].id == defaultId)
                    {
                        return CurrentRun.heroClasses[i];
                    }
                }
            }

            return CurrentRun.heroClasses[0];
        }

        // Convenience used when transitioning to RunOverview without an
        // explicit pick (e.g. a server response that has heroClasses but the
        // client has not implemented the picker yet).
        public void EnsureHeroInitialized()
        {
            if (CurrentHero != null) return;

            var fallback = GetHeroClassByIdOrDefault(null);
            if (fallback != null)
            {
                InitializeHeroFromClass(fallback);
            }
        }

        public void ApplyLevelUpChoice(LevelUpChoiceDto choice)
        {
            if (choice == null)
            {
                Debug.LogWarning("ApplyLevelUpChoice called with null choice.");
                return;
            }

            if (CurrentHero == null || CurrentHero.stats == null)
            {
                Debug.LogWarning("ApplyLevelUpChoice called with no current hero.");
                return;
            }

            if (_pendingLevelUps <= 0)
            {
                Debug.LogWarning("ApplyLevelUpChoice called with no pending level-ups.");
                return;
            }

            switch (choice.stat)
            {
                case "health":
                case "maxHealth":
                    CurrentHero.stats.maxHealth += choice.amount;
                    break;
                case "mana":
                case "maxMana":
                    CurrentHero.stats.maxMana += choice.amount;
                    break;
                case "attack":
                    CurrentHero.stats.attack += choice.amount;
                    break;
                case "defense":
                    CurrentHero.stats.defense += choice.amount;
                    break;
                case "magic":
                    CurrentHero.stats.magic += choice.amount;
                    break;
                default:
                    Debug.LogWarning($"Unknown level-up stat '{choice.stat}'. No stat changed.");
                    return;
            }

            _pendingLevelUps -= 1;
        }

        public void SetSelectedEncounterIndex(int index)
        {
            SelectedEncounterIndex = index;
        }

        public bool IsEncounterUnlocked(int index)
        {
            return index >= 0 && index <= HighestUnlockedEncounterIndex;
        }

        public bool IsEncounterCleared(int index)
        {
            return _clearedEncounters.Contains(index);
        }

        // ---------- Catalog lookups ----------

        public MoveDto GetMoveById(string moveId)
        {
            if (CurrentRun == null || CurrentRun.moves == null || string.IsNullOrEmpty(moveId)) return null;
            for (int i = 0; i < CurrentRun.moves.Count; i++)
                if (CurrentRun.moves[i].id == moveId) return CurrentRun.moves[i];
            return null;
        }

        public MonsterDto GetMonsterById(string monsterId)
        {
            if (CurrentRun == null || CurrentRun.monsters == null || string.IsNullOrEmpty(monsterId)) return null;
            for (int i = 0; i < CurrentRun.monsters.Count; i++)
                if (CurrentRun.monsters[i].id == monsterId) return CurrentRun.monsters[i];
            return null;
        }

        public BattleEnvironmentDto GetEnvironmentById(string environmentId)
        {
            if (CurrentRun == null || CurrentRun.environments == null || string.IsNullOrEmpty(environmentId)) return null;
            for (int i = 0; i < CurrentRun.environments.Count; i++)
                if (CurrentRun.environments[i].id == environmentId) return CurrentRun.environments[i];
            return null;
        }

        public EncounterDto GetEncounterByIndex(int index)
        {
            if (CurrentRun == null || CurrentRun.encounters == null) return null;
            for (int i = 0; i < CurrentRun.encounters.Count; i++)
                if (CurrentRun.encounters[i].index == index) return CurrentRun.encounters[i];
            return null;
        }

        public ItemDto GetItemById(string itemId)
        {
            if (CurrentRun == null || CurrentRun.items == null || string.IsNullOrEmpty(itemId)) return null;
            for (int i = 0; i < CurrentRun.items.Count; i++)
                if (CurrentRun.items[i].id == itemId) return CurrentRun.items[i];
            return null;
        }

        // ---------- Inventory & equipping ----------

        public int GetEquippedSlotCap(string slot)
        {
            switch (slot)
            {
                case "weapon": return WeaponSlots;
                case "armor": return ArmorSlots;
                case "trinket": return TrinketSlots;
                default: return 0;
            }
        }

        public bool IsItemOwned(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _inventoryItemIds.Contains(itemId);
        }

        public bool IsItemEquipped(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var pair in _equippedItemIds)
            {
                if (pair.Value != null && pair.Value.Contains(itemId)) return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetEquippedItemIds(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return System.Array.Empty<string>();
            return _equippedItemIds.TryGetValue(slot, out var list) ? list : System.Array.Empty<string>();
        }

        // Adds an item to the inventory if not already owned. Returns true when
        // the item was newly added.
        public bool AddItemToInventory(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (_inventoryItemIds.Contains(itemId)) return false;
            if (GetItemById(itemId) == null)
            {
                Debug.LogWarning($"AddItemToInventory: unknown item id '{itemId}'.");
                return false;
            }

            _inventoryItemIds.Add(itemId);
            return true;
        }

        // Equips an owned item into its declared slot. If the slot is full, the
        // oldest equipped item in that slot is bumped back to the inventory.
        public bool EquipItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (!_inventoryItemIds.Contains(itemId))
            {
                Debug.LogWarning($"EquipItem: '{itemId}' is not in the inventory.");
                return false;
            }

            var item = GetItemById(itemId);
            if (item == null || string.IsNullOrEmpty(item.slot))
            {
                Debug.LogWarning($"EquipItem: item '{itemId}' has no slot.");
                return false;
            }

            int cap = GetEquippedSlotCap(item.slot);
            if (cap <= 0)
            {
                Debug.LogWarning($"EquipItem: slot '{item.slot}' has no capacity.");
                return false;
            }

            if (!_equippedItemIds.TryGetValue(item.slot, out var list))
            {
                list = new List<string>();
                _equippedItemIds[item.slot] = list;
            }

            if (list.Contains(itemId)) return true; // already equipped

            while (list.Count >= cap)
            {
                list.RemoveAt(0);
            }

            list.Add(itemId);
            return true;
        }

        public bool UnequipItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var pair in _equippedItemIds)
            {
                if (pair.Value != null && pair.Value.Remove(itemId)) return true;
            }
            return false;
        }

        // Sums all flat stat bonuses across currently equipped items.
        public StatsDto GetEquippedItemStatBonuses()
        {
            var totals = new StatsDto();
            if (CurrentRun == null) return totals;

            foreach (var pair in _equippedItemIds)
            {
                if (pair.Value == null) continue;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    var item = GetItemById(pair.Value[i]);
                    if (item == null || item.statBonuses == null) continue;

                    for (int b = 0; b < item.statBonuses.Count; b++)
                    {
                        var bonus = item.statBonuses[b];
                        if (bonus == null) continue;
                        AddBonus(totals, bonus.stat, bonus.amount);
                    }
                }
            }

            return totals;
        }

        // Returns base hero stats plus the sum of equipped item bonuses. The
        // hero's permanent stats are not mutated by this call.
        public StatsDto GetHeroStatsWithItemBonuses()
        {
            if (CurrentHero == null || CurrentHero.stats == null) return new StatsDto();

            var bonuses = GetEquippedItemStatBonuses();
            return new StatsDto
            {
                maxHealth = CurrentHero.stats.maxHealth + bonuses.maxHealth,
                maxMana = CurrentHero.stats.maxMana + bonuses.maxMana,
                attack = CurrentHero.stats.attack + bonuses.attack,
                defense = CurrentHero.stats.defense + bonuses.defense,
                magic = CurrentHero.stats.magic + bonuses.magic
            };
        }

        private static void AddBonus(StatsDto totals, string stat, int amount)
        {
            switch (stat)
            {
                case "maxHealth": totals.maxHealth += amount; break;
                case "maxMana": totals.maxMana += amount; break;
                case "attack": totals.attack += amount; break;
                case "defense": totals.defense += amount; break;
                case "magic": totals.magic += amount; break;
            }
        }

        // ---------- Branching map ----------

        public RunMapNodeDto GetMapNodeById(string nodeId)
        {
            if (CurrentRun == null || CurrentRun.mapNodes == null || string.IsNullOrEmpty(nodeId)) return null;
            for (int i = 0; i < CurrentRun.mapNodes.Count; i++)
            {
                if (CurrentRun.mapNodes[i] != null && CurrentRun.mapNodes[i].id == nodeId)
                    return CurrentRun.mapNodes[i];
            }
            return null;
        }

        public bool IsMapNodeAvailable(string nodeId) =>
            !string.IsNullOrEmpty(nodeId) && _availableNodeIds.Contains(nodeId);

        public bool IsMapNodeCleared(string nodeId) =>
            !string.IsNullOrEmpty(nodeId) && _clearedNodeIds.Contains(nodeId);

        // Stores the player's choice from the map screen. Battle and Shop nodes
        // both call this — Battle nodes also bind the encounter index so the
        // battle scene resolves the right monster from the existing pipeline.
        //
        // Selecting a node *commits* that depth in a Slay-the-Spire style:
        // every other currently-available sibling is dropped from the
        // available set so the player cannot back out and clear an
        // alternate branch later. The chosen node remains the only
        // available entry until it is cleared, at which point its
        // connectedTo successors form the next row of choices.
        public void SetSelectedMapNode(string nodeId)
        {
            SelectedMapNodeId = nodeId;

            var node = GetMapNodeById(nodeId);
            if (node != null && node.encounterIndex >= 0)
            {
                SelectedEncounterIndex = node.encounterIndex;
            }

            if (string.IsNullOrEmpty(nodeId) || node == null) return;
            if (_clearedNodeIds.Contains(nodeId)) return;

            // Commit: collapse availability to the single chosen node.
            _availableNodeIds.Clear();
            _availableNodeIds.Add(nodeId);
        }

        // Defensive validator. Rebuilds _availableNodeIds from the current
        // committed path so stale saves or out-of-band edits cannot leave
        // sibling branches reachable. Only relevant for standard runs with a
        // branching map; no-op otherwise.
        public void RecalculateAvailableNodesFromCurrentPath()
        {
            if (CurrentRun == null || !HasMap) return;

            _availableNodeIds.Clear();

            // 1. A committed-but-not-yet-cleared selection is the only choice.
            if (!string.IsNullOrEmpty(SelectedMapNodeId)
                && !_clearedNodeIds.Contains(SelectedMapNodeId)
                && GetMapNodeById(SelectedMapNodeId) != null)
            {
                _availableNodeIds.Add(SelectedMapNodeId);
                return;
            }

            // 2. No commit yet and no clears → run start, depth 0 nodes.
            if (_clearedNodeIds.Count == 0)
            {
                string startId = CurrentRun.startingMapNodeId;
                if (!string.IsNullOrEmpty(startId) && GetMapNodeById(startId) != null)
                {
                    _availableNodeIds.Add(startId);
                    return;
                }

                for (int i = 0; i < CurrentRun.mapNodes.Count; i++)
                {
                    var n = CurrentRun.mapNodes[i];
                    if (n != null && n.depth == 0) _availableNodeIds.Add(n.id);
                }
                return;
            }

            // 3. Otherwise the available frontier is the union of connectedTo
            //    of cleared nodes, minus any successor that is itself cleared.
            foreach (var clearedId in _clearedNodeIds)
            {
                var cleared = GetMapNodeById(clearedId);
                if (cleared == null || cleared.connectedTo == null) continue;
                for (int i = 0; i < cleared.connectedTo.Count; i++)
                {
                    string next = cleared.connectedTo[i];
                    if (string.IsNullOrEmpty(next)) continue;
                    if (_clearedNodeIds.Contains(next)) continue;
                    if (GetMapNodeById(next) == null) continue;
                    _availableNodeIds.Add(next);
                }
            }
        }

        // Marks the currently selected node cleared and unlocks the nodes it
        // points at. Returns true when the cleared node was a Boss.
        public bool MarkSelectedMapNodeCleared()
        {
            if (string.IsNullOrEmpty(SelectedMapNodeId)) return false;

            var node = GetMapNodeById(SelectedMapNodeId);
            if (node == null) return false;

            _clearedNodeIds.Add(node.id);
            _availableNodeIds.Remove(node.id);

            // Unlock connected nodes; cleared nodes are not re-added.
            if (node.connectedTo != null)
            {
                for (int i = 0; i < node.connectedTo.Count; i++)
                {
                    string next = node.connectedTo[i];
                    if (string.IsNullOrEmpty(next)) continue;
                    if (_clearedNodeIds.Contains(next)) continue;
                    _availableNodeIds.Add(next);
                }
            }

            bool isBoss = node.type == "Boss";
            if (isBoss)
            {
                RunCompleted = true;
            }
            return isBoss;
        }

        private void ResetMapProgress()
        {
            SelectedMapNodeId = null;
            RunCompleted = false;
            _clearedNodeIds.Clear();
            _availableNodeIds.Clear();

            if (CurrentRun == null || CurrentRun.mapNodes == null || CurrentRun.mapNodes.Count == 0)
            {
                return;
            }

            // Seed the available set. If startingMapNodeId is provided we trust
            // it; otherwise we unlock every depth-0 node so the player can still
            // start the run.
            string startId = CurrentRun.startingMapNodeId;
            if (!string.IsNullOrEmpty(startId) && GetMapNodeById(startId) != null)
            {
                _availableNodeIds.Add(startId);
                return;
            }

            for (int i = 0; i < CurrentRun.mapNodes.Count; i++)
            {
                var n = CurrentRun.mapNodes[i];
                if (n != null && n.depth == 0) _availableNodeIds.Add(n.id);
            }
        }

        // ---------- Endless Mode ----------

        // Returns the encounter the battle scene should resolve. Standard runs
        // look it up by SelectedEncounterIndex; endless runs return the
        // synthesized floor encounter built by GenerateNextEndlessFloor().
        public EncounterDto ResolveCurrentEncounter()
        {
            if (CurrentRunMode == RunMode.Endless)
            {
                return _endlessEncounter;
            }
            return GetEncounterByIndex(SelectedEncounterIndex);
        }

        // Builds the next endless floor based on the server-provided periods.
        // No-op when not in endless mode or when EndlessMode.Enabled is false.
        public void GenerateNextEndlessFloor()
        {
            if (CurrentRunMode != RunMode.Endless) return;
            if (CurrentRun == null || CurrentRun.endlessMode == null || !CurrentRun.endlessMode.enabled) return;
            if (EndlessRunOver) return;

            var cfg = CurrentRun.endlessMode;
            if (EndlessFloor <= 0)
            {
                EndlessFloor = cfg.startingFloor > 0 ? cfg.startingFloor : 1;
            }

            string floorType = ResolveFloorType(cfg, EndlessFloor);
            int level = ResolveFloorLevel(cfg, EndlessFloor);

            EndlessFloorType = floorType;

            if (floorType == "Shop")
            {
                _endlessEncounter = null;
                return;
            }

            string monsterId = SamplePool(PoolForType(cfg, floorType)) ?? SamplePool(cfg.monsterPool);
            string environmentId = SamplePool(cfg.environmentPool);

            if (string.IsNullOrEmpty(monsterId))
            {
                Debug.LogWarning("Endless mode: no monster id available; ending run.");
                EndlessRunOver = true;
                _endlessEncounter = null;
                return;
            }

            _endlessEncounter = new EncounterDto
            {
                index = -1, // not part of CurrentRun.encounters
                monsterId = monsterId,
                level = level,
                environmentId = environmentId
            };
        }

        // Awards XP and gold using the endless curves, processes drops/learns,
        // and advances to the next floor. Called by BattleController on victory
        // when CurrentRunMode == Endless. Returns the same VictoryRewardResult
        // shape the standard victory hook produces, so the battle UI's reward
        // log keeps working without further branches.
        public VictoryRewardResult ApplyEndlessVictoryRewards()
        {
            var result = new VictoryRewardResult();
            if (CurrentRun == null || CurrentHero == null || CurrentRun.endlessMode == null || _endlessEncounter == null)
            {
                return result;
            }

            var cfg = CurrentRun.endlessMode;
            var rules = CurrentRun.rules;
            int floor = EndlessFloor;

            int gold = ScaleByBp(cfg.rewardGoldBase + (floor - 1) * Math.Max(0, cfg.rewardGoldPerFloor),
                                 cfg.endlessGoldScalingBp);
            int xp = ScaleByBp(cfg.xpBase + (floor - 1) * Math.Max(0, cfg.xpPerFloor),
                               cfg.endlessXpScalingBp);

            if (gold > 0)
            {
                _currentGold += gold;
                result.GoldGained = gold;
            }
            result.CurrentGold = _currentGold;

            result.XpGained = xp;
            CurrentHero.xp += xp;

            int pendingLevelUps = 0;
            if (rules != null && rules.xpPerLevel > 0)
            {
                while (CurrentHero.xp >= rules.xpPerLevel)
                {
                    CurrentHero.xp -= rules.xpPerLevel;
                    CurrentHero.level += 1;
                    pendingLevelUps += 1;
                }
            }

            if (pendingLevelUps > 0)
            {
                _pendingLevelUps += pendingLevelUps;
                result.LeveledUp = true;
                result.NewLevel = CurrentHero.level;
                result.PendingLevelUps = _pendingLevelUps;
            }

            // Reuse the standard drop/learn helpers so endless monsters still
            // award items and moves through the same code path.
            var monster = GetMonsterById(_endlessEncounter.monsterId);
            if (monster != null && rules != null)
            {
                TryLearnMove(monster, rules, result);
                TryDropItem(monster, result);
            }

            result.EndlessFloorCleared = floor;

            // Advance and queue up the next floor.
            EndlessFloor = floor + 1;
            if (EndlessFloor - 1 > EndlessBestFloor)
            {
                EndlessBestFloor = EndlessFloor - 1;
            }

            GenerateNextEndlessFloor();
            result.EndlessFloorNext = EndlessFloor;
            return result;
        }

        // Marks the current shop floor as visited and advances to the next one.
        // Called by ShopController when the player exits a shop in endless mode.
        public void AdvanceEndlessShopFloor()
        {
            if (CurrentRunMode != RunMode.Endless) return;
            if (EndlessFloorType != "Shop") return;
            EndlessFloor += 1;
            GenerateNextEndlessFloor();
        }

        public void EndEndlessRun()
        {
            EndlessRunOver = true;
            EndlessFloorType = string.Empty;
            _endlessEncounter = null;
        }

        private void ResetEndlessState()
        {
            EndlessFloor = 0;
            EndlessBestFloor = 0;
            EndlessRunOver = false;
            EndlessFloorType = string.Empty;
            _endlessEncounter = null;
        }

        private static string ResolveFloorType(EndlessModeConfigDto cfg, int floor)
        {
            // Boss > Elite > Shop > Battle precedence.
            if (cfg.bossEvery > 0 && floor % cfg.bossEvery == 0
                && cfg.bossMonsterPool != null && cfg.bossMonsterPool.Count > 0)
            {
                return "Boss";
            }
            if (cfg.eliteEvery > 0 && floor % cfg.eliteEvery == 0
                && cfg.eliteMonsterPool != null && cfg.eliteMonsterPool.Count > 0)
            {
                return "Elite";
            }
            if (cfg.shopEvery > 0 && floor % cfg.shopEvery == 0)
            {
                return "Shop";
            }
            return "Battle";
        }

        private static int ResolveFloorLevel(EndlessModeConfigDto cfg, int floor)
        {
            int baseLevel = cfg.baseLevel > 0 ? cfg.baseLevel : 1;
            if (cfg.levelIncreaseEvery <= 0) return baseLevel;
            return baseLevel + (floor - 1) / cfg.levelIncreaseEvery;
        }

        private static List<string> PoolForType(EndlessModeConfigDto cfg, string type)
        {
            switch (type)
            {
                case "Boss":  return cfg.bossMonsterPool;
                case "Elite": return cfg.eliteMonsterPool;
                default:      return cfg.monsterPool;
            }
        }

        private string SamplePool(List<string> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return pool[_random.Next(pool.Count)];
        }

        // bp = basis points; 100 = +1.00x. <= 0 means "no scaling".
        private static int ScaleByBp(int value, int bp)
        {
            if (bp <= 0 || value <= 0) return value;
            // value * (1 + bp/100)
            long scaled = value + (long)value * bp / 100L;
            if (scaled < 0) return value;
            if (scaled > int.MaxValue) return int.MaxValue;
            return (int)scaled;
        }

        // ---------- Victory rewards ----------

        public VictoryRewardResult ApplyVictoryRewards(int encounterIndex)
        {
            var result = new VictoryRewardResult();

            if (CurrentRun == null || CurrentHero == null || CurrentRun.rules == null) return result;

            var encounter = GetEncounterByIndex(encounterIndex);
            if (encounter == null) return result;

            var monster = GetMonsterById(encounter.monsterId);
            if (monster == null) return result;

            var rules = CurrentRun.rules;

            result.XpGained = rules.xpPerVictory;
            CurrentHero.xp += rules.xpPerVictory;

            // Gold reward: a flat amount per victory, tracked entirely on the client.
            if (rules.goldPerVictory > 0)
            {
                _currentGold += rules.goldPerVictory;
                result.GoldGained = rules.goldPerVictory;
            }
            result.CurrentGold = _currentGold;

            int pendingLevelUps = 0;
            while (rules.xpPerLevel > 0 && CurrentHero.xp >= rules.xpPerLevel)
            {
                CurrentHero.xp -= rules.xpPerLevel;
                CurrentHero.level += 1;
                pendingLevelUps += 1;
            }

            if (pendingLevelUps > 0)
            {
                _pendingLevelUps += pendingLevelUps;
                result.LeveledUp = true;
                result.NewLevel = CurrentHero.level;
                result.PendingLevelUps = _pendingLevelUps;
            }

            TryLearnMove(monster, rules, result);
            TryDropItem(monster, result);

            _clearedEncounters.Add(encounterIndex);

            if (encounterIndex == HighestUnlockedEncounterIndex
                && CurrentRun.encounters != null
                && encounterIndex + 1 < CurrentRun.encounters.Count)
            {
                HighestUnlockedEncounterIndex = encounterIndex + 1;
                result.UnlockedNextEncounter = true;
                result.NextUnlockedIndex = HighestUnlockedEncounterIndex;
            }

            // Branching map: clear the selected node and unlock its successors.
            // If no map node is selected (older client flow), this is a no-op.
            if (HasMap && !string.IsNullOrEmpty(SelectedMapNodeId))
            {
                bool wasBoss = MarkSelectedMapNodeCleared();
                if (wasBoss)
                {
                    result.RunCompleted = true;
                }
            }

            return result;
        }

        private void TryLearnMove(MonsterDto monster, RulesConfigDto rules, VictoryRewardResult result)
        {
            if (monster == null || monster.moves == null || monster.moves.Count == 0) return;

            string candidateId = monster.moves[_random.Next(monster.moves.Count)];

            if (CurrentHero.learnedMovePool == null)
            {
                CurrentHero.learnedMovePool = new List<string>();
            }

            if (CurrentHero.learnedMovePool.Contains(candidateId)) return;

            CurrentHero.learnedMovePool.Add(candidateId);
            result.NewMoveLearned = true;
            result.LearnedMoveId = candidateId;

            var move = GetMoveById(candidateId);
            result.LearnedMoveName = move != null ? move.name : candidateId;

            if (CurrentHero.equippedMoves == null)
            {
                CurrentHero.equippedMoves = new List<string>();
            }

            if (CurrentHero.equippedMoves.Count < rules.equippedMoveSlots)
            {
                CurrentHero.equippedMoves.Add(candidateId);
                result.AutoEquipped = true;
            }
        }

        private void TryDropItem(MonsterDto monster, VictoryRewardResult result)
        {
            if (monster == null || monster.itemDrops == null || monster.itemDrops.Count == 0) return;

            string candidateId = monster.itemDrops[_random.Next(monster.itemDrops.Count)];
            var item = GetItemById(candidateId);
            if (item == null) return;

            result.ItemDropped = true;
            result.DroppedItemId = candidateId;
            result.DroppedItemName = item.name;

            if (_inventoryItemIds.Contains(candidateId))
            {
                result.ItemAlreadyOwned = true;
                return;
            }

            _inventoryItemIds.Add(candidateId);
            result.ItemAddedToInventory = true;

            // Auto-equip into a free slot if possible (no swap if a slot is full).
            if (!string.IsNullOrEmpty(item.slot))
            {
                int cap = GetEquippedSlotCap(item.slot);
                if (cap > 0)
                {
                    if (!_equippedItemIds.TryGetValue(item.slot, out var list))
                    {
                        list = new List<string>();
                        _equippedItemIds[item.slot] = list;
                    }

                    if (list.Count < cap)
                    {
                        list.Add(candidateId);
                        result.ItemAutoEquipped = true;
                    }
                }
            }
        }

        private void ResetInventory()
        {
            _inventoryItemIds.Clear();
            _equippedItemIds.Clear();
        }

        // ---------- Shop ----------

        public bool CanAffordShopOffer(ShopOfferDto offer)
        {
            if (offer == null) return false;
            return _currentGold >= offer.price;
        }

        // Returns true on a successful purchase. Callers should refresh UI on
        // true and surface 'reason' on false. Failure cases:
        // - not enough gold
        // - Item offer for an item already owned (we block to avoid wasted gold)
        // - unknown offer type / item / stat
        public bool PurchaseShopOffer(ShopOfferDto offer, out string reason)
        {
            reason = string.Empty;

            if (offer == null)
            {
                reason = "No offer selected.";
                return false;
            }

            if (CurrentHero == null || CurrentHero.stats == null)
            {
                reason = "No active hero.";
                return false;
            }

            if (_currentGold < offer.price)
            {
                reason = $"Not enough gold ({_currentGold}/{offer.price}).";
                return false;
            }

            switch (offer.type)
            {
                case "Item":
                {
                    if (string.IsNullOrEmpty(offer.itemId))
                    {
                        reason = "Offer is missing an item id.";
                        return false;
                    }

                    var item = GetItemById(offer.itemId);
                    if (item == null)
                    {
                        reason = $"Unknown item '{offer.itemId}'.";
                        return false;
                    }

                    if (_inventoryItemIds.Contains(offer.itemId))
                    {
                        reason = $"You already own {item.name}.";
                        return false;
                    }

                    _currentGold -= offer.price;
                    _inventoryItemIds.Add(offer.itemId);
                    return true;
                }

                case "StatUpgrade":
                {
                    if (!ApplyStatUpgrade(offer.stat, offer.amount))
                    {
                        reason = $"Unknown stat '{offer.stat}'.";
                        return false;
                    }

                    _currentGold -= offer.price;
                    return true;
                }

                default:
                    reason = $"Unknown offer type '{offer.type}'.";
                    return false;
            }
        }

        private bool ApplyStatUpgrade(string stat, int amount)
        {
            switch (stat)
            {
                case "maxHealth": CurrentHero.stats.maxHealth += amount; return true;
                case "maxMana":   CurrentHero.stats.maxMana   += amount; return true;
                case "attack":    CurrentHero.stats.attack    += amount; return true;
                case "defense":   CurrentHero.stats.defense   += amount; return true;
                case "magic":     CurrentHero.stats.magic     += amount; return true;
                default: return false;
            }
        }

        // Returns true if buying this Item offer would be wasted gold (already owned).
        public bool IsShopOfferAlreadyOwned(ShopOfferDto offer)
        {
            if (offer == null || offer.type != "Item" || string.IsNullOrEmpty(offer.itemId)) return false;
            return _inventoryItemIds.Contains(offer.itemId);
        }

        private static HeroDto HeroFromClass(HeroClassDto source)
        {
            if (source == null) return null;

            return new HeroDto
            {
                id = source.id,
                name = source.name,
                level = 1,
                xp = 0,
                stats = source.startingStats == null ? new StatsDto() : new StatsDto
                {
                    maxHealth = source.startingStats.maxHealth,
                    maxMana = source.startingStats.maxMana,
                    attack = source.startingStats.attack,
                    defense = source.startingStats.defense,
                    magic = source.startingStats.magic
                },
                equippedMoves = source.startingMoves != null
                    ? new List<string>(source.startingMoves)
                    : new List<string>(),
                learnedMovePool = source.startingLearnedMoves != null
                    ? new List<string>(source.startingLearnedMoves)
                    : new List<string>()
            };
        }

        // ---------- Save / Restore ----------

        // Builds a serializable snapshot of the current run. Catalog data
        // (run config) is intentionally not saved — Continue refetches /run/config
        // and reapplies this snapshot.
        public SaveGameData CreateSaveData()
        {
            var data = new SaveGameData
            {
                saveVersion = 1,
                savedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                runMode = CurrentRunMode == RunMode.Endless ? "Endless" : "Standard",
                selectedHeroClassId = SelectedHeroClassId,
                gold = _currentGold,
                pendingLevelUps = _pendingLevelUps,
                selectedMapNodeId = SelectedMapNodeId,
                runCompleted = RunCompleted,
                selectedEncounterIndex = SelectedEncounterIndex,
                highestUnlockedEncounterIndex = HighestUnlockedEncounterIndex
            };

            if (CurrentHero != null)
            {
                data.hero.id = CurrentHero.id;
                data.hero.name = CurrentHero.name;
                data.hero.level = CurrentHero.level;
                data.hero.xp = CurrentHero.xp;
                if (CurrentHero.stats != null)
                {
                    data.hero.maxHealth = CurrentHero.stats.maxHealth;
                    data.hero.maxMana = CurrentHero.stats.maxMana;
                    data.hero.attack = CurrentHero.stats.attack;
                    data.hero.defense = CurrentHero.stats.defense;
                    data.hero.magic = CurrentHero.stats.magic;
                }
                if (CurrentHero.equippedMoves != null)
                    data.hero.equippedMoves = new List<string>(CurrentHero.equippedMoves);
                if (CurrentHero.learnedMovePool != null)
                    data.hero.learnedMovePool = new List<string>(CurrentHero.learnedMovePool);
            }

            data.inventoryItemIds = new List<string>(_inventoryItemIds);
            foreach (var pair in _equippedItemIds)
            {
                if (pair.Value == null || pair.Value.Count == 0) continue;
                data.equippedSlots.Add(new EquippedSlotSaveData
                {
                    slot = pair.Key,
                    itemIds = new List<string>(pair.Value)
                });
            }

            data.clearedNodeIds = new List<string>(_clearedNodeIds);
            data.availableNodeIds = new List<string>(_availableNodeIds);
            data.clearedEncounters = new List<int>(_clearedEncounters);

            data.endless.floor = EndlessFloor;
            data.endless.bestFloor = EndlessBestFloor;
            data.endless.runOver = EndlessRunOver;
            data.endless.floorType = EndlessFloorType;
            if (_endlessEncounter != null)
            {
                data.endless.monsterId = _endlessEncounter.monsterId;
                data.endless.monsterLevel = _endlessEncounter.level;
                data.endless.environmentId = _endlessEncounter.environmentId;
            }

            return data;
        }

        // Restores mutable state from a SaveGameData snapshot onto the current
        // run config. Call SetCurrentRun(...) with a freshly fetched run config
        // *before* invoking this. Returns false when the save references a
        // hero/state that cannot be reconstructed from the current config.
        public bool RestoreFromSaveData(SaveGameData data)
        {
            if (data == null || CurrentRun == null) return false;

            CurrentRunMode = string.Equals(data.runMode, "Endless", StringComparison.OrdinalIgnoreCase)
                ? RunMode.Endless
                : RunMode.Standard;

            // Re-seed the hero from the saved class so move ids / stats line
            // up with the current catalog, then overwrite mutable fields with
            // the saved values.
            HeroClassDto cls = GetHeroClassByIdOrDefault(data.selectedHeroClassId);
            if (cls != null)
            {
                CurrentHero = HeroFromClass(cls);
                SelectedHeroClassId = cls.id;
            }
            else if (CurrentRun.hero != null)
            {
                CurrentHero = CloneHero(CurrentRun.hero);
                SelectedHeroClassId = data.selectedHeroClassId;
            }

            if (CurrentHero == null)
            {
                Debug.LogWarning("[Save] Restore failed: no hero could be reconstructed.");
                return false;
            }

            // Apply hero mutable fields from the save.
            if (data.hero != null)
            {
                CurrentHero.id = string.IsNullOrEmpty(data.hero.id) ? CurrentHero.id : data.hero.id;
                CurrentHero.name = string.IsNullOrEmpty(data.hero.name) ? CurrentHero.name : data.hero.name;
                CurrentHero.level = data.hero.level > 0 ? data.hero.level : 1;
                CurrentHero.xp = data.hero.xp;

                if (CurrentHero.stats == null) CurrentHero.stats = new StatsDto();
                CurrentHero.stats.maxHealth = data.hero.maxHealth;
                CurrentHero.stats.maxMana = data.hero.maxMana;
                CurrentHero.stats.attack = data.hero.attack;
                CurrentHero.stats.defense = data.hero.defense;
                CurrentHero.stats.magic = data.hero.magic;

                CurrentHero.learnedMovePool = FilterKnownMoves(data.hero.learnedMovePool);
                CurrentHero.equippedMoves = FilterKnownMoves(data.hero.equippedMoves);
            }

            _currentGold = Math.Max(0, data.gold);
            _pendingLevelUps = Math.Max(0, data.pendingLevelUps);

            // Inventory + equipped (drop unknown ids).
            ResetInventory();
            if (data.inventoryItemIds != null)
            {
                for (int i = 0; i < data.inventoryItemIds.Count; i++)
                {
                    string id = data.inventoryItemIds[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    if (GetItemById(id) == null) continue;
                    if (!_inventoryItemIds.Contains(id)) _inventoryItemIds.Add(id);
                }
            }
            if (data.equippedSlots != null)
            {
                for (int s = 0; s < data.equippedSlots.Count; s++)
                {
                    var slot = data.equippedSlots[s];
                    if (slot == null || string.IsNullOrEmpty(slot.slot) || slot.itemIds == null) continue;
                    int cap = GetEquippedSlotCap(slot.slot);
                    if (cap <= 0) continue;

                    if (!_equippedItemIds.TryGetValue(slot.slot, out var list))
                    {
                        list = new List<string>();
                        _equippedItemIds[slot.slot] = list;
                    }
                    for (int i = 0; i < slot.itemIds.Count && list.Count < cap; i++)
                    {
                        string id = slot.itemIds[i];
                        if (string.IsNullOrEmpty(id)) continue;
                        if (GetItemById(id) == null) continue;
                        if (!_inventoryItemIds.Contains(id)) continue;
                        if (!list.Contains(id)) list.Add(id);
                    }
                }
            }

            // Standard map state.
            ResetMapProgress();
            if (HasMap)
            {
                _availableNodeIds.Clear();
                _clearedNodeIds.Clear();

                if (data.clearedNodeIds != null)
                {
                    for (int i = 0; i < data.clearedNodeIds.Count; i++)
                    {
                        string id = data.clearedNodeIds[i];
                        if (!string.IsNullOrEmpty(id) && GetMapNodeById(id) != null) _clearedNodeIds.Add(id);
                    }
                }
                if (data.availableNodeIds != null)
                {
                    for (int i = 0; i < data.availableNodeIds.Count; i++)
                    {
                        string id = data.availableNodeIds[i];
                        if (string.IsNullOrEmpty(id)) continue;
                        if (_clearedNodeIds.Contains(id)) continue;
                        if (GetMapNodeById(id) != null) _availableNodeIds.Add(id);
                    }
                }

                // If the save left no available nodes (e.g. save right before a
                // boss kill that didn't get stored), reseed from depth 0 so the
                // run is still playable.
                if (_availableNodeIds.Count == 0 && !data.runCompleted)
                {
                    string startId = CurrentRun.startingMapNodeId;
                    if (!string.IsNullOrEmpty(startId) && GetMapNodeById(startId) != null
                        && !_clearedNodeIds.Contains(startId))
                    {
                        _availableNodeIds.Add(startId);
                    }
                }

                SelectedMapNodeId = !string.IsNullOrEmpty(data.selectedMapNodeId)
                    && GetMapNodeById(data.selectedMapNodeId) != null
                    ? data.selectedMapNodeId
                    : null;
                RunCompleted = data.runCompleted;

                // Repair any stale availability from older saves so the
                // committed-path rule holds after Continue.
                RecalculateAvailableNodesFromCurrentPath();
            }

            // Linear fallback.
            SelectedEncounterIndex = data.selectedEncounterIndex;
            HighestUnlockedEncounterIndex = data.highestUnlockedEncounterIndex;
            _clearedEncounters.Clear();
            if (data.clearedEncounters != null)
            {
                for (int i = 0; i < data.clearedEncounters.Count; i++)
                {
                    _clearedEncounters.Add(data.clearedEncounters[i]);
                }
            }

            // Endless state.
            ResetEndlessState();
            if (CurrentRunMode == RunMode.Endless && data.endless != null)
            {
                EndlessFloor = data.endless.floor > 0 ? data.endless.floor : 1;
                EndlessBestFloor = Math.Max(0, data.endless.bestFloor);
                EndlessRunOver = data.endless.runOver;
                EndlessFloorType = data.endless.floorType;

                if (!EndlessRunOver
                    && !string.IsNullOrEmpty(data.endless.monsterId)
                    && GetMonsterById(data.endless.monsterId) != null)
                {
                    _endlessEncounter = new EncounterDto
                    {
                        index = -1,
                        monsterId = data.endless.monsterId,
                        level = data.endless.monsterLevel > 0 ? data.endless.monsterLevel : 1,
                        environmentId = data.endless.environmentId
                    };
                }
                else if (!EndlessRunOver)
                {
                    // Saved monster vanished from the catalog — roll a fresh
                    // floor instead of crashing the run.
                    GenerateNextEndlessFloor();
                }
            }

            return true;
        }

        // Convenience wrappers used by UI buttons.
        public bool SaveCurrentRun()
        {
            if (CurrentRun == null || CurrentHero == null)
            {
                Debug.LogWarning("[Save] Cannot save: no active run.");
                return false;
            }
            return SaveGameService.TryWrite(CreateSaveData());
        }

        public void DeleteSavedRun() => SaveGameService.TryDelete();

        // Helper: keep only move ids that exist in the current move catalog.
        private List<string> FilterKnownMoves(List<string> source)
        {
            var result = new List<string>();
            if (source == null) return result;
            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i];
                if (!string.IsNullOrEmpty(id) && GetMoveById(id) != null) result.Add(id);
            }
            return result;
        }

        private static HeroDto CloneHero(HeroDto source)
        {
            if (source == null) return null;

            var clone = new HeroDto
            {
                id = source.id,
                name = source.name,
                level = source.level,
                xp = source.xp,
                stats = source.stats == null ? null : new StatsDto
                {
                    maxHealth = source.stats.maxHealth,
                    maxMana = source.stats.maxMana,
                    attack = source.stats.attack,
                    defense = source.stats.defense,
                    magic = source.stats.magic
                },
                equippedMoves = source.equippedMoves != null ? new List<string>(source.equippedMoves) : new List<string>(),
                learnedMovePool = source.learnedMovePool != null ? new List<string>(source.learnedMovePool) : new List<string>()
            };

            return clone;
        }
    }
}
