using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using EndlessRunner.Currency;

namespace EndlessRunner.Core.Shop
{
    /// <summary>
    /// ShopManager - Quản lý toàn bộ shop system, mua bán items và IAP integration
    /// Phase 4 component cho meta-game framework
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        #region Events
        
        /// <summary>
        /// Item purchased successfully - (itemId, quantity, totalPrice)
        /// </summary>
        public static event Action<string, int, long> OnItemPurchased;
        
        /// <summary>
        /// Purchase failed - (itemId, reason)
        /// </summary>
        public static event Action<string, string> OnPurchaseFailed;
        
        /// <summary>
        /// IAP purchase started - (itemId, iapProductId)
        /// </summary>
        public static event Action<string, string> OnIAPPurchaseStarted;
        
        /// <summary>
        /// IAP purchase completed - (itemId, iapProductId, success)
        /// </summary>
        public static event Action<string, string, bool> OnIAPPurchaseCompleted;
        
        /// <summary>
        /// Shop data refreshed - (availableItems count)
        /// </summary>
        public static event Action<int> OnShopDataRefreshed;
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Shop Configuration")]
        [SerializeField]
        [Tooltip("All shop items definitions")]
        private ShopItemDefinition[] _allShopItems = new ShopItemDefinition[0];
        
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _enableDebugLog = true;
        
        [Header("IAP Settings")]
        [SerializeField]
        [Tooltip("Enable IAP purchases")]
        private bool _iapEnabled = true;
        
        [SerializeField]
        [Tooltip("IAP purchase timeout in seconds")]
        private float _iapTimeoutSeconds = 30f;
        
        [Header("Discount System")]
        [SerializeField]
        [Tooltip("Enable discount system")]
        private bool _discountEnabled = true;
        
        [SerializeField]
        [Tooltip("Max discount percentage (0-1)")]
        private float _maxDiscountPercent = 0.5f;
        
        #endregion
        
        #region Private Fields
        
        // Singleton instance
        private static ShopManager _instance;
        public static ShopManager Instance => _instance;
        
        // Shop data
        private Dictionary<string, ShopItemDefinition> _itemsById;
        private Dictionary<string, int> _ownedItems; // itemId -> quantity/owned
        private Dictionary<string, float> _itemDiscounts; // itemId -> discount percent (0-1)
        
        // Dependencies
        private ICurrencySystem _currencyManager;
        
        // State tracking
        private bool _isInitialized = false;
        private bool _isPurchasing = false;
        private CancellationTokenSource _iapCancellationTokenSource;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Check if shop system is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Check if currently making a purchase
        /// </summary>
        public bool IsPurchasing => _isPurchasing;
        
