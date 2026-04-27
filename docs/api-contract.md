# API Contract

Two HTTP endpoints are exposed by the ASP.NET Core server. All payloads are JSON. All identifiers are stable strings unless noted.

## Conventions

- Base URL: `http://localhost:5000` (configurable).
- All responses use `application/json; charset=utf-8`.
- Error responses use standard HTTP status codes with a small JSON body: `{ "error": "<message>" }`.
- Stat and damage values are integers. Probabilities are floats in `[0, 1]`.

---

## 1. GET `/run/config`

**Purpose**
Return the full configuration the client needs to start and run an entire playthrough: the hero's starting state, the ordered list of encounters, and the catalog of monsters and moves referenced by those encounters.

**When called**
Once, by the client, when the player starts a new run from the Main Menu.

**Request**
No parameters. No body.

**Response (200 OK)**

```json
{
  "runId": "run-2026-04-21-abcdef",
  "hero": {
    "id": "knight",
    "name": "Knight",
    "level": 1,
    "xp": 0,
    "stats": {
      "maxHealth": 100,
      "maxMana": 20,
      "attack": 20,
      "defense": 15,
      "magic": 20
    },
    "equippedMoves": ["slash", "shield_up", "battle_cry", "second_wind"],
    "learnedMovePool": ["slash", "shield_up", "battle_cry", "second_wind"]
  },
  "endlessMode": {
    "enabled": true,
    "startingFloor": 1,
    "eliteEvery": 5,
    "shopEvery": 3,
    "bossEvery": 10,
    "baseLevel": 1,
    "levelIncreaseEvery": 2,
    "rewardGoldBase": 15,
    "rewardGoldPerFloor": 2,
    "xpBase": 30,
    "xpPerFloor": 3,
    "endlessGoldScalingBp": 0,
    "endlessXpScalingBp": 0,
    "monsterPool": ["goblin_warrior", "goblin_mage", "giant_spider", "skeleton_knight", "forest_troll"],
    "eliteMonsterPool": ["witch", "fire_elemental", "forest_troll", "skeleton_knight"],
    "bossMonsterPool": ["dragon"],
    "environmentPool": ["training_fields", "arcane_library", "spider_nest", "dark_altar", "dragon_peak", "crypt", "ancient_forest", "ember_chamber"]
  },
  "startingMapNodeId": "start_goblin_warrior",
  "mapNodes": [
    { "id": "start_goblin_warrior", "depth": 0, "position": 0, "type": "Battle", "encounterIndex": 0, "connectedTo": ["goblin_mage_path", "spider_path"] },
    { "id": "goblin_mage_path",     "depth": 1, "position": 0, "type": "Battle", "encounterIndex": 1, "connectedTo": ["skeleton_path", "shop_early"] },
    { "id": "spider_path",          "depth": 1, "position": 1, "type": "Battle", "encounterIndex": 2, "connectedTo": ["shop_early", "forest_troll_path"] },
    { "id": "skeleton_path",        "depth": 2, "position": 0, "type": "Elite",  "encounterIndex": 3, "connectedTo": ["witch_path"] },
    { "id": "shop_early",           "depth": 2, "position": 1, "type": "Shop",   "encounterIndex": -1, "connectedTo": ["witch_path", "shop_late", "fire_elemental_path"] },
    { "id": "forest_troll_path",    "depth": 2, "position": 2, "type": "Battle", "encounterIndex": 4, "connectedTo": ["fire_elemental_path"] },
    { "id": "witch_path",           "depth": 3, "position": 0, "type": "Elite",  "encounterIndex": 5, "connectedTo": ["dragon_peak"] },
    { "id": "shop_late",            "depth": 3, "position": 1, "type": "Shop",   "encounterIndex": -1, "connectedTo": ["dragon_peak"] },
    { "id": "fire_elemental_path",  "depth": 3, "position": 2, "type": "Elite",  "encounterIndex": 6, "connectedTo": ["dragon_peak"] },
    { "id": "dragon_peak",          "depth": 4, "position": 0, "type": "Boss",   "encounterIndex": 7, "connectedTo": [] }
  ],
  "defaultHeroClassId": "knight",
  "heroClasses": [
    {
      "id": "knight",
      "name": "Knight",
      "description": "Balanced melee fighter. Reliable damage, solid defense, and a small self-heal.",
      "startingStats": { "maxHealth": 100, "maxMana": 20, "attack": 20, "defense": 15, "magic": 20 },
      "startingMoves": ["slash", "shield_up", "battle_cry", "second_wind"],
      "startingLearnedMoves": ["slash", "shield_up", "battle_cry", "second_wind", "power_stance", "iron_skin", "rend"]
    },
    {
      "id": "ranger",
      "name": "Ranger",
      "description": "Agile striker. Trades raw HP for higher Attack and bleeding follow-ups.",
      "startingStats": { "maxHealth": 90, "maxMana": 18, "attack": 24, "defense": 12, "magic": 8 },
      "startingMoves": ["slash", "rend", "iron_skin", "second_wind"],
      "startingLearnedMoves": ["slash", "rend", "iron_skin", "second_wind", "power_stance", "battle_cry"]
    },
    {
      "id": "mage",
      "name": "Mage",
      "description": "Burst caster. High Magic and Max Mana, low Defense. Lives or dies by spell timing.",
      "startingStats": { "maxHealth": 80, "maxMana": 35, "attack": 8, "defense": 10, "magic": 24 },
      "startingMoves": ["firebolt", "mana_drain", "arcane_focus", "hex_shield"],
      "startingLearnedMoves": ["firebolt", "mana_drain", "arcane_focus", "hex_shield", "arcane_surge", "second_wind"]
    },
    {
      "id": "cleric",
      "name": "Cleric",
      "description": "Durable support. Strong heals and defensive blessings, modest magic damage.",
      "startingStats": { "maxHealth": 110, "maxMana": 28, "attack": 10, "defense": 14, "magic": 18 },
      "startingMoves": ["blessed_mend", "smite", "shield_up", "iron_skin"],
      "startingLearnedMoves": ["blessed_mend", "smite", "shield_up", "iron_skin", "battle_cry", "second_wind"]
    }
  ],
  "encounters": [
    { "index": 0, "monsterId": "goblin_warrior",  "level": 1, "environmentId": "training_fields" },
    { "index": 1, "monsterId": "goblin_mage",     "level": 2, "environmentId": "arcane_library"  },
    { "index": 2, "monsterId": "giant_spider",    "level": 3, "environmentId": "spider_nest"     },
    { "index": 3, "monsterId": "skeleton_knight", "level": 4, "environmentId": "crypt"           },
    { "index": 4, "monsterId": "forest_troll",    "level": 5, "environmentId": "ancient_forest"  },
    { "index": 5, "monsterId": "witch",           "level": 5, "environmentId": "dark_altar"      },
    { "index": 6, "monsterId": "fire_elemental",  "level": 6, "environmentId": "ember_chamber"   },
    { "index": 7, "monsterId": "dragon",          "level": 7, "environmentId": "dragon_peak"     }
  ],
  "environments": [
    {
      "id": "training_fields",
      "name": "Training Fields",
      "description": "Open ground favors clean strikes. Physical hits land a little harder.",
      "physicalDamageBonus": 2
    },
    {
      "id": "dragon_peak",
      "name": "Dragon Peak",
      "description": "Thin mountain air pushes magic harder; healing is a touch less effective.",
      "magicDamageBonus": 3,
      "healingBonus": -3
    }
  ],
  "monsters": [
    {
      "id": "goblin_warrior",
      "name": "Goblin Warrior",
      "baseStats": {
        "maxHealth": 60,
        "maxMana": 10,
        "attack": 12,
        "defense": 8,
        "magic": 4
      },
      "moves": ["rusty_blade", "dirty_kick", "frenzy", "headbutt"],
      "itemDrops": ["rusty_shortsword", "tattered_leather"]
    },
    {
      "id": "witch",
      "name": "Witch",
      "baseStats": {
        "maxHealth": 80,
        "maxMana": 25,
        "attack": 8,
        "defense": 8,
        "magic": 18
      },
      "moves": ["shadow_bolt", "drain_life", "curse", "dark_pact", "bleeding_curse"],
      "itemDrops": ["hex_focus", "lifedrinker_locket"]
    }
  ],
  "shopOffers": [
    {
      "id": "shop_apprentice_wand",
      "name": "Apprentice Wand",
      "description": "+3 Magic. A simple wand for budding spellcasters.",
      "price": 60,
      "type": "Item",
      "itemId": "apprentice_wand"
    },
    {
      "id": "shop_upgrade_health",
      "name": "+10 Max Health",
      "description": "Permanent: raises Max Health by 10.",
      "price": 50,
      "type": "StatUpgrade",
      "stat": "maxHealth",
      "amount": 10
    }
  ],
  "items": [
    {
      "id": "rusty_shortsword",
      "name": "Rusty Shortsword",
      "description": "Crude but serviceable. +3 Attack.",
      "slot": "weapon",
      "rarity": "common",
      "statBonuses": [
        { "stat": "attack", "amount": 3 }
      ]
    },
    {
      "id": "lifedrinker_locket",
      "name": "Lifedrinker Locket",
      "description": "Steals a drop of vitality with every spell. +10 Max Health, +2 Magic.",
      "slot": "trinket",
      "rarity": "rare",
      "statBonuses": [
        { "stat": "maxHealth", "amount": 10 },
        { "stat": "magic", "amount": 2 }
      ]
    },
    {
      "id": "dragonbone_blade",
      "name": "Dragonbone Blade",
      "description": "Forged from a dragon's own bones. +8 Attack, +5 Max Health.",
      "slot": "weapon",
      "rarity": "epic",
      "statBonuses": [
        { "stat": "attack", "amount": 8 },
        { "stat": "maxHealth", "amount": 5 }
      ]
    }
  ],
  "moves": [
    {
      "id": "slash",
      "name": "Slash",
      "category": "Physical",
      "power": 20,
      "description": "A direct sword strike."
    },
    {
      "id": "shield_up",
      "name": "Shield Up",
      "category": "Buff",
      "power": 0,
      "manaCost": 2,
      "effect": {
        "kind": "BuffDefense",
        "amount": 5,
        "durationTurns": 2,
        "target": "Self"
      },
      "description": "Raises the Knight's Defense for 2 turns."
    },
    {
      "id": "rend",
      "name": "Rend",
      "category": "Physical",
      "power": 12,
      "manaCost": 2,
      "effect": {
        "kind": "Bleed",
        "amount": 4,
        "durationTurns": 2,
        "target": "Opponent"
      },
      "description": "A tearing strike that leaves the foe bleeding."
    },
    {
      "id": "second_wind",
      "name": "Second Wind",
      "category": "Heal",
      "power": 0,
      "effect": {
        "kind": "Heal",
        "amount": 25,
        "durationTurns": 0,
        "target": "Self"
      },
      "description": "Restores a portion of the Knight's HP."
    },
    {
      "id": "shadow_bolt",
      "name": "Shadow Bolt",
      "category": "Magic",
      "power": 20,
      "description": "A bolt of shadow energy."
    }
  ],
  "rules": {
    "buffDurationTurns": 2,
    "xpPerVictory": 25,
    "xpPerLevel": 100,
    "statGainPerLevel": {
      "maxHealth": 10,
      "maxMana": 0,
      "attack": 2,
      "defense": 2,
      "magic": 2
    },
    "equippedMoveSlots": 4,
    "equippedItemSlots": { "weapon": 1, "armor": 1, "trinket": 2 },
    "goldPerVictory": 15,
    "levelUpChoices": [
      { "id": "health",  "name": "+15 Max Health", "description": "Toughen up. Raises Max Health by 15.",     "stat": "health",  "amount": 15 },
      { "id": "attack",  "name": "+4 Attack",      "description": "Hit harder. Raises Attack by 4.",          "stat": "attack",  "amount": 4  },
      { "id": "defense", "name": "+4 Defense",     "description": "Brace yourself. Raises Defense by 4.",     "stat": "defense", "amount": 4  },
      { "id": "magic",   "name": "+4 Magic",       "description": "Sharpen your focus. Raises Magic by 4.",   "stat": "magic",   "amount": 4  },
      { "id": "mana",    "name": "+10 Max Mana",   "description": "Deepen your reserves. Raises Max Mana by 10.", "stat": "mana", "amount": 10 }
    ]
  }
}
```

