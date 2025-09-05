using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace EndlessRunner.Core.SceneManagement
{
    /// <summary>
    /// SceneManager - Handles Boot → MainMenu → Game scene transitions với async loading
    /// Phase 4 component cho meta-game & UI framework
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        #region Scene Names
        
        public static class SceneNames
        {
            public const string BOOT = "Boot";
            public const string MAIN_MENU = "MainMenu"; 
            public const string GAME = "Game";
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Scene Configuration")]
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _enableDebugLog = true;
        
        [SerializeField]
        [Tooltip("Loading screen duration (minimum)")]
        private float _minLoadingDuration = 1f;
        
        [Header("Loading Animation")]
        [SerializeField]
        [Tooltip("Loading screen prefab")]
        private GameObject _loadingScreenPrefab;
        
        [SerializeField]
        [Tooltip("Fade transition duration")]
        private float _fadeTransitionDuration = 0.5f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Scene load started event - (sceneName, loadMode)
        /// </summary>
        public static event Action<string, LoadSceneMode> OnSceneLoadStarted;
        
        /// <summary>
        /// Scene load progress event - (sceneName, progress 0-1)
        /// </summary>
        public static event Action<string, float> OnSceneLoadProgress;
        
        /// <summary>
        /// Scene load completed event - (sceneName, success)
        /// </summary>
        public static event Action<string, bool> OnSceneLoadCompleted;
        
        /// <summary>
        /// Scene transition started event - (fromScene, toScene)
        /// </summary>
        public static event Action<string, string> OnSceneTransitionStarted;
        
        /// <summary>
        /// Scene transition completed event - (fromScene, toScene, success)
        /// </summary>
        public static event Action<string, string, bool> OnSceneTransitionCompleted;
        
        #endregion
        
        #region Private Fields
        
        // Singleton instance
        private static SceneManager _instance;
        public static SceneManager Instance => _instance;
        
        // Current state
        private bool _isLoading = false;
        private string _currentSceneName;
        private GameObject _currentLoadingScreen;
        
        // Loading task cancellation token
        private CancellationTokenSource _loadingCancellationTokenSource;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Check if scene loading is in progress
        /// </summary>
        public bool IsLoading => _isLoading;
        
        /// <summary>
        /// Get current active scene name
        /// </summary>
        public string CurrentSceneName => _currentSceneName;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                LogDebug($"[SceneManager] Initialized - Current scene: {_currentSceneName}");
            }
            else
            {
                LogDebug("[SceneManager] Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Cleanup loading screen
            if (_currentLoadingScreen != null)
            {
                Destroy(_currentLoadingScreen);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Load scene asynchronously with loading screen
        /// </summary>
        public async void LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            if (_isLoading)
            {
                LogDebug($"[SceneManager] Already loading scene, ignoring request for {sceneName}");
                return;
            }
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneManager] Scene name cannot be null or empty");
                return;
            }
            
            LogDebug($"[SceneManager] Loading scene: {sceneName} (mode: {loadMode})");
            
            // Cancel previous loading task if any
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource?.Dispose();
            _loadingCancellationTokenSource = new CancellationTokenSource();
            
            await LoadSceneTask(sceneName, loadMode, _loadingCancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Transition from current scene to new scene
        /// </summary>
        public void TransitionToScene(string targetSceneName)
        {
            if (_isLoading)
            {
                LogDebug($"[SceneManager] Already loading, ignoring transition to {targetSceneName}");
                return;
            }
            
            string fromScene = _currentSceneName;
            
            OnSceneTransitionStarted?.Invoke(fromScene, targetSceneName);
            LogDebug($"[SceneManager] Transition: {fromScene} → {targetSceneName}");
            
            LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        }
        
        /// <summary>
        /// Load main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            TransitionToScene(SceneNames.MAIN_MENU);
        }
        
        /// <summary>
        /// Load game scene
        /// </summary>
        public void LoadGame()
        {
            TransitionToScene(SceneNames.GAME);
        }
        
        /// <summary>
        /// Reload current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (!string.IsNullOrEmpty(_currentSceneName))
            {
                TransitionToScene(_currentSceneName);
            }
        }
        
        /// <summary>
        /// Check if scene exists in build settings
        /// </summary>
        public bool SceneExistsInBuild(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                if (sceneNameFromPath.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask LoadSceneTask(string sceneName, LoadSceneMode loadMode, CancellationToken cancellationToken)
        {
            _isLoading = true;
            string fromScene = _currentSceneName;
            bool success = false;
            
            try
            {
                // Check if scene exists first
                if (!SceneExistsInBuild(sceneName))
                {
                    Debug.LogError($"[SceneManager] Scene '{sceneName}' not found in build settings");
                    OnSceneLoadCompleted?.Invoke(sceneName, false);
                    if (loadMode == LoadSceneMode.Single)
                    {
                        OnSceneTransitionCompleted?.Invoke(fromScene, sceneName, false);
                    }
                    return;
                }
                
                // Show loading screen
                ShowLoadingScreen();
                await UniTask.Delay(100, cancellationToken: cancellationToken); // Wait for loading screen setup
                
                OnSceneLoadStarted?.Invoke(sceneName, loadMode);
                
                // Start actual loading process
                success = await LoadSceneAsyncInternal(sceneName, loadMode, cancellationToken);
                
                // Hide loading screen after a brief delay
                await UniTask.Delay(200, cancellationToken: cancellationToken);
                HideLoadingScreen();
                
            }
            catch (OperationCanceledException)
            {
                LogDebug($"[SceneManager] Scene loading cancelled: {sceneName}");
                success = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneManager] Scene loading failed: {sceneName} - {ex.Message}");
                success = false;
                HideLoadingScreen();
            }
            finally
            {
                // Cleanup and notify
                _isLoading = false;
                OnSceneLoadCompleted?.Invoke(sceneName, success);
                
                if (loadMode == LoadSceneMode.Single)
                {
                    OnSceneTransitionCompleted?.Invoke(fromScene, sceneName, success);
                }
                
                _loadingCancellationTokenSource?.Dispose();
                _loadingCancellationTokenSource = null;
            }
        }
        
        private async UniTask<bool> LoadSceneAsyncInternal(string sceneName, LoadSceneMode loadMode, CancellationToken cancellationToken)
        {
            try
            {
                // Start async loading
                AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadMode);
                if (asyncLoad == null)
                {
                    Debug.LogError($"[SceneManager] Failed to start async load for scene '{sceneName}'");
                    return false;
                }
                
                asyncLoad.allowSceneActivation = false;
                float startTime = Time.time;
                
                // Loading progress loop
                while (!asyncLoad.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                    OnSceneLoadProgress?.Invoke(sceneName, progress);
                    
                    // Wait for minimum loading duration
                    float elapsedTime = Time.time - startTime;
                    bool minTimeReached = elapsedTime >= _minLoadingDuration;
                    
                    if (asyncLoad.progress >= 0.9f && minTimeReached)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                    
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                
                _currentSceneName = sceneName;
                LogDebug($"[SceneManager] Scene loaded successfully: {sceneName}");
                return true;
                
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneManager] Internal scene loading error: {sceneName} - {ex.Message}");
                return false;
            }
        }
        
        private void ShowLoadingScreen()
        {
            if (_loadingScreenPrefab != null && _currentLoadingScreen == null)
            {
                _currentLoadingScreen = Instantiate(_loadingScreenPrefab);
                DontDestroyOnLoad(_currentLoadingScreen);
                
                // Ensure loading screen is on top
                Canvas loadingCanvas = _currentLoadingScreen.GetComponent<Canvas>();
                if (loadingCanvas != null)
                {
                    loadingCanvas.sortingOrder = 1000;
                }
                
                LogDebug("[SceneManager] Loading screen shown");
            }
        }
        
        private void HideLoadingScreen()
        {
            if (_currentLoadingScreen != null)
            {
                // Add fade out animation if needed
                Destroy(_currentLoadingScreen);
                _currentLoadingScreen = null;
                
                LogDebug("[SceneManager] Loading screen hidden");
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
        
        #region Static Utility Methods
        
        /// <summary>
        /// Get current scene name (static)
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return Instance?._currentSceneName ?? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        
        /// <summary>
        /// Quick scene transition (static)
        /// </summary>
        public static void TransitionTo(string sceneName)
        {
            Instance?.TransitionToScene(sceneName);
        }
        
        /// <summary>
        /// Quick main menu load (static)
        /// </summary>
        public static void GoToMainMenu()
        {
            Instance?.LoadMainMenu();
        }
        
        /// <summary>
        /// Quick game load (static)
        /// </summary>
        public static void StartGame()
        {
            Instance?.LoadGame();
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        
        [ContextMenu("Go To Main Menu")]
        private void EditorGoToMainMenu()
        {
            if (Application.isPlaying)
            {
                LoadMainMenu();
            }
            else
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Scenes/{SceneNames.MAIN_MENU}.unity");
            }
        }
        
        [ContextMenu("Go To Game")]
        private void EditorGoToGame()
        {
            if (Application.isPlaying)
            {
                LoadGame();
            }
            else
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Scenes/{SceneNames.GAME}.unity");
            }
        }
        
        [ContextMenu("Debug Current Scene")]
        private void DebugCurrentScene()
        {
            Debug.Log($"Current Scene: {_currentSceneName}");
            Debug.Log($"Is Loading: {_isLoading}");
            Debug.Log($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
        
        #endif
        
        #endregion
    }
}
