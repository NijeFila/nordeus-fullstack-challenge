namespace NordeusChallenge.Api.Models;

public class RulesConfig
{
    public int BuffDurationTurns { get; set; }
    public int XpPerVictory { get; set; }
    public int XpPerLevel { get; set; }

    // Kept for backward compatibility with older clients that auto-apply a
    // fixed stat gain on level up. Newer clients use LevelUpChoices instead
    // and present the player with an attribute pick.
    public Stats StatGainPerLevel { get; set; } = new();

    public int EquippedMoveSlots { get; set; }

    // Attribute choices offered to the player on level up. The client picks
    // one entry and applies its stat delta to the hero.
    public List<LevelUpChoice> LevelUpChoices { get; set; } = new();
}
