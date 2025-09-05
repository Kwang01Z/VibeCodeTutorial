using UnityEngine;
using UnityEditor;
using EndlessRunner.Gameplay;
using EndlessRunner.Data;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Custom Editor cho ObstacleSpawner với runtime debugging và controls
    /// </summary>
    [CustomEditor(typeof(ObstacleSpawner))]
    public class ObstacleSpawnerEditor : UnityEditor.Editor
    {
        private ObstacleSpawner _spawner;
        private bool _showRuntimeStats = true;
        private bool _showDebugControls = true;
        
        private void OnEnable()
        {
            _spawner = (ObstacleSpawner)target;
        }
        
        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Runtime stats and controls available during Play mode", MessageType.Info);
                return;
            }
            
            if (!_spawner.IsInitialized)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Spawner not yet initialized", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // Runtime Stats Section
            DrawRuntimeStatsSection();
            
            EditorGUILayout.Space(5);
            
            // Debug Controls Section
            DrawDebugControlsSection();
        }
        
        /// <summary>
        /// Draw runtime statistics
        /// </summary>
        private void DrawRuntimeStatsSection()
        {
            _showRuntimeStats = EditorGUILayout.Foldout(_showRuntimeStats, "Runtime Statistics", true);
            
            if (!_showRuntimeStats) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Spawner Stats
            EditorGUILayout.LabelField("Spawner Stats:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_spawner.GetSpawnerStats(), GUILayout.Height(120));
            
            EditorGUILayout.Space(5);
            
            // Pool Stats
            EditorGUILayout.LabelField("Pool Stats:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_spawner.GetPoolStats(), GUILayout.Height(60));
            
            EditorGUILayout.Space(5);
            
            // Real-time values with progress bars
            DrawProgressBars();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw debug controls
        /// </summary>
        private void DrawDebugControlsSection()
        {
            _showDebugControls = EditorGUILayout.Foldout(_showDebugControls, "Debug Controls", true);
            
            if (!_showDebugControls) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Reset controls
            EditorGUILayout.LabelField("Reset Controls:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset Spawner"))
            {
                _spawner.ResetSpawner();
                _spawner.StartRun();
                Debug.Log("[ObstacleSpawnerEditor] Reset spawner and started new run");
            }
            
            if (GUILayout.Button("Reset Seed"))
            {
                uint newSeed = (uint)Random.Range(1000, 99999);
                _spawner.SetSeed(newSeed);
                Debug.Log($"[ObstacleSpawnerEditor] Set new seed: {newSeed}");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Force spawn controls
            EditorGUILayout.LabelField("Force Spawn Controls:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Easy Chunk"))
            {
                var easyChunk = GetRandomChunkByDifficulty(DifficultyTag.Easy);
                if (easyChunk != null)
                    _spawner.ForceSpawnChunk(easyChunk.name);
            }
            
            if (GUILayout.Button("Medium Chunk"))
            {
                var mediumChunk = GetRandomChunkByDifficulty(DifficultyTag.Medium);
                if (mediumChunk != null)
                    _spawner.ForceSpawnChunk(mediumChunk.name);
            }
            
            if (GUILayout.Button("Hard Chunk"))
            {
                var hardChunk = GetRandomChunkByDifficulty(DifficultyTag.Hard);
                if (hardChunk != null)
                    _spawner.ForceSpawnChunk(hardChunk.name);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Seed input field
            EditorGUILayout.LabelField("Seed Control:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            uint currentSeed = _spawner.CurrentSeed;
            uint newSeedInput = (uint)EditorGUILayout.IntField("Seed:", (int)currentSeed);
            
            if (newSeedInput != currentSeed && GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                _spawner.SetSeed(newSeedInput);
                Debug.Log($"[ObstacleSpawnerEditor] Applied new seed: {newSeedInput}");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw progress bars cho visual feedback
        /// </summary>
        private void DrawProgressBars()
        {
            // Active chunks progress (max khoảng 10-15 chunks reasonable)
            int activeCount = _spawner.ActiveChunkCount;
            int maxReasonable = 15;
            float activeProgress = Mathf.Clamp01(activeCount / (float)maxReasonable);
            
            EditorGUILayout.LabelField($"Active Chunks: {activeCount}");
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(18, 18), activeProgress, $"{activeCount}/{maxReasonable}");
            
            EditorGUILayout.Space(3);
            
            // Next spawn distance indicator (khoảng cách đến next spawn)
            if (_spawner.transform != null)
            {
                var player = FindObjectOfType<SimpleRunnerController>();
                if (player != null)
                {
                    float playerZ = player.transform.position.z;
                    float nextSpawnZ = _spawner.NextSpawnZ;
                    float spawnDistance = nextSpawnZ - playerZ;
                    float maxSpawnDistance = 100f; // Typical spawn distance
                    
                    float spawnProgress = Mathf.Clamp01(1f - (spawnDistance / maxSpawnDistance));
                    
                    EditorGUILayout.LabelField($"Next Spawn Distance: {spawnDistance:F1}m");
                    EditorGUI.ProgressBar(GUILayoutUtility.GetRect(18, 18), spawnProgress, $"{spawnDistance:F1}m");
                }
            }
        }
        
        /// <summary>
        /// Helper: Get random chunk by difficulty using reflection
        /// </summary>
        private ChunkDefinition GetRandomChunkByDifficulty(DifficultyTag difficulty)
        {
            // Use reflection to call private method
            var method = typeof(ObstacleSpawner).GetMethod("GetRandomChunkByDifficulty", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return (ChunkDefinition)method?.Invoke(_spawner, new object[] { difficulty });
        }
        
        /// <summary>
        /// Repaint inspector continuously during play mode
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && _spawner != null && _spawner.IsInitialized)
            {
                // Repaint at reasonable rate (not every frame)
                if (EditorApplication.timeSinceStartup % 0.1f < 0.02f) // Every 100ms
                {
                    Repaint();
                }
            }
        }
    }
}
