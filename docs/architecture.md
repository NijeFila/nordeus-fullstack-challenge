# Architecture

## Overview

The project is split into two deployables that talk over HTTP and JSON:

- **Unity client** (`/client-unity`) handles the screens, input, local battle simulation, and progression UI.
- **ASP.NET Core server** (`/server`) is the source of truth for run configuration and the opponent's move selection.

```
┌──────────────────────┐         HTTP/JSON         ┌──────────────────────┐
│    Unity Client      │ ───── GET run config ───▶ │   ASP.NET Core API   │
│  (Menu, Run, Battle) │ ◀──── run config ──────── │                      │
│                      │ ───── GET next move ────▶ │  Config + bot logic  │
│                      │ ◀──── move choice ─────── │                      │
└──────────────────────┘                           └──────────────────────┘
```

## Unity Client

What it owns:

- Rendering the Main Menu, Class Selection, Run Overview, Move Management, Item Management, Shop, Battle, and the Endless panel.
- Capturing player input and driving turn order.
- Applying damage calculations using the shared stat rules.
- Tracking experience, level-ups, gold, items, and learned moves within a run.
- Asking the server for run configuration when a run starts and for the opponent's move on each enemy turn.

The client treats the server's configuration as read-only data it consumes at the start of a run. That keeps gameplay data out of the Unity project and makes balance changes a server-side concern.

## ASP.NET Core Server

What it owns:

- A `GET` endpoint that returns the full run configuration the client needs to start a run.
- A `GET` endpoint that accepts the current battle state and returns the opponent's next move.
- The monster, stat, move, environment, item, shop, hero-class, map, and endless catalog in one place.

The server is stateless in the simple case: each request is handled using the configuration it owns, without retaining per-player state between calls. That keeps the surface area small and the contract explicit.

## Why These Responsibilities Live Server-Side

- **Run configuration** sits on the server so the data shape and values can evolve without rebuilding the Unity client. The client is never the authority on what content exists.
- **Opponent move selection** sits on the server so opponent behavior is centralised, easy to reason about in isolation, and can grow later (weighting, difficulty bands) without touching the client.

Damage resolution and turn flow stay on the client because they're driven by input timing and animation. The rules themselves come from the server's configuration, so both sides agree on the math.

## Design Principles

- **Clear separation of concerns.** The client presents and simulates; the server owns configuration and opponent decisions. Neither crosses the other's boundary.
- **Data-driven configuration.** Monsters, stats, and moves are defined as data served by the API, not hard-coded in the client.
- **Simple UI first.** Screens are built to be functional and readable before any polish. Visual work is the last phase.
- **Incremental delivery.** The plan is phased so each step produces something runnable. The core battle loop is prioritised over breadth of features.
- **Readable code.** Straightforward code, explicit contracts, and small files are preferred over abstractions that only pay off at larger scale.
