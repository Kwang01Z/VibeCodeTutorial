using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessRunner.Core;

namespace EndlessRunner.Currency.UI
{
    /// <summary>
    /// Component để display currency amount trong UI với animation support
    /// </summary>
    public class CurrencyDisplayUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Loại currency để display")]
        private CurrencyType _currencyType = CurrencyType.Coins;

        [SerializeField]
        [Tooltip("Auto-find CurrencyManager nếu không assign")]
        private bool _autoFindCurrencyManager = true;

        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Text component để hiển thị số lượng")]
        private TextMeshProUGUI _amountText;

        [SerializeField]
        [Tooltip("Icon image cho currency")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("Background/frame image")]
        private Image _backgroundImage;

        [Header("Display Settings")]
        [SerializeField]
        [Tooltip("Use abbreviated format (1.5K instead of 1500)")]
        private bool _useAbbreviatedFormat = true;

        [SerializeField]
        [Tooltip("Prefix text (ví dụ: \"Coins: \")")]
        private string _prefix = "";

        [SerializeField]
        [Tooltip("Suffix text (ví dụ: \" GP\")")]
        private string _suffix = "";

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Enable value change animation")]
        private bool _enableAnimation = true;

        [SerializeField]
        [Tooltip("Animation duration cho value changes")]
        private float _animationDuration = 0.5f;

        [SerializeField]
        [Tooltip("Scale animation khi value thay đổi")]
        private bool _enableScaleAnimation = true;

        [SerializeField]
        [Tooltip("Scale multiplier cho bounce effect")]
        private float _scaleMultiplier = 1.2f;

        [SerializeField]
        [Tooltip("Color animation cho gain/loss")]
        private bool _enableColorAnimation = true;

        [SerializeField]
        [Tooltip("Color khi gain currency")]
        private Color _gainColor = Color.green;

        [SerializeField]
        [Tooltip("Color khi lose currency")]
        private Color _loseColor = Color.red;

        [SerializeField]
        [Tooltip("Default text color")]
        private Color _defaultColor = Color.white;

        // Private fields
        private ICurrencySystem _currencySystem;
        private long _currentDisplayedAmount = 0;
        private long _targetAmount = 0;
        private Coroutine _animationCoroutine;
        private Vector3 _originalScale;
        private Color _originalTextColor;

        // Animation state
        private bool _isAnimating = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache original values
            if (transform != null)
                _originalScale = transform.localScale;

            if (_amountText != null)
                _originalTextColor = _amountText.color;

