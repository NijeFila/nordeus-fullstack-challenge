using System;
using System.Collections.Generic;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    // Post-battle panel that asks the player to spend each pending level-up
    // on one of rules.levelUpChoices. Shown by BattleController on victory
    // when the hero levels up; hidden by default.
    public class LevelUpChoicePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private LevelUpChoiceButtonView choiceButtonPrefab;
        [SerializeField] private Button continueButton;

        private readonly List<LevelUpChoiceButtonView> _spawned = new();
        private List<LevelUpChoiceDto> _choices;
        private Action _onComplete;

        private void Awake()
        {
            // Do not toggle active state here. The panel root is disabled in
            // the editor by default; if we forced it off in Awake we would
            // immediately re-disable the root the first time Show() activates
            // it (Awake fires the moment the GameObject becomes active).
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void Show(int newLevel, List<LevelUpChoiceDto> choices, Action onComplete)
        {
            _choices = choices;
            _onComplete = onComplete;

            if (root == null)
            {
                Debug.LogWarning("LevelUpChoicePanelView has no root assigned. Skipping.");
                if (onComplete != null) onComplete();
                return;
            }

            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("LevelUpChoicePanelView shown with no choices. Skipping.");
                root.SetActive(false);
                if (onComplete != null) onComplete();
                return;
            }

            root.SetActive(true);

            if (titleText != null) titleText.text = Loc.Tr("levelup.title", "Level Up");
            if (levelText != null) levelText.text = string.Format(Loc.Tr("levelup.level_label", "Level {0}"), newLevel);
            UpdateSummary();
            BuildButtons();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        private void BuildButtons()
        {
            ClearButtons();

            if (choicesContainer == null || choiceButtonPrefab == null || _choices == null)
            {
                Debug.LogWarning("LevelUpChoicePanelView is missing button references.");
                return;
            }

            for (int i = 0; i < _choices.Count; i++)
            {
                var view = Instantiate(choiceButtonPrefab, choicesContainer);
                view.Bind(_choices[i], OnChoiceClicked);
                _spawned.Add(view);
            }
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i].gameObject);
                }
            }
            _spawned.Clear();
        }

        private void OnChoiceClicked(LevelUpChoiceDto choice)
        {
            if (GameSession.Instance == null)
            {
                Debug.LogWarning("No GameSession; cannot apply level-up choice.");
                return;
            }

            GameSession.Instance.ApplyLevelUpChoice(choice);
            UpdateSummary();

            if (GameSession.Instance.HasPendingLevelUp)
            {
                // More picks remaining — keep the panel up and let the player choose again.
                return;
            }

            SetButtonsInteractable(false);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
            else
            {
                // No continue button wired up; auto-complete so the run stays usable.
                Hide();
                if (_onComplete != null) _onComplete();
            }
        }

        private void OnContinueClicked()
        {
            Hide();
            if (_onComplete != null) _onComplete();
        }

        private void Hide()
        {
            ClearButtons();
            if (root != null) root.SetActive(false);
        }

        private void SetButtonsInteractable(bool value)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    _spawned[i].SetInteractable(value);
                }
            }
        }

        private void UpdateSummary()
        {
            if (summaryText == null) return;

            var hero = GameSession.Instance != null ? GameSession.Instance.CurrentHero : null;
            int pending = GameSession.Instance != null ? GameSession.Instance.PendingLevelUps : 0;

            if (hero == null || hero.stats == null)
            {
                summaryText.text = string.Empty;
                return;
            }

            string remaining = pending > 0
                ? "\n" + string.Format(Loc.Tr("levelup.picks_remaining", "Picks remaining: {0}"), pending)
                : "\n" + Loc.Tr("levelup.picks_done", "All picks spent.");
            summaryText.text =
                $"{Loc.Tr("label.hp", "HP")} {hero.stats.maxHealth}  |  {Loc.Tr("label.mp", "MP")} {hero.stats.maxMana}\n" +
                $"{Loc.Tr("label.atk", "ATK")} {hero.stats.attack}  |  {Loc.Tr("label.def", "DEF")} {hero.stats.defense}  |  {Loc.Tr("label.mag", "MAG")} {hero.stats.magic}" +
                remaining;
        }
    }
}
