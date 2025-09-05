using System;
using UnityEngine;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface cho health management system với I-frames support.
    /// Được implement bởi HealthComponent cho player health tracking.
    /// </summary>
    public interface IHealthSystem
    {
        /// <summary>
        /// Số mạng hiện tại (0 = chết)
        /// </summary>
        int CurrentHealth { get; }
        
        /// <summary>
        /// Số mạng tối đa
        /// </summary>
        int MaxHealth { get; }
        
        /// <summary>
        /// Đang trong trạng thái bất tử (I-frames) hay không
        /// </summary>
        bool IsInIFrames { get; }
        
        /// <summary>
        /// Còn sống hay đã chết
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Thời gian còn lại của I-frames (seconds)
        /// </summary>
        float IFramesTimeRemaining { get; }
        
        /// <summary>
        /// Nhận damage và trigger I-frames
        /// </summary>
        /// <param name="amount">Số mạng mất (default = 1)</param>
        /// <param name="damageSource">Nguồn damage cho logging/analytics</param>
        /// <returns>True nếu damage được apply (không trong I-frames)</returns>
        bool TakeDamage(int amount = 1, string damageSource = "Unknown");
        
        /// <summary>
        /// Hồi phục sức khỏe
        /// </summary>
        /// <param name="amount">Số mạng hồi (default = 1)</param>
        /// <returns>True nếu heal được apply (không full health)</returns>
        bool RestoreHealth(int amount = 1);
        
        /// <summary>
        /// Kích hoạt I-frames với thời lượng cụ thể
        /// </summary>
        /// <param name="duration">Thời lượng I-frames (seconds)</param>
        void SetIFrames(float duration);
        
        /// <summary>
        /// Reset health về max và clear I-frames
        /// </summary>
        void ResetToFullHealth();
        
        /// <summary>
        /// Set health trực tiếp (dùng cho cheats/debug)
        /// </summary>
        /// <param name="health">Health mới</param>
        void SetHealth(int health);
        
        // Events
        
        /// <summary>
        /// Fired khi health thay đổi (damage hoặc heal)
        /// </summary>
        event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
        
        /// <summary>
        /// Fired khi player chết (health <= 0)
        /// </summary>
        event Action OnDeath;
        
        /// <summary>
        /// Fired khi I-frames bắt đầu
        /// </summary>
        event Action<float> OnIFramesStarted; // (duration)
        
        /// <summary>
        /// Fired khi I-frames kết thúc
        /// </summary>
        event Action OnIFramesEnded;
        
        /// <summary>
        /// Fired khi nhận damage (kể cả blocked bởi I-frames)
        /// </summary>
        event Action<int, string, bool> OnDamageTaken; // (amount, source, wasBlocked)
        
        /// <summary>
        /// Fired khi được heal
        /// </summary>
        event Action<int> OnHealthRestored; // (amount)
    }
    
    /// <summary>
    /// Extension methods cho IHealthSystem
    /// </summary>
    public static class HealthSystemExtensions
    {
        /// <summary>
        /// Kiểm tra có phải full health không
        /// </summary>
        public static bool IsFullHealth(this IHealthSystem health)
        {
            return health.CurrentHealth >= health.MaxHealth;
        }
        
        /// <summary>
        /// Lấy health percentage [0.0f, 1.0f]
        /// </summary>
        public static float GetHealthPercentage(this IHealthSystem health)
        {
            if (health.MaxHealth <= 0) return 0f;
            return Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
        }
        
        /// <summary>
        /// Kiểm tra có critical health không (≤ 1 heart)
        /// </summary>
        public static bool IsCriticalHealth(this IHealthSystem health)
        {
            return health.CurrentHealth <= 1 && health.IsAlive;
        }
        
        /// <summary>
        /// Lấy I-frames progress [0.0f, 1.0f] (0 = just started, 1 = almost done)
        /// </summary>
        public static float GetIFramesProgress(this IHealthSystem health)
        {
            if (!health.IsInIFrames) return 1f;
            
            // Cần access tới total duration để tính progress
            // Implementation sẽ override trong HealthComponent
            return 0f;
        }
    }
}
