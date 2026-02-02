using System;
using static GlobalDefine;

namespace PixelSurvival
{
    public enum AchievementType
    {
        None = 0,
        CollectGold1,
        CollectGold2,
        CollectGold3,
        CollectGold4,
        CollectGold5,
        ClearChapter1,
        ClearChapter2,
        ClearChapter3,
        ClearChapter4,
        ClearChapter5,
        ClearChapter6,
    }

    [Serializable]
    public class AchievementData
    {
        public AchievementType AchievementType;
        public string AchievementName;
        public int AchievementGoal;
        public RewardType AchievementRewardType;
        public int AchievementRewardAmount;
    }
}
