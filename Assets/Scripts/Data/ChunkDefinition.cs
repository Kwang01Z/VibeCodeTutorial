using UnityEngine;
using EndlessRunner.Data;
using System.Collections.Generic;
using System.Linq;

namespace EndlessRunner.Data
{
    /// <summary>
    /// Định nghĩa một chunk với obstacles và metadata.
    /// Được sử dụng bởi ObstacleSpawner để tạo procedural map.
    /// </summary>
    [CreateAssetMenu(fileName = "ChunkDefinition", menuName = "EndlessRunner/Chunk Definition", order = 1)]
    public class ChunkDefinition : ScriptableObject
    {
        [Header("Basic Settings")]
        [SerializeField, Tooltip("Prefab chunk anchor (có script ChunkAnchor)")]
        private GameObject _chunkPrefab;
        
        [SerializeField, Tooltip("Chiều dài chunk theo trục Z (meter)")]
        [Range(10f, 50f)]
        private float _chunkLength = 20f;
        
        [SerializeField, Tooltip("Độ khó - ảnh hưởng spawn rules")]
        private DifficultyTag _difficulty = DifficultyTag.Easy;
        
        [Header("Obstacles")]
        [SerializeField, Tooltip("Danh sách obstacles trong chunk")]
        private ObstacleInfo[] _obstacles = new ObstacleInfo[0];
        
        [Header("Spawn Settings")]
        [SerializeField, Tooltip("Weight khi chọn chunk (càng cao càng dễ được chọn)")]
        [Range(0.1f, 10f)]
        private float _spawnWeight = 1f;
        
        [SerializeField, Tooltip("Distance tối thiểu để unlock chunk này")]
        [Range(0f, 1000f)]
        private float _unlockDistance = 0f;
        
        [Header("Validation")]
        [SerializeField, Tooltip("Khoảng cách tối thiểu giữa obstacles cùng lane (m)")]
        [Range(0.1f, 2f)]
        private float _obstacleMinDistance = 0.2f;
        
        [SerializeField, Tooltip("Có debug validation trong scene view không")]
        private bool _debugValidation = true;
        
        // Cached validation results
        [System.NonSerialized]
        private ValidationResult _cachedValidation;
        [System.NonSerialized]
        private bool _validationCacheValid = false;
        
        /// <summary>
        /// Prefab để spawn chunk này
        /// </summary>
        public GameObject ChunkPrefab => _chunkPrefab;
        
        /// <summary>
        /// Chiều dài chunk (meter)
        /// </summary>
        public float ChunkLength => _chunkLength;
        
        /// <summary>
        /// Độ khó của chunk
        /// </summary>
        public DifficultyTag Difficulty => _difficulty;
        
        /// <summary>
        /// Obstacles trong chunk
        /// </summary>
        public ObstacleInfo[] Obstacles => _obstacles;
        
        /// <summary>
        /// Weight cho spawn selection
        /// </summary>
        public float SpawnWeight => _spawnWeight;
        
        /// <summary>
        /// Distance cần để unlock chunk
        /// </summary>
        public float UnlockDistance => _unlockDistance;
        
        /// <summary>
        /// Khoảng cách tối thiểu giữa obstacles
        /// </summary>
        public float ObstacleMinDistance => _obstacleMinDistance;
        
        /// <summary>
        /// Validation result structure
        /// </summary>
        [System.Serializable]
        public struct ValidationResult
        {
            public bool isValid;
            public string[] errors;
            public string[] warnings;
            public int conflictCount;
            
            public ValidationResult(bool valid, string[] errors = null, string[] warnings = null, int conflicts = 0)
            {
                isValid = valid;
                this.errors = errors ?? new string[0];
                this.warnings = warnings ?? new string[0];
                conflictCount = conflicts;
            }
        }
        
