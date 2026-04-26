using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class BattleEnvironmentDto
    {
        public string id;
        public string name;
        public string description;
        public int physicalDamageBonus;
        public int magicDamageBonus;
        public int healingBonus;
        public int endOfTurnDamage;
        public int poisonBonusTurns;
        public int bleedBonusDamage;
        public int manaRegenBonus;
    }
}
