using System;
using UnityEngine;

namespace EndlessRunner.Currency
{
    /// <summary>
    /// Enum định nghĩa các loại tiền tệ trong game
    /// </summary>
    [System.Serializable]
    public enum CurrencyType
    {
        None = 0,
        Coins = 1,      // Tiền xu - primary currency từ gameplay
        Gems = 2,       // Kim cương - premium currency từ IAP
        Keys = 3,       // Chìa khóa - special currency để mở cửa/chest
        Experience = 4  // Kinh nghiệm - progression currency
    }

    /// <summary>
    /// Data structure cho một currency transaction
    /// </summary>
    [System.Serializable]
    public struct CurrencyTransaction
    {
        public CurrencyType type;
        public long amount;
        public string reason;
        public DateTime timestamp;
        
        public CurrencyTransaction(CurrencyType type, long amount, string reason = "")
        {
            this.type = type;
            this.amount = amount;
            this.reason = reason;
            this.timestamp = DateTime.Now;
        }
        
        public bool IsValid => amount != 0 && type != CurrencyType.None;
    }

    /// <summary>
    /// Configuration data cho một loại currency
    /// </summary>
    [System.Serializable]
    public struct CurrencyConfig
    {
        [SerializeField] private CurrencyType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _color;
        [SerializeField] private long _maxAmount;
        [SerializeField] private long _startAmount;
        [SerializeField] private bool _canGoNegative;
        
        public CurrencyType Type => _type;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Color Color => _color;
        public long MaxAmount => _maxAmount;
        public long StartAmount => _startAmount;
        public bool CanGoNegative => _canGoNegative;
        
        public CurrencyConfig(CurrencyType type, string displayName, long maxAmount = long.MaxValue, 
                            long startAmount = 0, bool canGoNegative = false)
        {
            _type = type;
            _displayName = displayName;
            _icon = null;
            _color = Color.white;
            _maxAmount = maxAmount;
            _startAmount = startAmount;
            _canGoNegative = canGoNegative;
        }
        
        public static CurrencyConfig GetDefault(CurrencyType type)
        {
            switch (type)
            {
                case CurrencyType.Coins:
                    return new CurrencyConfig(type, "Coins", 999999999L, 0, false);
                case CurrencyType.Gems:
                    return new CurrencyConfig(type, "Gems", 999999L, 0, false);
                case CurrencyType.Keys:
                    return new CurrencyConfig(type, "Keys", 999L, 0, false);
                case CurrencyType.Experience:
                    return new CurrencyConfig(type, "XP", long.MaxValue, 0, false);
                default:
                    return new CurrencyConfig(type, type.ToString(), 0, 0, false);
            }
        }
    }

    /// <summary>
    /// Currency formatting utilities
    /// </summary>
    public static class CurrencyFormatter
    {
        /// <summary>
        /// Format currency amount with abbreviated notation (K, M, B)
        /// </summary>
        public static string FormatAmount(long amount, bool useAbbreviation = true)
        {
            if (!useAbbreviation || amount < 1000)
            {
                return amount.ToString("N0");
            }
            
            if (amount < 1000000)
            {
                return $"{(amount / 1000f):F1}K";
            }
            
            if (amount < 1000000000)
            {
                return $"{(amount / 1000000f):F1}M";
            }
            
            return $"{(amount / 1000000000f):F1}B";
        }
        
        /// <summary>
        /// Format currency transaction cho display
        /// </summary>
        public static string FormatTransaction(CurrencyTransaction transaction, bool showSign = true)
        {
            string sign = showSign && transaction.amount > 0 ? "+" : "";
            return $"{sign}{FormatAmount(transaction.amount)}";
        }
        
        /// <summary>
        /// Get color for currency amount (green for positive, red for negative)
        /// </summary>
        public static Color GetAmountColor(long amount)
        {
            if (amount > 0) return Color.green;
            if (amount < 0) return Color.red;
            return Color.white;
        }
    }
}
