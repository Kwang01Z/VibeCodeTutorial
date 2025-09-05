using UnityEngine;
using UnityEditor;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Utility để kiểm tra compile status và chạy basic tests
    /// </summary>
    public static class CompileCheck
    {
        [MenuItem("Tools/Compile Check/Test HealthComponent Instantiation")]
        public static void TestHealthComponentInstantiation()
        {
            Debug.Log("=== Testing HealthComponent Instantiation ===");
            
            try
            {
                // Test tạo GameObject với HealthComponent
                var testObj = new GameObject("TestHealthComponent");
                var healthComp = testObj.AddComponent<EndlessRunner.Gameplay.HealthComponent>();
                
                // Kiểm tra basic properties
                Debug.Log($"✓ HealthComponent created successfully");
                Debug.Log($"  - Max Health: {healthComp.MaxHealth}");
                Debug.Log($"  - Current Health: {healthComp.CurrentHealth}");
                Debug.Log($"  - Is Alive: {healthComp.IsAlive}");
                Debug.Log($"  - Is In IFrames: {healthComp.IsInIFrames}");
                
                // Cleanup
                Object.DestroyImmediate(testObj);
                
                Debug.Log("✓ All tests passed! HealthComponent works correctly.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Test failed: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [MenuItem("Tools/Compile Check/Show Compilation Status")]
        public static void ShowCompilationStatus()
        {
            bool isCompiling = EditorApplication.isCompiling;
            bool hasCompileErrors = EditorUtility.scriptCompilationFailed;
            
            Debug.Log("=== Compilation Status ===");
            Debug.Log($"Is Compiling: {isCompiling}");
            Debug.Log($"Has Compile Errors: {hasCompileErrors}");
            
            if (isCompiling)
            {
                Debug.Log("⏳ Unity is currently compiling...");
            }
            else if (hasCompileErrors)
            {
                Debug.LogError("✗ There are compilation errors!");
            }
            else
            {
                Debug.Log("✓ Compilation successful!");
            }
        }
    }
}
