using UnityEngine;
using EndlessRunner.Data;
using EndlessRunner.Items;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Quick validation test to ensure ItemEffectSystem compiles and works correctly
    /// Attach this to a GameObject and check Context Menu for test options
    /// </summary>
    public class ValidationTest : MonoBehaviour
    {
        [Header("Validation Status")]
        [SerializeField] private bool _compilationOK = true;
        [SerializeField] private bool _systemInitialized = false;
        [SerializeField] private bool _eventsWorking = false;
        
        private ItemEffectSystem _effectSystem;

        private void Start()
        {
            ValidateCompilation();
        }

        [ContextMenu("Run Full Validation")]
        public void RunFullValidation()
        {
            Debug.Log("=== ItemEffectSystem Validation ===");
            
            // Test 1: Compilation
            _compilationOK = ValidateCompilation();
            LogResult("Compilation", _compilationOK);

            // Test 2: System initialization
            _systemInitialized = ValidateSystemInitialization();
            LogResult("System Initialization", _systemInitialized);

            // Test 3: Events
            _eventsWorking = ValidateEventSystem();
            LogResult("Event System", _eventsWorking);

            // Test 4: Basic functionality
            bool basicFunctionsWork = ValidateBasicFunctions();
            LogResult("Basic Functions", basicFunctionsWork);

            // Summary
            bool allPassed = _compilationOK && _systemInitialized && _eventsWorking && basicFunctionsWork;
            Debug.Log($"=== Validation Complete: {(allPassed ? "✅ ALL PASSED" : "❌ SOME FAILED")} ===");
        }

        private bool ValidateCompilation()
        {
            try
            {
                // Test ItemDefinition creation
                var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                if (testItem == null) return false;

                // Test ItemStackingRule enum
                var stackingRule = ItemStackingRule.Add;
                if (stackingRule != ItemStackingRule.Add) return false;

                // Test ActiveItemEffect creation
                var activeEffect = new ActiveItemEffect(testItem);
                if (activeEffect == null) return false;

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Compilation validation failed: {e.Message}");
                return false;
            }
        }

        private bool ValidateSystemInitialization()
        {
            try
            {
                _effectSystem = ItemEffectSystem.Instance;
                if (_effectSystem == null) return false;

                // Test basic properties
                int count = _effectSystem.ActiveEffectCount;
                bool hasEffects = _effectSystem.HasActiveEffects;
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"System initialization validation failed: {e.Message}");
                return false;
            }
        }

        private bool ValidateEventSystem()
        {
            try
            {
                bool eventFired = false;

                // Subscribe to event
                ItemEffectEvents.OnEffectStarted.AddListener((effect) => {
                    eventFired = true;
                });

                // Apply test effect - use default values since properties are read-only
                var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                _effectSystem.ApplyItemEffect(testItem);

                // Check if event fired
                return eventFired;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Event system validation failed: {e.Message}");
                return false;
            }
        }

        private bool ValidateBasicFunctions()
        {
            try
            {
                // Test effect application - use default values since properties are read-only
                var magnetItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                _effectSystem.ApplyItemEffect(magnetItem);

                // Test querying values
                bool hasMagnet = _effectSystem.HasMagnetEffect;
                float magnetRadius = _effectSystem.CurrentMagnetRadius;

                if (!hasMagnet || magnetRadius <= 0) return false;

                // Test multiplier
                var multiplierItem = ItemDefinition.CreateDefault(ItemType.Multiplier);
                _effectSystem.ApplyItemEffect(multiplierItem);

                float multiplier = _effectSystem.CurrentScoreMultiplier;
                if (multiplier < 1f) return false; // Default multiplier should be >= 1

                // Test clearing
                _effectSystem.ClearAllEffects();
                if (_effectSystem.HasActiveEffects) return false;

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Basic functions validation failed: {e.Message}");
                return false;
            }
        }

        private void LogResult(string testName, bool passed)
        {
            string status = passed ? "✅ PASS" : "❌ FAIL";
            Debug.Log($"[Validation] {testName}: {status}");
        }

        [ContextMenu("Test Single Effect")]
        public void TestSingleEffect()
        {
            if (_effectSystem == null)
                _effectSystem = ItemEffectSystem.Instance;

            // Use default values since ItemDefinition properties are read-only
            var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);

            _effectSystem.ApplyItemEffect(testItem);
            Debug.Log($"Applied magnet effect - Has magnet: {_effectSystem.HasMagnetEffect}, Radius: {_effectSystem.CurrentMagnetRadius}");
            Debug.Log($"Default Duration: {testItem.Duration}s, Default Effect Value: {testItem.EffectValue}");
        }

        [ContextMenu("Test Stacking")]
        public void TestStacking()
        {
            if (_effectSystem == null)
                _effectSystem = ItemEffectSystem.Instance;

            // Note: ItemDefinition properties are read-only, so we test with default values
            var stackableItem = ItemDefinition.CreateDefault(ItemType.Multiplier);
            Debug.Log($"Testing stacking with default multiplier item (CanStack: {stackableItem.CanStack}, Rule: {stackableItem.StackingRule}, Value: {stackableItem.EffectValue})");

            // Apply first
            _effectSystem.ApplyItemEffect(stackableItem);
            float firstValue = _effectSystem.CurrentScoreMultiplier;

            // Apply second (may or may not stack depending on default settings)
            _effectSystem.ApplyItemEffect(stackableItem);
            float secondValue = _effectSystem.CurrentScoreMultiplier;

            Debug.Log($"Stacking test - First: {firstValue}x, Second: {secondValue}x, Stacked: {secondValue != firstValue}");
            
            if (stackableItem.CanStack)
            {
                Debug.Log($"Item supports stacking with rule: {stackableItem.StackingRule}");
            }
            else
            {
                Debug.Log("Item does not support stacking (expected same value)");
            }
        }

        [ContextMenu("Show System Info")]
        public void ShowSystemInfo()
        {
            if (_effectSystem == null)
                _effectSystem = ItemEffectSystem.Instance;

            Debug.Log(_effectSystem.GetSystemStats());
        }
    }
}
