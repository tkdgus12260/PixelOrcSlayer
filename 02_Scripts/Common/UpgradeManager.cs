using System.Collections;
using System.Collections.Generic;
using PixelSurvival;
using UnityEngine;

namespace PixelSurvival
{
    public class UpgradeManager : SingletonBehaviour<UpgradeManager>
    {
        protected override void Init()
        {
            isDestroyOnLoad = true;
        
            base.Init();
        }

        public bool TryUpgradeItem(UpgradeItemSlotData upgradeItemData, float chance)
        {
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData is invalid");
                return false;
            }

            if (upgradeItemData == null)
            {
                Logger.LogError("upgradeItemData is invalid");
                return false;
            }

            // 세팅된 업그레이드 확률로 업그레이드 진행
            float roll = Random.value;
            bool isSuccess = roll < chance;
            
            if (isSuccess)
            {
                SuccessUpgradeItem(upgradeItemData);
                return true;
            }
            else
            {
                FailUpgradeItem();
                return false;
            }
        }


        public void SuccessUpgradeItem(UpgradeItemSlotData upgradeItemData)
        {
            AudioManager.Instance.PlaySFX(SFX.upgrade_success);
            
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData is invalid");
                return;
            }
            
            upgradeItemData.UpgradeLevel++;
            
            // 업그레이드 성공 시 아이템의 SerialNumber를 가져와 중복 아이템일지라도 선택한 아이템을 업그레이드 하게 동작. 
            var userItemData = new UserItemData(upgradeItemData.SerialNumber, upgradeItemData.ItemId, upgradeItemData.UpgradeLevel);
            // 업그레이드 후 인벤토리에 재세팅
            userInventoryData.UpdateInventoryItem(userItemData);
            userInventoryData.SaveData();
            
            var uiData = new ConfirmUIData();
            uiData.ConfirmType = ConfirmType.OK;
            uiData.TitleTxt = "Upgrade Success";
            uiData.DescTxt = $"{upgradeItemData.UpgradeLevel - 1}  ->  {upgradeItemData.UpgradeLevel}";
            uiData.OKBtnTxt = "OK";
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }

        public void FailUpgradeItem()
        {
            AudioManager.Instance.PlaySFX(SFX.upgrade_fail);
            
            var uiData = new ConfirmUIData();
            uiData.ConfirmType = ConfirmType.OK;
            uiData.TitleTxt = "Upgrade Fail";
            uiData.DescTxt = "There is no change to the item.";
            uiData.OKBtnTxt = "OK";
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }
    }   
}
