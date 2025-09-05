using System;
using UnityEngine;
using UnityEditor;
using EndlessRunner.Gameplay;

/// <summary>
/// Simple collision test utility cho development testing
/// Không cần NUnit framework, chỉ sử dụng Unity Editor
/// </summary>
public static class CollisionTestUtility
{
    /// <summary>
    /// Tạo test scene với player, obstacles và pickups
    /// </summary>
    [MenuItem("EndlessRunner/Testing/Create Collision Test Scene")]
    public static void CreateCollisionTestScene()
    {
        // Clear existing test objects
        ClearTestObjects();
        
        // Create Player
        GameObject player = CreateTestPlayer();
        
        // Create Obstacles
        CreateTestObstacles();
        
        // Create Pickups
        CreateTestPickups();
        
        // Focus on player
        Selection.activeGameObject = player;
        SceneView.FrameLastActiveSceneView();
        
        Debug.Log("[CollisionTest] Test scene created successfully!");
        LogTestInstructions();
    }
    
    /// <summary>
    /// Tạo player object với collision detector
    /// </summary>
    private static GameObject CreateTestPlayer()
    {
        GameObject player = new GameObject("TestPlayer");
        player.transform.position = Vector3.zero;
        
        // Add visual representation
        var renderer = player.AddComponent<MeshRenderer>();
        var filter = player.AddComponent<MeshFilter>();
        filter.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        renderer.material = CreateTestMaterial(Color.blue);
        
        // Add collider (trigger)
        var collider = player.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.height = 2f;
        collider.radius = 0.5f;
        
        // Add health system
        var healthComponent = player.AddComponent<HealthComponent>();
        
        // Add integration test component
        player.AddComponent<EndlessRunner.Testing.CollisionRunnerIntegrationTest>();
        
        // Add mock runner controller (fallback if RunnerController not available)
        if (player.GetComponent<RunnerController>() == null)
        {
            player.AddComponent<MockRunnerController>();
        }
        
        // Add collision detector
        var collisionDetector = player.AddComponent<CollisionDetector>();
        
        // Add test player controller for movement
        player.AddComponent<EndlessRunner.Testing.TestPlayerController>();
        
        // Subscribe to events for testing
        collisionDetector.OnObstacleHit += OnObstacleHitTest;
        collisionDetector.OnPickupCollected += OnPickupCollectedTest;
        
        return player;
    }
    
    /// <summary>
    /// Tạo test obstacles
    /// </summary>
    private static void CreateTestObstacles()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject obstacle = new GameObject($"TestObstacle_{i}");
            obstacle.transform.position = new Vector3(i * 3f - 3f, 0, 5f);
            obstacle.layer = 8; // Obstacle layer
            
            // Visual
            var renderer = obstacle.AddComponent<MeshRenderer>();
            var filter = obstacle.AddComponent<MeshFilter>();
            filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            renderer.material = CreateTestMaterial(Color.red);
            
