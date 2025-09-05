using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using EndlessRunner.Gameplay;
using EndlessRunner.Data;

namespace EndlessRunner.Tests
{
    /// <summary>
    /// Comprehensive unit tests cho SpeedManager system
    /// Tests: Speed curve evaluation, distance tracking, milestones, performance
    /// </summary>
    [TestFixture]
    public class SpeedManagerTests
    {
        #region Test Setup
        
        private GameObject _testGameObject;
        private SpeedManager _speedManager;
        private SpeedCurve _testSpeedCurve;
        private Rigidbody _rigidbody;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject với SpeedManager
            _testGameObject = new GameObject("TestSpeedManager");
            _speedManager = _testGameObject.AddComponent<SpeedManager>();
            _rigidbody = _testGameObject.AddComponent<Rigidbody>();
            
            // Create test SpeedCurve
            _testSpeedCurve = CreateTestSpeedCurve();
            
            // Setup SpeedManager inspector fields via reflection
            SetPrivateField(_speedManager, "_speedConfig", _testSpeedCurve);
            SetPrivateField(_speedManager, "_rigidbody", _rigidbody);
            SetPrivateField(_speedManager, "_distanceMilestones", new float[] { 100f, 250f, 500f });
            SetPrivateField(_speedManager, "_speedTiers", new float[] { 10f, 15f, 20f });
            
            // Initialize position
            _testGameObject.transform.position = Vector3.zero;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
                
            if (_testSpeedCurve != null)
                Object.DestroyImmediate(_testSpeedCurve);
        }
        
