using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class GoldProductItem : MonoBehaviour
    {
        public Image GoldImg;
        public TextMeshProUGUI AmountTxt;
        public TextMeshProUGUI CostTxt;

        private ProductData _productData;

        public async void SetInfo(string productId)
        {
            _productData = DataTableManager.Instance.GetProductData(productId);
            if (_productData == null)
            {
                Logger.LogError($"Product does not exist. ProductId: {productId}");
                return;
            }
            AsyncOperationHandle<Texture2D> operationHandle = Addressables.LoadAssetAsync<Texture2D>(_productData.ProductId);
            await operationHandle.Task;
            
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var goldImgTexture = operationHandle.Result;
                GoldImg.sprite = Sprite.Create(goldImgTexture, new Rect(0,0, goldImgTexture.width, goldImgTexture.height), new Vector2(1f, 1f));
            }

            AmountTxt.text = _productData.RewardGold.ToString("N0");
            CostTxt.text = _productData.PurchaseCost.ToString("N0");
        }

        public void OnClickItem()
        {
            var uiData = new ConfirmUIData(); 
            uiData.ConfirmType = ConfirmType.OK_CANCEL; 
            uiData.TitleTxt = "Buy Item";
            uiData.DescTxt = $"Would you like to purchase the selected item?";
            uiData.OKBtnTxt = "OK"; 
            uiData.CancelBtnTxt = "Cancel";
            uiData.OnClickOKBtn += () =>
            {
                ShopManager.Instance.PurchaseProduct(_productData.ProductId);
            };
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }
    }
}