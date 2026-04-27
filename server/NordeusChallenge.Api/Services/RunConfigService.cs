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
        var environments = BuildEnvironments();
        var items = BuildItems();
        var shopOffers = BuildShopOffers();
        var rules = BuildRules();
        var heroClasses = BuildHeroClasses();
        var mapNodes = BuildMapNodes();

        return new RunConfigResponse
        {
            RunId = $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Hero = hero,
            Encounters = encounters,
            Monsters = monsters,
            Moves = moves,
            Environments = environments,
            Items = items,
            ShopOffers = shopOffers,
            Rules = rules,
            HeroClasses = heroClasses,
            DefaultHeroClassId = "knight",
            MapNodes = mapNodes,
            StartingMapNodeId = "start_goblin_warrior"
        };
    }

    // Branching run map. Encounter indices line up with BuildEncounters():
    //   0 = goblin_warrior, 1 = goblin_mage, 2 = giant_spider,
    //   3 = skeleton_knight, 4 = forest_troll, 5 = witch,
    //   6 = fire_elemental, 7 = dragon
    //
    // Layout (left → right within each depth, depth increases downward):
    //
    //   D0:                         start_goblin_warrior
    //                                 /              \
    //   D1:               goblin_mage_path        spider_path
    //                       /        \             /        \
    //   D2:    skeleton_path     shop_early     forest_troll_path
    //                  \             |              /
    //                   \            |             /
    //   D3:        witch_path     shop_late    fire_elemental_path
    //                      \         |         /
    //                       \        |        /
    //   D4:                       dragon_peak  (Boss)
    //
    // Every path eventually reaches the boss. Shops are optional detours that
    // sit between battle layers, so the player trades a battle slot for a
    // chance to spend gold.
    private static List<RunMapNode> BuildMapNodes() => new()
    {
        // Depth 0
        new RunMapNode
        {
            Id = "start_goblin_warrior",
            Depth = 0,
            Position = 0,
            Type = "Battle",
            EncounterIndex = 0,
            ConnectedTo = new List<string> { "goblin_mage_path", "spider_path" }
        },

        // Depth 1
        new RunMapNode
        {
            Id = "goblin_mage_path",
            Depth = 1,
            Position = 0,
            Type = "Battle",
            EncounterIndex = 1,
            ConnectedTo = new List<string> { "skeleton_path", "shop_early" }
        },
        new RunMapNode
        {
            Id = "spider_path",
            Depth = 1,
            Position = 1,
            Type = "Battle",
            EncounterIndex = 2,
            ConnectedTo = new List<string> { "shop_early", "forest_troll_path" }
        },

        // Depth 2
        new RunMapNode
        {
            Id = "skeleton_path",
            Depth = 2,
            Position = 0,
            Type = "Elite",
            EncounterIndex = 3,
            ConnectedTo = new List<string> { "witch_path" }
        },
        new RunMapNode
        {
            Id = "shop_early",
            Depth = 2,
            Position = 1,
            Type = "Shop",
            EncounterIndex = -1,
            ConnectedTo = new List<string> { "witch_path", "shop_late", "fire_elemental_path" }
        },
        new RunMapNode
        {
            Id = "forest_troll_path",
            Depth = 2,
            Position = 2,
            Type = "Battle",
            EncounterIndex = 4,
            ConnectedTo = new List<string> { "fire_elemental_path" }
        },

        // Depth 3
        new RunMapNode
        {
            Id = "witch_path",
            Depth = 3,
            Position = 0,
            Type = "Elite",
            EncounterIndex = 5,
            ConnectedTo = new List<string> { "dragon_peak" }
        },
        new RunMapNode
        {
            Id = "shop_late",
            Depth = 3,
            Position = 1,
            Type = "Shop",
            EncounterIndex = -1,
            ConnectedTo = new List<string> { "dragon_peak" }
        },
        new RunMapNode
        {
            Id = "fire_elemental_path",
            Depth = 3,
            Position = 2,
            Type = "Elite",
            EncounterIndex = 6,
            ConnectedTo = new List<string> { "dragon_peak" }
        },

        // Depth 4 — boss
        new RunMapNode
        {
            Id = "dragon_peak",
            Depth = 4,
            Position = 0,
            Type = "Boss",
            EncounterIndex = 7,
            ConnectedTo = new List<string>()
        }
    };

    private static List<HeroClass> BuildHeroClasses() => new()
    {
        // Knight: balanced, the original default. Identical to BuildHero() so a
        // client that ignores HeroClasses still gets the same starting state.
        new HeroClass
        {
            Id = "knight",
            Name = "Knight",
            Description = "Balanced melee fighter. Reliable damage, solid defense, and a small self-heal.",
            StartingStats = new Stats
            {
                MaxHealth = 100,
                MaxMana = 20,
                Attack = 20,
                Defense = 15,
                Magic = 20
            },
            StartingMoves = new List<string> { "slash", "shield_up", "battle_cry", "second_wind" },
            StartingLearnedMoves = new List<string>
            {
                "slash", "shield_up", "battle_cry", "second_wind",
                "power_stance", "iron_skin", "rend"
            }
        },

        // Ranger: light striker. Higher attack, weak magic, leans on bleed and evasion.
        new HeroClass
        {
            Id = "ranger",
            Name = "Ranger",
            Description = "Agile striker. Trades raw HP for higher Attack and bleeding follow-ups.",
            StartingStats = new Stats
            {
                MaxHealth = 90,
                MaxMana = 18,
                Attack = 24,
                Defense = 12,
                Magic = 8
            },
            // Uses existing moves: slash (physical), rend (bleed), iron_skin
            // (defensive), second_wind (heal). All effect kinds already supported.
            StartingMoves = new List<string> { "slash", "rend", "iron_skin", "second_wind" },
            StartingLearnedMoves = new List<string>
            {
                "slash", "rend", "iron_skin", "second_wind",
                "power_stance", "battle_cry"
            }
        },

        // Mage: glass cannon. High magic and mana, fragile defense.
        new HeroClass
        {
            Id = "mage",
            Name = "Mage",
            Description = "Burst caster. High Magic and Max Mana, low Defense. Lives or dies by spell timing.",
            StartingStats = new Stats
            {
                MaxHealth = 80,
                MaxMana = 35,
                Attack = 8,
                Defense = 10,
                Magic = 24
            },
            // Uses existing moves: firebolt (magic dmg), mana_drain (debuff
            // magic), arcane_focus (new magic buff, see BuildMoves), hex_shield
            // (light defense). Effect kinds: DebuffMagic, BuffMagic, BuffDefense.
            StartingMoves = new List<string> { "firebolt", "mana_drain", "arcane_focus", "hex_shield" },
            StartingLearnedMoves = new List<string>
            {
                "firebolt", "mana_drain", "arcane_focus", "hex_shield",
                "arcane_surge", "second_wind"
            }
        },

        // Cleric: durable supporter. Strong heal, magic damage, defense buff.
        new HeroClass
        {
            Id = "cleric",
            Name = "Cleric",
            Description = "Durable support. Strong heals and defensive blessings, modest magic damage.",
            StartingStats = new Stats
            {
                MaxHealth = 110,
                MaxMana = 28,
                Attack = 10,
                Defense = 14,
                Magic = 18
            },
            // Uses existing moves: blessed_mend (new heal, see BuildMoves),
            // smite (new magic dmg, see BuildMoves), shield_up (BuffDefense),
            // iron_skin (DamageReduction).
            StartingMoves = new List<string> { "blessed_mend", "smite", "shield_up", "iron_skin" },
            StartingLearnedMoves = new List<string>
            {
                "blessed_mend", "smite", "shield_up", "iron_skin",
                "battle_cry", "second_wind"
            }
        }
    };

    private static List<ShopOffer> BuildShopOffers() => new()
    {
        // ---------- Item offers (3) ----------
        new ShopOffer
        {
            Id = "shop_apprentice_wand",
            Name = "Apprentice Wand",
            Description = "+3 Magic. A simple wand for budding spellcasters.",
            Price = 60,
            Type = "Item",
            ItemId = "apprentice_wand"
        },
        new ShopOffer
        {
            Id = "shop_chitin_plate",
            Name = "Chitin Plate",
            Description = "+4 Defense, +8 Max Health. Layered insectoid armor.",
            Price = 90,
            Type = "Item",
            ItemId = "chitin_plate"
        },
        new ShopOffer
        {
            Id = "shop_lifedrinker_locket",
            Name = "Lifedrinker Locket",
            Description = "+10 Max Health, +2 Magic. Steals a drop of vitality with every spell.",
            Price = 140,
            Type = "Item",
            ItemId = "lifedrinker_locket"
        },

        // ---------- Stat upgrade offers (5) ----------
        new ShopOffer
        {
            Id = "shop_upgrade_health",
            Name = "+10 Max Health",
            Description = "Permanent: raises Max Health by 10.",
            Price = 50,
            Type = "StatUpgrade",
            Stat = "maxHealth",
            Amount = 10
        },
        new ShopOffer
        {
            Id = "shop_upgrade_mana",
            Name = "+5 Max Mana",
            Description = "Permanent: raises Max Mana by 5.",
            Price = 50,
            Type = "StatUpgrade",
            Stat = "maxMana",
            Amount = 5
        },
        new ShopOffer
        {
            Id = "shop_upgrade_attack",
            Name = "+2 Attack",
            Description = "Permanent: raises Attack by 2.",
            Price = 60,
            Type = "StatUpgrade",
            Stat = "attack",
            Amount = 2
        },
        new ShopOffer
        {
            Id = "shop_upgrade_defense",
            Name = "+2 Defense",
            Description = "Permanent: raises Defense by 2.",
            Price = 60,
            Type = "StatUpgrade",
            Stat = "defense",
            Amount = 2
        },
        new ShopOffer
        {
            Id = "shop_upgrade_magic",
            Name = "+2 Magic",
            Description = "Permanent: raises Magic by 2.",
            Price = 60,
            Type = "StatUpgrade",
            Stat = "magic",
            Amount = 2
        }
    };

    private static Hero BuildHero()
    {
        var equipped = new List<string>
        {
            "slash",
            "shield_up",
            "battle_cry",
            "second_wind"
        };

        // Pool also includes a few moves the player can swap in via Move
        // Management to demonstrate the new effect kinds.
        var pool = new List<string>(equipped)
        {
            "power_stance",
            "iron_skin",
            "rend"
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
                MaxMana = 20,
                Attack = 20,
                Defense = 15,
                Magic = 20
            },
            EquippedMoves = equipped,
            LearnedMovePool = pool
        };
    }

    private static List<Encounter> BuildEncounters() => new()
    {
        new Encounter { Index = 0, MonsterId = "goblin_warrior",  Level = 1, EnvironmentId = "training_fields" },
        new Encounter { Index = 1, MonsterId = "goblin_mage",     Level = 2, EnvironmentId = "arcane_library"  },
        new Encounter { Index = 2, MonsterId = "giant_spider",    Level = 3, EnvironmentId = "spider_nest"     },
        new Encounter { Index = 3, MonsterId = "skeleton_knight", Level = 4, EnvironmentId = "crypt"           },
        new Encounter { Index = 4, MonsterId = "forest_troll",    Level = 5, EnvironmentId = "ancient_forest"  },
        new Encounter { Index = 5, MonsterId = "witch",           Level = 5, EnvironmentId = "dark_altar"      },
        new Encounter { Index = 6, MonsterId = "fire_elemental",  Level = 6, EnvironmentId = "ember_chamber"   },
        new Encounter { Index = 7, MonsterId = "dragon",          Level = 7, EnvironmentId = "dragon_peak"     }
    };

    private static List<BattleEnvironment> BuildEnvironments() => new()
    {
        new BattleEnvironment
        {
            Id = "training_fields",
            Name = "Training Fields",
            Description = "Open ground favors clean strikes. Physical hits land a little harder.",
            PhysicalDamageBonus = 2
        },
        new BattleEnvironment
        {
            Id = "arcane_library",
            Name = "Arcane Library",
            Description = "Latent magic in the air empowers spells and gently restores mana.",
            MagicDamageBonus = 2,
            ManaRegenBonus = 1
        },
        new BattleEnvironment
        {
            Id = "spider_nest",
            Name = "Spider Nest",
            Description = "Lingering venom makes poisons cling longer.",
            PoisonBonusTurns = 1
        },
        new BattleEnvironment
        {
            Id = "dark_altar",
            Name = "Dark Altar",
            Description = "Cursed ground deepens bleeds and saps a sliver of life each turn.",
            BleedBonusDamage = 1,
            EndOfTurnDamage = 2
        },
        new BattleEnvironment
        {
            Id = "dragon_peak",
            Name = "Dragon Peak",
            Description = "Thin mountain air pushes magic harder; healing is a touch less effective.",
            MagicDamageBonus = 3,
            HealingBonus = -3
        },
        new BattleEnvironment
        {
            Id = "crypt",
            Name = "Crypt",
            Description = "Cold tomb air dulls healing and lets bleeds linger a little longer.",
            BleedBonusDamage = 1,
            HealingBonus = -1
        },
        new BattleEnvironment
        {
            Id = "ancient_forest",
            Name = "Ancient Forest",
            Description = "Damp moss and old roots favor patient defenders. Poisons bite a touch harder.",
            PoisonBonusTurns = 1
        },
        new BattleEnvironment
        {
            Id = "ember_chamber",
            Name = "Ember Chamber",
            Description = "Heat from glowing embers strengthens spells but withers healing.",
            MagicDamageBonus = 2,
            HealingBonus = -2
        }
    };

    private static List<Monster> BuildMonsters() => new()
    {
        new Monster
        {
            Id = "goblin_warrior",
            Name = "Goblin Warrior",
            BaseStats = new Stats { MaxHealth = 60, MaxMana = 10, Attack = 12, Defense = 8, Magic = 4 },
            Moves = new List<string> { "rusty_blade", "dirty_kick", "frenzy", "headbutt" },
            ItemDrops = new List<string> { "rusty_shortsword", "tattered_leather" }
        },
        new Monster
        {
            Id = "goblin_mage",
            Name = "Goblin Mage",
            BaseStats = new Stats { MaxHealth = 55, MaxMana = 25, Attack = 6, Defense = 6, Magic = 14 },
            Moves = new List<string> { "firebolt", "arcane_surge", "mana_drain", "hex_shield" },
            ItemDrops = new List<string> { "apprentice_wand", "mana_charm" }
        },
        new Monster
        {
            Id = "giant_spider",
            Name = "Giant Spider",
            BaseStats = new Stats { MaxHealth = 75, MaxMana = 10, Attack = 14, Defense = 10, Magic = 4 },
            Moves = new List<string> { "bite", "web_throw", "pounce", "skitter", "venom_bite" },
            ItemDrops = new List<string> { "chitin_plate", "venom_fang" }
        },
        new Monster
        {
            Id = "witch",
            Name = "Witch",
            BaseStats = new Stats { MaxHealth = 80, MaxMana = 25, Attack = 8, Defense = 8, Magic = 15 },
            Moves = new List<string> { "shadow_bolt", "drain_life", "curse", "dark_pact", "bleeding_curse" },
            ItemDrops = new List<string> { "hex_focus", "lifedrinker_locket" }
        },
        new Monster
        {
            Id = "dragon",
            Name = "Dragon",
            BaseStats = new Stats { MaxHealth = 120, MaxMana = 30, Attack = 18, Defense = 14, Magic = 16 },
            Moves = new List<string> { "flame_breath", "claw_swipe", "intimidate", "dragon_scales" },
            ItemDrops = new List<string> { "dragonbone_blade", "scale_aegis" }
        },
        new Monster
        {
            Id = "skeleton_knight",
            Name = "Skeleton Knight",
            BaseStats = new Stats { MaxHealth = 90, MaxMana = 15, Attack = 15, Defense = 12, Magic = 4 },
            Moves = new List<string> { "bone_slash", "shield_wall", "grave_wound", "death_rattle" },
            ItemDrops = new List<string> { "bone_pauldrons", "grave_signet" }
        },
        new Monster
        {
            Id = "forest_troll",
            Name = "Forest Troll",
            BaseStats = new Stats { MaxHealth = 110, MaxMana = 18, Attack = 16, Defense = 11, Magic = 6 },
            Moves = new List<string> { "heavy_club", "regenerate", "toxic_spores", "thick_hide" },
            ItemDrops = new List<string> { "troll_hide_cloak" }
        },
        new Monster
        {
            Id = "fire_elemental",
            Name = "Fire Elemental",
            BaseStats = new Stats { MaxHealth = 95, MaxMana = 28, Attack = 8, Defense = 9, Magic = 14 },
            Moves = new List<string> { "flame_burst", "scorching_aura", "ignite", "mana_flare" },
            ItemDrops = new List<string> { "ember_core" }
        }
    };

    private static List<Item> BuildItems() => new()
    {
        // ---------- Goblin Warrior drops ----------
        new Item
        {
            Id = "rusty_shortsword",
            Name = "Rusty Shortsword",
            Description = "Crude but serviceable. +3 Attack.",
            Slot = "weapon",
            Rarity = "common",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "attack", Amount = 3 }
            }
        },
        new Item
        {
            Id = "tattered_leather",
            Name = "Tattered Leather",
            Description = "Worn but better than nothing. +2 Defense, +5 Max Health.",
            Slot = "armor",
            Rarity = "common",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "defense", Amount = 2 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 5 }
            }
        },

        // ---------- Goblin Mage drops ----------
        new Item
        {
            Id = "apprentice_wand",
            Name = "Apprentice Wand",
            Description = "A simple wand humming with arcane energy. +3 Magic.",
            Slot = "weapon",
            Rarity = "common",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "magic", Amount = 3 }
            }
        },
        new Item
        {
            Id = "mana_charm",
            Name = "Mana Charm",
            Description = "Steadies the flow of mana. +8 Max Mana, +1 Magic.",
            Slot = "trinket",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "maxMana", Amount = 8 },
                new ItemStatBonus { Stat = "magic", Amount = 1 }
            }
        },

        // ---------- Giant Spider drops ----------
        new Item
        {
            Id = "chitin_plate",
            Name = "Chitin Plate",
            Description = "Layered insectoid armor. +4 Defense, +8 Max Health.",
            Slot = "armor",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "defense", Amount = 4 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 8 }
            }
        },
        new Item
        {
            Id = "venom_fang",
            Name = "Venom Fang",
            Description = "A wickedly curved fang slick with venom. +2 Attack, +2 Defense.",
            Slot = "trinket",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "attack", Amount = 2 },
                new ItemStatBonus { Stat = "defense", Amount = 2 }
            }
        },

        // ---------- Witch drops ----------
        new Item
        {
            Id = "hex_focus",
            Name = "Hex Focus",
            Description = "Concentrates dark energies through bone and silver. +4 Magic, +5 Max Mana.",
            Slot = "weapon",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "magic", Amount = 4 },
                new ItemStatBonus { Stat = "maxMana", Amount = 5 }
            }
        },
        new Item
        {
            Id = "lifedrinker_locket",
            Name = "Lifedrinker Locket",
            Description = "Steals a drop of vitality with every spell. +10 Max Health, +2 Magic.",
            Slot = "trinket",
            Rarity = "rare",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "maxHealth", Amount = 10 },
                new ItemStatBonus { Stat = "magic", Amount = 2 }
            }
        },

        // ---------- Dragon drops (late-run, strong) ----------
        new Item
        {
            Id = "dragonbone_blade",
            Name = "Dragonbone Blade",
            Description = "Forged from a dragon's own bones. +8 Attack, +5 Max Health.",
            Slot = "weapon",
            Rarity = "epic",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "attack", Amount = 8 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 5 }
            }
        },
        new Item
        {
            Id = "scale_aegis",
            Name = "Scale Aegis",
            Description = "A bulwark of layered dragon scales. +6 Defense, +15 Max Health.",
            Slot = "armor",
            Rarity = "epic",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "defense", Amount = 6 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 15 }
            }
        },

        // ---------- Skeleton Knight drops ----------
        new Item
        {
            Id = "bone_pauldrons",
            Name = "Bone Pauldrons",
            Description = "Heavy plates fashioned from old bone. +3 Defense, +10 Max Health.",
            Slot = "armor",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "defense", Amount = 3 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 10 }
            }
        },
        new Item
        {
            Id = "grave_signet",
            Name = "Grave Signet",
            Description = "A cold signet ring humming with quiet menace. +2 Attack, +2 Magic.",
            Slot = "trinket",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "attack", Amount = 2 },
                new ItemStatBonus { Stat = "magic", Amount = 2 }
            }
        },

        // ---------- Forest Troll drops ----------
        new Item
        {
            Id = "troll_hide_cloak",
            Name = "Troll Hide Cloak",
            Description = "Thick troll hide that shrugs off blows. +3 Defense, +12 Max Health.",
            Slot = "armor",
            Rarity = "uncommon",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "defense", Amount = 3 },
                new ItemStatBonus { Stat = "maxHealth", Amount = 12 }
            }
        },

        // ---------- Fire Elemental drops ----------
        new Item
        {
            Id = "ember_core",
            Name = "Ember Core",
            Description = "A still-warm shard of living flame. +3 Magic, +6 Max Mana.",
            Slot = "trinket",
            Rarity = "rare",
            StatBonuses = new List<ItemStatBonus>
            {
                new ItemStatBonus { Stat = "magic", Amount = 3 },
                new ItemStatBonus { Stat = "maxMana", Amount = 6 }
            }
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
            ManaCost = 2,
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
            ManaCost = 2,
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
            ManaCost = 4,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 25,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "Restores a portion of the Knight's HP."
        },
        new Move
        {
            Id = "power_stance",
            Name = "Power Stance",
            Category = "Buff",
            Power = 0,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "DamageIncrease",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Channels focus, increasing damage dealt for 2 turns."
        },
        new Move
        {
            Id = "iron_skin",
            Name = "Iron Skin",
            Category = "Buff",
            Power = 0,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "DamageReduction",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Hardens the body, reducing incoming damage for 2 turns."
        },
        new Move
        {
            Id = "rend",
            Name = "Rend",
            Category = "Physical",
            Power = 12,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "Bleed",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A tearing strike that leaves the foe bleeding."
        },

        // ---------- Class-specific moves (Mage, Cleric) ----------
        new Move
        {
            Id = "arcane_focus",
            Name = "Arcane Focus",
            Category = "Buff",
            Power = 0,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "BuffMagic",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Gathers latent energy, raising Magic for 2 turns."
        },
        new Move
        {
            Id = "blessed_mend",
            Name = "Blessed Mend",
            Category = "Heal",
            Power = 0,
            ManaCost = 4,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 28,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "A whispered prayer that knits wounds closed."
        },
        new Move
        {
            Id = "smite",
            Name = "Smite",
            Category = "Magic",
            Power = 16,
            ManaCost = 3,
            Effect = null,
            Description = "A column of radiant force."
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
            ManaCost = 2,
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
            ManaCost = 2,
            Effect = null,
            Description = "A small bolt of fire."
        },
        new Move
        {
            Id = "arcane_surge",
            Name = "Arcane Surge",
            Category = "Magic",
            Power = 22,
            ManaCost = 4,
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
            ManaCost = 2,
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
            Description = "A sharp bite."
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
        new Move
        {
            Id = "venom_bite",
            Name = "Venom Bite",
            Category = "Physical",
            Power = 12,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "Poison",
                Amount = 3,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A venomous bite that poisons the target for 2 turns."
        },

        // ---------- Witch ----------
        new Move
        {
            Id = "shadow_bolt",
            Name = "Shadow Bolt",
            Category = "Magic",
            Power = 20,
            ManaCost = 2,
            Effect = null,
            Description = "A bolt of shadow energy."
        },
        new Move
        {
            Id = "drain_life",
            Name = "Drain Life",
            Category = "Heal",
            Power = 0,
            ManaCost = 4,
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
            ManaCost = 2,
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
            HpCost = 5,
            Effect = new MoveEffect
            {
                Kind = "BuffMagic",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "A dark bargain that pays HP to raise Magic for 2 turns."
        },
        new Move
        {
            Id = "bleeding_curse",
            Name = "Bleeding Curse",
            Category = "Magic",
            Power = 8,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "Bleed",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A hex that opens unseen wounds for 2 turns."
        },

        // ---------- Dragon ----------
        new Move
        {
            Id = "flame_breath",
            Name = "Flame Breath",
            Category = "Magic",
            Power = 20,
            ManaCost = 6,
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
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Hardens scales, raising Defense for 2 turns."
        },

        // ---------- Skeleton Knight ----------
        new Move
        {
            Id = "bone_slash",
            Name = "Bone Slash",
            Category = "Physical",
            Power = 18,
            Effect = null,
            Description = "A clean strike with a chipped bone blade."
        },
        new Move
        {
            Id = "shield_wall",
            Name = "Shield Wall",
            Category = "Buff",
            Power = 0,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "DamageReduction",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Braces behind a tower shield, reducing incoming damage for 2 turns."
        },
        new Move
        {
            Id = "grave_wound",
            Name = "Grave Wound",
            Category = "Physical",
            Power = 12,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "Bleed",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A grim cut that opens a slow, bleeding wound for 2 turns."
        },
        new Move
        {
            Id = "death_rattle",
            Name = "Death Rattle",
            Category = "Debuff",
            Power = 0,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "DebuffDefense",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "An unnerving rattle that lowers the opponent's Defense for 2 turns."
        },

        // ---------- Forest Troll ----------
        new Move
        {
            Id = "heavy_club",
            Name = "Heavy Club",
            Category = "Physical",
            Power = 18,
            Effect = null,
            Description = "A crushing swing with a massive club."
        },
        new Move
        {
            Id = "regenerate",
            Name = "Regenerate",
            Category = "Heal",
            Power = 0,
            ManaCost = 4,
            Effect = new MoveEffect
            {
                Kind = "Heal",
                Amount = 22,
                DurationTurns = 0,
                Target = "Self"
            },
            Description = "Knits torn flesh back together, restoring HP."
        },
        new Move
        {
            Id = "toxic_spores",
            Name = "Toxic Spores",
            Category = "Magic",
            Power = 6,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "Poison",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A cloud of pungent spores that poisons the target for 2 turns."
        },
        new Move
        {
            Id = "thick_hide",
            Name = "Thick Hide",
            Category = "Buff",
            Power = 0,
            ManaCost = 2,
            Effect = new MoveEffect
            {
                Kind = "BuffDefense",
                Amount = 5,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Toughens skin and sinew, raising Defense for 2 turns."
        },

        // ---------- Fire Elemental ----------
        new Move
        {
            Id = "flame_burst",
            Name = "Flame Burst",
            Category = "Magic",
            Power = 20,
            ManaCost = 3,
            Effect = null,
            Description = "A sudden burst of flame."
        },
        new Move
        {
            Id = "scorching_aura",
            Name = "Scorching Aura",
            Category = "Buff",
            Power = 0,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "DamageIncrease",
                Amount = 4,
                DurationTurns = 2,
                Target = "Self"
            },
            Description = "Wreathes itself in heat, increasing damage dealt for 2 turns."
        },
        new Move
        {
            Id = "ignite",
            Name = "Ignite",
            Category = "Magic",
            Power = 10,
            ManaCost = 3,
            Effect = new MoveEffect
            {
                Kind = "Bleed",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "Sets the target ablaze, searing them for 2 turns."
        },
        new Move
        {
            Id = "mana_flare",
            Name = "Mana Flare",
            Category = "Magic",
            Power = 16,
            ManaCost = 4,
            Effect = new MoveEffect
            {
                Kind = "DebuffMagic",
                Amount = 4,
                DurationTurns = 2,
                Target = "Opponent"
            },
            Description = "A blinding flare that scorches and saps the opponent's Magic for 2 turns."
        }
    };

    private static RulesConfig BuildRules() => new()
    {
        BuffDurationTurns = 2,
        XpPerVictory = 35,
        XpPerLevel = 100,
        StatGainPerLevel = new Stats
        {
            MaxHealth = 10,
            MaxMana = 0,
            Attack = 2,
            Defense = 2,
            Magic = 2
        },
        EquippedMoveSlots = 4,
        GoldPerVictory = 20,
        EquippedItemSlots = new Dictionary<string, int>
        {
            { "weapon", 1 },
            { "armor", 1 },
            { "trinket", 2 }
        },
        LevelUpChoices = new List<LevelUpChoice>
        {
            new LevelUpChoice
            {
                Id = "health",
                Name = "+15 Max Health",
                Description = "Toughen up. Raises Max Health by 15.",
                Stat = "health",
                Amount = 15
            },
            new LevelUpChoice
            {
                Id = "attack",
                Name = "+4 Attack",
                Description = "Hit harder. Raises Attack by 4.",
                Stat = "attack",
                Amount = 4
            },
            new LevelUpChoice
            {
                Id = "defense",
                Name = "+4 Defense",
                Description = "Brace yourself. Raises Defense by 4.",
                Stat = "defense",
                Amount = 4
            },
            new LevelUpChoice
            {
                Id = "magic",
                Name = "+4 Magic",
                Description = "Sharpen your focus. Raises Magic by 4.",
                Stat = "magic",
                Amount = 4
            },
            new LevelUpChoice
            {
                Id = "mana",
                Name = "+10 Max Mana",
                Description = "Deepen your reserves. Raises Max Mana by 10.",
                Stat = "mana",
                Amount = 10
            }
        }
    };
}
