using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// ItemEffectSystem - Core system để quản lý active items, stacking rules, duration tracking.
    /// Integrates với SpeedManager, HealthSystem, và UI systems theo ROADMAP specifications.
    /// </summary>
    public class ItemEffectSystem : MonoBehaviour, IItemEffectSystem
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] 
        [Tooltip("Maximum số lượng items có thể active cùng lúc")]
        private int _maxActiveItems = 10;
        
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _enableDebugLog = false;
        
        [Header("Integration References")]
        [SerializeField]
        [Tooltip("SpeedManager để apply speed multipliers")]
        private SpeedManager _speedManager;
        
        [SerializeField]
        [Tooltip("HealthComponent để restore health")]
        private HealthComponent _healthComponent;
        
        [SerializeField]
        [Tooltip("RunnerController để handle layer changes")]
        private RunnerController _runnerController;
        
        [SerializeField]
        [Tooltip("CurrencyManager để handle currency rewards")]
        private EndlessRunner.Currency.CurrencyManager _currencyManager;
        
        #endregion
        
        #region Private Fields
        
        // Active items storage
        private List<ActiveItem> _activeItems = new List<ActiveItem>();
        private Dictionary<string, int> _itemLookup = new Dictionary<string, int>();
        
        // Manual item system
        private ItemDefinition _manualItem;
        private Timer _manualCooldownTimer;
        
        // Cached values
        private float _currentMultiplier = 1f;
        private bool _isMagnetActive = false;
        private bool _isInvisibleActive = false;
        
        // Component references
        private ISpeedManager _speedManagerInterface;
        private IHealthSystem _healthSystemInterface;
        private IRunnerController _runnerControllerInterface;
        
        #endregion
        
        #region Internal Structures
        
        /// <summary>
        /// Internal active item data
        /// </summary>
        [System.Serializable]
        private struct ActiveItem
        {
            public ItemDefinition definition;
            public Timer timer;
            public float effectValue;
            public int stackCount;
            public DateTime activatedAt;
            
            public ActiveItem(ItemDefinition definition, float duration, float effectValue)
            {
                this.definition = definition;
                this.timer = new Timer();
                this.timer.Start(duration);
                this.effectValue = effectValue;
                this.stackCount = 1;
                this.activatedAt = DateTime.Now;
            }
        }
        
        #endregion

        #region IItemEffectSystem Properties
        
        public bool HasActiveItems => _activeItems.Count > 0;
        public int ActiveItemCount => _activeItems.Count;
        public ItemDefinition ManualItem => _manualItem;
        public bool IsManualCooldown => _manualCooldownTimer.IsRunning;
        public float ManualCooldownRemaining => _manualCooldownTimer.IsRunning ? 
            _manualCooldownTimer.Progress * _manualCooldownTimer.Duration : 0f;
        public float CurrentMultiplier => _currentMultiplier;
        public bool IsMagnetActive => _isMagnetActive;
        public bool IsInvisibleActive => _isInvisibleActive;
        
        #endregion

        #region Events
        
        public event Action<ItemDefinition, bool> OnItemActivated;
        public event Action<ItemDefinition, bool> OnItemDeactivated;
        public event Action<ItemDefinition, int, float> OnItemStacked;
        public event Action<float, float> OnMultiplierChanged;
        public event Action<bool, float, float> OnMagnetStateChanged;
        public event Action<bool, string> OnInvisibleStateChanged;
        public event Action<ItemDefinition, string> OnManualItemChanged;
        public event Action<bool, float> OnManualCooldownChanged;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            ValidateConfiguration();
        }
        
        private void Update()
        {
            UpdateActiveItems();
            UpdateManualCooldown();
        }
        
        private void OnDestroy()
        {
            ClearAllItems();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Auto-find components nếu không assign
            if (_speedManager == null)
                _speedManager = GetComponent<SpeedManager>();
            if (_healthComponent == null)
                _healthComponent = GetComponent<HealthComponent>();
            if (_runnerController == null)
                _runnerController = GetComponent<RunnerController>();
            
            // Cache interfaces
            _speedManagerInterface = _speedManager;
            _healthSystemInterface = _healthComponent;
            _runnerControllerInterface = _runnerController;
            
            LogDebug("[ItemEffectSystem] Components initialized");
        }
        
        private void ValidateConfiguration()
        {
            if (_speedManagerInterface == null)
            {
                Debug.LogWarning("[ItemEffectSystem] No ISpeedManager found - multiplier effects disabled");
            }
            
            if (_healthSystemInterface == null)
            {
                Debug.LogWarning("[ItemEffectSystem] No IHealthSystem found - life items disabled");
            }
            
            if (_runnerControllerInterface == null)
            {
                Debug.LogWarning("[ItemEffectSystem] No IRunnerController found - invisible effects limited");
            }
            
            LogDebug($"[ItemEffectSystem] Validation completed - Max items: {_maxActiveItems}");
        }
        
        #endregion

        #region Item Management Implementation
        
        public bool ActivateItem(ItemDefinition item, bool forceManual = false)
        {
            if (item == null)
            {
                Debug.LogError("[ItemEffectSystem] Cannot activate null item");
                return false;
            }
            
            LogDebug($"[ItemEffectSystem] Activating item: {item.DisplayName} (force manual: {forceManual})");
            
            // Handle manual items
            if (forceManual || (item.Type == ItemType.Manual))
            {
                return SetManualItem(item);
            }
            
            // Handle special instant items
            if (item.Type == ItemType.Life)
            {
                return ActivateLifeItem(item);
            }
            
            if (item.Type == ItemType.Currency)
            {
                return ActivateCurrencyItem(item);
            }
            
            // Handle duration-based items
            return ActivateDurationItem(item);
        }
        
        public bool DeactivateItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            
            if (!_itemLookup.TryGetValue(itemId, out int index))
                return false;
            
            if (index >= 0 && index < _activeItems.Count)
            {
                var item = _activeItems[index];
                RemoveActiveItem(index, false); // false = manual deactivation
                return true;
            }
            
            return false;
        }
        
        public int DeactivateItemsOfType(ItemType itemType)
        {
            int deactivatedCount = 0;
            
            // Iterate backwards để tránh index shifting
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i].definition.Type == itemType)
                {
                    RemoveActiveItem(i, false);
                    deactivatedCount++;
                }
            }
            
            LogDebug($"[ItemEffectSystem] Deactivated {deactivatedCount} items of type {itemType}");
            return deactivatedCount;
        }
        
        public void ClearAllItems()
        {
            LogDebug($"[ItemEffectSystem] Clearing {_activeItems.Count} active items");
            
            // Deactivate all items
            while (_activeItems.Count > 0)
            {
                RemoveActiveItem(0, false);
            }
            
            // Clear manual item
            if (_manualItem != null)
            {
                var oldManual = _manualItem;
                _manualItem = null;
                OnManualItemChanged?.Invoke(oldManual, "cleared");
            }
        }
        
        public bool UseManualItem()
        {
            if (_manualItem == null)
            {
                LogDebug("[ItemEffectSystem] No manual item to use");
                return false;
            }
            
            if (IsManualCooldown)
            {
                LogDebug("[ItemEffectSystem] Manual item on cooldown");
                return false;
            }
            
            var itemToUse = _manualItem;
            _manualItem = null;
            
            // Start cooldown (use cooldown từ item definition)
            float cooldownDuration = itemToUse.Type == ItemType.Manual ? 
                itemToUse.ManualCooldown : 5f; // Default 5s
            _manualCooldownTimer.Start(cooldownDuration);
            
            // Activate the item
            bool success = ActivateItem(itemToUse, false);
            
            // Fire events
            OnManualItemChanged?.Invoke(itemToUse, success ? "used" : "failed");
            OnManualCooldownChanged?.Invoke(true, cooldownDuration);
            
            LogDebug($"[ItemEffectSystem] Used manual item: {itemToUse.DisplayName} (success: {success})");
            return success;
        }
        
        #endregion

        #region Query Methods Implementation
        
        public bool IsItemActive(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _itemLookup.ContainsKey(itemId);
        }
        
        public bool IsItemTypeActive(ItemType itemType)
        {
            return _activeItems.Any(item => item.definition.Type == itemType);
        }
        
        public float GetRemainingTime(string itemId)
        {
            if (_itemLookup.TryGetValue(itemId, out int index) && 
                index >= 0 && index < _activeItems.Count)
            {
                var item = _activeItems[index];
                return item.timer.IsRunning ? 
                    item.timer.Duration * (1f - item.timer.Progress) : 0f;
            }
            return -1f;
        }
        
        public float GetRemainingTimeByType(ItemType itemType)
        {
            var item = _activeItems.FirstOrDefault(i => i.definition.Type == itemType);
            if (item.definition != null)
            {
                return item.timer.IsRunning ? 
                    item.timer.Duration * (1f - item.timer.Progress) : 0f;
            }
            return -1f;
        }
        
        public int GetMultiplierStackCount()
        {
            var multiplierItem = _activeItems.FirstOrDefault(i => i.definition.Type == ItemType.Multiplier);
            return multiplierItem.definition != null ? multiplierItem.stackCount : 0;
        }
        
        public IReadOnlyList<ActiveItemInfo> GetActiveItems()
        {
            var result = new List<ActiveItemInfo>();
            
            foreach (var item in _activeItems)
            {
                result.Add(CreateActiveItemInfo(item));
            }
            
            return result.AsReadOnly();
        }
        
        public IReadOnlyList<ActiveItemInfo> GetActiveItemsOfType(ItemType itemType)
        {
            var result = new List<ActiveItemInfo>();
            
            foreach (var item in _activeItems.Where(i => i.definition.Type == itemType))
            {
                result.Add(CreateActiveItemInfo(item));
            }
            
            return result.AsReadOnly();
        }
        
        #endregion

        #region Private Helper Methods
        
        private void UpdateActiveItems()
        {
            // Update timers và check expiry
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                item.timer.Update(Time.deltaTime);
                
                if (item.timer.IsExpired)
                {
                    RemoveActiveItem(i, true); // true = expired
                }
                else
                {
                    // Update item trong list (struct copy)
                    _activeItems[i] = item;
                }
            }
        }
        
        private void UpdateManualCooldown()
        {
            if (_manualCooldownTimer.IsRunning)
            {
                _manualCooldownTimer.Update(Time.deltaTime);
                
                if (_manualCooldownTimer.IsExpired)
                {
                    OnManualCooldownChanged?.Invoke(false, 0f);
                    LogDebug("[ItemEffectSystem] Manual cooldown finished");
                }
            }
        }
        
        private bool SetManualItem(ItemDefinition item)
        {
            if (_manualItem != null)
            {
                LogDebug($"[ItemEffectSystem] Replacing manual item: {_manualItem.DisplayName} -> {item.DisplayName}");
            }
            
            _manualItem = item;
            OnManualItemChanged?.Invoke(item, "set");
            
            LogDebug($"[ItemEffectSystem] Manual item set: {item.DisplayName}");
            return true;
        }
        
        private bool ActivateLifeItem(ItemDefinition item)
        {
            if (_healthSystemInterface == null)
            {
                Debug.LogWarning("[ItemEffectSystem] Cannot activate life item - no health system");
                return false;
            }
            
            int restoreAmount = Mathf.RoundToInt(item.EffectValue);
            
            // Check if can restore health (don't waste life items)
            if (_healthSystemInterface.CurrentHealth >= _healthSystemInterface.MaxHealth)
            {
                LogDebug("[ItemEffectSystem] Health already at max - life item ignored");
                return false;
            }
            
            _healthSystemInterface.RestoreHealth(restoreAmount);
            
            // Fire activation event (immediate)
            OnItemActivated?.Invoke(item, false);
            OnItemDeactivated?.Invoke(item, false); // Immediately deactivated
            
            LogDebug($"[ItemEffectSystem] Life item activated - restored {restoreAmount} health");
            return true;
        }
        
        private bool ActivateCurrencyItem(ItemDefinition item)
        {
            if (_currencyManager == null)
            {
                Debug.LogWarning("[ItemEffectSystem] Cannot activate currency item - no currency manager");
                return false;
            }
            
            // Add currency to player
            bool success = _currencyManager.AddCurrency(item.CurrencyType, item.CurrencyAmount, $"Item: {item.DisplayName}");
            
            if (success)
            {
                // Fire activation event (immediate)
                OnItemActivated?.Invoke(item, false);
                OnItemDeactivated?.Invoke(item, false); // Immediately deactivated
                
                LogDebug($"[ItemEffectSystem] Currency item activated - added {item.CurrencyAmount} {item.CurrencyType}");
            }
            else
            {
                LogDebug($"[ItemEffectSystem] Failed to add currency: {item.CurrencyAmount} {item.CurrencyType}");
            }
            
            return success;
        }
        
        private bool ActivateDurationItem(ItemDefinition item)
        {
            // Check space limit
            if (_activeItems.Count >= _maxActiveItems)
            {
                Debug.LogWarning($"[ItemEffectSystem] Max active items reached ({_maxActiveItems})");
                return false;
            }
            
            // Handle stacking logic
            return HandleItemStacking(item);
        }
        
        private bool HandleItemStacking(ItemDefinition item)
        {
            // Tìm existing item cùng type
            var existingIndex = _activeItems.FindIndex(i => i.definition.ItemId == item.ItemId);
            
            if (existingIndex >= 0)
            {
                // Handle stacking
                return ApplyStackingRule(existingIndex, item);
            }
            else
            {
                // Add new item
                return AddNewActiveItem(item);
            }
        }
        
        private bool ApplyStackingRule(int existingIndex, ItemDefinition newItem)
        {
            var existing = _activeItems[existingIndex];
            
            switch (newItem.StackingRule)
            {
                case ItemStackingRule.Replace:
                    LogDebug($"[ItemEffectSystem] Replacing item: {newItem.DisplayName}");
                    RemoveActiveItem(existingIndex, false);
                    return AddNewActiveItem(newItem);
                
                case ItemStackingRule.Add:
                    LogDebug($"[ItemEffectSystem] Adding duration: {newItem.DisplayName}");
                    existing.timer.Start(existing.timer.TimeRemaining + newItem.Duration);
                    _activeItems[existingIndex] = existing;
                    OnItemStacked?.Invoke(newItem, existing.stackCount, existing.effectValue);
                    return true;
                
                case ItemStackingRule.Multiply:
                    LogDebug($"[ItemEffectSystem] Multiplying effect: {newItem.DisplayName}");
                    existing.stackCount++;
                    existing.effectValue *= newItem.EffectValue;
                    _activeItems[existingIndex] = existing;
                    
                    // Update multiplier value
                    UpdateMultiplierValue();
                    OnItemStacked?.Invoke(newItem, existing.stackCount, existing.effectValue);
                    return true;
                
                default:
                    return AddNewActiveItem(newItem);
            }
        }
        
        private bool AddNewActiveItem(ItemDefinition item)
        {
            var activeItem = new ActiveItem(item, item.Duration, item.EffectValue);
            
            _activeItems.Add(activeItem);
            _itemLookup[item.ItemId] = _activeItems.Count - 1;
            
            // Apply item effects
            ApplyItemEffects(item, true);
            
            OnItemActivated?.Invoke(item, false);
            
            LogDebug($"[ItemEffectSystem] Added active item: {item.DisplayName} ({item.Duration:F1}s)");
            return true;
        }
        
        private void RemoveActiveItem(int index, bool expired)
        {
            if (index < 0 || index >= _activeItems.Count) return;
            
            var item = _activeItems[index];
            
            // Remove from lookup
            _itemLookup.Remove(item.definition.ItemId);
            
            // Update indices cho items sau index này
            foreach (var kvp in _itemLookup.ToList())
            {
                if (kvp.Value > index)
                {
                    _itemLookup[kvp.Key] = kvp.Value - 1;
                }
            }
            
            // Remove từ list
            _activeItems.RemoveAt(index);
            
            // Disable item effects
            ApplyItemEffects(item.definition, false);
            
            OnItemDeactivated?.Invoke(item.definition, expired);
            
            LogDebug($"[ItemEffectSystem] Removed item: {item.definition.DisplayName} (expired: {expired})");
        }
        
        private void ApplyItemEffects(ItemDefinition item, bool activate)
        {
            switch (item.Type)
            {
                case ItemType.Magnet:
                    ApplyMagnetEffect(item, activate);
                    break;
                    
                case ItemType.Multiplier:
                    UpdateMultiplierValue();
                    break;
                    
                case ItemType.Invisible:
                    ApplyInvisibleEffect(item, activate);
                    break;
            }
        }
        
        private void ApplyMagnetEffect(ItemDefinition item, bool activate)
        {
            _isMagnetActive = activate && _activeItems.Any(i => i.definition.Type == ItemType.Magnet);
            
            float radius = activate ? item.EffectValue : 0f;
            float speed = activate ? 8f : 0f; // Default magnet speed
            
            OnMagnetStateChanged?.Invoke(_isMagnetActive, radius, speed);
            
            LogDebug($"[ItemEffectSystem] Magnet {(activate ? "activated" : "deactivated")} - radius: {radius:F1}");
        }
        
        private void ApplyInvisibleEffect(ItemDefinition item, bool activate)
        {
            _isInvisibleActive = activate && _activeItems.Any(i => i.definition.Type == ItemType.Invisible);
            
            // Change layer via RunnerController
            if (_runnerControllerInterface != null)
            {
                // Logic sẽ được implement trong RunnerController integration
                LogDebug($"[ItemEffectSystem] Invisible {(activate ? "activated" : "deactivated")} - would change layer");
            }
            
            OnInvisibleStateChanged?.Invoke(_isInvisibleActive, "PlayerIFrame");
        }
        
        private void UpdateMultiplierValue()
        {
            float oldMultiplier = _currentMultiplier;
            _currentMultiplier = 1f;
            
            // Calculate total multiplier từ all active multiplier items
            foreach (var item in _activeItems.Where(i => i.definition.Type == ItemType.Multiplier))
            {
                _currentMultiplier *= item.effectValue;
            }
            
            if (Mathf.Abs(_currentMultiplier - oldMultiplier) > 0.01f)
            {
                OnMultiplierChanged?.Invoke(oldMultiplier, _currentMultiplier);
                LogDebug($"[ItemEffectSystem] Multiplier changed: {oldMultiplier:F1}x -> {_currentMultiplier:F1}x");
            }
        }
        
        private ActiveItemInfo CreateActiveItemInfo(ActiveItem item)
        {
            float remainingTime = item.timer.IsRunning ? 
                item.timer.Duration * (1f - item.timer.Progress) : 0f;
            float progress = item.timer.Progress;
            
            return new ActiveItemInfo(
                item.definition.ItemId,
                item.definition,
                remainingTime,
                progress,
                item.effectValue,
                item.stackCount,
                item.activatedAt
            );
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message);
            }
        }
        
        #endregion

        #region Editor Utilities
        
        #if UNITY_EDITOR
        
        [ContextMenu("Debug Active Items")]
        private void DebugActiveItems()
        {
            Debug.Log($"=== ItemEffectSystem Debug ===");
            Debug.Log($"Active Items: {_activeItems.Count}/{_maxActiveItems}");
            Debug.Log($"Current Multiplier: {_currentMultiplier:F1}x");
            Debug.Log($"Magnet Active: {_isMagnetActive}");
            Debug.Log($"Invisible Active: {_isInvisibleActive}");
            Debug.Log($"Manual Item: {(_manualItem?.DisplayName ?? "None")}");
            Debug.Log($"Manual Cooldown: {(IsManualCooldown ? $"{ManualCooldownRemaining:F1}s" : "None")}");
            
            foreach (var item in _activeItems)
            {
                float remaining = item.timer.IsRunning ? 
                    item.timer.Duration * (1f - item.timer.Progress) : 0f;
                Debug.Log($"- {item.definition.DisplayName}: {remaining:F1}s remaining, " +
                         $"stack: {item.stackCount}, value: {item.effectValue:F1}");
            }
        }
        
        [ContextMenu("Clear All Items")]
        private void EditorClearItems()
        {
            if (Application.isPlaying)
                ClearAllItems();
        }
        
        #endif
        
        #endregion
    }
}
