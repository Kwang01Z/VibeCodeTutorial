using UnityEngine;
using System.Collections;
using EndlessRunner.Currency.UI;

namespace EndlessRunner.Currency
{
    /// <summary>
    /// Component để test và validate CurrencyManager trong Phase 3
    /// Bao gồm test save/load, UI updates, performance testing, và batch operations
    /// </summary>
    public class CurrencyManagerTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField]
        [Tooltip("CurrencyManager để test")]
        private CurrencyManager _currencyManager;

        [SerializeField]
        [Tooltip("Test UI displays")]
        private CurrencyDisplayUI[] _currencyDisplays;

        [SerializeField]
        [Tooltip("Enable automatic testing on start")]
        private bool _runAutomaticTests = false;

        [SerializeField]
        [Tooltip("Test interval for automatic tests")]
        private float _testInterval = 2f;

        [Header("Test Data")]
        [SerializeField]
        [Tooltip("Amount to add in tests")]
        private long _testAddAmount = 1000;

        [SerializeField]
        [Tooltip("Amount to spend in tests")]
        private long _testSpendAmount = 500;

        [SerializeField]
        [Tooltip("Number of transactions for batch test")]
        private int _batchTestSize = 10;

        // Private fields
        private ICurrencySystem _currencySystem;
        private bool _isRunningTests = false;
        private int _testCounter = 0;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_currencyManager == null)
                _currencyManager = FindObjectOfType<CurrencyManager>();
        }

        private void Start()
        {
            InitializeTester();
            
            if (_runAutomaticTests)
            {
                StartCoroutine(RunAutomaticTests());
            }
        }

        #endregion

        #region Initialization

        private void InitializeTester()
        {
            _currencySystem = _currencyManager;

            if (_currencySystem != null)
            {
                // Subscribe to events for testing
                _currencySystem.OnCurrencyChanged += OnCurrencyChanged;
                _currencySystem.OnTransactionProcessed += OnTransactionProcessed;
                _currencySystem.OnBatchTransactionProcessed += OnBatchTransactionProcessed;

                Debug.Log("[CurrencyManagerTester] Initialized and subscribed to events");
            }
            else
            {
                Debug.LogError("[CurrencyManagerTester] No CurrencyManager found!", this);
            }
        }

        #endregion

        #region Event Handlers

        private void OnCurrencyChanged(CurrencyType type, long oldAmount, long newAmount, string reason)
        {
            Debug.Log($"[CurrencyTester] Currency Changed: {type} {oldAmount} -> {newAmount} ({reason})");
        }

        private void OnTransactionProcessed(CurrencyTransaction transaction, bool success)
        {
            Debug.Log($"[CurrencyTester] Transaction: {transaction.type} {transaction.amount} - {(success ? "SUCCESS" : "FAILED")}");
        }

        private void OnBatchTransactionProcessed(CurrencyTransaction[] transactions, bool allSuccess)
        {
            Debug.Log($"[CurrencyTester] Batch Transaction: {transactions.Length} transactions - {(allSuccess ? "ALL SUCCESS" : "SOME FAILED")}");
        }

        #endregion

        #region Automatic Tests

        private IEnumerator RunAutomaticTests()
        {
            _isRunningTests = true;
            Debug.Log("[CurrencyManagerTester] Starting automatic tests...");

            while (_isRunningTests)
            {
                yield return new WaitForSeconds(_testInterval);

                if (_currencySystem == null) break;

                _testCounter++;

                switch (_testCounter % 6)
                {
                    case 1:
                        TestAddCurrency();
                        break;
                    case 2:
                        TestSpendCurrency();
                        break;
                    case 3:
                        TestBatchTransactions();
                        break;
                    case 4:
                        TestSaveLoad();
                        break;
                    case 5:
                        TestValidation();
                        break;
                    case 0:
                        TestPerformance();
                        break;
                }
            }
        }

        #endregion

        #region Test Methods

        [ContextMenu("Test Add Currency")]
        public void TestAddCurrency()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Add Currency ===");

            foreach (var currencyType in _currencySystem.AvailableCurrencies)
            {
                long beforeAmount = _currencySystem.GetAmount(currencyType);
                bool success = _currencySystem.AddCurrency(currencyType, _testAddAmount, "Test Add");
                long afterAmount = _currencySystem.GetAmount(currencyType);

                Debug.Log($"{currencyType}: {beforeAmount} -> {afterAmount} (success: {success})");
            }
        }

        [ContextMenu("Test Spend Currency")]
        public void TestSpendCurrency()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Spend Currency ===");

            foreach (var currencyType in _currencySystem.AvailableCurrencies)
            {
                long beforeAmount = _currencySystem.GetAmount(currencyType);
                bool hasEnough = _currencySystem.HasEnough(currencyType, _testSpendAmount);
                bool success = _currencySystem.SpendCurrency(currencyType, _testSpendAmount, "Test Spend");
                long afterAmount = _currencySystem.GetAmount(currencyType);

                Debug.Log($"{currencyType}: {beforeAmount} -> {afterAmount} (hasEnough: {hasEnough}, success: {success})");
            }
        }

        [ContextMenu("Test Batch Transactions")]
        public void TestBatchTransactions()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Batch Transactions ===");

            var transactions = new CurrencyTransaction[_batchTestSize];
            var availableCurrencies = _currencySystem.AvailableCurrencies;

            for (int i = 0; i < _batchTestSize; i++)
            {
                var currencyType = availableCurrencies[i % availableCurrencies.Count];
                long amount = (i % 2 == 0) ? _testAddAmount : -_testSpendAmount;
                transactions[i] = new CurrencyTransaction(currencyType, amount, $"Batch Test {i}");
            }

            bool success = _currencySystem.ProcessTransactions(transactions);
            Debug.Log($"Batch transaction result: {success}");
        }

        [ContextMenu("Test Save/Load")]
        public void TestSaveLoad()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Save/Load ===");

            // Record current amounts
            var beforeAmounts = new System.Collections.Generic.Dictionary<CurrencyType, long>();
            foreach (var currencyType in _currencySystem.AvailableCurrencies)
            {
                beforeAmounts[currencyType] = _currencySystem.GetAmount(currencyType);
            }

            // Save data
            _currencySystem.SaveData();
            Debug.Log("Data saved");

            // Modify values
            _currencySystem.AddCurrency(CurrencyType.Coins, 12345, "Test modification");

            // Load data (should restore original values)
            _currencySystem.LoadData();
            Debug.Log("Data loaded");

            // Verify values
            foreach (var kvp in beforeAmounts)
            {
                long currentAmount = _currencySystem.GetAmount(kvp.Key);
                bool matches = currentAmount == kvp.Value;
                Debug.Log($"{kvp.Key}: Expected {kvp.Value}, Got {currentAmount} - {(matches ? "PASS" : "FAIL")}");
            }
        }

        [ContextMenu("Test Validation")]
        public void TestValidation()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Validation ===");

            // Test invalid currency type
            Debug.Log("Testing invalid currency operations:");

            // Test negative amounts
            bool result1 = _currencySystem.AddCurrency(CurrencyType.Coins, -100, "Negative add test");
            Debug.Log($"Add negative amount: {result1} (should be false)");

            bool result2 = _currencySystem.SpendCurrency(CurrencyType.Coins, -100, "Negative spend test");
            Debug.Log($"Spend negative amount: {result2} (should be false)");

            // Test spending more than available
            long currentCoins = _currencySystem.GetAmount(CurrencyType.Coins);
            long largeAmount = currentCoins + 10000;
            bool hasEnough = _currencySystem.HasEnough(CurrencyType.Coins, largeAmount);
            bool spendResult = _currencySystem.SpendCurrency(CurrencyType.Coins, largeAmount, "Over-spend test");
            
            Debug.Log($"Has enough for {largeAmount} coins: {hasEnough} (should be false)");
            Debug.Log($"Spend more than available: {spendResult} (should be false)");
        }

        [ContextMenu("Test Performance")]
        public void TestPerformance()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Testing Performance ===");

            int operationCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Test rapid additions
            for (int i = 0; i < operationCount; i++)
            {
                _currencySystem.AddCurrency(CurrencyType.Coins, 1, "Performance test");
            }

            stopwatch.Stop();
            float addTime = (float)stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();

            // Test rapid queries
            for (int i = 0; i < operationCount; i++)
            {
                long amount = _currencySystem.GetAmount(CurrencyType.Coins);
            }

            stopwatch.Stop();
            float queryTime = (float)stopwatch.ElapsedMilliseconds;

            Debug.Log($"Performance Test Results:");
            Debug.Log($"- {operationCount} Add operations: {addTime:F1}ms ({addTime / operationCount:F3}ms per operation)");
            Debug.Log($"- {operationCount} Query operations: {queryTime:F1}ms ({queryTime / operationCount:F3}ms per operation)");

            if (addTime > 100f)
            {
                Debug.LogWarning("Add operations are slow! Consider optimization.");
            }

            if (queryTime > 50f)
            {
                Debug.LogWarning("Query operations are slow! Consider optimization.");
            }
        }

        [ContextMenu("Test UI Updates")]
        public void TestUIUpdates()
        {
            if (_currencyDisplays == null || _currencyDisplays.Length == 0)
            {
                Debug.LogWarning("[CurrencyManagerTester] No UI displays assigned for testing");
                return;
            }

            Debug.Log("=== Testing UI Updates ===");

            StartCoroutine(TestUIUpdatesCoroutine());
        }

        private IEnumerator TestUIUpdatesCoroutine()
        {
            foreach (var display in _currencyDisplays)
            {
                if (display == null) continue;

                Debug.Log($"Testing UI updates for currency display");

                // Test incremental updates
                for (int i = 0; i < 5; i++)
                {
                    _currencySystem.AddCurrency(CurrencyType.Coins, 100, "UI Test");
                    yield return new WaitForSeconds(0.5f);
                }

                yield return new WaitForSeconds(1f);

                // Test large update
                _currencySystem.AddCurrency(CurrencyType.Coins, 5000, "UI Large Test");
                yield return new WaitForSeconds(2f);
            }

            Debug.Log("UI update test completed");
        }

        #endregion

        #region Utility Methods

        [ContextMenu("Reset All Currency")]
        public void ResetAllCurrency()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Resetting All Currency ===");
            _currencySystem.ResetCurrencies();
        }

        [ContextMenu("Add Test Currency")]
        public void AddTestCurrency()
        {
            if (_currencySystem == null) return;

            _currencySystem.AddCurrency(CurrencyType.Coins, 10000, "Test");
            _currencySystem.AddCurrency(CurrencyType.Gems, 500, "Test");
            _currencySystem.AddCurrency(CurrencyType.Keys, 25, "Test");
        }

        [ContextMenu("Debug Currency State")]
        public void DebugCurrencyState()
        {
            if (_currencySystem == null) return;

            Debug.Log("=== Currency State Debug ===");
            Debug.Log($"System Initialized: {_currencySystem.IsInitialized}");
            Debug.Log($"Available Currencies: {_currencySystem.AvailableCurrencies.Count}");

            foreach (var currencyType in _currencySystem.AvailableCurrencies)
            {
                long amount = _currencySystem.GetAmount(currencyType);
                var config = _currencySystem.GetConfig(currencyType);
                string formatted = CurrencyFormatter.FormatAmount(amount);
                
                Debug.Log($"- {currencyType}: {formatted} / {CurrencyFormatter.FormatAmount(config.MaxAmount)}");
            }
        }

        [ContextMenu("Stop Automatic Tests")]
        public void StopAutomaticTests()
        {
            _isRunningTests = false;
            Debug.Log("[CurrencyManagerTester] Stopped automatic tests");
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            _isRunningTests = false;

            if (_currencySystem != null)
            {
                _currencySystem.OnCurrencyChanged -= OnCurrencyChanged;
                _currencySystem.OnTransactionProcessed -= OnTransactionProcessed;
                _currencySystem.OnBatchTransactionProcessed -= OnBatchTransactionProcessed;
            }
        }

        #endregion
    }
}
