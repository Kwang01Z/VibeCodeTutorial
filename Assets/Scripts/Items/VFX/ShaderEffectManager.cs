using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;
using EndlessRunner.Data;
using EndlessRunner.Core;

namespace EndlessRunner.Items.VFX
{
    /// <summary>
    /// ShaderEffectManager - Advanced shader-based effects for item system
    /// Handles screen effects, material swapping, and shader property animations
    /// </summary>
    public class ShaderEffectManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Player Materials")]
        [SerializeField] private Renderer _playerRenderer;
        [SerializeField] private Material _defaultPlayerMaterial;
        [SerializeField] private Material _magnetEffectMaterial;
        [SerializeField] private Material _speedEffectMaterial;
        [SerializeField] private Material _invisibilityEffectMaterial;
        [SerializeField] private Material _healingEffectMaterial;

        [Header("Screen Effects")]
        [SerializeField] private Material _screenDistortionMaterial;
        [SerializeField] private Material _screenTintMaterial;
        [SerializeField] private UnityEngine.Camera _effectCamera;

        [Header("Effect Configurations")]
        [SerializeField] private ShaderEffectConfig[] _effectConfigs;
        [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _transitionSpeed = 2f;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private float _lodDistance = 30f;
        [SerializeField] private int _maxConcurrentEffects = 5;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _visualizeEffects = false;

        #endregion

        #region Private Fields

        // Active shader effects tracking
        private Dictionary<ItemType, ActiveShaderEffect> _activeEffects = new Dictionary<ItemType, ActiveShaderEffect>();
        private Dictionary<ItemType, ShaderEffectConfig> _configLookup = new Dictionary<ItemType, ShaderEffectConfig>();
        
        // Material instances for runtime modification
        private Dictionary<ItemType, Material> _materialInstances = new Dictionary<ItemType, Material>();
        private Material _currentPlayerMaterial;

        // Animation coroutines
        private Dictionary<ItemType, Coroutine> _animationCoroutines = new Dictionary<ItemType, Coroutine>();

        // Cached components
        private Transform _playerTransform;
        private UnityEngine.Camera _mainCamera;

        // Shader property IDs (cached for performance)
        private static readonly int _EffectIntensity = Shader.PropertyToID("_EffectIntensity");
        private static readonly int _EffectColor = Shader.PropertyToID("_EffectColor");
        private static readonly int _Time = Shader.PropertyToID("_Time");
        private static readonly int _Speed = Shader.PropertyToID("_Speed");
        private static readonly int _Distortion = Shader.PropertyToID("_Distortion");
        private static readonly int _Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int _Pulse = Shader.PropertyToID("_Pulse");
        private static readonly int _Glow = Shader.PropertyToID("_Glow");

        #endregion

        #region Properties

        public int ActiveEffectCount => _activeEffects.Count;
        public bool HasActiveEffects => _activeEffects.Count > 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeShaderEffects();
            CacheReferences();
            SubscribeToEvents();
        }

        private void Start()
        {
            ValidateConfiguration();
            SetupDefaultMaterial();
        }

        private void Update()
        {
            UpdateShaderEffects();
            UpdateLOD();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupMaterials();
        }

        #endregion

        #region Initialization

        private void InitializeShaderEffects()
        {
            // Create material instances to avoid modifying originals
            CreateMaterialInstances();
            
            // Setup config lookup
            foreach (var config in _effectConfigs)
            {
                if (config != null)
                {
                    _configLookup[config.itemType] = config;
                }
            }

            LogDebug("[ShaderEffectManager] Initialized shader effects system");
        }

        private void CreateMaterialInstances()
        {
            if (_magnetEffectMaterial != null)
                _materialInstances[ItemType.Magnet] = new Material(_magnetEffectMaterial);
                
            if (_speedEffectMaterial != null)
                _materialInstances[ItemType.Multiplier] = new Material(_speedEffectMaterial);
                
            if (_invisibilityEffectMaterial != null)
                _materialInstances[ItemType.Invisible] = new Material(_invisibilityEffectMaterial);
                
            if (_healingEffectMaterial != null)
                _materialInstances[ItemType.Life] = new Material(_healingEffectMaterial);
        }

        private void CacheReferences()
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            _mainCamera = UnityEngine.Camera.main;

            if (_playerRenderer == null && _playerTransform != null)
                _playerRenderer = _playerTransform.GetComponent<Renderer>();

