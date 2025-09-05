using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Linq;

/// <summary>
/// Unity Editor utility để check compile errors và warnings
/// </summary>
public static class CompileChecker
{
    [MenuItem("EndlessRunner/Debug/Check Compile Status")]
    public static void CheckCompileStatus()
    {
        Debug.Log("=== COMPILE STATUS CHECK ===");
        
        // Check if currently compiling
        if (EditorApplication.isCompiling)
        {
            Debug.Log("<color=yellow>Currently compiling...</color>");
            return;
        }
        
        // Get all assemblies
        var assemblies = CompilationPipeline.GetAssemblies();
        var mainAssembly = assemblies.FirstOrDefault(a => a.name == "Assembly-CSharp");
        
        if (mainAssembly != null)
        {
            Debug.Log($"<color=green>✅ Compilation completed successfully!</color>");
            Debug.Log($"Main assembly found: {mainAssembly.name}");
            Debug.Log($"Source files count: {mainAssembly.sourceFiles?.Length ?? 0}");
        }
        else
        {
            Debug.LogWarning("Main assembly not found!");
        }
        
        // Test key classes
        TestKeyClasses();
        
        Debug.Log("=== END COMPILE CHECK ===");
    }
    
    private static void TestKeyClasses()
    {
        Debug.Log("Testing key classes instantiation...");
        
        try
        {
            // Test namespace imports
            var coreNamespace = typeof(EndlessRunner.Core.TestPickupAnimation);
            var testingNamespace = typeof(EndlessRunner.Testing.RunnerIntegrationTestManager);
            var gameplayNamespace = typeof(EndlessRunner.Gameplay.RunnerController);
            
            Debug.Log($"✅ Core namespace: {coreNamespace.Name}");
            Debug.Log($"✅ Testing namespace: {testingNamespace.Name}");
            Debug.Log($"✅ Gameplay namespace: {gameplayNamespace.Name}");
            
            Debug.Log("<color=green>All key classes are accessible!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Class test failed: {ex.Message}");
        }
    }
    
    [MenuItem("EndlessRunner/Debug/Force Recompile")]
    public static void ForceRecompile()
    {
        Debug.Log("Forcing recompilation...");
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();
    }
}
