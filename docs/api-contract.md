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
    "id": "hero",
    "name": "Hero",
    "level": 1,
    "xp": 0,
    "stats": {
      "maxHealth": 100,
      "attack": 20,
      "defense": 15,
      "magic": 20
    },
    "equippedMoves": ["tackle", "ember", "guard", "spark"],
    "learnedMovePool": ["tackle", "ember", "guard", "spark"]
  },
  "encounters": [
    { "index": 0, "monsterId": "slime",    "level": 1 },
    { "index": 1, "monsterId": "wolf",     "level": 2 },
    { "index": 2, "monsterId": "golem",    "level": 3 },
    { "index": 3, "monsterId": "wraith",   "level": 4 },
    { "index": 4, "monsterId": "dragon",   "level": 5 }
  ],
  "monsters": [
    {
      "id": "slime",
      "name": "Slime",
      "baseStats": {
        "maxHealth": 60,
        "attack": 10,
        "defense": 8,
        "magic": 6
      },
      "moves": ["tackle", "slam"]
    }
  ],
  "moves": [
    {
      "id": "tackle",
      "name": "Tackle",
      "category": "Physical",
      "power": 20,
      "effect": null,
      "description": "A straightforward physical hit."
    },
    {
      "id": "ember",
      "name": "Ember",
      "category": "Magic",
      "power": 22,
      "effect": null,
      "description": "A small burst of magical fire."
    },
    {
      "id": "guard",
      "name": "Guard",
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
| `monsterId`        | string | Id from the run config (e.g. `wolf`).                  |
| `monsterLevel`     | int    | Level of the monster in the current encounter.         |
| `monsterHealth`    | int    | Current HP.                                            |
| `monsterMaxHealth` | int    | Max HP.                                                |
| `heroHealth`       | int    | Current hero HP.                                       |
| `heroMaxHealth`    | int    | Max hero HP.                                           |
| `turn`             | int    | Battle turn counter, starting at 1.                    |

Example:
```
GET /battle/next-move?monsterId=wolf&monsterLevel=2&monsterHealth=30&monsterMaxHealth=70&heroHealth=80&heroMaxHealth=100&turn=3
```

**Response (200 OK)**

```json
{
  "moveId": "bite"
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
- Local tracking of hero HP, XP, level, equipped moves, and learned move pool during a run.
- Advancing through the encounter list and ending the run on hero defeat or final victory.

The server is stateless between calls: `/run/config` returns a self-contained snapshot, and `/battle/next-move` is answered purely from the inputs in the request plus the server's static catalog.
