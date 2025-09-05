using System;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// RunnerController - Core state machine cho runner character
    /// Quản lý Jump/Slide/LaneChange/Hit với physics-based movement
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RunnerController : MonoBehaviour, IRunnerController
    {
        #region Serialized Fields
        
        [Header("Dependencies")]
        [SerializeField] private LaneController _laneController;
        [SerializeField] private InputHandler _inputHandler;
        [SerializeField] private SpeedManager _speedManager;
        
        [Header("Physics Configuration")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _slideDownForce = 5f;
        [SerializeField] private float _hitKnockbackForce = 3f;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask _groundLayerMask = 1; // Default layer
        
        [Header("Advanced Physics")]
        [SerializeField] private float _groundAdhesionForce = 8f;
        [SerializeField] private float _airDrag = 0.1f;
        [SerializeField] private float _groundDrag = 2f;
        [SerializeField] private float _slideDrag = 0.5f;
        [SerializeField] private float _maxFallSpeed = 15f;
        [SerializeField] private bool _useGravityScale = true;
        [SerializeField] private float _normalGravityScale = 1f;
        [SerializeField] private float _slideGravityScale = 2f;
        [SerializeField] private float _hitRecoveryForce = 2f;
        
        [Header("State Timing")]
        [SerializeField] private float _jumpDuration = 0.6f;
        [SerializeField] private float _slideDuration = 0.7f;
        [SerializeField] private float _hitDuration = 0.1f;
        [SerializeField] private float _iFrameDuration = 1.2f;
        
        [Header("Advanced Settings")]
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _inputBufferTime = 0.15f;
        [SerializeField] private bool _allowChainActions = true;
        [SerializeField] private bool _allowLaneChangeInAir = true;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _enableGizmos = true;
        
        #endregion
        
        #region Private Fields
        
        // Component References
        private Rigidbody _rigidbody;
        private Collider _collider;
        private Transform _transform;
        private ISpeedManager _speedManagerInterface;
        
        // State Management
        private RunnerState _currentState = RunnerState.Running;
        private RunnerState _previousState = RunnerState.Running;
        
        // Timers
        private Timer _stateTimer;        // Thời lượng trạng thái hiện tại
        private Timer _coyoteTimer;       // Coyote time cho jump
        private Timer _inputBufferTimer;  // Buffer input khi chưa thể thực hiện
        
        // Buffered Input
        private RunnerAction _bufferedAction = RunnerAction.Jump;
        private int _bufferedActionParameter = 0;
        
        // Ground Detection
        private bool _isGrounded = true;
        private bool _wasGroundedLastFrame = true;
        private Vector3 _groundCheckOrigin;
        
        // Slide System
        private float _originalColliderHeight;
        private Vector3 _originalColliderCenter;
        private bool _isSliding = false;
        
        // Health System Integration
        private IHealthSystem _healthSystem;
        
        // I-frames System
        private Timer _iFrameTimer;
        private bool _wasHitThisFrame = false;
        private Vector3 _hitKnockbackVelocity;
        
        // Initialization
        private bool _isInitialized = false;
        
        #endregion
        
        #region IRunnerController Properties
        
        public RunnerState CurrentState => _currentState;
        
        public bool IsAlive => _healthSystem?.IsAlive ?? true && _currentState != RunnerState.Dead;
        
        public bool IsGrounded => _isGrounded || _coyoteTimer.IsRunning;
        
        public bool IsInIFrames => _currentState == RunnerState.IFrames || (_healthSystem?.IsInIFrames ?? false);
        
        public float StateTimeRemaining => _stateTimer.IsRunning ? _stateTimer.Progress * GetStateDuration(_currentState) : 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
            ValidateDependencies();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            UpdateTimers();
            UpdateGroundDetection();
            ProcessBufferedInput();
            UpdateStateMachine();
        }
        
        private void FixedUpdate()
        {
            if (!_isInitialized) return;
            
            ApplyPhysics();
        }
        
        private void OnDrawGizmos()
        {
            if (!_enableGizmos) return;
            
            DrawGroundCheck();
            DrawStateInfo();
            DrawPhysicsDebug();
        }
        
        #endregion
        
        #region Initialization
        
        private void CacheComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _transform = transform;
            
            // Auto-find dependencies nếu chưa assign
            if (_laneController == null)
                _laneController = GetComponent<LaneController>();
                
            if (_inputHandler == null)
                _inputHandler = FindObjectOfType<InputHandler>();
                
            if (_speedManager == null)
                _speedManager = GetComponent<SpeedManager>();
                
            // Cache health system component
            _healthSystem = GetComponent<IHealthSystem>();
            
            // Setup ground check origin
            _groundCheckOrigin = _collider != null ? _collider.bounds.center : _transform.position;
            
            LogDebug("[RunnerController] Components cached");
        }
        
        private void ValidateDependencies()
        {
            bool hasErrors = false;
            
            if (_rigidbody == null)
            {
                Debug.LogError("[RunnerController] Rigidbody component is required!", this);
                hasErrors = true;
            }
            
            if (_laneController == null)
            {
                Debug.LogError("[RunnerController] LaneController reference is required!", this);
                hasErrors = true;
            }
            
            if (_inputHandler == null)
            {
                Debug.LogError("[RunnerController] InputHandler reference is required!", this);
                hasErrors = true;
            }
            
            // SpeedManager is optional but recommended
            if (_speedManager == null)
            {
                Debug.LogWarning("[RunnerController] SpeedManager not found - speed integration disabled", this);
            }
            
            // HealthSystem is required for collision handling
            if (_healthSystem == null)
            {
                Debug.LogError("[RunnerController] IHealthSystem component (HealthComponent) is required!", this);
                hasErrors = true;
            }
            
            if (hasErrors)
            {
                enabled = false;
                return;
            }
            
            LogDebug("[RunnerController] Dependencies validated successfully");
        }
        
        private void Initialize()
        {
            if (_laneController == null || _inputHandler == null) return;
            
            // Setup SpeedManager interface
            _speedManagerInterface = _speedManager;
            
            // Subscribe to SpeedManager events if available
            if (_speedManagerInterface != null)
            {
                _speedManagerInterface.OnSpeedChanged += HandleSpeedChanged;
                _speedManagerInterface.OnDistanceMilestone += HandleDistanceMilestone;
                _speedManagerInterface.OnProgressionStateChanged += HandleProgressionStateChanged;
            }
            
            // Subscribe to input events
            _inputHandler.OnJumpRequested += HandleJumpRequested;
            _inputHandler.OnSlideRequested += HandleSlideRequested;
            _inputHandler.OnLaneChangeRequested += HandleLaneChangeRequested;
            _inputHandler.OnManualItemRequested += HandleManualItemRequested;
            _inputHandler.OnPauseRequested += HandlePauseRequested;
            
            // Subscribe to lane controller events
            _laneController.OnLaneChangeStarted += HandleLaneChangeStarted;
            _laneController.OnLaneChangeCompleted += HandleLaneChangeCompleted;
            _laneController.OnLaneChangeRejected += HandleLaneChangeRejected;
            
            // Subscribe to health system events
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath += HandlePlayerDeath;
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnIFramesStarted += HandleIFramesStarted;
                _healthSystem.OnIFramesEnded += HandleIFramesEnded;
            }
            
            // Store original collider dimensions for slide
            InitializeSlideSystem();
            
            // Initialize state
            _currentState = RunnerState.Running;
            _isInitialized = true;
            
            LogDebug("[RunnerController] Initialized successfully");
            OnStateChanged?.Invoke(RunnerState.Running, RunnerState.Running, 0f);
        }
        
        #endregion
        
        #region Timer Management
        
        private void UpdateTimers()
        {
            _stateTimer.Update(Time.deltaTime);
            _coyoteTimer.Update(Time.deltaTime);
            _inputBufferTimer.Update(Time.deltaTime);
            _iFrameTimer.Update(Time.deltaTime);
        }
        
        private float GetStateDuration(RunnerState state)
        {
            return state switch
            {
                RunnerState.Jumping => _jumpDuration,
                RunnerState.Sliding => _slideDuration,
                RunnerState.Hit => _hitDuration,
                RunnerState.IFrames => _iFrameDuration,
                _ => 0f
            };
        }
        
        #endregion
        
        #region Ground Detection
        
        private void UpdateGroundDetection()
        {
            _wasGroundedLastFrame = _isGrounded;
            
            // Raycast down để kiểm tra ground
            _groundCheckOrigin = _collider.bounds.center;
            _isGrounded = Physics.Raycast(_groundCheckOrigin, Vector3.down, 
                _collider.bounds.extents.y + _groundCheckDistance, _groundLayerMask);
            
            // Bắt đầu coyote timer khi vừa rời ground
            if (_wasGroundedLastFrame && !_isGrounded)
            {
                _coyoteTimer.Start(_coyoteTime);
                LogDebug("[RunnerController] Left ground - Coyote time started");
            }
        }
        
        #endregion
        
        #region State Machine Core
        
        private void UpdateStateMachine()
        {
            // Kiểm tra auto-transitions khi timer hết
            if (_stateTimer.IsExpired)
            {
                HandleStateTimerExpired();
            }
            
            // Kiểm tra landing (Jump -> Running)
            if (_currentState == RunnerState.Jumping && _isGrounded)
            {
                TryChangeState(RunnerState.Running, 0f);
            }
        }
        
        private void HandleStateTimerExpired()
        {
            var nextState = RunnerStateRules.GetAutoTransitionState(_currentState, IsAlive);
            
            if (nextState != _currentState)
            {
                TryChangeState(nextState, GetStateDuration(nextState));
                LogDebug($"[RunnerController] Auto-transition: {_currentState} -> {nextState}");
            }
        }
        
        private bool TryChangeState(RunnerState newState, float duration)
        {
            if (newState == _currentState) return true;
            
            var oldState = _currentState;
            
            _previousState = _currentState;
            _currentState = newState;
            
            // Handle state exit
            HandleStateExited(oldState);
            
            // Handle state enter
            HandleStateEntered(newState);
            
            // Start timer nếu cần
            if (duration > 0)
            {
                _stateTimer.Start(duration);
            }
            else
            {
                _stateTimer.Stop();
            }
            
            LogDebug($"[RunnerController] State changed: {_previousState} -> {_currentState} (duration: {duration:F2}s)");
            OnStateChanged?.Invoke(_previousState, _currentState, duration);
            
            return true;
        }
        
        #endregion
        
        #region IRunnerController Methods
        
        public bool CanPerformAction(RunnerAction action, int parameter = 0)
        {
            return RunnerStateRules.CanPerformAction(_currentState, action, IsGrounded, IsAlive);
        }
        
        public bool PerformAction(RunnerAction action, int parameter = 0)
        {
            if (!CanPerformAction(action, parameter))
            {
                // Thử buffer input nếu có thể
                if (_allowChainActions && _inputBufferTime > 0)
                {
                    BufferAction(action, parameter);
                }
                
                LogDebug($"[RunnerController] Action rejected: {action} (param: {parameter})");
                OnActionPerformed?.Invoke(action, parameter, false);
                return false;
            }
            
            ExecuteAction(action, parameter);
            OnActionPerformed?.Invoke(action, parameter, true);
            return true;
        }
        
        public void ForceChangeState(RunnerState newState, float duration = 0f)
        {
            if (duration <= 0)
                duration = GetStateDuration(newState);
                
            TryChangeState(newState, duration);
            LogDebug($"[RunnerController] Force state change: {newState}");
        }
        
        public void ResetToRunning()
        {
            _healthSystem?.ResetToFullHealth();
            TryChangeState(RunnerState.Running, 0f);
            
            // Clear timers
            _stateTimer.Stop();
            _coyoteTimer.Stop();
            _inputBufferTimer.Stop();
            _iFrameTimer.Stop();
            
            LogDebug("[RunnerController] Reset to Running state");
            OnRevive?.Invoke();
        }
        
        #endregion
        
        #region Input Buffer System
        
        private void BufferAction(RunnerAction action, int parameter)
        {
            _bufferedAction = action;
            _bufferedActionParameter = parameter;
            _inputBufferTimer.Start(_inputBufferTime);
            
            LogDebug($"[RunnerController] Action buffered: {action} (param: {parameter})");
        }
        
        private void ProcessBufferedInput()
        {
            if (!_inputBufferTimer.IsRunning) return;
            
            if (CanPerformAction(_bufferedAction, _bufferedActionParameter))
            {
                ExecuteAction(_bufferedAction, _bufferedActionParameter);
                _inputBufferTimer.Stop();
                
                LogDebug($"[RunnerController] Buffered action executed: {_bufferedAction}");
                OnActionPerformed?.Invoke(_bufferedAction, _bufferedActionParameter, true);
            }
        }
        
        #endregion
        
        #region Action Execution
        
        private void ExecuteAction(RunnerAction action, int parameter)
        {
            switch (action)
            {
                case RunnerAction.Jump:
                    ExecuteJump();
                    break;
                case RunnerAction.Slide:
                    ExecuteSlide();
                    break;
                case RunnerAction.LaneChange:
                    ExecuteLaneChange(parameter);
                    break;
                case RunnerAction.UseManualItem:
                    ExecuteManualItem();
                    break;
                case RunnerAction.Hit:
                    ExecuteHit();
                    break;
                case RunnerAction.Pause:
                    ExecutePause();
                    break;
                default:
                    LogDebug($"[RunnerController] Unknown action: {action}");
                    break;
            }
        }
        
        private void ExecuteJump()
        {
            // Validate jump conditions (should already be checked by CanPerformAction)
            if (!IsGrounded)
            {
                LogDebug("[RunnerController] Jump failed - not grounded and no coyote time");
                return;
            }
            
            // Apply jump force
            Vector3 currentVelocity = _rigidbody.velocity;
            currentVelocity.y = 0f; // Reset Y velocity để jump consistency
            _rigidbody.velocity = currentVelocity;
            
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            
            // Stop coyote timer since we used it
            _coyoteTimer.Stop();
            
            // Change state
            var targetState = RunnerStateRules.GetTargetState(_currentState, RunnerAction.Jump);
            TryChangeState(targetState, GetStateDuration(targetState));
            
            LogDebug($"[RunnerController] Jump executed - force: {_jumpForce}, coyote used: {!_isGrounded}");
        }
        
        private void ExecuteSlide()
        {
            // Validate slide conditions
            if (!IsGrounded)
            {
                LogDebug("[RunnerController] Slide failed - not grounded");
                return;
            }
            
            // Enter slide mode
            StartSliding();
            
            // Apply downward force để "dính" vào ground
            _rigidbody.AddForce(Vector3.down * _slideDownForce, ForceMode.Impulse);
            
            // Change state
            var targetState = RunnerStateRules.GetTargetState(_currentState, RunnerAction.Slide);
            TryChangeState(targetState, GetStateDuration(targetState));
            
            LogDebug($"[RunnerController] Slide executed - force: {_slideDownForce}");
        }
        
        private void ExecuteLaneChange(int direction)
        {
            // Delegate to LaneController
            var targetLane = _laneController.CurrentLane + direction;
            var success = _laneController.RequestLaneChange(targetLane);
            
            if (success && _currentState != RunnerState.LaneChanging)
            {
                var targetState = RunnerStateRules.GetTargetState(_currentState, RunnerAction.LaneChange);
                TryChangeState(targetState, 0f); // Duration controlled by LaneController
            }
            
            LogDebug($"[RunnerController] Lane change executed: direction={direction}, success={success}");
        }
        
        private void ExecuteManualItem()
        {
            // TODO: Integrate với ItemSystem
            LogDebug("[RunnerController] Manual item executed (stub)");
        }
        
        private void ExecuteHit()
        {
            // Check if already in I-frames or dead
            if (IsInIFrames || _currentState == RunnerState.Dead || !IsAlive)
            {
                LogDebug("[RunnerController] Hit ignored - in I-frames, dead, or already processed");
                return;
            }
            
            // Apply speed slowdown effect
            ApplyHitSpeedEffect();
            
            // Apply knockback force
            ApplyHitKnockback();
            
            // Damage is handled by CollisionDetector -> HealthSystem
            // RunnerController just handles the physics/animation reaction
            
            // Change to Hit state (short reaction)
            var targetState = RunnerStateRules.GetTargetState(_currentState, RunnerAction.Hit);
            TryChangeState(targetState, GetStateDuration(targetState));
            
            LogDebug($"[RunnerController] Hit executed - health: {_healthSystem?.CurrentHealth ?? 0}/{_healthSystem?.MaxHealth ?? 0}, knockback applied");
        }
        
        private void ExecutePause()
        {
            // Simple pause implementation
            Time.timeScale = Time.timeScale > 0 ? 0 : 1;
            LogDebug($"[RunnerController] Game {(Time.timeScale > 0 ? "resumed" : "paused")}");
        }
        
        #endregion
        
        #region Physics Implementation
        
        /// <summary>
        /// Apply state-specific physics trong FixedUpdate
        /// Handles forward movement, ground adhesion, drag, gravity scaling
        /// </summary>
        private void ApplyPhysics()
        {
            if (_rigidbody == null || _currentState == RunnerState.Dead) return;
            
            // Apply forward movement from SpeedManager
            ApplyForwardMovement();
            
            // Apply state-specific physics
            switch (_currentState)
            {
                case RunnerState.Running:
                    ApplyRunningPhysics();
                    break;
                    
                case RunnerState.Jumping:
                    ApplyJumpingPhysics();
                    break;
                    
                case RunnerState.Sliding:
                    ApplySlidingPhysics();
                    break;
                    
                case RunnerState.Hit:
                    ApplyHitPhysics();
                    break;
                    
                case RunnerState.IFrames:
                    ApplyIFramePhysics();
                    break;
                    
                case RunnerState.LaneChanging:
                    ApplyLaneChangePhysics();
                    break;
            }
            
            // Apply drag based on state
            ApplyDrag();
            
            // Apply gravity scaling
            ApplyGravityScaling();
            
            // Apply ground adhesion if needed
            ApplyGroundAdhesion();
            
            // Clamp velocities
            ClampVelocities();
        }
        
        /// <summary>
        /// Apply forward movement từ SpeedManager
        /// </summary>
        private void ApplyForwardMovement()
        {
            if (_speedManagerInterface == null) return;
            
            float targetSpeed = _speedManagerInterface.CurrentSpeed;
            Vector3 currentVelocity = _rigidbody.velocity;
            
            // Set Z velocity to target speed (forward movement)
            currentVelocity.z = targetSpeed;
            _rigidbody.velocity = currentVelocity;
        }
        
        /// <summary>
        /// Physics for Running state - normal ground movement
        /// </summary>
        private void ApplyRunningPhysics()
        {
            // Running state relies mainly on SpeedManager for forward movement
            // Apply slight downward force to maintain ground contact
            if (_isGrounded)
            {
                Vector3 groundForce = Vector3.down * _groundAdhesionForce * 0.5f;
                _rigidbody.AddForce(groundForce, ForceMode.Force);
            }
        }
        
        /// <summary>
        /// Physics for Jumping state - air control and landing
        /// </summary>
        private void ApplyJumpingPhysics()
        {
            // No additional forces needed - gravity and initial jump impulse handle this
            // Could add air control or variable jump height here if needed
        }
        
        /// <summary>
        /// Physics for Sliding state - ground adhesion and downward force
        /// </summary>
        private void ApplySlidingPhysics()
        {
            if (!_isGrounded) return;
            
            // Strong downward force to keep runner glued to ground
            Vector3 slideForce = Vector3.down * _groundAdhesionForce;
            _rigidbody.AddForce(slideForce, ForceMode.Force);
            
            // Additional slide-specific downward impulse for momentum
            Vector3 slideDownForce = Vector3.down * _slideDownForce * Time.fixedDeltaTime;
            _rigidbody.AddForce(slideDownForce, ForceMode.Force);
        }
        
        /// <summary>
        /// Physics for Hit state - knockback and recovery
        /// </summary>
        private void ApplyHitPhysics()
        {
            // Apply recovery force to help runner get back to normal speed
            if (_isGrounded)
            {
                Vector3 recoveryForce = Vector3.forward * _hitRecoveryForce;
                _rigidbody.AddForce(recoveryForce, ForceMode.Force);
            }
        }
        
        /// <summary>
        /// Physics for IFrames state - reduced physics response
        /// </summary>
        private void ApplyIFramePhysics()
        {
            // Slightly reduced physics response during I-frames
            // Apply gentle ground adhesion
            if (_isGrounded)
            {
                Vector3 adhesionForce = Vector3.down * _groundAdhesionForce * 0.3f;
                _rigidbody.AddForce(adhesionForce, ForceMode.Force);
            }
        }
        
        /// <summary>
        /// Physics for LaneChanging state - lateral movement handled by LaneController
        /// </summary>
        private void ApplyLaneChangePhysics()
        {
            // Lane changing physics handled by LaneController
            // Apply normal ground adhesion
            if (_isGrounded)
            {
                Vector3 adhesionForce = Vector3.down * _groundAdhesionForce * 0.7f;
                _rigidbody.AddForce(adhesionForce, ForceMode.Force);
            }
        }
        
        /// <summary>
        /// Apply drag based on current state
        /// </summary>
        private void ApplyDrag()
        {
            float targetDrag = _currentState switch
            {
                RunnerState.Jumping => _airDrag,
                RunnerState.Sliding => _slideDrag,
                _ => _groundDrag
            };
            
            _rigidbody.drag = Mathf.Lerp(_rigidbody.drag, targetDrag, 
                5f * Time.fixedDeltaTime);
        }
        
        /// <summary>
        /// Apply gravity scaling based on state
        /// </summary>
        private void ApplyGravityScaling()
        {
            if (!_useGravityScale) return;
            
            float targetGravityScale = _currentState switch
            {
                RunnerState.Sliding => _slideGravityScale,
                _ => _normalGravityScale
            };
            
            // Smooth transition to target gravity scale
            float currentScale = _rigidbody.useGravity ? 1f : 0f;
            if (Mathf.Abs(currentScale - targetGravityScale) > 0.1f)
            {
                // Apply additional downward force to simulate gravity scaling
                Vector3 additionalGravity = Physics.gravity * (targetGravityScale - 1f);
                _rigidbody.AddForce(additionalGravity, ForceMode.Acceleration);
            }
        }
        
        /// <summary>
        /// Apply ground adhesion to prevent bouncing
        /// </summary>
        private void ApplyGroundAdhesion()
        {
            if (!_isGrounded || _currentState == RunnerState.Jumping) return;
            
            // Check if runner is moving upward when should be on ground
            if (_rigidbody.velocity.y > 0.1f)
            {
                Vector3 adhesionForce = Vector3.down * _groundAdhesionForce;
                _rigidbody.AddForce(adhesionForce, ForceMode.Force);
                
                LogDebug($"[RunnerController] Ground adhesion applied - velocity.y was {_rigidbody.velocity.y:F2}");
            }
        }
        
        /// <summary>
        /// Clamp velocities to prevent excessive speeds
        /// </summary>
        private void ClampVelocities()
        {
            Vector3 velocity = _rigidbody.velocity;
            
            // Clamp Y velocity (prevent excessive falling/jumping)
            velocity.y = Mathf.Clamp(velocity.y, -_maxFallSpeed, _jumpForce * 1.2f);
            
            // Z velocity is controlled by SpeedManager, don't clamp it
            // X velocity should be controlled by LaneController
            
            _rigidbody.velocity = velocity;
        }
        
        #endregion
        
        #region Input Event Handlers
        
        private void HandleJumpRequested()
        {
            PerformAction(RunnerAction.Jump);
        }
        
        private void HandleSlideRequested()
        {
            PerformAction(RunnerAction.Slide);
        }
        
        private void HandleLaneChangeRequested(int direction)
        {
            PerformAction(RunnerAction.LaneChange, direction);
        }
        
        private void HandleManualItemRequested()
        {
            PerformAction(RunnerAction.UseManualItem);
        }
        
        private void HandlePauseRequested()
        {
            PerformAction(RunnerAction.Pause);
        }
        
        #endregion
        
        #region Lane Controller Event Handlers
        
        private void HandleLaneChangeStarted(int fromLane, int toLane)
        {
            if (_currentState != RunnerState.LaneChanging)
            {
                TryChangeState(RunnerState.LaneChanging, 0f);
            }
            LogDebug($"[RunnerController] Lane change started: {fromLane} -> {toLane}");
        }
        
        private void HandleLaneChangeCompleted(int newLane)
        {
            if (_currentState == RunnerState.LaneChanging)
            {
                TryChangeState(RunnerState.Running, 0f);
            }
            LogDebug($"[RunnerController] Lane change completed: {newLane}");
        }
        
        private void HandleLaneChangeRejected(int requestedLane, string reason)
        {
            LogDebug($"[RunnerController] Lane change rejected: {requestedLane} ({reason})");
        }
        
        #endregion
        
        #region HealthSystem Event Handlers
        
        /// <summary>
        /// Handler khi player chết (từ HealthSystem)
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (_currentState != RunnerState.Dead)
            {
                TryChangeState(RunnerState.Dead, 0f);
            }
            
            LogDebug("[RunnerController] Player death handled from HealthSystem");
            OnDeath?.Invoke(); // Forward event
        }
        
        /// <summary>
        /// Handler khi health thay đổi (từ HealthSystem)
        /// </summary>
        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            LogDebug($"[RunnerController] Health changed: {currentHealth}/{maxHealth}");
            
            // TODO: Trigger health UI updates, audio feedback
            // OnHealthUpdated?.Invoke(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// Handler khi I-frames bắt đầu (từ HealthSystem)
        /// </summary>
        private void HandleIFramesStarted(float duration)
        {
            // Đồng bộ state machine với health system I-frames
            if (_currentState == RunnerState.Hit)
            {
                // Transition Hit -> IFrames
                TryChangeState(RunnerState.IFrames, duration);
            }
            
            LogDebug($"[RunnerController] I-frames started from HealthSystem: {duration}s");
        }
        
        /// <summary>
        /// Handler khi I-frames kết thúc (từ HealthSystem)
        /// </summary>
        private void HandleIFramesEnded()
        {
            // Transition IFrames -> Running nếu còn sống
            if (_currentState == RunnerState.IFrames && IsAlive)
            {
                TryChangeState(RunnerState.Running, 0f);
            }
            
            LogDebug("[RunnerController] I-frames ended from HealthSystem");
        }
        
        #endregion
        
        #region Public API Extensions
        
        /// <summary>
        /// Apply hit từ collision system
        /// </summary>
        public void ApplyHit()
        {
            PerformAction(RunnerAction.Hit);
        }
        
        /// <summary>
        /// Lấy thông tin debug state hiện tại
        /// </summary>
        public string GetDebugInfo()
        {
            var healthInfo = _healthSystem != null ? $"{_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}" : "N/A";
            return $"State: {_currentState}, Grounded: {_isGrounded}, Health: {healthInfo}, " +
                   $"StateTimer: {(_stateTimer.IsRunning ? _stateTimer.Progress.ToString("F2") : "N/A")}, " +
                   $"IFrames: {IsInIFrames}";
        }
        
        #endregion
        
        #region Debug & Visualization
        
        private void DrawGroundCheck()
        {
            if (_collider == null) return;
            
            var origin = _collider.bounds.center;
            var distance = _collider.bounds.extents.y + _groundCheckDistance;
            
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector3.down * distance);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin + Vector3.down * distance, 0.1f);
        }
        
        /// <summary>
        /// Draw physics debug information
        /// </summary>
        private void DrawPhysicsDebug()
        {
            if (_rigidbody == null || !Application.isPlaying) return;
            
            Vector3 position = _transform.position;
            Vector3 velocity = _rigidbody.velocity;
            
            // Draw velocity vector
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, velocity * 0.5f);
            
            // Draw forward movement vector (Z axis)
            Gizmos.color = Color.green;
            Vector3 forwardVel = new Vector3(0, 0, velocity.z);
            Gizmos.DrawRay(position + Vector3.up * 0.5f, forwardVel * 0.5f);
            
            // Draw upward movement vector (Y axis)
            if (Mathf.Abs(velocity.y) > 0.1f)
            {
                Gizmos.color = velocity.y > 0 ? Color.cyan : Color.magenta;
                Vector3 upwardVel = new Vector3(0, velocity.y, 0);
                Gizmos.DrawRay(position + Vector3.right * 0.5f, upwardVel * 0.5f);
            }
            
            // Draw lateral movement vector (X axis - lane changes)
            if (Mathf.Abs(velocity.x) > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 lateralVel = new Vector3(velocity.x, 0, 0);
                Gizmos.DrawRay(position + Vector3.back * 0.5f, lateralVel * 0.5f);
            }
            
            // Draw collision bounds
            DrawCollisionBounds();
            
            // Draw state-specific physics debug
            DrawStatePhysicsDebug();
        }
        
        /// <summary>
        /// Draw collision bounds visualization
        /// </summary>
        private void DrawCollisionBounds()
        {
            if (_collider == null) return;
            
            Gizmos.color = _currentState switch
            {
                RunnerState.Sliding => Color.red,
                RunnerState.Hit => Color.magenta,
                RunnerState.IFrames => Color.cyan,
                _ => Color.white
            };
            
            // Draw collider bounds
            if (_collider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(capsule.bounds.center, capsule.bounds.size);
            }
            else if (_collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            }
        }
        
        /// <summary>
        /// Draw state-specific physics debug info
        /// </summary>
        private void DrawStatePhysicsDebug()
        {
            Vector3 position = _transform.position;
            
            switch (_currentState)
            {
                case RunnerState.Sliding:
                    // Draw slide forces
                    Gizmos.color = Color.red;
                    Vector3 slideForce = Vector3.down * _groundAdhesionForce * 0.1f;
                    Gizmos.DrawRay(position, slideForce);
                    break;
                    
                case RunnerState.Hit:
                    // Draw knockback direction
                    Gizmos.color = Color.magenta;
                    Vector3 knockbackDir = new Vector3(0, 0.3f, -1f).normalized;
                    Gizmos.DrawRay(position, knockbackDir * 2f);
                    break;
                    
                case RunnerState.Running:
                    // Draw ground adhesion
                    if (_isGrounded)
                    {
                        Gizmos.color = Color.green;
                        Vector3 adhesion = Vector3.down * _groundAdhesionForce * 0.05f;
                        Gizmos.DrawRay(position, adhesion);
                    }
                    break;
            }
            
            // Draw speed info
            DrawSpeedDebug();
        }
        
        /// <summary>
        /// Draw speed debug information
        /// </summary>
        private void DrawSpeedDebug()
        {
            if (_speedManagerInterface == null) return;
            
            Vector3 position = _transform.position + Vector3.up * 3f;
            float currentSpeed = _speedManagerInterface.CurrentSpeed;
            float targetSpeed = _speedManagerInterface.TargetSpeed;
            
            // Draw current speed bar
            Gizmos.color = Color.green;
            float speedScale = currentSpeed / 20f; // Normalize to max expected speed
            Gizmos.DrawLine(position, position + Vector3.right * speedScale * 2f);
            
            // Draw target speed bar
            Gizmos.color = Color.yellow;
            float targetScale = targetSpeed / 20f;
            Gizmos.DrawLine(position + Vector3.up * 0.2f, 
                           position + Vector3.up * 0.2f + Vector3.right * targetScale * 2f);
        }
        
        private void DrawStateInfo()
        {
            if (!Application.isPlaying) return;
            
            var pos = _transform.position + Vector3.up * 2f;
            
            #if UNITY_EDITOR
            var healthInfo = _healthSystem != null ? $"{_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}" : "N/A";
            UnityEditor.Handles.Label(pos, $"{_currentState}\nHP: {healthInfo}\nIF: {IsInIFrames}");
            #endif
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }
        
        #endregion
        
        #region Events
        
        public event Action<RunnerState, RunnerState, float> OnStateChanged;
        public event Action<RunnerAction, int, bool> OnActionPerformed;
        public event Action OnDeath;
        public event Action OnRevive;
        
        #endregion
        
        #region Slide System
        
        private void InitializeSlideSystem()
        {
            if (_collider == null) return;
            
            // Store original collider dimensions
            if (_collider is CapsuleCollider capsule)
            {
                _originalColliderHeight = capsule.height;
                _originalColliderCenter = capsule.center;
            }
            else if (_collider is BoxCollider box)
            {
                _originalColliderHeight = box.size.y;
                _originalColliderCenter = box.center;
            }
            
            LogDebug($"[RunnerController] Slide system initialized - original height: {_originalColliderHeight}");
        }
        
        private void StartSliding()
        {
            if (_isSliding) return;
            
            _isSliding = true;
            
            // Modify collider to half height
            ModifyColliderForSlide(true);
            
            LogDebug("[RunnerController] Started sliding - collider modified");
        }
        
        private void StopSliding()
        {
            if (!_isSliding) return;
            
            _isSliding = false;
            
            // Restore original collider
            ModifyColliderForSlide(false);
            
            LogDebug("[RunnerController] Stopped sliding - collider restored");
        }
        
        private void ModifyColliderForSlide(bool isSliding)
        {
            if (_collider == null) return;
            
            if (_collider is CapsuleCollider capsule)
            {
                if (isSliding)
                {
                    capsule.height = _originalColliderHeight * 0.5f;
                    capsule.center = new Vector3(_originalColliderCenter.x, _originalColliderCenter.y - _originalColliderHeight * 0.25f, _originalColliderCenter.z);
                }
                else
                {
                    capsule.height = _originalColliderHeight;
                    capsule.center = _originalColliderCenter;
                }
            }
            else if (_collider is BoxCollider box)
            {
                if (isSliding)
                {
                    var newSize = box.size;
                    newSize.y = _originalColliderHeight * 0.5f;
                    box.size = newSize;
                    
                    box.center = new Vector3(_originalColliderCenter.x, _originalColliderCenter.y - _originalColliderHeight * 0.25f, _originalColliderCenter.z);
                }
                else
                {
                    var newSize = box.size;
                    newSize.y = _originalColliderHeight;
                    box.size = newSize;
                    
                    box.center = _originalColliderCenter;
                }
            }
        }
        
        #endregion
        
        #region Health System Integration
        
        /// <summary>
        /// Khôi phục máu qua HealthSystem (cho item Life hoặc testing)
        /// </summary>
        /// <param name="amount">Lượng máu khôi phục</param>
        public void RestoreHealth(int amount = 1)
        {
            _healthSystem?.RestoreHealth(amount);
        }
        
        /// <summary>
        /// Get current health info từ HealthSystem
        /// </summary>
        public (int current, int max) GetHealthInfo()
        {
            if (_healthSystem != null)
            {
                return (_healthSystem.CurrentHealth, _healthSystem.MaxHealth);
            }
            return (0, 0);
        }
        
        /// <summary>
        /// Set invincible mode (for testing)
        /// </summary>
        /// <param name="invincible">True để bật chế độ bất tử</param>
        public void SetInvincible(bool invincible)
        {
            if (_healthSystem is HealthComponent healthComponent)
            {
                // Access private field through reflection or add public method to HealthComponent
                LogDebug($"[RunnerController] Invincible mode request: {invincible} (requires HealthComponent support)");
            }
        }
        
        #endregion
        
        #region Hit Physics
        
        /// <summary>
        /// Apply knockback force khi bị hit
        /// </summary>
        private void ApplyHitKnockback()
        {
            if (_rigidbody == null) return;
            
            // Calculate knockback direction (backward and slightly up)
            Vector3 knockbackDirection = new Vector3(0, 0.3f, -1f).normalized;
            Vector3 knockbackForce = knockbackDirection * _hitKnockbackForce;
            
            // Store for physics application
            _hitKnockbackVelocity = knockbackForce;
            
            // Apply immediately
            _rigidbody.AddForce(knockbackForce, ForceMode.Impulse);
            
            LogDebug($"[RunnerController] Knockback applied - force: {knockbackForce}");
        }
        
        #endregion
        
        #region State Change Handlers
        
        private void HandleStateEntered(RunnerState newState)
        {
            switch (newState)
            {
                case RunnerState.Sliding:
                    StartSliding();
                    break;
                    
                case RunnerState.Hit:
                    // Hit reaction handled in ExecuteHit
                    break;
                    
                case RunnerState.IFrames:
                    // Start I-frame timer
                    _iFrameTimer.Start(_iFrameDuration);
                    LogDebug($"[RunnerController] I-frames started - duration: {_iFrameDuration}s");
                    break;
                    
                case RunnerState.Dead:
                    // Stop all timers and disable input
                    _stateTimer.Stop();
                    _coyoteTimer.Stop();
                    _inputBufferTimer.Stop();
                    _iFrameTimer.Stop();
                    
                    LogDebug("[RunnerController] Entered Dead state");
                    break;
                    
                case RunnerState.Running:
                    if (_previousState == RunnerState.Sliding)
                    {
                        StopSliding();
                    }
                    break;
            }
        }
        
        private void HandleStateExited(RunnerState oldState)
        {
            switch (oldState)
            {
                case RunnerState.Sliding:
                    StopSliding();
                    break;
                    
                case RunnerState.IFrames:
                    LogDebug("[RunnerController] I-frames ended");
                    break;
            }
        }
        
        #endregion
        
        #region SpeedManager Integration
        
        /// <summary>
        /// Áp dụng speed effect khi bị hit (làm chậm tạm thời)
        /// </summary>
        private void ApplyHitSpeedEffect()
        {
            if (_speedManagerInterface == null) return;
            
            // Apply speed slowdown for 1 second
            _speedManagerInterface.SetSpeedModifier(0.6f, 1f);
            
            LogDebug("[RunnerController] Hit speed effect applied - 60% speed for 1s");
        }
        
        /// <summary>
        /// Pause speed progression khi game pause
        /// </summary>
        public void PauseSpeed()
        {
            if (_speedManagerInterface != null)
            {
                _speedManagerInterface.PauseProgression();
            }
        }
        
        /// <summary>
        /// Resume speed progression khi game resume
        /// </summary>
        public void ResumeSpeed()
        {
            if (_speedManagerInterface != null)
            {
                _speedManagerInterface.ResumeProgression();
            }
        }
        
        /// <summary>
        /// Get current speed info from SpeedManager
        /// </summary>
        public (float currentSpeed, float targetSpeed, float distance) GetSpeedInfo()
        {
            if (_speedManagerInterface != null)
            {
                return (_speedManagerInterface.CurrentSpeed, _speedManagerInterface.TargetSpeed, _speedManagerInterface.DistanceRun);
            }
            return (0f, 0f, 0f);
        }
        
        #endregion
        
        #region SpeedManager Event Handlers
        
        private void HandleSpeedChanged(float newSpeed, float oldSpeed, float targetSpeed)
        {
            LogDebug($"[RunnerController] Speed changed: {oldSpeed:F1} -> {newSpeed:F1} (target: {targetSpeed:F1})");
            
            // TODO: Trigger speed change effects/audio
            // OnSpeedTierChanged?.Invoke(newSpeed);
        }
        
        private void HandleDistanceMilestone(float milestone, float totalDistance)
        {
            LogDebug($"[RunnerController] Distance milestone reached: {milestone}m (total: {totalDistance:F1}m)");
            
            // TODO: Trigger milestone rewards/effects
            // OnMilestoneReached?.Invoke(milestone);
        }
        
        private void HandleProgressionStateChanged(bool isRunning, float distance, float speed)
        {
            LogDebug($"[RunnerController] Speed progression {(isRunning ? "started" : "paused")} - distance: {distance:F1}m, speed: {speed:F1}");
            
            // Auto-start speed progression khi runner enters Running state
            if (!isRunning && _currentState == RunnerState.Running)
            {
                _speedManagerInterface?.StartProgression();
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnJumpRequested -= HandleJumpRequested;
                _inputHandler.OnSlideRequested -= HandleSlideRequested;
                _inputHandler.OnLaneChangeRequested -= HandleLaneChangeRequested;
                _inputHandler.OnManualItemRequested -= HandleManualItemRequested;
                _inputHandler.OnPauseRequested -= HandlePauseRequested;
            }
            
            if (_laneController != null)
            {
                _laneController.OnLaneChangeStarted -= HandleLaneChangeStarted;
                _laneController.OnLaneChangeCompleted -= HandleLaneChangeCompleted;
                _laneController.OnLaneChangeRejected -= HandleLaneChangeRejected;
            }
            
            if (_speedManagerInterface != null)
            {
                _speedManagerInterface.OnSpeedChanged -= HandleSpeedChanged;
                _speedManagerInterface.OnDistanceMilestone -= HandleDistanceMilestone;
                _speedManagerInterface.OnProgressionStateChanged -= HandleProgressionStateChanged;
            }
            
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath -= HandlePlayerDeath;
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnIFramesStarted -= HandleIFramesStarted;
                _healthSystem.OnIFramesEnded -= HandleIFramesEnded;
            }
        }
        
        #endregion
    }
}
