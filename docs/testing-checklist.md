# Manual Testing Checklist

There are no automated tests in this submission. The list below is the manual smoke-test plan a reviewer can walk through end-to-end. Default backend URL is `http://localhost:5046`.

## Backend Endpoint Tests

Run `dotnet run` from `server/NordeusChallenge.Api`, then exercise the endpoints (curl or browser).

- **`GET /run/config`**
  - Returns HTTP 200 with a JSON body.
  - Top-level fields present: `runId`, `hero`, `encounters`, `monsters`, `moves`, `environments`, `items`, `shopOffers`, `rules`, `heroClasses`, `defaultHeroClassId`, `mapNodes`, `startingMapNodeId`, `endlessMode`.
  - `encounters.length` is 8; each entry has a valid `monsterId`, `level`, and `environmentId` referencing the catalogs.
  - `heroClasses.length` is 4 (`knight`, `ranger`, `mage`, `cleric`). `defaultHeroClassId` matches one of them.
  - `mapNodes` contains exactly one `Boss` entry. Every `connectedTo` id resolves to another node id. Every non-Shop node has a valid `encounterIndex`. Shop nodes use `-1`.
  - `endlessMode.enabled` is `true`. Pools reference only ids that also appear in `monsters` / `environments`.

- **`GET /battle/next-move`**
  - Minimal call (`monsterId` + HP / hero-HP fields) returns 200 with a `moveId` from that monster's move list.
  - Calling with `monsterMana=0` excludes any move whose `manaCost > 0`.
  - Calling with `monsterHealth` low (e.g. `monsterHealth=10` on a 100 HP monster) prefers a Heal-category move when one is in the monster's move list.
  - Calling with `heroHealth` very low (`heroHealth=5` on a 100 HP hero) prefers the highest-power affordable damaging move.
  - Calling with `monsterEffects=BuffDefense` and a move whose effect kind is `BuffDefense` and target `Self` should not return that redundant move (unless it is the only legal pick).
  - Unknown `monsterId` returns 200 with `null` / empty (the bot fails closed; client falls back gracefully).

## Standard Run Tests

Start a Standard Run from the Main Menu.

- **Class selection** — all four classes show localized name and description; selecting each one seeds the hero with the expected stats and equipped moves on Run Overview.
- **Run Overview (map mode)** — only the starting node is *Available*; everything else is *Locked*. Boss / Elite / Shop highlights match node type.
- **Battle node** — clicking an available battle node opens the Battle scene with the right monster name, level, and environment. Clearing a node moves it to *Cleared* and unlocks every node listed in its `connectedTo`.
- **Shop node** — opens the existing Shop scene; pressing Back marks the shop node *Cleared* and unlocks its connections.
- **Boss node (Dragon)** — defeating the boss triggers the run-victory banner; remaining map buttons go non-interactive.
- **Defeat** — losing a battle keeps the same map node selectable; no XP / gold loss.
- **Battle systems**
  - All five move categories resolve: Physical, Magic, Heal, Buff, Debuff.
  - Status effects tick at end of turn (Bleed, Poison) and decrement durations.
  - Buff / Debuff modifiers apply to effective stats during the battle.
  - Mana and HP costs are paid; unaffordable moves disable on the hero side.
  - Environment modifiers reflect in damage / healing numbers (e.g. Training Fields adds +2 to physical hits).
- **Post-battle**
  - XP and gold gained match `rules.xpPerVictory` / `rules.goldPerVictory` for standard runs.
  - Item drops land in the inventory and auto-equip into a free slot when possible; duplicates are reported but not re-added.
  - Move learning auto-equips when a slot is free; duplicates are skipped.
  - Level-up triggers the attribute picker the right number of times when multiple thresholds cross in one battle.
- **Item Management** — equipping / unequipping changes the hero summary line on the screen; trinket cap of 2 is enforced.
- **Move Management** — the equipped slots and learned pool update correctly when swapping moves.
- **Shop** — buying an item subtracts gold and adds the item to inventory; stat-upgrade offers update the hero stats; "Need Xg" and "Owned" labels show the right state.

## Endless Run Tests

Start an Endless Run from the Main Menu.

