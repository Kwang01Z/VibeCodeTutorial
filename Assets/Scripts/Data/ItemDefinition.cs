using UnityEngine;
using System;

namespace EndlessRunner.Data
{
    /// <summary>
    /// ScriptableObject định nghĩa cấu hình cho các item trong game (Magnet, Multiplier, Invisible, Life)
    /// Hỗ trợ validation, stacking rules, và editor integration
    /// </summary>
    [CreateAssetMenu(menuName = "EndlessRunner/Item Definition", fileName = "New Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _itemId = "item_magnet";
        [SerializeField] private string _displayName = "Magnet";
        [SerializeField] private ItemType _type = ItemType.Magnet;
        [SerializeField, TextArea(2, 4)] private string _description = "Attracts nearby coins automatically";
        
        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _pickupPrefab;
        [SerializeField] private Color _effectColor = Color.yellow;
        
        [Header("Gameplay Properties")]
        [SerializeField, Range(0.1f, 60f)] private float _duration = 10f;
        [SerializeField] private float _effectValue = 1f; // Multiplier value, magnet radius, etc.
        [SerializeField] private bool _canStack = false;
        [SerializeField] private ItemStackingRule _stackingRule = ItemStackingRule.Replace;
        [SerializeField, Range(0, 100)] private int _rarity = 1; // Higher = rarer
        
        [Header("Unlock Requirements")]
        [SerializeField] private float _unlockDistance = 0f;
        [SerializeField] private bool _isEnabled = true;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _pickupSound;
        [SerializeField] private AudioClip _activateSound;
        [SerializeField] private AudioClip _expireSound;
        
        [Header("Manual Item Settings")]
        [SerializeField, Range(0.1f, 30f)] private float _manualCooldown = 5f;
        
        [Header("Currency Settings")]
        [SerializeField] private EndlessRunner.Currency.CurrencyType _currencyType = EndlessRunner.Currency.CurrencyType.Coins;
        [SerializeField] private long _currencyAmount = 100;

        // Public Properties
        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public ItemType Type => _type;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject PickupPrefab => _pickupPrefab;
        public Color EffectColor => _effectColor;
        public float Duration => _duration;
        public float EffectValue => _effectValue;
        public bool CanStack => _canStack;
        public ItemStackingRule StackingRule => _stackingRule;
        public int Rarity => _rarity;
        public float UnlockDistance => _unlockDistance;
        public bool IsEnabled => _isEnabled;
        public AudioClip PickupSound => _pickupSound;
        public AudioClip ActivateSound => _activateSound;
        public AudioClip ExpireSound => _expireSound;
        public float ManualCooldown => _manualCooldown;
        public EndlessRunner.Currency.CurrencyType CurrencyType => _currencyType;
        public long CurrencyAmount => _currencyAmount;

        /// <summary>
        /// Validate item configuration, gọi từ Editor
        /// </summary>
        public bool ValidateConfiguration(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Check required fields
            if (string.IsNullOrEmpty(_itemId))
            {
                errorMessage = "Item ID không được để trống";
                return false;
            }

            if (string.IsNullOrEmpty(_displayName))
            {
                errorMessage = "Display Name không được để trống";
                return false;
            }

            // Validate duration
            if (_duration <= 0f && _type != ItemType.Life)
            {
                errorMessage = "Duration phải > 0 cho items có thời gian";
                return false;
            }

            // Validate effect value
            if (_effectValue <= 0f)
            {
                errorMessage = "Effect Value phải > 0";
                return false;
            }

            // Type-specific validation
            switch (_type)
            {
                case ItemType.Magnet:
                    if (_effectValue < 0.5f || _effectValue > 10f)
                    {
                        errorMessage = "Magnet radius nên từ 0.5 đến 10 units";
                        return false;
                    }
                    break;

                case ItemType.Multiplier:
                    if (_effectValue < 1.1f || _effectValue > 10f)
                    {
                        errorMessage = "Multiplier value nên từ 1.1x đến 10x";
                        return false;
                    }
                    break;

                case ItemType.Life:
                    if (_effectValue < 1f || _effectValue != Mathf.Floor(_effectValue))
                    {
                        errorMessage = "Life value phải là số nguyên >= 1";
                        return false;
                    }
                    break;
            }

            // Validate stacking rules
            if (_canStack && _stackingRule == ItemStackingRule.Multiply && _type != ItemType.Multiplier)
            {
                errorMessage = "Multiply stacking chỉ dành cho Multiplier items";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check xem item có thể unlock tại distance hiện tại không
        /// </summary>
        public bool IsUnlockedAt(float currentDistance)
        {
            return _isEnabled && currentDistance >= _unlockDistance;
        }

        /// <summary>
        /// Get drop weight dựa trên rarity và distance
        /// </summary>
        public float GetDropWeight(float currentDistance)
        {
            if (!IsUnlockedAt(currentDistance))
                return 0f;

            // Rarity càng cao thì drop weight càng thấp
            return Mathf.Max(0.1f, 1f / (1f + _rarity * 0.1f));
        }

        /// <summary>
        /// Tạo ItemDefinition mặc định cho testing
        /// </summary>
        public static ItemDefinition CreateDefault(ItemType type)
        {
            var item = CreateInstance<ItemDefinition>();
            
            switch (type)
            {
                case ItemType.Magnet:
                    item._itemId = "item_magnet";
                    item._displayName = "Magnet";
                    item._duration = 10f;
                    item._effectValue = 2f; // 2 unit radius
                    item._canStack = false;
                    item._stackingRule = ItemStackingRule.Replace;
                    item._effectColor = Color.yellow;
                    item._description = "Attracts nearby coins automatically";
                    break;

                case ItemType.Multiplier:
                    item._itemId = "item_multiplier";
                    item._displayName = "2x Multiplier";
                    item._duration = 12f;
                    item._effectValue = 2f;
                    item._canStack = true;
                    item._stackingRule = ItemStackingRule.Multiply;
                    item._effectColor = Color.green;
                    item._description = "Double your coin and score gains";
                    break;

                case ItemType.Invisible:
                    item._itemId = "item_invisible";
                    item._displayName = "Ghost Mode";
                    item._duration = 4f;
                    item._effectValue = 1f;
                    item._canStack = false;
                    item._stackingRule = ItemStackingRule.Add;
                    item._effectColor = new Color(1f, 1f, 1f, 0.5f);
                    item._description = "Pass through obstacles safely";
                    break;

                case ItemType.Life:
                    item._itemId = "item_life";
                    item._displayName = "Extra Life";
                    item._duration = 0f; // Instant
                    item._effectValue = 1f; // +1 health
                    item._canStack = false;
                    item._stackingRule = ItemStackingRule.Replace;
                    item._effectColor = Color.red;
                    item._description = "Restore one health point";
                    break;
            }

            return item;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility - tạo test items
        /// </summary>
        [ContextMenu("Create Test Items")]
        private void CreateTestItems()
        {
            foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
            {
                if (type == ItemType.Manual) continue;
                
                var item = CreateDefault(type);
                string assetPath = $"Assets/Configs/Items/Test_{type}_Item.asset";
                UnityEditor.AssetDatabase.CreateAsset(item, assetPath);
            }
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Created test items in Assets/Configs/Items/");
        }
#endif

        private void OnValidate()
        {
            // Auto-correct invalid values
            _duration = Mathf.Max(0f, _duration);
            _effectValue = Mathf.Max(0f, _effectValue);
            _rarity = Mathf.Max(0, _rarity);
            _unlockDistance = Mathf.Max(0f, _unlockDistance);

            // Auto-generate itemId from display name nếu empty
            if (string.IsNullOrEmpty(_itemId) && !string.IsNullOrEmpty(_displayName))
            {
                _itemId = "item_" + _displayName.ToLower().Replace(' ', '_');
            }
        }
    }

    /// <summary>
    /// Item types available trong endless runner
    /// </summary>
    public enum ItemType
    {
        Magnet,      // Hút coins trong radius
        Multiplier,  // Nhân điểm và coins
        Invisible,   // Xuyên obstacles
        Life,        // Thêm health
        Manual,      // Item cần activate thủ công
        Currency     // Tiền tệ (coins, gems, keys)
    }

    /// <summary>
    /// Rules cho stacking multiple items cùng type
    /// </summary>
    public enum ItemStackingRule
    {
        Replace,     // Item mới thay thế item cũ
        Add,         // Cộng duration
        Multiply     // Nhân effect value (chỉ cho Multiplier)
    }
}
