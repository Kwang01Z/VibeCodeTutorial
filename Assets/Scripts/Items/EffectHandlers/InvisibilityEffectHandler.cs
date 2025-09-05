using UnityEngine;
using System.Collections.Generic;
using EndlessRunner.Data;

namespace EndlessRunner.Items
{
    /// <summary>
    /// InvisibilityEffectHandler - Xử lý invisibility effects để player tránh collision với obstacles
    /// </summary>
    public class InvisibilityEffectHandler : BaseEffectHandler
    {
        #region Serialized Fields

        [Header("Invisibility Settings")]
        [SerializeField] private LayerMask _obstacleLayerMask = -1;
        [SerializeField] private int _invisibleLayer = 31; // Layer for invisible state
        [SerializeField] private bool _disableObstacleCollision = true;
        [SerializeField] private bool _maintainItemCollection = true;

        [Header("Visual Effects")]
        [SerializeField] private bool _enableVisualFeedback = true;
        [SerializeField] private float _transparencyAlpha = 0.3f;
        [SerializeField] private Color _invisibilityTint = Color.cyan;
        [SerializeField] private bool _enableShimmering = true;
        [SerializeField] private float _shimmerSpeed = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip _invisibilityStartSound;
        [SerializeField] private AudioClip _invisibilityEndSound;
        [SerializeField] private AudioSource _audioSource;

        #endregion

        #region Private Fields

        private int _originalLayer;
        private LayerMask _originalCollisionMask;
        private bool _isInvisible = false;

        // Visual effects
        private List<Renderer> _playerRenderers = new List<Renderer>();
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private Dictionary<Renderer, Material[]> _invisibilityMaterials = new Dictionary<Renderer, Material[]>();
        
        // Collision management
        private Collider _playerCollider;
        private List<Collider> _playerColliders = new List<Collider>();

        // Animation
        private float _shimmerTime = 0f;

        #endregion

        #region BaseEffectHandler Implementation

        public override ItemType HandledItemType => ItemType.Invisible;

        protected override void OnInitialize()
        {
            // Cache player components
            CachePlayerComponents();
            
            // Setup audio
            SetupAudio();
            
            // Prepare invisibility materials
            if (_enableVisualFeedback)
            {
                PrepareInvisibilityMaterials();
            }

            LogDebug("[InvisibilityEffectHandler] Initialized invisibility system");
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!_isInvisible) return;

            // Update shimmering effect
            if (_enableShimmering && _enableVisualFeedback)
            {
                UpdateShimmeringEffect(deltaTime);
            }
        }

        protected override void OnEffectStart(ActiveItemEffect effect)
        {
            LogDebug("[InvisibilityEffectHandler] Invisibility effect started");

            // Enable invisibility
            EnableInvisibility(true);
            
            // Play start sound
            PlaySound(_invisibilityStartSound);
        }

        protected override void OnEffectEnd(ItemType itemType, string itemId)
        {
            LogDebug("[InvisibilityEffectHandler] Invisibility effect ended");

            // Disable invisibility
            EnableInvisibility(false);
            
            // Play end sound
            PlaySound(_invisibilityEndSound);
        }

        protected override void OnEffectStack(ActiveItemEffect effect, int previousStackCount)
        {
            // For invisibility, stacking just extends duration
            // Visual and collision effects remain the same
            LogDebug($"[InvisibilityEffectHandler] Invisibility stacked - total duration extended");
        }

        protected override void OnCleanup()
        {
            // Ensure invisibility is disabled
            EnableInvisibility(false);
            
            // Cleanup materials
            CleanupInvisibilityMaterials();
        }

        #endregion

        #region Component Caching

        private void CachePlayerComponents()
        {
            // Cache all renderers on player
            _playerRenderers.Clear();
            _playerRenderers.AddRange(GetComponentsInChildren<Renderer>());

            // Cache all colliders on player
            _playerColliders.Clear();
            _playerColliders.AddRange(GetComponentsInChildren<Collider>());
            _playerCollider = GetComponent<Collider>(); // Primary collider

            // Store original layer
            _originalLayer = gameObject.layer;

            LogDebug($"[InvisibilityEffectHandler] Cached {_playerRenderers.Count} renderers, {_playerColliders.Count} colliders");
        }