**Notes**
- `heroClasses` carries the selectable archetypes shown by the client's class picker. The client lets the player pick one before starting a run; the chosen class's `startingStats`, `startingMoves`, and `startingLearnedMoves` seed the active hero. `defaultHeroClassId` identifies the class the picker highlights by default.
- The legacy `hero` field is preserved for backward compatibility and mirrors the class identified by `defaultHeroClassId`. Clients that have not been updated to read `heroClasses` continue to receive the original Knight starting state.
- `mapNodes` describes a small branching run map. The player begins on the node identified by `startingMapNodeId` and unlocks every node listed in the cleared node's `connectedTo` after winning that battle (or leaving a Shop). `Battle`, `Elite`, and `Boss` nodes set `encounterIndex` to a slot in `encounters`; `Shop` nodes use `encounterIndex: -1` and route the player to the existing shop screen. Defeating the unique `Boss` node ends the run successfully. The linear `encounters` list is still emitted, so older clients can continue to walk encounters sequentially.
- `endlessMode` carries Endless Mode rules and content pools. When `endlessMode.enabled` is `true` the client may offer an Endless Mode entry point (separate from the standard run). The server does not track endless progress; the client rolls each floor on demand from these inputs. Floor periods (`eliteEvery`, `shopEvery`, `bossEvery`) are resolved with a fixed precedence of **Boss > Elite > Shop > Battle**. Pools (`monsterPool`, `eliteMonsterPool`, `bossMonsterPool`, `environmentPool`) reference ids already present in `monsters` and `environments`, so Endless Mode reuses the existing battle pipeline and ships no new content. Defeat ends the endless run; the standard branching run is unaffected.
- The full `moves` catalog is returned once so the client never has to look up a move by id out-of-band.
- The example above shows a subset of monsters, moves, and environments for brevity. The real response includes every monster referenced by `encounters`, every move referenced by the hero or any monster, and every environment referenced by an encounter.
- `encounters` is ordered; the client advances through it one battle at a time.
- Each encounter carries an `environmentId` that points into `environments`. The Unity client looks the entry up and applies its modifiers during that battle. Environments are intentionally simple flat-integer modifiers (e.g. `physicalDamageBonus: 2`, `healingBonus: -3`) so they fold into the existing damage and status pipeline without any new math.
- Modifier fields default to `0` and are omitted from the JSON when zero.
- `rules` carries the tunable numbers so the client and server agree on formulas without hard-coding them on the client.
- `rules.levelUpChoices` lists the attribute increases the client offers the player on a level up. Each entry has a `stat` (`health`, `mana`, `attack`, `defense`, or `magic`) and a flat `amount`. The client presents the list during the post-battle flow and applies the picked entry to the hero. `rules.statGainPerLevel` is retained for backward compatibility with older clients that auto-apply a fixed stat gain; newer clients ignore it and drive level-ups from `levelUpChoices` instead.
- `items` is the full catalog of equippable gear referenced by `monsters[].itemDrops`. Each item has a `slot` (`weapon`, `armor`, or `trinket`), a `rarity` string (UI hint), and a `statBonuses` list of flat integer bonuses keyed by `stat` (`maxHealth`, `maxMana`, `attack`, `defense`, `magic`). The client tracks ownership and equipped items locally during the run; bonuses from currently equipped items are added to the hero's base stats during battle.
- `monsters[].itemDrops` lists the item ids a monster can drop on victory. The client decides how to pick from the list (the prototype simply rolls one entry uniformly at random and grants it to the player on first kill).
- `rules.equippedItemSlots` declares how many items can be worn per slot. The default is `{ "weapon": 1, "armor": 1, "trinket": 2 }`. The client enforces these caps in inventory UI; the server only declares the contract.
- `rules.goldPerVictory` is the flat amount of gold awarded for each battle victory (default `15`). Defeats grant nothing. Gold is tracked entirely on the client during a run and resets on a new run.
- `shopOffers` is the catalog of purchasable entries the client renders in the in-run shop. Each offer has a `price` in gold and a `type`:
  - `"Item"` offers grant the item with the matching `itemId` (which must exist in the `items` catalog). `stat` and `amount` are unused.
  - `"StatUpgrade"` offers permanently raise the hero's chosen `stat` (`maxHealth`, `maxMana`, `attack`, `defense`, or `magic`) by `amount`. `itemId` is unused.
  Purchases are resolved entirely on the client: gold is debited from the run total, and the offer's effect (item granted or stat raised) is applied to the local run state. The server is not informed.
