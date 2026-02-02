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
    public class EquipmentCollectionItemSlotData : InfiniteScrollData
    {
        public int ItemId;
        public string ItemName;
        public int Damage;
        public int Hp;
        public string Description;
        public string Sources;
    }
    
    public class EquipmentCollectionItemSlot : InfiniteScrollItem
    {
        public Image ItemGradeBg;
        public Image ItemIcon;

        private EquipmentCollectionItemSlotData _equipmentCollectionItemSlotData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);

            _equipmentCollectionItemSlotData = scrollData as EquipmentCollectionItemSlotData;
            if (_equipmentCollectionItemSlotData == null)
            {
                Logger.Log("_equipmentCollectionUIData is invalid.");
                return;
            }
            
            var itemGrade = (ItemGrade)((_equipmentCollectionItemSlotData.ItemId / 1000) % 10);
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

            StringBuilder sb = new StringBuilder(_equipmentCollectionItemSlotData.ItemId.ToString());
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

        public void OnClickEquipmentCollectionItemSlot()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            var uiData = new EquipmentCollectionDescriptionUIData();
            uiData.ItemId = _equipmentCollectionItemSlotData.ItemId;
            uiData.ItemName = _equipmentCollectionItemSlotData.ItemName;
            uiData.Description = _equipmentCollectionItemSlotData.Description;
            uiData.Damage = _equipmentCollectionItemSlotData.Damage;
            uiData.Hp = _equipmentCollectionItemSlotData.Hp;
            uiData.Sources = _equipmentCollectionItemSlotData.Sources;
            
            UIManager.Instance.OpenUIFromAA<EquipmentCollectionDescriptionUI>(uiData);
        }
    }
}