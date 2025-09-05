using UnityEngine;

namespace EndlessRunner.Camera
{
    /// <summary>
    /// Camera follow system cho endless runner game
    /// Smooth tracking với customizable offset và damping
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField, Tooltip("Player transform để follow")]
        private Transform _target;
        
        [Header("Position Settings")]
        [SerializeField, Tooltip("Offset position từ target")]
        private Vector3 _offset = new Vector3(0f, 4f, -5f);
        
        [SerializeField, Tooltip("Tốc độ follow position")]
        [Range(0.1f, 10f)]
        private float _followSpeed = 5f;
        
        [Header("Rotation Settings")]
        [SerializeField, Tooltip("Camera có tự động look at target không")]
        private bool _lookAtTarget = true;
        
        [SerializeField, Tooltip("Tốc độ rotation smoothing")]
        [Range(0.1f, 10f)]
        private float _rotationSpeed = 3f;
        
        [SerializeField, Tooltip("Fixed rotation nếu không look at target")]
        private Vector3 _fixedRotation = new Vector3(20f, 0f, 0f);
        
        [Header("Constraints")]
        [SerializeField, Tooltip("Giới hạn movement theo X axis")]
        private bool _constrainX = false;
        
        [SerializeField, Tooltip("Min/Max X position")]
        private Vector2 _xConstraint = new Vector2(-5f, 5f);
        
        [SerializeField, Tooltip("Giới hạn movement theo Y axis")]
        private bool _constrainY = false;
        
        [SerializeField, Tooltip("Min/Max Y position")]
        private Vector2 _yConstraint = new Vector2(1f, 10f);
        
        [Header("Smoothing")]
        [SerializeField, Tooltip("Sử dụng smooth damping thay vì lerp")]
        private bool _useSmoothDamping = true;
        
        [SerializeField, Tooltip("Thời gian smooth damping")]
        [Range(0.1f, 2f)]
        private float _dampTime = 0.3f;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Hiển thị debug info")]
        private bool _debugMode = false;
        
        [SerializeField, Tooltip("Hiển thị gizmos")]
        private bool _showGizmos = true;
        
        // Runtime variables
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _targetPosition;
        private Vector3 _currentPosition;
        
        // Auto-find target nếu không được assign
        private void Start()
        {
            ValidateTarget();
            
            // Set initial position nếu có target
            if (_target != null)
            {
                _targetPosition = _target.position + _offset;
                transform.position = _targetPosition;
                
                if (_lookAtTarget)
                {
                    transform.LookAt(_target);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(_fixedRotation);
                }
            }
            
            if (_debugMode)
            {
                Debug.Log($"[CameraFollow] Initialized - Target: {(_target ? _target.name : "None")}", this);
            }
        }
        
        private void LateUpdate()
        {
            if (_target == null)
            {
                if (_debugMode && Time.frameCount % 60 == 0) // Log mỗi giây
                {
                    Debug.LogWarning("[CameraFollow] No target assigned", this);
                }
                return;
            }
            
            UpdatePosition();
            UpdateRotation();
        }
        
        /// <summary>
        /// Cập nhật camera position
        /// </summary>
        private void UpdatePosition()
        {
            // Calculate target position
            _targetPosition = _target.position + _offset;
            
            // Apply constraints
            if (_constrainX)
            {
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, _xConstraint.x, _xConstraint.y);
            }
            
            if (_constrainY)
            {
                _targetPosition.y = Mathf.Clamp(_targetPosition.y, _yConstraint.x, _yConstraint.y);
            }
            
            // Apply smoothing
            if (_useSmoothDamping)
            {
                _currentPosition = Vector3.SmoothDamp(
                    transform.position, 
                    _targetPosition, 
                    ref _velocity, 
                    _dampTime
                );
            }
            else
            {
                _currentPosition = Vector3.Lerp(
                    transform.position, 
                    _targetPosition, 
                    _followSpeed * Time.deltaTime
                );
            }
            
            transform.position = _currentPosition;
        }
        
        /// <summary>
        /// Cập nhật camera rotation
        /// </summary>
        private void UpdateRotation()
        {
            if (_lookAtTarget)
            {
                // Smooth look at target
                Vector3 directionToTarget = (_target.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    _rotationSpeed * Time.deltaTime
                );
            }
            else
            {
                // Fixed rotation
                Quaternion targetRotation = Quaternion.Euler(_fixedRotation);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    _rotationSpeed * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// Validate và auto-find target
        /// </summary>
        private void ValidateTarget()
        {
            if (_target == null)
            {
                // Try to find player by tag
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                    if (_debugMode)
                    {
                        Debug.Log($"[CameraFollow] Auto-found target: {_target.name}", this);
                    }
                }
                else if (_debugMode)
                {
                    Debug.LogWarning("[CameraFollow] No Player tag found, assign target manually", this);
                }
            }
        }
        
        /// <summary>
        /// Set target runtime
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            
            if (_debugMode)
            {
                Debug.Log($"[CameraFollow] Target set to: {(_target ? _target.name : "None")}", this);
            }
        }
        
        /// <summary>
        /// Set offset runtime
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            _offset = newOffset;
            
            if (_debugMode)
            {
                Debug.Log($"[CameraFollow] Offset set to: {_offset}", this);
            }
        }
        
        /// <summary>
        /// Set follow speed runtime
        /// </summary>
        public void SetFollowSpeed(float speed)
        {
            _followSpeed = Mathf.Max(0.1f, speed);
            
            if (_debugMode)
            {
                Debug.Log($"[CameraFollow] Follow speed set to: {_followSpeed}", this);
            }
        }
        
        /// <summary>
        /// Instant snap to target position
        /// </summary>
        [ContextMenu("Snap to Target")]
        public void SnapToTarget()
        {
            if (_target != null)
            {
                transform.position = _target.position + _offset;
                
                if (_lookAtTarget)
                {
                    transform.LookAt(_target);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(_fixedRotation);
                }
                
                if (_debugMode)
                {
                    Debug.Log("[CameraFollow] Snapped to target position", this);
                }
            }
        }
        
        /// <summary>
        /// Reset to default settings
        /// </summary>
        [ContextMenu("Reset Settings")]
        public void ResetSettings()
        {
            _offset = new Vector3(0f, 4f, -5f);
            _followSpeed = 5f;
            _rotationSpeed = 3f;
            _lookAtTarget = true;
            _useSmoothDamping = true;
            _dampTime = 0.3f;
            
            if (_debugMode)
            {
                Debug.Log("[CameraFollow] Settings reset to default", this);
            }
        }
        
        private void OnValidate()
        {
            // Clamp values trong editor
            _followSpeed = Mathf.Max(0.1f, _followSpeed);
            _rotationSpeed = Mathf.Max(0.1f, _rotationSpeed);
            _dampTime = Mathf.Max(0.1f, _dampTime);
            
            // Validate constraints
            if (_xConstraint.x > _xConstraint.y)
            {
                _xConstraint.y = _xConstraint.x;
            }
            
            if (_yConstraint.x > _yConstraint.y)
            {
                _yConstraint.y = _yConstraint.x;
            }
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showGizmos || _target == null) return;
            
            // Draw connection line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _target.position);
            
            // Draw target position
            Vector3 targetPos = _target.position + _offset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(targetPos, Vector3.one * 0.3f);
            
            // Draw constraints nếu enabled
            if (_constrainX)
            {
                Gizmos.color = Color.red;
                Vector3 minX = new Vector3(_xConstraint.x, targetPos.y, targetPos.z);
                Vector3 maxX = new Vector3(_xConstraint.y, targetPos.y, targetPos.z);
                Gizmos.DrawLine(minX + Vector3.up, minX - Vector3.up);
                Gizmos.DrawLine(maxX + Vector3.up, maxX - Vector3.up);
                Gizmos.DrawLine(minX, maxX);
            }
            
            if (_constrainY)
            {
                Gizmos.color = Color.blue;
                Vector3 minY = new Vector3(targetPos.x, _yConstraint.x, targetPos.z);
                Vector3 maxY = new Vector3(targetPos.x, _yConstraint.y, targetPos.z);
                Gizmos.DrawLine(minY + Vector3.right, minY - Vector3.right);
                Gizmos.DrawLine(maxY + Vector3.right, maxY - Vector3.right);
                Gizmos.DrawLine(minY, maxY);
            }
            
            // Draw look direction
            if (_lookAtTarget)
            {
                Gizmos.color = Color.cyan;
                Vector3 lookDirection = (_target.position - transform.position).normalized * 2f;
                Gizmos.DrawRay(transform.position, lookDirection);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            
            // Draw offset visualization
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawDottedLine(_target.position, _target.position + _offset, 2f);
            
            UnityEditor.Handles.Label(
                _target.position + _offset + Vector3.up * 0.5f, 
                $"Offset: {_offset}"
            );
        }
#endif
    }
}
