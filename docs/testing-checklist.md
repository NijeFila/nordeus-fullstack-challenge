# Manual Testing Checklist

There are no automated tests in this submission. The list below is the manual smoke-test plan I run before pushing, and what a reviewer can walk through end-to-end. Default backend URL is `http://localhost:5046`.

If anything in here fails on your machine, please flag it. I tested on Windows; I have not run it on macOS or Linux.

## Backend Endpoint Tests

Run `dotnet run` from `server/NordeusChallenge.Api`, then exercise the endpoints with curl or a browser.

**`GET /run/config`**

- Returns HTTP 200 with a JSON body.
- Top-level fields present: `runId`, `hero`, `encounters`, `monsters`, `moves`, `environments`, `items`, `shopOffers`, `rules`, `heroClasses`, `defaultHeroClassId`, `mapNodes`, `startingMapNodeId`, `endlessMode`.
- `encounters.length` is 8; each entry has a valid `monsterId`, `level`, and `environmentId` referencing the catalogs.
- `heroClasses.length` is 4 (`knight`, `ranger`, `mage`, `cleric`). `defaultHeroClassId` matches one of them.
- `mapNodes` contains exactly one `Boss` entry. Every `connectedTo` id resolves to another node id. Every non-Shop node has a valid `encounterIndex`. Shop nodes use `-1`.
- `endlessMode.enabled` is `true`. Pools reference only ids that also appear in `monsters` and `environments`.

**`GET /battle/next-move`**

- Minimal call (`monsterId` plus HP and hero-HP fields) returns 200 with a `moveId` from that monster's move list.
- Calling with `monsterMana=0` excludes any move whose `manaCost > 0`.
- Calling with low `monsterHealth` (for example `monsterHealth=10` on a 100 HP monster) prefers a Heal-category move when one is in the monster's move list.
- Calling with very low `heroHealth` (for example `heroHealth=5` on a 100 HP hero) prefers the highest-power affordable damaging move.
- Calling with `monsterEffects=BuffDefense` and a move whose effect kind is `BuffDefense` and target `Self` should not return that redundant move (unless it is the only legal pick).
- Unknown `monsterId` returns `400 Bad Request` with `{"error": "Unknown monsterId '...'."}`. Missing required query parameters return `400` with `{"error": "Missing required battle-state parameters."}`. The client surfaces the error in the battle log and continues to function.

## Standard Run Tests

Start a Standard Run from the Main Menu.

- **Class selection.** All four classes show localized name and description. Selecting each one seeds the hero with the expected stats and equipped moves on Run Overview.
- **Run Overview (map mode).** Only the starting node is *Available*; everything else is *Locked*. Boss, Elite, and Shop highlights match the node type.
- **Battle node.** Clicking an available battle node opens the Battle scene with the right monster name, level, and environment. Clearing a node moves it to *Cleared* and unlocks every node listed in its `connectedTo`.
- **Shop node.** Opens the Shop scene; pressing Back marks the shop node *Cleared* and unlocks its connections.
- **Boss node (Dragon).** Defeating the boss triggers the run-victory banner; the remaining map buttons go non-interactive.
- **Defeat.** Losing a battle keeps the same map node selectable; no XP or gold loss.

**Battle systems**

- All five move categories resolve: Physical, Magic, Heal, Buff, Debuff.
- Status effects tick at end of turn (Bleed, Poison) and decrement durations.
- Buff and Debuff modifiers apply to effective stats during the battle.
- Mana and HP costs are paid; unaffordable moves disable on the hero side.
- Environment modifiers reflect in damage and healing numbers (for example Training Fields adds +2 to physical hits).

**Post-battle**

- XP and gold gained match `rules.xpPerVictory` and `rules.goldPerVictory` for standard runs.
- Item drops land in the inventory and auto-equip into a free slot when possible. Duplicates are reported and not re-added.
- Move learning auto-equips when a slot is free. Duplicates are skipped.
- Level-up triggers the attribute picker the right number of times when multiple thresholds cross in one battle.

**Other screens**

- **Item Management.** Equipping or unequipping changes the hero summary line on the screen; the trinket cap of 2 is enforced.
- **Move Management.** The equipped slots and learned pool update correctly when swapping moves.
- **Shop.** Buying an item subtracts gold and adds the item to inventory. Stat-upgrade offers update the hero stats. "Need Xg" and "Owned" labels show the right state.

## Endless Run Tests

Start an Endless Run from the Main Menu.

