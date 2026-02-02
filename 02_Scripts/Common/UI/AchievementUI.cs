using Gpm.Ui;
using SuperMaxim.Messaging;

namespace PixelSurvival
{
    public class AchievementUI : BaseUI
    {
        public InfiniteScroll AchievementScrollList;

        private void OnEnable()
        {
            Messenger.Default.Subscribe<AchievementProgressMsg>(OnAchievementProgressed);
            Logger.Log("AchievementUI Subscribe done");
        }

        private void OnDisable()
        {
            Messenger.Default.Unsubscribe<AchievementProgressMsg>(OnAchievementProgressed);
            Logger.Log("AchievementUI Unsubscribe done");
        }

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            SetAchievementList();
            SortAchievementList();
        }

        private void SetAchievementList()
        {
            AchievementScrollList.Clear();

            var achievementDataList = DataTableManager.Instance.GetAchievementDataList();
            var userAchievementData = UserDataManager.Instance.GetUserData<UserAchievementData>();
            if (achievementDataList != null && userAchievementData != null)
            {
                foreach (var data in achievementDataList)
                {
                    var achievementItemData = new AchievementItemData();
                    achievementItemData.AchievementType = data.AchievementType;
                    var userAchieveData = userAchievementData.GetUserAchievementProgressData(data.AchievementType);
                    if (userAchieveData != null)
                    {
                        achievementItemData.AchieveAmount = userAchieveData.AchievementAmount;
                        achievementItemData.IsAchieved = userAchieveData.IsAchieved;
                        achievementItemData.IsRewardClaimed = userAchieveData.IsRewardClaimed;
                    }
                    AchievementScrollList.InsertData(achievementItemData);
                }
            }
        }

        private void SortAchievementList()
        {
            AchievementScrollList.SortDataList((a, b) =>
            {
                var achievementA = a.data as AchievementItemData;
                var achievementB = b.data as AchievementItemData;
                
                var ACompare = achievementA.IsAchieved && !achievementA.IsRewardClaimed;
                var BCompare = achievementB.IsAchieved && !achievementB.IsRewardClaimed;

                int compareResult = BCompare.CompareTo(ACompare);
                if (compareResult == 0)
                {
                    compareResult = achievementA.IsAchieved.CompareTo(achievementB.IsAchieved);
                    if (compareResult == 0)
                    {
                        compareResult = (achievementA.AchievementType).CompareTo(achievementB.AchievementType);
                    }
                }
                
                return compareResult;
            });
        }

        private void OnAchievementProgressed(AchievementProgressMsg achievementProgressMsg)
        {
            Logger.Log($"{GetType()}::OnAchievementProgressed");
            SetAchievementList();
            SortAchievementList();
        }
    }   
}