            if (_effectCamera == null)
                _effectCamera = _mainCamera;
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            ItemEffectEvents.OnEffectStarted.AddListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.AddListener(OnEffectStacked);
            ItemEffectEvents.OnAllEffectsChanged.AddListener(OnAllEffectsChanged);
        }

        private void UnsubscribeFromEvents()
        {
            ItemEffectEvents.OnEffectStarted.RemoveListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.RemoveListener(OnEffectStacked);
            ItemEffectEvents.OnAllEffectsChanged.RemoveListener(OnAllEffectsChanged);
        }

        #endregion

        #region Event Handlers

        private void OnEffectStarted(ActiveItemEffect effect)
        {
            StartShaderEffect(effect.ItemDefinition.Type, effect.ItemDefinition.EffectColor, effect.RemainingTime);
        }

        private void OnEffectEnded(ItemType itemType, string itemId)
        {
            EndShaderEffect(itemType);
        }

        private void OnEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            UpdateStackingEffect(effect.ItemDefinition.Type, effect.StackCount, effect.ItemDefinition.EffectColor);
        }

        private void OnAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects)
        {
            UpdateAllShaderEffects(allEffects);
        }

        #endregion

        #region Shader Effect Management

        public void StartShaderEffect(ItemType itemType, Color effectColor, float duration)
        {
            if (!_configLookup.TryGetValue(itemType, out ShaderEffectConfig config))
            {
                LogDebug($"[ShaderEffectManager] No shader config found for {itemType}");
                return;
            }

            // Stop existing effect if running
            if (_activeEffects.ContainsKey(itemType))
            {
                EndShaderEffect(itemType);
            }

            // Create new active effect
            var activeEffect = new ActiveShaderEffect
            {
                itemType = itemType,
                config = config,
                effectColor = effectColor,
                duration = duration,
                startTime = Time.unscaledTime,
                intensity = 0f,
                isActive = true
            };

            _activeEffects[itemType] = activeEffect;

            // Start animation coroutine
            if (_animationCoroutines.ContainsKey(itemType))
            {
                StopCoroutine(_animationCoroutines[itemType]);
            }
            _animationCoroutines[itemType] = StartCoroutine(AnimateShaderEffect(activeEffect));

            // Apply material changes
            ApplyShaderEffect(activeEffect);

            LogDebug($"[ShaderEffectManager] Started shader effect for {itemType}");
        }

        public void EndShaderEffect(ItemType itemType)
        {
            if (!_activeEffects.ContainsKey(itemType)) return;

            var activeEffect = _activeEffects[itemType];
            activeEffect.isActive = false;

            // Stop animation coroutine
            if (_animationCoroutines.ContainsKey(itemType))
            {
                StopCoroutine(_animationCoroutines[itemType]);
                _animationCoroutines.Remove(itemType);
            }

            // Start fade out animation
            StartCoroutine(FadeOutShaderEffect(activeEffect));

            _activeEffects.Remove(itemType);

            LogDebug($"[ShaderEffectManager] Ended shader effect for {itemType}");
        }

        public void UpdateStackingEffect(ItemType itemType, int stackCount, Color effectColor)
        {
            if (!_activeEffects.TryGetValue(itemType, out ActiveShaderEffect activeEffect))
                return;

            // Update intensity based on stack count
            float stackMultiplier = 1f + (stackCount - 1) * 0.3f; // 30% intensity increase per stack
            activeEffect.intensity = Mathf.Clamp01(activeEffect.intensity * stackMultiplier);
            activeEffect.effectColor = Color.Lerp(activeEffect.effectColor, effectColor, 0.5f);

            UpdateShaderProperties(activeEffect);

            LogDebug($"[ShaderEffectManager] Updated stacking effect for {itemType}, stacks: {stackCount}");
        }

        #endregion

        #region Material Management

        private void ApplyShaderEffect(ActiveShaderEffect effect)
        {
            switch (effect.config.effectType)
            {
                case ShaderEffectType.PlayerMaterial:
                    ApplyPlayerMaterialEffect(effect);
                    break;
                case ShaderEffectType.ScreenEffect:
                    ApplyScreenEffect(effect);
                    break;
                case ShaderEffectType.WorldEffect:
                    ApplyWorldEffect(effect);
                    break;
            }
        }

        private void ApplyPlayerMaterialEffect(ActiveShaderEffect effect)
        {
            if (_playerRenderer == null) return;

            if (_materialInstances.TryGetValue(effect.itemType, out Material materialInstance))
            {
                _playerRenderer.material = materialInstance;
                _currentPlayerMaterial = materialInstance;
                UpdateShaderProperties(effect);
            }
        }

        private void ApplyScreenEffect(ActiveShaderEffect effect)
        {
            // Screen effects would typically use post-processing or camera effects
            // Implementation depends on your rendering pipeline
            if (_effectCamera != null)
            {
                // Add screen effect logic here
                LogDebug($"[ShaderEffectManager] Applying screen effect for {effect.itemType}");
            }
        }

        private void ApplyWorldEffect(ActiveShaderEffect effect)
        {
            // World effects could affect environment materials, lighting, etc.
            LogDebug($"[ShaderEffectManager] Applying world effect for {effect.itemType}");
        }

        private void UpdateShaderProperties(ActiveShaderEffect effect)
        {
            if (_currentPlayerMaterial == null) return;

            var material = _currentPlayerMaterial;
            var config = effect.config;

            // Update common properties
            if (material.HasProperty(_EffectIntensity))
                material.SetFloat(_EffectIntensity, effect.intensity);

            if (material.HasProperty(_EffectColor))
                material.SetColor(_EffectColor, effect.effectColor);

            if (material.HasProperty(_Time))
                material.SetFloat(_Time, Time.unscaledTime);

            // Update type-specific properties
            UpdateTypeSpecificProperties(material, effect);
        }

        private void UpdateTypeSpecificProperties(Material material, ActiveShaderEffect effect)
        {
            switch (effect.itemType)
            {
                case ItemType.Magnet:
                    if (material.HasProperty(_Pulse))
                        material.SetFloat(_Pulse, Mathf.Sin(Time.unscaledTime * 5f) * 0.5f + 0.5f);
                    break;

                case ItemType.Multiplier:
                    if (material.HasProperty(_Speed))
                        material.SetFloat(_Speed, effect.intensity * 10f);
                    break;

                case ItemType.Invisible:
                    if (material.HasProperty(_Alpha))
                        material.SetFloat(_Alpha, 1f - effect.intensity * 0.7f); // Fade to 30% alpha
                    break;

                case ItemType.Life:
                    if (material.HasProperty(_Glow))
                        material.SetFloat(_Glow, effect.intensity * 2f);
                    break;
            }
        }

        private void SetupDefaultMaterial()
        {
            if (_playerRenderer != null && _defaultPlayerMaterial != null)
            {
                _playerRenderer.material = _defaultPlayerMaterial;
                _currentPlayerMaterial = _defaultPlayerMaterial;
            }
        }

        #endregion

        #region Animation

        private IEnumerator AnimateShaderEffect(ActiveShaderEffect effect)
        {
            var config = effect.config;
            float animationDuration = config.fadeInDuration;
            float elapsed = 0f;

            // Fade in
            while (elapsed < animationDuration && effect.isActive)
            {
                float t = elapsed / animationDuration;
                float curveValue = _intensityCurve.Evaluate(t);
                
                effect.intensity = Mathf.Lerp(0f, config.maxIntensity, curveValue);
                UpdateShaderProperties(effect);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Sustain phase
            effect.intensity = config.maxIntensity;
            
            while (effect.isActive)
            {
                // Update any time-based effects
                UpdateShaderProperties(effect);
                
                // Check if effect should end naturally
                if (effect.duration > 0f && Time.unscaledTime - effect.startTime >= effect.duration)
                {
                    effect.isActive = false;
                    break;
                }
                
                yield return null;
            }
        }

        private IEnumerator FadeOutShaderEffect(ActiveShaderEffect effect)
        {
            var config = effect.config;
            float animationDuration = config.fadeOutDuration;
            float elapsed = 0f;
            float startIntensity = effect.intensity;

            while (elapsed < animationDuration)
            {
                float t = elapsed / animationDuration;
                effect.intensity = Mathf.Lerp(startIntensity, 0f, t);
                UpdateShaderProperties(effect);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Reset to default material if no other effects active
            if (_activeEffects.Count == 0)
            {
                SetupDefaultMaterial();
            }
        }

        #endregion

        #region Update Methods

        private void UpdateShaderEffects()
        {
            foreach (var effect in _activeEffects.Values)
            {
                if (effect.isActive)
                {
                    UpdateShaderProperties(effect);
                }
            }
        }

        private void UpdateAllShaderEffects(Dictionary<string, ActiveItemEffect> allEffects)
        {
            // Update continuous shader effects based on active item effects
            foreach (var kvp in allEffects)
            {
                var effect = kvp.Value;
                if (_activeEffects.ContainsKey(effect.ItemDefinition.Type))
                {
                    var shaderEffect = _activeEffects[effect.ItemDefinition.Type];
                    // Update duration or other properties as needed
                    shaderEffect.duration = effect.RemainingTime;
                }
            }
        }

        private void UpdateLOD()
        {
            if (!_enableLOD || _mainCamera == null || _playerTransform == null) return;

            float distance = Vector3.Distance(_mainCamera.transform.position, _playerTransform.position);
            float lodMultiplier = Mathf.Lerp(1f, 0.3f, distance / _lodDistance);

            // Apply LOD to all active effects
            foreach (var effect in _activeEffects.Values)
            {
                if (effect.isActive && _currentPlayerMaterial != null)
                {
                    float adjustedIntensity = effect.intensity * lodMultiplier;
                    if (_currentPlayerMaterial.HasProperty(_EffectIntensity))
                    {
                        _currentPlayerMaterial.SetFloat(_EffectIntensity, adjustedIntensity);
                    }
                }
            }
        }

        #endregion

        #region Cleanup

        private void CleanupMaterials()
        {
            // Stop all coroutines
            foreach (var coroutine in _animationCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _animationCoroutines.Clear();

            // Cleanup material instances
            foreach (var material in _materialInstances.Values)
            {
                if (material != null)
                {
                    DestroyImmediate(material);
                }
            }
            _materialInstances.Clear();

            // Reset player material
            if (_playerRenderer != null && _defaultPlayerMaterial != null)
            {
                _playerRenderer.material = _defaultPlayerMaterial;
            }
        }

        #endregion

        #region Utility Methods

        private void ValidateConfiguration()
        {
            if (_effectConfigs == null || _effectConfigs.Length == 0)
            {
                LogDebug("[ShaderEffectManager] Warning: No shader effect configurations assigned");
            }

            if (_playerRenderer == null)
            {
                LogDebug("[ShaderEffectManager] Warning: Player renderer not assigned");
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }

        #endregion

        #region Public API

        public void ForceUpdateEffect(ItemType itemType, float intensity)
        {
            if (_activeEffects.TryGetValue(itemType, out ActiveShaderEffect effect))
            {
                effect.intensity = Mathf.Clamp01(intensity);
                UpdateShaderProperties(effect);
            }
        }

        public bool IsEffectActive(ItemType itemType)
        {
            return _activeEffects.ContainsKey(itemType) && _activeEffects[itemType].isActive;
        }

        public float GetEffectIntensity(ItemType itemType)
        {
            return _activeEffects.TryGetValue(itemType, out ActiveShaderEffect effect) ? effect.intensity : 0f;
        }

        #endregion

        #region Debug

        [ContextMenu("Test All Shader Effects")]
        private void TestAllShaderEffects()
        {
            StartShaderEffect(ItemType.Magnet, Color.cyan, 5f);
            StartCoroutine(TestEffectSequence());
        }

        private IEnumerator TestEffectSequence()
        {
            yield return new WaitForSeconds(1f);
            StartShaderEffect(ItemType.Multiplier, Color.yellow, 5f);
            
            yield return new WaitForSeconds(1f);
            StartShaderEffect(ItemType.Invisible, Color.blue, 5f);
            
            yield return new WaitForSeconds(1f);
            StartShaderEffect(ItemType.Life, Color.green, 5f);
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class ShaderEffectConfig
    {
        [Header("Basic Settings")]
        public ItemType itemType;
        public ShaderEffectType effectType;
        public float maxIntensity = 1f;
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;

        [Header("Animation")]
        public bool useCustomCurve = false;
        public AnimationCurve customIntensityCurve;
        public float animationSpeed = 1f;

        [Header("Advanced")]
        public bool enablePulsing = false;
        public float pulseFrequency = 2f;
        public bool enableColorShift = false;
        public Color[] colorShiftPalette;
    }

    public class ActiveShaderEffect
    {
        public ItemType itemType;
        public ShaderEffectConfig config;
        public Color effectColor;
        public float duration;
        public float startTime;
        public float intensity;
        public bool isActive;
    }

    public enum ShaderEffectType
    {
        PlayerMaterial,
        ScreenEffect,
        WorldEffect
    }

    #endregion
}
