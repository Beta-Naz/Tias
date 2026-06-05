using TIAS.Core.Base;
using TIAS.Core.StrategyPatern.Interface;
using TIAS.Interface;

namespace TIAS.Core.StrategyPatern
{
    public class DirectDamageStrategy : IAttackStrategy
    {
        public float Damage { get; set; }
        public float AttackRange {  get; set; }
        public DirectDamageStrategy(float damage, float attackRange)
        {
            Damage = damage;
            AttackRange = attackRange;
        }
        public void ExecuteAttack(Unit target)
        {
            if (target is IHealth health)
            {
                health.TakeDamage(Damage);
            }
        }
    }
}
