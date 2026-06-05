using System;

namespace TIAS.Core.Structure
{
    public struct CubeCoord
    {
        public int X, Y, Z;
        /// <summary>
        /// Коснтруктор кубических координат
        /// </summary>
        /// <param name="x">Восток-запад</param>
        /// <param name="y">Северо-запад - юго-восток</param>
        /// <param name="z">Северо-восток - юго-запад</param>
        /// <exception cref="ArgumentException"></exception>
        public CubeCoord(int x, int y, int z)
        {
            if (x + y + z != 0)
            {
                throw new ArgumentException("Для кубических координат x + y + z " +
                    "должно = 0");
            }
            X = x;
            Y = y;
            Z = z;
        }
    }
}
