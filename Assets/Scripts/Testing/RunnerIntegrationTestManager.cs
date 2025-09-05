using UnityEngine;
using EndlessRunner.Gameplay;
using EndlessRunner.Core;

namespace EndlessRunner.Testing
{
    /// <summary>
    /// Manager để control và monitor Runner Integration Testing
    /// Quản lý test flow và cung cấp automation hooks
    /// </summary>
    public class RunnerIntegrationTestManager : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private GameObject _testPlayer;
        [SerializeField] private bool _enableAutomation = false;
        [SerializeField] private float _testDuration = 60f;
        [SerializeField] private bool _logDetailedStats = true;
        
        [Header("Test Stats")]
        [SerializeField] private int _obstacleHits = 0;
        [SerializeField] private int _pickupsCollected = 0;
        [SerializeField] private int _stateTransitions = 0;
        [SerializeField] private float _testStartTime;
        
        // Components reference
        private RunnerController _runnerController;
        private CollisionDetector _collisionDetector;
        private HealthComponent _healthComponent;
        
        // Test state
        private bool _testRunning = false;
        
        private void Awake()
        {
            if (_testPlayer == null)
            {
                _testPlayer = GameObject.FindWithTag("RunnerIntegrationTest");
            }
            
            if (_testPlayer != null)
            {
                CacheComponents();
                SubscribeToEvents();
            }
        }
        
        private void Start()
        {
            if (_testPlayer != null)
            {
                StartTest();
            }
            else
            {
                Debug.LogWarning("[TEST MANAGER] No test player found! Test cannot start.");
            }
        }
        
        private void Update()
        {
            if (_testRunning && _enableAutomation)
            {
                UpdateAutomation();
            }
            
            // Check test timeout
            if (_testRunning && Time.time - _testStartTime > _testDuration)
            {
                EndTest("Test duration exceeded");
            }
        }
        
        /// <summary>
        /// Set test player externally
        /// </summary>
        public void SetTestPlayer(GameObject player)
        {
            _testPlayer = player;
            if (_testPlayer != null)
            {
                CacheComponents();
                SubscribeToEvents();
            }
        }
        
        /// <summary>
        /// Start integration test
        /// </summary>
        public void StartTest()
        {
            if (_testPlayer == null || _runnerController == null)
            {
                Debug.LogError("[TEST MANAGER] Cannot start test - missing components!");
                return;
            }
            
            _testRunning = true;
            _testStartTime = Time.time;
            ResetStats();
            
            Debug.Log("<color=cyan>[TEST MANAGER] Integration test STARTED</color>");
            Debug.Log($"Test Player: {_testPlayer.name}");
            Debug.Log($"Initial State: {_runnerController.CurrentState}");
            Debug.Log($"Initial Health: {_healthComponent.CurrentHealth}");
            
            if (_enableAutomation)
            {
                Debug.Log("<color=yellow>[TEST MANAGER] Automation mode ENABLED</color>");
            }
        }
        
        /// <summary>
        /// End integration test
        /// </summary>
        public void EndTest(string reason = "Manual stop")
        {
            if (!_testRunning) return;
            
            _testRunning = false;
            float testTime = Time.time - _testStartTime;
            
            Debug.Log($"<color=cyan>[TEST MANAGER] Integration test ENDED</color> - {reason}");
            LogTestResults(testTime);
        }
        
        /// <summary>
        /// Cache component references
        /// </summary>
        private void CacheComponents()
        {
            if (_testPlayer == null) return;
            
            _runnerController = _testPlayer.GetComponent<RunnerController>();
            _collisionDetector = _testPlayer.GetComponent<CollisionDetector>();
            _healthComponent = _testPlayer.GetComponent<HealthComponent>();
            
            if (_runnerController == null)
                Debug.LogError("[TEST MANAGER] RunnerController not found on test player!");
            if (_collisionDetector == null)
                Debug.LogError("[TEST MANAGER] CollisionDetector not found on test player!");
            if (_healthComponent == null)
                Debug.LogError("[TEST MANAGER] HealthComponent not found on test player!");
        }
        
        /// <summary>
        /// Subscribe to test events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_collisionDetector != null)
            {
                _collisionDetector.OnObstacleHit += OnTestObstacleHit;
                _collisionDetector.OnPickupCollected += OnTestPickupCollected;
            }
            
