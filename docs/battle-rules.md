# Battle Rules

The rules the prototype follows. Formulas are kept simple, explicit, and integer-based so the client and server stay in lockstep.

## Turn Flow

1. A battle begins with both the hero and the monster at full HP and full mana. Hero HP and mana are reset to `stats.maxHealth` and `stats.maxMana` at the start of every encounter; there is no HP or mana carry-over between battles.
2. Each turn has two phases, resolved in order:
   1. **Hero phase** — the player picks one of the hero's equipped moves. The move resolves immediately.
   2. **Monster phase** — the client calls `GET /battle/next-move` with the current state and resolves the returned move.
3. At the end of each turn:
   - Damage-over-time effects (`Bleed`, `Poison`) tick on each affected combatant: `health -= amount`, clamped to `0`.
   - Durations on all `ActiveStatusEffect`s are decremented by 1.
   - Effects that reach `0` are removed.
   - The turn counter is incremented.
4. The battle ends as soon as either combatant's HP reaches `0`. HP never goes negative; it is clamped to `0`.

If both HPs would drop to 0 on the same turn, the side whose move resolved first wins (hero phase before monster phase).

## Effective Stats

During damage and healing calculations, the effective value of a stat is:

```
effective(stat) = baseStat + sum(amount of active effects targeting that stat)
```

Effective stats are clamped to a minimum of `1` to avoid divide-by-zero or negative scaling.

## Resource Costs

Some moves declare a `manaCost` and / or `hpCost`. When such a move is used:

- The cost is paid by the caster the moment the move resolves.
- A move is not selectable if the caster cannot pay both costs in full. `manaCost` must be `<= caster.mana`. `hpCost` must be strictly less than `caster.health` so the caster cannot kill themselves casting it.
- Mana paid is subtracted: `caster.mana = caster.mana - manaCost`.
- HP paid is subtracted directly and bypasses Defense and `DamageReduction`.
- Mana never regenerates mid-battle in the prototype; both sides must spend their pool deliberately.

## Damage

### Physical moves

```
rawDamage = power + attacker.effectiveAttack - defender.effectiveDefense
damage    = max(1, rawDamage)
```

Physical damage is reduced by the defender's Defense and always deals at least 1.

### Magic moves

```
damage = power + attacker.effectiveMagic
```

Magic damage ignores Defense entirely.

Before being applied, damage is adjusted by any active `DamageIncrease` on the attacker and `DamageReduction` on the defender:

```
finalDamage = max(1, damage + sum(attacker.DamageIncrease) - sum(defender.DamageReduction))
```

`finalDamage` is then applied as `defender.health = max(0, defender.health - finalDamage)`.

## Healing

A move with a `Heal` effect restores HP to the `target`:

```
healed = effect.amount + caster.effectiveMagic / 2   // integer division
newHP  = min(target.maxHealth, target.health + healed)
```

Healing cannot exceed `maxHealth`. A `Heal` move does not deal damage even if it carries a `power` field; `power` is ignored when the category is `Heal`.

## Buffs and Debuffs

- Buffs and debuffs use `MoveEffect` with `durationTurns = 2` (configurable via `rules.buffDurationTurns`).
- On application, an `ActiveStatusEffect` is appended to the target's effect list with `turnsRemaining = durationTurns`.
- Multiple effects stack additively. Applying the same effect twice refreshes duration but does not stack amount a second time (the newer instance replaces the older one with the same `sourceMoveId` and `kind`).
- Effects tick down at the end of the turn in which they were applied.

Result: a buff or debuff cast on turn N is active for turn N and turn N+1, then expires.

## Damage-over-time (Bleed, Poison)

`Bleed` and `Poison` are status effects that sit on the target as `ActiveStatusEffect`s and deal `amount` damage at the end of every turn while active. They use the same duration mechanic as buffs and ignore Defense and `DamageReduction`. Multiple sources of the same kind do not stack on the same target — re-application refreshes duration but does not double the per-turn tick.

## Flat damage modifiers (DamageIncrease, DamageReduction)

