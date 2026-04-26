using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class RulesConfigDto
    {
        public int buffDurationTurns;
        public int xpPerVictory;
        public int xpPerLevel;

        // Retained for backward compatibility (older clients auto-applied this on
        // level up). Newer clients drive level-ups from levelUpChoices instead.
        public StatsDto statGainPerLevel;

        public int equippedMoveSlots;

        // Attribute increases the player can pick from when the hero levels up.
        public List<LevelUpChoiceDto> levelUpChoices;

        // Gold awarded on each battle victory. Defeats grant nothing.
        public int goldPerVictory;
    }
}
