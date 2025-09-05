using System;
using System.Collections;
using UnityEngine;
using EndlessRunner.Core;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Damage feedback system cung cấp visual, audio, và haptic feedback khi player nhận damage.
    /// Subscribe vào HealthComponent events để trigger effects.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    [DisallowMultipleComponent]
    public class DamageFeedback : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Visual Effects")]
        [SerializeField, Tooltip("Renderer để apply flash effect")]
        private Renderer _targetRenderer;
        
        [SerializeField, Tooltip("Material gốc (auto-cached nếu null)")]
        private Material _originalMaterial;
        
        [SerializeField, Tooltip("Flash material cho damage effect")]
        private Material _flashMaterial; // TODO: Create flash material hoặc use color tint
        
        [SerializeField, Tooltip("Flash duration (seconds)")]
        [Range(0.1f, 2f)]
        private float _flashDuration = 0.2f;
        
        [SerializeField, Tooltip("Flash blink count during I-frames")]
        [Range(1, 10)]
        private int _iFrameBlinkCount = 5;
        
        [Header("Screen Effects")]
        [SerializeField, Tooltip("Camera shake intensity")]
        [Range(0f, 2f)]
        private float _shakeIntensity = 0.3f;
        
        [SerializeField, Tooltip("Camera shake duration (seconds)")]
        [Range(0.1f, 1f)]
        private float _shakeDuration = 0.15f;
        
        [SerializeField, Tooltip("Screen flash color (alpha controls intensity)")]
        private Color _screenFlashColor = new Color(1f, 0.3f, 0.3f, 0.3f);
        
        [Header("Audio Effects")]
        [SerializeField, Tooltip("Hit sound effect")]
        private AudioClip _hitSFX; // TODO: Assign audio clip
        
        [SerializeField, Tooltip("Critical health warning sound")]
        private AudioClip _criticalHealthSFX;
        
        [SerializeField, Tooltip("Death sound effect")]
        private AudioClip _deathSFX;
        
        [SerializeField, Tooltip("Audio source for SFX (auto-found nếu null)")]
        private AudioSource _audioSource;
        
        [SerializeField, Tooltip("Hit SFX volume")]
        [Range(0f, 1f)]
        private float _sfxVolume = 0.8f;
        
        [Header("Haptic Feedback")]
        [SerializeField, Tooltip("Enable haptic feedback (mobile)")]
        private bool _enableHaptics = true;
        
        [SerializeField, Tooltip("Hit haptic intensity (0=light, 1=medium, 2=heavy)")]
        [Range(0, 2)]
        private int _hitHapticIntensity = 1;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logs")]
        private bool _debugMode = true;
        
        #endregion
        
        #region Private Fields
        
        private IHealthSystem _healthSystem;
        private UnityEngine.Camera _mainCamera;
        private Vector3 _originalCameraPosition;
        private Timer _flashTimer;
        private Timer _shakeTimer;
        private Coroutine _iFrameBlinkCoroutine;
        private bool _isFlashing = false;
        
        // Screen flash overlay (TODO: implement với UI overlay)
        private CanvasGroup _screenFlashOverlay;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
            InitializeMaterials();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            ValidateSetup();
        }
        
        private void Update()
        {
            UpdateTimers();
            UpdateCameraShake();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            // Cleanup coroutines
            if (_iFrameBlinkCoroutine != null)
            {
                StopCoroutine(_iFrameBlinkCoroutine);
            }
            
            // Restore original material
            RestoreOriginalMaterial();
        }
        
        #endregion
        
        #region Initialization
        
        private void CacheComponents()
        {
            _healthSystem = GetComponent<IHealthSystem>();
            _mainCamera = UnityEngine.Camera.main;
            
            if (_targetRenderer == null)
                _targetRenderer = GetComponent<Renderer>();
                
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>() ?? FindObjectOfType<AudioSource>();
                
            if (_mainCamera != null)
                _originalCameraPosition = _mainCamera.transform.position;
                
            LogDebug("[DamageFeedback] Components cached");
        }
        
        private void InitializeMaterials()
        {
            if (_targetRenderer != null && _originalMaterial == null)
            {
                _originalMaterial = _targetRenderer.material;
                LogDebug($"[DamageFeedback] Original material cached: {_originalMaterial.name}");
            }
            
            // TODO: Create default flash material nếu chưa assign
            if (_flashMaterial == null)
            {
                LogDebug("[DamageFeedback] Flash material not assigned - will use color tinting");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnDamageTaken += HandleDamageTaken;
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnDeath += HandlePlayerDeath;
                _healthSystem.OnIFramesStarted += HandleIFramesStarted;
                _healthSystem.OnIFramesEnded += HandleIFramesEnded;
                
                LogDebug("[DamageFeedback] Subscribed to health system events");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnDamageTaken -= HandleDamageTaken;
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnDeath -= HandlePlayerDeath;
                _healthSystem.OnIFramesStarted -= HandleIFramesStarted;
                _healthSystem.OnIFramesEnded -= HandleIFramesEnded;
            }
        }
        
        private void ValidateSetup()
        {
            if (_healthSystem == null)
            {
                Debug.LogError("[DamageFeedback] IHealthSystem component not found!", this);
                enabled = false;
                return;
            }
            
            if (_targetRenderer == null)
                Debug.LogWarning("[DamageFeedback] Target renderer not set - visual effects disabled", this);
                
            if (_audioSource == null)
                Debug.LogWarning("[DamageFeedback] Audio source not found - audio effects disabled", this);
                
            if (_mainCamera == null)
                Debug.LogWarning("[DamageFeedback] Main camera not found - screen effects disabled", this);
        }
        
        #endregion
        
        #region Timer Management
        
        private void UpdateTimers()
        {
            _flashTimer.Update(Time.deltaTime);
            _shakeTimer.Update(Time.deltaTime);
            
            // End flash effect khi timer hết
            if (_isFlashing && _flashTimer.IsExpired)
            {
                EndFlashEffect();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle khi player nhận damage (kể cả blocked damage)
        /// </summary>
        private void HandleDamageTaken(int amount, string source, bool wasBlocked)
        {
            LogDebug($"[DamageFeedback] Damage taken: {amount} from {source}, blocked: {wasBlocked}");
            
            if (wasBlocked)
            {
                // Blocked damage - subtle feedback
                TriggerBlockedDamageFeedback();
            }
            else
            {
                // Real damage - full feedback
                TriggerDamageFeedback(amount, source);
            }
        }
        
        /// <summary>
        /// Handle khi health thay đổi
        /// </summary>
        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            LogDebug($"[DamageFeedback] Health changed: {currentHealth}/{maxHealth}");
            
            // Play critical health warning nếu ở 1 tim cuối
            if (currentHealth == 1 && currentHealth < maxHealth)
            {
                TriggerCriticalHealthFeedback();
            }
        }
        
        /// <summary>
        /// Handle khi player chết
        /// </summary>
        private void HandlePlayerDeath()
        {
            LogDebug("[DamageFeedback] Player death feedback");
            TriggerDeathFeedback();
        }
        
        /// <summary>
        /// Handle khi I-frames bắt đầu
        /// </summary>
        private void HandleIFramesStarted(float duration)
        {
            LogDebug($"[DamageFeedback] I-frames started: {duration}s");
            StartIFrameVisualFeedback(duration);
        }
        
        /// <summary>
        /// Handle khi I-frames kết thúc
        /// </summary>
        private void HandleIFramesEnded()
        {
            LogDebug("[DamageFeedback] I-frames ended");
            EndIFrameVisualFeedback();
        }
        
        #endregion
        
        #region Feedback Methods
        
        /// <summary>
        /// Trigger full damage feedback (flash, shake, sound, haptic)
        /// </summary>
        private void TriggerDamageFeedback(int damageAmount, string source)
        {
            // Visual effects
            TriggerFlashEffect();
            TriggerCameraShake();
            
            // Audio effect
            PlaySFX(_hitSFX);
            
            // Haptic feedback
            TriggerHapticFeedback(_hitHapticIntensity);
            
            // Screen flash (TODO: implement)
            // TriggerScreenFlash();
            
            LogDebug($"[DamageFeedback] Full damage feedback triggered for {damageAmount} damage from {source}");
        }
        
        /// <summary>
        /// Trigger subtle feedback cho blocked damage
        /// </summary>
        private void TriggerBlockedDamageFeedback()
        {
            // Light visual feedback only
            if (_targetRenderer != null && _originalMaterial != null)
            {
                // Quick flash với reduced intensity
                StartCoroutine(QuickFlashCoroutine(0.1f, 0.5f));
            }
            
            LogDebug("[DamageFeedback] Blocked damage feedback");
        }
        
        /// <summary>
        /// Trigger critical health warning
        /// </summary>
        private void TriggerCriticalHealthFeedback()
        {
            PlaySFX(_criticalHealthSFX);
            TriggerHapticFeedback(0); // Light haptic
            
            LogDebug("[DamageFeedback] Critical health feedback");
        }
        
        /// <summary>
        /// Trigger death feedback
        /// </summary>
        private void TriggerDeathFeedback()
        {
            // Strong effects
            TriggerCameraShake(_shakeIntensity * 1.5f, _shakeDuration * 1.5f);
            PlaySFX(_deathSFX);
            TriggerHapticFeedback(2); // Heavy haptic
            
            // Long flash effect
            TriggerFlashEffect(_flashDuration * 2f);
            
            LogDebug("[DamageFeedback] Death feedback triggered");
        }
        
        #endregion
        
        #region Visual Effects
        
        /// <summary>
        /// Start flash effect trên renderer
        /// </summary>
        private void TriggerFlashEffect(float duration = -1f)
        {
            if (_targetRenderer == null) return;
            
            if (duration < 0) duration = _flashDuration;
            
            _flashTimer.Start(duration);
            _isFlashing = true;
            
            // Apply flash material hoặc tint
            if (_flashMaterial != null)
            {
                _targetRenderer.material = _flashMaterial;
            }
            else
            {
                // TODO: Use MaterialPropertyBlock để tint color thay vì swap material
                // This avoids creating material instances
                LogDebug("[DamageFeedback] Flash material not available - using fallback tinting");
            }
        }
        
        /// <summary>
        /// End flash effect và restore original material
        /// </summary>
        private void EndFlashEffect()
        {
            if (!_isFlashing) return;
            
            _isFlashing = false;
            RestoreOriginalMaterial();
        }
        
        /// <summary>
        /// Restore original material
        /// </summary>
        private void RestoreOriginalMaterial()
        {
            if (_targetRenderer != null && _originalMaterial != null)
            {
                _targetRenderer.material = _originalMaterial;
            }
        }
        
        /// <summary>
        /// Start I-frame visual feedback (blinking effect)
        /// </summary>
        private void StartIFrameVisualFeedback(float duration)
        {
            if (_iFrameBlinkCoroutine != null)
            {
                StopCoroutine(_iFrameBlinkCoroutine);
            }
            
            _iFrameBlinkCoroutine = StartCoroutine(IFrameBlinkCoroutine(duration));
        }
        
        /// <summary>
        /// End I-frame visual feedback
        /// </summary>
        private void EndIFrameVisualFeedback()
        {
            if (_iFrameBlinkCoroutine != null)
            {
                StopCoroutine(_iFrameBlinkCoroutine);
                _iFrameBlinkCoroutine = null;
            }
            
            // Ensure renderer is visible
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = true;
            }
        }
        
        /// <summary>
        /// Coroutine cho blinking effect trong I-frames
        /// </summary>
        private IEnumerator IFrameBlinkCoroutine(float duration)
        {
            if (_targetRenderer == null) yield break;
            
            float blinkInterval = duration / (_iFrameBlinkCount * 2); // *2 for on/off cycles
            
            for (int i = 0; i < _iFrameBlinkCount; i++)
            {
                // Blink off
                _targetRenderer.enabled = false;
                yield return new WaitForSeconds(blinkInterval);
                
                // Blink on
                _targetRenderer.enabled = true;
                yield return new WaitForSeconds(blinkInterval);
            }
            
            // Ensure visible at end
            _targetRenderer.enabled = true;
            _iFrameBlinkCoroutine = null;
        }
        
        /// <summary>
        /// Quick flash coroutine với custom duration và intensity
        /// </summary>
        private IEnumerator QuickFlashCoroutine(float duration, float intensity)
        {
            // TODO: Implement color tinting với MaterialPropertyBlock
            yield return new WaitForSeconds(duration);
        }
        
        #endregion
        
        #region Screen Effects
        
        /// <summary>
        /// Trigger camera shake
        /// </summary>
        private void TriggerCameraShake(float intensity = -1f, float duration = -1f)
        {
            if (_mainCamera == null) return;
            
            if (intensity < 0) intensity = _shakeIntensity;
            if (duration < 0) duration = _shakeDuration;
            
            _shakeTimer.Start(duration);
            
            LogDebug($"[DamageFeedback] Camera shake: intensity={intensity}, duration={duration}");
        }
        
        /// <summary>
        /// Update camera shake effect trong Update()
        /// </summary>
        private void UpdateCameraShake()
        {
            if (_mainCamera == null || !_shakeTimer.IsRunning) return;
            
            // Calculate shake offset
            float shakeStrength = _shakeIntensity * (1f - _shakeTimer.Progress); // Fade out
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * shakeStrength;
            randomOffset.z = 0; // Keep Z position unchanged
            
            // Apply shake
            _mainCamera.transform.position = _originalCameraPosition + randomOffset;
            
            // Reset position khi shake ends
            if (_shakeTimer.IsExpired)
            {
                _mainCamera.transform.position = _originalCameraPosition;
            }
        }
        
        #endregion
        
        #region Audio Effects
        
        /// <summary>
        /// Play sound effect
        /// </summary>
        private void PlaySFX(AudioClip clip)
        {
            if (_audioSource == null || clip == null) return;
            
            _audioSource.PlayOneShot(clip, _sfxVolume);
            LogDebug($"[DamageFeedback] Playing SFX: {clip.name}");
        }
        
        #endregion
        
        #region Haptic Effects
        
        /// <summary>
        /// Trigger haptic feedback trên mobile devices
        /// </summary>
        private void TriggerHapticFeedback(int intensity)
        {
            if (!_enableHaptics) return;
            
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android haptic feedback
            try 
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                
                switch (intensity)
                {
                    case 0: // Light
                        vibrator.Call("vibrate", 50L);
                        break;
                    case 1: // Medium
                        vibrator.Call("vibrate", 100L);
                        break;
                    case 2: // Heavy
                        vibrator.Call("vibrate", 200L);
                        break;
                }
                
                LogDebug($"[DamageFeedback] Android haptic triggered: intensity={intensity}");
            }
            catch (System.Exception e)
            {
                LogDebug($"[DamageFeedback] Android haptic failed: {e.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS haptic feedback - would need native plugin
            LogDebug($"[DamageFeedback] iOS haptic triggered: intensity={intensity} (requires native plugin)");
#else
            // Editor/other platforms
            LogDebug($"[DamageFeedback] Haptic feedback simulated: intensity={intensity}");
#endif
        }
        
        #endregion
        
        #region Debug & Utility
        
        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log(message, this);
            }
        }
        
        /// <summary>
        /// Get current feedback status cho debugging
        /// </summary>
        public string GetFeedbackStatus()
        {
            return $"Flash: {_isFlashing}, Shake: {_shakeTimer.IsRunning}, " +
                   $"IFrameBlink: {_iFrameBlinkCoroutine != null}";
        }
        
        #endregion
        
        #region Editor Debug Methods
        
#if UNITY_EDITOR
        [ContextMenu("Test Damage Feedback")]
        private void TestDamageFeedback()
        {
            if (Application.isPlaying)
            {
                TriggerDamageFeedback(1, "Debug");
            }
        }
        
        [ContextMenu("Test I-Frame Feedback")]
        private void TestIFrameFeedback()
        {
            if (Application.isPlaying)
            {
                StartIFrameVisualFeedback(2f);
            }
        }
        
        [ContextMenu("Test Camera Shake")]
        private void TestCameraShake()
        {
            if (Application.isPlaying)
            {
                TriggerCameraShake();
            }
        }
#endif
        
        #endregion
    }
}
