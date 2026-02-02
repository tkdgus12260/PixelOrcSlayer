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
    public class GemProductItem : MonoBehaviour
    {
        public Image GemImg;
        public TextMeshProUGUI AmountTxt;
        public TextMeshProUGUI CostTxt;
        public Image AdIcon;
        public TextMeshProUGUI TimerTxt;
        private Button Button;
        private const float AD_COOLTIME_INTERVAL = 1f;
        private Coroutine _adCoolTimeCo;
        
        private ProductData _productData;

        private void OnDisable()
        {
            if (_adCoolTimeCo != null)
            {
                StopCoroutine(_adCoolTimeCo);
                _adCoolTimeCo = null;
            }
        }

        public async void SetInfo(string productId, DateTime currentDateTime)
        {
            _productData = DataTableManager.Instance.GetProductData(productId);
            if (_productData == null)
            {
                Logger.LogError($"Product does not exist. ProductId: {productId}");
                return;
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
                var goldImgTexture = operationHandle.Result;
                GemImg.sprite = Sprite.Create(goldImgTexture, new Rect(0,0, goldImgTexture.width, goldImgTexture.height), new Vector2(1f, 1f));
            }

            Button.enabled = false;
            TimerTxt.gameObject.SetActive(false);
            AmountTxt.text = _productData.RewardGem.ToString("N0");
            
            switch (_productData.PurchaseType)
            {
                case PurchaseType.IAP:
                    CostTxt.gameObject.SetActive(true);
                    AdIcon.gameObject.SetActive(false);
                    Button.enabled = true;
                    break;
                case PurchaseType.Ad:
                    CostTxt.gameObject.SetActive(false);
                    AdIcon.gameObject.SetActive(true);
                    SetAdCoolTime(currentDateTime);
                    break;
                default:
                    break;
            }
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
            
            DateTime nextAvailable = userPlayData.LastDailyFreeGemRewardedTime.AddHours(4); 
                
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
                case PurchaseType.IAP:
                    break;
                case PurchaseType.Ad:
                    var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
                    if (userPlayData != null)
                    {
                        AdsManager.Instance.ShowFreeRewardedAd(async () =>
                        {
                            // Google Ads Callback 함수 정상적인 동작을 위해 GetProductReward 함수를 한 프레임 뒤에 실행되게 수정.
                            StartCoroutine(GetProductRewardCo()); 
                            userPlayData.LastDailyFreeGemRewardedTime = await FirebaseManager.Instance.GetCurrentDateTime(); 
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
            
            ShopManager.Instance.GetProductReward(_productData.ProductId);
        }
    }
}