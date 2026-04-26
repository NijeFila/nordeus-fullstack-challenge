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
  "encounters": [
    { "index": 0, "monsterId": "goblin_warrior", "level": 1, "environmentId": "training_fields" },
    { "index": 1, "monsterId": "goblin_mage",    "level": 2, "environmentId": "arcane_library"  },
    { "index": 2, "monsterId": "giant_spider",   "level": 3, "environmentId": "spider_nest"     },
    { "index": 3, "monsterId": "witch",          "level": 4, "environmentId": "dark_altar"      },
    { "index": 4, "monsterId": "dragon",         "level": 5, "environmentId": "dragon_peak"     }
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
      "moves": ["rusty_blade", "dirty_kick", "frenzy", "headbutt"]
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
      "moves": ["shadow_bolt", "drain_life", "curse", "dark_pact", "bleeding_curse"]
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
    "equippedMoveSlots": 4
  }
}
```

**Notes**
- The full `moves` catalog is returned once so the client never has to look up a move by id out-of-band.
- The example above shows a subset of monsters, moves, and environments for brevity. The real response includes every monster referenced by `encounters`, every move referenced by the hero or any monster, and every environment referenced by an encounter.
- `encounters` is ordered; the client advances through it one battle at a time.
- Each encounter carries an `environmentId` that points into `environments`. The Unity client looks the entry up and applies its modifiers during that battle. Environments are intentionally simple flat-integer modifiers (e.g. `physicalDamageBonus: 2`, `healingBonus: -3`) so they fold into the existing damage and status pipeline without any new math.
- Modifier fields default to `0` and are omitted from the JSON when zero.
- `rules` carries the tunable numbers so the client and server agree on formulas without hard-coding them on the client.
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
- `400 Bad Request` — unknown `monsterId`, missing required parameters, non-positive `monsterMaxHealth` or `heroMaxHealth`, or `turn < 1`.

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
