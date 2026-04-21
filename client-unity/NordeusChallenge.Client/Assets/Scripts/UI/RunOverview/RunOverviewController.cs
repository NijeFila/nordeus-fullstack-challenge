using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NordeusChallenge.Client.UI.RunOverview
{
    public class RunOverviewController : MonoBehaviour
    {
        [Header("Hero / Moves")]
        [SerializeField] private TMP_Text heroText;
        [SerializeField] private TMP_Text equippedMovesText;

        [Header("Encounters")]
        [SerializeField] private Transform encountersContainer;
        [SerializeField] private EncounterButtonView encounterButtonPrefab;

        private void Start()
        {
            if (GameSession.Instance == null || GameSession.Instance.CurrentRun == null)
            {
                SetHeroText("No active run.");
                SetEquippedMovesText(string.Empty);
                ClearEncountersContainer();
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            RenderHero(run.hero);
            RenderEquippedMoves(run);
            RenderEncounters(run);
        }

        private void RenderHero(HeroDto hero)
        {
            if (hero == null)
            {
                SetHeroText("No hero data.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{hero.name} (Lv {hero.level})");
            if (hero.stats != null)
            {
                sb.AppendLine($"HP {hero.stats.maxHealth} | ATK {hero.stats.attack} | DEF {hero.stats.defense} | MAG {hero.stats.magic}");
            }
            sb.Append($"XP {hero.xp}");
            SetHeroText(sb.ToString());
        }

        private void RenderEquippedMoves(RunConfigResponseDto run)
        {
            if (run.hero == null || run.hero.equippedMoves == null || run.hero.equippedMoves.Count == 0)
            {
                SetEquippedMovesText("No equipped moves.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Equipped Moves:");

            for (int i = 0; i < run.hero.equippedMoves.Count; i++)
            {
                string moveId = run.hero.equippedMoves[i];
                var move = GameSession.Instance.GetMoveById(moveId);

                if (move == null)
                {
                    sb.AppendLine($"- {moveId}");
                    continue;
                }

                sb.AppendLine($"- {move.name} ({move.category}, Pow {move.power})");
            }

            SetEquippedMovesText(sb.ToString().TrimEnd());
        }

        private void RenderEncounters(RunConfigResponseDto run)
        {
            ClearEncountersContainer();

            if (encountersContainer == null || encounterButtonPrefab == null)
            {
                return;
            }

            if (run.encounters == null || run.encounters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < run.encounters.Count; i++)
            {
                var encounter = run.encounters[i];
                var monster = GameSession.Instance.GetMonsterById(encounter.monsterId);
                string monsterName = monster != null ? monster.name : encounter.monsterId;
                string label = $"{encounter.index + 1}. {monsterName} (Lv {encounter.level})";

                var view = Instantiate(encounterButtonPrefab, encountersContainer);
                view.Bind(encounter.index, label, OnEncounterSelected);
            }
        }

        private void OnEncounterSelected(int encounterIndex)
        {
            if (GameSession.Instance == null)
            {
                return;
            }

            GameSession.Instance.SetSelectedEncounterIndex(encounterIndex);
            SceneManager.LoadScene(SceneNames.Battle);
        }

        private void ClearEncountersContainer()
        {
            if (encountersContainer == null)
            {
                return;
            }

            for (int i = encountersContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(encountersContainer.GetChild(i).gameObject);
            }
        }

        private void SetHeroText(string value)
        {
            if (heroText != null) heroText.text = value;
        }

        private void SetEquippedMovesText(string value)
        {
            if (equippedMovesText != null) equippedMovesText.text = value;
        }
    }
}
