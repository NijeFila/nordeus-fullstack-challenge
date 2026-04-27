using System;
using System.IO;
using UnityEngine;

namespace NordeusChallenge.Client.Runtime
{
    // Tiny JSON-on-disk save service. No encryption, no compression, no slots —
    // just a single file under Application.persistentDataPath. Failures are
    // surfaced as nulls/false so callers can show a localized error rather
    // than crash.
    public static class SaveGameService
    {
        public const string SaveFileName = "nordeus_challenge_save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool SaveExists()
        {
            try
            {
                return File.Exists(SavePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Save] SaveExists check failed: {ex.Message}");
                return false;
            }
        }

        public static bool TryWrite(SaveGameData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[Save] TryWrite called with null data.");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[Save] Wrote save file to {SavePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to write save file: {ex.Message}");
                return false;
            }
        }

        // Returns null when the file is missing, unreadable, or malformed. The
        // caller decides how to surface the error to the player.
        public static SaveGameData TryRead()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return null;
                }

                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning("[Save] Save file is empty.");
                    return null;
                }

                var data = JsonUtility.FromJson<SaveGameData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[Save] Save file did not deserialize.");
                    return null;
                }
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to read save file: {ex.Message}");
                return null;
            }
        }

        public static bool TryDelete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log($"[Save] Deleted save file at {SavePath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to delete save file: {ex.Message}");
                return false;
            }
        }
    }
}
