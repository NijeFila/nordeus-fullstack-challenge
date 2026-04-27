# Feature Checklist

A reviewer-oriented map of what was asked for, where it lives, and its implementation status. Status keys: **Done**, **Partial**, **Not implemented**.

## Core Challenge Requirements

| Requirement | Status | Notes |
|---|---|---|
| Backend exposes a run configuration endpoint | Done | `GET /run/config` in `RunEndpoints` returns the full payload. |
| Backend exposes an opponent-move endpoint | Done | `GET /battle/next-move` in `BattleEndpoints`; logic in `BattleService.SelectNextMove`. |
| Client fetches run configuration on Start | Done | `MainMenuController` → `RunApiClient.GetRunConfig`. |
| Run overview screen with encounter list | Done | `RunOverviewController`; renders branching map when `mapNodes` is present, otherwise the linear list. |
| Turn-based battle scene | Done | `BattleController`, `MoveButtonView`, `MoveInfoPanelView`. |
| Five move categories (Physical, Magic, Heal, Buff, Debuff) | Done | `Move.Category` and the `ResolveMove` switch. |
| XP and progression on victory | Done | `GameSession.ApplyVictoryRewards` (and `ApplyEndlessVictoryRewards` for endless). |
| Move management screen | Done | `MoveManagementController`. |
| Shared data contract between client and server | Done | DTOs under `Assets/Scripts/Models` mirror server `Models`. |
| Server is stateless | Done | `RunConfigService` and `BattleService` hold no per-player state. |

## Bonus Backlog

| Bonus item | Status | Where |
|---|---|---|
| **Move descriptions** | Done | `Move.Description` on the server; `MoveInfoPanelView` and the battle move-info text on the client. |
| **Attribute choices on level up** | Done | `RulesConfig.LevelUpChoices` on the server; `LevelUpChoicePanelView` and `GameSession.ApplyLevelUpChoice` on the client. Multiple level-ups queue. |
| **Status effects** | Done | Bleed, Poison, Buff/Debuff (Attack/Defense/Magic), DamageIncrease, DamageReduction, Heal — all resolved in `BattleController`. |
| **Resource costs** | Done | `Move.ManaCost` / `HpCost`, hero-side affordability check, bot-side affordability filter in `BattleService`. |
| **Save & exit** | Done | `SaveGameService`, `SaveGameData`, `GameSession.CreateSaveData` / `RestoreFromSaveData`, **Continue Run** + **Delete Save** buttons on Main Menu, `SaveAndExitButton` component on safe screens. |
| **Battle log** | Done | Rolling log fed by `BattleController.AppendLog`; localized lines. |
| **Battle animations / feedback** | Done | `BattleFeedbackView`, `FloatingCombatText`, `HpBarView`. Hit / heal flashes, animated HP bars, floating numbers. |
| **Smarter bot** | Done | `BattleService` layers affordability filter → heal-when-low → finishing blow → redundant-effect skip → mild damage bias. |
| **Items** | Done | `Item` model with slot, rarity, stat bonuses; client-side ownership and equipping in `GameSession`; bonuses applied at battle start. |
| **Shop** | Done | `ShopOffer` catalog (item + stat-upgrade types); `ShopController` and `ShopOfferView`; gold tracked per run. |
| **More enemies and moves** | Done | 8 monsters total; new content includes Skeleton Knight, Forest Troll, Fire Elemental plus four moves each, three new environments, four new item drops. |
| **Non-linear map** | Done | `RunMapNode` graph (`Battle` / `Elite` / `Shop` / `Boss`); `RunOverviewController` map renderer, `RunMapNodeView`, `GameSession` map progression. Linear `encounters` array still emitted for backward compatibility. |
| **Environmental effects** | Done | `BattleEnvironment` modifiers (physical / magic damage, healing, end-of-turn damage, poison duration, bleed bonus, mana regen) wired into the battle pipeline. |
| **Endless mode** | Done | `EndlessModeConfig` on the server with monster / elite / boss / environment pools and linear reward curves; client-side floor generation in `GameSession.GenerateNextEndlessFloor`; defeat ends the endless run. |
| **Hero classes** | Done | Four classes (Knight, Ranger, Mage, Cleric) in `heroClasses`; pre-run picker in `ClassSelectionController`; legacy `hero` field preserved for backward compatibility. |

## Extra Polish (not in the brief)

| Item | Status | Where |
|---|---|---|
| English + Serbian Latin localization | Done | `Loc`, `LocalizedNames`, `LocalizedText`, `LanguageSelector`; tables under `Assets/Resources/Localization/`. |
| Visual catalog with default-sprite fallback | Done | `VisualCatalog` ScriptableObject; missing ids return defaults instead of crashing. |
| Backward compatibility on every server addition | Done | Each new field on `RunConfigResponse` (hero classes, map nodes, endless config, save-relevant ids) is additive; older clients ignore them. |

## Explicitly Not Implemented

| Item | Status | Reason |
|---|---|---|
| Cloud save / multi-device sync | Not implemented | Out of scope; the brief did not require it and persistence is local-only. |
| Save encryption / anti-tamper | Not implemented | Out of scope for an interview prototype. |
| Multiple save slots | Not implemented | Single-slot save keeps the UI and code small. |
| Server-authoritative battle resolution | Not implemented | The server only owns run config and opponent move selection; client resolves the player's chosen move. Listed under "What I would improve next." |
| Automated tests | Not implemented | Time budget favored end-to-end coverage of the bonus backlog. |
| Procedural map generation | Not implemented | The non-linear map is a single hand-authored graph. |
| Mid-battle save | Not implemented | Saves are restricted to safe screens to avoid mid-turn state inconsistencies. |
