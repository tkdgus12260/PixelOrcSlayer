using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

namespace PixelSurvival
{
    public class UserPlayData : IUserData
    {
        public bool IsLoaded { get; set; }
        public int MaxClearedChapter { get; set; }
        public int SelectedChapter { get; set; } = 1;
        
        public DateTime LastDailyFreeGemRewardedTime { get; set; }
        public DateTime LastHourFreeChestRewardedTime { get; set; }
        public bool HasAdPackPurchased { get; set; }
        
        public void SetDefaultData()
        {
            Logger.Log($"{GetType()}::SetDefaultData");

            MaxClearedChapter = 0;
            SelectedChapter = 1;
        }

        public void LoadData()
        {
            Logger.Log($"{GetType()}::LoadData");

            FirebaseManager.Instance.LoadUserData<UserPlayData>(() =>
            {
                IsLoaded = true;
            });
        }

        public void SaveData()
        {
            Logger.Log($"{GetType()}::SaveData");

            FirebaseManager.Instance.SaveUserData<UserPlayData>(ConvertDataToFirestoreDic());
        }

        private Dictionary<string, object> ConvertDataToFirestoreDic()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>()
            {
                { "MaxClearedChapter", MaxClearedChapter },
                {"LastDailyFreeGemRewardedTime", Timestamp.FromDateTime(LastDailyFreeGemRewardedTime)},
                {"LastHourFreeChestRewardedTime", Timestamp.FromDateTime(LastHourFreeChestRewardedTime)},
                {"HasAdPackPurchased", HasAdPackPurchased},
            };

            return dic;
        }

        public void SetData(Dictionary<string, object> firestoreDic)
        {
            ConvertFirestoreDicToData(firestoreDic);
        }

        private void ConvertFirestoreDicToData(Dictionary<string, object> dic)
        {
            if (dic.TryGetValue("MaxClearedChapter", out var maxClearedChapter) && maxClearedChapter != null)
            {
                MaxClearedChapter =  Convert.ToInt32(maxClearedChapter);
            }

            if (dic.TryGetValue("LastDailyFreeGemRewardedTime", out var lastDailyFreeGemRewardedTimeValue))
            {
                if (lastDailyFreeGemRewardedTimeValue is Timestamp lastDailyFreeGemRewardedTime)
                {
                    LastDailyFreeGemRewardedTime = lastDailyFreeGemRewardedTime.ToDateTime().ToLocalTime();
                }
            }
            
            if (dic.TryGetValue("LastHourFreeChestRewardedTime", out var lastHourFreeChestRewardedTimeValue))
            {
                if (lastHourFreeChestRewardedTimeValue is Timestamp lastHourFreeChestRewardedTime)
                {
                    LastHourFreeChestRewardedTime = lastHourFreeChestRewardedTime.ToDateTime().ToLocalTime();
                }
            }
            
            if(dic.TryGetValue("HasAdPackPurchased", out var hasAdPackPurchasedValue) && hasAdPackPurchasedValue is bool hasAdPackPurchased) HasAdPackPurchased = hasAdPackPurchased;
        }
    }
}