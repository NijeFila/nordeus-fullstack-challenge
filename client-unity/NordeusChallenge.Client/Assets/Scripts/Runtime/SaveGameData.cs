using System;
using System.Collections.Generic;
using NordeusChallenge.Client.Models;

namespace NordeusChallenge.Client.Runtime
{
    // Serializable run snapshot written to disk by SaveGameService. Only stores
    // mutable, run-scoped state — the run config itself (catalogs, encounters,
    // map graph, endless config) is refetched from the server on Continue.
    [Serializable]
    public class SaveGameData
    {
        public int saveVersion = 1;
        public string savedAt;

        // "Standard" or "Endless" — written as a string so JsonUtility doesn't
        // pin the file to a particular enum ordering.
        public string runMode = "Standard";

        public string selectedHeroClassId;

        // Hero mutable state. We don't reuse HeroDto directly because StatsDto
        // is a class on the wire and we want a flat, predictable layout.
        public HeroSaveData hero = new();

        public int gold;

        // Inventory + equipped items by slot. Equipped entries are also in
        // inventoryItemIds (matches in-memory invariant).
        public List<string> inventoryItemIds = new();
        public List<EquippedSlotSaveData> equippedSlots = new();

        public int pendingLevelUps;

        // Standard map progression.
        public List<string> clearedNodeIds = new();
        public List<string> availableNodeIds = new();
        public string selectedMapNodeId;
        public bool runCompleted;

        // Linear fallback (kept for runs without mapNodes).
        public int selectedEncounterIndex = -1;
        public int highestUnlockedEncounterIndex = -1;
        public List<int> clearedEncounters = new();

        // Endless mode state.
        public EndlessSaveData endless = new();
    }

    [Serializable]
    public class HeroSaveData
    {
        public string id;
        public string name;
        public int level = 1;
        public int xp;

        public int maxHealth;
        public int maxMana;
        public int attack;
        public int defense;
        public int magic;

        public List<string> equippedMoves = new();
        public List<string> learnedMovePool = new();
    }

    [Serializable]
    public class EquippedSlotSaveData
    {
        public string slot;
        public List<string> itemIds = new();
    }

    [Serializable]
    public class EndlessSaveData
    {
        public int floor;
        public int bestFloor;
        public bool runOver;
        public string floorType;
        public string monsterId;
        public int monsterLevel;
        public string environmentId;
    }
}