        private void SetupAudio()
        {
            // Create audio source if not assigned
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

        #endregion

        #region Invisibility Logic

        private void EnableInvisibility(bool enable)
        {
            _isInvisible = enable;

            // Handle collision
            if (_disableObstacleCollision)
            {
                SetCollisionState(enable);
            }

            // Handle visual effects
            if (_enableVisualFeedback)
            {
                SetVisualState(enable);
            }

            LogDebug($"[InvisibilityEffectHandler] Invisibility {(enable ? "enabled" : "disabled")}");
        }

        private void SetCollisionState(bool invisible)
        {
            if (invisible)
            {
                // Change to invisible layer
                ChangeToInvisibleLayer();
                
                // Alternatively or additionally, disable specific collision detection
                DisableObstacleCollisions();
            }
            else
            {
                // Restore original layer
                RestoreOriginalLayer();
                
                // Re-enable collision detection
                EnableObstacleCollisions();
            }
        }

        private void ChangeToInvisibleLayer()
        {
            // Change player to invisible layer
            gameObject.layer = _invisibleLayer;
            
            // Change all child objects too
            SetLayerRecursively(transform, _invisibleLayer);
        }

        private void RestoreOriginalLayer()
        {
            // Restore original layer
            gameObject.layer = _originalLayer;
            
            // Restore all child objects too
            SetLayerRecursively(transform, _originalLayer);
        }

        private void SetLayerRecursively(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            
            for (int i = 0; i < trans.childCount; i++)
            {
                SetLayerRecursively(trans.GetChild(i), layer);
            }
        }

        private void DisableObstacleCollisions()
        {
            // Disable collision between player and obstacle layers
            foreach (var collider in _playerColliders)
            {
                if (collider != null)
                {
                    // Method 1: Use Physics.IgnoreLayerCollision
                    for (int i = 0; i < 32; i++)
                    {
                        if ((_obstacleLayerMask & (1 << i)) != 0)
                        {
                            Physics.IgnoreLayerCollision(collider.gameObject.layer, i, true);
                        }
                    }
                }
            }
        }

        private void EnableObstacleCollisions()
        {
            // Re-enable collision between player and obstacle layers
            foreach (var collider in _playerColliders)
            {
                if (collider != null)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        if ((_obstacleLayerMask & (1 << i)) != 0)
                        {
                            Physics.IgnoreLayerCollision(collider.gameObject.layer, i, false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Visual Effects

        private void PrepareInvisibilityMaterials()
        {
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null) continue;

                // Store original materials
                Material[] originalMats = renderer.materials;
                _originalMaterials[renderer] = originalMats;

                // Create invisibility materials
                Material[] invisMats = new Material[originalMats.Length];
                for (int i = 0; i < originalMats.Length; i++)
                {
                    invisMats[i] = CreateInvisibilityMaterial(originalMats[i]);
                }
                _invisibilityMaterials[renderer] = invisMats;
            }
        }

        private Material CreateInvisibilityMaterial(Material originalMaterial)
        {
            // Create new material based on original
            Material invisMaterial = new Material(originalMaterial);

            // Set transparency mode
            invisMaterial.SetFloat("_Mode", 3); // Transparent mode
            invisMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            invisMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            invisMaterial.SetInt("_ZWrite", 0);
            invisMaterial.DisableKeyword("_ALPHATEST_ON");
            invisMaterial.EnableKeyword("_ALPHABLEND_ON");
            invisMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            invisMaterial.renderQueue = 3000;

            // Apply transparency and tint
            Color color = invisMaterial.color;
            color.a = _transparencyAlpha;
            color = Color.Lerp(color, _invisibilityTint, 0.3f);
            invisMaterial.color = color;

            return invisMaterial;
        }

        private void SetVisualState(bool invisible)
        {
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null) continue;

                if (invisible)
                {
                    // Apply invisibility materials
                    if (_invisibilityMaterials.ContainsKey(renderer))
                    {
                        renderer.materials = _invisibilityMaterials[renderer];
                    }
                }
                else
                {
                    // Restore original materials
                    if (_originalMaterials.ContainsKey(renderer))
                    {
                        renderer.materials = _originalMaterials[renderer];
                    }
                }
            }
        }

        private void UpdateShimmeringEffect(float deltaTime)
        {
            _shimmerTime += deltaTime * _shimmerSpeed;
            
            // Create shimmering effect by modulating alpha
            float shimmer = Mathf.Sin(_shimmerTime) * 0.1f + 0.9f; // 0.8 to 1.0 range
            
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null || !_invisibilityMaterials.ContainsKey(renderer)) continue;

                foreach (var material in _invisibilityMaterials[renderer])
                {
                    if (material != null)
                    {
                        Color color = material.color;
                        color.a = _transparencyAlpha * shimmer;
                        material.color = color;
                    }
                }
            }
        }

        private void CleanupInvisibilityMaterials()
        {
            // Destroy created materials
            foreach (var materials in _invisibilityMaterials.Values)
            {
                foreach (var material in materials)
                {
                    if (material != null)
                    {
                        DestroyImmediate(material);
                    }
                }
            }
            
            _invisibilityMaterials.Clear();
            _originalMaterials.Clear();
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Debug & Utilities

        [ContextMenu("Test Enable Invisibility")]
        private void TestEnableInvisibility()
        {
            if (Application.isPlaying)
            {
                EnableInvisibility(true);
                LogDebug("Test invisibility enabled");
            }
        }

        [ContextMenu("Test Disable Invisibility")]
        private void TestDisableInvisibility()
        {
            if (Application.isPlaying)
            {
                EnableInvisibility(false);
                LogDebug("Test invisibility disabled");
            }
        }

        public string GetInvisibilityStatus()
        {
            return $"Invisibility Handler Status:\n" +
                   $"- Is Invisible: {_isInvisible}\n" +
                   $"- Current Layer: {gameObject.layer}\n" +
                   $"- Original Layer: {_originalLayer}\n" +
                   $"- Renderers Count: {_playerRenderers.Count}\n" +
                   $"- Visual Feedback: {_enableVisualFeedback}\n" +
                   $"- Effect Active: {_currentEffect != null}";
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_isInvisible) return;

            // Draw invisibility indicator
            Gizmos.color = _invisibilityTint;
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            // Draw layer info
            Vector3 labelPos = transform.position + Vector3.up * 3f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"Invisible\nLayer: {gameObject.layer}");
            #endif
        }

        #endregion
    }
}
