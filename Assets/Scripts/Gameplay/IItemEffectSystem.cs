using System;
using System.Collections.Generic;
using EndlessRunner.Data;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface cho Item Effect System quản lý active items, stacking, và duration tracking.
    /// Supports Magnet, Multiplier (x2), Invisible, Life, và Manual items theo ROADMAP specs.
    /// </summary>
    public interface IItemEffectSystem
    {
        #region Properties
        
        /// <summary>Có item nào đang active không</summary>
        bool HasActiveItems { get; }
        
        /// <summary>Số lượng items đang active</summary>
        int ActiveItemCount { get; }
        
        /// <summary>Manual item slot hiện tại (null nếu empty)</summary>
        ItemDefinition ManualItem { get; }
        
        /// <summary>Manual item đang cooldown không</summary>
        bool IsManualCooldown { get; }
        
        /// <summary>Thời gian còn lại của manual cooldown</summary>
        float ManualCooldownRemaining { get; }
        
        /// <summary>Current multiplier value (1.0 = normal, 2.0 = x2, 4.0 = x4...)</summary>
        float CurrentMultiplier { get; }
        
        /// <summary>Magnet đang active không</summary>
        bool IsMagnetActive { get; }
        
        /// <summary>Invisible đang active không</summary>
        bool IsInvisibleActive { get; }
        
        #endregion

        #region Item Management Methods
        
        /// <summary>
        /// Activate một item (auto-activate hoặc manual).
        /// </summary>
        /// <param name="item">Item definition để activate</param>
        /// <param name="forceManual">Force vào manual slot thay vì auto-activate</param>
        /// <returns>True nếu activate thành công</returns>
        bool ActivateItem(ItemDefinition item, bool forceManual = false);
        
        /// <summary>
        /// Deactivate item theo ID.
        /// </summary>
        /// <param name="itemId">ID của item để deactivate</param>
        /// <returns>True nếu deactivate thành công</returns>
        bool DeactivateItem(string itemId);
        
        /// <summary>
        /// Deactivate tất cả items của một loại.
        /// </summary>
        /// <param name="itemType">Loại item để deactivate</param>
        /// <returns>Số lượng items đã deactivate</returns>
        int DeactivateItemsOfType(ItemType itemType);
        
        /// <summary>
        /// Clear tất cả active items (emergency reset).
        /// </summary>
        void ClearAllItems();
        
        /// <summary>
        /// Use manual item từ slot.
        /// </summary>
        /// <returns>True nếu use thành công</returns>
        bool UseManualItem();
        
        #endregion

        #region Query Methods
        
        /// <summary>
        /// Check xem item có đang active không.
        /// </summary>
        /// <param name="itemId">ID của item</param>
        /// <returns>True nếu active</returns>
        bool IsItemActive(string itemId);
        
        /// <summary>
        /// Check xem item type có đang active không.
        /// </summary>
        /// <param name="itemType">Loại item</param>
        /// <returns>True nếu có item loại này active</returns>
        bool IsItemTypeActive(ItemType itemType);
        
        /// <summary>
        /// Get thời gian còn lại của item.
        /// </summary>
        /// <param name="itemId">ID của item</param>
        /// <returns>Thời gian còn lại (s), -1 nếu không tìm thấy</returns>
        float GetRemainingTime(string itemId);
        
        /// <summary>
        /// Get thời gian còn lại của item type (item đầu tiên tìm thấy).
        /// </summary>
        /// <param name="itemType">Loại item</param>
        /// <returns>Thời gian còn lại (s), -1 nếu không tìm thấy</returns>
        float GetRemainingTimeByType(ItemType itemType);
        
        /// <summary>
        /// Get stack count của multiplier items.
        /// </summary>
        /// <returns>Stack count (1 = single x2, 2 = x4, 3 = x8...)</returns>
        int GetMultiplierStackCount();
        
        /// <summary>
        /// Get tất cả active items (read-only).
        /// </summary>
        /// <returns>List của active items</returns>
        IReadOnlyList<ActiveItemInfo> GetActiveItems();
        
        /// <summary>
        /// Get active items của một loại cụ thể.
        /// </summary>
        /// <param name="itemType">Loại item</param>
        /// <returns>List của active items với loại này</returns>
        IReadOnlyList<ActiveItemInfo> GetActiveItemsOfType(ItemType itemType);
        
        #endregion

        #region Events
        
        /// <summary>
        /// Event khi item được activate.
        /// Parameters: (itemDefinition, wasStacked)
        /// </summary>
        event Action<ItemDefinition, bool> OnItemActivated;
        
        /// <summary>
        /// Event khi item expire hoặc bị deactivate.
        /// Parameters: (itemDefinition, expired vs manual deactivation)
        /// </summary>
        event Action<ItemDefinition, bool> OnItemDeactivated;
        
        /// <summary>
        /// Event khi item stack với item hiện tại.
        /// Parameters: (itemDefinition, newStackCount, newValue)
        /// </summary>
        event Action<ItemDefinition, int, float> OnItemStacked;
        
        /// <summary>
        /// Event khi multiplier value thay đổi.
        /// Parameters: (oldMultiplier, newMultiplier)
        /// </summary>
        event Action<float, float> OnMultiplierChanged;
        
        /// <summary>
        /// Event khi magnet state thay đổi.
        /// Parameters: (isActive, radius, speed)
        /// </summary>
        event Action<bool, float, float> OnMagnetStateChanged;
        
        /// <summary>
        /// Event khi invisible state thay đổi.
        /// Parameters: (isActive, layerName)
        /// </summary>
        event Action<bool, string> OnInvisibleStateChanged;
        
        /// <summary>
        /// Event khi manual item được set/used/cleared.
        /// Parameters: (itemDefinition, action: "set"/"used"/"cleared")
        /// </summary>
        event Action<ItemDefinition, string> OnManualItemChanged;
        
        /// <summary>
        /// Event khi manual cooldown bắt đầu/kết thúc.
        /// Parameters: (isInCooldown, cooldownDuration)
        /// </summary>
        event Action<bool, float> OnManualCooldownChanged;
        
        #endregion
    }

    /// <summary>
    /// Read-only information về active item
    /// </summary>
    public struct ActiveItemInfo
    {
        public string ItemId { get; }
        public ItemDefinition Definition { get; }
        public float RemainingTime { get; }
        public float Progress { get; }
        public float EffectValue { get; }
        public int StackCount { get; }
        public DateTime ActivatedAt { get; }
        
        public ActiveItemInfo(string itemId, ItemDefinition definition, float remainingTime, 
                            float progress, float effectValue, int stackCount, DateTime activatedAt)
        {
            ItemId = itemId;
            Definition = definition;
            RemainingTime = remainingTime;
            Progress = progress;
            EffectValue = effectValue;
            StackCount = stackCount;
            ActivatedAt = activatedAt;
        }
    }
}
