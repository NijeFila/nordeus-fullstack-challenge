// Editor-only validator for the VisualCatalog asset.
//
// Menu: Tools/Nordeus Challenge/Visuals/Validate Visual Catalog
//
// What it does:
//   - Locates the VisualCatalog asset in the project (first one found).
//   - Reads its private serialized lists (heroPortraits, monsterPortraits,
//     moveIcons, effectIcons, itemIcons, statIcons) through SerializedObject.
//   - Reports any expected id that is missing or has a null sprite.
//   - Reports the default* fields that are unassigned.
//   - Never writes to the asset. Never auto-assigns sprites.
//
// The expected id lists are mirrored from the server's RunConfigService.
// If you change the server catalog, regenerate the lists below — there is a
// // SOURCE-OF-TRUTH comment marker on each one to make that easy to find.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NordeusChallenge.Client.Visual;
using UnityEditor;
using UnityEngine;

namespace NordeusChallenge.Client.EditorTools
{
    public static class VisualCatalogValidator
    {
        // SOURCE-OF-TRUTH: hero / class ids on the server.
        private static readonly string[] ExpectedHeroIds =
        {
            "knight", "ranger", "mage", "cleric"
        };

        // SOURCE-OF-TRUTH: monster ids.
        private static readonly string[] ExpectedMonsterIds =
        {
            "goblin_warrior", "goblin_mage", "giant_spider", "skeleton_knight",
            "forest_troll", "witch", "fire_elemental", "dragon"
        };

        // SOURCE-OF-TRUTH: every move id authored in BuildMoves().
        private static readonly string[] ExpectedMoveIds =
        {
            "slash", "shield_up", "battle_cry", "second_wind",
            "power_stance", "iron_skin", "rend",
            "arcane_focus", "blessed_mend", "smite",
            "rusty_blade", "dirty_kick", "frenzy", "headbutt",
            "firebolt", "arcane_surge", "mana_drain", "hex_shield",
            "bite", "web_throw", "pounce", "skitter", "venom_bite",
            "shadow_bolt", "drain_life", "curse", "dark_pact", "bleeding_curse",
            "flame_breath", "claw_swipe", "intimidate", "dragon_scales",
            "bone_slash", "shield_wall", "grave_wound", "death_rattle",
            "heavy_club", "regenerate", "toxic_spores", "thick_hide",
            "flame_burst", "scorching_aura", "ignite", "mana_flare"
        };

        // SOURCE-OF-TRUTH: effect kinds (case-sensitive, capitalised).
        private static readonly string[] ExpectedEffectKinds =
        {
            "Bleed", "Poison", "Heal",
            "BuffAttack", "DebuffAttack",
            "BuffDefense", "DebuffDefense",
            "BuffMagic", "DebuffMagic",
            "DamageIncrease", "DamageReduction"
        };

        // SOURCE-OF-TRUTH: item ids from BuildItems().
        private static readonly string[] ExpectedItemIds =
        {
            "rusty_shortsword", "tattered_leather",
            "apprentice_wand", "mana_charm",
            "chitin_plate", "venom_fang",
            "hex_focus", "lifedrinker_locket",
            "dragonbone_blade", "scale_aegis",
            "bone_pauldrons", "grave_signet",
            "troll_hide_cloak", "ember_core"
        };

        // SOURCE-OF-TRUTH: stat ids passed to GetStatIcon (shop StatUpgrade
        // offers). The level-up panel uses health/mana aliases but does not
        // call GetStatIcon today; the optional list is a future-proof hint.
        private static readonly string[] ExpectedStatIdsRequired =
        {
            "maxHealth", "maxMana", "attack", "defense", "magic"
        };
        private static readonly string[] ExpectedStatIdsOptional =
        {
            "health", "mana"
        };

