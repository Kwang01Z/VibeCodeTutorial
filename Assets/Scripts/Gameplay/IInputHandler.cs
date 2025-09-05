using System;
using UnityEngine;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface cho Input Handler - xử lý input từ PC và mobile
    /// </summary>
    public interface IInputHandler
    {
        #region Properties
        
        /// <summary>
        /// Input handler có enabled không
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Chế độ tay trái (mobile)
        /// </summary>
        bool IsLeftHandMode { get; }
        
        /// <summary>
        /// Platform hiện tại đang chạy
        /// </summary>
        InputPlatform CurrentPlatform { get; }
        
        /// <summary>
        /// Input buffer có đang active không
        /// </summary>
        bool HasBufferedInput { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Bật/tắt left-hand mode (mirror UI/input cho mobile)
        /// </summary>
        /// <param name="enabled">True để bật left-hand mode</param>
        void SetLeftHandMode(bool enabled);
        
        /// <summary>
        /// Clear tất cả buffered input
        /// </summary>
        void ClearInputBuffer();
        
        /// <summary>
        /// Force set platform (cho testing)
        /// </summary>
        /// <param name="platform">Platform cần set</param>
        void SetPlatform(InputPlatform platform);
        
        /// <summary>
        /// Update sensitivity cho mobile swipe
        /// </summary>
        /// <param name="sensitivity">Sensitivity value (0.5-2.0)</param>
        void SetSwipeSensitivity(float sensitivity);
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event khi yêu cầu nhảy
        /// </summary>
        event Action OnJumpRequested;
        
        /// <summary>
        /// Event khi yêu cầu trượt (slide)
        /// </summary>
        event Action OnSlideRequested;
        
        /// <summary>
        /// Event khi yêu cầu chuyển làn
        /// </summary>
        /// <param name="direction">-1 cho trái, +1 cho phải</param>
        event Action<int> OnLaneChangeRequested;
        
        /// <summary>
        /// Event khi yêu cầu dùng manual item
        /// </summary>
        event Action OnManualItemRequested;
        
        /// <summary>
        /// Event khi yêu cầu pause game
        /// </summary>
        event Action OnPauseRequested;
        
        /// <summary>
        /// Event khi input bị buffer (không thể execute ngay)
        /// </summary>
        /// <param name="inputType">Loại input bị buffer</param>
        /// <param name="bufferTime">Thời gian buffer</param>
        event Action<InputType, float> OnInputBuffered;
        
        /// <summary>
        /// Event khi buffered input được execute
        /// </summary>
        /// <param name="inputType">Loại input được execute</param>
        event Action<InputType> OnBufferedInputExecuted;
        
        /// <summary>
        /// Event khi input bị reject (không hợp lệ)
        /// </summary>
        /// <param name="inputType">Loại input bị reject</param>
        /// <param name="reason">Lý do reject</param>
        event Action<InputType, string> OnInputRejected;
        
        #endregion
    }
    
    /// <summary>
    /// Enum cho các platform input
    /// </summary>
    public enum InputPlatform
    {
        /// <summary>Auto-detect platform</summary>
        Auto,
        
        /// <summary>PC with keyboard input</summary>
        PC,
        
        /// <summary>Mobile with touch input</summary>
        Mobile,
        
        /// <summary>Console with gamepad (future)</summary>
        Console
    }
    
    /// <summary>
    /// Enum cho các loại input
    /// </summary>
    public enum InputType
    {
        /// <summary>Nhảy</summary>
        Jump,
        
        /// <summary>Trượt</summary>
        Slide,
        
        /// <summary>Chuyển làn trái</summary>
        LaneLeft,
        
        /// <summary>Chuyển làn phải</summary>
        LaneRight,
        
        /// <summary>Dùng manual item</summary>
        ManualItem,
        
        /// <summary>Pause</summary>
        Pause
    }
    
    /// <summary>
    /// Data structure cho swipe gesture
    /// </summary>
    [System.Serializable]
    public struct SwipeData
    {
        public Vector2 startPosition;
        public Vector2 endPosition;
        public Vector2 direction;
        public float distance;
        public float duration;
        public bool isValid;
        
        public SwipeData(Vector2 start, Vector2 end, float time)
        {
            startPosition = start;
            endPosition = end;
            direction = (end - start).normalized;
            distance = Vector2.Distance(start, end);
            duration = time;
            isValid = false; // Will be validated by InputHandler
        }
        
        public override string ToString()
        {
            return $"Swipe: {startPosition} -> {endPosition}, dist={distance:F1}px, time={duration:F2}s, dir={direction}";
        }
    }
    
    /// <summary>
    /// Configuration cho input detection
    /// </summary>
    [System.Serializable]
    public struct InputConfig
    {
        [Header("Mobile Swipe")]
        public float swipeThreshold;
        public float maxSwipeTime;
        public float minSwipeTime;
        public float swipeSensitivity;
        
        [Header("Input Buffer")]
        public float inputBufferTime;
        public bool allowInputBuffer;
        
        [Header("Validation")]
        public float inputCooldown;
        public bool preventSpamming;
        
        public static InputConfig Default => new InputConfig
        {
            swipeThreshold = 100f,
            maxSwipeTime = 0.5f,
            minSwipeTime = 0.05f,
            swipeSensitivity = 1.0f,
            inputBufferTime = 0.1f,
            allowInputBuffer = true,
            inputCooldown = 0.05f,
            preventSpamming = true
        };
    }
}
