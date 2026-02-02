using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ShopUI : BaseUI
    {
        public ScrollRect _scrollRect;
        
        public Transform GoldProductsGroupTrs;
        public Transform GemProductsGroupTrs;
        public Transform ChestProductsGroupTrs;
        public Transform PackProductsGroupTrs;
        
        private readonly string _goldProductItemName = "GoldProductItem";
        private readonly string _gemProductItemName = "GemProductItem";
        private readonly string _chestProductItemName = "ChestProductItem";
        private readonly string _packProductItemName = "PackProductItem";
        
        private DateTime _currentDateTime;
        
        private void OnEnable()
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        public override async void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            gameObject.SetActive(false);
            
            await GetCurrentDateTime();
            
            SetGemProducts();
            SetGoldProducts();
            SetChestProducts();
            SetPackProducts();
            
            gameObject.SetActive(true);
        }

        private async Task GetCurrentDateTime()
        {
            _currentDateTime = await FirebaseManager.Instance.GetCurrentDateTime();
        }
        
        // Gem Product Item Setting
        private async void SetGemProducts()
        {
            foreach (Transform child in GemProductsGroupTrs)
            {
                Destroy(child.gameObject);
            }
            
            var productList = DataTableManager.Instance.GetProductDatas(ProductType.Gem);
            if (productList.Count == 0)
            {
                Logger.LogError($"No products found for {ProductType.Gem}");
                return;
            }

            if (_gemProductItemName == string.Empty)
            {
                Logger.LogError($"No item Name found for {ProductType.Gem}");
                return;
            }

            foreach (var product in productList)
            {
                // 사업자 관련해 현재 인앱결제 부분 비활성화.
                if(product.PurchaseType == PurchaseType.IAP) return;
                
                AsyncOperationHandle<GameObject> operationHandle  = Addressables.InstantiateAsync(_gemProductItemName);
                await operationHandle.Task;
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var productItemObj = operationHandle.Result;
                    productItemObj.transform.SetParent(GemProductsGroupTrs);
                    productItemObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    var gemProductItem = productItemObj.GetComponent<GemProductItem>();
                    if (gemProductItem != null)
                    {
                        gemProductItem.SetInfo(product.ProductId, _currentDateTime);
                    }
                }
            }
        }
        
        // Gold Product Item Setting
        private async void SetGoldProducts()
        {
            foreach (Transform child in GoldProductsGroupTrs)
            {
                Destroy(child.gameObject);
            }

            var productList = DataTableManager.Instance.GetProductDatas(ProductType.Gold);
            if (productList.Count == 0)
            {
                Logger.LogError($"No products found for {ProductType.Gold}");
                return;
            }

            if (_goldProductItemName == string.Empty)
            {
                Logger.LogError($"No item Name found for {ProductType.Gold}");
                return;
            }

            foreach (var product in productList)
            {
                AsyncOperationHandle<GameObject> operationHandle  = Addressables.InstantiateAsync(_goldProductItemName);
                await operationHandle.Task;
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var productItemObj = operationHandle.Result;
                    productItemObj.transform.SetParent(GoldProductsGroupTrs);
                    productItemObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    var goldProductItem = productItemObj.GetComponent<GoldProductItem>();
                    if (goldProductItem != null)
                    {
                        goldProductItem.SetInfo(product.ProductId);
                    }
                }
            }
        }
        
        // Chest Product Item Setting
        private async void SetChestProducts()
        {
            foreach (Transform child in ChestProductsGroupTrs)
            {
                Destroy(child.gameObject);
            }

            var productList = DataTableManager.Instance.GetProductDatas(ProductType.Chest);
            if (productList.Count == 0)
            {
                Logger.LogError($"No products found for {ProductType.Chest}");
                return;
            }

            if (_chestProductItemName == string.Empty)
            {
                Logger.LogError($"No item Name found for {ProductType.Chest}");
                return;
            }

            foreach (var product in productList)
            {
                AsyncOperationHandle<GameObject> operationHandle  = Addressables.InstantiateAsync(_chestProductItemName);
                await operationHandle.Task;
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var productItemObj = operationHandle.Result;
                    productItemObj.transform.SetParent(ChestProductsGroupTrs);
                    productItemObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    var chestProductItem = productItemObj.GetComponent<ChestProductItem>();
                    if (chestProductItem != null)
                    {
                        chestProductItem.SetInfo(product.ProductId, _currentDateTime);
                    }
                }
            }
        }

        private async void SetPackProducts()
        {
            foreach (Transform child in PackProductsGroupTrs)
            {
                Destroy(child.gameObject);
            }
            
            var productList = DataTableManager.Instance.GetProductDatas(ProductType.Pack);
            if (productList.Count == 0)
            {
                Logger.LogError($"No products found for {ProductType.Pack}");
                return;
            }

            if (_packProductItemName == string.Empty)
            {
                Logger.LogError($"No item Name found for {ProductType.Pack}");
                return;
            }
            
            foreach (var product in productList)
            {
                AsyncOperationHandle<GameObject> operationHandle  = Addressables.InstantiateAsync($"{_packProductItemName}_{product.ProductId}");
                await operationHandle.Task;
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var productItemObj = operationHandle.Result;
                    productItemObj.transform.SetParent(PackProductsGroupTrs);
                    productItemObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    var packProductItem = productItemObj.GetComponent<PackProductItem>();
                    if (packProductItem != null)
                    {
                        packProductItem.SetInfo(product.ProductId);
                    }
                }
            }
        }

        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();

            if (UIManager.Instance.GetActiveUI<InventoryUI>() != null)
            {
                PlayerManager.Instance.ShowInventoryPlayer();
            }
        }
    }
}
