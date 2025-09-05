using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EndlessRunner.Gameplay;

namespace EndlessRunner.UI
{
    /// <summary>
    /// Health UI component để hiển thị tim trên HUD.
    /// Automatically finds và subscribes vào IHealthSystem để update UI.
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Heart Configuration")]
        [SerializeField, Tooltip("Heart sprite khi full")]
        private Sprite _fullHeartSprite;
        
        [SerializeField, Tooltip("Heart sprite khi empty/damaged")]
        private Sprite _emptyHeartSprite;
        
        [SerializeField, Tooltip("Max số tim có thể hiển thị")]
        [Range(1, 10)]
        private int _maxHearts = 5;
        
        [SerializeField, Tooltip("Heart size (pixels)")]
        [Range(20f, 100f)]
        private float _heartSize = 40f;
        
        [SerializeField, Tooltip("Spacing giữa các tim")]
        [Range(0f, 20f)]
        private float _heartSpacing = 5f;
        
        [Header("Layout")]
        [SerializeField, Tooltip("Container cho heart icons")]
        private Transform _heartsContainer;
        
        [SerializeField, Tooltip("Horizontal layout group (auto-found nếu null)")]
        private HorizontalLayoutGroup _layoutGroup;
        
        [SerializeField, Tooltip("Auto-create hearts nếu container empty")]
        private bool _autoCreateHearts = true;
        
        [Header("Animation")]
        [SerializeField, Tooltip("Enable heart animations")]
        private bool _enableAnimations = true;
        
        [SerializeField, Tooltip("Animation duration cho lose/gain heart")]
        [Range(0.1f, 1f)]
        private float _animationDuration = 0.3f;
        
        [SerializeField, Tooltip("Pulse scale multiplier khi critical health")]
        [Range(1f, 2f)]
        private float _criticalPulseScale = 1.2f;
        
        [Header("Colors")]
        [SerializeField, Tooltip("Normal heart color")]
        private Color _normalColor = Color.white;
        
        [SerializeField, Tooltip("Critical health color (1 tim còn lại)")]
        private Color _criticalColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        [SerializeField, Tooltip("Empty heart color")]
        private Color _emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        
        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logs")]
        private bool _debugMode = true;
        
        [SerializeField, Tooltip("Show health info text (debug)")]
        private bool _showDebugText = false;
        
        [SerializeField, Tooltip("Debug text component")]
        private Text _debugText;
        
        #endregion
        
        #region Private Fields
        
        private IHealthSystem _healthSystem;
        private List<Image> _heartImages = new List<Image>();
        private int _currentDisplayedHealth = -1;
        private int _maxDisplayedHealth = -1;
        private bool _isInitialized = false;
        
