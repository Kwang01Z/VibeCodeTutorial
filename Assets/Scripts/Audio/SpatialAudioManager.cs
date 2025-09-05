using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
using EndlessRunner.Data;
using EndlessRunner.Audio;

namespace EndlessRunner.Audio
{
    /// <summary>
    /// SpatialAudioManager - Advanced 3D spatial audio system
    /// Handles 3D positioning, environmental audio, occlusion, and dynamic audio zones
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Spatial Audio Settings")]
        [SerializeField] private bool _enableSpatialAudio = true;
        [SerializeField] private float _globalSpatialRange = 100f;
        [SerializeField] private AnimationCurve _distanceAttenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private float _dopplerLevel = 0.1f;

        [Header("Environmental Audio")]
        [SerializeField] private bool _enableEnvironmentalAudio = true;
        [SerializeField] private EnvironmentalAudioZone[] _environmentalZones;
        [SerializeField] private float _environmentTransitionSpeed = 2f;

        [Header("Occlusion System")]
        [SerializeField] private bool _enableOcclusion = true;
        [SerializeField] private LayerMask _occlusionLayers = -1;
        [SerializeField] private float _occlusionCheckInterval = 0.1f;
        [SerializeField] private float _occlusionStrength = 0.8f;
        [SerializeField] private AnimationCurve _occlusionCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

        [Header("Reverb Zones")]
        [SerializeField] private bool _enableDynamicReverb = true;
        [SerializeField] private ReverbZoneData[] _reverbZones;
        [SerializeField] private float _reverbTransitionSpeed = 1f;

        [Header("Audio LOD")]
        [SerializeField] private bool _enableAudioLOD = true;
        [SerializeField] private float _nearLODDistance = 20f;
        [SerializeField] private float _farLODDistance = 50f;
        [SerializeField] private int _maxHighQualitySources = 10;
        [SerializeField] private int _maxMediumQualitySources = 20;

        [Header("Wind and Movement")]
        [SerializeField] private bool _enableWindEffects = true;
        [SerializeField] private float _windStrength = 0f;
        [SerializeField] private Vector3 _windDirection = Vector3.right;
        [SerializeField] private AudioClip _windAudioClip;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _visualizeAudioZones = false;
        [SerializeField] private bool _showOcclusionRays = false;

        #endregion

        #region Private Fields

        // Spatial audio tracking
        private List<SpatialAudioSource> _spatialSources = new List<SpatialAudioSource>();
        private Dictionary<AudioSource, SpatialAudioSource> _spatialSourceLookup = new Dictionary<AudioSource, SpatialAudioSource>();

        // Environmental zones
        private EnvironmentalAudioZone _currentEnvironmentalZone;
        private AudioSource _environmentalAudioSource;
        private Coroutine _environmentTransitionCoroutine;

        // Occlusion system
        private Dictionary<AudioSource, OcclusionData> _occlusionData = new Dictionary<AudioSource, OcclusionData>();
        private Coroutine _occlusionUpdateCoroutine;

        // Reverb system
        private AudioReverbZone _currentReverbZone;
        private ReverbZoneData _currentReverbData;

        // Wind system
        private AudioSource _windAudioSource;
        private float _currentWindIntensity = 0f;

        // Cached references
        private Transform _listenerTransform;
        private UnityEngine.Camera _mainCamera;
        private AudioListener _audioListener;

        // LOD tracking
        private int _currentHighQualityCount = 0;
        private int _currentMediumQualityCount = 0;

        #endregion

        #region Properties

