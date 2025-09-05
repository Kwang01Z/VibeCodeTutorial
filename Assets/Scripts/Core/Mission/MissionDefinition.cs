using System;
using UnityEngine;

namespace EndlessRunner.Core.Mission
{
    /// <summary>
    /// MissionDefinition - ScriptableObject định nghĩa nhiệm vụ trong game
    /// Hỗ trợ nhiều loại mission: Distance, Coins, Items, Obstacles, Special
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "EndlessRunner/Mission/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        [System.Serializable]
        public enum MissionType
        {
            Distance,       // Chạy được x mét
            Coins,          // Thu thập x coins
            Items,          // Thu thập x items
            Obstacles,      // Tránh x obstacles liên tiếp
            PowerUp,        // Sử dụng x power-ups
            Special,        // Mission đặc biệt (custom logic)
            Survival,       // Sống sót x giây
            Speed,          // Đạt tốc độ x
            Combo,          // Combo action (nhảy + trượt liên tiếp)
            Theme           // Hoàn thành trong theme cụ thể
        }

        [System.Serializable]
        public enum MissionDifficulty
        {
            Easy,
            Medium,
            Hard,
            Expert
        }

        [System.Serializable]
        public enum RewardType
        {
            BoneFish,       // Xương Cá
            MouseTrap,      // Bẫy Chuột  
            PowerUp,        // Power-up items
            Experience,     // EXP points
            Unlock          // Unlock character/theme
        }

        [System.Serializable]
        public struct MissionReward
        {
            public RewardType rewardType;
            public int amount;
            public string itemId; // For PowerUp/Unlock rewards
        }

        #region Basic Information

        [Header("Basic Information")]
        [SerializeField]
        private string _missionId = "";

        [SerializeField]
        private string _displayName = "";

        [SerializeField]
        [TextArea(2, 4)]
        private string _description = "";

        [SerializeField]
        private MissionType _missionType = MissionType.Distance;

        [SerializeField]
        private MissionDifficulty _difficulty = MissionDifficulty.Easy;

        [SerializeField]
        private Sprite _icon;

        #endregion

        #region Mission Requirements

        [Header("Mission Requirements")]
        [SerializeField]
        [Tooltip("Target value to complete mission")]
        private int _targetValue = 100;

        [SerializeField]
        [Tooltip("Specific item ID required (for Items/PowerUp missions)")]
        private string _requiredItemId = "";

        [SerializeField]
        [Tooltip("Required theme ID (for Theme missions)")]
        private string _requiredThemeId = "";

        [SerializeField]
        [Tooltip("Must complete in single run")]
        private bool _requiresSingleRun = true;

        [SerializeField]
        [Tooltip("Custom parameter for special missions")]
        private string _customParameter = "";

        #endregion

        #region Availability

        [Header("Availability")]
        [SerializeField]
        [Tooltip("Minimum player level to unlock")]
        private int _requiredLevel = 1;

        [SerializeField]
        [Tooltip("Required total distance")]
        private long _requiredTotalDistance = 0;

        [SerializeField]
        [Tooltip("Prerequisites mission IDs")]
        private string[] _prerequisiteMissions = new string[0];

        [SerializeField]
        [Tooltip("Mission weight for random selection")]
        private float _selectionWeight = 1f;

        #endregion

        #region Rewards

        [Header("Rewards")]
        [SerializeField]
        [Tooltip("Mission rewards")]
        private MissionReward[] _rewards = new MissionReward[1];

        [SerializeField]
        [Tooltip("Experience points gained")]
        private int _experienceReward = 10;

        #endregion

        #region Time Constraints

        [Header("Time Constraints")]
        [SerializeField]
        [Tooltip("Mission has time limit")]
        private bool _hasTimeLimit = false;

        [SerializeField]
        [Tooltip("Time limit in hours")]
        private int _timeLimitHours = 24;

        [SerializeField]
        [Tooltip("Is daily mission")]
        private bool _isDailyMission = false;

        #endregion

        #region Properties

        public string MissionId => _missionId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public MissionType Type => _missionType;
        public MissionDifficulty Difficulty => _difficulty;
        public Sprite Icon => _icon;

        public int TargetValue => _targetValue;
        public string RequiredItemId => _requiredItemId;
        public string RequiredThemeId => _requiredThemeId;
        public bool RequiresSingleRun => _requiresSingleRun;
        public string CustomParameter => _customParameter;

        public int RequiredLevel => _requiredLevel;
        public long RequiredTotalDistance => _requiredTotalDistance;
        public string[] PrerequisiteMissions => _prerequisiteMissions;
        public float SelectionWeight => _selectionWeight;

