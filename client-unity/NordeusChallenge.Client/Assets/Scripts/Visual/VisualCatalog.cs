using System;
using System.Collections.Generic;
using UnityEngine;

namespace NordeusChallenge.Client.Visual
{
    [CreateAssetMenu(menuName = "Nordeus/Visual Catalog", fileName = "VisualCatalog")]
    public class VisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct SpriteEntry
        {
            public string id;
            public Sprite sprite;
        }

        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private Sprite defaultMoveIcon;
        [SerializeField] private Sprite defaultEffectIcon;

        [SerializeField] private List<SpriteEntry> heroPortraits = new();
        [SerializeField] private List<SpriteEntry> monsterPortraits = new();
        [SerializeField] private List<SpriteEntry> moveIcons = new();
        [SerializeField] private List<SpriteEntry> effectIcons = new();

        public Sprite GetHeroPortrait(string heroId) => Lookup(heroPortraits, heroId, defaultPortrait);
        public Sprite GetMonsterPortrait(string monsterId) => Lookup(monsterPortraits, monsterId, defaultPortrait);
        public Sprite GetMoveIcon(string moveId) => Lookup(moveIcons, moveId, defaultMoveIcon);
        public Sprite GetEffectIcon(string effectKind) => Lookup(effectIcons, effectKind, defaultEffectIcon);

        private static Sprite Lookup(List<SpriteEntry> list, string id, Sprite fallback)
        {
            if (list == null || string.IsNullOrEmpty(id))
            {
                return fallback;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id && list[i].sprite != null)
                {
                    return list[i].sprite;
                }
            }

            return fallback;
        }
    }
}
