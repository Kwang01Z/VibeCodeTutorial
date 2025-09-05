using UnityEngine;
using System;

namespace EndlessRunner.Data
{
    /// <summary>
    /// ScriptableObject định nghĩa cấu hình cho các loại tiền tệ trong game
    /// Hỗ trợ session currency (Xương Cá - mất khi kết thúc run) và persistent currency (Bẫy Chuột - lưu giữ)
    /// </summary>
    [CreateAssetMenu(menuName = "EndlessRunner/Currency Definition", fileName = "New Currency Definition")]
    public class CurrencyDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _currencyId = "fish_bone";
        [SerializeField] private string _displayName = "Xương Cá";
        [SerializeField] private CurrencyType _type = CurrencyType.Session;
        [SerializeField, TextArea(2, 3)] private string _description = "Tiền tệ kiếm được trong run, mất khi kết thúc";

        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _pickupPrefab;
        [SerializeField] private Color _displayColor = Color.yellow;

        [Header("Gameplay Properties")]
        [SerializeField] private int _baseValue = 1; // Giá trị mặc định khi nhặt
        [SerializeField] private int _maxAmount = 999999; // Số lượng tối đa có thể sở hữu
        [SerializeField] private bool _showInHUD = true; // Hiển thị trong HUD hay không
        [SerializeField, Range(0, 100)] private int _dropChance = 80; // % chance rơi trong run

        [Header("Conversion & Economics")]
        [SerializeField] private bool _canConvertTo = false;
        [SerializeField] private CurrencyDefinition _convertTarget; // Currency có thể convert sang
        [SerializeField] private float _conversionRate = 10f; // Tỷ lệ convert (10 Xương Cá = 1 Bẫy Chuột)

        [Header("Audio")]
        [SerializeField] private AudioClip _pickupSound;
        [SerializeField] private AudioClip _spendSound;
        [SerializeField] private AudioClip _gainSound;

        // Public Properties
        public string CurrencyId => _currencyId;
        public string DisplayName => _displayName;
        public CurrencyType Type => _type;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject PickupPrefab => _pickupPrefab;
        public Color DisplayColor => _displayColor;
        public int BaseValue => _baseValue;
        public int MaxAmount => _maxAmount;
        public bool ShowInHUD => _showInHUD;
        public int DropChance => _dropChance;
        public bool CanConvertTo => _canConvertTo;
        public CurrencyDefinition ConvertTarget => _convertTarget;
        public float ConversionRate => _conversionRate;
        public AudioClip PickupSound => _pickupSound;
        public AudioClip SpendSound => _spendSound;
        public AudioClip GainSound => _gainSound;

        /// <summary>
        /// Loại tiền tệ trong game
        /// </summary>
        public enum CurrencyType
        {
            Session,    // Mất khi kết thúc run (Xương Cá)
            Persistent  // Lưu giữ vĩnh viễn (Bẫy Chuột)
        }

