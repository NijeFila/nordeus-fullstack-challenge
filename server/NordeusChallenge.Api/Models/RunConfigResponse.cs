namespace NordeusChallenge.Api.Models;

public class RunConfigResponse
{
    public string RunId { get; set; } = string.Empty;

    // Default hero for clients that don't yet read HeroClasses. Mirrors the
    // class identified by DefaultHeroClassId.
    public Hero Hero { get; set; } = new();

    public List<Encounter> Encounters { get; set; } = new();
    public List<Monster> Monsters { get; set; } = new();
    public List<Move> Moves { get; set; } = new();
    public List<BattleEnvironment> Environments { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<ShopOffer> ShopOffers { get; set; } = new();
    public RulesConfig Rules { get; set; } = new();

    // Selectable hero archetypes. The client shows a class picker before the
    // run starts and seeds the active hero from the chosen entry.
    public List<HeroClass> HeroClasses { get; set; } = new();

    // Id of the class to highlight by default in the picker. Also matches the
    // archetype used to populate the legacy Hero field.
    public string DefaultHeroClassId { get; set; } = string.Empty;
}
