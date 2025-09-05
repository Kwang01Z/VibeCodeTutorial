using UnityEngine;
using UnityEditor;
using EndlessRunner.Data;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Custom Editor cho ChunkDefinition với validation display và gizmos visualization
    /// </summary>
    [CustomEditor(typeof(ChunkDefinition))]
    public class ChunkDefinitionEditor : UnityEditor.Editor
    {
        private ChunkDefinition _target;
        private bool _showValidationDetails = true;
        private bool _showObstacleStats = true;
        
        private void OnEnable()
        {
            _target = (ChunkDefinition)target;
        }
        
        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Validation section
            DrawValidationSection();
            
            EditorGUILayout.Space(5);
            
            // Stats section
            DrawStatsSection();
            
            EditorGUILayout.Space(5);
            
            // Debug info section
            DrawDebugSection();
        }
        
        /// <summary>
        /// Draw validation results
        /// </summary>
        private void DrawValidationSection()
        {
            _showValidationDetails = EditorGUILayout.Foldout(_showValidationDetails, "Validation Results", true);
            
            if (!_showValidationDetails) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var validation = _target.Validate();
            
            // Status indicator
            var statusStyle = new GUIStyle(EditorStyles.boldLabel);
            if (validation.isValid)
            {
                statusStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("✓ Valid", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("✗ Invalid", statusStyle);
            }
            
            // Errors
            if (validation.errors != null && validation.errors.Length > 0)
            {
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                foreach (var error in validation.errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            
            // Warnings
            if (validation.warnings != null && validation.warnings.Length > 0)
            {
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                foreach (var warning in validation.warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
            
            // Conflicts
            if (validation.conflictCount > 0)
            {
                EditorGUILayout.HelpBox($"Found {validation.conflictCount} position conflicts", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw obstacle statistics
        /// </summary>
        private void DrawStatsSection()
        {
            _showObstacleStats = EditorGUILayout.Foldout(_showObstacleStats, "Obstacle Statistics", true);
            
            if (!_showObstacleStats) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var stats = _target.GetObstacleStats();
            
            EditorGUILayout.LabelField($"Total Obstacles: {stats["Total"]}");
            
            if (stats["Total"] > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Left Lane: {stats["Lane0"]}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"Center: {stats["Lane1"]}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Right: {stats["Lane2"]}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Any: {stats["AnyLane"]}", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                
                // Density calculation
                float density = stats["Total"] / Mathf.Max(_target.ChunkLength, 1f);
                string densityText = $"Density: {density:F2}/m";
                if (density > 0.5f)
                {
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(densityText, style);
                }
                else
                {
                    EditorGUILayout.LabelField(densityText);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw debug information và tools
        /// </summary>
        private void DrawDebugSection()
        {
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Revalidate button
            if (GUILayout.Button("Force Revalidate"))
            {
                _target.InvalidateValidation();
                var result = _target.Validate();
                Debug.Log($"[ChunkDefinition] Revalidation complete: {(_target.name)} - Valid: {result.isValid}, Errors: {result.errors.Length}, Warnings: {result.warnings.Length}");
            }
            
            EditorGUILayout.Space(5);
            
            // Debug info text area
            EditorGUILayout.LabelField("Debug Info:");
            EditorGUILayout.TextArea(_target.GetDebugInfo(), GUILayout.Height(80));
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Scene view gizmos
        /// </summary>
        private void OnSceneGUI()
        {
            if (_target == null || _target.Obstacles == null) return;
            
            // Chỉ vẽ gizmos khi asset được select
            DrawChunkVisualization();
        }
        
        /// <summary>
        /// Draw chunk visualization trong Scene view
        /// </summary>
        private void DrawChunkVisualization()
        {
            // Base position (có thể là selection.activeTransform nếu chunk được gắn vào GameObject)
            Vector3 basePosition = Vector3.zero;
            
            // Vẽ chunk bounds
            Handles.color = Color.white;
            Vector3 chunkCenter = basePosition + Vector3.forward * _target.ChunkLength * 0.5f;
            Vector3 chunkSize = new Vector3(6f, 0.5f, _target.ChunkLength);
            Handles.DrawWireCube(chunkCenter, chunkSize);
            
            // Vẽ lane lines
            Handles.color = Color.gray;
            for (int lane = 0; lane < 3; lane++)
            {
                float laneX = (lane - 1) * 2f; // -2, 0, 2
                Vector3 laneStart = basePosition + new Vector3(laneX, 0, 0);
                Vector3 laneEnd = laneStart + Vector3.forward * _target.ChunkLength;
                Handles.DrawLine(laneStart, laneEnd);
            }
            
            // Vẽ obstacles
            var validation = _target.Validate();
            for (int i = 0; i < _target.Obstacles.Length; i++)
            {
                var obs = _target.Obstacles[i];
                Vector3 worldPos = basePosition + obs.localPosition;
                
                // Màu theo validation status
                if (!obs.IsValid)
                {
                    Handles.color = Color.red;
                }
                else if (HasConflictAtIndex(i))
                {
                    Handles.color = Color.yellow;
                }
                else
                {
                    Handles.color = _target.Difficulty switch
                    {
                        DifficultyTag.Easy => Color.green,
                        DifficultyTag.Medium => Color.blue,
                        DifficultyTag.Hard => Color.red,
                        _ => Color.white
                    };
                }
                
                // Vẽ obstacle representation
                Handles.DrawWireDisc(worldPos, Vector3.up, 0.5f);
                Handles.DrawLine(worldPos, worldPos + Vector3.up * 2f);
                
                // Label với info
                string obstacleLabel = $"{i}\n";
                if (obs.requiredLane >= 0)
                    obstacleLabel += $"L{obs.requiredLane}";
                else
                    obstacleLabel += "Any";
                
                Handles.Label(worldPos + Vector3.up * 2.5f, obstacleLabel);
                
                // Lane indicator line
                if (obs.requiredLane >= 0 && obs.requiredLane <= 2)
                {
                    Vector3 lanePos = worldPos;
                    lanePos.x = (obs.requiredLane - 1) * 2f;
                    Handles.DrawDottedLine(worldPos, lanePos, 3f);
                }
            }
            
            // Title
            Handles.color = Color.white;
            Handles.Label(basePosition + Vector3.up * 3f, $"ChunkDefinition: {_target.name}\nLength: {_target.ChunkLength}m | Difficulty: {_target.Difficulty}");
        }
        
        /// <summary>
        /// Kiểm tra obstacle có conflict không
        /// </summary>
        private bool HasConflictAtIndex(int index)
        {
            if (_target.Obstacles == null || index < 0 || index >= _target.Obstacles.Length)
                return false;
            
            var target = _target.Obstacles[index];
            for (int i = 0; i < _target.Obstacles.Length; i++)
            {
                if (i != index && target.ConflictsWith(_target.Obstacles[i], _target.ObstacleMinDistance))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Menu item để tạo chunk definition
        /// </summary>
        [MenuItem("Assets/Create/EndlessRunner/Chunk Definition")]
        public static void CreateChunkDefinition()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                path = "Assets";
            
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewChunkDefinition.asset");
            
            var chunk = CreateInstance<ChunkDefinition>();
            AssetDatabase.CreateAsset(chunk, assetPath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = chunk;
        }
    }
}
