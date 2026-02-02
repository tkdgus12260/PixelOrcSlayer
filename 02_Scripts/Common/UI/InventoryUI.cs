using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gpm.Ui;
using TMPro;
using UnityEngine;

namespace PixelSurvival
{
    public enum InventorySortType
    {
        ItemGrade,
        ItemType,
    }

    public class InventoryUI : BaseUI
    {
        public EquippedItemSlot WeaponSlot;
        public EquippedItemSlot SecondarySlot;
        public EquippedItemSlot NecklaceSlot;
        public EquippedItemSlot RingSlot;

        public InfiniteScroll InventoryScrollList;
        public TextMeshProUGUI SortBtnTxt;

        private InventorySortType _inventorySortType = InventorySortType.ItemGrade;

        private GameObject _playerObj;
        public RectTransform PlayerContainerTrs;
        
        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            SetEquippedItems();
            SetInventory();
            SortInventory();
            
            PlayerManager.Instance.SetPlayerContainerPos(PlayerContainerTrs);
        }

        private async void SetPlayer()
        {
                _playerObj = await PlayerManager.Instance.LoadInventoryPlayer("InventoryPlayer");
                
                if (_playerObj == null)
                {
                    Logger.LogError("InventoryPlayer instantiate failed.");
                    return;
                }
        }
        
        private void SetEquippedItems()
        {
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("UserInventoryData does not exist");
                return;
            }

            if (userInventoryData.EquippedWeaponData != null)
            {
                WeaponSlot.SetInfo(userInventoryData.EquippedWeaponData);
            }
            else
            {
                WeaponSlot.ClearItem();
            }

            if (userInventoryData.EquippedSecondaryData != null)
            {
                SecondarySlot.SetInfo(userInventoryData.EquippedSecondaryData);
            }
            else
            {
                SecondarySlot.ClearItem();
            }

            if (userInventoryData.EquippedNecklaceData != null)
            {
                NecklaceSlot.SetInfo(userInventoryData.EquippedNecklaceData);
            }
            else
            {
                NecklaceSlot.ClearItem();
            }

            if (userInventoryData.EquippedRingData != null)
            {
                RingSlot.SetInfo(userInventoryData.EquippedRingData);
            }
            else
            {
                RingSlot.ClearItem();
            }
        }

        public void SetInventory()
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

                    var itemSlotData = new InventoryItemSlotData();
                    itemSlotData.SerialNumber = itemData.SerialNumber;
                    itemSlotData.ItemId = itemData.ItemId;
                    itemSlotData.UpgradeLevel = itemData.UpgradeLevel;
                    InventoryScrollList.InsertData(itemSlotData);
                }
            }

            if (_playerObj == null)
            {
                SetPlayer();
            }
            else
            {
                PlayerManager.Instance.ShowInventoryPlayer();   
            }
        }

        public void SortInventory()
        {
            switch (_inventorySortType)
            {
                case InventorySortType.ItemGrade:
                    SortBtnTxt.text = "GRADE";

                    InventoryScrollList.SortDataList((a, b) =>
                    {
                        var itemA = a.data as InventoryItemSlotData;
                        var itemB = b.data as InventoryItemSlotData;

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
                        var itemA = a.data as InventoryItemSlotData;
                        var itemB = b.data as InventoryItemSlotData;

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
            Logger.Log($"{GetType()}::OnClickUpgradeBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<UpgradeItemUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }

        public void OnClickCombineBtn()
        {
            Logger.Log($"{GetType()}::OnClickCombineBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<CombineItemUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }

        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();
            
            PlayerManager.Instance.HideInventoryPlayer();
        }

        public void OnClickEquipmentCollectionBtn()
        {
            Logger.Log($"{GetType()}::OnClickEquipmentCollectionBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<EquipmentCollectionUI>(uiData);
            
            PlayerManager.Instance.HideInventoryPlayer();
        }
        
        public void OnEquipItem(int itemId)
        {
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData does not exist");
                return;
            }

            var itemType = (ItemType)(itemId / 10000);
            switch (itemType)
            {
                case ItemType.weapon:
                    WeaponSlot.SetInfo(userInventoryData.EquippedWeaponData);
                    break;
                case ItemType.Secondary:
                    SecondarySlot.SetInfo(userInventoryData.EquippedSecondaryData);
                    break;
                case ItemType.Necklace:
                    NecklaceSlot.SetInfo(userInventoryData.EquippedNecklaceData);
                    break;
                case ItemType.Ring:
                    RingSlot.SetInfo(userInventoryData.EquippedRingData);
                    break;
                default:
                    break;
            }

            SetInventory();
            SortInventory();
        }

        public void OnUnequipItem(int itemId)
        {
            var itemType = (ItemType)(itemId / 10000);
            switch (itemType)
            {
                case ItemType.weapon:
                    WeaponSlot.ClearItem();
                    break;
                case ItemType.Secondary:
                    SecondarySlot.ClearItem();
                    break;
                case ItemType.Necklace:
                    NecklaceSlot.ClearItem();
                    break;
                case ItemType.Ring:
                    RingSlot.ClearItem();
                    break;
                default:
                    break;
            }

            SetInventory();
            SortInventory();
        }
    }
}