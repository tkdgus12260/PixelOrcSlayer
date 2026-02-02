namespace PixelSurvival
{
    public enum Team
    {
        Player,
        Enemy,
    }

    public interface IDamageable
    {
        Team Team { get; }

        bool IsInvulnerable
        {
            get;
            set;
        }
        void TakeDamage(int damage);
    }

    public interface IDamageSource
    {
        Team SourceTeam { get; }
        int Damage { get; }
    }
}