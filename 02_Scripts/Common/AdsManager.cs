using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace PixelSurvival
{
    public class AdsManager : SingletonBehaviour<AdsManager>
    {
        protected override void Init()
        {
            base.Init();

            InitAdsService();
            InitBannerAds();
            InitInterstitialAds();
            InitRewardedAds();
        }

        private void InitAdsService()
        {
            MobileAds.Initialize(initStatus =>
            {
                // Check initialization successful
                var isInitSuccess = true;
                var statusMap = initStatus.getAdapterStatusMap();
                foreach (var status in statusMap)
                {
                    var className = status.Key;
                    var adapterStatus = status.Value;
                    Logger.Log($"Adapter: {className}, Status: {adapterStatus.InitializationState}, Description: {adapterStatus.Description}");
                    if (adapterStatus.InitializationState != AdapterState.Ready)
                    {
                        isInitSuccess = false;
                    }
                }

                if (isInitSuccess)
                {
                    Logger.Log("Google Ads successfully Initialized");
                }
                else
                {
                    Logger.Log("Google Ads failed to initialize");
                }
            });
        }

        #region BannerAds

        private BannerView _topBannerView;
        private string _topBannerAdId = string.Empty;
        // Ads Test Id
        private const string AOS_BANNER_TEST_AD_ID = "ca-app-pub-3940256099942544/6300978111";
        private const string IOS_BANNER_TEST_AD_ID = "ca-app-pub-3940256099942544/2934735716";
        // Ads Real Id
        private const string AOS_BANNER_AD_ID = "";
        private const string IOS_BANNER_AD_ID = "";
        
        private void InitBannerAds()
        {
            SetTopBannerAdId();
        }

        private void SetTopBannerAdId()
        {
#if DEV_VER
#if UNITY_ANDROID
            _topBannerAdId = AOS_BANNER_TEST_AD_ID;
#elif UNITY_IOS
            _topBannerAdId = IOS_BANNER_TEST_AD_ID;
#endif
#else
#if UNITY_ANDROID
            _topBannerAdId = AOS_BANNER_AD_ID;
#elif UNITY_IOS
            _topBannerAdId = IOS_BANNER_AD_ID;
#endif
#endif
        }

        public void EnableTopBannerAd(bool value)
        {
            Logger.Log($"Enable Top Banner Ad: {value}");

            if (value)
            {
                if (_topBannerView == null)
                {
                    AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                    _topBannerView = new BannerView(_topBannerAdId, adaptiveSize, AdPosition.Top);

                    // Create ad request
                    AdRequest request = new AdRequest();
                    // Load banner with the request
                    _topBannerView.LoadAd(request);
                    ListenToTopBannerAdEvents();
                }
                else
                {
                    _topBannerView.Show();
                }
            }
            else
            {
                if (_topBannerView != null)
                {
                    _topBannerView.Hide();
                }
            }
        }

        private void ListenToTopBannerAdEvents()
        {
            if (_topBannerView == null)
            {
                Logger.LogError("TopBannerView is null");
                return;
            }

            _topBannerView.OnBannerAdLoaded += () =>
            {
                Logger.Log($"_topBannerView loaded an ad with response : {_topBannerView.GetResponseInfo()}");
            };

            _topBannerView.OnBannerAdLoadFailed += error =>
            {
                Logger.LogError($"_topBannerView Failed to load an ad with error : {error}");
            };

            _topBannerView.OnAdPaid += (adValue) =>
            {
                Logger.Log($"_topBannerView ad paid {adValue.Value}{adValue.CurrencyCode}");
            };

            _topBannerView.OnAdImpressionRecorded += () =>
            {
                Logger.Log($"_topBannerView ad impression recorded");
            };

            _topBannerView.OnAdClicked += () =>
            {
                Logger.Log($"_topBannerView ad clicked");
            };

            _topBannerView.OnAdFullScreenContentOpened += () =>
            {
                Logger.Log($"_topBannerView ad full screen content opened");
            };

            _topBannerView.OnAdFullScreenContentClosed += () =>
            {
                Logger.Log($"_topBannerView ad full screen content closed");
            };
        }
        
        #endregion
        
        #region InterstitialAd

        /* 차후 추가할 전면 광고 시 ex.)
        private InterstitialAd _stageClearInterstitialAd;
        private string _chapterStageInterstitialAdId = string.Empty;
        추가 후 광고 게시
        */
        
        private InterstitialAd _chapterClearInterstitial;
        private string _chapterClearInterstitialAdId = string.Empty;
        
        // Ads Test Id
        private const string AOS_INTERSTITIAL_TEST_AD_ID = "ca-app-pub-3940256099942544/1033173712";
        private const string IOS_INTERSTITIAL_TEST_AD_ID = "ca-app-pub-3940256099942544/4411468910";
        // Ads Real Id
        private const string AOS_INTERSTITIAL_AD_ID = "";
        private const string IOS_INTERSTITIAL_AD_ID = "";

        private Action _onFinishChapterClearInterstitialAd = null;

        private void InitInterstitialAds()
        {
            SetChapterClearInterstitialAdId();
            LoadChapterClearInterstitialAd();
        }

        private void SetChapterClearInterstitialAdId()
        {
#if DEV_VER
#if UNITY_ANDROID
            _chapterClearInterstitialAdId = AOS_INTERSTITIAL_TEST_AD_ID;
#elif UNITY_IOS
            _chapterClearInterstitialAdId = IOS_INTERSTITIAL_TEST_AD_ID;
#endif
#else
#if UNITY_ANDROID
            _chapterClearInterstitialAdId = AOS_INTERSTITIAL_AD_ID;
#elif UNITY_IOS
            _chapterClearInterstitialAdId = IOS_INTERSTITIAL_AD_ID;
#endif
#endif
        }

        private void LoadChapterClearInterstitialAd()
        {
            // Create ad request
            var adReqeust = new AdRequest();
            
            // Send request to load ad
            InterstitialAd.Load(_chapterClearInterstitialAdId, adReqeust,
                (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        Logger.LogError($"Interstitial ad faild to load. Error: {error}");
                        return;
                    }
                    
                    Logger.Log($"Interstitial ad loaded successfully. Respinse: {ad.GetResponseInfo()}");
                    _chapterClearInterstitial = ad;
                    ListenToChapterClearInterstitialAdEvents();
                });
        }

        private void ListenToChapterClearInterstitialAdEvents()
        {
            if (_chapterClearInterstitial == null)
            {
                Logger.LogError("_chapterClearInterstitial is null");
                return;
            }

            _chapterClearInterstitial.OnAdPaid += (adValue) =>
            {
                Logger.Log($"_chapterClearInterstitial ad paid : {adValue.Value}{adValue.CurrencyCode}");
            };
            _chapterClearInterstitial.OnAdImpressionRecorded += () =>
            {
                Logger.Log($"_chapterClearInterstitial ad impression recorded");
            };
            _chapterClearInterstitial.OnAdClicked += () =>
            {
                Logger.Log($"_chapterClearInterstitial ad clicked");
            };
            _chapterClearInterstitial.OnAdFullScreenContentOpened += () =>
            {
                Logger.Log("_chapterClearInterstitial ad full screen content opened");
            };
            _chapterClearInterstitial.OnAdFullScreenContentClosed += () =>
            {
                Logger.Log("_chapterClearInterstitial ad full screen content closed");
                LoadChapterClearInterstitialAd();
                _onFinishChapterClearInterstitialAd?.Invoke();
                _onFinishChapterClearInterstitialAd = null;
            };
            _chapterClearInterstitial.OnAdFullScreenContentFailed += (error) =>
            {
                Logger.Log($"_chapterClearInterstitial ad failed to open full screen content closed. Error: {error}");
                LoadChapterClearInterstitialAd();
                _onFinishChapterClearInterstitialAd?.Invoke();
                _onFinishChapterClearInterstitialAd = null;
            };
        }

        public void ShowChapterClearInterstitialAd(Action onFinishChapterClearInterstitialAd = null)
        {
            if (_chapterClearInterstitial != null && _chapterClearInterstitial.CanShowAd())
            {
                Logger.Log("Showing chapter clear interstitial ad");
                _chapterClearInterstitial.Show();
                _onFinishChapterClearInterstitialAd = onFinishChapterClearInterstitialAd;
            }
            else
            {
                Logger.LogError($"Chapter clear interstitial ad is not ready yet.");
            }
        }
        
        #endregion

        #region RewardedAd

        private RewardedAd _freeRewardedAd;
        private string _freeRewardedAdId = string.Empty;
        
        // Ads Test Id
        private const string AOS_REWARDED_AD_TEST_AD_ID = "ca-app-pub-3940256099942544/5224354917";
        private const string IOS_REWARDED_AD_TEST_AD_ID = "ca-app-pub-3940256099942544/1712485313";
        // Ads Real Id
        private const string AOS_FREE_GEM_REWARDED_AD_ID = "";
        private const string IOS_FREE_GEM_REWARDED_AD_ID = "";
        
        private void InitRewardedAds()
        {
            SetFreeGemRewardedAdId();
            LoadFreeGemRewardedAd();
        }

        private void SetFreeGemRewardedAdId()
        {
#if DEV_VER
#if UNITY_ANDROID
            _freeRewardedAdId = AOS_REWARDED_AD_TEST_AD_ID;
#elif UNITY_IOS
            _freeRewardedAdId = IOS_REWARDED_AD_TEST_AD_ID;
#endif
#else
#if UNITY_ANDROID
            _freeGemRewardedAdId = AOS_FREE_GEM_REWARDED_AD_ID;
#elif UNITY_IOS
            _freeRewardedAdId = IOS_FREE_GEM_REWARDED_AD_ID;
#endif
#endif
        }

        private void LoadFreeGemRewardedAd()
        {
            var adReqeust = new AdRequest();
            
            RewardedAd.Load(_freeRewardedAdId, adReqeust,
                (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        Logger.LogError($"Rewarded ad faild to load. Error: {error}");
                        return;
                    }
                    Logger.Log($"Rewarded ad loaded successfully. Response: {ad.GetResponseInfo()}");
                    _freeRewardedAd = ad;
                    ListenToFreeGemRewardedAdEvents();
                });
        }

        private void ListenToFreeGemRewardedAdEvents()
        {
            if (_freeRewardedAd == null)
            {
                Logger.LogError("_freeGemRewardedAd is null");
                return;
            }

            _freeRewardedAd.OnAdPaid += (adValue) =>
            {
                Logger.Log($"_freeGemRewardedAd paid : {adValue.Value}{adValue.CurrencyCode}");
            };
            _freeRewardedAd.OnAdImpressionRecorded += () =>
            {
                Logger.Log($"_freeGemRewardedAd impression recorded");
            };
            _freeRewardedAd.OnAdClicked += () =>
            {
                Logger.Log($"_freeGemRewardedAd ad clicked");
            };
            _freeRewardedAd.OnAdFullScreenContentOpened += () =>
            {
                Logger.Log("_freeGemRewardedAd full screen content opened");
            };
            _freeRewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Logger.Log("_freeGemRewardedAd full screen closed");
                LoadFreeGemRewardedAd();
            };
            _freeRewardedAd.OnAdFullScreenContentFailed += (error) =>
            {
                Logger.LogError($"_freeGemRewardedAd ad failed: {error}");
                LoadFreeGemRewardedAd();
            };
        }

        public void ShowFreeRewardedAd(Action onRewardedAdClosed = null)
        {
            Logger.Log("ShowFreeRewardedAd");

            if (_freeRewardedAd != null && _freeRewardedAd.CanShowAd())
            {
                _freeRewardedAd.Show((reward) =>
                {
                    Logger.Log($"Rewarded FreeGem");
                    onRewardedAdClosed?.Invoke();
                });
            }
            else
            {
                Logger.LogError($"FreeGemRewardedAd failed");
            }
        }

        #endregion
        
        protected override void Dispose()
        {
            if (_topBannerView != null)
            {
                _topBannerView.Destroy();
                _topBannerView = null;
            }

            if (_chapterClearInterstitial != null)
            {
                _chapterClearInterstitial.Destroy();
                _chapterClearInterstitial = null;
            }

            if (_freeRewardedAd != null)
            {
                _freeRewardedAd.Destroy();
                _freeRewardedAd = null;
            }
            
            base.Dispose();
        }
    }
}