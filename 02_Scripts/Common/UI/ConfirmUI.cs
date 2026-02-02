using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvival
{
    public enum ConfirmType
    {
        OK,
        OK_CANCEL,
    }

    public class ConfirmUIData : BaseUIData
    {
        public ConfirmType ConfirmType;
        public string TitleTxt;
        public string DescTxt;
        public string OKBtnTxt;
        public Action OnClickOKBtn;
        public string CancelBtnTxt;
        public Action OnClickCancelBtn;
        public bool CloseFirst;
    }

    public class ConfirmUI : BaseUI
    {
        public TextMeshProUGUI TitleTxt;
        public TextMeshProUGUI DescTxt;
        public Button OKBtn;
        public Button CancelBtn;

        public TextMeshProUGUI OKBtnTxt;
        public TextMeshProUGUI CancelBtnTxt;

        private ConfirmUIData _confirmUIData;
        private Action _onClickOKBtn;
        private Action _onClickCancelBtn;
        
        private bool _closeFirst = false;

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            _confirmUIData = uiData as ConfirmUIData;

            TitleTxt.text = _confirmUIData.TitleTxt;
            DescTxt.text = _confirmUIData.DescTxt;
            OKBtnTxt.text = _confirmUIData.OKBtnTxt;
            _onClickOKBtn = _confirmUIData.OnClickOKBtn;
            CancelBtnTxt.text = _confirmUIData.CancelBtnTxt;
            _onClickCancelBtn = _confirmUIData.OnClickCancelBtn;
            _closeFirst =  _confirmUIData.CloseFirst;
            
            OKBtn.gameObject.SetActive(true);
            CancelBtn.gameObject.SetActive(_confirmUIData.ConfirmType == ConfirmType.OK_CANCEL);
        }

        public void OnClickOKBtn()
        {
            if (_closeFirst)
            {
                CloseUI();
                _onClickOKBtn?.Invoke();
                _onClickOKBtn = null;
            }
            else
            {
                _onClickOKBtn?.Invoke();
                _onClickOKBtn = null;
                CloseUI();
            }
        }

        public void OnClickCancelBtn()
        {
            if (_closeFirst)
            {
                CloseUI();
                _onClickCancelBtn?.Invoke();
                _onClickCancelBtn = null;
            }
            else
            {
                _onClickCancelBtn?.Invoke();
                _onClickCancelBtn = null;
                CloseUI();
            }
        }
    }
}