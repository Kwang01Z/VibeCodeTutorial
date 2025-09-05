using System;
using System.Collections.Generic;

namespace EndlessRunner.Currency
{
    /// <summary>
    /// Interface cho Currency System - sử dụng cho dependency injection và testing
    /// </summary>
    public interface ICurrencySystem
    {
        #region Properties
        
        /// <summary>
        /// Check if currency system đã initialized
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Get all available currency types
        /// </summary>
        IReadOnlyList<CurrencyType> AvailableCurrencies { get; }
        
        #endregion
        
        #region Currency Operations
        
        /// <summary>
        /// Get current amount của currency type
        /// </summary>
        long GetAmount(CurrencyType type);
        
        /// <summary>
        /// Check if có đủ currency để spend
        /// </summary>
        bool HasEnough(CurrencyType type, long amount);
        
        /// <summary>
        /// Check if multiple currencies đủ để spend
        /// </summary>
        bool HasEnough(params CurrencyTransaction[] transactions);
        
        /// <summary>
        /// Add currency amount
        /// </summary>
        bool AddCurrency(CurrencyType type, long amount, string reason = "");
        
        /// <summary>
        /// Spend currency amount
        /// </summary>
        bool SpendCurrency(CurrencyType type, long amount, string reason = "");
        
        /// <summary>
        /// Process multiple currency transactions atomically
        /// </summary>
        bool ProcessTransactions(params CurrencyTransaction[] transactions);
        
        /// <summary>
        /// Set currency amount directly (admin/cheat function)
        /// </summary>
        bool SetCurrency(CurrencyType type, long amount, string reason = "");
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Get configuration cho currency type
        /// </summary>
        CurrencyConfig GetConfig(CurrencyType type);
        
        /// <summary>
        /// Reset currency về default amounts
        /// </summary>
        void ResetCurrencies();
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Save currency data
        /// </summary>
        void SaveData();
        
        /// <summary>
        /// Load currency data
        /// </summary>
        void LoadData();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Currency amount changed event
        /// Params: (CurrencyType type, long oldAmount, long newAmount, string reason)
        /// </summary>
        event Action<CurrencyType, long, long, string> OnCurrencyChanged;
        
        /// <summary>
        /// Currency transaction processed event
        /// Params: (CurrencyTransaction transaction, bool success)
        /// </summary>
        event Action<CurrencyTransaction, bool> OnTransactionProcessed;
        
        /// <summary>
        /// Multiple transactions processed event (for atomic operations)
        /// Params: (CurrencyTransaction[] transactions, bool allSuccess)
        /// </summary>
        event Action<CurrencyTransaction[], bool> OnBatchTransactionProcessed;
        
        #endregion
    }
}
