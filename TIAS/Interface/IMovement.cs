using TIAS.Core.Structure;
using TIAS.Models;

namespace TIAS.Interface
{
    public interface IMovement
    {
        float Speed { get; }
        HexCoord Position { get; }
        void Move(HexCoord vector2);
    }
}
