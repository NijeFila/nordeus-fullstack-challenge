using NordeusChallenge.Api.Models;
using NordeusChallenge.Api.Services;

namespace NordeusChallenge.Api.Endpoints;

public static class BattleEndpoints
{
    public static void MapBattleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/battle/next-move", (
            string? monsterId,
            int? monsterLevel,
            int? monsterHealth,
            int? monsterMaxHealth,
            int? heroHealth,
            int? heroMaxHealth,
            int? monsterMana,
            string? lastMonsterMoveId,
            string? monsterEffects,
            string? heroEffects,
            int? turn,
            BattleService battleService) =>
        {
            if (string.IsNullOrWhiteSpace(monsterId))
            {
                return Results.BadRequest(new { error = "monsterId is required." });
            }

            if (monsterLevel is null || monsterHealth is null || monsterMaxHealth is null
                || heroHealth is null || heroMaxHealth is null || turn is null)
            {
                return Results.BadRequest(new { error = "Missing required battle-state parameters." });
            }

            if (monsterMaxHealth <= 0 || heroMaxHealth <= 0 || turn < 1)
            {
                return Results.BadRequest(new { error = "Invalid battle-state values." });
            }

            var moveId = battleService.SelectNextMove(
                monsterId,
                monsterHealth.Value,
                monsterMaxHealth.Value,
                heroHealth.Value,
                heroMaxHealth.Value,
                monsterMana,
                lastMonsterMoveId,
                ParseEffects(monsterEffects),
                ParseEffects(heroEffects));

            if (moveId is null)
            {
                return Results.BadRequest(new { error = $"Unknown monsterId '{monsterId}'." });
            }

            return Results.Ok(new NextMoveResponse { MoveId = moveId });
        })
        .WithName("GetNextMove");
    }

    private static IReadOnlyCollection<string> ParseEffects(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
