using UnityEngine;

namespace EndlessRunner.Testing
{
    /// <summary>
    /// Simple player controller cho collision testing
    /// Cho phép di chuyển player bằng WASD/Arrow keys
    /// </summary>
    [AddComponentMenu("EndlessRunner/Testing/Test Player Controller")]
    public class TestPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField, Tooltip("Movement speed")]
        private float _moveSpeed = 5f;
        
        [SerializeField, Tooltip("Rotation speed when turning")]
        private float _rotationSpeed = 360f;
        
        [Header("Movement Bounds")]
        [SerializeField, Tooltip("Maximum X position (left/right)")]
        private float _maxX = 10f;
        
        [SerializeField, Tooltip("Maximum Z position (forward/backward)")]
        private float _maxZ = 15f;
        
        [SerializeField, Tooltip("Minimum Z position")]
        private float _minZ = -5f;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Show movement debug info")]
        private bool _showDebugInfo = true;
        
        [SerializeField, Tooltip("Show movement bounds gizmos")]
        private bool _showBounds = true;
        
        // Runtime state
        private Vector3 _targetPosition;
        private Vector3 _moveDirection;
        private bool _isMoving = false;
        
        // Input tracking
        private bool _leftPressed;
        private bool _rightPressed;
        private bool _upPressed;
        private bool _downPressed;
        
        /// <summary>
        /// Current movement direction
        /// </summary>
        public Vector3 MoveDirection => _moveDirection;
        
        /// <summary>
        /// Is player currently moving
        /// </summary>
        public bool IsMoving => _isMoving;
        
        /// <summary>
        /// Current movement speed
        /// </summary>
        public float CurrentSpeed => _moveSpeed;
        
        private void Start()
        {
            _targetPosition = transform.position;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[TestPlayerController] Initialized at position {transform.position}");
                LogControls();
            }
        }
        
        private void Update()
        {
            HandleInput();
            UpdateMovement();
            UpdateRotation();
            
            // Debug info
            if (_showDebugInfo && _isMoving)
            {
                Debug.DrawRay(transform.position, _moveDirection * 2f, Color.cyan);
            }
        }
        
        /// <summary>
        /// Handle keyboard input
        /// </summary>
        private void HandleInput()
        {
            // WASD và Arrow Keys
            _leftPressed = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            _rightPressed = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            _upPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            _downPressed = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            
            // Calculate movement direction
            Vector3 inputDirection = Vector3.zero;
            
            if (_leftPressed) inputDirection.x -= 1f;
            if (_rightPressed) inputDirection.x += 1f;
            if (_upPressed) inputDirection.z += 1f;
            if (_downPressed) inputDirection.z -= 1f;
            
            // Normalize diagonal movement
            if (inputDirection.magnitude > 1f)
            {
                inputDirection.Normalize();
            }
            
            _moveDirection = inputDirection;
            _isMoving = inputDirection.magnitude > 0f;
        }
        
        /// <summary>
        /// Update player position
        /// </summary>
        private void UpdateMovement()
        {
            if (_isMoving)
            {
                // Calculate new position
                Vector3 deltaMove = _moveDirection * _moveSpeed * Time.deltaTime;
                Vector3 newPosition = transform.position + deltaMove;
                
                // Apply bounds
                newPosition.x = Mathf.Clamp(newPosition.x, -_maxX, _maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, _minZ, _maxZ);
                
                // Apply movement
                transform.position = newPosition;
                _targetPosition = newPosition;
            }
        }
        
        /// <summary>
        /// Update player rotation to face movement direction
        /// </summary>
        private void UpdateRotation()
        {
            if (_isMoving && _moveDirection.magnitude > 0.1f)
            {
                // Calculate target rotation
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                
                // Smooth rotation
                float rotationStep = _rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
            }
        }
        
        /// <summary>
        /// Set player position (for testing)
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -_maxX, _maxX);
            position.z = Mathf.Clamp(position.z, _minZ, _maxZ);
            
            transform.position = position;
            _targetPosition = position;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[TestPlayerController] Position set to {position}");
            }
        }
        
        /// <summary>
        /// Move to specific position smoothly
        /// </summary>
        public void MoveTo(Vector3 targetPosition)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -_maxX, _maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, _minZ, _maxZ);
            
            _targetPosition = targetPosition;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[TestPlayerController] Moving to {targetPosition}");
            }
        }
        
        /// <summary>
        /// Reset player to origin
        /// </summary>
        public void ResetPosition()
        {
            SetPosition(Vector3.zero);
        }
        
        /// <summary>
        /// Enable/disable movement
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled)
            {
                _moveDirection = Vector3.zero;
                _isMoving = false;
            }
        }
        
        /// <summary>
        /// Log control instructions
        /// </summary>
        private void LogControls()
        {
            Debug.Log("<color=cyan>[TEST PLAYER CONTROLS]</color>\n" +
                      "• WASD or Arrow Keys: Move player\n" +
                      "• Player rotates to face movement direction\n" +
                      "• Movement is bounded within the test area\n" +
                      "• Collide with red obstacles to test collision detection\n" +
                      "• Collect green pickups to test pickup system");
        }
        
        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Position: {transform.position:F2}\n" +
                   $"Direction: {_moveDirection:F2}\n" +
                   $"IsMoving: {_isMoving}\n" +
                   $"Speed: {_moveSpeed}\n" +
                   $"Target: {_targetPosition:F2}";
        }
        
        private void OnValidate()
        {
            // Clamp values
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            _maxX = Mathf.Max(1f, _maxX);
            _maxZ = Mathf.Max(1f, _maxZ);
            _minZ = Mathf.Min(-1f, _minZ);
        }
        
        private void OnDisable()
        {
            // Reset state when disabled
            _moveDirection = Vector3.zero;
            _isMoving = false;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Draw movement bounds và debug info trong Scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showBounds) return;
            
            // Draw movement bounds
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(0, transform.position.y, (_maxZ + _minZ) * 0.5f);
            Vector3 size = new Vector3(_maxX * 2f, 0.1f, _maxZ - _minZ);
            Gizmos.DrawWireCube(center, size);
            
            // Draw current position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw movement direction
            if (_isMoving && Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, _moveDirection * 2f);
            }
            
            // Draw target position if different
            if (Vector3.Distance(transform.position, _targetPosition) > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_targetPosition, 0.3f);
                Gizmos.DrawLine(transform.position, _targetPosition);
            }
        }
        
        /// <summary>
        /// Context menu items cho testing
        /// </summary>
        [ContextMenu("Reset Position")]
        private void ContextResetPosition()
        {
            if (Application.isPlaying)
            {
                ResetPosition();
            }
            else
            {
                transform.position = Vector3.zero;
            }
        }
        
        [ContextMenu("Move to Obstacles")]
        private void ContextMoveToObstacles()
        {
            if (Application.isPlaying)
            {
                SetPosition(new Vector3(0, 0, 5f)); // Move towards obstacles
            }
        }
        
        [ContextMenu("Move to Pickups")]
        private void ContextMoveToPickups()
        {
            if (Application.isPlaying)
            {
                SetPosition(new Vector3(0, 0, 10f)); // Move towards pickups
            }
        }
        
        [ContextMenu("Print Debug Info")]
        private void ContextPrintDebugInfo()
        {
            if (Application.isPlaying)
            {
                Debug.Log($"[TestPlayerController]\n{GetDebugInfo()}");
            }
        }
#endif
    }
}
