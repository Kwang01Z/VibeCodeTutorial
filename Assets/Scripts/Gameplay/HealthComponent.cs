using System;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Health management component với I-frames support.
    /// Quản lý player health, damage immunity, và related events.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthComponent : MonoBehaviour, IHealthSystem
    {
        [Header("Health Settings")]
        [SerializeField, Tooltip("Số mạng tối đa")]
        [Range(1, 10)]
        private int _maxHealth = 3;
        
        [SerializeField, Tooltip("Health khi start game (default = max)")]
        [Range(0, 10)]
        private int _startHealth = -1; // -1 = use max health
        
        [Header("I-Frames Settings")]
        [SerializeField, Tooltip("Thời lượng I-frames sau khi nhận damage (seconds)")]
        [Range(0.1f, 5f)]
        private float _defaultIFrameDuration = 1.2f;
        
        [SerializeField, Tooltip("I-frames sau revive (seconds)")]
        [Range(0.5f, 5f)]
        private float _reviveIFrameDuration = 2.5f;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Hiển thị debug logs")]
        private bool _debugMode = true;
        
        [SerializeField, Tooltip("Invincible mode cho testing")]
        private bool _invincibleMode = false;
        
        // Runtime state
        private int _currentHealth;
        private Timer _iFrameTimer;
        private bool _isDead = false;
        
        // Events - implement IHealthSystem
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;
        public event Action<float> OnIFramesStarted;
        public event Action OnIFramesEnded;
        public event Action<int, string, bool> OnDamageTaken;
        public event Action<int> OnHealthRestored;
        
        // Properties - implement IHealthSystem
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsInIFrames => _iFrameTimer.IsRunning;
        public bool IsAlive => _currentHealth > 0 && !_isDead;
        public float IFramesTimeRemaining => _iFrameTimer.IsRunning ? _iFrameTimer.TimeRemaining : 0f;
        
        private void Awake()
        {
            // Initialize health
            int startHealth = _startHealth < 0 ? _maxHealth : _startHealth;
            _currentHealth = Mathf.Clamp(startHealth, 0, _maxHealth);
            _isDead = _currentHealth <= 0;
            
            // Initialize timer
            _iFrameTimer = new Timer();
            
            if (_debugMode)
            {
                Debug.Log($"[HealthComponent] Initialized với {_currentHealth}/{_maxHealth} health", this);
            }
        }
        
        private void Start()
        {
            // Fire initial health changed event
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
        
        private void Update()
        {
            // Update I-frames timer
            bool wasInIFrames = _iFrameTimer.IsRunning;
            _iFrameTimer.Update(Time.deltaTime);
            
            // Check if I-frames just ended
            if (wasInIFrames && !_iFrameTimer.IsRunning)
            {
                OnIFramesEnded?.Invoke();
                
                if (_debugMode)
                {
                    Debug.Log("[HealthComponent] I-frames ended", this);
                }
            }
        }
        
        public bool TakeDamage(int amount = 1, string damageSource = "Unknown")
        {
            // Validate parameters
            if (amount <= 0)
            {
                Debug.LogWarning($"[HealthComponent] Invalid damage amount: {amount}", this);
                return false;
            }
            
            // Check invincible mode
            if (_invincibleMode)
            {
                OnDamageTaken?.Invoke(amount, damageSource, true);
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Damage blocked by invincible mode: {amount} from {damageSource}", this);
                }
                return false;
            }
            
            // Check I-frames
            bool wasBlocked = IsInIFrames;
            OnDamageTaken?.Invoke(amount, damageSource, wasBlocked);
            
            if (wasBlocked)
            {
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Damage blocked by I-frames: {amount} from {damageSource}", this);
                }
                return false;
            }
            
            // Check if already dead
            if (!IsAlive)
            {
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Damage ignored - already dead: {amount} from {damageSource}", this);
                }
                return false;
            }
            
            // Apply damage
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            
            // Trigger I-frames after taking damage
            SetIFrames(_defaultIFrameDuration);
            
            // Fire events
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            
            if (_debugMode)
            {
                Debug.Log($"[HealthComponent] Took {amount} damage from {damageSource}. Health: {oldHealth} → {_currentHealth}", this);
            }
            
            // Check for death
            if (_currentHealth <= 0 && !_isDead)
            {
                _isDead = true;
                OnDeath?.Invoke();
                
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Player died from {damageSource}", this);
                }
            }
            
            return true;
        }
        
        public bool RestoreHealth(int amount = 1)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[HealthComponent] Invalid heal amount: {amount}", this);
                return false;
            }
            
            // Can't heal if dead (need revive instead)
            if (!IsAlive)
            {
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Heal ignored - player is dead", this);
                }
                return false;
            }
            
            // Can't heal if already at max
            if (_currentHealth >= _maxHealth)
            {
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Heal ignored - already at max health", this);
                }
                return false;
            }
            
            // Apply healing
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            
            // Fire events
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnHealthRestored?.Invoke(amount);
            
            if (_debugMode)
            {
                Debug.Log($"[HealthComponent] Restored {amount} health. Health: {oldHealth} → {_currentHealth}", this);
            }
            
            return true;
        }
        
        public void SetIFrames(float duration)
        {
            if (duration <= 0f)
            {
                Debug.LogWarning($"[HealthComponent] Invalid I-frames duration: {duration}", this);
                return;
            }
            
            // Start or extend I-frames
            bool wasAlreadyInIFrames = _iFrameTimer.IsRunning;
            _iFrameTimer.Start(duration);
            
            if (!wasAlreadyInIFrames)
            {
                OnIFramesStarted?.Invoke(duration);
                
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Started I-frames for {duration}s", this);
                }
            }
            else
            {
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Extended I-frames to {duration}s", this);
                }
            }
        }
        
        public void ResetToFullHealth()
        {
            int oldHealth = _currentHealth;
            _currentHealth = _maxHealth;
            _isDead = false;
            
            // Clear I-frames
            _iFrameTimer.Stop();
            
            // Fire events if health changed
            if (oldHealth != _currentHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                
                if (oldHealth <= 0)
                {
                    // Coming back from death
                    if (_debugMode)
                    {
                        Debug.Log("[HealthComponent] Revived to full health", this);
                    }
                }
                else
                {
                    OnHealthRestored?.Invoke(_currentHealth - oldHealth);
                    
                    if (_debugMode)
                    {
                        Debug.Log($"[HealthComponent] Reset to full health: {oldHealth} → {_currentHealth}", this);
                    }
                }
            }
        }
        
        public void SetHealth(int health)
        {
            int clampedHealth = Mathf.Clamp(health, 0, _maxHealth);
            int oldHealth = _currentHealth;
            _currentHealth = clampedHealth;
            
            // Update dead state
            bool wasDeadBefore = _isDead;
            _isDead = _currentHealth <= 0;
            
            // Fire events if health changed
            if (oldHealth != _currentHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                
                if (_debugMode)
                {
                    Debug.Log($"[HealthComponent] Health set directly: {oldHealth} → {_currentHealth}", this);
                }
                
                // Check for death
                if (_currentHealth <= 0 && !wasDeadBefore)
                {
                    OnDeath?.Invoke();
                    
                    if (_debugMode)
                    {
                        Debug.Log("[HealthComponent] Player died from SetHealth", this);
                    }
                }
            }
        }
        
        /// <summary>
        /// Revive player với I-frames extended
        /// </summary>
        public void Revive()
        {
            if (IsAlive)
            {
                Debug.LogWarning("[HealthComponent] Revive called but player is not dead", this);
                return;
            }
            
            ResetToFullHealth();
            SetIFrames(_reviveIFrameDuration);
            
            if (_debugMode)
            {
                Debug.Log($"[HealthComponent] Player revived với {_reviveIFrameDuration}s I-frames", this);
            }
        }
        
        /// <summary>
        /// Get I-frames progress [0.0f, 1.0f]
        /// </summary>
        public float GetIFramesProgress()
        {
            if (!IsInIFrames) return 1f;
            return _iFrameTimer.Progress;
        }
        
        /// <summary>
        /// Debug info cho Inspector
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Health: {_currentHealth}/{_maxHealth}\n" +
                   $"Alive: {IsAlive}\n" +
                   $"I-frames: {IsInIFrames} ({IFramesTimeRemaining:F2}s remaining)\n" +
                   $"Invincible: {_invincibleMode}";
        }
        
        private void OnValidate()
        {
            // Clamp values trong Inspector
            _maxHealth = Mathf.Max(1, _maxHealth);
            _defaultIFrameDuration = Mathf.Max(0.1f, _defaultIFrameDuration);
            _reviveIFrameDuration = Mathf.Max(0.1f, _reviveIFrameDuration);
            
            if (_startHealth >= 0)
            {
                _startHealth = Mathf.Clamp(_startHealth, 0, _maxHealth);
            }
        }
        
        private void OnDisable()
        {
            // Clear events để tránh memory leaks
            OnHealthChanged = null;
            OnDeath = null;
            OnIFramesStarted = null;
            OnIFramesEnded = null;
            OnDamageTaken = null;
            OnHealthRestored = null;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Debug controls trong Inspector
        /// </summary>
        [Space(10)]
        [Header("Debug Controls (Runtime Only)")]
        [SerializeField, Range(0, 10)] private int _debugSetHealth = 3;
        [SerializeField, Range(0.1f, 5f)] private float _debugIFramesDuration = 1f;
        
        [ContextMenu("Debug: Take 1 Damage")]
        private void DebugTakeDamage()
        {
            if (Application.isPlaying)
            {
                TakeDamage(1, "Debug");
            }
        }
        
        [ContextMenu("Debug: Restore 1 Health")]
        private void DebugRestoreHealth()
        {
            if (Application.isPlaying)
            {
                RestoreHealth(1);
            }
        }
        
        [ContextMenu("Debug: Set I-Frames")]
        private void DebugSetIFrames()
        {
            if (Application.isPlaying)
            {
                SetIFrames(_debugIFramesDuration);
            }
        }
        
        [ContextMenu("Debug: Reset To Full Health")]
        private void DebugResetHealth()
        {
            if (Application.isPlaying)
            {
                ResetToFullHealth();
            }
        }
        
        [ContextMenu("Debug: Set Health")]
        private void DebugSetHealthValue()
        {
            if (Application.isPlaying)
            {
                SetHealth(_debugSetHealth);
            }
        }
        
        [ContextMenu("Debug: Revive")]
        private void DebugRevive()
        {
            if (Application.isPlaying)
            {
                Revive();
            }
        }
        
        [ContextMenu("Debug: Toggle Invincible")]
        private void DebugToggleInvincible()
        {
            if (Application.isPlaying)
            {
                _invincibleMode = !_invincibleMode;
                Debug.Log($"[HealthComponent] Invincible mode: {_invincibleMode}", this);
            }
        }
#endif
    }
}
