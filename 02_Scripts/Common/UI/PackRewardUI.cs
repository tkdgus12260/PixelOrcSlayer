using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class PackRewardUIData : BaseUIData
    {
        public List<RewardItemSlotData> Rewards = new List<RewardItemSlotData>();
    }
    
    public class PackRewardUI : BaseUI
    {
        private readonly string _rewardItemSlotName = "RewardItemSlot";
        public Transform RewardItemTrs;

        private PackRewardUIData _packRewardUIData;
        
        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            SetRewardItem(uiData);
        }

        private async void SetRewardItem(BaseUIData uiData)
        {

            _packRewardUIData = uiData as PackRewardUIData;
            if (_packRewardUIData == null)
            {
                Logger.LogError("ChestLootUI Data is invalid");
            }

            foreach (Transform child in RewardItemTrs)
            {
                Destroy(child.gameObject);
            }

            foreach (var reward in _packRewardUIData.Rewards)
            {
                AsyncOperationHandle<GameObject> operationRewardHandle =
                    Addressables.InstantiateAsync(_rewardItemSlotName);
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