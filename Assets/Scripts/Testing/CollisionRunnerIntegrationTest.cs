using System;
using System.Collections;
using UnityEngine;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Testing
{
    /// <summary>
    /// Integration test cho CollisionDetector và RunnerController
    /// Test Hit state transitions và event flow
    /// </summary>
    [AddComponentMenu("EndlessRunner/Testing/Collision Runner Integration Test")]
    public class CollisionRunnerIntegrationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField, Tooltip("Auto run tests on Start")]
        private bool _autoRunTests = false;
        
        [SerializeField, Tooltip("Test delay between steps (seconds)")]
        [Range(0.1f, 2f)]
        private float _testDelay = 0.5f;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Enable detailed logging")]
        private bool _enableLogging = true;
        
        [SerializeField, Tooltip("Show visual indicators")]
        private bool _showVisualIndicators = true;
        
        // Test results
        private int _testsRun = 0;
        private int _testsPassed = 0;
        private int _testsFailed = 0;
        
        // Test state
        private bool _isRunningTests = false;
        private CollisionDetector _collisionDetector;
        private RunnerController _runnerController;
        private HealthComponent _healthComponent;
        
        // Event tracking
        private int _obstacleHitEventCount = 0;
        private int _stateChangeEventCount = 0;
        private int _actionPerformedEventCount = 0;
        private RunnerState _lastState = RunnerState.Running;
        private RunnerAction _lastAction = RunnerAction.Jump;
        
        private void Start()
        {
            SetupComponents();
            
            if (_autoRunTests)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        private void SetupComponents()
        {
            // Get or create required components
            _collisionDetector = GetComponent<CollisionDetector>();
            _runnerController = GetComponent<RunnerController>();
            _healthComponent = GetComponent<HealthComponent>();
            
            if (_collisionDetector == null)
            {
                LogError("CollisionDetector component not found! Add it to this GameObject.");
                return;
            }
            
            if (_runnerController == null)
            {
                LogError("RunnerController component not found! Add it to this GameObject.");
                return;
            }
            
            if (_healthComponent == null)
            {
                LogError("HealthComponent component not found! Add it to this GameObject.");
                return;
            }
            
            // Subscribe to events
            SubscribeToEvents();
            
            Log("Integration test components setup complete.");
        }
        
        private void SubscribeToEvents()
        {
            if (_collisionDetector != null)
            {
                _collisionDetector.OnObstacleHit += OnObstacleHitEvent;
                _collisionDetector.OnAnyCollision += OnAnyCollisionEvent;
            }
            
            if (_runnerController != null)
            {
                _runnerController.OnStateChanged += OnStateChangedEvent;
                _runnerController.OnActionPerformed += OnActionPerformedEvent;
            }
            
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged += OnHealthChangedEvent;
                _healthComponent.OnIFramesStarted += OnIFramesStartedEvent;
                _healthComponent.OnIFramesEnded += OnIFramesEndedEvent;
            }
        }
        
        /// <summary>
        /// Run all integration tests
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunTests()
        {
            if (_isRunningTests)
            {
                Log("Tests already running...");
                return;
            }
            
            StartCoroutine(RunAllTests());
        }
        
        private IEnumerator RunAllTests()
        {
            _isRunningTests = true;
            ResetTestStats();
            
            Log("=== Starting Collision-Runner Integration Tests ===");
            
            // Test 1: Basic Hit State Transition
            yield return StartCoroutine(TestBasicHitStateTransition());
            
            // Test 2: I-Frames Protection
            yield return StartCoroutine(TestIFramesProtection());
            
            // Test 3: Event Flow Sequence
            yield return StartCoroutine(TestEventFlowSequence());
            
            // Test 4: Health Integration
            yield return StartCoroutine(TestHealthIntegration());
            
            // Test 5: Multiple Collisions
            yield return StartCoroutine(TestMultipleCollisions());
            
            // Print final results
            PrintTestResults();
            
            _isRunningTests = false;
        }
        
        /// <summary>
        /// Test 1: Basic Hit State Transition
        /// Running → Hit → IFrames → Running
        /// </summary>
        private IEnumerator TestBasicHitStateTransition()
        {
            Log("--- Test 1: Basic Hit State Transition ---");
            StartTest();
            
            // Ensure starting state
            if (_runnerController.CurrentState != RunnerState.Running)
            {
                _runnerController.ResetToRunning();
                yield return new WaitForSeconds(0.1f);
            }
            
            RunnerState initialState = _runnerController.CurrentState;
            Assert(initialState == RunnerState.Running, "Should start in Running state");
            
            // Trigger hit via CollisionDetector
            _collisionDetector.ForceObstacleCollision("IntegrationTest");
            
            yield return new WaitForSeconds(0.1f);
            
            // Should be in Hit state briefly
            RunnerState hitState = _runnerController.CurrentState;
            Assert(hitState == RunnerState.Hit || hitState == RunnerState.IFrames, 
                "Should be in Hit or IFrames state after collision");
            
            // Wait for Hit state to complete and enter I-frames
            yield return new WaitForSeconds(0.3f);
            
            RunnerState iFrameState = _runnerController.CurrentState;
            Assert(iFrameState == RunnerState.IFrames, "Should be in IFrames state");
            
            // Wait for I-frames to complete
            yield return new WaitForSeconds(1.5f);
            
            RunnerState finalState = _runnerController.CurrentState;
            Assert(finalState == RunnerState.Running, "Should return to Running state");
            
            CompleteTest("Basic Hit State Transition");
        }
        
        /// <summary>
        /// Test 2: I-Frames Protection
        /// Verify that hits during I-frames are blocked
        /// </summary>
        private IEnumerator TestIFramesProtection()
        {
            Log("--- Test 2: I-Frames Protection ---");
            StartTest();
            
            // Reset to clean state
            _runnerController.ResetToRunning();
            int initialHealth = _healthComponent.CurrentHealth;
            yield return new WaitForSeconds(0.1f);
            
            // First hit to enter I-frames
            _collisionDetector.ForceObstacleCollision("FirstHit");
            yield return new WaitForSeconds(0.3f);
            
            Assert(_runnerController.IsInIFrames, "Should be in I-frames after first hit");
            
            int healthAfterFirstHit = _healthComponent.CurrentHealth;
            Assert(healthAfterFirstHit < initialHealth, "Health should decrease after first hit");
            
            // Second hit during I-frames (should be blocked)
            _collisionDetector.ForceObstacleCollision("SecondHit");
            yield return new WaitForSeconds(0.1f);
            
            int healthAfterSecondHit = _healthComponent.CurrentHealth;
            Assert(healthAfterSecondHit == healthAfterFirstHit, 
                "Health should not decrease during I-frames");
            
            CompleteTest("I-Frames Protection");
        }
        
        /// <summary>
        /// Test 3: Event Flow Sequence
        /// Verify all events fire in correct order
        /// </summary>
        private IEnumerator TestEventFlowSequence()
        {
            Log("--- Test 3: Event Flow Sequence ---");
            StartTest();
            
            // Reset event counters
            ResetEventCounters();
            _runnerController.ResetToRunning();
            yield return new WaitForSeconds(0.1f);
            
            // Trigger collision and track events
            _collisionDetector.ForceObstacleCollision("EventTest");
            
            yield return new WaitForSeconds(0.1f);
            
            Assert(_obstacleHitEventCount > 0, "OnObstacleHit event should fire");
            Assert(_stateChangeEventCount > 0, "OnStateChanged event should fire");
            Assert(_actionPerformedEventCount > 0, "OnActionPerformed event should fire");
            Assert(_lastAction == RunnerAction.Hit, "Last action should be Hit");
            
            CompleteTest("Event Flow Sequence");
        }
        
        /// <summary>
        /// Test 4: Health Integration
        /// Verify health system integration
        /// </summary>
        private IEnumerator TestHealthIntegration()
        {
            Log("--- Test 4: Health Integration ---");
            StartTest();
            
            // Start with full health
            _runnerController.ResetToRunning();
            yield return new WaitForSeconds(0.1f);
            
            int maxHealth = _healthComponent.MaxHealth;
            int currentHealth = _healthComponent.CurrentHealth;
            
            Assert(currentHealth == maxHealth, "Should start with full health");
            
            // Deal damage through collision
            for (int i = 0; i < maxHealth; i++)
            {
                _collisionDetector.ForceObstacleCollision($"DamageTest_{i}");
                
                // Wait for hit to process
                yield return new WaitForSeconds(0.2f);
                
                // Wait for I-frames to end before next hit
                yield return new WaitForSeconds(1.3f);
                
                int expectedHealth = maxHealth - (i + 1);
                int actualHealth = _healthComponent.CurrentHealth;
                
                Log($"Hit {i + 1}: Expected {expectedHealth}, Actual {actualHealth}");
                Assert(actualHealth == expectedHealth, 
                    $"Health should be {expectedHealth} after {i + 1} hits");
                
                if (actualHealth <= 0)
                {
                    Assert(_runnerController.CurrentState == RunnerState.Dead, 
                        "Should be in Dead state when health reaches 0");
                    break;
                }
            }
            
            CompleteTest("Health Integration");
        }
        
        /// <summary>
        /// Test 5: Multiple Collisions Handling
        /// </summary>
        private IEnumerator TestMultipleCollisions()
        {
            Log("--- Test 5: Multiple Collisions Handling ---");
            StartTest();
            
            // Reset to clean state
            _runnerController.ResetToRunning();
            ResetEventCounters();
            yield return new WaitForSeconds(0.1f);
            
            int initialObstacleCount = _collisionDetector.ObstacleCollisionCount;
            
            // Rapid fire collisions (only first should count due to cooldown)
            for (int i = 0; i < 5; i++)
            {
                _collisionDetector.ForceObstacleCollision($"RapidFire_{i}");
                yield return new WaitForFixedUpdate(); // Very small delay
            }
            
            yield return new WaitForSeconds(0.2f);
            
            int finalObstacleCount = _collisionDetector.ObstacleCollisionCount;
            int collisionIncrease = finalObstacleCount - initialObstacleCount;
            
            // Should only process some collisions due to cooldown and I-frames
            Assert(collisionIncrease >= 1 && collisionIncrease <= 2, 
                $"Should process 1-2 collisions, got {collisionIncrease}");
            
            CompleteTest("Multiple Collisions Handling");
        }
        
        #region Test Utilities
        
        private void StartTest()
        {
            _testsRun++;
        }
        
        private void CompleteTest(string testName)
        {
            _testsPassed++;
            Log($"✓ {testName} - PASSED");
        }
        
        private void FailTest(string testName, string reason)
        {
            _testsFailed++;
            LogError($"✗ {testName} - FAILED: {reason}");
        }
        
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception($"Assertion failed: {message}");
            }
        }
        
        private void ResetTestStats()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
        }
        
        private void ResetEventCounters()
        {
            _obstacleHitEventCount = 0;
            _stateChangeEventCount = 0;
            _actionPerformedEventCount = 0;
        }
        
        private void PrintTestResults()
        {
            Log("=== Test Results ===");
            Log($"Tests Run: {_testsRun}");
            Log($"Tests Passed: {_testsPassed}");
            Log($"Tests Failed: {_testsFailed}");
            
            if (_testsFailed == 0)
            {
                Log("🎉 ALL TESTS PASSED! Integration working correctly.");
            }
            else
            {
                LogError($"❌ {_testsFailed} test(s) failed. Check implementation.");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnObstacleHitEvent(CollisionEventData data)
        {
            _obstacleHitEventCount++;
            Log($"Event: ObstacleHit - {data.gameObject?.name ?? "Unknown"}");
        }
        
        private void OnAnyCollisionEvent(CollisionEventData data)
        {
            Log($"Event: AnyCollision - {data.gameObject?.name ?? "Unknown"}");
        }
        
        private void OnStateChangedEvent(RunnerState oldState, RunnerState newState, float duration)
        {
            _stateChangeEventCount++;
            _lastState = newState;
            Log($"Event: StateChanged - {oldState} → {newState} ({duration:F2}s)");
        }
        
        private void OnActionPerformedEvent(RunnerAction action, int parameter, bool success)
        {
            _actionPerformedEventCount++;
            _lastAction = action;
            Log($"Event: ActionPerformed - {action} (param: {parameter}, success: {success})");
        }
        
        private void OnHealthChangedEvent(int currentHealth, int maxHealth)
        {
            Log($"Event: HealthChanged - {currentHealth}/{maxHealth}");
        }
        
        private void OnIFramesStartedEvent(float duration)
        {
            Log($"Event: I-Frames Started - {duration:F2}s");
        }
        
        private void OnIFramesEndedEvent()
        {
            Log("Event: I-Frames Ended");
        }
        
        #endregion
        
        #region Debug Utilities
        
        private void Log(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[IntegrationTest] {message}", this);
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[IntegrationTest] {message}", this);
        }
        
        /// <summary>
        /// Get current test status
        /// </summary>
        public string GetTestStatus()
        {
            if (_isRunningTests)
            {
                return "Running tests...";
            }
            
            if (_testsRun == 0)
            {
                return "No tests run yet";
            }
            
            return $"Tests: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed";
        }
        
        /// <summary>
        /// Manual test methods for inspector
        /// </summary>
        [ContextMenu("Test: Force Hit")]
        private void DebugForceHit()
        {
            if (Application.isPlaying && _collisionDetector != null)
            {
                _collisionDetector.ForceObstacleCollision("ManualTest");
            }
        }
        
        [ContextMenu("Test: Reset to Running")]
        private void DebugResetToRunning()
        {
            if (Application.isPlaying && _runnerController != null)
            {
                _runnerController.ResetToRunning();
            }
        }
        
        [ContextMenu("Test: Print Status")]
        private void DebugPrintStatus()
        {
            if (Application.isPlaying)
            {
                Log($"Current Status:\n{GetTestStatus()}");
                
                if (_runnerController != null)
                {
                    Log($"Runner State: {_runnerController.CurrentState}");
                    Log($"Is Alive: {_runnerController.IsAlive}");
                    Log($"Is In I-Frames: {_runnerController.IsInIFrames}");
                }
                
                if (_healthComponent != null)
                {
                    Log($"Health: {_healthComponent.CurrentHealth}/{_healthComponent.MaxHealth}");
                }
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDisable()
        {
            // Unsubscribe from events
            if (_collisionDetector != null)
            {
                _collisionDetector.OnObstacleHit -= OnObstacleHitEvent;
                _collisionDetector.OnAnyCollision -= OnAnyCollisionEvent;
            }
            
            if (_runnerController != null)
            {
                _runnerController.OnStateChanged -= OnStateChangedEvent;
                _runnerController.OnActionPerformed -= OnActionPerformedEvent;
            }
            
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnHealthChangedEvent;
                _healthComponent.OnIFramesStarted -= OnIFramesStartedEvent;
                _healthComponent.OnIFramesEnded -= OnIFramesEndedEvent;
            }
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showVisualIndicators || !Application.isPlaying) return;
            
            Vector3 pos = transform.position + Vector3.up * 3f;
            
            // Draw test status
            Color statusColor = _isRunningTests ? Color.yellow : 
                               _testsFailed > 0 ? Color.red : Color.green;
            
            Gizmos.color = statusColor;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
            
            // Draw event indicators
            if (_obstacleHitEventCount > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pos + Vector3.right, 0.1f);
            }
            
            if (_stateChangeEventCount > 0)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pos + Vector3.left, 0.1f);
            }
        }
#endif
        
        #endregion
    }
}
