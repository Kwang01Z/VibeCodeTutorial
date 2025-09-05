using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using EndlessRunner.Data;

namespace EndlessRunner.Items
{
    /// <summary>
    /// UI component hiển thị active item effects với icons, timers và stack counts
    /// Auto-updates khi có effect changes thông qua ItemEffectEvents
    /// </summary>
    public class ItemEffectUI : MonoBehaviour
    {
        [Header("UI Layout")]
        [SerializeField] private Transform _effectContainer;
        [SerializeField] private GameObject _effectUIPrefab;
        [SerializeField] private float _spacing = 10f;
        [SerializeField] private int _maxVisibleEffects = 6;

        [Header("Animation")]
        [SerializeField] private bool _animateNewEffects = true;
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private AnimationCurve _scaleAnimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Update Settings")]
        [SerializeField] private bool _updateTimersContinuously = true;
        [SerializeField] private float _timerUpdateFrequency = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Runtime tracking
        private Dictionary<string, EffectUIElement> _activeUIElements = new Dictionary<string, EffectUIElement>();
        private float _lastTimerUpdate;
        private bool _isSubscribed;

        // Performance tracking
        private int _totalUIUpdates;
        private int _peakEffectCount;

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateSetup();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshAllEffects();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_updateTimersContinuously && Time.unscaledTime - _lastTimerUpdate > _timerUpdateFrequency)
            {
                UpdateAllTimerDisplays();
                _lastTimerUpdate = Time.unscaledTime;
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;

            ItemEffectEvents.OnEffectStarted.AddListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(OnEffectEnded);
            ItemEffectEvents.OnAllEffectsChanged.AddListener(OnAllEffectsChanged);
            ItemEffectEvents.OnEffectStacked.AddListener(OnEffectStacked);

            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;

            ItemEffectEvents.OnEffectStarted.RemoveListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(OnEffectEnded);
            ItemEffectEvents.OnAllEffectsChanged.RemoveListener(OnAllEffectsChanged);
            ItemEffectEvents.OnEffectStacked.RemoveListener(OnEffectStacked);

            _isSubscribed = false;
        }

        #endregion

        #region Event Handlers

        private void OnEffectStarted(ActiveItemEffect effect)
        {
            CreateEffectUI(effect);
            _totalUIUpdates++;
            _peakEffectCount = Mathf.Max(_peakEffectCount, _activeUIElements.Count);
        }

        private void OnEffectEnded(ItemType itemType, string itemId)
        {
            RemoveEffectUI(itemId);
            _totalUIUpdates++;
        }

