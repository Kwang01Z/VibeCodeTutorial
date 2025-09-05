using System;
using System.Collections.Generic;
using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// SpeedManager - Physics-based speed progression system
    /// Quản lý tốc độ runner theo distance với smooth transitions
    /// </summary>
    public class SpeedManager : MonoBehaviour, ISpeedManager
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private SpeedCurve _speedConfig;
        [SerializeField] private Rigidbody _rigidbody;
        
        [Header("Milestones")]
        [SerializeField] private float[] _distanceMilestones = { 100f, 250f, 500f, 1000f, 2000f };
        [SerializeField] private float[] _speedTiers = { 10f, 12f, 15f, 18f, 20f };
        
        [Header("Settings")]
        [SerializeField] private bool _autoStartProgression = false;
        [SerializeField] private float _speedChangeThreshold = 0.1f;
        [SerializeField] private Vector3 _forwardDirection = Vector3.forward;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = false;
        [SerializeField] private bool _drawGizmos = false;
        
        #endregion
        
        #region Private Fields
        
        private float _currentSpeed;
        private float _targetSpeed;
        private float _distanceRun;
        private float _timeRunning;
        private bool _isPaused;
        private bool _isRunning;
        
        // Speed modifier system
        private float _speedModifier = 1f;
        private Timer _speedModifierTimer;
        private string _speedModifierReason = "";
        
        // Fixed speed override
        private bool _hasFixedSpeed;
        private float _fixedSpeed;
        private Timer _fixedSpeedTimer;
        
        // Milestone tracking
        private HashSet<float> _reachedMilestones = new HashSet<float>();
        private HashSet<int> _reachedSpeedTiers = new HashSet<int>();
        
        // Cached values
        private Vector3 _lastPosition;
        private float _lastSpeed;
        
        #endregion
        
        #region Properties (ISpeedManager)
        
        public float CurrentSpeed => _currentSpeed;
        public float TargetSpeed => _targetSpeed;
        public float DistanceRun => _distanceRun;
        public float TimeRunning => _timeRunning;
        public bool IsPaused => _isPaused;
        public float SpeedModifier => _speedModifier;
        
        #endregion
        
        #region Events (ISpeedManager)
        
        public event Action<float, float, float> OnSpeedChanged;
        public event Action<float, float> OnDistanceMilestone;
        public event Action<int, float> OnSpeedTierReached;
        public event Action<bool, float, float> OnProgressionStateChanged;
        public event Action<float, float, string> OnSpeedModifierApplied;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Validate configuration
            ValidateConfiguration();
            
            // Initialize state
            ResetProgression();
            
            // Cache initial position
            _lastPosition = transform.position;
            
            LogDebug("[SpeedManager] Initialized");
        }
        
        private void Start()
        {
            if (_autoStartProgression)
            {
                StartProgression();
            }
        }
        
        private void Update()
        {
            if (!_isRunning || _isPaused) return;
            
            // Update timers
            UpdateTimers();
            
            // Update time running
            _timeRunning += Time.deltaTime;
        }
        
        private void FixedUpdate()
        {
            if (!_isRunning || _isPaused) return;
            
            // Update distance và speed
            UpdateDistance();
            UpdateSpeed();
            ApplySpeed();
            
            // Check milestones
            CheckMilestones();
        }
        
        #endregion
        
        #region Control Methods (ISpeedManager)
        
        public void StartProgression()
        {
            _isRunning = true;
            _isPaused = false;
            
            LogDebug("[SpeedManager] Speed progression started");
            OnProgressionStateChanged?.Invoke(true, _distanceRun, _currentSpeed);
        }
        
        public void PauseProgression()
        {
            _isPaused = true;
            
            LogDebug("[SpeedManager] Speed progression paused");
            OnProgressionStateChanged?.Invoke(false, _distanceRun, _currentSpeed);
        }
        
        public void ResumeProgression()
        {
            if (!_isRunning) return;
            
            _isPaused = false;
            
            // Update last position để tránh distance jump
            _lastPosition = transform.position;
            
            LogDebug("[SpeedManager] Speed progression resumed");
            OnProgressionStateChanged?.Invoke(true, _distanceRun, _currentSpeed);
        }
        
        public void ResetProgression()
        {
            _currentSpeed = _speedConfig != null ? _speedConfig.BaseSpeed : 8f;
            _targetSpeed = _currentSpeed;
            _distanceRun = 0f;
            _timeRunning = 0f;
            _isPaused = false;
            _isRunning = false;
            
            // Reset modifiers
            _speedModifier = 1f;
            _speedModifierTimer.Stop();
            _hasFixedSpeed = false;
            _fixedSpeedTimer.Stop();
            
            // Reset milestone tracking
            _reachedMilestones.Clear();
            _reachedSpeedTiers.Clear();
            
            // Cache position
            _lastPosition = transform.position;
            _lastSpeed = _currentSpeed;
            
            LogDebug("[SpeedManager] Speed progression reset");
            OnProgressionStateChanged?.Invoke(false, _distanceRun, _currentSpeed);
        }
        
        public void SetSpeedModifier(float modifier, float duration = 0f)
        {
            _speedModifier = Mathf.Max(0.1f, modifier); // Minimum 10% speed
            _speedModifierReason = $"Modifier: {modifier:F2}";
            
            if (duration > 0f)
            {
                _speedModifierTimer.Start(duration);
            }
            else
            {
                _speedModifierTimer.Stop();
            }
            
            LogDebug($"[SpeedManager] Speed modifier set: {modifier:F2} for {duration:F1}s");
            OnSpeedModifierApplied?.Invoke(modifier, duration, _speedModifierReason);
        }
        
        public void ClearSpeedModifier()
        {
            _speedModifier = 1f;
            _speedModifierTimer.Stop();
            _speedModifierReason = "";
            
            LogDebug("[SpeedManager] Speed modifier cleared");
            OnSpeedModifierApplied?.Invoke(1f, 0f, "Cleared");
        }
        
        public void SetFixedSpeed(float speed, float duration)
        {
            _fixedSpeed = Mathf.Max(0.1f, speed);
            _hasFixedSpeed = true;
            _fixedSpeedTimer.Start(duration);
            
            LogDebug($"[SpeedManager] Fixed speed set: {speed:F1} for {duration:F1}s");
        }
        
        #endregion
        
        #region Query Methods (ISpeedManager)
        
        public float GetSpeedAtDistance(float distance)
        {
            if (_speedConfig == null) return 8f;
            return _speedConfig.EvaluateSpeed(distance);
        }
        
        public float GetProgressPercent()
        {
            if (_speedConfig == null) return 0f;
            return _speedConfig.GetCurveValueAtDistance(_distanceRun);
        }
        
        public bool HasReachedMilestone(float milestone)
        {
            return _reachedMilestones.Contains(milestone);
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateTimers()
        {
            _speedModifierTimer.Update(Time.deltaTime);
            _fixedSpeedTimer.Update(Time.deltaTime);
            
            // Check timer expiry
            if (_speedModifierTimer.IsExpired)
            {
                ClearSpeedModifier();
            }
            
            if (_fixedSpeedTimer.IsExpired && _hasFixedSpeed)
            {
                _hasFixedSpeed = false;
                LogDebug("[SpeedManager] Fixed speed expired");
            }
        }
        
        private void UpdateDistance()
        {
            if (_rigidbody == null) return;
            
            // Calculate distance from movement
            Vector3 currentPosition = transform.position;
            Vector3 movement = currentPosition - _lastPosition;
            
            // Project movement onto forward direction
            float distanceDelta = Vector3.Dot(movement, _forwardDirection.normalized);
            
            // Only count forward movement
            if (distanceDelta > 0f)
            {
                _distanceRun += distanceDelta;
            }
            
            _lastPosition = currentPosition;
        }
        
        private void UpdateSpeed()
        {
            if (_speedConfig == null) return;
            
            // Determine target speed
            if (_hasFixedSpeed)
            {
                _targetSpeed = _fixedSpeed;
            }
            else
            {
                _targetSpeed = _speedConfig.EvaluateSpeed(_distanceRun);
            }
            
            // Apply speed modifier
            _targetSpeed *= _speedModifier;
            
            // Smooth transition to target speed
            float smoothness = _speedConfig.AccelerationSmoothness;
            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, 
                smoothness * Time.fixedDeltaTime);
            
            // Fire speed change event if significant change
            if (Mathf.Abs(_currentSpeed - _lastSpeed) > _speedChangeThreshold)
            {
                OnSpeedChanged?.Invoke(_currentSpeed, _lastSpeed, _targetSpeed);
                _lastSpeed = _currentSpeed;
            }
        }
        
        private void ApplySpeed()
        {
            if (_rigidbody == null) return;
            
            // Apply speed to rigidbody
            Vector3 targetVelocity = _forwardDirection.normalized * _currentSpeed;
            
            // Keep Y velocity (gravity/jump)
            targetVelocity.y = _rigidbody.velocity.y;
            
            // Apply velocity
            _rigidbody.velocity = targetVelocity;
        }
        
        private void CheckMilestones()
        {
            // Check distance milestones
            foreach (float milestone in _distanceMilestones)
            {
                if (_distanceRun >= milestone && !_reachedMilestones.Contains(milestone))
                {
                    _reachedMilestones.Add(milestone);
                    LogDebug($"[SpeedManager] Distance milestone reached: {milestone}m");
                    OnDistanceMilestone?.Invoke(milestone, _distanceRun);
                }
            }
            
            // Check speed tier milestones
            for (int i = 0; i < _speedTiers.Length; i++)
            {
                if (_currentSpeed >= _speedTiers[i] && !_reachedSpeedTiers.Contains(i))
                {
                    _reachedSpeedTiers.Add(i);
                    LogDebug($"[SpeedManager] Speed tier {i} reached: {_speedTiers[i]:F1} m/s");
                    OnSpeedTierReached?.Invoke(i, _speedTiers[i]);
                }
            }
        }
        
        private void ValidateConfiguration()
        {
            if (_speedConfig == null)
            {
                Debug.LogWarning("[SpeedManager] No SpeedConfig assigned! Using default values.");
            }
            
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    Debug.LogError("[SpeedManager] No Rigidbody found! SpeedManager requires Rigidbody component.");
                }
            }
            
            // Validate arrays
            if (_distanceMilestones == null || _distanceMilestones.Length == 0)
            {
                _distanceMilestones = new float[] { 100f, 500f, 1000f };
            }
            
            if (_speedTiers == null || _speedTiers.Length == 0)
            {
                _speedTiers = new float[] { 10f, 15f, 20f };
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
        
        #region Debug Methods
        
        public string GetDebugInfo()
        {
            return $"SpeedManager Debug Info:\n" +
                   $"Running: {_isRunning}, Paused: {_isPaused}\n" +
                   $"Distance: {_distanceRun:F1}m, Time: {_timeRunning:F1}s\n" +
                   $"Current Speed: {_currentSpeed:F1} m/s\n" +
                   $"Target Speed: {_targetSpeed:F1} m/s\n" +
                   $"Speed Modifier: {_speedModifier:F2} ({_speedModifierReason})\n" +
                   $"Fixed Speed: {(_hasFixedSpeed ? $"{_fixedSpeed:F1} m/s" : "None")}\n" +
                   $"Progress: {GetProgressPercent():P1}\n" +
                   $"Milestones: {_reachedMilestones.Count}/{_distanceMilestones.Length}\n" +
                   $"Speed Tiers: {_reachedSpeedTiers.Count}/{_speedTiers.Length}";
        }
        
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || !Application.isPlaying) return;
            
            // Draw speed indicator
            Gizmos.color = Color.green;
            Vector3 speedVector = _forwardDirection.normalized * (_currentSpeed * 0.5f);
            Gizmos.DrawRay(transform.position, speedVector);
            
            // Draw target speed
            Gizmos.color = Color.yellow;
            Vector3 targetVector = _forwardDirection.normalized * (_targetSpeed * 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, targetVector);
        }
        
        #endregion
        
        #region Editor Utilities
        
        #if UNITY_EDITOR
        [UnityEngine.ContextMenu("Start Progression")]
        private void EditorStartProgression()
        {
            if (Application.isPlaying)
                StartProgression();
        }
        
        [UnityEngine.ContextMenu("Pause Progression")]
        private void EditorPauseProgression()
        {
            if (Application.isPlaying)
                PauseProgression();
        }
        
        [UnityEngine.ContextMenu("Reset Progression")]
        private void EditorResetProgression()
        {
            if (Application.isPlaying)
                ResetProgression();
        }
        
        [UnityEngine.ContextMenu("Print Debug Info")]
        private void EditorPrintDebugInfo()
        {
            if (Application.isPlaying)
                Debug.Log(GetDebugInfo());
        }
        #endif
        
        #endregion
    }
}
