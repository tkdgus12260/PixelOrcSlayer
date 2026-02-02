using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public interface IUserData
    {
        bool IsLoaded { get; set; }
        void SetDefaultData();  // 기본 값으로 데이터 초기화
        void LoadData();        // 데이터 로드 
        void SaveData();        // 데이터 저장 
        void SetData(Dictionary<string, object> firestoreDic);
    }
}