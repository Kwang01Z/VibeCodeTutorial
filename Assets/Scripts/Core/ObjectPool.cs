using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Generic object pool cho MonoBehaviour objects.
    /// Tránh Instantiate/Destroy hitches trong gameplay.
    /// </summary>
    /// <typeparam name="T">Type kế thừa từ MonoBehaviour</typeparam>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly Queue<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly bool _worldPositionStays;
        private readonly int _initialCapacity;
        
        /// <summary>
        /// Số lượng object đang available trong pool
        /// </summary>
        public int AvailableCount => _pool.Count;
        
        /// <summary>
        /// Tổng số object đã tạo (active + pooled)
        /// </summary>
        public int TotalCreated { get; private set; }
        
        /// <summary>
        /// Constructor với prefab và parent transform
        /// </summary>
        /// <param name="prefab">Prefab để clone</param>
        /// <param name="parent">Parent transform cho objects</param>
        /// <param name="initialCapacity">Số lượng pre-warm</param>
        /// <param name="worldPositionStays">SetParent worldPositionStays parameter</param>
        public ObjectPool(T prefab, Transform parent = null, int initialCapacity = 0, bool worldPositionStays = false)
        {
            _prefab = prefab;
            _parent = parent;
            _worldPositionStays = worldPositionStays;
            _initialCapacity = initialCapacity;
            _pool = new Queue<T>(_initialCapacity);
            
            // Pre-warm pool nếu cần
            if (_initialCapacity > 0)
            {
                PreWarm(_initialCapacity);
            }
        }
        
        /// <summary>
        /// Lấy object từ pool hoặc tạo mới nếu pool empty
        /// </summary>
        /// <returns>Active object ready to use</returns>
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                // Lấy từ pool
                obj = _pool.Dequeue();
            }
            else
            {
                // Tạo mới nếu pool empty
                obj = CreateNewObject();
            }
            
            // Activate object
            obj.gameObject.SetActive(true);
            
            return obj;
        }
        
        /// <summary>
        /// Trả object về pool (deactivate và queue lại)
        /// </summary>
        /// <param name="obj">Object cần return</param>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] Trying to return null object");
                return;
            }
            
            // Safety check: object đã return chưa
            if (!obj.gameObject.activeSelf)
            {
                Debug.LogWarning($"[ObjectPool] Object {obj.name} đã inactive, có thể đã return rồi");
                return;
            }
            
            // Deactivate và return về parent
            obj.gameObject.SetActive(false);
            
            if (_parent != null)
            {
                obj.transform.SetParent(_parent, _worldPositionStays);
            }
            
            // Queue lại vào pool
            _pool.Enqueue(obj);
        }
        
        /// <summary>
        /// Pre-warm pool với số lượng objects cụ thể
        /// </summary>
        /// <param name="count">Số lượng objects tạo sẵn</param>
        public void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        /// <summary>
        /// Clear toàn bộ pool và destroy objects
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            
            TotalCreated = 0;
        }
        
        /// <summary>
        /// Get object với callback setup
        /// </summary>
        /// <param name="setupCallback">Callback để setup object sau khi get</param>
        /// <returns>Configured object</returns>
        public T Get(System.Action<T> setupCallback)
        {
            T obj = Get();
            setupCallback?.Invoke(obj);
            return obj;
        }
        
        /// <summary>
        /// Return object với callback cleanup
        /// </summary>
        /// <param name="obj">Object cần return</param>
        /// <param name="cleanupCallback">Callback để cleanup trước khi return</param>
        public void Return(T obj, System.Action<T> cleanupCallback)
        {
            if (obj == null) return;
            
            cleanupCallback?.Invoke(obj);
            Return(obj);
        }
        
        /// <summary>
        /// Tạo object mới từ prefab
        /// </summary>
        /// <returns>New object instance</returns>
        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent, _worldPositionStays);
            TotalCreated++;
            
            // Optional: set name cho dễ debug
            obj.name = $"{_prefab.name}_{TotalCreated:000}";
            
            return obj;
        }
        
        /// <summary>
        /// Pool statistics cho debugging
        /// </summary>
        /// <returns>Pool info string</returns>
        public string GetStats()
        {
            return $"ObjectPool<{typeof(T).Name}>: Available={AvailableCount}, Total={TotalCreated}, Active={TotalCreated - AvailableCount}";
        }
    }
    
    /// <summary>
    /// Extension methods cho ObjectPool
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Get object with position và rotation
        /// </summary>
        public static T GetAtPosition<T>(this ObjectPool<T> pool, Vector3 position, Quaternion rotation = default) where T : MonoBehaviour
        {
            T obj = pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        
        /// <summary>
        /// Auto-return object sau thời gian cụ thể
        /// </summary>
        public static T GetWithAutoReturn<T>(this ObjectPool<T> pool, float returnAfter) where T : MonoBehaviour
        {
            T obj = pool.Get();
            
            // Add component để auto return (nếu chưa có)
            var autoReturn = obj.GetComponent<AutoReturnToPool<T>>();
            if (autoReturn == null)
            {
                autoReturn = obj.gameObject.AddComponent<AutoReturnToPool<T>>();
            }
            
            autoReturn.Initialize(pool, returnAfter);
            return obj;
        }
    }
    
    /// <summary>
    /// Component helper để auto-return object về pool sau thời gian
    /// </summary>
    /// <typeparam name="T">Pool object type</typeparam>
    public class AutoReturnToPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        private ObjectPool<T> _pool;
        private Timer _returnTimer;
        
        public void Initialize(ObjectPool<T> pool, float returnAfter)
        {
            _pool = pool;
            _returnTimer.Start(returnAfter);
        }
        
        private void Update()
        {
            _returnTimer.Update(Time.deltaTime);
            
            if (_returnTimer.IsExpired)
            {
                _pool.Return(GetComponent<T>());
            }
        }
    }
}
