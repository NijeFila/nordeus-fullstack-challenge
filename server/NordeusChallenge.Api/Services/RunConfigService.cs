using NordeusChallenge.Api.Models;

namespace NordeusChallenge.Api.Services;

// Returns a hardcoded run configuration. Values are intentionally simple
// and explicit so they are easy to tune while the prototype evolves.
public class RunConfigService
{
    public RunConfigResponse GetRunConfig()
    {
        var moves = BuildMoves();
        var monsters = BuildMonsters();
        var hero = BuildHero();
        var encounters = BuildEncounters();
        var rules = BuildRules();

        return new RunConfigResponse
        {
            RunId = $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Hero = hero,
            Encounters = encounters,
            Monsters = monsters,
            Moves = moves,
            Rules = rules
        };
    }

    private static Hero BuildHero()
    {
        var equipped = new List<string>
        {
            "sword_strike",
            "shield_bash",
            "holy_light",
            "guard_stance"
        };

        return new Hero
        {
            Id = "knight",
            Name = "Knight",
            Level = 1,
            Xp = 0,
            Stats = new Stats
            {
                MaxHealth = 100,
                Attack = 20,
                Defense = 15,
                Magic = 20
            },
            EquippedMoves = new List<string>(equipped),
            LearnedMovePool = new List<string>(equipped)
        };
    }

    private static List<Encounter> BuildEncounters() => new()
    {
        new Encounter { Index = 0, MonsterId = "goblin_warrior", Level = 1 },
        new Encounter { Index = 1, MonsterId = "goblin_mage",    Level = 2 },
        new Encounter { Index = 2, MonsterId = "giant_spider",   Level = 3 },
        new Encounter { Index = 3, MonsterId = "witch",          Level = 4 },
        new Encounter { Index = 4, MonsterId = "dragon",         Level = 5 }
    };

    private static List<Monster> BuildMonsters() => new()
    {
        new Monster
        {
            Id = "goblin_warrior",
            Name = "Goblin Warrior",
            BaseStats = new Stats { MaxHealth = 60, Attack = 12, Defense = 8, Magic = 4 },
            Moves = new List<string> { "rusty_slash", "reckless_charge" }
        },
        new Monster
        {
            Id = "goblin_mage",
            Name = "Goblin Mage",
            BaseStats = new Stats { MaxHealth = 55, Attack = 6, Defense = 6, Magic = 14 },
            Moves = new List<string> { "fire_bolt", "hex" }
        },
        new Monster
        {
            Id = "giant_spider",
            Name = "Giant Spider",
            BaseStats = new Stats { MaxHealth = 75, Attack = 14, Defense = 10, Magic = 4 },
            Moves = new List<string> { "bite", "web_shot" }
        },
        new Monster
        {
            Id = "witch",
            Name = "Witch",
            BaseStats = new Stats { MaxHealth = 80, Attack = 8, Defense = 8, Magic = 18 },
            Moves = new List<string> { "curse", "dark_blast", "mend" }
        },
        new Monster
        {
            Id = "dragon",
            Name = "Dragon",
            BaseStats = new Stats { MaxHealth = 120, Attack = 18, Defense = 14, Magic = 16 },
            Moves = new List<string> { "claw", "fire_breath", "roar" }
        }
    };

    private static List<Move> BuildMoves() => new()
    {
        // Hero moves
        new Move
        {
            Id = "sword_strike",
            Name = "Sword Strike",
            Category = "Physical",
            Power = 20,
            Effect = null,
            Description = "A clean sword hit."
        },
        new Move
        {
            Id = "shield_bash",
            Name = "Shield Bash",
            Category = "Physical",
            Power = 14,
            Effect = new MoveEffect
            {
                Kind = "DebuffAttack",
                Amount = 3,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A shield hit that lowers the opponent's Attack for 2 turns."
        },
        new Move
        {
            Id = "holy_light",
            Name = "Holy Light",
            Category = "Magic",
            Power = 22,
            Effect = null,
            Description = "A burst of radiant energy."
        },
        new Move
        {
            Id = "guard_stance",
            Name = "Guard Stance",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Raises Defense for 2 turns."
        },

        // Goblin Warrior
        new Move
        {
            Id = "rusty_slash",
            Name = "Rusty Slash",
            Category = "Physical",
            Power = 12,
            Effect = null,
            Description = "A crude physical slash."
        },
        new Move
        {
            Id = "reckless_charge",
            Name = "Reckless Charge",
            Category = "Physical",
            Power = 18,
            Effect = new MoveEffect
            {
                Kind = "DebuffDefense",
                Amount = 3,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A strong charge that leaves the attacker exposed."
        },

        // Goblin Mage
        new Move
        {
            Id = "fire_bolt",
            Name = "Fire Bolt",
            Category = "Magic",
            Power = 16,
            Effect = null,
            Description = "A small bolt of fire."
        },
        new Move
        {
            Id = "hex",
            Name = "Hex",
            Category = "Debuff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "DebuffMagic",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "Lowers the opponent's Magic for 2 turns."
        },

        // Giant Spider
        new Move
        {
            Id = "bite",
            Name = "Bite",
            Category = "Physical",
            Power = 16,
            Effect = null,
            Description = "A sharp bite."
        },
        new Move
        {
            Id = "web_shot",
            Name = "Web Shot",
            Category = "Debuff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "DebuffAttack",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "Entangles the target, lowering their Attack for 2 turns."
        },

        // Witch
        new Move
        {
            Id = "curse",
            Name = "Curse",
            Category = "Debuff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "DebuffDefense",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "Weakens the opponent's Defense for 2 turns."
        },
        new Move
        {
            Id = "dark_blast",
            Name = "Dark Blast",
            Category = "Magic",
            Power = 20,
            Effect = null,
            Description = "A blast of dark energy."
        },
        new Move
        {
            Id = "mend",
            Name = "Mend",
            Category = "Heal",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 20,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "Restores HP to the caster."
        },

        // Dragon
        new Move
        {
            Id = "claw",
            Name = "Claw",
            Category = "Physical",
            Power = 20,
            Effect = null,
            Description = "A heavy claw swipe."
        },
        new Move
        {
            Id = "fire_breath",
            Name = "Fire Breath",
            Category = "Magic",
            Power = 24,
            Effect = null,
            Description = "A powerful cone of fire."
        },
        new Move
        {
            Id = "roar",
            Name = "Roar",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffAttack",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A mighty roar that raises Attack for 2 turns."
        }
    };

    private static RulesConfig BuildRules() => new()
    {
        BuffDurationTurns = 2,
        XpPerVictory = 25,
        XpPerLevel = 100,
        StatGainPerLevel = new Stats
        {
            MaxHealth = 10,
            Attack = 2,
            Defense = 2,
            Magic = 2
        },
        EquippedMoveSlots = 4
    };
}
