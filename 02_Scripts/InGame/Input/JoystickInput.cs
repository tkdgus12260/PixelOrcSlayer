using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelSurvival
{
    public class JoystickInput : SingletonBehaviour<JoystickInput>
    {
        public Vector2 MoveInput => _moveInput;

        private Vector2 _startTouchPosScreen;
        private Vector2 _bgCenterScreenPos;
        private bool _isTouching = false;
        private Vector2 _moveInput = Vector2.zero;

        [SerializeField] private RectTransform _bg;

        [SerializeField] private RectTransform _handle;
        [SerializeField] private Canvas _uiCanvas;

        [SerializeField] private bool _floating = true;
        [SerializeField] private bool _followWhenOutside = false;
        [SerializeField] private bool _hideUIWhenIdle = true;
        [SerializeField] private float _radiusPixels = 160f;

        private RectTransform _bgParentRT;

        const float EPS = 0.0001f;

        
        protected override void Init()
        {
            isDestroyOnLoad = true;
            
            base.Init();
            
            if (!_uiCanvas) _uiCanvas = _bg ? _bg.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();

            _bgParentRT = _bg ? _bg.parent as RectTransform : null;

            if (_radiusPixels <= 0f && _bg)
                _radiusPixels = Mathf.Min(_bg.rect.width, _bg.rect.height) * 0.5f;

            if (_hideUIWhenIdle)
            {
                if (_bg) _bg.gameObject.SetActive(false);
                if (_handle) _handle.anchoredPosition = Vector2.zero;
            }
        }
        
        ///////////////////// Enemy Die Test Method /////////////////////
        private void OnEsc(InputValue value)
        {
            Logger.Log($"{GetType()}::OnEsc");
            var enemies = InGameManager.Instance.Enemies;
            if (enemies.Count > 0)
            {
                enemies[0].OnDie();
            }
        }
        /////////////////////////////////////////////////////////////////
        
        private void Update()
        {
            if (GameManager.Instance.IsPaused)
            {
                if (_bg) _bg.gameObject.SetActive(false);
                if (_handle) _handle.anchoredPosition = Vector2.zero;
                return;   
            }
            
            var touch = Touchscreen.current?.primaryTouch;
            if (touch == null) return;

            if (touch.press.wasPressedThisFrame && touch.position.ReadValue().y >= Screen.height * 0.8f)
            {
                _isTouching = false;
                _moveInput = Vector2.zero;

                if (_hideUIWhenIdle)
                {
                    if (_bg) _bg.gameObject.SetActive(false);
                    if (_handle) _handle.anchoredPosition = Vector2.zero;
                }
                return;
            }
            
            if (touch.press.wasPressedThisFrame)
            {
                _startTouchPosScreen = touch.position.ReadValue();
                _isTouching = true;

                if (_bg && _bgParentRT)
                {
                    if (_floating)
                    {
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                _bgParentRT, _startTouchPosScreen, null, out var local))
                        {
                            _bg.anchoredPosition = local;
                        }
                    }

                    if (_hideUIWhenIdle) _bg.gameObject.SetActive(true);
                }

                _bgCenterScreenPos = RectTransformUtility.WorldToScreenPoint(null, _bg.TransformPoint(_bg.rect.center));

                if (_handle) _handle.anchoredPosition = Vector2.zero;
                _moveInput = Vector2.zero;
            }

            if (_isTouching && touch.press.isPressed)
            {
                Vector2 currentScreen = touch.position.ReadValue();
                Vector2 deltaScreen = currentScreen - _bgCenterScreenPos;

                if (_followWhenOutside && deltaScreen.sqrMagnitude > _radiusPixels * _radiusPixels)
                {
                    Vector2 dir = deltaScreen.normalized;
                    _bgCenterScreenPos = currentScreen - dir * _radiusPixels;
                    deltaScreen = currentScreen - _bgCenterScreenPos;
                }

                float scale = _uiCanvas ? _uiCanvas.scaleFactor : 1f;
                float radiusCanvas = _radiusPixels / Mathf.Max(scale, 0.0001f);
                Vector2 deltaCanvas = deltaScreen / Mathf.Max(scale, 0.0001f);
                Vector2 clampedCanvas = Vector2.ClampMagnitude(deltaCanvas, radiusCanvas);

                if (_handle) _handle.anchoredPosition = clampedCanvas;

                Vector2 norm = (radiusCanvas > 0f) ? (clampedCanvas / radiusCanvas) : Vector2.zero;
                float mag = norm.magnitude;

                if (mag <= EPS)
                    _moveInput = Vector2.zero;
                else
                    _moveInput = norm.normalized;
            }
            else if (touch.press.wasReleasedThisFrame)
            {
                _moveInput = Vector2.zero;
                _isTouching = false;

                if (_hideUIWhenIdle && _bg) _bg.gameObject.SetActive(false);
                if (_handle) _handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}