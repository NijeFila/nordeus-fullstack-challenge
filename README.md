# Nordeus Full Stack Challenge

A turn-based monster battle prototype built for the Nordeus Job Fair 2026 Full Stack Challenge. The project pairs a Unity client with an ASP.NET Core backend. The server owns run configuration and opponent move selection; the client handles presentation, input, and local resolution of the player's actions.

## Project Overview

- The player starts a run, picks a hero class, traverses a small branching map (or a generated Endless Mode floor sequence), and fights monsters in turn-based battles.
- Moves fall into five categories: Physical, Magic, Heal, Buff, Debuff. Status effects layered on top: Bleed, Poison, BuffAttack/Defense/Magic, DebuffAttack/Defense/Magic, DamageIncrease, DamageReduction, Heal.
- Winning awards XP, gold, and may grant a learned move or an item drop. Levelling up lets the player pick an attribute increase from a small set.
- Between battles the player can manage equipped moves, manage equipped items, visit a shop, save & exit, or pick the next map node.
- All player-facing strings are localized; the prototype ships with English and Serbian Latin.

## Tech Stack

- **Client:** Unity 2022.3 LTS (C#), TextMeshPro, UGUI, URP
- **Server:** ASP.NET Core 8 minimal APIs (.NET 8)
- **Transport:** HTTP / JSON
- **Persistence:** local JSON save file under `Application.persistentDataPath` (no database)
- **Tooling:** .NET SDK, Unity Editor, Git

## Folder Structure

```
nordeus-fullstack-challenge/
├── client-unity/NordeusChallenge.Client/   # Unity project
│   └── Assets/
│       ├── Scenes/        # MainMenu, ClassSelection, RunOverview, MoveManagement,
│       │                  # ItemManagement, Shop, Battle
│       ├── Scripts/
│       │   ├── Core/         # SceneNames
│       │   ├── Models/       # DTOs mirroring server contracts
│       │   ├── Networking/   # RunApiClient, BattleApiClient
│       │   ├── Runtime/      # GameSession, SaveGameService, reward resolution
│       │   ├── Localization/ # Loc, LocalizedNames, LocalizedText, LanguageSelector
│       │   ├── UI/           # MainMenu, ClassSelection, RunOverview, MoveManagement,
│       │   │                 # ItemManagement, Shop, Battle, Common
│       │   └── Visual/       # VisualCatalog ScriptableObject
│       └── Resources/Localization/   # en.json, sr-Latn.json
├── server/NordeusChallenge.Api/
│   ├── Endpoints/         # RunEndpoints, BattleEndpoints
│   ├── Services/          # RunConfigService, BattleService
│   ├── Models/            # Request/response DTOs
│   └── Program.cs
├── docs/                  # Design notes, API contract, submission notes, checklists
└── README.md
```

## How to Run the Backend

Prerequisites: .NET SDK 8.0+

```
cd server/NordeusChallenge.Api
dotnet run
```

The server listens on `http://localhost:5046` by default. Key endpoints:

- `GET /run/config` — returns the full run configuration: hero, monsters, moves, encounters, environments, items, shop offers, rules, hero classes, branching map nodes, and Endless Mode config.
- `GET /battle/next-move` — returns the monster's next move id given current battle state. Optional query parameters cover monster mana and active effects on both sides.

See `docs/api-contract.md` for the complete contract and example payloads.

## How to Run the Unity Client

Prerequisites: Unity 2022.3 LTS or newer (URP).

1. Open `client-unity/NordeusChallenge.Client` in the Unity Editor.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Make sure the backend is running.
4. The `baseUrl` field on `MainMenuController` and `BattleController` defaults to `http://localhost:5046`. Adjust if needed.
5. Press **Play**.

## Standard Run Flow

1. **Main Menu** — choose a language, then click **Standard Run**. The client calls `GET /run/config`.
2. **Class Selection** — pick one of four hero classes (Knight, Ranger, Mage, Cleric). The chosen class seeds the active hero (stats, equipped moves, learned move pool).
3. **Run Overview** — the branching run map is rendered. Available nodes are clickable; cleared nodes are flagged.
4. **Battle / Elite / Boss nodes** — the Battle scene opens against the encounter referenced by the node. On victory the client awards XP, gold, possible item drop, possible move learn, and offers an attribute pick when the hero levels up. On defeat the same node is replayed (no XP / gold loss).
5. **Shop nodes** — the existing shop screen opens. Items and stat upgrades cost gold; leaving the shop unlocks the connected nodes.
6. **Boss node (Dragon)** — defeating the boss completes the run. The map shows the run-victory banner.

The Run Overview also exposes Move Management, Item Management, the Shop, and a **Save & Exit** button.

## Endless Mode Flow

1. **Main Menu** — click **Endless Run**. The client calls `GET /run/config` and uses the returned `endlessMode` block.
2. **Class Selection** — same picker as the standard run.
3. **Run Overview (Endless)** — instead of a branching map, the player sees the current floor number, the upcoming enemy or shop, and a **Start Floor** button. Floors are generated from the server's pools and period rules: every Nth floor is Elite, Shop, or Boss; the rest are normal Battles.
4. **Battles** — same battle pipeline as standard. Victory advances the floor; XP and gold scale linearly with floor number.
5. **Shop floors** — visit the standard shop scene; pressing Back advances to the next floor.
6. **Defeat** — ends the endless run. The summary shows the floor reached. The player can return to the Main Menu.

## Key Implemented Features

- Core: run config, encounter list, turn-based battle with five move categories, server-driven opponent moves, XP and level-up flow, learned-move auto-equip, move management.
- Combat depth: status effects (Bleed, Poison, DamageIncrease, DamageReduction, Buff/Debuff for Attack/Defense/Magic), Heal moves, mana and HP costs, battle log, hit/heal feedback animations.
- Smarter bot: heal-when-low, finishing-blow rule, redundant-effect skip, mild damage bias, affordability filter (mana and HP costs).
- Progression and economy: attribute choice on level-up, items with stat bonuses, item management screen, shop with item and stat-upgrade offers, gold rewards.
- Content: 8 monsters, 30+ moves, 8 environments, 11 items, 8 shop offers, 5 level-up choices, 4 hero classes (Knight, Ranger, Mage, Cleric).
- Map: small Slay-the-Spire-style branching graph with Battle / Elite / Shop / Boss node types.
- Endless Mode: server-defined pools and curves, client-side floor generation, period-based floor types, linear reward curves.
- Save & Exit: local JSON save with Continue Run on Main Menu; restores hero state, gold, inventory, equipped items, and map / endless progression.
- Localization: custom JSON-backed system with English and Serbian Latin tables; covers UI labels, dynamic monster / move / item / environment / shop / class names and descriptions; falls back to English then to the server-provided string.
- Environmental effects: small flat integer modifiers attached to encounters (physical / magic damage bonus, healing bonus, end-of-turn damage, poison duration, bleed bonus, mana regen).

## Architecture Summary

- **Server is authoritative for two concerns only**: run configuration and opponent move selection. Stateless between requests; exposes a small minimal-API surface.
- **Client owns** scene flow, UI state, and local resolution of the player's chosen move against shared rules. A `GameSession` MonoBehaviour singleton holds the active run, hero, items, gold, map progression, endless state, and run mode.
- **DTOs** on the client mirror the server's response shapes (Unity `JsonUtility`-friendly), so the same data model is used end-to-end.
- **Networking** is isolated in small API client classes (`RunApiClient`, `BattleApiClient`) so controllers stay focused on presentation and flow.
- **Save / load** writes a single JSON file under `Application.persistentDataPath`. The save stores only mutable run state; the run config is refetched from the server on Continue.
- **Localization** is a static `Loc` API backed by JSON tables under `Resources/Localization/`. Dynamic data names route through `LocalizedNames` helpers using `<category>.<id>.{name,description}` keys with the server's English text as the final fallback.

## Known Limitations

- No automated tests on either side. The scope favored a working end-to-end slice across the bonus backlog.
- The server is not authoritative for player actions — only for run config and monster moves. A dishonest client could misreport outcomes.
- One save slot. No cloud save, no encryption, no autosave.
- Monster AI is a small layered heuristic; it does not adapt across a run or across players.
- The branching map is a single hand-authored graph; it is not procedural.
- Endless Mode is a server-shipped configuration plus client-side floor generation; difficulty scaling is the linear formulas in `endlessMode` plus encounter-level scaling. There is no late-game tuning beyond the optional basis-point multipliers.
- The class picker, map UI, and endless panel are functional UGUI layouts; no animated transitions.
- `baseUrl` lives on individual controllers with a sensible default rather than in a central config asset.

See `docs/submission-notes.md` for the full per-feature breakdown, `docs/feature-checklist.md` for the requirement-by-requirement status, and `docs/testing-checklist.md` for the manual test plan.
