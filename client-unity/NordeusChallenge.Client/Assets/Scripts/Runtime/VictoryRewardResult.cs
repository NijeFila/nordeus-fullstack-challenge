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

        public bool UnlockedNextEncounter;
        public int NextUnlockedIndex = -1;
    }
}
