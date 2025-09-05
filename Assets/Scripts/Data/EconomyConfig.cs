using UnityEngine;
using System;
using System.Collections.Generic;

namespace EndlessRunner.Data
{
    /// <summary>
    /// ScriptableObject cấu hình toàn bộ economy system của game
    /// Quản lý drop rates, spawn patterns, difficulty scaling cho items và currencies
    /// </summary>
    [CreateAssetMenu(menuName = "EndlessRunner/Economy Config", fileName = "Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Currency Drop Rates")]
        [SerializeField] private AnimationCurve _coinDropRate = AnimationCurve.Linear(0f, 0.8f, 10f, 0.6f);
        [SerializeField] private AnimationCurve _premiumCoinDropRate = AnimationCurve.Linear(0f, 0.05f, 10f, 0.1f);
        [SerializeField, Range(1, 10)] private int _coinClusterSize = 3;
        [SerializeField, Range(0.5f, 5f)] private float _coinSpacing = 1.5f;

        [Header("Item Drop Rates")]
        [SerializeField] private AnimationCurve _itemDropRate = AnimationCurve.Linear(0f, 0.1f, 10f, 0.3f);
        [SerializeField] private ItemDropWeight[] _itemWeights;

        [Header("Score & Multiplier")]
        [SerializeField, Range(1, 100)] private int _baseScorePerMeter = 10;
        [SerializeField] private AnimationCurve _speedBonusCurve = AnimationCurve.EaseInOut(0f, 1f, 20f, 2f);
        [SerializeField, Range(1f, 10f)] private float _maxMultiplierStack = 8f;

        [Header("Distance Progression")]
        [SerializeField] private AnimationCurve _difficultyProgression = AnimationCurve.EaseInOut(0f, 0f, 5f, 1f);
        [SerializeField, Range(100f, 2000f)] private float _maxProgressionDistance = 1000f;

        [Header("Spawn Density")]
        [SerializeField] private AnimationCurve _pickupDensity = AnimationCurve.Linear(0f, 1f, 10f, 0.8f);
        [SerializeField, Range(5f, 50f)] private float _minSpawnDistance = 15f;
        [SerializeField, Range(10f, 100f)] private float _maxSpawnDistance = 40f;

        // Public Properties
        public AnimationCurve CoinDropRate => _coinDropRate;
        public AnimationCurve PremiumCoinDropRate => _premiumCoinDropRate;
        public int CoinClusterSize => _coinClusterSize;
        public float CoinSpacing => _coinSpacing;
        public AnimationCurve ItemDropRate => _itemDropRate;
        public ItemDropWeight[] ItemWeights => _itemWeights;
        public int BaseScorePerMeter => _baseScorePerMeter;
        public AnimationCurve SpeedBonusCurve => _speedBonusCurve;
        public float MaxMultiplierStack => _maxMultiplierStack;
        public AnimationCurve DifficultyProgression => _difficultyProgression;
        public float MaxProgressionDistance => _maxProgressionDistance;
        public AnimationCurve PickupDensity => _pickupDensity;
        public float MinSpawnDistance => _minSpawnDistance;
        public float MaxSpawnDistance => _maxSpawnDistance;

        /// <summary>
        /// Cấu hình drop weight cho từng item type
        /// </summary>
        [System.Serializable]
        public struct ItemDropWeight
        {
            [SerializeField] private ItemDefinition _itemDefinition;
            [SerializeField, Range(0f, 1f)] private float _baseWeight;
            [SerializeField] private AnimationCurve _weightCurve; // Weight theo distance

            public ItemDefinition ItemDefinition => _itemDefinition;
            public float BaseWeight => _baseWeight;
            public AnimationCurve WeightCurve => _weightCurve;

            public float GetWeightAtDistance(float distanceKm)
            {
                return _baseWeight * _weightCurve.Evaluate(distanceKm);
            }
        }

        /// <summary>
        /// Calculate coin drop chance tại distance hiện tại
        /// </summary>
        public float GetCoinDropChance(float distanceKm)
        {
            return _coinDropRate.Evaluate(distanceKm);
        }

        /// <summary>
        /// Calculate premium coin drop chance
        /// </summary>
        public float GetPremiumCoinDropChance(float distanceKm)
        {
            return _premiumCoinDropRate.Evaluate(distanceKm);
        }

        /// <summary>
        /// Calculate item drop chance tổng
        /// </summary>
        public float GetItemDropChance(float distanceKm)
        {
            return _itemDropRate.Evaluate(distanceKm);
        }

        /// <summary>
        /// Get pickup spawn distance range
        /// </summary>
        public Vector2 GetSpawnDistanceRange(float distanceKm)
        {
            float density = _pickupDensity.Evaluate(distanceKm);
            float minDist = Mathf.Lerp(_maxSpawnDistance, _minSpawnDistance, density);
            float maxDist = Mathf.Lerp(_minSpawnDistance * 1.5f, _maxSpawnDistance, 1f - density);
            
            return new Vector2(minDist, maxDist);
        }

        /// <summary>
        /// Calculate score multiplier based on speed và current multipliers
        /// </summary>
        public float CalculateScoreMultiplier(float currentSpeed, float baseSpeed, float itemMultiplier = 1f)
        {
            float speedRatio = currentSpeed / baseSpeed;
            float speedBonus = _speedBonusCurve.Evaluate(speedRatio);
            float totalMultiplier = speedBonus * itemMultiplier;
            
            return Mathf.Min(totalMultiplier, _maxMultiplierStack);
        }

