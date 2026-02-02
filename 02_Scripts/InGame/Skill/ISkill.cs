namespace PixelSurvival
{
    public interface ISkill
    {
        void Init(SkillData skillData, int damage, Team team);
    }
}