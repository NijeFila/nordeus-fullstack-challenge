using NordeusChallenge.Api.Models;

namespace NordeusChallenge.Api.Services;

// Picks the monster's next move. The selection is intentionally shallow,
// but takes battle state into account: resource costs, current HP/mana,
// active status effects on both sides, and whether a finishing blow is
// in reach.
public class BattleService
{
    private const double LowSelfHealthRatio = 0.35;
    private const double FinishHeroHealthRatio = 0.25;
    private const double DamagePreferenceChance = 0.7;

    private readonly RunConfigService _runConfigService;
    private readonly Random _random = new();

    public BattleService(RunConfigService runConfigService)
    {
        _runConfigService = runConfigService;
    }

    public string? SelectNextMove(
        string monsterId,
        int monsterHealth,
        int monsterMaxHealth,
        int heroHealth,
        int heroMaxHealth,
        int? monsterMana,
        string? lastMonsterMoveId,
        IReadOnlyCollection<string> monsterActiveEffects,
        IReadOnlyCollection<string> heroActiveEffects)
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

        // 1. Filter to moves the monster can actually pay for. HP cost must
        //    leave the monster alive after spending; mana cost must fit.
        //    If monsterMana is not supplied, the bot does not enforce mana
        //    cost — older clients that don't track mana keep working.
        var affordable = availableMoves
            .Where(m => (monsterMana is null || (m.ManaCost ?? 0) <= monsterMana)
                     && (m.HpCost ?? 0) < monsterHealth)
            .ToList();

        if (affordable.Count == 0)
        {
            // Hard fallback: take any free move; otherwise the cheapest one.
            var free = availableMoves.FirstOrDefault(m =>
                (m.ManaCost ?? 0) == 0 && (m.HpCost ?? 0) == 0);
            return (free ?? availableMoves[0]).Id;
        }

        // Anti-loop: if the previous monster move was a heal, the monster may
        // not pick a heal again this turn. This breaks the "heal-every-turn"
        // pattern that otherwise negates all hero damage.
        bool justHealed = false;
        if (!string.IsNullOrEmpty(lastMonsterMoveId))
        {
            var last = availableMoves.FirstOrDefault(m => m.Id == lastMonsterMoveId);
            if (last is not null && IsHealMove(last))
            {
                justHealed = true;
            }
        }

        if (justHealed)
        {
            var nonHeal = affordable.Where(m => !IsHealMove(m)).ToList();
            if (nonHeal.Count > 0)
            {
                affordable = nonHeal;
            }
        }

        // 2. Heal when low — only if affordable and not already healed last turn.
        if (!justHealed && monsterMaxHealth > 0)
        {
            double ratio = (double)monsterHealth / monsterMaxHealth;
            if (ratio < LowSelfHealthRatio)
            {
                var heal = affordable.FirstOrDefault(IsHealMove);
                if (heal is not null)
                {
                    return heal.Id;
                }
            }
        }

        // 3. Reach for a finishing blow when the hero is nearly down.
        if (heroMaxHealth > 0)
        {
            double heroRatio = (double)heroHealth / heroMaxHealth;
            if (heroRatio < FinishHeroHealthRatio)
            {
                var finisher = affordable
                    .Where(IsDamagingMove)
                    .OrderByDescending(m => m.Power)
                    .FirstOrDefault();
                if (finisher is not null)
                {
                    return finisher.Id;
                }
            }
        }

        // 4. Don't waste a turn re-applying a buff or debuff that is already
        //    on the relevant target.
        var meaningful = affordable
            .Where(m => !IsRedundantEffect(m, monsterActiveEffects, heroActiveEffects))
            .ToList();
        if (meaningful.Count == 0)
        {
            meaningful = affordable;
        }

        // 5. Mild bias toward damaging moves; otherwise random across the rest.
        var damaging = meaningful.Where(IsDamagingMove).ToList();
        var pool = damaging.Count > 0 && _random.NextDouble() < DamagePreferenceChance
            ? damaging
            : meaningful;

        return pool[_random.Next(pool.Count)].Id;
    }

    private static bool IsHealMove(Move move) =>
        move.Category == "Heal" || move.Effect?.Kind == "Heal";

    private static bool IsDamagingMove(Move move) =>
        move.Category == "Physical" || move.Category == "Magic";

    private static bool IsRedundantEffect(
        Move move,
        IReadOnlyCollection<string> selfEffects,
        IReadOnlyCollection<string> opponentEffects)
    {
        var effect = move.Effect;
        if (effect is null || effect.Kind == "Heal")
        {
            return false;
        }

        return effect.Target switch
        {
            "Self"     => selfEffects.Contains(effect.Kind),
            "Opponent" => opponentEffects.Contains(effect.Kind),
            _ => false
        };
    }
}
