using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class UserSettingsData : IUserData
    {
        public bool IsLoaded { get; set; }
        public bool BGMSound { get; set; }
        public bool SFXSound { get; set; }
        public float BGMSoundVolume { get; set; }
        public float SFXSoundVolume { get; set; }

        public void SetDefaultData()
        {
            Logger.Log($"{GetType()}::SetDefaultData");

            BGMSound = true;
            SFXSound = true;
            BGMSoundVolume = 0.5f;
            SFXSoundVolume = 0.5f;
        }

        public void LoadData()
        {
            Logger.Log($"{GetType()}::LoadData");

            FirebaseManager.Instance.LoadUserData<UserSettingsData>(() =>
            {
                IsLoaded = true;
            });
        }

        public void SaveData()
        {
            Logger.Log($"{GetType()}::SaveData");

            FirebaseManager.Instance.SaveUserData<UserSettingsData>(ConvertDataToFirestoreDic());
        }

        private Dictionary<string, object> ConvertDataToFirestoreDic()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                {"BGMSound", BGMSound},
                {"BGMSoundVolume", BGMSoundVolume},
                {"SFXSound", SFXSound},
                {"SFXSoundVolume", SFXSoundVolume}
            };
            
            return dic;
        }

        public void SetData(Dictionary<string, object> firestoreDic)
        {
            ConvertFirestoreDicToData(firestoreDic);
        }

        private void ConvertFirestoreDicToData(Dictionary<string, object> dic)
        {
            if(dic.TryGetValue("BGMSound", out var bgmSoundValue) && bgmSoundValue is bool bgmSound) BGMSound = bgmSound;
            if(dic.TryGetValue("SFXSound", out var sfxSoundValue) && sfxSoundValue is bool sfxSound) SFXSound = sfxSound;
            if (dic.TryGetValue("BGMSoundVolume", out var bgmVolumeValue))
                BGMSoundVolume = Convert.ToSingle(bgmVolumeValue);
            if (dic.TryGetValue("SFXSoundVolume", out var sfxVolumeValue))
                SFXSoundVolume = Convert.ToSingle(sfxVolumeValue);
        }
    }
}