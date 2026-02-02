using System;
using System.Collections.Generic;
using System.Linq;
using SuperMaxim.Messaging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class LoadProductDataTableMsg
    {
        
    }
    
    public class DataTableManager : SingletonBehaviour<DataTableManager>
    {
        public void LoadDataTables()
        {
            LoadChapterDataTable();                 // 챕터 데이터
            LoadStageDataTable();                   // 스테이지 데이터
            LoadItemDataTable();                    // 아이템 데이터
            LoadEnemyDataTable();                   // 적 데이터 
            LoadEnemyCostumeDataTable();            // 적 커스터마이징 데이터
            LoadPlayerDataTable();                  // 플레이어 데이터
            LoadSkillDataTable();                   // 스킬 데이터
            LoadAchievementDataTable();             // 업적 데이터
            LoadProductDataTable();                 // 상점 데이터
            LoadChestRewardProbabilityDataTable();  // 확률형 아이템 데이터
        }

        #region CHAPTER_DATA

        private const string CHAPTER_DATA_TABLE = "ChapterDataTable";
        [SerializeField]
        private List<ChapterData> _chapterDataTable = new List<ChapterData>();

        private async void LoadChapterDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(CHAPTER_DATA_TABLE);

            foreach (var data in parsedDataTable)
            {
                var chapterData = new ChapterData
                {
                    ChapterNo = Convert.ToInt32(data["chapter_no"]),
                    TotalStage = Convert.ToInt32(data["total_stage"]),
                    ChapterName = data["chapter_name"].ToString(),
                    StageName = data["stage_name"].ToString(),
                    ChapterRewardGem = Convert.ToInt32(data["chapter_reward_gem"]),
                    ChapterRewardGold = Convert.ToInt32(data["chapter_reward_gold"]),
                };
                
                if (data.ContainsKey("drop_items"))
                {
                    var raw = data["drop_items"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var tokens = raw.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var t in tokens)
                        {
                            if (int.TryParse(t.Trim(), out var id))
                            {
                                chapterData.DropItemIds.Add(id);
                            }
                        }
                    }
                }
                
                _chapterDataTable.Add(chapterData);
            }
        }

        public ChapterData GetChapterData(int chapterNo)
        {
            return _chapterDataTable.Where(item => item.ChapterNo == chapterNo).FirstOrDefault();
        }

        #endregion

        #region STAGE_DATA

        private const string STAGE_DATA_TABLE = "StageDataTable";
        [SerializeField]
        private List<StageData> StageDataTable = new List<StageData>();

        private async void LoadStageDataTable()
        {
            StageDataTable.Clear();

            var parsedDataTable = await CSVReader.ReadFromAA(STAGE_DATA_TABLE);
            foreach (var data in parsedDataTable)
            {
                var sd = new StageData
                {
                    ChapterNo = Convert.ToInt32(data["chapter_no"]),
                    StageNo = Convert.ToInt32(data["stage_no"]),
                    EnemyName = data["enemy_name"].ToString(),
                    EnemyCount = Convert.ToInt32(data["enemy_count"]),
                };

                StageDataTable.Add(sd);
            }
        }

        public List<StageData> GetStageDatas(int chapterNo, int stageNo)
        {
            return StageDataTable.Where(s => s.ChapterNo == chapterNo && s.StageNo == stageNo).ToList();
        }

        #endregion

        #region ITEM_DATA

        private const string ITEM_DATA_TABLE = "ItemDataTable";
        [SerializeField]
        private List<ItemData> ItemDataTable = new List<ItemData>();

        private async void LoadItemDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(ITEM_DATA_TABLE);

            foreach (var data in parsedDataTable)
            {
                var itemData = new ItemData
                {
                    ItemId = Convert.ToInt32(data["item_id"]),
                    ItemName = data["item_name"].ToString(),
                    DropRate = Convert.ToSingle(data["drop_rate"]),
                    Damage = Convert.ToInt32(data["damage"]),
                    Hp = Convert.ToInt32(data["hp"]),
                    ValueIncrease = Convert.ToInt32(data["value_increase"]),
                    SellPrice = Convert.ToInt32(data["sell_price"]),
                    Description = data["description"].ToString(),
                    Sources = data["sources"].ToString(),
                };

                ItemDataTable.Add(itemData);
            }
        }

        public ItemData GetItemData(int itemId)
        {
            return ItemDataTable.Where(item => item.ItemId == itemId).FirstOrDefault();
        }

        public List<ItemData> GetItemDatas()
        {
            return ItemDataTable;
        }

        #endregion

        #region ENEMY_DATA

        private const string ENEMY_DATA_TABLE = "EnemyDataTable";
        [SerializeField]
        private List<EnemyData> EnemyDataTable = new List<EnemyData>();
        
        private async void LoadEnemyDataTable()
        {
            EnemyDataTable.Clear();

            var parsedDataTable = await CSVReader.ReadFromAA(ENEMY_DATA_TABLE);
            foreach (var data in parsedDataTable)
            {
                var enemyData = new EnemyData
                {
                    EnemyAddress = data["enemy_address"].ToString(),
                    EnemyName = data["enemy_name"].ToString(),
                    EnemyType = Enum.Parse<EnemyType>(data["enemy_type"].ToString(), true),
                    MaxHP = Convert.ToInt32(data["max_hp"]),
                    AttackDamage = Convert.ToInt32(data["attack_damage"]),
                    MoveSpeed = Convert.ToSingle(data["move_speed"]),
                    AttackSpeed = Convert.ToSingle(data["attack_speed"]),
                    AttackRange = Convert.ToSingle(data["attack_range"]),
                    RewardGold = Convert.ToInt32(data["reward_gold"]),
                };

                EnemyDataTable.Add(enemyData);
            }
        }

        public EnemyData GetEnemyData(string enemyAddress)
        {
            return EnemyDataTable.Where(enemy => enemy.EnemyAddress == enemyAddress).FirstOrDefault();
        }

        #endregion

        #region ENEMY_COSTUME_DATA
        
        private const string ENEMY_COSTUME_KEY = "EnemyCostume";
        private readonly Dictionary<string, EnemyCostumeData> _enemyCostumes
            = new Dictionary<string, EnemyCostumeData>(System.StringComparer.OrdinalIgnoreCase);

        private async void LoadEnemyCostumeDataTable()
        {
            _enemyCostumes.Clear();

            var locHandle = Addressables.LoadResourceLocationsAsync(
                ENEMY_COSTUME_KEY, typeof(EnemyCostumeData)
            );
            var locations = await locHandle.Task;

            if (locHandle.Status != AsyncOperationStatus.Succeeded || locations == null || locations.Count == 0)
            {
                Logger.LogError($"{GetType()}::No locations for key '{ENEMY_COSTUME_KEY}'. " +
                                  "Check Addressables Address/Label.");
                Addressables.Release(locHandle);
                return;
            }

            var loadHandle = Addressables.LoadAssetsAsync<EnemyCostumeData>(
                locations, callback: null, releaseDependenciesOnFailure: true
            );
            var assets = await loadHandle.Task;

            Addressables.Release(locHandle);

            if (loadHandle.Status != AsyncOperationStatus.Succeeded || assets == null)
                return;

            foreach (var so in assets)
            {
                if (!so) continue;
                if (_enemyCostumes.ContainsKey(so.name)) continue;
                _enemyCostumes.Add(so.name, so);
                Logger.Log($"Loaded EnemyCostume: {so.name}");
            }

            Logger.Log($"{GetType()}::Loaded EnemyCostumeData: {_enemyCostumes.Count} items.");
        }


        public EnemyCostumeData GetEnemyCostume(string enemyAddress)
        {
            if (string.IsNullOrEmpty(enemyAddress)) return null;
            return _enemyCostumes.TryGetValue(enemyAddress, out var so) ? so : null;
        }
        #endregion
        
        #region PLAYER_DATA

        private const string PLAYER_DATA_TABLE = "PlayerDataTable";
        [SerializeField]
        private readonly List<PlayerData> PlayerDataTable = new List<PlayerData>();

        private async void LoadPlayerDataTable()
        {
            PlayerDataTable.Clear();

            var parsedDataTable = await CSVReader.ReadFromAA(PLAYER_DATA_TABLE);
            foreach (var data in parsedDataTable)
            {
                var playerData = new PlayerData()
                {
                    PlayerType = data["player_type"].ToString(),
                    MaxHP = Convert.ToInt32(data["max_hp"]),
                    AttackDamage = Convert.ToInt32(data["attack_damage"]),
                    MoveSpeed = Convert.ToSingle(data["move_speed"]),
                    AttackSpeed = Convert.ToSingle(data["attack_speed"]),
                };

                PlayerDataTable.Add(playerData);
            }
        }

        public PlayerData GetplayerData(string playerType)
        {
            return PlayerDataTable.Where(player => player.PlayerType == playerType).FirstOrDefault();
        }

        #endregion

        #region SKILL_DATA

        private const string SKILL_DATA_TABLE = "SkillDataTable";
        [SerializeField]
        private List<SkillData> SkillDataTable = new List<SkillData>();

        private async void LoadSkillDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(SKILL_DATA_TABLE);

            foreach (var data in parsedDataTable)
            {
                var skillData = new SkillData()
                {
                    ItemId = Convert.ToInt32(data["item_id"]),
                    SkillName = data["skill_name"].ToString(),
                    Cooldown = Convert.ToSingle(data["cooldown"]),
                    ObjectCount = Convert.ToInt32(data["object_count"]),
                };

                SkillDataTable.Add(skillData);
            }
        }

        public SkillData GetSkillData(int itemId)
        {
            return SkillDataTable.Where(item => item.ItemId == itemId).FirstOrDefault();
        }

        #endregion
        
        #region ACHIEVEMENT_DATA
        private const string ACHIEVEMENT_DATA_TABLE = "AchievementDataTable";
        [SerializeField]
        private List<AchievementData> _achievementDataTable = new List<AchievementData>();

        public List<AchievementData> GetAchievementDataList()
        {
            return _achievementDataTable;
        }

        private async void LoadAchievementDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(ACHIEVEMENT_DATA_TABLE);

            foreach (var data in parsedDataTable)
            {
                var achievementData = new AchievementData()
                {
                    AchievementType = Enum.Parse<AchievementType>(data["achievement_type"].ToString(), true),
                    AchievementName = data["achievement_name"].ToString(),
                    AchievementGoal = Convert.ToInt32(data["achievement_goal"]),
                    AchievementRewardType = Enum.Parse<GlobalDefine.RewardType>(data["achievement_reward_type"].ToString(), true),
                    AchievementRewardAmount = Convert.ToInt32(data["achievement_reward_amount"]),
                };
                
                _achievementDataTable.Add(achievementData);
            }
        }

        public AchievementData GetAchievementData(AchievementType achievementType)
        {
            return _achievementDataTable.Where(item => item.AchievementType == achievementType).FirstOrDefault();
        }
        
        public List<AchievementType> GetAchievementTypes(GlobalDefine.RewardType rewardType)
        {
            return _achievementDataTable
                .Where(item => item.AchievementRewardType == rewardType)
                .Select(item => item.AchievementType)
                .ToList();
        }
        
        #endregion

        #region PRODUCT_DATA

        private const string PRODUCT_DATA_TABLE = "ProductDataTable";
        [SerializeField]
        private List<ProductData> _productDataTable = new List<ProductData>();
        
        public async void LoadProductDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(PRODUCT_DATA_TABLE);

            foreach (var data in parsedDataTable)
            {
                var productData = new ProductData
                {
                    ProductId = data["product_id"].ToString(),
                    ProductType = (ProductType)Enum.Parse(typeof(ProductType), data["product_type"].ToString()),
                    ProductName = data["product_name"].ToString(),
                    PurchaseType = (PurchaseType)Enum.Parse(typeof(PurchaseType), data["purchase_type"].ToString()),
                    PurchaseCost = Convert.ToInt32(data["purchase_cost"]),
                    RewardGem = Convert.ToInt32(data["reward_gem"]),
                    RewardGold = Convert.ToInt32(data["reward_gold"]),
                    RewardItemId = Convert.ToInt32(data["reward_item_id"]),
                };
                
                _productDataTable.Add(productData);
            }
        }

        public ProductData GetProductData(string productId)
        {
            return _productDataTable.Where(item => item.ProductId == productId).FirstOrDefault();
        }

        public List<ProductData> GetProductDatas(ProductType productType)
        {
            return _productDataTable.Where(item => item.ProductType == productType).ToList();
        }
        
        #endregion

        #region CHEST_REWARD_PROBABILITY_DATA

        private const string CHEST_REWARD_PROBABILITY_DATA_TABLE = "ChestRewardProbabilityDataTable";
        private List<ChestRewardProbabilityData> _chestRewardProbabilityDataTable = new List<ChestRewardProbabilityData>();

        private async void LoadChestRewardProbabilityDataTable()
        {
            var parsedDataTable = await CSVReader.ReadFromAA(CHEST_REWARD_PROBABILITY_DATA_TABLE);
            
            foreach (var data in parsedDataTable)
            {
                var chestRewardProbabilityData = new ChestRewardProbabilityData
                {
                    ItemId = Convert.ToInt32(data["item_id"]),
                    ChestId = data["chest_id"].ToString(),
                    LootProbability = Convert.ToInt32(data["loot_probability"])
                };
                
                _chestRewardProbabilityDataTable.Add(chestRewardProbabilityData);
            }
        }

        public List<ChestRewardProbabilityData> GetChestRewardProbabilityDatas(string chestId)
        {
            return _chestRewardProbabilityDataTable.Where(item => item.ChestId == chestId).ToList();
        }
        #endregion
    }

    [Serializable]
    public class ChapterData
    {
        public int ChapterNo;
        public int TotalStage;
        public string ChapterName;
        public string StageName;
        public int ChapterRewardGem;
        public int ChapterRewardGold;
        public List<int> DropItemIds = new List<int>();
    }

    [Serializable]
    public class StageData
    {
        public int ChapterNo;
        public int StageNo;
        public string EnemyName;
        public int EnemyCount;
    }

    [Serializable]
    public class ItemData
    {
        public int ItemId;
        public string ItemName;
        public float DropRate;
        public int Damage;
        public int Hp;
        public int ValueIncrease;
        public int SellPrice;
        public string Description;
        public string Sources;
    }

    [Serializable]
    public class EnemyData
    {
        public string EnemyAddress;
        public string EnemyName;
        public EnemyType EnemyType;
        public int MaxHP;
        public int AttackDamage;
        public float MoveSpeed;
        public float AttackSpeed;
        public float AttackRange;
        public int RewardGold;
    }

    [Serializable]
    public class PlayerData
    {
        public string PlayerType;
        public int MaxHP;
        public int AttackDamage;
        public float MoveSpeed;
        public float AttackSpeed;
    }

    [Serializable]
    public class SkillData
    {
        public int ItemId;
        public string SkillName;
        public float Cooldown;
        public int ObjectCount;
    }

    public enum ItemType
    {
        weapon = 1,
        Secondary,
        Necklace,
        Ring,
    }

    public enum ItemGrade
    {
        Common = 1,
        Uncommon,
        Rare,
        Epic,
        Legendary,
    }

    public enum GoodsItemType
    {
        Gold = 1,
        Gem,
    }
}