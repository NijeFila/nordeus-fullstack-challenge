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
      "attack": 20,
      "defense": 15,
      "magic": 20
    },
    "equippedMoves": ["sword_strike", "shield_bash", "holy_light", "guard_stance"],
    "learnedMovePool": ["sword_strike", "shield_bash", "holy_light", "guard_stance"]
  },
  "encounters": [
    { "index": 0, "monsterId": "goblin_warrior", "level": 1 },
    { "index": 1, "monsterId": "goblin_mage",    "level": 2 },
    { "index": 2, "monsterId": "giant_spider",   "level": 3 },
    { "index": 3, "monsterId": "witch",          "level": 4 },
    { "index": 4, "monsterId": "dragon",         "level": 5 }
  ],
  "monsters": [
    {
      "id": "goblin_warrior",
      "name": "Goblin Warrior",
      "baseStats": {
        "maxHealth": 60,
        "attack": 12,
        "defense": 8,
        "magic": 4
      },
      "moves": ["rusty_slash", "reckless_charge"]
    }
  ],
  "moves": [
    {
      "id": "sword_strike",
      "name": "Sword Strike",
      "category": "Physical",
      "power": 20,
      "effect": null,
      "description": "A clean sword hit."
    },
    {
      "id": "holy_light",
      "name": "Holy Light",
      "category": "Magic",
      "power": 22,
      "effect": null,
      "description": "A burst of radiant energy."
    },
    {
      "id": "guard_stance",
      "name": "Guard Stance",
      "category": "Buff",
      "power": 0,
      "effect": {
        "kind": "BuffDefense",
        "amount": 5,
        "durationTurns": 2,
        "target": "Self"
      },
      "description": "Raises Defense for 2 turns."
    }
  ],
  "rules": {
    "buffDurationTurns": 2,
    "xpPerVictory": 25,
    "xpPerLevel": 100,
    "statGainPerLevel": {
      "maxHealth": 10,
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
- `encounters` is ordered; the client advances through it one battle at a time.
- `rules` carries the tunable numbers so the client and server agree on formulas without hard-coding them on the client.

---

## 2. GET `/battle/next-move`

**Purpose**
Given the current battle state, return the move the opponent monster will use on its next turn.

**When called**
By the client at the start of each enemy turn during a battle.

**Request**

Query parameters (preferred for a GET):

| Param              | Type   | Description                                            |
|--------------------|--------|--------------------------------------------------------|
| `monsterId`        | string | Id from the run config (e.g. `goblin_warrior`).        |
| `monsterLevel`     | int    | Level of the monster in the current encounter.         |
| `monsterHealth`    | int    | Current HP.                                            |
| `monsterMaxHealth` | int    | Max HP.                                                |
| `heroHealth`       | int    | Current hero HP.                                       |
| `heroMaxHealth`    | int    | Max hero HP.                                           |
| `turn`             | int    | Battle turn counter, starting at 1.                    |

Example:
```
GET /battle/next-move?monsterId=goblin_warrior&monsterLevel=1&monsterHealth=30&monsterMaxHealth=60&heroHealth=80&heroMaxHealth=100&turn=3
```

**Response (200 OK)**

```json
{
  "moveId": "rusty_slash"
}
```

**Errors**
- `400 Bad Request` — unknown `monsterId` or missing required parameters.

**Notes**
- The server selects from the monster's declared `moves` in the run config. No move is returned that the monster does not own.
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
