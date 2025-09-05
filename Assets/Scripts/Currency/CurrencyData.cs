using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EndlessRunner.Currency
{
    /// <summary>
    /// ScriptableObject chứa configuration và persistent data cho currency system
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyData", menuName = "EndlessRunner/Currency/CurrencyData")]
    public class CurrencyData : ScriptableObject
    {
        [Header("Currency Configuration")]
        [SerializeField] 
        [Tooltip("Configurations cho tất cả currency types")]
        private CurrencyConfig[] _currencyConfigs = new CurrencyConfig[]
        {
            CurrencyConfig.GetDefault(CurrencyType.Coins),
            CurrencyConfig.GetDefault(CurrencyType.Gems),
            CurrencyConfig.GetDefault(CurrencyType.Keys),
            CurrencyConfig.GetDefault(CurrencyType.Experience)
        };

        [Header("Persistence Settings")]
        [SerializeField]
        [Tooltip("Enable auto-save khi có thay đổi")]
        private bool _autoSave = true;

        [SerializeField]
        [Tooltip("Auto-save interval (seconds)")]
        private float _autoSaveInterval = 30f;

        [SerializeField]
        [Tooltip("PlayerPrefs key prefix cho save data")]
        private string _saveKeyPrefix = "Currency_";

        [Header("Runtime Data - Readonly")]
        [SerializeField]
        [Tooltip("Current currency amounts - chỉ đọc trong inspector")]
        private CurrencyAmount[] _currentAmounts = new CurrencyAmount[0];

        // Runtime dictionary cho fast lookup
        private Dictionary<CurrencyType, CurrencyConfig> _configLookup;
        private Dictionary<CurrencyType, long> _amounts;
        private bool _isInitialized = false;

        #region Properties

        public bool IsInitialized => _isInitialized;
        public bool AutoSave => _autoSave;
        public float AutoSaveInterval => _autoSaveInterval;
        public IReadOnlyList<CurrencyConfig> CurrencyConfigs => _currencyConfigs;
        public IReadOnlyList<CurrencyType> AvailableCurrencies => _configLookup?.Keys.ToList();

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize currency data - gọi từ CurrencyManager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[CurrencyData] Already initialized!", this);
                return;
            }

            // Build lookup dictionaries
            BuildConfigLookup();
            InitializeAmounts();

            _isInitialized = true;
            LogDebug("[CurrencyData] Initialized successfully");
        }

        private void BuildConfigLookup()
        {
            _configLookup = new Dictionary<CurrencyType, CurrencyConfig>();
            
            foreach (var config in _currencyConfigs)
            {
                if (config.Type == CurrencyType.None)
                {
                    Debug.LogWarning($"[CurrencyData] Skipping None currency type in config", this);
                    continue;
                }

                if (_configLookup.ContainsKey(config.Type))
                {
                    Debug.LogWarning($"[CurrencyData] Duplicate config for {config.Type}", this);
                    continue;
                }

                _configLookup[config.Type] = config;
            }
        }

        private void InitializeAmounts()
        {
            _amounts = new Dictionary<CurrencyType, long>();
            
            // Set default amounts từ config
            foreach (var config in _configLookup.Values)
            {
                _amounts[config.Type] = config.StartAmount;
            }

            UpdateInspectorAmounts();
        }

        #endregion

        #region Configuration Access

        /// <summary>
        /// Get config cho currency type
        /// </summary>
        public CurrencyConfig GetConfig(CurrencyType type)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CurrencyData] Not initialized!", this);
                return CurrencyConfig.GetDefault(type);
            }

            if (_configLookup.TryGetValue(type, out var config))
            {
                return config;
            }

            Debug.LogWarning($"[CurrencyData] No config found for {type}, using default", this);
            return CurrencyConfig.GetDefault(type);
        }

        /// <summary>
        /// Check if currency type được support
        /// </summary>
        public bool IsSupported(CurrencyType type)
        {
            return _isInitialized && _configLookup.ContainsKey(type);
        }

        #endregion

        #region Amount Management

        /// <summary>
        /// Get current amount
        /// </summary>
        public long GetAmount(CurrencyType type)
        {
            if (!_isInitialized || !_amounts.TryGetValue(type, out long amount))
            {
                return 0;
            }

            return amount;
        }

        /// <summary>
        /// Set amount với validation
        /// </summary>
        public bool SetAmount(CurrencyType type, long amount, out long actualAmount)
        {
            actualAmount = amount;

            if (!_isInitialized)
            {
                Debug.LogError("[CurrencyData] Not initialized!", this);
                return false;
            }

            if (!IsSupported(type))
            {
                Debug.LogWarning($"[CurrencyData] Unsupported currency type: {type}", this);
                return false;
            }

            var config = GetConfig(type);
            
            // Validate constraints
            if (!config.CanGoNegative && amount < 0)
            {
                actualAmount = 0;
                LogDebug($"[CurrencyData] Clamped {type} to 0 (was {amount})");
            }
            else if (amount > config.MaxAmount)
            {
                actualAmount = config.MaxAmount;
                LogDebug($"[CurrencyData] Clamped {type} to max {config.MaxAmount} (was {amount})");
            }

            _amounts[type] = actualAmount;
            UpdateInspectorAmounts();

            return actualAmount == amount; // true nếu không bị clamp
        }

        /// <summary>
        /// Add amount với validation
        /// </summary>
        public bool AddAmount(CurrencyType type, long delta, out long actualDelta)
        {
            long currentAmount = GetAmount(type);
            long newAmount = currentAmount + delta;
            
            bool success = SetAmount(type, newAmount, out long actualNewAmount);
            actualDelta = actualNewAmount - currentAmount;
            
            return success && actualDelta == delta;
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Save data to PlayerPrefs
        /// </summary>
        public void SaveToPlayerPrefs()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CurrencyData] Cannot save - not initialized!", this);
                return;
            }

            try
            {
                foreach (var kvp in _amounts)
                {
                    string key = _saveKeyPrefix + kvp.Key.ToString();
                    string value = kvp.Value.ToString();
                    PlayerPrefs.SetString(key, value);
                }

                // Save timestamp
                PlayerPrefs.SetString(_saveKeyPrefix + "SaveTime", DateTime.Now.ToBinary().ToString());
                PlayerPrefs.Save();

                LogDebug("[CurrencyData] Saved to PlayerPrefs");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyData] Save failed: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Load data from PlayerPrefs
        /// </summary>
        public void LoadFromPlayerPrefs()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CurrencyData] Cannot load - not initialized!", this);
                return;
            }

            try
            {
                bool hasAnyData = false;

                foreach (var type in _configLookup.Keys.ToList())
                {
                    string key = _saveKeyPrefix + type.ToString();
                    
                    if (PlayerPrefs.HasKey(key))
                    {
                        string valueStr = PlayerPrefs.GetString(key, "0");
                        if (long.TryParse(valueStr, out long value))
                        {
                            SetAmount(type, value, out _);
                            hasAnyData = true;
                        }
                        else
                        {
                            Debug.LogWarning($"[CurrencyData] Invalid saved value for {type}: {valueStr}", this);
                        }
                    }
                }

                if (hasAnyData)
                {
                    LogDebug("[CurrencyData] Loaded from PlayerPrefs");
                }
                else
                {
                    LogDebug("[CurrencyData] No saved data found, using defaults");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyData] Load failed: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Clear saved data
        /// </summary>
        public void ClearSavedData()
        {
            try
            {
                foreach (var type in System.Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>())
                {
                    if (type == CurrencyType.None) continue;
                    
                    string key = _saveKeyPrefix + type.ToString();
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                    }
                }

                PlayerPrefs.DeleteKey(_saveKeyPrefix + "SaveTime");
                PlayerPrefs.Save();

                LogDebug("[CurrencyData] Cleared saved data");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyData] Clear data failed: {ex.Message}", this);
            }
        }

        #endregion

        #region Inspector Support

        /// <summary>
        /// Struct cho inspector display
        /// </summary>
        [System.Serializable]
        private struct CurrencyAmount
        {
            public CurrencyType type;
            public long amount;
            public string formatted;

            public CurrencyAmount(CurrencyType type, long amount)
            {
                this.type = type;
                this.amount = amount;
                this.formatted = CurrencyFormatter.FormatAmount(amount);
            }
        }

        /// <summary>
        /// Update inspector display
        /// </summary>
        private void UpdateInspectorAmounts()
        {
            if (!_isInitialized) return;

            _currentAmounts = new CurrencyAmount[_amounts.Count];
            int index = 0;
            
            foreach (var kvp in _amounts)
            {
                _currentAmounts[index] = new CurrencyAmount(kvp.Key, kvp.Value);
                index++;
            }
        }

        #endregion

        #region Validation & Debug

        private void OnValidate()
        {
            // Validate configs
            var typesSeen = new HashSet<CurrencyType>();
            
            for (int i = 0; i < _currencyConfigs.Length; i++)
            {
                var config = _currencyConfigs[i];
                
                if (config.Type == CurrencyType.None)
                {
                    Debug.LogWarning($"[CurrencyData] Config {i} has None type", this);
                }
                
                if (typesSeen.Contains(config.Type))
                {
                    Debug.LogWarning($"[CurrencyData] Duplicate config for {config.Type}", this);
                }
                else
                {
                    typesSeen.Add(config.Type);
                }
            }

            // Clamp values
            _autoSaveInterval = Mathf.Max(1f, _autoSaveInterval);
        }

        private void LogDebug(string message)
        {
            #if UNITY_EDITOR
            Debug.Log(message);
            #endif
        }

        #if UNITY_EDITOR
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CurrencyData] Can only reset in play mode");
                return;
            }

            if (_isInitialized)
            {
                foreach (var config in _configLookup.Values)
                {
                    SetAmount(config.Type, config.StartAmount, out _);
                }
                
                Debug.Log("[CurrencyData] Reset to default amounts");
            }
        }

        [ContextMenu("Debug Current State")]
        private void DebugCurrentState()
        {
            Debug.Log("=== CurrencyData Debug ===");
            Debug.Log($"Initialized: {_isInitialized}");
            Debug.Log($"Auto Save: {_autoSave} ({_autoSaveInterval}s)");
            
            if (_isInitialized && _amounts != null)
            {
                foreach (var kvp in _amounts)
                {
                    var config = GetConfig(kvp.Key);
                    Debug.Log($"{kvp.Key}: {CurrencyFormatter.FormatAmount(kvp.Value)} " +
                             $"(max: {CurrencyFormatter.FormatAmount(config.MaxAmount)})");
                }
            }
        }
        #endif

        #endregion
    }
}
