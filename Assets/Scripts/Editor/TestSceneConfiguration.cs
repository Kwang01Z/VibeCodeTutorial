using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EndlessRunner.Core;
using EndlessRunner.Gameplay;
using EndlessRunner.Testing;

/// <summary>
/// Configuration utility để setup test scenes cho Runner Integration testing
/// Đảm bảo môi trường test hoạt động chính xác với RunnerController và các components liên quan
/// </summary>
public static class TestSceneConfiguration
{
    private const string TEST_SCENE_NAME = "CollisionRunnerIntegrationTest";
    private const string RUNNER_TEST_TAG = "RunnerIntegrationTest";
    
    /// <summary>
    /// Tạo complete test scene với full runner integration
    /// </summary>
    [MenuItem("EndlessRunner/Testing/Create Runner Integration Test Scene")]
    public static void CreateRunnerIntegrationTestScene()
    {
        // Save current scene if needed
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            bool save = EditorUtility.DisplayDialog("Save Current Scene",
                "Current scene has unsaved changes. Save before creating test scene?",
                "Save", "Don't Save");
                
            if (save)
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }
        
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = TEST_SCENE_NAME;
        
        // Setup test environment
        SetupTestEnvironment();
        
        Debug.Log($"<color=cyan>[TEST SCENE]</color> Created {TEST_SCENE_NAME} with full Runner Integration setup");
        LogRunnerTestInstructions();
    }
    
    /// <summary>
    /// Setup complete test environment với runner integration
    /// </summary>
    private static void SetupTestEnvironment()
    {
        // Create test player với full integration
        GameObject testPlayer = CreateIntegratedTestPlayer();
        
        // Create test obstacles
        CreateTestObstacles();
        
        // Create test pickups
        CreateTestPickups();
        
        // Setup test manager
        CreateTestManager();
        
        // Focus on player
        Selection.activeGameObject = testPlayer;
        SceneView.FrameLastActiveSceneView();
    }
    
    /// <summary>
    /// Tạo player với full runner integration
    /// </summary>
    private static GameObject CreateIntegratedTestPlayer()
    {
        GameObject player = new GameObject("IntegratedTestPlayer");
        player.tag = RUNNER_TEST_TAG;
        player.transform.position = Vector3.zero;
        
        // Visual representation
        CreatePlayerVisual(player);
        
        // Physics components
        CreatePlayerPhysics(player);
        
        // Core gameplay components
        CreateCoreComponents(player);
        
        // Integration test component
        player.AddComponent<CollisionRunnerIntegrationTest>();
        
        // Test movement controller
        player.AddComponent<TestPlayerController>();
        
        return player;
    }
    
    /// <summary>
    /// Tạo visual representation cho player
    /// </summary>
    private static void CreatePlayerVisual(GameObject player)
    {
        var renderer = player.AddComponent<MeshRenderer>();
        var filter = player.AddComponent<MeshFilter>();
        filter.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        
        Material playerMat = new Material(Shader.Find("Standard"));
        playerMat.color = Color.blue;
        playerMat.SetFloat("_Metallic", 0f);
        playerMat.SetFloat("_Glossiness", 0.5f);
        renderer.material = playerMat;
    }
    
    /// <summary>
    /// Tạo physics components cho player
    /// </summary>
    private static void CreatePlayerPhysics(GameObject player)
    {
        // Trigger collider cho collision detection
        var triggerCollider = player.AddComponent<CapsuleCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.height = 2f;
        triggerCollider.radius = 0.5f;
        triggerCollider.center = Vector3.zero;
        
        // Rigidbody cho physics
        var rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false; // Controlled movement for testing
        rb.isKinematic = false;
    }
    
    /// <summary>
    /// Tạo core gameplay components
    /// </summary>
    private static void CreateCoreComponents(GameObject player)
    {
        // Health Component
        var healthComponent = player.AddComponent<HealthComponent>();
        
        // Runner Controller (real implementation)
        var runnerController = player.AddComponent<RunnerController>();
        
        // Collision Detector
        var collisionDetector = player.AddComponent<CollisionDetector>();
        
        // Subscribe to events for testing feedback
        collisionDetector.OnObstacleHit += OnTestObstacleHit;
        collisionDetector.OnPickupCollected += OnTestPickupCollected;
        
        healthComponent.OnHealthChanged += OnTestHealthChanged;
        healthComponent.OnDeath += OnTestPlayerDeath;
        
        runnerController.OnStateChanged += OnTestStateChanged;
        runnerController.OnActionPerformed += OnTestActionPerformed;
    }
    
    /// <summary>
    /// Tạo obstacles cho testing
    /// </summary>
    private static void CreateTestObstacles()
    {
        Vector3[] obstaclePositions = {
            new Vector3(-4f, 0f, 5f),
            new Vector3(0f, 0f, 8f),
            new Vector3(4f, 0f, 12f),
            new Vector3(-2f, 0f, 15f),
            new Vector3(2f, 0f, 18f)
        };
        
        for (int i = 0; i < obstaclePositions.Length; i++)
        {
            GameObject obstacle = new GameObject($"TestObstacle_{i:00}");
            obstacle.transform.position = obstaclePositions[i];
            obstacle.layer = LayerMask.NameToLayer("Obstacle") != -1 ? LayerMask.NameToLayer("Obstacle") : 8;
            
            // Visual
            var renderer = obstacle.AddComponent<MeshRenderer>();
            var filter = obstacle.AddComponent<MeshFilter>();
            filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            
            Material obstacleMat = new Material(Shader.Find("Standard"));
            obstacleMat.color = Color.red;
            obstacleMat.SetFloat("_Metallic", 0.2f);
            obstacleMat.SetFloat("_Glossiness", 0.7f);
            renderer.material = obstacleMat;
            
            // Collider
            var collider = obstacle.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }
    
    /// <summary>
    /// Tạo pickups cho testing
    /// </summary>
    private static void CreateTestPickups()
    {
        Vector3[] pickupPositions = {
            new Vector3(-3f, 0f, 6f),
            new Vector3(1f, 0f, 9f),
            new Vector3(3f, 0f, 13f),
            new Vector3(-1f, 0f, 16f),
            new Vector3(1f, 0f, 20f)
        };
        
        for (int i = 0; i < pickupPositions.Length; i++)
        {
            GameObject pickup = new GameObject($"TestPickup_{i:00}");
            pickup.transform.position = pickupPositions[i];
            pickup.layer = LayerMask.NameToLayer("Pickup") != -1 ? LayerMask.NameToLayer("Pickup") : 10;
            
            // Visual
            var renderer = pickup.AddComponent<MeshRenderer>();
            var filter = pickup.AddComponent<MeshFilter>();
            filter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            
            Material pickupMat = new Material(Shader.Find("Standard"));
            pickupMat.color = Color.green;
            pickupMat.SetFloat("_Metallic", 0f);
            pickupMat.SetFloat("_Glossiness", 0.8f);
            renderer.material = pickupMat;
            
            // Collider
            var collider = pickup.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.6f;
            
            // Animation
            pickup.AddComponent<TestPickupAnimation>();
        }
    }
    
    /// <summary>
    /// Tạo test manager để control testing flow
    /// </summary>
    private static void CreateTestManager()
    {
        GameObject testManager = new GameObject("TestManager");
        var manager = testManager.AddComponent<RunnerIntegrationTestManager>();
        
        // Find and assign test player
        var testPlayer = GameObject.FindWithTag(RUNNER_TEST_TAG);
        if (testPlayer != null)
        {
            manager.SetTestPlayer(testPlayer);
        }
    }
    
    #region Event Handlers cho testing feedback
    
    private static void OnTestObstacleHit(CollisionEventData data)
    {
        Debug.Log($"<color=red>[TEST] OBSTACLE HIT:</color> {data.gameObject.name} at {data.collisionPoint}");
        
        // Visual feedback
        if (data.gameObject != null)
        {
            var renderer = data.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }
        }
    }
    
    private static void OnTestPickupCollected(CollisionEventData data)
    {
        Debug.Log($"<color=green>[TEST] PICKUP COLLECTED:</color> {data.gameObject.name} at {data.collisionPoint}");
        
        // Disable pickup visual
        if (data.gameObject != null)
        {
            data.gameObject.SetActive(false);
        }
    }
    
    private static void OnTestHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"<color=orange>[TEST] HEALTH CHANGED:</color> {oldHealth} → {newHealth}");
    }
    
    private static void OnTestPlayerDeath()
    {
        Debug.Log("<color=red>[TEST] PLAYER DEATH</color>");
    }
    
    private static void OnTestStateChanged(RunnerState oldState, RunnerState newState, float duration)
    {
        Debug.Log($"<color=cyan>[TEST] STATE CHANGED:</color> {oldState} → {newState} (Duration: {duration:F2}s)");
    }
    
    private static void OnTestActionPerformed(RunnerAction action, int parameter, bool success)
    {
        Debug.Log($"<color=magenta>[TEST] ACTION:</color> {action} (Param: {parameter}, Success: {success})");
    }
    
    #endregion
    
    /// <summary>
    /// Log instructions cho runner integration testing
    /// </summary>
    private static void LogRunnerTestInstructions()
    {
        Debug.Log("<color=cyan>=== RUNNER INTEGRATION TEST INSTRUCTIONS ===</color>\n" +
                  "1. Press PLAY to start integration testing\n" +
                  "2. Use WASD/Arrow Keys để move blue player capsule\n" +
                  "3. Hit RED obstacles để test Hit state transition\n" +
                  "4. Collect GREEN pickups để test pickup logic\n" +
                  "5. Watch console for detailed state/event logging\n" +
                  "6. Monitor RunnerController inspector for real-time state\n" +
                  "7. Test IFrames by hitting obstacles rapidly\n" +
                  "8. Check HealthComponent for damage integration\n\n" +
                  "<color=yellow>Expected Behavior:</color>\n" +
                  "- Hit obstacle: Running → Hit → IFrames → Running\n" +
                  "- IFrames should prevent damage for short period\n" +
                  "- Health should decrease on obstacle hit\n" +
                  "- Pickups should not cause Hit state\n" +
                  "- State transitions should be smooth and predictable");
    }
    
    /// <summary>
    /// Quick test runner để verify integration
    /// </summary>
    [MenuItem("EndlessRunner/Testing/Verify Runner Integration")]
    public static void VerifyRunnerIntegration()
    {
        var testPlayer = GameObject.FindWithTag(RUNNER_TEST_TAG);
        if (testPlayer == null)
        {
            Debug.LogWarning("No runner integration test player found. Create test scene first.");
            return;
        }
        
        var runnerController = testPlayer.GetComponent<RunnerController>();
        var collisionDetector = testPlayer.GetComponent<CollisionDetector>();
        var healthComponent = testPlayer.GetComponent<HealthComponent>();
        
        bool isValid = true;
        
        if (runnerController == null)
        {
            Debug.LogError("RunnerController not found on test player!");
            isValid = false;
        }
        
        if (collisionDetector == null)
        {
            Debug.LogError("CollisionDetector not found on test player!");
            isValid = false;
        }
        
        if (healthComponent == null)
        {
            Debug.LogError("HealthComponent not found on test player!");
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log("<color=green>[VERIFICATION PASSED]</color> Runner Integration setup is valid!");
            
            // Test basic integration
            if (Application.isPlaying)
            {
                TestBasicIntegration(runnerController, collisionDetector, healthComponent);
            }
            else
            {
                Debug.Log("Enter Play Mode to run integration tests.");
            }
        }
        else
        {
            Debug.LogError("<color=red>[VERIFICATION FAILED]</color> Runner Integration setup has issues!");
        }
    }
    
    /// <summary>
    /// Test basic integration trong Play Mode
    /// </summary>
    private static void TestBasicIntegration(RunnerController runner, CollisionDetector detector, HealthComponent health)
    {
        Debug.Log("Testing basic runner integration...");
        
        // Test 1: Initial state
        Debug.Log($"Initial State: {runner.CurrentState}");
        Debug.Log($"Initial Health: {health.CurrentHealth}");
        Debug.Log($"Can Perform Hit: {runner.CanPerformAction(RunnerAction.Hit)}");
        
        // Test 2: Force hit action
        int initialHealth = health.CurrentHealth;
        bool hitSuccess = runner.PerformAction(RunnerAction.Hit);
        
        Debug.Log($"Hit Action Success: {hitSuccess}");
        Debug.Log($"State After Hit: {runner.CurrentState}");
        Debug.Log($"Health After Hit: {health.CurrentHealth} (Was: {initialHealth})");
        
        Debug.Log("<color=green>Basic integration test completed!</color>");
    }
}