- **Run Overview (endless panel).** Shows `Floor 1`, the next-enemy line, and a **Start Floor** button. The branching map is hidden.
- **Battle, Elite, and Boss floors.** Battle floors draw from `monsterPool`. Floor 5 (with `eliteEvery = 5`) is an Elite from `eliteMonsterPool`. Floor 10 is a Boss from `bossMonsterPool` (Dragon by default).
- **Shop floors.** Floors 3, 6, and 9 (with `shopEvery = 3`, when not overridden by a Boss or Elite slot) open the Shop scene; pressing Back advances the floor.
- **Floor scaling.** Encounter level rises by 1 every `levelIncreaseEvery` floors (default 2).
- **Reward scaling.** Gold and XP follow the linear formulas (`rewardGoldBase + (floor-1)*rewardGoldPerFloor`, `xpBase + (floor-1)*xpPerFloor`); reward log lines reflect the increasing values.
- **Defeat.** The endless run ends. The panel switches to the defeat summary with the floor reached and a **Return to Menu** button. No further floors are reachable.
- **Best floor tracking.** `EndlessBestFloor` updates when a floor is cleared.

## Save and Load Tests

- **Save & Exit on Run Overview (standard run).** Click Save & Exit. The scene returns to Main Menu, and **Continue Run** and **Delete Save** appear. Continue refetches `/run/config`, restores hero, gold, inventory, equipped items, and map progression. Cleared and Available nodes match the saved state.
- **Save & Exit on Move Management, Item Management, or Shop.** Same behavior: save lands on disk, the Main Menu shows the buttons, Continue restores correctly.
- **Save & Exit during Endless Mode.** Continue restores `EndlessFloor`, `EndlessBestFloor`, `EndlessFloorType`, and the synthesized next-enemy when the saved monster id is still in the catalog. If the saved monster is missing, the next floor is rerolled instead of crashing.
- **Delete Save.** The file under `Application.persistentDataPath/nordeus_challenge_save.json` is removed; the two save-related buttons disappear immediately.
- **Malformed save.** Replace the JSON with garbage; clicking **Continue Run** surfaces "Could not load save." and stays on Main Menu without crashing.
- **Stale ids.** Hand-edit the save to reference a non-existent move, item, or map-node id. Restore drops only the unknown ids and keeps the rest of the run intact. If no hero can be reconstructed, **Continue Run** stays on Main Menu with the same localized error.
- **No save.** Clicking **Continue Run** when the file is missing surfaces "No save file found." and does not transition.

## Localization Tests

- **Language picker on Main Menu.** Switching to Serbian Latin updates the Main Menu labels, status text, and the rest of the UI immediately. The choice persists across an editor restart through `PlayerPrefs`.
- **Dynamic data.** Monster names, move names and descriptions, item names and descriptions, environment names and descriptions, shop offer text, level-up choice text, and class names and descriptions all swap to Serbian Latin.
- **Latin only.** Confirm no Cyrillic characters appear in any Serbian string.
- **Fallbacks.** Temporarily remove a key from `sr-Latn.json`; the corresponding label falls back to the English entry. Remove the key from `en.json` too; the label falls back to the server-provided English string. Neither path crashes.
- **Format strings.** Placeholders (`{0}`, `{1}`) render correctly in both languages (for example "Floor {0}" and "Sprat {0}", "+{0} XP." and "+{0} iskustva.").

## Settings Tests

- **Open the panel.** Click **Options** on the Main Menu; the settings panel appears with controls populated from the current engine state.
- **Resolution.** Pick a different available resolution and click Apply; the window resizes and the status text reads "Settings applied.". Re-enter Play; the saved resolution applies before any scene loads.
- **VSync and framerate.** Toggle VSync off and pick a 60 cap. Confirm with the Stats overlay.
- **Language.** Pick Serbian Latin, click Apply; every `LocalizedText` retranslates immediately.
- **Reset to Defaults.** Status text reads "Defaults restored." Restart Play to confirm the bootstrapper does not re-apply old values.

## Regression Checks

After exercising any of the bonus features, walk a quick regression pass:

- The standard run still completes end-to-end with the legacy linear `encounters` list when `mapNodes` are absent (test by temporarily clearing `MapNodes` on the server).
- Toggling Endless Mode off (`endlessMode.enabled = false`) hides the Endless Run flow on the Main Menu without affecting Standard Run.
- Hero classes: picking each class produces the expected starting stats and starting moves on the Run Overview.
- Items, Move Management, and Shop screens render correctly with eight encounters worth of drops and offers.
- Environment modifiers still apply when the encounter is reached through the branching map (not just the linear list).
- Save & Exit followed by Continue produces the same in-game state on every safe screen.
- Switching the language mid-run does not break formatting in the battle log, the level-up panel, or the run-overview hero summary.
- Defeat in standard mode replays the same encounter; defeat in endless mode ends the run.
- `dotnet build` from `server/NordeusChallenge.Api` succeeds with 0 warnings and 0 errors.
- The Unity Editor logs no `NullReferenceException` or `MissingReferenceException` during a full standard run, a full endless ramp through floor 10, or a save-and-continue cycle.
