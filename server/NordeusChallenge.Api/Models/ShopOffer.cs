namespace NordeusChallenge.Api.Models;

// A single purchasable entry in the run's shop. Two flavors are supported:
// - Item offers (Type = "Item") grant the item with the matching ItemId.
// - StatUpgrade offers (Type = "StatUpgrade") permanently raise the chosen
//   hero stat by Amount.
//
// The same shape covers both so the client only deals with one list. Fields
// not relevant to a given Type are left empty / zero.
public class ShopOffer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Cost in gold. Paid on purchase.
    public int Price { get; set; }

    // One of: "Item", "StatUpgrade".
    public string Type { get; set; } = string.Empty;

    // For Type = "Item": the item id granted on purchase. Empty otherwise.
    public string ItemId { get; set; } = string.Empty;

    // For Type = "StatUpgrade": one of "maxHealth", "maxMana", "attack",
    // "defense", "magic". Empty otherwise.
    public string Stat { get; set; } = string.Empty;

    // For Type = "StatUpgrade": flat amount added to the chosen stat.
    public int Amount { get; set; }
}
