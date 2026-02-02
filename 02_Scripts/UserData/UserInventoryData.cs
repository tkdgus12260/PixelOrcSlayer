using System;
using System.Collections;
using System.Collections.Generic;
using SuperMaxim.Messaging;
using UnityEngine;

namespace PixelSurvival
{
    [Serializable]
    public class UserItemData
    {
        public long SerialNumber;
        public int ItemId;
        public int UpgradeLevel;

        public UserItemData(long serialNumber, int itemId, int upgradeLevel)
        {
            SerialNumber = serialNumber;
            ItemId = itemId;
            UpgradeLevel = upgradeLevel;
        }
    }
    
    [Serializable]
    public class UserInventoryDataListWrapper
    {
        public List<UserItemData> InventoryItemDataList;
    }

    public class UserInventoryData : IUserData
    {
        public bool IsLoaded { get; set; }
        public UserItemData EquippedWeaponData { get; set; }
        public UserItemData EquippedSecondaryData { get; set; }
        public UserItemData EquippedNecklaceData { get; set; }
        public UserItemData EquippedRingData { get; set; }

        public List<UserItemData> InventoryItemDataList { get; set; } = new List<UserItemData>();

        public List<long> EquippedItemList { get; set; } = new();

        
        public void SetDefaultData()
        {
            Logger.Log($"{GetType()}::SetDefaultData");
            SetEquippedItemList();
            
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                + UnityEngine.Random.Range(0, 9999).ToString("D4")), 11101, 0));
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                + UnityEngine.Random.Range(0, 9999).ToString("D4")), 11201, 0));
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                + UnityEngine.Random.Range(0, 9999).ToString("D4")), 21101, 0));
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                + UnityEngine.Random.Range(0, 9999).ToString("D4")), 31101, 0));
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                + UnityEngine.Random.Range(0, 9999).ToString("D4")), 41101, 0));

            foreach (var itemData in InventoryItemDataList)
            {
                if (itemData.ItemId == 11201)
                    continue;
                
                EquipItem(itemData.SerialNumber, itemData.ItemId, itemData.UpgradeLevel);
            }
        }

        public void LoadData()
        {
            Logger.Log($"{GetType()}::LoadData");

            FirebaseManager.Instance.LoadUserData<UserInventoryData>(() =>
            {
                IsLoaded = true;
                SetEquippedItemList();
            });
        }

        public void SaveData()
        {
            Logger.Log($"{GetType()}::SaveData");
            FirebaseManager.Instance.SaveUserData<UserInventoryData>(ConvertDataToFirestoreDic());
        }

        private Dictionary<string, object> ConvertDataToFirestoreDic()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                { "EquippedWeaponData", ConvertUserItemDataDic(EquippedWeaponData) },
                { "EquippedSecondaryData", ConvertUserItemDataDic(EquippedSecondaryData) },
                { "EquippedNecklaceData", ConvertUserItemDataDic(EquippedNecklaceData) },
                { "EquippedRingData", ConvertUserItemDataDic(EquippedRingData) },
                { "InventoryItemDataList", ConvertInventoryListToDic(InventoryItemDataList) }
            };
            
            return dic;
        }

        private Dictionary<string, object> ConvertUserItemDataDic(UserItemData userItemData)
        {
            if (userItemData == null)
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                { "SerialNumber", userItemData.SerialNumber },
                { "ItemId", userItemData.ItemId },
                { "UpgradeLevel", userItemData.UpgradeLevel },
            };
        }

        private List<Dictionary<string, object>> ConvertInventoryListToDic(List<UserItemData> inventoryItemDataList)
        {
            List<Dictionary<string, object>> convertedInventoryList = new List<Dictionary<string, object>>();
            foreach (var item in inventoryItemDataList)
            {
                convertedInventoryList.Add(ConvertUserItemDataDic(item));
            }
            
            return convertedInventoryList;
        }
        
        public void SetData(Dictionary<string, object> firestoreDic)
        {
            ConvertFirestoreDicToData(firestoreDic);
        }

        private void ConvertFirestoreDicToData(Dictionary<string, object> dic)
        {
            if (dic.TryGetValue("EquippedWeaponData", out var equippedWeaponDataValue) &&
                equippedWeaponDataValue is Dictionary<string, object> equippedWeaponDataDic)
            {
                EquippedWeaponData = ConvertDicToUserItemData(equippedWeaponDataDic);
            }
            if (dic.TryGetValue("EquippedSecondaryData", out var equippedSecondaryDataValue) &&
                equippedSecondaryDataValue is Dictionary<string, object> equippedSecondaryDataDic)
            {
                EquippedSecondaryData = ConvertDicToUserItemData(equippedSecondaryDataDic);
            }
            if (dic.TryGetValue("EquippedNecklaceData", out var equippedNecklaceDataValue) &&
                equippedNecklaceDataValue is Dictionary<string, object> equippedNecklaceDataDic)
            {
                EquippedNecklaceData = ConvertDicToUserItemData(equippedNecklaceDataDic);
            }
            if (dic.TryGetValue("EquippedRingData", out var equippedRingDataValue) &&
                equippedRingDataValue is Dictionary<string, object> equippedRingDataDic)
            {
                EquippedRingData = ConvertDicToUserItemData(equippedRingDataDic);
            }

            if (dic.TryGetValue("InventoryItemDataList", out var inventoryItemDataListValue) &&
                inventoryItemDataListValue is List<object> inventoryItemDataList)
            {
                InventoryItemDataList = ConvertDicToInventoryList(inventoryItemDataList);
            }
        }

        private UserItemData ConvertDicToUserItemData(Dictionary<string, object> dic)
        {
            if (dic == null)
            {
                return null;
            }

            long itemSerialNumber = 0;
            if (dic.TryGetValue("SerialNumber", out var serialNumberValue) && serialNumberValue is long serialNumber)
            {
                itemSerialNumber = serialNumber;
            }
            
            int itemId = 0;
            if (dic.TryGetValue("ItemId", out var itemIdValue) && itemIdValue != null)
            {
                itemId = Convert.ToInt32(itemIdValue);
            }
            
            int upgradeLevel = 0;
            if (dic.TryGetValue("UpgradeLevel", out var upgradeLevelValue) && upgradeLevelValue != null)
            {
                upgradeLevel = Convert.ToInt32(upgradeLevelValue);
            }

            if (itemSerialNumber == 0 || itemId == 0 || upgradeLevel == -1)
            {
                Logger.LogError("Serial Number and ItemId are null.");
                return null;
            }
            
            return new UserItemData(itemSerialNumber, itemId, upgradeLevel);
        }

        private List<UserItemData> ConvertDicToInventoryList(List<object> list)
        {
            List<UserItemData> inventoryList = new List<UserItemData>();

            foreach (var item in list)
            {
                if (item is Dictionary<string, object> itemDic)
                {
                    inventoryList.Add(ConvertDicToUserItemData(itemDic));
                }
            }
            
            return inventoryList;
        }

        public void SetEquippedItemList()
        {
            if (EquippedWeaponData != null)
            {
                var itemData = DataTableManager.Instance.GetItemData(EquippedWeaponData.ItemId);
                if (itemData != null)
                {
                    EquippedItemList.Add(EquippedWeaponData.SerialNumber);
                }
            }

            if (EquippedSecondaryData != null)
            {
                var itemData = DataTableManager.Instance.GetItemData(EquippedSecondaryData.ItemId);
                if (itemData != null)
                {
                    EquippedItemList.Add(EquippedSecondaryData.SerialNumber);
                }
            }

            if (EquippedNecklaceData != null)
            {
                var itemData = DataTableManager.Instance.GetItemData(EquippedNecklaceData.ItemId);
                if (itemData != null)
                {
                    EquippedItemList.Add(EquippedNecklaceData.SerialNumber);
                }
            }

            if (EquippedRingData != null)
            {
                var itemData = DataTableManager.Instance.GetItemData(EquippedRingData.ItemId);
                if (itemData != null)
                {
                    EquippedItemList.Add(EquippedRingData.SerialNumber);
                }
            }
        }

        public bool IsEquipped(long serialNumber)
        {
            return EquippedItemList.Contains(serialNumber);
        }

        public void EquipItem(long serialNumber, int itemId, int upgradeLevel)
        {
            var itemData = DataTableManager.Instance.GetItemData(itemId);
            if (itemData == null)
            {
                Logger.Log($"Item data does not exist. ItemId:{itemId}");
                return;
            }

            var itemType = (ItemType)(itemId / 10000);
            switch (itemType)
            {
                case ItemType.weapon:
                    if (EquippedWeaponData != null)
                    {
                        EquippedItemList.Remove(EquippedWeaponData.SerialNumber);
                        EquippedWeaponData = null;
                    }

                    EquippedWeaponData = new UserItemData(serialNumber, itemId, upgradeLevel);
                    break;
                case ItemType.Secondary:
                    if (EquippedSecondaryData != null)
                    {
                        EquippedItemList.Remove(EquippedSecondaryData.SerialNumber);
                        EquippedSecondaryData = null;
                    }

                    EquippedSecondaryData = new UserItemData(serialNumber, itemId, upgradeLevel);
                    break;
                case ItemType.Necklace:
                    if (EquippedNecklaceData != null)
                    {
                        EquippedItemList.Remove(EquippedNecklaceData.SerialNumber);
                        EquippedNecklaceData = null;
                    }

                    EquippedNecklaceData = new UserItemData(serialNumber, itemId, upgradeLevel);
                    break;
                case ItemType.Ring:
                    if (EquippedRingData != null)
                    {
                        EquippedItemList.Remove(EquippedRingData.SerialNumber);
                        EquippedRingData = null;
                    }

                    EquippedRingData = new UserItemData(serialNumber, itemId, upgradeLevel);
                    break;
                default:
                    break;
            }

            EquippedItemList.Add(serialNumber);
        }

        public void UnequipItem(long serialNumber, int itemId)
        {
            var itemType = (ItemType)(itemId / 10000);
            switch (itemType)
            {
                case ItemType.weapon:
                    EquippedWeaponData = null;
                    break;
                case ItemType.Secondary:
                    EquippedSecondaryData = null;
                    break;
                case ItemType.Necklace:
                    EquippedNecklaceData = null;
                    break;
                case ItemType.Ring:
                    EquippedRingData = null;
                    break;
                default:
                    break;
            }

            EquippedItemList.Remove(serialNumber);
        }
        
        public void UpdateInventoryItem(UserItemData userItemData)
        {
            if (userItemData == null)
            {
                Logger.LogError("userItemData is null.");
                return;
            }

            int index = InventoryItemDataList.FindIndex(x => x != null && x.SerialNumber == userItemData.SerialNumber);
            if (index < 0)
            {
                Logger.LogError($"Inventory item not found. SerialNumber: {userItemData.SerialNumber}");
                return;
            }

            InventoryItemDataList[index] = userItemData;
        }
        
        public void RemoveItem(long serialNumber)
        {
            if (IsEquipped(serialNumber))
            {
                Logger.LogError($"Cannot remove equipped item. SerialNumber: {serialNumber}");
                return;
            }

            int index = InventoryItemDataList.FindIndex(x => x != null && x.SerialNumber == serialNumber);
            if (index < 0)
            {
                Logger.LogError($"Inventory item not found. SerialNumber: {serialNumber}");
                return;
            }

            InventoryItemDataList.RemoveAt(index);
            return;
        }

        public void AcquireItem(int itemId, int upgradeLevel)
        {
            Logger.Log($"Item acquired. ItemId:{itemId} upgradeLevel:{upgradeLevel}");
            
            InventoryItemDataList.Add(new UserItemData(long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss") 
                                                                  + UnityEngine.Random.Range(0, 9999).ToString("D4")), itemId, upgradeLevel));
        }
        
        public IEnumerable<UserItemData> GetEquippedItemData()
        {
            if (EquippedWeaponData   != null) yield return EquippedWeaponData;
            if (EquippedSecondaryData!= null) yield return EquippedSecondaryData;
            if (EquippedNecklaceData != null) yield return EquippedNecklaceData;
            if (EquippedRingData     != null) yield return EquippedRingData;
        }
    }
}