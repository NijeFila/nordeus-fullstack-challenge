# Architecture

## Overview

The project is split into two deployables that communicate over HTTP/JSON:

- **Unity client** (`/client-unity`) — presentation, input, local battle simulation, and progression UI.
- **ASP.NET Core server** (`/server`) — authoritative source for run configuration and opponent move selection.

```
┌──────────────────────┐         HTTP/JSON         ┌──────────────────────┐
│    Unity Client      │ ───── GET run config ───▶ │   ASP.NET Core API   │
│  (Menu, Run, Battle) │ ◀──── run config ──────── │                      │
│                      │ ───── GET next move ────▶ │  Config + AI logic   │
│                      │ ◀──── move choice ─────── │                      │
└──────────────────────┘                           └──────────────────────┘
```

## Unity Client

Responsibilities:

- Rendering the Main Menu, Run Overview, Move Management, Battle Screen, and Post-Battle flow.
- Capturing player input and driving turn order.
- Applying damage calculations based on shared stat rules.
- Tracking experience, level-ups, and newly learned moves within a run.
- Requesting run configuration at the start of a run and the opponent's move each enemy turn.

The client treats the server's configuration as read-only data it consumes at the start of a run, which keeps game data out of the Unity project and makes balance changes a server-side concern.

## ASP.NET Core Server

Responsibilities:

- Exposing a `GET` endpoint that returns the full run configuration the client needs to start a run.
- Exposing a `GET` endpoint that accepts the current battle state and returns the opponent's next move.
- Holding the monster, stat, and move definitions in one place.

The server is stateless in the simple case: each request is handled using the configuration it owns, without retaining per-player state between calls. This keeps the surface area small and the contract explicit.

## Why These Responsibilities Live Server-Side

- **Run configuration** sits on the server so that the data shape and values can evolve without rebuilding the Unity client, and so the client is never the authority on what content exists.
- **Opponent move selection** sits on the server so that opponent behavior is centralized, testable in isolation, and can later grow (weighting, difficulty scaling) without touching the client.

Damage resolution and turn flow stay on the client because they are driven by input timing and animation. The rules themselves come from the server's configuration, so both sides agree on the math.

## Design Principles

- **Clear separation of concerns.** The client presents and simulates; the server owns configuration and opponent decisions. Neither crosses the other's boundary.
- **Data-driven configuration.** Monsters, stats, and moves are defined as data served by the API rather than hard-coded in the client.
- **Simple UI first.** Screens are built to be functional and readable before any polish. Visual work is the last phase, not the first.
- **Incremental delivery.** The plan is phased so that each step produces something runnable. The core battle loop is prioritized over breadth of features.
- **Readability over cleverness.** Straightforward code, explicit contracts, and small files are preferred over abstractions that only pay off at larger scale.
