namespace NordeusChallenge.Api.Models;

// A piece of equippable gear that can drop from a monster on victory.
// Items are stateless catalog entries; the client tracks ownership and
// equipped slots locally during the run.
public class Item
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // One of: "weapon", "armor", "trinket".
    public string Slot { get; set; } = string.Empty;

    // One of: "common", "uncommon", "rare", "epic", "legendary".
    public string Rarity { get; set; } = "common";

    // Flat stat bonuses applied while the item is equipped.
    public List<ItemStatBonus> StatBonuses { get; set; } = new();
}
