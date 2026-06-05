using System;
using TIAS.Core.Structure;

namespace TIAS.Core.Hex
{
    public static class HexMath
    {
        /// <summary>
        /// Преобразование осевых координат в кубические
        /// </summary>
        /// <param name="hexCoord">Осевые координаты</param>
        /// <returns></returns>
        public static CubeCoord AxialToCube(HexCoord hexCoord)
        {
            int x = hexCoord.Q;
            int z = hexCoord.R;
            int y = -x - z;
            return new CubeCoord(x, y, z);
        }
        /// <summary>
        /// Преобразование кубических координат в осевые
        /// </summary>
        /// <param name="cubeCoord">Кубические координаты</param>
        /// <returns></returns>
        public static HexCoord CubeToAxial(CubeCoord cubeCoord)
        {
            return new HexCoord(cubeCoord.X, cubeCoord.Z);
        }
        /// <summary>
        /// Нахождение дистанции между гексами
        /// </summary>
        /// <param name="hexA">откуда гекс</param>
        /// <param name="hexB">куда гекс</param>
        /// <returns></returns>
        public static int Distance(HexCoord hexA, HexCoord hexB)
        {
            CubeCoord cubeA = AxialToCube(hexA);
            CubeCoord cubeB = AxialToCube(hexB);
            int distance = (Math.Abs(cubeA.X - cubeB.X) +
                Math.Abs(cubeA.Y - cubeB.Y) +
                Math.Abs(cubeA.Z - cubeB.Z)) / 2;
            return distance;
        }

    }
}
