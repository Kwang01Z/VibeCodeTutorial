using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Currency
{
    /// <summary>
    /// CurrencyManager - Core implementation của currency system cho Phase 3
    /// Thread-safe operations, persistence, validation, và event system
    /// </summary>
    public class CurrencyManager : MonoBehaviour, ICurrencySystem
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("CurrencyData ScriptableObject chứa config và persistent data")]
        private CurrencyData _currencyData;
        
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _enableDebugLog = false;
        
        [SerializeField]
        [Tooltip("Enable transaction history tracking")]
        private bool _trackTransactionHistory = true;
        
        [SerializeField]
        [Tooltip("Maximum transaction history entries")]
        private int _maxHistoryEntries = 100;
        
        #endregion
        
        #region Private Fields
        
        // Thread safety - sử dụng lock cho multi-threaded access
        private readonly object _lockObject = new object();
        
        // Auto-save system
        private Timer _autoSaveTimer;
        private bool _hasUnsavedChanges = false;
        
        // Transaction history cho debugging/analytics
        private List<CurrencyTransaction> _transactionHistory;
        
        // Cached available currencies
        private List<CurrencyType> _availableCurrencies;
        
        // State tracking
        private bool _isInitialized = false;
        
        #endregion
        
        #region ICurrencySystem Properties
        
        public bool IsInitialized 
        {
            get 
            {
                lock (_lockObject)
                {
                    return _isInitialized;
                }
            }
        }
        
        public IReadOnlyList<CurrencyType> AvailableCurrencies 
        {
            get
            {
                lock (_lockObject)
                {
                    return _availableCurrencies?.AsReadOnly();
                }
            }
        }
        
        #endregion
        
        #region ICurrencySystem Events
        
        public event Action<CurrencyType, long, long, string> OnCurrencyChanged;
        public event Action<CurrencyTransaction, bool> OnTransactionProcessed;
        public event Action<CurrencyTransaction[], bool> OnBatchTransactionProcessed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeSystem();
        }
        
        private void Start()
        {
            LoadData();
            SetupAutoSave();
        }
        
        private void Update()
        {
            UpdateAutoSave();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _hasUnsavedChanges)
            {
                SaveData();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _hasUnsavedChanges)
            {
                SaveData();
            }
        }
        
        private void OnDestroy()
        {
            SaveData();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSystem()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    LogDebug("[CurrencyManager] Already initialized!");
                    return;
                }
                
                // Validate currency data
                if (_currencyData == null)
                {
                    Debug.LogError("[CurrencyManager] CurrencyData is null! Creating default data.", this);
                    _currencyData = ScriptableObject.CreateInstance<CurrencyData>();
                }
                
                // Initialize currency data
                _currencyData.Initialize();
                
                // Setup transaction history
                if (_trackTransactionHistory)
                {
                    _transactionHistory = new List<CurrencyTransaction>();
                }
                
                // Cache available currencies
                _availableCurrencies = _currencyData.AvailableCurrencies?.ToList() ?? new List<CurrencyType>();
                
                // Initialize auto-save timer
                _autoSaveTimer = new Timer();
                
                _isInitialized = true;
                LogDebug($"[CurrencyManager] Initialized with {_availableCurrencies.Count} currencies");
            }
        }
        
        private void SetupAutoSave()
        {
            if (_currencyData != null && _currencyData.AutoSave)
            {
                _autoSaveTimer.Start(_currencyData.AutoSaveInterval);
                LogDebug($"[CurrencyManager] Auto-save enabled: {_currencyData.AutoSaveInterval}s");
            }
        }
        
        private void UpdateAutoSave()
        {
            if (_autoSaveTimer.IsRunning)
            {
                _autoSaveTimer.Update(Time.deltaTime);
                
                if (_autoSaveTimer.IsExpired && _hasUnsavedChanges)
                {
                    SaveData();
                    _autoSaveTimer.Start(_currencyData.AutoSaveInterval);
                }
            }
        }
        
        #endregion
        
        #region ICurrencySystem Implementation - Currency Operations
        
        public long GetAmount(CurrencyType type)
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return 0;
                return _currencyData.GetAmount(type);
            }
        }
        
        public bool HasEnough(CurrencyType type, long amount)
        {
            if (amount <= 0) return true;
            
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return false;
                return _currencyData.GetAmount(type) >= amount;
            }
        }
        
        public bool HasEnough(params CurrencyTransaction[] transactions)
        {
            if (transactions == null || transactions.Length == 0) return true;
            
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return false;
                
                foreach (var transaction in transactions)
                {
                    if (!transaction.IsValid) continue;
                    
                    // Only check spend transactions (negative amounts)
                    if (transaction.amount < 0)
                    {
                        long required = Math.Abs(transaction.amount);
                        if (_currencyData.GetAmount(transaction.type) < required)
                        {
                            return false;
                        }
                    }
                }
                
                return true;
            }
        }
        
        public bool AddCurrency(CurrencyType type, long amount, string reason = "")
        {
            if (amount <= 0)
            {
                LogDebug($"[CurrencyManager] Invalid add amount: {amount}");
                return false;
            }
            
            var transaction = new CurrencyTransaction(type, amount, reason);
            return ProcessSingleTransaction(transaction);
        }
        
        public bool SpendCurrency(CurrencyType type, long amount, string reason = "")
        {
            if (amount <= 0)
            {
                LogDebug($"[CurrencyManager] Invalid spend amount: {amount}");
                return false;
            }
            
            var transaction = new CurrencyTransaction(type, -amount, reason);
            return ProcessSingleTransaction(transaction);
        }
        
        public bool SetCurrency(CurrencyType type, long amount, string reason = "")
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return false;
                
                long oldAmount = _currencyData.GetAmount(type);
                
                if (_currencyData.SetAmount(type, amount, out long actualAmount))
                {
                    // Fire events
                    FireCurrencyChangedEvent(type, oldAmount, actualAmount, reason);
                    
                    // Track transaction
                    var transaction = new CurrencyTransaction(type, actualAmount - oldAmount, $"Set: {reason}");
                    TrackTransaction(transaction, true);
                    
                    _hasUnsavedChanges = true;
                    
                    LogDebug($"[CurrencyManager] Set {type}: {oldAmount} -> {actualAmount} ({reason})");
                    return true;
                }
                
                return false;
            }
        }
        
        public bool ProcessTransactions(params CurrencyTransaction[] transactions)
        {
            if (transactions == null || transactions.Length == 0)
            {
                LogDebug("[CurrencyManager] No transactions to process");
                return true;
            }
            
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return false;
                
                // Validate all transactions first
                foreach (var transaction in transactions)
                {
                    if (!ValidateTransaction(transaction))
                    {
                        LogDebug($"[CurrencyManager] Invalid transaction: {transaction.type} {transaction.amount}");
                        OnBatchTransactionProcessed?.Invoke(transactions, false);
                        return false;
                    }
                }
                
                // Check if có đủ currency cho spend transactions
                if (!HasEnough(transactions))
                {
                    LogDebug("[CurrencyManager] Insufficient currency for batch transaction");
                    OnBatchTransactionProcessed?.Invoke(transactions, false);
                    return false;
                }
                
                // Process all transactions
                var oldAmounts = new Dictionary<CurrencyType, long>();
                bool allSuccess = true;
                
                foreach (var transaction in transactions)
                {
                    var type = transaction.type;
                    
                    // Store old amount
                    if (!oldAmounts.ContainsKey(type))
                    {
                        oldAmounts[type] = _currencyData.GetAmount(type);
                    }
                    
                    // Process transaction
                    long currentAmount = _currencyData.GetAmount(type);
                    long newAmount = currentAmount + transaction.amount;
                    
                    if (_currencyData.SetAmount(type, newAmount, out long actualAmount))
                    {
                        TrackTransaction(transaction, true);
                        OnTransactionProcessed?.Invoke(transaction, true);
                    }
                    else
                    {
                        TrackTransaction(transaction, false);
                        OnTransactionProcessed?.Invoke(transaction, false);
                        allSuccess = false;
                    }
                }
                
                // Fire currency changed events
                foreach (var kvp in oldAmounts)
                {
                    long newAmount = _currencyData.GetAmount(kvp.Key);
                    if (newAmount != kvp.Value)
                    {
                        FireCurrencyChangedEvent(kvp.Key, kvp.Value, newAmount, "Batch Transaction");
                    }
                }
                
                OnBatchTransactionProcessed?.Invoke(transactions, allSuccess);
                
                if (allSuccess)
                {
                    _hasUnsavedChanges = true;
                    LogDebug($"[CurrencyManager] Processed {transactions.Length} transactions successfully");
                }
                
                return allSuccess;
            }
        }
        
        #endregion
        
        #region ICurrencySystem Implementation - Configuration & Persistence
        
        public CurrencyConfig GetConfig(CurrencyType type)
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return CurrencyConfig.GetDefault(type);
                return _currencyData.GetConfig(type);
            }
        }
        
        public void ResetCurrencies()
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return;
                
                foreach (var type in _availableCurrencies)
                {
                    var config = _currencyData.GetConfig(type);
                    SetCurrency(type, config.StartAmount, "Reset");
                }
                
                LogDebug("[CurrencyManager] Reset all currencies to default values");
            }
        }
        
        public void SaveData()
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return;
                
                try
                {
                    _currencyData.SaveToPlayerPrefs();
                    _hasUnsavedChanges = false;
                    LogDebug("[CurrencyManager] Data saved successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CurrencyManager] Save failed: {ex.Message}", this);
                }
            }
        }
        
        public void LoadData()
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return;
                
                try
                {
                    _currencyData.LoadFromPlayerPrefs();
                    _hasUnsavedChanges = false;
                    
                    // Fire events for initial loaded values
                    foreach (var type in _availableCurrencies)
                    {
                        long amount = _currencyData.GetAmount(type);
                        FireCurrencyChangedEvent(type, 0, amount, "Data Loaded");
                    }
                    
                    LogDebug("[CurrencyManager] Data loaded successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CurrencyManager] Load failed: {ex.Message}", this);
                }
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private bool ProcessSingleTransaction(CurrencyTransaction transaction)
        {
            lock (_lockObject)
            {
                if (!ValidateInitialized()) return false;
                
                if (!ValidateTransaction(transaction))
                {
                    TrackTransaction(transaction, false);
                    OnTransactionProcessed?.Invoke(transaction, false);
                    return false;
                }
                
                long oldAmount = _currencyData.GetAmount(transaction.type);
                long newAmount = oldAmount + transaction.amount;
                
                // Check sufficient funds for spend transactions
                if (transaction.amount < 0 && oldAmount < Math.Abs(transaction.amount))
                {
                    LogDebug($"[CurrencyManager] Insufficient {transaction.type}: has {oldAmount}, need {Math.Abs(transaction.amount)}");
                    TrackTransaction(transaction, false);
                    OnTransactionProcessed?.Invoke(transaction, false);
                    return false;
                }
                
                if (_currencyData.SetAmount(transaction.type, newAmount, out long actualAmount))
                {
                    FireCurrencyChangedEvent(transaction.type, oldAmount, actualAmount, transaction.reason);
                    TrackTransaction(transaction, true);
                    OnTransactionProcessed?.Invoke(transaction, true);
                    
                    _hasUnsavedChanges = true;
                    
                    LogDebug($"[CurrencyManager] {transaction.type}: {oldAmount} -> {actualAmount} ({transaction.reason})");
                    return true;
                }
                
                TrackTransaction(transaction, false);
                OnTransactionProcessed?.Invoke(transaction, false);
                return false;
            }
        }
        
        private bool ValidateTransaction(CurrencyTransaction transaction)
        {
            if (!transaction.IsValid)
            {
                return false;
            }
            
            if (!_currencyData.IsSupported(transaction.type))
            {
                LogDebug($"[CurrencyManager] Unsupported currency type: {transaction.type}");
                return false;
            }
            
            return true;
        }
        
        private bool ValidateInitialized()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CurrencyManager] Not initialized!", this);
                return false;
            }
            
            return true;
        }
        
        private void FireCurrencyChangedEvent(CurrencyType type, long oldAmount, long newAmount, string reason)
        {
            try
            {
                OnCurrencyChanged?.Invoke(type, oldAmount, newAmount, reason);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyManager] Event error: {ex.Message}", this);
            }
        }
        
        private void TrackTransaction(CurrencyTransaction transaction, bool success)
        {
            if (!_trackTransactionHistory || _transactionHistory == null) return;
            
            try
            {
                _transactionHistory.Add(transaction);
                
                // Trim history if too long
                if (_transactionHistory.Count > _maxHistoryEntries)
                {
                    int removeCount = _transactionHistory.Count - _maxHistoryEntries;
                    _transactionHistory.RemoveRange(0, removeCount);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyManager] Transaction history error: {ex.Message}", this);
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Get transaction history (for debugging/analytics)
        /// </summary>
        public IReadOnlyList<CurrencyTransaction> GetTransactionHistory()
        {
            lock (_lockObject)
            {
                return _transactionHistory?.AsReadOnly();
            }
        }
        
        /// <summary>
        /// Clear transaction history
        /// </summary>
        public void ClearTransactionHistory()
        {
            lock (_lockObject)
            {
                _transactionHistory?.Clear();
                LogDebug("[CurrencyManager] Transaction history cleared");
            }
        }
        
        /// <summary>
        /// Get formatted display string cho currency amount
        /// </summary>
        public string GetFormattedAmount(CurrencyType type, bool useAbbreviation = true)
        {
            long amount = GetAmount(type);
            return CurrencyFormatter.FormatAmount(amount, useAbbreviation);
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        
        [ContextMenu("Debug Currency State")]
        private void DebugCurrencyState()
        {
            Debug.Log("=== CurrencyManager Debug ===");
            Debug.Log($"Initialized: {_isInitialized}");
            Debug.Log($"Unsaved Changes: {_hasUnsavedChanges}");
            Debug.Log($"Auto Save: {_autoSaveTimer.IsRunning}");
            
            if (_isInitialized)
            {
                foreach (var type in _availableCurrencies)
                {
                    long amount = GetAmount(type);
                    var config = GetConfig(type);
                    Debug.Log($"{type}: {CurrencyFormatter.FormatAmount(amount)} / {CurrencyFormatter.FormatAmount(config.MaxAmount)}");
                }
            }
            
            if (_transactionHistory != null && _transactionHistory.Count > 0)
            {
                Debug.Log($"Transaction History: {_transactionHistory.Count} entries");
                for (int i = Math.Max(0, _transactionHistory.Count - 5); i < _transactionHistory.Count; i++)
                {
                    var tx = _transactionHistory[i];
                    Debug.Log($"  {tx.type}: {CurrencyFormatter.FormatTransaction(tx)} - {tx.reason}");
                }
            }
        }
        
        [ContextMenu("Add Test Currency")]
        private void AddTestCurrency()
        {
            if (Application.isPlaying)
            {
                AddCurrency(CurrencyType.Coins, 1000, "Test Add");
                AddCurrency(CurrencyType.Gems, 50, "Test Add");
            }
        }
        
        [ContextMenu("Reset All Currencies")]
        private void EditorResetCurrencies()
        {
            if (Application.isPlaying)
            {
                ResetCurrencies();
            }
        }
        
        #endif
        
        #endregion
    }
}
