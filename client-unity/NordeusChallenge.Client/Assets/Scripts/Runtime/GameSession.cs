using NordeusChallenge.Client.Models;
using UnityEngine;

namespace NordeusChallenge.Client.Runtime
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public RunConfigResponseDto CurrentRun { get; private set; }

        public int SelectedEncounterIndex { get; private set; } = -1;

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
            SelectedEncounterIndex = -1;
        }

        public void ClearCurrentRun()
        {
            CurrentRun = null;
            SelectedEncounterIndex = -1;
        }

        public void SetSelectedEncounterIndex(int index)
        {
            SelectedEncounterIndex = index;
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
    }
}
