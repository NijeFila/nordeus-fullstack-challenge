# Submission Notes

A short, honest summary of what is in this submission, what I deliberately kept simple, and what I would tackle next if I had more time.

## Implemented Core Requirements

**Server (ASP.NET Core 8 minimal API)**

- `GET /run/config` returns a complete run configuration: hero, monsters, moves, encounters, environments, items, shop offers, hero classes, branching map, Endless Mode config, and tunable rules (XP curve, gold per victory, equipped slot caps, level-up choices, stat gain per level).
- `GET /battle/next-move` returns the monster's next move id given the current battle state. Optional query parameters cover monster mana and the active effects on each side.
- Run config and battle logic live in `RunConfigService` and `BattleService`.

**Client (Unity)**

- Main menu that fetches `/run/config` and routes the player into the chosen mode.
- Run overview that shows the branching map (or the Endless Mode panel) plus equipped moves, equipped items, inventory, gold, and hero stats.
- Battle scene with turn-based flow, five move categories (Physical, Magic, Heal, Buff, Debuff), timed active effects, mana and HP costs, and a running combat log.
- Server-driven opponent turn through `/battle/next-move`.
- Post-battle progression: XP gain, level-up with attribute choice, learning new moves with auto-equip into free slots, item drops, gold rewards, and unlocking the next map node.
- Move Management for re-equipping moves from the learned pool.

**Data contract** is shared between client and server through matching DTOs, so a single shape describes the hero, moves, monsters, encounters, environments, items, shop offers, hero classes, map nodes, and endless config.

## Implemented Bonus Features

- **Move descriptions.** Every move ships with a description; battle and management screens render them through a shared info panel.
- **Attribute choices on level up.** `rules.levelUpChoices` drives a post-battle picker. If multiple level-ups happen in one battle the picker queues until they are all spent.
- **Status effects.** Bleed and Poison damage-over-time, Buff and Debuff for Attack, Defense, and Magic, plus DamageIncrease and DamageReduction. Stacking is bounded to one instance per `(source move, kind)` pair.
- **Resource costs.** Moves can declare `manaCost` and `hpCost`. The bot's affordability filter respects both for the monster, and the move buttons disable when the hero cannot pay.
- **Save and Exit.** Local JSON save under `Application.persistentDataPath`. The Main Menu shows **Continue Run** and **Delete Save** when a save exists. Continue refetches the run config and reapplies the saved mutable state.
- **Battle log.** Last few lines of combat events shown alongside the battle UI.
- **Battle animations and feedback.** Hit and heal feedback (HP bars, floating combat text, simple damage and heal flashes) over the existing UGUI battle scene.
- **Smarter opponent.** `BattleService` filters by affordability, prefers heals when low, reaches for finishing blows, skips redundant buffs or DoTs already on the relevant target, and applies a mild damage bias on the rest.
- **Items.** Equippable gear with flat stat bonuses, slot constraints (`weapon`, `armor`, `trinket` with caps), and rarity strings. Bonuses apply at battle start.
- **Shop.** Gold-priced item and stat-upgrade offers; one shop catalog returned in the run config; ownership tracked client-side during the run.
- **More enemies and moves.** 8 monsters (Goblin Warrior, Goblin Mage, Giant Spider, Skeleton Knight, Forest Troll, Witch, Fire Elemental, Dragon) with four moves each, plus class-specific extras for Mage and Cleric.
- **Non-linear map.** Small Slay-the-Spire-style branching graph (10 nodes across 5 depths) with Battle, Elite, Shop, and Boss node types. Reuses the existing encounter list by index.
- **Environmental effects.** Flat integer modifiers attached to encounters (physical or magic damage bonus, healing bonus, end-of-turn damage, poison duration, bleed bonus, mana regen). Applied symmetrically to both combatants.
- **Endless mode.** Server ships pools (monster, elite, boss, environment) and curves (level scaling, gold and XP per floor, optional basis-point multipliers); the client generates each floor on demand. Floor types use a fixed Boss > Elite > Shop > Battle precedence. Defeat ends the endless run.
- **Hero classes.** Four selectable archetypes (Knight, Ranger, Mage, Cleric) returned in `heroClasses`. The client shows a class picker before the run starts and seeds the active hero from the chosen entry. The legacy `hero` field still mirrors the default class for older clients.

## Extra Polish

- **Localization.** A small custom JSON-backed system loaded from `Resources/Localization/`. Two languages ship: English and Serbian Latin (Latin only, no Cyrillic). Covers UI labels and all dynamic monster, move, item, environment, shop, and class names and descriptions. Missing keys fall back to English, then to the server-provided English string. A language picker on the Main Menu persists the choice through `PlayerPrefs`.
- **Settings panel.** Resolution, fullscreen mode, VSync, framerate cap, optional audio sliders, language, and a reset button. Values persist in `PlayerPrefs` and a small bootstrapper applies them before any scene loads on next launch.
- **VisualCatalog.** A single ScriptableObject mapping monster, hero, move, and effect ids to sprites with a default-fallback path. Missing entries don't crash the UI.
- **Defensive guards.** Restoring a save drops unknown move, item, and map-node ids; the hero gets reseeded from the saved class id with mutable fields applied on top.

## Deliberate Simplifications

- Run state lives in memory plus a single optional JSON save. No accounts, no server-side persistence, no cloud save, no autosave.
- The server is authoritative for run config and monster moves only. A dishonest client could misreport outcomes.
- One save slot, no encryption, no compression.
- The opponent's move logic is a layered heuristic; it doesn't adapt over a run or across players.
- The branching map is one hand-authored graph; it isn't procedural.
- Endless Mode difficulty is the linear curves in `endlessMode` plus the standard monster scaling rule. The optional basis-point multiplier fields ship at zero (disabled).
- Battle resolution (damage, heals, status ticking) runs on the client against rules defined on the server side; there is no rollback or anti-cheat.
- UI is functional UGUI: TextMeshPro labels, layout groups, simple animations and flashes. No bespoke shaders or complex transitions.
- No automated tests on either side. The scope and time budget went into a working end-to-end slice across the bonus backlog.

## What I Would Improve Next

These are the items I'd pick up first if I had another week.

- **Server authority for the battle.** Move damage, heal, and effect application to the server; have the client submit the player's chosen move and receive the resolved turn. Keeps rules in one place and removes the trust gap.
- **Tests.** Unit tests around `BattleService` move selection and the client-side damage, heal, and effect math. A couple of integration tests over `/run/config` and `/battle/next-move`.
- **Config-driven content.** Load monsters, moves, encounters, items, hero classes, the map graph, and the endless config from JSON on the server, so designers can iterate without a code change.
- **Procedural map generator.** Replace the hand-authored map with a small generator (per run, with a seed derived from `runId`) so each playthrough varies.
- **Better opponent.** Lightweight scoring over available moves (account for matchup, current effects, the value of a setup turn) instead of the current ordered heuristic.
- **One client config asset.** A single ScriptableObject for `baseUrl` and related settings, plus a small retry / error-surface layer in the API clients.
- **Save-slot UX.** Multiple save slots with timestamp and run summary; a clearer "save now" indicator outside of the Save & Exit button.
- **Endless tuning passes.** Curate the endless pools by floor band, drop more interesting environments and elites at higher floors, and exercise the basis-point multipliers for late-game ramp.

## Closing Note

The brief asks for a small, well-shaped vertical slice; I treated the bonus backlog as an opportunity to show how I prioritise once the core works. If anything in here looks off, please flag it. I would much rather hear "this section is over-engineered" or "I expected X here" than have you guess at what I was thinking.
