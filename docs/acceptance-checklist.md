# Acceptance Checklist

A checklist that maps directly to the challenge requirements.

## Client

- [x] Main Menu is present and lets the player start a new run (Standard, Endless, or Continue).
- [x] Run Overview shows the player's monster (the active hero), the current run state, and the next available encounters or floors.
- [x] Move Management screen lets the player inspect and re-equip moves from the learned pool.
- [x] Battle Screen renders both combatants, their HP and mana, and the available moves with descriptions.
- [x] The player can select a move and see the result applied (damage, heal, buff, debuff, status effect).
- [x] Post-Battle flow shows outcome, awards XP and gold, surfaces level-ups and item drops, and returns the player to the run.
- [x] Navigation between screens works without dead ends.

## Server

- [x] `GET /run/config` is implemented, reachable, and returns the full run payload.
- [x] `GET /battle/next-move` is implemented, reachable, and returns the opponent's move id.
- [x] Response payloads are documented with examples in `docs/api-contract.md`.
- [x] Endpoints return sensible responses for missing or unknown input (for example unknown `monsterId` returns 200 with an empty body, the client falls back gracefully).
- [x] Server runs locally with a single command: `dotnet run` from `server/NordeusChallenge.Api`.

## Game Systems

- [x] Stats include Health (`maxHealth`), Attack, Defense, and Magic. Mana (`maxMana`) is added so resource costs are meaningful.
- [x] Physical moves use Attack vs. Defense in damage calculation.
- [x] Magic moves use Magic in damage calculation.
- [x] Each monster has its own set of learned moves.
- [x] The hero gains experience from battles.
- [x] Levelling up improves stats through an attribute pick from `rules.levelUpChoices`.
- [x] New moves are learned through progression and auto-equip into a free slot.

## Submission Polish

- [x] The project builds and runs from a fresh clone with the documented steps.
- [x] `README.md` describes the project, stack, and setup.
- [x] `docs/` contains the challenge summary, architecture, implementation plan, API contract, data model, battle rules, feature checklist, testing checklist, submission notes, and this file.
- [x] Code is organised by responsibility (Endpoints, Services, Models on the server; Models, Networking, Runtime, UI, Localization, Visual on the client).
- [x] The repository is free of unneeded build artifacts and editor files (Unity `Library/`, `Temp/`, `bin/`, `obj/`, `.vs/`, IDE caches are all ignored).

## Optional Bonus

- [x] Smarter opponent move selection (`BattleService` does affordability filtering, heal-when-low, finishing-blow, redundant-effect skip, mild damage bias).
- [x] Audio or visual feedback on actions (HP bars, floating combat text, hit and heal flashes, battle log).
- [x] Expanded move and content set (8 monsters, 30+ moves, 8 environments, 11 items, 8 shop offers, 4 hero classes).
- [x] Run persistence between sessions (local JSON save under `Application.persistentDataPath`, with **Continue Run** on the Main Menu).
- [ ] Server-side validation of battle outcomes. Listed under "What I would improve next" in `docs/submission-notes.md`. The server currently owns run config and opponent move selection only.
