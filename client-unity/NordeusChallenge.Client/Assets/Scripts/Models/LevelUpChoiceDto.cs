using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class LevelUpChoiceDto
    {
        public string id;
        public string name;
        public string description;

        // One of "health" (maxHealth), "mana" (maxMana), "attack", "defense", "magic".
        public string stat;

        public int amount;
    }
}
