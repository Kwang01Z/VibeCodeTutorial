using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessRunner.Data;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UINotification - Component for animated notification messages
    /// </summary>
    public class UINotification : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _closeButton;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _defaultBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _infoColor = Color.cyan;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private NotificationType _currentType;
        
        #endregion

        #region Properties

        public RectTransform RectTransform 
        { 
            get 
            { 
                if (_rectTransform == null) 
                    _rectTransform = GetComponent<RectTransform>(); 
                return _rectTransform; 
            } 
        }

        public CanvasGroup CanvasGroup 
        { 
            get 
            { 
                if (_canvasGroup == null) 
                    _canvasGroup = GetComponent<CanvasGroup>(); 
                return _canvasGroup; 
            } 
        }

        public NotificationType NotificationType => _currentType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            SetupCloseButton();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_messageText == null)
                _messageText = GetComponentInChildren<TextMeshProUGUI>();

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (_iconImage == null)
                _iconImage = transform.Find("Icon")?.GetComponent<Image>();

            if (_closeButton == null)
                _closeButton = GetComponentInChildren<Button>();

            // Ensure CanvasGroup exists
            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();
        }

        private void SetupCloseButton()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        #endregion

        #region Public Methods

        public void SetMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }

        public void SetColor(Color color)
        {
            if (_messageText != null)
            {
                _messageText.color = color;
            }
        }

        public void SetType(NotificationType type)
        {
            _currentType = type;
            ApplyTypeVisuals(type);
        }

        public void SetIcon(Sprite iconSprite)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = iconSprite;
                _iconImage.gameObject.SetActive(iconSprite != null);
            }
        }

        #endregion

        #region Private Methods

        private void ApplyTypeVisuals(NotificationType type)
        {
            Color backgroundColor = _defaultBackgroundColor;
            
            switch (type)
            {
                case NotificationType.Success:
                case NotificationType.EffectStarted:
                    backgroundColor = _successColor * 0.3f;
                    backgroundColor.a = 0.9f;
                    break;
                    
                case NotificationType.Warning:
                    backgroundColor = _warningColor * 0.3f;
                    backgroundColor.a = 0.9f;
                    break;
                    
                case NotificationType.Error:
                    backgroundColor = _errorColor * 0.3f;
                    backgroundColor.a = 0.9f;
                    break;
                    
                case NotificationType.Info:
                case NotificationType.EffectEnded:
                case NotificationType.EffectStacked:
                    backgroundColor = _infoColor * 0.3f;
                    backgroundColor.a = 0.9f;
                    break;
                    
                case NotificationType.Achievement:
                    backgroundColor = Color.yellow * 0.3f;
                    backgroundColor.a = 0.9f;
                    break;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = backgroundColor;
            }
        }

        private void OnCloseButtonClicked()
        {
            // This would trigger the removal animation
            // The UIAnimationManager would handle the actual removal
            gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// UIFloatingText - Component for animated floating text effects
    /// </summary>
    public class UIFloatingText : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Outline _textOutline;
        [SerializeField] private Shadow _textShadow;

        [Header("Visual Settings")]
        [SerializeField] private Font _defaultFont;
        [SerializeField] private float _defaultFontSize = 24f;
        [SerializeField] private FontStyles _defaultFontStyle = FontStyles.Bold;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        #endregion

        #region Properties

        public RectTransform RectTransform 
        { 
            get 
            { 
                if (_rectTransform == null) 
                    _rectTransform = GetComponent<RectTransform>(); 
                return _rectTransform; 
            } 
        }

        public CanvasGroup CanvasGroup 
        { 
            get 
            { 
                if (_canvasGroup == null) 
                    _canvasGroup = GetComponent<CanvasGroup>(); 
                return _canvasGroup; 
            } 
        }

        public TextMeshProUGUI TextComponent => _text;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            SetupDefaultStyling();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_text == null)
                _text = GetComponent<TextMeshProUGUI>();

            if (_text == null)
                _text = GetComponentInChildren<TextMeshProUGUI>();

            if (_textOutline == null)
                _textOutline = _text?.GetComponent<Outline>();

            if (_textShadow == null)
                _textShadow = _text?.GetComponent<Shadow>();

            // Ensure CanvasGroup exists
            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();
        }

        private void SetupDefaultStyling()
        {
            if (_text != null)
            {
                _text.fontSize = _defaultFontSize;
                _text.fontStyle = _defaultFontStyle;
                _text.alignment = TextAlignmentOptions.Center;
            }

            // Setup outline for better visibility
            if (_textOutline != null)
            {
                _textOutline.effectColor = Color.black;
                _textOutline.effectDistance = new Vector2(1, -1);
            }
        }

        #endregion

        #region Public Methods

        public void SetText(string text)
        {
            if (_text != null)
            {
                _text.text = text;
            }
        }

        public void SetColor(Color color)
        {
            if (_text != null)
            {
                _text.color = color;
            }
        }

        public void SetFontSize(float fontSize)
        {
            if (_text != null)
            {
                _text.fontSize = fontSize;
            }
        }

        public void SetFontStyle(FontStyles fontStyle)
        {
            if (_text != null)
            {
                _text.fontStyle = fontStyle;
            }
        }

        public void SetOutlineColor(Color outlineColor)
        {
            if (_textOutline != null)
            {
                _textOutline.effectColor = outlineColor;
            }
        }

        public void EnableOutline(bool enable)
        {
            if (_textOutline != null)
            {
                _textOutline.enabled = enable;
            }
        }

        #endregion
    }

    /// <summary>
    /// UIEffectIcon - Component for animated effect icon displays
    /// </summary>
    public class UIEffectIcon : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _stackCountText;
        [SerializeField] private Image _glowEffect;

        [Header("Visual Settings")]
        [SerializeField] private Color _defaultBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private AnimationCurve _glowPulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _glowPulseSpeed = 2f;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private ItemType _itemType;
        private int _stackCount = 1;
        private bool _isGlowing = false;

        #endregion

        #region Properties

        public RectTransform RectTransform 
        { 
            get 
            { 
                if (_rectTransform == null) 
                    _rectTransform = GetComponent<RectTransform>(); 
                return _rectTransform; 
            } 
        }

        public CanvasGroup CanvasGroup 
        { 
            get 
            { 
                if (_canvasGroup == null) 
                    _canvasGroup = GetComponent<CanvasGroup>(); 
                return _canvasGroup; 
            } 
        }

        public ItemType ItemType => _itemType;
        public int StackCount => _stackCount;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            if (_isGlowing)
            {
                UpdateGlowEffect();
            }
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_iconImage == null)
                _iconImage = GetComponent<Image>();

            if (_backgroundImage == null)
                _backgroundImage = transform.Find("Background")?.GetComponent<Image>();

            if (_stackCountText == null)
                _stackCountText = GetComponentInChildren<TextMeshProUGUI>();

            if (_glowEffect == null)
                _glowEffect = transform.Find("Glow")?.GetComponent<Image>();

            // Ensure CanvasGroup exists
            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();
        }

        #endregion

        #region Public Methods

        public void SetIcon(Sprite iconSprite)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = iconSprite;
            }
        }

        public void SetColor(Color color)
        {
            if (_iconImage != null)
            {
                _iconImage.color = color;
            }

            // Tint background slightly
            if (_backgroundImage != null)
            {
                Color bgColor = Color.Lerp(_defaultBackgroundColor, color, 0.3f);
                bgColor.a = _defaultBackgroundColor.a;
                _backgroundImage.color = bgColor;
            }

            // Update glow effect
            if (_glowEffect != null)
            {
                Color glowColor = color;
                glowColor.a = 0.5f;
                _glowEffect.color = glowColor;
            }
        }

        public void SetItemType(ItemType itemType)
        {
            _itemType = itemType;
        }

        public void SetStackCount(int stackCount)
        {
            _stackCount = stackCount;
            UpdateStackCountDisplay();
        }

        public void EnableGlow(bool enable)
        {
            _isGlowing = enable;
            
            if (_glowEffect != null)
            {
                _glowEffect.gameObject.SetActive(enable);
            }
        }

        public void SetBackgroundColor(Color backgroundColor)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = backgroundColor;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateStackCountDisplay()
        {
            if (_stackCountText != null)
            {
                if (_stackCount > 1)
                {
                    _stackCountText.text = _stackCount.ToString();
                    _stackCountText.gameObject.SetActive(true);
                }
                else
                {
                    _stackCountText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateGlowEffect()
        {
            if (_glowEffect == null) return;

            float pulseValue = _glowPulseCurve.Evaluate((Time.unscaledTime * _glowPulseSpeed) % 1f);
            Color currentColor = _glowEffect.color;
            currentColor.a = pulseValue * 0.8f; // Max alpha of 0.8
            _glowEffect.color = currentColor;

            // Scale effect
            float scaleMultiplier = 1f + (pulseValue * 0.1f);
            _glowEffect.transform.localScale = Vector3.one * scaleMultiplier;
        }

        #endregion
    }

    /// <summary>
    /// UIProgressBar - Advanced animated progress bar component
    /// </summary>
    public class UIProgressBar : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _overlayImage;
        [SerializeField] private TextMeshProUGUI _valueText;

        [Header("Visual Settings")]
        [SerializeField] private Color _lowValueColor = Color.red;
        [SerializeField] private Color _midValueColor = Color.yellow;
        [SerializeField] private Color _highValueColor = Color.green;
        [SerializeField] private bool _useColorGradient = true;
        [SerializeField] private bool _showValueText = true;

        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool _smoothFill = true;

        #endregion

        #region Private Fields

        private float _currentValue = 1f;
        private float _maxValue = 1f;
        private float _displayValue = 1f;

        #endregion

        #region Properties

        public float CurrentValue => _currentValue;
        public float MaxValue => _maxValue;
        public float NormalizedValue => _currentValue / _maxValue;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            if (_smoothFill)
            {
                UpdateSmoothFill();
            }
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_fillImage == null)
                _fillImage = transform.Find("Fill")?.GetComponent<Image>();

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (_overlayImage == null)
                _overlayImage = transform.Find("Overlay")?.GetComponent<Image>();

            if (_valueText == null)
                _valueText = GetComponentInChildren<TextMeshProUGUI>();
        }

        #endregion

        #region Public Methods

        public void SetValue(float value, bool animate = true)
        {
            float targetValue = Mathf.Clamp(value, 0f, _maxValue);
            
            if (animate)
            {
                StartCoroutine(AnimateToValue(targetValue));
            }
            else
            {
                _currentValue = targetValue;
                _displayValue = targetValue;
                UpdateVisuals();
            }
        }

        public void SetMaxValue(float maxValue)
        {
            _maxValue = Mathf.Max(maxValue, 0.001f); // Prevent division by zero
            UpdateVisuals();
        }

        public void SetColors(Color lowColor, Color midColor, Color highColor)
        {
            _lowValueColor = lowColor;
            _midValueColor = midColor;
            _highValueColor = highColor;
            UpdateVisuals();
        }

        public void SetProgress(float normalizedProgress, bool animate = true)
        {
            SetValue(normalizedProgress * _maxValue, animate);
        }

        #endregion

        #region Private Methods

        private System.Collections.IEnumerator AnimateToValue(float targetValue)
        {
            float startValue = _currentValue;
            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _animationDuration;
                float curveValue = _animationCurve.Evaluate(t);
                
                _currentValue = Mathf.Lerp(startValue, targetValue, curveValue);
                UpdateVisuals();
                
                yield return null;
            }

            _currentValue = targetValue;
            UpdateVisuals();
        }

        private void UpdateSmoothFill()
        {
            if (Mathf.Abs(_displayValue - _currentValue) > 0.001f)
            {
                _displayValue = Mathf.Lerp(_displayValue, _currentValue, Time.unscaledDeltaTime * 5f);
                UpdateFillAmount();
            }
        }

        private void UpdateVisuals()
        {
            UpdateFillAmount();
            UpdateFillColor();
            UpdateValueText();
        }

        private void UpdateFillAmount()
        {
            if (_fillImage != null)
            {
                float fillAmount = _smoothFill ? (_displayValue / _maxValue) : NormalizedValue;
                _fillImage.fillAmount = fillAmount;
            }
        }

        private void UpdateFillColor()
        {
            if (_fillImage == null || !_useColorGradient) return;

            float normalizedValue = NormalizedValue;
            Color targetColor;

            if (normalizedValue <= 0.5f)
            {
                targetColor = Color.Lerp(_lowValueColor, _midValueColor, normalizedValue * 2f);
            }
            else
            {
                targetColor = Color.Lerp(_midValueColor, _highValueColor, (normalizedValue - 0.5f) * 2f);
            }

            _fillImage.color = targetColor;
        }

        private void UpdateValueText()
        {
            if (_valueText != null && _showValueText)
            {
                string valueString = $"{_currentValue:F0} / {_maxValue:F0}";
                _valueText.text = valueString;
            }
        }

        #endregion
    }
}
