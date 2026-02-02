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
    public class EquipmentCollectionDescriptionUIData : BaseUIData
    {
        public int ItemId;
        public string ItemName;
        public int Damage;
        public int Hp;
        public string Description;
        public string Sources;
    }
    
    public class EquipmentCollectionDescriptionUI : BaseUI
    {
        public Image ItemIcon;
        public Image ItemGradeBg;
        public TextMeshProUGUI ItemGradeTxt;
        public TextMeshProUGUI ItemNameTxt;
        public TextMeshProUGUI DamageTxt;
        public TextMeshProUGUI HpTxt;
        public TextMeshProUGUI DescriptionTxt;
        public TextMeshProUGUI GetDescriptionTxt;

        private EquipmentCollectionDescriptionUIData _equipmentCollectionDescriptionUIData;
        
        public override async void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            _equipmentCollectionDescriptionUIData = uiData as EquipmentCollectionDescriptionUIData;
            if (_equipmentCollectionDescriptionUIData == null)
            {
                Logger.LogError("_equipmentCollectionDescriptionUIData is invalid");
                return;
            }
            
            var itemDamage = _equipmentCollectionDescriptionUIData.Damage;
            DamageTxt.text = itemDamage.ToString();
            
            var itemHp = _equipmentCollectionDescriptionUIData.Hp;
            HpTxt.text = itemHp.ToString();
            
            var itemDescription = _equipmentCollectionDescriptionUIData.Description;
            DescriptionTxt.text = itemDescription;
            
            var itemSources = _equipmentCollectionDescriptionUIData.Sources;
            GetDescriptionTxt.text = itemSources;
            
            var itemGrade = (ItemGrade)((_equipmentCollectionDescriptionUIData.ItemId / 1000) % 10);
         
            ItemGradeTxt.text = itemGrade.ToString();
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
                ItemGradeTxt.color = color;
                ItemGradeBg.color = color;
            }

            StringBuilder sb = new StringBuilder(_equipmentCollectionDescriptionUIData.ItemId.ToString());
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

                ItemNameTxt.text = _equipmentCollectionDescriptionUIData.ItemName;
            }
        }
    }
}
