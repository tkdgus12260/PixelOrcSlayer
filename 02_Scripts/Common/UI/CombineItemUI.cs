using System.Text;
using System.Threading.Tasks;
using Gpm.Ui;
using SuperMaxim.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class CombineItemUI : BaseUI
    {
        [Header("Combine Target Item")]
        public Image CombineTargetImg;
        public Image CombineTargetBg;
        public TextMeshProUGUI CombineTargetTxt;
        public Button CancelTargetBtn;

        [Header("Combine Material Item")]
        public Image CombineMaterialImg;
        public Image CombineMaterialBg;
        public TextMeshProUGUI CombineMaterialTxt;
        public Button CancelMaterialBtn;

        public Button CombineBtn;
        public InfiniteScroll InventoryScrollList;
        public TextMeshProUGUI SortBtnTxt;

        private InventorySortType _inventorySortType = InventorySortType.ItemGrade;


        private CombineItemSlotData _combineTargetItemData;
        private CombineItemSlotData _combineMaterialItemData;
        
        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            SetInventory();
            SortInventory();
            ResetTarget();
        }

        private void SetInventory()
        {
            InventoryScrollList.Clear();

            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData != null)
            {
                foreach (var itemData in userInventoryData.InventoryItemDataList)
                {
                    if (userInventoryData.IsEquipped(itemData.SerialNumber) || itemData.UpgradeLevel < 5)
                        continue;

                    var slotData = new CombineItemSlotData();
                    slotData.SerialNumber = itemData.SerialNumber;
                    slotData.ItemId = itemData.ItemId;
                    slotData.UpgradeLevel = itemData.UpgradeLevel;

                    InventoryScrollList.InsertData(slotData);
                }
            }
        } 
        
       private void SortInventory()
        {
            switch (_inventorySortType)
            {
                case InventorySortType.ItemGrade:
                    SortBtnTxt.text = "GRADE";

                    InventoryScrollList.SortDataList((a, b) =>
                    {
                        var itemA = a.data as CombineItemSlotData;
                        var itemB = b.data as CombineItemSlotData;

                        if (itemA == null || itemB == null) return 0;

                        int idA = itemA.ItemId;
                        int idB = itemB.ItemId;

                        int typeA  = idA / 10000;       
                        int typeB  = idB / 10000;

                        int gradeA = (idA / 1000) % 10; 
                        int gradeB = (idB / 1000) % 10;

                        int equipA = (idA / 100) % 10;  
                        int equipB = (idB / 100) % 10;

                        int kindA  = idA % 100;         
                        int kindB  = idB % 100;

                        int compareResult = gradeB.CompareTo(gradeA);
                        
                        if (compareResult == 0)
                            compareResult = typeA.CompareTo(typeB);
                        if (compareResult == 0)
                            compareResult = equipB.CompareTo(equipA);
                        if (compareResult == 0)
                            compareResult = kindB.CompareTo(kindA);
                        if (compareResult == 0)
                            compareResult = itemB.UpgradeLevel.CompareTo(itemA.UpgradeLevel);
                        if (compareResult == 0)
                            compareResult = itemB.SerialNumber.CompareTo(itemA.SerialNumber);

                        return compareResult;
                    });
                    break;
                case InventorySortType.ItemType:
                    SortBtnTxt.text = "TYPE";

                    InventoryScrollList.SortDataList((a, b) =>
                    {
                        var itemA = a.data as CombineItemSlotData;
                        var itemB = b.data as CombineItemSlotData;

                        if (itemA == null || itemB == null) return 0;

                        int idA = itemA.ItemId;
                        int idB = itemB.ItemId;

                        int typeA  = idA / 10000;       
                        int typeB  = idB / 10000;

                        int gradeA = (idA / 1000) % 10; 
                        int gradeB = (idB / 1000) % 10;

                        int equipA = (idA / 100) % 10;  
                        int equipB = (idB / 100) % 10;

                        int kindA  = idA % 100;         
                        int kindB  = idB % 100;

                        int compareResult = typeA.CompareTo(typeB);
                        if (compareResult == 0)
                            compareResult = gradeB.CompareTo(gradeA);
                        if (compareResult == 0)
                            compareResult = equipB.CompareTo(equipA);
                        if (compareResult == 0)
                            compareResult = kindB.CompareTo(kindA);
                        if (compareResult == 0)
                            compareResult = itemB.UpgradeLevel.CompareTo(itemA.UpgradeLevel);
                        if (compareResult == 0)
                            compareResult = itemB.SerialNumber.CompareTo(itemA.SerialNumber);

                        return compareResult;
                    });
                    break;
                default:
                    break;
            }
        }

        private void SetCombineItemInventory(int targetItemId)
        {
            InventoryScrollList.Clear();

            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData != null)
            {
                foreach (var itemData in userInventoryData.InventoryItemDataList)
                {
                    if (userInventoryData.IsEquipped(itemData.SerialNumber) || itemData.UpgradeLevel < 5)
                        continue;

                    if (itemData.ItemId != targetItemId)
                        continue;

                    if (_combineTargetItemData != null && itemData.SerialNumber == _combineTargetItemData.SerialNumber)
                        continue;
                    if (_combineMaterialItemData != null && itemData.SerialNumber == _combineMaterialItemData.SerialNumber)
                        continue;

                    var slotData = new CombineItemSlotData();
                    slotData.SerialNumber = itemData.SerialNumber;
                    slotData.ItemId = itemData.ItemId;
                    slotData.UpgradeLevel = itemData.UpgradeLevel;

                    InventoryScrollList.InsertData(slotData);
                }
            }
        }

        public async void SetCombineItem(CombineItemSlotData slotData)
        {
            if (slotData == null)
                return;

            if (_combineTargetItemData == null)
            {
                _combineTargetItemData = slotData;
                await SetCombineTargetItem(_combineTargetItemData);

                SetCombineItemInventory(_combineTargetItemData.ItemId);
                return;
            }

            if (_combineMaterialItemData == null)
            {
                if (slotData.ItemId != _combineTargetItemData.ItemId)
                    return;

                _combineMaterialItemData = slotData;
                await SetCombineMaterialItem(_combineMaterialItemData);

                SetCombineItemInventory(_combineTargetItemData.ItemId);
            }
        }

        private async Task SetCombineTargetItem(CombineItemSlotData data)
        {
            CombineTargetTxt.text =
                $"+{data.UpgradeLevel}  {DataTableManager.Instance.GetItemData(data.ItemId).ItemName}";

            CancelTargetBtn.gameObject.SetActive(true);
            CombineTargetBg.gameObject.SetActive(true);
            CombineTargetImg.gameObject.SetActive(true);
            CombineTargetTxt.gameObject.SetActive(true);

            var itemGrade = (ItemGrade)((data.ItemId / 1000) % 10);
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
                CombineTargetBg.color = color;
            }

            StringBuilder sb = new StringBuilder(data.ItemId.ToString());
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
                    CombineTargetImg.sprite = itemIconSprite;
                }
            }
        }

        private async Task SetCombineMaterialItem(CombineItemSlotData data)
        {
            CombineMaterialTxt.text =
                $"+{data.UpgradeLevel}  {DataTableManager.Instance.GetItemData(data.ItemId).ItemName}";

            CancelMaterialBtn.gameObject.SetActive(true);
            CombineMaterialBg.gameObject.SetActive(true);
            CombineMaterialImg.gameObject.SetActive(true);
            CombineMaterialTxt.gameObject.SetActive(true);

            var itemGrade = (ItemGrade)((data.ItemId / 1000) % 10);
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
                CombineMaterialBg.color = color;
            }

            StringBuilder sb = new StringBuilder(data.ItemId.ToString());
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
                    CombineMaterialImg.sprite = itemIconSprite;
                }
            }
        }

        private void ResetTarget()
        {
            _combineTargetItemData = null;
            _combineMaterialItemData = null;

            CancelTargetBtn.gameObject.SetActive(false);
            CombineTargetImg.gameObject.SetActive(false);
            CombineTargetBg.gameObject.SetActive(false);
            CombineTargetTxt.gameObject.SetActive(false);

            CancelMaterialBtn.gameObject.SetActive(false);
            CombineMaterialImg.gameObject.SetActive(false);
            CombineMaterialBg.gameObject.SetActive(false);
            CombineMaterialTxt.gameObject.SetActive(false);
        }

        private void ResetMaterial()
        {
            _combineMaterialItemData = null;

            CancelMaterialBtn.gameObject.SetActive(false);
            CombineMaterialImg.gameObject.SetActive(false);
            CombineMaterialBg.gameObject.SetActive(false);
            CombineMaterialTxt.gameObject.SetActive(false);

            SetCombineItemInventory(_combineTargetItemData.ItemId);
        }

        public void OnClickSortBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            switch (_inventorySortType)
            {
                case InventorySortType.ItemGrade:
                    _inventorySortType = InventorySortType.ItemType;
                    break;
                case InventorySortType.ItemType:
                    _inventorySortType = InventorySortType.ItemGrade;
                    break;
                default:
                    break;
            }

            SortInventory();
        }
        
        public void OnClickCombineBtn()
        {
            if (_combineTargetItemData == null || _combineMaterialItemData == null)
            {
                Logger.Log("Combine items are not selected.");
                return;
            }

            if (_combineTargetItemData.ItemId != _combineMaterialItemData.ItemId)
            {
                Logger.Log("Only same ItemId can be combined.");
                return;
            }
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            int itemGrade = (_combineTargetItemData.ItemId / 1000) % 10;
            long combineCost = CombineCost[itemGrade];
            
            var uiData = new ConfirmUIData(); 
            uiData.ConfirmType = ConfirmType.OK_CANCEL; 
            uiData.TitleTxt = "Try Combine"; 
            uiData.DescTxt = $"Combine [{DataTableManager.Instance.GetItemData(_combineTargetItemData.ItemId).ItemName}]"+
                             $"\nCombine cost : {combineCost} G"; 
            uiData.OKBtnTxt = "OK"; 
            uiData.CancelBtnTxt = "Cancel"; 
            uiData.OnClickOKBtn += () =>
            {
                var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
                if (userGoodsData == null)
                {
                    Logger.LogError("UserGoodsData is null");
                    return;
                }

                if (userGoodsData.Gold < combineCost)
                {
                    var uiData = new ConfirmUIData();
                    uiData.ConfirmType = ConfirmType.OK;
                    uiData.TitleTxt = "Not enough Gold";
                    uiData.DescTxt = $"You don't have enough gold.\nGold you have: {userGoodsData.Gold} G\nGold you need: {combineCost} G";
                    uiData.OKBtnTxt = "OK";
                    uiData.CloseFirst = true;

                    UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
                    return;
                }

                userGoodsData.Gold -= combineCost;
                userGoodsData.SaveData();

                var goldUpdateMsg = new GoldUpdateMsg();
                Messenger.Default.Publish(goldUpdateMsg);
                
                if (CombineManager.Instance.TryCombineItem(_combineTargetItemData, _combineMaterialItemData))
                {
                    OnClickCancelTargetBtn();
                }
                else
                {
                    OnClickCancelMaterialBtn();
                }
            };
            uiData.CloseFirst = true;
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }

        public void OnClickCancelTargetBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.un_equipped);
            ResetTarget();
        }

        public void OnClickCancelMaterialBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.un_equipped);
            ResetMaterial();
        }

        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();

            ResetTarget();
            
            var inventoryUI = UIManager.Instance.GetActiveUI<InventoryUI>().GetComponent<InventoryUI>();
            inventoryUI.SetInventory();
            inventoryUI.SortInventory();
        }

        #region COMBINE INFO

        
        private readonly long[] CombineCost =
        {
            0,
            10000,
            20000,    
            50000,    
            100000,    
            200000,   
        };

        #endregion
    }
}
