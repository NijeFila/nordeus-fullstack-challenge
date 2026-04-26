namespace NordeusChallenge.Api.Models;

// Battlefield environment that flavors a single encounter. All modifiers are
// flat integers so the client can apply them in the existing damage / status
// pipeline without introducing percentage math. Zero means "no effect".
public class BattleEnvironment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Added to outgoing Physical move damage by either combatant.
    public int PhysicalDamageBonus { get; set; }

    // Added to outgoing Magic move damage by either combatant.
    public int MagicDamageBonus { get; set; }

    // Added to the amount restored by Heal effects.
    public int HealingBonus { get; set; }

    // Flat HP loss applied to both combatants at the end of every turn.
    public int EndOfTurnDamage { get; set; }

    // Extra turns appended to the duration of newly applied Poison effects.
    public int PoisonBonusTurns { get; set; }

    // Added to each Bleed tick's per-turn damage on this battlefield.
    public int BleedBonusDamage { get; set; }

    // Mana restored to both combatants at the end of every turn.
    public int ManaRegenBonus { get; set; }
}
