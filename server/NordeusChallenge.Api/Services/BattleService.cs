using NordeusChallenge.Api.Models;

namespace NordeusChallenge.Api.Services;

// Picks the monster's next move. Selection is intentionally simple:
// heal when low, otherwise uniform random from the monster's move list.
public class BattleService
{
    private const double LowHealthRatio = 0.35;

    private readonly RunConfigService _runConfigService;
    private readonly Random _random = new();

    public BattleService(RunConfigService runConfigService)
    {
        _runConfigService = runConfigService;
    }

    public string? SelectNextMove(
        string monsterId,
        int monsterHealth,
        int monsterMaxHealth)
    {
        var config = _runConfigService.GetRunConfig();

        var monster = config.Monsters.FirstOrDefault(m => m.Id == monsterId);
        if (monster is null || monster.Moves.Count == 0)
        {
            return null;
        }

        var availableMoves = config.Moves
            .Where(m => monster.Moves.Contains(m.Id))
            .ToList();

        if (availableMoves.Count == 0)
        {
            return null;
        }

        if (monsterMaxHealth > 0)
        {
            double ratio = (double)monsterHealth / monsterMaxHealth;
            if (ratio < LowHealthRatio)
            {
                var healMove = availableMoves.FirstOrDefault(m => m.Category == "Heal");
                if (healMove is not null)
                {
                    return healMove.Id;
                }
            }
        }

        var pick = availableMoves[_random.Next(availableMoves.Count)];
        return pick.Id;
    }
}
