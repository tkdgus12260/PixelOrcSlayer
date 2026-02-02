using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class EquippedItemSlot : MonoBehaviour
    {
        public Image AddIcon;
        public Image EquippedItemGradeBg;
        public Image EquippedItemIcon;
        public TextMeshProUGUI UpgradeTxt;
        
        private UserItemData _equippedItemData;

        public async void SetInfo(UserItemData userItemData)
        {
            _equippedItemData = userItemData;

            AddIcon.gameObject.SetActive(false);
            EquippedItemGradeBg.gameObject.SetActive(true);
            EquippedItemIcon.gameObject.SetActive(true);
            UpgradeTxt.text = _equippedItemData.UpgradeLevel > 0
                ? $"+{_equippedItemData.UpgradeLevel.ToString()}"
                : string.Empty;

            var itemGrade = (ItemGrade)((_equippedItemData.ItemId / 1000) % 10);
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
                EquippedItemGradeBg.color = color;
            }

            StringBuilder sb = new StringBuilder(userItemData.ItemId.ToString());
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
                    EquippedItemIcon.sprite = itemIconSprite;
                }
            }
        }

        public void ClearItem()
        {
            _equippedItemData = null;

            AddIcon.gameObject.SetActive(true);
            EquippedItemGradeBg.gameObject.SetActive(false);
            EquippedItemIcon.gameObject.SetActive(false);
        }

        public void OnClickEquippedItemSlot()
        {
            var uiData = new EquipmentUIData();
            uiData.SerialNumber = _equippedItemData.SerialNumber;
            uiData.ItemId = _equippedItemData.ItemId;
            uiData.UpgradeLevel = _equippedItemData.UpgradeLevel;
            uiData.IsEquipped = true;
            uiData.IsInventory = false;
            UIManager.Instance.OpenUIFromAA<EquipmentUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }
    }
}