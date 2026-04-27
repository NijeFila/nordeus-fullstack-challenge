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

    // Branching run map. Nodes reference entries in Encounters by index.
    // Clients that don't read this field can still walk Encounters linearly.
    public List<RunMapNode> MapNodes { get; set; } = new();

    // Id of the starting node on the map. The boss node is identified by
    // having type == "Boss"; no separate field is needed for it.
    public string StartingMapNodeId { get; set; } = string.Empty;

    // Endless Mode rules. Optional — when EndlessMode.Enabled is false the
    // client ignores this object entirely and only the standard run is shown.
    public EndlessModeConfig EndlessMode { get; set; } = new();
}
