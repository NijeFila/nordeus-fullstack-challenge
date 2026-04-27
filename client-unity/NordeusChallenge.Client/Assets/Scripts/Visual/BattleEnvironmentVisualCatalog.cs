using System;
using System.Collections.Generic;
using UnityEngine;

namespace NordeusChallenge.Client.Visual
{
    // Maps environment ids returned in RunConfig to a battle background sprite
    // and an optional overlay tint. Lives as a ScriptableObject so designers
    // can drop sprites in via the inspector without touching code.
    //
    // Lookup falls back to the configured default sprite when the environment
    // id is unknown or has no entry, so the Battle scene never breaks if a new
    // environment is added on the server before art lands.
    [CreateAssetMenu(menuName = "Nordeus/Battle Environment Visual Catalog", fileName = "BattleEnvironmentVisualCatalog")]
    public class BattleEnvironmentVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string environmentId;
            public Sprite backgroundSprite;
            // Optional per-environment overlay tint. Use white (1,1,1,1) for
            // "no tint". Alpha can be used to slightly darken or warm the
            // background without bespoke sprites.
            public Color overlayColor;
        }

        [Header("Defaults")]
        [SerializeField] private Sprite defaultBackground;
        [SerializeField] private Color defaultOverlayColor = Color.white;

        [Header("Per-Environment Entries")]
        [SerializeField] private List<Entry> entries = new();

        public Sprite GetBackground(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId)) return defaultBackground;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].environmentId == environmentId && entries[i].backgroundSprite != null)
                {
                    return entries[i].backgroundSprite;
                }
            }
            return defaultBackground;
        }

        public Color GetOverlayColor(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId)) return defaultOverlayColor;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].environmentId == environmentId)
                {
                    var c = entries[i].overlayColor;
                    // Treat fully-zero color as "unset" so the inspector default doesn't blank the screen.
                    if (c.r == 0f && c.g == 0f && c.b == 0f && c.a == 0f) return defaultOverlayColor;
                    return c;
                }
            }
            return defaultOverlayColor;
        }
    }
}
