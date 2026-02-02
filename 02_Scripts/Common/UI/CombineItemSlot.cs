using System.Text;
using Gpm.Ui;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class CombineItemSlotData : InfiniteScrollData
    {
        public long SerialNumber;
        public int ItemId;
        public int UpgradeLevel;
    }
    
    public class CombineItemSlot : InfiniteScrollItem
    {
        public Image ItemGradeBg;
        public Image ItemIcon;

        private CombineItemSlotData _combineItemSlotData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);
            
            _combineItemSlotData = scrollData as CombineItemSlotData;
            if (_combineItemSlotData == null)
            {
                Logger.Log("CombineItemSlotData is null.");
                return;
            }
            
            var itemGrade = (ItemGrade)((_combineItemSlotData.ItemId / 1000) % 10);
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

            StringBuilder sb = new StringBuilder(_combineItemSlotData.ItemId.ToString());
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

        public void OnClickCombineItemSlot()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            UIManager.Instance.GetActiveUI<CombineItemUI>().GetComponent<CombineItemUI>().SetCombineItem(_combineItemSlotData);
        }
    }
}