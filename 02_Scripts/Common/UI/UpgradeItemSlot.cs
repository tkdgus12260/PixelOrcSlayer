using System.Text;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class UpgradeItemSlotData : InfiniteScrollData
    {
        public long SerialNumber;
        public int ItemId;
        public int UpgradeLevel;
    }
    
    public class UpgradeItemSlot : InfiniteScrollItem
    {
        public Image ItemGradeBg;
        public Image ItemIcon;
        public TextMeshProUGUI UpgradeTxt;
        
        private UpgradeItemSlotData _upgradeItemSlotData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);
            
            _upgradeItemSlotData = scrollData as UpgradeItemSlotData;
            if (_upgradeItemSlotData == null)
            {
                Logger.Log("UpgradeItemSlotData is null.");
                return;
            }
            
            UpgradeTxt.text = _upgradeItemSlotData.UpgradeLevel > 0
                ? $"+{_upgradeItemSlotData.UpgradeLevel.ToString()}"
                : string.Empty;
            
            var itemGrade = (ItemGrade)((_upgradeItemSlotData.ItemId / 1000) % 10);
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

            StringBuilder sb = new StringBuilder(_upgradeItemSlotData.ItemId.ToString());
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

        public void OnClickUpgradeItemSlot()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            UIManager.Instance.GetActiveUI<UpgradeItemUI>().GetComponent<UpgradeItemUI>().SetUpgradeItem(_upgradeItemSlotData);
        }
    }
}