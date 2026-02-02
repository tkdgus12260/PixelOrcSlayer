using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelSurvival
{
    public class JoystickHandleUI : MonoBehaviour
    {
        public GameObject JoystickBg;
        public GameObject JoystickHandle;

        RectTransform _canvasRect;
        RectTransform _joyRect;
        Canvas _canvas;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas ? _canvas.transform as RectTransform : null;
            _joyRect = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (Touchscreen.current == null || _canvasRect == null || _joyRect == null) return;

            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRect,
                        touch.position.ReadValue(),
                        GetCanvasCamera(_canvas),
                        out var local))
                {
                    _joyRect.anchoredPosition = local;
                    JoystickBg.SetActive(true);
                    JoystickHandle.SetActive(true);
                }
            }

            if (touch.press.wasReleasedThisFrame)
            {
                JoystickBg.SetActive(false);
                JoystickHandle.SetActive(false);
            }
        }

        static Camera GetCanvasCamera(Canvas c)
        {
            if (c == null) return null;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return c.worldCamera;
        }
    }
}