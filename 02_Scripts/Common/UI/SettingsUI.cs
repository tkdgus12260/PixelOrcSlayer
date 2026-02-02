using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class SettingsUI : BaseUI
    {
        public TextMeshProUGUI GameVersionTxt;

        public Sprite SoundSprite;
        public Sprite MuteSprite;

        public Button BGMBtn;
        public Button SFXBtn;

        public Slider BGMSlider;
        public Slider SFXSlider;

        private const string PRIVACY_POLICY_URL = "https://sites.google.com/view/pixelorcslayer?usp=sharing";

        private const float DEFAULT_VOLUME = 0.5f;

        private bool _bgmDirty;
        private bool _sfxDirty;
        private bool _ignoreSliderCallback;

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            SetGameVersion();
            
            InitBGMUI(AudioManager.Instance.IsBGMMute, AudioManager.Instance.BGMVolume);
            InitSFXUI(AudioManager.Instance.IsSFXMute, AudioManager.Instance.SFXVolume);
            
            BGMSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            SFXSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            BindSaveOnRelease(BGMSlider, SaveBGMVolume);
            BindSaveOnRelease(SFXSlider, SaveSFXVolume);
            
            _bgmDirty = false;
            _sfxDirty = false;
        }

        private void SetGameVersion()
        {
            GameVersionTxt.text = $"Version : {Application.version}";
        }

        private void InitBGMUI(bool isMute, float volume)
        {
            if (BGMBtn == null || BGMSlider == null)
            {
                Logger.LogError("BGMBtn or BGMSlider is null");
                return;
            }
            
            if (isMute)
            {
                BGMBtn.image.sprite = MuteSprite;
                BGMSlider.value = volume;
            }
            else
            {
                BGMBtn.image.sprite = SoundSprite;
                BGMSlider.value = volume;
            }
        }

        private void InitSFXUI(bool isMute, float volume)
        {
            if (SFXBtn == null || SFXSlider == null)
            {
                Logger.LogError("SFXBtn or SFXSlider is null");
                return;
            }

            if (isMute)
            {
                SFXBtn.image.sprite = MuteSprite;
                SFXSlider.value = volume;
            }
            else
            {
                SFXBtn.image.sprite = SoundSprite;
                SFXSlider.value = volume;
            }
        }
        
        private void SetBGMState(bool isOn, float volume, bool setSliderValue)
        {
            if (isOn)
            {
                BGMBtn.image.sprite = SoundSprite;
                AudioManager.Instance.BGMMute(false);
                AudioManager.Instance.SetBGMVolume(volume);
                if (setSliderValue) SetSliderSilently(BGMSlider, volume);
            }
            else
            {
                BGMBtn.image.sprite = MuteSprite;
                AudioManager.Instance.BGMMute(true);
                if (setSliderValue) SetSliderSilently(BGMSlider, 0f);
            }
        }

        private void SetSFXState(bool isOn, float volume, bool setSliderValue)
        {
            if (isOn)
            {
                SFXBtn.image.sprite = SoundSprite;
                AudioManager.Instance.SFXMute(false);
                AudioManager.Instance.SetSFXVolume(volume);
                if (setSliderValue) SetSliderSilently(SFXSlider, volume);
            }
            else
            {
                SFXBtn.image.sprite = MuteSprite;
                AudioManager.Instance.SFXMute(true);
                if (setSliderValue) SetSliderSilently(SFXSlider, 0f);
            }
        }

        private void SetSliderSilently(Slider slider, float value)
        {
            _ignoreSliderCallback = true;
            slider.value = value;
            _ignoreSliderCallback = false;
        }

        private void OnBGMVolumeChanged(float value)
        {
            if (_ignoreSliderCallback) return;

            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;

            _bgmDirty = true;

            if (value <= 0f)
            {
                userSettingsData.BGMSound = false;
                userSettingsData.BGMSoundVolume = 0f;

                SetBGMState(false, 0f, setSliderValue: false);
            }
            else
            {
                userSettingsData.BGMSound = true;
                userSettingsData.BGMSoundVolume = value;

                SetBGMState(true, value, setSliderValue: false);
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (_ignoreSliderCallback) return;

            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;

            _sfxDirty = true;

            if (value <= 0f)
            {
                userSettingsData.SFXSound = false;
                userSettingsData.SFXSoundVolume = 0f;

                SetSFXState(false, 0f, setSliderValue: false);
            }
            else
            {
                userSettingsData.SFXSound = true;
                userSettingsData.SFXSoundVolume = value;

                SetSFXState(true, value, setSliderValue: false);
            }
        }

        private void SaveBGMVolume()
        {
            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;
            if (!_bgmDirty) return;

            _bgmDirty = false;
            userSettingsData.SaveData();
        }

        private void SaveSFXVolume()
        {
            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;
            if (!_sfxDirty) return;

            _sfxDirty = false;
            userSettingsData.SaveData();
        }

        private void BindSaveOnRelease(Slider slider, System.Action onRelease)
        {
            if (!slider) return;

            var trigger = slider.GetComponent<EventTrigger>();
            if (!trigger) trigger = slider.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();

            AddOrReplaceTrigger(trigger, EventTriggerType.EndDrag, _ => onRelease?.Invoke());
            AddOrReplaceTrigger(trigger, EventTriggerType.PointerUp, _ => onRelease?.Invoke());
        }

        private void AddOrReplaceTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = trigger.triggers.Find(e => e.eventID == type);
            if (entry == null)
            {
                entry = new EventTrigger.Entry { eventID = type };
                trigger.triggers.Add(entry);
            }
            entry.callback.RemoveAllListeners();
            entry.callback.AddListener(action);
        }

        public void OnClickBGMBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;

            if (userSettingsData.BGMSound)
            {
                userSettingsData.BGMSound = false;
                userSettingsData.BGMSoundVolume = 0f;
                _bgmDirty = true;

                SetBGMState(false, 0f, setSliderValue: true);
            }
            else
            {
                userSettingsData.BGMSound = true;
                userSettingsData.BGMSoundVolume = DEFAULT_VOLUME;
                _bgmDirty = true;

                SetBGMState(true, DEFAULT_VOLUME, setSliderValue: true);
            }

            userSettingsData.SaveData();
            _bgmDirty = false;
        }

        public void OnClickSFXBtn()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData == null) return;

            if (userSettingsData.SFXSound)
            {
                userSettingsData.SFXSound = false;
                userSettingsData.SFXSoundVolume = 0f;
                _sfxDirty = true;

                SetSFXState(false, 0f, setSliderValue: true);
            }
            else
            {
                userSettingsData.SFXSound = true;
                userSettingsData.SFXSoundVolume = DEFAULT_VOLUME;
                _sfxDirty = true;

                SetSFXState(true, DEFAULT_VOLUME, setSliderValue: true);
            }

            userSettingsData.SaveData();
            _sfxDirty = false;
        }
        
        public void OnClickPrivacyPolicyURL()
        {
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            Application.OpenURL(PRIVACY_POLICY_URL);
        }

        public void OnClickLogoutBtn()
        {
            FirebaseManager.Instance.SignOut();
        }
    }
}
