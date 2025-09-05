using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using EndlessRunner.Currency;

namespace EndlessRunner.Core.Mission
{
    /// <summary>
    /// MissionManager - Quản lý hệ thống nhiệm vụ với 3 mission song song, reroll logic
    /// Phase 4 component cho meta-game framework
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [System.Serializable]
        public class ActiveMission
        {
            public string missionId;
            public int currentProgress;
            public DateTime assignedTime;
            public DateTime? completedTime;
            public bool isCompleted;
            public bool isRewardClaimed;

            public ActiveMission(string id)
            {
                missionId = id;
                currentProgress = 0;
                assignedTime = DateTime.Now;
                completedTime = null;
                isCompleted = false;
                isRewardClaimed = false;
            }
        }

        #region Events

        /// <summary>
        /// Mission progress updated - (missionId, currentProgress, targetValue, isCompleted)
        /// </summary>
        public static event Action<string, int, int, bool> OnMissionProgressUpdated;

        /// <summary>
        /// Mission completed - (missionId, missionDefinition)
        /// </summary>
        public static event Action<string, MissionDefinition> OnMissionCompleted;

        /// <summary>
        /// Mission reward claimed - (missionId, rewards)
        /// </summary>
        public static event Action<string, MissionDefinition.MissionReward[]> OnMissionRewardClaimed;

        /// <summary>
        /// Mission rerolled - (oldMissionId, newMissionId)
        /// </summary>
        public static event Action<string, string> OnMissionRerolled;

        /// <summary>
        /// Daily missions reset - (newMissionIds)
        /// </summary>
        public static event Action<string[]> OnDailyMissionsReset;

        #endregion

        #region Serialized Fields

        [Header("Mission Configuration")]
        [SerializeField]
        [Tooltip("All available mission definitions")]
        private MissionDefinition[] _allMissions = new MissionDefinition[0];

        [SerializeField]
        [Tooltip("Maximum concurrent missions")]
        private int _maxConcurrentMissions = 3;

        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _enableDebugLog = true;

        [Header("Reroll Settings")]
        [SerializeField]
        [Tooltip("Allow mission rerolling")]
        private bool _allowReroll = true;

        [SerializeField]
        [Tooltip("Reroll cost in MouseTrap currency")]
        private int _rerollCost = 1;

        [SerializeField]
        [Tooltip("Free rerolls per day")]
        private int _freeRerollsPerDay = 1;

        [Header("Daily Reset")]
        [SerializeField]
        [Tooltip("Daily reset hour (24h format)")]
        private int _dailyResetHour = 0;

        [SerializeField]
        [Tooltip("Minimum time between resets (hours)")]
        private int _minResetIntervalHours = 20;

        #endregion

        #region Private Fields

        // Singleton instance
        private static MissionManager _instance;
        public static MissionManager Instance => _instance;

        // Mission data
        private Dictionary<string, MissionDefinition> _missionDefinitionsById;
        private List<ActiveMission> _activeMissions;
        private Dictionary<string, int> _completedMissionCounts; // missionId -> times completed
        private DateTime _lastDailyReset;
        private int _rerollsUsedToday;

        // Dependencies
        private ICurrencySystem _currencyManager;

        // State tracking
        private bool _isInitialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// Check if mission system is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get active missions
        /// </summary>
        public ActiveMission[] ActiveMissions => _activeMissions?.ToArray() ?? new ActiveMission[0];

        /// <summary>
        /// Get remaining free rerolls today
        /// </summary>
        public int RemainingFreeRerolls => Mathf.Max(0, _freeRerollsPerDay - _rerollsUsedToday);

