# Data Model

Core domain objects used by both the client and server. Field types are given at the conceptual level (int, string, float, enum). The same shapes are used on the wire and in memory, with small runtime-only extensions noted where relevant.

## Stats

Role: the four numeric attributes that drive combat.

Fields:
- `maxHealth` (int) — hit points ceiling.
- `maxMana` (int) — mana pool ceiling. `0` means the entity has no mana resource.
- `attack` (int) — scales physical damage dealt.
- `defense` (int) — reduces incoming physical damage.
- `magic` (int) — scales magic damage dealt and healing.

Stats are used both as base values (on monsters) and as the current values on a battling entity after level-ups and buffs.

## Move

Role: an action a hero or monster can take on its turn.

Fields:
- `id` (string) — stable identifier.
- `name` (string) — display name.
- `category` (enum) — `Physical`, `Magic`, `Buff`, `Debuff`, `Heal`.
- `power` (int) — base power used by the damage or heal formula. `0` for pure buffs/debuffs.
- `manaCost` (int | null) — optional mana cost paid by the caster on use. Omitted when the move is free.
- `hpCost` (int | null) — optional HP cost paid by the caster on use. Omitted when the move is free. The caster cannot be reduced below `1` HP by paying this cost.
- `effect` (MoveEffect | null) — optional secondary effect applied alongside or instead of damage.
- `description` (string) — short UI text.

## MoveEffect

Role: a structured description of what a move does beyond its base damage. Kept intentionally narrow for the prototype.

Fields:
- `kind` (enum) — one of:
  - `BuffAttack`, `BuffDefense`, `BuffMagic`, `DebuffAttack`, `DebuffDefense`, `DebuffMagic` — stat-modifier effects.
  - `Heal` — flat heal applied immediately.
  - `Bleed`, `Poison` — damage-over-time on the target; `amount` is dealt at the end of each turn the effect is active.
  - `DamageIncrease`, `DamageReduction` — flat modifiers applied after the base damage formula. `DamageIncrease` adds `amount` to outgoing damage; `DamageReduction` subtracts `amount` from incoming damage. Both clamp the final damage to a minimum of `1`.
- `amount` (int) — meaning depends on `kind` (stat delta, heal amount, per-turn DoT damage, or flat damage delta).
- `durationTurns` (int) — for everything except `Heal`, how many turns the effect remains active.
- `target` (enum) — `Self` or `Opponent`.

Heals are treated as an effect rather than damage so the same `Move` shape covers every action.

## ActiveStatusEffect

Role: a buff or debuff currently applied to a battling entity. Exists only during a battle on the client.

Fields:
- `sourceMoveId` (string) — which move applied it (for UI and debugging).
- `kind` (enum) — same enum as `MoveEffect.kind` (excluding `Heal`).
- `amount` (int) — signed delta applied to the affected stat, per-turn DoT damage, or flat damage delta, depending on `kind`.
- `turnsRemaining` (int) — decremented at the end of each turn; removed when it reaches 0.

A battling entity holds a list of these. Effective stats during a turn are base stats plus the sum of matching active effects.

## Monster

Role: an opponent entity the hero can face in an encounter.

Fields:
- `id` (string)
- `name` (string)
- `baseStats` (Stats) — values at level 1.
- `moves` (string[]) — ids of moves this monster can use.
- `itemDrops` (string[]) — ids of items this monster can drop on victory. The client picks from this list when awarding loot; an empty list means the monster never drops gear.

Monsters are scaled to an encounter's level on the client using the same per-level gains as the hero.

The monster catalog for the prototype: Goblin Warrior, Goblin Mage, Giant Spider, Witch, Dragon.

## Hero

Role: the player-controlled entity, persistent across the run. In the prototype the hero is the Knight.

Fields:
- `id` (string) — constant, e.g. `"knight"`.
- `name` (string)
- `level` (int)
- `xp` (int) — progress toward the next level.
- `stats` (Stats) — current stats including level-up gains.
- `equippedMoves` (string[]) — ids of moves usable in battle; up to `equippedMoveSlots` entries (4 in the prototype).
- `learnedMovePool` (string[]) — ids of every move the hero has ever learned.

