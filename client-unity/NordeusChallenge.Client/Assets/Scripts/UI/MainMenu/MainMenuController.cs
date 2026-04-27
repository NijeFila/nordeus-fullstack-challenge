using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Models;
using NordeusChallenge.Client.Networking;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string baseUrl = "http://localhost:5046";

        [Header("UI References")]
        [Tooltip("Standard Run button. Wired to OnStartClicked.")]
        [SerializeField] private Button startButton;

        [Tooltip("Optional Endless Run button. Wired to OnEndlessStartClicked. Hidden if endlessMode is disabled.")]
        [SerializeField] private Button endlessButton;

        [Tooltip("Optional Continue Run button. Hidden when no save file exists.")]
        [SerializeField] private Button continueButton;

        [Tooltip("Optional Delete Save button. Hidden when no save file exists.")]
        [SerializeField] private Button deleteSaveButton;

        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text statusText;

        private RunApiClient _apiClient;
        private bool _requestInFlight;
        private RunMode _pendingMode = RunMode.Standard;

        // When true, the response handler restores from save instead of
        // routing the player to ClassSelection.
        private bool _pendingContinue;

        private void Awake()
        {
            _apiClient = new RunApiClient(baseUrl);
            if (statusText != null) statusText.text = string.Empty;
        }

        private void OnEnable()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (endlessButton != null) endlessButton.onClick.AddListener(OnEndlessStartClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (deleteSaveButton != null) deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            RefreshSaveButtons();
        }

        private void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
            if (endlessButton != null) endlessButton.onClick.RemoveListener(OnEndlessStartClicked);
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
            if (deleteSaveButton != null) deleteSaveButton.onClick.RemoveListener(OnDeleteSaveClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnStartClicked() => BeginRunRequest(RunMode.Standard, isContinue: false);

        private void OnEndlessStartClicked() => BeginRunRequest(RunMode.Endless, isContinue: false);

        private void OnContinueClicked()
        {
            if (!SaveGameService.SaveExists())
            {
                SetStatus(Loc.Tr("ui.main_menu.no_save", "No save file found."));
                RefreshSaveButtons();
                return;
            }
            // Run mode is restored from the save itself; pass Standard as a
            // placeholder so the request kicks off.
            BeginRunRequest(RunMode.Standard, isContinue: true);
        }

        private void OnDeleteSaveClicked()
        {
            SaveGameService.TryDelete();
            SetStatus(Loc.Tr("ui.save.deleted", "Save deleted."));
            RefreshSaveButtons();
        }

        private void BeginRunRequest(RunMode mode, bool isContinue)
        {
            if (_requestInFlight) return;

            _pendingMode = mode;
            _pendingContinue = isContinue;
            _requestInFlight = true;
            SetButtonsInteractable(false);
            SetStatus(Loc.Tr("ui.main_menu.loading", "Loading run..."));

            StartCoroutine(_apiClient.GetRunConfig(OnRunConfigSuccess, OnRunConfigError));
        }

        private void OnRunConfigSuccess(RunConfigResponseDto run)
        {
            _requestInFlight = false;

            if (GameSession.Instance == null)
            {
                OnRunConfigError("GameSession is missing in the scene.");
                return;
            }

            if (_pendingContinue)
            {
                Debug.Log("[MainMenu] Restoring save data onto freshly fetched run config.");
                GameSession.Instance.SetCurrentRun(run);
                var data = SaveGameService.TryRead();
                if (data == null)
                {
                    SetStatus(Loc.Tr("ui.save.load_failed", "Could not load save."));
                    GameSession.Instance.ClearCurrentRun();
                    SetButtonsInteractable(true);
                    RefreshSaveButtons();
                    return;
                }

                if (!GameSession.Instance.RestoreFromSaveData(data))
                {
                    SetStatus(Loc.Tr("ui.save.load_failed", "Could not load save."));
                    GameSession.Instance.ClearCurrentRun();
                    SetButtonsInteractable(true);
                    RefreshSaveButtons();
                    return;
                }

                SetStatus(Loc.Tr("ui.save.loaded", "Run restored."));
                SceneManager.LoadScene(SceneNames.RunOverview);
                return;
            }

            // Fresh-run flow.
            if (_pendingMode == RunMode.Endless
                && (run == null || run.endlessMode == null || !run.endlessMode.enabled))
            {
                _pendingMode = RunMode.Standard;
            }

            GameSession.Instance.SetCurrentRun(run);
            GameSession.Instance.SetRunMode(_pendingMode);

            bool hasClasses = run != null && run.heroClasses != null && run.heroClasses.Count > 0;
            SceneManager.LoadScene(hasClasses ? SceneNames.ClassSelection : SceneNames.RunOverview);
        }

        private void OnRunConfigError(string error)
        {
            _requestInFlight = false;
            SetStatus(string.Format(Loc.Tr("ui.main_menu.error", "Could not start run. {0}"), error));
            SetButtonsInteractable(true);
            RefreshSaveButtons();
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("Exit requested (editor: no quit).");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshSaveButtons()
        {
            bool exists = SaveGameService.SaveExists();
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = exists;
            }
            // Delete Save button is removed from layout per rework instructions.
            if (deleteSaveButton != null)
            {
                deleteSaveButton.gameObject.SetActive(false);
            }
        }

        private void SetButtonsInteractable(bool value)
        {
            if (startButton != null) startButton.interactable = value;
            if (endlessButton != null) endlessButton.interactable = value;
            if (continueButton != null) continueButton.interactable = value && SaveGameService.SaveExists();
            if (deleteSaveButton != null) deleteSaveButton.interactable = value && SaveGameService.SaveExists();
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
