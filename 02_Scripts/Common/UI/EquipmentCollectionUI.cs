using System.Collections;
using System.Collections.Generic;
using Gpm.Ui;
using TMPro;
using UnityEngine;

namespace PixelSurvival
{
    public class EquipmentCollectionUI : BaseUI
    {
        public InfiniteScroll CollectionScrollList;
        public TextMeshProUGUI SortBtnTxt;
        
        private InventorySortType _inventorySortType = InventorySortType.ItemGrade;

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            SetEquipmentCollection();
            SortEquipmentCollection();
        }

        private void SetEquipmentCollection()
        {
            CollectionScrollList.Clear();
            
            var itemDatas = DataTableManager.Instance.GetItemDatas();
            if (itemDatas != null)
            {
                foreach (var itemData in itemDatas)
                {
                    // 골드 보석 예외처리
                    if(itemData.ItemId == 1 || itemData.ItemId == 2)
                        continue;
                    
                    var skillData = DataTableManager.Instance.GetSkillData(itemData.ItemId);
                    if (skillData == null)
                    {
                        Logger.LogError($"skillData is Invalid. ItemId : {itemData.ItemId}");
                        return;
                    }

                    var collectionSlotData = new EquipmentCollectionItemSlotData();
                    collectionSlotData.ItemId = itemData.ItemId;
                    collectionSlotData.ItemName = itemData.ItemName;
                    collectionSlotData.Damage = itemData.Damage;
                    collectionSlotData.Hp = itemData.Hp;
                    var itemDescriptionTemplate = itemData.Description;
                    var itemDescription = GlobalDefine.DescriptionFormat(itemDescriptionTemplate, new Dictionary<string, object>
                    {
                        ["objectCount"] = skillData.ObjectCount,
                        ["cooldown"] = skillData.Cooldown
                    });
                    collectionSlotData.Description = itemDescription;
                    collectionSlotData.Sources = itemData.Sources;
                    CollectionScrollList.InsertData(collectionSlotData);
                }
            }
        }
        
         private void SortEquipmentCollection()
        {
            switch (_inventorySortType)
            {
                case InventorySortType.ItemGrade:
                    SortBtnTxt.text = "GRADE";

                    CollectionScrollList.SortDataList((a, b) =>
                    {
                        var itemA = a.data as EquipmentCollectionItemSlotData;
                        var itemB = b.data as EquipmentCollectionItemSlotData;
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

                        // Grade 내림차순 → Type 오름차순 → Equip 내림차순 → Kind 내림차순 → ItemId 내림차순
                        int compareResult = gradeB.CompareTo(gradeA);

                        if (compareResult == 0) compareResult = typeA.CompareTo(typeB);
                        if (compareResult == 0) compareResult = equipB.CompareTo(equipA);
                        if (compareResult == 0) compareResult = kindB.CompareTo(kindA);
                        if (compareResult == 0) compareResult = idB.CompareTo(idA);

                        return compareResult;
                    });
                    break;

                case InventorySortType.ItemType:
                    SortBtnTxt.text = "TYPE";

                    CollectionScrollList.SortDataList((a, b) =>
                    {
                        var itemA = a.data as EquipmentCollectionItemSlotData;
                        var itemB = b.data as EquipmentCollectionItemSlotData;
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

                        // Type 오름차순 → Grade 내림차순 → Equip 내림차순 → Kind 내림차순 → ItemId 내림차순
                        int compareResult = typeA.CompareTo(typeB);

                        if (compareResult == 0) compareResult = gradeB.CompareTo(gradeA);
                        if (compareResult == 0) compareResult = equipB.CompareTo(equipA);
                        if (compareResult == 0) compareResult = kindB.CompareTo(kindA);
                        if (compareResult == 0) compareResult = idB.CompareTo(idA);

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

            _inventorySortType = _inventorySortType == InventorySortType.ItemGrade
                ? InventorySortType.ItemType
                : InventorySortType.ItemGrade;

            SortEquipmentCollection();
        }
        
        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();

            PlayerManager.Instance.ShowInventoryPlayer();
        }
    }
}