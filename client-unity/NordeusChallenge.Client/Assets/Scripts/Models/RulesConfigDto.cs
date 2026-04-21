using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class RulesConfigDto
    {
        public int buffDurationTurns;
        public int xpPerVictory;
        public int xpPerLevel;
        public StatsDto statGainPerLevel;
        public int equippedMoveSlots;
    }
}
