namespace NordeusChallenge.Api.Models;

// Server-provided rules for the Endless Mode side of the run.
//
// The server does not generate floors itself — it ships the pools, periods,
// and reward curves; the client uses them to roll the next floor on demand.
// This keeps Endless Mode small, deterministic on the client side, and
// completely orthogonal to the standard branching run.
public class EndlessModeConfig
{
    // When false, the client should hide the Endless Mode entry point and
    // ignore the rest of this object.
    public bool Enabled { get; set; }

    // Floor index the run starts at. Floors are 1-based for display; the
    // client may treat floor 1 as the first encounter.
    public int StartingFloor { get; set; } = 1;

    // Every Nth floor is an Elite encounter (drawn from EliteMonsterPool).
    // 0 disables Elites.
    public int EliteEvery { get; set; } = 5;

    // Every Nth floor is a Shop interlude. 0 disables Shops.
    public int ShopEvery { get; set; } = 3;

    // Every Nth floor is a Boss encounter (drawn from BossMonsterPool).
    // 0 disables Bosses (endless monster grind only).
    public int BossEvery { get; set; } = 10;

    // Encounter level for floor 1.
    public int BaseLevel { get; set; } = 1;

    // The encounter level goes up by 1 every Nth floor. 0 keeps the level
    // fixed at BaseLevel.
    public int LevelIncreaseEvery { get; set; } = 2;

    // Gold reward = RewardGoldBase + (floor - 1) * RewardGoldPerFloor.
    public int RewardGoldBase { get; set; } = 15;
    public int RewardGoldPerFloor { get; set; } = 2;

    // XP reward = XpBase + (floor - 1) * XpPerFloor.
    public int XpBase { get; set; } = 30;
    public int XpPerFloor { get; set; } = 3;

    // Optional multipliers, expressed in basis points so JsonUtility can
    // serialize them cleanly without floats. 100 = +1.00x, 110 = +1.10x.
    // Applied on top of the linear formulas above for a slow late-game ramp.
    // 0 (or any value <= 0) means "no scaling on top of the linear curve".
    public int EndlessGoldScalingBp { get; set; } = 0;
    public int EndlessXpScalingBp { get; set; } = 0;

    // Pools the client samples from when generating each floor. Ids reference
    // entries in RunConfigResponse.Monsters and RunConfigResponse.Environments.
    public List<string> MonsterPool { get; set; } = new();
    public List<string> EliteMonsterPool { get; set; } = new();
    public List<string> BossMonsterPool { get; set; } = new();
    public List<string> EnvironmentPool { get; set; } = new();
}
