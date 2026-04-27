using System.Collections.Generic;
using NordeusChallenge.Client.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Settings
{
    // Drives the in-menu Settings panel. Pure MonoBehaviour: no DI, no event
    // bus, no service. Holds a single pending state in memory; Apply commits
    // it to PlayerPrefs and the live engine settings.
    //
    // Every UI reference is optional — the controller checks for null before
    // touching anything, so partial wiring (e.g. no audio mixer, no language
    // dropdown) keeps working.
    public class SettingsController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Display")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fullscreenDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown framerateDropdown;

        [Header("Audio (optional)")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SfxVolume";
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Gameplay (optional)")]
        [SerializeField] private Toggle reduceMotionToggle;

        [Header("Language (optional)")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        // Fullscreen mode list shown to the player. Order is the index stored
        // in PlayerPrefs and matches the FullScreenMode enum values.
        private static readonly FullScreenMode[] FullscreenModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.MaximizedWindow,
            FullScreenMode.Windowed
        };
        private static readonly string[] FullscreenLabelKeys =
        {
            "ui.settings.fullscreen.exclusive",
            "ui.settings.fullscreen.borderless",
            "ui.settings.fullscreen.maximized",
            "ui.settings.fullscreen.windowed"
        };
        private static readonly string[] FullscreenLabelFallbacks =
        {
            "Exclusive Fullscreen",
            "Borderless",
            "Maximized Window",
            "Windowed"
        };

        // Target framerate options. -1 means "unlimited" (Application default).
        private static readonly int[] FramerateOptions = { -1, 30, 60, 120, 144, 240 };

        private static readonly string[] LanguageCodes = { Loc.English, Loc.SerbianLatin };
        private static readonly string[] LanguageLabels = { "English", "Srpski (latinica)" };

        // Resolutions exposed to the player, deduplicated by (width, height).
        private List<Resolution> _resolutions = new();

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (statusText != null) statusText.text = string.Empty;

            BuildResolutionOptions();
            BuildFullscreenOptions();
            BuildFramerateOptions();
            BuildLanguageOptions();
        }

        private void OnEnable()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
        }

        private void OnDisable()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        // ---------- Panel open / close ----------

        public void Open()
        {
            LoadIntoControls();
            if (statusText != null) statusText.text = string.Empty;
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ---------- Build dropdown options ----------

        private void BuildResolutionOptions()
        {
            if (resolutionDropdown == null) return;

            _resolutions.Clear();
            var seen = new HashSet<(int, int)>();
            var raw = Screen.resolutions;
            for (int i = 0; i < raw.Length; i++)
            {
                var r = raw[i];
                if (seen.Add((r.width, r.height)))
                {
                    _resolutions.Add(r);
                }
            }

            // Make sure the current desktop resolution is at least present.
            var current = Screen.currentResolution;
            if (seen.Add((current.width, current.height)))
            {
                _resolutions.Add(current);
            }

            _resolutions.Sort((a, b) =>
            {
                int cmp = a.width.CompareTo(b.width);
                return cmp != 0 ? cmp : a.height.CompareTo(b.height);
            });

            resolutionDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(_resolutions.Count);
            for (int i = 0; i < _resolutions.Count; i++)
            {
                var r = _resolutions[i];
                options.Add(new TMP_Dropdown.OptionData($"{r.width} x {r.height}"));
            }
            resolutionDropdown.AddOptions(options);
        }

        private void BuildFullscreenOptions()
        {
            if (fullscreenDropdown == null) return;

            fullscreenDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(FullscreenModes.Length);
            for (int i = 0; i < FullscreenModes.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(
                    Loc.Tr(FullscreenLabelKeys[i], FullscreenLabelFallbacks[i])));
            }
            fullscreenDropdown.AddOptions(options);
        }

        private void BuildFramerateOptions()
        {
            if (framerateDropdown == null) return;

            framerateDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(FramerateOptions.Length);
            for (int i = 0; i < FramerateOptions.Length; i++)
            {
                int v = FramerateOptions[i];
                string label = v < 0
                    ? Loc.Tr("ui.settings.framerate.unlimited", "Unlimited")
                    : v.ToString();
                options.Add(new TMP_Dropdown.OptionData(label));
            }
            framerateDropdown.AddOptions(options);
        }

        private void BuildLanguageOptions()
        {
            if (languageDropdown == null) return;

            languageDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(LanguageLabels.Length);
            for (int i = 0; i < LanguageLabels.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(LanguageLabels[i]));
            }
            languageDropdown.AddOptions(options);
        }

        // ---------- Read PlayerPrefs / engine state into controls ----------

        private void LoadIntoControls()
        {
            // Resolution
            if (resolutionDropdown != null && _resolutions.Count > 0)
            {
                int width = PlayerPrefs.GetInt(SettingsKeys.ResolutionWidth, Screen.width);
                int height = PlayerPrefs.GetInt(SettingsKeys.ResolutionHeight, Screen.height);
                int idx = FindResolutionIndex(width, height);
                resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
            }

            // Fullscreen
            if (fullscreenDropdown != null)
            {
                int saved = PlayerPrefs.GetInt(SettingsKeys.FullscreenMode, (int)Screen.fullScreenMode);
                int dropdownIndex = FullscreenModeToIndex((FullScreenMode)saved);
                fullscreenDropdown.SetValueWithoutNotify(dropdownIndex);
            }

            // VSync
            if (vsyncToggle != null)
            {
                bool on = PlayerPrefs.GetInt(SettingsKeys.VSync, QualitySettings.vSyncCount > 0 ? 1 : 0) > 0;
                vsyncToggle.SetIsOnWithoutNotify(on);
            }

            // Framerate
            if (framerateDropdown != null)
            {
                int target = PlayerPrefs.GetInt(SettingsKeys.TargetFramerate, Application.targetFrameRate);
                framerateDropdown.SetValueWithoutNotify(FindFramerateIndex(target));
            }

            // Audio
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SettingsKeys.MasterVolume, 0.8f));
            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SettingsKeys.MusicVolume, 0.8f));
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SettingsKeys.SfxVolume, 0.8f));

            // Gameplay
            if (reduceMotionToggle != null)
                reduceMotionToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(SettingsKeys.ReduceMotion, 0) > 0);

            // Language
            if (languageDropdown != null)
            {
                int idx = LanguageIndexFromCode(Loc.CurrentLanguage);
                languageDropdown.SetValueWithoutNotify(idx);
            }
        }

        // ---------- Apply / Reset ----------

        public void Apply()
        {
            // Display
            if (resolutionDropdown != null && _resolutions.Count > 0)
            {
                int idx = Mathf.Clamp(resolutionDropdown.value, 0, _resolutions.Count - 1);
                var r = _resolutions[idx];
                PlayerPrefs.SetInt(SettingsKeys.ResolutionWidth, r.width);
                PlayerPrefs.SetInt(SettingsKeys.ResolutionHeight, r.height);
                PlayerPrefs.SetInt(SettingsKeys.RefreshRate, r.refreshRate);
            }

            FullScreenMode chosenFullscreen = Screen.fullScreenMode;
            if (fullscreenDropdown != null)
            {
                int idx = Mathf.Clamp(fullscreenDropdown.value, 0, FullscreenModes.Length - 1);
                chosenFullscreen = FullscreenModes[idx];
                PlayerPrefs.SetInt(SettingsKeys.FullscreenMode, (int)chosenFullscreen);
            }

            int finalWidth = PlayerPrefs.GetInt(SettingsKeys.ResolutionWidth, Screen.width);
            int finalHeight = PlayerPrefs.GetInt(SettingsKeys.ResolutionHeight, Screen.height);
            int finalRefresh = PlayerPrefs.GetInt(SettingsKeys.RefreshRate, 0);
            if (finalWidth > 0 && finalHeight > 0)
            {
                if (finalRefresh > 0)
                    Screen.SetResolution(finalWidth, finalHeight, chosenFullscreen, finalRefresh);
                else
                    Screen.SetResolution(finalWidth, finalHeight, chosenFullscreen);
            }
            else
            {
                Screen.fullScreenMode = chosenFullscreen;
            }

            if (vsyncToggle != null)
            {
                int v = vsyncToggle.isOn ? 1 : 0;
                PlayerPrefs.SetInt(SettingsKeys.VSync, v);
                QualitySettings.vSyncCount = v;
            }

            if (framerateDropdown != null)
            {
                int idx = Mathf.Clamp(framerateDropdown.value, 0, FramerateOptions.Length - 1);
                int target = FramerateOptions[idx];
                PlayerPrefs.SetInt(SettingsKeys.TargetFramerate, target);
                Application.targetFrameRate = target;
            }

            // Audio
            ApplyVolumeSlider(masterVolumeSlider, masterVolumeParam, SettingsKeys.MasterVolume);
            ApplyVolumeSlider(musicVolumeSlider, musicVolumeParam, SettingsKeys.MusicVolume);
            ApplyVolumeSlider(sfxVolumeSlider, sfxVolumeParam, SettingsKeys.SfxVolume);

            // Gameplay
            if (reduceMotionToggle != null)
            {
                PlayerPrefs.SetInt(SettingsKeys.ReduceMotion, reduceMotionToggle.isOn ? 1 : 0);
            }

            // Language
            if (languageDropdown != null)
            {
                int idx = Mathf.Clamp(languageDropdown.value, 0, LanguageCodes.Length - 1);
                Loc.SetLanguage(LanguageCodes[idx]);
            }

            PlayerPrefs.Save();
            if (statusText != null) statusText.text = Loc.Tr("ui.settings.applied", "Settings applied.");
        }

        public void ResetToDefaults()
        {
            // Wipe only keys we own.
            PlayerPrefs.DeleteKey(SettingsKeys.ResolutionWidth);
            PlayerPrefs.DeleteKey(SettingsKeys.ResolutionHeight);
            PlayerPrefs.DeleteKey(SettingsKeys.RefreshRate);
            PlayerPrefs.DeleteKey(SettingsKeys.FullscreenMode);
            PlayerPrefs.DeleteKey(SettingsKeys.VSync);
            PlayerPrefs.DeleteKey(SettingsKeys.TargetFramerate);
            PlayerPrefs.DeleteKey(SettingsKeys.MasterVolume);
            PlayerPrefs.DeleteKey(SettingsKeys.MusicVolume);
            PlayerPrefs.DeleteKey(SettingsKeys.SfxVolume);
            PlayerPrefs.DeleteKey(SettingsKeys.ReduceMotion);
            PlayerPrefs.Save();

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;

            // Re-seed controls from the (now empty) prefs / current engine state.
            LoadIntoControls();

            if (statusText != null) statusText.text = Loc.Tr("ui.settings.reset_done", "Defaults restored.");
        }

        // ---------- Helpers ----------

        private void ApplyVolumeSlider(Slider slider, string mixerParam, string prefsKey)
        {
            if (slider == null) return;
            float linear = Mathf.Clamp01(slider.value);
            PlayerPrefs.SetFloat(prefsKey, linear);

            if (audioMixer != null && !string.IsNullOrEmpty(mixerParam))
            {
                // Map linear 0..1 to dB. 0 → -80 dB (silent), 1 → 0 dB.
                float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
                audioMixer.SetFloat(mixerParam, db);
            }
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].width == width && _resolutions[i].height == height) return i;
            }
            return -1;
        }

        private static int FullscreenModeToIndex(FullScreenMode mode)
        {
            for (int i = 0; i < FullscreenModes.Length; i++)
            {
                if (FullscreenModes[i] == mode) return i;
            }
            return 1; // default to Borderless (FullScreenWindow)
        }

        private static int FindFramerateIndex(int target)
        {
            for (int i = 0; i < FramerateOptions.Length; i++)
            {
                if (FramerateOptions[i] == target) return i;
            }
            return 0; // unlimited
        }

        private static int LanguageIndexFromCode(string code)
        {
            for (int i = 0; i < LanguageCodes.Length; i++)
            {
                if (LanguageCodes[i] == code) return i;
            }
            return 0;
        }
    }
}
