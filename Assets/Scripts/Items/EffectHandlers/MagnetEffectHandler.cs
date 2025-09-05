using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EndlessRunner.Data;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Items
{
    /// <summary>
    /// MagnetEffectHandler - Xử lý magnet effect để attract items về phía player
    /// </summary>
    public class MagnetEffectHandler : BaseEffectHandler
    {
        #region Serialized Fields

        [Header("Magnet Settings")]
        [SerializeField] private LayerMask _itemLayerMask = -1;
        [SerializeField] private float _attractionForce = 15f;
        [SerializeField] private float _maxAttractionDistance = 10f;
        [SerializeField] private AnimationCurve _attractionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Performance")]
        [SerializeField] private int _maxItemsPerFrame = 10;
        [SerializeField] private float _scanFrequency = 0.1f;

        [Header("Visual Feedback")]
        [SerializeField] private bool _showMagnetRadius = true;
        [SerializeField] private Color _magnetRadiusColor = Color.cyan;

        #endregion

        #region Private Fields

        private List<Collider> _nearbyItems = new List<Collider>();
        private Collider[] _itemScanBuffer;
        private float _lastScanTime;
        private float _currentRadius;

        // Visual feedback
        private LineRenderer _radiusVisualizer;

        #endregion

        #region BaseEffectHandler Implementation

        public override ItemType HandledItemType => ItemType.Magnet;

        protected override void OnInitialize()
        {
            // Initialize scan buffer
            _itemScanBuffer = new Collider[50]; // Reasonable buffer size

            // Create visual feedback if needed
            if (_showMagnetRadius)
            {
                CreateRadiusVisualizer();
            }

            LogDebug("[MagnetEffectHandler] Initialized magnet system");
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_currentEffect == null) return;

            // Update current radius from effect
            _currentRadius = _currentEffect.CurrentEffectValue;

            // Scan for nearby items at specified frequency
            if (Time.unscaledTime - _lastScanTime >= _scanFrequency)
            {
                ScanForNearbyItems();
                _lastScanTime = Time.unscaledTime;
            }

            // Attract nearby items
            AttractNearbyItems(deltaTime);

            // Update visual feedback
            UpdateRadiusVisualization();
        }

        protected override void OnEffectStart(ActiveItemEffect effect)
        {
            _currentRadius = effect.CurrentEffectValue;
            
            LogDebug($"[MagnetEffectHandler] Magnet effect started - radius: {_currentRadius:F1}");

            // Enable visual feedback
            if (_radiusVisualizer != null)
            {
                _radiusVisualizer.enabled = true;
            }
        }

        protected override void OnEffectEnd(ItemType itemType, string itemId)
        {
            _currentRadius = 0f;
            _nearbyItems.Clear();

            LogDebug("[MagnetEffectHandler] Magnet effect ended");

            // Disable visual feedback
            if (_radiusVisualizer != null)
            {
                _radiusVisualizer.enabled = false;
            }
        }

        protected override void OnEffectStack(ActiveItemEffect effect, int previousStackCount)
        {
            // Update radius when stacked
            _currentRadius = effect.CurrentEffectValue;
            
            LogDebug($"[MagnetEffectHandler] Magnet effect stacked - new radius: {_currentRadius:F1}");
        }

        protected override void OnCleanup()
        {
            _nearbyItems.Clear();
            
            if (_radiusVisualizer != null)
            {
                DestroyImmediate(_radiusVisualizer.gameObject);
            }
        }

        #endregion

        #region Magnet Logic

        private void ScanForNearbyItems()
        {
            if (_currentRadius <= 0f || transform == null) return;

            _nearbyItems.Clear();

            // Use OverlapSphereNonAlloc for performance
            int itemCount = Physics.OverlapSphereNonAlloc(
                transform.position, 
                _currentRadius, 
                _itemScanBuffer, 
                _itemLayerMask
            );

            for (int i = 0; i < itemCount && i < _maxItemsPerFrame; i++)
            {
                Collider item = _itemScanBuffer[i];
                if (item != null && IsValidItem(item))
                {
                    _nearbyItems.Add(item);
                }
            }

            LogDebug($"[MagnetEffectHandler] Found {_nearbyItems.Count} nearby items");
        }

        private void AttractNearbyItems(float deltaTime)
        {
            if (_nearbyItems.Count == 0 || _currentRadius <= 0f) return;

            Vector3 playerPosition = transform.position;

            for (int i = _nearbyItems.Count - 1; i >= 0; i--)
            {
                Collider item = _nearbyItems[i];
                
                if (item == null)
                {
                    _nearbyItems.RemoveAt(i);
                    continue;
                }

                ApplyMagneticForce(item, playerPosition, deltaTime);
            }
        }

        private void ApplyMagneticForce(Collider item, Vector3 playerPosition, float deltaTime)
        {
            Vector3 itemPosition = item.transform.position;
            Vector3 directionToPlayer = (playerPosition - itemPosition).normalized;
            float distance = Vector3.Distance(itemPosition, playerPosition);

            // Skip if too close (to prevent jittering)
            if (distance < 0.5f) return;

            // Calculate force based on distance and attraction curve
            float normalizedDistance = Mathf.Clamp01(distance / _currentRadius);
            float forceMultiplier = _attractionCurve.Evaluate(1f - normalizedDistance);
            
            Vector3 attractionForce = directionToPlayer * _attractionForce * forceMultiplier * deltaTime;

            // Apply force to item's Rigidbody if it exists
            Rigidbody itemRigidbody = item.GetComponent<Rigidbody>();
            if (itemRigidbody != null)
            {
                itemRigidbody.AddForce(attractionForce, ForceMode.Force);
            }
            else
            {
                // Fallback: direct transform movement
                item.transform.position += attractionForce * 0.1f; // Reduced for smoothness
            }

            // Check if item should be collected automatically
            if (distance < 1f)
            {
                TryCollectItem(item);
            }
        }

        private bool IsValidItem(Collider item)
        {
            // Check if item has ItemPickup component
            var itemPickup = item.GetComponent<ItemPickup>();
            if (itemPickup == null) return false;

            // Check if item is already being attracted or collected
            if (itemPickup.IsPickedUp) return false;

            // Check if item is within max attraction distance
            float distance = Vector3.Distance(transform.position, item.transform.position);
            return distance <= _maxAttractionDistance;
        }

        private void TryCollectItem(Collider item)
        {
            var itemPickup = item.GetComponent<ItemPickup>();
            if (itemPickup != null && !itemPickup.IsPickedUp)
            {
                // Trigger collection through ItemPickup
                itemPickup.Collect(_runnerController.gameObject);
                LogDebug($"[MagnetEffectHandler] Auto-collected item: {itemPickup.name}");
            }
        }

        #endregion

        #region Visual Feedback

        private void CreateRadiusVisualizer()
        {
            GameObject visualizerGO = new GameObject("MagnetRadiusVisualizer");
            visualizerGO.transform.SetParent(transform);
            visualizerGO.transform.localPosition = Vector3.zero;

            _radiusVisualizer = visualizerGO.AddComponent<LineRenderer>();
            _radiusVisualizer.material = CreateRadiusMaterial();
            _radiusVisualizer.startColor = _magnetRadiusColor;
            _radiusVisualizer.endColor = _magnetRadiusColor;
            _radiusVisualizer.startWidth = 0.1f;
            _radiusVisualizer.endWidth = 0.1f;
            _radiusVisualizer.useWorldSpace = false;
            _radiusVisualizer.enabled = false;

            // Create circle points
            CreateCirclePoints();
        }

        private Material CreateRadiusMaterial()
        {
            // Create simple unlit material for radius visualization
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = _magnetRadiusColor;
            return material;
        }

        private void CreateCirclePoints()
        {
            if (_radiusVisualizer == null) return;

            int segments = 32;
            _radiusVisualizer.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                Vector3 point = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
                );
                _radiusVisualizer.SetPosition(i, point);
            }
        }

        private void UpdateRadiusVisualization()
        {
            if (_radiusVisualizer == null || !_radiusVisualizer.enabled) return;

            // Update circle size based on current radius
            for (int i = 0; i < _radiusVisualizer.positionCount; i++)
            {
                Vector3 point = _radiusVisualizer.GetPosition(i);
                point = point.normalized * _currentRadius;
                _radiusVisualizer.SetPosition(i, point);
            }

            // Update color alpha based on effect intensity
            Color color = _magnetRadiusColor;
            color.a = GetCurrentEffectIntensity() * 0.5f;
            _radiusVisualizer.startColor = color;
            _radiusVisualizer.endColor = color;
        }

        #endregion

        #region Debug & Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _currentRadius <= 0f) return;

            // Draw magnet radius
            Gizmos.color = _magnetRadiusColor;
            Gizmos.DrawWireSphere(transform.position, _currentRadius);

            // Draw nearby items
            Gizmos.color = Color.yellow;
            foreach (var item in _nearbyItems)
            {
                if (item != null)
                {
                    Gizmos.DrawLine(transform.position, item.transform.position);
                    Gizmos.DrawWireCube(item.transform.position, Vector3.one * 0.5f);
                }
            }
        }

        [ContextMenu("Test Scan Items")]
        private void TestScanItems()
        {
            ScanForNearbyItems();
            Debug.Log($"Found {_nearbyItems.Count} items in radius {_currentRadius}", this);
        }

        #endregion
    }
}
