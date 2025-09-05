using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Items;
using EndlessRunner.Gameplay;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UIAnimationManager - Advanced UI animation system using Unity built-in animations
    /// Handles smooth transitions, effect notifications, score animations, and UI feedback
    /// </summary>
    public class UIAnimationManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Transform _notificationParent;
        [SerializeField] private Transform _floatingTextParent;
        [SerializeField] private Transform _effectIconParent;

        [Header("Notification System")]
        [SerializeField] private GameObject _notificationPrefab;
        [SerializeField] private float _notificationDuration = 3f;
        [SerializeField] private int _maxConcurrentNotifications = 5;
        [SerializeField] private Vector2 _notificationSpacing = new Vector2(0, 100);

        [Header("Floating Text")]
        [SerializeField] private GameObject _floatingTextPrefab;
        [SerializeField] private float _floatingTextDuration = 2f;
        [SerializeField] private AnimationCurve _floatingTextCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Effect Icons")]
        [SerializeField] private GameObject _effectIconPrefab;
        [SerializeField] private float _effectIconDuration = 0.8f;
        [SerializeField] private Vector2 _effectIconSize = new Vector2(64, 64);

        [Header("Screen Transitions")]
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField] private float _fadeTransitionDuration = 1f;
        [SerializeField] private AnimationCurve _fadeEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("UI Element Animations")]
        [SerializeField] private float _defaultAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve _defaultEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private float _punchDuration = 0.2f;

        [Header("Performance")]
        [SerializeField] private int _maxFloatingTexts = 20;
        [SerializeField] private int _maxEffectIcons = 15;
        [SerializeField] private bool _recycleAnimations = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _visualizeAnimations = false;

        #endregion

        #region Private Fields

        // Animation pools
        private Queue<UINotification> _notificationPool = new Queue<UINotification>();
        private Queue<UIFloatingText> _floatingTextPool = new Queue<UIFloatingText>();
        private Queue<UIEffectIcon> _effectIconPool = new Queue<UIEffectIcon>();

        // Active animations
        private List<UINotification> _activeNotifications = new List<UINotification>();
        private List<UIFloatingText> _activeFloatingTexts = new List<UIFloatingText>();
        private List<UIEffectIcon> _activeEffectIcons = new List<UIEffectIcon>();

        // Animation tracking
        private Dictionary<string, Coroutine> _runningCoroutines = new Dictionary<string, Coroutine>();
        private Dictionary<Transform, List<Coroutine>> _activeAnimations = new Dictionary<Transform, List<Coroutine>>();

        // UI state tracking
        private bool _isTransitioning = false;
        private UIAnimationState _currentState = UIAnimationState.Idle;

        // Cached components
        private UnityEngine.Camera _uiCamera;
        private RectTransform _canvasRect;

        // Animation stats
        private int _totalAnimationsPlayed = 0;
        private float _averageAnimationDuration = 0f;

        #endregion

        #region Properties

        public bool IsTransitioning => _isTransitioning;
        public int ActiveAnimationCount => _activeNotifications.Count + _activeFloatingTexts.Count + _activeEffectIcons.Count;
        public UIAnimationState CurrentState => _currentState;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAnimationSystem();
            CacheReferences();
            CreateAnimationPools();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdateActiveAnimations();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupAnimations();
        }

        #endregion

        #region Initialization

        private void InitializeAnimationSystem()
        {
            _currentState = UIAnimationState.Idle;
            _totalAnimationsPlayed = 0;
            _averageAnimationDuration = 0f;
            
            LogDebug("[UIAnimationManager] Animation system initialized with Unity built-in animations");
        }

        private void CacheReferences()
        {
            if (_mainCanvas == null)
                _mainCanvas = FindObjectOfType<Canvas>();

            _canvasRect = _mainCanvas.GetComponent<RectTransform>();
            _uiCamera = _mainCanvas.worldCamera ?? UnityEngine.Camera.main;

            CreateParentContainers();
        }

        private void CreateParentContainers()
        {
            if (_notificationParent == null)
            {
                GameObject notificationGO = new GameObject("NotificationParent");
                notificationGO.transform.SetParent(_mainCanvas.transform, false);
                _notificationParent = notificationGO.transform;
                
                RectTransform rect = notificationGO.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-20, -20);
            }

            if (_floatingTextParent == null)
            {
                GameObject floatingTextGO = new GameObject("FloatingTextParent");
                floatingTextGO.transform.SetParent(_mainCanvas.transform, false);
                _floatingTextParent = floatingTextGO.transform;
            }

            if (_effectIconParent == null)
            {
                GameObject effectIconGO = new GameObject("EffectIconParent");
                effectIconGO.transform.SetParent(_mainCanvas.transform, false);
                _effectIconParent = effectIconGO.transform;
            }
        }

        private void CreateAnimationPools()
        {
            for (int i = 0; i < _maxConcurrentNotifications; i++)
                CreatePooledNotification();

            for (int i = 0; i < _maxFloatingTexts; i++)
                CreatePooledFloatingText();

            for (int i = 0; i < _maxEffectIcons; i++)
                CreatePooledEffectIcon();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            ItemEffectEvents.OnEffectStarted.AddListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.AddListener(OnEffectStacked);
            ItemPickup.OnItemPicked += OnItemPicked;
        }

        private void UnsubscribeFromEvents()
        {
            ItemEffectEvents.OnEffectStarted.RemoveListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.RemoveListener(OnEffectStacked);
            ItemPickup.OnItemPicked -= OnItemPicked;
        }

        #endregion

        #region Event Handlers

        private void OnEffectStarted(ActiveItemEffect effect)
        {
            ShowNotification($"{effect.ItemDefinition.DisplayName} activated!", 
                           effect.ItemDefinition.EffectColor, 
                           NotificationType.EffectStarted);

            ShowEffectIcon(effect.ItemDefinition.Type, effect.ItemDefinition.EffectColor);
            AnimateEffectUI(effect.ItemDefinition.Type, true);
        }

        private void OnEffectEnded(ItemType itemType, string itemId)
        {
            ShowNotification($"{itemType} ended", Color.gray, NotificationType.EffectEnded);
            AnimateEffectUI(itemType, false);
        }

        private void OnEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            ShowNotification($"{effect.ItemDefinition.DisplayName} x{effect.StackCount}!", 
                           effect.ItemDefinition.EffectColor, 
                           NotificationType.EffectStacked);

            Vector3 screenPos = GetStackUIPosition(effect.ItemDefinition.Type);
            ShowFloatingText($"+{effect.StackCount - previousStackCount}", 
                           screenPos, 
                           effect.ItemDefinition.EffectColor);
        }

        private void OnItemPicked(ItemDefinition itemDefinition, Vector3 worldPosition)
        {
            Vector3 screenPos = WorldToUIPosition(worldPosition);
            
            ShowFloatingText($"+{itemDefinition.DisplayName}", 
                           screenPos, 
                           itemDefinition.EffectColor);

            AnimateScoreIncrease();
        }

        private void OnScoreChanged(int newScore, int previousScore)
        {
            int difference = newScore - previousScore;
            if (difference > 0)
            {
                ShowFloatingText($"+{difference}", GetScoreUIPosition(), Color.yellow);
                AnimateScoreUI();
            }
        }

        private void OnHealthChanged(float newHealth, float maxHealth, float previousHealth)
        {
            float healthPercentage = newHealth / maxHealth;
            
            if (newHealth < previousHealth)
                FlashScreen(Color.red, 0.3f, 0.2f);
            else if (newHealth > previousHealth)
                FlashScreen(Color.green, 0.2f, 0.15f);

            AnimateHealthBar(healthPercentage);
        }

        #endregion

        #region Notification System

        public void ShowNotification(string message, Color color, NotificationType type = NotificationType.Info)
        {
            var notification = GetPooledNotification();
            if (notification == null) return;

            ConfigureNotification(notification, message, color, type);
            StartCoroutine(AnimateNotificationIn(notification));
            _activeNotifications.Add(notification);

            StartCoroutine(RemoveNotificationAfterDelay(notification, _notificationDuration));
            LogDebug($"[UIAnimationManager] Showing notification: {message}");
        }

        private void ConfigureNotification(UINotification notification, string message, Color color, NotificationType type)
        {
            notification.SetMessage(message);
            notification.SetColor(color);
            notification.SetType(type);
            notification.gameObject.SetActive(true);

            PositionNotification(notification);
        }

        private void PositionNotification(UINotification notification)
        {
            int index = _activeNotifications.Count;
            Vector2 targetPosition = new Vector2(0, -index * _notificationSpacing.y);
            notification.RectTransform.anchoredPosition = targetPosition + Vector2.right * 300;
        }

        private IEnumerator AnimateNotificationIn(UINotification notification)
        {
            var rectTransform = notification.RectTransform;
            var canvasGroup = notification.CanvasGroup;
            Vector2 targetPos = new Vector2(0, rectTransform.anchoredPosition.y);

            // Set initial values
            rectTransform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            // Start all animations simultaneously
            var slideCoroutine = StartCoroutine(rectTransform.AnimateAnchoredPosition(targetPos, _defaultAnimationDuration, _defaultEaseCurve));
            var scaleCoroutine = StartCoroutine(rectTransform.AnimateScale(Vector3.one, _defaultAnimationDuration, _defaultEaseCurve));
            var fadeCoroutine = StartCoroutine(canvasGroup.AnimateAlpha(1f, _defaultAnimationDuration * 0.5f, _defaultEaseCurve));

            // Wait for all animations to complete
            yield return slideCoroutine;
            yield return scaleCoroutine;
            yield return fadeCoroutine;

            _totalAnimationsPlayed++;
        }

        private IEnumerator AnimateNotificationOut(UINotification notification)
        {
            var rectTransform = notification.RectTransform;
            var canvasGroup = notification.CanvasGroup;

            // Animate out simultaneously
            var fadeCoroutine = StartCoroutine(canvasGroup.AnimateAlpha(0f, _defaultAnimationDuration * 0.5f, _defaultEaseCurve));
            var slideCoroutine = StartCoroutine(rectTransform.AnimateAnchoredPositionX(300f, _defaultAnimationDuration, _defaultEaseCurve));
            var scaleCoroutine = StartCoroutine(rectTransform.AnimateScale(Vector3.zero, _defaultAnimationDuration, _defaultEaseCurve));

            yield return fadeCoroutine;
            yield return slideCoroutine;
            yield return scaleCoroutine;

            ReturnNotificationToPool(notification);
        }

        private IEnumerator RemoveNotificationAfterDelay(UINotification notification, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_activeNotifications.Contains(notification))
                RemoveNotification(notification);
        }

        private void RemoveNotification(UINotification notification)
        {
            _activeNotifications.Remove(notification);
            StartCoroutine(AnimateNotificationOut(notification));
            RepositionNotifications();
        }

        private void RepositionNotifications()
        {
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                var notification = _activeNotifications[i];
                Vector2 targetPos = new Vector2(0, -i * _notificationSpacing.y);
                StartCoroutine(notification.RectTransform.AnimateAnchoredPosition(targetPos, _defaultAnimationDuration * 0.5f, _defaultEaseCurve));
            }
        }

        #endregion

        #region Floating Text System

        public void ShowFloatingText(string text, Vector3 screenPosition, Color color)
        {
            var floatingText = GetPooledFloatingText();
            if (floatingText == null) return;

            ConfigureFloatingText(floatingText, text, screenPosition, color);
            StartCoroutine(AnimateFloatingText(floatingText));
            _activeFloatingTexts.Add(floatingText);

            LogDebug($"[UIAnimationManager] Showing floating text: {text}");
        }

        private void ConfigureFloatingText(UIFloatingText floatingText, string text, Vector3 screenPosition, Color color)
        {
            floatingText.SetText(text);
            floatingText.SetColor(color);
            floatingText.gameObject.SetActive(true);

            Vector2 uiPosition = ScreenToUIPosition(screenPosition);
            floatingText.RectTransform.anchoredPosition = uiPosition;
        }

        private IEnumerator AnimateFloatingText(UIFloatingText floatingText)
        {
            var rectTransform = floatingText.RectTransform;
            var canvasGroup = floatingText.CanvasGroup;
            var startPos = rectTransform.anchoredPosition;
            var endPos = startPos + Vector2.up * 100f;

            // Set initial values
            rectTransform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            // Start animations
            var moveCoroutine = StartCoroutine(rectTransform.AnimateAnchoredPosition(endPos, _floatingTextDuration, _floatingTextCurve));
            var scaleCoroutine = StartCoroutine(rectTransform.AnimateScale(Vector3.one, _floatingTextDuration * 0.2f, _defaultEaseCurve));
            
            // Fade in
            yield return StartCoroutine(canvasGroup.AnimateAlpha(1f, _floatingTextDuration * 0.1f, _defaultEaseCurve));
            
            // Wait 
            yield return new WaitForSeconds(_floatingTextDuration * 0.7f);
            
            // Fade out
            var fadeOutCoroutine = StartCoroutine(canvasGroup.AnimateAlpha(0f, _floatingTextDuration * 0.2f, _defaultEaseCurve));
            
            yield return moveCoroutine;
            yield return scaleCoroutine;
            yield return fadeOutCoroutine;

            ReturnFloatingTextToPool(floatingText);
        }

        #endregion

        #region Effect Icon System

        public void ShowEffectIcon(ItemType itemType, Color color)
        {
            var effectIcon = GetPooledEffectIcon();
            if (effectIcon == null) return;

            ConfigureEffectIcon(effectIcon, itemType, color);
            StartCoroutine(AnimateEffectIcon(effectIcon));
            _activeEffectIcons.Add(effectIcon);

            LogDebug($"[UIAnimationManager] Showing effect icon: {itemType}");
        }

        private void ConfigureEffectIcon(UIEffectIcon effectIcon, ItemType itemType, Color color)
        {
            effectIcon.SetIcon(GetItemTypeSprite(itemType));
            effectIcon.SetColor(color);
            effectIcon.gameObject.SetActive(true);

            effectIcon.RectTransform.anchoredPosition = Vector2.zero;
            effectIcon.RectTransform.sizeDelta = _effectIconSize;
        }

        private IEnumerator AnimateEffectIcon(UIEffectIcon effectIcon)
        {
            var rectTransform = effectIcon.RectTransform;
            var canvasGroup = effectIcon.CanvasGroup;
            
            // Set initial values
            rectTransform.localScale = Vector3.one * 2f;
            canvasGroup.alpha = 1f;

            // Scale down animation
            var scaleCoroutine = StartCoroutine(rectTransform.AnimateScale(Vector3.one, _effectIconDuration * 0.3f, _defaultEaseCurve));
            
            // Rotation animation
            var rotateCoroutine = StartCoroutine(rectTransform.AnimateRotation(new Vector3(0, 0, 360), _effectIconDuration, AnimationCurve.Linear(0, 0, 1, 1)));
            
            // Wait a bit, then move to final position
            yield return new WaitForSeconds(_effectIconDuration * 0.2f);
            
            Vector2 targetPos = GetEffectIconFinalPosition(effectIcon.ItemType);
            var moveCoroutine = StartCoroutine(rectTransform.AnimateAnchoredPosition(targetPos, _effectIconDuration * 0.8f, _defaultEaseCurve));
            
            // Wait more, then fade out
            yield return new WaitForSeconds(_effectIconDuration * 0.5f);
            
            var fadeCoroutine = StartCoroutine(canvasGroup.AnimateAlpha(0f, _effectIconDuration * 0.3f, _defaultEaseCurve));

            yield return scaleCoroutine;
            yield return rotateCoroutine;
            yield return moveCoroutine;
            yield return fadeCoroutine;

            ReturnEffectIconToPool(effectIcon);
        }

        #endregion

        #region UI Element Animations

        public void AnimateButton(Button button, UIAnimationType animationType = UIAnimationType.Punch)
        {
            if (button == null) return;

            var rectTransform = button.transform as RectTransform;
            
            switch (animationType)
            {
                case UIAnimationType.Punch:
                    StartCoroutine(AnimatePunchScale(rectTransform, Vector3.one * 0.1f, _punchDuration));
                    break;
                    
                case UIAnimationType.Bounce:
                    StartCoroutine(AnimateBounce(rectTransform));
                    break;
                    
                case UIAnimationType.Shake:
                    StartCoroutine(AnimateShake(rectTransform, 10f, _punchDuration));
                    break;
            }

            LogDebug($"[UIAnimationManager] Animated button: {button.name}");
        }

        public void AnimateSlider(Slider slider, float targetValue, float duration = -1f)
        {
            if (slider == null) return;

            float animDuration = duration > 0 ? duration : _defaultAnimationDuration;
            StartCoroutine(AnimateSliderValue(slider, targetValue, animDuration));
            LogDebug($"[UIAnimationManager] Animated slider to: {targetValue}");
        }

        public void AnimateText(TextMeshProUGUI text, string targetText, float duration = -1f)
        {
            if (text == null) return;

            float animDuration = duration > 0 ? duration : _defaultAnimationDuration;
            StartCoroutine(AnimateTextValue(text, targetText, animDuration));
            LogDebug($"[UIAnimationManager] Animated text to: {targetText}");
        }

        #endregion

        #region Screen Effects

        public void FlashScreen(Color flashColor, float intensity = 0.5f, float duration = 0.2f)
        {
            if (_fadeOverlay == null) return;

            StartCoroutine(FlashScreenCoroutine(flashColor, intensity, duration));
        }

        private IEnumerator FlashScreenCoroutine(Color flashColor, float intensity, float duration)
        {
            _fadeOverlay.gameObject.SetActive(true);
            
            var overlayImage = _fadeOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.color = flashColor;

            _fadeOverlay.alpha = 0f;
            
            // Flash in
            yield return StartCoroutine(_fadeOverlay.AnimateAlpha(intensity, duration * 0.1f, _fadeEaseCurve));
            
            // Flash out
            yield return StartCoroutine(_fadeOverlay.AnimateAlpha(0f, duration * 0.9f, _fadeEaseCurve));
            
            _fadeOverlay.gameObject.SetActive(false);
        }

        public void FadeToBlack(System.Action onComplete = null) => FadeScreen(Color.black, 1f, _fadeTransitionDuration, onComplete);
        public void FadeFromBlack(System.Action onComplete = null) => FadeScreen(Color.black, 0f, _fadeTransitionDuration, onComplete);

        public void FadeScreen(Color fadeColor, float targetAlpha, float duration, System.Action onComplete = null)
        {
            if (_fadeOverlay == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(FadeScreenCoroutine(fadeColor, targetAlpha, duration, onComplete));
        }

        private IEnumerator FadeScreenCoroutine(Color fadeColor, float targetAlpha, float duration, System.Action onComplete)
        {
            _isTransitioning = true;
            _fadeOverlay.gameObject.SetActive(true);
            
            var overlayImage = _fadeOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.color = fadeColor;

            yield return StartCoroutine(_fadeOverlay.AnimateAlpha(targetAlpha, duration, _fadeEaseCurve));
            
            _isTransitioning = false;
            if (targetAlpha <= 0f)
                _fadeOverlay.gameObject.SetActive(false);
                
            onComplete?.Invoke();
        }

        #endregion

        #region Animation Helpers

        private IEnumerator AnimatePunchScale(RectTransform rectTransform, Vector3 punch, float duration)
        {
            Vector3 originalScale = rectTransform.localScale;
            Vector3 targetScale = originalScale + punch;
            
            yield return StartCoroutine(rectTransform.AnimateScale(targetScale, duration * 0.5f, _defaultEaseCurve));
            yield return StartCoroutine(rectTransform.AnimateScale(originalScale, duration * 0.5f, _defaultEaseCurve));
        }

        private IEnumerator AnimateBounce(RectTransform rectTransform)
        {
            Vector3 originalScale = rectTransform.localScale;
            Vector3 targetScale = originalScale * _punchScale;
            
            yield return StartCoroutine(rectTransform.AnimateScale(targetScale, _punchDuration * 0.5f, _defaultEaseCurve));
            yield return StartCoroutine(rectTransform.AnimateScale(originalScale, _punchDuration * 0.5f, _defaultEaseCurve));
        }

        private IEnumerator AnimateShake(RectTransform rectTransform, float strength, float duration)
        {
            Vector2 originalPos = rectTransform.anchoredPosition;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = originalPos.x + Random.Range(-strength, strength);
                float y = originalPos.y + Random.Range(-strength, strength);
                rectTransform.anchoredPosition = new Vector2(x, y);
                yield return null;
            }
            
            rectTransform.anchoredPosition = originalPos;
        }

        private IEnumerator AnimateSliderValue(Slider slider, float targetValue, float duration)
        {
            float startValue = slider.value;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = _defaultEaseCurve.Evaluate(t);
                slider.value = Mathf.Lerp(startValue, targetValue, curveT);
                yield return null;
            }
            
            slider.value = targetValue;
        }

        private IEnumerator AnimateTextValue(TextMeshProUGUI text, string targetText, float duration)
        {
            string startText = text.text;
            int targetLength = targetText.Length;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int currentLength = Mathf.RoundToInt(t * targetLength);
                text.text = targetText.Substring(0, currentLength);
                yield return null;
            }
            
            text.text = targetText;
        }

        #endregion

        #region Animation Pools & Utility Methods

        private UINotification GetPooledNotification()
        {
            if (_notificationPool.Count > 0)
                return _notificationPool.Dequeue();
            else if (_recycleAnimations)
                return CreatePooledNotification();
            return null;
        }

        private void ReturnNotificationToPool(UINotification notification)
        {
            if (notification == null) return;
            
            notification.gameObject.SetActive(false);
            _activeNotifications.Remove(notification);
            _notificationPool.Enqueue(notification);
        }

        private UINotification CreatePooledNotification()
        {
            if (_notificationPrefab == null) return null;

            GameObject notificationGO = Instantiate(_notificationPrefab, _notificationParent);
            var notification = notificationGO.GetComponent<UINotification>();
            
            if (notification == null)
                notification = notificationGO.AddComponent<UINotification>();

            notification.gameObject.SetActive(false);
            _notificationPool.Enqueue(notification);
            
            return notification;
        }

        private UIFloatingText GetPooledFloatingText()
        {
            if (_floatingTextPool.Count > 0)
                return _floatingTextPool.Dequeue();
            else if (_recycleAnimations)
                return CreatePooledFloatingText();
            return null;
        }

        private void ReturnFloatingTextToPool(UIFloatingText floatingText)
        {
            if (floatingText == null) return;
            
            floatingText.gameObject.SetActive(false);
            _activeFloatingTexts.Remove(floatingText);
            _floatingTextPool.Enqueue(floatingText);
        }

        private UIFloatingText CreatePooledFloatingText()
        {
            if (_floatingTextPrefab == null) return null;

            GameObject floatingTextGO = Instantiate(_floatingTextPrefab, _floatingTextParent);
            var floatingText = floatingTextGO.GetComponent<UIFloatingText>();
            
            if (floatingText == null)
                floatingText = floatingTextGO.AddComponent<UIFloatingText>();

            floatingText.gameObject.SetActive(false);
            _floatingTextPool.Enqueue(floatingText);
            
            return floatingText;
        }

        private UIEffectIcon GetPooledEffectIcon()
        {
            if (_effectIconPool.Count > 0)
                return _effectIconPool.Dequeue();
            else if (_recycleAnimations)
                return CreatePooledEffectIcon();
            return null;
        }

        private void ReturnEffectIconToPool(UIEffectIcon effectIcon)
        {
            if (effectIcon == null) return;
            
            effectIcon.gameObject.SetActive(false);
            _activeEffectIcons.Remove(effectIcon);
            _effectIconPool.Enqueue(effectIcon);
        }

        private UIEffectIcon CreatePooledEffectIcon()
        {
            if (_effectIconPrefab == null) return null;

            GameObject effectIconGO = Instantiate(_effectIconPrefab, _effectIconParent);
            var effectIcon = effectIconGO.GetComponent<UIEffectIcon>();
            
            if (effectIcon == null)
                effectIcon = effectIconGO.AddComponent<UIEffectIcon>();

            effectIcon.gameObject.SetActive(false);
            _effectIconPool.Enqueue(effectIcon);
            
            return effectIcon;
        }

        private void UpdateActiveAnimations()
        {
            // Clean up completed animations
            CleanupCompletedAnimations(_activeNotifications);
            CleanupCompletedAnimations(_activeFloatingTexts);
            CleanupCompletedAnimations(_activeEffectIcons);
        }

        private void CleanupCompletedAnimations<T>(List<T> activeList) where T : MonoBehaviour
        {
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                if (activeList[i] == null || !activeList[i].gameObject.activeInHierarchy)
                    activeList.RemoveAt(i);
            }
        }

        private void AnimateEffectUI(ItemType itemType, bool isStarting)
        {
            Transform effectUI = FindEffectUIElement(itemType);
            if (effectUI != null)
            {
                var rectTransform = effectUI as RectTransform;
                if (isStarting)
                {
                    StartCoroutine(AnimatePunchScale(rectTransform, Vector3.one * 0.2f, _punchDuration));
                }
                else
                {
                    StartCoroutine(AnimateBounce(rectTransform));
                }
            }
        }

        private void AnimateScoreIncrease()
        {
            Transform scoreUI = FindScoreUIElement();
            if (scoreUI != null)
            {
                var rectTransform = scoreUI as RectTransform;
                StartCoroutine(AnimatePunchScale(rectTransform, Vector3.one * 0.15f, _punchDuration));
            }
        }

        private void AnimateScoreUI()
        {
            Transform scoreUI = FindScoreUIElement();
            if (scoreUI != null)
            {
                var rectTransform = scoreUI as RectTransform;
                StartCoroutine(AnimateShake(rectTransform, 5f, _punchDuration));
            }
        }

        private void AnimateHealthBar(float healthPercentage)
        {
            Slider healthBar = FindHealthBarElement();
            if (healthBar != null)
            {
                AnimateSlider(healthBar, healthPercentage, 0.5f);
                
                Image fillImage = healthBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    Color targetColor = Color.Lerp(Color.red, Color.green, healthPercentage);
                    StartCoroutine(AnimateImageColor(fillImage, targetColor, 0.3f));
                }
            }
        }

        private IEnumerator AnimateImageColor(Image image, Color targetColor, float duration)
        {
            Color startColor = image.color;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                image.color = Color.Lerp(startColor, targetColor, _defaultEaseCurve.Evaluate(t));
                yield return null;
            }
            
            image.color = targetColor;
        }

        private Vector2 WorldToUIPosition(Vector3 worldPosition)
        {
            Vector2 screenPos = _uiCamera.WorldToScreenPoint(worldPosition);
            return ScreenToUIPosition(screenPos);
        }

        private Vector2 ScreenToUIPosition(Vector2 screenPosition)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, _uiCamera, out localPoint);
            return localPoint;
        }

        private Vector3 GetStackUIPosition(ItemType itemType) => Vector3.zero;
        private Vector3 GetScoreUIPosition() => FindScoreUIElement()?.position ?? Vector3.zero;
        private Vector2 GetEffectIconFinalPosition(ItemType itemType) => Vector2.zero;
        private Transform FindEffectUIElement(ItemType itemType) => null;
        private Transform FindScoreUIElement() => GameObject.Find("ScoreText")?.transform;
        private Slider FindHealthBarElement() => GameObject.Find("HealthBar")?.GetComponent<Slider>();
        private Sprite GetItemTypeSprite(ItemType itemType) => null;

        private void CleanupAnimations()
        {
            StopAllCoroutines();
            _runningCoroutines.Clear();
            _activeAnimations.Clear();
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
                Debug.Log(message, this);
        }

        #endregion

        #region Public API

        public void StopAllAnimations()
        {
            StopAllCoroutines();
            _runningCoroutines.Clear();
            _activeAnimations.Clear();
            _currentState = UIAnimationState.Idle;
        }

        public void PauseAllAnimations()
        {
            Time.timeScale = 0f;
            _currentState = UIAnimationState.Paused;
        }

        public void ResumeAllAnimations()
        {
            Time.timeScale = 1f;
            _currentState = UIAnimationState.Playing;
        }

        public bool IsAnimating(Transform target)
        {
            return _activeAnimations.ContainsKey(target) && _activeAnimations[target].Count > 0;
        }

        public void KillAnimation(Transform target)
        {
            if (_activeAnimations.TryGetValue(target, out List<Coroutine> coroutines))
            {
                foreach (var coroutine in coroutines)
                    StopCoroutine(coroutine);
                _activeAnimations.Remove(target);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Test Notification")]
        private void TestNotification() => ShowNotification("Test Notification!", Color.cyan, NotificationType.Info);

        [ContextMenu("Test Floating Text")]
        private void TestFloatingText() => ShowFloatingText("+100 Points!", Vector3.zero, Color.yellow);

        [ContextMenu("Test Screen Flash")]
        private void TestScreenFlash() => FlashScreen(Color.red, 0.5f, 0.3f);

        #endregion
    }

    #region Supporting Classes

    public enum UIAnimationType
    {
        Punch, Bounce, Shake, Scale, Fade, Slide
    }

    public enum UIAnimationState
    {
        Idle, Playing, Paused, Transitioning
    }

    public enum NotificationType
    {
        Info, Warning, Error, Success, EffectStarted, EffectEnded, EffectStacked, Achievement
    }

    #endregion
}
