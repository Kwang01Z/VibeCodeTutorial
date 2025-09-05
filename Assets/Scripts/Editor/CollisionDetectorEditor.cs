using UnityEngine;
using UnityEditor;
using EndlessRunner.Gameplay;

/// <summary>
/// Custom editor cho CollisionDetector với debug tools và runtime monitoring
/// </summary>
[CustomEditor(typeof(CollisionDetector))]
public class CollisionDetectorEditor : Editor
{
    private CollisionDetector _target;
    private bool _showLayerSettings = true;
    private bool _showCollisionSettings = true;
    private bool _showDebugSettings = true;
    private bool _showRuntimeStats = true;
    private bool _showDebugControls = true;
    
    // Style caching
    private GUIStyle _headerStyle;
    private GUIStyle _statsStyle;
    private GUIStyle _buttonStyle;
    
    private void OnEnable()
    {
        _target = target as CollisionDetector;
    }
    
    public override void OnInspectorGUI()
    {
        InitializeStyles();
        
        serializedObject.Update();
        
        // Header
        DrawHeader();
        
        EditorGUILayout.Space(10);
        
        // Settings sections
        DrawLayerSettings();
        DrawCollisionSettings();
        DrawDebugSettings();
        
        EditorGUILayout.Space(10);
        
        // Runtime information
        if (Application.isPlaying)
        {
            DrawRuntimeStats();
            DrawDebugControls();
        }
        else
        {
            EditorGUILayout.HelpBox("Runtime statistics and debug controls available during play mode.", MessageType.Info);
        }
        
        // Validation warnings
        DrawValidationWarnings();
        
        serializedObject.ApplyModifiedProperties();
        
        // Request repaint during play mode for live stats
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(_target);
            Repaint();
        }
    }
    
    private void InitializeStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        if (_statsStyle == null)
        {
            _statsStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
        
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fixedHeight = 25;
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Collision Detector", _headerStyle);
        EditorGUILayout.LabelField("Handles collision detection between player and game objects", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLayerSettings()
    {
        _showLayerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showLayerSettings, "Layer Configuration");
        
        if (_showLayerSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Obstacle layer
            SerializedProperty obstacleLayerProp = serializedObject.FindProperty("_obstacleLayer");
            EditorGUILayout.PropertyField(obstacleLayerProp, new GUIContent("Obstacle Layer", "Layer mask for obstacles that damage the player"));
            
            // Show which layers are included
            LayerMask obstacleLayer = obstacleLayerProp.intValue;
            string obstacleLayerNames = GetLayerNames(obstacleLayer);
            if (!string.IsNullOrEmpty(obstacleLayerNames))
            {
                EditorGUILayout.LabelField("Includes: " + obstacleLayerNames, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space(5);
            
            // Pickup layer
            SerializedProperty pickupLayerProp = serializedObject.FindProperty("_pickupLayer");
            EditorGUILayout.PropertyField(pickupLayerProp, new GUIContent("Pickup Layer", "Layer mask for pickups that player can collect"));
            
            // Show which layers are included
            LayerMask pickupLayer = pickupLayerProp.intValue;
            string pickupLayerNames = GetLayerNames(pickupLayer);
            if (!string.IsNullOrEmpty(pickupLayerNames))
            {
                EditorGUILayout.LabelField("Includes: " + pickupLayerNames, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    private void DrawCollisionSettings()
    {
        _showCollisionSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showCollisionSettings, "Collision Settings");
        
        if (_showCollisionSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Collision cooldown
            SerializedProperty cooldownProp = serializedObject.FindProperty("_collisionCooldown");
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Collision Cooldown", "Minimum time between obstacle collision events"));
            EditorGUILayout.LabelField($"Prevents rapid collision spam", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(5);
            
            // Detection distance
            SerializedProperty distanceProp = serializedObject.FindProperty("_maxDetectionDistance");
            EditorGUILayout.PropertyField(distanceProp, new GUIContent("Max Detection Distance", "Maximum distance to detect collisions"));
            EditorGUILayout.LabelField($"Optimizes collision detection performance", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    private void DrawDebugSettings()
    {
        _showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showDebugSettings, "Debug Settings");
        
        if (_showDebugSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            SerializedProperty debugModeProp = serializedObject.FindProperty("_debugMode");
            SerializedProperty showGizmosProp = serializedObject.FindProperty("_showGizmos");
            
            EditorGUILayout.PropertyField(debugModeProp, new GUIContent("Debug Mode", "Enable debug logging and additional features"));
            EditorGUILayout.PropertyField(showGizmosProp, new GUIContent("Show Gizmos", "Draw debug gizmos in scene view"));
            
            if (debugModeProp.boolValue || showGizmosProp.boolValue)
            {
                EditorGUILayout.HelpBox("Debug features enabled. May impact performance.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    private void DrawRuntimeStats()
    {
        _showRuntimeStats = EditorGUILayout.BeginFoldoutHeaderGroup(_showRuntimeStats, "Runtime Statistics");
        
        if (_showRuntimeStats)
        {
            EditorGUILayout.BeginVertical(_statsStyle);
            
            // Collision counts
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Obstacle Hits:", GUILayout.Width(120));
            EditorGUILayout.LabelField(_target.ObstacleCollisionCount.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pickups Collected:", GUILayout.Width(120));
            EditorGUILayout.LabelField(_target.PickupCollisionCount.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            // Cooldown status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("In Cooldown:", GUILayout.Width(120));
            string cooldownText = _target.IsInCooldown ? "Yes" : "No";
            GUIStyle cooldownStyle = _target.IsInCooldown ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(cooldownText, cooldownStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Full debug stats
            if (GUILayout.Button("Show Detailed Stats", _buttonStyle))
            {
                Debug.Log(_target.GetDebugStats());
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    private void DrawDebugControls()
    {
        _showDebugControls = EditorGUILayout.BeginFoldoutHeaderGroup(_showDebugControls, "Debug Controls");
        
        if (_showDebugControls)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Obstacle Hit", _buttonStyle))
            {
                _target.ForceObstacleCollision("Editor_Debug");
            }
            
            if (GUILayout.Button("Reset Stats", _buttonStyle))
            {
                _target.ResetStats();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Use these buttons to test collision behavior during runtime.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    private void DrawValidationWarnings()
    {
        // Check for required components
        var healthSystem = _target.GetComponent<IHealthSystem>();
        var runnerController = _target.GetComponent<IRunnerController>();
        var collider = _target.GetComponent<Collider>();
        
        bool hasWarnings = false;
        
        if (healthSystem == null)
        {
            EditorGUILayout.HelpBox("IHealthSystem component not found! Add a HealthComponent to this GameObject.", MessageType.Error);
            hasWarnings = true;
        }
        
        if (runnerController == null)
        {
            EditorGUILayout.HelpBox("IRunnerController component not found! Player controller integration may not work.", MessageType.Warning);
            hasWarnings = true;
        }
        
        if (collider != null && !collider.isTrigger)
        {
            EditorGUILayout.HelpBox("Collider should be set as Trigger for collision detection to work properly.", MessageType.Warning);
            hasWarnings = true;
        }
        
        if (collider == null)
        {
            EditorGUILayout.HelpBox("No Collider component found! Add a Collider component set as Trigger.", MessageType.Error);
            hasWarnings = true;
        }
        
        // Check layer configuration
        LayerMask obstacleLayer = serializedObject.FindProperty("_obstacleLayer").intValue;
        LayerMask pickupLayer = serializedObject.FindProperty("_pickupLayer").intValue;
        
        if (obstacleLayer == 0)
        {
            EditorGUILayout.HelpBox("Obstacle layer mask is empty. No obstacles will be detected.", MessageType.Warning);
            hasWarnings = true;
        }
        
        if ((obstacleLayer & pickupLayer) != 0)
        {
            EditorGUILayout.HelpBox("Obstacle and pickup layers overlap. This may cause unexpected behavior.", MessageType.Warning);
            hasWarnings = true;
        }
        
        if (!hasWarnings)
        {
            EditorGUILayout.HelpBox("Configuration looks good! ✓", MessageType.Info);
        }
    }
    
    /// <summary>
    /// Get human-readable layer names from layer mask
    /// </summary>
    private string GetLayerNames(LayerMask layerMask)
    {
        System.Collections.Generic.List<string> layerNames = new System.Collections.Generic.List<string>();
        
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layerNames.Add($"{layerName} ({i})");
                }
                else
                {
                    layerNames.Add($"Layer {i}");
                }
            }
        }
        
        return string.Join(", ", layerNames);
    }
    
    /// <summary>
    /// Draw gizmos in scene view
    /// </summary>
    private void OnSceneGUI()
    {
        if (_target == null) return;
        
        SerializedProperty showGizmosProp = serializedObject.FindProperty("_showGizmos");
        if (!showGizmosProp.boolValue) return;
        
        SerializedProperty maxDistanceProp = serializedObject.FindProperty("_maxDetectionDistance");
        float maxDistance = maxDistanceProp.floatValue;
        
        // Draw detection range
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(_target.transform.position, Vector3.up, maxDistance);
        
        // Draw layer indicators
        Handles.color = Color.red;
        Handles.DrawWireCube(_target.transform.position + Vector3.up * 2f, Vector3.one * 0.2f);
        Handles.Label(_target.transform.position + Vector3.up * 2.2f, "Obstacles");
        
        Handles.color = Color.green;
        Handles.DrawWireCube(_target.transform.position + Vector3.up * 2.5f, Vector3.one * 0.2f);
        Handles.Label(_target.transform.position + Vector3.up * 2.7f, "Pickups");
    }
    
    /// <summary>
    /// Custom menu items
    /// </summary>
    [MenuItem("EndlessRunner/Collision/Add Collision Detector")]
    private static void AddCollisionDetector()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Select a GameObject first!");
            return;
        }
        
        if (selected.GetComponent<CollisionDetector>() != null)
        {
            Debug.LogWarning("GameObject already has a CollisionDetector!");
            return;
        }
        
        // Add required components
        if (selected.GetComponent<Collider>() == null)
        {
            var collider = selected.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            Debug.Log("Added BoxCollider set as Trigger");
        }
        
        if (selected.GetComponent<IHealthSystem>() == null)
        {
            selected.AddComponent<HealthComponent>();
            Debug.Log("Added HealthComponent");
        }
        
        // Add collision detector
        selected.AddComponent<CollisionDetector>();
        Debug.Log("Added CollisionDetector with required components");
    }
    
    [MenuItem("EndlessRunner/Collision/Setup Collision Layers")]
    private static void SetupCollisionLayers()
    {
        // This would typically open a custom window for layer setup
        // For now, just show instructions
        EditorUtility.DisplayDialog("Setup Collision Layers", 
            "To setup collision layers:\n\n" +
            "1. Go to Edit -> Project Settings -> Tags and Layers\n" +
            "2. Create 'Obstacle' layer (suggested: layer 8)\n" +
            "3. Create 'Pickup' layer (suggested: layer 10)\n" +
            "4. Assign these layers to your obstacle and pickup GameObjects\n" +
            "5. Configure the CollisionDetector layer masks", 
            "OK");
    }
}
