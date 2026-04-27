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

        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text statusText;

        private RunApiClient _apiClient;
        private bool _requestInFlight;
        private RunMode _pendingMode = RunMode.Standard;

        private void Awake()
        {
            _apiClient = new RunApiClient(baseUrl);

            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
        }

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }
            if (endlessButton != null)
            {
                endlessButton.onClick.AddListener(OnEndlessStartClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
            if (endlessButton != null)
            {
                endlessButton.onClick.RemoveListener(OnEndlessStartClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        private void OnStartClicked() => BeginRunRequest(RunMode.Standard);

        private void OnEndlessStartClicked() => BeginRunRequest(RunMode.Endless);

        private void BeginRunRequest(RunMode mode)
        {
            if (_requestInFlight) return;

            _pendingMode = mode;
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

            // If the user chose Endless but the server has it disabled, fall
            // back to Standard rather than dropping the player into an empty
            // endless run.
            if (_pendingMode == RunMode.Endless
                && (run == null || run.endlessMode == null || !run.endlessMode.enabled))
            {
                _pendingMode = RunMode.Standard;
            }

            GameSession.Instance.SetCurrentRun(run);
            GameSession.Instance.SetRunMode(_pendingMode);

            // If the server returned hero classes, route to the picker. Older
            // server payloads without classes go straight to the run overview
            // using the legacy hero field already loaded by SetCurrentRun.
            bool hasClasses = run != null && run.heroClasses != null && run.heroClasses.Count > 0;
            SceneManager.LoadScene(hasClasses ? SceneNames.ClassSelection : SceneNames.RunOverview);
        }

        private void OnRunConfigError(string error)
        {
            _requestInFlight = false;
            SetStatus(string.Format(Loc.Tr("ui.main_menu.error", "Could not start run. {0}"), error));
            SetButtonsInteractable(true);
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

        private void SetButtonsInteractable(bool value)
        {
            if (startButton != null) startButton.interactable = value;
            if (endlessButton != null) endlessButton.interactable = value;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
