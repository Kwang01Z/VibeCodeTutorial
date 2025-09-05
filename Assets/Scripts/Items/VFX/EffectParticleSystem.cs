using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items.VFX
{
    /// <summary>
    /// EffectParticleSystem - Advanced particle system manager cho tất cả item effects
    /// Manages particle pools, dynamic effects, và performance optimization
    /// </summary>
    public class EffectParticleSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Particle System Prefabs")]
        [SerializeField] private ParticleSystem _magnetParticlePrefab;
        [SerializeField] private ParticleSystem _speedParticlePrefab;
        [SerializeField] private ParticleSystem _invisibilityParticlePrefab;
        [SerializeField] private ParticleSystem _healingParticlePrefab;
        [SerializeField] private ParticleSystem _pickupParticlePrefab;
        [SerializeField] private ParticleSystem _stackingParticlePrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 50;
        [SerializeField] private bool _autoExpandPool = true;

        [Header("Effect Configuration")]
        [SerializeField] private EffectParticleConfig[] _effectConfigs;
        [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _defaultEffectDuration = 2f;

        [Header("Performance")]
        [SerializeField] private int _maxConcurrentEffects = 20;
        [SerializeField] private float _cullDistance = 50f;
        [SerializeField] private bool _enableLOD = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _showPoolStats = false;

        #endregion

        #region Private Fields

        // Particle pools by effect type
        private Dictionary<ItemType, ParticlePool> _particlePools;
        private Dictionary<ItemType, EffectParticleConfig> _configLookup;

        // Active effect tracking
        private List<ActiveParticleEffect> _activeEffects = new List<ActiveParticleEffect>();
        private Queue<ActiveParticleEffect> _effectQueue = new Queue<ActiveParticleEffect>();

        // Performance tracking
        private int _totalEffectsCreated;
        private int _totalEffectsReturned;
        private float _averageEffectDuration;

        // Cache
        private Transform _playerTransform;
        private UnityEngine.Camera _mainCamera;

        #endregion

        #region Properties

        public int ActiveEffectCount => _activeEffects.Count;
        public int TotalEffectsCreated => _totalEffectsCreated;
        public float AverageEffectDuration => _averageEffectDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePools();
            CacheReferences();
            SubscribeToEvents();
        }

        private void Start()
        {
            ValidateConfiguration();
        }

        private void Update()
        {
            UpdateActiveEffects();
            ProcessEffectQueue();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupPools();
        }

        #endregion

        #region Initialization

        private void InitializePools()
        {
            _particlePools = new Dictionary<ItemType, ParticlePool>();
            _configLookup = new Dictionary<ItemType, EffectParticleConfig>();

            // Create pools for each effect type
            CreatePool(ItemType.Magnet, _magnetParticlePrefab);
            CreatePool(ItemType.Multiplier, _speedParticlePrefab);
            CreatePool(ItemType.Invisible, _invisibilityParticlePrefab);
            CreatePool(ItemType.Life, _healingParticlePrefab);

            // Setup config lookup
            foreach (var config in _effectConfigs)
            {
                if (config != null)
                {
                    _configLookup[config.itemType] = config;
                }
            }

            LogDebug($"[EffectParticleSystem] Initialized {_particlePools.Count} particle pools");
        }

        private void CreatePool(ItemType itemType, ParticleSystem prefab)
        {
            if (prefab == null)
            {
                LogDebug($"[EffectParticleSystem] No prefab assigned for {itemType}, skipping pool creation");
                return;
            }

            var poolGO = new GameObject($"{itemType}ParticlePool");
            poolGO.transform.SetParent(transform);

            var pool = poolGO.AddComponent<ParticlePool>();
            pool.Initialize(prefab, _initialPoolSize, _maxPoolSize, _autoExpandPool);

            _particlePools[itemType] = pool;
        }

        private void CacheReferences()
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            _mainCamera = UnityEngine.Camera.main;

            if (_playerTransform == null)
                LogDebug("[EffectParticleSystem] Warning: Player transform not found");
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            ItemEffectEvents.OnEffectStarted.AddListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.AddListener(OnEffectStacked);
            ItemEffectEvents.OnAllEffectsChanged.AddListener(OnAllEffectsChanged);

            // Additional pickup events
            ItemPickup.OnItemPicked += OnItemPicked;
        }

        private void UnsubscribeFromEvents()
        {
            ItemEffectEvents.OnEffectStarted.RemoveListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(OnEffectEnded);
            ItemEffectEvents.OnEffectStacked.RemoveListener(OnEffectStacked);
            ItemEffectEvents.OnAllEffectsChanged.RemoveListener(OnAllEffectsChanged);

            ItemPickup.OnItemPicked -= OnItemPicked;
        }

        #endregion

        #region Event Handlers

        private void OnEffectStarted(ActiveItemEffect effect)
        {
            PlayEffectStartParticles(effect.ItemDefinition.Type, GetPlayerPosition());
        }

        private void OnEffectEnded(ItemType itemType, string itemId)
        {
            PlayEffectEndParticles(itemType, GetPlayerPosition());
        }

        private void OnEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            PlayStackingParticles(effect.ItemDefinition.Type, GetPlayerPosition(), effect.StackCount);
        }

        private void OnItemPicked(ItemDefinition itemDefinition, Vector3 position)
        {
            PlayPickupParticles(itemDefinition.Type, position, itemDefinition.EffectColor);
        }

        private void OnAllEffectsChanged(Dictionary<string, ActiveItemEffect> allEffects)
        {
            // Update continuous effects based on active effects
            UpdateContinuousEffects(allEffects);
        }

        #endregion

        #region Particle Effect Methods

        public void PlayEffectStartParticles(ItemType itemType, Vector3 position)
        {
            if (!_particlePools.ContainsKey(itemType)) return;

            var particleSystem = GetPooledParticleSystem(itemType);
            if (particleSystem == null) return;

            ConfigureParticleSystem(particleSystem, itemType, EffectTrigger.Start);
            PlayParticleEffect(particleSystem, position, _defaultEffectDuration);
        }

        public void PlayEffectEndParticles(ItemType itemType, Vector3 position)
        {
            if (!_particlePools.ContainsKey(itemType)) return;

            var particleSystem = GetPooledParticleSystem(itemType);
            if (particleSystem == null) return;

            ConfigureParticleSystem(particleSystem, itemType, EffectTrigger.End);
            PlayParticleEffect(particleSystem, position, _defaultEffectDuration * 0.5f);
        }

        public void PlayStackingParticles(ItemType itemType, Vector3 position, int stackCount)
        {
            if (_stackingParticlePrefab == null) return;

            var particleSystem = GetPooledStackingParticle();
            if (particleSystem == null) return;

            // Configure stacking effect based on stack count
            var main = particleSystem.main;
            main.startColor = GetEffectColor(itemType);
            
            var emission = particleSystem.emission;
            emission.rateOverTime = 20f * stackCount; // More particles for higher stacks

            PlayParticleEffect(particleSystem, position, 1f);
        }

        public void PlayPickupParticles(ItemType itemType, Vector3 position, Color color)
        {
            if (_pickupParticlePrefab == null) return;

            var particleSystem = GetPooledPickupParticle();
            if (particleSystem == null) return;

            var main = particleSystem.main;
            main.startColor = color;
            
            PlayParticleEffect(particleSystem, position, 1.5f);
        }

        #endregion

        #region Particle System Management

        private ParticleSystem GetPooledParticleSystem(ItemType itemType)
        {
            if (!_particlePools.TryGetValue(itemType, out ParticlePool pool))
                return null;

            return pool.GetPooledObject();
        }

        private ParticleSystem GetPooledStackingParticle()
        {
            // Use magnet pool for stacking effects as fallback
            return GetPooledParticleSystem(ItemType.Magnet);
        }

        private ParticleSystem GetPooledPickupParticle()
        {
            // Use magnet pool for pickup effects as fallback  
            return GetPooledParticleSystem(ItemType.Magnet);
        }

        private void ConfigureParticleSystem(ParticleSystem ps, ItemType itemType, EffectTrigger trigger)
        {
            if (!_configLookup.TryGetValue(itemType, out EffectParticleConfig config))
            {
                LogDebug($"[EffectParticleSystem] No config found for {itemType}");
                return;
            }

            ApplyParticleConfiguration(ps, config, trigger);
        }

        private void ApplyParticleConfiguration(ParticleSystem ps, EffectParticleConfig config, EffectTrigger trigger)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var velocityOverLifetime = ps.velocityOverLifetime;

            // Apply base configuration
            main.startColor = config.baseColor;
            main.startSize = config.startSize;
            main.startLifetime = config.lifetime;
            emission.rateOverTime = config.emissionRate;

            // Apply trigger-specific modifications
            switch (trigger)
            {
                case EffectTrigger.Start:
                    emission.rateOverTime = config.emissionRate * config.startIntensityMultiplier;
                    break;
                case EffectTrigger.End:
                    emission.rateOverTime = config.emissionRate * config.endIntensityMultiplier;
                    main.startColor = Color.Lerp(config.baseColor, Color.white, 0.5f);
                    break;
                case EffectTrigger.Continuous:
                    // Continuous effects use base values
                    break;
            }
        }

        private void PlayParticleEffect(ParticleSystem ps, Vector3 position, float duration)
        {
            if (ps == null) return;

            // Position the particle system
            ps.transform.position = position;

            // Create active effect tracker
            var activeEffect = new ActiveParticleEffect
            {
                particleSystem = ps,
                duration = duration,
                startTime = Time.unscaledTime,
                isActive = true
            };

            // Play the effect
            ps.Play();
            _activeEffects.Add(activeEffect);
            _totalEffectsCreated++;

            LogDebug($"[EffectParticleSystem] Playing particle effect at {position}, duration: {duration}s");
        }

        #endregion

        #region Update Management

        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                
                if (!effect.isActive || effect.particleSystem == null)
                {
                    RemoveActiveEffect(i);
                    continue;
                }

                // Check if effect should end
                float elapsed = Time.unscaledTime - effect.startTime;
                if (elapsed >= effect.duration)
                {
                    StopParticleEffect(effect);
                    RemoveActiveEffect(i);
                }
                else if (_enableLOD)
                {
                    // Distance-based LOD
                    UpdateEffectLOD(effect);
                }
            }
        }

        private void ProcessEffectQueue()
        {
            // Process any queued effects (for performance management)
            while (_effectQueue.Count > 0 && _activeEffects.Count < _maxConcurrentEffects)
            {
                var queuedEffect = _effectQueue.Dequeue();
                _activeEffects.Add(queuedEffect);
                queuedEffect.particleSystem.Play();
            }
        }

        private void UpdateEffectLOD(ActiveParticleEffect effect)
        {
            if (_mainCamera == null || effect.particleSystem == null) return;

            float distance = Vector3.Distance(_mainCamera.transform.position, effect.particleSystem.transform.position);
            
            // Cull distant effects
            if (distance > _cullDistance)
            {
                effect.particleSystem.gameObject.SetActive(false);
                return;
            }

            // Adjust particle count based on distance
            effect.particleSystem.gameObject.SetActive(true);
            var emission = effect.particleSystem.emission;
            float lodMultiplier = Mathf.Lerp(1f, 0.3f, distance / _cullDistance);
            // Note: This modifies emission rate, may want to cache original values
        }

        private void RemoveActiveEffect(int index)
        {
            var effect = _activeEffects[index];
            ReturnParticleToPool(effect.particleSystem);
            _activeEffects.RemoveAt(index);
            _totalEffectsReturned++;
        }

        private void StopParticleEffect(ActiveParticleEffect effect)
        {
            if (effect.particleSystem != null)
            {
                effect.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            effect.isActive = false;
        }

        #endregion

        #region Continuous Effects

        private void UpdateContinuousEffects(Dictionary<string, ActiveItemEffect> allEffects)
        {
            // Handle continuous effects like speed trails, magnet fields, etc.
            foreach (var kvp in allEffects)
            {
                var effect = kvp.Value;
                UpdateContinuousEffect(effect);
            }
        }

        private void UpdateContinuousEffect(ActiveItemEffect effect)
        {
            switch (effect.ItemDefinition.Type)
            {
                case ItemType.Magnet:
                    UpdateMagnetFieldEffect(effect);
                    break;
                case ItemType.Multiplier:
                    UpdateSpeedTrailEffect(effect);
                    break;
                case ItemType.Invisible:
                    UpdateInvisibilityEffect(effect);
                    break;
                case ItemType.Life:
                    // Health effects are typically instant, no continuous particles
                    break;
            }
        }

        private void UpdateMagnetFieldEffect(ActiveItemEffect effect)
        {
            // Create/update magnetic field visualization
            // This could be a continuous particle system showing the magnet radius
        }

        private void UpdateSpeedTrailEffect(ActiveItemEffect effect)
        {
            // Create/update speed trail particles
            // Intensity based on current speed multiplier
        }

        private void UpdateInvisibilityEffect(ActiveItemEffect effect)
        {
            // Create/update shimmer/distortion particles around player
        }

        #endregion

        #region Pool Management

        private void ReturnParticleToPool(ParticleSystem ps)
        {
            if (ps == null) return;

            // Find which pool this particle system belongs to
            foreach (var pool in _particlePools.Values)
            {
                if (pool.ReturnToPool(ps))
                {
                    return;
                }
            }

            // If not found in pools, destroy it
            LogDebug("[EffectParticleSystem] Particle system not found in any pool, destroying");
            Destroy(ps.gameObject);
        }

        private void CleanupPools()
        {
            foreach (var pool in _particlePools.Values)
            {
                if (pool != null)
                {
                    pool.Clear();
                }
            }
            _particlePools.Clear();
        }

        #endregion

        #region Utility Methods

        private Vector3 GetPlayerPosition()
        {
            return _playerTransform != null ? _playerTransform.position : Vector3.zero;
        }

        private Color GetEffectColor(ItemType itemType)
        {
            if (_configLookup.TryGetValue(itemType, out EffectParticleConfig config))
            {
                return config.baseColor;
            }

            // Default colors
            return itemType switch
            {
                ItemType.Magnet => Color.cyan,
                ItemType.Multiplier => Color.yellow,
                ItemType.Invisible => Color.blue,
                ItemType.Life => Color.green,
                _ => Color.white
            };
        }

        private void ValidateConfiguration()
        {
            if (_effectConfigs == null || _effectConfigs.Length == 0)
            {
                LogDebug("[EffectParticleSystem] Warning: No effect configurations assigned");
            }

            foreach (var config in _effectConfigs)
            {
                if (config == null)
                {
                    LogDebug("[EffectParticleSystem] Warning: Null configuration found in effect configs");
                }
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

        #region Debug & Stats

        public string GetPoolStats()
        {
            if (!_showPoolStats) return "";

            var stats = "Particle Pool Stats:\n";
            foreach (var kvp in _particlePools)
            {
                var pool = kvp.Value;
                stats += $"- {kvp.Key}: {pool.ActiveCount}/{pool.TotalCount}\n";
            }
            stats += $"Active Effects: {_activeEffects.Count}\n";
            stats += $"Total Created: {_totalEffectsCreated}\n";
            stats += $"Total Returned: {_totalEffectsReturned}";

            return stats;
        }

        [ContextMenu("Show Pool Stats")]
        private void ShowPoolStats()
        {
            Debug.Log(GetPoolStats(), this);
        }

        [ContextMenu("Test All Effects")]
        private void TestAllEffects()
        {
            Vector3 testPos = GetPlayerPosition() + Vector3.up * 2f;
            
            PlayEffectStartParticles(ItemType.Magnet, testPos);
            PlayEffectStartParticles(ItemType.Multiplier, testPos + Vector3.right);
            PlayEffectStartParticles(ItemType.Invisible, testPos + Vector3.left);
            PlayEffectStartParticles(ItemType.Life, testPos + Vector3.forward);
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class EffectParticleConfig
    {
        [Header("Basic Settings")]
        public ItemType itemType;
        public Color baseColor = Color.white;
        public float startSize = 1f;
        public float lifetime = 2f;
        public float emissionRate = 20f;

        [Header("Intensity Multipliers")]
        public float startIntensityMultiplier = 1.5f;
        public float endIntensityMultiplier = 0.5f;
        public float continuousIntensityMultiplier = 1f;

        [Header("Advanced")]
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public bool enableContinuousEffect = false;
    }

    public class ActiveParticleEffect
    {
        public ParticleSystem particleSystem;
        public float duration;
        public float startTime;
        public bool isActive;
    }

    public enum EffectTrigger
    {
        Start,
        End,
        Continuous
    }

    #endregion
}
