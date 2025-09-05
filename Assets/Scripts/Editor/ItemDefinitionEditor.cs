using UnityEngine;
using UnityEditor;
using EndlessRunner.Data;

namespace EndlessRunner.Editor
{
    /// <summary>
    /// Custom Editor cho ItemDefinition với validation display và utility functions
    /// </summary>
    [CustomEditor(typeof(ItemDefinition))]
    public class ItemDefinitionEditor : UnityEditor.Editor
    {
        private ItemDefinition _targetItem;
        private bool _showValidation = true;
        private bool _showPreview = true;
        private bool _showUtilities = false;

        private void OnEnable()
        {
            _targetItem = target as ItemDefinition;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            DrawHeader();
            EditorGUILayout.Space();

            // Default Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawValidationSection();
            EditorGUILayout.Space();
            DrawPreviewSection();
            EditorGUILayout.Space();
            DrawUtilitiesSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField($"Item Definition: {_targetItem.DisplayName}", titleStyle);
            
            if (!string.IsNullOrEmpty(_targetItem.Description))
            {
                GUIStyle descStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField(_targetItem.Description, descStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSection()
        {
            _showValidation = EditorGUILayout.Foldout(_showValidation, "🔍 Validation", true);
            if (!_showValidation) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Run validation
            bool isValid = _targetItem.ValidateConfiguration(out string errorMessage);

            if (isValid)
            {
                EditorGUILayout.HelpBox("✅ Item configuration is valid!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"❌ Validation Error: {errorMessage}", MessageType.Error);
            }

            // Additional validation info
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation Details:", EditorStyles.boldLabel);

            // Type-specific validation
            switch (_targetItem.Type)
            {
                case ItemType.Magnet:
                    DrawMagnetValidation();
                    break;
                case ItemType.Multiplier:
                    DrawMultiplierValidation();
                    break;
                case ItemType.Invisible:
                    DrawInvisibleValidation();
                    break;
                case ItemType.Life:
                    DrawLifeValidation();
                    break;
            }

            // Stacking validation
            if (_targetItem.CanStack)
            {
                string stackingInfo = _targetItem.StackingRule switch
                {
                    ItemStackingRule.Replace => "Replaces existing item",
                    ItemStackingRule.Add => "Adds duration to existing item",
                    ItemStackingRule.Multiply => "Multiplies effect value",
                    _ => "Unknown stacking rule"
                };
                EditorGUILayout.LabelField($"Stacking: {stackingInfo}");
            }
            else
            {
                EditorGUILayout.LabelField("Stacking: Not allowed");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMagnetValidation()
        {
            float radius = _targetItem.EffectValue;
            Color statusColor = (radius >= 0.5f && radius <= 10f) ? Color.green : Color.red;
            
            EditorGUI.BeginChangeCheck();
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Magnet Radius: {radius:F1} units");
            GUI.color = Color.white;
            
            if (radius < 0.5f)
                EditorGUILayout.HelpBox("Radius quá nhỏ, khuyến nghị >= 0.5", MessageType.Warning);
            else if (radius > 10f)
                EditorGUILayout.HelpBox("Radius quá lớn, khuyến nghị <= 10", MessageType.Warning);
        }

        private void DrawMultiplierValidation()
        {
            float multiplier = _targetItem.EffectValue;
            Color statusColor = (multiplier >= 1.1f && multiplier <= 10f) ? Color.green : Color.red;
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Multiplier Value: {multiplier:F1}x");
            GUI.color = Color.white;
            
            if (multiplier < 1.1f)
                EditorGUILayout.HelpBox("Multiplier quá nhỏ, khuyến nghị >= 1.1x", MessageType.Warning);
            else if (multiplier > 10f)
                EditorGUILayout.HelpBox("Multiplier quá lớn, khuyến nghị <= 10x", MessageType.Warning);
        }

        private void DrawInvisibleValidation()
        {
            float duration = _targetItem.Duration;
            Color statusColor = (duration >= 2f && duration <= 8f) ? Color.green : Color.yellow;
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Invisible Duration: {duration:F1}s");
            GUI.color = Color.white;
            
            if (duration < 2f)
                EditorGUILayout.HelpBox("Duration ngắn, có thể không hữu ích", MessageType.Info);
            else if (duration > 8f)
                EditorGUILayout.HelpBox("Duration dài, có thể làm game quá dễ", MessageType.Info);
        }

        private void DrawLifeValidation()
        {
            float healthAmount = _targetItem.EffectValue;
            bool isInteger = Mathf.Approximately(healthAmount, Mathf.Round(healthAmount));
            
            Color statusColor = (healthAmount >= 1f && isInteger) ? Color.green : Color.red;
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Health Restore: +{healthAmount:F0}");
            GUI.color = Color.white;
            
            if (!isInteger)
                EditorGUILayout.HelpBox("Health amount phải là số nguyên", MessageType.Error);
        }

        private void DrawPreviewSection()
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "👁️ Preview", true);
            if (!_showPreview) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Icon preview
            if (_targetItem.Icon != null)
            {
                EditorGUILayout.LabelField("Icon Preview:");
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_targetItem.Icon.texture, GUILayout.Width(64), GUILayout.Height(64));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No icon assigned", MessageType.Info);
            }

            // Color preview
            EditorGUILayout.LabelField("Effect Color:");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ColorField(_targetItem.EffectColor);
            EditorGUI.EndDisabledGroup();

            // Drop weight preview
            float dropWeight = _targetItem.GetDropWeight(1000f); // At 1km
            EditorGUILayout.LabelField($"Drop Weight (at 1km): {dropWeight:F3}");

            EditorGUILayout.EndVertical();
        }

        private void DrawUtilitiesSection()
        {
            _showUtilities = EditorGUILayout.Foldout(_showUtilities, "🔧 Utilities", true);
            if (!_showUtilities) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Reset to defaults button
            if (GUILayout.Button("Reset to Type Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Item", 
                    $"Reset this item to default values for {_targetItem.Type}?", "Yes", "Cancel"))
                {
                    ResetToTypeDefaults();
                }
            }

            // Test drop weight button  
            if (GUILayout.Button("Test Drop Weights"))
            {
                ShowDropWeightTest();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Batch operations
            EditorGUILayout.LabelField("Batch Operations:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create All Default Items"))
            {
                if (EditorUtility.DisplayDialog("Create Default Items",
                    "Create default ItemDefinition assets for all item types?", "Yes", "Cancel"))
                {
                    CreateAllDefaultItems();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ResetToTypeDefaults()
        {
            Undo.RecordObject(_targetItem, "Reset Item to Defaults");
            
            var defaultItem = ItemDefinition.CreateDefault(_targetItem.Type);
            
            // Copy default values (this is a simplified approach)
            EditorUtility.CopySerialized(defaultItem, _targetItem);
            
            DestroyImmediate(defaultItem);
            EditorUtility.SetDirty(_targetItem);
            
            Debug.Log($"Reset {_targetItem.name} to {_targetItem.Type} defaults");
        }

        private void ShowDropWeightTest()
        {
            Debug.Log("=== Drop Weight Test ===");
            for (float distance = 0f; distance <= 5000f; distance += 1000f)
            {
                float weight = _targetItem.GetDropWeight(distance);
                bool unlocked = _targetItem.IsUnlockedAt(distance);
                Debug.Log($"Distance: {distance}m, Weight: {weight:F3}, Unlocked: {unlocked}");
            }
            Debug.Log("========================");
        }

        private void CreateAllDefaultItems()
        {
            string folderPath = "Assets/Configs/Items";
            
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Configs", "Items");
            }

            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
            {
                if (itemType == ItemType.Manual) continue;

                var item = ItemDefinition.CreateDefault(itemType);
                string assetPath = $"{folderPath}/{itemType}_Default.asset";
                
                AssetDatabase.CreateAsset(item, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created default items in {folderPath}");
        }

        // Context menu items
        [MenuItem("Assets/Create/EndlessRunner/Quick Setup/All Default Items")]
        public static void CreateAllDefaultItemsMenu()
        {
            string folderPath = "Assets/Configs/Items";
            
            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
                AssetDatabase.CreateFolder("Assets", "Configs");
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets/Configs", "Items");

            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
            {
                if (itemType == ItemType.Manual) continue;

                var item = ItemDefinition.CreateDefault(itemType);
                string assetPath = $"{folderPath}/{itemType}_Default.asset";
                
                AssetDatabase.CreateAsset(item, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created all default items in {folderPath}");
            
            // Select the folder in Project view
            var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            Selection.activeObject = folderAsset;
            EditorGUIUtility.PingObject(folderAsset);
        }
    }
}