        // Animation
        private Coroutine _pulseCoroutine;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
            InitializeLayout();
        }
        
        private void Start()
        {
            FindHealthSystem();
            InitializeHearts();
            SubscribeToEvents();
            UpdateDisplay(true); // Force initial update
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void CacheComponents()
        {
            if (_heartsContainer == null)
                _heartsContainer = transform;
                
            if (_layoutGroup == null)
                _layoutGroup = _heartsContainer.GetComponent<HorizontalLayoutGroup>();
                
            if (_debugText == null && _showDebugText)
                _debugText = GetComponentInChildren<Text>();
                
            LogDebug("[HealthUI] Components cached");
        }
        
        private void InitializeLayout()
        {
            // Auto-create HorizontalLayoutGroup nếu chưa có
            if (_layoutGroup == null)
            {
                _layoutGroup = _heartsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                _layoutGroup.childForceExpandWidth = false;
                _layoutGroup.childForceExpandHeight = false;
                _layoutGroup.childControlWidth = false;
                _layoutGroup.childControlHeight = false;
                _layoutGroup.childScaleWidth = false;
                _layoutGroup.childScaleHeight = false;
                
                LogDebug("[HealthUI] HorizontalLayoutGroup auto-created");
            }
            
            // Configure layout spacing
            if (_layoutGroup != null)
            {
                _layoutGroup.spacing = _heartSpacing;
            }
        }
        
        private void FindHealthSystem()
        {
            // Find HealthComponent trong scene
            var healthComponent = FindObjectOfType<HealthComponent>();
            if (healthComponent != null)
            {
                _healthSystem = healthComponent;
                LogDebug($"[HealthUI] Found HealthSystem: {healthComponent.name}");
            }
            else
            {
                Debug.LogWarning("[HealthUI] No HealthComponent found in scene. HealthUI will not function.", this);
            }
        }
        
        private void InitializeHearts()
        {
            if (_healthSystem == null) return;
            
            // Clear existing hearts
            ClearHearts();
            
            // Create hearts based on max health
            int maxHealth = _healthSystem.MaxHealth;
            int heartsToCreate = Mathf.Min(maxHealth, _maxHearts);
            
            for (int i = 0; i < heartsToCreate; i++)
            {
                CreateHeart(i);
            }
            
            _maxDisplayedHealth = maxHealth;
            LogDebug($"[HealthUI] Initialized {heartsToCreate} hearts for max health {maxHealth}");
            _isInitialized = true;
        }
        
        private void CreateHeart(int index)
        {
            // Create GameObject
            var heartGO = new GameObject($"Heart_{index:00}");
            heartGO.transform.SetParent(_heartsContainer, false);
            
            // Add Image component
            var heartImage = heartGO.AddComponent<Image>();
            heartImage.sprite = _fullHeartSprite;
            heartImage.color = _normalColor;
            heartImage.preserveAspect = true;
            
            // Set size
            var rectTransform = heartImage.rectTransform;
            rectTransform.sizeDelta = new Vector2(_heartSize, _heartSize);
            
            // Add to list
            _heartImages.Add(heartImage);
            
            LogDebug($"[HealthUI] Created heart {index}: {heartGO.name}");
        }
        
        private void ClearHearts()
        {
            foreach (var heart in _heartImages)
            {
                if (heart != null)
                {
                    DestroyImmediate(heart.gameObject);
                }
            }
            _heartImages.Clear();
            
            LogDebug("[HealthUI] Cleared existing hearts");
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnDeath += HandlePlayerDeath;
                _healthSystem.OnHealthRestored += HandleHealthRestored;
                
                LogDebug("[HealthUI] Subscribed to health system events");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnDeath -= HandlePlayerDeath;
                _healthSystem.OnHealthRestored -= HandleHealthRestored;
            }
        }
        
        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            LogDebug($"[HealthUI] Health changed: {currentHealth}/{maxHealth}");
            
            // Re-initialize hearts nếu max health thay đổi
            if (maxHealth != _maxDisplayedHealth)
            {
                InitializeHearts();
            }
            
            UpdateDisplay();
        }
        
        private void HandlePlayerDeath()
        {
            LogDebug("[HealthUI] Player death - all hearts empty");
            UpdateDisplay();
            
            // Stop pulse animation nếu đang chạy
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }
        
        private void HandleHealthRestored(int amount)
        {
            LogDebug($"[HealthUI] Health restored: +{amount}");
            
            if (_enableAnimations)
            {
                // TODO: Add restoration animation
            }
            
            UpdateDisplay();
        }
        
        #endregion
        
        #region Display Update
        
        /// <summary>
        /// Update heart display based on current health
        /// </summary>
        public void UpdateDisplay(bool forceUpdate = false)
        {
            if (_healthSystem == null || !_isInitialized) return;
            
            int currentHealth = _healthSystem.CurrentHealth;
            int maxHealth = _healthSystem.MaxHealth;
            
            // Skip update nếu không có thay đổi
            if (!forceUpdate && currentHealth == _currentDisplayedHealth) return;
            
            _currentDisplayedHealth = currentHealth;
            
            // Update each heart
            for (int i = 0; i < _heartImages.Count; i++)
            {
                UpdateHeart(i, i < currentHealth, currentHealth, maxHealth);
            }
            
            // Handle critical health pulse
            HandleCriticalHealthEffect(currentHealth, maxHealth);
            
            // Update debug text
            UpdateDebugText(currentHealth, maxHealth);
            
            LogDebug($"[HealthUI] Display updated: {currentHealth}/{maxHealth}");
        }
        
        private void UpdateHeart(int heartIndex, bool isFull, int currentHealth, int maxHealth)
        {
            if (heartIndex >= _heartImages.Count) return;
            
            var heartImage = _heartImages[heartIndex];
            if (heartImage == null) return;
            
            // Update sprite
            heartImage.sprite = isFull ? _fullHeartSprite : _emptyHeartSprite;
            
            // Update color
            Color targetColor;
            if (isFull && currentHealth == 1) // Critical health
            {
                targetColor = _criticalColor;
            }
            else if (isFull)
            {
                targetColor = _normalColor;
            }
            else
            {
                targetColor = _emptyColor;
            }
            
            heartImage.color = targetColor;
            
            // Animation nếu enabled
            if (_enableAnimations && heartIndex == currentHealth && heartIndex > 0)
            {
                // Animation cho heart vừa mất (pulse out)
                StartCoroutine(HeartLossAnimation(heartImage));
            }
        }
        
        private void HandleCriticalHealthEffect(int currentHealth, int maxHealth)
        {
            if (currentHealth == 1 && currentHealth < maxHealth)
            {
                // Start critical health pulse
                if (_pulseCoroutine == null && _enableAnimations)
                {
                    _pulseCoroutine = StartCoroutine(CriticalHealthPulse());
                }
            }
            else
            {
                // Stop critical health pulse
                if (_pulseCoroutine != null)
                {
                    StopCoroutine(_pulseCoroutine);
                    _pulseCoroutine = null;
                    
                    // Reset scale cho tất cả hearts
                    foreach (var heart in _heartImages)
                    {
                        if (heart != null)
                        {
                            heart.transform.localScale = Vector3.one;
                        }
                    }
                }
            }
        }
        
        private void UpdateDebugText(int currentHealth, int maxHealth)
        {
            if (_debugText != null && _showDebugText)
            {
                _debugText.text = $"HP: {currentHealth}/{maxHealth}";
            }
        }
        
        #endregion
        
        #region Animations
        
        /// <summary>
        /// Animation khi mất heart
        /// </summary>
        private System.Collections.IEnumerator HeartLossAnimation(Image heartImage)
        {
            if (heartImage == null) yield break;
            
            var originalScale = heartImage.transform.localScale;
            float elapsed = 0f;
            
            // Scale up then down
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _animationDuration;
                
                // Scale curve: 1 -> 1.3 -> 1
                float scaleMultiplier = 1f + 0.3f * Mathf.Sin(progress * Mathf.PI);
                heartImage.transform.localScale = originalScale * scaleMultiplier;
                
                yield return null;
            }
            
            heartImage.transform.localScale = originalScale;
        }
        
        /// <summary>
        /// Pulse animation cho critical health (1 tim còn lại)
        /// </summary>
        private System.Collections.IEnumerator CriticalHealthPulse()
        {
            if (_heartImages.Count == 0 || _heartImages[0] == null) yield break;
            
            var firstHeart = _heartImages[0]; // Tim cuối cùng còn lại
            var originalScale = firstHeart.transform.localScale;
            
            while (true)
            {
                // Pulse up
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / 0.5f;
                    float scaleMultiplier = Mathf.Lerp(1f, _criticalPulseScale, progress);
                    firstHeart.transform.localScale = originalScale * scaleMultiplier;
                    yield return null;
                }
                
                // Pulse down
                elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / 0.5f;
                    float scaleMultiplier = Mathf.Lerp(_criticalPulseScale, 1f, progress);
                    firstHeart.transform.localScale = originalScale * scaleMultiplier;
                    yield return null;
                }
                
                // Small delay
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually set health system reference
        /// </summary>
        public void SetHealthSystem(IHealthSystem healthSystem)
        {
            UnsubscribeFromEvents();
            _healthSystem = healthSystem;
            
            if (_healthSystem != null)
            {
                SubscribeToEvents();
                InitializeHearts();
                UpdateDisplay(true);
                
                LogDebug($"[HealthUI] Health system set manually: {healthSystem}");
            }
        }
        
        /// <summary>
        /// Force refresh display
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay(true);
        }
        
        /// <summary>
        /// Get current display status
        /// </summary>
        public (int currentHealth, int maxHealth, int heartsShown) GetDisplayStatus()
        {
            int currentHealth = _healthSystem?.CurrentHealth ?? 0;
            int maxHealth = _healthSystem?.MaxHealth ?? 0;
            int heartsShown = _heartImages.Count;
            
            return (currentHealth, maxHealth, heartsShown);
        }
        
        #endregion
        
        #region Debug
        
        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log(message, this);
            }
        }
        
        /// <summary>
        /// Get debug info cho Inspector
        /// </summary>
        public string GetDebugInfo()
        {
            if (_healthSystem == null) return "No HealthSystem found";
            
            return $"Health: {_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}\\n" +
                   $"Hearts: {_heartImages.Count}\\n" +
                   $"Displayed: {_currentDisplayedHealth}/{_maxDisplayedHealth}\\n" +
                   $"Critical Pulse: {_pulseCoroutine != null}";
        }
        
        #endregion
        
        #region Editor Validation
        
        private void OnValidate()
        {
            // Clamp values
            _maxHearts = Mathf.Max(1, _maxHearts);
            _heartSize = Mathf.Max(20f, _heartSize);
            _heartSpacing = Mathf.Max(0f, _heartSpacing);
            _animationDuration = Mathf.Max(0.1f, _animationDuration);
            _criticalPulseScale = Mathf.Max(1f, _criticalPulseScale);
            
            // Update layout spacing nếu đã có layout group
            if (_layoutGroup != null)
            {
                _layoutGroup.spacing = _heartSpacing;
            }
        }
        
        #endregion
        
        #region Context Menu (Editor)
        
#if UNITY_EDITOR
        [ContextMenu("Refresh Hearts")]
        private void RefreshHeartsEditor()
        {
            if (Application.isPlaying)
            {
                InitializeHearts();
                UpdateDisplay(true);
            }
            else
            {
                Debug.Log("[HealthUI] Refresh Hearts can only be used in Play Mode");
            }
        }
        
        [ContextMenu("Test Health Loss")]
        private void TestHealthLoss()
        {
            if (Application.isPlaying && _healthSystem != null)
            {
                _healthSystem.TakeDamage(1, "UI Test");
            }
        }
        
        [ContextMenu("Test Health Restore")]
        private void TestHealthRestore()
        {
            if (Application.isPlaying && _healthSystem != null)
            {
                _healthSystem.RestoreHealth(1);
            }
        }
#endif
        
        #endregion
    }
}
