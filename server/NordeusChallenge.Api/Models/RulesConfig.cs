namespace NordeusChallenge.Api.Models;

public class RulesConfig
{
    public int BuffDurationTurns { get; set; }
    public int XpPerVictory { get; set; }
    public int XpPerLevel { get; set; }
    public Stats StatGainPerLevel { get; set; } = new();
    public int EquippedMoveSlots { get; set; }
}