The hero's current HP is not stored on this object between battles; it is reset to `stats.maxHealth` at the start of every encounter.

## EquippedMoveSet

Role: a conceptual view over the hero's currently equipped moves. Not a separate entity on the wire; it is the `Hero.equippedMoves` array together with the rule that its length is capped at `equippedMoveSlots`.

## LearnedMovePool

Role: the full set of moves the hero has unlocked across a run. Also not a separate entity on the wire; it is `Hero.learnedMovePool`. The Move Management screen lets the player swap entries between the pool and the equipped set.

## Encounter

Role: a single battle slot in the run's encounter list.

Fields:
- `index` (int) — position in the run, starting at 0.
- `monsterId` (string) — reference into the monster catalog.
- `level` (int) — the level at which the monster is instantiated.
- `environmentId` (string) — reference into the environment catalog. Selects the battlefield used for this encounter.

## BattleEnvironment

Role: a battlefield that flavors a single encounter with small, readable combat modifiers. Returned as part of `RunConfig` and looked up on the client by `Encounter.environmentId`.

Fields (all integer modifiers, default `0` meaning "no effect"):
- `id` (string)
- `name` (string)
- `description` (string)
- `physicalDamageBonus` (int) — added to outgoing Physical move damage by either combatant on this battlefield.
- `magicDamageBonus` (int) — added to outgoing Magic move damage by either combatant.
- `healingBonus` (int) — added to the amount restored by `Heal` effects. Can be negative to dampen healing.
- `endOfTurnDamage` (int) — flat HP loss applied to both combatants at the end of every turn (in addition to DoTs). Bypasses Defense and `DamageReduction`.
- `poisonBonusTurns` (int) — extra turns appended to the duration of newly applied `Poison` effects on this battlefield.
- `bleedBonusDamage` (int) — added to each `Bleed` tick's per-turn damage.
- `manaRegenBonus` (int) — mana restored to both combatants at the end of every turn.

Environments are deliberately symmetric: they apply to both hero and monster so the battlefield reads as a place, not as a hero-only buff.

## RunConfig

Role: the payload returned by `GET /run/config`. Self-contained description of an entire run.

Fields:
- `runId` (string) — identifier for the run instance.
- `hero` (Hero) — starting hero state.
- `encounters` (Encounter[]) — ordered list of battles.
- `monsters` (Monster[]) — catalog referenced by encounters.
- `moves` (Move[]) — catalog referenced by hero and monsters.
- `environments` (BattleEnvironment[]) — catalog referenced by encounters.
- `items` (Item[]) — catalog referenced by `monsters[].itemDrops` and by the hero's inventory.
- `shopOffers` (ShopOffer[]) — catalog of in-run shop entries the client renders.
- `rules` (object) — tunable constants: `buffDurationTurns`, `xpPerVictory`, `xpPerLevel`, `statGainPerLevel` (Stats delta, retained for backward compatibility), `equippedMoveSlots`, `levelUpChoices` (LevelUpChoice[]), `equippedItemSlots` (string→int map of cap per slot), `goldPerVictory` (int).

## Item

Role: a piece of equippable gear. Items are stateless catalog entries returned in `RunConfig.items`; ownership and equipped state live entirely on the client during a run.

Fields:
- `id` (string) — stable identifier.
- `name` (string) — display name.
- `description` (string) — short UI text.
- `slot` (enum) — `weapon`, `armor`, or `trinket`.
- `rarity` (enum) — `common`, `uncommon`, `rare`, `epic`, `legendary`. Used as a UI hint (color/badge); the server does not gate behavior on rarity.
- `statBonuses` (ItemStatBonus[]) — flat integer bonuses applied while equipped.

## ItemStatBonus

Role: a single flat stat increase granted by an equipped item.

Fields:
- `stat` (enum) — `maxHealth`, `maxMana`, `attack`, `defense`, or `magic`.
- `amount` (int) — flat amount added to the matching base stat. May be negative.

While an item is equipped, the sum of its `statBonuses` is added to the hero's base stats. These bonuses stack additively with level-up gains and with active battle effects (buffs, debuffs, `DamageIncrease`, `DamageReduction`).

## ShopOffer

