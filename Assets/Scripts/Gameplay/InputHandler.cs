using System;
using System.Collections.Generic;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Input Handler component - xử lý input từ PC và mobile với buffering và validation
    /// </summary>
    public class InputHandler : MonoBehaviour, IInputHandler
    {
        [Header("Configuration")]
        [SerializeField] private InputConfig _config = InputConfig.Default;
        [SerializeField] private InputPlatform _forcePlatform = InputPlatform.Auto;
        
        [Header("PC Keyboard Keys")]
        [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode _slideKey = KeyCode.S;
        [SerializeField] private KeyCode _laneLeftKey = KeyCode.A;
        [SerializeField] private KeyCode _laneRightKey = KeyCode.D;
        [SerializeField] private KeyCode _manualItemKey = KeyCode.E;
        [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _showSwipeDebug = false;
        
        #region Private Fields
        
        private InputPlatform _currentPlatform = InputPlatform.Auto;
        private bool _isLeftHandMode = false;
        private bool _isEnabled = true;
        
        // Mobile touch tracking
        private bool _isTouching = false;
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private int _activeTouchId = -1;
        
        // Input buffering
        private Dictionary<InputType, Timer> _bufferedInputs = new Dictionary<InputType, Timer>();
        private Dictionary<InputType, Timer> _inputCooldowns = new Dictionary<InputType, Timer>();
        
        // Validation
        private SwipeData _lastSwipeData;
        private float _lastInputTime;
        
        #endregion
        
        #region Properties (IInputHandler)
        
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set 
            { 
                _isEnabled = value;
                if (!_isEnabled)
                {
                    ClearInputBuffer();
                    ClearAllTouches();
                }
                LogDebug($"InputHandler {(value ? "enabled" : "disabled")}");
            } 
        }
        
        public bool IsLeftHandMode => _isLeftHandMode;
        public InputPlatform CurrentPlatform => _currentPlatform;
        
        public bool HasBufferedInput 
        { 
            get 
            { 
                foreach (var timer in _bufferedInputs.Values)
                {
                    if (timer.IsRunning) return true;
                }
                return false;
            } 
        }
        
        #endregion
        
        #region Events (IInputHandler)
        
        public event Action OnJumpRequested;
        public event Action OnSlideRequested;
        public event Action<int> OnLaneChangeRequested;
        public event Action OnManualItemRequested;
        public event Action OnPauseRequested;
        public event Action<InputType, float> OnInputBuffered;
        public event Action<InputType> OnBufferedInputExecuted;
        public event Action<InputType, string> OnInputRejected;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Initialize timers dictionaries
            foreach (InputType inputType in System.Enum.GetValues(typeof(InputType)))
            {
                _bufferedInputs[inputType] = new Timer();
                _inputCooldowns[inputType] = new Timer();
            }
            
            // Detect platform
            DetectPlatform();
            
            // Enable input actions
            EnableInputActions();
        }
        
        private void Start()
        {
            LogDebug($"InputHandler initialized on {_currentPlatform} platform");
        }
        
        private void Update()
        {
            if (!_isEnabled) return;
            
            UpdateTimers();
            ProcessBufferedInputs();
            
            // Process platform-specific input
            switch (_currentPlatform)
            {
                case InputPlatform.PC:
                    ProcessPCInput();
                    break;
                case InputPlatform.Mobile:
                    ProcessMobileInput();
                    break;
            }
        }
        
        private void OnEnable()
        {
            EnableInputActions();
        }
        
        private void OnDisable()
        {
            DisableInputActions();
            ClearAllTouches();
        }
        
        private void OnDestroy()
        {
            DisableInputActions();
        }
        
        #endregion
        
        #region Public Methods (IInputHandler)
        
        public void SetLeftHandMode(bool enabled)
        {
            if (_isLeftHandMode != enabled)
            {
                _isLeftHandMode = enabled;
                LogDebug($"Left-hand mode: {enabled}");
                // Note: Left-hand mode chỉ ảnh hưởng mobile swipe direction interpretation
            }
        }
        
        public void ClearInputBuffer()
        {
            foreach (var inputType in _bufferedInputs.Keys)
            {
                _bufferedInputs[inputType].Stop();
            }
            LogDebug("Input buffer cleared");
        }
        
        public void SetPlatform(InputPlatform platform)
        {
            if (platform != _currentPlatform)
            {
                _currentPlatform = platform;
                ClearInputBuffer();
                ClearAllTouches();
                LogDebug($"Platform set to: {platform}");
            }
        }
        
        public void SetSwipeSensitivity(float sensitivity)
        {
            _config.swipeSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3.0f);
            _config.swipeThreshold = 100f * _config.swipeSensitivity;
            LogDebug($"Swipe sensitivity set to: {sensitivity} (threshold: {_config.swipeThreshold}px)");
        }
        
        #endregion
        
        #region Private Methods - Platform Detection
        
        private void DetectPlatform()
        {
            if (_forcePlatform != InputPlatform.Auto)
            {
                _currentPlatform = _forcePlatform;
                return;
            }
            
            // Auto-detect based on platform
            #if UNITY_EDITOR
                _currentPlatform = InputPlatform.PC; // Default to PC in editor
            #elif UNITY_STANDALONE
                _currentPlatform = InputPlatform.PC;
            #elif UNITY_ANDROID || UNITY_IOS
                _currentPlatform = InputPlatform.Mobile;
            #else
                _currentPlatform = InputPlatform.PC; // Fallback
            #endif
            
            // Runtime detection override
            if (Application.isMobilePlatform && Input.touchSupported)
            {
                _currentPlatform = InputPlatform.Mobile;
            }
        }
        
        #endregion
        
        #region Private Methods - Input Actions
        
        private void EnableInputActions()
        {
            // No-op for keyboard input (always enabled)
        }
        
        private void DisableInputActions()
        {
            // No-op for keyboard input
        }
        
        #endregion
        
        #region Private Methods - Timers & Buffering
        
        private void UpdateTimers()
        {
            float deltaTime = Time.deltaTime;
            
            // Update buffered input timers
            foreach (var inputType in _bufferedInputs.Keys)
            {
                _bufferedInputs[inputType].Update(deltaTime);
                _inputCooldowns[inputType].Update(deltaTime);
            }
        }
        
        private void ProcessBufferedInputs()
        {
            foreach (var kvp in _bufferedInputs)
            {
                if (kvp.Value.IsExpired)
                {
                    kvp.Value.Stop();
                    ExecuteBufferedInput(kvp.Key);
                }
            }
        }
        
        private void BufferInput(InputType inputType)
        {
            if (!_config.allowInputBuffer) return;
            
            _bufferedInputs[inputType].Start(_config.inputBufferTime);
            OnInputBuffered?.Invoke(inputType, _config.inputBufferTime);
            LogDebug($"Input buffered: {inputType} for {_config.inputBufferTime}s");
        }
        
        private void ExecuteBufferedInput(InputType inputType)
        {
            if (CanExecuteInput(inputType))
            {
                ExecuteInput(inputType);
                OnBufferedInputExecuted?.Invoke(inputType);
                LogDebug($"Buffered input executed: {inputType}");
            }
        }
        
        #endregion
        
        #region Private Methods - Input Validation
        
        private bool CanExecuteInput(InputType inputType)
        {
            // Check if in cooldown
            if (_config.preventSpamming && _inputCooldowns[inputType].IsRunning)
            {
                OnInputRejected?.Invoke(inputType, "Input in cooldown");
                return false;
            }
            
            // Check general input rate limiting
            if (Time.time - _lastInputTime < _config.inputCooldown)
            {
                OnInputRejected?.Invoke(inputType, "Input rate limited");
                return false;
            }
            
            return true;
        }
        
        private void ExecuteInput(InputType inputType)
        {
            if (!CanExecuteInput(inputType))
            {
                BufferInput(inputType);
                return;
            }
            
            // Start cooldown
            _inputCooldowns[inputType].Start(_config.inputCooldown);
            _lastInputTime = Time.time;
            
            // Execute input action
            switch (inputType)
            {
                case InputType.Jump:
                    OnJumpRequested?.Invoke();
                    break;
                case InputType.Slide:
                    OnSlideRequested?.Invoke();
                    break;
                case InputType.LaneLeft:
                    OnLaneChangeRequested?.Invoke(_isLeftHandMode ? 1 : -1);
                    break;
                case InputType.LaneRight:
                    OnLaneChangeRequested?.Invoke(_isLeftHandMode ? -1 : 1);
                    break;
                case InputType.ManualItem:
                    OnManualItemRequested?.Invoke();
                    break;
                case InputType.Pause:
                    OnPauseRequested?.Invoke();
                    break;
            }
            
            LogDebug($"Input executed: {inputType}");
        }
        
        #endregion
        
        #region Private Methods - PC Input
        
        private void ProcessPCInput()
        {
            // Process keyboard input using configured keys
            if (Input.GetKeyDown(_jumpKey))
            {
                ExecuteInput(InputType.Jump);
            }
            
            if (Input.GetKeyDown(_slideKey))
            {
                ExecuteInput(InputType.Slide);
            }
            
            if (Input.GetKeyDown(_laneLeftKey))
            {
                ExecuteInput(InputType.LaneLeft);
            }
            
            if (Input.GetKeyDown(_laneRightKey))
            {
                ExecuteInput(InputType.LaneRight);
            }
            
            if (Input.GetKeyDown(_manualItemKey))
            {
                ExecuteInput(InputType.ManualItem);
            }
            
            if (Input.GetKeyDown(_pauseKey))
            {
                ExecuteInput(InputType.Pause);
            }
            
            // Additional fallback keys
            ProcessKeyboardFallback();
        }
        
        private void ProcessKeyboardFallback()
        {
            // Additional common keys for convenience
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                ExecuteInput(InputType.Jump);
            }
            
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ExecuteInput(InputType.Slide);
            }
            
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ExecuteInput(InputType.LaneLeft);
            }
            
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ExecuteInput(InputType.LaneRight);
            }
        }
        
        #endregion
        
        #region Private Methods - Mobile Input
        
        private void ProcessMobileInput()
        {
            // Process touch input
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    ProcessTouch(touch);
                }
            }
            else
            {
                // No touches, reset tracking
                if (_isTouching)
                {
                    HandleTouchEnd();
                }
            }
            
            // Fallback mouse input for testing in editor
            #if UNITY_EDITOR
            ProcessMouseFallback();
            #endif
        }
        
        private void ProcessTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchStart(touch);
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == _activeTouchId)
                    {
                        HandleTouchEnd(touch);
                    }
                    break;
                    
                case TouchPhase.Moved:
                    if (touch.fingerId == _activeTouchId && _isTouching)
                    {
                        // Update swipe data in real-time (for visual feedback if needed)
                        UpdateSwipeData(touch.position);
                    }
                    break;
            }
        }
        
        private void HandleTouchStart(Touch touch)
        {
            if (_isTouching) return; // Ignore multi-touch for now
            
            _isTouching = true;
            _activeTouchId = touch.fingerId;
            _touchStartPos = touch.position;
            _touchStartTime = Time.time;
            
            if (_showSwipeDebug)
            {
                LogDebug($"Touch started at {touch.position}");
            }
        }
        
        private void HandleTouchEnd(Touch? touch = null)
        {
            if (!_isTouching) return;
            
            Vector2 endPos = touch?.position ?? _touchStartPos;
            float duration = Time.time - _touchStartTime;
            
            // Create swipe data
            SwipeData swipeData = new SwipeData(_touchStartPos, endPos, duration);
            
            // Validate and process swipe
            if (ValidateSwipe(swipeData))
            {
                ProcessSwipe(swipeData);
            }
            
            // Reset tracking
            _isTouching = false;
            _activeTouchId = -1;
        }
        
        #if UNITY_EDITOR
        private void ProcessMouseFallback()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isTouching = true;
                _touchStartPos = Input.mousePosition;
                _touchStartTime = Time.time;
            }
            else if (Input.GetMouseButtonUp(0) && _isTouching)
            {
                Vector2 endPos = Input.mousePosition;
                float duration = Time.time - _touchStartTime;
                SwipeData swipeData = new SwipeData(_touchStartPos, endPos, duration);
                
                if (ValidateSwipe(swipeData))
                {
                    ProcessSwipe(swipeData);
                }
                
                _isTouching = false;
            }
        }
        #endif
        
        private void UpdateSwipeData(Vector2 currentPos)
        {
            // Can be used for real-time swipe feedback
            Vector2 delta = currentPos - _touchStartPos;
            if (delta.magnitude > _config.swipeThreshold * 0.5f)
            {
                // Swipe is getting significant, could show visual hint
            }
        }
        
        #endregion
        
        #region Private Methods - Swipe Processing
        
        private bool ValidateSwipe(SwipeData swipeData)
        {
            // Check distance threshold
            if (swipeData.distance < _config.swipeThreshold)
            {
                LogDebug($"Swipe too short: {swipeData.distance}px < {_config.swipeThreshold}px");
                return false;
            }
            
            // Check time constraints
            if (swipeData.duration < _config.minSwipeTime || swipeData.duration > _config.maxSwipeTime)
            {
                LogDebug($"Swipe timing invalid: {swipeData.duration}s (range: {_config.minSwipeTime}-{_config.maxSwipeTime}s)");
                return false;
            }
            
            return true;
        }
        
        private void ProcessSwipe(SwipeData swipeData)
        {
            _lastSwipeData = swipeData;
            
            // Determine swipe direction
            Vector2 direction = swipeData.direction;
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            
            if (_showSwipeDebug)
            {
                LogDebug($"Swipe processed: {swipeData}");
            }
            
            // Determine dominant direction
            if (absY > absX)
            {
                // Vertical swipe
                if (direction.y > 0)
                {
                    // Swipe up = Jump
                    ExecuteInput(InputType.Jump);
                }
                else
                {
                    // Swipe down = Slide
                    ExecuteInput(InputType.Slide);
                }
            }
            else
            {
                // Horizontal swipe
                if (direction.x > 0)
                {
                    // Swipe right
                    ExecuteInput(_isLeftHandMode ? InputType.LaneLeft : InputType.LaneRight);
                }
                else
                {
                    // Swipe left
                    ExecuteInput(_isLeftHandMode ? InputType.LaneRight : InputType.LaneLeft);
                }
            }
        }
        
        #endregion
        
        #region Private Methods - Utilities
        
        private void ClearAllTouches()
        {
            _isTouching = false;
            _activeTouchId = -1;
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[InputHandler] {message}", this);
            }
        }
        
        #endregion
        
        #region Public Debug Methods
        
        /// <summary>
        /// Get debug status của input handler
        /// </summary>
        /// <returns>Debug status string</returns>
        public string GetDebugStatus()
        {
            return $"Platform: {_currentPlatform} | " +
                   $"Enabled: {_isEnabled} | " +
                   $"LeftHand: {_isLeftHandMode} | " +
                   $"Touching: {_isTouching} | " +
                   $"Buffered: {HasBufferedInput} | " +
                   $"LastInput: {Time.time - _lastInputTime:F1}s ago";
        }
        
        /// <summary>
        /// Test specific input type (for debugging)
        /// </summary>
        /// <param name="inputType">Input type to test</param>
        public void TestInput(InputType inputType)
        {
            LogDebug($"Testing input: {inputType}");
            ExecuteInput(inputType);
        }
        
        /// <summary>
        /// Get swipe configuration info
        /// </summary>
        /// <returns>Swipe config string</returns>
        public string GetSwipeConfigInfo()
        {
            return $"Threshold: {_config.swipeThreshold}px | " +
                   $"TimeRange: {_config.minSwipeTime}-{_config.maxSwipeTime}s | " +
                   $"Sensitivity: {_config.swipeSensitivity}x";
        }
        
        #endregion
    }
}
