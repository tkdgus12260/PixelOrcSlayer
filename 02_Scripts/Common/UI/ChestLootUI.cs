using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ChestLootUIData : BaseUIData
    {
        public string ChestId;
        // public List<int> RewardItemIds = new List<int>();
        public List<RewardItemSlotData> Rewards = new List<RewardItemSlotData>();
    }   
    
    public class ChestLootUI : BaseUI
    {
        public Image ChestImg;
        private readonly string _rewardItemSlotName = "RewardItemSlot";
        public Transform RewardItemTrs;
        
        private ChestLootUIData _chestLootUIData;

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            SetRewardItem(uiData);
        }

        private async void SetRewardItem(BaseUIData uiData)
        {
            
            _chestLootUIData = uiData as ChestLootUIData;
            if (_chestLootUIData == null)
            {
                Logger.LogError("ChestLootUI Data is invalid");
            }
            
            AsyncOperationHandle<Texture2D> operationHandle  = Addressables.LoadAssetAsync<Texture2D>($"chest_open_{_chestLootUIData.ChestId}");
            await operationHandle.Task;
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var chestImgTexture = operationHandle.Result;
                ChestImg.sprite = Sprite.Create(chestImgTexture, new Rect(0, 0, chestImgTexture.width, chestImgTexture.height), new Vector2(1f, 1f));
            }

            foreach (Transform child in RewardItemTrs)
            {
                Destroy(child.gameObject);
            }

            foreach (var reward in _chestLootUIData.Rewards)
            {
                AsyncOperationHandle<GameObject> operationRewardHandle  = Addressables.InstantiateAsync(_rewardItemSlotName);
                await operationRewardHandle.Task;
                if (operationRewardHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var rewardItemSlotObj = operationRewardHandle.Result;
                    rewardItemSlotObj.transform.SetParent(RewardItemTrs);
                    rewardItemSlotObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    rewardItemSlotObj.transform.localScale = Vector3.one;
                    
                    var rewardiItemUI = rewardItemSlotObj.GetComponent<RewardItemSlot>();
                    if (rewardiItemUI != null)
                    {
                        rewardiItemUI.SetInfo(reward);
                    }
                }
            }
        }
    }
}