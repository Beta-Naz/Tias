using TIAS.Core.Structure;
using TIAS.Interface;
using TIAS.Models;

namespace TIAS.Core.StrategyPatern.Interface
{
    public interface IMovementStrategy
    {
        HexCoord Position { get; }
        int Speed { get; }
        void Move(HexCoord targetPosition);
        bool IsValidWay(HexCoord targetPosition);
    }
}
