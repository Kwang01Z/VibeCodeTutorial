using UnityEngine;
using EndlessRunner.Data;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Represents một item effect đang active với timer và stacking logic
    /// </summary>
    [System.Serializable]
    public class ActiveItemEffect
    {
        #region Fields

        [SerializeField] private ItemDefinition _itemDefinition;
        [SerializeField] private float _remainingTime;
        [SerializeField] private int _stackCount;
        [SerializeField] private float _currentEffectValue;
        
        // Runtime tracking
        private float _originalDuration;
        private float _baseEffectValue;
        private bool _isPermanent;

        #endregion

        #region Properties

        public ItemDefinition ItemDefinition => _itemDefinition;
        public float RemainingTime => _remainingTime;
        public int StackCount => _stackCount;
        public float CurrentEffectValue => _currentEffectValue;
        public bool IsExpired => !_isPermanent && _remainingTime <= 0f;
        public bool IsPermanent => _isPermanent;
        public float Progress => _isPermanent ? 1f : 1f - (_remainingTime / _originalDuration);

        #endregion

        #region Constructor

        public ActiveItemEffect(ItemDefinition itemDefinition)
        {
            _itemDefinition = itemDefinition;
            _originalDuration = itemDefinition.Duration;
            _baseEffectValue = itemDefinition.EffectValue;
            _remainingTime = _originalDuration;
            _stackCount = 1;
            _currentEffectValue = _baseEffectValue;
            _isPermanent = itemDefinition.Type == ItemType.Life; // Life effects are permanent
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates timer và returns true nếu effect vẫn active
        /// </summary>
        public bool UpdateTimer(float deltaTime)
        {
            if (_isPermanent) return true;

            _remainingTime -= deltaTime;
            return _remainingTime > 0f;
        }

        /// <summary>
        /// Applies stacking rule khi nhận thêm effect cùng loại
        /// </summary>
        public void ApplyStacking(ItemDefinition newItem)
        {
            if (!_itemDefinition.CanStack || _itemDefinition.ItemId != newItem.ItemId)
                return;

            switch (_itemDefinition.StackingRule)
            {
                case ItemStackingRule.Replace:
                    ApplyReplaceStacking(newItem);
                    break;
                    
                case ItemStackingRule.Add:
                    ApplyAdditiveStacking(newItem);
                    break;
                    
                case ItemStackingRule.Multiply:
                    ApplyMultiplicativeStacking(newItem);
                    break;
            }
        }

        /// <summary>
        /// Gets formatted display string cho UI
        /// </summary>
        public string GetDisplayText()
        {
            if (_isPermanent)
                return _stackCount > 1 ? $"x{_stackCount}" : "";

            if (_remainingTime < 60f)
                return $"{_remainingTime:F0}s";
            else
                return $"{_remainingTime / 60f:F1}m";
        }

        /// <summary>
        /// Gets effect intensity based on item type và current value
        /// </summary>
        public float GetEffectIntensity()
        {
            return _itemDefinition.Type switch
            {
                ItemType.Magnet => _currentEffectValue / 5f, // Base radius 5
                ItemType.Multiplier => (_currentEffectValue - 1f) / 2f, // Base multiplier 2x
                ItemType.Invisible => 1f,
                ItemType.Life => _stackCount / 3f, // Up to 3 lives
                _ => 0f
            };
        }

        #endregion

        #region Private Methods

        private void ApplyReplaceStacking(ItemDefinition newItem)
        {
            // Reset duration and effect to new values
            _remainingTime = newItem.Duration;
            _originalDuration = newItem.Duration;
            _currentEffectValue = newItem.EffectValue;
            _stackCount = 1; // Reset stack count
        }

        private void ApplyAdditiveStacking(ItemDefinition newItem)
        {
            // Add duration, increment stack count
            _remainingTime += newItem.Duration;
            _stackCount++;
            
            // Effect value remains the same for additive stacking
            // (duration is extended, not effect strength)
        }

        private void ApplyMultiplicativeStacking(ItemDefinition newItem)
        {
            // Increment stack count
            _stackCount++;
            
            // Multiply effect value based on stacking
            // For diminishing returns: new_value = base_value * (1 + (stack-1) * 0.5)
            float stackMultiplier = 1f + (_stackCount - 1) * 0.5f;
            _currentEffectValue = _baseEffectValue * stackMultiplier;
            
            // Reset duration to new item's duration
            _remainingTime = newItem.Duration;
            _originalDuration = newItem.Duration;
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            return $"ActiveEffect[{_itemDefinition.DisplayName}]: {_remainingTime:F1}s, x{_stackCount}, Value:{_currentEffectValue:F2}";
        }

        #endregion
    }
}
