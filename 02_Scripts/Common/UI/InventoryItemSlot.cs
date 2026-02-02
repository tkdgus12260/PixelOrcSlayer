using System.Collections;
using System.Collections.Generic;
using System.Text;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class InventoryItemSlotData : InfiniteScrollData
    {
        public long SerialNumber;
        public int ItemId;
        public int UpgradeLevel;
    }

    public class InventoryItemSlot : InfiniteScrollItem
    {
        public Image ItemGradeBg;
        public Image ItemIcon;
        public TextMeshProUGUI UpgradeTxt;

        private InventoryItemSlotData _inventoryItemSlotData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);

            _inventoryItemSlotData = scrollData as InventoryItemSlotData;
            if (_inventoryItemSlotData == null)
            {
                Logger.Log("_inventoryItemSlotData is invalid.");
                return;
            }
            
            UpgradeTxt.text = _inventoryItemSlotData.UpgradeLevel > 0
                ? $"+{_inventoryItemSlotData.UpgradeLevel.ToString()}"
                : string.Empty;

            var itemGrade = (ItemGrade)((_inventoryItemSlotData.ItemId / 1000) % 10);
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

            StringBuilder sb = new StringBuilder(_inventoryItemSlotData.ItemId.ToString());
            sb[1] = '1';
            var itemIconName = sb.ToString();
            var address = $"Equipments[{itemIconName}]";
            
            AsyncOperationHandle<Sprite> operationHandle =
                Addressables.LoadAssetAsync<Sprite>(address);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var itemIconSprite = operationHandle.Result;
                if (itemIconSprite != null)
                {
                    ItemIcon.sprite = itemIconSprite;
                }
            }
        }

        public void OnClickInventoryItemSlot()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            var uiData = new EquipmentUIData();
            uiData.SerialNumber = _inventoryItemSlotData.SerialNumber;
            uiData.ItemId = _inventoryItemSlotData.ItemId;
            uiData.UpgradeLevel = _inventoryItemSlotData.UpgradeLevel;
            uiData.IsInventory = true;
            UIManager.Instance.OpenUIFromAA<EquipmentUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }
    }
}