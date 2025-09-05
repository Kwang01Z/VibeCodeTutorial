using UnityEngine;
using System;
using System.Collections.Generic;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Specialized ObjectPool cho item pickups trong Phase 2
    /// Sử dụng composition pattern với specialized features cho pickup items
    /// </summary>
    /// <typeparam name="T">Type của pickup component (phải inherit MonoBehaviour)</typeparam>
    public class PickablePool<T> where T : MonoBehaviour
    {
        // Core ObjectPool instance
        private readonly ObjectPool<T> _basePool;
        
        // Events
        public event Action<T> OnItemSpawned;
        public event Action<T> OnItemReturned;
        public event Action<int> OnPoolSizeChanged;

        // Pool statistics
        private int _totalSpawned;
        private int _totalReturned;
        private readonly HashSet<T> _activeItems = new HashSet<T>();
        
        // Performance tracking
        private float _averageLifetime;
        private readonly List<float> _lifetimes = new List<float>(100);
        
        public int TotalSpawned => _totalSpawned;
        public int TotalReturned => _totalReturned;
        public int ActiveCount => _activeItems.Count;
        public float AverageLifetime => _averageLifetime;
        public IReadOnlyCollection<T> ActiveItems => _activeItems;
        
        // Delegate base pool properties
        public int AvailableCount => _basePool.AvailableCount;
        public int TotalCreated => _basePool.TotalCreated;

        /// <summary>
        /// Constructor cho PickablePool
        /// </summary>
        /// <param name="prefab">Prefab template cho pool</param>
        /// <param name="parent">Parent transform cho pooled objects</param>
        /// <param name="initialSize">Initial pool size</param>
        public PickablePool(T prefab, Transform parent, int initialSize = 10)
        {
            _basePool = new ObjectPool<T>(prefab, parent, initialSize);
        }

        /// <summary>
        /// Spawn item từ pool với tracking
        /// </summary>
        public T Get()
        {
            T item = _basePool.Get();
            if (item != null)
            {
                _totalSpawned++;
                _activeItems.Add(item);
                
                // Track spawn time cho lifetime calculation
                var tracker = item.GetComponent<PickupLifetimeTracker>();
                if (tracker == null)
                    tracker = item.gameObject.AddComponent<PickupLifetimeTracker>();
                tracker.MarkSpawned();
                
                OnItemSpawned?.Invoke(item);
                OnPoolSizeChanged?.Invoke(AvailableCount);
            }
            return item;
        }

        /// <summary>
        /// Return item về pool với lifetime tracking
        /// </summary>
        public void Return(T item)
        {
            if (item == null || !_activeItems.Contains(item))
                return;

            _totalReturned++;
            _activeItems.Remove(item);
            
            // Calculate lifetime
            var tracker = item.GetComponent<PickupLifetimeTracker>();
            if (tracker != null)
            {
                float lifetime = tracker.GetLifetime();
                _lifetimes.Add(lifetime);
                
                // Keep only last 100 samples cho performance
                if (_lifetimes.Count > 100)
                    _lifetimes.RemoveAt(0);
                
                // Recalculate average
                float total = 0f;
                foreach (float lt in _lifetimes)
                    total += lt;
                _averageLifetime = total / _lifetimes.Count;
            }

            _basePool.Return(item);
            
            OnItemReturned?.Invoke(item);
            OnPoolSizeChanged?.Invoke(AvailableCount);
        }

        /// <summary>
        /// Return tất cả active items về pool
        /// </summary>
        public void ReturnAll()
        {
            var activeList = new List<T>(_activeItems);
            foreach (var item in activeList)
            {
                Return(item);
            }
        }
        
        /// <summary>
        /// Pre-warm pool với số lượng objects cụ thể
        /// </summary>
        /// <param name="count">Số lượng objects tạo sẵn</param>
        public void PreWarm(int count)
        {
            _basePool.PreWarm(count);
        }
        
        /// <summary>
        /// Clear toàn bộ pool và destroy objects
        /// </summary>
        public void Clear()
        {
            _activeItems.Clear();
            _lifetimes.Clear();
            _totalSpawned = 0;
            _totalReturned = 0;
            _averageLifetime = 0f;
            
            _basePool.Clear();
        }

        /// <summary>
        /// Get pool statistics summary
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                TotalSpawned = _totalSpawned,
                TotalReturned = _totalReturned,
                ActiveCount = _activeItems.Count,
                AvailableCount = AvailableCount,
                AverageLifetime = _averageLifetime,
                EfficiencyRatio = _totalSpawned > 0 ? (float)_totalReturned / _totalSpawned : 0f
            };
        }

        /// <summary>
        /// Check xem item có đang active trong pool không
        /// </summary>
        public bool IsItemActive(T item)
        {
            return _activeItems.Contains(item);
        }

        /// <summary>
        /// Force cleanup cho performance optimization
        /// </summary>
        public void Cleanup()
        {
            // Remove null references
            _activeItems.RemoveWhere(item => item == null);
            
            // Clear old lifetime data
            if (_lifetimes.Count > 50)
            {
                int removeCount = _lifetimes.Count - 50;
                _lifetimes.RemoveRange(0, removeCount);
            }
        }

        /// <summary>
        /// Pool statistics data structure
        /// </summary>
        [System.Serializable]
        public struct PoolStatistics
        {
            public int TotalSpawned;
            public int TotalReturned;
            public int ActiveCount;
            public int AvailableCount;
            public float AverageLifetime;
            public float EfficiencyRatio; // Returned/Spawned ratio
            
            public override string ToString()
            {
                return $"Pool Stats: {ActiveCount} active, {AvailableCount} available, " +
                       $"{TotalSpawned} spawned, {EfficiencyRatio:P1} efficiency, " +
                       $"{AverageLifetime:F2}s avg lifetime";
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor debugging support
        /// </summary>
        [ContextMenu("Debug Pool State")]
        public void DebugPoolState()
        {
            var stats = GetStatistics();
            Debug.Log($"[PickablePool<{typeof(T).Name}>] {stats}");
            
            Debug.Log($"Active Items ({_activeItems.Count}):");
            foreach (var item in _activeItems)
            {
                if (item != null)
                {
                    var tracker = item.GetComponent<PickupLifetimeTracker>();
                    float lifetime = tracker != null ? tracker.GetLifetime() : -1f;
                    Debug.Log($"  - {item.name}: {lifetime:F2}s lifetime");
                }
                else
                {
                    Debug.LogWarning("  - NULL ITEM (cleanup needed)");
                }
            }
        }
#endif
    }

    /// <summary>
    /// Helper component để track lifetime của pickup items
    /// Automatically added by PickablePool
    /// </summary>
    internal class PickupLifetimeTracker : MonoBehaviour
    {
        private float _spawnTime;
        private bool _isTracking;

        public void MarkSpawned()
        {
            _spawnTime = Time.time;
            _isTracking = true;
        }

        public float GetLifetime()
        {
            return _isTracking ? Time.time - _spawnTime : 0f;
        }

        private void OnDisable()
        {
            _isTracking = false;
        }
    }

    /// <summary>
    /// Factory class để tạo specialized PickablePools
    /// </summary>
    public static class PickablePoolFactory
    {
        /// <summary>
        /// Tạo pool cho ItemPickup components
        /// </summary>
        public static PickablePool<T> CreateItemPool<T>(T prefab, Transform parent, int initialSize = 20) 
            where T : MonoBehaviour
        {
            if (prefab == null)
            {
                Debug.LogError("[PickablePoolFactory] Prefab cannot be null");
                return null;
            }

            var pool = new PickablePool<T>(prefab, parent, initialSize);
            
            // Pre-warm pool
            pool.PreWarm(initialSize);
            
            Debug.Log($"[PickablePoolFactory] Created pool for {typeof(T).Name} with {initialSize} pre-warmed items");
            
            return pool;
        }

        /// <summary>
        /// Tạo multiple pools cho different item types
        /// </summary>
        public static Dictionary<string, PickablePool<MonoBehaviour>> CreateMultiItemPools(
            Dictionary<string, MonoBehaviour> prefabMap, Transform parent, int sizePerPool = 15)
        {
            var pools = new Dictionary<string, PickablePool<MonoBehaviour>>();

            foreach (var kvp in prefabMap)
            {
                string itemType = kvp.Key;
                MonoBehaviour prefab = kvp.Value;

                if (prefab == null)
                {
                    Debug.LogWarning($"[PickablePoolFactory] Null prefab for {itemType}, skipping");
                    continue;
                }

                var pool = new PickablePool<MonoBehaviour>(prefab, parent, sizePerPool);
                pool.PreWarm(sizePerPool);
                pools[itemType] = pool;
                
                Debug.Log($"[PickablePoolFactory] Created {itemType} pool with {sizePerPool} items");
            }

            return pools;
        }
    }
}
