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
    public class DescriptionUIData : BaseUIData
    {
        public int ItemId;
    }
    
    public class DescriptionUI : BaseUI
    {
        public Image ItemGradeBg;
        public Image ItemIcon;
        
        public TextMeshProUGUI ItemGradeTxt;
        public TextMeshProUGUI ItemNameTxt;
        public TextMeshProUGUI DamageTxt;
        public TextMeshProUGUI HpTxt;
        public TextMeshProUGUI DescriptionTxt;

        private DescriptionUIData _descriptionUIData;

        public override async void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            _descriptionUIData = uiData as DescriptionUIData;
            if (_descriptionUIData == null)
            {
                Debug.LogError("Description UI Data is invalid");
                return;
            }
            
            var itemData = DataTableManager.Instance.GetItemData(_descriptionUIData.ItemId);
            if (itemData == null)
            {
                Logger.LogError($"itemData is Invalid. ItemId : {_descriptionUIData.ItemId}");
                return;
            }
            
            var skillData = DataTableManager.Instance.GetSkillData(_descriptionUIData.ItemId);
            if (skillData == null)
            {
                Logger.LogError($"skillData is Invalid. ItemId : {_descriptionUIData.ItemId}");
                return;
            }
            
            var itemDamage = itemData.Damage;
            DamageTxt.text = itemDamage.ToString();
            
            var itemHp = itemData.Hp;
            HpTxt.text = itemHp.ToString();
            
            var itemDescriptionTemplate = itemData.Description;
            var itemDescription = GlobalDefine.DescriptionFormat(itemDescriptionTemplate, new Dictionary<string, object>
            {
                ["objectCount"] = skillData.ObjectCount,
                ["cooldown"] = skillData.Cooldown
            });
            
            DescriptionTxt.text = itemDescription;
            
            var itemGrade = (ItemGrade)((_descriptionUIData.ItemId / 1000) % 10);
         
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

            StringBuilder sb = new StringBuilder(_descriptionUIData.ItemId.ToString());
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

                ItemNameTxt.text = itemData.ItemName;
            }
        }
    }
}