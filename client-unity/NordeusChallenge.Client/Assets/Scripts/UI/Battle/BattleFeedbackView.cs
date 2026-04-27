using System.Collections;
using System.Collections.Generic;
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

        // Per-target baselines captured the first time we touch each transform.
        // Without these, rapid clicks would capture an already-mutated localScale
        // / localPosition as the "origin" and the visual would drift permanently.
        private readonly Dictionary<RectTransform, Vector3> _baselineScale = new();
        private readonly Dictionary<RectTransform, Vector3> _baselinePosition = new();

        // Tracks the running coroutine per target so a new feedback request
        // cancels the previous one instead of stacking on the same transform.
        private readonly Dictionary<RectTransform, Coroutine> _runningShake = new();
        private readonly Dictionary<RectTransform, Coroutine> _runningScale = new();
        private readonly Dictionary<Image, Coroutine> _runningFlash = new();
        private readonly Dictionary<Image, Color> _baselineColor = new();

        public void PlayHit(CombatantVisuals target, int damage)
        {
            if (target == null) return;
            if (target.portrait != null) StartFlash(target.portrait, hitFlashColor, hitFlashDuration);
            if (target.shakeRoot != null) StartShake(target.shakeRoot, hitShakeDuration, hitShakeMagnitude);
            SpawnFloatingText(target, $"-{damage}", damageTextColor);
        }

        public void PlayHeal(CombatantVisuals target, int amount)
        {
            if (target == null) return;
            if (target.portrait != null) StartFlash(target.portrait, healFlashColor, healFlashDuration);
            if (target.shakeRoot != null) StartScalePunch(target.shakeRoot, healPunchDuration, healPunchScale);
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

        // Make sure that if the view is disabled mid-feedback (scene change,
        // battle end), every transform we mutated is restored.
        private void OnDisable()
        {
            foreach (var pair in _baselineScale)
            {
                if (pair.Key != null) pair.Key.localScale = pair.Value;
            }
            foreach (var pair in _baselinePosition)
            {
                if (pair.Key != null) pair.Key.localPosition = pair.Value;
            }
            foreach (var pair in _baselineColor)
            {
                if (pair.Key != null) pair.Key.color = pair.Value;
            }
            _runningShake.Clear();
            _runningScale.Clear();
            _runningFlash.Clear();
        }

        private Vector3 GetOrCacheBaselineScale(RectTransform rt)
        {
            if (!_baselineScale.TryGetValue(rt, out var baseline))
            {
                baseline = rt.localScale;
                _baselineScale[rt] = baseline;
            }
            return baseline;
        }

        private Vector3 GetOrCacheBaselinePosition(RectTransform rt)
        {
            if (!_baselinePosition.TryGetValue(rt, out var baseline))
            {
                baseline = rt.localPosition;
                _baselinePosition[rt] = baseline;
            }
            return baseline;
        }

        private Color GetOrCacheBaselineColor(Image image)
        {
            if (!_baselineColor.TryGetValue(image, out var baseline))
            {
                baseline = image.color;
                _baselineColor[image] = baseline;
            }
            return baseline;
        }

        private void StartFlash(Image image, Color flash, float duration)
        {
            Color baseline = GetOrCacheBaselineColor(image);
            if (_runningFlash.TryGetValue(image, out var running) && running != null)
            {
                StopCoroutine(running);
                image.color = baseline;
            }
            _runningFlash[image] = StartCoroutine(FlashColor(image, baseline, flash, duration));
        }

        private void StartShake(RectTransform rt, float duration, float magnitude)
        {
            Vector3 baseline = GetOrCacheBaselinePosition(rt);
            if (_runningShake.TryGetValue(rt, out var running) && running != null)
            {
                StopCoroutine(running);
                rt.localPosition = baseline;
            }
            _runningShake[rt] = StartCoroutine(Shake(rt, baseline, duration, magnitude));
        }

        private void StartScalePunch(RectTransform rt, float duration, float peakScale)
        {
            Vector3 baseline = GetOrCacheBaselineScale(rt);
            // Reset to baseline before launching a new punch so the second
            // animation never compounds on top of the first.
            if (_runningScale.TryGetValue(rt, out var running) && running != null)
            {
                StopCoroutine(running);
                rt.localScale = baseline;
            }
            _runningScale[rt] = StartCoroutine(ScalePunch(rt, baseline, duration, peakScale));
        }

        private IEnumerator FlashColor(Image image, Color baseline, Color flash, float duration)
        {
            try
            {
                image.color = flash;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    if (image == null) yield break;
                    image.color = Color.Lerp(flash, baseline, Mathf.Clamp01(t / duration));
                    yield return null;
                }
            }
            finally
            {
                if (image != null) image.color = baseline;
                _runningFlash.Remove(image);
            }
        }

        private IEnumerator Shake(RectTransform rt, Vector3 baseline, float duration, float magnitude)
        {
            try
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    if (rt == null) yield break;
                    float damping = 1f - Mathf.Clamp01(t / duration);
                    float x = (Random.value * 2f - 1f) * magnitude * damping;
                    float y = (Random.value * 2f - 1f) * magnitude * damping;
                    rt.localPosition = baseline + new Vector3(x, y, 0f);
                    yield return null;
                }
            }
            finally
            {
                if (rt != null) rt.localPosition = baseline;
                _runningShake.Remove(rt);
            }
        }

        private IEnumerator ScalePunch(RectTransform rt, Vector3 baseline, float duration, float peakScale)
        {
            try
            {
                Vector3 peak = baseline * peakScale;
                float half = Mathf.Max(0.0001f, duration * 0.5f);

                float t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    if (rt == null) yield break;
                    rt.localScale = Vector3.Lerp(baseline, peak, Mathf.Clamp01(t / half));
                    yield return null;
                }
                t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    if (rt == null) yield break;
                    rt.localScale = Vector3.Lerp(peak, baseline, Mathf.Clamp01(t / half));
                    yield return null;
                }
            }
            finally
            {
                if (rt != null) rt.localScale = baseline;
                _runningScale.Remove(rt);
            }
        }
    }
}
