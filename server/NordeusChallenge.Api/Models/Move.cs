namespace NordeusChallenge.Api.Models;

public class Move
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // One of: Physical, Magic, Buff, Debuff, Heal.
    public string Category { get; set; } = string.Empty;

    public int Power { get; set; }

    // Optional resource costs. Omitted from JSON when null.
    public int? ManaCost { get; set; }
    public int? HpCost { get; set; }

    public MoveEffect? Effect { get; set; }

    public string Description { get; set; } = string.Empty;
}
