using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Items;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Audio
{
    /// <summary>
    /// AudioManager - Advanced audio system for EndlessRunner
    /// Handles dynamic music, layered soundscapes, 3D audio, and adaptive audio
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup _masterMixerGroup;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _ambientMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;

        [Header("Music System")]
        [SerializeField] private MusicTrackData[] _musicTracks;
        [SerializeField] private float _musicCrossfadeDuration = 2f;
        [SerializeField] private bool _enableAdaptiveMusic = true;
        [SerializeField] private float _adaptiveTransitionSpeed = 1f;

        [Header("Sound Effect System")]
        [SerializeField] private SoundEffectData[] _soundEffects;
        [SerializeField] private int _maxConcurrentSounds = 32;
        [SerializeField] private float _soundCullDistance = 50f;
        [SerializeField] private bool _enable3DAudio = true;

        [Header("Ambient System")]
        [SerializeField] private AmbientSoundData[] _ambientSounds;
        [SerializeField] private float _ambientFadeSpeed = 0.5f;
        [SerializeField] private bool _enableDynamicAmbient = true;

        [Header("Audio Pools")]
        [SerializeField] private int _audioSourcePoolSize = 20;
        [SerializeField] private int _maxPoolSize = 50;
        [SerializeField] private bool _autoExpandPool = true;

        [Header("Performance")]
        [SerializeField] private bool _enableAudioLOD = true;
        [SerializeField] private float _lodNearDistance = 10f;
        [SerializeField] private float _lodFarDistance = 30f;
        [SerializeField] private int _maxHighQualitySounds = 8;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _showAudioStats = false;

        #endregion

        #region Private Fields

        // Audio source pools
        private Queue<AudioSource> _audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> _activeAudioSources = new List<AudioSource>();
        private HashSet<AudioSource> _persistentSources = new HashSet<AudioSource>();

        // Music system
        private AudioSource _currentMusicSource;
        private AudioSource _crossfadeMusicSource;
        private MusicTrackData _currentTrack;
        private Coroutine _musicCrossfadeCoroutine;
        private float _currentMusicIntensity = 0.5f;

        // Sound libraries
        private Dictionary<string, SoundEffectData> _soundLibrary = new Dictionary<string, SoundEffectData>();
        private Dictionary<string, AmbientSoundData> _ambientLibrary = new Dictionary<string, AmbientSoundData>();

        // Active audio tracking
        private List<ActiveAudioInstance> _activeInstances = new List<ActiveAudioInstance>();
        private Dictionary<ItemType, List<AudioSource>> _itemAudioSources = new Dictionary<ItemType, List<AudioSource>>();

        // Adaptive audio
        private float _gameSpeed = 1f;
        private float _playerHealth = 1f;
        private int _currentScore = 0;
        private GameState _currentGameState = GameState.Playing;

        // Cached references
        private Transform _playerTransform;
        private Transform _listenerTransform;
        private UnityEngine.Camera _mainCamera;

        // Audio stats
        private int _totalSoundsPlayed;
        private float _averageAudioLatency;

        #endregion

        #region Properties

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.7f;
        public float SFXVolume { get; private set; } = 1f;
        public float AmbientVolume { get; private set; } = 0.8f;
        public float UIVolume { get; private set; } = 1f;

        public bool IsMusicPlaying => _currentMusicSource != null && _currentMusicSource.isPlaying;
        public int ActiveSourcesCount => _activeAudioSources.Count;
        public int PooledSourcesCount => _audioSourcePool.Count;

        #endregion

        #region Singleton

        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            ValidateConfiguration();
            SubscribeToEvents();
            StartDefaultMusic();
        }

        private void Update()
        {
            UpdateActiveAudio();
            UpdateAdaptiveAudio();
            UpdateAudioLOD();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupAudio();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            InitializeAudioPools();
            BuildSoundLibraries();
            SetupMusicSources();
            CacheReferences();
            LoadAudioSettings();

            LogDebug("[AudioManager] Audio system initialized");
        }

        private void InitializeAudioPools()
        {
            // Create initial pool of audio sources
            for (int i = 0; i < _audioSourcePoolSize; i++)
            {
                CreatePooledAudioSource();
            }

            // Initialize item audio source dictionaries
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
            {
                _itemAudioSources[itemType] = new List<AudioSource>();
            }
        }

        private AudioSource CreatePooledAudioSource()
        {
            GameObject sourceGO = new GameObject("PooledAudioSource");
            sourceGO.transform.SetParent(transform);
            
            AudioSource source = sourceGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _sfxMixerGroup;
            
            _audioSourcePool.Enqueue(source);
            return source;
        }

        private void BuildSoundLibraries()
        {
            // Build sound effect library
            foreach (var soundData in _soundEffects)
            {
                if (soundData != null && !string.IsNullOrEmpty(soundData.soundId))
                {
                    _soundLibrary[soundData.soundId] = soundData;
                }
            }

            // Build ambient sound library
            foreach (var ambientData in _ambientSounds)
            {
                if (ambientData != null && !string.IsNullOrEmpty(ambientData.ambientId))
                {
                    _ambientLibrary[ambientData.ambientId] = ambientData;
                }
            }

            LogDebug($"[AudioManager] Built sound library: {_soundLibrary.Count} sounds, {_ambientLibrary.Count} ambient tracks");
        }

        private void SetupMusicSources()
        {
            // Create dedicated music sources for crossfading
            GameObject musicGO = new GameObject("MusicSources");
            musicGO.transform.SetParent(transform);

            _currentMusicSource = musicGO.AddComponent<AudioSource>();
            _currentMusicSource.outputAudioMixerGroup = _musicMixerGroup;
            _currentMusicSource.loop = true;
            _currentMusicSource.playOnAwake = false;

            _crossfadeMusicSource = musicGO.AddComponent<AudioSource>();
            _crossfadeMusicSource.outputAudioMixerGroup = _musicMixerGroup;
            _crossfadeMusicSource.loop = true;
            _crossfadeMusicSource.playOnAwake = false;
        }

        private void CacheReferences()
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            _mainCamera = UnityEngine.Camera.main;
            _listenerTransform = _mainCamera?.transform;

            if (_listenerTransform == null)
                _listenerTransform = transform;
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            // Item system events
            ItemEffectEvents.OnEffectStarted.AddListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.AddListener(OnEffectEnded);
            ItemPickup.OnItemPicked += OnItemPicked;

            // Game state events - these would need to be implemented in your game
            // GameEvents.OnGameStateChanged += OnGameStateChanged;
            // PlayerEvents.OnHealthChanged += OnPlayerHealthChanged;
            // ScoreEvents.OnScoreChanged += OnScoreChanged;
        }

        private void UnsubscribeFromEvents()
        {
            ItemEffectEvents.OnEffectStarted.RemoveListener(OnEffectStarted);
            ItemEffectEvents.OnEffectEnded.RemoveListener(OnEffectEnded);
            ItemPickup.OnItemPicked -= OnItemPicked;
        }

        #endregion

        #region Event Handlers

        private void OnEffectStarted(ActiveItemEffect effect)
        {
            PlayItemEffectSound(effect.ItemDefinition.Type, "start", GetPlayerPosition());
            UpdateMusicIntensity();
        }

        private void OnEffectEnded(ItemType itemType, string itemId)
        {
            PlayItemEffectSound(itemType, "end", GetPlayerPosition());
            UpdateMusicIntensity();
        }

        private void OnItemPicked(ItemDefinition itemDefinition, Vector3 position)
        {
            PlayItemPickupSound(itemDefinition.Type, position);
        }

        private void OnGameStateChanged(GameState newState)
        {
            _currentGameState = newState;
            HandleGameStateMusic(newState);
        }

        private void OnPlayerHealthChanged(float newHealth, float maxHealth)
        {
            _playerHealth = newHealth / maxHealth;
            UpdateAdaptiveAudioParameters();
        }

        private void OnScoreChanged(int newScore)
        {
            _currentScore = newScore;
            UpdateMusicIntensity();
        }

        #endregion

        #region Music System

        public void PlayMusic(string trackId, bool crossfade = true)
        {
            var track = GetMusicTrack(trackId);
            if (track == null)
            {
                LogDebug($"[AudioManager] Music track not found: {trackId}");
                return;
            }

            if (crossfade && _currentMusicSource.isPlaying)
            {
                StartMusicCrossfade(track);
            }
            else
            {
                PlayMusicImmediate(track);
            }
        }

        public void StopMusic(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic(_musicCrossfadeDuration));
            }
            else
            {
                _currentMusicSource.Stop();
                if (_crossfadeMusicSource.isPlaying)
                    _crossfadeMusicSource.Stop();
            }
        }

        public void SetMusicIntensity(float intensity)
        {
            _currentMusicIntensity = Mathf.Clamp01(intensity);
            UpdateMusicLayers();
        }

        private void StartMusicCrossfade(MusicTrackData newTrack)
        {
            if (_musicCrossfadeCoroutine != null)
            {
                StopCoroutine(_musicCrossfadeCoroutine);
            }

            _musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(newTrack));
        }

        private IEnumerator CrossfadeMusicCoroutine(MusicTrackData newTrack)
        {
            // Setup crossfade source
            _crossfadeMusicSource.clip = newTrack.musicClip;
            _crossfadeMusicSource.volume = 0f;
            _crossfadeMusicSource.Play();

            float currentVolume = _currentMusicSource.volume;
            float elapsed = 0f;

            // Crossfade
            while (elapsed < _musicCrossfadeDuration)
            {
                float t = elapsed / _musicCrossfadeDuration;
                
                _currentMusicSource.volume = Mathf.Lerp(currentVolume, 0f, t);
                _crossfadeMusicSource.volume = Mathf.Lerp(0f, MusicVolume, t);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Swap sources
            _currentMusicSource.Stop();
            var temp = _currentMusicSource;
            _currentMusicSource = _crossfadeMusicSource;
            _crossfadeMusicSource = temp;

            _currentTrack = newTrack;
            _musicCrossfadeCoroutine = null;

            LogDebug($"[AudioManager] Crossfaded to music: {newTrack.trackId}");
        }

        private void PlayMusicImmediate(MusicTrackData track)
        {
            _currentMusicSource.clip = track.musicClip;
            _currentMusicSource.volume = MusicVolume;
            _currentMusicSource.Play();
            _currentTrack = track;

            LogDebug($"[AudioManager] Playing music: {track.trackId}");
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = _currentMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _currentMusicSource.Stop();
            _currentMusicSource.volume = MusicVolume;
        }

        #endregion

        #region Sound Effects

        public AudioSource PlaySound(string soundId, Vector3? position = null, Transform parent = null)
        {
            if (!_soundLibrary.TryGetValue(soundId, out SoundEffectData soundData))
            {
                LogDebug($"[AudioManager] Sound not found: {soundId}");
                return null;
            }

            var audioSource = GetPooledAudioSource();
            if (audioSource == null)
            {
                LogDebug("[AudioManager] No available audio sources in pool");
                return null;
            }

            ConfigureAudioSource(audioSource, soundData, position, parent);
            audioSource.Play();

            TrackActiveAudio(audioSource, soundData);
            _totalSoundsPlayed++;

            LogDebug($"[AudioManager] Playing sound: {soundId}");
            return audioSource;
        }

        public void PlayItemPickupSound(ItemType itemType, Vector3 position)
        {
            string soundId = GetItemPickupSoundId(itemType);
            var audioSource = PlaySound(soundId, position);
            
            if (audioSource != null)
            {
                // Add to item audio sources for potential cleanup
                if (!_itemAudioSources.ContainsKey(itemType))
                    _itemAudioSources[itemType] = new List<AudioSource>();
                
                _itemAudioSources[itemType].Add(audioSource);
            }
        }

        public void PlayItemEffectSound(ItemType itemType, string effectType, Vector3 position)
        {
            string soundId = $"item_{itemType.ToString().ToLower()}_{effectType}";
            PlaySound(soundId, position);
        }

        public void StopItemSounds(ItemType itemType)
        {
            if (_itemAudioSources.TryGetValue(itemType, out List<AudioSource> sources))
            {
                foreach (var source in sources)
                {
                    if (source != null && source.isPlaying)
                    {
                        StartCoroutine(FadeOutAudioSource(source, 0.2f));
                    }
                }
                sources.Clear();
            }
        }

        private IEnumerator FadeOutAudioSource(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration && source != null)
            {
                float t = elapsed / duration;
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (source != null)
            {
                source.Stop();
                ReturnAudioSourceToPool(source);
            }
        }

        #endregion

        #region Audio Source Management

        private AudioSource GetPooledAudioSource()
        {
            if (_audioSourcePool.Count > 0)
            {
                var source = _audioSourcePool.Dequeue();
                _activeAudioSources.Add(source);
                return source;
            }
            else if (_autoExpandPool && _activeAudioSources.Count < _maxPoolSize)
            {
                var source = CreatePooledAudioSource();
                _audioSourcePool.Dequeue(); // Remove from pool since we just added it
                _activeAudioSources.Add(source);
                return source;
            }

            // Pool exhausted, find oldest non-persistent source
            return ReclaimOldestAudioSource();
        }

        private AudioSource ReclaimOldestAudioSource()
        {
            for (int i = 0; i < _activeAudioSources.Count; i++)
            {
                var source = _activeAudioSources[i];
                if (!_persistentSources.Contains(source) && source != null)
                {
                    source.Stop();
                    return source;
                }
            }
            return null;
        }

        private void ReturnAudioSourceToPool(AudioSource source)
        {
            if (source == null) return;

            // Reset audio source
            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.pitch = 1f;
            source.loop = false;
            source.spatialBlend = 0f;
            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;

            // Return to pools
            _activeAudioSources.Remove(source);
            _persistentSources.Remove(source);
            _audioSourcePool.Enqueue(source);
        }

        private void ConfigureAudioSource(AudioSource source, SoundEffectData data, Vector3? position, Transform parent)
        {
            source.clip = data.GetRandomClip();
            source.volume = data.volume * SFXVolume;
            source.pitch = Random.Range(data.pitchMin, data.pitchMax);
            source.loop = data.loop;
            source.outputAudioMixerGroup = data.mixerGroup ?? _sfxMixerGroup;

            // 3D Audio setup
            if (position.HasValue && _enable3DAudio)
            {
                source.spatialBlend = data.spatialBlend;
                source.minDistance = data.minDistance;
                source.maxDistance = data.maxDistance;
                source.rolloffMode = data.rolloffMode;
                
                source.transform.position = position.Value;
            }
            else
            {
                source.spatialBlend = 0f; // 2D audio
            }

            // Parent setup
            if (parent != null)
            {
                source.transform.SetParent(parent);
            }
        }

        #endregion

        #region Adaptive Audio

        private void UpdateAdaptiveAudio()
        {
            if (!_enableAdaptiveMusic || _currentTrack == null) return;

            UpdateMusicIntensity();
            UpdateAdaptiveAudioParameters();
        }

        private void UpdateMusicIntensity()
        {
            float targetIntensity = CalculateTargetMusicIntensity();
            _currentMusicIntensity = Mathf.Lerp(_currentMusicIntensity, targetIntensity, Time.unscaledDeltaTime * _adaptiveTransitionSpeed);
            UpdateMusicLayers();
        }

        private float CalculateTargetMusicIntensity()
        {
            float intensity = 0.5f; // Base intensity

            // Adjust based on game speed
            intensity += Mathf.Clamp01(_gameSpeed - 1f) * 0.3f;

            // Adjust based on health (lower health = higher tension)
            intensity += (1f - _playerHealth) * 0.4f;

            // Adjust based on score (higher score = more energetic)
            intensity += Mathf.Clamp01(_currentScore / 10000f) * 0.2f;

            return Mathf.Clamp01(intensity);
        }

        private void UpdateMusicLayers()
        {
            if (_currentTrack?.adaptiveLayers == null) return;

            // Update layered music based on intensity
            foreach (var layer in _currentTrack.adaptiveLayers)
            {
                if (layer.audioSource != null)
                {
                    float targetVolume = _currentMusicIntensity >= layer.intensityThreshold ? layer.volume : 0f;
                    layer.audioSource.volume = Mathf.Lerp(layer.audioSource.volume, targetVolume, Time.unscaledDeltaTime * 2f);
                }
            }
        }

        private void UpdateAdaptiveAudioParameters()
        {
            // Update audio mixer parameters based on game state
            if (_masterMixerGroup != null)
            {
                // Example: Lower pitch when health is low
                float pitchShift = Mathf.Lerp(-0.1f, 0f, _playerHealth);
                _masterMixerGroup.audioMixer.SetFloat("MasterPitch", pitchShift);

                // Example: Add reverb in dangerous situations
                float reverbLevel = (1f - _playerHealth) * 0.5f;
                _masterMixerGroup.audioMixer.SetFloat("ReverbLevel", reverbLevel);
            }
        }

        #endregion

        #region Audio LOD

        private void UpdateAudioLOD()
        {
            if (!_enableAudioLOD || _listenerTransform == null) return;

            int highQualityCount = 0;

            for (int i = _activeAudioSources.Count - 1; i >= 0; i--)
            {
                var source = _activeAudioSources[i];
                if (source == null || !source.isPlaying)
                {
                    ReturnAudioSourceToPool(source);
                    continue;
                }

                float distance = Vector3.Distance(_listenerTransform.position, source.transform.position);
                
                // Cull distant sounds
                if (distance > _soundCullDistance)
                {
                    source.Stop();
                    ReturnAudioSourceToPool(source);
                    continue;
                }

                // Apply LOD
                if (distance <= _lodNearDistance && highQualityCount < _maxHighQualitySounds)
                {
                    // High quality
                    source.volume = Mathf.Lerp(source.volume, GetOriginalVolume(source), Time.unscaledDeltaTime);
                    highQualityCount++;
                }
                else if (distance <= _lodFarDistance)
                {
                    // Medium quality
                    float volumeMultiplier = Mathf.Lerp(1f, 0.3f, (distance - _lodNearDistance) / (_lodFarDistance - _lodNearDistance));
                    source.volume = GetOriginalVolume(source) * volumeMultiplier;
                }
                else
                {
                    // Low quality or cull
                    source.volume = GetOriginalVolume(source) * 0.1f;
                }
            }
        }

        #endregion

        #region Volume Controls

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            _masterMixerGroup?.audioMixer.SetFloat("MasterVolume", LinearToDecibel(MasterVolume));
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            _musicMixerGroup?.audioMixer.SetFloat("MusicVolume", LinearToDecibel(MusicVolume));
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            _sfxMixerGroup?.audioMixer.SetFloat("SFXVolume", LinearToDecibel(SFXVolume));
        }

        public void SetAmbientVolume(float volume)
        {
            AmbientVolume = Mathf.Clamp01(volume);
            _ambientMixerGroup?.audioMixer.SetFloat("AmbientVolume", LinearToDecibel(AmbientVolume));
        }

        public void SetUIVolume(float volume)
        {
            UIVolume = Mathf.Clamp01(volume);
            _uiMixerGroup?.audioMixer.SetFloat("UIVolume", LinearToDecibel(UIVolume));
        }

        private float LinearToDecibel(float linear)
        {
            return linear > 0f ? 20f * Mathf.Log10(linear) : -80f;
        }

        #endregion

        #region Utility Methods

        private void UpdateActiveAudio()
        {
            // Update active audio instances and clean up finished ones
            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeInstances[i];
                if (instance.audioSource == null || !instance.audioSource.isPlaying)
                {
                    _activeInstances.RemoveAt(i);
                }
            }
        }

        private void TrackActiveAudio(AudioSource source, SoundEffectData data)
        {
            var instance = new ActiveAudioInstance
            {
                audioSource = source,
                soundData = data,
                startTime = Time.unscaledTime
            };
            _activeInstances.Add(instance);
        }

        private MusicTrackData GetMusicTrack(string trackId)
        {
            foreach (var track in _musicTracks)
            {
                if (track != null && track.trackId == trackId)
                    return track;
            }
            return null;
        }

        private string GetItemPickupSoundId(ItemType itemType)
        {
            return $"pickup_{itemType.ToString().ToLower()}";
        }

        private float GetOriginalVolume(AudioSource source)
        {
            // This would need to be enhanced to track original volumes
            return 1f;
        }

        private Vector3 GetPlayerPosition()
        {
            return _playerTransform != null ? _playerTransform.position : Vector3.zero;
        }

        private void StartDefaultMusic()
        {
            if (_musicTracks.Length > 0 && _musicTracks[0] != null)
            {
                PlayMusic(_musicTracks[0].trackId, false);
            }
        }

        private void HandleGameStateMusic(GameState state)
        {
            // Handle music changes based on game state
            switch (state)
            {
                case GameState.Menu:
                    PlayMusic("menu_music");
                    break;
                case GameState.Playing:
                    PlayMusic("gameplay_music");
                    break;
                case GameState.Paused:
                    // Pause or lower music
                    break;
                case GameState.GameOver:
                    PlayMusic("gameover_music");
                    break;
            }
        }

        private void ValidateConfiguration()
        {
            if (_soundEffects == null || _soundEffects.Length == 0)
                LogDebug("[AudioManager] Warning: No sound effects configured");

            if (_musicTracks == null || _musicTracks.Length == 0)
                LogDebug("[AudioManager] Warning: No music tracks configured");
        }

        private void LoadAudioSettings()
        {
            // Load saved audio settings from PlayerPrefs or save system
            SetMasterVolume(PlayerPrefs.GetFloat("AudioMasterVolume", 1f));
            SetMusicVolume(PlayerPrefs.GetFloat("AudioMusicVolume", 0.7f));
            SetSFXVolume(PlayerPrefs.GetFloat("AudioSFXVolume", 1f));
            SetAmbientVolume(PlayerPrefs.GetFloat("AudioAmbientVolume", 0.8f));
            SetUIVolume(PlayerPrefs.GetFloat("AudioUIVolume", 1f));
        }

        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("AudioMasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("AudioMusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("AudioSFXVolume", SFXVolume);
            PlayerPrefs.SetFloat("AudioAmbientVolume", AmbientVolume);
            PlayerPrefs.SetFloat("AudioUIVolume", UIVolume);
            PlayerPrefs.Save();
        }

        private void CleanupAudio()
        {
            StopAllCoroutines();
            
            foreach (var source in _activeAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
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

        #region Debug

        [ContextMenu("Show Audio Stats")]
        private void ShowAudioStats()
        {
            string stats = $"Audio Stats:\n" +
                          $"- Active Sources: {_activeAudioSources.Count}\n" +
                          $"- Pooled Sources: {_audioSourcePool.Count}\n" +
                          $"- Total Sounds Played: {_totalSoundsPlayed}\n" +
                          $"- Current Music: {(_currentTrack?.trackId ?? "None")}\n" +
                          $"- Music Intensity: {_currentMusicIntensity:F2}";
            
            Debug.Log(stats, this);
        }

        [ContextMenu("Test Sound")]
        private void TestSound()
        {
            if (_soundLibrary.Count > 0)
            {
                var firstSound = _soundLibrary.Values.GetEnumerator();
                firstSound.MoveNext();
                PlaySound(firstSound.Current.soundId);
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class ActiveAudioInstance
    {
        public AudioSource audioSource;
        public SoundEffectData soundData;
        public float startTime;
    }

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    #endregion
}
