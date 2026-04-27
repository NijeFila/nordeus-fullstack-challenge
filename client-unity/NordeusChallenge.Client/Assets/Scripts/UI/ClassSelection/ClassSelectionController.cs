using System.Text;
using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using NordeusChallenge.Client.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.ClassSelection
{
    // Pre-run class picker. Reads hero classes from the active run config,
    // renders one card per class, and seeds GameSession.CurrentHero from the
    // chosen class. If the run has no class catalog (older server) the
    // controller falls back to the legacy hero already on the run.
    public class ClassSelectionController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private VisualCatalog visualCatalog;

        [Header("Layout")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private HeroClassCardView cardPrefab;

        [Header("Detail")]
        [SerializeField] private TMP_Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonLabel;
        [SerializeField] private Button backButton;

        private string _selectedClassId;

        private void Start()
        {
            if (titleText != null)
            {
                titleText.text = Loc.Tr("ui.class_selection.title", "Choose Your Class");
            }
            if (confirmButtonLabel != null)
            {
                confirmButtonLabel.text = Loc.Tr("ui.class_selection.choose", "Begin Run");
            }

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            // Pre-select the default class so the confirm button is usable
            // even if the player presses it without clicking a card.
            var session = GameSession.Instance;
            if (session != null && session.CurrentRun != null)
            {
                var defaultClass = session.GetHeroClassByIdOrDefault(null);
                _selectedClassId = defaultClass != null ? defaultClass.id : null;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }

        private void Refresh()
        {
            ClearCards();

            var session = GameSession.Instance;
            if (session == null || session.CurrentRun == null || session.CurrentRun.heroClasses == null
                || session.CurrentRun.heroClasses.Count == 0)
            {
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
                if (confirmButton != null) confirmButton.interactable = false;
                return;
            }

            var classes = session.CurrentRun.heroClasses;
            for (int i = 0; i < classes.Count; i++)
            {
                var heroClass = classes[i];
                if (heroClass == null) continue;
                SpawnCard(heroClass);
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = !string.IsNullOrEmpty(_selectedClassId);
            }
        }

        private void SpawnCard(HeroClassDto heroClass)
        {
            if (cardsContainer == null || cardPrefab == null) return;

            string name = Loc.Tr($"class.{heroClass.id}.name", heroClass.name);
            string description = Loc.Tr($"class.{heroClass.id}.description", heroClass.description);
            string statsLine = BuildStatsLine(heroClass.startingStats);
            string movesLine = BuildMovesLine(heroClass.startingMoves);
            Sprite portraitSprite = visualCatalog != null ? visualCatalog.GetHeroPortrait(heroClass.id) : null;

            bool isSelected = heroClass.id == _selectedClassId;

            var card = Instantiate(cardPrefab, cardsContainer);
            card.Bind(heroClass.id, name, description, statsLine, movesLine, portraitSprite, isSelected, OnCardSelected);
        }

        private void OnCardSelected(string classId)
        {
            if (string.IsNullOrEmpty(classId)) return;
            _selectedClassId = classId;
            SetStatus(string.Empty);
            Refresh();
        }

        private void OnConfirmClicked()
        {
            var session = GameSession.Instance;
            if (session == null || session.CurrentRun == null)
            {
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
                return;
            }

            // Resolve the chosen class, falling back to defaultHeroClassId if
            // the player somehow confirmed without a selection.
            var chosen = session.GetHeroClassByIdOrDefault(_selectedClassId);
            if (chosen == null)
            {
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
                return;
            }

            session.InitializeHeroFromClass(chosen);
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private void OnBackClicked()
        {
            // Backing out of class selection abandons the prepared run config.
            if (GameSession.Instance != null) GameSession.Instance.ClearCurrentRun();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private static string BuildStatsLine(StatsDto stats)
        {
            if (stats == null) return string.Empty;

            string hp = Loc.Tr("label.hp", "HP");
            string mp = Loc.Tr("label.mp", "MP");
            string atk = Loc.Tr("label.atk", "ATK");
            string def = Loc.Tr("label.def", "DEF");
            string mag = Loc.Tr("label.mag", "MAG");

            return $"{hp} {stats.maxHealth}  |  {mp} {stats.maxMana}\n" +
                   $"{atk} {stats.attack}  |  {def} {stats.defense}  |  {mag} {stats.magic}";
        }

        private static string BuildMovesLine(System.Collections.Generic.List<string> moveIds)
        {
            if (moveIds == null || moveIds.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < moveIds.Count; i++)
            {
                string moveId = moveIds[i];
                string moveName = string.IsNullOrEmpty(moveId)
                    ? string.Empty
                    : Loc.Tr($"move.{moveId}.name", moveId);
                if (i > 0) sb.Append(" · ");
                sb.Append(moveName);
            }
            return sb.ToString();
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }

        private void ClearCards()
        {
            if (cardsContainer == null) return;
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }
        }
    }
}
