using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelSurvival
{
    public class UserDataManager : SingletonBehaviour<UserDataManager>
    {
        // 저장된 유저 데이터 존재 여부
        public bool ExistsSaveData { get; private set; }

        // 모든 유저 데이터 인스턴스를 저장하는 컨테이너
        public List<IUserData> UserDataList { get; private set; } = new List<IUserData>();

        protected override void Init()
        {
            base.Init();

            // 모든 유저 데이터를 UserDataList에 추가
            UserDataList.Add(new UserSettingsData());
            UserDataList.Add(new UserGoodsData());
            UserDataList.Add(new UserInventoryData());
            UserDataList.Add(new UserPlayData());
            UserDataList.Add(new UserAchievementData());
        }

        public void SetDefaultUserData()
        {
            for (int i = 0; i < UserDataList.Count; i++)
            {
                UserDataList[i].SetDefaultData();
            }
        }

        public void LoadUserData()
        {
            for (int i = 0; i < UserDataList.Count; i++)
            {
                UserDataList[i].LoadData();
            }
        }

        public void SaveUserData()
        {
            for (int i = 0; i < UserDataList.Count; i++)
            {
                UserDataList[i].SaveData();
            }
        }

        public T GetUserData<T>() where T : class, IUserData
        {
            return UserDataList.OfType<T>().FirstOrDefault();
        }

        public bool IsUserDataLoaded()
        {
            for (int i = 0; i < UserDataList.Count; i++)
            {
                if (!UserDataList[i].IsLoaded)
                {
                    Logger.LogWarning("IsUserDataLoaded fail");
                    return false;
                }
            }
            
            return true;
        }
    }
}