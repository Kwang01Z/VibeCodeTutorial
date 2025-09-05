using System;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Zero-allocation timer struct với pause/resume support.
    /// Sử dụng cho coyote time, input buffer, item duration, I-frames, etc.
    /// </summary>
    [Serializable]
    public struct Timer
    {
        private float _duration;
        private float _elapsedTime;
        private bool _isRunning;
        private bool _isPaused;
        
        /// <summary>
        /// Timer đang chạy (không pause)
        /// </summary>
        public readonly bool IsRunning => _isRunning && !_isPaused;
        
        /// <summary>
        /// Timer đã hết thời gian
        /// </summary>
        public readonly bool IsExpired => _isRunning && _elapsedTime >= _duration;
        
        /// <summary>
        /// Tiến độ từ 0.0 đến 1.0
        /// </summary>
        public readonly float Progress => _duration > 0 ? Math.Min(_elapsedTime / _duration, 1f) : 0f;
        
        /// <summary>
        /// Thời gian còn lại (seconds)
        /// </summary>
        public readonly float TimeRemaining => _isRunning ? Math.Max(0, _duration - _elapsedTime) : 0f;
        
        /// <summary>
        /// Thời gian đã trôi qua (seconds)
        /// </summary>
        public readonly float ElapsedTime => _elapsedTime;
        
        /// <summary>
        /// Tổng thời gian duration (seconds)
        /// </summary>
        public readonly float Duration => _duration;
        
        /// <summary>
        /// Bắt đầu timer với duration cụ thể
        /// </summary>
        /// <param name="duration">Thời gian chạy (seconds)</param>
        public void Start(float duration)
        {
            _duration = Math.Max(0, duration);
            _elapsedTime = 0f;
            _isRunning = true;
            _isPaused = false;
        }
        
        /// <summary>
        /// Dừng timer hoàn toàn
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
            _elapsedTime = 0f;
        }
        
        /// <summary>
        /// Tạm dừng timer (có thể resume)
        /// </summary>
        public void Pause()
        {
            if (_isRunning)
            {
                _isPaused = true;
            }
        }
        
        /// <summary>
        /// Tiếp tục timer từ trạng thái pause
        /// </summary>
        public void Resume()
        {
            if (_isRunning)
            {
                _isPaused = false;
            }
        }
        
        /// <summary>
        /// Reset timer về trạng thái ban đầu (giữ nguyên duration)
        /// </summary>
        public void Reset()
        {
            if (_isRunning)
            {
                _elapsedTime = 0f;
                _isPaused = false;
            }
        }
        
        /// <summary>
        /// Cập nhật timer với deltaTime. Gọi trong Update/FixedUpdate.
        /// </summary>
        /// <param name="deltaTime">Time.deltaTime hoặc Time.fixedDeltaTime</param>
        public void Update(float deltaTime)
        {
            if (IsRunning && !IsExpired)
            {
                _elapsedTime += deltaTime;
            }
        }
        
        /// <summary>
        /// Cập nhật và kiểm tra expire trong một call
        /// </summary>
        /// <param name="deltaTime">Delta time</param>
        /// <returns>True nếu timer vừa expire trong frame này</returns>
        public bool UpdateAndCheckExpired(float deltaTime)
        {
            bool wasExpired = IsExpired;
            Update(deltaTime);
            return !wasExpired && IsExpired;
        }
        
        /// <summary>
        /// Thêm thời gian vào timer đang chạy
        /// </summary>
        /// <param name="additionalTime">Thời gian thêm vào (seconds)</param>
        public void AddTime(float additionalTime)
        {
            if (_isRunning)
            {
                _duration += Math.Max(0, additionalTime);
            }
        }
        
        /// <summary>
        /// Tạo timer mới và start ngay
        /// </summary>
        /// <param name="duration">Duration (seconds)</param>
        /// <returns>Timer đã start</returns>
        public static Timer StartNew(float duration)
        {
            Timer timer = new Timer();
            timer.Start(duration);
            return timer;
        }
        
        /// <summary>
        /// Timer đã hoàn thành (expired và không còn chạy)
        /// </summary>
        public readonly bool IsCompleted => !_isRunning || IsExpired;
        
        /// <summary>
        /// Override ToString cho debugging
        /// </summary>
        public override readonly string ToString()
        {
            if (!_isRunning) return "Timer: Stopped";
            if (_isPaused) return $"Timer: Paused ({_elapsedTime:F2}s/{_duration:F2}s)";
            if (IsExpired) return $"Timer: Expired ({_duration:F2}s)";
            return $"Timer: Running ({_elapsedTime:F2}s/{_duration:F2}s - {Progress:P1})";
        }
    }
}
