using System;
using System.Collections.Generic;

namespace NordeusChallenge.Client.Models
{
    [Serializable]
    public class ItemDto
    {
        public string id;
        public string name;
        public string description;

        // One of: "weapon", "armor", "trinket".
        public string slot;

        // One of: "common", "uncommon", "rare", "epic", "legendary". UI hint only.
        public string rarity;

        public List<ItemStatBonusDto> statBonuses;
    }
}
