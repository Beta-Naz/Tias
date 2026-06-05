namespace TIAS.Core.Structure
{
    public struct HexCoord
    {
        public int Q, R;
        /// <summary>
        /// Конструктор для осевых координат
        /// </summary>
        /// <param name="q">Столбец</param>
        /// <param name="r">Ряд</param>
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }
    }
}
