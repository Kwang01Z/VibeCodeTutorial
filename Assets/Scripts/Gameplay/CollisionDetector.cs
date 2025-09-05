using System;
using UnityEngine;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Data cho collision events
    /// </summary>
    [System.Serializable]
    public struct CollisionEventData
    {
        public Vector3 collisionPoint;
        public Vector3 collisionNormal;
        public Collider collider;
        public GameObject gameObject;
        public string tag;
        public int layer;
        public float relativeSpeed;
        public float timestamp;
        
        public CollisionEventData(Collider other, Vector3 point, Vector3 normal, float speed)
        {
            collider = other;
            gameObject = other.gameObject;
            tag = other.tag;
            layer = other.gameObject.layer;
            collisionPoint = point;
            collisionNormal = normal;
            relativeSpeed = speed;
            timestamp = Time.time;
        }
        
        public CollisionEventData(Collider other)
        {
            collider = other;
            gameObject = other.gameObject;
            tag = other.tag;
            layer = other.gameObject.layer;
            collisionPoint = other.transform.position;
            collisionNormal = Vector3.up;
            relativeSpeed = 0f;
            timestamp = Time.time;
        }
        
        public bool IsValid => collider != null && gameObject != null;
    }
    
    /// <summary>
    /// Collision detector component cho player.
    /// Handle collision với obstacles, pickups và other interaction objects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class CollisionDetector : MonoBehaviour
    {
        [Header("Layer Configuration")]
        [SerializeField, Tooltip("Layer mask cho obstacles")]
        private LayerMask _obstacleLayer = 1 << 8; // "Obstacle" layer
        
        [SerializeField, Tooltip("Layer mask cho pickups")]
        private LayerMask _pickupLayer = 1 << 10; // "Pickup" layer
        
        [Header("Collision Settings")]
        [SerializeField, Tooltip("Minimum thời gian giữa collision events (seconds)")]
        [Range(0f, 1f)]
        private float _collisionCooldown = 0.1f;
        
        [SerializeField, Tooltip("Maximum distance để detect collision")]
        [Range(1f, 10f)]
        private float _maxDetectionDistance = 3f;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Hiển thị debug info")]
        private bool _debugMode = true;
        
        [SerializeField, Tooltip("Draw debug gizmos")]
        private bool _showGizmos = true;
        
        // Cached components
        private IHealthSystem _healthSystem;
        private IRunnerController _runnerController;
        private ISpeedManager _speedManager;
        private Collider _triggerCollider;
        
        // Runtime state
        private float _lastCollisionTime = 0f;
        private int _obstacleCollisionCount = 0;
        private int _pickupCollisionCount = 0;
        
        // Events
        public event Action<CollisionEventData> OnObstacleHit;
        public event Action<CollisionEventData> OnPickupCollected;
        public event Action<CollisionEventData> OnAnyCollision;
        
        /// <summary>
        /// Tổng số obstacle collisions
        /// </summary>
        public int ObstacleCollisionCount => _obstacleCollisionCount;
        
        /// <summary>
        /// Tổng số pickup collisions  
        /// </summary>
        public int PickupCollisionCount => _pickupCollisionCount;
        
        /// <summary>
        /// Có trong collision cooldown không
        /// </summary>
        public bool IsInCooldown => Time.time - _lastCollisionTime < _collisionCooldown;
        
        private void Awake()
        {
            // Cache components
            _triggerCollider = GetComponent<Collider>();
            _healthSystem = GetComponent<IHealthSystem>();
            _runnerController = GetComponent<IRunnerController>();
            
            // Validate trigger collider
            if (!_triggerCollider.isTrigger)
            {
                Debug.LogWarning("[CollisionDetector] Collider should be set as Trigger", this);
                _triggerCollider.isTrigger = true;
            }
        }
        
        private void Start()
        {
            // Find SpeedManager (có thể ở GameObject khác)
            if (_speedManager == null)
            {
                _speedManager = FindObjectOfType<SpeedManager>() as ISpeedManager;
            }
            
            // Validate required components
            if (_healthSystem == null)
            {
                Debug.LogError("[CollisionDetector] IHealthSystem component not found!", this);
            }
            
            if (_runnerController == null)
            {
                Debug.LogError("[CollisionDetector] IRunnerController component not found!", this);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[CollisionDetector] Initialized - Obstacle Layer: {_obstacleLayer}, Pickup Layer: {_pickupLayer}", this);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Skip if in cooldown (for obstacle collisions)
            if (IsInCooldown && IsObstacle(other))
            {
                if (_debugMode)
                {
                    Debug.Log($"[CollisionDetector] Collision ignored due to cooldown: {other.name}", this);
                }
                return;
            }
            
            // Skip if too far away
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > _maxDetectionDistance)
            {
                return;
            }
            
            // Create collision data
            CollisionEventData collisionData = CreateCollisionData(other);
            
            // Fire general collision event
            OnAnyCollision?.Invoke(collisionData);
            
            // Route to specific handlers
            if (IsObstacle(other))
            {
                HandleObstacleCollision(collisionData);
            }
            else if (IsPickup(other))
            {
                HandlePickupCollision(collisionData);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[CollisionDetector] Collision with {other.name} (Layer: {other.gameObject.layer})", this);
            }
        }
        
        /// <summary>
        /// Handle collision với obstacle
        /// </summary>
        private void HandleObstacleCollision(CollisionEventData collisionData)
        {
            _obstacleCollisionCount++;
            _lastCollisionTime = Time.time;
            
            // Fire event trước khi apply damage
            OnObstacleHit?.Invoke(collisionData);
            
            // Apply damage through health system
            if (_healthSystem != null)
            {
                string damageSource = $"Obstacle_{collisionData.gameObject.name}";
                bool damageTaken = _healthSystem.TakeDamage(1, damageSource);
                
                if (damageTaken && _runnerController != null)
                {
                    // Trigger hit state in runner controller
                    _runnerController.PerformAction(RunnerAction.Hit);
                }
                
                if (_debugMode)
                {
                    Debug.Log($"[CollisionDetector] Obstacle hit - Damage taken: {damageTaken}, Health: {_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}", this);
                }
            }
        }
        
        /// <summary>
        /// Handle collision với pickup (placeholder cho Phase 2)
        /// </summary>
        private void HandlePickupCollision(CollisionEventData collisionData)
        {
            _pickupCollisionCount++;
            
            // Fire event
            OnPickupCollected?.Invoke(collisionData);
            
            // TODO: Phase 2 - Integrate với ItemSystem
            // For now, just log and disable the pickup
            if (_debugMode)
            {
                Debug.Log($"[CollisionDetector] Pickup collected: {collisionData.gameObject.name}", this);
            }
            
            // Disable pickup object (temporary)
            collisionData.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Kiểm tra có phải obstacle layer không
        /// </summary>
        private bool IsObstacle(Collider collider)
        {
            return (_obstacleLayer & (1 << collider.gameObject.layer)) != 0;
        }
        
        /// <summary>
        /// Kiểm tra có phải pickup layer không
        /// </summary>
        private bool IsPickup(Collider collider)
        {
            return (_pickupLayer & (1 << collider.gameObject.layer)) != 0;
        }
        
        /// <summary>
        /// Tạo collision data từ collider
        /// </summary>
        private CollisionEventData CreateCollisionData(Collider other)
        {
            // Calculate collision point (midpoint between objects)
            Vector3 collisionPoint = (transform.position + other.transform.position) * 0.5f;
            
            // Calculate collision normal (direction from other to this)
            Vector3 collisionNormal = (transform.position - other.transform.position).normalized;
            
            // Get relative speed
            float relativeSpeed = _speedManager?.CurrentSpeed ?? 0f;
            
            return new CollisionEventData(other, collisionPoint, collisionNormal, relativeSpeed);
        }
        
        /// <summary>
        /// Reset collision statistics
        /// </summary>
        public void ResetStats()
        {
            _obstacleCollisionCount = 0;
            _pickupCollisionCount = 0;
            _lastCollisionTime = 0f;
            
            if (_debugMode)
            {
                Debug.Log("[CollisionDetector] Stats reset", this);
            }
        }
        
        /// <summary>
        /// Get debug statistics
        /// </summary>
        public string GetDebugStats()
        {
            return $"Collision Stats:\n" +
                   $"Obstacles Hit: {_obstacleCollisionCount}\n" +
                   $"Pickups Collected: {_pickupCollisionCount}\n" +
                   $"In Cooldown: {IsInCooldown}\n" +
                   $"Last Collision: {Time.time - _lastCollisionTime:F2}s ago";
        }
        
        /// <summary>
        /// Force trigger collision (for testing)
        /// </summary>
        public void ForceObstacleCollision(string obstacleName = "Debug")
        {
            if (!_debugMode) return;
            
            // Create fake collision data
            var fakeCollisionData = new CollisionEventData
            {
                collisionPoint = transform.position,
                collisionNormal = Vector3.back,
                collider = null,
                gameObject = null,
                tag = "Obstacle",
                layer = LayerMask.NameToLayer("Obstacle"),
                relativeSpeed = _speedManager?.CurrentSpeed ?? 10f,
                timestamp = Time.time
            };
            
            HandleObstacleCollision(fakeCollisionData);
            
            Debug.Log($"[CollisionDetector] Forced obstacle collision: {obstacleName}", this);
        }
        
        private void OnValidate()
        {
            // Clamp values
            _collisionCooldown = Mathf.Max(0f, _collisionCooldown);
            _maxDetectionDistance = Mathf.Max(0.1f, _maxDetectionDistance);
        }
        
        private void OnDisable()
        {
            // Clear events để tránh memory leaks
            OnObstacleHit = null;
            OnPickupCollected = null;
            OnAnyCollision = null;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _maxDetectionDistance);
            
            // Draw layer indicators
            if (_debugMode)
            {
                // Obstacle detection range
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.2f);
                
                // Pickup detection range  
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.2f);
            }
        }
        
        /// <summary>
        /// Debug controls trong Inspector
        /// </summary>
        [ContextMenu("Debug: Force Obstacle Hit")]
        private void DebugForceObstacleHit()
        {
            if (Application.isPlaying)
            {
                ForceObstacleCollision("Debug_Obstacle");
            }
        }
        
        [ContextMenu("Debug: Reset Stats")]
        private void DebugResetStats()
        {
            if (Application.isPlaying)
            {
                ResetStats();
            }
        }
        
        [ContextMenu("Debug: Print Stats")]
        private void DebugPrintStats()
        {
            if (Application.isPlaying)
            {
                Debug.Log(GetDebugStats(), this);
            }
        }
#endif
    }
}
