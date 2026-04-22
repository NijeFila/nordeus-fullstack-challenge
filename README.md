# Nordeus Full Stack Challenge

A turn-based monster battle prototype built for the Nordeus Job Fair 2026 Full Stack Challenge. The project pairs a Unity client with an ASP.NET Core backend. The server owns run configuration and opponent move selection; the client handles presentation, input, and local resolution of the player's actions.

## Project Overview

- Player starts a run, picks an encounter on a linear map, and fights a monster in a turn-based battle.
- Moves fall into five categories: Physical, Magic, Heal, Buff, Debuff.
- Winning awards XP, unlocks the next encounter, and can grant a new learned move.
- Between battles the player can re-equip moves from the learned pool.

## Tech Stack

- **Client:** Unity (C#), TextMeshPro, UGUI
- **Server:** ASP.NET Core minimal APIs (.NET)
- **Transport:** HTTP/JSON
- **Tooling:** .NET SDK, Unity Editor, Git

## Folder Structure

```
nordeus-fullstack-challenge/
├── client-unity/
│   └── NordeusChallenge.Client/    # Unity project
│       └── Assets/
│           ├── Scenes/             # MainMenu, RunOverview, MoveManagement, Battle
│           ├── Scripts/
│           │   ├── Core/           # SceneNames
│           │   ├── Models/         # DTOs mirroring server contracts
│           │   ├── Networking/     # RunApiClient, BattleApiClient
│           │   ├── Runtime/        # GameSession, reward resolution
│           │   └── UI/             # MainMenu, RunOverview, MoveManagement, Battle
│           ├── Prefabs/
│           └── UI/ , Art/ , Settings/
├── server/
│   └── NordeusChallenge.Api/
│       ├── Endpoints/              # RunEndpoints, BattleEndpoints
│       ├── Services/               # RunConfigService, BattleService
│       ├── Models/                 # Request/response DTOs
│       └── Program.cs
├── docs/                           # Design notes, API contract, submission notes
└── README.md
```

## Setup

### Prerequisites

- .NET SDK 8.0+
- Unity 2022.3 LTS or newer (URP)
- Git

### Server

```
cd server/NordeusChallenge.Api
dotnet run
```

The server listens on `http://localhost:5046` by default. Key endpoints:

- `GET /run/config` — returns the run configuration (hero, monsters, moves, encounters, rules).
- `GET /battle/next-move?monsterId=...&level=...&monsterHp=...&monsterMaxHp=...&heroHp=...&heroMaxHp=...&turn=...` — returns the monster's next move id.

See `docs/api-contract.md` for the full contract.

### Unity Client

1. Open `client-unity/NordeusChallenge.Client` with Unity.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Ensure the server is running.
4. The `baseUrl` field on the `MainMenuController` and `BattleController` defaults to `http://localhost:5046`. Adjust if needed.
5. Press Play.

## Features

- Main menu that starts a run by fetching run configuration from the server.
- Run overview with a linear list of encounters and progressive unlocks.
- Turn-based battle with Physical / Magic / Heal / Buff / Debuff categories, active timed effects, and a running combat log.
- Server-driven monster AI via `/battle/next-move`.
- Post-battle progression: XP, level-up with stat gain, learning new moves, auto-equip into free slots.
- Move management screen for re-equipping moves from the learned pool.
- Move info panel in battle and move management that shows name, category, power (when relevant), and description.

## Architecture Summary

- **Server is authoritative for two concerns only:** run configuration and opponent move selection. It is stateless between requests and exposes a small minimal-API surface.
- **Client owns** scene flow, UI state, and local resolution of the player's chosen move against shared rules. A `GameSession` singleton holds the active run, hero, and lookup tables for moves, monsters, and encounters.
- **DTOs** on the client mirror the server's response shapes so the same data model is used end-to-end.
- **Networking** is isolated in small API client classes (`RunApiClient`, `BattleApiClient`) so controllers stay focused on presentation and flow.

## Prototype Simplifications / Tradeoffs

- Run state (XP, learned moves, equipped moves) lives only in memory on the client; closing the app ends the run.
- The server does not validate or persist the player's actions; it only serves config and picks monster moves.
- A single hero and a fixed, linear encounter list — no branching or procedural content.
- Battle resolution (damage, heals, buff/debuff stacking) runs on the client against rules defined on the server side; there is no rollback or anti-cheat.
- Monster AI is deterministic given its inputs; no difficulty scaling beyond encounter level.
- UI is functional rather than polished — TMP text blocks and standard UGUI buttons, no animations or VFX.
- No automated tests; the scope and time budget favored a working end-to-end slice over coverage.

See `docs/submission-notes.md` for what was implemented against the brief and what I would pick up next.
