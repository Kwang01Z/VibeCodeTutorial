using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Layer Validation utility cho Phase 1.7 Collision & Health System.
    /// Auto-validates và setup required layers, physics matrix, và component configurations.
    /// </summary>
    public class LayerValidation : EditorWindow
    {
        #region Required Layer Setup
        
        private static readonly Dictionary<int, string> RequiredLayers = new Dictionary<int, string>
        {
            { 0, "Default" },        // Ground, Environment
            { 8, "Player" },         // Player character
            { 9, "Obstacle" },       // Collision obstacles
            { 10, "Pickup" },        // Items & collectibles (Phase 2)
            { 11, "PlayerIFrame" }   // Player during I-frames
        };
        
        // Physics collision matrix (true = collision enabled)
        private static readonly bool[,] PhysicsMatrix = new bool[12, 12]
        {
            // Default Player Obstacle Pickup PlayerIFrame ...
            { true,  true,  true,    true,   true,   true, true, true, true, true, true, true },  // Default
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // TransparentFX
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // IgnoreRaycast
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // Layer 3
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // Water
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // UI
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // Layer 6
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // Layer 7
            { true,  false, true,    true,   false,  true, true, true, true, true, true, true },  // Player (8)
            { true,  true,  false,   false,  false,  true, true, true, true, true, true, true },  // Obstacle (9)
            { true,  true,  false,   false,  false,  true, true, true, true, true, true, true },  // Pickup (10)
            { true,  false, false,   false,  false,  true, true, true, true, true, true, true },  // PlayerIFrame (11)
        };
        
        #endregion
        
        #region Menu Items
        
        [MenuItem("EndlessRunner/Phase 2/Validate Layer Setup")]
        public static void ValidateLayerSetup()
        {
            var window = GetWindow<LayerValidation>("Layer Validation Phase 2");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        [MenuItem("EndlessRunner/Phase 2/Auto-Setup Layers")]
        public static void AutoSetupLayers()
        {
            if (EditorUtility.DisplayDialog("Auto-Setup Layers", 
                "This will automatically configure layers and physics matrix for Phase 2.\n\n" +
                "Includes: Player, Obstacle, Pickup, PlayerIFrame layers.\n\n" +
                "This action cannot be undone. Continue?", "Yes", "Cancel"))
            {
                SetupRequiredLayers();
                ConfigurePhysicsMatrix();
                Debug.Log("[LayerValidation] Auto-setup completed successfully!");
                
                // Refresh validation
                ValidateLayerSetup();
            }
        }
        
        #endregion
        
        #region Validation Results
        
        private Vector2 _scrollPosition;
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        
        private class ValidationResult
        {
            public string category;
            public string message;
            public MessageType type;
            public System.Action fixAction;
            
            public ValidationResult(string category, string message, MessageType type, System.Action fixAction = null)
            {
                this.category = category;
                this.message = message;
                this.type = type;
                this.fixAction = fixAction;
            }
        }
        
        #endregion
        
        #region Editor Window UI
        
        private void OnEnable()
        {
            RunValidation();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUILayout.Label("Phase 1.7 - Layer Setup Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Validation", GUILayout.Height(25)))
            {
                RunValidation();
            }
            if (GUILayout.Button("Auto-Setup All", GUILayout.Height(25)))
            {
                AutoSetupLayers();
            }
            if (GUILayout.Button("Documentation", GUILayout.Height(25)))
            {
                string filePath = Application.dataPath.Replace("/Assets", "") + "/LAYER_SETUP_PHASE1_7.md";
                if (System.IO.File.Exists(filePath))
                {
                    // Open file with default application
                    System.Diagnostics.Process.Start(filePath);
                }
                else
                {
                    Debug.LogWarning($"[LayerValidation] Documentation file not found: {filePath}");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Results
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            foreach (var result in _validationResults)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Category header
                GUILayout.Label(result.category, EditorStyles.boldLabel);
                
                // Message
                EditorGUILayout.HelpBox(result.message, result.type);
                
                // Fix button
                if (result.fixAction != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Fix", GUILayout.Width(60)))
                    {
                        result.fixAction();
                        RunValidation();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            // Summary
            EditorGUILayout.Space(10);
            var errors = _validationResults.FindAll(r => r.type == MessageType.Error).Count;
            var warnings = _validationResults.FindAll(r => r.type == MessageType.Warning).Count;
            var info = _validationResults.FindAll(r => r.type == MessageType.Info).Count;
            
            GUIStyle summaryStyle = new GUIStyle(EditorStyles.helpBox);
            summaryStyle.fontSize = 12;
            
            if (errors == 0 && warnings == 0)
            {
                EditorGUILayout.HelpBox($"✅ All validations passed! ({info} info)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"❌ Issues found: {errors} errors, {warnings} warnings", MessageType.Error);
            }
        }
        
        #endregion
        
        #region Core Validation Logic
        
        private void RunValidation()
        {
            _validationResults.Clear();
            
            ValidateRequiredLayers();
            ValidatePhysicsMatrix();
            ValidatePlayerSetup();
            ValidateObstacleSetup();
            ValidatePickupSetup();
            ValidatePerformance();
        }
        
        private void ValidateRequiredLayers()
        {
            foreach (var kvp in RequiredLayers)
            {
                int layerIndex = kvp.Key;
                string expectedName = kvp.Value;
                string actualName = LayerMask.LayerToName(layerIndex);
                
                if (string.IsNullOrEmpty(actualName))
                {
                    _validationResults.Add(new ValidationResult(
                        "Missing Layer", 
                        $"Layer {layerIndex} should be '{expectedName}' but is empty",
                        MessageType.Error,
                        () => SetLayerName(layerIndex, expectedName)
                    ));
                }
                else if (actualName != expectedName)
                {
                    _validationResults.Add(new ValidationResult(
                        "Layer Name Mismatch", 
                        $"Layer {layerIndex} is '{actualName}' but should be '{expectedName}'",
                        MessageType.Warning,
                        () => SetLayerName(layerIndex, expectedName)
                    ));
                }
                else
                {
                    _validationResults.Add(new ValidationResult(
                        "Layer Setup", 
                        $"✅ Layer {layerIndex}: '{expectedName}' configured correctly",
                        MessageType.Info
                    ));
                }
            }
        }
        
        private void ValidatePhysicsMatrix()
        {
            bool matrixCorrect = true;
            int issues = 0;
            
            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    bool expected = PhysicsMatrix[i, j];
                    bool actual = !Physics.GetIgnoreLayerCollision(i, j);
                    
                    if (expected != actual)
                    {
                        matrixCorrect = false;
                        issues++;
                    }
                }
            }
            
            if (matrixCorrect)
            {
                _validationResults.Add(new ValidationResult(
                    "Physics Matrix", 
                    "✅ Physics collision matrix configured correctly",
                    MessageType.Info
                ));
            }
            else
            {
                _validationResults.Add(new ValidationResult(
                    "Physics Matrix", 
                    $"❌ {issues} collision matrix settings incorrect. Expected Player↔Obstacle, Player↔Pickup enabled; PlayerIFrame isolated.",
                    MessageType.Error,
                    () => ConfigurePhysicsMatrix()
                ));
            }
        }
        
        private void ValidatePlayerSetup()
        {
            var player = FindObjectOfType<EndlessRunner.Gameplay.RunnerController>();
            if (player == null)
            {
                _validationResults.Add(new ValidationResult(
                    "Player Setup", 
                    "⚠️ No RunnerController found in scene. Player setup cannot be validated.",
                    MessageType.Warning
                ));
                return;
            }
            
            var playerGO = player.gameObject;
            
            // Layer check
            if (playerGO.layer != LayerMask.NameToLayer("Player"))
            {
                _validationResults.Add(new ValidationResult(
                    "Player Layer", 
                    $"Player GameObject layer is {LayerMask.LayerToName(playerGO.layer)} but should be 'Player'",
                    MessageType.Error,
                    () => playerGO.layer = LayerMask.NameToLayer("Player")
                ));
            }
            
            // Component checks
            var healthComponent = playerGO.GetComponent<EndlessRunner.Gameplay.HealthComponent>();
            var collisionDetector = playerGO.GetComponent<EndlessRunner.Gameplay.CollisionDetector>();
            var rigidbody = playerGO.GetComponent<Rigidbody>();
            var collider = playerGO.GetComponent<Collider>();
            
            if (healthComponent == null)
                _validationResults.Add(new ValidationResult("Player Components", "❌ HealthComponent missing", MessageType.Error));
            
            if (collisionDetector == null)
                _validationResults.Add(new ValidationResult("Player Components", "❌ CollisionDetector missing", MessageType.Error));
            
            if (rigidbody == null)
                _validationResults.Add(new ValidationResult("Player Components", "❌ Rigidbody missing", MessageType.Error));
            else
            {
                if (rigidbody.isKinematic)
                    _validationResults.Add(new ValidationResult("Player Physics", "❌ Rigidbody should not be kinematic", MessageType.Error));
                    
                if (!rigidbody.freezeRotation)
                    _validationResults.Add(new ValidationResult("Player Physics", "⚠️ Should freeze rotation XYZ", MessageType.Warning));
            }
            
            if (collider == null)
                _validationResults.Add(new ValidationResult("Player Components", "❌ Collider missing", MessageType.Error));
            else if (!collider.isTrigger)
                _validationResults.Add(new ValidationResult("Player Physics", "❌ Player Collider should be Trigger", MessageType.Error));
            
            if (healthComponent && collisionDetector && rigidbody && collider && playerGO.layer == LayerMask.NameToLayer("Player"))
            {
                _validationResults.Add(new ValidationResult(
                    "Player Setup", 
                    "✅ Player GameObject configured correctly",
                    MessageType.Info
                ));
            }
        }
        
        private void ValidateObstacleSetup()
        {
            // Find obstacles trong scene (if any spawned)
            var obstacles = FindObjectsOfType<EndlessRunner.Gameplay.ChunkAnchor>();
            
            if (obstacles.Length == 0)
            {
                _validationResults.Add(new ValidationResult(
                    "Obstacle Setup", 
                    "ℹ️ No ChunkAnchors/Obstacles found in scene. Run game to spawn obstacles for validation.",
                    MessageType.Info
                ));
                return;
            }
            
            int correctObstacles = 0;
            int totalObstacles = 0;
            
            foreach (var chunkAnchor in obstacles)
            {
                var obstacleColliders = chunkAnchor.GetComponentsInChildren<Collider>();
                foreach (var col in obstacleColliders)
                {
                    totalObstacles++;
                    
                    if (col.gameObject.layer == LayerMask.NameToLayer("Obstacle") && col.isTrigger)
                    {
                        correctObstacles++;
                    }
                }
            }
            
            if (totalObstacles == 0)
            {
                _validationResults.Add(new ValidationResult(
                    "Obstacle Setup", 
                    "ℹ️ No obstacle colliders found in spawned chunks.",
                    MessageType.Info
                ));
            }
            else if (correctObstacles == totalObstacles)
            {
                _validationResults.Add(new ValidationResult(
                    "Obstacle Setup", 
                    $"✅ All {totalObstacles} obstacles configured correctly (Layer='Obstacle', IsTrigger=true)",
                    MessageType.Info
                ));
            }
            else
            {
                _validationResults.Add(new ValidationResult(
                    "Obstacle Setup", 
                    $"❌ {totalObstacles - correctObstacles}/{totalObstacles} obstacles misconfigured. Check Layer='Obstacle' and IsTrigger=true",
                    MessageType.Error
                ));
            }
        }
        
        private void ValidatePickupSetup()
        {
            // Phase 2: Validate pickup objects trong scene
            var pickups = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name == "ItemPickup")
                .ToArray();
            
            if (pickups.Length == 0)
            {
                _validationResults.Add(new ValidationResult(
                    "Pickup Setup", 
                    "ℹ️ No ItemPickup components found in scene. This is normal if Phase 2 pickups haven't been spawned yet.",
                    MessageType.Info
                ));
                return;
            }
            
            int correctPickups = 0;
            int totalPickups = pickups.Length;
            
            foreach (var pickup in pickups)
            {
                var pickupGO = pickup.gameObject;
                var collider = pickupGO.GetComponent<Collider>();
                
                bool isCorrect = true;
                var issues = new List<string>();
                
                // Layer check
                if (pickupGO.layer != LayerMask.NameToLayer("Pickup"))
                {
                    isCorrect = false;
                    issues.Add($"Layer = {LayerMask.LayerToName(pickupGO.layer)} (should be 'Pickup')");
                }
                
                // Collider check
                if (collider == null)
                {
                    isCorrect = false;
                    issues.Add("Missing Collider component");
                }
                else if (!collider.isTrigger)
                {
                    isCorrect = false;
                    issues.Add("Collider.isTrigger = false (should be true)");
                }
                
                if (isCorrect)
                {
                    correctPickups++;
                }
                else
                {
                    _validationResults.Add(new ValidationResult(
                        "Pickup Configuration", 
                        $"❌ Pickup '{pickupGO.name}': {string.Join(", ", issues)}",
                        MessageType.Warning
                    ));
                }
            }
            
            if (correctPickups == totalPickups)
            {
                _validationResults.Add(new ValidationResult(
                    "Pickup Setup", 
                    $"✅ All {totalPickups} pickups configured correctly (Layer='Pickup', IsTrigger=true)",
                    MessageType.Info
                ));
            }
            else
            {
                _validationResults.Add(new ValidationResult(
                    "Pickup Setup", 
                    $"⚠️ {correctPickups}/{totalPickups} pickups configured correctly. Check misconfigured pickups above.",
                    MessageType.Warning
                ));
            }
        }
        
        private void ValidatePerformance()
        {
            // Basic performance checks
            var colliders = FindObjectsOfType<Collider>();
            int triggerColliders = 0;
            int physicColliders = 0;
            
            foreach (var col in colliders)
            {
                if (col.isTrigger) triggerColliders++;
                else physicColliders++;
            }
            
            if (triggerColliders > physicColliders * 2)
            {
                _validationResults.Add(new ValidationResult(
                    "Performance", 
                    $"⚠️ High trigger ratio: {triggerColliders} triggers vs {physicColliders} physics colliders. Consider optimization.",
                    MessageType.Warning
                ));
            }
            else
            {
                _validationResults.Add(new ValidationResult(
                    "Performance", 
                    $"✅ Collider ratio reasonable: {triggerColliders} triggers, {physicColliders} physics",
                    MessageType.Info
                ));
            }
        }
        
        #endregion
        
        #region Auto-Setup Methods
        
        private static void SetupRequiredLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");
            
            foreach (var kvp in RequiredLayers)
            {
                int layerIndex = kvp.Key;
                string layerName = kvp.Value;
                
                if (layerIndex < layersProp.arraySize)
                {
                    var layerProp = layersProp.GetArrayElementAtIndex(layerIndex);
                    layerProp.stringValue = layerName;
                }
            }
            
            tagManager.ApplyModifiedProperties();
            Debug.Log("[LayerValidation] Required layers setup completed");
        }
        
        private static void SetLayerName(int layerIndex, string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");
            
            if (layerIndex < layersProp.arraySize)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(layerIndex);
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                
                Debug.Log($"[LayerValidation] Set layer {layerIndex} to '{layerName}'");
            }
        }
        
        private static void ConfigurePhysicsMatrix()
        {
            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    bool shouldCollide = PhysicsMatrix[i, j];
                    Physics.IgnoreLayerCollision(i, j, !shouldCollide);
                }
            }
            
            Debug.Log("[LayerValidation] Physics collision matrix configured");
        }
        
        #endregion
        
        #region Utility Methods
        
        [MenuItem("EndlessRunner/Phase 1.7/Quick Layer Test")]
        public static void QuickLayerTest()
        {
            Debug.Log("=== Quick Layer Test ===");
            
            foreach (var kvp in RequiredLayers)
            {
                int layerIndex = kvp.Key;
                string expectedName = kvp.Value;
                string actualName = LayerMask.LayerToName(layerIndex);
                
                if (actualName == expectedName)
                    Debug.Log($"✅ Layer {layerIndex}: '{actualName}'");
                else
                    Debug.LogError($"❌ Layer {layerIndex}: '{actualName}' (expected: '{expectedName}')");
            }
            
            // Test key collision pairs
            Debug.Log($"Player ↔ Obstacle collision: {!Physics.GetIgnoreLayerCollision(8, 9)}");
            Debug.Log($"Player ↔ Pickup collision: {!Physics.GetIgnoreLayerCollision(8, 10)}");
            Debug.Log($"PlayerIFrame ↔ Obstacle collision: {!Physics.GetIgnoreLayerCollision(11, 9)}");
        }
        
        #endregion
    }
}