        /// <summary>
        /// Validate chunk definition và trả về kết quả
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult Validate()
        {
            if (_validationCacheValid)
                return _cachedValidation;
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // 1. Kiểm tra basic settings
            if (_chunkPrefab == null)
                errors.Add("Chunk Prefab không được null");
            
            if (_chunkLength <= 0)
                errors.Add($"Chunk Length phải > 0, hiện tại: {_chunkLength}");
            
            if (_spawnWeight <= 0)
                warnings.Add($"Spawn Weight = 0 có nghĩa chunk này sẽ không bao giờ được spawn");
            
            // 2. Kiểm tra obstacles
            int conflictCount = 0;
            if (_obstacles != null && _obstacles.Length > 0)
            {
                // Kiểm tra obstacle validity
                for (int i = 0; i < _obstacles.Length; i++)
                {
                    var obstacle = _obstacles[i];
                    
                    if (!obstacle.IsValid)
                    {
                        errors.Add($"Obstacle[{i}]: Invalid - prefab null hoặc lane không hợp lệ");
                        continue;
                    }
                    
                    // Kiểm tra position nằm trong chunk bounds
                    if (obstacle.localPosition.z < 0 || obstacle.localPosition.z > _chunkLength)
                    {
                        warnings.Add($"Obstacle[{i}]: Position Z ({obstacle.localPosition.z:F2}) nằm ngoài chunk bounds [0, {_chunkLength}]");
                    }
                    
                    // Kiểm tra conflicts với obstacles khác
                    for (int j = i + 1; j < _obstacles.Length; j++)
                    {
                        var otherObstacle = _obstacles[j];
                        
                        if (obstacle.ConflictsWith(otherObstacle, _obstacleMinDistance))
                        {
                            errors.Add($"Obstacle[{i}] conflicts với Obstacle[{j}]: cùng lane và gần nhau < {_obstacleMinDistance}m");
                            conflictCount++;
                        }
                    }
                }
                
                // Kiểm tra density warning
                float density = _obstacles.Length / _chunkLength;
                if (density > 0.5f) // Hơn 0.5 obstacles/meter
                {
                    warnings.Add($"Obstacle density cao ({density:F2}/m) có thể tạo ra combo không thể né");
                }
            }
            
            // 3. Kiểm tra difficulty consistency
            switch (_difficulty)
            {
                case DifficultyTag.Easy:
                    if (_obstacles != null && _obstacles.Length > 3)
                        warnings.Add("Easy chunk có quá nhiều obstacles (>3)");
                    break;
                    
                case DifficultyTag.Hard:
                    if (_obstacles == null || _obstacles.Length < 2)
                        warnings.Add("Hard chunk nên có ít nhất 2 obstacles");
                    break;
            }
            
            bool isValid = errors.Count == 0;
            _cachedValidation = new ValidationResult(isValid, errors.ToArray(), warnings.ToArray(), conflictCount);
            _validationCacheValid = true;
            
            return _cachedValidation;
        }
        
        /// <summary>
        /// Invalidate validation cache (gọi khi có thay đổi)
        /// </summary>
        public void InvalidateValidation()
        {
            _validationCacheValid = false;
        }
        
        /// <summary>
        /// Lấy obstacles theo lane cụ thể
        /// </summary>
        /// <param name="laneIndex">Lane index (0=Left, 1=Center, 2=Right)</param>
        /// <returns>Array obstacles trong lane đó</returns>
        public ObstacleInfo[] GetObstaclesInLane(int laneIndex)
        {
            if (_obstacles == null) return new ObstacleInfo[0];
            
            return _obstacles.Where(obs => obs.requiredLane == laneIndex || obs.requiredLane == -1).ToArray();
        }
        
