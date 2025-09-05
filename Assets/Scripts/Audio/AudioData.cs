using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace EndlessRunner.Audio
{
    /// <summary>
    /// MusicTrackData - Configuration for music tracks with adaptive layers
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "MusicTrack", menuName = "EndlessRunner/Audio/Music Track")]
    public class MusicTrackData : ScriptableObject
    {
        [Header("Basic Info")]
        public string trackId;
        public string displayName;
        [TextArea(3, 5)]
        public string description;

        [Header("Audio Clip")]
        public AudioClip musicClip;
        public AudioMixerGroup mixerGroup;

        [Header("Playback Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.5f, 2f)]
        public float pitch = 1f;
        public bool loop = true;
        public float fadeInDuration = 2f;
        public float fadeOutDuration = 2f;

        [Header("Adaptive Music")]
        public bool enableAdaptiveMusic = false;
        public AdaptiveMusicLayer[] adaptiveLayers;
        public AudioClip[] intensityVariations; // Different versions for different intensities
        
        [Header("Mood and Context")]
        public MusicMood mood = MusicMood.Neutral;
        public GameplayContext[] suitableContexts;
        public float energyLevel = 0.5f; // 0 = calm, 1 = intense

        [Header("Transition Settings")]
        public bool allowsCrossfade = true;
        public float crossfadePoint = 0f; // Time in seconds where crossfade sounds best
        public MusicTransitionType preferredTransition = MusicTransitionType.Crossfade;
    }

    [System.Serializable]
    public class AdaptiveMusicLayer
    {
        public string layerName;
        public AudioSource audioSource; // Will be set at runtime
        public AudioClip layerClip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0f, 1f)]
        public float intensityThreshold = 0.5f; // When this layer becomes active
        public LayerBehavior behavior = LayerBehavior.VolumeBlend;
    }

    /// <summary>
    /// SoundEffectData - Configuration for individual sound effects
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "SoundEffect", menuName = "EndlessRunner/Audio/Sound Effect")]
    public class SoundEffectData : ScriptableObject
    {
        [Header("Basic Info")]
        public string soundId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;

        [Header("Audio Clips")]
        public AudioClip[] audioClips; // Multiple clips for variation
        public AudioMixerGroup mixerGroup;

        [Header("Playback Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitchMin = 0.9f;
        [Range(0.1f, 3f)]
        public float pitchMax = 1.1f;
        public bool loop = false;

        [Header("3D Audio Settings")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
        public float minDistance = 1f;
        public float maxDistance = 500f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Behavior")]
        public bool interruptible = true;
        public bool stackable = false; // Can multiple instances play simultaneously?
        public int maxConcurrentInstances = 3;
        public float cooldownTime = 0f; // Minimum time between plays

        [Header("Context and Priority")]
        public SoundPriority priority = SoundPriority.Normal;
        public SoundCategory category = SoundCategory.SFX;
        public GameplayContext[] playbackContexts;

        [Header("Effects")]
        public bool enableRandomization = true;
        public AudioEffectConfig[] audioEffects;

        /// <summary>
        /// Get a random audio clip from the collection
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (audioClips == null || audioClips.Length == 0)
                return null;
            
            return audioClips[Random.Range(0, audioClips.Length)];
        }

        /// <summary>
        /// Get randomized pitch within the specified range
        /// </summary>
        public float GetRandomPitch()
        {
            return Random.Range(pitchMin, pitchMax);
        }
    }

    /// <summary>
    /// AmbientSoundData - Configuration for ambient sounds and soundscapes
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "AmbientSound", menuName = "EndlessRunner/Audio/Ambient Sound")]
    public class AmbientSoundData : ScriptableObject
    {
        [Header("Basic Info")]
        public string ambientId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;

        [Header("Audio Configuration")]
        public AudioClip ambientClip;
        public AudioMixerGroup mixerGroup;

        [Header("Playback Settings")]
        [Range(0f, 1f)]
        public float volume = 0.5f;
        [Range(0.5f, 2f)]
        public float pitch = 1f;
        public bool loop = true;
        public float fadeInDuration = 3f;
        public float fadeOutDuration = 3f;

        [Header("Dynamic Behavior")]
        public bool enableDynamicVolume = true;
        public AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float dynamicRange = 0.3f; // How much volume can vary

        [Header("Layered Ambient")]
        public AmbientLayer[] ambientLayers;
        public bool enableWeatherResponse = false;
        public bool enableTimeOfDayResponse = false;

        [Header("Context")]
        public EnvironmentType[] environments;
        public WeatherCondition[] weatherConditions;
        public TimeOfDay[] timeOfDaySettings;
    }

    [System.Serializable]
    public class AmbientLayer
    {
        public string layerName;
        public AudioClip layerClip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0f, 1f)]
        public float probability = 1f; // Chance this layer will play
        public float delayMin = 0f;
        public float delayMax = 0f;
    }

    /// <summary>
    /// AudioEffectConfig - Configuration for audio processing effects
    /// </summary>
    [System.Serializable]
    public class AudioEffectConfig
    {
        public AudioEffectType effectType;
        public bool enabled = true;
        
        [Header("Reverb")]
        public float reverbLevel = 0f;
        public AudioReverbPreset reverbPreset = AudioReverbPreset.Generic;
        
        [Header("Echo")]
        public float echoDelay = 500f;
        public float echoDecay = 0.5f;
        
        [Header("Distortion")]
        public float distortionLevel = 0f;
        
        [Header("Low Pass Filter")]
        public float lowPassCutoff = 22000f;
        public float lowPassResonance = 1f;
        
        [Header("High Pass Filter")]
        public float highPassCutoff = 10f;
        public float highPassResonance = 1f;
    }

    /// <summary>
    /// Audio Events - ScriptableObject for defining audio event triggers
    /// </summary>
    [CreateAssetMenu(fileName = "AudioEvent", menuName = "EndlessRunner/Audio/Audio Event")]
    public class AudioEvent : ScriptableObject
    {
        [Header("Event Info")]
        public string eventId;
        public string displayName;
        [TextArea(2, 3)]
        public string description;

        [Header("Audio Response")]
        public AudioEventResponse[] responses;
        public bool randomizeResponse = false;
        public float cooldownTime = 0f;

        [Header("Conditions")]
        public AudioEventCondition[] conditions;

        /// <summary>
        /// Play this audio event
        /// </summary>
        public void Play(Vector3? position = null, Transform parent = null)
        {
            if (AudioManager.Instance == null) return;

            // Check conditions
            if (!CheckConditions()) return;

            // Get response to play
            var response = GetResponse();
            if (response == null) return;

            // Play the sound
            switch (response.responseType)
            {
                case AudioResponseType.PlaySound:
                    AudioManager.Instance.PlaySound(response.soundId, position, parent);
                    break;
                case AudioResponseType.PlayMusic:
                    AudioManager.Instance.PlayMusic(response.musicId, response.crossfade);
                    break;
                case AudioResponseType.StopMusic:
                    AudioManager.Instance.StopMusic(response.fadeOut);
                    break;
                case AudioResponseType.ChangeVolume:
                    // Implementation would depend on which volume to change
                    break;
            }
        }

        private bool CheckConditions()
        {
            if (conditions == null || conditions.Length == 0)
                return true;

            foreach (var condition in conditions)
            {
                if (!condition.IsConditionMet())
                    return false;
            }
            return true;
        }

        private AudioEventResponse GetResponse()
        {
            if (responses == null || responses.Length == 0)
                return null;

            if (randomizeResponse && responses.Length > 1)
            {
                return responses[Random.Range(0, responses.Length)];
            }
            
            return responses[0];
        }
    }

    [System.Serializable]
    public class AudioEventResponse
    {
        public AudioResponseType responseType;
        public string soundId;
        public string musicId;
        public bool crossfade = true;
        public bool fadeOut = true;
        public float volumeTarget = 1f;
        public float delay = 0f;
    }

    [System.Serializable]
    public class AudioEventCondition
    {
        public ConditionType conditionType;
        public string parameterName;
        public ComparisonType comparison;
        public float targetValue;
        public string targetString;
        public bool boolValue;

        public bool IsConditionMet()
        {
            // This would need to be implemented based on your game's parameter system
            // For now, always return true
            return true;
        }
    }

    #region Enums

    public enum MusicMood
    {
        Calm,
        Neutral,
        Energetic,
        Tense,
        Epic,
        Melancholic,
        Mysterious,
        Heroic
    }

    public enum GameplayContext
    {
        Menu,
        Gameplay,
        Pause,
        GameOver,
        Victory,
        Loading,
        Settings,
        Cutscene,
        Boss,
        PowerUp,
        Danger
    }

    public enum MusicTransitionType
    {
        Immediate,
        Crossfade,
        FadeOut,
        WaitForEnd,
        Stinger // Short musical phrase that bridges tracks
    }

    public enum LayerBehavior
    {
        VolumeBlend, // Fade in/out based on intensity
        OnOff, // Either playing or not
        Probability // Random chance to play based on intensity
    }

    public enum SoundPriority
    {
        Low = 1,
        Normal = 128,
        High = 200,
        Critical = 256
    }

    public enum SoundCategory
    {
        SFX,
        Voice,
        Music,
        Ambient,
        UI,
        Footsteps,
        Weapons,
        Environment,
        Pickup,
        Effect
    }

    public enum AudioEffectType
    {
        None,
        Reverb,
        Echo,
        Distortion,
        LowPassFilter,
        HighPassFilter,
        Chorus,
        Flanger
    }

    public enum EnvironmentType
    {
        Indoor,
        Outdoor,
        Underground,
        Water,
        Sky,
        Forest,
        City,
        Desert,
        Snow,
        Cave
    }

    public enum WeatherCondition
    {
        Clear,
        Rain,
        Storm,
        Snow,
        Fog,
        Wind,
        Calm
    }

    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Night,
        Midnight
    }

    public enum AudioResponseType
    {
        PlaySound,
        PlayMusic,
        StopMusic,
        StopSound,
        ChangeVolume,
        ChangePitch,
        ApplyEffect
    }

    public enum ConditionType
    {
        FloatComparison,
        BoolComparison,
        StringComparison,
        GameState,
        PlayerHealth,
        Score,
        Time
    }

    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    #endregion
}