        private SpeedCurve CreateTestSpeedCurve()
        {
            var curve = ScriptableObject.CreateInstance<SpeedCurve>();
            
            // Set private fields via reflection
            SetPrivateField(curve, "_baseSpeed", 8f);
            SetPrivateField(curve, "_maxSpeed", 20f);
            SetPrivateField(curve, "_accelerationSmoothness", 2f);
            
            // Create simple linear curve: 0-1km -> 0-12 speed bonus
            var animCurve = new AnimationCurve();
            animCurve.AddKey(0f, 0f);      // 0km = +0 speed
            animCurve.AddKey(1f, 12f);     // 1km = +12 speed (maxed out)
            SetPrivateField(curve, "_speedCurve", animCurve);
            
            return curve;
        }
        
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
        
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }
        
        #endregion
        
        #region Basic Functionality Tests
        
        [Test]
        public void SpeedManager_ImplementsInterface_Success()
        {
            // Test: SpeedManager implements ISpeedManager correctly
            Assert.IsNotNull(_speedManager as ISpeedManager);
            
            ISpeedManager speedInterface = _speedManager;
            Assert.IsNotNull(speedInterface);
            
            LogTestResult("SpeedManager implements ISpeedManager interface");
        }
        
        [Test]
        public void SpeedManager_InitialState_IsCorrect()
        {
            // Test: Initial state values are correct
            Assert.AreEqual(8f, _speedManager.CurrentSpeed, 0.1f, "Initial speed should be base speed");
            Assert.AreEqual(0f, _speedManager.DistanceRun, 0.1f, "Initial distance should be 0");
            Assert.AreEqual(0f, _speedManager.TimeRunning, 0.1f, "Initial time should be 0");
            Assert.IsFalse(_speedManager.IsPaused, "Should not be paused initially");
            Assert.AreEqual(1f, _speedManager.SpeedModifier, 0.1f, "Initial speed modifier should be 1.0");
            
            LogTestResult("Initial state values are correct");
        }
        
        [Test]
        public void SpeedCurve_EvaluateSpeed_WorksCorrectly()
        {
            // Test: SpeedCurve evaluates speed correctly
            Assert.AreEqual(8f, _testSpeedCurve.EvaluateSpeed(0f), 0.1f, "Speed at 0m should be base speed");
            
            float speed500m = _testSpeedCurve.EvaluateSpeed(500f);
            Assert.Greater(speed500m, 8f, "Speed at 500m should be greater than base");
            Assert.Less(speed500m, 20f, "Speed at 500m should not exceed max");
            
            Assert.AreEqual(20f, _testSpeedCurve.EvaluateSpeed(1000f), 0.1f, "Speed at 1000m should be max speed");
            
            LogTestResult($"SpeedCurve evaluation: 0m={8f}, 500m={speed500m:F1}, 1000m={20f}");
        }
        
        #endregion
        
        #region State Control Tests
        
        [Test]
        public void SpeedManager_StartProgression_UpdatesState()
        {
            // Test: StartProgression changes state correctly
            bool eventFired = false;
            _speedManager.OnProgressionStateChanged += (running, distance, speed) => {
                eventFired = true;
                Assert.IsTrue(running, "Progression should be running");
                Assert.AreEqual(0f, distance, 0.1f, "Distance should be 0 at start");
            };
            
            _speedManager.StartProgression();
            
            Assert.IsTrue(eventFired, "OnProgressionStateChanged event should fire");
            LogTestResult("StartProgression updates state correctly");
        }
        
        [Test]
        public void SpeedManager_PauseResume_WorksCorrectly()
        {
            // Test: Pause and resume functionality
            _speedManager.StartProgression();
            
            _speedManager.PauseProgression();
            Assert.IsTrue(_speedManager.IsPaused, "Should be paused after PauseProgression");
            
            _speedManager.ResumeProgression();
            Assert.IsFalse(_speedManager.IsPaused, "Should not be paused after ResumeProgression");
            
            LogTestResult("Pause/Resume functionality works correctly");
        }
        
        [Test]
        public void SpeedManager_ResetProgression_ClearsState()
        {
            // Test: ResetProgression clears all state
            _speedManager.StartProgression();
            _speedManager.SetSpeedModifier(0.5f, 5f);
            
            // Simulate some distance và time
            SetPrivateField(_speedManager, "_distanceRun", 100f);
            SetPrivateField(_speedManager, "_timeRunning", 10f);
            
            _speedManager.ResetProgression();
            
            Assert.AreEqual(0f, _speedManager.DistanceRun, 0.1f, "Distance should be reset");
            Assert.AreEqual(0f, _speedManager.TimeRunning, 0.1f, "Time should be reset");
            Assert.AreEqual(1f, _speedManager.SpeedModifier, 0.1f, "Speed modifier should be reset");
            Assert.IsFalse(_speedManager.IsPaused, "Should not be paused");
            
            LogTestResult("ResetProgression clears state correctly");
        }
        
        #endregion
        
        #region Speed Modifier Tests
        
        [Test]
        public void SpeedManager_SetSpeedModifier_UpdatesCorrectly()
        {
            // Test: Speed modifier updates correctly
            bool eventFired = false;
            _speedManager.OnSpeedModifierApplied += (modifier, duration, reason) => {
                eventFired = true;
                Assert.AreEqual(0.5f, modifier, 0.1f, "Modifier should be 0.5");
                Assert.AreEqual(2f, duration, 0.1f, "Duration should be 2 seconds");
            };
            
            _speedManager.SetSpeedModifier(0.5f, 2f);
            
            Assert.AreEqual(0.5f, _speedManager.SpeedModifier, 0.1f, "Speed modifier should be set");
            Assert.IsTrue(eventFired, "OnSpeedModifierApplied event should fire");
            
            LogTestResult("SetSpeedModifier updates correctly");
        }
        
        [Test]
        public void SpeedManager_SpeedModifier_ClampsMinimumValue()
        {
            // Test: Speed modifier clamps to minimum 0.1
            _speedManager.SetSpeedModifier(-0.5f); // Negative value
            Assert.AreEqual(0.1f, _speedManager.SpeedModifier, 0.01f, "Should clamp to minimum 0.1");
            
            _speedManager.SetSpeedModifier(0.05f); // Below minimum
            Assert.AreEqual(0.1f, _speedManager.SpeedModifier, 0.01f, "Should clamp to minimum 0.1");
            
            LogTestResult("Speed modifier clamps to minimum value");
        }
        
        [Test]
        public void SpeedManager_ClearSpeedModifier_ResetsToNormal()
        {
            // Test: ClearSpeedModifier resets to 1.0
            _speedManager.SetSpeedModifier(0.7f, 5f);
            _speedManager.ClearSpeedModifier();
            
            Assert.AreEqual(1f, _speedManager.SpeedModifier, 0.1f, "Speed modifier should be reset to 1.0");
            
            LogTestResult("ClearSpeedModifier resets to normal");
        }
        
        #endregion
        
        #region Query Methods Tests
        
        [Test]
        public void SpeedManager_GetSpeedAtDistance_ReturnsCorrectValues()
        {
            // Test: GetSpeedAtDistance returns correct values
            Assert.AreEqual(8f, _speedManager.GetSpeedAtDistance(0f), 0.1f, "Speed at 0m");
            
            float speed500 = _speedManager.GetSpeedAtDistance(500f);
            Assert.Greater(speed500, 8f, "Speed at 500m should be higher");
            Assert.Less(speed500, 20f, "Speed at 500m should not exceed max");
            
            Assert.AreEqual(20f, _speedManager.GetSpeedAtDistance(1000f), 0.1f, "Speed at 1000m should be max");
            
            LogTestResult($"GetSpeedAtDistance: 0m={8f}, 500m={speed500:F1}, 1000m={20f}");
        }
        
        [Test]
        public void SpeedManager_GetProgressPercent_ReturnsCorrectValue()
        {
            // Test: GetProgressPercent returns 0-1 value
            SetPrivateField(_speedManager, "_distanceRun", 0f);
            Assert.AreEqual(0f, _speedManager.GetProgressPercent(), 0.1f, "Progress at 0m should be 0");
            
            SetPrivateField(_speedManager, "_distanceRun", 500f);
            float progress = _speedManager.GetProgressPercent();
            Assert.GreaterOrEqual(progress, 0f, "Progress should be >= 0");
            Assert.LessOrEqual(progress, 1f, "Progress should be <= 1");
            
            LogTestResult($"GetProgressPercent at 500m: {progress:F3}");
        }
        
        [Test]
        public void SpeedManager_HasReachedMilestone_TracksCorrectly()
        {
            // Test: HasReachedMilestone tracks correctly
            Assert.IsFalse(_speedManager.HasReachedMilestone(100f), "Should not have reached 100m initially");
            
            // Simulate reaching milestone (would normally happen in CheckMilestones)
            SetPrivateField(_speedManager, "_distanceRun", 150f);
            var reachedMilestones = GetPrivateField<System.Collections.Generic.HashSet<float>>(_speedManager, "_reachedMilestones");
            reachedMilestones.Add(100f);
            
            Assert.IsTrue(_speedManager.HasReachedMilestone(100f), "Should have reached 100m milestone");
            Assert.IsFalse(_speedManager.HasReachedMilestone(250f), "Should not have reached 250m milestone yet");
            
            LogTestResult("HasReachedMilestone tracks correctly");
        }
        
        #endregion
        
        #region Integration Tests
        
        [UnityTest]
        public IEnumerator SpeedManager_UpdateCycle_WorksCorrectly()
        {
            // Test: Full update cycle works correctly
            _speedManager.StartProgression();
            
            // Wait for few frames để components initialize
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            
            // Speed should still be base speed initially (no movement yet)
            Assert.AreEqual(8f, _speedManager.CurrentSpeed, 0.5f, "Should be at base speed initially");
            
            LogTestResult("Update cycle works correctly in play mode");
        }
        
        [UnityTest] 
        public IEnumerator SpeedManager_DistanceTracking_UpdatesOverTime()
        {
            // Test: Distance tracking updates over time
            _speedManager.StartProgression();
            _rigidbody.velocity = Vector3.forward * 10f; // Set forward velocity
            
            float initialDistance = _speedManager.DistanceRun;
            
            // Wait for physics updates
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            
            float finalDistance = _speedManager.DistanceRun;
            
            Assert.Greater(finalDistance, initialDistance, "Distance should increase over time");
            
            LogTestResult($"Distance tracking: {initialDistance:F2} -> {finalDistance:F2}");
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void SpeedManager_PerformanceTest_NoGCAllocation()
        {
            // Test: Major operations don't cause GC allocation
            _speedManager.StartProgression();
            
            // Measure GC before operation
            long gcBefore = System.GC.GetTotalMemory(false);
            
            // Perform operations that should not allocate
            for (int i = 0; i < 1000; i++)
            {
                _speedManager.GetSpeedAtDistance(i * 10f);
                _speedManager.GetProgressPercent();
                _speedManager.SetSpeedModifier(1.0f);
                _speedManager.ClearSpeedModifier();
            }
            
            long gcAfter = System.GC.GetTotalMemory(false);
            long allocated = gcAfter - gcBefore;
            
            // Allow small allocation (<1KB) for reasonable tolerance
            Assert.Less(allocated, 1024, $"Should not allocate significant memory. Allocated: {allocated} bytes");
            
            LogTestResult($"Performance test completed. Memory allocated: {allocated} bytes");
        }
        
        [Test]
        public void SpeedManager_SpeedEvaluation_PerformanceTest()
        {
            // Test: Speed evaluation performance
            const int iterations = 10000;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                float distance = i * 0.1f;
                _speedManager.GetSpeedAtDistance(distance);
            }
            
            stopwatch.Stop();
            
            float avgTimeMs = (float)stopwatch.ElapsedMilliseconds / iterations;
            
            // Should be very fast (< 0.001ms per call)
            Assert.Less(avgTimeMs, 0.001f, $"Speed evaluation too slow: {avgTimeMs:F6}ms per call");
            
            LogTestResult($"Speed evaluation performance: {avgTimeMs:F6}ms per call ({iterations} iterations)");
        }
        
        #endregion
        
        #region Edge Cases Tests
        
        [Test]
        public void SpeedManager_NegativeDistance_HandledCorrectly()
        {
            // Test: Negative distance is handled correctly
            float speed = _speedManager.GetSpeedAtDistance(-100f);
            Assert.AreEqual(8f, speed, 0.1f, "Negative distance should return base speed");
            
            LogTestResult("Negative distance handled correctly");
        }
        
        [Test]
        public void SpeedManager_ExtremelyLargeDistance_HandledCorrectly()
        {
            // Test: Extremely large distance values
            float speed = _speedManager.GetSpeedAtDistance(float.MaxValue / 2f);
            Assert.LessOrEqual(speed, 20f, "Extremely large distance should not exceed max speed");
            Assert.GreaterOrEqual(speed, 8f, "Speed should not be less than base speed");
            
            LogTestResult($"Extremely large distance handled correctly: {speed:F1}");
        }
        
        [Test]
        public void SpeedManager_NullSpeedCurve_HandlesGracefully()
        {
            // Test: Null SpeedCurve is handled gracefully
            SetPrivateField(_speedManager, "_speedConfig", null);
            
            Assert.DoesNotThrow(() => {
                float speed = _speedManager.GetSpeedAtDistance(100f);
                Assert.AreEqual(8f, speed, 0.1f, "Should return default base speed when no curve");
            });
            
            LogTestResult("Null SpeedCurve handled gracefully");
        }
        
        [Test]
        public void SpeedManager_ZeroDurationModifier_WorksCorrectly()
        {
            // Test: Zero duration modifier (permanent)
            _speedManager.SetSpeedModifier(0.8f, 0f); // Permanent modifier
            Assert.AreEqual(0.8f, _speedManager.SpeedModifier, 0.1f, "Permanent modifier should be set");
            
            // Should not auto-clear with time
            LogTestResult("Zero duration (permanent) modifier works correctly");
        }
        
        #endregion
        
        #region Debug Utility Tests
        
        [Test]
        public void SpeedManager_GetDebugInfo_ReturnsValidString()
        {
            // Test: GetDebugInfo returns non-empty string
            string debugInfo = _speedManager.GetDebugInfo();
            
            Assert.IsNotNull(debugInfo, "Debug info should not be null");
            Assert.IsNotEmpty(debugInfo, "Debug info should not be empty");
            Assert.IsTrue(debugInfo.Contains("SpeedManager"), "Debug info should contain 'SpeedManager'");
            Assert.IsTrue(debugInfo.Contains("Speed"), "Debug info should contain speed information");
            
            LogTestResult($"GetDebugInfo returns valid string: {debugInfo.Length} characters");
        }
        
        #endregion
        
        #region Test Utilities
        
        private void LogTestResult(string message)
        {
            Debug.Log($"[SpeedManagerTest] ✅ {message}");
        }
        
        #endregion
    }
}
