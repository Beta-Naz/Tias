using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.StrategyPatern;
using TIAS.Core.Structure;

namespace TIAS.Models
{
    public class Infanity : Unit
    {
        public Infanity(int id, HexCoord position, TypeAlliance typeAlliance)
           : base(id, 100, 15, position, typeAlliance)
        {
            UnitName = "Пехота";
            InitializeStrategy(
                new GroundMoveStrategy(12, position),
                new DirectDamageStrategy(25, 1));
        }
    }
}