        /// <summary>
        /// Đếm obstacles theo difficulty breakdown
        /// </summary>
        /// <returns>Dictionary với counts</returns>
        public Dictionary<string, int> GetObstacleStats()
        {
            var stats = new Dictionary<string, int>
            {
                ["Total"] = _obstacles?.Length ?? 0,
                ["Lane0"] = 0,
                ["Lane1"] = 0,
                ["Lane2"] = 0,
                ["AnyLane"] = 0
            };
            
            if (_obstacles != null)
            {
                foreach (var obs in _obstacles)
                {
                    switch (obs.requiredLane)
                    {
                        case 0: stats["Lane0"]++; break;
                        case 1: stats["Lane1"]++; break;
                        case 2: stats["Lane2"]++; break;
                        case -1: stats["AnyLane"]++; break;
                    }
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// Kiểm tra chunk có thể spawn sau lastDifficulty không (theo spawn rules)
        /// </summary>
        /// <param name="lastDifficulty">Difficulty của chunk trước đó</param>
        /// <returns>True nếu có thể spawn</returns>
        public bool CanSpawnAfter(DifficultyTag lastDifficulty)
        {
            // Rule: Hard chunk không thể spawn liên tiếp
            return !(lastDifficulty == DifficultyTag.Hard && _difficulty == DifficultyTag.Hard);
        }
        
        /// <summary>
        /// Kiểm tra chunk đã unlock chưa theo distance
        /// </summary>
        /// <param name="currentDistance">Distance hiện tại</param>
        /// <returns>True nếu đã unlock</returns>
        public bool IsUnlocked(float currentDistance)
        {
            return currentDistance >= _unlockDistance;
        }
        
        /// <summary>
        /// Unity callback - validate khi có thay đổi trong Inspector
        /// </summary>
        private void OnValidate()
        {
            // Clamp values
            _chunkLength = Mathf.Max(1f, _chunkLength);
            _spawnWeight = Mathf.Max(0f, _spawnWeight);
            _unlockDistance = Mathf.Max(0f, _unlockDistance);
            _obstacleMinDistance = Mathf.Max(0.1f, _obstacleMinDistance);
            
            // Invalidate cache
            InvalidateValidation();
            
            // Auto-validate if debug enabled
            if (_debugValidation && Application.isPlaying)
            {
                var result = Validate();
                if (!result.isValid)
                {
                    Debug.LogWarning($"[ChunkDefinition] {name} có validation errors: {string.Join(", ", result.errors)}", this);
                }
            }
        }
        
        /// <summary>
        /// Debug info cho Inspector
        /// </summary>
        /// <returns>Info string</returns>
        public string GetDebugInfo()
        {
            var validation = Validate();
            var stats = GetObstacleStats();
            
            return $"Chunk: {name}\n" +
                   $"Length: {_chunkLength}m, Difficulty: {_difficulty}\n" +
                   $"Obstacles: {stats["Total"]} (L{stats["Lane0"]}/C{stats["Lane1"]}/R{stats["Lane2"]}/Any{stats["AnyLane"]})\n" +
                   $"Valid: {validation.isValid}, Errors: {validation.errors.Length}, Warnings: {validation.warnings.Length}\n" +
                   $"Weight: {_spawnWeight}, Unlock: {_unlockDistance}m";
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Draw gizmos cho visualization (chỉ khi select asset)
        /// </summary>
        private void OnDrawGizmos()
        {
            // ScriptableObject không có transform, gizmos chỉ vẽ khi được select
            // Hoặc khi được reference từ MonoBehaviour có transform
        }
        
        /// <summary>
        /// Kiểm tra obstacle tại index có conflict không
        /// </summary>
        /// <param name="index">Index của obstacle</param>
        /// <returns>True nếu có conflict</returns>
        private bool HasConflictAtIndex(int index)
        {
            if (_obstacles == null || index < 0 || index >= _obstacles.Length)
                return false;
            
            var target = _obstacles[index];
            for (int i = 0; i < _obstacles.Length; i++)
            {
                if (i != index && target.ConflictsWith(_obstacles[i], _obstacleMinDistance))
                    return true;
            }
            
            return false;
        }
#endif
    }
}
