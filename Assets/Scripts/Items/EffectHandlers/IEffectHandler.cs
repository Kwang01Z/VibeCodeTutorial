using System.Collections.Generic;
using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Interface cho tất cả effect handlers
    /// Định nghĩa contract cho việc xử lý effects
    /// </summary>
    public interface IEffectHandler
    {
        #region Properties

        /// <summary>
        /// Có được initialize chưa
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Item type mà handler này xử lý
        /// </summary>
        ItemType HandledItemType { get; }

        /// <summary>
        /// Có đang active không
        /// </summary>
        bool IsActive { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initialize handler với dependencies
        /// </summary>
        void Initialize(ItemEffectIntegrator integrator, RunnerController runner, IHealthSystem health, SpeedManager speed);

        /// <summary>
        /// Update handler mỗi frame (nếu cần)
        /// </summary>
        void UpdateHandler(float deltaTime);

        /// <summary>
        /// Cleanup resources
        /// </summary>
        void Cleanup();

        #endregion

        #region Effect Events

        /// <summary>
        /// Called khi effect bắt đầu
        /// </summary>
        void OnEffectStarted(ActiveItemEffect effect);

        /// <summary>
        /// Called khi effect kết thúc
        /// </summary>
        void OnEffectEnded(ItemType itemType, string itemId);

        /// <summary>
        /// Called khi effect được stack
        /// </summary>
        void OnEffectStacked(ActiveItemEffect effect, int previousStackCount);

        /// <summary>
        /// Called khi effect được update (timer, value changes)
        /// </summary>
        void OnEffectUpdated(ActiveItemEffect effect);

        /// <summary>
        /// Called khi tất cả effects thay đổi
        /// </summary>
        void OnAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects);

        #endregion
    }

    /// <summary>
    /// Base class cho tất cả effect handlers
    /// Provides common functionality và structure
    /// </summary>
    public abstract class BaseEffectHandler : MonoBehaviour, IEffectHandler
    {
        #region Protected Fields

        [Header("Base Handler Settings")]
        [SerializeField] protected bool _enableDebugLog = false;
        [SerializeField] protected bool _isActive = true;

        // Dependencies
        protected ItemEffectIntegrator _integrator;
        protected RunnerController _runnerController;
        protected IHealthSystem _healthSystem;
        protected SpeedManager _speedManager;

        // State
        protected bool _isInitialized = false;
        protected ActiveItemEffect _currentEffect;

        #endregion

        #region IEffectHandler Properties

        public virtual bool IsInitialized => _isInitialized;
        public abstract ItemType HandledItemType { get; }
        public virtual bool IsActive => _isActive && _isInitialized;

        #endregion

        #region IEffectHandler Implementation

        public virtual void Initialize(ItemEffectIntegrator integrator, RunnerController runner, IHealthSystem health, SpeedManager speed)
        {
            _integrator = integrator;
            _runnerController = runner;
            _healthSystem = health;
            _speedManager = speed;

            // Perform custom initialization
            OnInitialize();

            _isInitialized = true;
            LogDebug($"[{GetType().Name}] Initialized for {HandledItemType}");
        }

        public virtual void UpdateHandler(float deltaTime)
        {
            if (!IsActive) return;

            OnUpdate(deltaTime);
        }

        public virtual void Cleanup()
        {
            OnCleanup();
            
            _currentEffect = null;
            _isInitialized = false;
            LogDebug($"[{GetType().Name}] Cleaned up");
        }

        public virtual void OnEffectStarted(ActiveItemEffect effect)
        {
            if (effect.ItemDefinition.Type != HandledItemType) return;

            _currentEffect = effect;
            OnEffectStart(effect);
            LogDebug($"[{GetType().Name}] Effect started: {effect.ItemDefinition.DisplayName}");
        }

        public virtual void OnEffectEnded(ItemType itemType, string itemId)
        {
            if (itemType != HandledItemType) return;

            OnEffectEnd(itemType, itemId);
            _currentEffect = null;
            LogDebug($"[{GetType().Name}] Effect ended: {itemType}");
        }

        public virtual void OnEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            if (effect.ItemDefinition.Type != HandledItemType) return;

            _currentEffect = effect;
            OnEffectStack(effect, previousStackCount);
            LogDebug($"[{GetType().Name}] Effect stacked: {effect.ItemDefinition.DisplayName} (x{effect.StackCount})");
        }

        public virtual void OnEffectUpdated(ActiveItemEffect effect)
        {
            if (effect.ItemDefinition.Type != HandledItemType) return;

            _currentEffect = effect;
            OnEffectUpdate(effect);
        }

        public virtual void OnAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects)
        {
            OnAllEffectsChange(allEffects);
        }

        #endregion

        #region Abstract/Virtual Methods

        /// <summary>
        /// Override để thực hiện custom initialization
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Override để thực hiện per-frame updates
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// Override để thực hiện cleanup
        /// </summary>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// Override để xử lý khi effect bắt đầu
        /// </summary>
        protected abstract void OnEffectStart(ActiveItemEffect effect);

        /// <summary>
        /// Override để xử lý khi effect kết thúc
        /// </summary>
        protected abstract void OnEffectEnd(ItemType itemType, string itemId);

        /// <summary>
        /// Override để xử lý khi effect được stack
        /// </summary>
        protected virtual void OnEffectStack(ActiveItemEffect effect, int previousStackCount)
        {
            // Default: same as effect start
            OnEffectStart(effect);
        }

        /// <summary>
        /// Override để xử lý effect updates
        /// </summary>
        protected virtual void OnEffectUpdate(ActiveItemEffect effect) { }

        /// <summary>
        /// Override để xử lý khi tất cả effects thay đổi
        /// </summary>
        protected virtual void OnAllEffectsChange(Dictionary<string, ActiveItemEffect> allEffects) { }

        #endregion

        #region Utilities

        protected void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }

        protected void LogWarning(string message)
        {
            if (_enableDebugLog)
            {
                Debug.LogWarning(message, this);
            }
        }

        protected void LogError(string message)
        {
            Debug.LogError(message, this);
        }

        /// <summary>
        /// Kiểm tra có dependency cần thiết không
        /// </summary>
        protected bool ValidateDependencies(params object[] dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (dependency == null)
                {
                    LogError($"[{GetType().Name}] Missing required dependency: {dependency?.GetType().Name ?? "null"}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get current effect intensity (0-1 range)
        /// </summary>
        protected float GetCurrentEffectIntensity()
        {
            return _currentEffect?.GetEffectIntensity() ?? 0f;
        }

        #endregion

        #region Debug

        [ContextMenu("Show Current Effect")]
        protected void ShowCurrentEffect()
        {
            if (_currentEffect != null)
            {
                Debug.Log($"Current Effect: {_currentEffect}", this);
            }
            else
            {
                Debug.Log("No current effect", this);
            }
        }

        #endregion
    }
}
