using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.MoveManagement
{
    public class MoveManagementController : MonoBehaviour
    {
        [Header("Equipped")]
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private EquippedSlotView equippedSlotPrefab;

        [Header("Learned")]
        [SerializeField] private Transform learnedContainer;
        [SerializeField] private MoveListItemView learnedItemPrefab;

        [Header("UI")]
        [SerializeField] private TMP_Text selectedMoveText;
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
                SetStatus("No active run.");
                ClearContainer(equippedContainer);
                ClearContainer(learnedContainer);
                UpdateSelectedMoveText();
                return;
            }

            RenderEquipped();
            RenderLearned();
            UpdateSelectedMoveText();
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

                string labelText;
                bool hasMove = !string.IsNullOrEmpty(moveId);
                if (hasMove)
                {
                    var move = GameSession.Instance.GetMoveById(moveId);
                    labelText = $"Slot {i + 1}: {FormatMoveLine(move, moveId)}";
                }
                else
                {
                    labelText = $"Slot {i + 1}: Empty";
                }

                var view = Instantiate(equippedSlotPrefab, equippedContainer);
                view.Bind(i, labelText, hasMove, canAssign, OnAssignSlot, OnClearSlot);
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

                var view = Instantiate(learnedItemPrefab, learnedContainer);
                view.Bind(moveId, labelText, isSelected, OnLearnedMoveSelected);
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
                SetStatus("Select a learned move first.");
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

        private void UpdateSelectedMoveText()
        {
            if (selectedMoveText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_selectedMoveId))
            {
                selectedMoveText.text = "Select a learned move to see details.";
                return;
            }

            var move = GameSession.Instance != null
                ? GameSession.Instance.GetMoveById(_selectedMoveId)
                : null;

            if (move == null)
            {
                selectedMoveText.text = $"<b>{_selectedMoveId}</b>";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{move.name}</b>  ({move.category}");
            if (move.power > 0)
            {
                sb.Append($", Pow {move.power}");
            }
            sb.Append(")");
            if (!string.IsNullOrEmpty(move.description))
            {
                sb.AppendLine();
                sb.Append(move.description);
            }
            selectedMoveText.text = sb.ToString();
        }

        private static string FormatMoveLine(MoveDto move, string fallbackId)
        {
            if (move == null)
            {
                return fallbackId;
            }

            return $"{move.name} ({move.category}, Pow {move.power})";
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
