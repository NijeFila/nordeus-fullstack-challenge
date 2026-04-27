namespace NordeusChallenge.Api.Models;

// A selectable hero archetype returned in RunConfigResponse.
// The client picks one before a run starts and uses its starting stats and
// moves to seed the Hero. Existing single-hero clients can ignore this list
// and continue to use RunConfigResponse.Hero as the default Knight.
public class HeroClass
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Starting stats at level 1. Mirrors the existing Stats shape used by Hero
    // and Monster, so client-side scaling and item bonuses keep working unchanged.
    public Stats StartingStats { get; set; } = new();

    // Move ids the hero starts with already equipped (capped at
    // RulesConfig.EquippedMoveSlots on the client side).
    public List<string> StartingMoves { get; set; } = new();

    // Move ids the hero already knows on day one. Should be a superset of
    // StartingMoves so the Move Management screen has something to swap to.
    public List<string> StartingLearnedMoves { get; set; } = new();
}
