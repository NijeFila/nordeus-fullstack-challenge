using NordeusChallenge.Client.Core;
using NordeusChallenge.Client.Localization;
using NordeusChallenge.Client.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Common
{
    // Drop-in component: wires a button to "save the current run and return
    // to Main Menu". Attach to safe screens (RunOverview, MoveManagement,
    // ItemManagement, Shop). The component tolerates a missing button — if
    // none is assigned it just sits dormant.
    public class SaveAndExitButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonLabel;

        [Tooltip("Optional status label shown briefly after a save attempt.")]
        [SerializeField] private TMP_Text statusText;

        private void Start()
        {
            if (buttonLabel != null)
            {
                buttonLabel.text = Loc.Tr("ui.save.save_and_exit", "Save & Exit");
            }
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            bool ok = session.SaveCurrentRun();
            if (statusText != null)
            {
                statusText.text = Loc.Tr(
                    ok ? "ui.save.saved" : "ui.save.load_failed",
                    ok ? "Run saved." : "Could not save the run.");
            }

            if (ok)
            {
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }
    }
}
