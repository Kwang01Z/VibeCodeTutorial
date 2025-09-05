using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;
using System.Collections.Generic;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Component gắn vào chunk prefab để quản lý setup và lifecycle.
    /// Được sử dụng với ObjectPool để spawn/despawn chunks.
    /// </summary>
    public class ChunkAnchor : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Hiển thị debug info trong Inspector")]
        private bool _showDebugInfo = true;
        
        [SerializeField, Tooltip("Hiển thị gizmos trong Scene view")]
        private bool _showGizmos = true;
        
        // Runtime state
        private ChunkDefinition _currentDefinition;
        private List<GameObject> _spawnedObstacles;
        private bool _isSetup = false;
        private float _spawnTimeStamp;
        
        // Cached components
        private Transform _cachedTransform;
        
        /// <summary>
        /// ChunkDefinition hiện tại đang được sử dụng
        /// </summary>
        public ChunkDefinition CurrentDefinition => _currentDefinition;
        
        /// <summary>
        /// Chiều dài chunk hiện tại
        /// </summary>
        public float ChunkLength => _currentDefinition?.ChunkLength ?? 0f;
        
        /// <summary>
        /// Vị trí Z của chunk end (để tính toán spawn next chunk)
        /// </summary>
        public float EndPositionZ => transform.position.z + ChunkLength;
        
        /// <summary>
        /// Chunk đã được setup chưa
        /// </summary>
        public bool IsSetup => _isSetup;
        
        /// <summary>
        /// Thời điểm spawn chunk (để debug)
        /// </summary>
        public float SpawnTimeStamp => _spawnTimeStamp;
        
        /// <summary>
        /// Số lượng obstacles đã spawn
        /// </summary>
        public int SpawnedObstacleCount => _spawnedObstacles?.Count ?? 0;
        
        private void Awake()
        {
            // Cache components
            _cachedTransform = transform;
            _spawnedObstacles = new List<GameObject>(8); // Pre-allocate
        }
        
        /// <summary>
        /// Setup chunk với ChunkDefinition cụ thể
        /// </summary>
        /// <param name="definition">ChunkDefinition để apply</param>
        public void Setup(ChunkDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[ChunkAnchor] Setup với null ChunkDefinition!", this);
                return;
            }
            
            // Validate definition trước khi setup
            var validation = definition.Validate();
            if (!validation.isValid)
            {
                Debug.LogWarning($"[ChunkAnchor] Setup chunk với validation errors: {string.Join(", ", validation.errors)}", this);
            }
            
            _currentDefinition = definition;
            _spawnTimeStamp = Time.time;
            
            // Clear previous obstacles nếu có
            ClearObstacles();
            
            // Spawn obstacles theo definition
            SpawnObstacles();
            
            _isSetup = true;
            
            // Debug log
            if (_showDebugInfo)
            {
                Debug.Log($"[ChunkAnchor] Setup chunk '{definition.name}' tại Z={_cachedTransform.position.z:F2}, " +
                         $"spawned {_spawnedObstacles.Count} obstacles", this);
            }
        }
        
        /// <summary>
        /// Reset chunk về trạng thái default (gọi khi return về pool)
        /// </summary>
        public void ResetChunk()
        {
            ClearObstacles();
            _currentDefinition = null;
            _isSetup = false;
            _spawnTimeStamp = 0f;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[ChunkAnchor] Reset chunk tại {_cachedTransform.position}", this);
            }
        }
        
        /// <summary>
        /// Spawn obstacles theo ChunkDefinition
        /// </summary>
        private void SpawnObstacles()
        {
            if (_currentDefinition?.Obstacles == null) return;
            
            foreach (var obstacleInfo in _currentDefinition.Obstacles)
            {
                if (!obstacleInfo.IsValid)
                {
                    Debug.LogWarning($"[ChunkAnchor] Skip invalid obstacle trong chunk {_currentDefinition.name}", this);
                    continue;
                }
                
                // Tạo obstacle instance
                GameObject obstacleInstance = Instantiate(obstacleInfo.prefab, _cachedTransform);
                
                // Set position & rotation
                obstacleInstance.transform.localPosition = obstacleInfo.localPosition;
                obstacleInstance.transform.localRotation = Quaternion.identity;
                
                // Đảm bảo obstacle có layer đúng
                if (obstacleInstance.layer != LayerMask.NameToLayer("Obstacle"))
                {
                    obstacleInstance.layer = LayerMask.NameToLayer("Obstacle");
                    
                    // Apply cho children nếu có
                    foreach (Transform child in obstacleInstance.transform)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("Obstacle");
                    }
                }
                
                // Add vào list để track
                _spawnedObstacles.Add(obstacleInstance);
                
                // Optional: thêm tag debug
                if (_showDebugInfo && string.IsNullOrEmpty(obstacleInstance.name))
                {
                    obstacleInstance.name = $"{obstacleInfo.prefab.name}_L{obstacleInfo.requiredLane}";
                }
            }
        }
        
        /// <summary>
        /// Clear tất cả spawned obstacles
        /// </summary>
        private void ClearObstacles()
        {
            if (_spawnedObstacles == null) return;
            
            // Destroy tất cả obstacles
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                if (_spawnedObstacles[i] != null)
                {
                    DestroyImmediate(_spawnedObstacles[i]);
                }
            }
            
            _spawnedObstacles.Clear();
        }
        
        /// <summary>
        /// Kiểm tra chunk có nằm trong range cụ thể không
        /// </summary>
        /// <param name="playerZ">Vị trí Z của player</param>
        /// <param name="spawnDistance">Distance để spawn</param>
        /// <param name="despawnDistance">Distance để despawn</param>
        /// <returns>True nếu chunk nằm trong active range</returns>
        public bool IsInActiveRange(float playerZ, float spawnDistance, float despawnDistance)
        {
            float chunkStartZ = _cachedTransform.position.z;
            float chunkEndZ = chunkStartZ + ChunkLength;
            
            // Chunk active nếu:
            // - End của chunk chưa quá xa so với player (chưa cần despawn)
            // - Start của chunk không quá gần player (đã spawn xong)
            return chunkEndZ >= playerZ - despawnDistance && chunkStartZ <= playerZ + spawnDistance;
        }
        
        /// <summary>
        /// Kiểm tra chunk có cần despawn không
        /// </summary>
        /// <param name="playerZ">Vị trí Z của player</param>
        /// <param name="despawnDistance">Distance để despawn</param>
        /// <returns>True nếu cần despawn</returns>
        public bool ShouldDespawn(float playerZ, float despawnDistance)
        {
            return EndPositionZ < playerZ - despawnDistance;
        }
        
        /// <summary>
        /// Get obstacles theo lane cụ thể (runtime query)
        /// </summary>
        /// <param name="laneIndex">Lane index</param>
        /// <returns>List obstacles trong lane</returns>
        public List<GameObject> GetObstaclesInLane(int laneIndex)
        {
            var result = new List<GameObject>();
            
            if (_currentDefinition?.Obstacles == null || _spawnedObstacles == null)
                return result;
            
            for (int i = 0; i < _currentDefinition.Obstacles.Length && i < _spawnedObstacles.Count; i++)
            {
                var obstacleInfo = _currentDefinition.Obstacles[i];
                if (obstacleInfo.requiredLane == laneIndex || obstacleInfo.requiredLane == -1)
                {
                    if (_spawnedObstacles[i] != null)
                        result.Add(_spawnedObstacles[i]);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Debug info cho Inspector và logging
        /// </summary>
        /// <returns>Debug string</returns>
        public string GetDebugInfo()
        {
            if (!_isSetup)
                return "ChunkAnchor: Not setup";
            
            return $"Chunk: {_currentDefinition?.name ?? "Unknown"}\n" +
                   $"Position: {_cachedTransform.position}\n" +
                   $"Length: {ChunkLength:F2}m\n" +
                   $"Difficulty: {_currentDefinition?.Difficulty}\n" +
                   $"Obstacles: {SpawnedObstacleCount}\n" +
                   $"Spawned: {Time.time - _spawnTimeStamp:F2}s ago\n" +
                   $"EndZ: {EndPositionZ:F2}";
        }
        
        // Unity Lifecycle Events
        private void OnEnable()
        {
            // Reset timestamp khi chunk được active lại
            if (_isSetup)
                _spawnTimeStamp = Time.time;
        }
        
        private void OnDisable()
        {
            // Log disable cho debug
            if (_showDebugInfo && _isSetup)
            {
                Debug.Log($"[ChunkAnchor] Chunk '{_currentDefinition?.name}' disabled after {Time.time - _spawnTimeStamp:F2}s", this);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup khi destroy
            ClearObstacles();
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Gizmos cho debug visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            Vector3 position = _cachedTransform ? _cachedTransform.position : transform.position;
            float length = ChunkLength;
            
            // Vẽ chunk bounds
            Gizmos.color = _isSetup ? Color.green : Color.gray;
            Vector3 center = position + Vector3.forward * length * 0.5f;
            Vector3 size = new Vector3(6f, 0.5f, length);
            Gizmos.DrawWireCube(center, size);
            
            // Vẽ obstacles nếu có
            if (_isSetup && _spawnedObstacles != null)
            {
                Gizmos.color = Color.red;
                foreach (var obstacle in _spawnedObstacles)
                {
                    if (obstacle != null)
                    {
                        Gizmos.DrawWireSphere(obstacle.transform.position, 0.3f);
                    }
                }
            }
            
            // Label
            if (_showDebugInfo)
            {
                UnityEditor.Handles.Label(position + Vector3.up * 2f, 
                    _currentDefinition?.name ?? "ChunkAnchor");
            }
        }
        
        /// <summary>
        /// Custom Inspector info
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;
            
            // Highlight selected chunk
            Vector3 position = _cachedTransform ? _cachedTransform.position : transform.position;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(position + Vector3.forward * ChunkLength * 0.5f, 
                               new Vector3(7f, 1f, ChunkLength));
                               
            // Draw lane indicators
            Gizmos.color = Color.blue;
            for (int i = 0; i < 3; i++)
            {
                float laneX = (i - 1) * 2f; // -2, 0, 2
                Vector3 laneStart = position + new Vector3(laneX, 0, 0);
                Vector3 laneEnd = laneStart + Vector3.forward * ChunkLength;
                Gizmos.DrawLine(laneStart, laneEnd);
            }
        }
        
        /// <summary>
        /// Validate prefab trong Editor
        /// </summary>
        private void OnValidate()
        {
            // Auto-assign transform cache nếu null
            if (_cachedTransform == null)
                _cachedTransform = transform;
        }
#endif
    }
}