        public MissionReward[] Rewards => _rewards;
        public int ExperienceReward => _experienceReward;

        public bool HasTimeLimit => _hasTimeLimit;
        public int TimeLimitHours => _timeLimitHours;
        public bool IsDailyMission => _isDailyMission;

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Auto-generate MissionId nếu trống
            if (string.IsNullOrEmpty(_missionId) && !string.IsNullOrEmpty(name))
            {
                _missionId = name.Replace(" ", "_").ToLowerInvariant();
            }

            // Validation rules
            if (_targetValue <= 0)
            {
                _targetValue = 1;
            }

            if (_requiredLevel < 1)
            {
                _requiredLevel = 1;
            }

            if (_selectionWeight < 0)
            {
                _selectionWeight = 0f;
            }

            if (_experienceReward < 0)
            {
                _experienceReward = 0;
            }

            if (_timeLimitHours < 1 && _hasTimeLimit)
            {
                _timeLimitHours = 1;
            }

            // Type-specific validation
            ValidateTypeSpecificFields();

            // Rewards validation
            if (_rewards == null || _rewards.Length == 0)
            {
                _rewards = new MissionReward[1];
                _rewards[0] = new MissionReward
                {
                    rewardType = RewardType.BoneFish,
                    amount = 100,
                    itemId = ""
                };
            }
        }

        private void ValidateTypeSpecificFields()
        {
            switch (_missionType)
            {
                case MissionType.Items:
                case MissionType.PowerUp:
                    if (string.IsNullOrEmpty(_requiredItemId))
                    {
                        Debug.LogWarning($"[MissionDefinition] {_missionType} mission '{name}' missing RequiredItemId");
                    }
                    break;

                case MissionType.Theme:
                    if (string.IsNullOrEmpty(_requiredThemeId))
                    {
                        Debug.LogWarning($"[MissionDefinition] Theme mission '{name}' missing RequiredThemeId");
                    }
                    break;

                case MissionType.Special:
                    if (string.IsNullOrEmpty(_customParameter))
                    {
                        Debug.LogWarning($"[MissionDefinition] Special mission '{name}' missing CustomParameter");
                    }
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Check if mission is available for player
        /// </summary>
        public bool IsAvailableForPlayer(int playerLevel, long totalDistance, string[] completedMissions)
        {
            // Level requirement
            if (playerLevel < _requiredLevel)
                return false;

            // Distance requirement
            if (totalDistance < _requiredTotalDistance)
                return false;

            // Prerequisites check
            if (_prerequisiteMissions.Length > 0)
            {
                foreach (string prerequisite in _prerequisiteMissions)
                {
                    bool hasPrerequisite = false;
                    foreach (string completed in completedMissions)
                    {
                        if (completed == prerequisite)
                        {
                            hasPrerequisite = true;
                            break;
                        }
                    }

                    if (!hasPrerequisite)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get formatted description with target value
        /// </summary>
        public string GetFormattedDescription()
        {
            string formattedDesc = _description;

            // Replace placeholders
            formattedDesc = formattedDesc.Replace("{target}", _targetValue.ToString("N0"));
            formattedDesc = formattedDesc.Replace("{item}", GetItemDisplayName(_requiredItemId));
            formattedDesc = formattedDesc.Replace("{theme}", GetThemeDisplayName(_requiredThemeId));

            return formattedDesc;
        }

        /// <summary>
        /// Get difficulty color
        /// </summary>
        public Color GetDifficultyColor()
        {
            switch (_difficulty)
            {
                case MissionDifficulty.Easy:
                    return Color.green;
                case MissionDifficulty.Medium:
                    return Color.yellow;
                case MissionDifficulty.Hard:
                    return new Color(1f, 0.5f, 0f, 1f); // Orange
                case MissionDifficulty.Expert:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Get total reward value (for sorting/comparison)
        /// </summary>
        public int GetTotalRewardValue()
        {
            int totalValue = _experienceReward;

            foreach (var reward in _rewards)
            {
                switch (reward.rewardType)
                {
                    case RewardType.BoneFish:
                        totalValue += reward.amount;
                        break;
                    case RewardType.MouseTrap:
                        totalValue += reward.amount * 10; // MouseTrap worth more
                        break;
                    case RewardType.PowerUp:
                        totalValue += reward.amount * 5;
                        break;
                    case RewardType.Experience:
                        totalValue += reward.amount;
                        break;
                    case RewardType.Unlock:
                        totalValue += 1000; // Unlocks are valuable
                        break;
                }
            }

            return totalValue;
        }

        /// <summary>
        /// Get mission progress description
        /// </summary>
        public string GetProgressDescription(int currentProgress)
        {
            switch (_missionType)
            {
                case MissionType.Distance:
                    return $"{currentProgress:N0}m / {_targetValue:N0}m";
                case MissionType.Coins:
                    return $"{currentProgress} / {_targetValue} coins";
                case MissionType.Items:
                    return $"{currentProgress} / {_targetValue} {GetItemDisplayName(_requiredItemId)}";
                case MissionType.Obstacles:
                    return $"{currentProgress} / {_targetValue} avoided";
                case MissionType.PowerUp:
                    return $"{currentProgress} / {_targetValue} used";
                case MissionType.Survival:
                    return $"{currentProgress}s / {_targetValue}s";
                case MissionType.Speed:
                    return $"{currentProgress} / {_targetValue} km/h";
                case MissionType.Combo:
                    return $"{currentProgress} / {_targetValue} combos";
                default:
                    return $"{currentProgress} / {_targetValue}";
            }
        }

        #endregion

        #region Private Utility Methods

        private string GetItemDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "items";

            // TODO: Integration với ShopManager để lấy display name
            switch (itemId.ToLower())
            {
                case "magnet":
                    return "Magnets";
                case "x2":
                    return "x2 Multipliers";
                case "invisible":
                    return "Invisible";
                case "life":
                    return "Extra Lives";
                default:
                    return itemId;
            }
        }

        private string GetThemeDisplayName(string themeId)
        {
            if (string.IsNullOrEmpty(themeId))
                return "theme";

            // TODO: Integration với ThemeManager để lấy display name
            switch (themeId.ToLower())
            {
                case "city":
                    return "City";
                case "forest":
                    return "Forest";
                case "beach":
                    return "Beach";
                case "winter":
                    return "Winter";
                default:
                    return themeId;
            }
        }

        #endregion

        #region Editor Support

        #if UNITY_EDITOR

        [ContextMenu("Generate Easy Distance Mission")]
        private void GenerateEasyDistanceMission()
        {
            _missionType = MissionType.Distance;
            _difficulty = MissionDifficulty.Easy;
            _displayName = "Short Run";
            _description = "Run {target} meters in a single game";
            _targetValue = 500;
            _requiresSingleRun = true;
            _rewards = new MissionReward[]
            {
                new MissionReward { rewardType = RewardType.BoneFish, amount = 250, itemId = "" }
            };
        }

        [ContextMenu("Generate Medium Coins Mission")]
        private void GenerateMediumCoinsMission()
        {
            _missionType = MissionType.Coins;
            _difficulty = MissionDifficulty.Medium;
            _displayName = "Coin Collector";
            _description = "Collect {target} coins";
            _targetValue = 100;
            _requiresSingleRun = false;
            _rewards = new MissionReward[]
            {
                new MissionReward { rewardType = RewardType.BoneFish, amount = 500, itemId = "" },
                new MissionReward { rewardType = RewardType.MouseTrap, amount = 1, itemId = "" }
            };
        }

        [ContextMenu("Generate Hard PowerUp Mission")]
        private void GenerateHardPowerUpMission()
        {
            _missionType = MissionType.PowerUp;
            _difficulty = MissionDifficulty.Hard;
            _displayName = "Power Master";
            _description = "Use {target} {item} in a single run";
            _targetValue = 5;
            _requiredItemId = "magnet";
            _requiresSingleRun = true;
            _rewards = new MissionReward[]
            {
                new MissionReward { rewardType = RewardType.BoneFish, amount = 1000, itemId = "" },
                new MissionReward { rewardType = RewardType.PowerUp, amount = 3, itemId = "x2" }
            };
        }

        [ContextMenu("Generate Daily Mission")]
        private void GenerateDailyMission()
        {
            _missionType = MissionType.Distance;
            _difficulty = MissionDifficulty.Medium;
            _displayName = "Daily Challenge";
            _description = "Complete a {target}m run today";
            _targetValue = 1000;
            _isDailyMission = true;
            _hasTimeLimit = true;
            _timeLimitHours = 24;
            _rewards = new MissionReward[]
            {
                new MissionReward { rewardType = RewardType.BoneFish, amount = 1000, itemId = "" },
                new MissionReward { rewardType = RewardType.MouseTrap, amount = 2, itemId = "" }
            };
        }

        #endif

        #endregion
    }
}
