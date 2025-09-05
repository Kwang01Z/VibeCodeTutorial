using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UILoadingScreen - Loading screen component with animated elements
    /// Provides visual feedback during loading operations
    /// </summary>
    public class UILoadingScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _loadingIcon;
        [SerializeField] private Image _backgroundImage;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _iconRotationSpeed = 180f; // degrees per second
        [SerializeField] private bool _enableTextAnimation = true;
        [SerializeField] private string[] _loadingMessages = { "Loading", "Loading.", "Loading..", "Loading..." };
        [SerializeField] private float _textAnimationInterval = 0.5f;

        [Header("Progress Bar")]
        [SerializeField] private bool _showProgressBar = true;
        [SerializeField] private bool _smoothProgressFill = true;
        [SerializeField] private float _progressFillSpeed = 2f;

        [Header("Visual Settings")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private bool _blurBackground = false;

        #endregion

        #region Private Fields

        private bool _isVisible = false;
        private bool _isShowing = false;
        private Coroutine _textAnimationCoroutine;
        private Coroutine _iconRotationCoroutine;
        private float _currentProgress = 0f;
        private float _targetProgress = 0f;
        private string _currentLoadingMessage = "Loading...";

        #endregion

        #region Properties

        public bool IsVisible => _isVisible;
        public bool IsShowing => _isShowing;
        public float Progress => _currentProgress;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            SetupInitialState();
        }

        private void Update()
        {
            if (_isVisible && _smoothProgressFill)
            {
                UpdateProgressFill();
            }
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_loadingText == null)
                _loadingText = GetComponentInChildren<TextMeshProUGUI>();

            if (_progressBar == null)
                _progressBar = GetComponentInChildren<Slider>();

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
        }

        private void SetupInitialState()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true; // Block input during loading

            if (_backgroundImage != null)
            {
                _backgroundImage.color = _backgroundColor;
            }

            if (_progressBar != null)
            {
                _progressBar.value = 0f;
                _progressBar.gameObject.SetActive(_showProgressBar);
            }

            if (_progressText != null)
            {
                _progressText.gameObject.SetActive(_showProgressBar);
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region Public Methods

        public void Show(string message = "Loading...")
        {
            if (_isShowing) return;

            _currentLoadingMessage = message;
            _isShowing = true;
            
            gameObject.SetActive(true);
            StartCoroutine(ShowCoroutine());
        }

        public void Hide()
        {
            if (!_isVisible) return;

            StartCoroutine(HideCoroutine());
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
            
            if (!_smoothProgressFill && _progressBar != null)
            {
                _progressBar.value = _targetProgress;
                _currentProgress = _targetProgress;
            }

            UpdateProgressText();
        }

        public void SetLoadingMessage(string message)
        {
            _currentLoadingMessage = message;
            
            if (_loadingText != null && !_enableTextAnimation)
            {
                _loadingText.text = message;
            }
        }

        public void ShowWithProgress(string message = "Loading...", float initialProgress = 0f)
        {
            SetProgress(initialProgress);
            Show(message);
        }

        #endregion

        #region Show/Hide Logic

        private IEnumerator ShowCoroutine()
        {
            // Setup UI elements
            if (_loadingText != null)
            {
                _loadingText.text = _currentLoadingMessage;
            }

            if (_progressBar != null && _showProgressBar)
            {
                _progressBar.value = _currentProgress;
            }

            // Fade in
            yield return _canvasGroup.DOFade(1f, _fadeInDuration).WaitForCompletion();

            _isVisible = true;
            _isShowing = false;

            // Start animations
            StartAnimations();
        }

        private IEnumerator HideCoroutine()
        {
            _isVisible = false;

            // Stop animations
            StopAllAnimations();

            // Fade out
            yield return _canvasGroup.DOFade(0f, _fadeOutDuration).WaitForCompletion();

            gameObject.SetActive(false);
            _isShowing = false;
        }

        #endregion

        #region Animations

        private void StartAnimations()
        {
            if (_enableTextAnimation && _loadingText != null)
            {
                _textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
            }

            if (_loadingIcon != null)
            {
                _iconRotationCoroutine = StartCoroutine(RotateLoadingIcon());
            }
        }

        private void StopAllAnimations()
        {
            if (_textAnimationCoroutine != null)
            {
                StopCoroutine(_textAnimationCoroutine);
                _textAnimationCoroutine = null;
            }

            if (_iconRotationCoroutine != null)
            {
                StopCoroutine(_iconRotationCoroutine);
                _iconRotationCoroutine = null;
            }

            // Stop DOTween animations
            _canvasGroup?.DOKill();
            _loadingIcon?.transform.DOKill();
        }

        private IEnumerator AnimateLoadingText()
        {
            int messageIndex = 0;

            while (_isVisible)
            {
                if (_loadingText != null && _loadingMessages.Length > 0)
                {
                    _loadingText.text = _loadingMessages[messageIndex];
                    messageIndex = (messageIndex + 1) % _loadingMessages.Length;
                }

                yield return new WaitForSeconds(_textAnimationInterval);
            }
        }

        private IEnumerator RotateLoadingIcon()
        {
            if (_loadingIcon == null) yield break;

            while (_isVisible)
            {
                _loadingIcon.transform.Rotate(0f, 0f, _iconRotationSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        private void UpdateProgressFill()
        {
            if (_progressBar == null) return;

            if (Mathf.Abs(_currentProgress - _targetProgress) > 0.001f)
            {
                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.unscaledDeltaTime * _progressFillSpeed);
                _progressBar.value = _currentProgress;
                UpdateProgressText();
            }
        }

        private void UpdateProgressText()
        {
            if (_progressText != null)
            {
                int percentage = Mathf.RoundToInt(_currentProgress * 100f);
                _progressText.text = $"{percentage}%";
            }
        }

        #endregion

        #region Utility Methods

        public void SetBackgroundColor(Color color)
        {
            _backgroundColor = color;
            if (_backgroundImage != null)
            {
                _backgroundImage.color = color;
            }
        }

        public void SetIconRotationSpeed(float speed)
        {
            _iconRotationSpeed = speed;
        }

        public void SetTextAnimationSpeed(float interval)
        {
            _textAnimationInterval = interval;
        }

        #endregion

        #region Debug

        [ContextMenu("Test Show")]
        private void TestShow()
        {
            Show("Testing Loading Screen...");
        }

        [ContextMenu("Test Hide")]
        private void TestHide()
        {
            Hide();
        }

        [ContextMenu("Test Progress")]
        private void TestProgress()
        {
            StartCoroutine(TestProgressCoroutine());
        }

        private IEnumerator TestProgressCoroutine()
        {
            Show("Testing Progress...");
            
            for (float i = 0f; i <= 1f; i += 0.1f)
            {
                SetProgress(i);
                yield return new WaitForSeconds(0.2f);
            }
            
            yield return new WaitForSeconds(1f);
            Hide();
        }

        #endregion
    }
}
