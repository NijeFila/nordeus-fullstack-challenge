using System;
using System.Collections.Generic;
using UnityEngine;

namespace NordeusChallenge.Client.Localization
{
    // Small custom localization service. Loads JSON tables from
    // Resources/Localization/<code>.json on demand and falls back to
    // English when a key is missing in the active language.
    //
    // Usage:
    //   Loc.Tr("ui.main_menu.start")
    //   Loc.Tr("move.slash.name", move.name)  // server text as fallback
    //   Loc.SetLanguage("sr-Latn")
    public static class Loc
    {
        public const string English = "en";
        public const string SerbianLatin = "sr-Latn";

        private const string PlayerPrefsKey = "nordeus.language";

        private static readonly string[] SupportedCodes = { English, SerbianLatin };

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>();

        private static string _current;
        private static bool _initialized;

        public static event Action LanguageChanged;

        public static string CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return _current;
            }
        }

        public static IReadOnlyList<string> SupportedLanguages => SupportedCodes;

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || !IsSupported(code))
            {
                code = English;
            }

            EnsureInitialized();
            if (_current == code)
            {
                return;
            }

            _current = code;
            EnsureLoaded(code);
            PlayerPrefs.SetString(PlayerPrefsKey, code);
            PlayerPrefs.Save();

            LanguageChanged?.Invoke();
        }

        public static string Tr(string key, string fallback = "")
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            if (TryLookup(_current, key, out string value))
            {
                return value;
            }

            // Fall back to English when the active language is missing the key.
            if (_current != English && TryLookup(English, key, out string enValue))
            {
                return enValue;
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }

            return key;
        }

        // Forces a refresh notification without changing the language.
        // Useful from editor tooling or after dynamic table edits.
        public static void Refresh()
        {
            EnsureInitialized();
            LanguageChanged?.Invoke();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            string saved = PlayerPrefs.GetString(PlayerPrefsKey, English);
            _current = IsSupported(saved) ? saved : English;

            // English is always preloaded so it can serve as the fallback table.
            EnsureLoaded(English);
            if (_current != English)
            {
                EnsureLoaded(_current);
            }
        }

        private static bool IsSupported(string code)
        {
            for (int i = 0; i < SupportedCodes.Length; i++)
            {
                if (SupportedCodes[i] == code) return true;
            }
            return false;
        }

        private static bool TryLookup(string code, string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(code)) return false;
            EnsureLoaded(code);
            if (Tables.TryGetValue(code, out var table) && table != null)
            {
                return table.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);
            }
            return false;
        }

        private static void EnsureLoaded(string code)
        {
            if (Tables.ContainsKey(code))
            {
                return;
            }

            var table = new Dictionary<string, string>();
            Tables[code] = table;

            var asset = Resources.Load<TextAsset>($"Localization/{code}");
            if (asset == null)
            {
                Debug.LogWarning($"[Loc] Missing localization file: Resources/Localization/{code}.json");
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<LocalizationFile>(asset.text);
                if (parsed?.entries == null)
                {
                    Debug.LogWarning($"[Loc] Empty or malformed localization file: {code}.json");
                    return;
                }

                for (int i = 0; i < parsed.entries.Count; i++)
                {
                    var entry = parsed.entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                    table[entry.key] = entry.value ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Loc] Failed to parse {code}.json: {ex.Message}");
            }
        }

        [Serializable]
        private class LocalizationFile
        {
            public string languageCode;
            public List<LocalizationEntry> entries;
        }

        [Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