        /// <summary>
        /// Get next daily reset time
        /// </summary>
        public DateTime NextDailyReset
        {
            get
            {
                var today = DateTime.Today;
                var resetTime = today.AddHours(_dailyResetHour);
                if (DateTime.Now >= resetTime)
                {
                    resetTime = resetTime.AddDays(1);
                }
                return resetTime;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LogDebug("[MissionManager] Initialized");
            }
            else
            {
                LogDebug("[MissionManager] Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        private async UniTask InitializeAsync()
        {
            try
            {
                LogDebug("[MissionManager] Initializing...");

                // Get dependencies
                var currencyManagerComponent = FindObjectOfType<CurrencyManager>();
                _currencyManager = currencyManagerComponent;
                if (_currencyManager == null)
                {
                    Debug.LogError("[MissionManager] CurrencyManager not found!");
                    return;
                }

                // Initialize data structures
                _missionDefinitionsById = new Dictionary<string, MissionDefinition>();
                _activeMissions = new List<ActiveMission>();
                _completedMissionCounts = new Dictionary<string, int>();

                // Load mission definitions
                LoadMissionDefinitions();

                // Load mission progress from save system
                await LoadMissionProgressAsync();

                // Check for daily reset
                CheckForDailyReset();

                // Ensure we have active missions
                await EnsureActiveMissionsAsync();

                _isInitialized = true;
                LogDebug($"[MissionManager] Initialized successfully with {_activeMissions.Count} active missions");

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MissionManager] Initialization failed: {ex.Message}");
            }
        }

        private void LoadMissionDefinitions()
        {
            _missionDefinitionsById.Clear();

            foreach (var mission in _allMissions)
            {
                if (mission == null)
                {
                    Debug.LogWarning("[MissionManager] Null mission definition found in array");
                    continue;
                }

                if (string.IsNullOrEmpty(mission.MissionId))
                {
                    Debug.LogWarning($"[MissionManager] Mission '{mission.name}' has empty MissionId");
                    continue;
                }

                if (_missionDefinitionsById.ContainsKey(mission.MissionId))
                {
                    Debug.LogWarning($"[MissionManager] Duplicate mission ID: {mission.MissionId}");
                    continue;
                }

                _missionDefinitionsById.Add(mission.MissionId, mission);
                LogDebug($"[MissionManager] Loaded mission: {mission.MissionId} ({mission.DisplayName})");
            }
        }

        private async UniTask LoadMissionProgressAsync()
        {
            // TODO: Integration với Save System
            // Tạm thời dùng dummy data
            _activeMissions.Clear();
            _completedMissionCounts.Clear();
            _lastDailyReset = DateTime.Today;
            _rerollsUsedToday = 0;

            // Dummy active missions for testing
            if (_missionDefinitionsById.Count > 0)
            {
                var missionIds = new List<string>(_missionDefinitionsById.Keys);
                for (int i = 0; i < Mathf.Min(3, missionIds.Count); i++)
                {
                    var activeMission = new ActiveMission(missionIds[i]);
                    activeMission.currentProgress = UnityEngine.Random.Range(0, 
                        _missionDefinitionsById[missionIds[i]].TargetValue / 2);
                    _activeMissions.Add(activeMission);
                }
            }

            await UniTask.Yield();
            LogDebug($"[MissionManager] Loaded {_activeMissions.Count} active missions");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Update mission progress
        /// </summary>
        public void UpdateMissionProgress(MissionDefinition.MissionType missionType, int amount, string itemId = "", string themeId = "")
        {
            if (!_isInitialized)
                return;

            foreach (var activeMission in _activeMissions)
            {
                if (activeMission.isCompleted)
                    continue;

                var missionDef = GetMissionDefinition(activeMission.missionId);
                if (missionDef == null || missionDef.Type != missionType)
                    continue;

                // Check type-specific requirements
                if (!CheckMissionRequirements(missionDef, itemId, themeId))
                    continue;

                // Update progress
                int oldProgress = activeMission.currentProgress;
                activeMission.currentProgress += amount;
                
                // Clamp to target
                activeMission.currentProgress = Mathf.Min(activeMission.currentProgress, missionDef.TargetValue);

                LogDebug($"[MissionManager] Updated {activeMission.missionId}: {oldProgress} -> {activeMission.currentProgress}/{missionDef.TargetValue}");

                // Check completion
                bool wasCompleted = activeMission.isCompleted;
                if (activeMission.currentProgress >= missionDef.TargetValue && !wasCompleted)
                {
                    CompleteMission(activeMission);
                }

                // Notify progress update
                OnMissionProgressUpdated?.Invoke(
                    activeMission.missionId,
                    activeMission.currentProgress,
                    missionDef.TargetValue,
                    activeMission.isCompleted
                );
            }
        }

        /// <summary>
        /// Claim mission reward
        /// </summary>
        public async UniTask<bool> ClaimMissionRewardAsync(string missionId)
        {
            var activeMission = GetActiveMission(missionId);
            if (activeMission == null)
            {
                LogDebug($"[MissionManager] Active mission not found: {missionId}");
                return false;
            }

            if (!activeMission.isCompleted)
            {
                LogDebug($"[MissionManager] Mission not completed: {missionId}");
                return false;
            }

            if (activeMission.isRewardClaimed)
            {
                LogDebug($"[MissionManager] Reward already claimed: {missionId}");
                return false;
            }

            var missionDef = GetMissionDefinition(missionId);
            if (missionDef == null)
            {
                Debug.LogError($"[MissionManager] Mission definition not found: {missionId}");
                return false;
            }

            try
            {
                // Give rewards
                foreach (var reward in missionDef.Rewards)
                {
                    switch (reward.rewardType)
                    {
                        case MissionDefinition.RewardType.BoneFish:
                            AddCurrencyByString("BoneFish", reward.amount);
                            break;
                        case MissionDefinition.RewardType.MouseTrap:
                            AddCurrencyByString("MouseTrap", reward.amount);
                            break;
                        case MissionDefinition.RewardType.Experience:
                            // TODO: Add experience to player profile
                            break;
                        case MissionDefinition.RewardType.PowerUp:
                            // TODO: Add power-up to inventory
                            break;
                        case MissionDefinition.RewardType.Unlock:
                            // TODO: Unlock item
                            break;
                    }
                }

                // Give experience
                if (missionDef.ExperienceReward > 0)
                {
                    // TODO: Add experience to player profile
                }

                // Mark as claimed
                activeMission.isRewardClaimed = true;

                // Save progress
                await SaveMissionProgressAsync();

                LogDebug($"[MissionManager] Claimed rewards for mission: {missionId}");
                OnMissionRewardClaimed?.Invoke(missionId, missionDef.Rewards);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MissionManager] Failed to claim reward for {missionId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reroll a mission
        /// </summary>
        public async UniTask<bool> RerollMissionAsync(string missionId)
        {
            if (!_allowReroll)
            {
                LogDebug("[MissionManager] Reroll disabled");
                return false;
            }

            var activeMission = GetActiveMission(missionId);
            if (activeMission == null)
            {
                LogDebug($"[MissionManager] Active mission not found: {missionId}");
                return false;
            }

            if (activeMission.isCompleted)
            {
                LogDebug($"[MissionManager] Cannot reroll completed mission: {missionId}");
                return false;
            }

            // Check reroll cost
            bool usesFreeReroll = RemainingFreeRerolls > 0;
            if (!usesFreeReroll)
            {
                if (!HasEnoughCurrencyByString("MouseTrap", _rerollCost))
                {
                    LogDebug($"[MissionManager] Insufficient MouseTrap for reroll: need {_rerollCost}");
                    return false;
                }
            }

            try
            {
                // Find replacement mission
                var newMissionId = SelectReplacementMission(missionId);
                if (string.IsNullOrEmpty(newMissionId))
                {
                    LogDebug("[MissionManager] No replacement mission available");
                    return false;
                }

                // Deduct cost
                if (!usesFreeReroll)
                {
                    bool deductSuccess = SpendCurrencyByString("MouseTrap", _rerollCost);
                    if (!deductSuccess)
                    {
                        LogDebug("[MissionManager] Failed to deduct reroll cost");
                        return false;
                    }
                }
                else
                {
                    _rerollsUsedToday++;
                }

                // Replace mission
                string oldMissionId = activeMission.missionId;
                activeMission.missionId = newMissionId;
                activeMission.currentProgress = 0;
                activeMission.assignedTime = DateTime.Now;
                activeMission.isCompleted = false;
                activeMission.isRewardClaimed = false;

                // Save progress
                await SaveMissionProgressAsync();

                LogDebug($"[MissionManager] Rerolled mission: {oldMissionId} -> {newMissionId}");
                OnMissionRerolled?.Invoke(oldMissionId, newMissionId);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MissionManager] Failed to reroll mission {missionId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get mission definition by ID
        /// </summary>
        public MissionDefinition GetMissionDefinition(string missionId)
        {
            _missionDefinitionsById.TryGetValue(missionId, out var definition);
            return definition;
        }

        /// <summary>
        /// Get active mission by ID
        /// </summary>
        public ActiveMission GetActiveMission(string missionId)
        {
            foreach (var mission in _activeMissions)
            {
                if (mission.missionId == missionId)
                    return mission;
            }
            return null;
        }

        /// <summary>
        /// Check if can reroll mission
        /// </summary>
        public bool CanRerollMission(string missionId, out string reason)
        {
            reason = "";

            if (!_allowReroll)
            {
                reason = "Reroll disabled";
                return false;
            }

            var activeMission = GetActiveMission(missionId);
            if (activeMission == null)
            {
                reason = "Mission not found";
                return false;
            }

            if (activeMission.isCompleted)
            {
                reason = "Mission completed";
                return false;
            }

            if (RemainingFreeRerolls > 0)
            {
                return true;
            }

            if (!HasEnoughCurrencyByString("MouseTrap", _rerollCost))
            {
                reason = $"Need {_rerollCost} MouseTrap";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Force daily reset (for testing)
        /// </summary>
        public async UniTask ForceDailyResetAsync()
        {
            LogDebug("[MissionManager] Forcing daily reset...");
            await PerformDailyResetAsync();
        }

        #endregion

        #region Private Methods

        private void CheckForDailyReset()
        {
            var now = DateTime.Now;
            var nextReset = NextDailyReset;
            var timeSinceLastReset = now - _lastDailyReset;

            bool shouldReset = false;

            // Check if it's time for daily reset
            if (now >= nextReset && timeSinceLastReset.TotalHours >= _minResetIntervalHours)
            {
                shouldReset = true;
            }

            // Check if this is the first run (no last reset recorded)
            if (_lastDailyReset == DateTime.MinValue)
            {
                shouldReset = true;
            }

            if (shouldReset)
            {
                PerformDailyResetAsync().Forget();
            }
        }

        private async UniTask PerformDailyResetAsync()
        {
            LogDebug("[MissionManager] Performing daily reset...");

            // Reset daily counters
            _rerollsUsedToday = 0;
            _lastDailyReset = DateTime.Now.Date;

            // Remove completed missions and create new ones
            var newMissionIds = new List<string>();

            for (int i = _activeMissions.Count - 1; i >= 0; i--)
            {
                var mission = _activeMissions[i];
                if (mission.isCompleted && mission.isRewardClaimed)
                {
                    // Remove completed and claimed missions
                    _activeMissions.RemoveAt(i);
                    LogDebug($"[MissionManager] Removed completed mission: {mission.missionId}");
                }
            }

            // Fill up to max concurrent missions
            while (_activeMissions.Count < _maxConcurrentMissions)
            {
                var newMissionId = SelectNewMission();
                if (!string.IsNullOrEmpty(newMissionId))
                {
                    var newMission = new ActiveMission(newMissionId);
                    _activeMissions.Add(newMission);
                    newMissionIds.Add(newMissionId);
                    LogDebug($"[MissionManager] Added new mission: {newMissionId}");
                }
                else
                {
                    break; // No more available missions
                }
            }

            // Save progress
            await SaveMissionProgressAsync();

            if (newMissionIds.Count > 0)
            {
                OnDailyMissionsReset?.Invoke(newMissionIds.ToArray());
            }

            LogDebug($"[MissionManager] Daily reset completed. Active missions: {_activeMissions.Count}");
        }

        private async UniTask EnsureActiveMissionsAsync()
        {
            // Fill up to max concurrent missions if needed
            while (_activeMissions.Count < _maxConcurrentMissions)
            {
                var newMissionId = SelectNewMission();
                if (!string.IsNullOrEmpty(newMissionId))
                {
                    var newMission = new ActiveMission(newMissionId);
                    _activeMissions.Add(newMission);
                    LogDebug($"[MissionManager] Added mission to fill slots: {newMissionId}");
                }
                else
                {
                    break; // No more available missions
                }
            }

            if (_activeMissions.Count > 0)
            {
                await SaveMissionProgressAsync();
            }
        }

        private string SelectNewMission()
        {
            // Get available missions (not currently active)
            var availableMissions = new List<MissionDefinition>();
            var activeMissionIds = new HashSet<string>();

            foreach (var activeMission in _activeMissions)
            {
                activeMissionIds.Add(activeMission.missionId);
            }

            foreach (var kvp in _missionDefinitionsById)
            {
                var mission = kvp.Value;
                if (activeMissionIds.Contains(mission.MissionId))
                    continue;

                // TODO: Check availability based on player stats
                if (mission.IsAvailableForPlayer(1, 0, new string[0]))
                {
                    availableMissions.Add(mission);
                }
            }

            if (availableMissions.Count == 0)
                return "";

            // Weighted selection
            float totalWeight = 0f;
            foreach (var mission in availableMissions)
            {
                totalWeight += mission.SelectionWeight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var mission in availableMissions)
            {
                currentWeight += mission.SelectionWeight;
                if (randomValue <= currentWeight)
                {
                    return mission.MissionId;
                }
            }

            // Fallback to first available
            return availableMissions[0].MissionId;
        }

        private string SelectReplacementMission(string excludeMissionId)
        {
            // Similar to SelectNewMission but exclude current mission
            var availableMissions = new List<MissionDefinition>();
            var excludeIds = new HashSet<string>();

            foreach (var activeMission in _activeMissions)
            {
                excludeIds.Add(activeMission.missionId);
            }
            excludeIds.Add(excludeMissionId);

            foreach (var kvp in _missionDefinitionsById)
            {
                var mission = kvp.Value;
                if (excludeIds.Contains(mission.MissionId))
                    continue;

                // TODO: Check availability based on player stats
                if (mission.IsAvailableForPlayer(1, 0, new string[0]))
                {
                    availableMissions.Add(mission);
                }
            }

            if (availableMissions.Count == 0)
                return "";

            // Random selection for reroll
            int randomIndex = UnityEngine.Random.Range(0, availableMissions.Count);
            return availableMissions[randomIndex].MissionId;
        }

        private bool CheckMissionRequirements(MissionDefinition mission, string itemId, string themeId)
        {
            switch (mission.Type)
            {
                case MissionDefinition.MissionType.Items:
                case MissionDefinition.MissionType.PowerUp:
                    return string.IsNullOrEmpty(mission.RequiredItemId) || 
                           mission.RequiredItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase);

                case MissionDefinition.MissionType.Theme:
                    return string.IsNullOrEmpty(mission.RequiredThemeId) ||
                           mission.RequiredThemeId.Equals(themeId, StringComparison.OrdinalIgnoreCase);

                default:
                    return true;
            }
        }

        private void CompleteMission(ActiveMission activeMission)
        {
            activeMission.isCompleted = true;
            activeMission.completedTime = DateTime.Now;

            // Track completion count
            if (_completedMissionCounts.ContainsKey(activeMission.missionId))
            {
                _completedMissionCounts[activeMission.missionId]++;
            }
            else
            {
                _completedMissionCounts[activeMission.missionId] = 1;
            }

            var missionDef = GetMissionDefinition(activeMission.missionId);
            LogDebug($"[MissionManager] Mission completed: {activeMission.missionId} ({missionDef?.DisplayName})");
            
            OnMissionCompleted?.Invoke(activeMission.missionId, missionDef);

            // Save progress
            SaveMissionProgressAsync().Forget();
        }

        private async UniTask SaveMissionProgressAsync()
        {
            // TODO: Integration với Save System
            LogDebug("[MissionManager] Mission progress saved (stub)");
            await UniTask.Yield();
        }

        /// <summary>
        /// Helper method to add currency by string name (converts to enum)
        /// </summary>
        private void AddCurrencyByString(string currencyName, long amount)
        {
            if (System.Enum.TryParse<CurrencyType>(currencyName, true, out CurrencyType currencyType))
            {
                _currencyManager.AddCurrency(currencyType, amount);
            }
            else
            {
                Debug.LogWarning($"[MissionManager] Invalid currency type: {currencyName}");
            }
        }
        
        /// <summary>
        /// Helper method to check currency by string name (converts to enum)
        /// </summary>
        private bool HasEnoughCurrencyByString(string currencyName, long amount)
        {
            if (System.Enum.TryParse<CurrencyType>(currencyName, true, out CurrencyType currencyType))
            {
                return _currencyManager.HasEnough(currencyType, amount);
            }
            
            Debug.LogWarning($"[MissionManager] Invalid currency type: {currencyName}");
            return false;
        }
        
        /// <summary>
        /// Helper method to spend currency by string name (converts to enum)
        /// </summary>
        private bool SpendCurrencyByString(string currencyName, long amount)
        {
            if (System.Enum.TryParse<CurrencyType>(currencyName, true, out CurrencyType currencyType))
            {
                return _currencyManager.SpendCurrency(currencyType, amount);
            }
            
            Debug.LogWarning($"[MissionManager] Invalid currency type: {currencyName}");
            return false;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message);
            }
        }

        #endregion

        #region Editor Support

        #if UNITY_EDITOR

        [ContextMenu("Debug Active Missions")]
        private void DebugActiveMissions()
        {
            if (!Application.isPlaying || _activeMissions == null)
            {
                Debug.Log("Mission system not initialized or not playing");
                return;
            }

            Debug.Log($"=== Active Missions ({_activeMissions.Count}) ===");
            foreach (var mission in _activeMissions)
            {
                var def = GetMissionDefinition(mission.missionId);
                string name = def?.DisplayName ?? mission.missionId;
                string status = mission.isCompleted ? "COMPLETED" : "IN PROGRESS";
                string progress = def != null ? $"{mission.currentProgress}/{def.TargetValue}" : $"{mission.currentProgress}";
                Debug.Log($"- {name}: {progress} ({status})");
            }
        }

        [ContextMenu("Force Daily Reset")]
        private void EditorForceDailyReset()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ForceDailyResetAsync().Forget();
            }
        }

        [ContextMenu("Complete All Missions")]
        private void EditorCompleteAllMissions()
        {
            if (!Application.isPlaying || !_isInitialized)
                return;

            foreach (var mission in _activeMissions)
            {
                if (!mission.isCompleted)
                {
                    var def = GetMissionDefinition(mission.missionId);
                    if (def != null)
                    {
                        mission.currentProgress = def.TargetValue;
                        CompleteMission(mission);
                    }
                }
            }
        }

        #endif

        #endregion
    }
}