- Fields with a `null` value (e.g. `effect`, `manaCost`, or `hpCost` on a free pure-damage move) are omitted from the JSON response.
- `manaCost` and `hpCost` on a `move` are optional resource costs paid by the caster. A move without either field is free. `maxMana = 0` on an entity simply means it has no mana resource and can only use moves with no `manaCost`.

---

## 2. GET `/battle/next-move`

**Purpose**
Given the current battle state, return the move the opponent monster will use on its next turn.

**When called**
By the client at the start of each enemy turn during a battle.

**Request**

Query parameters (preferred for a GET):

| Param              | Type   | Required | Description                                                                                                  |
|--------------------|--------|----------|--------------------------------------------------------------------------------------------------------------|
| `monsterId`        | string | yes      | Id from the run config (e.g. `goblin_warrior`).                                                              |
| `monsterLevel`     | int    | yes      | Level of the monster in the current encounter.                                                               |
| `monsterHealth`    | int    | yes      | Current HP.                                                                                                  |
| `monsterMaxHealth` | int    | yes      | Max HP.                                                                                                      |
| `heroHealth`       | int    | yes      | Current hero HP.                                                                                             |
| `heroMaxHealth`    | int    | yes      | Max hero HP.                                                                                                 |
| `turn`             | int    | yes      | Battle turn counter, starting at 1.                                                                          |
| `monsterMana`      | int    | no       | Current monster mana. If omitted, the server does not enforce mana costs at all (older clients keep working).|
| `monsterEffects`   | string | no       | Comma-separated list of `kind` values currently active on the monster (e.g. `BuffAttack,Bleed`).             |
| `heroEffects`      | string | no       | Comma-separated list of `kind` values currently active on the hero.                                          |