- **Run Overview (endless panel)** — shows `Floor 1`, the next-enemy line, and a **Start Floor** button. The branching map is hidden.
- **Battle / Elite / Boss floors** — `Battle` floors draw from `monsterPool`; floor 5 (with `eliteEvery = 5`) is an Elite from `eliteMonsterPool`; floor 10 is a Boss from `bossMonsterPool` (Dragon by default).
- **Shop floors** — floor 3 / 6 / 9 (with `shopEvery = 3`, when not overridden by a Boss / Elite slot) opens the Shop scene; pressing Back advances the floor.
- **Floor scaling** — encounter level rises by 1 every `levelIncreaseEvery` floors (default 2).
- **Reward scaling** — gold and XP follow the linear formulas (`rewardGoldBase + (floor-1)*rewardGoldPerFloor`, `xpBase + (floor-1)*xpPerFloor`); reward log lines reflect the increasing values.
- **Defeat** — endless run ends; the panel switches to the defeat summary with the floor reached and a **Return to Menu** button. No further floors are reachable.
- **Best floor tracking** — `EndlessBestFloor` updates when a floor is cleared.

## Save / Load Tests

- **Save & Exit on Run Overview (standard run)** — clears Save & Exit, returns to Main Menu, **Continue Run** and **Delete Save** appear. Continue refetches `/run/config`, restores hero, gold, inventory, equipped items, and map progression. Cleared / Available nodes match the saved state.
- **Save & Exit on Move Management / Item Management / Shop** — same behavior: save lands on disk, Main Menu shows the buttons, Continue restores correctly.
- **Save & Exit during Endless Mode** — Continue restores `EndlessFloor`, `EndlessBestFloor`, `EndlessFloorType`, and the synthesized next-enemy when the saved monster id is still in the catalog. If the saved monster is missing, the next floor is rerolled instead of crashing.
- **Delete Save** — file under `Application.persistentDataPath/nordeus_challenge_save.json` is removed; the two save-related buttons disappear immediately.
- **Malformed save** — replace the JSON with garbage; clicking **Continue Run** surfaces "Could not load save." and stays on Main Menu without crashing.
- **Stale ids** — hand-edit the save to reference a non-existent move / item / map node id; restore drops only the unknown ids and keeps the rest of the run intact. If no hero can be reconstructed, **Continue Run** stays on Main Menu with the same localized error.
- **No save** — clicking **Continue Run** when the file is missing surfaces "No save file found." and does not transition.

## Localization Tests

- **Language picker on Main Menu** — switching to Serbian Latin updates the Main Menu labels, status text, and the rest of the UI immediately. Persists across an editor restart (PlayerPrefs).
- **Dynamic data** — monster names, move names / descriptions, item names / descriptions, environment names / descriptions, shop offer text, level-up choice text, and class names / descriptions all swap to Serbian Latin.
- **Latin only** — confirm no Cyrillic characters appear in any Serbian string.
- **Fallbacks** — temporarily remove a key from `sr-Latn.json`; the corresponding label falls back to the English entry. Remove the key from `en.json` too; the label falls back to the server-provided English string. Neither path crashes.
- **Format strings** — placeholders (`{0}`, `{1}`) render correctly in both languages (e.g. "Floor {0}" / "Sprat {0}", "+{0} XP." / "+{0} iskustva.").

## Regression Checks

After any of the bonus features is exercised, walk a quick regression pass:

- Standard run still completes end-to-end with the legacy linear `encounters` list when `mapNodes` are absent (server can be tested by temporarily clearing `MapNodes`).
- Endless Mode toggling off (`endlessMode.enabled = false`) hides the Endless Run flow on the Main Menu without affecting Standard Run.
- Hero classes — picking each class produces the expected starting stats and starting moves on the Run Overview.
- Items, Move Management, and Shop screens render correctly with eight encounters worth of drops and offers.
- Environment modifiers still apply when the encounter is reached via the branching map (not just the linear list).
- Save & Exit followed by Continue produces the same in-game state on every safe screen.
- Switching the language mid-run does not break formatting in the battle log, the level-up panel, or the run-overview hero summary.
- Defeat in standard mode replays the same encounter; defeat in endless mode ends the run.
- `dotnet build` from `server/NordeusChallenge.Api` succeeds with 0 warnings and 0 errors.
- Unity Editor logs no `NullReferenceException` or `MissingReferenceException` during a full standard run, a full endless ramp through floor 10, and a save / continue cycle.