        public bool SpatialAudioEnabled => _enableSpatialAudio;
        public int ActiveSpatialSourcesCount => _spatialSources.Count;
        public EnvironmentalAudioZone CurrentEnvironmentalZone => _currentEnvironmentalZone;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSpatialAudio();
            CacheReferences();
        }

        private void Start()
        {
            SetupEnvironmentalAudio();
            SetupWindSystem();
            StartOcclusionUpdates();
        }

        private void Update()
        {
            UpdateSpatialAudio();
            UpdateEnvironmentalZones();
            UpdateReverbZones();
            UpdateWindEffects();
            UpdateAudioLOD();
        }

        private void OnDestroy()
        {
            CleanupSpatialAudio();
        }

        #endregion

        #region Initialization

        private void InitializeSpatialAudio()
        {
            // Configure Unity's audio settings for spatial audio
            AudioSettings.speakerMode = AudioSpeakerMode.Stereo; // Can be changed based on target platform
            
            LogDebug("[SpatialAudioManager] Spatial audio system initialized");
        }

        private void CacheReferences()
        {
            _mainCamera = UnityEngine.Camera.main;
            _audioListener = FindObjectOfType<AudioListener>();
            
            if (_audioListener != null)
            {
                _listenerTransform = _audioListener.transform;
            }
            else if (_mainCamera != null)
            {
                _listenerTransform = _mainCamera.transform;
            }
            else
            {
                _listenerTransform = transform;
            }
        }

        private void SetupEnvironmentalAudio()
        {
            // Create environmental audio source
            GameObject envAudioGO = new GameObject("EnvironmentalAudio");
            envAudioGO.transform.SetParent(transform);
            
            _environmentalAudioSource = envAudioGO.AddComponent<AudioSource>();
            _environmentalAudioSource.loop = true;
            _environmentalAudioSource.playOnAwake = false;
            _environmentalAudioSource.spatialBlend = 0f; // 2D for environmental
        }

        private void SetupWindSystem()
        {
            if (!_enableWindEffects || _windAudioClip == null) return;

            GameObject windAudioGO = new GameObject("WindAudio");
            windAudioGO.transform.SetParent(_listenerTransform);
            windAudioGO.transform.localPosition = Vector3.zero;

            _windAudioSource = windAudioGO.AddComponent<AudioSource>();
            _windAudioSource.clip = _windAudioClip;
            _windAudioSource.loop = true;
            _windAudioSource.volume = 0f;
            _windAudioSource.spatialBlend = 0f;
            _windAudioSource.Play();
        }

        #endregion

        #region Spatial Audio Management

        public void RegisterSpatialAudioSource(AudioSource audioSource, SpatialAudioConfig config = null)
        {
            if (!_enableSpatialAudio || audioSource == null) return;

            // Check if already registered
            if (_spatialSourceLookup.ContainsKey(audioSource)) return;

            var spatialSource = new SpatialAudioSource
            {
                audioSource = audioSource,
                config = config ?? new SpatialAudioConfig(),
                lastPosition = audioSource.transform.position,
                velocity = Vector3.zero,
                occlusionLevel = 0f,
                lodLevel = AudioLODLevel.High
            };

            _spatialSources.Add(spatialSource);
            _spatialSourceLookup[audioSource] = spatialSource;

            // Configure audio source for spatial audio
            ConfigureSpatialAudioSource(spatialSource);

            LogDebug($"[SpatialAudioManager] Registered spatial audio source: {audioSource.name}");
        }

        public void UnregisterSpatialAudioSource(AudioSource audioSource)
        {
            if (!_spatialSourceLookup.TryGetValue(audioSource, out SpatialAudioSource spatialSource))
                return;

            _spatialSources.Remove(spatialSource);
            _spatialSourceLookup.Remove(audioSource);

            // Clean up occlusion data
            _occlusionData.Remove(audioSource);

            LogDebug($"[SpatialAudioManager] Unregistered spatial audio source: {audioSource.name}");
        }

        private void ConfigureSpatialAudioSource(SpatialAudioSource spatialSource)
        {
            var audioSource = spatialSource.audioSource;
            var config = spatialSource.config;

            // Set 3D spatial blend
            audioSource.spatialBlend = config.spatialBlend;
            audioSource.minDistance = config.minDistance;
            audioSource.maxDistance = config.maxDistance;
            audioSource.rolloffMode = config.rolloffMode;
            audioSource.dopplerLevel = _dopplerLevel * config.dopplerMultiplier;

            // Set volume rolloff custom curve if provided
            if (config.useCustomAttenuationCurve && config.customAttenuationCurve != null)
            {
                audioSource.rolloffMode = AudioRolloffMode.Custom;
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, config.customAttenuationCurve);
            }
        }

        #endregion

        #region Spatial Audio Updates

        private void UpdateSpatialAudio()
        {
            if (!_enableSpatialAudio || _listenerTransform == null) return;

            for (int i = _spatialSources.Count - 1; i >= 0; i--)
            {
                var spatialSource = _spatialSources[i];
                
                // Remove null or inactive sources
                if (spatialSource.audioSource == null || !spatialSource.audioSource.gameObject.activeInHierarchy)
                {
                    _spatialSources.RemoveAt(i);
                    continue;
                }

                UpdateSpatialAudioSource(spatialSource);
            }
        }

        private void UpdateSpatialAudioSource(SpatialAudioSource spatialSource)
        {
            var audioSource = spatialSource.audioSource;
            var sourceTransform = audioSource.transform;
            var config = spatialSource.config;

            // Update velocity for Doppler effect
            Vector3 currentPosition = sourceTransform.position;
            spatialSource.velocity = (currentPosition - spatialSource.lastPosition) / Time.unscaledDeltaTime;
            spatialSource.lastPosition = currentPosition;

            // Calculate distance to listener
            float distance = Vector3.Distance(currentPosition, _listenerTransform.position);
            
            // Apply custom distance attenuation if enabled
            if (config.useGlobalDistanceAttenuation)
            {
                float attenuationFactor = _distanceAttenuationCurve.Evaluate(distance / _globalSpatialRange);
                float targetVolume = config.baseVolume * attenuationFactor;
                
                // Apply occlusion
                if (_enableOcclusion && _occlusionData.ContainsKey(audioSource))
                {
                    var occlusionData = _occlusionData[audioSource];
                    targetVolume *= (1f - occlusionData.occlusionLevel * _occlusionStrength);
                }

                audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.unscaledDeltaTime * config.volumeTransitionSpeed);
            }

            // Update directional audio if enabled
            if (config.enableDirectionalAudio)
            {
                UpdateDirectionalAudio(spatialSource, distance);
            }
        }

        private void UpdateDirectionalAudio(SpatialAudioSource spatialSource, float distance)
        {
            var audioSource = spatialSource.audioSource;
            var sourceTransform = audioSource.transform;
            var config = spatialSource.config;

            // Calculate direction from listener to source
            Vector3 directionToSource = (sourceTransform.position - _listenerTransform.position).normalized;
            Vector3 sourceForward = sourceTransform.forward;

            // Calculate angle between source forward and direction to listener
            float angle = Vector3.Angle(sourceForward, -directionToSource);
            float directionality = Mathf.Lerp(1f, config.directionalityStrength, angle / 180f);

            // Apply directional volume modification
            float currentVolume = audioSource.volume;
            audioSource.volume = currentVolume * directionality;
        }

        #endregion

        #region Environmental Audio

        private void UpdateEnvironmentalZones()
        {
            if (!_enableEnvironmentalAudio || _environmentalZones == null) return;

            var playerPosition = _listenerTransform.position;
            EnvironmentalAudioZone targetZone = null;
            float closestDistance = float.MaxValue;

            // Find the closest environmental zone
            foreach (var zone in _environmentalZones)
            {
                if (zone == null) continue;

                float distance = Vector3.Distance(playerPosition, zone.center);
                
                if (distance <= zone.radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    targetZone = zone;
                }
            }

            // Change environment if needed
            if (targetZone != _currentEnvironmentalZone)
            {
                ChangeEnvironmentalZone(targetZone);
            }

            // Update environmental audio volume based on distance
            if (_currentEnvironmentalZone != null && _environmentalAudioSource != null)
            {
                float distance = Vector3.Distance(playerPosition, _currentEnvironmentalZone.center);
                float normalizedDistance = Mathf.Clamp01(distance / _currentEnvironmentalZone.radius);
                float volumeMultiplier = 1f - normalizedDistance;
                
                _environmentalAudioSource.volume = _currentEnvironmentalZone.volume * volumeMultiplier;
            }
        }

        private void ChangeEnvironmentalZone(EnvironmentalAudioZone newZone)
        {
            if (_environmentTransitionCoroutine != null)
            {
                StopCoroutine(_environmentTransitionCoroutine);
            }

            _environmentTransitionCoroutine = StartCoroutine(TransitionEnvironmentalZone(newZone));
        }

        private IEnumerator TransitionEnvironmentalZone(EnvironmentalAudioZone newZone)
        {
            var oldZone = _currentEnvironmentalZone;
            _currentEnvironmentalZone = newZone;

            // Fade out old environment
            if (oldZone != null && _environmentalAudioSource.isPlaying)
            {
                float startVolume = _environmentalAudioSource.volume;
                float elapsed = 0f;
                
                while (elapsed < oldZone.fadeOutDuration)
                {
                    float t = elapsed / oldZone.fadeOutDuration;
                    _environmentalAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                    
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                
                _environmentalAudioSource.Stop();
            }

            // Fade in new environment
            if (newZone != null && newZone.environmentalClip != null)
            {
                _environmentalAudioSource.clip = newZone.environmentalClip;
                _environmentalAudioSource.volume = 0f;
                _environmentalAudioSource.Play();

                float elapsed = 0f;
                
                while (elapsed < newZone.fadeInDuration)
                {
                    float t = elapsed / newZone.fadeInDuration;
                    _environmentalAudioSource.volume = Mathf.Lerp(0f, newZone.volume, t);
                    
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            LogDebug($"[SpatialAudioManager] Environmental zone changed to: {newZone?.zoneName ?? "None"}");
        }

        #endregion

        #region Occlusion System

        private void StartOcclusionUpdates()
        {
            if (_enableOcclusion)
            {
                _occlusionUpdateCoroutine = StartCoroutine(OcclusionUpdateCoroutine());
            }
        }

        private IEnumerator OcclusionUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_occlusionCheckInterval);
                UpdateOcclusion();
            }
        }

        private void UpdateOcclusion()
        {
            if (_listenerTransform == null) return;

            foreach (var spatialSource in _spatialSources)
            {
                var audioSource = spatialSource.audioSource;
                if (audioSource == null || !audioSource.isPlaying) continue;

                UpdateSourceOcclusion(audioSource);
            }
        }

        private void UpdateSourceOcclusion(AudioSource audioSource)
        {
            Vector3 listenerPosition = _listenerTransform.position;
            Vector3 sourcePosition = audioSource.transform.position;
            Vector3 direction = sourcePosition - listenerPosition;
            float distance = direction.magnitude;

            // Perform raycast for occlusion
            bool isOccluded = false;
            float occlusionLevel = 0f;

            if (Physics.Raycast(listenerPosition, direction.normalized, out RaycastHit hit, distance, _occlusionLayers))
            {
                if (hit.collider.gameObject != audioSource.gameObject)
                {
                    isOccluded = true;
                    
                    // Calculate occlusion level based on hit distance ratio
                    float hitRatio = hit.distance / distance;
                    occlusionLevel = _occlusionCurve.Evaluate(1f - hitRatio);
                }
            }

            // Update or create occlusion data
            if (!_occlusionData.ContainsKey(audioSource))
            {
                _occlusionData[audioSource] = new OcclusionData();
            }

            var occlusionData = _occlusionData[audioSource];
            occlusionData.isOccluded = isOccluded;
            occlusionData.occlusionLevel = Mathf.Lerp(occlusionData.occlusionLevel, occlusionLevel, Time.unscaledDeltaTime * 5f);

            // Debug visualization
            if (_showOcclusionRays)
            {
                Color rayColor = isOccluded ? Color.red : Color.green;
                Debug.DrawRay(listenerPosition, direction, rayColor, _occlusionCheckInterval);
            }
        }

        #endregion

        #region Reverb Zones

        private void UpdateReverbZones()
        {
            if (!_enableDynamicReverb || _reverbZones == null) return;

            var playerPosition = _listenerTransform.position;
            ReverbZoneData targetReverbData = null;
            float closestDistance = float.MaxValue;

            // Find the closest reverb zone
            foreach (var reverbData in _reverbZones)
            {
                if (reverbData == null) continue;

                float distance = Vector3.Distance(playerPosition, reverbData.center);
                
                if (distance <= reverbData.radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    targetReverbData = reverbData;
                }
            }

            // Apply reverb changes
            if (targetReverbData != _currentReverbData)
            {
                ApplyReverbZone(targetReverbData);
                _currentReverbData = targetReverbData;
            }
        }

        private void ApplyReverbZone(ReverbZoneData reverbData)
        {
            if (_currentReverbZone == null)
            {
                // Create reverb zone if it doesn't exist
                GameObject reverbGO = new GameObject("DynamicReverbZone");
                reverbGO.transform.SetParent(_listenerTransform);
                reverbGO.transform.localPosition = Vector3.zero;
                _currentReverbZone = reverbGO.AddComponent<AudioReverbZone>();
            }

            if (reverbData != null)
            {
                _currentReverbZone.reverbPreset = reverbData.reverbPreset;
                _currentReverbZone.room = (int)reverbData.room;
                _currentReverbZone.roomHF = (int)reverbData.roomHF;
                _currentReverbZone.roomLF = (int)reverbData.roomLF;
                _currentReverbZone.decayTime = reverbData.decayTime;
                _currentReverbZone.decayHFRatio = reverbData.decayHFRatio;
                _currentReverbZone.reflections = (int)reverbData.reflections;
                _currentReverbZone.reflectionsDelay = reverbData.reflectionsDelay;
                _currentReverbZone.reverb = (int)reverbData.reverb;
                _currentReverbZone.reverbDelay = reverbData.reverbDelay;
                _currentReverbZone.diffusion = reverbData.diffusion;
                _currentReverbZone.density = reverbData.density;
                // Note: hfReference and roomRolloffFactor are not supported in Unity's AudioReverbZone

                LogDebug($"[SpatialAudioManager] Applied reverb zone: {reverbData.zoneName}");
            }
        }

        #endregion

        #region Wind Effects

        private void UpdateWindEffects()
        {
            if (!_enableWindEffects || _windAudioSource == null) return;

            // Calculate wind intensity based on player movement and wind strength
            Vector3 playerVelocity = Vector3.zero; // This would come from player controller
            float movementWindFactor = playerVelocity.magnitude * 0.1f; // Scale factor
            
            float targetWindIntensity = Mathf.Clamp01(_windStrength + movementWindFactor);
            _currentWindIntensity = Mathf.Lerp(_currentWindIntensity, targetWindIntensity, Time.unscaledDeltaTime * 2f);

            // Apply wind audio
            _windAudioSource.volume = _currentWindIntensity * 0.3f; // Max 30% volume
            _windAudioSource.pitch = 0.8f + (_currentWindIntensity * 0.4f); // Pitch varies with intensity
        }

        public void SetWindParameters(float windStrength, Vector3 windDirection)
        {
            _windStrength = Mathf.Clamp01(windStrength);
            _windDirection = windDirection.normalized;
        }

        #endregion

        #region Audio LOD

        private void UpdateAudioLOD()
        {
            if (!_enableAudioLOD) return;

            _currentHighQualityCount = 0;
            _currentMediumQualityCount = 0;

            // Sort sources by distance for LOD priority
            _spatialSources.Sort((a, b) => 
            {
                float distA = Vector3.Distance(a.audioSource.transform.position, _listenerTransform.position);
                float distB = Vector3.Distance(b.audioSource.transform.position, _listenerTransform.position);
                return distA.CompareTo(distB);
            });

            foreach (var spatialSource in _spatialSources)
            {
                var audioSource = spatialSource.audioSource;
                if (audioSource == null || !audioSource.isPlaying) continue;

                float distance = Vector3.Distance(audioSource.transform.position, _listenerTransform.position);
                AudioLODLevel targetLOD = CalculateAudioLOD(distance);

                // Apply LOD if it changed
                if (spatialSource.lodLevel != targetLOD)
                {
                    ApplyAudioLOD(spatialSource, targetLOD);
                    spatialSource.lodLevel = targetLOD;
                }
            }
        }

        private AudioLODLevel CalculateAudioLOD(float distance)
        {
            if (distance <= _nearLODDistance && _currentHighQualityCount < _maxHighQualitySources)
            {
                _currentHighQualityCount++;
                return AudioLODLevel.High;
            }
            else if (distance <= _farLODDistance && _currentMediumQualityCount < _maxMediumQualitySources)
            {
                _currentMediumQualityCount++;
                return AudioLODLevel.Medium;
            }
            else
            {
                return AudioLODLevel.Low;
            }
        }

        private void ApplyAudioLOD(SpatialAudioSource spatialSource, AudioLODLevel lodLevel)
        {
            var audioSource = spatialSource.audioSource;

            switch (lodLevel)
            {
                case AudioLODLevel.High:
                    // Full quality
                    audioSource.priority = 64;  // High priority
                    break;
                    
                case AudioLODLevel.Medium:
                    // Reduced quality
                    audioSource.priority = 128; // Normal priority
                    break;
                    
                case AudioLODLevel.Low:
                    // Low quality or potentially culled
                    audioSource.priority = 200; // Low priority
                    audioSource.volume *= 0.5f; // Reduce volume
                    break;
            }
        }

        #endregion

        #region Cleanup

        private void CleanupSpatialAudio()
        {
            if (_occlusionUpdateCoroutine != null)
            {
                StopCoroutine(_occlusionUpdateCoroutine);
            }

            if (_environmentTransitionCoroutine != null)
            {
                StopCoroutine(_environmentTransitionCoroutine);
            }

            _spatialSources.Clear();
            _spatialSourceLookup.Clear();
            _occlusionData.Clear();
        }

        #endregion

        #region Utility Methods

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!_visualizeAudioZones) return;

            // Draw environmental zones
            if (_environmentalZones != null)
            {
                Gizmos.color = Color.green;
                foreach (var zone in _environmentalZones)
                {
                    if (zone == null) continue;
                    Gizmos.DrawWireSphere(zone.center, zone.radius);
                }
            }

            // Draw reverb zones
            if (_reverbZones != null)
            {
                Gizmos.color = Color.blue;
                foreach (var reverbData in _reverbZones)
                {
                    if (reverbData == null) continue;
                    Gizmos.DrawWireSphere(reverbData.center, reverbData.radius);
                }
            }

            // Draw current environmental zone
            if (_currentEnvironmentalZone != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentEnvironmentalZone.center, _currentEnvironmentalZone.radius);
            }
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class SpatialAudioConfig
    {
        [Header("Basic Spatial Settings")]
        public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 500f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        public float dopplerMultiplier = 1f;

        [Header("Volume Control")]
        public float baseVolume = 1f;
        public bool useGlobalDistanceAttenuation = true;
        public float volumeTransitionSpeed = 5f;

        [Header("Custom Attenuation")]
        public bool useCustomAttenuationCurve = false;
        public AnimationCurve customAttenuationCurve;

        [Header("Directional Audio")]
        public bool enableDirectionalAudio = false;
        public float directionalityStrength = 0.5f;
    }

    public class SpatialAudioSource
    {
        public AudioSource audioSource;
        public SpatialAudioConfig config;
        public Vector3 lastPosition;
        public Vector3 velocity;
        public float occlusionLevel;
        public AudioLODLevel lodLevel;
    }

    [System.Serializable]
    public class EnvironmentalAudioZone
    {
        public string zoneName;
        public Vector3 center;
        public float radius;
        public AudioClip environmentalClip;
        public float volume = 0.5f;
        public float fadeInDuration = 2f;
        public float fadeOutDuration = 2f;
        public bool loop = true;
    }

    [System.Serializable]
    public class ReverbZoneData
    {
        public string zoneName;
        public Vector3 center;
        public float radius;
        
        [Header("Reverb Settings")]
        public AudioReverbPreset reverbPreset = AudioReverbPreset.Generic;
        public float room = -1000f;
        public float roomHF = -100f;
        public float roomLF = 0f;
        public float decayTime = 1.49f;
        public float decayHFRatio = 0.83f;
        public float reflections = -2602f;
        public float reflectionsDelay = 0.007f;
        public float reverb = 200f;
        public float reverbDelay = 0.011f;
        public float diffusion = 100f;
        public float density = 100f;
        
        // Note: hfReference and roomRolloffFactor are not supported by Unity's AudioReverbZone
        // These properties are kept for data storage but won't be applied to the AudioReverbZone
        [Header("Unsupported Properties (for data only)")]
        public float hfReference = 5000f;
        public float roomRolloffFactor = 0f;
    }

    public class OcclusionData
    {
        public bool isOccluded;
        public float occlusionLevel;
    }

    public enum AudioLODLevel
    {
        High,
        Medium,
        Low
    }

    #endregion
}
