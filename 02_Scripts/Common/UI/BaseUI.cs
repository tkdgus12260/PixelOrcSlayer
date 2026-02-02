using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class BaseUIData
    {
        public Action OnShow;
        public Action OnClose;
    }

    public class BaseUI : MonoBehaviour
    {
        public Animation _uIOpenAnim;

        private Action _onShow;
        private Action _onClose;

        public virtual void Init(Transform anchor)
        {
            Logger.Log($"{GetType()} init.");

            _onShow = null;
            _onClose = null;

            transform.SetParent(anchor);

            var rectTransform = GetComponent<RectTransform>();
            if (!rectTransform)
            {
                Logger.LogError("UI does not have rectransform.");
                return;
            }

            rectTransform.localPosition = new Vector3(0f, 0f, 0f);
            rectTransform.localScale = new Vector3(1f, 1f, 1f);
            rectTransform.offsetMin = new Vector2(0, 0);
            rectTransform.offsetMax = new Vector2(0, 0);
        }

        public virtual void SetInfo(BaseUIData uiData)
        {
            Logger.Log($"{GetType()} set info.");

            _onShow = uiData.OnShow;
            _onClose = uiData.OnClose;
        }

        public virtual void ShowUI()
        {
            if (_uIOpenAnim)
            {
                _uIOpenAnim.Play();
            }

            _onShow?.Invoke();
            _onShow = null;
        }

        public virtual void CloseUI(bool isCloseAll = false)
        {
            if (!isCloseAll)
            {
                _onClose?.Invoke();
            }

            _onClose = null;

            UIManager.Instance.CloseUI(this);
        }

        public virtual void OnClickCloseButton()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            CloseUI();
        }
    }
}