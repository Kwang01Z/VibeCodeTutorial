using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace EndlessRunner.Items.VFX
{
    /// <summary>
    /// ParticlePool - Efficient object pooling system for particle systems
    /// Handles dynamic creation, reuse, and cleanup of particle effects
    /// </summary>
    public class ParticlePool : MonoBehaviour
    {
        #region Private Fields

        private ParticleSystem _prefab;
        private Queue<ParticleSystem> _availableParticles = new Queue<ParticleSystem>();
        private HashSet<ParticleSystem> _activeParticles = new HashSet<ParticleSystem>();
        
        private int _initialSize;
        private int _maxSize;
        private bool _autoExpand;
        private bool _isInitialized;

        #endregion

        #region Properties

        public int ActiveCount => _activeParticles.Count;
        public int AvailableCount => _availableParticles.Count;
        public int TotalCount => ActiveCount + AvailableCount;
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the particle pool with specified parameters
        /// </summary>
        public void Initialize(ParticleSystem prefab, int initialSize, int maxSize, bool autoExpand)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ParticlePool] Already initialized, skipping...");
                return;
            }

            _prefab = prefab;
            _initialSize = initialSize;
            _maxSize = maxSize;
            _autoExpand = autoExpand;

            CreateInitialParticles();
            _isInitialized = true;
        }

        /// <summary>
        /// Get a pooled particle system for use
        /// </summary>
        public ParticleSystem GetPooledObject()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[ParticlePool] Pool not initialized!");
                return null;
            }

            ParticleSystem particle = null;

            // Try to get from available queue
            if (_availableParticles.Count > 0)
            {
                particle = _availableParticles.Dequeue();
            }
            // Create new if we can expand
            else if (_autoExpand && TotalCount < _maxSize)
            {
                particle = CreateNewParticle();
            }
            // Reuse oldest active particle as fallback
            else if (_activeParticles.Count > 0)
            {
                Debug.LogWarning("[ParticlePool] Pool exhausted, reusing oldest particle");
                particle = GetOldestActiveParticle();
                if (particle != null)
                {
                    ResetParticle(particle);
                }
            }

            if (particle != null)
            {
                _activeParticles.Add(particle);
                particle.gameObject.SetActive(true);
            }

            return particle;
        }

        /// <summary>
        /// Return a particle system to the pool
        /// </summary>
        public bool ReturnToPool(ParticleSystem particle)
        {
            if (!_isInitialized || particle == null)
                return false;

            // Check if this particle belongs to our pool
            if (!_activeParticles.Contains(particle))
                return false;

            // Remove from active and add to available
            _activeParticles.Remove(particle);
            
            // Reset and deactivate
            ResetParticle(particle);
            particle.gameObject.SetActive(false);
            
            _availableParticles.Enqueue(particle);
            return true;
        }

        /// <summary>
        /// Clear all particles and reset the pool
        /// </summary>
        public void Clear()
        {
            // Stop and destroy all particles
            foreach (var particle in _activeParticles)
            {
                if (particle != null)
                {
                    particle.Stop(true);
                    DestroyImmediate(particle.gameObject);
                }
            }

            while (_availableParticles.Count > 0)
            {
                var particle = _availableParticles.Dequeue();
                if (particle != null)
                {
                    DestroyImmediate(particle.gameObject);
                }
            }

            _activeParticles.Clear();
            _availableParticles.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Warm up the pool by pre-creating particles
        /// </summary>
        public void WarmUp(int additionalCount)
        {
            if (!_isInitialized) return;

            int targetCount = Mathf.Min(TotalCount + additionalCount, _maxSize);
            int particlesToCreate = targetCount - TotalCount;

            for (int i = 0; i < particlesToCreate; i++)
            {
                var particle = CreateNewParticle();
                if (particle != null)
                {
                    particle.gameObject.SetActive(false);
                    _availableParticles.Enqueue(particle);
                }
            }
        }

        #endregion

        #region Private Methods

        private void CreateInitialParticles()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var particle = CreateNewParticle();
                if (particle != null)
                {
                    particle.gameObject.SetActive(false);
                    _availableParticles.Enqueue(particle);
                }
            }
        }

        private ParticleSystem CreateNewParticle()
        {
            if (_prefab == null)
            {
                Debug.LogError("[ParticlePool] Prefab is null!");
                return null;
            }

            var particleGO = Instantiate(_prefab.gameObject, transform);
            var particle = particleGO.GetComponent<ParticleSystem>();
            
            if (particle == null)
            {
                Debug.LogError("[ParticlePool] Prefab doesn't have ParticleSystem component!");
                DestroyImmediate(particleGO);
                return null;
            }

            // Configure for pooling
            ConfigureParticleForPooling(particle);
            
            return particle;
        }

        private void ConfigureParticleForPooling(ParticleSystem particle)
        {
            // Ensure particle doesn't auto-destroy
            var main = particle.main;
            main.stopAction = ParticleSystemStopAction.Disable;
            
            // Add cleanup component if needed
            var cleaner = particle.gameObject.GetComponent<ParticlePoolCleaner>();
            if (cleaner == null)
            {
                cleaner = particle.gameObject.AddComponent<ParticlePoolCleaner>();
            }
            cleaner.Initialize(this, particle);
        }

        private void ResetParticle(ParticleSystem particle)
        {
            if (particle == null) return;

            // Stop the particle system
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // Reset transform
            particle.transform.localPosition = Vector3.zero;
            particle.transform.localRotation = Quaternion.identity;
            particle.transform.localScale = Vector3.one;

            // Clear any remaining particles
            particle.Clear();
        }

        private ParticleSystem GetOldestActiveParticle()
        {
            // Simple implementation - return first found
            // Could be enhanced with timestamp tracking
            foreach (var particle in _activeParticles)
            {
                if (particle != null)
                {
                    _activeParticles.Remove(particle);
                    return particle;
                }
            }
            return null;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Clear();
        }

        #endregion

        #region Debug

        public void LogPoolStats()
        {
            Debug.Log($"[ParticlePool] Stats - Active: {ActiveCount}, Available: {AvailableCount}, Total: {TotalCount}, Max: {_maxSize}");
        }

        [ContextMenu("Log Pool Stats")]
        private void DebugLogStats()
        {
            LogPoolStats();
        }

        #endregion
    }

    /// <summary>
    /// Helper component to automatically return particles to pool when they finish
    /// </summary>
    public class ParticlePoolCleaner : MonoBehaviour
    {
        private ParticlePool _pool;
        private ParticleSystem _particle;
        private Coroutine _cleanupCoroutine;

        public void Initialize(ParticlePool pool, ParticleSystem particle)
        {
            _pool = pool;
            _particle = particle;
        }

        private void OnEnable()
        {
            if (_cleanupCoroutine != null)
            {
                StopCoroutine(_cleanupCoroutine);
            }
            _cleanupCoroutine = StartCoroutine(CheckForCompletion());
        }

        private void OnDisable()
        {
            if (_cleanupCoroutine != null)
            {
                StopCoroutine(_cleanupCoroutine);
                _cleanupCoroutine = null;
            }
        }

        private IEnumerator CheckForCompletion()
        {
            if (_particle == null) yield break;

            // Wait for particle system to finish
            yield return new WaitUntil(() => !_particle.isPlaying && _particle.particleCount == 0);
            
            // Small delay to ensure all particles are gone
            yield return new WaitForSeconds(0.1f);

            // Return to pool
            if (_pool != null && _particle != null)
            {
                _pool.ReturnToPool(_particle);
            }
        }
    }
}
