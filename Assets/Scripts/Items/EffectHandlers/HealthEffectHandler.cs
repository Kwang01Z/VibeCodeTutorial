using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items
{
    /// <summary>
    /// HealthEffectHandler - Xử lý health/life effects để restore và extend player health
    /// </summary>
    public class HealthEffectHandler : BaseEffectHandler
    {
        #region Serialized Fields

        [Header("Health Settings")]
        [SerializeField] private bool _restoreHealthImmediately = true;
        [SerializeField] private bool _increasesMaxHealth = false;
        [SerializeField] private int _maxBonusHealth = 3;
        [SerializeField] private bool _enableReviveOnDeath = true;

        [Header("Visual Effects")]
        [SerializeField] private bool _enableVisualFeedback = true;
        [SerializeField] private Color _healthBoostColor = Color.green;
        [SerializeField] private ParticleSystem _healingParticles;
        [SerializeField] private float _effectDuration = 1f;

        [Header("Audio")]
        [SerializeField] private AudioClip _healthGainSound;
        [SerializeField] private AudioClip _maxHealthIncreaseSound;
        [SerializeField] private AudioSource _audioSource;

        #endregion

        #region Private Fields

        private int _originalMaxHealth;
        private int _currentBonusHealth = 0;
        private bool _hasSubscribedToHealthEvents = false;

        // Visual effects
        private Renderer _playerRenderer;
        private Material _originalMaterial;
        private Coroutine _visualEffectCoroutine;

        #endregion

        #region BaseEffectHandler Implementation

        public override ItemType HandledItemType => ItemType.Life;

        protected override void OnInitialize()
        {
            // Cache health system info
            if (_healthSystem != null)
            {
                _originalMaxHealth = _healthSystem.MaxHealth;
                SubscribeToHealthEvents();
            }

            // Setup audio
            SetupAudio();

            // Cache visual components
            if (_enableVisualFeedback)
            {
                SetupVisualEffects();
            }

            LogDebug("[HealthEffectHandler] Initialized health system");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // Health effects are usually instant or event-based
            // No continuous updates needed
        }

        protected override void OnEffectStart(ActiveItemEffect effect)
        {
            int healthToRestore = effect.StackCount;
            
            LogDebug($"[HealthEffectHandler] Health effect started - restoring {healthToRestore} health");

            // Apply health restoration
            ApplyHealthEffect(healthToRestore);

            // Play audio feedback
            PlayHealthSound(_healthGainSound);

            // Trigger visual effects
            if (_enableVisualFeedback)
            {
                TriggerHealingVisuals();
            }
        }

        protected override void OnEffectEnd(ItemType itemType, string itemId)
        {
            LogDebug("[HealthEffectHandler] Health effect ended");

            // For life effects, ending doesn't usually remove health
            // But we might reset bonus health if configured to do so
            if (_increasesMaxHealth)
            {
                ResetBonusHealth();
            }
        }

        protected override void OnEffectStack(ActiveItemEffect effect, int previousStackCount)
        {
            int additionalHealth = effect.StackCount - previousStackCount;
            
            LogDebug($"[HealthEffectHandler] Health effect stacked - additional {additionalHealth} health");

            // Apply additional health restoration
            ApplyHealthEffect(additionalHealth);

            // Play stacking sound
            PlayHealthSound(_maxHealthIncreaseSound);

            // Update visual effects
            if (_enableVisualFeedback)
            {
                TriggerHealingVisuals();
            }
        }

        protected override void OnCleanup()
        {
            // Reset any bonus health
            ResetBonusHealth();

            // Unsubscribe from events
            UnsubscribeFromHealthEvents();

            // Stop any ongoing visual effects
            StopVisualEffects();
        }

        #endregion

        #region Health Management

        private void ApplyHealthEffect(int healthAmount)
        {
            if (!ValidateDependencies(_healthSystem)) return;

            if (_restoreHealthImmediately)
            {
                // Restore health immediately
                _healthSystem.RestoreHealth(healthAmount);
                LogDebug($"[HealthEffectHandler] Restored {healthAmount} health immediately");
            }

            if (_increasesMaxHealth && _currentBonusHealth < _maxBonusHealth)
            {
                // Increase max health (if supported by health system)
                int bonusToAdd = Mathf.Min(healthAmount, _maxBonusHealth - _currentBonusHealth);
                
                if (bonusToAdd > 0)
                {
                    AddBonusHealth(bonusToAdd);
                    LogDebug($"[HealthEffectHandler] Added {bonusToAdd} bonus health");
                }
            }
        }

        private void AddBonusHealth(int bonusAmount)
        {
            _currentBonusHealth += bonusAmount;

            // Note: This depends on IHealthSystem supporting max health modification
            // If your health system doesn't support this, you might need to extend it
            // or track bonus health separately
            LogDebug($"[HealthEffectHandler] Bonus health added: {bonusAmount} (total: {_currentBonusHealth})");
        }

        private void ResetBonusHealth()
        {
            if (_currentBonusHealth > 0)
            {
                _currentBonusHealth = 0;
                LogDebug("[HealthEffectHandler] Bonus health reset");
            }
        }

        #endregion

        #region Health Event Handling

        private void SubscribeToHealthEvents()
        {
            if (_hasSubscribedToHealthEvents || _healthSystem == null) return;

            _healthSystem.OnHealthChanged += OnHealthChanged;
            _healthSystem.OnDeath += OnPlayerDeath;

            if (_healthSystem is IHealthSystem healthSystem)
            {
                // Subscribe to additional events if available
            }

            _hasSubscribedToHealthEvents = true;
        }

        private void UnsubscribeFromHealthEvents()
        {
            if (!_hasSubscribedToHealthEvents || _healthSystem == null) return;

            _healthSystem.OnHealthChanged -= OnHealthChanged;
            _healthSystem.OnDeath -= OnPlayerDeath;

            _hasSubscribedToHealthEvents = false;
        }

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            LogDebug($"[HealthEffectHandler] Health changed: {currentHealth}/{maxHealth}");

            // Could trigger additional effects based on health changes
            // For example, different visual effects based on health percentage
        }

        private void OnPlayerDeath()
        {
            if (_enableReviveOnDeath && _currentEffect != null && _currentEffect.StackCount > 0)
            {
                LogDebug("[HealthEffectHandler] Attempting revive on death");
                
                // Consume one stack for revive
                // Note: This would require modifying the current effect's stack count
                // which might need additional implementation in the effect system
                AttemptRevive();
            }
        }

        private void AttemptRevive()
        {
            // Revive player with partial health
            if (_healthSystem != null)
            {
                _healthSystem.RestoreHealth(1); // Revive with 1 health
                LogDebug("[HealthEffectHandler] Player revived!");

                // Play special revive sound/effects
                PlayHealthSound(_maxHealthIncreaseSound);
                
                if (_enableVisualFeedback)
                {
                    TriggerReviveVisuals();
                }
            }
        }

        #endregion

        #region Audio

        private void SetupAudio()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                    _audioSource.spatialBlend = 0f; // 2D sound
                }
            }
        }

        private void PlayHealthSound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Visual Effects

        private void SetupVisualEffects()
        {
            // Cache player renderer
            _playerRenderer = GetComponent<Renderer>();
            if (_playerRenderer == null)
            {
                _playerRenderer = GetComponentInChildren<Renderer>();
            }

            if (_playerRenderer != null)
            {
                _originalMaterial = _playerRenderer.material;
            }
        }

        private void TriggerHealingVisuals()
        {
            // Stop any existing visual effect
            StopVisualEffects();

            // Start new healing visual effect
            _visualEffectCoroutine = StartCoroutine(HealingVisualEffect());

            // Trigger particle system if available
            if (_healingParticles != null)
            {
                _healingParticles.Play();
            }
        }

        private void TriggerReviveVisuals()
        {
            // More dramatic visual effect for revive
            _visualEffectCoroutine = StartCoroutine(ReviveVisualEffect());

            // Enhanced particle effects for revive
            if (_healingParticles != null)
            {
                var main = _healingParticles.main;
                var emission = _healingParticles.emission;
                
                // Temporarily boost particle system
                float originalRate = emission.rateOverTime.constant;
                emission.rateOverTime = originalRate * 3f;
                
                _healingParticles.Play();

                // Reset after effect
                StartCoroutine(ResetParticleSystemAfterDelay(2f, originalRate));
            }
        }

        private void StopVisualEffects()
        {
            if (_visualEffectCoroutine != null)
            {
                StopCoroutine(_visualEffectCoroutine);
                _visualEffectCoroutine = null;
            }

            // Restore original material
            if (_playerRenderer != null && _originalMaterial != null)
            {
                _playerRenderer.material = _originalMaterial;
            }
        }

        #endregion

        #region Visual Effect Coroutines

        private System.Collections.IEnumerator HealingVisualEffect()
        {
            if (_playerRenderer == null || _originalMaterial == null) yield break;

            // Create healing material
            Material healingMaterial = new Material(_originalMaterial);
            Color originalColor = healingMaterial.color;
            Color healingColor = Color.Lerp(originalColor, _healthBoostColor, 0.5f);

            float elapsed = 0f;

            while (elapsed < _effectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _effectDuration;

                // Pulse effect
                float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;
                Color currentColor = Color.Lerp(originalColor, healingColor, pulse * (1f - t));
                healingMaterial.color = currentColor;

                _playerRenderer.material = healingMaterial;

                yield return null;
            }

            // Restore original material
            _playerRenderer.material = _originalMaterial;
            DestroyImmediate(healingMaterial);

            _visualEffectCoroutine = null;
        }

        private System.Collections.IEnumerator ReviveVisualEffect()
        {
            if (_playerRenderer == null || _originalMaterial == null) yield break;

            // More dramatic revive effect
            Material reviveMaterial = new Material(_originalMaterial);
            Color originalColor = reviveMaterial.color;

            float elapsed = 0f;
            float reviveDuration = _effectDuration * 2f; // Longer for revive

            while (elapsed < reviveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / reviveDuration;

                // Flash effect with multiple colors
                float flash = Mathf.Sin(t * Mathf.PI * 8f);
                Color flashColor = flash > 0 ? Color.white : _healthBoostColor;
                Color currentColor = Color.Lerp(originalColor, flashColor, Mathf.Abs(flash) * (1f - t));

                reviveMaterial.color = currentColor;
                _playerRenderer.material = reviveMaterial;

                yield return null;
            }

            // Restore original material
            _playerRenderer.material = _originalMaterial;
            DestroyImmediate(reviveMaterial);

            _visualEffectCoroutine = null;
        }

        private System.Collections.IEnumerator ResetParticleSystemAfterDelay(float delay, float originalRate)
        {
            yield return new WaitForSeconds(delay);

            if (_healingParticles != null)
            {
                var emission = _healingParticles.emission;
                emission.rateOverTime = originalRate;
            }
        }

        #endregion

        #region Debug & Utilities

        [ContextMenu("Test Health Gain")]
        private void TestHealthGain()
        {
            if (Application.isPlaying)
            {
                ApplyHealthEffect(1);
                LogDebug("Applied test health gain");
            }
        }

        [ContextMenu("Test Revive")]
        private void TestRevive()
        {
            if (Application.isPlaying)
            {
                AttemptRevive();
                LogDebug("Attempted test revive");
            }
        }

        public string GetHealthStatus()
        {
            string healthInfo = "N/A";
            if (_healthSystem != null)
            {
                healthInfo = $"{_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}";
            }

            return $"Health Handler Status:\n" +
                   $"- Current Health: {healthInfo}\n" +
                   $"- Original Max Health: {_originalMaxHealth}\n" +
                   $"- Bonus Health: {_currentBonusHealth}\n" +
                   $"- Effect Active: {_currentEffect != null}\n" +
                   $"- Visual Feedback: {_enableVisualFeedback}";
        }

        #endregion
    }
}
