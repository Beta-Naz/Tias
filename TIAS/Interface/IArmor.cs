namespace TIAS.Interface
{
    public interface IArmor
    {
        float Armor { get; }
        float ReduceDamage(float incomingDamage);
    }
}
