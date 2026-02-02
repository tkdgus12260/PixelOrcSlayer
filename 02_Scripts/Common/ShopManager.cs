using System;
using System.Collections;
using System.Collections.Generic;
using SuperMaxim.Messaging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PixelSurvival
{
    public class ShopManager : SingletonBehaviour<ShopManager>
    {
        public void PurchaseProduct(string productId)
        {
            var productData = DataTableManager.Instance.GetProductData(productId);
            if (productData == null)
            {
                Logger.LogError($"No product data. ProductId: {productId}");
                return;
            }
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            
            switch (productData.PurchaseType)
            {
                case PurchaseType.IAP:
                case PurchaseType.Ad:
                case PurchaseType.Gem:
                    if (userGoodsData == null)
                    {
                        Logger.LogError($"No user data.");
                        return;
                    }

                    if (userGoodsData.Gem >= productData.PurchaseCost)
                    {
                        userGoodsData.Gem -= productData.PurchaseCost;
                        userGoodsData.SaveData();
                        var gemUpdateMsg = new GemUpdateMsg();
                        Messenger.Default.Publish(gemUpdateMsg);
                        GetProductReward(productId);
                    }
                    else
                    {
                        var uiData = new ConfirmUIData();
                        uiData.ConfirmType = ConfirmType.OK;
                        uiData.TitleTxt = "Purchase Fail";
                        uiData.DescTxt = "Not enough gem";
                        uiData.OKBtnTxt = "OK";
                        UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
                    }
                    break;
                case PurchaseType.Gold:
                    if (userGoodsData == null)
                    {
                        Logger.LogError($"No user data.");
                        return;
                    }

                    if (userGoodsData.Gold >= productData.PurchaseCost)
                    {
                        userGoodsData.Gold -= productData.PurchaseCost;
                        userGoodsData.SaveData();
                        var goldUpdateMsg = new GoldUpdateMsg();
                        Messenger.Default.Publish(goldUpdateMsg);
                        GetProductReward(productId);
                    }
                    else
                    {
                        var uiData = new ConfirmUIData();
                        uiData.ConfirmType = ConfirmType.OK;
                        uiData.TitleTxt = "Purchase Fail";
                        uiData.DescTxt = "Not enough gold";
                        uiData.OKBtnTxt = "OK";
                        UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
                    }
                    break;
                default:
                    break;
            }
        }

        public void GetProductReward(string productId)
        {
            var productData = DataTableManager.Instance.GetProductData(productId);
            if (productData == null)
            {
                Logger.LogError($"No product data. ProductId: {productId}");
                return;
            }
            
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError($"No user data.");
                return;
            }

            switch (productData.ProductType)
            {
                case ProductType.Pack:
                    userGoodsData.Gem += productData.RewardGem;
                    userGoodsData.Gold += productData.RewardGold;
                    userGoodsData.SaveData();
                    
                    var gemUpdateMsgForPack = new GemUpdateMsg();
                    gemUpdateMsgForPack.IsAdd = true;
                    Messenger.Default.Publish(gemUpdateMsgForPack);
                    
                    var goldUpdateMsgForPack = new GoldUpdateMsg();
                    goldUpdateMsgForPack.IsAdd = true;
                    Messenger.Default.Publish(goldUpdateMsgForPack);
                    
                    var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
                    if (userInventoryData != null)
                    {
                        userInventoryData.AcquireItem(productData.RewardItemId, 0);
                    }
                    userInventoryData.SaveData();

                    var packUIData = new PackRewardUIData();
                    packUIData.Rewards.Add(new RewardItemSlotData { ItemId = productData.RewardItemId, GoodsAmount = 0, FirstReward = false });
                    packUIData.Rewards.Add(new RewardItemSlotData { ItemId = 1, GoodsAmount = productData.RewardGold, FirstReward = false });
                    packUIData.Rewards.Add(new RewardItemSlotData { ItemId = 2, GoodsAmount = productData.RewardGem, FirstReward = false });
                    UIManager.Instance.OpenUIFromAA<PackRewardUI>(packUIData);
                    break;
                case ProductType.Gem:
                    userGoodsData.Gem += productData.RewardGem;
                    userGoodsData.SaveData();
                    var gemUpdateMsg = new GemUpdateMsg();
                    gemUpdateMsg.IsAdd = true;
                    Messenger.Default.Publish(gemUpdateMsg);
                    break;
                case ProductType.Chest:
                    OpenChest(productId);
                    break;
                case ProductType.Gold:
                    userGoodsData.Gold += productData.RewardGold;
                    userGoodsData.SaveData();
                    var goldUpdateMsg = new GoldUpdateMsg();
                    goldUpdateMsg.IsAdd = true;
                    Messenger.Default.Publish(goldUpdateMsg);
                    break;
                default:
                    break;
            }
        }
        
        private void OpenChest(string productId)
        {
            var chestRewardProbabilityDatas = DataTableManager.Instance.GetChestRewardProbabilityDatas(productId);
            var totalProbability = 0;
            
            foreach (var probabilityData in chestRewardProbabilityDatas)
            {
                totalProbability += probabilityData.LootProbability;
            }

            if (totalProbability != 100)
            {
                Logger.LogWarning($"Total probability doesn't sum up to 100. {totalProbability}");
            }
            
            var resultValue = Random.Range(0, totalProbability);
            var cumulativeProbability = 0;
            foreach (var probabilityData in chestRewardProbabilityDatas)
            {
                cumulativeProbability += probabilityData.LootProbability;
                if (resultValue < cumulativeProbability)
                {
                    var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
                    if (userInventoryData != null)
                    {
                        userInventoryData.AcquireItem(probabilityData.ItemId, 0);
                        userInventoryData.SaveData();

                        var chestUiData = new ChestLootUIData();
                        chestUiData.ChestId = productId;
                        chestUiData.Rewards.Add(new RewardItemSlotData
                        {
                            ItemId = probabilityData.ItemId, GoodsAmount = 0, FirstReward = false
                        });
                        // chestUiData.RewardItemIds.Add(probabilityData.ItemId);
                        UIManager.Instance.OpenUIFromAA<ChestLootUI>(chestUiData);
                    }
                    break;
                }
            }
        }
    }
}