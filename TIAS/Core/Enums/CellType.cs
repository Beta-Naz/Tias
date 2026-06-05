using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIAS.Core.Enums
{
    public enum CellType
    {
        Plain, //Равнина - свободно ходить
        Mountain, //Гора - нельзя ходить
        Hill, //Холмы - можно ходить, но замедляет, также снижает урон
        Water, //Вода - нельзя в привочном ходить, но можно преобразоваться в корабль, будет добавлена если успею
        Forest, //Лес - можно ходить, но замедляет, также снижает урон
        City //Город - можно ходить, снижает немного урон артиллерии
    }
}
