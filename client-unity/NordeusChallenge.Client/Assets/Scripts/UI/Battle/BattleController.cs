using System.Collections;
using System.Collections.Generic;
using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Networking;
using NordeusChallenge.Client.Runtime;
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

        [Header("Hero UI")]
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text heroStatsText;
        [SerializeField] private TMP_Text heroHealthText;

        [Header("Monster UI")]
        [SerializeField] private TMP_Text monsterNameText;
        [SerializeField] private TMP_Text monsterStatsText;
        [SerializeField] private TMP_Text monsterHealthText;

        [Header("Moves")]
        [SerializeField] private Transform movesContainer;
        [SerializeField] private MoveButtonView moveButtonPrefab;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private Button backButton;

        private BattleApiClient _battleApi;

        private HeroDto _hero;
        private MonsterDto _monster;
        private EncounterDto _encounter;
        private StatsDto _monsterScaledStats;

        private int _heroHealth;
        private int _heroMaxHealth;
        private int _monsterHealth;
        private int _monsterMaxHealth;
        private int _turn;
        private bool _battleOver;
        private bool _inputLocked;

        private const int MaxLogLines = 8;

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
            _log.Clear();
            SetLog(string.Empty);

            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null)
            {
                SetStatus("No active run.");
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            _encounter = GameSession.Instance.GetEncounterByIndex(GameSession.Instance.SelectedEncounterIndex);
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

            _hero = run.hero;
            _turn = 1;
            _monsterScaledStats = ScaleMonsterStats(_monster.baseStats, _encounter.level, run.rules);

            _heroMaxHealth = _hero.stats.maxHealth;
            _heroHealth = _heroMaxHealth;
            _monsterMaxHealth = _monsterScaledStats.maxHealth;
            _monsterHealth = _monsterMaxHealth;

            RenderHero();
            RenderMonster();
            RenderMoves(_hero);
            AppendLog($"A wild {_monster.name} appears.");
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

            StartCoroutine(RunTurn(move));
        }

        private IEnumerator RunTurn(MoveDto heroMove)
        {
            _inputLocked = true;
            SetMovesInteractable(false);

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

            TickEffectsAtEndOfTurn();
            _turn++;
            RefreshCombatantsUi();

            _inputLocked = false;
            SetMovesInteractable(true);
        }

        private void ResolveHeroMove(MoveDto move)
        {
            ResolveMove(
                move,
                isHeroAttacker: true,
                attackerName: _hero.name,
                defenderName: _monster.name);
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

            ResolveMove(
                move,
                isHeroAttacker: false,
                attackerName: _monster.name,
                defenderName: _hero.name);
        }

        // ---------- Move resolution ----------

        private void ResolveMove(MoveDto move, bool isHeroAttacker, string attackerName, string defenderName)
        {
            int attackerAttack = EffectiveAttack(isHeroAttacker);
            int attackerMagic = EffectiveMagic(isHeroAttacker);
            int defenderDefense = EffectiveDefense(!isHeroAttacker);

            switch (move.category)
            {
                case "Physical":
                {
                    int damage = Mathf.Max(1, move.power + attackerAttack - defenderDefense);
                    ApplyDamage(isHeroAttacker, damage);
                    AppendLog($"{attackerName} used {move.name}. {defenderName} took {damage} damage.");
                    break;
                }
                case "Magic":
                {
                    int damage = move.power + attackerMagic;
                    ApplyDamage(isHeroAttacker, damage);
                    AppendLog($"{attackerName} used {move.name}. {defenderName} took {damage} damage.");
                    break;
                }
                case "Heal":
                {
                    int healed = ApplyHeal(isHeroAttacker, move, attackerMagic);
                    AppendLog($"{attackerName} used {move.name}. Restored {healed} HP.");
                    break;
                }
                case "Buff":
                case "Debuff":
                {
                    AppendLog($"{attackerName} used {move.name}.");
                    break;
                }
                default:
                {
                    AppendLog($"{attackerName} used {move.name}.");
                    break;
                }
            }

            if (move.effect != null)
            {
                ApplyEffect(move, isHeroAttacker);
            }
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
            int amount = (move.effect != null ? move.effect.amount : 0) + (casterMagic / 2);
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

            list.Add(new ActiveEffect
            {
                SourceMoveId = move.id,
                Kind = effect.kind,
                Amount = effect.amount,
                TurnsRemaining = effect.durationTurns
            });
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
            int baseValue = isHero ? _hero.stats.attack : _monsterScaledStats.attack;
            int delta = SumEffectsFor(isHero ? _heroEffects : _monsterEffects, "BuffAttack", "DebuffAttack");
            return Mathf.Max(1, baseValue + delta);
        }

        private int EffectiveDefense(bool isHero)
        {
            int baseValue = isHero ? _hero.stats.defense : _monsterScaledStats.defense;
            int delta = SumEffectsFor(isHero ? _heroEffects : _monsterEffects, "BuffDefense", "DebuffDefense");
            return Mathf.Max(1, baseValue + delta);
        }

        private int EffectiveMagic(bool isHero)
        {
            int baseValue = isHero ? _hero.stats.magic : _monsterScaledStats.magic;
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

        // ---------- UI ----------

        private void RenderHero()
        {
            if (heroNameText != null)
            {
                heroNameText.text = $"{_hero.name} (Lv {_hero.level})";
            }
            UpdateHeroStatsText();
            UpdateHeroHealthText();
        }

        private void RenderMonster()
        {
            if (monsterNameText != null)
            {
                monsterNameText.text = $"{_monster.name} (Lv {_encounter.level})";
            }
            UpdateMonsterStatsText();
            UpdateMonsterHealthText();
        }

        private void RefreshCombatantsUi()
        {
            UpdateHeroStatsText();
            UpdateMonsterStatsText();
            UpdateHeroHealthText();
            UpdateMonsterHealthText();
        }

        private void UpdateHeroStatsText()
        {
            if (heroStatsText != null)
            {
                heroStatsText.text = $"ATK {EffectiveAttack(true)} | DEF {EffectiveDefense(true)} | MAG {EffectiveMagic(true)}";
            }
        }

        private void UpdateMonsterStatsText()
        {
            if (monsterStatsText != null)
            {
                monsterStatsText.text = $"ATK {EffectiveAttack(false)} | DEF {EffectiveDefense(false)} | MAG {EffectiveMagic(false)}";
            }
        }

        private void UpdateHeroHealthText()
        {
            if (heroHealthText != null)
            {
                heroHealthText.text = $"HP {_heroHealth} / {_heroMaxHealth}";
            }
        }

        private void UpdateMonsterHealthText()
        {
            if (monsterHealthText != null)
            {
                monsterHealthText.text = $"HP {_monsterHealth} / {_monsterMaxHealth}";
            }
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
                view.Bind(move, OnMoveSelected);
                _spawnedMoveButtons.Add(view);
            }
        }

        private void SetMovesInteractable(bool value)
        {
            for (int i = 0; i < _spawnedMoveButtons.Count; i++)
            {
                var view = _spawnedMoveButtons[i];
                if (view == null) continue;
                var button = view.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = value;
                }
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

            string message = heroWon
                ? $"Victory against {_monster.name}."
                : $"Defeated by {_monster.name}.";
            SetStatus(message);
            AppendLog(message);
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
    }
}
