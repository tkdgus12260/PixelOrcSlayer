using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuperMaxim.Messaging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ChapterClearUIData : BaseUIData
    {
        public int Chapter;
        public bool EarnReward;
        public int GoldAmount;
        public List<int> RewardItems;
    }

    public class ChapterClearUI : BaseUI
    {
        [SerializeField] private Transform RewardParent;
        private readonly string _rewardItemSlotName = "RewardItemSlot";
        
        private ChapterClearUIData _data;

        public override async void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            _data = uiData as ChapterClearUIData;
            if (_data == null)
            {
                Logger.LogError("ChapterClearUIData is invalid");
                return;
            }

            var chapterData = DataTableManager.Instance.GetChapterData(_data.Chapter);
            if (chapterData == null)
            {
                Logger.LogError($"ChapterData invalid. Chapter:{_data.Chapter}");
                return;
            }
            
            ClearRewardSlots();

            long totalRewardGold = 0;
            long totalRewardGem = 0;
            List<int> totalItems =  new();
            
            var rewards = SetRewardList(_data, chapterData);
            foreach (var reward in rewards)
            {
                var slot = await CreateRewardSlot();
                slot.SetInfo(reward);
                
                switch (reward.ItemId)
                {
                    case 1:
                        totalRewardGold += reward.GoodsAmount;
                        break;
                    case 2:
                        totalRewardGem += reward.GoodsAmount;
                        break;
                    default:
                        totalItems.Add(reward.ItemId);
                        break;
                }
            }
            
            // 재화 업데이트
            var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            if (userGoodsData == null)
            {
                Logger.LogError("UserGoodsData does not exist.");
                return;
            }
            
            userGoodsData.Gold += totalRewardGold;
            userGoodsData.Gem += totalRewardGem;
            userGoodsData.SaveData();

            var goldUpdateMsg = new GoldUpdateMsg();
            goldUpdateMsg.IsAdd = true;
            Messenger.Default.Publish(goldUpdateMsg);
            
            var gemUpdateMsg = new GemUpdateMsg();
            gemUpdateMsg.IsAdd = true;
            Messenger.Default.Publish(gemUpdateMsg);
            
            // 업적 업데이트
            var userAchievementData = UserDataManager.Instance.GetUserData<UserAchievementData>();
            if (userAchievementData != null)
            { 
                var AchievementTypes =
                    DataTableManager.Instance.GetAchievementTypes(GlobalDefine.RewardType.Gem);

                foreach (var achievementType in AchievementTypes)
                {
                    userAchievementData.ProgressAchievement(achievementType, totalRewardGold);
                } 
            }
            
            // 장비 업데이트 
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData does not exist.");
                return;
            }

            foreach (var itemId in totalItems)
            {
                userInventoryData.AcquireItem(itemId, 0);
            }
            userInventoryData.SaveData();

            string itemIds = string.Empty;
            foreach (var equipment in userInventoryData.EquippedItemList)
            {
                itemIds += ", " + equipment;
            }
            
            // Test LogEvent
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"chapter_no", _data.Chapter.ToString()},
                {"item", itemIds}
            };
            FirebaseManager.Instance.LogCustomEvent("chapter_clear", parameters);

        }

        private List<RewardItemSlotData> SetRewardList(ChapterClearUIData data, ChapterData chapter)
        {
            var list = new List<RewardItemSlotData>();

            if (data.EarnReward)
            {
                list.Add(new RewardItemSlotData { ItemId = (int)GoodsItemType.Gold, GoodsAmount = chapter.ChapterRewardGold, FirstReward = true });
                list.Add(new RewardItemSlotData { ItemId = (int)GoodsItemType.Gem, GoodsAmount = chapter.ChapterRewardGem,  FirstReward = true });
                
                list.Add(new RewardItemSlotData { ItemId = (int)GoodsItemType.Gold, GoodsAmount = data.GoldAmount, FirstReward = false });
                
                if (data.RewardItems != null)
                {
                    foreach (var itemId in data.RewardItems)
                    {
                        list.Add(new RewardItemSlotData { ItemId = itemId, GoodsAmount = 0, FirstReward = false });
                    }
                }
            }
            else
            {
                list.Add(new RewardItemSlotData { ItemId = (int)GoodsItemType.Gold, GoodsAmount = data.GoldAmount, FirstReward = false });

                if (data.RewardItems != null)
                {
                    foreach (var itemId in data.RewardItems)
                    {
                        list.Add(new RewardItemSlotData { ItemId = itemId, GoodsAmount = 0, FirstReward = false });
                    }
                }
            }

            return list;
        }

        private void ClearRewardSlots()
        {
            for (int i = 0; i < RewardParent.childCount; i++)
            {
                var child = RewardParent.GetChild(i);
                if (child) Destroy(child.gameObject);
            }
        }

        private async Task<RewardItemSlot> CreateRewardSlot()
        {
            AsyncOperationHandle<GameObject> operationRewardHandle  = Addressables.InstantiateAsync(_rewardItemSlotName);
            await operationRewardHandle.Task;
            if (operationRewardHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var rewardItemSlotObj = operationRewardHandle.Result;
                rewardItemSlotObj.transform.SetParent(RewardParent);
                rewardItemSlotObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                rewardItemSlotObj.transform.localScale = Vector3.one;
                var rewardItemSlot = rewardItemSlotObj.GetComponent<RewardItemSlot>();
                return rewardItemSlot;
            }

            return null;
        }

        public void OnClickHomeBtn()
        { 
            UIManager.Instance.CloseAllOpenUI();
            SceneLoader.Instance.LoadScene(SceneType.Lobby);
        }
    }
}
