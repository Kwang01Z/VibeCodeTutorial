using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace EndlessRunner.Core.SceneManagement
{
    /// <summary>
    /// LoadingScreen - UI component cho loading screen với progress bar và animation
    /// Dùng để hiển thị loading progress khi chuyển scene
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Main loading screen panel")]
        private GameObject _loadingPanel;
        
        [SerializeField]
        [Tooltip("Progress bar slider")]
        private Slider _progressBar;
        
        [SerializeField]
        [Tooltip("Progress percentage text")]
        private TextMeshProUGUI _progressText;
        
        [SerializeField]
        [Tooltip("Loading status text")]
        private TextMeshProUGUI _statusText;
        
        [SerializeField]
        [Tooltip("Loading spinner/animation")]
        private RectTransform _loadingSpinner;
        
        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Fade in/out duration")]
        private float _fadeDuration = 0.5f;
        
        [SerializeField]
        [Tooltip("Spinner rotation speed (degrees/second)")]
        private float _spinnerSpeed = 360f;
        
        [SerializeField]
        [Tooltip("Progress bar smooth duration")]
        private float _progressSmoothDuration = 0.2f;
        
        [Header("Canvas Settings")]
        [SerializeField]
        [Tooltip("Canvas group for fading")]
        private CanvasGroup _canvasGroup;
        
        [SerializeField]
        [Tooltip("Loading screen canvas")]
        private Canvas _canvas;
        
        [Header("Loading Tips")]
        [SerializeField]
        [Tooltip("Random loading tips")]
        private string[] _loadingTips = {
            "Mẹo: Nhảy lên platform để tránh chướng ngại vật!",
            "Mẹo: Thu thập coin để mở khóa trang phục mới!",
            "Mẹo: Sử dụng power-up để có lợi thế trong game!",
            "Mẹo: Thử thách bạn bè với high score của bạn!",
            "Mẹo: Hoàn thành mission để nhận phần thưởng!"
        };
        
        #endregion
        
        #region Private Fields
        
        // State tracking
        private bool _isVisible = false;
        private bool _isAnimating = false;
        
        // Animation coroutines
        private Coroutine _fadeCoroutine;
        private Coroutine _spinnerCoroutine;
        private Coroutine _progressCoroutine;
        
        // Current values
        private float _currentProgress = 0f;
        private float _targetProgress = 0f;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Check if loading screen is visible
        /// </summary>
        public bool IsVisible => _isVisible;
        
        /// <summary>
        /// Check if loading screen is animating
        /// </summary>
        public bool IsAnimating => _isAnimating;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Setup canvas
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            
            // Setup initial state
            if (_loadingPanel != null)
                _loadingPanel.SetActive(false);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            
            // Setup canvas sorting
            if (_canvas != null)
            {
                _canvas.sortingOrder = 1000;
                _canvas.overrideSorting = true;
            }
            
            // Initialize progress
            SetProgress(0f, "Đang khởi tạo...");
        }
        
        private void OnDestroy()
        {
            // Stop all animations
            StopAllAnimations();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Show loading screen with fade in
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;
            
            _isVisible = true;
            
            // Activate panel
            if (_loadingPanel != null)
                _loadingPanel.SetActive(true);
            
            // Start fade in
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, _fadeDuration));
            
            // Start spinner animation
            StartSpinnerAnimation();
            
            // Show random tip
            ShowRandomTip();
            
            // Reset progress
            SetProgress(0f, "Đang tải...");
        }
        
        /// <summary>
        /// Hide loading screen with fade out
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;
            
            _isVisible = false;
            
            // Start fade out
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, _fadeDuration, () => {
                if (_loadingPanel != null)
                    _loadingPanel.SetActive(false);
            }));
            
            // Stop animations
            StopSpinnerAnimation();
        }
        
        /// <summary>
        /// Set loading progress
        /// </summary>
        /// <param name="progress">Progress value 0-1</param>
        /// <param name="statusMessage">Status message to display</param>
        public void SetProgress(float progress, string statusMessage = null)
        {
            _targetProgress = Mathf.Clamp01(progress);
            
            // Update status text
            if (!string.IsNullOrEmpty(statusMessage) && _statusText != null)
            {
                _statusText.text = statusMessage;
            }
            
            // Smooth progress animation
            if (_progressCoroutine != null)
                StopCoroutine(_progressCoroutine);
            _progressCoroutine = StartCoroutine(SmoothProgressCoroutine());
        }
        
        /// <summary>
        /// Set loading status text
        /// </summary>
        public void SetStatus(string statusMessage)
        {
            if (_statusText != null)
            {
                _statusText.text = statusMessage;
            }
        }
        
        /// <summary>
        /// Show completion state
        /// </summary>
        public void ShowComplete()
        {
            SetProgress(1f, "Hoàn thành!");
        }
        
        #endregion
        
        #region Private Animation Methods
        
        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration, System.Action onComplete = null)
        {
            if (_canvasGroup == null) yield break;
            
            _isAnimating = true;
            
            float elapsedTime = 0f;
            
            // Enable raycasts when fading in
            if (toAlpha > fromAlpha)
            {
                _canvasGroup.blocksRaycasts = true;
            }
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / duration;
                
                _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
                
                yield return null;
            }
            
            _canvasGroup.alpha = toAlpha;
            
            // Disable raycasts when faded out
            if (toAlpha <= fromAlpha)
            {
                _canvasGroup.blocksRaycasts = false;
            }
            
            _isAnimating = false;
            onComplete?.Invoke();
        }
        
        private void StartSpinnerAnimation()
        {
            if (_loadingSpinner == null) return;
            
            StopSpinnerAnimation();
            _spinnerCoroutine = StartCoroutine(SpinnerCoroutine());
        }
        
        private void StopSpinnerAnimation()
        {
            if (_spinnerCoroutine != null)
            {
                StopCoroutine(_spinnerCoroutine);
                _spinnerCoroutine = null;
            }
        }
        
        private IEnumerator SpinnerCoroutine()
        {
            while (true)
            {
                if (_loadingSpinner != null)
                {
                    _loadingSpinner.Rotate(0f, 0f, -_spinnerSpeed * Time.unscaledDeltaTime);
                }
                yield return null;
            }
        }
        
        private IEnumerator SmoothProgressCoroutine()
        {
            float startProgress = _currentProgress;
            float elapsedTime = 0f;
            
            while (elapsedTime < _progressSmoothDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / _progressSmoothDuration;
                
                _currentProgress = Mathf.Lerp(startProgress, _targetProgress, progress);
                
                // Update UI
                UpdateProgressUI();
                
                yield return null;
            }
            
            _currentProgress = _targetProgress;
            UpdateProgressUI();
        }
        
        private void UpdateProgressUI()
        {
            // Update progress bar
            if (_progressBar != null)
            {
                _progressBar.value = _currentProgress;
            }
            
            // Update progress text
            if (_progressText != null)
            {
                int percentage = Mathf.RoundToInt(_currentProgress * 100);
                _progressText.text = $"{percentage}%";
            }
        }
        
        private void ShowRandomTip()
        {
            if (_loadingTips.Length > 0 && _statusText != null)
            {
                int randomIndex = Random.Range(0, _loadingTips.Length);
                _statusText.text = _loadingTips[randomIndex];
            }
        }
        
        private void StopAllAnimations()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            
            if (_spinnerCoroutine != null)
            {
                StopCoroutine(_spinnerCoroutine);
                _spinnerCoroutine = null;
            }
            
            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
                _progressCoroutine = null;
            }
            
            _isAnimating = false;
        }
        
        #endregion
        
        #region Scene Manager Integration
        
        private void OnEnable()
        {
            // Subscribe to SceneManager events
            SceneManager.OnSceneLoadStarted += HandleSceneLoadStarted;
            SceneManager.OnSceneLoadProgress += HandleSceneLoadProgress;
            SceneManager.OnSceneLoadCompleted += HandleSceneLoadCompleted;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from SceneManager events
            SceneManager.OnSceneLoadStarted -= HandleSceneLoadStarted;
            SceneManager.OnSceneLoadProgress -= HandleSceneLoadProgress;
            SceneManager.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
        }
        
        private void HandleSceneLoadStarted(string sceneName, LoadSceneMode loadMode)
        {
            Show();
            SetStatus($"Đang tải {sceneName}...");
        }
        
        private void HandleSceneLoadProgress(string sceneName, float progress)
        {
            SetProgress(progress);
        }
        
        private void HandleSceneLoadCompleted(string sceneName, bool success)
        {
            if (success)
            {
                ShowComplete();
                // Hide after a brief delay
                StartCoroutine(HideAfterDelay(0.5f));
            }
            else
            {
                SetStatus("Lỗi tải scene!");
                StartCoroutine(HideAfterDelay(2f));
            }
        }
        
        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        
        [ContextMenu("Test Show")]
        private void TestShow()
        {
            Show();
        }
        
        [ContextMenu("Test Hide")]
        private void TestHide()
        {
            Hide();
        }
        
        [ContextMenu("Test Progress 50%")]
        private void TestProgress50()
        {
            SetProgress(0.5f, "Đang tải... 50%");
        }
        
        [ContextMenu("Test Complete")]
        private void TestComplete()
        {
            ShowComplete();
        }
        
        #endif
        
        #endregion
    }
}
