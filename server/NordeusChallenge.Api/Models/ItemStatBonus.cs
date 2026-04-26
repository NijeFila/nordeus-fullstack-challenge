namespace NordeusChallenge.Api.Models;

// A single flat stat bonus granted by an equipped item. The client adds the
// bonus to the matching base stat for the duration of the run while the item
// remains equipped.
public class ItemStatBonus
{
    // One of: "maxHealth", "maxMana", "attack", "defense", "magic".
    public string Stat { get; set; } = string.Empty;

    // Flat amount added to the chosen stat. Integer, may be negative (rare).
    public int Amount { get; set; }
}
