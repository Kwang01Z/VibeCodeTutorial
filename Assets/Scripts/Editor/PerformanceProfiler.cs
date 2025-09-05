using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EndlessRunner.Gameplay;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Performance profiling utility cho Phase 1.7 Health & Collision System.
    /// Tests GC allocations, CPU performance, và memory usage với collision spam.
    /// </summary>
    public class PerformanceProfiler : EditorWindow
    {
        #region Performance Targets
        
        // Performance targets cho Phase 1.7
        private const float TARGET_MAX_CPU_MS_PER_HIT = 0.3f;     // ≤0.3ms per collision
        private const int TARGET_MAX_GC_ALLOC_BYTES = 0;         // 0B GC allocation per collision
        private const int TARGET_MAX_MEMORY_KB = 50;             // ≤50KB total memory increase
        private const int COLLISION_SPAM_COUNT = 100;            // Number of collisions for stress test
        
        #endregion
        
        #region UI State
        
        private Vector2 _scrollPosition;
        private bool _isRunning = false;
        private string _lastResults = "";
        private PerformanceTestResults _performanceResults;
        
        #endregion
        
        #region Performance Test Results
        
        [System.Serializable]
        public class PerformanceTestResults
        {
            public float averageCollisionTimeMs;
            public float maxCollisionTimeMs;
            public long totalGCAllocations;
            public long memoryIncreaseKB;
            public int successfulCollisions;
            public int totalCollisions;
            public bool passedCPUTest;
            public bool passedMemoryTest;
            public bool passedGCTest;
            public string detailedReport;
        }
        
        #endregion
        
        #region Menu Items
        
        [MenuItem("EndlessRunner/Phase 1.7/Performance Profiler")]
        public static void OpenProfiler()
        {
            var window = GetWindow<PerformanceProfiler>("Performance Profiler");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        [MenuItem("EndlessRunner/Phase 1.7/Quick Performance Test")]
        public static void QuickPerformanceTest()
        {
            if (Application.isPlaying)
            {
                Debug.Log("[PerformanceProfiler] Starting quick performance test...");
                // This would trigger a quick test if we were in play mode
            }
            else
            {
                Debug.LogWarning("[PerformanceProfiler] Performance tests require Play Mode. Please enter Play Mode first.");
            }
        }
        
        #endregion
        
        #region Editor Window UI
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUILayout.Label("Phase 1.7 - Performance Profiler", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Performance Targets Display
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Performance Targets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"• CPU per collision: ≤{TARGET_MAX_CPU_MS_PER_HIT}ms");
            EditorGUILayout.LabelField($"• GC allocation: {TARGET_MAX_GC_ALLOC_BYTES}B per collision");
            EditorGUILayout.LabelField($"• Memory increase: ≤{TARGET_MAX_MEMORY_KB}KB total");
            EditorGUILayout.LabelField($"• Collision spam count: {COLLISION_SPAM_COUNT} hits");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Test Controls
            GUI.enabled = Application.isPlaying && !_isRunning;
            if (GUILayout.Button("Run Performance Test", GUILayout.Height(30)))
            {
                StartPerformanceTest();
            }
            
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Run Memory Profiling", GUILayout.Height(25)))
            {
                RunMemoryProfile();
            }
            
            GUI.enabled = true;
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Performance tests require Play Mode. Please enter Play Mode to run tests.", MessageType.Warning);
            }
            
            if (_isRunning)
            {
                EditorGUILayout.HelpBox("Performance test running... Please wait.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Results Display
            if (_performanceResults != null)
            {
                DisplayPerformanceResults();
            }
            
            // Detailed Results
            if (!string.IsNullOrEmpty(_lastResults))
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("Detailed Results", EditorStyles.boldLabel);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
                EditorGUILayout.TextArea(_lastResults, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void DisplayPerformanceResults()
        {
            EditorGUILayout.Space(5);
            GUILayout.Label("Performance Test Results", EditorStyles.boldLabel);
            
            // Overall Status
            var overallPassed = _performanceResults.passedCPUTest && 
                               _performanceResults.passedMemoryTest && 
                               _performanceResults.passedGCTest;
            
            var statusColor = overallPassed ? Color.green : Color.red;
            var statusText = overallPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED";
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = originalColor;
            
            EditorGUILayout.Space(5);
            
            // Individual Results
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // CPU Performance
            var cpuColor = _performanceResults.passedCPUTest ? Color.green : Color.red;
            var cpuIcon = _performanceResults.passedCPUTest ? "✅" : "❌";
            
            GUI.color = cpuColor;
            EditorGUILayout.LabelField($"{cpuIcon} CPU Performance");
            GUI.color = originalColor;
            EditorGUILayout.LabelField($"   Average: {_performanceResults.averageCollisionTimeMs:F3}ms per collision");
            EditorGUILayout.LabelField($"   Max: {_performanceResults.maxCollisionTimeMs:F3}ms");
            EditorGUILayout.LabelField($"   Target: ≤{TARGET_MAX_CPU_MS_PER_HIT}ms");
            
            EditorGUILayout.Space(5);
            
            // GC Allocation
            var gcColor = _performanceResults.passedGCTest ? Color.green : Color.red;
            var gcIcon = _performanceResults.passedGCTest ? "✅" : "❌";
            
            GUI.color = gcColor;
            EditorGUILayout.LabelField($"{gcIcon} GC Allocation");
            GUI.color = originalColor;
            EditorGUILayout.LabelField($"   Total: {_performanceResults.totalGCAllocations}B");
            EditorGUILayout.LabelField($"   Per collision: {_performanceResults.totalGCAllocations / (float)_performanceResults.totalCollisions:F1}B");
            EditorGUILayout.LabelField($"   Target: {TARGET_MAX_GC_ALLOC_BYTES}B per collision");
            
            EditorGUILayout.Space(5);
            
            // Memory Usage
            var memColor = _performanceResults.passedMemoryTest ? Color.green : Color.red;
            var memIcon = _performanceResults.passedMemoryTest ? "✅" : "❌";
            
            GUI.color = memColor;
            EditorGUILayout.LabelField($"{memIcon} Memory Usage");
            GUI.color = originalColor;
            EditorGUILayout.LabelField($"   Increase: {_performanceResults.memoryIncreaseKB}KB");
            EditorGUILayout.LabelField($"   Target: ≤{TARGET_MAX_MEMORY_KB}KB");
            
            EditorGUILayout.Space(5);
            
            // Collision Success Rate
            var successRate = (_performanceResults.successfulCollisions / (float)_performanceResults.totalCollisions) * 100f;
            EditorGUILayout.LabelField($"📊 Collision Success Rate");
            EditorGUILayout.LabelField($"   {_performanceResults.successfulCollisions}/{_performanceResults.totalCollisions} ({successRate:F1}%)");
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Performance Testing
        
        private void StartPerformanceTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PerformanceProfiler] Cannot run performance test outside of Play Mode");
                return;
            }
            
            _isRunning = true;
            _lastResults = "";
            
            // Start coroutine for performance testing
            var testRunner = new GameObject("PerformanceTestRunner");
            var runner = testRunner.AddComponent<PerformanceTestRunner>();
            runner.StartTest(COLLISION_SPAM_COUNT, OnPerformanceTestComplete);
            
            Debug.Log($"[PerformanceProfiler] Starting performance test with {COLLISION_SPAM_COUNT} collision operations...");
        }
        
        private void OnPerformanceTestComplete(PerformanceTestResults results)
        {
            _isRunning = false;
            _performanceResults = results;
            
            // Generate detailed report
            var report = GenerateDetailedReport(results);
            _lastResults = report;
            
            // Log results
            Debug.Log($"[PerformanceProfiler] Performance test completed:\n{report}");
            
            // Save results to file
            SaveResultsToFile(results, report);
            
            Repaint();
        }
        
        private string GenerateDetailedReport(PerformanceTestResults results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PHASE 1.7 PERFORMANCE TEST RESULTS ===");
            sb.AppendLine($"Test Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine($"Platform: {Application.platform}");
            sb.AppendLine();
            
            sb.AppendLine("PERFORMANCE TARGETS:");
            sb.AppendLine($"• CPU per collision: ≤{TARGET_MAX_CPU_MS_PER_HIT}ms");
            sb.AppendLine($"• GC allocation: {TARGET_MAX_GC_ALLOC_BYTES}B per collision");
            sb.AppendLine($"• Memory increase: ≤{TARGET_MAX_MEMORY_KB}KB total");
            sb.AppendLine();
            
            sb.AppendLine("TEST RESULTS:");
            sb.AppendLine($"✓ Total Collisions: {results.totalCollisions}");
            sb.AppendLine($"✓ Successful Collisions: {results.successfulCollisions}");
            sb.AppendLine($"✓ Success Rate: {(results.successfulCollisions / (float)results.totalCollisions) * 100f:F1}%");
            sb.AppendLine();
            
            sb.AppendLine("CPU PERFORMANCE:");
            sb.AppendLine($"• Average Time: {results.averageCollisionTimeMs:F3}ms per collision");
            sb.AppendLine($"• Maximum Time: {results.maxCollisionTimeMs:F3}ms");
            sb.AppendLine($"• Target: ≤{TARGET_MAX_CPU_MS_PER_HIT}ms");
            sb.AppendLine($"• Status: {(results.passedCPUTest ? "PASSED ✅" : "FAILED ❌")}");
            sb.AppendLine();
            
            sb.AppendLine("MEMORY ALLOCATION:");
            sb.AppendLine($"• Total GC Allocation: {results.totalGCAllocations}B");
            sb.AppendLine($"• Per Collision: {results.totalGCAllocations / (float)results.totalCollisions:F1}B");
            sb.AppendLine($"• Memory Increase: {results.memoryIncreaseKB}KB");
            sb.AppendLine($"• GC Test: {(results.passedGCTest ? "PASSED ✅" : "FAILED ❌")}");
            sb.AppendLine($"• Memory Test: {(results.passedMemoryTest ? "PASSED ✅" : "FAILED ❌")}");
            sb.AppendLine();
            
            var overallPassed = results.passedCPUTest && results.passedMemoryTest && results.passedGCTest;
            sb.AppendLine($"OVERALL STATUS: {(overallPassed ? "PASSED ✅" : "FAILED ❌")}");
            
            if (!overallPassed)
            {
                sb.AppendLine();
                sb.AppendLine("RECOMMENDATIONS:");
                if (!results.passedCPUTest)
                    sb.AppendLine("• Optimize collision detection logic để reduce CPU usage");
                if (!results.passedGCTest)
                    sb.AppendLine("• Eliminate GC allocations in collision hot path");
                if (!results.passedMemoryTest)
                    sb.AppendLine("• Review memory management và object pooling");
            }
            
            return sb.ToString();
        }
        
        private void SaveResultsToFile(PerformanceTestResults results, string report)
        {
            try
            {
                var fileName = $"PerformanceResults_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = System.IO.Path.Combine(Application.dataPath, "..", fileName);
                
                System.IO.File.WriteAllText(filePath, report);
                Debug.Log($"[PerformanceProfiler] Results saved to: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PerformanceProfiler] Failed to save results: {ex.Message}");
            }
        }
        
        private void RunMemoryProfile()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PerformanceProfiler] Memory profiling requires Play Mode");
                return;
            }
            
            // Trigger GC và get baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long initialMemory = System.GC.GetTotalMemory(false);
            
            Debug.Log($"[PerformanceProfiler] Memory Profile - Initial: {initialMemory / 1024}KB");
            Debug.Log("[PerformanceProfiler] Trigger some collisions và check memory again in a few seconds...");
        }
        
        #endregion
    }
    
    #region Performance Test Runner Component
    
    /// <summary>
    /// MonoBehaviour runner cho performance tests trong Play Mode
    /// </summary>
    public class PerformanceTestRunner : MonoBehaviour
    {
        private System.Action<PerformanceProfiler.PerformanceTestResults> _onComplete;
        private int _collisionCount;
        private List<float> _collisionTimes = new List<float>();
        private long _initialMemory;
        private long _initialGCMemory;
        
        public void StartTest(int collisionCount, System.Action<PerformanceProfiler.PerformanceTestResults> onComplete)
        {
            _collisionCount = collisionCount;
            _onComplete = onComplete;
            
            StartCoroutine(RunPerformanceTest());
        }
        
        private IEnumerator RunPerformanceTest()
        {
            // Setup test environment
            yield return SetupTestEnvironment();
            
            // Baseline memory measurement
            System.GC.Collect();
            yield return new WaitForEndOfFrame();
            _initialMemory = System.GC.GetTotalMemory(false);
            _initialGCMemory = _initialMemory;
            
            Debug.Log($"[PerformanceTest] Baseline memory: {_initialMemory / 1024}KB");
            
            // Create test player và obstacle
            var testPlayer = CreateTestPlayer();
            var testObstacle = CreateTestObstacle();
            
            var healthComponent = testPlayer.GetComponent<HealthComponent>();
            var stopwatch = new System.Diagnostics.Stopwatch();
            
            int successfulCollisions = 0;
            
            // Run collision spam test
            for (int i = 0; i < _collisionCount; i++)
            {
                // Reset player health nếu cần
                if (!healthComponent.IsAlive)
                {
                    DestroyImmediate(testPlayer);
                    testPlayer = CreateTestPlayer();
                    healthComponent = testPlayer.GetComponent<HealthComponent>();
                }
                
                // Wait for I-frames to clear
                while (healthComponent.IsInIFrames)
                {
                    yield return new WaitForFixedUpdate();
                }
                
                // Measure collision time
                stopwatch.Restart();
                
                // Trigger collision
                testPlayer.transform.position = testObstacle.transform.position;
                yield return new WaitForFixedUpdate();
                
                stopwatch.Stop();
                _collisionTimes.Add((float)stopwatch.Elapsed.TotalMilliseconds);
                
                if (healthComponent.CurrentHealth < 3) // Collision successful
                {
                    successfulCollisions++;
                }
                
                // Reset position
                testPlayer.transform.position = Vector3.zero;
                yield return new WaitForFixedUpdate();
                
                // Progress update
                if (i % 10 == 0)
                {
                    Debug.Log($"[PerformanceTest] Progress: {i + 1}/{_collisionCount} collisions");
                }
            }
            
            // Final memory measurement
            yield return new WaitForEndOfFrame();
            System.GC.Collect();
            yield return new WaitForEndOfFrame();
            long finalMemory = System.GC.GetTotalMemory(false);
            
            // Cleanup test objects
            if (testPlayer != null) DestroyImmediate(testPlayer);
            if (testObstacle != null) DestroyImmediate(testObstacle);
            
            // Calculate results
            var results = new PerformanceProfiler.PerformanceTestResults
            {
                totalCollisions = _collisionCount,
                successfulCollisions = successfulCollisions,
                averageCollisionTimeMs = _collisionTimes.Count > 0 ? _collisionTimes.Average() : 0f,
                maxCollisionTimeMs = _collisionTimes.Count > 0 ? _collisionTimes.Max() : 0f,
                totalGCAllocations = finalMemory - _initialMemory,
                memoryIncreaseKB = (finalMemory - _initialMemory) / 1024,
                passedCPUTest = _collisionTimes.Count > 0 ? _collisionTimes.Average() <= 0.3f : false,
                passedGCTest = (finalMemory - _initialMemory) <= 0, // Ideally 0 allocation
                passedMemoryTest = ((finalMemory - _initialMemory) / 1024) <= 50 // ≤50KB
            };
            
            _onComplete?.Invoke(results);
            
            // Cleanup runner
            DestroyImmediate(gameObject);
        }
        
        private IEnumerator SetupTestEnvironment()
        {
            // Ensure required layers exist
            if (LayerMask.NameToLayer("Player") == -1 || LayerMask.NameToLayer("Obstacle") == -1)
            {
                Debug.LogError("[PerformanceTest] Required layers not found. Run Layer Setup first.");
                yield break;
            }
            
            yield return new WaitForEndOfFrame();
        }
        
        private GameObject CreateTestPlayer()
        {
            var player = new GameObject("PerformanceTestPlayer");
            player.layer = LayerMask.NameToLayer("Player");
            
            // Add required components
            player.AddComponent<RunnerController>();
            player.AddComponent<HealthComponent>();
            player.AddComponent<CollisionDetector>();
            var rb = player.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            
            var collider = player.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            
            player.transform.position = Vector3.zero;
            return player;
        }
        
        private GameObject CreateTestObstacle()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "PerformanceTestObstacle";
            obstacle.layer = LayerMask.NameToLayer("Obstacle");
            obstacle.GetComponent<Collider>().isTrigger = true;
            obstacle.transform.position = new Vector3(0, 0, 1f);
            return obstacle;
        }
    }
    
    #endregion
}
