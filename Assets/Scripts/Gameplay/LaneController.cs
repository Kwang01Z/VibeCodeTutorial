using System;
using UnityEngine;
using EndlessRunner.Core;
using EndlessRunner.Data;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Lane Controller component - quản lý chuyển làn với physics-based movement
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LaneController : MonoBehaviour, ILaneController
    {
        [Header("Configuration")]
        [SerializeField] private LaneConfig _laneConfig;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _enableDebugGizmos = false;
        
        #region Private Fields
        
        private Rigidbody _rigidbody;
        private LaneControllerState _state = LaneControllerState.Idle;
        
        // Lane state
        private int _currentLane = 1; // Start at center lane
        private int _targetLane = 1;
        private float _startXPosition;
        private float _targetXPosition;
        
        // Timers
        private Timer _laneChangeTimer;
        private Timer _cooldownTimer;
        
        // Physics backup (để restore sau lane change)
        private float _originalDrag;
        private bool _wasKinematic;
        
        #endregion
        
        #region Properties (ILaneController)
        
        public int CurrentLane => _currentLane;
        public int TargetLane => _targetLane;
        public bool IsChangingLane => _laneChangeTimer.IsRunning;
        public float LaneChangeProgress => _laneChangeTimer.Progress;
        public float CurrentXPosition => transform.position.x;
        
        public bool CanChangeLaneGeneral => 
            _state == LaneControllerState.Idle && 
            !_cooldownTimer.IsRunning && 
            _laneConfig != null &&
            enabled;
        
        #endregion
        
        #region Events (ILaneController)
        
        public event Action<int, int> OnLaneChangeStarted;
        public event Action<int> OnLaneChangeCompleted;
        public event Action<int> OnLaneChangeCancelled;
        public event Action<float, float> OnLaneChangeProgress;
        public event Action<int, string> OnLaneChangeRejected;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache rigidbody component
            _rigidbody = GetComponent<Rigidbody>();
            
            // Store original physics settings
            _originalDrag = _rigidbody.drag;
            _wasKinematic = _rigidbody.isKinematic;
            
            // Validate configuration
            if (_laneConfig == null)
            {
                Debug.LogError("[LaneController] LaneConfig is null! Please assign a LaneConfig asset.", this);
            }
        }
        
        private void Start()
        {
            // Initialize position to center lane
            if (_laneConfig != null)
            {
                SetLaneImmediate(_laneConfig.GetCenterLaneIndex());
            }
        }
        
        private void Update()
        {
            UpdateTimers();
            UpdateStateMachine();
        }
        
        private void FixedUpdate()
        {
            if (IsChangingLane)
            {
                UpdateLaneChangePhysics();
            }
        }
        
        private void OnValidate()
        {
            // Auto-assign rigidbody in editor
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }
        
        #endregion
        
        #region Public Methods (ILaneController)
        
        public bool RequestLaneChange(int targetLane)
        {
            // Validate request
            if (!ValidateLaneChangeRequest(targetLane, out string reason))
            {
                OnLaneChangeRejected?.Invoke(targetLane, reason);
                LogDebug($"Lane change rejected: {reason}");
                return false;
            }
            
            // Start lane change
            StartLaneChange(targetLane);
            return true;
        }
        
        public bool RequestLaneChangeRelative(int direction)
        {
            if (direction == 0)
            {
                OnLaneChangeRejected?.Invoke(_currentLane, "Direction cannot be zero");
                return false;
            }
            
            int targetLane = _currentLane + direction;
            return RequestLaneChange(targetLane);
        }
        
        public void CancelLaneChange()
        {
            if (!IsChangingLane) return;
            
            // Stop lane change
            _laneChangeTimer.Stop();
            _state = LaneControllerState.Idle;
            
            // Restore physics
            RestorePhysicsSettings();
            
            // Fire event
            OnLaneChangeCancelled?.Invoke(_currentLane);
            LogDebug($"Lane change cancelled, staying at lane {_currentLane}");
        }
        
        public bool CanChangeLane(int targetLane)
        {
            return ValidateLaneChangeRequest(targetLane, out _);
        }
        
        public void SetLaneImmediate(int laneIndex)
        {
            if (_laneConfig == null || !_laneConfig.IsValidLane(laneIndex))
            {
                LogDebug($"Cannot set invalid lane: {laneIndex}");
                return;
            }
            
            // Cancel any ongoing lane change
            if (IsChangingLane)
            {
                CancelLaneChange();
            }
            
            // Set position immediately
            _currentLane = laneIndex;
            _targetLane = laneIndex;
            
            Vector3 position = transform.position;
            position.x = _laneConfig.GetLanePosition(laneIndex);
            transform.position = position;
            
            LogDebug($"Set lane immediate to {laneIndex} at X={position.x}");
        }
        
        public float GetLaneXPosition(int laneIndex)
        {
            return _laneConfig?.GetLanePosition(laneIndex) ?? 0f;
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateTimers()
        {
            _laneChangeTimer.Update(Time.deltaTime);
            _cooldownTimer.Update(Time.deltaTime);
        }
        
        private void UpdateStateMachine()
        {
            switch (_state)
            {
                case LaneControllerState.Changing:
                    UpdateLaneChangeProgress();
                    
                    if (_laneChangeTimer.IsExpired)
                    {
                        CompleteLaneChange();
                    }
                    break;
                    
                case LaneControllerState.Cooldown:
                    if (_cooldownTimer.IsExpired)
                    {
                        _state = LaneControllerState.Idle;
                        LogDebug("Lane change cooldown completed");
                    }
                    break;
            }
        }
        
        private void UpdateLaneChangeProgress()
        {
            if (!IsChangingLane) return;
            
            // Calculate progress với curve
            float progress = _laneChangeTimer.Progress;
            float curveValue = _laneConfig.EvaluateLaneChangeCurve(progress);
            
            // Interpolate position
            float currentX = Mathf.Lerp(_startXPosition, _targetXPosition, curveValue);
            
            // Fire progress event
            OnLaneChangeProgress?.Invoke(progress, currentX);
        }
        
        private void UpdateLaneChangePhysics()
        {
            if (!IsChangingLane || _laneConfig == null) return;
            
            // Calculate target position với curve
            float progress = _laneChangeTimer.Progress;
            float curveValue = _laneConfig.EvaluateLaneChangeCurve(progress);
            float targetX = Mathf.Lerp(_startXPosition, _targetXPosition, curveValue);
            
            if (_laneConfig.UseForceBasedMovement)
            {
                // Physics-based movement với AddForce
                float currentX = transform.position.x;
                float deltaX = targetX - currentX;
                
                if (Mathf.Abs(deltaX) > 0.01f)
                {
                    Vector3 force = Vector3.right * (deltaX * _laneConfig.LaneChangeForce);
                    _rigidbody.AddForce(force, ForceMode.Force);
                }
            }
            else
            {
                // Direct velocity modification
                Vector3 velocity = _rigidbody.velocity;
                velocity.x = (targetX - transform.position.x) / Time.fixedDeltaTime;
                _rigidbody.velocity = velocity;
            }
        }
        
        private bool ValidateLaneChangeRequest(int targetLane, out string reason)
        {
            reason = string.Empty;
            
            // Check if enabled
            if (!enabled)
            {
                reason = "LaneController is disabled";
                return false;
            }
            
            // Check configuration
            if (_laneConfig == null)
            {
                reason = "LaneConfig is null";
                return false;
            }
            
            // Check state
            if (!CanChangeLaneGeneral)
            {
                if (_state == LaneControllerState.Changing)
                    reason = "Already changing lane";
                else if (_state == LaneControllerState.Cooldown)
                    reason = "In cooldown";
                else if (_state == LaneControllerState.Disabled)
                    reason = "Controller disabled";
                else
                    reason = "Cannot change lane in current state";
                return false;
            }
            
            // Check target lane validity
            if (!_laneConfig.IsValidLane(targetLane))
            {
                reason = $"Invalid target lane: {targetLane}";
                return false;
            }
            
            // Check if same lane
            if (targetLane == _currentLane)
            {
                reason = "Already in target lane";
                return false;
            }
            
            // Check lane change rules
            if (!_laneConfig.CanChangeLane(_currentLane, targetLane))
            {
                reason = $"Cannot change from lane {_currentLane} to {targetLane} (only adjacent lanes allowed)";
                return false;
            }
            
            return true;
        }
        
        private void StartLaneChange(int targetLane)
        {
            // Set up lane change
            _targetLane = targetLane;
            _startXPosition = transform.position.x;
            _targetXPosition = _laneConfig.GetLanePosition(targetLane);
            
            // Start timer
            _laneChangeTimer.Start(_laneConfig.LaneChangeTime);
            _state = LaneControllerState.Changing;
            
            // Apply physics settings for lane change
            if (_laneConfig.UseForceBasedMovement)
            {
                _rigidbody.drag = _laneConfig.LaneChangeDrag;
            }
            
            // Fire event
            OnLaneChangeStarted?.Invoke(_currentLane, targetLane);
            LogDebug($"Started lane change: {_currentLane} -> {targetLane} (distance: {Mathf.Abs(_targetXPosition - _startXPosition):F2})");
        }
        
        private void CompleteLaneChange()
        {
            // Update current lane
            int oldLane = _currentLane;
            _currentLane = _targetLane;
            
            // Ensure exact position
            Vector3 position = transform.position;
            position.x = _targetXPosition;
            transform.position = position;
            
            // Stop any lateral velocity
            Vector3 velocity = _rigidbody.velocity;
            velocity.x = 0f;
            _rigidbody.velocity = velocity;
            
            // Restore physics settings
            RestorePhysicsSettings();
            
            // Start cooldown
            if (_laneConfig.LaneChangeCooldown > 0f)
            {
                _cooldownTimer.Start(_laneConfig.LaneChangeCooldown);
                _state = LaneControllerState.Cooldown;
            }
            else
            {
                _state = LaneControllerState.Idle;
            }
            
            // Fire event
            OnLaneChangeCompleted?.Invoke(_currentLane);
            LogDebug($"Completed lane change: {oldLane} -> {_currentLane} at X={position.x:F2}");
        }
        
        private void RestorePhysicsSettings()
        {
            if (_rigidbody != null)
            {
                _rigidbody.drag = _originalDrag;
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[LaneController] {message}", this);
            }
        }
        
        #endregion
        
        #region Debug & Gizmos
        
        private void OnDrawGizmos()
        {
            if (!_enableDebugGizmos || _laneConfig == null) return;
            
            // Draw lane positions
            for (int i = 0; i < _laneConfig.LaneCount; i++)
            {
                float laneX = _laneConfig.GetLanePosition(i);
                Vector3 lanePosition = new Vector3(laneX, transform.position.y, transform.position.z);
                
                // Color coding
                if (i == _currentLane)
                    Gizmos.color = Color.green; // Current lane
                else if (i == _targetLane && IsChangingLane)
                    Gizmos.color = Color.yellow; // Target lane
                else
                    Gizmos.color = Color.white; // Other lanes
                
                // Draw lane line
                Gizmos.DrawLine(lanePosition + Vector3.forward * 5f, lanePosition - Vector3.forward * 5f);
                
                // Draw lane bounds
                float halfWidth = _laneConfig.LaneWidth * 0.5f;
                Gizmos.color = Color.gray;
                Vector3 leftBound = lanePosition + Vector3.left * halfWidth;
                Vector3 rightBound = lanePosition + Vector3.right * halfWidth;
                Gizmos.DrawLine(leftBound + Vector3.forward * 2f, leftBound - Vector3.forward * 2f);
                Gizmos.DrawLine(rightBound + Vector3.forward * 2f, rightBound - Vector3.forward * 2f);
            }
            
            // Draw current position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            
            // Draw lane change progress
            if (IsChangingLane)
            {
                Gizmos.color = Color.cyan;
                Vector3 startPos = new Vector3(_startXPosition, transform.position.y, transform.position.z);
                Vector3 targetPos = new Vector3(_targetXPosition, transform.position.y, transform.position.z);
                Gizmos.DrawLine(startPos, targetPos);
                
                // Draw progress indicator
                Vector3 progressPos = Vector3.Lerp(startPos, targetPos, LaneChangeProgress);
                Gizmos.DrawWireCube(progressPos, Vector3.one * 0.3f);
            }
        }
        
        #endregion
        
        #region Public Debug Methods
        
        /// <summary>
        /// Get debug status information
        /// </summary>
        /// <returns>Debug status string</returns>
        public string GetDebugStatus()
        {
            if (_laneConfig == null) return "LaneConfig is null";
            
            return $"Lane: {_currentLane}/{_laneConfig.LaneCount-1} | " +
                   $"State: {_state} | " +
                   $"Position: {transform.position.x:F2} | " +
                   $"Changing: {IsChangingLane} | " +
                   $"Progress: {LaneChangeProgress:P1} | " +
                   $"Cooldown: {_cooldownTimer.IsRunning}";
        }
        
        /// <summary>
        /// Force enable/disable controller
        /// </summary>
        /// <param name="enable">Enable state</param>
        public void SetEnabled(bool enable)
        {
            if (enable)
            {
                _state = LaneControllerState.Idle;
            }
            else
            {
                if (IsChangingLane)
                {
                    CancelLaneChange();
                }
                _state = LaneControllerState.Disabled;
            }
            
            enabled = enable;
        }
        
        #endregion
    }
}
