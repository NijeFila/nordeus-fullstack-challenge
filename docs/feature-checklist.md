# Feature Checklist

A row-by-row map of what was asked for, where it lives in the code, and whether I shipped it. Status keys: **Done**, **Partial**, **Not implemented**.

## Core Challenge Requirements

| Requirement | Status | Notes |
|---|---|---|
| Backend exposes a run configuration endpoint | Done | `GET /run/config` in `RunEndpoints` returns the full payload. |
| Backend exposes an opponent-move endpoint | Done | `GET /battle/next-move` in `BattleEndpoints`; logic in `BattleService.SelectNextMove`. |
| Client fetches run configuration on Start | Done | `MainMenuController` then `RunApiClient.GetRunConfig`. |
| Run overview screen with encounter list | Done | `RunOverviewController`; renders the branching map when `mapNodes` is present, otherwise the linear list. |
| Turn-based battle scene | Done | `BattleController`, `MoveButtonView`, `MoveInfoPanelView`. |
| Five move categories (Physical, Magic, Heal, Buff, Debuff) | Done | `Move.Category` and the `ResolveMove` switch. |
| XP and progression on victory | Done | `GameSession.ApplyVictoryRewards` and `ApplyEndlessVictoryRewards` for endless. |
| Move management screen | Done | `MoveManagementController`. |
| Shared data contract between client and server | Done | DTOs under `Assets/Scripts/Models` mirror server `Models`. |
| Server is stateless | Done | `RunConfigService` and `BattleService` hold no per-player state. |

## Bonus Backlog

| Bonus item | Status | Where |
|---|---|---|
| **Move descriptions** | Done | `Move.Description` on the server; `MoveInfoPanelView` and the battle move-info text on the client. |
| **Attribute choices on level up** | Done | `RulesConfig.LevelUpChoices` on the server; `LevelUpChoicePanelView` and `GameSession.ApplyLevelUpChoice` on the client. Multiple level-ups queue up. |
| **Status effects** | Done | Bleed, Poison, Buff and Debuff for Attack, Defense, and Magic, plus DamageIncrease and DamageReduction. All resolved in `BattleController`. |
| **Resource costs** | Done | `Move.ManaCost` and `Move.HpCost`, hero-side affordability check, bot-side affordability filter in `BattleService`. |
| **Save & exit** | Done | `SaveGameService`, `SaveGameData`, `GameSession.CreateSaveData` and `RestoreFromSaveData`, **Continue Run** and **Delete Save** buttons on the Main Menu, `SaveAndExitButton` component on safe screens. |
| **Battle log** | Done | Rolling log fed by `BattleController.AppendLog`; lines are localized. |
| **Battle animations and feedback** | Done | `BattleFeedbackView`, `FloatingCombatText`, `HpBarView`. Hit and heal flashes, animated HP bars, floating numbers. |
| **Smarter opponent** | Done | `BattleService` layers an affordability filter, a heal-when-low rule, a finishing-blow rule, a redundant-effect skip, and a mild damage bias. |
| **Items** | Done | `Item` model with slot, rarity, and stat bonuses. Client-side ownership and equipping in `GameSession`; bonuses applied at battle start. |
| **Shop** | Done | `ShopOffer` catalog (item and stat-upgrade types); `ShopController` and `ShopOfferView`; gold tracked per run. |
| **More enemies and moves** | Done | 8 monsters total. New content includes Skeleton Knight, Forest Troll, and Fire Elemental, four moves each, three new environments, four new item drops. |
| **Non-linear map** | Done | `RunMapNode` graph (Battle, Elite, Shop, Boss); `RunOverviewController` map renderer, `RunMapNodeView`, `GameSession` map progression. The linear `encounters` array is still emitted for backward compatibility. |
| **Environmental effects** | Done | `BattleEnvironment` modifiers (physical and magic damage, healing, end-of-turn damage, poison duration, bleed bonus, mana regen) wired into the battle pipeline. |
| **Endless mode** | Done | `EndlessModeConfig` on the server with monster, elite, boss, and environment pools, plus linear reward curves. Client-side floor generation in `GameSession.GenerateNextEndlessFloor`. Defeat ends the endless run. |
| **Hero classes** | Done | Four classes (Knight, Ranger, Mage, Cleric) in `heroClasses`. Pre-run picker in `ClassSelectionController`; legacy `hero` field preserved for backward compatibility. |

## Extra Polish (not in the brief)

| Item | Status | Where |
|---|---|---|
| English and Serbian Latin localization | Done | `Loc`, `LocalizedNames`, `LocalizedText`, `LanguageSelector`; tables under `Assets/Resources/Localization/`. |
| Settings panel (resolution, fullscreen, VSync, framerate, audio, language, reset) | Done | `SettingsController`, `SettingsBootstrapper`, `SettingsKeys` under `Assets/Scripts/UI/Settings/`. |
| Visual catalog with default-sprite fallback | Done | `VisualCatalog` ScriptableObject; missing ids return defaults instead of crashing. |
| Backward compatibility on every server addition | Done | Each new field on `RunConfigResponse` (hero classes, map nodes, endless config, save-relevant ids) is additive; older clients ignore them. |

## Explicitly Not Implemented

| Item | Status | Reason |
|---|---|---|
| Cloud save / multi-device sync | Not implemented | Out of scope; the brief did not require it and persistence is local only. |
| Save encryption / anti-tamper | Not implemented | Out of scope for an interview prototype. |
| Multiple save slots | Not implemented | Single-slot save keeps the UI and code small. |
| Server-authoritative battle resolution | Not implemented | The server only owns run config and opponent move selection. The client resolves the player's chosen move. Listed under "What I would improve next." |
| Automated tests | Not implemented | Time budget went into the bonus backlog instead. First thing I would add next. |
| Procedural map generation | Not implemented | The non-linear map is a single hand-authored graph. |
| Mid-battle save | Not implemented | Saves are restricted to safe screens to avoid mid-turn state inconsistencies. |
