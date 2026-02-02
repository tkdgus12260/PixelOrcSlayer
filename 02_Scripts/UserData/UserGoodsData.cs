using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class UserGoodsData : IUserData
    {
        public bool IsLoaded { get; set; }
        
        // 보석
        public long Gem { get; set; }
        public long Gold { get; set; }

        public void SetDefaultData()
        {
            Logger.Log($"{GetType()}::SetDefaultData");

            Gem = 0;
            Gold = 0;
        }

        public void LoadData()
        {
            Logger.Log($"{GetType()}::LoadData");

            FirebaseManager.Instance.LoadUserData<UserGoodsData>(() =>
            {
                IsLoaded = true;
            });
        }

        public void SaveData()
        {
            Logger.Log($"{GetType()}::SaveData");

            FirebaseManager.Instance.SaveUserData<UserGoodsData>(ConvertDataToFirestoreDic());
        }

        private Dictionary<string, object> ConvertDataToFirestoreDic()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                { "Gem", Gem },
                { "Gold", Gold }
            };

            return dic;
        }
        
        public void SetData(Dictionary<string, object> firestoreDic)
        {
            Logger.Log($"{GetType()}::SetData");
            
            ConvertFirestoreDicToData(firestoreDic);
        }

        private void ConvertFirestoreDicToData(Dictionary<string, object> dic)
        {
            if(dic.TryGetValue("Gem", out var gemValue) && gemValue is long gem) Gem = gem;
            if(dic.TryGetValue("Gold", out var goldValue) && goldValue is long gold) Gold = gold;
        }
    }
}