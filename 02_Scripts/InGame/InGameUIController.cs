using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class InGameUIController : MonoBehaviour
    {
        public Slider HpSlider;
        public TextMeshProUGUI HpTxt;
        
        public GameObject BossHpBar;
        public Slider BossHpSlider;
        public TextMeshProUGUI BossTxt;
        public TextMeshProUGUI BossHpTxt;
        
        [SerializeField] private BossOffscreenArrow _bossOffscreenArrow;
        private BaseEnemy _bossEnemy;
        
        public void Init()
        {
            UIManager.Instance.EnableGoodsUI(false);
            BossHpBar.SetActive(false);

            if (_bossOffscreenArrow == null)
            {
                _bossOffscreenArrow = FindObjectOfType<BossOffscreenArrow>();
            }
        }

        public void InitBoss(BaseEnemy bossEnemy, string enemyName, string enemyAddress)
        {
            if (BossHpBar != null)
            {
                BossHpBar.SetActive(true);
                _bossEnemy = bossEnemy;
                BossTxt.text = enemyName;
            }

            if (_bossOffscreenArrow != null)
            {
                _bossOffscreenArrow.SetInfo(enemyAddress);
                _bossOffscreenArrow.Show(bossEnemy.gameObject.transform);
            }
        }

        // public void PlayerHpUpdate()
        // {
        //     if (HpSlider != null)
        //     {
        //         if (InGameManager.Instance.Player != null)
        //         {
        //             HpSlider.value = InGameManager.Instance.Player.CurrentHpPercent;
        //             HpTxt.text = $"{InGameManager.Instance.Player.CurrentHp} / {InGameManager.Instance.Player.MaxHP}";
        //         }
        //     }
        // }
        //
        // public void BossHpUpdate()
        // {
        //     if (_bossEnemy)
        //     {
        //         if (BossHpSlider != null)
        //         {
        //             BossHpSlider.value = _bossEnemy.CurrentHpPercent;
        //             BossHpTxt.text = $"{_bossEnemy.CurrentHp} / {_bossEnemy.MaxHP}";
        //         }
        //     }
        // }
        
        private void Update()
        {
            if (HpSlider != null)
            {
                if (InGameManager.Instance.Player != null)
                {
                    HpSlider.value = InGameManager.Instance.Player.CurrentHpPercent;
                    HpTxt.text = $"{InGameManager.Instance.Player.CurrentHp} / {InGameManager.Instance.Player.MaxHP}";
                }
            }

            if (_bossEnemy)
            {
                if (BossHpSlider != null)
                {
                    BossHpSlider.value = _bossEnemy.CurrentHpPercent;
                    BossHpTxt.text = $"{_bossEnemy.CurrentHp} / {_bossEnemy.MaxHP}";
                }
            }
        }

        // private void OnApplicationFocus(bool hasFocus)
        // {
        //     if (!hasFocus)
        //     {
        //         if (!InGameManager.Instance.IsPaused)
        //         {
        //             var uiData = new BaseUIData();
        //             UIManager.Instance.OpenUI<PauseUI>(uiData);
        //             
        //             InGameManager.Instance.GamePause();
        //         }
        //     }
        // }

        public void OnClickPauseBtn()
        {
            Logger.Log($"{GetType()}::OnClickPauseBtn");
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<PauseUI>(uiData);
            
            InGameManager.Instance.GamePause();
        }
    }
}