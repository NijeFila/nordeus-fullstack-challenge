# Challenge Summary

A turn-based monster battle game delivered as a Unity client backed by an ASP.NET Core server. The player progresses through a sequence of battles, manages a roster of monsters with distinct stats and moves, and the server drives run configuration and opponent behavior.

## Required Core

### Client

- **Main Menu.** Entry point to start a new run.
- **Run Overview.** Shows the player's monsters, current progress, and the path forward.
- **Move Management.** Lets the player inspect and organise the moves known by their monsters.
- **Battle Screen.** Turn-based combat between a player monster and an opponent monster.
- **Post-Battle Flow.** Presents the outcome of a battle and any rewards or progression steps.
- **Progression.** Monsters gain experience, level up, and learn new moves across a run.

### Server

- A `GET` endpoint that returns the **run configuration** (monsters, base stats, moves, and any data needed to start a run).
- A `GET` endpoint that returns the **opponent monster's next move** given the current battle state.

### Game Systems

- Stats: **Health**, **Attack**, **Defense**, **Magic**.
- Moves of two categories: **physical** (scaled by Attack vs. Defense) and **magic** (scaled by Magic).
- Damage calculation grounded in the stats above.
- Levelling that improves stats and unlocks new learned moves.
- Each monster has its own set of learned moves.

## Backend Responsibilities

The server is the single source of truth for:

1. **Run configuration:** what monsters exist, their base data, available moves, and the shape of the run.
2. **Opponent move selection:** which move the enemy uses on its turn.

The client is responsible for showing the game, handling input, resolving damage against shared rules, and animating the battle.

## Optional Bonus Ideas

These were possibilities, not commitments. I attempted them once the core loop was solid, and most made it into the final submission. See `docs/feature-checklist.md` for the per-item status.

- Additional server-side validation of battle outcomes.
- Persistence of runs between sessions.
- Richer opponent logic (for example weighted move selection based on matchup).
- Audio, visual effects, or improved animations.
- Expanded move set or type system.

## Success Criteria

- All required client screens are present and navigable.
- Both server endpoints are implemented, documented, and consumed by the client.
- Battle rules behave consistently with the stat definitions.
- Levelling and learned moves affect later battles.
- Code is organised, readable, and straightforward to run locally.
- Documentation is enough for a reviewer to understand the design and run the project without guesswork.
