using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using EndlessRunner.Data;
using EndlessRunner.Core;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Singleton system quản lý active item effects với timer và stacking logic
    /// </summary>
    public class ItemEffectSystem : MonoBehaviour
    {
        #region Singleton

        private static ItemEffectSystem _instance;
        public static ItemEffectSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemEffectSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemEffectSystem");
                        _instance = go.AddComponent<ItemEffectSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        [Header("Effect Management")]
        [SerializeField] private bool _enableEffectLogging = true;
        [SerializeField] private float _updateFrequency = 0.1f; // Update every 100ms for performance
        [SerializeField] private int _maxActiveEffects = 20;

        [Header("Debug Info")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private List<ActiveItemEffect> _debugActiveEffects = new List<ActiveItemEffect>();

        // Runtime tracking
        private Dictionary<string, ActiveItemEffect> _activeEffects = new Dictionary<string, ActiveItemEffect>();
        private float _lastUpdateTime;
        private bool _isUpdatingEffects;

        // Performance tracking
        private int _totalEffectsApplied;
        private int _totalEffectsExpired;
        private float _averageUpdateTime;

        #endregion

        #region Properties

        public IReadOnlyDictionary<string, ActiveItemEffect> ActiveEffects => _activeEffects;
        public int ActiveEffectCount => _activeEffects.Count;
        public bool HasActiveEffects => _activeEffects.Count > 0;

        // Specific effect queries
        public bool HasMagnetEffect => HasEffectOfType(ItemType.Magnet);
        public bool HasMultiplierEffect => HasEffectOfType(ItemType.Multiplier);
        public bool HasInvisibilityEffect => HasEffectOfType(ItemType.Invisible);

        // Current effect values
        public float CurrentMagnetRadius => GetEffectValue(ItemType.Magnet);
        public float CurrentScoreMultiplier => GetEffectValue(ItemType.Multiplier, 1f);
        public int CurrentExtraLives => Mathf.RoundToInt(GetEffectValue(ItemType.Life));

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateEffectTimers();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ClearAllEffects();
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies item effect với stacking logic
        /// </summary>
        public void ApplyItemEffect(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
            {
                LogWarning("Cannot apply null item definition");
                return;
            }

            string itemId = itemDefinition.ItemId;
            
            // Check for existing effect
            if (_activeEffects.TryGetValue(itemId, out ActiveItemEffect existingEffect))
            {
                ApplyStackingEffect(existingEffect, itemDefinition);
            }
            else
            {
                ApplyNewEffect(itemDefinition);
            }

            _totalEffectsApplied++;
            UpdateDebugList();
        }

        /// <summary>
        /// Removes specific effect by item ID
        /// </summary>
        public bool RemoveEffect(string itemId)
        {
            if (_activeEffects.TryGetValue(itemId, out ActiveItemEffect effect))
            {
                _activeEffects.Remove(itemId);
                ItemEffectEvents.InvokeEffectEnded(effect.ItemDefinition.Type, itemId);
                ItemEffectEvents.InvokeAllEffectsChanged(_activeEffects);
                
                LogInfo($"Removed effect: {effect.ItemDefinition.DisplayName}");
                UpdateDebugList();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all active effects
        /// </summary>
        public void ClearAllEffects()
        {
            var effectsToRemove = _activeEffects.Values.ToList();
            _activeEffects.Clear();

            foreach (var effect in effectsToRemove)
            {
                ItemEffectEvents.InvokeEffectEnded(effect.ItemDefinition.Type, effect.ItemDefinition.ItemId);
            }

            if (effectsToRemove.Count > 0)
            {
                ItemEffectEvents.InvokeAllEffectsChanged(_activeEffects);
                LogInfo($"Cleared {effectsToRemove.Count} active effects");
            }

            UpdateDebugList();
        }

        /// <summary>
        /// Gets active effect by item ID
        /// </summary>
        public ActiveItemEffect GetActiveEffect(string itemId)
        {
            _activeEffects.TryGetValue(itemId, out ActiveItemEffect effect);
            return effect;
        }

        /// <summary>
        /// Gets all effects of specific type
        /// </summary>
        public List<ActiveItemEffect> GetEffectsOfType(ItemType itemType)
        {
            return _activeEffects.Values
                .Where(effect => effect.ItemDefinition.Type == itemType)
                .ToList();
        }

        /// <summary>
        /// Checks if có active effect của type cụ thể
        /// </summary>
        public bool HasEffectOfType(ItemType itemType)
        {
            return _activeEffects.Values.Any(effect => effect.ItemDefinition.Type == itemType);
        }

        /// <summary>
        /// Gets combined effect value cho type cụ thể
        /// </summary>
        public float GetEffectValue(ItemType itemType, float defaultValue = 0f)
        {
            var effects = GetEffectsOfType(itemType);
            if (effects.Count == 0) return defaultValue;

            return itemType switch
            {
                ItemType.Magnet => effects.Max(e => e.CurrentEffectValue),
                ItemType.Multiplier => effects.Max(e => e.CurrentEffectValue),
                ItemType.Life => effects.Sum(e => e.StackCount),
                ItemType.Invisible => effects.Any() ? 1f : 0f,
                _ => defaultValue
            };
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            _activeEffects = new Dictionary<string, ActiveItemEffect>();
            _lastUpdateTime = Time.unscaledTime;
            
            LogInfo("ItemEffectSystem initialized");
        }

        private void UpdateEffectTimers()
        {
            if (_isUpdatingEffects || Time.unscaledTime - _lastUpdateTime < _updateFrequency)
                return;

            _isUpdatingEffects = true;
            float startTime = Time.unscaledTime;

            var effectsToRemove = new List<string>();
            var deltaTime = Time.unscaledTime - _lastUpdateTime;

            foreach (var kvp in _activeEffects)
            {
                var effect = kvp.Value;
                
                if (!effect.UpdateTimer(deltaTime))
                {
                    effectsToRemove.Add(kvp.Key);
                }
                else if (!effect.IsPermanent)
                {
                    ItemEffectEvents.InvokeEffectUpdated(effect);
                }
            }

            // Remove expired effects
            foreach (string itemId in effectsToRemove)
            {
                var effect = _activeEffects[itemId];
                _activeEffects.Remove(itemId);
                ItemEffectEvents.InvokeEffectEnded(effect.ItemDefinition.Type, itemId);
                _totalEffectsExpired++;
                
                LogInfo($"Effect expired: {effect.ItemDefinition.DisplayName}");
            }

            if (effectsToRemove.Count > 0)
            {
                ItemEffectEvents.InvokeAllEffectsChanged(_activeEffects);
                UpdateDebugList();
            }

            _lastUpdateTime = Time.unscaledTime;
            
            // Update performance tracking
            float updateTime = Time.unscaledTime - startTime;
            _averageUpdateTime = Mathf.Lerp(_averageUpdateTime, updateTime, 0.1f);
            
            _isUpdatingEffects = false;
        }

        private void ApplyNewEffect(ItemDefinition itemDefinition)
        {
            if (_activeEffects.Count >= _maxActiveEffects)
            {
                LogWarning($"Max active effects reached ({_maxActiveEffects}), ignoring new effect");
                return;
            }

            var newEffect = new ActiveItemEffect(itemDefinition);
            _activeEffects[itemDefinition.ItemId] = newEffect;

            ItemEffectEvents.InvokeEffectStarted(newEffect);
            ItemEffectEvents.InvokeAllEffectsChanged(_activeEffects);

            LogInfo($"Applied new effect: {itemDefinition.DisplayName} ({newEffect.RemainingTime:F1}s)");
        }

        private void ApplyStackingEffect(ActiveItemEffect existingEffect, ItemDefinition newItemDefinition)
        {
            if (!existingEffect.ItemDefinition.CanStack)
            {
                LogInfo($"Effect {newItemDefinition.DisplayName} cannot stack, ignoring");
                return;
            }

            int previousStackCount = existingEffect.StackCount;
            existingEffect.ApplyStacking(newItemDefinition);

            ItemEffectEvents.InvokeEffectStacked(existingEffect, previousStackCount);
            ItemEffectEvents.InvokeAllEffectsChanged(_activeEffects);

            LogInfo($"Stacked effect: {newItemDefinition.DisplayName} (x{existingEffect.StackCount}, {existingEffect.RemainingTime:F1}s)");
        }

        private void UpdateDebugList()
        {
            if (_showDebugInfo)
            {
                _debugActiveEffects.Clear();
                _debugActiveEffects.AddRange(_activeEffects.Values);
            }
        }

        private void LogInfo(string message)
        {
            if (_enableEffectLogging)
                Debug.Log($"[ItemEffectSystem] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableEffectLogging)
                Debug.LogWarning($"[ItemEffectSystem] {message}");
        }

        #endregion

        #region Debug & Editor

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugApplyTestEffect(ItemType itemType)
        {
            var testItem = ItemDefinition.CreateDefault(itemType);
            ApplyItemEffect(testItem);
        }

        public string GetSystemStats()
        {
            return $"ItemEffectSystem Stats:\n" +
                   $"- Active Effects: {_activeEffects.Count}/{_maxActiveEffects}\n" +
                   $"- Total Applied: {_totalEffectsApplied}\n" +
                   $"- Total Expired: {_totalEffectsExpired}\n" +
                   $"- Avg Update Time: {_averageUpdateTime * 1000f:F2}ms\n" +
                   $"- Current Values:\n" +
                   $"  • Magnet: {CurrentMagnetRadius:F1}\n" +
                   $"  • Multiplier: {CurrentScoreMultiplier:F1}x\n" +
                   $"  • Extra Lives: {CurrentExtraLives}\n" +
                   $"  • Invisible: {(HasInvisibilityEffect ? "Yes" : "No")}";
        }

        #endregion
    }
}
