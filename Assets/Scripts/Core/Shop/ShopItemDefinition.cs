using System;
using UnityEngine;

namespace EndlessRunner.Core.Shop
{
    /// <summary>
    /// ShopItemDefinition - ScriptableObject định nghĩa item có thể mua trong store
    /// Hỗ trợ các loại: PowerUp, Character, Accessory, Theme
    /// </summary>
    [CreateAssetMenu(fileName = "ShopItem_", menuName = "EndlessRunner/Shop/Shop Item Definition")]
    public class ShopItemDefinition : ScriptableObject
    {
        [System.Serializable]
        public enum ShopItemType
        {
            PowerUp,
            Character, 
            Accessory,
            Theme
        }

        [System.Serializable]
        public enum CurrencyType
        {
            BoneFish,      // Xương Cá (soft currency)
            MouseTrap,     // Bẫy Chuột (premium currency)
            RealMoney      // IAP
        }

        [System.Serializable]
        public enum PurchaseType
        {
            OneTime,       // Mua 1 lần (Character, Theme)
            Consumable,    // Tiêu hao (PowerUp)
            Permanent      // Vĩnh viễn (Accessory)
        }

        #region Basic Info
        
        [Header("Basic Information")]
        [SerializeField]
        private string _itemId = "";
        
        [SerializeField]
        private string _displayName = "";
        
        [SerializeField]
        [TextArea(3, 5)]
        private string _description = "";
        
        [SerializeField]
        private ShopItemType _itemType = ShopItemType.PowerUp;
        
        [SerializeField]
        private Sprite _icon;
        
        [SerializeField]
        private GameObject _previewPrefab;
        
        #endregion
        
        #region Pricing
        
        [Header("Pricing")]
        [SerializeField]
        private CurrencyType _currencyType = CurrencyType.BoneFish;
        
        [SerializeField]
        private long _price = 100;
        
        [SerializeField]
        private PurchaseType _purchaseType = PurchaseType.OneTime;
        
        [SerializeField]
        private bool _isIAPItem = false;
        
        [SerializeField]
        private string _iapProductId = "";
        
        #endregion
        
        #region Availability
        
        [Header("Availability")]
        [SerializeField]
        private bool _isAvailableByDefault = true;
        
        [SerializeField]
        private int _requiredLevel = 1;
        
        [SerializeField]
        private long _requiredDistance = 0;
        
        [SerializeField]
        private string[] _requiredAchievements = new string[0];
        
        [SerializeField]
        private bool _isLimitedTime = false;
        
        [SerializeField]
        private DateTime _availableFrom = DateTime.MinValue;
        
        [SerializeField]
        private DateTime _availableUntil = DateTime.MaxValue;
        
        #endregion
        
        #region PowerUp Specific (chỉ cho PowerUp items)
        
        [Header("PowerUp Settings (PowerUp only)")]
        [SerializeField]
        private int _powerUpQuantity = 1;
        
        [SerializeField]
        private float _powerUpDuration = 10f;
        
        [SerializeField]
        private string _powerUpEffectId = "";
        
        #endregion
        
        #region Character/Accessory Stats
        
        [Header("Character/Accessory Stats")]
        [SerializeField]
        private float _coinBonusMultiplier = 1f;
        
        [SerializeField]
        private float _powerUpDurationBonus = 0f;
        
        [SerializeField]
        private int _startingLives = 0;
        
        [SerializeField]
        private bool _hasImmuneFirstHit = false;
        
        [SerializeField]
        private float _magnetRadiusBonus = 0f;
        
        #endregion
        
        #region Properties
        
        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ShopItemType ItemType => _itemType;
        public Sprite Icon => _icon;
        public GameObject PreviewPrefab => _previewPrefab;
        
        public CurrencyType Currency => _currencyType;
        public long Price => _price;
        public PurchaseType Purchase => _purchaseType;
        public bool IsIAPItem => _isIAPItem;
        public string IAPProductId => _iapProductId;
        
        public bool IsAvailableByDefault => _isAvailableByDefault;
        public int RequiredLevel => _requiredLevel;
        public long RequiredDistance => _requiredDistance;
        public string[] RequiredAchievements => _requiredAchievements;
        public bool IsLimitedTime => _isLimitedTime;
        public DateTime AvailableFrom => _availableFrom;
        public DateTime AvailableUntil => _availableUntil;
        
        public int PowerUpQuantity => _powerUpQuantity;
        public float PowerUpDuration => _powerUpDuration;
        public string PowerUpEffectId => _powerUpEffectId;
        
        public float CoinBonusMultiplier => _coinBonusMultiplier;
        public float PowerUpDurationBonus => _powerUpDurationBonus;
        public int StartingLives => _startingLives;
        public bool HasImmuneFirstHit => _hasImmuneFirstHit;
        public float MagnetRadiusBonus => _magnetRadiusBonus;
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Auto-generate ItemId nếu trống
            if (string.IsNullOrEmpty(_itemId) && !string.IsNullOrEmpty(name))
            {
                _itemId = name.Replace(" ", "_").ToLowerInvariant();
            }
            
            // Validation rules
            if (_price <= 0)
            {
                _price = 1;
            }
            
            if (_powerUpQuantity <= 0 && _itemType == ShopItemType.PowerUp)
            {
                _powerUpQuantity = 1;
            }
            
