using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NordeusChallenge.Client.UI.Common
{
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private float animateDuration = 0.25f;

        private int _current;
        private int _max = 1;
        private Coroutine _animation;

        public void SetImmediate(int current, int max)
        {
            StopAnimation();
            _max = Mathf.Max(1, max);
            _current = Mathf.Clamp(current, 0, _max);
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = (float)_current / _max;
            }
            UpdateFillColor();
            UpdateLabel();
        }

        public void SetValue(int current, int max)
        {
            _max = Mathf.Max(1, max);
            _current = Mathf.Clamp(current, 0, _max);
            UpdateLabel();

            if (slider == null)
            {
                return;
            }

            float target = (float)_current / _max;
            StopAnimation();
            if (!gameObject.activeInHierarchy || animateDuration <= 0f)
            {
                slider.value = target;
                UpdateFillColor();
                return;
            }
            _animation = StartCoroutine(Animate(slider.value, target));
        }

        private IEnumerator Animate(float from, float to)
        {
            float t = 0f;
            while (t < animateDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / animateDuration);
                slider.value = Mathf.Lerp(from, to, p);
                UpdateFillColor();
                yield return null;
            }
            slider.value = to;
            UpdateFillColor();
            _animation = null;
        }

        private void StopAnimation()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        private void UpdateFillColor()
        {
            if (fillImage == null || slider == null || healthGradient == null)
            {
                return;
            }
            fillImage.color = healthGradient.Evaluate(slider.value);
        }

        private void UpdateLabel()
        {
            if (label != null)
            {
                label.text = $"{_current} / {_max}";
            }
        }
    }
}