        [MenuItem("Tools/Nordeus Challenge/Visuals/Validate Visual Catalog")]
        public static void Validate()
        {
            var catalog = FindFirstAssetOfType<VisualCatalog>();
            if (catalog == null)
            {
                Debug.LogWarning("[VisualCatalog] No VisualCatalog asset found in the project. Create one via Assets > Create > Nordeus > Visual Catalog and re-run the validator.");
                return;
            }

            Debug.Log($"[VisualCatalog] Validating asset at {AssetDatabase.GetAssetPath(catalog)}.");

            var so = new SerializedObject(catalog);

            CheckSpriteField(so, "defaultPortrait");
            CheckSpriteField(so, "defaultMoveIcon");
            CheckSpriteField(so, "defaultEffectIcon");
            CheckSpriteField(so, "defaultItemIcon");
            CheckSpriteField(so, "defaultStatIcon");

            CheckList(so, "heroPortraits",     ExpectedHeroIds,                       "Hero portrait");
            CheckList(so, "monsterPortraits",  ExpectedMonsterIds,                    "Monster portrait");
            CheckList(so, "moveIcons",         ExpectedMoveIds,                       "Move icon");
            CheckList(so, "effectIcons",       ExpectedEffectKinds,                   "Effect icon");
            CheckList(so, "itemIcons",         ExpectedItemIds,                       "Item icon");
            CheckList(so, "statIcons",         ExpectedStatIdsRequired,               "Stat icon (required)");
            CheckListOptional(so, "statIcons", ExpectedStatIdsOptional,               "Stat icon (optional)");

            Debug.Log("[VisualCatalog] Validation complete.");
        }

        private static void CheckSpriteField(SerializedObject so, string fieldName)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[VisualCatalog] Field '{fieldName}' not found on the asset.");
                return;
            }
            if (prop.objectReferenceValue == null)
            {
                Debug.LogWarning($"[VisualCatalog] Default '{fieldName}' is unassigned.");
            }
        }

        private static void CheckList(SerializedObject so, string listFieldName, string[] expectedIds, string label)
        {
            var list = so.FindProperty(listFieldName);
            if (list == null || !list.isArray)
            {
                Debug.LogError($"[VisualCatalog] List '{listFieldName}' not found on the asset (expected array).");
                return;
            }

            var presentWithSprite = ReadPresentIds(list, requireSprite: true);
            var presentEntries = ReadPresentIds(list, requireSprite: false);
            var missing = new List<string>();
            var spriteMissing = new List<string>();

            foreach (var id in expectedIds)
            {
                if (!presentEntries.Contains(id))
                {
                    missing.Add(id);
                }
                else if (!presentWithSprite.Contains(id))
                {
                    spriteMissing.Add(id);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning($"[VisualCatalog] {label}: {missing.Count} missing entries → {string.Join(", ", missing)}");
            }
            if (spriteMissing.Count > 0)
            {
                Debug.LogWarning($"[VisualCatalog] {label}: {spriteMissing.Count} entries have no sprite assigned → {string.Join(", ", spriteMissing)}");
            }
            if (missing.Count == 0 && spriteMissing.Count == 0)
            {
                Debug.Log($"[VisualCatalog] {label}: OK ({expectedIds.Length} entries).");
            }

            // Surface extra ids that aren't in the expected list (typo risk).
            var unexpected = presentEntries.Where(id => !expectedIds.Contains(id) && !ExpectedStatIdsOptional.Contains(id)).ToList();
            if (label.StartsWith("Stat icon"))
            {
                // Stat list shares optional ids; suppress noise from required pass.
                return;
            }
            if (unexpected.Count > 0)
            {
                Debug.Log($"[VisualCatalog] {label}: {unexpected.Count} unexpected id(s) on the asset (typo or stale entry?) → {string.Join(", ", unexpected)}");
            }
        }

        private static void CheckListOptional(SerializedObject so, string listFieldName, string[] optionalIds, string label)
        {
            var list = so.FindProperty(listFieldName);
            if (list == null || !list.isArray) return;
            var presentWithSprite = ReadPresentIds(list, requireSprite: true);
            var notFilled = optionalIds.Where(id => !presentWithSprite.Contains(id)).ToList();
            if (notFilled.Count > 0)
            {
                Debug.Log($"[VisualCatalog] {label}: optional ids without sprites → {string.Join(", ", notFilled)} (safe to skip).");
            }
        }

        private static HashSet<string> ReadPresentIds(SerializedProperty list, bool requireSprite)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var idProp = entry.FindPropertyRelative("id");
                var spriteProp = entry.FindPropertyRelative("sprite");
                if (idProp == null) continue;
                string id = idProp.stringValue;
                if (string.IsNullOrEmpty(id)) continue;
                if (requireSprite && (spriteProp == null || spriteProp.objectReferenceValue == null)) continue;
                set.Add(id);
            }
            return set;
        }

        private static T FindFirstAssetOfType<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
            return null;
        }
    }
}
#endif