        /// <summary>
        /// Calculate base score per meter
        /// </summary>
        public int CalculateScorePerMeter(float distanceKm, float scoreMultiplier = 1f)
        {
            int baseScore = _baseScorePerMeter;
            return Mathf.RoundToInt(baseScore * scoreMultiplier);
        }

        /// <summary>
        /// Get weighted random item definition
        /// </summary>
        public ItemDefinition SelectRandomItem(float distanceKm, System.Random random = null)
        {
            if (_itemWeights == null || _itemWeights.Length == 0)
                return null;

            random = random ?? new System.Random();
            
            // Calculate total weight
            float totalWeight = 0f;
            var validItems = new List<(ItemDefinition item, float weight)>();

            foreach (var itemWeight in _itemWeights)
            {
                if (itemWeight.ItemDefinition == null) continue;
                if (!itemWeight.ItemDefinition.IsUnlockedAt(distanceKm * 1000f)) continue;

                float weight = itemWeight.GetWeightAtDistance(distanceKm);
                if (weight > 0f)
                {
                    validItems.Add((itemWeight.ItemDefinition, weight));
                    totalWeight += weight;
                }
            }

            if (validItems.Count == 0 || totalWeight <= 0f)
                return null;

            // Weighted selection
            float randomValue = (float)random.NextDouble() * totalWeight;
            float currentWeight = 0f;

            foreach (var (item, weight) in validItems)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                    return item;
            }

            // Fallback to last item
            return validItems[validItems.Count - 1].item;
        }

        /// <summary>
        /// Calculate difficulty factor at distance
        /// </summary>
        public float GetDifficultyFactor(float distanceKm)
        {
            float normalizedDistance = Mathf.Clamp01(distanceKm / (_maxProgressionDistance / 1000f));
            return _difficultyProgression.Evaluate(normalizedDistance);
        }

        /// <summary>
        /// Validate economy configuration
        /// </summary>
        public bool ValidateConfiguration(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Check curves
            if (_coinDropRate == null || _coinDropRate.keys.Length < 2)
            {
                errorMessage = "Coin Drop Rate curve phải có ít nhất 2 keyframes";
                return false;
            }

            if (_itemDropRate == null || _itemDropRate.keys.Length < 2)
            {
                errorMessage = "Item Drop Rate curve phải có ít nhất 2 keyframes";
                return false;
            }

            // Check item weights
            if (_itemWeights == null || _itemWeights.Length == 0)
            {
                errorMessage = "Item Weights array không được empty";
                return false;
            }

            // Check individual item weights
            for (int i = 0; i < _itemWeights.Length; i++)
            {
                var itemWeight = _itemWeights[i];
                if (itemWeight.ItemDefinition == null)
                {
                    errorMessage = $"Item Weight[{i}] có ItemDefinition null";
                    return false;
                }

                if (itemWeight.WeightCurve == null || itemWeight.WeightCurve.keys.Length < 2)
                {
                    errorMessage = $"Item Weight[{i}] curve phải có ít nhất 2 keyframes";
                    return false;
                }
            }

            // Check numeric values
            if (_baseScorePerMeter <= 0)
            {
                errorMessage = "Base Score Per Meter phải > 0";
                return false;
            }

            if (_minSpawnDistance >= _maxSpawnDistance)
            {
                errorMessage = "Min Spawn Distance phải < Max Spawn Distance";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tạo EconomyConfig mặc định
        /// </summary>
        public static EconomyConfig CreateDefault()
        {
            var config = CreateInstance<EconomyConfig>();

            // Setup default curves
            config._coinDropRate = AnimationCurve.Linear(0f, 0.7f, 10f, 0.5f);
            config._premiumCoinDropRate = AnimationCurve.Linear(0f, 0.02f, 10f, 0.08f);
            config._itemDropRate = AnimationCurve.Linear(0f, 0.1f, 10f, 0.25f);
            config._speedBonusCurve = AnimationCurve.EaseInOut(0f, 1f, 3f, 2f);
            config._difficultyProgression = AnimationCurve.EaseInOut(0f, 0.1f, 5f, 1f);
            config._pickupDensity = AnimationCurve.Linear(0f, 0.8f, 10f, 0.6f);

            // Default values
            config._coinClusterSize = 3;
            config._coinSpacing = 1.5f;
            config._baseScorePerMeter = 10;
            config._maxMultiplierStack = 8f;
            config._maxProgressionDistance = 1000f;
            config._minSpawnDistance = 15f;
            config._maxSpawnDistance = 40f;

            return config;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility - tạo default config
        /// </summary>
        [ContextMenu("Create Default Economy Config")]
        private void CreateDefaultEconomyConfig()
        {
            var config = CreateDefault();
            string assetPath = "Assets/Configs/Economy_Config.asset";
            UnityEditor.AssetDatabase.CreateAsset(config, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Created default economy config at Assets/Configs/Economy_Config.asset");
        }
#endif

        private void OnValidate()
        {
            // Auto-correct invalid values
            _coinClusterSize = Mathf.Max(1, _coinClusterSize);
            _coinSpacing = Mathf.Max(0.5f, _coinSpacing);
            _baseScorePerMeter = Mathf.Max(1, _baseScorePerMeter);
            _maxMultiplierStack = Mathf.Max(1f, _maxMultiplierStack);
            _maxProgressionDistance = Mathf.Max(100f, _maxProgressionDistance);
            _minSpawnDistance = Mathf.Max(5f, _minSpawnDistance);
            _maxSpawnDistance = Mathf.Max(_minSpawnDistance + 5f, _maxSpawnDistance);
        }
    }
}
