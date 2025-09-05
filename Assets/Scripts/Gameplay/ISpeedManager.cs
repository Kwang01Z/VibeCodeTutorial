using System;
using UnityEngine;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface for Speed Management System
    /// Quản lý tốc độ runner dựa trên distance với smooth acceleration
    /// </summary>
    public interface ISpeedManager
    {
        #region Properties
        
        /// <summary>
        /// Tốc độ hiện tại của runner (m/s)
        /// </summary>
        float CurrentSpeed { get; }
        
        /// <summary>
        /// Tốc độ target theo curve (m/s)
        /// </summary>
        float TargetSpeed { get; }
        
        /// <summary>
        /// Quãng đường đã chạy (m)
        /// </summary>
        float DistanceRun { get; }
        
        /// <summary>
        /// Thời gian đã chạy (seconds)
        /// </summary>
        float TimeRunning { get; }
        
        /// <summary>
        /// Có đang pause speed progression không
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// Speed modifier hiện tại (default 1.0)
        /// </summary>
        float SpeedModifier { get; }
        
        #endregion
        
        #region Control Methods
        
        /// <summary>
        /// Bắt đầu speed progression
        /// </summary>
        void StartProgression();
        
        /// <summary>
        /// Pause speed progression (giữ nguyên speed hiện tại)
        /// </summary>
        void PauseProgression();
        
        /// <summary>
        /// Resume speed progression
        /// </summary>
        void ResumeProgression();
        
        /// <summary>
        /// Reset về trạng thái ban đầu
        /// </summary>
        void ResetProgression();
        
        /// <summary>
        /// Set speed modifier tạm thời (VD: hit slowdown, boost item)
        /// </summary>
        /// <param name="modifier">Modifier value (1.0 = normal)</param>
        /// <param name="duration">Thời gian áp dụng (0 = permanent)</param>
        void SetSpeedModifier(float modifier, float duration = 0f);
        
        /// <summary>
        /// Clear speed modifier về normal
        /// </summary>
        void ClearSpeedModifier();
        
        /// <summary>
        /// Set tốc độ cụ thể (override curve tạm thời)
        /// </summary>
        /// <param name="speed">Target speed</param>
        /// <param name="duration">Thời gian duy trì</param>
        void SetFixedSpeed(float speed, float duration);
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Get tốc độ tại distance cụ thể
        /// </summary>
        /// <param name="distance">Distance in meters</param>
        /// <returns>Speed at that distance</returns>
        float GetSpeedAtDistance(float distance);
        
        /// <summary>
        /// Get progress theo curve (0-1)
        /// </summary>
        /// <returns>Progress value</returns>
        float GetProgressPercent();
        
        /// <summary>
        /// Kiểm tra có đạt milestone distance không
        /// </summary>
        /// <param name="milestone">Milestone distance</param>
        /// <returns>True nếu đã vượt milestone</returns>
        bool HasReachedMilestone(float milestone);
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event khi speed thay đổi
        /// (newSpeed, oldSpeed, targetSpeed)
        /// </summary>
        event Action<float, float, float> OnSpeedChanged;
        
        /// <summary>
        /// Event khi đạt distance milestone
        /// (milestone, totalDistance)
        /// </summary>
        event Action<float, float> OnDistanceMilestone;
        
        /// <summary>
        /// Event khi tốc độ đạt mức mới (speed tiers)
        /// (speedTier, speed)
        /// </summary>
        event Action<int, float> OnSpeedTierReached;
        
        /// <summary>
        /// Event khi bắt đầu/pause/resume progression
        /// (isRunning, distance, speed)
        /// </summary>
        event Action<bool, float, float> OnProgressionStateChanged;
        
        /// <summary>
        /// Event khi speed modifier được áp dụng
        /// (modifier, duration, reason)
        /// </summary>
        event Action<float, float, string> OnSpeedModifierApplied;
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Get debug information string
        /// </summary>
        /// <returns>Debug info</returns>
        string GetDebugInfo();
        
        #endregion
    }
    
    /// <summary>
    /// Speed milestone data for tracking
    /// </summary>
    [System.Serializable]
    public struct SpeedMilestone
    {
        public float distance;
        public float targetSpeed;
        public bool isReached;
        
        public SpeedMilestone(float distance, float targetSpeed)
        {
            this.distance = distance;
            this.targetSpeed = targetSpeed;
            this.isReached = false;
        }
    }
}
