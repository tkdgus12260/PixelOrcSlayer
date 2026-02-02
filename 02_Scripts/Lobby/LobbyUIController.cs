using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class LobbyUIController : MonoBehaviour
    {
        public TextMeshProUGUI CurrChapterNameTxt;
        public RawImage CurrChapterBg;

        public void Init()
        {
            UIManager.Instance.EnableGoodsUI(true);
            SetCurrChapter();
        }

        public async void SetCurrChapter()
        {
            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                Logger.LogError($"UserPlayData does not exist");
                return;
            }

            var currChapterData = DataTableManager.Instance.GetChapterData(userPlayData.SelectedChapter);
            if (currChapterData == null)
            {
                Logger.LogError($"UserPlayData does not exist");
                return;
            }

            CurrChapterNameTxt.text = currChapterData.ChapterName;
            AsyncOperationHandle<Texture2D> operationHandle =
                Addressables.LoadAssetAsync<Texture2D>($"ChapterBG{userPlayData.SelectedChapter.ToString("D3")}");
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var bgTextrure = operationHandle.Result;
                if (bgTextrure != null)
                {
                    CurrChapterBg.texture = bgTextrure;
                }
            }
        }

        
        // private void Update()
        // {
        //     HandleInput();
        // }

        // private void HandleInput()
        // {
        //     if (Input.GetKeyUp(KeyCode.Escape))
        //     {
        //         AudioManager.Instance.PlaySFX(SFX.ui_button_click);
        //
        //         var frontUI = UIManager.Instance.GetCurrentFrontUI();
        //         if (frontUI != null)
        //         {
        //             frontUI.CloseUI();
        //         }
        //         else
        //         {
        //             // 게임 종료
        //             var uiData = new ConfirmUIData();
        //             uiData.ConfirmType = ConfirmType.OK_CANCEL;
        //             uiData.TitleTxt = "Quit";
        //             uiData.DescTxt = "Do you want to quit the game?";
        //             uiData.OKBtnTxt = "Quit";
        //             uiData.CancelBtnTxt = "Cancel";
        //             uiData.OnClickOKBtn = () =>
        //             {
        //                 Application.Quit();
        //             };
        //             UIManager.Instance.OpenUI<ConfirmUI>(uiData);
        //         }
        //     }
        // }

        public void OnClickSettingsBtn()
        {
            Logger.Log($"{GetType()}::OnClickSettingsBtn");

            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<SettingsUI>(uiData);
        }

        public void OnClickInventoryBtn()
        {
            Logger.Log($"{GetType()}::OnClickInventoryBtn");

            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<InventoryUI>(uiData);
        }

        public void OnClickCurrChapter()
        {
            Logger.Log($"{GetType()}::OnClickCurrChapter");

            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<ChapterListUI>(uiData);
        }

        public void OnClickStartBtn()
        {
            Logger.Log($"{GetType()}::OnClickStartBtn");

            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            AudioManager.Instance.StopBGM();
            LobbyManager.Instance.StartInGame();
        }

        public void OnClickAchievementBtn()
        {
            Logger.Log($"{GetType()}::OnClickAchievementBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<AchievementUI>(uiData);
        }
        
        public void OnClickShopBtn()
        {
            Logger.Log($"{GetType()}::OnClickShopBtn");
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);
            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<ShopUI>(uiData);
        }
    }
}