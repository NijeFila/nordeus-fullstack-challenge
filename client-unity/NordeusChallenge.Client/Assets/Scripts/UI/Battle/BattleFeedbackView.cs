using System.Collections;
using NordeusChallenge.Client.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Battle
{
    public class BattleFeedbackView : MonoBehaviour
    {
        [SerializeField] private FloatingCombatText floatingTextPrefab;

        [Header("Hit")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float hitFlashDuration = 0.22f;
        [SerializeField] private float hitShakeDuration = 0.2f;
        [SerializeField] private float hitShakeMagnitude = 8f;
        [SerializeField] private Color damageTextColor = new Color(1f, 0.4f, 0.4f);

        [Header("Heal")]
        [SerializeField] private Color healFlashColor = new Color(0.45f, 1f, 0.55f, 1f);
        [SerializeField] private float healFlashDuration = 0.25f;
        [SerializeField] private float healPunchDuration = 0.25f;
        [SerializeField] private float healPunchScale = 1.1f;
        [SerializeField] private Color healTextColor = new Color(0.5f, 1f, 0.6f);

        public void PlayHit(CombatantVisuals target, int damage)
        {
            if (target == null) return;
            if (target.portrait != null) StartCoroutine(FlashColor(target.portrait, hitFlashColor, hitFlashDuration));
            if (target.shakeRoot != null) StartCoroutine(Shake(target.shakeRoot, hitShakeDuration, hitShakeMagnitude));
            SpawnFloatingText(target, $"-{damage}", damageTextColor);
        }

        public void PlayHeal(CombatantVisuals target, int amount)
        {
            if (target == null) return;
            if (target.portrait != null) StartCoroutine(FlashColor(target.portrait, healFlashColor, healFlashDuration));
            if (target.shakeRoot != null) StartCoroutine(ScalePunch(target.shakeRoot, healPunchDuration, healPunchScale));
            SpawnFloatingText(target, $"+{amount}", healTextColor);
        }

        public void SpawnFloatingText(CombatantVisuals target, string value, Color color)
        {
            if (target == null || target.floatingTextAnchor == null || floatingTextPrefab == null)
            {
                return;
            }

            var instance = Instantiate(floatingTextPrefab, target.floatingTextAnchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one;
            instance.Play(value, color);
        }

        private static IEnumerator FlashColor(Image image, Color flash, float duration)
        {
            Color original = image.color;
            image.color = flash;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                image.color = Color.Lerp(flash, original, Mathf.Clamp01(t / duration));
                yield return null;
            }
            image.color = original;
        }

        private static IEnumerator Shake(RectTransform rt, float duration, float magnitude)
        {
            Vector3 origin = rt.localPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float damping = 1f - Mathf.Clamp01(t / duration);
                float x = (Random.value * 2f - 1f) * magnitude * damping;
                float y = (Random.value * 2f - 1f) * magnitude * damping;
                rt.localPosition = origin + new Vector3(x, y, 0f);
                yield return null;
            }
            rt.localPosition = origin;
        }

        private static IEnumerator ScalePunch(RectTransform rt, float duration, float peakScale)
        {
            Vector3 origin = rt.localScale;
            Vector3 peak = origin * peakScale;
            float half = Mathf.Max(0.0001f, duration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(origin, peak, Mathf.Clamp01(t / half));
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(peak, origin, Mathf.Clamp01(t / half));
                yield return null;
            }
            rt.localScale = origin;
        }
    }
}
