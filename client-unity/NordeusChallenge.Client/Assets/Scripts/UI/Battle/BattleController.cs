using System.Collections;
using System.Collections.Generic;
using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Networking;
using NordeusChallenge.Client.Runtime;
using NordeusChallenge.Client.UI.Common;
using NordeusChallenge.Client.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    public class BattleController : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string baseUrl = "http://localhost:5046";

        [Header("Visual")]
        [SerializeField] private VisualCatalog visualCatalog;

        [Header("Hero UI")]
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text heroStatsText;
        [SerializeField] private TMP_Text heroHealthText;
        [SerializeField] private TMP_Text heroManaText;
        [SerializeField] private TMP_Text heroStatusEffectsText;
        [SerializeField] private HpBarView heroHpBar;
        [SerializeField] private CombatantVisuals heroVisuals;

        [Header("Monster UI")]
        [SerializeField] private TMP_Text monsterNameText;
        [SerializeField] private TMP_Text monsterStatsText;
        [SerializeField] private TMP_Text monsterHealthText;
        [SerializeField] private TMP_Text monsterManaText;
        [SerializeField] private TMP_Text monsterStatusEffectsText;
        [SerializeField] private HpBarView monsterHpBar;
        [SerializeField] private CombatantVisuals monsterVisuals;

        [Header("Moves")]
        [SerializeField] private Transform movesContainer;
        [SerializeField] private MoveButtonView moveButtonPrefab;
        [SerializeField] private TMP_Text moveInfoText;
        [SerializeField] private MoveInfoPanelView moveInfoPanel;

        [Header("Feedback")]
        [SerializeField] private BattleFeedbackView feedbackView;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private TMP_Text environmentText;
        [SerializeField] private Button backButton;

        [Header("Environment Background (optional)")]
        [SerializeField] private Image environmentBackgroundImage;
        [SerializeField] private BattleEnvironmentVisualCatalog environmentVisualCatalog;

        [Header("Level Up")]
        [SerializeField] private LevelUpChoicePanelView levelUpPanel;

        private BattleApiClient _battleApi;

        private HeroDto _hero;
        private MonsterDto _monster;
        private EncounterDto _encounter;
        private BattleEnvironmentDto _environment;
        private StatsDto _monsterScaledStats;
        // Snapshot of hero stats for this battle including equipped item bonuses.
        // Permanent hero stats are never mutated by equipping items.
        private StatsDto _heroBattleStats;

        private int _heroHealth;
        private int _heroMaxHealth;
        private int _heroMana;
        private int _heroMaxMana;
        private int _monsterHealth;
        private int _monsterMaxHealth;
        private int _monsterMana;
        private int _monsterMaxMana;
        private int _turn;
        private bool _battleOver;
        private bool _inputLocked;

        private const int MaxLogLines = 8;
        private const int ManaRegenPerTurn = 5;

        private readonly List<ActiveEffect> _heroEffects = new();
        private readonly List<ActiveEffect> _monsterEffects = new();
        private readonly List<MoveButtonView> _spawnedMoveButtons = new();
        private readonly Queue<string> _log = new();

        private void Start()
        {
            _battleApi = new BattleApiClient(baseUrl);

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            SetStatus(string.Empty);
            SetMoveInfo(string.Empty);
            _log.Clear();
            SetLog(string.Empty);

            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null)
            {
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            _hero = GameSession.Instance.CurrentHero ?? run.hero;
            // Endless mode synthesizes its encounter on the session; standard
            // mode looks one up by SelectedEncounterIndex. ResolveCurrentEncounter
            // hides the branch.
            _encounter = GameSession.Instance.ResolveCurrentEncounter();
            if (_encounter == null)
            {
                SetStatus("No encounter selected.");
                return;
            }

            _monster = GameSession.Instance.GetMonsterById(_encounter.monsterId);
            if (_monster == null)
            {
                SetStatus("Unknown monster.");
                return;
            }

            _turn = 1;
            _environment = GameSession.Instance.GetEnvironmentById(_encounter.environmentId);
            _monsterScaledStats = ScaleMonsterStats(_monster.baseStats, _encounter.level, run.rules);
            _heroBattleStats = GameSession.Instance.GetHeroStatsWithItemBonuses();
            RenderEnvironment();
            ApplyEnvironmentBackground();

            _heroMaxHealth = _heroBattleStats.maxHealth;
            _heroHealth = _heroMaxHealth;
            _heroMaxMana = _heroBattleStats.maxMana;
            _heroMana = _heroMaxMana;

            _monsterMaxHealth = _monsterScaledStats.maxHealth;
            _monsterHealth = _monsterMaxHealth;
            _monsterMaxMana = _monsterScaledStats.maxMana;
            _monsterMana = _monsterMaxMana;

            RenderHero();
            RenderMonster();
            if (heroHpBar != null) heroHpBar.SetImmediate(_heroHealth, _heroMaxHealth);
            if (monsterHpBar != null) monsterHpBar.SetImmediate(_monsterHealth, _monsterMaxHealth);
            RenderMoves(_hero);
            RefreshAffordability();
            // Battle intro: just the encounter line. The environment is shown
            // in its dedicated panel (RenderEnvironment) so we don't repeat it
            // in the rolling battle log.
            AppendLog(string.Format(Loc.Tr("battle.appears", "A wild {0} appears."), LocalizedNames.Name(_monster)));
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        // ---------- Turn flow ----------

        private void OnMoveSelected(MoveDto move)
        {
            if (_battleOver || _inputLocked || move == null)
            {
                return;
            }

            if (!CanAfford(move, isHero: true))
            {
                AppendLog(string.Format(
                    Loc.Tr("battle.move_unavailable", "{0} is unavailable: {1}."),
                    LocalizedNames.Name(move),
                    AffordabilityReason(move, isHero: true)));
                return;
            }

            StartCoroutine(RunTurn(move));
        }

        private IEnumerator RunTurn(MoveDto heroMove)
        {
            _inputLocked = true;
            SetMovesInteractable(false);

            // Clear the previous turn's lines so the battle log only shows the
            // current exchange. End-of-battle summaries are appended after this
            // turn resolves, so they remain readable.
            ClearLog();

            ResolveHeroMove(heroMove);
            RefreshCombatantsUi();

            if (_monsterHealth <= 0)
            {
                EndBattle(true);
                yield break;
            }

            yield return StartCoroutine(ResolveMonsterTurn());
            RefreshCombatantsUi();

            if (_heroHealth <= 0)
            {
                EndBattle(false);
                yield break;
            }

            ApplyDoTTicks();
            RefreshCombatantsUi();

            if (_heroHealth <= 0)
            {
                EndBattle(false);
                yield break;
            }
            if (_monsterHealth <= 0)
            {
                EndBattle(true);
                yield break;
            }

            ApplyEnvironmentEndOfTurn();
            RefreshCombatantsUi();

            if (_heroHealth <= 0)
            {
                EndBattle(false);
                yield break;
            }
            if (_monsterHealth <= 0)
            {
                EndBattle(true);
                yield break;
            }

            TickEffectsAtEndOfTurn();
            RegenerateMana();
            _turn++;
            RefreshCombatantsUi();
            RefreshAffordability();

            _inputLocked = false;
            SetMovesInteractable(true);
        }

        private void ResolveHeroMove(MoveDto move)
        {
            PayCosts(move, isHero: true);
            ResolveMove(
                move,
                isHeroAttacker: true,
                attackerName: LocalizedNames.Name(_hero),
                defenderName: LocalizedNames.Name(_monster));
        }

        private IEnumerator ResolveMonsterTurn()
        {
            string selectedMoveId = null;
            string error = null;

            yield return _battleApi.GetNextMove(
                _monster.id,
                _encounter.level,
                _monsterHealth,
                _monsterMaxHealth,
                _heroHealth,
                _heroMaxHealth,
                _turn,
                _monsterMana,
                CollectKinds(_monsterEffects),
                CollectKinds(_heroEffects),
                id => selectedMoveId = id,
                e => error = e);

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(selectedMoveId))
            {
                AppendLog($"Monster hesitates ({error}).");
                yield break;
            }

            var move = GameSession.Instance.GetMoveById(selectedMoveId);
            if (move == null)
            {
                AppendLog($"Monster tried unknown move {selectedMoveId}.");
                yield break;
            }

            // The server already filtered by affordability when given mana,
            // but verify locally so HP/mana costs are paid honestly.
            if (!CanAfford(move, isHero: false))
            {
                AppendLog(string.Format(
                    Loc.Tr("battle.move_unavailable", "{0} is unavailable: {1}."),
                    LocalizedNames.Name(move),
                    Loc.Tr("battle.reason_unavailable", "unavailable")));
                yield break;
            }

            PayCosts(move, isHero: false);
            ResolveMove(
                move,
                isHeroAttacker: false,
                attackerName: LocalizedNames.Name(_monster),
                defenderName: LocalizedNames.Name(_hero));
        }

        // ---------- Resource costs ----------

        private bool CanAfford(MoveDto move, bool isHero)
        {
            int mana = isHero ? _heroMana : _monsterMana;
            int hp = isHero ? _heroHealth : _monsterHealth;
            if (move.manaCost > 0 && mana < move.manaCost) return false;
            if (move.hpCost > 0 && hp <= move.hpCost) return false;
            return true;
        }

        private string AffordabilityReason(MoveDto move, bool isHero)
        {
            int mana = isHero ? _heroMana : _monsterMana;
            int hp = isHero ? _heroHealth : _monsterHealth;
            if (move.manaCost > 0 && mana < move.manaCost) return Loc.Tr("battle.reason_no_mana", "not enough mana");
            if (move.hpCost > 0 && hp <= move.hpCost) return Loc.Tr("battle.reason_no_hp", "not enough HP");
            return Loc.Tr("battle.reason_unavailable", "unavailable");
        }

        private void PayCosts(MoveDto move, bool isHero)
        {
            string casterName = isHero ? LocalizedNames.Name(_hero) : LocalizedNames.Name(_monster);

            if (move.manaCost > 0)
            {
                if (isHero) _heroMana = Mathf.Max(0, _heroMana - move.manaCost);
                else _monsterMana = Mathf.Max(0, _monsterMana - move.manaCost);
                AppendLog(string.Format(Loc.Tr("battle.spent_mana", "{0} spent {1} mana."), casterName, move.manaCost));
            }

            if (move.hpCost > 0)
            {
                if (isHero) _heroHealth = Mathf.Max(1, _heroHealth - move.hpCost);
                else _monsterHealth = Mathf.Max(1, _monsterHealth - move.hpCost);
                AppendLog(string.Format(Loc.Tr("battle.paid_hp", "{0} paid {1} HP."), casterName, move.hpCost));
            }
        }

        private void RegenerateMana()
        {
            int regen = ManaRegenPerTurn + (_environment != null ? _environment.manaRegenBonus : 0);
            if (regen <= 0) regen = 0;
            if (_heroMaxMana > 0)
            {
                _heroMana = Mathf.Min(_heroMaxMana, _heroMana + regen);
            }
            if (_monsterMaxMana > 0)
            {
                _monsterMana = Mathf.Min(_monsterMaxMana, _monsterMana + regen);
            }
        }

        // ---------- Move resolution ----------

        private void ResolveMove(MoveDto move, bool isHeroAttacker, string attackerName, string defenderName)
        {
            int attackerAttack = EffectiveAttack(isHeroAttacker);
            int attackerMagic = EffectiveMagic(isHeroAttacker);
            int defenderDefense = EffectiveDefense(!isHeroAttacker);

            string moveName = LocalizedNames.Name(move);
            string usedDamage = Loc.Tr("battle.used_move_damage", "{0} used {1}. {2} took {3} damage.");
            string usedHeal = Loc.Tr("battle.used_move_heal", "{0} used {1}. Restored {2} HP.");
            string usedSimple = Loc.Tr("battle.used_move", "{0} used {1}.");

            switch (move.category)
            {
                case "Physical":
                {
                    int raw = move.power + attackerAttack - defenderDefense + EnvPhysicalBonus();
                    int damage = AdjustOutgoingDamage(raw, isHeroAttacker, defenderIsHero: !isHeroAttacker);
                    ApplyDamage(isHeroAttacker, damage);
                    PlayDamageFeedback(isHeroAttacker, damage);
                    AppendLog(string.Format(usedDamage, attackerName, moveName, defenderName, damage));
                    break;
                }
                case "Magic":
                {
                    int raw = move.power + attackerMagic + EnvMagicBonus();
                    int damage = AdjustOutgoingDamage(raw, isHeroAttacker, defenderIsHero: !isHeroAttacker);
                    ApplyDamage(isHeroAttacker, damage);
                    PlayDamageFeedback(isHeroAttacker, damage);
                    AppendLog(string.Format(usedDamage, attackerName, moveName, defenderName, damage));
                    break;
                }
                case "Heal":
                {
                    int healed = ApplyHeal(isHeroAttacker, move, attackerMagic);
                    PlayHealFeedback(isHeroAttacker, healed);
                    AppendLog(string.Format(usedHeal, attackerName, moveName, healed));
                    break;
                }
                case "Buff":
                case "Debuff":
                default:
                {
                    AppendLog(string.Format(usedSimple, attackerName, moveName));
                    break;
                }
            }

            if (move.effect != null)
            {
                ApplyEffect(move, isHeroAttacker);
            }
        }

        private int AdjustOutgoingDamage(int raw, bool attackerIsHero, bool defenderIsHero)
        {
            int increase = SumKind(attackerIsHero ? _heroEffects : _monsterEffects, "DamageIncrease");
            int reduction = SumKind(defenderIsHero ? _heroEffects : _monsterEffects, "DamageReduction");
            return Mathf.Max(1, raw + increase - reduction);
        }

        private void ApplyDamage(bool isHeroAttacker, int damage)
        {
            if (isHeroAttacker)
            {
                _monsterHealth = Mathf.Max(0, _monsterHealth - damage);
            }
            else
            {
                _heroHealth = Mathf.Max(0, _heroHealth - damage);
            }
        }

        private int ApplyHeal(bool isHeroAttacker, MoveDto move, int casterMagic)
        {
            int amount = Mathf.Max(0, (move.effect != null ? move.effect.amount : 0) + (casterMagic / 2) + EnvHealingBonus());
            if (isHeroAttacker)
            {
                int before = _heroHealth;
                _heroHealth = Mathf.Min(_heroMaxHealth, _heroHealth + amount);
                return _heroHealth - before;
            }
            else
            {
                int before = _monsterHealth;
                _monsterHealth = Mathf.Min(_monsterMaxHealth, _monsterHealth + amount);
                return _monsterHealth - before;
            }
        }

        private void ApplyEffect(MoveDto move, bool isHeroAttacker)
        {
            var effect = move.effect;
            if (effect == null || effect.kind == "Heal")
            {
                return;
            }

            bool targetIsHero = effect.target == "Self" ? isHeroAttacker : !isHeroAttacker;
            var list = targetIsHero ? _heroEffects : _monsterEffects;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].SourceMoveId == move.id && list[i].Kind == effect.kind)
                {
                    list.RemoveAt(i);
                }
            }

            int duration = effect.durationTurns;
            if (effect.kind == "Poison" && _environment != null)
            {
                duration += _environment.poisonBonusTurns;
            }

            list.Add(new ActiveEffect
            {
                SourceMoveId = move.id,
                Kind = effect.kind,
                Amount = effect.amount,
                TurnsRemaining = duration
            });
        }

        // ---------- Status ticking ----------

        private void ApplyDoTTicks()
        {
            int bleedBonus = _environment != null ? _environment.bleedBonusDamage : 0;
            int heroBleedTicks = CountKind(_heroEffects, "Bleed");
            int monsterBleedTicks = CountKind(_monsterEffects, "Bleed");
            int heroDot = SumKind(_heroEffects, "Bleed") + (heroBleedTicks * bleedBonus) + SumKind(_heroEffects, "Poison");
            int monsterDot = SumKind(_monsterEffects, "Bleed") + (monsterBleedTicks * bleedBonus) + SumKind(_monsterEffects, "Poison");

            if (heroDot > 0)
            {
                _heroHealth = Mathf.Max(0, _heroHealth - heroDot);
                AppendLog(string.Format(Loc.Tr("battle.dot_damage", "{0} suffers {1} damage from status effects."), LocalizedNames.Name(_hero), heroDot));
                if (feedbackView != null && heroVisuals != null)
                {
                    feedbackView.PlayHit(heroVisuals, heroDot);
                }
            }

            if (monsterDot > 0)
            {
                _monsterHealth = Mathf.Max(0, _monsterHealth - monsterDot);
                AppendLog(string.Format(Loc.Tr("battle.dot_damage", "{0} suffers {1} damage from status effects."), LocalizedNames.Name(_monster), monsterDot));
                if (feedbackView != null && monsterVisuals != null)
                {
                    feedbackView.PlayHit(monsterVisuals, monsterDot);
                }
            }
        }

        private void TickEffectsAtEndOfTurn()
        {
            TickList(_heroEffects);
            TickList(_monsterEffects);
        }

        private static void TickList(List<ActiveEffect> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                list[i].TurnsRemaining -= 1;
                if (list[i].TurnsRemaining <= 0)
                {
                    list.RemoveAt(i);
                }
            }
        }

        // ---------- Effective stats ----------

        private int EffectiveAttack(bool isHero)
        {
            int baseValue = isHero ? _heroBattleStats.attack : _monsterScaledStats.attack;
            int delta = SumEffectsFor(isHero ? _heroEffects : _monsterEffects, "BuffAttack", "DebuffAttack");
            return Mathf.Max(1, baseValue + delta);
        }

        private int EffectiveDefense(bool isHero)
        {
            int baseValue = isHero ? _heroBattleStats.defense : _monsterScaledStats.defense;
            int delta = SumEffectsFor(isHero ? _heroEffects : _monsterEffects, "BuffDefense", "DebuffDefense");
            return Mathf.Max(1, baseValue + delta);
        }

        private int EffectiveMagic(bool isHero)
        {
            int baseValue = isHero ? _heroBattleStats.magic : _monsterScaledStats.magic;
            int delta = SumEffectsFor(isHero ? _heroEffects : _monsterEffects, "BuffMagic", "DebuffMagic");
            return Mathf.Max(1, baseValue + delta);
        }

        private static int SumEffectsFor(List<ActiveEffect> list, string buffKind, string debuffKind)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == buffKind)
                {
                    total += list[i].Amount;
                }
                else if (list[i].Kind == debuffKind)
                {
                    total -= list[i].Amount;
                }
            }
            return total;
        }

        private static int SumKind(List<ActiveEffect> list, string kind)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == kind)
                {
                    total += list[i].Amount;
                }
            }
            return total;
        }

        private static int CountKind(List<ActiveEffect> list, string kind)
        {
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        private static List<string> CollectKinds(List<ActiveEffect> list)
        {
            var result = new List<string>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (!string.IsNullOrEmpty(list[i].Kind))
                {
                    result.Add(list[i].Kind);
                }
            }
            return result;
        }

        // ---------- Environment ----------

        private int EnvPhysicalBonus() => _environment != null ? _environment.physicalDamageBonus : 0;
        private int EnvMagicBonus() => _environment != null ? _environment.magicDamageBonus : 0;
        private int EnvHealingBonus() => _environment != null ? _environment.healingBonus : 0;

        private void RenderEnvironment()
        {
            if (environmentText == null) return;
            if (_environment == null)
            {
                environmentText.text = string.Empty;
                return;
            }

            string envName = LocalizedNames.Name(_environment);
            string envDesc = LocalizedNames.Description(_environment);
            environmentText.text = string.IsNullOrEmpty(envDesc)
                ? envName
                : $"<b>{envName}</b>\n{envDesc}";
        }

        private void ApplyEnvironmentEndOfTurn()
        {
            if (_environment == null || _environment.endOfTurnDamage <= 0) return;

            int dmg = _environment.endOfTurnDamage;
            _heroHealth = Mathf.Max(0, _heroHealth - dmg);
            _monsterHealth = Mathf.Max(0, _monsterHealth - dmg);
            AppendLog(string.Format(Loc.Tr("battle.environment_damage", "The {0} deals {1} damage to both combatants."), LocalizedNames.Name(_environment), dmg));

            if (feedbackView != null)
            {
                if (heroVisuals != null) feedbackView.PlayHit(heroVisuals, dmg);
                if (monsterVisuals != null) feedbackView.PlayHit(monsterVisuals, dmg);
            }
        }

        // ---------- UI ----------

        private void RenderHero()
        {
            if (heroNameText != null)
            {
                heroNameText.text = $"{LocalizedNames.Name(_hero)} ({Loc.Tr("label.level_short", "Lv")} {_hero.level})";
            }
            if (heroVisuals != null && heroVisuals.portrait != null && visualCatalog != null)
            {
                var sprite = visualCatalog.GetHeroPortrait(_hero.id);
                heroVisuals.portrait.sprite = sprite;
                heroVisuals.portrait.enabled = sprite != null;
            }
            UpdateHeroStatsText();
            UpdateHeroHealthText();
            UpdateHeroManaText();
            UpdateHeroStatusEffectsText();
        }

        private void RenderMonster()
        {
            if (monsterNameText != null)
            {
                monsterNameText.text = $"{LocalizedNames.Name(_monster)} ({Loc.Tr("label.level_short", "Lv")} {_encounter.level})";
            }
            if (monsterVisuals != null && monsterVisuals.portrait != null && visualCatalog != null)
            {
                var sprite = visualCatalog.GetMonsterPortrait(_monster.id);
                monsterVisuals.portrait.sprite = sprite;
                monsterVisuals.portrait.enabled = sprite != null;
            }
            UpdateMonsterStatsText();
            UpdateMonsterHealthText();
            UpdateMonsterManaText();
            UpdateMonsterStatusEffectsText();
        }

        private void RefreshCombatantsUi()
        {
            UpdateHeroStatsText();
            UpdateMonsterStatsText();
            UpdateHeroHealthText();
            UpdateMonsterHealthText();
            UpdateHeroManaText();
            UpdateMonsterManaText();
            UpdateHeroStatusEffectsText();
            UpdateMonsterStatusEffectsText();
            if (heroHpBar != null) heroHpBar.SetValue(_heroHealth, _heroMaxHealth);
            if (monsterHpBar != null) monsterHpBar.SetValue(_monsterHealth, _monsterMaxHealth);
        }

        private void PlayDamageFeedback(bool isHeroAttacker, int damage)
        {
            if (feedbackView == null || damage <= 0) return;
            var target = isHeroAttacker ? monsterVisuals : heroVisuals;
            feedbackView.PlayHit(target, damage);
        }

        private void PlayHealFeedback(bool isHeroAttacker, int amount)
        {
            if (feedbackView == null || amount <= 0) return;
            var target = isHeroAttacker ? heroVisuals : monsterVisuals;
            feedbackView.PlayHeal(target, amount);
        }

        private void UpdateHeroStatsText()
        {
            if (heroStatsText != null)
            {
                heroStatsText.text = $"{Loc.Tr("label.atk", "ATK")} {EffectiveAttack(true)} | {Loc.Tr("label.def", "DEF")} {EffectiveDefense(true)} | {Loc.Tr("label.mag", "MAG")} {EffectiveMagic(true)}";
            }
        }

        private void UpdateMonsterStatsText()
        {
            if (monsterStatsText != null)
            {
                monsterStatsText.text = $"{Loc.Tr("label.atk", "ATK")} {EffectiveAttack(false)} | {Loc.Tr("label.def", "DEF")} {EffectiveDefense(false)} | {Loc.Tr("label.mag", "MAG")} {EffectiveMagic(false)}";
            }
        }

        private void UpdateHeroHealthText()
        {
            if (heroHealthText != null)
            {
                heroHealthText.text = $"{Loc.Tr("label.hp", "HP")} {_heroHealth} / {_heroMaxHealth}";
            }
        }

        private void UpdateMonsterHealthText()
        {
            if (monsterHealthText != null)
            {
                monsterHealthText.text = $"{Loc.Tr("label.hp", "HP")} {_monsterHealth} / {_monsterMaxHealth}";
            }
        }

        private void UpdateHeroManaText()
        {
            if (heroManaText == null) return;
            heroManaText.text = _heroMaxMana > 0 ? $"{Loc.Tr("label.mp", "MP")} {_heroMana} / {_heroMaxMana}" : string.Empty;
        }

        private void UpdateMonsterManaText()
        {
            if (monsterManaText == null) return;
            monsterManaText.text = _monsterMaxMana > 0 ? $"{Loc.Tr("label.mp", "MP")} {_monsterMana} / {_monsterMaxMana}" : string.Empty;
        }

        private void UpdateHeroStatusEffectsText()
        {
            if (heroStatusEffectsText != null)
            {
                heroStatusEffectsText.text = FormatEffects(_heroEffects);
            }
        }

        private void UpdateMonsterStatusEffectsText()
        {
            if (monsterStatusEffectsText != null)
            {
                monsterStatusEffectsText.text = FormatEffects(_monsterEffects);
            }
        }

        private static string FormatEffects(List<ActiveEffect> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(list[i].Kind);
                sb.Append('(').Append(list[i].TurnsRemaining).Append(')');
            }
            return sb.ToString();
        }

        private void RenderMoves(HeroDto hero)
        {
            ClearMovesContainer();

            if (movesContainer == null || moveButtonPrefab == null || hero == null || hero.equippedMoves == null)
            {
                return;
            }

            for (int i = 0; i < hero.equippedMoves.Count; i++)
            {
                var move = GameSession.Instance.GetMoveById(hero.equippedMoves[i]);
                if (move == null)
                {
                    continue;
                }

                var view = Instantiate(moveButtonPrefab, movesContainer);
                Sprite iconSprite = visualCatalog != null ? visualCatalog.GetMoveIcon(move.id) : null;
                view.Bind(move, iconSprite, OnMoveSelected, OnMoveHovered);
                _spawnedMoveButtons.Add(view);
            }
        }

        private void SetMovesInteractable(bool value)
        {
            for (int i = 0; i < _spawnedMoveButtons.Count; i++)
            {
                var view = _spawnedMoveButtons[i];
                if (view == null) continue;
                view.SetInteractableExternal(value);
            }
        }

        private void RefreshAffordability()
        {
            for (int i = 0; i < _spawnedMoveButtons.Count; i++)
            {
                var view = _spawnedMoveButtons[i];
                if (view == null || view.Move == null) continue;
                view.SetAffordable(CanAfford(view.Move, isHero: true));
            }
        }

        private void ClearMovesContainer()
        {
            _spawnedMoveButtons.Clear();
            if (movesContainer == null)
            {
                return;
            }

            for (int i = movesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(movesContainer.GetChild(i).gameObject);
            }
        }

        private void EndBattle(bool heroWon)
        {
            _battleOver = true;
            SetMovesInteractable(false);

            if (heroWon)
            {
                string headline = string.Format(Loc.Tr("battle.victory_against", "Victory against {0}."), LocalizedNames.Name(_monster));
                AppendLog(headline);

                // Endless runs use a generated curve; standard runs use the
                // fixed per-encounter rewards on RulesConfig.
                var reward = GameSession.Instance != null
                    ? (GameSession.Instance.CurrentRunMode == RunMode.Endless
                        ? GameSession.Instance.ApplyEndlessVictoryRewards()
                        : GameSession.Instance.ApplyVictoryRewards(_encounter.index))
                    : null;

                SetStatus(BuildVictorySummary(headline, reward));

                if (reward != null)
                {
                    AppendLog(string.Format(Loc.Tr("battle.xp_gain", "+{0} XP."), reward.XpGained));
                    if (reward.GoldGained > 0)
                    {
                        AppendLog(string.Format(Loc.Tr("battle.gold_gain", "+{0} gold (total {1})."), reward.GoldGained, reward.CurrentGold));
                    }
                    if (reward.LeveledUp)
                    {
                        AppendLog(string.Format(Loc.Tr("battle.level_up", "Level up! Now Lv {0}."), reward.NewLevel));
                    }
                    if (reward.NewMoveLearned)
                    {
                        AppendLog(string.Format(
                            Loc.Tr(reward.AutoEquipped ? "battle.learned_move_auto" : "battle.learned_move",
                                reward.AutoEquipped ? "Learned {0} (auto-equipped)." : "Learned {0}."),
                            reward.LearnedMoveName));
                    }
                    if (reward.ItemDropped)
                    {
                        if (reward.ItemAddedToInventory)
                        {
                            AppendLog(string.Format(
                                Loc.Tr(reward.ItemAutoEquipped ? "battle.found_item_auto" : "battle.found_item",
                                    reward.ItemAutoEquipped ? "Found {0} (auto-equipped)." : "Found {0}."),
                                reward.DroppedItemName));
                        }
                        else if (reward.ItemAlreadyOwned)
                        {
                            AppendLog(string.Format(Loc.Tr("battle.found_duplicate", "Found {0} (duplicate, already owned)."), reward.DroppedItemName));
                        }
                    }
                    if (reward.UnlockedNextEncounter)
                    {
                        AppendLog(string.Format(Loc.Tr("battle.unlocked", "Encounter {0} unlocked."), reward.NextUnlockedIndex + 1));
                    }
                }

                MaybeShowLevelUpPanel(reward);
            }
            else
            {
                string message = string.Format(Loc.Tr("battle.defeated_by", "Defeated by {0}."), LocalizedNames.Name(_monster));
                SetStatus(message);
                AppendLog(message);

                // Endless runs end on defeat; the standard run simply replays
                // the same encounter, handled by the existing flow.
                if (GameSession.Instance != null && GameSession.Instance.CurrentRunMode == RunMode.Endless)
                {
                    GameSession.Instance.EndEndlessRun();
                }
            }
        }

        private void MaybeShowLevelUpPanel(VictoryRewardResult reward)
        {
            if (reward == null || !reward.LeveledUp || reward.PendingLevelUps <= 0)
            {
                return;
            }

            var rules = GameSession.Instance != null && GameSession.Instance.CurrentRun != null
                ? GameSession.Instance.CurrentRun.rules
                : null;

            if (rules == null || rules.levelUpChoices == null || rules.levelUpChoices.Count == 0)
            {
                // No choices configured — fall back to a no-op so older configs keep working.
                return;
            }

            if (levelUpPanel == null)
            {
                Debug.LogWarning("LevelUpChoicePanelView reference missing on BattleController; skipping picker.");
                return;
            }

            if (backButton != null)
            {
                backButton.interactable = false;
            }

            levelUpPanel.Show(reward.NewLevel, rules.levelUpChoices, OnLevelUpPanelClosed);
        }

        private void OnLevelUpPanelClosed()
        {
            if (backButton != null)
            {
                backButton.interactable = true;
            }
            UpdateHeroStatsText();
        }

        private static string BuildVictorySummary(string headline, VictoryRewardResult reward)
        {
            if (reward == null)
            {
                return headline;
            }

            var sb = new StringBuilder();
            sb.Append(headline);
            sb.Append(' ').Append(string.Format(Loc.Tr("battle.xp_gain", "+{0} XP."), reward.XpGained));
            if (reward.GoldGained > 0)
            {
                sb.Append(' ').Append(string.Format(Loc.Tr("battle.gold_gain", "+{0} gold (total {1})."), reward.GoldGained, reward.CurrentGold));
            }
            if (reward.LeveledUp)
            {
                sb.Append(' ').Append(string.Format(Loc.Tr("battle.level_up", "Level up! Now Lv {0}."), reward.NewLevel));
            }
            if (reward.NewMoveLearned)
            {
                sb.Append(' ').Append(string.Format(
                    Loc.Tr(reward.AutoEquipped ? "battle.learned_move_auto" : "battle.learned_move",
                        reward.AutoEquipped ? "Learned {0} (auto-equipped)." : "Learned {0}."),
                    reward.LearnedMoveName));
            }
            if (reward.ItemDropped)
            {
                if (reward.ItemAddedToInventory)
                {
                    sb.Append(' ').Append(string.Format(
                        Loc.Tr(reward.ItemAutoEquipped ? "battle.found_item_auto" : "battle.found_item",
                            reward.ItemAutoEquipped ? "Found {0} (auto-equipped)." : "Found {0}."),
                        reward.DroppedItemName));
                }
                else if (reward.ItemAlreadyOwned)
                {
                    sb.Append(' ').Append(string.Format(Loc.Tr("battle.found_duplicate", "Found {0} (duplicate, already owned)."), reward.DroppedItemName));
                }
            }
            return sb.ToString();
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        // ---------- Helpers ----------

        private static StatsDto ScaleMonsterStats(StatsDto baseStats, int level, RulesConfigDto rules)
        {
            var result = new StatsDto
            {
                maxHealth = baseStats.maxHealth,
                maxMana = baseStats.maxMana,
                attack = baseStats.attack,
                defense = baseStats.defense,
                magic = baseStats.magic
            };

            if (rules == null || rules.statGainPerLevel == null || level <= 1)
            {
                return result;
            }

            int extraLevels = level - 1;
            result.maxHealth += rules.statGainPerLevel.maxHealth * extraLevels;
            result.maxMana += rules.statGainPerLevel.maxMana * extraLevels;
            result.attack += rules.statGainPerLevel.attack * extraLevels;
            result.defense += rules.statGainPerLevel.defense * extraLevels;
            result.magic += rules.statGainPerLevel.magic * extraLevels;
            return result;
        }

        private void AppendLog(string line)
        {
            _log.Enqueue(line);
            while (_log.Count > MaxLogLines)
            {
                _log.Dequeue();
            }

            if (logText == null)
            {
                return;
            }

            var sb = new StringBuilder();
            foreach (var entry in _log)
            {
                sb.AppendLine(entry);
            }
            SetLog(sb.ToString().TrimEnd());
        }

        // Clears the rolling battle log. Called at the start of each hero turn
        // so the displayed log only describes the current exchange.
        private void ClearLog()
        {
            _log.Clear();
            SetLog(string.Empty);
        }

        // Resolves the current environment id against the visual catalog and
        // applies the matching background sprite (or the catalog default).
        // No-op when no Image is wired or no catalog is assigned.
        private void ApplyEnvironmentBackground()
        {
            if (environmentBackgroundImage == null) return;
            if (environmentVisualCatalog == null) return;

            string envId = _environment != null ? _environment.id : null;
            var sprite = environmentVisualCatalog.GetBackground(envId);
            var tint = environmentVisualCatalog.GetOverlayColor(envId);

            if (sprite != null)
            {
                environmentBackgroundImage.sprite = sprite;
                environmentBackgroundImage.enabled = true;
            }
            // Tint defaults to white in the catalog; safe to apply unconditionally.
            environmentBackgroundImage.color = tint;
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }

        private void SetLog(string value)
        {
            if (logText != null)
            {
                logText.text = value;
            }
        }

        private void OnMoveHovered(MoveDto move)
        {
            if (moveInfoPanel != null)
            {
                if (move == null)
                {
                    moveInfoPanel.Clear();
                }
                else
                {
                    Sprite iconSprite = visualCatalog != null ? visualCatalog.GetMoveIcon(move.id) : null;
                    moveInfoPanel.Show(move, iconSprite);
                }
            }

            if (moveInfoText == null)
            {
                return;
            }

            if (move == null)
            {
                moveInfoText.text = string.Empty;
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"<b>{LocalizedNames.Name(move)}</b>  ({LocalizedNames.Category(move.category)}");
            if (move.power > 0)
            {
                sb.Append($", {Loc.Tr("label.power", "Pow")} {move.power}");
            }
            if (move.manaCost > 0)
            {
                sb.Append($", {move.manaCost} {Loc.Tr("label.mp", "MP")}");
            }
            if (move.hpCost > 0)
            {
                sb.Append($", {move.hpCost} {Loc.Tr("label.hp", "HP")}");
            }
            sb.Append(")");
            string desc = LocalizedNames.Description(move);
            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine();
                sb.Append(desc);
            }
            moveInfoText.text = sb.ToString();
        }

        private void SetMoveInfo(string value)
        {
            if (moveInfoText != null)
            {
                moveInfoText.text = value;
            }
            if (moveInfoPanel != null && string.IsNullOrEmpty(value))
            {
                moveInfoPanel.Clear();
            }
        }
    }
}
