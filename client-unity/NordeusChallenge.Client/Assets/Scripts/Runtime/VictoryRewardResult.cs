namespace NordeusChallenge.Client.Runtime
{
    public class VictoryRewardResult
    {
        public int XpGained;
        public bool LeveledUp;
        public int NewLevel;

        // Number of level-up attribute picks the player still owes after this
        // victory. The post-battle UI shows the picker that many times.
        public int PendingLevelUps;

        public bool NewMoveLearned;
        public bool AutoEquipped;
        public string LearnedMoveId;
        public string LearnedMoveName;

        // Item drop info (set when the defeated monster has at least one entry
        // in itemDrops). DroppedItemId / DroppedItemName describe the rolled
        // item; ItemAddedToInventory is false if the player already owned it.
        public bool ItemDropped;
        public string DroppedItemId;
        public string DroppedItemName;
        public bool ItemAddedToInventory;
        public bool ItemAlreadyOwned;
        public bool ItemAutoEquipped;

        // Gold awarded on this victory and the resulting run-total gold.
        public int GoldGained;
        public int CurrentGold;

        public bool UnlockedNextEncounter;
        public int NextUnlockedIndex = -1;

        // True when the cleared map node was the Boss. The client uses this to
        // surface the run-victory message and end the run flow.
        public bool RunCompleted;

        // Endless Mode: floor that was just cleared, and the floor the player
        // is now on (already advanced). 0 / 0 in standard runs.
        public int EndlessFloorCleared;
        public int EndlessFloorNext;
    }
}
