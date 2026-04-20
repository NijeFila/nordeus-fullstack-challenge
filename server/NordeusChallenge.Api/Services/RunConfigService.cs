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
            "slash",
            "shield_up",
            "battle_cry",
            "second_wind"
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
            Moves = new List<string> { "rusty_blade", "dirty_kick", "frenzy", "headbutt" }
        },
        new Monster
        {
            Id = "goblin_mage",
            Name = "Goblin Mage",
            BaseStats = new Stats { MaxHealth = 55, Attack = 6, Defense = 6, Magic = 14 },
            Moves = new List<string> { "firebolt", "arcane_surge", "mana_drain", "hex_shield" }
        },
        new Monster
        {
            Id = "giant_spider",
            Name = "Giant Spider",
            BaseStats = new Stats { MaxHealth = 75, Attack = 14, Defense = 10, Magic = 4 },
            Moves = new List<string> { "bite", "web_throw", "pounce", "skitter" }
        },
        new Monster
        {
            Id = "witch",
            Name = "Witch",
            BaseStats = new Stats { MaxHealth = 80, Attack = 8, Defense = 8, Magic = 18 },
            Moves = new List<string> { "shadow_bolt", "drain_life", "curse", "dark_pact" }
        },
        new Monster
        {
            Id = "dragon",
            Name = "Dragon",
            BaseStats = new Stats { MaxHealth = 120, Attack = 18, Defense = 14, Magic = 16 },
            Moves = new List<string> { "flame_breath", "claw_swipe", "intimidate", "dragon_scales" }
        }
    };

    private static List<Move> BuildMoves() => new()
    {
        // ---------- Hero: Knight ----------
        new Move
        {
            Id = "slash",
            Name = "Slash",
            Category = "Physical",
            Power = 20,
            Effect = null,
            Description = "A direct sword strike."
        },
        new Move
        {
            Id = "shield_up",
            Name = "Shield Up",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Raises the Knight's Defense for 2 turns."
        },
        new Move
        {
            Id = "battle_cry",
            Name = "Battle Cry",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffAttack",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A rallying shout that raises Attack for 2 turns."
        },
        new Move
        {
            Id = "second_wind",
            Name = "Second Wind",
            Category = "Heal",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 25,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "Restores a portion of the Knight's HP."
        },

        // ---------- Goblin Warrior ----------
        new Move
        {
            Id = "rusty_blade",
            Name = "Rusty Blade",
            Category = "Physical",
            Power = 14,
            Effect = null,
            Description = "A crude physical swing."
        },
        new Move
        {
            Id = "dirty_kick",
            Name = "Dirty Kick",
            Category = "Physical",
            Power = 10,
            Effect = new MoveEffect
            {
                Kind = "DebuffAttack",
                Amount = 3,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A low blow that lowers the opponent's Attack for 2 turns."
        },
        new Move
        {
            Id = "frenzy",
            Name = "Frenzy",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffAttack",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Works itself into a rage, raising Attack for 2 turns."
        },
        new Move
        {
            Id = "headbutt",
            Name = "Headbutt",
            Category = "Physical",
            Power = 18,
            Effect = null,
            Description = "A reckless headfirst charge."
        },

        // ---------- Goblin Mage ----------
        new Move
        {
            Id = "firebolt",
            Name = "Firebolt",
            Category = "Magic",
            Power = 18,
            Effect = null,
            Description = "A small bolt of fire."
        },
        new Move
        {
            Id = "arcane_surge",
            Name = "Arcane Surge",
            Category = "Magic",
            Power = 22,
            Effect = null,
            Description = "A burst of raw arcane energy."
        },
        new Move
        {
            Id = "mana_drain",
            Name = "Mana Drain",
            Category = "Debuff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "DebuffMagic",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "Saps the opponent's Magic for 2 turns."
        },
        new Move
        {
            Id = "hex_shield",
            Name = "Hex Shield",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A warded barrier that raises Defense for 2 turns."
        },

        // ---------- Giant Spider ----------
        new Move
        {
            Id = "bite",
            Name = "Bite",
            Category = "Physical",
            Power = 16,
            Effect = null,
            Description = "A sharp venomous bite."
        },
        new Move
        {
            Id = "web_throw",
            Name = "Web Throw",
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
        new Move
        {
            Id = "pounce",
            Name = "Pounce",
            Category = "Physical",
            Power = 20,
            Effect = null,
            Description = "A leaping strike from above."
        },
        new Move
        {
            Id = "skitter",
            Name = "Skitter",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 3,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Darts around erratically, raising Defense for 2 turns."
        },

        // ---------- Witch ----------
        new Move
        {
            Id = "shadow_bolt",
            Name = "Shadow Bolt",
            Category = "Magic",
            Power = 20,
            Effect = null,
            Description = "A bolt of shadow energy."
        },
        new Move
        {
            Id = "drain_life",
            Name = "Drain Life",
            Category = "Heal",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 18,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "Siphons vitality, restoring the caster's HP."
        },
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
            Id = "dark_pact",
            Name = "Dark Pact",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffMagic",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A dark bargain that raises Magic for 2 turns."
        },

        // ---------- Dragon ----------
        new Move
        {
            Id = "flame_breath",
            Name = "Flame Breath",
            Category = "Magic",
            Power = 24,
            Effect = null,
            Description = "A powerful cone of fire."
        },
        new Move
        {
            Id = "claw_swipe",
            Name = "Claw Swipe",
            Category = "Physical",
            Power = 20,
            Effect = null,
            Description = "A heavy swipe with razor claws."
        },
        new Move
        {
            Id = "intimidate",
            Name = "Intimidate",
            Category = "Debuff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "DebuffAttack",
                Amount = 5,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A fearsome display that lowers the opponent's Attack for 2 turns."
        },
        new Move
        {
            Id = "dragon_scales",
            Name = "Dragon Scales",
            Category = "Buff",
            Power = 0,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Hardens scales, raising Defense for 2 turns."
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
