using System;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class ShopOfferDto
    {
        public string id;
        public string name;
        public string description;
        public int price;

        // One of: "Item", "StatUpgrade".
        public string type;

        // For Item offers: id of an entry in RunConfigResponseDto.items.
        public string itemId;

        // For StatUpgrade offers: one of "maxHealth", "maxMana", "attack",
        // "defense", "magic".
        public string stat;

        // For StatUpgrade offers: flat amount added to the chosen stat.
        public int amount;
    }
}
