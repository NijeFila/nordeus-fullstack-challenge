namespace NordeusChallenge.Api.Models;

public class Monster
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Stats BaseStats { get; set; } = new();
    public List<string> Moves { get; set; } = new();
}