            // Find currency manager if needed
            if (_autoFindCurrencyManager)
            {
                _currencySystem = FindObjectOfType<CurrencyManager>();
            }
        }

        private void Start()
        {
            // Setup UI
            SetupUI();

            // Subscribe to currency changes
            if (_currencySystem != null)
            {
                _currencySystem.OnCurrencyChanged += OnCurrencyChanged;
                
                // Initialize with current value
                _currentDisplayedAmount = _currencySystem.GetAmount(_currencyType);
                _targetAmount = _currentDisplayedAmount;
                UpdateDisplay(false); // No animation for initial value
            }
            else
            {
                Debug.LogWarning($"[CurrencyDisplayUI] No CurrencySystem found for {_currencyType}", this);
            }
        }

        private void OnDestroy()
        {
            // Cleanup
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            if (_currencySystem != null)
            {
                _currencySystem.OnCurrencyChanged -= OnCurrencyChanged;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set currency system manually (for dependency injection)
        /// </summary>
        public void SetCurrencySystem(ICurrencySystem currencySystem)
        {
            // Unsubscribe from old system
            if (_currencySystem != null)
            {
                _currencySystem.OnCurrencyChanged -= OnCurrencyChanged;
            }

            _currencySystem = currencySystem;

            // Subscribe to new system
            if (_currencySystem != null)
            {
                _currencySystem.OnCurrencyChanged += OnCurrencyChanged;
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Set currency type to display
        /// </summary>
        public void SetCurrencyType(CurrencyType type)
        {
            _currencyType = type;
            SetupUI();
            RefreshDisplay();
        }

        /// <summary>
        /// Force refresh display
        /// </summary>
        public void RefreshDisplay()
        {
            if (_currencySystem != null)
            {
                long currentAmount = _currencySystem.GetAmount(_currencyType);
                _currentDisplayedAmount = currentAmount;
                _targetAmount = currentAmount;
                UpdateDisplay(false);
            }
        }

        /// <summary>
        /// Set displayed amount manually (for testing)
        /// </summary>
        public void SetDisplayAmount(long amount, bool animate = true)
        {
            _targetAmount = amount;
            UpdateDisplay(animate);
        }

        #endregion

        #region Private Methods

        private void SetupUI()
        {
            if (_currencySystem == null) return;

            var config = _currencySystem.GetConfig(_currencyType);

            // Setup icon
            if (_iconImage != null && config.Icon != null)
            {
                _iconImage.sprite = config.Icon;
                _iconImage.color = config.Color;
            }

            // Setup background color
            if (_backgroundImage != null)
            {
                _backgroundImage.color = config.Color * 0.3f; // Darker version
            }

            // Setup default text color nếu chưa set
            if (_amountText != null && _defaultColor == Color.white)
            {
                _defaultColor = config.Color;
                _amountText.color = _defaultColor;
                _originalTextColor = _defaultColor;
            }
        }

        private void OnCurrencyChanged(CurrencyType type, long oldAmount, long newAmount, string reason)
        {
            if (type != _currencyType) return;

            _targetAmount = newAmount;
            UpdateDisplay(_enableAnimation);
        }

        private void UpdateDisplay(bool animate)
        {
            if (_amountText == null) return;

            if (animate && _enableAnimation && _currentDisplayedAmount != _targetAmount)
            {
                // Stop existing animation
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }

                _animationCoroutine = StartCoroutine(AnimateValueChange());
            }
            else
            {
                // Immediate update
                _currentDisplayedAmount = _targetAmount;
                UpdateAmountText();
            }
        }

        private IEnumerator AnimateValueChange()
        {
            _isAnimating = true;

            long startAmount = _currentDisplayedAmount;
            long targetAmount = _targetAmount;
            bool isGain = targetAmount > startAmount;

            // Color animation
            if (_enableColorAnimation && _amountText != null)
            {
                Color targetColor = isGain ? _gainColor : _loseColor;
                _amountText.color = targetColor;
            }

            // Scale animation start
            if (_enableScaleAnimation)
            {
                transform.localScale = _originalScale * _scaleMultiplier;
            }

            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _animationDuration;

                // Smooth progress curve
                progress = Mathf.SmoothStep(0f, 1f, progress);

                // Animate value
                _currentDisplayedAmount = (long)Mathf.Lerp(startAmount, targetAmount, progress);
                UpdateAmountText();

                // Scale animation (bounce back)
                if (_enableScaleAnimation)
                {
                    float scaleProgress = 1f - progress;
                    float scale = Mathf.Lerp(1f, _scaleMultiplier, scaleProgress);
                    transform.localScale = _originalScale * scale;
                }

                yield return null;
            }

            // Ensure final values
            _currentDisplayedAmount = targetAmount;
            UpdateAmountText();

            // Reset scale
            if (_enableScaleAnimation)
            {
                transform.localScale = _originalScale;
            }

            // Reset color
            if (_enableColorAnimation && _amountText != null)
            {
                _amountText.color = _originalTextColor;
            }

            _isAnimating = false;
            _animationCoroutine = null;
        }

        private void UpdateAmountText()
        {
            if (_amountText == null) return;

            string formattedAmount = CurrencyFormatter.FormatAmount(_currentDisplayedAmount, _useAbbreviatedFormat);
            _amountText.text = _prefix + formattedAmount + _suffix;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // Clamp values
            _animationDuration = Mathf.Max(0.1f, _animationDuration);
            _scaleMultiplier = Mathf.Max(1f, _scaleMultiplier);

            // Update display in editor (if playing)
            if (Application.isPlaying)
            {
                RefreshDisplay();
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Test Gain Animation")]
        private void TestGainAnimation()
        {
            if (Application.isPlaying)
            {
                SetDisplayAmount(_currentDisplayedAmount + 1000, true);
            }
        }

        [ContextMenu("Test Loss Animation")]
        private void TestLossAnimation()
        {
            if (Application.isPlaying)
            {
                SetDisplayAmount(System.Math.Max(0L, _currentDisplayedAmount - 500), true);
            }
        }

        [ContextMenu("Refresh Display")]
        private void EditorRefreshDisplay()
        {
            if (Application.isPlaying)
            {
                RefreshDisplay();
            }
        }
        #endif

        #endregion
    }
}
