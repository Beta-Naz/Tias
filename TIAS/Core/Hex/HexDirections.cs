using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIAS.Core.Structure;

namespace TIAS.Core.Hex
{
    public static class HexDirections
    {
        private static readonly CubeCoord[] CubeDirections =
        {
            new CubeCoord(1, -1, 0), // Восток (E) - направление 1
            new CubeCoord(1, 0, -1), // Северо-восток (NE) - направление 2
            new CubeCoord(0, 1, -1), // Северо-запад (NW) - направление 3
            new CubeCoord(-1, 1, 0), // Запад (W) - направление 4
            new CubeCoord(-1, 0, 1), // Юго-запад (SW) - направление 5
            new CubeCoord(0, -1, 1)  // Юго-восток (SE) - направление 6
        };
        private static readonly HexCoord[] AxialDirections =
        {
            new HexCoord(1,0),  // Восток (E)
            new HexCoord(1,-1), // Северо-восток (NE)
            new HexCoord(0,-1), // Северо-запад (NW)
            new HexCoord(-1,0), // Запад (W)
            new HexCoord(-1,1), // Юго-запад (SW)
            new HexCoord(0,1),  // Юго-восток (SE)
        };
        public static HexCoord GetNeighbor(HexCoord center, int direction)
        {
            direction = direction % 6;
            HexCoord dir = AxialDirections[direction];
            return new HexCoord(
                center.Q + dir.Q,
                center.R + dir.R
                );
        }
        public static List<HexCoord> GetAllNeighBor(HexCoord center)
        {
            var neighbors = new List<HexCoord>();
            for(int i = 0; i < AxialDirections.Length; i++)
            {
                neighbors.Add(GetNeighbor(center, i));
            }
            return neighbors;
        }
        public static string PrintNeighborPositions(HexCoord center)
        {
            string message = $"Центр: ({center.Q}, {center.R})" +
             "Соседи:" +
             $"   NW ({center.Q}, {center.R - 1})    NE ({center.Q + 1}, {center.R - 1})" +
             $"W ({center.Q - 1}, {center.R})    [ЦЕНТР]    E ({center.Q + 1}, {center.R})" +
             $"   SW ({center.Q - 1}, {center.R + 1})    SE ({center.Q}, {center.R + 1})";
            return message;
        }
    }
}
