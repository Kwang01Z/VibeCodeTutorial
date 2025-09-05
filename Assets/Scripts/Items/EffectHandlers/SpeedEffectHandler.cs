using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items
{
    /// <summary>
    /// SpeedEffectHandler - Xử lý speed multiplier effects để modify player speed
    /// </summary>
    public class SpeedEffectHandler : BaseEffectHandler
    {
        #region Serialized Fields

        [Header("Speed Settings")]
        [SerializeField] private bool _modifyForwardSpeed = true;
        [SerializeField] private bool _modifySpeedProgression = true;
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private bool _enableVisualFeedback = true;
        [SerializeField] private Color _speedBoostColor = Color.yellow;
        [SerializeField] private ParticleSystem _speedParticles;

        #endregion

        #region Private Fields

        private float _originalSpeedModifier = 1f;
        private float _currentSpeedModifier = 1f;
        private bool _isSpeedModified = false;

        // Visual feedback
        private Renderer _playerRenderer;
        private Material _originalMaterial;
        private Material _speedBoostMaterial;

        #endregion

        #region BaseEffectHandler Implementation

        public override ItemType HandledItemType => ItemType.Multiplier;

        protected override void OnInitialize()
        {
            // Cache player renderer for visual effects
            if (_enableVisualFeedback)
            {
                CachePlayerRenderer();
                CreateSpeedBoostMaterial();
            }

            LogDebug("[SpeedEffectHandler] Initialized speed system");
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_currentEffect == null) return;

            // Update speed modifier from current effect
            float targetModifier = _currentEffect.CurrentEffectValue;
            
            // Smooth transition to target modifier
            if (Mathf.Abs(_currentSpeedModifier - targetModifier) > 0.01f)
            {
                _currentSpeedModifier = Mathf.Lerp(_currentSpeedModifier, targetModifier, 
                    deltaTime / _transitionDuration);
                
                ApplySpeedModifier(_currentSpeedModifier);
            }

            // Update visual effects
            UpdateVisualEffects();
        }

        protected override void OnEffectStart(ActiveItemEffect effect)
        {
            float multiplierValue = effect.CurrentEffectValue;
            
            LogDebug($"[SpeedEffectHandler] Speed effect started - multiplier: {multiplierValue:F2}x");

            // Apply speed modification
            ApplySpeedModifier(multiplierValue);
            
            // Enable visual feedback
            EnableVisualFeedback(true);
        }

        protected override void OnEffectEnd(ItemType itemType, string itemId)
        {
            LogDebug("[SpeedEffectHandler] Speed effect ended");

            // Reset speed to original
            RestoreOriginalSpeed();
            
            // Disable visual feedback
            EnableVisualFeedback(false);
        }

        protected override void OnEffectStack(ActiveItemEffect effect, int previousStackCount)
        {
            float newMultiplier = effect.CurrentEffectValue;
            
            LogDebug($"[SpeedEffectHandler] Speed effect stacked - new multiplier: {newMultiplier:F2}x");

            // Apply new speed modifier
            ApplySpeedModifier(newMultiplier);
        }

        protected override void OnCleanup()
        {
            RestoreOriginalSpeed();
            CleanupVisualEffects();
        }

        #endregion

        #region Speed Modification

        private void ApplySpeedModifier(float modifier)
        {
            if (!ValidateDependencies(_speedManager)) return;

            _currentSpeedModifier = modifier;

            if (_modifyForwardSpeed)
            {
                // Apply speed modifier to SpeedManager
                _speedManager.SetSpeedModifier(modifier, -1f); // -1f = permanent until manually removed
                _isSpeedModified = true;
            }

            if (_modifySpeedProgression && _speedManager is SpeedManager speedManager)
            {
                // Optionally modify speed progression rate
                // This could affect how quickly the game speeds up over time
                LogDebug($"[SpeedEffectHandler] Speed progression modifier applied: {modifier:F2}x");
            }

            LogDebug($"[SpeedEffectHandler] Applied speed modifier: {modifier:F2}x");
        }

        private void RestoreOriginalSpeed()
        {
            if (!_isSpeedModified || !ValidateDependencies(_speedManager)) return;

            // Remove speed modifier
            _speedManager.SetSpeedModifier(1f, 0f); // Reset to normal speed immediately
            
            _currentSpeedModifier = 1f;
            _isSpeedModified = false;

            LogDebug("[SpeedEffectHandler] Restored original speed");
        }

        #endregion

        #region Visual Effects

        private void CachePlayerRenderer()
        {
            // Try to find renderer on player
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

        private void CreateSpeedBoostMaterial()
        {
            if (_originalMaterial != null)
            {
                _speedBoostMaterial = new Material(_originalMaterial);
                _speedBoostMaterial.color = Color.Lerp(_originalMaterial.color, _speedBoostColor, 0.5f);
                
                // Add emission for glow effect
                if (_speedBoostMaterial.HasProperty("_EmissionColor"))
                {
                    _speedBoostMaterial.SetColor("_EmissionColor", _speedBoostColor * 0.3f);
                    _speedBoostMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        private void EnableVisualFeedback(bool enable)
        {
            // Material change
            if (_playerRenderer != null && _speedBoostMaterial != null)
            {
                _playerRenderer.material = enable ? _speedBoostMaterial : _originalMaterial;
            }

            // Particle effects
            if (_speedParticles != null)
            {
                if (enable)
                {
                    if (!_speedParticles.isPlaying)
                        _speedParticles.Play();
                }
                else
                {
                    if (_speedParticles.isPlaying)
                        _speedParticles.Stop();
                }
            }
        }

        private void UpdateVisualEffects()
        {
            if (!_enableVisualFeedback || _currentEffect == null) return;

            float intensity = GetCurrentEffectIntensity();

            // Update material properties based on intensity
            if (_playerRenderer != null && _speedBoostMaterial != null)
            {
                Color currentColor = Color.Lerp(_originalMaterial.color, _speedBoostColor, intensity * 0.7f);
                _speedBoostMaterial.color = currentColor;

                // Update emission
                if (_speedBoostMaterial.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = _speedBoostColor * intensity * 0.5f;
                    _speedBoostMaterial.SetColor("_EmissionColor", emissionColor);
                }
            }

            // Update particle system
            UpdateParticleEffects(intensity);
        }

        private void UpdateParticleEffects(float intensity)
        {
            if (_speedParticles == null) return;

            var main = _speedParticles.main;
            var emission = _speedParticles.emission;

            // Scale emission rate with intensity
            emission.rateOverTime = 20f * intensity;

            // Scale particle speed with intensity
            main.startSpeed = 5f * intensity;

            // Update color
            var colorOverLifetime = _speedParticles.colorOverLifetime;
            if (colorOverLifetime.enabled)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(_speedBoostColor, 0.0f),
                        new GradientColorKey(Color.white, 1.0f) 
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(intensity, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f) 
                    }
                );
                colorOverLifetime.color = gradient;
            }
        }

        private void CleanupVisualEffects()
        {
            // Restore original material
            if (_playerRenderer != null && _originalMaterial != null)
            {
                _playerRenderer.material = _originalMaterial;
            }

            // Destroy created materials
            if (_speedBoostMaterial != null)
            {
                DestroyImmediate(_speedBoostMaterial);
            }

            // Stop particles
            if (_speedParticles != null && _speedParticles.isPlaying)
            {
                _speedParticles.Stop();
            }
        }

        #endregion

        #region Debug & Utilities

        [ContextMenu("Test Speed Boost")]
        private void TestSpeedBoost()
        {
            if (Application.isPlaying && _speedManager != null)
            {
                ApplySpeedModifier(2.0f);
                LogDebug("Applied test speed boost (2x)");
            }
        }

        [ContextMenu("Reset Speed")]
        private void TestResetSpeed()
        {
            if (Application.isPlaying)
            {
                RestoreOriginalSpeed();
                LogDebug("Reset speed to normal");
            }
        }

        public string GetSpeedStatus()
        {
            return $"Speed Handler Status:\n" +
                   $"- Current Modifier: {_currentSpeedModifier:F2}x\n" +
                   $"- Is Modified: {_isSpeedModified}\n" +
                   $"- Effect Active: {_currentEffect != null}\n" +
                   $"- Visual Feedback: {_enableVisualFeedback}";
        }

        #endregion
    }
}