`DamageIncrease` and `DamageReduction` are buff-shaped effects that do not change a base stat. Instead, they adjust the final damage of a Physical or Magic move at the moment it resolves (see the Damage section). They follow the same duration rules as other buffs.

## Win / Loss Conditions

- **Battle victory** — monster HP reaches `0`. The hero advances to the next encounter.
- **Battle defeat** — hero HP reaches `0`. The run does not end: the same encounter remains the current one and can be replayed. Nothing permanent is lost on defeat — the hero keeps their level, XP, stats, learned moves, and equipped moves. No XP is awarded.
- **Run victory** — the hero wins the final encounter in `RunConfig.encounters`. The run ends successfully.

The hero's HP is always reset to full at the start of each encounter, including replays.

## XP and Leveling

- On victory, the hero gains `rules.xpPerVictory` XP (default 25).
- On defeat, no XP is awarded.
- When `hero.xp >= rules.xpPerLevel` (default 100):
  - `hero.level` increases by 1.
  - `hero.xp` is reduced by `xpPerLevel`; any remainder carries over toward the next level.
  - Each stat in `hero.stats` increases by the corresponding entry in `rules.statGainPerLevel`.
- For simplicity, at most one level-up is resolved per battle. If the hero would have enough XP for more than one level-up in a single battle (rare with the default numbers), the remaining XP is retained and a further level-up is resolved after the next victory. No XP is discarded.

## Move Learning After Victory

- On victory, one random move from the defeated monster's `moves` list is considered for learning.
- If the hero's `learnedMovePool` already contains that move, no new move is learned this battle.
- Otherwise, the move is added to `learnedMovePool`.
- If `equippedMoves` has a free slot (fewer than `equippedMoveSlots` entries), the new move is auto-equipped.
- If all slots are full, the move is only added to the pool; the Move Management screen is where the player swaps it in.

## Monster Scaling

A monster in an encounter with `level = L` is instantiated by applying `(L - 1)` level-ups to its `baseStats`, using the same `statGainPerLevel` rule as the hero. Monsters do not earn XP during battle.

## Opponent Move Selection

The server's `GET /battle/next-move` returns a move id drawn from the monster's declared move list. The prototype uses a small layered rule, applied in order:

1. **Affordability filter.** Drop any move whose `manaCost` exceeds `monsterMana`, or whose `hpCost` would reduce the monster to `0` HP or below.
2. **Heal when low.** If `monsterHealth / monsterMaxHealth < 0.35` and an affordable Heal move is available, use it.
3. **Finishing blow.** If `heroHealth / heroMaxHealth < 0.25`, pick the highest-`power` affordable Physical or Magic move.
4. **Skip redundant effects.** From what's left, drop moves whose `effect.kind` is already active on the relevant target (a `Self` buff already on the monster, or an `Opponent` debuff/DoT already on the hero). Active effects come from the optional `monsterEffects` and `heroEffects` query parameters.
5. **Mild offensive bias.** Among the remaining moves, pick a damaging move with probability ~0.7, otherwise pick uniformly from all remaining options.

Selection is deterministic only in the sense that it respects this layered rule; the random fallback is intentionally non-deterministic so repeat battles feel different. If the optional state parameters are omitted, the corresponding step degrades gracefully — an unknown `monsterMana` simply skips the affordability check so older clients still get a valid move.

## Prototype Simplifications

Intentional simplifications for the challenge scope:

- No elemental types, accuracy rolls, critical hits, or miss chance.
- No turn-order stat (speed); hero always acts first.
- Integer math throughout, with `max(1, …)` floors on damage.
- Buffs/debuffs have a fixed 2-turn duration and do not stack beyond one instance per `(source, kind)` pair.
- At most one level-up is resolved per battle; any remaining XP carries over to the next victory rather than being discarded.
- Hero HP is reset to full at the start of every encounter. There is no HP carry-over and no between-battle healing mechanic.
- Defeat does not end the run. The current encounter is simply replayed. No XP is gained, and no progression is rolled back.
- Monster move selection is deliberately shallow — a low-HP heal rule plus uniform random — so behavior is easy to reason about and extend.
- No persistence: starting a new run always begins from the server's default configuration.
