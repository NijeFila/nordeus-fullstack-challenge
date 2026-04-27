namespace NordeusChallenge.Api.Models;

// One node on the branching run map. Nodes form a small directed acyclic
// graph: every battle/shop has zero or more outgoing connections to nodes at
// a deeper layer, and the player always converges on the single Boss node.
//
// Backwards compatibility: the linear `encounters` list is still emitted, so
// clients that have not adopted the map can keep walking the run sequentially.
public class RunMapNode
{
    public string Id { get; set; } = string.Empty;

    // Layer index, 0 for the starting node, increases toward the boss.
    public int Depth { get; set; }

    // Horizontal slot within the layer (0..N from left to right). Purely a
    // hint for client layout; logic only depends on `Connections`.
    public int Position { get; set; }

    // One of: "Battle", "Elite", "Shop", "Boss".
    public string Type { get; set; } = "Battle";

    // Index into RunConfigResponse.Encounters for Battle/Elite/Boss nodes.
    // -1 for Shop nodes (and any other non-encounter node), since JsonUtility
    // does not handle nullable ints cleanly.
    public int EncounterIndex { get; set; } = -1;

    // Ids of nodes the player can travel to after clearing this node. Empty
    // on the Boss node.
    public List<string> ConnectedTo { get; set; } = new();
}
