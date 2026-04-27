# Submission Notes

Notes on what is implemented in this submission, what was intentionally left out, and what I would address next.

## Implemented Core Requirements

- **Server (ASP.NET Core minimal API)**
  - `GET /run/config` returns a full run configuration: hero, monsters, moves, encounters, and rules (XP curve, stat gain per level, equipped move slots).
  - `GET /battle/next-move` returns the monster's next move id given current battle state (monster id, level, both HP pools, current turn).
  - Config and battle logic live in dedicated services (`RunConfigService`, `BattleService`).
- **Client (Unity)**
  - Main menu that requests a run config and transitions into the run.
  - Run overview scene with a linear encounter list and encounter unlocking.
  - Battle scene with turn-based flow, five move categories (Physical, Magic, Heal, Buff, Debuff), timed active effects, and a combat log.
  - Server-driven opponent turn via `/battle/next-move`.
  - Post-battle progression: XP gain, level up with stat growth, learning new moves with auto-equip into free slots, and unlocking the next encounter.
  - Move management scene for re-equipping moves from the learned pool.
- **Data contract** shared between client and server via matching DTOs so a single shape describes moves, heroes, monsters, encounters, and rules.

## Implemented Bonus / Polish Items

- Move info panel in the battle scene — hovering or keyboard-selecting a move button shows its name, category, power (when relevant), and description.
- Improved selected-move detail in the move management scene with the same formatting.
- Rich-text formatting (bold name) in both info panels for readability.
- Consistent stat display (`ATK / DEF / MAG`, `HP x / y`) and running combat log in battle.
- Graceful empty / error states (no active run, no encounter selected, unknown monster, unknown move).
- Combat depth: status effects (Bleed, Poison, DamageIncrease, DamageReduction) layered on top of the existing Buff/Debuff/Heal kinds.
- Environmental effects per encounter, plus a level-up choice panel that lets the player invest each gain into one of `rules.levelUpChoices`.
- Items, item management, and a small in-run shop with a gold economy. Items are stateless catalog entries; ownership and equipped state live on the client.
- Custom localization layer (English + Serbian Latin) covering UI labels and dynamic data names with English fallback.
- **Expanded content:** three new monsters (Skeleton Knight, Forest Troll, Fire Elemental) with four moves each, three new environments (Crypt, Ancient Forest, Ember Chamber), and four new item drops (Bone Pauldrons, Grave Signet, Troll Hide Cloak, Ember Core). The default encounter list now runs eight battles ramping from Lv 1 to Lv 7.
- **Hero classes:** `RunConfigResponse.heroClasses` exposes four selectable archetypes — Knight (balanced), Ranger (high attack with bleed), Mage (burst caster), Cleric (durable supporter). Each class declares `startingStats`, `startingMoves`, and `startingLearnedMoves`; the client lets the player pick one before the run starts and seeds the active Hero from that entry. The legacy `hero` field is preserved as a Knight-shaped default for clients that have not adopted the picker. Three small class-flavor moves (`arcane_focus`, `blessed_mend`, `smite`) were added to round out Mage and Cleric kits, all using effect kinds that already existed in the engine.
- **Non-linear map (this submission):** `RunConfigResponse.mapNodes` describes a small Slay-the-Spire-style branching graph. Ten nodes laid out across five depths (start → two paths → fork with optional shop → fork with optional shop → boss). `Battle`, `Elite`, and `Boss` nodes carry an `encounterIndex` into the existing `encounters` list, so no encounter data is duplicated. `Shop` nodes use `encounterIndex: -1` and reuse the existing shop screen on the client. Every path eventually converges on the Dragon boss. The linear `encounters` array is still emitted, so older clients keep working unchanged.

## Known Limitations

- Run state is in-memory on the client; closing the app ends the run.
- The server is not authoritative for player actions — only for config and monster moves. A dishonest client could misreport outcomes.
- No persistence layer; `RunConfigService` returns a hardcoded configuration.
- No automated tests on either side.
- UI is functional but unpolished — no animations, VFX, or sound.
- A single hero and a single linear encounter path; no branching, no procedural content.
- Monster AI is deterministic and relatively simple; it does not adapt over a run.
- `baseUrl` is hardcoded per controller with a sensible default (`http://localhost:5046`) rather than driven by a central config asset.

## What I Would Improve Next

- **Server authority over battle resolution.** Move damage, heal, and effect application to the server and have the client submit the player's chosen move instead of resolving locally. Eliminates the trust gap and keeps rules in one place.
- **Persistence.** A small store (file or SQLite) for runs, so a player can resume after closing the app, and so server-side stats become possible.
- **Tests.** Unit tests around `BattleService` move selection and the client-side damage/heal/effect math; a couple of integration tests hitting the endpoints.
- **Config-driven content.** Load monsters, moves, and encounters from JSON on the server so designers can iterate without a code change.
- **Better AI.** Lightweight scoring over available moves (prefer heals when low HP, avoid overwriting active buffs, account for matchup) rather than the current heuristic.
- **UI polish.** Health bars, simple hit/heal feedback, and transitions between scenes; better layout for the move info panel.
- **Centralized API configuration.** One ScriptableObject or config scene for `baseUrl` and related settings instead of per-controller fields.
- **Error surfacing.** Replace silent `statusText` fallbacks with clearer retry prompts when the server is unreachable.
