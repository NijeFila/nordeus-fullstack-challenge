namespace NordeusChallenge.Api.Models;

public class Hero
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Xp { get; set; }
    public Stats Stats { get; set; } = new();
    public List<string> EquippedMoves { get; set; } = new();
    public List<string> LearnedMovePool { get; set; } = new();
}