            if (_powerUpDuration <= 0f && _itemType == ShopItemType.PowerUp)
            {
                _powerUpDuration = 5f;
            }
            
            // IAP validation
            if (_isIAPItem && string.IsNullOrEmpty(_iapProductId))
            {
                Debug.LogWarning($"[ShopItemDefinition] IAP item '{name}' missing Product ID");
            }
            
            // Currency type validation cho IAP
            if (_isIAPItem && _currencyType != CurrencyType.RealMoney)
            {
                Debug.LogWarning($"[ShopItemDefinition] IAP item '{name}' should use RealMoney currency");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Check nếu item có sẵn cho người chơi
        /// </summary>
        public bool IsAvailableForPlayer(int playerLevel, long totalDistance, string[] unlockedAchievements)
        {
            // Default availability check
            if (!_isAvailableByDefault)
                return false;
            
            // Level requirement
            if (playerLevel < _requiredLevel)
                return false;
            
            // Distance requirement  
            if (totalDistance < _requiredDistance)
                return false;
            
            // Achievement requirements
            if (_requiredAchievements.Length > 0)
            {
                foreach (string requiredAchievement in _requiredAchievements)
                {
                    bool hasAchievement = false;
                    foreach (string unlockedAchievement in unlockedAchievements)
                    {
                        if (unlockedAchievement == requiredAchievement)
                        {
                            hasAchievement = true;
                            break;
                        }
                    }
                    
                    if (!hasAchievement)
                        return false;
                }
            }
            
            // Time-limited availability
            if (_isLimitedTime)
            {
                DateTime now = DateTime.Now;
                if (now < _availableFrom || now > _availableUntil)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get display price string với currency symbol
        /// </summary>
        public string GetDisplayPriceString()
        {
            switch (_currencyType)
            {
                case CurrencyType.BoneFish:
                    return $"{_price:N0} 🐟";
                case CurrencyType.MouseTrap:
                    return $"{_price} 🪤";
                case CurrencyType.RealMoney:
                    return _isIAPItem ? GetIAPPriceString() : $"${_price:F2}";
                default:
                    return _price.ToString("N0");
            }
        }
        
        /// <summary>
        /// Get IAP price string (sẽ được override bởi IAP service)
        /// </summary>
        private string GetIAPPriceString()
        {
            // TODO: Integration với IAP service để lấy localized price
            return $"${_price:F2}";
        }
        
        /// <summary>
        /// Check if item can be purchased multiple times
        /// </summary>
        public bool CanPurchaseMultipleTimes()
        {
            return _purchaseType == PurchaseType.Consumable;
        }
        
        /// <summary>
        /// Get item rarity color (based on price ranges)
        /// </summary>
        public Color GetRarityColor()
        {
            if (_currencyType == CurrencyType.RealMoney)
                return new Color(1f, 0.84f, 0f, 1f); // Gold
            
            switch (_itemType)
            {
                case ShopItemType.PowerUp:
                    if (_price <= 100) return Color.white;       // Common
                    if (_price <= 500) return Color.green;       // Uncommon  
                    return Color.blue;                           // Rare
                    
                case ShopItemType.Character:
                    if (_price <= 1000) return Color.green;      // Uncommon
                    if (_price <= 5000) return Color.blue;       // Rare
                    return new Color(0.64f, 0.21f, 0.93f, 1f);  // Epic (purple)
                    
                case ShopItemType.Accessory:
                    if (_price <= 2000) return Color.green;      // Uncommon
                    if (_price <= 8000) return Color.blue;       // Rare  
                    return new Color(0.64f, 0.21f, 0.93f, 1f);  // Epic
                    
                case ShopItemType.Theme:
                    if (_price <= 3000) return Color.blue;       // Rare
                    return new Color(0.64f, 0.21f, 0.93f, 1f);  // Epic
                    
                default:
                    return Color.white;
            }
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        
        [ContextMenu("Generate Sample PowerUp")]
        private void GenerateSamplePowerUp()
        {
            _itemType = ShopItemType.PowerUp;
            _displayName = "Speed Boost";
            _description = "Tăng tốc độ chạy trong 10 giây";
            _currencyType = CurrencyType.BoneFish;
            _price = 250;
            _purchaseType = PurchaseType.Consumable;
            _powerUpQuantity = 3;
            _powerUpDuration = 10f;
            _powerUpEffectId = "speed_boost";
        }
        
        [ContextMenu("Generate Sample Character")]
        private void GenerateSampleCharacter()
        {
            _itemType = ShopItemType.Character;
            _displayName = "Ninja Cat";
            _description = "Mèo ninja với khả năng miễn nhiễm hit đầu tiên";
            _currencyType = CurrencyType.BoneFish;
            _price = 5000;
            _purchaseType = PurchaseType.OneTime;
            _coinBonusMultiplier = 1.2f;
            _hasImmuneFirstHit = true;
            _startingLives = 1;
        }
        
        [ContextMenu("Generate Sample IAP")]
        private void GenerateSampleIAP()
        {
            _itemType = ShopItemType.PowerUp;
            _displayName = "Power Pack";
            _description = "Gói power-up premium";
            _currencyType = CurrencyType.RealMoney;
            _price = 299; // cents
            _purchaseType = PurchaseType.Consumable;
            _isIAPItem = true;
            _iapProductId = "com.endlessrunner.powerpack";
        }
        
        #endif
        
        #endregion
    }
}
