namespace NordeusChallenge.Client.UI.Settings
{
    // Single source of truth for PlayerPrefs keys touched by the settings
    // system. Centralised so the bootstrapper and the controller can't drift.
    public static class SettingsKeys
    {
        public const string ResolutionWidth   = "settings.resolution.width";
        public const string ResolutionHeight  = "settings.resolution.height";
        public const string RefreshRate       = "settings.resolution.refresh";
        public const string FullscreenMode    = "settings.fullscreenMode";
        public const string VSync             = "settings.vsync";
        public const string TargetFramerate   = "settings.targetFramerate";

        public const string MasterVolume      = "settings.audio.master";
        public const string MusicVolume       = "settings.audio.music";
        public const string SfxVolume         = "settings.audio.sfx";

        public const string ReduceMotion      = "settings.gameplay.reduceMotion";
    }
}