Example:
```
GET /battle/next-move?monsterId=witch&monsterLevel=4&monsterHealth=20&monsterMaxHealth=80&heroHealth=70&heroMaxHealth=100&turn=3&monsterMana=22&monsterEffects=BuffMagic&heroEffects=BuffDefense
```

**Response (200 OK)**

```json
{
  "moveId": "drain_life"
}
```

**Errors**
- `400 Bad Request`: unknown `monsterId`, missing required parameters, non-positive `monsterMaxHealth` or `heroMaxHealth`, or `turn < 1`.

**Notes**
- The server selects from the monster's declared `moves` in the run config. No move is returned that the monster does not own.
- The optional `monsterMana`, `monsterEffects`, and `heroEffects` parameters let the bot filter unaffordable moves and avoid re-applying buffs and debuffs already active on the relevant target. Older clients that omit them still get a valid (if shallower) selection.
- Selection logic is intentionally simple for the prototype (see `battle-rules.md`), but stays server-side so it can evolve without client changes.

---

## Client vs Server Responsibilities

**Server owns**
- The full run configuration: hero starting state, encounter order, monster and move catalogs, and rule constants.
- The opponent's move choice on each enemy turn.

**Client owns**
- All UI and navigation (Main Menu, Run Overview, Move Management, Battle, Post-Battle).
- Turn flow and input handling.
- Damage, healing, and buff resolution using the formulas and constants supplied by the server.
- Local tracking of hero XP, level, equipped moves, and learned move pool during a run. Hero HP is reset to full at the start of each battle and is not tracked between encounters.
- Advancing through the encounter list on victory, or replaying the same encounter on defeat. The run ends only when the final encounter is cleared.

The server is stateless between calls: `/run/config` returns a self-contained snapshot, and `/battle/next-move` is answered purely from the inputs in the request plus the server's static catalog.
