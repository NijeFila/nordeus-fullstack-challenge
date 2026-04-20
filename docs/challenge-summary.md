# Challenge Summary

A turn-based monster battle game delivered as a Unity client backed by an ASP.NET Core server. The player progresses through a sequence of battles, managing a roster of monsters with distinct stats and moves, while the server drives run configuration and opponent behavior.

## Required Core

### Client

- **Main Menu** — entry point to start a new run.
- **Run Overview** — shows the player's monsters, current progress, and the path forward.
- **Move Management** — lets the player inspect and organize the moves known by their monsters.
- **Battle Screen** — turn-based combat between a player monster and an opponent monster.
- **Post-Battle Flow** — presents the outcome of a battle and any rewards or progression steps.
- **Progression** — monsters gain experience, level up, and learn new moves across a run.

### Server

- `GET` endpoint that returns the **run configuration** (monsters, base stats, moves, and any data needed to start a run).
- `GET` endpoint that returns the **opponent monster's next move** given the current battle state.

### Game Systems

- Stats: **Health**, **Attack**, **Defense**, **Magic**.
- Moves of two categories: **physical** (scaled by Attack vs. Defense) and **magic** (scaled by Magic).
- Damage calculation grounded in the above stats.
- Leveling that improves stats and unlocks new learned moves.
- Each monster has its own set of learned moves.

## Backend Responsibilities

The server is the single source of truth for:

1. **Run configuration** — what monsters exist, their base data, available moves, and how the run is shaped.
2. **Opponent move selection** — deciding which move the enemy uses on its turn.

The client is responsible for presenting the game, handling input, resolving damage against shared rules, and animating the battle.

## Optional Bonus Ideas

These are held as possibilities, not commitments. They will only be attempted once the core loop is solid.

- Additional server-side validation of battle outcomes.
- Persistence of runs between sessions.
- Richer opponent AI (e.g., weighted move selection based on matchup).
- Audio, visual effects, or improved animations.
- Expanded move set or type system.

## Success Criteria

- All required client screens are present and navigable.
- Both server endpoints are implemented, documented, and consumed by the client.
- Battle rules behave consistently with the stat definitions.
- Leveling and learned moves affect subsequent battles.
- Code is organized, readable, and straightforward to run locally.
- Documentation is sufficient for a reviewer to understand the design and run the project without guesswork.
