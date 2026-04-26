namespace NordeusChallenge.Api.Models;

public class MoveEffect
{
    // One of:
    //   BuffAttack, BuffDefense, BuffMagic,
    //   DebuffAttack, DebuffDefense, DebuffMagic,
    //   Heal,
    //   Bleed, Poison,            // damage-over-time on the target
    //   DamageIncrease,           // flat bonus to outgoing damage
    //   DamageReduction.          // flat reduction to incoming damage
    public string Kind { get; set; } = string.Empty;

    // Stat delta for buffs/debuffs, flat heal amount for Heal,
    // per-turn damage for Bleed/Poison, flat damage delta for
    // DamageIncrease / DamageReduction.
    public int Amount { get; set; }

    // Ignored for Heal.
    public int DurationTurns { get; set; }

    // "Self" or "Opponent".
    public string Target { get; set; } = "Self";
}
