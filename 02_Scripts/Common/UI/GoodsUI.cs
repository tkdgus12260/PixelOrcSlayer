using System;
using System.Collections;
using System.Collections.Generic;
using SuperMaxim.Messaging;
using TMPro;
using UnityEngine;

namespace PixelSurvival
{
    public class GoldUpdateMsg
    {
        public bool IsAdd;
    }

    public class GemUpdateMsg
    {
        public bool IsAdd;
    }
    
    public class GoodsUI : MonoBehaviour
    {
        public TextMeshProUGUI GoldAmountTxt;
        public TextMeshProUGUI GemAmountTxt;

        private Coroutine _goldIncreaseCo;
        private Coroutine _gemIncreaseCo;
        private const float GOOSDS_INCREASE_DURATION = 0.5f;

        private void OnEnable()
        {
            Messenger.Default.Subscribe<GoldUpdateMsg>(OnUpdateGold);
            Messenger.Default.Subscribe<GemUpdateMsg>(OnUpdateGem);
        }

        private void OnDisable()
        {
            Messenger.Default.Unsubscribe<GoldUpdateMsg>(OnUpdateGold);
            Messenger.Default.Unsubscribe<GemUpdateMsg>(OnUpdateGem);
        }

        public void SetValues()
        {
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("No user goods data");
            }
            else
            {
                GoldAmountTxt.text = userGoodsData.Gold.ToString("N0");
                GemAmountTxt.text = userGoodsData.Gem.ToString("N0");
            }
        }

        private void OnUpdateGold(GoldUpdateMsg msg)
        {
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("UserGoodsData does not exist.");
                return;
            }
            

            if (msg.IsAdd)
            {
                if (_goldIncreaseCo != null)
                {
                    StopCoroutine(_goldIncreaseCo);
                }

                AudioManager.Instance.PlaySFX(SFX.ui_get);
                _goldIncreaseCo = StartCoroutine(InCreaseGoldCo());
            }
            else
            {
                GoldAmountTxt.text = userGoodsData.Gold.ToString("N0");
            }
        }

        private IEnumerator InCreaseGoldCo()
        {
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("UserGoodsData does not exist.");
                yield break;
            }
            
            AudioManager.Instance.PlaySFX(SFX.ui_increase);

            var elapsedTime = 0f;
            var currTextValue = Convert.ToInt64(GoldAmountTxt.text.Replace(",", ""));
            var destValue = userGoodsData.Gold;
            
            while (elapsedTime < GOOSDS_INCREASE_DURATION)
            {
                var currValue = Mathf.Lerp(currTextValue, destValue, elapsedTime / GOOSDS_INCREASE_DURATION);
                GoldAmountTxt.text = currValue.ToString("N0");
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            GoldAmountTxt.text = destValue.ToString("N0");
        }

        private void OnUpdateGem(GemUpdateMsg msg)
        {
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("UserGoodsData does not exist.");
                return;
            }
            
            AudioManager.Instance.PlaySFX(SFX.ui_get);

            if (msg.IsAdd)
            {
                if (_gemIncreaseCo != null)
                {
                    StopCoroutine(_gemIncreaseCo);
                }

                _gemIncreaseCo = StartCoroutine(IncreaseGemCo());
            }
            else
            {
                GemAmountTxt.text = userGoodsData.Gem.ToString("N0");
            }
        }

        private IEnumerator IncreaseGemCo()
        {
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("UserGoodsData does not exist.");
                yield break;
            }
            
            AudioManager.Instance.PlaySFX(SFX.ui_increase);
            
            var elapsedTime = 0f;
            var currTextValue = Convert.ToInt64(GemAmountTxt.text.Replace(",", ""));
            var destValue = userGoodsData.Gem;

            while (elapsedTime < GOOSDS_INCREASE_DURATION)
            {
                var currValue = Mathf.Lerp(currTextValue, destValue, elapsedTime / GOOSDS_INCREASE_DURATION);
                GemAmountTxt.text = currValue.ToString("N0");
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            GemAmountTxt.text = destValue.ToString("N0");
        }
        
        public void OnClickShopBtn()
        {
            Logger.Log($"{GetType()}::OnClickShopBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<ShopUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }
    }
}