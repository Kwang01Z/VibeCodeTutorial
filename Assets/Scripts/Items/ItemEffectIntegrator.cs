using UnityEngine;
using System.Collections.Generic;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items
{
    /// <summary>
    /// ItemEffectIntegrator - Core integration component giữa ItemEffectSystem và RunnerController
    /// Handles tất cả effect applications và modifications
    /// </summary>
    [RequireComponent(typeof(RunnerController))]
    public class ItemEffectIntegrator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Integration Settings")]
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _enableDebugLog = true;

        [Header("Effect Handlers")]
        [SerializeField] private MagnetEffectHandler _magnetHandler;
        [SerializeField] private SpeedEffectHandler _speedHandler;
        [SerializeField] private InvisibilityEffectHandler _invisibilityHandler;
        [SerializeField] private HealthEffectHandler _healthHandler;

        [Header("Performance Settings")]
        [SerializeField] private int _maxEffectsPerFrame = 5;
        [SerializeField] private float _updateFrequency = 0.1f;

        #endregion

        #region Private Fields

        private RunnerController _runnerController;
        private ItemEffectSystem _itemEffectSystem;
        private IHealthSystem _healthSystem;
        private SpeedManager _speedManager;

        // Effect handlers management
        private Dictionary<ItemType, IEffectHandler> _effectHandlers;
        private bool _isInitialized = false;
        private bool _isSubscribed = false;

        // Performance tracking
        private float _lastUpdateTime;
        private int _effectsProcessedThisFrame;

        #endregion

        #region Properties

        public RunnerController RunnerController => _runnerController;
        public bool IsInitialized => _isInitialized;
        public int ActiveHandlersCount => _effectHandlers?.Count ?? 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            ValidateComponents();

            if (_autoInitialize)
            {
                InitializeHandlers();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            UpdateEffectHandlers();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            _runnerController = GetComponent<RunnerController>();
            _itemEffectSystem = ItemEffectSystem.Instance;
            _healthSystem = GetComponent<IHealthSystem>();
            _speedManager = GetComponent<SpeedManager>() ?? FindObjectOfType<SpeedManager>();

            LogDebug("[ItemEffectIntegrator] Components cached");
        }

        private void ValidateComponents()
        {
            if (_runnerController == null)
            {
                Debug.LogError("[ItemEffectIntegrator] RunnerController component required!", this);
                enabled = false;
                return;
            }

            if (_itemEffectSystem == null)
            {
                Debug.LogError("[ItemEffectIntegrator] ItemEffectSystem not found!", this);
                enabled = false;
                return;
            }

            LogDebug("[ItemEffectIntegrator] Components validated");
        }

        private void InitializeHandlers()
        {
            _effectHandlers = new Dictionary<ItemType, IEffectHandler>();

            // Auto-find or create handlers
            if (_magnetHandler == null)
                _magnetHandler = GetComponent<MagnetEffectHandler>() ?? gameObject.AddComponent<MagnetEffectHandler>();
            
            if (_speedHandler == null)
                _speedHandler = GetComponent<SpeedEffectHandler>() ?? gameObject.AddComponent<SpeedEffectHandler>();
            
            if (_invisibilityHandler == null)
                _invisibilityHandler = GetComponent<InvisibilityEffectHandler>() ?? gameObject.AddComponent<InvisibilityEffectHandler>();
            
            if (_healthHandler == null)
                _healthHandler = GetComponent<HealthEffectHandler>() ?? gameObject.AddComponent<HealthEffectHandler>();

            // Register handlers
            RegisterHandler(ItemType.Magnet, _magnetHandler);
            RegisterHandler(ItemType.Multiplier, _speedHandler);
            RegisterHandler(ItemType.Invisible, _invisibilityHandler);
            RegisterHandler(ItemType.Life, _healthHandler);

            LogDebug($"[ItemEffectIntegrator] Initialized {_effectHandlers.Count} effect handlers");
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Subscribe to ItemEffectSystem events
            SubscribeToEffectEvents();

            // Initialize all handlers
            InitializeAllHandlers();

            _isInitialized = true;
            LogDebug("[ItemEffectIntegrator] Integration initialized successfully");
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEffectEvents()
        {
            if (_isSubscribed || _itemEffectSystem == null) return;

            ItemEffectEvents.OnEffectStarted.AddListener(HandleEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(HandleEffectEnded);
            ItemEffectEvents.OnEffectStacked.AddListener(HandleEffectStacked);
            ItemEffectEvents.OnEffectUpdated.AddListener(HandleEffectUpdated);
            ItemEffectEvents.OnAllEffectsChanged.AddListener(HandleAllEffectsChanged);

            _isSubscribed = true;
            LogDebug("[ItemEffectIntegrator] Subscribed to effect events");
        }

        private void UnsubscribeFromEffectEvents()
        {
            if (!_isSubscribed) return;

            ItemEffectEvents.OnEffectStarted.RemoveListener(HandleEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(HandleEffectEnded);
            ItemEffectEvents.OnEffectStacked.RemoveListener(HandleEffectStacked);
            ItemEffectEvents.OnEffectUpdated.RemoveListener(HandleEffectUpdated);
            ItemEffectEvents.OnAllEffectsChanged.RemoveListener(HandleAllEffectsChanged);

            _isSubscribed = false;
            LogDebug("[ItemEffectIntegrator] Unsubscribed from effect events");
        }

        #endregion

        #region Effect Event Handlers

        private void HandleEffectStarted(ActiveItemEffect effect)
        {
            LogDebug($"[ItemEffectIntegrator] Effect started: {effect.ItemDefinition.DisplayName}");

            if (_effectHandlers.TryGetValue(effect.ItemDefinition.Type, out IEffectHandler handler))
            {
                handler.OnEffectStarted(effect);
            }
        }

        private void HandleEffectEnded(ItemType itemType, string itemId)
        {
            LogDebug($"[ItemEffectIntegrator] Effect ended: {itemType}");

            if (_effectHandlers.TryGetValue(itemType, out IEffectHandler handler))
            {
                handler.OnEffectEnded(itemType, itemId);
            }
        }

        private void HandleEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            LogDebug($"[ItemEffectIntegrator] Effect stacked: {effect.ItemDefinition.DisplayName} (x{effect.StackCount})");

            if (_effectHandlers.TryGetValue(effect.ItemDefinition.Type, out IEffectHandler handler))
            {
                handler.OnEffectStacked(effect, previousStackCount);
            }
        }

        private void HandleEffectUpdated(ActiveItemEffect effect)
        {
            // Only process updates at specified frequency for performance
            if (Time.unscaledTime - _lastUpdateTime < _updateFrequency) return;

            if (_effectHandlers.TryGetValue(effect.ItemDefinition.Type, out IEffectHandler handler))
            {
                handler.OnEffectUpdated(effect);
            }
        }

        private void HandleAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects)
        {
            LogDebug($"[ItemEffectIntegrator] All effects changed: {allEffects.Count} active");

            // Notify all handlers about the change
            foreach (var handler in _effectHandlers.Values)
            {
                handler.OnAllEffectsChanged(allEffects);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Apply item effect - main entry point từ ItemPickup
        /// </summary>
        public void ApplyItemEffect(ItemDefinition itemDefinition)
        {
            if (!_isInitialized || itemDefinition == null)
            {
                LogDebug("[ItemEffectIntegrator] Cannot apply effect - not initialized or null item");
                return;
            }

            _itemEffectSystem.ApplyItemEffect(itemDefinition);
            LogDebug($"[ItemEffectIntegrator] Applied item effect: {itemDefinition.DisplayName}");
        }

        /// <summary>
        /// Remove specific effect
        /// </summary>
        public bool RemoveEffect(string itemId)
        {
            if (!_isInitialized) return false;

            return _itemEffectSystem.RemoveEffect(itemId);
        }

        /// <summary>
        /// Clear all effects
        /// </summary>
        public void ClearAllEffects()
        {
            if (!_isInitialized) return;

            _itemEffectSystem.ClearAllEffects();
        }

        /// <summary>
        /// Get current effect status for debugging
        /// </summary>
        public string GetEffectStatus()
        {
            if (!_isInitialized || _itemEffectSystem == null)
                return "Not initialized";

            return $"Active Effects: {_itemEffectSystem.ActiveEffectCount}\n" +
                   $"Magnet: {_itemEffectSystem.HasMagnetEffect} (R:{_itemEffectSystem.CurrentMagnetRadius:F1})\n" +
                   $"Multiplier: {_itemEffectSystem.HasMultiplierEffect} (x{_itemEffectSystem.CurrentScoreMultiplier:F1})\n" +
                   $"Invisible: {_itemEffectSystem.HasInvisibilityEffect}\n" +
                   $"Lives: {_itemEffectSystem.CurrentExtraLives}";
        }

        #endregion

        #region Handler Management

        private void RegisterHandler(ItemType itemType, IEffectHandler handler)
        {
            if (handler == null)
            {
                LogDebug($"[ItemEffectIntegrator] Cannot register null handler for {itemType}");
                return;
            }

            _effectHandlers[itemType] = handler;

            // Initialize handler with required dependencies
            handler.Initialize(this, _runnerController, _healthSystem, _speedManager);

            LogDebug($"[ItemEffectIntegrator] Registered handler for {itemType}");
        }

        private void InitializeAllHandlers()
        {
            foreach (var handler in _effectHandlers.Values)
            {
                if (handler != null && !handler.IsInitialized)
                {
                    handler.Initialize(this, _runnerController, _healthSystem, _speedManager);
                }
            }
        }

        private void UpdateEffectHandlers()
        {
            if (Time.unscaledTime - _lastUpdateTime < _updateFrequency) return;

            _effectsProcessedThisFrame = 0;

            foreach (var handler in _effectHandlers.Values)
            {
                if (_effectsProcessedThisFrame >= _maxEffectsPerFrame) break;

                handler?.UpdateHandler(Time.deltaTime);
                _effectsProcessedThisFrame++;
            }

            _lastUpdateTime = Time.unscaledTime;
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            UnsubscribeFromEffectEvents();

            // Cleanup all handlers
            if (_effectHandlers != null)
            {
                foreach (var handler in _effectHandlers.Values)
                {
                    handler?.Cleanup();
                }
                _effectHandlers.Clear();
            }

            _isInitialized = false;
            LogDebug("[ItemEffectIntegrator] Cleaned up");
        }

        #endregion

        #region Debug & Utilities

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }

        [ContextMenu("Show Effect Status")]
        private void ShowEffectStatus()
        {
            Debug.Log(GetEffectStatus(), this);
        }

        [ContextMenu("Clear All Effects")]
        private void DebugClearAllEffects()
        {
            ClearAllEffects();
        }

        #endregion
    }
}
