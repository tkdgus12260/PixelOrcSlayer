using System.Text;
using Gpm.Ui;
using SuperMaxim.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class UpgradeItemUI : BaseUI
    {
        public Image UpgradeItemImg;
        public Image UpgradeItemBg;
        public TextMeshProUGUI UpgradeItemTxt;
        public Button CancelBtn;
        public Button UpgradeBtn;
        
        public InfiniteScroll InventoryScrollList;
        public TextMeshProUGUI SortBtnTxt;

        private InventorySortType _inventorySortType = InventorySortType.ItemGrade;

        private UpgradeItemSlotData _upgradeItemData;
        
        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);
            
            SetInventory();
            SortInventory();
            ResetUpgradeItem();
        }
        
        private void SetInventory()
        {
            InventoryScrollList.Clear();

            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData != null)
            {
                foreach (var itemData in userInventoryData.InventoryItemDataList)
                {
                    if (userInventoryData.IsEquipped(itemData.SerialNumber))
                    {
                        continue;
                    }

                    var itemSlotData = new UpgradeItemSlotData();
                    itemSlotData.SerialNumber = itemData.SerialNumber;
                    itemSlotData.ItemId = itemData.ItemId;
                    itemSlotData.UpgradeLevel = itemData.UpgradeLevel;
                    InventoryScrollList.InsertData(itemSlotData);
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
                        var itemA = a.data as UpgradeItemSlotData;
                        var itemB = b.data as UpgradeItemSlotData;

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
                        var itemA = a.data as UpgradeItemSlotData;
                        var itemB = b.data as UpgradeItemSlotData;

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
        
        public async void SetUpgradeItem(UpgradeItemSlotData userItemData)
        {
            _upgradeItemData = userItemData;

            UpgradeItemTxt.text = $"+{_upgradeItemData.UpgradeLevel}  {DataTableManager.Instance.GetItemData(_upgradeItemData.ItemId).ItemName}";
            
            CancelBtn.gameObject.SetActive(true);
            UpgradeItemBg.gameObject.SetActive(true);
            UpgradeItemImg.gameObject.SetActive(true);
            UpgradeItemTxt.gameObject.SetActive(true);

            var itemGrade = (ItemGrade)((_upgradeItemData.ItemId / 1000) % 10);
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
                UpgradeItemBg.color = color;
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
                    UpgradeItemImg.sprite = itemIconSprite;
                }
            }
        }
        
        private void ResetUpgradeItem()
        {
            _upgradeItemData = null;
            
            CancelBtn.gameObject.SetActive(false);
            UpgradeItemImg.gameObject.SetActive(false);
            UpgradeItemBg.gameObject.SetActive(false);
            UpgradeItemTxt.gameObject.SetActive(false);
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

        public void OnClickUpgradeBtn()
        {
            if (_upgradeItemData == null)
            {
                Logger.Log("UpgradeItemData is null");
                return;
            }

            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            int currLevel = _upgradeItemData.UpgradeLevel;
            if (currLevel < 0)
            {
                Logger.LogError($"UpgradeLevel is invalid: {currLevel}");
                return;
            }

            // 아이템 등급 가져오기.
            var itemGrade = (ItemGrade)((_upgradeItemData.ItemId / 1000) % 10);
            int maxLevel = (itemGrade == ItemGrade.Legendary) ? GlobalDefine.LegendaryMaxUpgradeLevel : GlobalDefine.MaxUpgradeLevel;

            // 최대 업그레이드 레벨 체크
            if (currLevel >= maxLevel)
            {
                var failUiData = new ConfirmUIData
                {
                    ConfirmType = ConfirmType.OK,
                    TitleTxt = "Upgrade Fail",
                    DescTxt = "Already maximum upgrade level.",
                    OKBtnTxt = "OK"
                };
                UIManager.Instance.OpenUIFromAA<ConfirmUI>(failUiData);
                return;
            }

            if (currLevel >= GlobalDefine.UpgradeChance.Length)
            {
                Logger.LogError($"UpgradeChance is not defined for level: {currLevel}");
                return;
            }

            // 업그레이드
            float chance = GlobalDefine.UpgradeChance[currLevel];

            int gradeIndex = (int)itemGrade;

            if (gradeIndex < 0 || gradeIndex >= GlobalDefine.UpgradeCost.GetLength(0) ||
                currLevel < 0 || currLevel >= GlobalDefine.UpgradeCost.GetLength(1))
            {
                Logger.LogError($"UpgradeCost out of range. grade={gradeIndex}, level={currLevel}");
                return;
            }

            long upgradeCost = GlobalDefine.UpgradeCost[gradeIndex, currLevel];

            var uiData = new ConfirmUIData
            {
                ConfirmType = ConfirmType.OK_CANCEL,
                TitleTxt = "Try Upgrade",
                DescTxt =
                    $"Upgrade [{DataTableManager.Instance.GetItemData(_upgradeItemData.ItemId).ItemName}]" +
                    $"\nUpgrade Success Rate : {chance * 100}%" +
                    $"\nUpgrade cost : {upgradeCost} G",
                OKBtnTxt = "OK",
                CancelBtnTxt = "Cancel",
                CloseFirst = true
            };

            // 업그레이드 시 업그레이드 비용, 내 골드 체크 후 진행.
            uiData.OnClickOKBtn += () =>
            {
                var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
                if (userGoodsData == null)
                {
                    Logger.LogError("UserGoodsData is null");
                    return;
                }

                if (userGoodsData.Gold < upgradeCost)
                {
                    var failUiData = new ConfirmUIData
                    {
                        ConfirmType = ConfirmType.OK,
                        TitleTxt = "Upgrade Fail",
                        DescTxt =
                            $"You don't have enough gold.\nGold you have: {userGoodsData.Gold} G\nGold you need: {upgradeCost} G",
                        OKBtnTxt = "OK",
                        CloseFirst = true
                    };
                    UIManager.Instance.OpenUIFromAA<ConfirmUI>(failUiData);
                    return;
                }

                userGoodsData.Gold -= upgradeCost;
                userGoodsData.SaveData();

                Messenger.Default.Publish(new GoldUpdateMsg());

                // 업그레이드 시도
                if (UpgradeManager.Instance.TryUpgradeItem(_upgradeItemData, chance))
                {
                    UpgradeItemTxt.text =
                        $"+{_upgradeItemData.UpgradeLevel}  {DataTableManager.Instance.GetItemData(_upgradeItemData.ItemId).ItemName}";

                    SetInventory();
                    SortInventory();
                }
            };

            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }

        
        public void OnClickCancelBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.un_equipped);
            ResetUpgradeItem();
        }

        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();

            ResetUpgradeItem();
            
            var inventoryUI = UIManager.Instance.GetActiveUI<InventoryUI>().GetComponent<InventoryUI>();
            inventoryUI.SetInventory();
            inventoryUI.SortInventory();
        }
    }
}