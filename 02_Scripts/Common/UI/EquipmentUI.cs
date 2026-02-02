using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperMaxim.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class EquipmentUIData : BaseUIData
    {
        public long SerialNumber;
        public int ItemId;
        public int UpgradeLevel;
        public bool IsEquipped;
        public bool IsInventory;
    }

    public class EquipmentUI : BaseUI
    {
        public Image ItemGradeBg;
        public Image ItemIcon;

        public Button SellBtn;
        
        public TextMeshProUGUI ItemGradeTxt;
        public TextMeshProUGUI ItemNameTxt;
        public TextMeshProUGUI EquipBtnTxt;
        public TextMeshProUGUI DamageTxt;
        public TextMeshProUGUI HpTxt;
        public TextMeshProUGUI DescriptionTxt;
        
        private EquipmentUIData _equipmentUIData;

        public override async void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            _equipmentUIData = uiData as EquipmentUIData;
            if (_equipmentUIData == null)
            {
                Logger.LogError("_equipmentUIData is invalid");
                return;
            }

            var itemData = DataTableManager.Instance.GetItemData(_equipmentUIData.ItemId);
            if (itemData == null)
            {
                Logger.LogError($"itemData is Invalid. ItemId : {_equipmentUIData.ItemId}");
                return;
            }
            
            var skillData = DataTableManager.Instance.GetSkillData(itemData.ItemId);
            if (skillData == null)
            {
                Logger.LogError($"skillData is Invalid. ItemId : {itemData.ItemId}");
                return;
            }
            
            var itemDamage = itemData.Damage;
            var itemHp = itemData.Hp;
            var upgradeValueIncrease = itemData.ValueIncrease;
            
            if (_equipmentUIData.UpgradeLevel > 0)
            {
                if (itemDamage > 0)
                    DamageTxt.text = $"{itemDamage} <color=#FFB300>+{_equipmentUIData.UpgradeLevel * upgradeValueIncrease}</color>";
                else
                    DamageTxt.text = itemDamage.ToString();

                if (itemHp > 0)
                    HpTxt.text = $"{itemHp} <color=#FFB300>+{_equipmentUIData.UpgradeLevel * upgradeValueIncrease}</color>";
                else
                    HpTxt.text = itemHp.ToString();
            }
            else
            {
                DamageTxt.text = itemDamage.ToString();
                HpTxt.text = itemHp.ToString();
            }
            
            var itemDescriptionTemplate = itemData.Description;
            var itemDescription = GlobalDefine.DescriptionFormat(itemDescriptionTemplate, new Dictionary<string, object>
            {
                ["objectCount"] = skillData.ObjectCount,
                ["cooldown"] = skillData.Cooldown
            });
            
            DescriptionTxt.text = itemDescription;
            
            var itemGrade = (ItemGrade)((_equipmentUIData.ItemId / 1000) % 10);
         
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

            StringBuilder sb = new StringBuilder(_equipmentUIData.ItemId.ToString());
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

                ItemNameTxt.text = $"+{_equipmentUIData.UpgradeLevel}  {itemData.ItemName}";
                EquipBtnTxt.text = _equipmentUIData.IsEquipped ? "Unequip" : "Equip";
            }
            
            SellBtn.gameObject.SetActive(_equipmentUIData.IsInventory);
        }

        public void OnClickEquipBtn()
        {
            
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("userInventoryData is invalid");
                return;
            }

            if (_equipmentUIData.IsEquipped)
            {
                
                AudioManager.Instance.PlaySFX(SFX.un_equipped);
                userInventoryData.UnequipItem(_equipmentUIData.SerialNumber, _equipmentUIData.ItemId);
            }
            else
            {
                
                AudioManager.Instance.PlaySFX(SFX.equipped);
                userInventoryData.EquipItem(_equipmentUIData.SerialNumber, _equipmentUIData.ItemId, _equipmentUIData.UpgradeLevel);
            }

            userInventoryData.SaveData();

            var inventoryUI = UIManager.Instance.GetActiveUI<InventoryUI>() as InventoryUI;
            if (inventoryUI != null)
            {
                if (_equipmentUIData.IsEquipped)
                {
                    inventoryUI.OnUnequipItem(_equipmentUIData.ItemId);
                }
                else
                {
                    inventoryUI.OnEquipItem(_equipmentUIData.ItemId);
                }
            }

            CloseUI();
        }

        public void OnClickSellBtn()
        {
            var itemData = DataTableManager.Instance.GetItemData(_equipmentUIData.ItemId);
            if (itemData == null) return;
            
            var itemGrade = (ItemGrade)((_equipmentUIData.ItemId / 1000) % 10);

            // 누적 강화비용
            long upgradeRefund = GetTotalUpgradeCost(itemGrade, _equipmentUIData.UpgradeLevel);

            // 최종 판매가 = 기본 판매가 + 강화비용 누적
            long sellGold = itemData.SellPrice + upgradeRefund;

            var uiData = new ConfirmUIData();
            uiData.ConfirmType = ConfirmType.OK_CANCEL;
            uiData.TitleTxt = "Sell Item";
            uiData.DescTxt = $"Would you like to sell the [{itemData.ItemName}]\n Sell price : {sellGold} G";
            uiData.OKBtnTxt = "OK";
            uiData.OnClickOKBtn = () =>
            {
                var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
                if (userInventoryData != null)
                {
                    userInventoryData.RemoveItem(_equipmentUIData.SerialNumber);
                    userInventoryData.SaveData();
                }

                var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
                if (userGoodsData == null) return;

                userGoodsData.Gold += sellGold;
                userGoodsData.SaveData();

                var goldUpdateMsg = new GoldUpdateMsg();
                Messenger.Default.Publish(goldUpdateMsg);

                CloseUI();

                var inventoryUI = UIManager.Instance.GetActiveUI<InventoryUI>().GetComponent<InventoryUI>();
                inventoryUI.SetInventory();
                inventoryUI.SortInventory();
            };
            uiData.CancelBtnTxt = "Cancel";
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }


        public override void OnClickCloseButton()
        {
            base.OnClickCloseButton();
            
            PlayerManager.Instance.ShowInventoryPlayer();
        }
        
        private long GetTotalUpgradeCost(ItemGrade grade, int upgradeLevel)
        {
            if (upgradeLevel <= 0) return 0;

            int maxStep = Mathf.Min(upgradeLevel, GlobalDefine.UpgradeCost.GetLength(1)); // 10 칸 방어
            int gradeIndex = Mathf.Clamp((int)grade, 0, GlobalDefine.UpgradeCost.GetLength(0) - 1);

            long sum = 0;
            for (int step = 0; step < maxStep; step++)
                sum += GlobalDefine.UpgradeCost[gradeIndex, step];

            return sum;
        }

    }
}