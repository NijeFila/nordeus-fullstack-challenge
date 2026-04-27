# Nordeus Full Stack Challenge

A turn-based monster battle prototype I built for the Nordeus Job Fair 2026 Full Stack Challenge. The project pairs a Unity client with an ASP.NET Core backend. The server owns the run configuration and picks the opponent's moves; the client handles the screens, input, and the player's side of combat.

## Project Overview

The player starts a run, picks a hero class, walks through a small branching map (or generated floors in Endless Mode), and fights monsters in turn-based battles. Moves come in five categories: Physical, Magic, Heal, Buff, and Debuff. On top of that there are status effects like Bleed, Poison, BuffAttack/Defense/Magic and their debuff counterparts, plus DamageIncrease and DamageReduction.

Winning a battle gives XP and gold, can grant a learned move, and may drop an item. Levelling up lets the player pick which stat to boost from a small set of options. Between battles the player can manage equipped moves, manage equipped items, visit the shop, save and exit, or pick the next map node. Everything that is shown on screen is localized; the prototype ships with English and Serbian Latin.

## Tech Stack

- **Client:** Unity 2022.3 LTS (C#), TextMeshPro, UGUI, URP
- **Server:** ASP.NET Core 8 minimal APIs (.NET 8)
- **Transport:** HTTP with JSON
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
│       │   ├── Models/       # DTOs that mirror the server contracts
│       │   ├── Networking/   # RunApiClient, BattleApiClient
│       │   ├── Runtime/      # GameSession, SaveGameService, reward resolution
│       │   ├── Localization/ # Loc, LocalizedNames, LocalizedText, LanguageSelector
│       │   ├── UI/           # MainMenu, ClassSelection, RunOverview, MoveManagement,
│       │   │                 # ItemManagement, Shop, Battle, Common, Settings
│       │   └── Visual/       # VisualCatalog ScriptableObject
│       └── Resources/Localization/   # en.json, sr-Latn.json
├── server/NordeusChallenge.Api/
│   ├── Endpoints/         # RunEndpoints, BattleEndpoints
│   ├── Services/          # RunConfigService, BattleService
│   ├── Models/            # request and response DTOs
│   └── Program.cs
├── docs/                  # design notes, API contract, submission notes, checklists
└── README.md
```

## How to Run the Backend

You will need .NET SDK 8.0 or newer.

```
cd server/NordeusChallenge.Api
dotnet run
```

The server listens on `http://localhost:5046` by default. The two endpoints are:

- `GET /run/config` returns the full run configuration: hero, monsters, moves, encounters, environments, items, shop offers, rules, hero classes, the branching map nodes, and the Endless Mode config.
- `GET /battle/next-move` returns the monster's next move id given the current battle state. Optional query parameters cover monster mana and the active effects on each side.

`docs/api-contract.md` has the full payload examples.

## How to Run the Unity Client

You will need Unity 2022.3 LTS or newer with URP.

1. Open `client-unity/NordeusChallenge.Client` in the Unity Editor.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Make sure the backend is running.
4. The `baseUrl` field on `MainMenuController` and `BattleController` defaults to `http://localhost:5046`. Adjust if you host the API somewhere else.
5. Press **Play**.

## Standard Run Flow

1. **Main Menu.** Pick a language, then click **Standard Run**. The client calls `GET /run/config`.
2. **Class Selection.** Pick one of four hero classes (Knight, Ranger, Mage, Cleric). The class seeds the hero with starting stats, equipped moves, and a learned move pool.
3. **Run Overview.** The branching map shows up. Available nodes are clickable, cleared nodes get a tag, locked nodes are dimmed.
4. **Battle, Elite, or Boss nodes** open the Battle scene against the encounter referenced by the node. On victory the client awards XP, gold, a possible item drop, a possible new move, and an attribute pick when the hero levels up. Losing replays the same node, with no XP or gold loss.
5. **Shop nodes** open the shop screen. Items and stat upgrades cost gold; leaving the shop unlocks the connected nodes.
6. **Boss node (Dragon).** Beating the boss completes the run and the map shows a victory banner.

The Run Overview also has buttons for Move Management, Item Management, the Shop, and **Save & Exit**.

## Endless Mode Flow

1. **Main Menu.** Click **Endless Run**. The client calls `GET /run/config` and uses the `endlessMode` block.
2. **Class Selection.** Same picker as the standard run.
3. **Run Overview (Endless).** Instead of the branching map, the player sees the current floor number, the next enemy or shop, and a **Start Floor** button. Floors are generated from the server pools and period rules. Every Nth floor is an Elite, a Shop, or a Boss; everything else is a normal Battle.
4. **Battles** use the same battle pipeline as the standard run. Victory advances the floor; XP and gold scale with floor number.
5. **Shop floors** open the standard shop screen; pressing Back advances to the next floor.
6. **Defeat** ends the endless run. The summary shows the floor reached, and the player can return to the Main Menu.

## Key Implemented Features

- **Core.** Run config endpoint, encounter list, turn-based battle with five move categories, server-driven opponent moves, XP and level-up flow, learned-move auto-equip, move management screen.
- **Combat depth.** Status effects (Bleed, Poison, DamageIncrease, DamageReduction, Buff and Debuff for Attack, Defense, and Magic), Heal moves, mana and HP costs, battle log, hit and heal feedback.
- **Smarter opponent.** Heal-when-low rule, finishing-blow rule, redundant-effect skip, mild damage bias, and an affordability filter (mana and HP costs).
- **Progression and economy.** Attribute choice on level-up, items with stat bonuses, item management screen, shop with item and stat-upgrade offers, gold rewards.
- **Content.** 8 monsters, 40+ moves, 8 environments, 14 items, 8 shop offers, 5 level-up choices, 4 hero classes (Knight, Ranger, Mage, Cleric).
- **Map.** A small Slay-the-Spire-style branching graph with Battle, Elite, Shop, and Boss node types.
- **Endless Mode.** Server-defined pools and curves, client-side floor generation, period-based floor types, linear reward curves.
- **Save and Exit.** Local JSON save with **Continue Run** on the Main Menu. The save restores hero state, gold, inventory, equipped items, and the map or endless progression.
- **Localization.** A small custom JSON-backed system with English and Serbian Latin tables. Covers UI labels and the dynamic monster, move, item, environment, shop, and class names and descriptions. Falls back to English, then to the server-provided string.
- **Environmental effects.** Small flat integer modifiers per encounter (physical or magic damage bonus, healing bonus, end-of-turn damage, poison duration, bleed bonus, mana regen).
- **Settings panel.** Resolution, fullscreen mode, VSync, framerate cap, optional audio sliders, language, and a reset button. Settings persist in `PlayerPrefs` and apply before any scene loads on next launch.

## Architecture Summary

- **The server is authoritative for two things only:** the run configuration and the opponent's move selection. It is stateless between requests and exposes a tiny minimal-API surface.
- **The client owns** scene flow, UI state, and resolving the player's chosen move against shared rules. A `GameSession` MonoBehaviour singleton holds the active run, hero, items, gold, map progression, endless state, and run mode.
- **DTOs on the client** mirror the server's response shapes (Unity `JsonUtility`-friendly), so the same data model flows end-to-end.
- **Networking** is in two small API client classes (`RunApiClient`, `BattleApiClient`), so the controllers don't have to think about HTTP.
- **Save / load** writes a single JSON file under `Application.persistentDataPath`. The save only stores mutable run state; the run config gets refetched from the server on Continue.
- **Localization** is a static `Loc` API backed by JSON tables under `Resources/Localization/`. Dynamic data names go through `LocalizedNames` helpers using `<category>.<id>.{name,description}` keys, with the server's English text as the final fallback.

## Known Limitations

- No automated tests on either side. With more time this is the first thing I would add. I leaned into a working end-to-end slice across the bonus backlog instead.
- The server is authoritative for run config and monster moves only. A dishonest client could misreport outcomes; in production I would move battle resolution server-side.
- One save slot, no encryption, no autosave.
- The opponent's move logic is a small layered heuristic. It does not adapt across battles or across players.
- The branching map is one hand-authored graph; it doesn't generate per run.
- Endless Mode scaling is the linear formulas in `endlessMode` plus encounter-level scaling. The basis-point multiplier fields are there but ship at zero, so late-game ramp is intentionally gentle.
- The class picker, map UI, and endless panel are functional UGUI layouts; no animated transitions.
- `baseUrl` lives on individual controllers with a sensible default. A central config asset would be tidier.

`docs/submission-notes.md` has the full per-feature breakdown, `docs/feature-checklist.md` walks through the requirements row by row, and `docs/testing-checklist.md` is the manual smoke-test plan I run before pushing.
