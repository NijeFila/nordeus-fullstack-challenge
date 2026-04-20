namespace NordeusChallenge.Api.Models;

public class RunConfigResponse
{
    public string RunId { get; set; } = string.Empty;
    public Hero Hero { get; set; } = new();
    public List<Encounter> Encounters { get; set; } = new();
    public List<Monster> Monsters { get; set; } = new();
    public List<Move> Moves { get; set; } = new();
    public RulesConfig Rules { get; set; } = new();
}
