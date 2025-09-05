using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using TMPro;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UIMenuTransitionManager - Advanced menu transition and screen management system
    /// Handles smooth transitions between different UI screens and menus
    /// </summary>
    public class UIMenuTransitionManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Screen Management")]
        [SerializeField] private UIScreen[] _screens;
        [SerializeField] private UIScreen _defaultScreen;
        [SerializeField] private bool _hideAllOnStart = true;

        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private Ease _transitionEaseType = Ease.OutQuart;
        [SerializeField] private float _screenOffset = 1000f;

        [Header("Background")]
        [SerializeField] private CanvasGroup _backgroundGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private bool _fadeBackgroundBetweenScreens = true;

        [Header("Loading")]
        [SerializeField] private UILoadingScreen _loadingScreen;
        [SerializeField] private float _minLoadingTime = 1f;

        [Header("Audio Integration")]
        [SerializeField] private bool _playTransitionSounds = true;
        [SerializeField] private string _screenOpenSoundId = "ui_screen_open";
        [SerializeField] private string _screenCloseSoundId = "ui_screen_close";

        [Header("Performance")]
        [SerializeField] private bool _deactivateHiddenScreens = true;
        [SerializeField] private int _maxConcurrentTransitions = 3;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _showTransitionDebug = false;

        #endregion

        #region Private Fields

        // Screen management
        private Dictionary<string, UIScreen> _screenLookup = new Dictionary<string, UIScreen>();
        private UIScreen _currentScreen;
        private UIScreen _previousScreen;
        private Queue<ScreenTransition> _transitionQueue = new Queue<ScreenTransition>();
        
        // Transition state
        private bool _isTransitioning = false;
        private int _activeTransitions = 0;
        private Coroutine _currentTransitionCoroutine;

        // Animation tracking
        private Dictionary<UIScreen, Sequence> _screenAnimations = new Dictionary<UIScreen, Sequence>();
        private List<Tween> _activeTweens = new List<Tween>();

        // Screen history for back navigation
        private Stack<UIScreen> _screenHistory = new Stack<UIScreen>();
        private int _maxHistorySize = 10;

        #endregion

        #region Properties

        public UIScreen CurrentScreen => _currentScreen;
        public UIScreen PreviousScreen => _previousScreen;
        public bool IsTransitioning => _isTransitioning;
        public int QueuedTransitionsCount => _transitionQueue.Count;

        #endregion

        #region Events

        public System.Action<UIScreen, UIScreen> OnScreenTransitionStarted;
        public System.Action<UIScreen, UIScreen> OnScreenTransitionCompleted;
        public System.Action<UIScreen> OnScreenShown;
        public System.Action<UIScreen> OnScreenHidden;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeScreens();
            SetupScreenReferences();
        }

        private void Start()
        {
            if (_hideAllOnStart)
            {
                HideAllScreens();
            }

            if (_defaultScreen != null)
            {
                ShowScreen(_defaultScreen.ScreenName, false);
            }
        }

        private void Update()
        {
            ProcessTransitionQueue();
            HandleInput();
        }

        private void OnDestroy()
        {
            CleanupTransitions();
        }

        #endregion

        #region Initialization

        private void InitializeScreens()
        {
            foreach (var screen in _screens)
            {
                if (screen != null)
                {
                    _screenLookup[screen.ScreenName] = screen;
                    screen.Initialize(this);
                }
            }

            LogDebug($"[UIMenuTransitionManager] Initialized {_screens.Length} screens");
        }

        private void SetupScreenReferences()
        {
            // Setup initial positions and states
            foreach (var screen in _screens)
            {
                if (screen != null)
                {
                    screen.SetupInitialState();
                }
            }
        }

        #endregion

        #region Screen Management

        public void ShowScreen(string screenName, bool addToHistory = true, object data = null)
        {
            if (!_screenLookup.TryGetValue(screenName, out UIScreen targetScreen))
            {
                LogDebug($"[UIMenuTransitionManager] Screen not found: {screenName}");
                return;
            }

            ShowScreen(targetScreen, addToHistory, data);
        }

        public void ShowScreen(UIScreen targetScreen, bool addToHistory = true, object data = null)
        {
            if (targetScreen == null)
            {
                LogDebug("[UIMenuTransitionManager] Target screen is null");
                return;
            }

            if (targetScreen == _currentScreen)
            {
                LogDebug($"[UIMenuTransitionManager] Screen {targetScreen.ScreenName} is already active");
                return;
            }

            // Queue transition if currently transitioning
            if (_isTransitioning && _activeTransitions >= _maxConcurrentTransitions)
            {
                QueueTransition(new ScreenTransition
                {
                    targetScreen = targetScreen,
                    addToHistory = addToHistory,
                    data = data,
                    transitionType = ScreenTransitionType.Show
                });
                return;
            }

            StartScreenTransition(targetScreen, addToHistory, data);
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen != null)
            {
                HideScreen(_currentScreen);
            }
        }

        public void HideScreen(string screenName)
        {
            if (_screenLookup.TryGetValue(screenName, out UIScreen screen))
            {
                HideScreen(screen);
            }
        }

        public void HideScreen(UIScreen screen)
        {
            if (screen == null) return;

            StartCoroutine(HideScreenCoroutine(screen));
        }

        public void HideAllScreens()
        {
            foreach (var screen in _screens)
            {
                if (screen != null && screen.IsVisible)
                {
                    screen.Hide(true);
                    if (_deactivateHiddenScreens)
                    {
                        screen.gameObject.SetActive(false);
                    }
                }
            }

            _currentScreen = null;
            _previousScreen = null;
        }

        public bool GoBack()
        {
            if (_screenHistory.Count > 0)
            {
                var previousScreen = _screenHistory.Pop();
                ShowScreen(previousScreen, false);
                return true;
            }
            return false;
        }

        #endregion

        #region Screen Transitions

        private void StartScreenTransition(UIScreen targetScreen, bool addToHistory, object data)
        {
            _isTransitioning = true;
            _activeTransitions++;

            // Add current screen to history
            if (addToHistory && _currentScreen != null)
            {
                AddToHistory(_currentScreen);
            }

            _previousScreen = _currentScreen;
            
            // Start transition coroutine
            _currentTransitionCoroutine = StartCoroutine(PerformScreenTransition(targetScreen, data));

            // Trigger event
            OnScreenTransitionStarted?.Invoke(_currentScreen, targetScreen);

            LogDebug($"[UIMenuTransitionManager] Starting transition: {_currentScreen?.ScreenName ?? "None"} -> {targetScreen.ScreenName}");
        }

        private IEnumerator PerformScreenTransition(UIScreen targetScreen, object data)
        {
            // Prepare target screen
            targetScreen.gameObject.SetActive(true);
            targetScreen.PrepareForShow(data);

            // Determine transition type
            var transitionType = DetermineTransitionType(_currentScreen, targetScreen);
            
            // Play transition sound
            if (_playTransitionSounds)
            {
                PlayTransitionSound(_screenOpenSoundId);
            }

            // Hide current screen
            if (_currentScreen != null)
            {
                yield return StartCoroutine(HideScreenWithTransition(_currentScreen, transitionType));
            }

            // Update current screen reference
            _currentScreen = targetScreen;

            // Show new screen
            yield return StartCoroutine(ShowScreenWithTransition(targetScreen, transitionType));

            // Complete transition
            CompleteTransition();
        }

        private IEnumerator HideScreenWithTransition(UIScreen screen, ScreenTransitionType transitionType)
        {
            var sequence = CreateHideTransitionSequence(screen, transitionType);
            
            yield return sequence.WaitForCompletion();

            screen.OnHidden();
            OnScreenHidden?.Invoke(screen);

            if (_deactivateHiddenScreens)
            {
                screen.gameObject.SetActive(false);
            }
        }

        private IEnumerator ShowScreenWithTransition(UIScreen screen, ScreenTransitionType transitionType)
        {
            var sequence = CreateShowTransitionSequence(screen, transitionType);
            
            yield return sequence.WaitForCompletion();

            screen.OnShown();
            OnScreenShown?.Invoke(screen);
        }

        private void CompleteTransition()
        {
            _isTransitioning = false;
            _activeTransitions--;

            // Trigger completion event
            OnScreenTransitionCompleted?.Invoke(_previousScreen, _currentScreen);

            LogDebug($"[UIMenuTransitionManager] Transition completed: {_currentScreen?.ScreenName}");
        }

        private IEnumerator HideScreenCoroutine(UIScreen screen)
        {
            if (screen == _currentScreen)
            {
                _previousScreen = _currentScreen;
                _currentScreen = null;
            }

            var sequence = CreateHideTransitionSequence(screen, ScreenTransitionType.SlideLeft);
            yield return sequence.WaitForCompletion();

            screen.OnHidden();
            OnScreenHidden?.Invoke(screen);

            if (_deactivateHiddenScreens)
            {
                screen.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Transition Animations

        private Sequence CreateShowTransitionSequence(UIScreen screen, ScreenTransitionType transitionType)
        {
            var sequence = DOTween.Sequence();
            var rectTransform = screen.RectTransform;
            var canvasGroup = screen.CanvasGroup;

            // Kill existing animations for this screen
            if (_screenAnimations.ContainsKey(screen))
            {
                _screenAnimations[screen].Kill();
            }

            switch (transitionType)
            {
                case ScreenTransitionType.SlideLeft:
                    rectTransform.anchoredPosition = new Vector2(_screenOffset, 0);
                    sequence.Append(rectTransform.DOAnchorPosX(0, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideRight:
                    rectTransform.anchoredPosition = new Vector2(-_screenOffset, 0);
                    sequence.Append(rectTransform.DOAnchorPosX(0, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideUp:
                    rectTransform.anchoredPosition = new Vector2(0, -_screenOffset);
                    sequence.Append(rectTransform.DOAnchorPosY(0, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideDown:
                    rectTransform.anchoredPosition = new Vector2(0, _screenOffset);
                    sequence.Append(rectTransform.DOAnchorPosY(0, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.Fade:
                    canvasGroup.alpha = 0f;
                    sequence.Append(canvasGroup.DOFade(1f, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.Scale:
                    rectTransform.localScale = Vector3.zero;
                    sequence.Append(rectTransform.DOScale(Vector3.one, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.ScaleFade:
                    rectTransform.localScale = Vector3.zero;
                    canvasGroup.alpha = 0f;
                    sequence.Append(rectTransform.DOScale(Vector3.one, _transitionDuration).SetEase(_transitionEaseType));
                    sequence.Join(canvasGroup.DOFade(1f, _transitionDuration).SetEase(_transitionEaseType));
                    break;
            }

            // Add background fade if enabled
            if (_fadeBackgroundBetweenScreens && _backgroundGroup != null)
            {
                sequence.Join(AnimateBackground(screen));
            }

            _screenAnimations[screen] = sequence;
            return sequence;
        }

        private Sequence CreateHideTransitionSequence(UIScreen screen, ScreenTransitionType transitionType)
        {
            var sequence = DOTween.Sequence();
            var rectTransform = screen.RectTransform;
            var canvasGroup = screen.CanvasGroup;

            // Kill existing animations for this screen
            if (_screenAnimations.ContainsKey(screen))
            {
                _screenAnimations[screen].Kill();
            }

            switch (transitionType)
            {
                case ScreenTransitionType.SlideLeft:
                    sequence.Append(rectTransform.DOAnchorPosX(-_screenOffset, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideRight:
                    sequence.Append(rectTransform.DOAnchorPosX(_screenOffset, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideUp:
                    sequence.Append(rectTransform.DOAnchorPosY(_screenOffset, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.SlideDown:
                    sequence.Append(rectTransform.DOAnchorPosY(-_screenOffset, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.Fade:
                    sequence.Append(canvasGroup.DOFade(0f, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.Scale:
                    sequence.Append(rectTransform.DOScale(Vector3.zero, _transitionDuration).SetEase(_transitionEaseType));
                    break;

                case ScreenTransitionType.ScaleFade:
                    sequence.Append(rectTransform.DOScale(Vector3.zero, _transitionDuration).SetEase(_transitionEaseType));
                    sequence.Join(canvasGroup.DOFade(0f, _transitionDuration).SetEase(_transitionEaseType));
                    break;
            }

            _screenAnimations[screen] = sequence;
            return sequence;
        }

        private Tween AnimateBackground(UIScreen screen)
        {
            if (_backgroundImage != null && screen.BackgroundSprite != null)
            {
                // Change background sprite
                _backgroundImage.sprite = screen.BackgroundSprite;
                
                // Fade background
                var currentAlpha = _backgroundGroup.alpha;
                _backgroundGroup.alpha = 0f;
                return _backgroundGroup.DOFade(currentAlpha, _transitionDuration * 0.5f);
            }
            
            return null;
        }

        #endregion

        #region Loading Screen

        public void ShowLoadingScreen(string message = "Loading...")
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.Show(message);
            }
        }

        public void HideLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.Hide();
            }
        }

        public IEnumerator ShowLoadingScreenCoroutine(string message, float minTime = -1f)
        {
            float loadingTime = minTime > 0 ? minTime : _minLoadingTime;
            
            ShowLoadingScreen(message);
            yield return new WaitForSeconds(loadingTime);
        }

        #endregion

        #region Utility Methods

        private ScreenTransitionType DetermineTransitionType(UIScreen from, UIScreen to)
        {
            if (from == null) return ScreenTransitionType.Fade;
            
            // Use screen-specific transition types if available
            if (to.PreferredTransitionType != ScreenTransitionType.Default)
            {
                return to.PreferredTransitionType;
            }

            // Default logic based on screen hierarchy or type
            return ScreenTransitionType.SlideLeft;
        }

        private void QueueTransition(ScreenTransition transition)
        {
            _transitionQueue.Enqueue(transition);
            LogDebug($"[UIMenuTransitionManager] Queued transition to: {transition.targetScreen.ScreenName}");
        }

        private void ProcessTransitionQueue()
        {
            if (!_isTransitioning && _transitionQueue.Count > 0)
            {
                var transition = _transitionQueue.Dequeue();
                
                if (transition.transitionType == ScreenTransitionType.Show)
                {
                    StartScreenTransition(transition.targetScreen, transition.addToHistory, transition.data);
                }
            }
        }

        private void AddToHistory(UIScreen screen)
        {
            // Remove duplicates
            var tempStack = new Stack<UIScreen>();
            while (_screenHistory.Count > 0)
            {
                var historyScreen = _screenHistory.Pop();
                if (historyScreen != screen)
                {
                    tempStack.Push(historyScreen);
                }
            }

            // Restore stack
            while (tempStack.Count > 0)
            {
                _screenHistory.Push(tempStack.Pop());
            }

            // Add new screen
            _screenHistory.Push(screen);

            // Limit history size
            while (_screenHistory.Count > _maxHistorySize)
            {
                var tempArray = _screenHistory.ToArray();
                _screenHistory.Clear();
                for (int i = 0; i < _maxHistorySize; i++)
                {
                    _screenHistory.Push(tempArray[i]);
                }
            }
        }

        private void HandleInput()
        {
            // Handle back button (Android) or Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentScreen != null && _currentScreen.CanGoBack)
                {
                    if (!GoBack())
                    {
                        // Handle app exit or main menu
                        _currentScreen.OnBackPressed();
                    }
                }
            }
        }

        private void PlayTransitionSound(string soundId)
        {
            // Integration with AudioManager
            if (EndlessRunner.Audio.AudioManager.Instance != null)
            {
                EndlessRunner.Audio.AudioManager.Instance.PlaySound(soundId);
            }
        }

        private void CleanupTransitions()
        {
            DOTween.KillAll();
            
            foreach (var animation in _screenAnimations.Values)
            {
                animation?.Kill();
            }
            _screenAnimations.Clear();

            foreach (var tween in _activeTweens)
            {
                tween?.Kill();
            }
            _activeTweens.Clear();
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log(message, this);
            }
        }

        #endregion

        #region Public API

        public UIScreen GetScreen(string screenName)
        {
            _screenLookup.TryGetValue(screenName, out UIScreen screen);
            return screen;
        }

        public bool IsScreenActive(string screenName)
        {
            return _currentScreen?.ScreenName == screenName;
        }

        public void SetTransitionDuration(float duration)
        {
            _transitionDuration = duration;
        }

        public void ClearHistory()
        {
            _screenHistory.Clear();
        }

        #endregion

        #region Debug

        [ContextMenu("Show Debug Info")]
        private void ShowDebugInfo()
        {
            string debugInfo = $"Current Screen: {_currentScreen?.ScreenName ?? "None"}\n" +
                              $"Previous Screen: {_previousScreen?.ScreenName ?? "None"}\n" +
                              $"Is Transitioning: {_isTransitioning}\n" +
                              $"Queued Transitions: {_transitionQueue.Count}\n" +
                              $"History Count: {_screenHistory.Count}";
            
            Debug.Log(debugInfo, this);
        }

        #endregion
    }

    #region Supporting Classes and Enums

    [System.Serializable]
    public class ScreenTransition
    {
        public UIScreen targetScreen;
        public bool addToHistory;
        public object data;
        public ScreenTransitionType transitionType;
    }

    public enum ScreenTransitionType
    {
        Default,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        Fade,
        Scale,
        ScaleFade,
        Show // For queued transitions
    }

    #endregion
}
