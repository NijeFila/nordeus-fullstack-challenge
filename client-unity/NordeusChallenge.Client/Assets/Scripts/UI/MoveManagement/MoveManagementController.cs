using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using NordeusChallenge.Client.UI.Common;
using NordeusChallenge.Client.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.MoveManagement
{
    public class MoveManagementController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private VisualCatalog visualCatalog;

        [Header("Equipped")]
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private EquippedSlotView equippedSlotPrefab;

        [Header("Learned")]
        [SerializeField] private Transform learnedContainer;
        [SerializeField] private MoveListItemView learnedItemPrefab;

        [Header("Selected Move")]
        [SerializeField] private MoveInfoPanelView selectedMovePanel;
        [SerializeField] private TMP_Text selectedMoveText;

        [Header("UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button backButton;

        private string _selectedMoveId;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        private void Refresh()
        {
            if (GameSession.Instance == null
                || GameSession.Instance.CurrentRun == null
                || GameSession.Instance.CurrentHero == null)
            {
                SetStatus(Loc.Tr("ui.common.no_active_run", "No active run."));
                ClearContainer(equippedContainer);
                ClearContainer(learnedContainer);
                UpdateSelectedMove();
                return;
            }

            RenderEquipped();
            RenderLearned();
            UpdateSelectedMove();
        }

        private void RenderEquipped()
        {
            ClearContainer(equippedContainer);
            if (equippedContainer == null || equippedSlotPrefab == null)
            {
                return;
            }

            var run = GameSession.Instance.CurrentRun;
            var hero = GameSession.Instance.CurrentHero;
            int slotCount = run.rules != null ? run.rules.equippedMoveSlots : 4;
            bool canAssign = !string.IsNullOrEmpty(_selectedMoveId);

            for (int i = 0; i < slotCount; i++)
            {
                string moveId = (hero.equippedMoves != null && i < hero.equippedMoves.Count)
                    ? hero.equippedMoves[i]
                    : null;

                bool hasMove = !string.IsNullOrEmpty(moveId);
                MoveDto move = hasMove ? GameSession.Instance.GetMoveById(moveId) : null;

                string labelText = hasMove
                    ? string.Format(Loc.Tr("moves.slot_label", "Slot {0}: {1}"), i + 1, FormatMoveLine(move, moveId))
                    : string.Format(Loc.Tr("moves.slot_empty", "Slot {0}: Empty"), i + 1);
                Sprite iconSprite = (hasMove && visualCatalog != null) ? visualCatalog.GetMoveIcon(moveId) : null;

                var view = Instantiate(equippedSlotPrefab, equippedContainer);
                view.Bind(i, labelText, iconSprite, hasMove, canAssign, OnAssignSlot, OnClearSlot);
            }
        }

        private void RenderLearned()
        {
            ClearContainer(learnedContainer);
            if (learnedContainer == null || learnedItemPrefab == null)
            {
                return;
            }

            var hero = GameSession.Instance.CurrentHero;
            if (hero.learnedMovePool == null || hero.learnedMovePool.Count == 0)
            {
                return;
            }

            for (int i = 0; i < hero.learnedMovePool.Count; i++)
            {
                string moveId = hero.learnedMovePool[i];
                var move = GameSession.Instance.GetMoveById(moveId);
                string labelText = FormatMoveLine(move, moveId);
                bool isSelected = moveId == _selectedMoveId;
                Sprite iconSprite = visualCatalog != null ? visualCatalog.GetMoveIcon(moveId) : null;

                var view = Instantiate(learnedItemPrefab, learnedContainer);
                view.Bind(moveId, labelText, iconSprite, isSelected, OnLearnedMoveSelected);
            }
        }

        private void OnLearnedMoveSelected(string moveId)
        {
            _selectedMoveId = moveId;
            SetStatus(string.Empty);
            Refresh();
        }

        private void OnAssignSlot(int slotIndex)
        {
            if (string.IsNullOrEmpty(_selectedMoveId))
            {
                SetStatus(Loc.Tr("moves.select_first", "Select a learned move first."));
                return;
            }

            var hero = GameSession.Instance.CurrentHero;
            if (hero.equippedMoves == null)
            {
                hero.equippedMoves = new System.Collections.Generic.List<string>();
            }

            int existingIndex = hero.equippedMoves.IndexOf(_selectedMoveId);

            if (slotIndex < hero.equippedMoves.Count)
            {
                hero.equippedMoves[slotIndex] = _selectedMoveId;
                if (existingIndex != -1 && existingIndex != slotIndex)
                {
                    hero.equippedMoves.RemoveAt(existingIndex);
                }
            }
            else
            {
                if (existingIndex != -1)
                {
                    hero.equippedMoves.RemoveAt(existingIndex);
                }
                hero.equippedMoves.Add(_selectedMoveId);
            }

            SetStatus(string.Empty);
            Refresh();
        }

        private void OnClearSlot(int slotIndex)
        {
            var hero = GameSession.Instance.CurrentHero;
            if (hero.equippedMoves == null || slotIndex >= hero.equippedMoves.Count)
            {
                return;
            }

            hero.equippedMoves.RemoveAt(slotIndex);
            SetStatus(string.Empty);
            Refresh();
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private void UpdateSelectedMove()
        {
            MoveDto move = null;
            if (!string.IsNullOrEmpty(_selectedMoveId) && GameSession.Instance != null)
            {
                move = GameSession.Instance.GetMoveById(_selectedMoveId);
            }

            Sprite iconSprite = (move != null && visualCatalog != null)
                ? visualCatalog.GetMoveIcon(move.id)
                : null;

            if (selectedMovePanel != null)
            {
                if (move == null)
                {
                    selectedMovePanel.Clear();
                }
                else
                {
                    selectedMovePanel.Show(move, iconSprite);
                }
            }

            if (selectedMoveText != null)
            {
                selectedMoveText.text = BuildSelectedMoveText(move);
            }
        }

        private string BuildSelectedMoveText(MoveDto move)
        {
            if (move == null)
            {
                return string.IsNullOrEmpty(_selectedMoveId)
                    ? Loc.Tr("moves.detail_hint", "Select a learned move to see details.")
                    : $"<b>{_selectedMoveId}</b>";
            }

            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{LocalizedNames.Name(move)}</b>  ({LocalizedNames.Category(move.category)}");
            if (move.power > 0)
            {
                sb.Append($", {Loc.Tr("label.power", "Pow")} {move.power}");
            }
            sb.Append(")");
            string desc = LocalizedNames.Description(move);
            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine();
                sb.Append(desc);
            }
            return sb.ToString();
        }

        private static string FormatMoveLine(MoveDto move, string fallbackId)
        {
            if (move == null)
            {
                return fallbackId;
            }

            string name = LocalizedNames.Name(move);
            string category = LocalizedNames.Category(move.category);
            return move.power > 0
                ? $"{name} ({category}, {Loc.Tr("label.power", "Pow")} {move.power})"
                : $"{name} ({category})";
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }
    }
}
