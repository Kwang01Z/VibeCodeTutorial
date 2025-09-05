using UnityEngine;
using UnityEditor;
using EndlessRunner.Core;
using EndlessRunner.Testing;
using EndlessRunner.Gameplay;

/// <summary>
/// Simple compile test để verify các classes tồn tại và accessible
/// </summary>
public static class SimpleCompileTest
{
    [MenuItem("EndlessRunner/Debug/Simple Compile Test")]
    public static void RunSimpleCompileTest()
    {
        Debug.Log("=== SIMPLE COMPILE TEST ===");
        
        try
        {
            // Test instantiation của các key classes
            Debug.Log("Testing class accessibility...");
            
            // Core namespace
            var animationType = typeof(TestPickupAnimation);
            Debug.Log($"✅ {animationType.FullName}");
            
            // Testing namespace  
            var managerType = typeof(RunnerIntegrationTestManager);
            Debug.Log($"✅ {managerType.FullName}");
            
            // Gameplay namespace
            var controllerType = typeof(RunnerController);
            Debug.Log($"✅ {controllerType.FullName}");
            
            var detectorType = typeof(CollisionDetector);
            Debug.Log($"✅ {detectorType.FullName}");
            
            var healthType = typeof(HealthComponent);
            Debug.Log($"✅ {healthType.FullName}");
            
            // Test enums
            var stateType = typeof(RunnerState);
            Debug.Log($"✅ {stateType.FullName}");
            
            var actionType = typeof(RunnerAction);
            Debug.Log($"✅ {actionType.FullName}");
            
            Debug.Log("<color=green>🎉 ALL CLASSES COMPILE SUCCESSFULLY!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Compile test failed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
        
        Debug.Log("=== END COMPILE TEST ===");
    }
    
    [MenuItem("EndlessRunner/Debug/Test Component Creation")]
    public static void TestComponentCreation()
    {
        Debug.Log("=== COMPONENT CREATION TEST ===");
        
        try
        {
            // Tạo temporary GameObject để test component creation
            var testGO = new GameObject("CompileTestObject");
            
            // Test add components
            var animation = testGO.AddComponent<TestPickupAnimation>();
            Debug.Log($"✅ Added {animation.GetType().Name}");
            
            var manager = testGO.AddComponent<RunnerIntegrationTestManager>();
            Debug.Log($"✅ Added {manager.GetType().Name}");
            
            var controller = testGO.AddComponent<RunnerController>();
            Debug.Log($"✅ Added {controller.GetType().Name}");
            
            var detector = testGO.AddComponent<CollisionDetector>();
            Debug.Log($"✅ Added {detector.GetType().Name}");
            
            var health = testGO.AddComponent<HealthComponent>();
            Debug.Log($"✅ Added {health.GetType().Name}");
            
            Debug.Log("<color=green>🎉 ALL COMPONENTS CREATED SUCCESSFULLY!</color>");
            
            // Cleanup
            Object.DestroyImmediate(testGO);
            Debug.Log("✅ Test object cleaned up");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Component creation test failed: {ex.Message}");
        }
        
        Debug.Log("=== END COMPONENT TEST ===");
    }
}