        /// <summary>
        /// Validate currency configuration
        /// </summary>
        public bool ValidateConfiguration(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Check required fields
            if (string.IsNullOrEmpty(_currencyId))
            {
                errorMessage = "Currency ID không được để trống";
                return false;
            }

            if (string.IsNullOrEmpty(_displayName))
            {
                errorMessage = "Display Name không được để trống";
                return false;
            }

            // Validate numeric values
            if (_baseValue <= 0)
            {
                errorMessage = "Base Value phải > 0";
                return false;
            }

            if (_maxAmount <= 0)
            {
                errorMessage = "Max Amount phải > 0";
                return false;
            }

            if (_maxAmount < _baseValue)
            {
                errorMessage = "Max Amount phải >= Base Value";
                return false;
            }

            // Validate conversion
            if (_canConvertTo)
            {
                if (_convertTarget == null)
                {
                    errorMessage = "Convert Target không được null khi Can Convert To = true";
                    return false;
                }

                if (_conversionRate <= 0f)
                {
                    errorMessage = "Conversion Rate phải > 0";
                    return false;
                }

                if (_convertTarget == this)
                {
                    errorMessage = "Không thể convert sang chính nó";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate pickup value với multiplier
        /// </summary>
        public int CalculatePickupValue(float multiplier = 1f)
        {
            return Mathf.Max(1, Mathf.RoundToInt(_baseValue * multiplier));
        }

        /// <summary>
        /// Check xem amount có vượt max không
        /// </summary>
        public bool WouldExceedMax(int currentAmount, int addAmount)
        {
            return currentAmount + addAmount > _maxAmount;
        }

        /// <summary>
        /// Clamp amount trong giới hạn cho phép
        /// </summary>
        public int ClampAmount(int amount)
        {
            return Mathf.Clamp(amount, 0, _maxAmount);
        }

        /// <summary>
        /// Calculate conversion result
        /// </summary>
        public int CalculateConversion(int sourceAmount)
        {
            if (!_canConvertTo || _conversionRate <= 0f)
                return 0;

            return Mathf.FloorToInt(sourceAmount / _conversionRate);
        }

        /// <summary>
        /// Get conversion cost cho target amount
        /// </summary>
        public int GetConversionCost(int targetAmount)
        {
            if (!_canConvertTo || _conversionRate <= 0f)
                return int.MaxValue;

            return Mathf.CeilToInt(targetAmount * _conversionRate);
        }

        /// <summary>
        /// Format currency amount cho UI display
        /// </summary>
        public string FormatAmount(int amount)
        {
            if (amount < 1000)
                return amount.ToString();
            else if (amount < 1000000)
                return $"{amount / 1000f:F1}K";
            else
                return $"{amount / 1000000f:F1}M";
        }

        /// <summary>
        /// Tạo CurrencyDefinition mặc định
        /// </summary>
        public static CurrencyDefinition CreateDefault(CurrencyType type)
        {
            var currency = CreateInstance<CurrencyDefinition>();

            switch (type)
            {
                case CurrencyType.Session:
                    currency._currencyId = "fish_bone";
                    currency._displayName = "Xương Cá";
                    currency._type = CurrencyType.Session;
                    currency._description = "Tiền tệ kiếm được trong run, mất khi kết thúc";
                    currency._baseValue = 1;
                    currency._maxAmount = 999999;
                    currency._showInHUD = true;
                    currency._dropChance = 80;
                    currency._displayColor = Color.yellow;
                    currency._canConvertTo = true;
                    currency._conversionRate = 10f; // 10 Xương Cá = 1 Bẫy Chuột
                    break;

                case CurrencyType.Persistent:
                    currency._currencyId = "mouse_trap";
                    currency._displayName = "Bẫy Chuột";
                    currency._type = CurrencyType.Persistent;
                    currency._description = "Tiền tệ cao cấp, lưu giữ vĩnh viễn để mua items và upgrades";
                    currency._baseValue = 1;
                    currency._maxAmount = 99999;
                    currency._showInHUD = true;
                    currency._dropChance = 5;
                    currency._displayColor = Color.cyan;
                    currency._canConvertTo = false;
                    break;
            }

            return currency;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility - tạo default currencies
        /// </summary>
        [ContextMenu("Create Default Currencies")]
        private void CreateDefaultCurrencies()
        {
            // Tạo Xương Cá
            var fishBone = CreateDefault(CurrencyType.Session);
            string fishBonePath = "Assets/Configs/Currencies/FishBone_Currency.asset";
            UnityEditor.AssetDatabase.CreateAsset(fishBone, fishBonePath);

            // Tạo Bẫy Chuột
            var mouseTrap = CreateDefault(CurrencyType.Persistent);
            string mouseTrapPath = "Assets/Configs/Currencies/MouseTrap_Currency.asset";
            UnityEditor.AssetDatabase.CreateAsset(mouseTrap, mouseTrapPath);

            // Set conversion target cho Xương Cá
            fishBone._convertTarget = mouseTrap;

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Created default currencies in Assets/Configs/Currencies/");
        }
#endif

        private void OnValidate()
        {
            // Auto-correct invalid values
            _baseValue = Mathf.Max(1, _baseValue);
            _maxAmount = Mathf.Max(_baseValue, _maxAmount);
            _dropChance = Mathf.Clamp(_dropChance, 0, 100);
            _conversionRate = Mathf.Max(0.1f, _conversionRate);

            // Auto-generate currencyId từ display name nếu empty
            if (string.IsNullOrEmpty(_currencyId) && !string.IsNullOrEmpty(_displayName))
            {
                _currencyId = _displayName.ToLower()
                    .Replace(' ', '_')
                    .Replace("ă", "a")
                    .Replace("â", "a")
                    .Replace("ê", "e")
                    .Replace("ô", "o")
                    .Replace("ư", "u");
            }

            // Reset conversion nếu type là Persistent
            if (_type == CurrencyType.Persistent)
            {
                _canConvertTo = false;
                _convertTarget = null;
            }
        }
    }
}
