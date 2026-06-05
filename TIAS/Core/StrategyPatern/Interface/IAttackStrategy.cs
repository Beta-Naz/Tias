using TIAS.Core.Base;

namespace TIAS.Core.StrategyPatern.Interface
{
    public interface IAttackStrategy
    {
        float Damage { get; }
        float AttackRange { get; }
        void ExecuteAttack(Unit target);
    }
}