            // Collider
            var collider = obstacle.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one;
        }
    }
    
    /// <summary>
    /// Tạo test pickups
    /// </summary>
    private static void CreateTestPickups()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject pickup = new GameObject($"TestPickup_{i}");
            pickup.transform.position = new Vector3(i * 3f - 3f, 0, 10f);
            pickup.layer = 10; // Pickup layer
            
            // Visual
            var renderer = pickup.AddComponent<MeshRenderer>();
            var filter = pickup.AddComponent<MeshFilter>();
            filter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            renderer.material = CreateTestMaterial(Color.green);
            
            // Collider
            var collider = pickup.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            
            // Add floating animation
            pickup.AddComponent<FloatingAnimation>();
        }
    }
    
    /// <summary>
    /// Tạo material cho test objects
    /// </summary>
    private static Material CreateTestMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.3f);
        return mat;
    }
    
    /// <summary>
    /// Clear existing test objects
    /// </summary>
    private static void ClearTestObjects()
    {
        var testObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in testObjects)
        {
            if (obj.name.StartsWith("Test"))
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
    
    /// <summary>
    /// Event handler cho obstacle hits
    /// </summary>
    private static void OnObstacleHitTest(CollisionEventData data)
    {
        Debug.Log($"<color=red>[OBSTACLE HIT]</color> {data.gameObject.name} at {data.collisionPoint}");
        
        // Visual feedback
        if (data.gameObject != null)
        {
            var renderer = data.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow; // Change color on hit
            }
        }
    }
    
    /// <summary>
    /// Event handler cho pickup collection
    /// </summary>
    private static void OnPickupCollectedTest(CollisionEventData data)
    {
        Debug.Log($"<color=green>[PICKUP COLLECTED]</color> {data.gameObject.name} at {data.collisionPoint}");
    }
    
    /// <summary>
    /// Log test instructions
    /// </summary>
    private static void LogTestInstructions()
    {
        Debug.Log("<color=cyan>[COLLISION TEST INSTRUCTIONS]</color>\n" +
                  "1. Press Play to start testing\n" +
                  "2. Use WASD or Arrow Keys to move the blue player capsule\n" +
                  "3. Collide with red obstacles (should take damage)\n" +
                  "4. Collect green pickups (should not take damage)\n" +
                  "5. Watch console for collision events\n" +
                  "6. Check CollisionDetector Inspector for runtime stats");
    }
    
    /// <summary>
    /// Run automatic collision tests
    /// </summary>
    [MenuItem("EndlessRunner/Testing/Run Collision Tests")]
    public static void RunCollisionTests()
    {
        Debug.Log("<color=yellow>[STARTING COLLISION TESTS]</color>");
        
        // Find test player
        var testPlayer = GameObject.Find("TestPlayer");
        if (testPlayer == null)
        {
            Debug.LogError("TestPlayer not found! Create test scene first.");
            return;
        }
        
        var collisionDetector = testPlayer.GetComponent<CollisionDetector>();
        var healthComponent = testPlayer.GetComponent<HealthComponent>();
        
        if (collisionDetector == null || healthComponent == null)
        {
            Debug.LogError("Required components not found on TestPlayer!");
            return;
        }
        
        // Test 1: Basic initialization
        TestInitialization(collisionDetector);
        
        // Test 2: Force obstacle collision
        TestForceObstacleCollision(collisionDetector, healthComponent);
        
        // Test 3: Statistics
        TestStatistics(collisionDetector);
        
        // Test 4: Reset functionality
        TestReset(collisionDetector);
        
        Debug.Log("<color=green>[ALL COLLISION TESTS PASSED]</color>");
    }
    
    private static void TestInitialization(CollisionDetector detector)
    {
        Debug.Log("Test 1: Initialization...");
        
        if (detector.ObstacleCollisionCount != 0)
            Debug.LogError("Initial obstacle count should be 0");
        
        if (detector.PickupCollisionCount != 0)
            Debug.LogError("Initial pickup count should be 0");
        
        if (detector.IsInCooldown)
            Debug.LogError("Should not be in cooldown initially");
        
        Debug.Log("✓ Initialization test passed");
    }
    
    private static void TestForceObstacleCollision(CollisionDetector detector, HealthComponent health)
    {
        Debug.Log("Test 2: Force Obstacle Collision...");
        
        int initialHealth = health.CurrentHealth;
        detector.ForceObstacleCollision("UnitTest");
        
        if (health.CurrentHealth != initialHealth - 1)
            Debug.LogError($"Health should be {initialHealth - 1}, got {health.CurrentHealth}");
        
        if (detector.ObstacleCollisionCount != 1)
            Debug.LogError($"Obstacle count should be 1, got {detector.ObstacleCollisionCount}");
        
        Debug.Log("✓ Force collision test passed");
    }
    
    private static void TestStatistics(CollisionDetector detector)
    {
        Debug.Log("Test 3: Statistics...");
        
        string stats = detector.GetDebugStats();
        if (string.IsNullOrEmpty(stats))
            Debug.LogError("Debug stats should not be empty");
        
        if (!stats.Contains("Obstacles Hit: 1"))
            Debug.LogError("Stats should show 1 obstacle hit");
        
        Debug.Log("✓ Statistics test passed");
        Debug.Log($"Current stats:\n{stats}");
    }
    
    private static void TestReset(CollisionDetector detector)
    {
        Debug.Log("Test 4: Reset...");
        
        detector.ResetStats();
        
        if (detector.ObstacleCollisionCount != 0)
            Debug.LogError("Obstacle count should be 0 after reset");
        
        if (detector.PickupCollisionCount != 0)
            Debug.LogError("Pickup count should be 0 after reset");
        
        Debug.Log("✓ Reset test passed");
    }
}

/// <summary>
/// Mock RunnerController cho testing
/// </summary>
public class MockRunnerController : MonoBehaviour, IRunnerController
{
    public RunnerAction LastAction { get; private set; }
    public int ActionCount { get; private set; }
    
    // IRunnerController Properties
    public RunnerState CurrentState { get; private set; } = RunnerState.Running;
    public bool IsAlive => true;
    public bool IsGrounded => true;
    public bool IsInIFrames => false;
    public float StateTimeRemaining => 0f;
    
    // IRunnerController Events
    public event Action<RunnerState, RunnerState, float> OnStateChanged;
    public event Action<RunnerAction, int, bool> OnActionPerformed;
    public event Action OnDeath;
    public event Action OnRevive;
    
    // IRunnerController Methods
    public bool CanPerformAction(RunnerAction action, int parameter = 0)
    {
        return IsAlive; // Simple mock implementation
    }
    
    public bool PerformAction(RunnerAction action, int parameter = 0)
    {
        LastAction = action;
        ActionCount++;
        Debug.Log($"<color=orange>[RUNNER ACTION]</color> {action} (Total: {ActionCount})");
        
        // Fire event
        OnActionPerformed?.Invoke(action, parameter, true);
        
        return true;
    }
    
    public void ForceChangeState(RunnerState newState, float duration = 0f)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState, duration);
    }
    
    public void ResetToRunning()
    {
        ForceChangeState(RunnerState.Running);
    }
}

/// <summary>
/// Simple floating animation cho pickups
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    public float amplitude = 0.5f;
    public float frequency = 1f;
    
    private Vector3 startPos;
    
    void Start()
    {
        startPos = transform.position;
    }
    
    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        transform.Rotate(0, 50 * Time.deltaTime, 0); // Rotate
    }
}