            if (_runnerController != null)
            {
                _runnerController.OnStateChanged += OnTestStateChanged;
                _runnerController.OnActionPerformed += OnTestActionPerformed;
            }
            
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged += OnTestHealthChanged;
                _healthComponent.OnDeath += OnTestPlayerDeath;
            }
        }
        
        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_collisionDetector != null)
            {
                _collisionDetector.OnObstacleHit -= OnTestObstacleHit;
                _collisionDetector.OnPickupCollected -= OnTestPickupCollected;
            }
            
            if (_runnerController != null)
            {
                _runnerController.OnStateChanged -= OnTestStateChanged;
                _runnerController.OnActionPerformed -= OnTestActionPerformed;
            }
            
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnTestHealthChanged;
                _healthComponent.OnDeath -= OnTestPlayerDeath;
            }
        }
        
        /// <summary>
        /// Update automation logic
        /// </summary>
        private void UpdateAutomation()
        {
            // Simple automation - move forward and test actions
            if (_runnerController != null && _runnerController.CurrentState == RunnerState.Running)
            {
                // Auto-move forward
                _testPlayer.transform.Translate(Vector3.forward * 5f * Time.deltaTime);
                
                // Randomly test actions
                if (Random.Range(0f, 1f) < 0.02f) // 2% chance per frame
                {
                    var randomAction = (RunnerAction)Random.Range(0, System.Enum.GetValues(typeof(RunnerAction)).Length);
                    _runnerController.PerformAction(randomAction);
                }
            }
        }
        
        /// <summary>
        /// Reset test statistics
        /// </summary>
        private void ResetStats()
        {
            _obstacleHits = 0;
            _pickupsCollected = 0;
            _stateTransitions = 0;
        }
        
        /// <summary>
        /// Log detailed test results
        /// </summary>
        private void LogTestResults(float testTime)
        {
            Debug.Log("=== INTEGRATION TEST RESULTS ===");
            Debug.Log($"Test Duration: {testTime:F2}s");
            Debug.Log($"Obstacle Hits: {_obstacleHits}");
            Debug.Log($"Pickups Collected: {_pickupsCollected}");
            Debug.Log($"State Transitions: {_stateTransitions}");
            
            if (_healthComponent != null)
            {
                Debug.Log($"Final Health: {_healthComponent.CurrentHealth}");
            }
            
            if (_runnerController != null)
            {
                Debug.Log($"Final State: {_runnerController.CurrentState}");
            }
            
            // Calculate rates
            if (testTime > 0)
            {
                Debug.Log($"Hit Rate: {_obstacleHits / testTime:F2}/sec");
                Debug.Log($"Pickup Rate: {_pickupsCollected / testTime:F2}/sec");
                Debug.Log($"Transition Rate: {_stateTransitions / testTime:F2}/sec");
            }
            
            Debug.Log("=== END TEST RESULTS ===");
        }
        
        #region Event Handlers
        
        private void OnTestObstacleHit(CollisionEventData data)
        {
            _obstacleHits++;
            if (_logDetailedStats)
            {
                Debug.Log($"<color=red>[TEST MANAGER] OBSTACLE HIT #{_obstacleHits}:</color> {data.gameObject.name}");
            }
        }
        
        private void OnTestPickupCollected(CollisionEventData data)
        {
            _pickupsCollected++;
            if (_logDetailedStats)
            {
                Debug.Log($"<color=green>[TEST MANAGER] PICKUP COLLECTED #{_pickupsCollected}:</color> {data.gameObject.name}");
            }
        }
        
        private void OnTestStateChanged(RunnerState oldState, RunnerState newState, float duration)
        {
            _stateTransitions++;
            if (_logDetailedStats)
            {
                Debug.Log($"<color=cyan>[TEST MANAGER] STATE TRANSITION #{_stateTransitions}:</color> {oldState} → {newState} ({duration:F2}s)");
            }
        }
        
        private void OnTestActionPerformed(RunnerAction action, int parameter, bool success)
        {
            if (_logDetailedStats)
            {
                Debug.Log($"<color=magenta>[TEST MANAGER] ACTION:</color> {action} (Param: {parameter}, Success: {success})");
            }
        }
        
        private void OnTestHealthChanged(int oldHealth, int newHealth)
        {
            if (_logDetailedStats)
            {
                Debug.Log($"<color=orange>[TEST MANAGER] HEALTH:</color> {oldHealth} → {newHealth}");
            }
        }
        
        private void OnTestPlayerDeath()
        {
            Debug.Log("<color=red>[TEST MANAGER] PLAYER DEATH - Ending Test</color>");
            EndTest("Player died");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void OnDisable()
        {
            if (_testRunning)
            {
                EndTest("Manager disabled");
            }
        }
    }
}
