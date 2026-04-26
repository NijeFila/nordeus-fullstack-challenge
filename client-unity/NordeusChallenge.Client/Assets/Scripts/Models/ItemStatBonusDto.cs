using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class ItemStatBonusDto
    {
        // One of: "maxHealth", "maxMana", "attack", "defense", "magic".
        public string stat;

        public int amount;
    }
}
