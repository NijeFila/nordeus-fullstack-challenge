namespace NordeusChallenge.Api.Models;

public class MoveEffect
{
    // One of: BuffAttack, BuffDefense, BuffMagic,
    // DebuffAttack, DebuffDefense, DebuffMagic, Heal.
    public string Kind { get; set; } = string.Empty;

    public int Amount { get; set; }

    // Ignored for Heal.
    public int DurationTurns { get; set; }

    // "Self" or "Opponent".
    public string Target { get; set; } = "Self";
}
