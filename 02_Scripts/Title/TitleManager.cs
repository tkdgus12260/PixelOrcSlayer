using System;
using System.Collections;
using System.Collections.Generic;
using Gpm.LogViewer.Internal;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace PixelSurvival
{
    public enum RemoteResourceGroup
    {
        DataTable,
        EnemyCostume,
        Enemies,
        SkillData,
        Players,
        Prefabs,
        Stages,
        UI,
        UITexture,
        AudioClips,
        EquipmentTexture,
        SkillTexture,
    }
    
    public class TitleManager : MonoBehaviour
    {
        //로고
        public Animation LogoAnim;
        public TextMeshProUGUI LogoTxt;

        //타이틀
        public GameObject Title;
        public Slider LoadingSlider;
        public TextMeshProUGUI LoadingProgressTxt;

        private AsyncOperation _asyncOperation;
        
        private bool _isResourceDownloaded = false;
        

        private void Awake()
        {
            LogoAnim.gameObject.SetActive(true);
            Title.SetActive(false);

#if UNITY_IOS
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
            }
#endif
        }

        private IEnumerator Start()
        {
            UIManager.Instance.EnableGoodsUI(false);
            AdsManager.Instance.EnableTopBannerAd(false);
            GameManager.Instance.Resume();
            // Init Addressable
            Addressables.InitializeAsync().Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Logger.Log("Addressables initialized successfully.");
                    StartCoroutine(CheckResourcesCo());
                }
                else
                {
                    Logger.LogError("Failed to initialize addressables.");
                }
            };
            
            while (!_isResourceDownloaded)
            {
                yield return null;
            }
            
            DataTableManager.Instance.LoadDataTables();

            StartCoroutine(LoadGameCo());
        }

        private IEnumerator LoadGameCo()
        {
            Logger.Log($"{GetType()}::LoadGameCo");

            LogoAnim.Play();
            yield return new WaitForSeconds(LogoAnim.clip.length);

            LogoAnim.gameObject.SetActive(false);
            Title.SetActive(true);

            // Check third party service init
            while (!CheckThirdPartyServiceInit())
            {
                Logger.LogWarning("CheckThirdPartyServiceInit is fail");
                yield return null;
            }
            
            // Validata app version
            if(!ValidateAppVersion())
                yield break;
            
            // Check sign in
            if (!FirebaseManager.Instance.IsSignedIn())
            {
                var uiData = new BaseUIData();
                UIManager.Instance.OpenUIFromAA<LoginUI>(uiData);
            }
            
            // Wait until user is signed in
            while (!FirebaseManager.Instance.IsSignedIn())
            {
                yield return null;
            }
            
            // Load user data
            UserDataManager.Instance.LoadUserData();
            
            // Wait until all user data is loaded
            while (!UserDataManager.Instance.IsUserDataLoaded())
            {
                yield return null;
            }
            
            yield return StartCoroutine(LoadLobbyCo());
        }

        private bool CheckThirdPartyServiceInit()
        {
            return FirebaseManager.Instance.IsInit();
        }

        private bool ValidateAppVersion()
        {
            bool result = false;

            if (Application.version == FirebaseManager.Instance.GetAppVersion())
            {
                result = true;
            }
            else
            {
                var uiData = new ConfirmUIData();
                uiData.ConfirmType = ConfirmType.OK_CANCEL;
                uiData.TitleTxt = string.Empty;
                uiData.DescTxt = "App version is outdated. Will you update your app?";
                uiData.OKBtnTxt = "Update";
                uiData.CancelBtnTxt = "Cancel";
                uiData.OnClickOKBtn = () =>
                {
#if UNITY_ANDROID
                    Application.OpenURL(GlobalDefine.GOOGLE_PLAY_STORE_URL);
#elif UNITY_IOS
                    Application.OpenURL(GlobalDefine.APPLE_APP_STORE_URL);
#endif
                };
                uiData.OnClickCancelBtn = () =>
                {
                    Application.Quit();
                };
                
                UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
            }

            return result;
        }

        private IEnumerator CheckResourcesCo()
        {
            long totalResourcesDownloadSize = 0;
            List<string> groupsToDownload = new List<string>();

            foreach (var group in Enum.GetValues(typeof(RemoteResourceGroup)))
            {
                AsyncOperationHandle<long> getSizeHandle = Addressables.GetDownloadSizeAsync(group.ToString());
                yield return getSizeHandle;

                if (getSizeHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Logger.Log($"Download size for {group}: {getSizeHandle.Result} bytes");
                    if (getSizeHandle.Result > 0)
                    {
                        totalResourcesDownloadSize += getSizeHandle.Result;
                        groupsToDownload.Add(group.ToString());
                    }
                }
            }

            if (totalResourcesDownloadSize > 0)
            {
                var uiData = new ConfirmUIData();
                uiData.ConfirmType = ConfirmType.OK_CANCEL;
                uiData.TitleTxt = "Download";
                uiData.DescTxt = $"Download resources?\n{totalResourcesDownloadSize / 1000}KB";
                uiData.OKBtnTxt = "OK";
                uiData.CancelBtnTxt = "Cancel";
                uiData.OnClickOKBtn = () =>
                {
                    StartCoroutine(DownloadResourcesCo(groupsToDownload));
                };
                uiData.OnClickCancelBtn = () =>
                {
                    Application.Quit();
                };
                
                UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
            }
            else
            {
                Logger.Log("No resources download available.");
                _isResourceDownloaded = true;
            }
        }

        private IEnumerator DownloadResourcesCo(List<string> groupsToDownload)
        {
            foreach (var group in groupsToDownload)
            {
                AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(group);
                while (!downloadHandle.IsDone)
                {
                    float progress = downloadHandle.PercentComplete;
                    LoadingSlider.value = progress;
                    LoadingProgressTxt.text = $"Downloading: {(int)(progress * 100)}%";
                    yield return null;
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Logger.Log($"{group} download complete.");
                    Addressables.Release(downloadHandle);
                }
                else
                {
                    Logger.LogError($"Failed to download {group}: {downloadHandle.Status}");
                    yield break;
                }
            }
            
            _isResourceDownloaded = true;
        }

        private IEnumerator LoadLobbyCo()
        {
            _asyncOperation = SceneLoader.Instance.LoadSceneAsync(SceneType.Lobby);
            if (_asyncOperation == null)
            {
                Logger.Log("Lobby async loading error.");
                yield break;
            }
            
            _asyncOperation.allowSceneActivation = false;
            
            LoadingSlider.value = 0.15f;
            LoadingProgressTxt.text = $"{(int)(LoadingSlider.value * 100)}%";
            
            yield return new WaitForSeconds(0.5f);

            while (!_asyncOperation.isDone) 
            {
                LoadingSlider.value = _asyncOperation.progress < 0.15f ? 0.15f : _asyncOperation.progress;
                LoadingProgressTxt.text = $"{(int)(LoadingSlider.value * 100)}%";

                if (_asyncOperation.progress >= 0.9f)
                {
                    _asyncOperation.allowSceneActivation = true;
                    yield break;
                }

                yield return null;
            }
        }
    }
}