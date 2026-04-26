namespace NordeusChallenge.Api.Models;

// A single attribute increase the player can pick when the hero levels up.
// The client presents the list from RulesConfig.LevelUpChoices and applies
// the chosen entry's stat delta during the post-battle flow.
public class LevelUpChoice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Which stat this choice modifies. One of:
    // "health" (MaxHealth), "mana" (MaxMana), "attack", "defense", "magic".
    public string Stat { get; set; } = string.Empty;

    // Flat amount added to the chosen stat.
    public int Amount { get; set; }
}
