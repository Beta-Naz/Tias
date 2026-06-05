namespace TIAS.Interface
{
    public interface IHealth
    {
        float Health { get; }
        float MaxHealth { get; }
        void TakeDamage(float damage);
        bool IsDead { get; }
    }
}
