using TIAS.Core.Base;

namespace TIAS.Interface
{
    public interface IAttack
    {
        float Damage { get; }
        float AttackRange { get; }
        bool CanAttack(Unit target);
        void Attack(Unit target);
    }
}
