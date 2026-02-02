using System.Text;
using UnityEngine;

namespace PixelSurvival
{
    public class CombineManager : SingletonBehaviour<CombineManager>
    {
        protected override void Init()
        {
            isDestroyOnLoad = true;
        
            base.Init();
        }

        public bool TryCombineItem(CombineItemSlotData targetItem, CombineItemSlotData materialItem)
        {
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData is null");
                return false;
            }

            if (targetItem.ItemId != materialItem.ItemId)
                return false;

            userInventoryData.RemoveItem(materialItem.SerialNumber);

            targetItem.ItemId += 1000;
            targetItem.UpgradeLevel = 0;

            var newUserItem = new UserItemData(targetItem.SerialNumber, targetItem.ItemId, targetItem.UpgradeLevel);
            userInventoryData.UpdateInventoryItem(newUserItem);

            userInventoryData.SaveData();

            var uiData = new ConfirmUIData();
            uiData.ConfirmType = ConfirmType.OK;
            uiData.TitleTxt = "Combine Success";
            uiData.DescTxt = $"{DataTableManager.Instance.GetItemData(targetItem.ItemId).ItemName} Grade Up.";
            uiData.OKBtnTxt = "OK";
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);

            return true;
        }
    }
}