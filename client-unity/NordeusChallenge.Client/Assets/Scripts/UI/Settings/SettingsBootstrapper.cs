using UnityEngine;

namespace NordeusChallenge.Client.UI.Settings
{
    // Applies persisted display settings before any scene loads, so the very
    // first frame respects the player's saved resolution / vsync / framerate
    // without needing them to open the Settings panel first.
    //
    // Audio mixer values and language are intentionally not applied here:
    // - Audio: requires a scene-loaded AudioMixer reference; SettingsController
    //   handles it when the panel opens.
    // - Language: handled by Loc on first access, which already reads PlayerPrefs.
    public static class SettingsBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            ApplyVSync();
            ApplyTargetFramerate();
            ApplyResolutionAndFullscreen();
        }

        private static void ApplyVSync()
        {
            int vsync = PlayerPrefs.GetInt(SettingsKeys.VSync, 1);
            QualitySettings.vSyncCount = Mathf.Clamp(vsync, 0, 1);
        }

        private static void ApplyTargetFramerate()
        {
            // -1 = unlimited (Application's default).
            int target = PlayerPrefs.GetInt(SettingsKeys.TargetFramerate, -1);
            Application.targetFrameRate = target;
        }

        private static void ApplyResolutionAndFullscreen()
        {
            int width = PlayerPrefs.GetInt(SettingsKeys.ResolutionWidth, 0);
            int height = PlayerPrefs.GetInt(SettingsKeys.ResolutionHeight, 0);
            int fullscreenIndex = PlayerPrefs.GetInt(SettingsKeys.FullscreenMode, -1);

            FullScreenMode? mode = null;
            if (fullscreenIndex >= 0 && fullscreenIndex <= 3)
            {
                mode = (FullScreenMode)fullscreenIndex;
            }

            // If the saved resolution is unknown, leave Unity's default alone
            // and only nudge the fullscreen mode when one was saved.
            if (width <= 0 || height <= 0)
            {
                if (mode.HasValue)
                {
                    Screen.fullScreenMode = mode.Value;
                }
                return;
            }

            int refresh = PlayerPrefs.GetInt(SettingsKeys.RefreshRate, 0);
            FullScreenMode finalMode = mode ?? Screen.fullScreenMode;

            if (refresh > 0)
            {
                Screen.SetResolution(width, height, finalMode, refresh);
            }
            else
            {
                Screen.SetResolution(width, height, finalMode);
            }
        }
    }
}
