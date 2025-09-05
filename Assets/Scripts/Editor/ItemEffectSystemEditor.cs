using UnityEngine;
using UnityEditor;
using EndlessRunner.Items;
using EndlessRunner.Data;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Custom Editor cho ItemEffectSystem với debug visualization và runtime controls
    /// </summary>
    [CustomEditor(typeof(ItemEffectSystem))]
    public class ItemEffectSystemEditor : UnityEditor.Editor
    {
        private ItemEffectSystem _effectSystem;
        private bool _showActiveEffects = true;
        private bool _showSystemStats = true;
        private bool _showTestControls = false;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5; // 500ms

        private void OnEnable()
        {
            _effectSystem = target as ItemEffectSystem;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        public override void OnInspectorGUI()
        {
            // Auto refresh
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                Repaint();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            EditorGUILayout.Space();
            DrawHeader();
            EditorGUILayout.Space();

            // Default Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawSystemStatus();
            EditorGUILayout.Space();
            DrawActiveEffectsSection();
            EditorGUILayout.Space();
            DrawControlsSection();
            EditorGUILayout.Space();
            DrawTestControlsSection();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Item Effect System", titleStyle);

            if (_effectSystem != null)
            {
                string statusText = Application.isPlaying 
                    ? $"Running - {_effectSystem.ActiveEffectCount} active effects"
                    : "Not running";
                    
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };

                EditorGUILayout.LabelField(statusText, statusStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSystemStatus()
        {
            _showSystemStats = EditorGUILayout.Foldout(_showSystemStats, "📊 System Statistics", true);
            if (!_showSystemStats || !Application.isPlaying || _effectSystem == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Real-time stats
            EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Active Effects: {_effectSystem.ActiveEffectCount}");
            EditorGUILayout.LabelField($"Has Effects: {(_effectSystem.HasActiveEffects ? "Yes" : "No")}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Effect Values:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Magnet Radius: {_effectSystem.CurrentMagnetRadius:F1}");
            EditorGUILayout.LabelField($"Score Multiplier: {_effectSystem.CurrentScoreMultiplier:F1}x");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Extra Lives: {_effectSystem.CurrentExtraLives}");
            EditorGUILayout.LabelField($"Invisible: {(_effectSystem.HasInvisibilityEffect ? "Yes" : "No")}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detailed Statistics:", EditorStyles.boldLabel);
            
            string detailedStats = _effectSystem.GetSystemStats();
            string[] statLines = detailedStats.Split('\n');
            
            foreach (string line in statLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActiveEffectsSection()
        {
            _showActiveEffects = EditorGUILayout.Foldout(_showActiveEffects, "🔮 Active Effects", true);
            if (!_showActiveEffects || !Application.isPlaying || _effectSystem == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var activeEffects = _effectSystem.ActiveEffects;

            if (activeEffects.Count == 0)
            {
                EditorGUILayout.LabelField("No active effects", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var effect in activeEffects.Values)
                {
                    DrawEffectInfo(effect);
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEffectInfo(ActiveItemEffect effect)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with item name and icon
            EditorGUILayout.BeginHorizontal();
            
            // Icon if available
            if (effect.ItemDefinition.Icon != null)
            {
                Texture2D iconTexture = effect.ItemDefinition.Icon.texture;
                GUILayout.Label(iconTexture, GUILayout.Width(32), GUILayout.Height(32));
            }

            EditorGUILayout.BeginVertical();
            
            // Item name and type
            EditorGUILayout.LabelField(effect.ItemDefinition.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Type: {effect.ItemDefinition.Type}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            // Stack count if > 1
            if (effect.StackCount > 1)
            {
                GUIStyle stackStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 14
                };
                EditorGUILayout.LabelField($"x{effect.StackCount}", stackStyle, GUILayout.Width(40));
            }
            
            EditorGUILayout.EndHorizontal();

            // Effect details
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Value: {effect.CurrentEffectValue:F2}");
            
            if (!effect.IsPermanent)
            {
                EditorGUILayout.LabelField($"Remaining: {effect.RemainingTime:F1}s");
                EditorGUILayout.LabelField($"Progress: {effect.Progress * 100:F0}%");
            }
            else
            {
                EditorGUILayout.LabelField("Permanent");
            }
            EditorGUILayout.EndHorizontal();

            // Progress bar for timed effects
            if (!effect.IsPermanent)
            {
                Rect progressRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, effect.Progress, "");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawControlsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Controls:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = Application.isPlaying && _effectSystem != null;
            
            if (GUILayout.Button("Clear All Effects"))
            {
                _effectSystem.ClearAllEffects();
            }

            if (GUILayout.Button("Show System Stats"))
            {
                if (_effectSystem != null)
                {
                    Debug.Log(_effectSystem.GetSystemStats());
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);
            
            if (GUILayout.Button("Manual Refresh"))
            {
                Repaint();
            }

            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTestControlsSection()
        {
            _showTestControls = EditorGUILayout.Foldout(_showTestControls, "🧪 Test Controls", true);
            if (!_showTestControls) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.enabled = Application.isPlaying && _effectSystem != null;

            EditorGUILayout.LabelField("Quick Test Effects:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Magnet"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                _effectSystem.ApplyItemEffect(testItem);
            }

            if (GUILayout.Button("Test Multiplier"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Multiplier);
                _effectSystem.ApplyItemEffect(testItem);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Invisible"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Invisible);
                _effectSystem.ApplyItemEffect(testItem);
            }

            if (GUILayout.Button("Test Life"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Life);
                _effectSystem.ApplyItemEffect(testItem);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stacking Tests:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stack Magnet (Add)"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Magnet);
                // Note: Using default magnet item with default stacking rules
                // Default magnet has CanStack=true and StackingRule=Add
                _effectSystem.ApplyItemEffect(testItem);
            }

            if (GUILayout.Button("Stack Multiplier (Multiply)"))
            {
                var testItem = ItemDefinition.CreateDefault(ItemType.Multiplier);
                // Note: Using default multiplier item with default stacking rules
                // Default multiplier has CanStack=true and StackingRule=Multiply
                _effectSystem.ApplyItemEffect(testItem);
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }
    }
}
