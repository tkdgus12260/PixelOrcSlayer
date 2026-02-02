using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class PackProductItem : MonoBehaviour
    {
        public Image ItemGradeBg;
        public Image PurchasedDim;
        public Image AdIcon;
        private ProductData _productData;

        public void SetInfo(string productId)
        {
            _productData = DataTableManager.Instance.GetProductData(productId);
            if (_productData == null)
            {
                Logger.LogError($"Product doest not exist. ProductId : {productId}");
                return;
            }
            
            var itemGrade = (ItemGrade)(Convert.ToInt32(_productData.RewardItemId) / 1000 % 10);
            var hexColor = string.Empty;
            switch (itemGrade)
            {
                case ItemGrade.Common:    hexColor = "#1AB3FF"; break;
                case ItemGrade.Uncommon:  hexColor = "#51C52C"; break;
                case ItemGrade.Rare:      hexColor = "#EA5AFF"; break;
                case ItemGrade.Epic:      hexColor = "#FF9900"; break;
                case ItemGrade.Legendary: hexColor = "#F24949"; break;
                default: break;
            }

            Color color;
            if (ColorUtility.TryParseHtmlString(hexColor, out color))
            {
                ItemGradeBg.color = color;
            }
            
            RefreshPurchased();
        }
        
        private void RefreshPurchased()
        {
            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            bool hadPurchased = userPlayData != null && userPlayData.HasAdPackPurchased;

            if (PurchasedDim) PurchasedDim.gameObject.SetActive(hadPurchased);
            
            var button = GetComponent<Button>();
            if (button) button.enabled = !hadPurchased;
            
            AdIcon.gameObject.SetActive(!hadPurchased);
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
                            userPlayData.HasAdPackPurchased = true; 
                            userPlayData.SaveData(); 
                            RefreshPurchased();
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