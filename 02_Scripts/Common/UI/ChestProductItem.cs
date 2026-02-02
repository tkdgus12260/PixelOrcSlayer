using System;
using System.Collections;
using System.Collections.Generic;
using SuperMaxim.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ChestProductItem : MonoBehaviour
    {
        public Image ChestImg;
        public TextMeshProUGUI ProductNameTxt;
        public TextMeshProUGUI CostTxt;
        public Image AdIcon;
        public TextMeshProUGUI TimerTxt;
        private Button Button;
        private const float AD_COOLTIME_INTERVAL = 1f;
        private Coroutine _adCoolTimeCo;
        
        private ProductData _productData;

        public async void SetInfo(string productId, DateTime currentDateTime)
        {
            _productData = DataTableManager.Instance.GetProductData(productId);
            if (_productData == null)
            {
                Logger.LogError($"Product does not exist. ProductId: {productId}");
            }
            
            if (Button == null)
            {
                Button = GetComponent<Button>();
                if (Button == null)
                {
                    Logger.LogError($"Button is null.");
                    return;
                }
            }
            
            AsyncOperationHandle<Texture2D> operationHandle = Addressables.LoadAssetAsync<Texture2D>(_productData.ProductId);
            await operationHandle.Task;
            
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var chestImgTexture = operationHandle.Result;
                ChestImg.sprite = Sprite.Create(chestImgTexture, new Rect(0,0, chestImgTexture.width, chestImgTexture.height), new Vector2(1f, 1f));
            }
            
            Button.enabled = false;
            
            switch (_productData.PurchaseType)
            {
                case PurchaseType.Gold:
                    CostTxt.gameObject.SetActive(true);
                    CostTxt.text = _productData.PurchaseCost.ToString("N3");
                    AdIcon.gameObject.SetActive(false);
                    TimerTxt.gameObject.SetActive(false);
                    Button.enabled = true;
                    break;
                case PurchaseType.Ad:
                    CostTxt.gameObject.SetActive(false);
                    SetAdCoolTime(currentDateTime);
                    break;
                default:
                    break;
            }
            
            ProductNameTxt.text = _productData.ProductName;
            CostTxt.text = _productData.PurchaseCost.ToString("N0");
            // AdIcon.gameObject.SetActive(false);
        }
        
        private async void SetAdCoolTime(DateTime currentDateTime = default)
        {
            if (currentDateTime == default)
            {
                currentDateTime = await FirebaseManager.Instance.GetCurrentDateTime();
            }

            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                Logger.LogError($"UserData is null.");
                return;
            }
            
            DateTime nextAvailable = userPlayData.LastHourFreeChestRewardedTime.AddHours(1); 
                
            if (currentDateTime < nextAvailable)
            {
                Button.enabled = false;
                AdIcon.gameObject.SetActive(false);
                TimerTxt.gameObject.SetActive(true);
                _adCoolTimeCo = StartCoroutine(AdCoolTimerCo(nextAvailable - currentDateTime));
            }
            else
            {
                Button.enabled = true;
                AdIcon.gameObject.SetActive(true);
                TimerTxt.gameObject.SetActive(false);
            }
        }

        private IEnumerator AdCoolTimerCo(TimeSpan remainTime)
        {
            while (remainTime.TotalSeconds > 0)
            {
                TimerTxt.text = $"{remainTime.Hours:D2}:{remainTime.Minutes:D2}:{remainTime.Seconds:D2}";
                yield return new WaitForSeconds(AD_COOLTIME_INTERVAL);
                remainTime = remainTime.Subtract(TimeSpan.FromSeconds(AD_COOLTIME_INTERVAL));
            }
            
            TimerTxt.text = "00:00:00";
            yield return new WaitForSeconds(1f);
            _adCoolTimeCo = null;
            SetAdCoolTime();
        }

        public void OnClickItem()
        {
            Logger.Log($"{GetType()}::OnClickItem");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            switch (_productData.PurchaseType)
            {
                case PurchaseType.Gold:
                    var uiData = new ConfirmUIData(); 
                    uiData.ConfirmType = ConfirmType.OK_CANCEL; 
                    uiData.TitleTxt = "Buy Item";
                    uiData.DescTxt = $"Would you like to purchase the selected item?";
                    uiData.OKBtnTxt = "OK"; 
                    uiData.CancelBtnTxt = "Cancel";
                    uiData.OnClickOKBtn += () =>
                    {
                        // 골드 확인 골드 차감
                        var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
                        if (userGoodsData == null)
                        {
                            Logger.LogError("UserGoodsData is null");
                            return;
                        }

                        if (userGoodsData.Gold < _productData.PurchaseCost)
                        {
                            var failUiData = new ConfirmUIData();
                            failUiData.ConfirmType = ConfirmType.OK;
                            failUiData.TitleTxt = "Fail";
                            failUiData.DescTxt = $"You don't have enough gold.\nGold you have: {userGoodsData.Gold} G\nGold you need: {_productData.PurchaseCost} G";
                            failUiData.OKBtnTxt = "OK";
                            failUiData.CloseFirst = true;

                            UIManager.Instance.OpenUIFromAA<ConfirmUI>(failUiData);
                            return;
                        }

                        userGoodsData.Gold -= _productData.PurchaseCost;
                        userGoodsData.SaveData();

                        var goldUpdateMsg = new GoldUpdateMsg();
                        Messenger.Default.Publish(goldUpdateMsg);

                        AudioManager.Instance.PlaySFX(SFX.ui_get);
                        ShopManager.Instance.GetProductReward(_productData.ProductId);
                    };
                    UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
                    break;
                case PurchaseType.Ad:
                    var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
                    if (userPlayData != null)
                    {
                        AdsManager.Instance.ShowFreeRewardedAd(async () =>
                        {
                            // Google Ads Callback 함수 정상적인 동작을 위해 GetProductReward 함수를 한 프레임 뒤에 실행되게 수정.
                            StartCoroutine(GetProductRewardCo()); 
                            userPlayData.LastHourFreeChestRewardedTime = await FirebaseManager.Instance.GetCurrentDateTime(); 
                            userPlayData.SaveData(); 
                            SetAdCoolTime(); 
                        });
                    }
                    break;
                default:
                    break;
            }
        }
        
        private IEnumerator GetProductRewardCo()
        {
            yield return null;
            AudioManager.Instance.PlaySFX(SFX.ui_get);
            ShopManager.Instance.GetProductReward(_productData.ProductId);
        }

    }
}