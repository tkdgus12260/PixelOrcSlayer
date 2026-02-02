using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public enum BGM
    {
        lobby,
        title,
        in_game,
        COUNT
    }

    public enum SFX
    {
        chapter_clear,
        chapter_scroll,
        chapter_select,
        stage_clear,
        fire_strike,
        fire_orb,
        shuriken,
        shield_piece,
        ui_button_click,
        ui_get,
        ui_increase,
        equipped,
        un_equipped,
        upgrade_success,
        upgrade_fail,
        COUNT
    }

    public class AudioManager : SingletonBehaviour<AudioManager>
    {
        public Transform BGMTransform;
        public Transform SFXTransform;

        private readonly Dictionary<BGM, AudioClip> _bgmClips = new();
        private AudioSource _bgmSource;
        public float BGMVolume { get; private set; }
        public bool IsBGMMute { get; private set; }
        
        private readonly Dictionary<SFX, AudioClip> _sfxClips = new();
        [SerializeField] private int _sfxPoolSize = 16;
        private readonly List<AudioSource> _sfxPool = new();
        public float SFXVolume { get; private set; }
        public bool IsSFXMute { get; private set; }
        
        protected override void Init()
        {
            base.Init();

            LoadBGMClips();  
            LoadSFXClips();  
            CreateBGMSouce();
            CreateSFXPool(); 
        }

        public void OnLoadLobby()
        {
            var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
            if (userSettingsData != null)
            {
                BGMMute(!userSettingsData.BGMSound); 
                SetBGMVolume(userSettingsData.BGMSoundVolume);
                
                SFXMute(!userSettingsData.SFXSound); 
                SetSFXVolume(userSettingsData.SFXSoundVolume);
            }
        }
        
        private async void LoadBGMClips()
        {
            for (int i = 0; i < (int)BGM.COUNT; i++)
            {
                var bgm = (BGM)i;
                var audioName = bgm.ToString();
                AsyncOperationHandle<AudioClip> operationHandle = Addressables.LoadAssetAsync<AudioClip>(audioName);
                await operationHandle.Task;
                
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var clip = operationHandle.Result;
                    if (clip != null)
                    {
                        _bgmClips[bgm] = clip;
                    }
                }
            }
        }

        private async void LoadSFXClips()
        {
            for (int i = 0; i < (int)SFX.COUNT; i++)
            {
                var sfx = (SFX)i;
                var audioName = sfx.ToString();
                AsyncOperationHandle<AudioClip> operationHandle = Addressables.LoadAssetAsync<AudioClip>(audioName);
                await operationHandle.Task;
                
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var clip = operationHandle.Result;
                    if (clip != null)
                    {
                        _sfxClips[sfx] = clip;
                    }
                }
            }
        }

        private void CreateBGMSouce()
        {
            var go = new GameObject("BGM_Source");
            go.transform.SetParent(BGMTransform ? BGMTransform : transform, false);

            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
        }

        private void CreateSFXPool()
        {
            var parent = SFXTransform ? SFXTransform : transform;

            _sfxPool.Clear();
            for (int i = 0; i < Mathf.Max(1, _sfxPoolSize); i++)
            {
                var go = new GameObject($"SFX_Source_{i}");
                go.transform.SetParent(parent, false);

                var src = go.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;

                _sfxPool.Add(src);
            }
        }

        public void PlayBGM(BGM bgm)
        {
            if (_bgmSource == null) return;

            if (!_bgmClips.TryGetValue(bgm, out var clip) || clip == null)
            {
                Logger.LogError($"Invalid BGM clip. {bgm}");
                return;
            }

            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;

            _bgmSource.Stop();
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource) _bgmSource.Stop();
        }

        public void PlaySFX(SFX sfx)
        {
            if (!_sfxClips.TryGetValue(sfx, out var clip) || clip == null)
            {
                Logger.LogError($"Invalid SFX clip. {sfx}");
                return;
            }

            var src = GetAvailableSFXSource();
            if (src == null) return;
            
            src.PlayOneShot(clip);
        }

        public void SetBGMVolume(float volume)
        {
            BGMVolume = volume;
            _bgmSource.volume = BGMVolume;
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = volume;
            
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (_sfxPool[i])
                    _sfxPool[i].volume = SFXVolume;
            }
        }

        public void BGMMute(bool value)
        {
            IsBGMMute = value;
            _bgmSource.mute = IsBGMMute;
        }

        public void SFXMute(bool value)
        {
            IsSFXMute = value;
            
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (_sfxPool[i])
                    _sfxPool[i].mute = IsSFXMute;
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            // 재생 중이 아닌 오디오소스 우선
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                var src = _sfxPool[i];
                if (src && !src.isPlaying)
                    return src;
            }

            // 전부 재생 중일 때 우선적으로 0번을 종료하고 재사용. 
            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }
    }
}
