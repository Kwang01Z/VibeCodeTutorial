using UnityEngine;
using UnityEngine.UI;

namespace EndlessRunner.UI
{
    /// <summary>
    /// UIScreen - Base class for all UI screens in the menu system
    /// Provides common functionality for screen management and transitions
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Screen Settings")]
        [SerializeField] private string _screenName;
        [SerializeField] private ScreenTransitionType _preferredTransitionType = ScreenTransitionType.Default;
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private bool _canGoBack = true;
        [SerializeField] private bool _deactivateOnHide = true;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rectTransform;

        #endregion

        #region Private Fields

        private UIMenuTransitionManager _transitionManager;
        private bool _isVisible = false;
        private bool _isInitialized = false;

        #endregion

        #region Properties

        public string ScreenName => _screenName;
        public ScreenTransitionType PreferredTransitionType => _preferredTransitionType;
        public Sprite BackgroundSprite => _backgroundSprite;
        public bool CanGoBack => _canGoBack;
        public bool IsVisible => _isVisible;
        public bool IsInitialized => _isInitialized;

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

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            CacheComponents();
        }

        protected virtual void Start()
        {
            if (!_isInitialized)
            {
                Initialize(null);
            }
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Set default screen name if not provided
            if (string.IsNullOrEmpty(_screenName))
                _screenName = gameObject.name;
        }

        public virtual void Initialize(UIMenuTransitionManager transitionManager)
        {
            if (_isInitialized) return;

            _transitionManager = transitionManager;
            SetupInitialState();
            OnInitialize();
            _isInitialized = true;
        }

        public virtual void SetupInitialState()
        {
            // Ensure proper initial state
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // Set initial position
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = Vector2.zero;
                _rectTransform.localScale = Vector3.one;
            }

            _isVisible = false;
        }

        #endregion

        #region Screen Lifecycle

        public virtual void PrepareForShow(object data = null)
        {
            gameObject.SetActive(true);
            OnPrepareForShow(data);
        }

        public virtual void Show(bool immediate = false)
        {
            _isVisible = true;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                
                if (immediate)
                {
                    _canvasGroup.alpha = 1f;
                }
            }

            OnShow();
        }

        public virtual void Hide(bool immediate = false)
        {
            _isVisible = false;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                
                if (immediate)
                {
                    _canvasGroup.alpha = 0f;
                }
            }

            OnHide();

            if (_deactivateOnHide && immediate)
            {
                gameObject.SetActive(false);
            }
        }

        public virtual void OnShown()
        {
            OnScreenShown();
        }

        public virtual void OnHidden()
        {
            OnScreenHidden();
            
            if (_deactivateOnHide)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Virtual Methods (Override in derived classes)

        protected virtual void OnInitialize() { }
        protected virtual void OnPrepareForShow(object data) { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnScreenShown() { }
        protected virtual void OnScreenHidden() { }

        public virtual void OnBackPressed()
        {
            // Default behavior - close screen or go back
            if (_transitionManager != null && _canGoBack)
            {
                _transitionManager.GoBack();
            }
        }

        #endregion

        #region Utility Methods

        protected void ShowScreen(string screenName, object data = null)
        {
            if (_transitionManager != null)
            {
                _transitionManager.ShowScreen(screenName, true, data);
            }
        }

        protected void HideCurrentScreen()
        {
            if (_transitionManager != null)
            {
                _transitionManager.HideCurrentScreen();
            }
        }

        protected bool GoBack()
        {
            return _transitionManager?.GoBack() ?? false;
        }

        #endregion

        #region Debug

        [ContextMenu("Show This Screen")]
        private void DebugShowScreen()
        {
            if (_transitionManager != null)
            {
                _transitionManager.ShowScreen(_screenName);
            }
            else
            {
                Show(true);
            }
        }

        [ContextMenu("Hide This Screen")]
        private void DebugHideScreen()
        {
            Hide(true);
        }

        #endregion
    }
}
