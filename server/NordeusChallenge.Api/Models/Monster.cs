namespace NordeusChallenge.Api.Models;

public class Monster
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Stats BaseStats { get; set; } = new();
    public List<string> Moves { get; set; } = new();

    // Item ids this monster can drop on victory. The client decides how to
    // resolve a drop from the list; the server only declares possibilities.
    public List<string> ItemDrops { get; set; } = new();
}
