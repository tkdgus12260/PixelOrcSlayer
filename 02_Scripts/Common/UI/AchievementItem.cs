using System;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuperMaxim.Messaging;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class AchievementItemData : InfiniteScrollData
    {
        public AchievementType AchievementType;
        public long AchieveAmount;
        public bool IsAchieved;
        public bool IsRewardClaimed;
    }

    public class AchievementItem : InfiniteScrollItem
    {
        public GameObject AchievedBg;
        public GameObject UnAchievedBg;
        public TextMeshProUGUI AchievementNameTxt;
        public Slider AchievementProgressSlider;
        public TextMeshProUGUI AchievementProgressTxt;
        public Image RewardIcon;
        public TextMeshProUGUI RewardAmountTxt;
        public Button ClaimBtn;
        public Image ClaimBtnImg;
        public TextMeshProUGUI ClaimBtnTxt;
        
        private AchievementItemData _achievementItemData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);
            
            _achievementItemData = scrollData as AchievementItemData;
            if (_achievementItemData == null)
            {
                Logger.LogError("_achievementItemData is invalid.");
                return;
            }

            var achievementData = DataTableManager.Instance.GetAchievementData(_achievementItemData.AchievementType);
            if (achievementData == null)
            {
                Logger.LogError("achievementData does not exist.");
                return;
            }
            
            AchievedBg.SetActive(_achievementItemData.IsAchieved);
            UnAchievedBg.SetActive(!_achievementItemData.IsAchieved);
            AchievementNameTxt.text = achievementData.AchievementName;
            AchievementProgressSlider.value = (float)_achievementItemData.AchieveAmount / achievementData.AchievementGoal;
            AchievementProgressTxt.text = $"{_achievementItemData.AchieveAmount:N0}/{achievementData.AchievementGoal:N0}";
            RewardAmountTxt.text = achievementData.AchievementRewardAmount.ToString("N0");

            var rewardTextureName = 0;
            switch (achievementData.AchievementRewardType)
            {
                case GlobalDefine.RewardType.Gold:
                    rewardTextureName = (int)GoodsItemType.Gold;
                    break;
                case GlobalDefine.RewardType.Gem:
                    rewardTextureName = (int)GoodsItemType.Gem;
                    break;
                default:
                    break;
            }

            AsyncOperationHandle<Texture2D> operationHandle =
                Addressables.LoadAssetAsync<Texture2D>(rewardTextureName.ToString());
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var rewardTexture = operationHandle.Result;
                if (rewardTexture != null)
                {
                    RewardIcon.sprite = Sprite.Create(rewardTexture,
                        new Rect(0, 0, rewardTexture.width, rewardTexture.height), new Vector2(1f, 1f));
                }

                ClaimBtn.enabled = _achievementItemData.IsAchieved && !_achievementItemData.IsRewardClaimed;
                ClaimBtnImg.color = ClaimBtn.enabled ? Color.white : Color.gray;
                ClaimBtnTxt.color = ClaimBtn.enabled ? Color.white : Color.gray;
            }
        }

        public void OnClickClaimBtn()
        {
            if (!_achievementItemData.IsAchieved || _achievementItemData.IsRewardClaimed)
            {
                return;
            }

            var userAchievementData = UserDataManager.Instance.GetUserData<UserAchievementData>();
            if (userAchievementData == null)
            {
                Logger.LogError("userAchievementData does not exist.");
                return;
            }
            
            var achievementData = DataTableManager.Instance.GetAchievementData(_achievementItemData.AchievementType);
            if (achievementData == null)
            {
                Logger.LogError("achievementData does not exist.");
                return;
            }

            var userAchievedData = userAchievementData.GetUserAchievementProgressData(_achievementItemData.AchievementType);
            if (userAchievedData != null)
            {
                var userGoodsData = UserDataManager.Instance.GetUserData<UserGoodsData>();
                if (userGoodsData != null)
                {
                    userAchievedData.IsRewardClaimed = true;
                    userAchievementData.SaveData();
                    _achievementItemData.IsRewardClaimed = true;
                    
                    switch (achievementData.AchievementRewardType)
                    {
                        case GlobalDefine.RewardType.Gold:
                            userGoodsData.Gold += achievementData.AchievementRewardAmount;
                            var goldUpdateMsg = new GoldUpdateMsg();
                            goldUpdateMsg.IsAdd = true;
                            Messenger.Default.Publish(goldUpdateMsg);
                            break;
                        case GlobalDefine.RewardType.Gem:
                            userGoodsData.Gem += achievementData.AchievementRewardAmount;
                            var gemUpdateMsg = new GemUpdateMsg();
                            gemUpdateMsg.IsAdd = true;
                            Messenger.Default.Publish(gemUpdateMsg);
                            break;
                        default:
                            break;
                    }

                    userGoodsData.SaveData();

                    if (achievementData.AchievementRewardType == GlobalDefine.RewardType.Gold)
                    {
                        var achievementTypes =
                            DataTableManager.Instance.GetAchievementTypes(GlobalDefine.RewardType.Gem);

                        foreach (var achievementType in achievementTypes)
                        {
                            userAchievementData.ProgressAchievement(achievementType, achievementData.AchievementRewardAmount);
                        }
                    }
                    else
                    {
                        var achievementProgressMsg = new AchievementProgressMsg();
                        Messenger.Default.Publish(achievementProgressMsg);
                    }
                }
            }
        }
    }   
}
