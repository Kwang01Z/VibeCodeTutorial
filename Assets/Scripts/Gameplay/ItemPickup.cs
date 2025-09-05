using UnityEngine;
using System;
using EndlessRunner.Data;
using EndlessRunner.Core;
using EndlessRunner.Items;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Component cho item pickups trong Phase 2
    /// Handles collision detection, event firing, và auto-return về pool
    /// Đảm bảo zero GC allocation trong hot path
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ItemDefinition _itemDefinition;
        [SerializeField] private bool _autoReturnToPool = true;
        [SerializeField] private float _autoReturnDelay = 0.1f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private ParticleSystem _pickupEffect;
        [SerializeField] private float _pickupAnimDuration = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        // Cached components - Setup in Awake
        private Collider _collider;
        private Transform _transform;
        
        // Pool integration
        private IAutoReturnToPool _poolReturner;
        
        // Performance optimization - cached delegates
        private static readonly System.Action<ItemPickup> _cachedReturnAction = (pickup) => pickup.ReturnToPool();
        
        // Events - static để tránh allocation
        public static event Action<ItemDefinition, Vector3> OnItemPicked;
        
        // State tracking
        private bool _isPickedUp;
        private float _spawnTime;
        
        // Public properties
        public ItemDefinition ItemDefinition => _itemDefinition;
        public bool IsPickedUp => _isPickedUp;
        public float LifeTime => Time.time - _spawnTime;

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache components để tránh GetComponent calls
            _collider = GetComponent<Collider>();
            _transform = transform;
            
            // Validate setup
            ValidateSetup();
            
            // Setup pool returner interface
            _poolReturner = GetComponent<IAutoReturnToPool>();
        }

        private void OnEnable()
        {
            // Reset state khi object được reused từ pool
            _isPickedUp = false;
            _spawnTime = Time.time;
            
            // Ensure collider is enabled
            if (_collider != null)
                _collider.enabled = true;
                
            // Reset visual state
            if (_visualRoot != null)
                _visualRoot.SetActive(true);
        }

        private void OnDisable()
        {
            // Cancel pending return operations
            CancelInvoke(nameof(ReturnToPool));
        }

        private void OnTriggerEnter(Collider other)
        {
            // Performance: Early exit nếu đã picked up
            if (_isPickedUp) return;
            
            // Check player collision - sử dụng cached layer comparison
            int otherLayer = other.gameObject.layer;
            int playerLayer = LayerMask.NameToLayer("Player");
            int playerIFrameLayer = LayerMask.NameToLayer("PlayerIFrame");
            
            if (otherLayer != playerLayer && otherLayer != playerIFrameLayer)
                return;

            // Handle pickup
            HandlePickup(other);
        }

        #endregion

        #region Pickup Logic

        /// <summary>
        /// Xử lý pickup collision - zero GC allocation
        /// </summary>
        private void HandlePickup(Collider playerCollider)
        {
            // Validate item definition
            if (_itemDefinition == null)
            {
                Debug.LogWarning($"[ItemPickup] Item definition is null on {gameObject.name}", this);
                return;
            }
            
            // Mark as picked up immediately để prevent double-pickup
            _isPickedUp = true;
            
            // Disable collider immediately
            _collider.enabled = false;
            
            // Fire pickup event (static event để avoid allocation)
            OnItemPicked?.Invoke(_itemDefinition, _transform.position);
            
            // Apply item effect through ItemEffectIntegrator or ItemEffectSystem
            ApplyItemEffect();
            
            // Play pickup effects
            PlayPickupEffects();
            
            // Auto-return to pool nếu enabled
            if (_autoReturnToPool)
            {
                if (_autoReturnDelay <= 0f)
                {
                    ReturnToPool();
                }
                else
                {
                    // Sử dụng Invoke thay vì Coroutine để tránh GC
                    Invoke(nameof(ReturnToPool), _autoReturnDelay);
                }
            }
        }

        /// <summary>
        /// Play pickup effects (VFX, SFX)
        /// </summary>
        private void PlayPickupEffects()
        {
            // Hide visual immediately
            if (_visualRoot != null)
                _visualRoot.SetActive(false);
            
            // Play particle effect
            if (_pickupEffect != null && !_pickupEffect.isPlaying)
            {
                _pickupEffect.Play();
            }
            
            // Play sound effect
            if (_audioSource != null && _itemDefinition.PickupSound != null)
            {
                _audioSource.PlayOneShot(_itemDefinition.PickupSound);
            }
        }

        /// <summary>
        /// Apply item effect thông qua ItemEffectSystem hoặc ItemEffectIntegrator
        /// </summary>
        private void ApplyItemEffect()
        {
            if (_itemDefinition == null) return;
            
            // Try to apply through ItemEffectIntegrator first (preferred method)
            var integrator = FindObjectOfType<ItemEffectIntegrator>();
            if (integrator != null && integrator.IsInitialized)
            {
                integrator.ApplyItemEffect(_itemDefinition);
                return;
            }
            
            // Fallback: Get ItemEffectSystem instance và apply effect directly
            var effectSystem = FindObjectOfType<ItemEffectSystem>();
            if (effectSystem != null)
            {
                bool success = effectSystem.ActivateItem(_itemDefinition);
#if UNITY_EDITOR
                Debug.Log($"[ItemPickup] Item activated through ItemEffectSystem: {success}");
#endif
            }
            else
            {
                Debug.LogWarning($"[ItemPickup] Neither ItemEffectIntegrator nor ItemEffectSystem found, cannot apply effect for {_itemDefinition.DisplayName}", this);
            }
        }

        #endregion

        #region Pool Integration

        /// <summary>
        /// Return item về pool
        /// </summary>
        public void ReturnToPool()
        {
            // Cancel any pending return operations
            CancelInvoke(nameof(ReturnToPool));
            
            // Use pool returner interface nếu available
            if (_poolReturner != null)
            {
                _poolReturner.ReturnToPool();
            }
            else
            {
                // Fallback: disable GameObject
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Force pickup without collision (for testing)
        /// </summary>
        public void ForcePick()
        {
            if (!_isPickedUp)
            {
                HandlePickup(null);
            }
        }

        /// <summary>
        /// Public method để collect item từ external systems (như MagnetEffectHandler)
        /// </summary>
        public void Collect(GameObject collector = null)
        {
            if (!_isPickedUp)
            {
                HandlePickup(collector?.GetComponent<Collider>());
            }
        }

        /// <summary>
        /// Setup item với new ItemDefinition (pool reuse)
        /// </summary>
        public void Setup(ItemDefinition itemDef)
        {
            _itemDefinition = itemDef;
            
            // Update visual appearance based on item
            UpdateVisualAppearance();
            
            // Reset state
            _isPickedUp = false;
            _spawnTime = Time.time;
        }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Update visual appearance dựa trên ItemDefinition
        /// </summary>
        private void UpdateVisualAppearance()
        {
            if (_itemDefinition == null) return;
            
            // Update icon/sprite nếu có
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && _itemDefinition.Icon != null)
            {
                // Convert sprite to texture cho SpriteRenderer
                spriteRenderer.sprite = _itemDefinition.Icon;
                spriteRenderer.color = _itemDefinition.EffectColor;
            }
            
            // Update material color nếu có
            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Tạo material instance để tránh shared material modification
                var material = meshRenderer.material;
                material.color = _itemDefinition.EffectColor;
            }
            
            // Update particle system color
            if (_pickupEffect != null)
            {
                var main = _pickupEffect.main;
                main.startColor = _itemDefinition.EffectColor;
            }
        }

        #endregion

        #region Validation & Debug
        
        /// <summary>
        /// Debug logging method
        /// </summary>
        private void LogDebug(string message)
        {
            #if UNITY_EDITOR
            Debug.Log(message);
            #endif
        }

        /// <summary>
        /// Validate component setup
        /// </summary>
        private void ValidateSetup()
        {
            // Check required components
            if (_collider == null)
            {
                Debug.LogError($"[ItemPickup] Collider component missing on {gameObject.name}", this);
                return;
            }
            
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"[ItemPickup] Collider should be set as Trigger on {gameObject.name}", this);
            }
            
            // Check layer
            if (gameObject.layer != LayerMask.NameToLayer("Pickup"))
            {
                Debug.LogWarning($"[ItemPickup] GameObject layer should be 'Pickup' on {gameObject.name}", this);
            }
            
            // Check ItemDefinition
            if (_itemDefinition == null)
            {
                Debug.LogWarning($"[ItemPickup] ItemDefinition not assigned on {gameObject.name}", this);
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor validation
        /// </summary>
        private void OnValidate()
        {
            // Auto-setup trigger
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
            
            // Auto-setup layer
            if (gameObject.layer == 0) // Default layer
            {
                gameObject.layer = LayerMask.NameToLayer("Pickup");
            }
            
            // Clamp values
            _autoReturnDelay = Mathf.Max(0f, _autoReturnDelay);
            _pickupAnimDuration = Mathf.Max(0.1f, _pickupAnimDuration);
        }
        
        [ContextMenu("Test Pickup")]
        private void TestPickup()
        {
            ForcePick();
        }
        
        [ContextMenu("Setup Default Item")]
        private void SetupDefaultItem()
        {
            _itemDefinition = ItemDefinition.CreateDefault(ItemType.Magnet);
            UpdateVisualAppearance();
            Debug.Log($"[ItemPickup] Setup with default {_itemDefinition.Type} item");
        }
        #endif

        #endregion
    }

    /// <summary>
    /// Interface cho auto-return to pool functionality
    /// Implemented by AutoReturnToPool component
    /// </summary>
    public interface IAutoReturnToPool
    {
        void ReturnToPool();
    }
    
    /// <summary>
    /// Helper component cho auto-return to pool
    /// Automatically added by PickablePool system
    /// </summary>
    public class AutoReturnToPool<T> : MonoBehaviour, IAutoReturnToPool where T : MonoBehaviour
    {
        private PickablePool<T> _pool;
        private T _component;
        
        public void Initialize(PickablePool<T> pool, T component)
        {
            _pool = pool;
            _component = component;
        }
        
        public void ReturnToPool()
        {
            if (_pool != null && _component != null)
            {
                _pool.Return(_component);
            }
        }
    }
}
