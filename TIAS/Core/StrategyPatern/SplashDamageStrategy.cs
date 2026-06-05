using System.Collections.Generic;
using TIAS.Core.Base;
using TIAS.Core.Hex;
using TIAS.Core.Models;
using TIAS.Core.StrategyPatern.Interface;
using TIAS.Core.Structure;
using TIAS.Interface;

namespace TIAS.Core.StrategyPatern
{
    public class SplashDamageStrategy : IAttackStrategy
    {
        public float Damage { get; set; }
        public float AttackRange { get; set; }
        private int _splashDamage { get; set; }
        private MainWindow _playerPrefs => MainWindow.Instance;
        private HexMap CurrentLevel => _playerPrefs.Maps[_playerPrefs.CurrentLevel];
        public SplashDamageStrategy(float damage, float attackRange, int splashDamage)
        {
            Damage = damage;
            AttackRange = attackRange;
            _splashDamage = splashDamage;
        }
        public void ExecuteAttack(Unit target)
        {
            if(target is IHealth health)
            {
                health.TakeDamage(Damage);
            }
            float splashMultiplier = _splashDamage / 100f;
            float splashDamage = Damage * splashMultiplier;
            List<HexCoord> neighbars = HexDirections.GetAllNeighBor(target.Position);
            foreach(HexCoord coord in neighbars)
            {
                Unit unit = CurrentLevel.GetUnit(coord);
                if(unit != null) 
                {
                    if (unit is IHealth healthNeighbar)
                    {

                        healthNeighbar.TakeDamage(splashDamage);
                    }
                }
            }
        }
    }
}
