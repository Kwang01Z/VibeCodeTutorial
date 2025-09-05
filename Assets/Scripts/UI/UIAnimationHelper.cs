using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UIAnimationHelper - Alternative to DOTween using Unity's built-in animation systems
    /// Provides basic animation functionality without external dependencies
    /// </summary>
    public static class UIAnimationHelper
    {
        #region Animation Extensions

        public static void AnimatePosition(this RectTransform rectTransform, Vector2 targetPosition, float duration, System.Action onComplete = null)
        {
            if (rectTransform != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(AnimatePositionCoroutine(rectTransform, targetPosition, duration, onComplete));
            }
        }

        public static void AnimateScale(this Transform transform, Vector3 targetScale, float duration, System.Action onComplete = null)
        {
            if (transform != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(AnimateScaleCoroutine(transform, targetScale, duration, onComplete));
            }
        }

        public static void AnimateAlpha(this CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete = null)
        {
            if (canvasGroup != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(AnimateAlphaCoroutine(canvasGroup, targetAlpha, duration, onComplete));
            }
        }

        public static void AnimateColor(this Image image, Color targetColor, float duration, System.Action onComplete = null)
        {
            if (image != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(AnimateColorCoroutine(image, targetColor, duration, onComplete));
            }
        }

        public static void AnimateSliderValue(this Slider slider, float targetValue, float duration, System.Action onComplete = null)
        {
            if (slider != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(AnimateSliderCoroutine(slider, targetValue, duration, onComplete));
            }
        }

        public static void PunchScale(this Transform transform, Vector3 punchAmount, float duration)
        {
            if (transform != null)
            {
                MonoBehaviourHelper.Instance.StartCoroutine(PunchScaleCoroutine(transform, punchAmount, duration));
            }
        }

        // Additional methods for UIAnimationManager compatibility
        public static IEnumerator AnimateAnchoredPosition(this RectTransform rectTransform, Vector2 targetPosition, float duration, AnimationCurve easeCurve)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = easeCurve.Evaluate(t);
                
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveT);
                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
        }

        public static IEnumerator AnimateAnchoredPositionX(this RectTransform rectTransform, float targetX, float duration, AnimationCurve easeCurve)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 targetPosition = new Vector2(targetX, startPosition.y);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = easeCurve.Evaluate(t);
                
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveT);
                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
        }

        public static IEnumerator AnimateScale(this RectTransform rectTransform, Vector3 targetScale, float duration, AnimationCurve easeCurve)
        {
            Vector3 startScale = rectTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = easeCurve.Evaluate(t);
                
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveT);
                yield return null;
            }

            rectTransform.localScale = targetScale;
        }

        public static IEnumerator AnimateAlpha(this CanvasGroup canvasGroup, float targetAlpha, float duration, AnimationCurve easeCurve)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = easeCurve.Evaluate(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveT);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        public static IEnumerator AnimateRotation(this RectTransform rectTransform, Vector3 targetRotation, float duration, AnimationCurve easeCurve)
        {
            Vector3 startRotation = rectTransform.localEulerAngles;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = easeCurve.Evaluate(t);
                
                rectTransform.localEulerAngles = Vector3.Lerp(startRotation, targetRotation, curveT);
                yield return null;
            }

            rectTransform.localEulerAngles = targetRotation;
        }

        #endregion

        #region Animation Coroutines

        private static IEnumerator AnimatePositionCoroutine(RectTransform rectTransform, Vector2 targetPosition, float duration, System.Action onComplete)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = EaseOutQuad(t);
                
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
            onComplete?.Invoke();
        }

        private static IEnumerator AnimateScaleCoroutine(Transform transform, Vector3 targetScale, float duration, System.Action onComplete)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = EaseOutBack(t);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
            onComplete?.Invoke();
        }

        private static IEnumerator AnimateAlphaCoroutine(CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = EaseInOutQuad(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private static IEnumerator AnimateColorCoroutine(Image image, Color targetColor, float duration, System.Action onComplete)
        {
            Color startColor = image.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = EaseInOutQuad(t);
                
                image.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            image.color = targetColor;
            onComplete?.Invoke();
        }

        private static IEnumerator AnimateSliderCoroutine(Slider slider, float targetValue, float duration, System.Action onComplete)
        {
            float startValue = slider.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = EaseOutQuad(t);
                
                slider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            slider.value = targetValue;
            onComplete?.Invoke();
        }

        private static IEnumerator PunchScaleCoroutine(Transform transform, Vector3 punchAmount, float duration)
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float intensity = Mathf.Sin(t * Mathf.PI);
                
                Vector3 punchScale = originalScale + punchAmount * intensity;
                transform.localScale = punchScale;
                
                yield return null;
            }

            transform.localScale = originalScale;
        }

        #endregion

        #region Easing Functions

        private static float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        #endregion

        #region Sequence Helper

        public static AnimationSequence CreateSequence()
        {
            return new AnimationSequence();
        }

        #endregion
    }

    /// <summary>
    /// Simple animation sequence to chain multiple animations
    /// </summary>
    public class AnimationSequence
    {
        private List<System.Func<IEnumerator>> _animations = new List<System.Func<IEnumerator>>();
        private System.Action _onCompleteCallback;

        public AnimationSequence Append(System.Func<IEnumerator> animation)
        {
            _animations.Add(animation);
            return this;
        }

        public AnimationSequence OnComplete(System.Action onComplete)
        {
            _onCompleteCallback = onComplete;
            return this;
        }

        public void Play()
        {
            MonoBehaviourHelper.Instance.StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            foreach (var animation in _animations)
            {
                yield return animation();
            }
            _onCompleteCallback?.Invoke();
        }
    }

    /// <summary>
    /// Helper MonoBehaviour to run coroutines for static methods
    /// </summary>
    public class MonoBehaviourHelper : MonoBehaviour
    {
        private static MonoBehaviourHelper _instance;

        public static MonoBehaviourHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIAnimationHelper");
                    _instance = go.AddComponent<MonoBehaviourHelper>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