        /// <summary>
        /// Get all available shop items
        /// </summary>
        public ShopItemDefinition[] AllShopItems => _allShopItems;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LogDebug("[ShopManager] Initialized");
            }
            else
            {
                LogDebug("[ShopManager] Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeAsync().Forget();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Cancel any ongoing IAP operations
            _iapCancellationTokenSource?.Cancel();
            _iapCancellationTokenSource?.Dispose();
        }
        
        #endregion
        
        #region Initialization
        
        private async UniTask InitializeAsync()
        {
            try
            {
                LogDebug("[ShopManager] Initializing...");
                
                // Get dependencies
                var currencyManagerComponent = FindObjectOfType<CurrencyManager>();
                _currencyManager = currencyManagerComponent;
                if (_currencyManager == null)
                {
                    Debug.LogError("[ShopManager] CurrencyManager not found!");
                    return;
                }
                
                // Initialize data structures
                _itemsById = new Dictionary<string, ShopItemDefinition>();
                _ownedItems = new Dictionary<string, int>();
                _itemDiscounts = new Dictionary<string, float>();
                
                // Load shop items
                LoadShopItems();
                
                // Load owned items from save system
                await LoadOwnedItemsAsync();
                
                // Initialize IAP system
                if (_iapEnabled)
                {
                    await InitializeIAPAsync();
                }
                
                // Generate random discounts
                if (_discountEnabled)
                {
                    GenerateRandomDiscounts();
                }
                
                _isInitialized = true;
                LogDebug($"[ShopManager] Initialized successfully with {_allShopItems.Length} items");
                
                // Notify listeners
                OnShopDataRefreshed?.Invoke(_allShopItems.Length);
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopManager] Initialization failed: {ex.Message}");
            }
        }
        
        private void LoadShopItems()
        {
            _itemsById.Clear();
            
            foreach (var item in _allShopItems)
            {
                if (item == null)
                {
                    Debug.LogWarning("[ShopManager] Null shop item found in array");
                    continue;
                }
                
                if (string.IsNullOrEmpty(item.ItemId))
                {
                    Debug.LogWarning($"[ShopManager] Shop item '{item.name}' has empty ItemId");
                    continue;
                }
                
                if (_itemsById.ContainsKey(item.ItemId))
                {
                    Debug.LogWarning($"[ShopManager] Duplicate item ID: {item.ItemId}");
                    continue;
                }
                
                _itemsById.Add(item.ItemId, item);
                LogDebug($"[ShopManager] Loaded item: {item.ItemId} ({item.DisplayName})");
            }
        }
        
        private async UniTask LoadOwnedItemsAsync()
        {
            // TODO: Integration với Save System
            // Tạm thời dùng dummy data
            _ownedItems.Clear();
            
            // Dummy owned items for testing
            _ownedItems["magnet_powerup"] = 5;
            _ownedItems["x2_powerup"] = 3;
            _ownedItems["ninja_cat"] = 1;
            
            await UniTask.Yield();
            LogDebug($"[ShopManager] Loaded {_ownedItems.Count} owned items");
        }
        
        private async UniTask InitializeIAPAsync()
        {
            // TODO: Integration với Unity IAP
            LogDebug("[ShopManager] IAP initialization (stub)");
            await UniTask.Delay(100);
        }
        
        private void GenerateRandomDiscounts()
        {
            _itemDiscounts.Clear();
            
            // Random discount for 20% of items
            int discountCount = Mathf.Max(1, _allShopItems.Length / 5);
            var availableItems = new List<ShopItemDefinition>(_allShopItems);
            
            for (int i = 0; i < discountCount && availableItems.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableItems.Count);
                var item = availableItems[randomIndex];
                availableItems.RemoveAt(randomIndex);
                
                float discount = UnityEngine.Random.Range(0.1f, _maxDiscountPercent);
                _itemDiscounts[item.ItemId] = discount;
                
                LogDebug($"[ShopManager] Applied {discount:P1} discount to {item.ItemId}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get shop item by ID
        /// </summary>
        public ShopItemDefinition GetShopItem(string itemId)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ShopManager] Not initialized yet");
                return null;
            }
            
            _itemsById.TryGetValue(itemId, out var item);
            return item;
        }
        
        /// <summary>
        /// Get all items by type
        /// </summary>
        public ShopItemDefinition[] GetItemsByType(ShopItemDefinition.ShopItemType itemType)
        {
            if (!_isInitialized)
                return new ShopItemDefinition[0];
            
            var result = new List<ShopItemDefinition>();
            
            foreach (var item in _allShopItems)
            {
                if (item != null && item.ItemType == itemType)
                {
                    result.Add(item);
                }
            }
            
            return result.ToArray();
        }
        
        /// <summary>
        /// Check if player owns an item
        /// </summary>
        public bool DoesPlayerOwnItem(string itemId)
        {
            return _ownedItems.ContainsKey(itemId) && _ownedItems[itemId] > 0;
        }
        
        /// <summary>
        /// Get owned quantity of an item
        /// </summary>
        public int GetOwnedQuantity(string itemId)
        {
            _ownedItems.TryGetValue(itemId, out int quantity);
            return quantity;
        }
        
        /// <summary>
        /// Check if item can be purchased
        /// </summary>
        public bool CanPurchaseItem(string itemId, out string reason)
        {
            reason = "";
            
            if (!_isInitialized)
            {
                reason = "Shop not initialized";
                return false;
            }
            
            if (_isPurchasing)
            {
                reason = "Another purchase in progress";
                return false;
            }
            
            var item = GetShopItem(itemId);
            if (item == null)
            {
                reason = "Item not found";
                return false;
            }
            
            // Check if item is available
            // TODO: Get real player stats from save system
            if (!item.IsAvailableForPlayer(1, 0, new string[0]))
            {
                reason = "Item not available";
                return false;
            }
            
            // Check if already owned (for one-time purchases)
            if (item.Purchase == ShopItemDefinition.PurchaseType.OneTime && DoesPlayerOwnItem(itemId))
            {
                reason = "Already owned";
                return false;
            }
            
            // Check currency
            long finalPrice = GetFinalPrice(itemId);
            if (!HasEnoughCurrencyByString(item.Currency.ToString(), finalPrice))
            {
                reason = "Insufficient currency";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Purchase an item
        /// </summary>
        public async UniTask<bool> PurchaseItemAsync(string itemId, int quantity = 1)
        {
            if (!CanPurchaseItem(itemId, out string reason))
            {
                LogDebug($"[ShopManager] Cannot purchase {itemId}: {reason}");
                OnPurchaseFailed?.Invoke(itemId, reason);
                return false;
            }
            
            var item = GetShopItem(itemId);
            if (item == null)
            {
                OnPurchaseFailed?.Invoke(itemId, "Item not found");
                return false;
            }
            
            _isPurchasing = true;
            
            try
            {
                // Handle IAP purchases
                if (item.IsIAPItem)
                {
                    return await ProcessIAPPurchaseAsync(item);
                }
                
                // Handle currency purchases
                return await ProcessCurrencyPurchaseAsync(item, quantity);
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopManager] Purchase failed: {ex.Message}");
                OnPurchaseFailed?.Invoke(itemId, ex.Message);
                return false;
            }
            finally
            {
                _isPurchasing = false;
            }
        }
        
        /// <summary>
        /// Get final price with discounts applied
        /// </summary>
        public long GetFinalPrice(string itemId)
        {
            var item = GetShopItem(itemId);
            if (item == null) return 0;
            
            long basePrice = item.Price;
            
            if (_itemDiscounts.TryGetValue(itemId, out float discount))
            {
                long discountedPrice = (long)(basePrice * (1f - discount));
                return Math.Max(1L, discountedPrice);
            }
            
            return basePrice;
        }
        
        /// <summary>
        /// Get discount percentage for item (0-1)
        /// </summary>
        public float GetItemDiscount(string itemId)
        {
            _itemDiscounts.TryGetValue(itemId, out float discount);
            return discount;
        }
        
        /// <summary>
        /// Refresh shop data (reload discounts, check availability)
        /// </summary>
        public async UniTask RefreshShopDataAsync()
        {
            if (!_isInitialized) return;
            
            LogDebug("[ShopManager] Refreshing shop data...");
            
            // Regenerate discounts
            if (_discountEnabled)
            {
                GenerateRandomDiscounts();
            }
            
            // Reload owned items
            await LoadOwnedItemsAsync();
            
            OnShopDataRefreshed?.Invoke(_allShopItems.Length);
            LogDebug("[ShopManager] Shop data refreshed");
        }
        
        #endregion
        
        #region Private Purchase Methods
        
        private async UniTask<bool> ProcessCurrencyPurchaseAsync(ShopItemDefinition item, int quantity)
        {
            long finalPrice = GetFinalPrice(item.ItemId);
            long totalPrice = finalPrice * quantity;
            
            LogDebug($"[ShopManager] Processing currency purchase: {item.ItemId} x{quantity} for {totalPrice} {item.Currency}");
            
            // Deduct currency
            bool deductSuccess = SpendCurrencyByString(item.Currency.ToString(), totalPrice);
            if (!deductSuccess)
            {
                OnPurchaseFailed?.Invoke(item.ItemId, "Failed to deduct currency");
                return false;
            }
            
            // Add item to owned items
            int currentQuantity = GetOwnedQuantity(item.ItemId);
            int newQuantity = currentQuantity + (item.ItemType == ShopItemDefinition.ShopItemType.PowerUp ? 
                item.PowerUpQuantity * quantity : quantity);
            
            _ownedItems[item.ItemId] = newQuantity;
            
            // Save to persistent storage
            // TODO: Integration với Save System
            
            LogDebug($"[ShopManager] Purchase successful: {item.ItemId} x{quantity}");
            OnItemPurchased?.Invoke(item.ItemId, quantity, totalPrice);
            
            return true;
        }
        
        private async UniTask<bool> ProcessIAPPurchaseAsync(ShopItemDefinition item)
        {
            LogDebug($"[ShopManager] Processing IAP purchase: {item.ItemId} ({item.IAPProductId})");
            
            OnIAPPurchaseStarted?.Invoke(item.ItemId, item.IAPProductId);
            
            // Cancel previous IAP operation
            _iapCancellationTokenSource?.Cancel();
            _iapCancellationTokenSource?.Dispose();
            _iapCancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // TODO: Integration với Unity IAP
                // Simulate IAP process
                await UniTask.Delay(
                    TimeSpan.FromSeconds(2), 
                    cancellationToken: _iapCancellationTokenSource.Token
                );
                
                // Simulate success (50% chance for testing)
                bool success = UnityEngine.Random.value > 0.5f;
                
                if (success)
                {
                    // Add purchased item
                    int currentQuantity = GetOwnedQuantity(item.ItemId);
                    int newQuantity = currentQuantity + (item.ItemType == ShopItemDefinition.ShopItemType.PowerUp ? 
                        item.PowerUpQuantity : 1);
                    
                    _ownedItems[item.ItemId] = newQuantity;
                    
                    LogDebug($"[ShopManager] IAP purchase successful: {item.ItemId}");
                    OnItemPurchased?.Invoke(item.ItemId, 1, item.Price);
                    OnIAPPurchaseCompleted?.Invoke(item.ItemId, item.IAPProductId, true);
                    
                    return true;
                }
                else
                {
                    LogDebug($"[ShopManager] IAP purchase failed: {item.ItemId}");
                    OnPurchaseFailed?.Invoke(item.ItemId, "IAP transaction failed");
                    OnIAPPurchaseCompleted?.Invoke(item.ItemId, item.IAPProductId, false);
                    
                    return false;
                }
                
            }
            catch (OperationCanceledException)
            {
                LogDebug($"[ShopManager] IAP purchase cancelled: {item.ItemId}");
                OnPurchaseFailed?.Invoke(item.ItemId, "Purchase cancelled");
                OnIAPPurchaseCompleted?.Invoke(item.ItemId, item.IAPProductId, false);
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopManager] IAP purchase error: {ex.Message}");
                OnPurchaseFailed?.Invoke(item.ItemId, ex.Message);
                OnIAPPurchaseCompleted?.Invoke(item.ItemId, item.IAPProductId, false);
                return false;
            }
        }
        
        #endregion
        
        #region Currency Helper Methods
        
        /// <summary>
        /// Helper method to check currency by string name (converts to enum)
        /// </summary>
        private bool HasEnoughCurrencyByString(string currencyName, long amount)
        {
            if (System.Enum.TryParse<CurrencyType>(currencyName, true, out CurrencyType currencyType))
            {
                return _currencyManager.HasEnough(currencyType, amount);
            }
            
            Debug.LogWarning($"[ShopManager] Invalid currency type: {currencyName}");
            return false;
        }
        
        /// <summary>
        /// Helper method to spend currency by string name (converts to enum)
        /// </summary>
        private bool SpendCurrencyByString(string currencyName, long amount)
        {
            if (System.Enum.TryParse<CurrencyType>(currencyName, true, out CurrencyType currencyType))
            {
                return _currencyManager.SpendCurrency(currencyType, amount);
            }
            
            Debug.LogWarning($"[ShopManager] Invalid currency type: {currencyName}");
            return false;
        }
        
        #endregion
        
        #region Utility Methods
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        
        [ContextMenu("Refresh Shop Data")]
        private void EditorRefreshShopData()
        {
            if (Application.isPlaying && _isInitialized)
            {
                RefreshShopDataAsync().Forget();
            }
        }
        
        [ContextMenu("Debug Owned Items")]
        private void DebugOwnedItems()
        {
            if (!Application.isPlaying || _ownedItems == null)
            {
                Debug.Log("Shop not initialized or not playing");
                return;
            }
            
            Debug.Log($"=== Owned Items ({_ownedItems.Count}) ===");
            foreach (var kvp in _ownedItems)
            {
                var item = GetShopItem(kvp.Key);
                string itemName = item?.DisplayName ?? kvp.Key;
                Debug.Log($"- {itemName}: {kvp.Value}");
            }
        }
        
        [ContextMenu("Debug Discounts")]
        private void DebugDiscounts()
        {
            if (!Application.isPlaying || _itemDiscounts == null)
            {
                Debug.Log("Shop not initialized or not playing");
                return;
            }
            
            Debug.Log($"=== Item Discounts ({_itemDiscounts.Count}) ===");
            foreach (var kvp in _itemDiscounts)
            {
                var item = GetShopItem(kvp.Key);
                string itemName = item?.DisplayName ?? kvp.Key;
                Debug.Log($"- {itemName}: {kvp.Value:P1} off");
            }
        }
        
        #endif
        
        #endregion
    }
}
