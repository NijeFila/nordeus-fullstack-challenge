using NordeusChallenge.Client.Core;
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
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text statusText;

        private RunApiClient _apiClient;
        private bool _requestInFlight;

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

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        private void OnStartClicked()
        {
            if (_requestInFlight)
            {
                return;
            }

            _requestInFlight = true;
            SetStartInteractable(false);
            SetStatus("Loading run...");

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

            GameSession.Instance.SetCurrentRun(run);
            SceneManager.LoadScene(SceneNames.RunOverview);
        }

        private void OnRunConfigError(string error)
        {
            _requestInFlight = false;
            SetStatus($"Could not start run. {error}");
            SetStartInteractable(true);
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

        private void SetStartInteractable(bool value)
        {
            if (startButton != null)
            {
                startButton.interactable = value;
            }
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
