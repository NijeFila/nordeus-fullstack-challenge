# Battle Rules

The rules the prototype follows. Formulas are kept simple, explicit, and integer-based so the client and server stay in lockstep.

## Turn Flow

1. A battle begins with the hero and monster at full or carried-over HP (hero HP carries between encounters; monster always starts at full).
2. Each turn has two phases, resolved in order:
   1. **Hero phase** — the player picks one of the hero's equipped moves. The move resolves immediately.
   2. **Monster phase** — the client calls `GET /battle/next-move` with the current state and resolves the returned move.
3. At the end of each turn:
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

Damage is applied as `defender.health = max(0, defender.health - damage)`.

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

## Win / Loss Conditions

- **Battle victory** — monster HP reaches `0`.
- **Battle defeat** — hero HP reaches `0`.
- **Run victory** — the hero wins the final encounter in `RunConfig.encounters`.
- **Run defeat** — the hero is defeated in any encounter. The run ends; the player returns to the Main Menu.

Hero HP carries over between encounters on victory; it is not auto-restored.

## XP and Leveling

- On victory, the hero gains `rules.xpPerVictory` XP (default 25).
- On defeat, no XP is awarded.
- When `hero.xp >= rules.xpPerLevel` (default 100):
  - `hero.level` increases by 1.
  - `hero.xp` is reduced by `xpPerLevel` (carry-over preserved).
  - Each stat in `hero.stats` increases by the corresponding entry in `rules.statGainPerLevel`.
  - `hero.health` is fully restored to the new `maxHealth`.
- Only one level-up is granted per battle. Any additional XP beyond one level's worth is discarded for the prototype.

## Move Learning After Victory

- On victory, one random move from the defeated monster's `moves` list is considered for learning.
- If the hero's `learnedMovePool` already contains that move, no new move is learned this battle.
- Otherwise, the move is added to `learnedMovePool`.
- If `equippedMoves` has a free slot (fewer than `equippedMoveSlots` entries), the new move is auto-equipped.
- If all slots are full, the move is only added to the pool; the Move Management screen is where the player swaps it in.

## Monster Scaling

A monster in an encounter with `level = L` is instantiated by applying `(L - 1)` level-ups to its `baseStats`, using the same `statGainPerLevel` rule as the hero. Monsters do not earn XP during battle.

## Opponent Move Selection

The server's `GET /battle/next-move` returns a move id drawn from the monster's declared move list. The prototype uses a simple selection rule:

- If the monster has a healing move available and its `monsterHealth / monsterMaxHealth < 0.35`, return the healing move.
- Otherwise, return a uniformly random move from the monster's list.

Selection is deterministic only in the sense that it respects the rule above; the random fallback is intentionally non-deterministic so repeat battles feel different.

## Prototype Simplifications

Intentional simplifications for the challenge scope:

- No elemental types, accuracy rolls, critical hits, or miss chance.
- No turn-order stat (speed); hero always acts first.
- Integer math throughout, with `max(1, …)` floors on damage.
- Buffs/debuffs have a fixed 2-turn duration and do not stack beyond one instance per `(source, kind)` pair.
- Only one level-up is granted per battle; overflow XP is dropped.
- Hero HP carries between battles but is restored on level-up; there is no separate healing mechanic between encounters.
- Monster move selection is deliberately shallow — a low-HP heal rule plus uniform random — so behavior is easy to reason about and extend.
- No persistence: starting a new run always begins from the server's default configuration.
