using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;
using System.Collections.Generic;
using System;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Spawner hệ thống procedural chunks với pooling và deterministic random.
    /// Tuân thủ spawn rules (Hard không liên tiếp) và quản lý lifecycle chunks.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField, Tooltip("Danh sách chunk definitions có thể spawn")]
        private ChunkDefinition[] _availableChunks = new ChunkDefinition[0];
        
        [SerializeField, Tooltip("Transform player để track position")]
        private Transform _playerTransform;
        
        [Header("Spawn Configuration")]
        [SerializeField, Tooltip("Khoảng cách phía trước player để spawn chunk (m)")]
        [Range(30f, 100f)]
        private float _spawnDistance = 60f;
        
        [SerializeField, Tooltip("Khoảng cách phía sau player để despawn chunk (m)")]
        [Range(10f, 50f)]
        private float _despawnDistance = 30f;
        
        [SerializeField, Tooltip("Số chunk pre-warm khi khởi tạo pool")]
        [Range(3, 20)]
        private int _prewarmCount = 10;
        
        [SerializeField, Tooltip("Seed cho deterministic spawning")]
        private uint _seed = 12345;
        
        [Header("Difficulty Progression")]
        [SerializeField, Tooltip("Distance để bắt đầu tăng độ khó")]
        private float _difficultyRampStart = 100f;
        
        [SerializeField, Tooltip("Curve weight theo distance (X=km, Y=hardWeight)")]
        private AnimationCurve _difficultyWeightCurve = AnimationCurve.Linear(0f, 0.2f, 5f, 0.8f);
        
        [Header("Debug")]
        [SerializeField, Tooltip("Hiển thị debug info và gizmos")]
        private bool _debugMode = true;
        
        [SerializeField, Tooltip("Log spawning events")]
        private bool _logSpawning = false;
        
        // Runtime state
        private ObjectPool<ChunkAnchor> _chunkPool;
        private List<ChunkAnchor> _activeChunks;
        private XorShift128 _prng;
        private float _nextSpawnZ;
        private DifficultyTag _lastSpawnedDifficulty = DifficultyTag.Easy;
        private float _totalDistanceSpawned;
        private bool _isInitialized = false;
        
        // Performance tracking
        private int _chunksSpawned = 0;
        private int _chunksDespawned = 0;
        private float _lastFrameSpawnTime = 0f;
        
        // Cached components
        private ISpeedManager _speedManager;
        
        // Events
        public event Action<ChunkAnchor> OnChunkSpawned;
        public event Action<ChunkAnchor> OnChunkDespawned;
        public event Action OnSpawnerReset;
        
        /// <summary>
        /// Spawner đã được khởi tạo chưa
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Số chunk đang active
        /// </summary>
        public int ActiveChunkCount => _activeChunks?.Count ?? 0;
        
        /// <summary>
        /// Position Z tiếp theo sẽ spawn
        /// </summary>
        public float NextSpawnZ => _nextSpawnZ;
        
        /// <summary>
        /// Tổng distance đã spawn
        /// </summary>
        public float TotalDistanceSpawned => _totalDistanceSpawned;
        
        /// <summary>
        /// Difficulty của chunk cuối cùng
        /// </summary>
        public DifficultyTag LastSpawnedDifficulty => _lastSpawnedDifficulty;
        
        /// <summary>
        /// Current seed
        /// </summary>
        public uint CurrentSeed => _seed;
        
        private void Awake()
        {
            InitializeSpawner();
        }
        
        private void Start()
        {
            // Cache dependencies
            if (_playerTransform == null)
            {
                var player = FindObjectOfType<SimpleRunnerController>();
                if (player != null)
                {
                    _playerTransform = player.transform;
                    Debug.Log("[ObstacleSpawner] Auto-found player transform", this);
                }
                else
                {
                    Debug.LogError("[ObstacleSpawner] Không tìm thấy player transform!", this);
                }
            }
            
            _speedManager = FindObjectOfType<SpeedManager>() as ISpeedManager;
            
            // Start spawning
            StartRun();
        }
        
        private void Update()
        {
            if (!_isInitialized || _playerTransform == null) return;
            
            UpdateSpawning();
            UpdateDespawning();
            
            if (_debugMode)
            {
                UpdateDebugInfo();
            }
        }
        
        /// <summary>
        /// Khởi tạo spawner và pool
        /// </summary>
        private void InitializeSpawner()
        {
            // Validate chunks
            if (_availableChunks == null || _availableChunks.Length == 0)
            {
                Debug.LogError("[ObstacleSpawner] Không có chunk nào để spawn!", this);
                return;
            }
            
            // Validate chunk definitions
            int validChunks = 0;
            foreach (var chunk in _availableChunks)
            {
                if (chunk != null && chunk.ChunkPrefab != null)
                {
                    validChunks++;
                    var validation = chunk.Validate();
                    if (!validation.isValid)
                    {
                        Debug.LogWarning($"[ObstacleSpawner] Chunk '{chunk.name}' có validation issues: {string.Join(", ", validation.errors)}", chunk);
                    }
                }
            }
            
            if (validChunks == 0)
            {
                Debug.LogError("[ObstacleSpawner] Không có chunk hợp lệ nào!", this);
                return;
            }
            
            // Tạo pools cho chunks - sử dụng generic prefab vì chunks có thể khác nhau
            // Giả định: tất cả chunk prefabs đều có ChunkAnchor component
            var firstValidChunk = Array.Find(_availableChunks, c => c?.ChunkPrefab != null);
            var chunkAnchorPrefab = firstValidChunk.ChunkPrefab.GetComponent<ChunkAnchor>();
            
            if (chunkAnchorPrefab == null)
            {
                Debug.LogError("[ObstacleSpawner] Chunk prefab phải có ChunkAnchor component!", firstValidChunk);
                return;
            }
            
            // Initialize pool với empty prefab - sẽ setup runtime
            GameObject poolPrefab = new GameObject("ChunkPoolPrefab");
            poolPrefab.AddComponent<ChunkAnchor>();
            poolPrefab.SetActive(false);
            
            _chunkPool = new ObjectPool<ChunkAnchor>(poolPrefab.GetComponent<ChunkAnchor>(), transform, _prewarmCount);
            
            // Initialize collections
            _activeChunks = new List<ChunkAnchor>(32); // Pre-allocate reasonable size
            
            // Initialize PRNG
            _prng = new XorShift128(_seed);
            
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"[ObstacleSpawner] Initialized với {validChunks} chunks, pool size {_prewarmCount}, seed {_seed}", this);
            }
        }
        
        /// <summary>
        /// Bắt đầu run mới
        /// </summary>
        public void StartRun()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[ObstacleSpawner] StartRun gọi trước khi Initialize!", this);
                return;
            }
            
            // Reset state
            ResetSpawner();
            
            // Spawn initial chunks (3 chunks Easy)
            SpawnInitialChunks();
            
            if (_debugMode)
            {
                Debug.Log($"[ObstacleSpawner] Started run với {_activeChunks.Count} initial chunks", this);
            }
        }
        
        /// <summary>
        /// Set seed mới (gọi trước StartRun)
        /// </summary>
        /// <param name="newSeed">Seed mới</param>
        public void SetSeed(uint newSeed)
        {
            _seed = newSeed;
            _prng.Reseed(newSeed);
            
            if (_debugMode)
            {
                Debug.Log($"[ObstacleSpawner] Seed changed to {newSeed}", this);
            }
        }
        
        /// <summary>
        /// Reset spawner về trạng thái ban đầu
        /// </summary>
        public void ResetSpawner()
        {
            // Return tất cả active chunks về pool
            if (_activeChunks != null)
            {
                for (int i = _activeChunks.Count - 1; i >= 0; i--)
                {
                    var chunk = _activeChunks[i];
                    if (chunk != null)
                    {
                        chunk.ResetChunk();
                        _chunkPool.Return(chunk);
                    }
                }
                _activeChunks.Clear();
            }
            
            // Reset spawn state
            _nextSpawnZ = 0f;
            _lastSpawnedDifficulty = DifficultyTag.Easy;
            _totalDistanceSpawned = 0f;
            _chunksSpawned = 0;
            _chunksDespawned = 0;
            
            OnSpawnerReset?.Invoke();
        }
        
        /// <summary>
        /// Spawn 3 chunks đầu (Easy difficulty)
        /// </summary>
        private void SpawnInitialChunks()
        {
            for (int i = 0; i < 3; i++)
            {
                var easyChunk = GetRandomChunkByDifficulty(DifficultyTag.Easy);
                if (easyChunk != null)
                {
                    SpawnChunk(easyChunk);
                }
            }
        }
        
        /// <summary>
        /// Update spawn logic mỗi frame
        /// </summary>
        private void UpdateSpawning()
        {
            if (_playerTransform == null) return;
            
            float playerZ = _playerTransform.position.z;
            
            // Spawn chunks khi cần
            while (playerZ + _spawnDistance >= _nextSpawnZ)
            {
                var chunkDef = PickNextChunk();
                if (chunkDef != null)
                {
                    SpawnChunk(chunkDef);
                }
                else
                {
                    Debug.LogWarning("[ObstacleSpawner] Không thể pick chunk để spawn!", this);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Update despawn logic mỗi frame
        /// </summary>
        private void UpdateDespawning()
        {
            if (_playerTransform == null || _activeChunks == null) return;
            
            float playerZ = _playerTransform.position.z;
            
            // Duyệt ngược để safe remove
            for (int i = _activeChunks.Count - 1; i >= 0; i--)
            {
                var chunk = _activeChunks[i];
                if (chunk == null) continue;
                
                if (chunk.ShouldDespawn(playerZ, _despawnDistance))
                {
                    DespawnChunk(i);
                }
            }
        }
        
        /// <summary>
        /// Spawn chunk tại vị trí tiếp theo
        /// </summary>
        /// <param name="definition">ChunkDefinition để spawn</param>
        private void SpawnChunk(ChunkDefinition definition)
        {
            var chunk = _chunkPool.Get();
            if (chunk == null)
            {
                Debug.LogError("[ObstacleSpawner] Pool returned null chunk!", this);
                return;
            }
            
            // Setup position
            chunk.transform.position = new Vector3(0f, 0f, _nextSpawnZ);
            
            // Setup chunk với definition
            chunk.Setup(definition);
            
            // Add vào active list
            _activeChunks.Add(chunk);
            
            // Update spawn tracking
            _nextSpawnZ += definition.ChunkLength;
            _lastSpawnedDifficulty = definition.Difficulty;
            _totalDistanceSpawned += definition.ChunkLength;
            _chunksSpawned++;
            
            // Events
            OnChunkSpawned?.Invoke(chunk);
            
            // Logging
            if (_logSpawning)
            {
                Debug.Log($"[ObstacleSpawner] Spawned '{definition.name}' ({definition.Difficulty}) tại Z={_nextSpawnZ - definition.ChunkLength:F2}, " +
                         $"active: {_activeChunks.Count}, total: {_chunksSpawned}", this);
            }
        }
        
        /// <summary>
        /// Despawn chunk tại index
        /// </summary>
        /// <param name="index">Index trong _activeChunks</param>
        private void DespawnChunk(int index)
        {
            if (index < 0 || index >= _activeChunks.Count) return;
            
            var chunk = _activeChunks[index];
            _activeChunks.RemoveAt(index); // RemoveAt safe với reverse iteration
            
            if (chunk != null)
            {
                // Events trước khi reset
                OnChunkDespawned?.Invoke(chunk);
                
                // Reset và return về pool
                chunk.ResetChunk();
                _chunkPool.Return(chunk);
                
                _chunksDespawned++;
                
                if (_logSpawning)
                {
                    Debug.Log($"[ObstacleSpawner] Despawned chunk, active: {_activeChunks.Count}, despawned total: {_chunksDespawned}", this);
                }
            }
        }
        
        /// <summary>
        /// Pick chunk tiếp theo theo spawn rules và progression
        /// </summary>
        /// <returns>ChunkDefinition để spawn</returns>
        private ChunkDefinition PickNextChunk()
        {
            var availableChunks = GetAvailableChunks();
            if (availableChunks.Count == 0) return null;
            
            // Difficulty progression theo distance
            float currentDistance = _speedManager?.DistanceRun ?? _totalDistanceSpawned;
            
            if (currentDistance < _difficultyRampStart)
            {
                // Early game - mostly Easy
                return GetRandomChunkByDifficulty(DifficultyTag.Easy) ?? availableChunks[0];
            }
            
            // Weighted random theo distance curve
            return PickChunkWithWeightedDifficulty(availableChunks, currentDistance);
        }
        
        /// <summary>
        /// Get danh sách chunks có thể spawn (filter theo rules)
        /// </summary>
        /// <returns>List chunks available</returns>
        private List<ChunkDefinition> GetAvailableChunks()
        {
            var available = new List<ChunkDefinition>();
            float currentDistance = _speedManager?.DistanceRun ?? _totalDistanceSpawned;
            
            foreach (var chunk in _availableChunks)
            {
                if (chunk == null || chunk.ChunkPrefab == null) continue;
                
                // Check unlock distance
                if (!chunk.IsUnlocked(currentDistance)) continue;
                
                // Check spawn rules
                if (!chunk.CanSpawnAfter(_lastSpawnedDifficulty)) continue;
                
                available.Add(chunk);
            }
            
            return available;
        }
        
        /// <summary>
        /// Pick chunk với weighted difficulty progression
        /// </summary>
        /// <param name=\"availableChunks\">Chunks có thể chọn</param>
        /// <param name="currentDistance">Distance hiện tại</param>
        /// <returns>Selected chunk</returns>
        private ChunkDefinition PickChunkWithWeightedDifficulty(List<ChunkDefinition> availableChunks, float currentDistance)
        {
            // Tính weights cho từng difficulty
            float distanceKm = currentDistance / 1000f;
            float hardWeight = _difficultyWeightCurve.Evaluate(distanceKm);
            float mediumWeight = Mathf.Lerp(0.6f, 0.3f, hardWeight);
            float easyWeight = Mathf.Lerp(0.6f, 0.1f, hardWeight);
            
            // Build weighted lists
            var weightedChunks = new List<ChunkDefinition>();
            var weights = new List<float>();
            
            foreach (var chunk in availableChunks)
            {
                float weight = chunk.Difficulty switch
                {
                    DifficultyTag.Easy => easyWeight * chunk.SpawnWeight,
                    DifficultyTag.Medium => mediumWeight * chunk.SpawnWeight,
                    DifficultyTag.Hard => hardWeight * chunk.SpawnWeight,
                    _ => chunk.SpawnWeight
                };
                
                if (weight > 0f)
                {
                    weightedChunks.Add(chunk);
                    weights.Add(weight);
                }
            }
            
            // Weighted selection
            if (weightedChunks.Count > 0)
            {
                return RandomUtility.WeightedChoice(ref _prng, weightedChunks.ToArray(), weights.ToArray());
            }
            
            // Fallback - random từ available
            return _prng.Choose(availableChunks.ToArray());
        }
        
        /// <summary>
        /// Get random chunk theo difficulty cụ thể
        /// </summary>
        /// <param name="difficulty">Difficulty cần</param>
        /// <returns>Random chunk với difficulty đó</returns>
        private ChunkDefinition GetRandomChunkByDifficulty(DifficultyTag difficulty)
        {
            var matching = new List<ChunkDefinition>();
            
            foreach (var chunk in _availableChunks)
            {
                if (chunk != null && chunk.Difficulty == difficulty && chunk.ChunkPrefab != null)
                {
                    matching.Add(chunk);
                }
            }
            
            return matching.Count > 0 ? _prng.Choose(matching.ToArray()) : null;
        }
        
        /// <summary>
        /// Update debug info mỗi frame
        /// </summary>
        private void UpdateDebugInfo()
        {
            _lastFrameSpawnTime += Time.deltaTime;
        }
        
        /// <summary>
        /// Get pool statistics
        /// </summary>
        /// <returns>Pool stats string</returns>
        public string GetPoolStats()
        {
            return _chunkPool?.GetStats() ?? "Pool not initialized";
        }
        
        /// <summary>
        /// Get spawner statistics
        /// </summary>
        /// <returns>Spawner stats</returns>
        public string GetSpawnerStats()
        {
            return $"ObstacleSpawner Stats:\n" +
                   $"Active Chunks: {ActiveChunkCount}\n" +
                   $"Spawned: {_chunksSpawned}, Despawned: {_chunksDespawned}\n" +
                   $"Next Spawn Z: {_nextSpawnZ:F2}\n" +
                   $"Total Distance: {_totalDistanceSpawned:F2}m\n" +
                   $"Last Difficulty: {_lastSpawnedDifficulty}\n" +
                   $"Seed: {_seed}";
        }
        
        /// <summary>
        /// Force spawn chunk specific (debug only)
        /// </summary>
        /// <param name="chunkName">Tên chunk</param>
        public void ForceSpawnChunk(string chunkName)
        {
            if (!_debugMode) return;
            
            var chunk = Array.Find(_availableChunks, c => c?.name == chunkName);
            if (chunk != null)
            {
                SpawnChunk(chunk);
                Debug.Log($"[ObstacleSpawner] Force spawned '{chunkName}'", this);
            }
        }
        
        private void OnDisable()
        {
            // Cleanup events để tránh leaks
            OnChunkSpawned = null;
            OnChunkDespawned = null;
            OnSpawnerReset = null;
        }
        
        private void OnValidate()
        {
            // Clamp values
            _spawnDistance = Mathf.Max(30f, _spawnDistance);
            _despawnDistance = Mathf.Max(10f, _despawnDistance);
            _prewarmCount = Mathf.Max(3, _prewarmCount);
            
            // Ensure spawn distance > despawn distance
            if (_spawnDistance <= _despawnDistance)
            {
                _spawnDistance = _despawnDistance + 10f;
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Debug gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_debugMode || _playerTransform == null) return;
            
            Vector3 playerPos = _playerTransform.position;
            
            // Spawn distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerPos + Vector3.forward * _spawnDistance, 2f);
            UnityEditor.Handles.Label(playerPos + Vector3.forward * _spawnDistance + Vector3.up * 3f, "Spawn Distance");
            
            // Despawn distance (behind player)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerPos - Vector3.forward * _despawnDistance, 2f);
            UnityEditor.Handles.Label(playerPos - Vector3.forward * _despawnDistance + Vector3.up * 3f, "Despawn Distance");
            
            // Next spawn position
            Gizmos.color = Color.yellow;
            Vector3 nextSpawnPos = new Vector3(0f, 1f, _nextSpawnZ);
            Gizmos.DrawWireCube(nextSpawnPos, new Vector3(6f, 0.2f, 1f));
            UnityEditor.Handles.Label(nextSpawnPos + Vector3.up * 2f, $"Next Spawn\nZ={_nextSpawnZ:F1}");
            
            // Active chunks
            if (_activeChunks != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var chunk in _activeChunks)
                {
                    if (chunk != null)
                    {
                        Vector3 chunkCenter = chunk.transform.position + Vector3.forward * chunk.ChunkLength * 0.5f;
                        Gizmos.DrawWireCube(chunkCenter, new Vector3(6f, 0.1f, chunk.ChunkLength));
                    }
                }
            }
        }
        
#endif
    }
}
