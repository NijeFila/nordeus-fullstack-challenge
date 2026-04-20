# Nordeus Full Stack Challenge

A turn-based monster battle prototype built for the Nordeus Job Fair 2026 Full Stack Challenge. The project pairs a Unity client with an ASP.NET Core backend, with the server owning run configuration and the opponent's move selection.

## Tech Stack

- **Client:** Unity (C#)
- **Server:** ASP.NET Core (C#)
- **Transport:** HTTP/JSON
- **Tooling:** .NET SDK, Unity Editor, Git

## High-Level Architecture

The client drives presentation, input, and local battle simulation. The server is authoritative for two concerns:

1. Providing the run configuration at the start of a run.
2. Selecting the opponent monster's next move during a battle.

Everything else (animations, UI state, turn resolution for the player's chosen actions) runs on the client against rules shared with the server's configuration.

## Planned Folder Structure

```
nordeus-fullstack-challenge/
├── client-unity/        # Unity project (Main Menu, Run, Battle, Progression)
├── server/              # ASP.NET Core Web API (run config + move selection)
├── docs/                # Challenge summary, architecture, plan, checklist
├── .editorconfig
├── .gitignore
└── README.md
```

## Setup

### Server

_To be added once the project is scaffolded._

### Client

_To be added once the Unity project is scaffolded._

## Notes

This repository is being built as a submission for the Nordeus Full Stack Challenge. See `docs/` for the challenge summary, architecture, implementation plan, and acceptance checklist.
