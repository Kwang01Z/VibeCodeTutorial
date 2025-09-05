using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EndlessRunner.Data;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Testing utility cho ItemEffectSystem - kiểm tra tất cả functionality
    /// Bao gồm stacking rules, timer management, event system
    /// </summary>
    public class ItemEffectSystemTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _autoRunTests = false;
        [SerializeField] private float _testInterval = 2f;
        [SerializeField] private bool _enableDetailedLogs = true;

        [Header("Test Items")]
        [SerializeField] private ItemDefinition _testMagnetItem;
        [SerializeField] private ItemDefinition _testMultiplierItem;
        [SerializeField] private ItemDefinition _testInvisibleItem;
        [SerializeField] private ItemDefinition _testLifeItem;

        [Header("Performance Test")]
        [SerializeField] private int _performanceTestCount = 100;
        [SerializeField] private bool _runPerformanceTest = false;

        private ItemEffectSystem _effectSystem;
        private Coroutine _testCoroutine;
        private int _testsPassed;
        private int _testsFailed;

        #region Unity Lifecycle

        private void Start()
        {
            _effectSystem = ItemEffectSystem.Instance;
            SetupDefaultTestItems();

            if (_autoRunTests)
            {
                StartTesting();
            }
        }

        private void OnDestroy()
        {
            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
            }
        }

        #endregion

        #region Test Setup

        private void SetupDefaultTestItems()
        {
            if (_testMagnetItem == null)
                _testMagnetItem = ItemDefinition.CreateDefault(ItemType.Magnet);

            if (_testMultiplierItem == null)
                _testMultiplierItem = ItemDefinition.CreateDefault(ItemType.Multiplier);

            if (_testInvisibleItem == null)
                _testInvisibleItem = ItemDefinition.CreateDefault(ItemType.Invisible);

            if (_testLifeItem == null)
                _testLifeItem = ItemDefinition.CreateDefault(ItemType.Life);

            // Note: ItemDefinition properties are read-only, using default values
            // Default values: Magnet(Duration=10s), Multiplier(Duration=12s), etc.
            Log($"Test items created with default values:");
            Log($"- Magnet: Duration={_testMagnetItem.Duration}s, CanStack={_testMagnetItem.CanStack}, Rule={_testMagnetItem.StackingRule}");
            Log($"- Multiplier: Duration={_testMultiplierItem.Duration}s, CanStack={_testMultiplierItem.CanStack}, Rule={_testMultiplierItem.StackingRule}");
            Log($"- Invisible: Duration={_testInvisibleItem.Duration}s, CanStack={_testInvisibleItem.CanStack}");
            Log($"- Life: Duration={_testLifeItem.Duration}s, CanStack={_testLifeItem.CanStack}");
            
            Log("Test items setup completed");
        }

        #endregion

        #region Public Test Methods

        [ContextMenu("Start All Tests")]
        public void StartTesting()
        {
            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
            }
            
            _testsPassed = 0;
            _testsFailed = 0;
            _testCoroutine = StartCoroutine(RunAllTests());
        }

        [ContextMenu("Stop Tests")]
        public void StopTesting()
        {
            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
                _testCoroutine = null;
            }
            
            Log("Testing stopped");
        }

        [ContextMenu("Test Basic Effect Application")]
        public void TestBasicEffectApplication()
        {
            StartCoroutine(TestBasicEffectApplicationCoroutine());
        }

        [ContextMenu("Test Stacking Rules")]
        public void TestStackingRules()
        {
            StartCoroutine(TestStackingRulesCoroutine());
        }

        [ContextMenu("Test Performance")]
        public void TestPerformance()
        {
            StartCoroutine(TestPerformanceCoroutine());
        }

        [ContextMenu("Clear All Effects")]
        public void ClearAllEffects()
        {
            if (_effectSystem != null)
            {
                _effectSystem.ClearAllEffects();
                Log("All effects cleared");
            }
        }

        [ContextMenu("Show System Stats")]
        public void ShowSystemStats()
        {
            if (_effectSystem != null)
            {
                Debug.Log(_effectSystem.GetSystemStats());
            }
        }

        #endregion

        #region Test Implementations

        private IEnumerator RunAllTests()
        {
            Log("=== Starting ItemEffectSystem Tests ===");

            yield return TestBasicEffectApplicationCoroutine();
            yield return new WaitForSeconds(1f);

            yield return TestStackingRulesCoroutine();
            yield return new WaitForSeconds(1f);

            yield return TestTimerSystemCoroutine();
            yield return new WaitForSeconds(1f);

            yield return TestEventSystemCoroutine();
            yield return new WaitForSeconds(1f);

            if (_runPerformanceTest)
            {
                yield return TestPerformanceCoroutine();
                yield return new WaitForSeconds(1f);
            }

            Log($"=== Tests Complete: {_testsPassed} passed, {_testsFailed} failed ===");
            _testCoroutine = null;
        }

        private IEnumerator TestBasicEffectApplicationCoroutine()
        {
            Log("--- Testing Basic Effect Application ---");

            // Test applying single effects
            _effectSystem.ApplyItemEffect(_testMagnetItem);
            Assert(_effectSystem.HasMagnetEffect, "Magnet effect should be active");
            Assert(_effectSystem.CurrentMagnetRadius > 0f, "Magnet radius should be > 0");

            yield return new WaitForSeconds(0.5f);

            _effectSystem.ApplyItemEffect(_testMultiplierItem);
            Assert(_effectSystem.HasMultiplierEffect, "Multiplier effect should be active");
            Assert(_effectSystem.CurrentScoreMultiplier > 1f, "Score multiplier should be > 1");

            yield return new WaitForSeconds(0.5f);

            _effectSystem.ApplyItemEffect(_testInvisibleItem);
            Assert(_effectSystem.HasInvisibilityEffect, "Invisibility effect should be active");

            yield return new WaitForSeconds(0.5f);

            _effectSystem.ApplyItemEffect(_testLifeItem);
            Assert(_effectSystem.CurrentExtraLives > 0, "Should have extra lives");

            Log("Basic effect application tests completed");
        }

        private IEnumerator TestStackingRulesCoroutine()
        {
            Log("--- Testing Stacking Rules ---");

            _effectSystem.ClearAllEffects();
            yield return new WaitForSeconds(0.1f);

            // Test Additive Stacking (Magnet)
            _effectSystem.ApplyItemEffect(_testMagnetItem);
            var firstEffect = _effectSystem.GetActiveEffect(_testMagnetItem.ItemId);
            float initialDuration = firstEffect.RemainingTime;

            yield return new WaitForSeconds(0.5f);

            _effectSystem.ApplyItemEffect(_testMagnetItem); // Stack again
            var stackedEffect = _effectSystem.GetActiveEffect(_testMagnetItem.ItemId);
            
            Assert(stackedEffect.StackCount == 2, "Stack count should be 2");
            Assert(stackedEffect.RemainingTime > initialDuration, "Duration should be extended");

            // Test Multiplicative Stacking (Multiplier)
            _effectSystem.ClearAllEffects();
            yield return new WaitForSeconds(0.1f);

            _effectSystem.ApplyItemEffect(_testMultiplierItem);
            float baseValue = _effectSystem.CurrentScoreMultiplier;

            _effectSystem.ApplyItemEffect(_testMultiplierItem); // Stack again
            float stackedValue = _effectSystem.CurrentScoreMultiplier;

            Assert(stackedValue > baseValue, "Multiplier value should increase when stacked");

            Log("Stacking rules tests completed");
        }

        private IEnumerator TestTimerSystemCoroutine()
        {
            Log("--- Testing Timer System ---");

            _effectSystem.ClearAllEffects();
            yield return new WaitForSeconds(0.1f);

            // Apply short duration effect - using default values (Duration=10s by default)
            var shortDurationItem = ItemDefinition.CreateDefault(ItemType.Magnet);
            Log($"Testing with default magnet duration: {shortDurationItem.Duration}s");
            
            _effectSystem.ApplyItemEffect(shortDurationItem);
            Assert(_effectSystem.HasMagnetEffect, "Effect should be active initially");

            // Since we can't set custom duration, we'll test that effect is still active after short time
            yield return new WaitForSeconds(1.0f);
            Assert(_effectSystem.HasMagnetEffect, "Effect should still be active after 1s with default duration");

            Log("Timer system tests completed");
        }

        private IEnumerator TestEventSystemCoroutine()
        {
            Log("--- Testing Event System ---");

            bool effectStartedCalled = false;
            bool effectEndedCalled = false;

            // Subscribe to events
            ItemEffectEvents.OnEffectStarted.AddListener(effect => {
                effectStartedCalled = true;
                Log($"Event: Effect started - {effect.ItemDefinition.DisplayName}");
            });

            ItemEffectEvents.OnEffectEnded.AddListener((type, id) => {
                effectEndedCalled = true;
                Log($"Event: Effect ended - {type}");
            });

            _effectSystem.ClearAllEffects();
            yield return new WaitForSeconds(0.1f);

            // Apply effect with default duration
            var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
            Log($"Testing events with default magnet duration: {testItem.Duration}s");

            _effectSystem.ApplyItemEffect(testItem);
            yield return new WaitForSeconds(0.1f);

            Assert(effectStartedCalled, "OnEffectStarted should be called");

            // Wait for expiration
            yield return new WaitForSeconds(0.6f);
            Assert(effectEndedCalled, "OnEffectEnded should be called");

            Log("Event system tests completed");
        }

        private IEnumerator TestPerformanceCoroutine()
        {
            Log($"--- Testing Performance ({_performanceTestCount} effects) ---");

            _effectSystem.ClearAllEffects();
            yield return new WaitForSeconds(0.1f);

            float startTime = Time.realtimeSinceStartup;

            // Apply many effects rapidly
            // Note: We can't modify ItemDefinition properties as they are read-only
            // Using default magnet items with unique IDs for performance testing
            for (int i = 0; i < _performanceTestCount; i++)
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                // Note: ID is also read-only, so each CreateDefault will have same ID
                // This means only one effect will be active (subsequent ones will stack or replace)
                
                _effectSystem.ApplyItemEffect(testItem);
                
                // Yield occasionally to prevent frame drops
                if (i % 10 == 0)
                {
                    yield return null;
                }
            }

            float applicationTime = Time.realtimeSinceStartup - startTime;

            // Clear all effects
            startTime = Time.realtimeSinceStartup;
            _effectSystem.ClearAllEffects();
            float clearTime = Time.realtimeSinceStartup - startTime;

            Log($"Performance Results:");
            Log($"- Applied {_performanceTestCount} effects in {applicationTime:F3}s ({_performanceTestCount / applicationTime:F1} effects/sec)");
            Log($"- Cleared all effects in {clearTime:F3}s");

            Log("Performance tests completed");
        }

        #endregion

        #region Test Utilities

        private void Assert(bool condition, string message)
        {
            if (condition)
            {
                _testsPassed++;
                Log($"✓ PASS: {message}");
            }
            else
            {
                _testsFailed++;
                LogError($"✗ FAIL: {message}");
            }
        }

        private void Log(string message)
        {
            if (_enableDetailedLogs)
                Debug.Log($"[ItemEffectSystemTester] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[ItemEffectSystemTester] {message}");
        }

        #endregion

        #region Individual Item Tests

        [ContextMenu("Apply Test Magnet")]
        public void ApplyTestMagnet()
        {
            _effectSystem.ApplyItemEffect(_testMagnetItem);
            Log($"Applied magnet effect - Radius: {_effectSystem.CurrentMagnetRadius}");
        }

        [ContextMenu("Apply Test Multiplier")]
        public void ApplyTestMultiplier()
        {
            _effectSystem.ApplyItemEffect(_testMultiplierItem);
            Log($"Applied multiplier effect - Value: {_effectSystem.CurrentScoreMultiplier}x");
        }

        [ContextMenu("Apply Test Invisible")]
        public void ApplyTestInvisible()
        {
            _effectSystem.ApplyItemEffect(_testInvisibleItem);
            Log($"Applied invisibility effect");
        }

        [ContextMenu("Apply Test Life")]
        public void ApplyTestLife()
        {
            _effectSystem.ApplyItemEffect(_testLifeItem);
            Log($"Applied life effect - Extra lives: {_effectSystem.CurrentExtraLives}");
        }

        #endregion
    }
}
