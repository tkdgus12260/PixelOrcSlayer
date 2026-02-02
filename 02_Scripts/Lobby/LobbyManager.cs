using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class LobbyManager : SingletonBehaviour<LobbyManager>
    {
        public LobbyUIController LobbyUIController { get; private set; }

        private bool _isLoadingInGame;

        protected override void Init()
        {
            isDestroyOnLoad = true;
            _isLoadingInGame = false;

            base.Init();
            
            GameManager.Instance.Resume();
        }

        private void Start()
        {
            AdsManager.Instance.EnableTopBannerAd(true);
            
            LobbyUIController = FindObjectOfType<LobbyUIController>();

            if (!LobbyUIController)
            {
                Logger.LogError("LobbyUIController not found");
                return;
            }

            LobbyUIController.Init();
            AudioManager.Instance.OnLoadLobby();
            AudioManager.Instance.PlayBGM(BGM.lobby);
            
            // var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            // userInventoryData.AcquireItem(15203, 5);
            // userInventoryData.AcquireItem(25103, 5);
            // userInventoryData.AcquireItem(35103, 5);
            // userInventoryData.AcquireItem(45103, 5);
            // userInventoryData.SaveData();

            // var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
            // userGoodsData.Gold = long.MaxValue;
            // userGoodsData.SaveData();
        }

        public void StartInGame()
        {
            if (_isLoadingInGame)
            {
                return;
            }

            _isLoadingInGame = true;

            UIManager.Instance.Fade(Color.black, 0f, 1f, 0.5f, 0f, false, () =>
            {
                UIManager.Instance.CloseAllOpenUI();
                SceneLoader.Instance.LoadScene(SceneType.InGame);
            });
        }
    }
}