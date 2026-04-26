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

        public bool UnlockedNextEncounter;
        public int NextUnlockedIndex = -1;
    }
}
