using System;
using System.Collections.Generic;
using EndlessRunner.Data;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface cho Currency Management System quản lý Xương Cá (session) và Bẫy Chuột (persistent).
    /// Supports multipliers, milestone rewards, conversion, và save/load integration.
    /// </summary>
    public interface ICurrencyManager
    {
        #region Properties
        
        /// <summary>Có currency nào đang được track không</summary>
        bool HasCurrencies { get; }
        
        /// <summary>Current multiplier áp dụng cho session currencies</summary>
        float CurrentMultiplier { get; }
        
        /// <summary>Total session currencies earned this run</summary>
        int TotalSessionEarned { get; }
        
        /// <summary>Total persistent currencies earned all time</summary>
        int TotalPersistentEarned { get; }
        
        #endregion

        #region Currency Access Methods
        
        /// <summary>
        /// Get current amount của currency.
        /// </summary>
        /// <param name="currencyId">ID của currency</param>
        /// <returns>Current amount, -1 nếu không tìm thấy</returns>
        int GetCurrency(string currencyId);
        
        /// <summary>
        /// Get currency amount theo definition.
        /// </summary>
        /// <param name="definition">Currency definition</param>
        /// <returns>Current amount, -1 nếu không tìm thấy</returns>
        int GetCurrency(CurrencyDefinition definition);
        
        /// <summary>
        /// Check xem có đủ currency để spend không.
        /// </summary>
        /// <param name="currencyId">ID của currency</param>
        /// <param name="amount">Amount cần check</param>
        /// <returns>True nếu có đủ</returns>
        bool CanAfford(string currencyId, int amount);
        
        /// <summary>
        /// Check xem có đủ currency để spend không.
        /// </summary>
        /// <param name="definition">Currency definition</param>
        /// <param name="amount">Amount cần check</param>
        /// <returns>True nếu có đủ</returns>
        bool CanAfford(CurrencyDefinition definition, int amount);
        
        /// <summary>
        /// Get tất cả registered currencies với amounts.
        /// </summary>
        /// <returns>Dictionary của currencyId -> amount</returns>
        IReadOnlyDictionary<string, int> GetAllCurrencies();
        
        #endregion

        #region Currency Modification Methods
        
        /// <summary>
        /// Add currency với optional multiplier application.
        /// </summary>
        /// <param name="currencyId">ID của currency</param>
        /// <param name="amount">Amount để add</param>
        /// <param name="applyMultiplier">Apply current multiplier nếu currency supports it</param>
        /// <param name="source">Source của currency gain (cho analytics)</param>
        /// <returns>Actual amount added sau khi apply multiplier và clamp</returns>
        int AddCurrency(string currencyId, int amount, bool applyMultiplier = true, string source = "pickup");
        
        /// <summary>
        /// Add currency theo definition.
        /// </summary>
        /// <param name="definition">Currency definition</param>
        /// <param name="amount">Amount để add</param>
        /// <param name="applyMultiplier">Apply current multiplier</param>
        /// <param name="source">Source của currency gain</param>
        /// <returns>Actual amount added</returns>
        int AddCurrency(CurrencyDefinition definition, int amount, bool applyMultiplier = true, string source = "pickup");
        
        /// <summary>
        /// Spend currency nếu có đủ.
        /// </summary>
        /// <param name="currencyId">ID của currency</param>
        /// <param name="amount">Amount để spend</param>
        /// <param name="reason">Reason cho spending (cho analytics)</param>
        /// <returns>True nếu spend thành công</returns>
        bool SpendCurrency(string currencyId, int amount, string reason = "purchase");
        
        /// <summary>
        /// Spend currency theo definition.
        /// </summary>
        /// <param name="definition">Currency definition</param>
        /// <param name="amount">Amount để spend</param>
        /// <param name="reason">Reason cho spending</param>
        /// <returns>True nếu spend thành công</returns>
        bool SpendCurrency(CurrencyDefinition definition, int amount, string reason = "purchase");
        
        /// <summary>
        /// Set currency amount directly (admin/debug use).
        /// </summary>
        /// <param name="currencyId">ID của currency</param>
        /// <param name="amount">New amount</param>
        /// <returns>True nếu set thành công</returns>
        bool SetCurrency(string currencyId, int amount);
        
        #endregion

        #region Multiplier Management
        
        /// <summary>
        /// Set current currency multiplier.
        /// </summary>
        /// <param name="multiplier">New multiplier value</param>
        void SetMultiplier(float multiplier);
        
        /// <summary>
        /// Reset multiplier về 1.0.
        /// </summary>
        void ResetMultiplier();
        
        #endregion

        #region Session Management
        
        /// <summary>
        /// Start new currency session (reset session currencies).
        /// </summary>
        void StartNewSession();
        
        /// <summary>
        /// End current session (trigger conversion, analytics).
        /// </summary>
        void EndSession();
        
        /// <summary>
        /// Reset tất cả currencies về 0 (emergency reset).
        /// </summary>
        /// <param name="includePersistent">Reset cả persistent currencies</param>
        void ResetAllCurrencies(bool includePersistent = false);
        
        #endregion

        #region Milestone & Bonus Methods
        
        /// <summary>
        /// Check và award milestone rewards cho distance.
        /// </summary>
        /// <param name="distance">Current distance</param>
        /// <returns>List các rewards được awarded</returns>
        IList<MilestoneReward> CheckMilestoneRewards(float distance);
        
        /// <summary>
        /// Award bonus currency từ external source.
        /// </summary>
        /// <param name="currencyId">Currency ID</param>
        /// <param name="amount">Bonus amount</param>
        /// <param name="source">Source của bonus</param>
        /// <returns>Actual amount awarded</returns>
        int AwardBonus(string currencyId, int amount, string source = "bonus");
        
        #endregion

        #region Save/Load Integration
        
        /// <summary>
        /// Load currency data từ save system.
        /// </summary>
        /// <param name="saveData">Save data dictionary</param>
        void LoadCurrencyData(Dictionary<string, object> saveData);
        
        /// <summary>
        /// Save currency data to save system.
        /// </summary>
        /// <returns>Save data dictionary</returns>
        Dictionary<string, object> SaveCurrencyData();
        
        #endregion

        #region Events
        
        /// <summary>
        /// Event khi currency amount thay đổi.
        /// Parameters: (currencyId, oldAmount, newAmount, source)
        /// </summary>
        event Action<string, int, int, string> OnCurrencyChanged;
        
        /// <summary>
        /// Event khi currency được spent.
        /// Parameters: (currencyId, amountSpent, reason)
        /// </summary>
        event Action<string, int, string> OnCurrencySpent;
        
        /// <summary>
        /// Event khi currency được earned.
        /// Parameters: (currencyId, amountEarned, finalAmount, source)
        /// </summary>
        event Action<string, int, int, string> OnCurrencyEarned;
        
        /// <summary>
        /// Event khi multiplier thay đổi.
        /// Parameters: (oldMultiplier, newMultiplier)
        /// </summary>
        event Action<float, float> OnMultiplierChanged;
        
        /// <summary>
        /// Event khi milestone reward được awarded.
        /// Parameters: (currencyId, rewardAmount, distance, milestone)
        /// </summary>
        event Action<string, int, float, MilestoneReward> OnMilestoneRewarded;
        
        /// <summary>
        /// Event khi session starts/ends.
        /// Parameters: (isStarting, sessionCurrencyTotal)
        /// </summary>
        event Action<bool, int> OnSessionChanged;
        
        #endregion
    }

    /// <summary>
    /// Milestone reward data structure
    /// </summary>
    public struct MilestoneReward
    {
        public string CurrencyId { get; }
        public int Amount { get; }
        public float Distance { get; }
        public string Description { get; }
        
        public MilestoneReward(string currencyId, int amount, float distance, string description = "")
        {
            CurrencyId = currencyId;
            Amount = amount;
            Distance = distance;
            Description = description;
        }
    }
}
