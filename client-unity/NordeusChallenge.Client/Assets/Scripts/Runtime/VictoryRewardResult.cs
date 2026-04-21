namespace NordeusChallenge.Client.Runtime
{
    public class VictoryRewardResult
    {
        public int XpGained;
        public bool LeveledUp;
        public int NewLevel;

        public bool NewMoveLearned;
        public bool AutoEquipped;
        public string LearnedMoveId;
        public string LearnedMoveName;

        public bool UnlockedNextEncounter;
        public int NextUnlockedIndex = -1;
    }
}
