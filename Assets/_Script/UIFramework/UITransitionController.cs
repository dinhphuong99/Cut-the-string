using System.Collections;
using UnityEngine;

namespace UIFramework
{
    public static class UITransitionController
    {
        public static IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            float t = 0f;
            cg.alpha = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        public static IEnumerator FadeOut(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            float t = 0f;
            float start = cg.alpha;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(Mathf.Lerp(start, 0f, t / duration));
                yield return null;
            }
            cg.alpha = 0f;
        }
    }
}
