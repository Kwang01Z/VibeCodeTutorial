using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Simple Runner Controller cho testing - basic implementation để demo integration
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleRunnerController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LaneController _laneController;
        [SerializeField] private InputHandler _inputHandler;
        
        [Header("Movement Settings")]
        [SerializeField] private float _forwardSpeed = 10f;
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private bool _autoMove = true;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        
        #region Private Fields
        
        private Rigidbody _rigidbody;
        private bool _isInitialized = false;
        private Timer _jumpTimer;
        private bool _isGrounded = true;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            
            // Auto-find dependencies if not assigned
            if (_laneController == null)
                _laneController = GetComponent<LaneController>();
            if (_inputHandler == null)
                _inputHandler = FindObjectOfType<InputHandler>();
                
            ValidateDependencies();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            _jumpTimer.Update(Time.deltaTime);
            
            // Update ground detection (simple check)
            _isGrounded = transform.position.y <= 0.1f;
        }
        
        private void FixedUpdate()
        {
            if (!_isInitialized) return;
            
            // Auto forward movement
            if (_autoMove)
            {
                ApplyForwardMovement();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void ValidateDependencies()
        {
            if (_laneController == null)
            {
                Debug.LogError("[SimpleRunnerController] LaneController is required!", this);
                return;
            }
            
            if (_inputHandler == null)
            {
                Debug.LogError("[SimpleRunnerController] InputHandler is required!", this);
                return;
            }
        }
        
        private void Initialize()
        {
            if (_laneController == null || _inputHandler == null) return;
            
            // Subscribe to input events
            _inputHandler.OnJumpRequested += HandleJumpRequested;
            _inputHandler.OnSlideRequested += HandleSlideRequested;
            _inputHandler.OnLaneChangeRequested += HandleLaneChangeRequested;
            _inputHandler.OnManualItemRequested += HandleManualItemRequested;
            _inputHandler.OnPauseRequested += HandlePauseRequested;
            
            // Subscribe to lane change events
            _laneController.OnLaneChangeStarted += HandleLaneChangeStarted;
            _laneController.OnLaneChangeCompleted += HandleLaneChangeCompleted;
            
            _isInitialized = true;
            LogDebug("SimpleRunnerController initialized successfully");
        }
        
        #endregion
        
        #region Input Event Handlers
        
        private void HandleJumpRequested()
        {
            if (!_isGrounded || _jumpTimer.IsRunning)
            {
                LogDebug("Jump request ignored - not grounded or already jumping");
                return;
            }
            
            // Apply jump force
            Vector3 jumpVelocity = _rigidbody.velocity;
            jumpVelocity.y = _jumpForce;
            _rigidbody.velocity = jumpVelocity;
            
            // Start jump timer
            _jumpTimer.Start(0.5f); // Simple jump cooldown
            
            LogDebug("Jump executed");
        }
        
        private void HandleSlideRequested()
        {
            if (!_isGrounded)
            {
                LogDebug("Slide request ignored - not grounded");
                return;
            }
            
            // Simple slide implementation - just add downward force
            _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            
            LogDebug("Slide executed");
        }
        
        private void HandleLaneChangeRequested(int direction)
        {
            // Calculate target lane
            int targetLane = _laneController.CurrentLane + direction;
            
            // Request lane change through LaneController
            bool success = _laneController.RequestLaneChange(targetLane);
            
            LogDebug($"Lane change requested: current={_laneController.CurrentLane}, target={targetLane}, success={success}");
        }
        
        private void HandleManualItemRequested()
        {
            LogDebug("Manual item requested (not implemented yet)");
        }
        
        private void HandlePauseRequested()
        {
            // Simple pause implementation
            Time.timeScale = Time.timeScale > 0 ? 0 : 1;
            LogDebug($"Game {(Time.timeScale > 0 ? "resumed" : "paused")}");
        }
        
        #endregion
        
        #region Lane Change Event Handlers
        
        private void HandleLaneChangeStarted(int fromLane, int toLane)
        {
            LogDebug($"Lane change started: {fromLane} -> {toLane}");
        }
        
        private void HandleLaneChangeCompleted(int newLane)
        {
            LogDebug($"Lane change completed: now at lane {newLane}");
        }
        
        #endregion
        
        #region Movement
        
        private void ApplyForwardMovement()
        {
            // Simple forward movement
            Vector3 velocity = _rigidbody.velocity;
            velocity.z = _forwardSpeed;
            _rigidbody.velocity = velocity;
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Unsubscribe from events
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
            }
        }
        
        #endregion
        
        #region Debug & Utilities
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[SimpleRunnerController] {message}", this);
            }
        }
        
        /// <summary>
        /// Get debug status information
        /// </summary>
        /// <returns>Debug status string</returns>
        public string GetDebugStatus()
        {
            if (!_isInitialized) return "Not Initialized";
            
            return $"Speed: {_forwardSpeed}m/s | " +
                   $"Grounded: {_isGrounded} | " +
                   $"Jumping: {_jumpTimer.IsRunning} | " +
                   $"Lane: {_laneController.CurrentLane} | " +
                   $"Pos: {transform.position}";
        }
        
        /// <summary>
        /// Force test jump (for debugging)
        /// </summary>
        public void TestJump()
        {
            HandleJumpRequested();
        }
        
        /// <summary>
        /// Force test lane change (for debugging)
        /// </summary>
        /// <param name="direction">Direction to change lane</param>
        public void TestLaneChange(int direction)
        {
            HandleLaneChangeRequested(direction);
        }
        
        #endregion
    }
}
