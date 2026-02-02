using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class UIManager : SingletonBehaviour<UIManager>
    {
        public Transform UICanvasTrs;
        public Transform ClosedUITrs;

        public Image FadeImg;

        private BaseUI _frontUI;
        private GoodsUI _goodsUI;

        private Dictionary<Type, GameObject> _openUIPool = new Dictionary<Type, GameObject>();
        private Dictionary<Type, GameObject> _closedUIPool = new Dictionary<Type, GameObject>();

        [SerializeField] private TextMeshProUGUI FPSTxt;
        
        [Header("FPS Display")]
        private bool _showFPS = true;
        private float _fpsUpdateInterval = 0.25f;

        private float _fpsElapsed;
        private int _fpsFrameCount;
        private void Update()
        {
            if (!_showFPS || FPSTxt == null) return;

            _fpsElapsed += Time.unscaledDeltaTime;
            _fpsFrameCount++;

            if (_fpsElapsed < _fpsUpdateInterval) return;

            float fps = _fpsFrameCount / Mathf.Max(_fpsElapsed, 0.0001f);
            float ms = 1000f / Mathf.Max(fps, 0.0001f);

            FPSTxt.text = $"FPS: {fps:0.0} (ms: {ms:0.0})";

            _fpsElapsed = 0f;
            _fpsFrameCount = 0;
        }

        protected override void Init()
        {
            base.Init();

            FadeImg.transform.localScale = Vector3.zero;

            _goodsUI = FindObjectOfType<GoodsUI>();
            if (!_goodsUI)
            {
                Logger.LogError($"No goods UI found.");
            }
        }

        public async void OpenUIFromAA<T>(BaseUIData uiData)
        {
            Type uiType = typeof(T);

            if (_openUIPool.ContainsKey(uiType))
            {
                Logger.Log($"{uiType} already opened.");
                return;
            }
            else if(_closedUIPool.ContainsKey(uiType))
            {
                var ui = _closedUIPool[uiType].GetComponent<BaseUI>();
                _closedUIPool.Remove(uiType);
                
                var siblingIndex = UICanvasTrs.childCount - 2;
                ui.Init(UICanvasTrs);
                ui.transform.SetSiblingIndex(siblingIndex);
                ui.gameObject.SetActive(true);
                ui.SetInfo(uiData);
                ui.ShowUI();

                _frontUI = ui;
                _openUIPool[uiType] = ui.gameObject;
            }
            else
            {
                AsyncOperationHandle<GameObject> operationHandler = Addressables.InstantiateAsync(uiType.Name);
                await operationHandler.Task;

                if (operationHandler.Status == AsyncOperationStatus.Succeeded)
                {
                    Logger.Log($"{uiType} Load Asset Success");
                    
                    GameObject uiObj = operationHandler.Result;
                    var ui = uiObj.GetComponent<BaseUI>();
                    if (!ui)
                    {
                        Logger.LogError($"{uiType} does not exist.");
                        return;
                    }
                    
                    var siblingIndex = UICanvasTrs.childCount - 2;
                    ui.Init(UICanvasTrs);
                    ui.transform.SetSiblingIndex(siblingIndex);
                    ui.gameObject.SetActive(true);
                    ui.SetInfo(uiData);
                    ui.ShowUI();

                    _frontUI = ui;
                    _openUIPool[uiType] = ui.gameObject;
                }
                else
                {
                    Logger.LogError($"{uiType} Load Asset Failed");
                }
            }
        }

        public void CloseUI(BaseUI ui)
        {
            Type uiType = ui.GetType();

            Logger.Log($"{GetType()}::CloseUI({uiType})");

            ui.gameObject.SetActive(false);
            _openUIPool.Remove(uiType);
            _closedUIPool[uiType] = ui.gameObject;
            ui.transform.SetParent(ClosedUITrs);

            _frontUI = null;
            var lastChild = UICanvasTrs.GetChild(UICanvasTrs.childCount - 3);
            if (lastChild)
            {
                _frontUI = lastChild.gameObject.GetComponent<BaseUI>();
            }
        }

        public BaseUI GetActiveUI<T>()
        {
            var uiType = typeof(T);
            return _openUIPool.ContainsKey(uiType) ? _openUIPool[uiType].GetComponent<BaseUI>() : null;
        }

        public bool ExistsOpenUI()
        {
            return _frontUI != null;
        }

        public BaseUI GetCurrentFrontUI()
        {
            return _frontUI;
        }

        public void CloseCurrentFrontUI()
        {
            _frontUI.CloseUI();
        }

        public void CloseAllOpenUI()
        {
            while (_frontUI)
            {
                _frontUI.CloseUI(true);
            }
        }

        public void EnableGoodsUI(bool value)
        {
            _goodsUI.gameObject.SetActive(value);

            if (value)
            {
                _goodsUI.SetValues();
            }
        }

        public void Fade(Color color, float startAlpha, float endAlpha, float duration, float startDelay,
            bool deactivateOnFinish, Action onFinish = null)
        {
            StartCoroutine(FadeCo(color, startAlpha, endAlpha, duration, startDelay, deactivateOnFinish, onFinish));
        }

        private IEnumerator FadeCo(Color color, float startAlpha, float endAlpha, float duration, float startDelay,
            bool deactivateOnFinish, Action onFinish)
        {
            yield return new WaitForSeconds(startDelay);

            FadeImg.transform.localScale = Vector3.one;
            FadeImg.color = new Color(color.r, color.g, color.b, startAlpha);

            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < duration)
            {
                FadeImg.color = new Color(color.r, color.g, color.b,
                    Mathf.Lerp(startAlpha, endAlpha, (Time.realtimeSinceStartup - startTime) / duration));
                yield return null;
            }

            FadeImg.color = new Color(color.r, color.g, color.b, endAlpha);

            if (deactivateOnFinish)
            {
                FadeImg.transform.localScale = Vector3.zero;
            }

            onFinish?.Invoke();
        }
    }
}