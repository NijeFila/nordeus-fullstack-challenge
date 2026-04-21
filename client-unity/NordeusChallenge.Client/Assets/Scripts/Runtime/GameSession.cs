using System.Collections.Generic;
using NordeusChallenge.Client.Models;
using UnityEngine;

namespace NordeusChallenge.Client.Runtime
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public RunConfigResponseDto CurrentRun { get; private set; }

        public HeroDto CurrentHero { get; private set; }

        public int SelectedEncounterIndex { get; private set; } = -1;

        public int HighestUnlockedEncounterIndex { get; private set; } = -1;

        private readonly HashSet<int> _clearedEncounters = new();

        private readonly System.Random _random = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetCurrentRun(RunConfigResponseDto run)
        {
            CurrentRun = run;
            CurrentHero = CloneHero(run != null ? run.hero : null);
            SelectedEncounterIndex = -1;
            HighestUnlockedEncounterIndex = (run != null && run.encounters != null && run.encounters.Count > 0) ? 0 : -1;
            _clearedEncounters.Clear();
        }

        public void ClearCurrentRun()
        {
            CurrentRun = null;
            CurrentHero = null;
            SelectedEncounterIndex = -1;
            HighestUnlockedEncounterIndex = -1;
            _clearedEncounters.Clear();
        }

        public void SetSelectedEncounterIndex(int index)
        {
            SelectedEncounterIndex = index;
        }

        public bool IsEncounterUnlocked(int index)
        {
            return index >= 0 && index <= HighestUnlockedEncounterIndex;
        }

        public bool IsEncounterCleared(int index)
        {
            return _clearedEncounters.Contains(index);
        }

        public MoveDto GetMoveById(string moveId)
        {
            if (CurrentRun == null || CurrentRun.moves == null || string.IsNullOrEmpty(moveId))
            {
                return null;
            }

            for (int i = 0; i < CurrentRun.moves.Count; i++)
            {
                if (CurrentRun.moves[i].id == moveId)
                {
                    return CurrentRun.moves[i];
                }
            }

            return null;
        }

        public MonsterDto GetMonsterById(string monsterId)
        {
            if (CurrentRun == null || CurrentRun.monsters == null || string.IsNullOrEmpty(monsterId))
            {
                return null;
            }

            for (int i = 0; i < CurrentRun.monsters.Count; i++)
            {
                if (CurrentRun.monsters[i].id == monsterId)
                {
                    return CurrentRun.monsters[i];
                }
            }

            return null;
        }

        public EncounterDto GetEncounterByIndex(int index)
        {
            if (CurrentRun == null || CurrentRun.encounters == null)
            {
                return null;
            }

            for (int i = 0; i < CurrentRun.encounters.Count; i++)
            {
                if (CurrentRun.encounters[i].index == index)
                {
                    return CurrentRun.encounters[i];
                }
            }

            return null;
        }

        public VictoryRewardResult ApplyVictoryRewards(int encounterIndex)
        {
            var result = new VictoryRewardResult();

            if (CurrentRun == null || CurrentHero == null || CurrentRun.rules == null)
            {
                return result;
            }

            var encounter = GetEncounterByIndex(encounterIndex);
            if (encounter == null)
            {
                return result;
            }

            var monster = GetMonsterById(encounter.monsterId);
            if (monster == null)
            {
                return result;
            }

            var rules = CurrentRun.rules;

            result.XpGained = rules.xpPerVictory;
            CurrentHero.xp += rules.xpPerVictory;

            if (CurrentHero.xp >= rules.xpPerLevel)
            {
                CurrentHero.xp -= rules.xpPerLevel;
                CurrentHero.level += 1;

                if (CurrentHero.stats != null && rules.statGainPerLevel != null)
                {
                    CurrentHero.stats.maxHealth += rules.statGainPerLevel.maxHealth;
                    CurrentHero.stats.attack += rules.statGainPerLevel.attack;
                    CurrentHero.stats.defense += rules.statGainPerLevel.defense;
                    CurrentHero.stats.magic += rules.statGainPerLevel.magic;
                }

                result.LeveledUp = true;
                result.NewLevel = CurrentHero.level;
            }

            TryLearnMove(monster, rules, result);

            _clearedEncounters.Add(encounterIndex);

            if (encounterIndex == HighestUnlockedEncounterIndex
                && CurrentRun.encounters != null
                && encounterIndex + 1 < CurrentRun.encounters.Count)
            {
                HighestUnlockedEncounterIndex = encounterIndex + 1;
                result.UnlockedNextEncounter = true;
                result.NextUnlockedIndex = HighestUnlockedEncounterIndex;
            }

            return result;
        }

        private void TryLearnMove(MonsterDto monster, RulesConfigDto rules, VictoryRewardResult result)
        {
            if (monster == null || monster.moves == null || monster.moves.Count == 0)
            {
                return;
            }

            string candidateId = monster.moves[_random.Next(monster.moves.Count)];

            if (CurrentHero.learnedMovePool == null)
            {
                CurrentHero.learnedMovePool = new List<string>();
            }

            if (CurrentHero.learnedMovePool.Contains(candidateId))
            {
                return;
            }

            CurrentHero.learnedMovePool.Add(candidateId);
            result.NewMoveLearned = true;
            result.LearnedMoveId = candidateId;

            var move = GetMoveById(candidateId);
            result.LearnedMoveName = move != null ? move.name : candidateId;

            if (CurrentHero.equippedMoves == null)
            {
                CurrentHero.equippedMoves = new List<string>();
            }

            if (CurrentHero.equippedMoves.Count < rules.equippedMoveSlots)
            {
                CurrentHero.equippedMoves.Add(candidateId);
                result.AutoEquipped = true;
            }
        }

        private static HeroDto CloneHero(HeroDto source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new HeroDto
            {
                id = source.id,
                name = source.name,
                level = source.level,
                xp = source.xp,
                stats = source.stats == null ? null : new StatsDto
                {
                    maxHealth = source.stats.maxHealth,
                    attack = source.stats.attack,
                    defense = source.stats.defense,
                    magic = source.stats.magic
                },
                equippedMoves = source.equippedMoves != null ? new List<string>(source.equippedMoves) : new List<string>(),
                learnedMovePool = source.learnedMovePool != null ? new List<string>(source.learnedMovePool) : new List<string>()
            };

            return clone;
        }
    }
}