Role: a single purchasable entry in the in-run shop. Returned in `RunConfig.shopOffers`. Two flavors share one shape; fields not relevant to a given `type` are empty / zero.

Fields:
- `id` (string) — stable identifier.
- `name` (string) — display name.
- `description` (string) — UI text.
- `price` (int) — cost in gold.
- `type` (enum) — `Item` or `StatUpgrade`.
- `itemId` (string) — for `Item` offers, the id of an entry in `items`. Empty for `StatUpgrade`.
- `stat` (enum) — for `StatUpgrade` offers, one of `maxHealth`, `maxMana`, `attack`, `defense`, `magic`. Empty for `Item`.
- `amount` (int) — for `StatUpgrade` offers, the flat increase added to the chosen stat. `0` for `Item`.

Buying an `Item` offer adds the referenced item to the hero's inventory (no-op if already owned). Buying a `StatUpgrade` offer raises the matching base hero stat permanently for the rest of the run. Either way, the offer's `price` is debited from the run's gold total. The server is not notified — the shop is fully client-resolved.

## Gold (client-only)

Role: the run's spendable currency. Not part of the wire payload.

The client maintains a single integer `gold` value during a run:
- Initialized to `0` on a new run.
- Increased by `rules.goldPerVictory` after every battle victory (defeats grant nothing).
- Decreased by `ShopOffer.price` on each successful purchase. A purchase is rejected if `gold < price`.
- Reset to `0` when a new run begins. Like XP, items, and learned moves, gold is run-scoped only.

## Inventory (client-only)

The hero's inventory is not part of the wire payload. The client maintains, during a run:
- An owned-items list, populated when monsters drop items on victory.
- An equipped-items map keyed by slot (`weapon`, `armor`, `trinket`). The map respects the per-slot caps in `rules.equippedItemSlots` (default `{ "weapon": 1, "armor": 1, "trinket": 2 }`).

Equipping or unequipping changes the hero's effective base stats immediately. Items are run-scoped only — there is no persistence across runs.

## LevelUpChoice

Role: a single attribute increase the player can pick when the hero levels up. The client renders the list from `rules.levelUpChoices`, lets the player pick one, and applies its stat delta to the hero during the post-battle flow.

Fields:
- `id` (string) — stable identifier (e.g. `"health"`, `"attack"`).
- `name` (string) — short display label (e.g. `"+4 Attack"`).
- `description` (string) — UI text explaining the pick.
- `stat` (enum) — which stat to increase. One of `health` (MaxHealth), `mana` (MaxMana), `attack`, `defense`, `magic`.
- `amount` (int) — flat amount added to the chosen stat.

Older clients that still consume `rules.statGainPerLevel` keep working unchanged; newer clients drive level-ups from `levelUpChoices` and ignore `statGainPerLevel`.

## BattleState

Role: the live state of an ongoing battle on the client. Also the shape the client uses to parameterize `GET /battle/next-move` (subset of fields, flattened into query params).

Fields:
- `encounterIndex` (int)
- `turn` (int) — increments each full round, starting at 1.
- `hero`:
  - `stats` (Stats) — current base stats.
  - `health` (int) — current HP.
  - `mana` (int) — current MP.
  - `activeEffects` (ActiveStatusEffect[])
- `monster`:
  - `monsterId` (string)
  - `level` (int)
  - `stats` (Stats) — scaled stats.
  - `health` (int)
  - `mana` (int) — current MP.
  - `activeEffects` (ActiveStatusEffect[])

Both combatants start each battle at `stats.maxHealth` and `stats.maxMana`. Mana, like HP, does not carry over between encounters.

## BattleResult

Role: the outcome of a finished battle, consumed by the Post-Battle flow.

Fields:
- `encounterIndex` (int)
- `outcome` (enum) — `Victory` or `Defeat`.
- `turnsTaken` (int)
- `xpAwarded` (int) — `0` on defeat.
- `leveledUp` (bool)
- `newLevel` (int | null)
- `learnedMoveId` (string | null) — a move learned after victory, if any.

On `Defeat`, no progression is applied and the same encounter remains the current one, available to replay. On `Victory`, the client advances to the next encounter; if there is none, the run is complete.
