using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.Structure;
using TIAS.Interface;
using TIAS.Models;

namespace TIAS.Core.Factory
{
    public class UnitFactory : IUnitFactory
    {
        public Unit CreateUnit(int id, HexCoord position, UnitType type, TypeAlliance alliance)
        {
            switch (type)
            {
                case UnitType.Tank:
                    return new Tank(id, position, alliance);
                case UnitType.Artillery:
                    return new Artillery(id, position, alliance);
                case UnitType.Infanity:
                    return new Infanity(id, position, alliance);
                default:
                    return null;
            }
        }

        public Unit CreateUnit(HexCoord position, UnitType type, TypeAlliance alliance)
        {
            return CreateUnit(0, position, type, alliance);
        }
    }
}
