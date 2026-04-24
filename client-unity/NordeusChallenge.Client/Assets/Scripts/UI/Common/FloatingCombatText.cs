using System.Collections;
using TMPro;
using UnityEngine;

namespace NordeusChallenge.Client.UI.Common
{
    public class FloatingCombatText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float riseDistance = 60f;

        public void Play(string value, Color color)
        {
            if (text != null)
            {
                text.text = value;
                text.color = color;
            }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var rt = (RectTransform)transform;
            Vector3 start = rt.localPosition;
            Vector3 end = start + new Vector3(0f, riseDistance, 0f);

            float t = 0f;
            while (t < lifetime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / lifetime);
                rt.localPosition = Vector3.Lerp(start, end, p);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - p;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
