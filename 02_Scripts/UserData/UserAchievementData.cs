using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperMaxim.Messaging;
using UnityEngine;

namespace PixelSurvival
{
    [Serializable]
    public class UserAchievementProgressData
    {
        public AchievementType AchievementType;
        public long AchievementAmount;
        public bool IsAchieved;
        public bool IsRewardClaimed;
    }

    [Serializable]
    public class UserAchievementProgressDataListWrapper
    {
        public List<UserAchievementProgressData> AchievementProgressDataList;
    }

    public class AchievementProgressMsg
    {
        
    }
    
    public class UserAchievementData : IUserData
    {
        public bool IsLoaded { get; set; }
        
        public List<UserAchievementProgressData> AchievementProgressDataList { get; set; } = new();
        
        public void SetDefaultData()
        { 
            
        }

        public void LoadData()
        {
            Logger.Log($"{GetType()}::LoadData");
            
            FirebaseManager.Instance.LoadUserData<UserAchievementData>(() =>
            {
                IsLoaded = true;
            });
        }

        public void SaveData()
        {
            Logger.Log($"{GetType()}::SaveData");
            
            FirebaseManager.Instance.SaveUserData<UserAchievementData>(ConvertDataToFirestoreDic());
        }

        private Dictionary<string, object> ConvertDataToFirestoreDic()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            List<Dictionary<string, object>> convertedAchievementProgressDataList = new();
            foreach (var item in AchievementProgressDataList)
            {
                var convertedItem = new Dictionary<string, object>()
                {
                    { "AchievementType", item.AchievementType },
                    { "AchievementAmount", item.AchievementAmount },
                    { "IsAchieved", item.IsAchieved },
                    { "IsRewardClaimed", item.IsRewardClaimed },
                };
                convertedAchievementProgressDataList.Add(convertedItem);
            }
            
            dic["AchievementProgressDataList"] = convertedAchievementProgressDataList;
            return dic;
        }

        public void SetData(Dictionary<string, object> firestoreDic)
        {
            ConvertFirestoreDicToData(firestoreDic);
        }

        private void ConvertFirestoreDicToData(Dictionary<string, object> dic)
        {
            if (dic.TryGetValue("AchievementProgressDataList", out object achievementDataObj) &&
                achievementDataObj is List<object> achievementList)
            {
                foreach (var item in achievementList)
                {
                    if (item is Dictionary<string, object> itemDic)
                    {
                        UserAchievementProgressData achievementProgressData = new UserAchievementProgressData();

                        if (itemDic.TryGetValue("AchievementType", out object achievementTypeValue) && achievementTypeValue != null)
                        {
                            achievementProgressData.AchievementType = (AchievementType)Convert.ToInt32(achievementTypeValue);
                        }

                        if (itemDic.TryGetValue("AchievementAmount", out object achievementAmountValue) && achievementAmountValue != null)
                        {
                            achievementProgressData.AchievementAmount = Convert.ToInt32(achievementAmountValue);
                        }

                        if (itemDic.TryGetValue("IsAchieved", out var isAchievedValue) &&
                            isAchievedValue is bool isAchieved)
                        {
                            achievementProgressData.IsAchieved = isAchieved;
                        }
                        
                        if (itemDic.TryGetValue("IsRewardClaimed", out var isRewardClaimedValue) &&
                            isRewardClaimedValue is bool isRewardClaimed)
                        {
                            achievementProgressData.IsRewardClaimed = isRewardClaimed;
                        }
                        
                        AchievementProgressDataList.Add(achievementProgressData);
                    }
                }
            }
        }

        public UserAchievementProgressData GetUserAchievementProgressData(AchievementType achievementType)
        {
            return AchievementProgressDataList.Where(item => item.AchievementType == achievementType).FirstOrDefault();
        }

        public void ProgressAchievement(AchievementType achievementType, long ahcieveAmount)
        {
            var achievementData = DataTableManager.Instance.GetAchievementData(achievementType);
            if (achievementData == null)
            {
                Logger.LogError("AchievementData does not exist.");
                return;
            }
            
            UserAchievementProgressData userAchievementProgressData = GetUserAchievementProgressData(achievementType);
            if (userAchievementProgressData == null)
            {
                userAchievementProgressData = new UserAchievementProgressData();
                userAchievementProgressData.AchievementType = achievementType;
                AchievementProgressDataList.Add(userAchievementProgressData);
            }
            
            if (!userAchievementProgressData.IsAchieved)
            {
                userAchievementProgressData.AchievementAmount += ahcieveAmount;
                if (userAchievementProgressData.AchievementAmount > achievementData.AchievementGoal)
                {
                    userAchievementProgressData.AchievementAmount = achievementData.AchievementGoal;
                }
                if (userAchievementProgressData.AchievementAmount == achievementData.AchievementGoal)
                {
                    userAchievementProgressData.IsAchieved = true;
                }
                SaveData();
            }
            
            var achievementProgressMsg = new AchievementProgressMsg();
            Messenger.Default.Publish(achievementProgressMsg);
        }
    }   
}