        private void OnAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects)
        {
            RefreshAllEffects();
            _totalUIUpdates++;
        }

        private void OnEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            if (_activeUIElements.TryGetValue(effect.ItemDefinition.ItemId, out EffectUIElement uiElement))
            {
                uiElement.UpdateEffect(effect);
                AnimateStackChange(uiElement);
            }
        }

        #endregion

        #region UI Management

        private void CreateEffectUI(ActiveItemEffect effect)
        {
            if (_effectUIPrefab == null || _effectContainer == null)
            {
                Debug.LogWarning("[ItemEffectUI] Missing UI prefab or container");
                return;
            }

            // Don't create if already exists
            if (_activeUIElements.ContainsKey(effect.ItemDefinition.ItemId))
                return;

            // Check max visible limit
            if (_activeUIElements.Count >= _maxVisibleEffects)
            {
                Debug.LogWarning($"[ItemEffectUI] Max visible effects reached ({_maxVisibleEffects})");
                return;
            }

            // Instantiate UI element
            GameObject uiGO = Instantiate(_effectUIPrefab, _effectContainer);
            var uiElement = uiGO.GetComponent<EffectUIElement>();

            if (uiElement == null)
            {
                uiElement = uiGO.AddComponent<EffectUIElement>();
            }

            // Setup and track
            uiElement.Setup(effect);
            _activeUIElements[effect.ItemDefinition.ItemId] = uiElement;

            // Animate entry if enabled
            if (_animateNewEffects)
            {
                AnimateEntry(uiElement);
            }

            ArrangeEffectElements();
        }

        private void RemoveEffectUI(string itemId)
        {
            if (_activeUIElements.TryGetValue(itemId, out EffectUIElement uiElement))
            {
                _activeUIElements.Remove(itemId);

                // Animate exit
                AnimateExit(uiElement, () => {
                    if (uiElement != null)
                        Destroy(uiElement.gameObject);
                });

                ArrangeEffectElements();
            }
        }

        private void RefreshAllEffects()
        {
            var effectSystem = ItemEffectSystem.Instance;
            if (effectSystem == null) return;

            var currentEffects = effectSystem.ActiveEffects;

            // Remove UI elements for effects that no longer exist
            var elementsToRemove = _activeUIElements.Keys
                .Where(itemId => !currentEffects.ContainsKey(itemId))
                .ToList();

            foreach (string itemId in elementsToRemove)
            {
                RemoveEffectUI(itemId);
            }

            // Create or update UI elements for current effects
            foreach (var effect in currentEffects.Values)
            {
                if (_activeUIElements.ContainsKey(effect.ItemDefinition.ItemId))
                {
                    _activeUIElements[effect.ItemDefinition.ItemId].UpdateEffect(effect);
                }
                else
                {
                    CreateEffectUI(effect);
                }
            }
        }

        private void UpdateAllTimerDisplays()
        {
            foreach (var uiElement in _activeUIElements.Values)
            {
                uiElement.UpdateTimerDisplay();
            }
        }

        private void ArrangeEffectElements()
        {
            if (_effectContainer == null) return;

            float currentX = 0f;
            int index = 0;

            foreach (var uiElement in _activeUIElements.Values.OrderBy(ui => ui.CreatedTime))
            {
                var rectTransform = uiElement.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(currentX, 0f);
                    currentX += rectTransform.sizeDelta.x + _spacing;
                }
                index++;
            }
        }

        #endregion

        #region Animation

        private void AnimateEntry(EffectUIElement uiElement)
        {
            var rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Start from scale 0
            rectTransform.localScale = Vector3.zero;

            // Use StartCoroutine as fallback animation
            StartCoroutine(ScaleAnimation(rectTransform, Vector3.zero, Vector3.one, _animationDuration));
        }

        private void AnimateExit(EffectUIElement uiElement, System.Action onComplete)
        {
            var rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            // Use StartCoroutine as fallback animation
            StartCoroutine(ScaleAnimation(rectTransform, Vector3.one, Vector3.zero, _animationDuration * 0.7f, onComplete));
        }

        private void AnimateStackChange(EffectUIElement uiElement)
        {
            var rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Quick scale pulse using coroutine
            StartCoroutine(PulseAnimation(rectTransform));
        }

        #endregion

        #region Animation Coroutines

        private System.Collections.IEnumerator ScaleAnimation(RectTransform target, Vector3 from, Vector3 to, float duration, System.Action onComplete = null)
        {
            if (target == null) yield break;

            float elapsedTime = 0f;
            target.localScale = from;

            while (elapsedTime < duration)
            {
                if (target == null) yield break;

                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                
                // Apply easing curve if available
                float easedProgress = _scaleAnimCurve.Evaluate(progress);
                target.localScale = Vector3.LerpUnclamped(from, to, easedProgress);
                
                yield return null;
            }

            if (target != null)
                target.localScale = to;
            
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator PulseAnimation(RectTransform target)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 pulseScale = originalScale * 1.2f;
            float pulseDuration = 0.1f;

            // Scale up
            yield return ScaleAnimation(target, originalScale, pulseScale, pulseDuration);
            
            // Scale back down
            yield return ScaleAnimation(target, pulseScale, originalScale, pulseDuration);
        }

        #endregion

        #region Validation & Debug

        private void ValidateSetup()
        {
            if (_effectContainer == null)
            {
                Debug.LogError("[ItemEffectUI] Effect container is not assigned", this);
            }

            if (_effectUIPrefab == null)
            {
                Debug.LogWarning("[ItemEffectUI] Effect UI prefab is not assigned", this);
            }

            // Clamp values
            _maxVisibleEffects = Mathf.Max(1, _maxVisibleEffects);
            _timerUpdateFrequency = Mathf.Max(0.05f, _timerUpdateFrequency);
            _animationDuration = Mathf.Max(0.1f, _animationDuration);
        }

        public string GetDebugInfo()
        {
            return $"ItemEffectUI Debug:\n" +
                   $"- Active UI Elements: {_activeUIElements.Count}\n" +
                   $"- Total UI Updates: {_totalUIUpdates}\n" +
                   $"- Peak Effect Count: {_peakEffectCount}\n" +
                   $"- Is Subscribed: {_isSubscribed}";
        }

        [ContextMenu("Force Refresh")]
        private void ForceRefresh()
        {
            RefreshAllEffects();
        }

        #endregion
    }

    /// <summary>
    /// Individual UI element cho một effect
    /// </summary>
    public class EffectUIElement : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _stackText;
        [SerializeField] private Image _progressBar;
        [SerializeField] private Image _backgroundImage;

        private ActiveItemEffect _effect;
        private float _createdTime;

        public float CreatedTime => _createdTime;
        public ActiveItemEffect Effect => _effect;

        public void Setup(ActiveItemEffect effect)
        {
            _effect = effect;
            _createdTime = Time.unscaledTime;
            
            UpdateEffect(effect);
        }

        public void UpdateEffect(ActiveItemEffect effect)
        {
            _effect = effect;
            
            UpdateIcon();
            UpdateTimerDisplay();
            UpdateStackDisplay();
            UpdateProgressBar();
            UpdateColors();
        }

        public void UpdateTimerDisplay()
        {
            if (_timerText != null && _effect != null)
            {
                _timerText.text = _effect.GetDisplayText();
                
                // Color based on remaining time
                if (!_effect.IsPermanent)
                {
                    float progress = _effect.Progress;
                    if (progress > 0.7f)
                        _timerText.color = Color.red;
                    else if (progress > 0.4f)
                        _timerText.color = Color.yellow;
                    else
                        _timerText.color = Color.white;
                }
            }
        }

        private void UpdateIcon()
        {
            if (_iconImage != null && _effect != null && _effect.ItemDefinition.Icon != null)
            {
                _iconImage.sprite = _effect.ItemDefinition.Icon;
            }
        }

        private void UpdateStackDisplay()
        {
            if (_stackText != null && _effect != null)
            {
                if (_effect.StackCount > 1)
                {
                    _stackText.text = $"x{_effect.StackCount}";
                    _stackText.gameObject.SetActive(true);
                }
                else
                {
                    _stackText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateProgressBar()
        {
            if (_progressBar != null && _effect != null)
            {
                if (_effect.IsPermanent)
                {
                    _progressBar.gameObject.SetActive(false);
                }
                else
                {
                    _progressBar.gameObject.SetActive(true);
                    _progressBar.fillAmount = 1f - _effect.Progress;
                }
            }
        }

        private void UpdateColors()
        {
            if (_effect != null)
            {
                Color effectColor = _effect.ItemDefinition.EffectColor;
                
                if (_backgroundImage != null)
                {
                    _backgroundImage.color = effectColor * 0.3f; // Dimmed background
                }
                
                if (_progressBar != null)
                {
                    _progressBar.color = effectColor;
                }
            }
        }
    }
}
