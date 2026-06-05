using System.Collections.Generic;
using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.Structure;

namespace TIAS.Core.Models
{
    public class HexMap
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public CellType[,] Cells { get; set; }
        public List<Unit> Units { get; private set; }
        public HexMap()
        {
            Units = new List<Unit>();
        }
        public string MapName { get; set; }
        public string Description { get; set; }
        public HexMap(int id, int width, int height)
        {
            Id = id;
            Width = width;
            Height = height;
            Cells = LoadCellType(width, height);
            Units = new List<Unit>();
        }
        public HexMap(int id, int width, int height, CellType[,] cells, List<Unit> units)
        {
            Id = id;
            Width = width;
            Height = height;
            Cells = cells;
            Units = new List<Unit>(units);
        }
        public void AddUnit(Unit unit)
        {
            if (Units == null)
            {
                return;
            }
            if(GetUnit(unit.Position) != null)
            {
                return;
            }
            Units.Add(unit);
        }
        public void DeleteUnit(Unit unit)
        {
            Units.Remove(unit);
        }
        private CellType[,] LoadCellType(int width, int height)
        {
            CellType[,] newCells = new CellType[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if(i == 0 || i == width - 1 || j == 0 || j == Height - 1)
                    {
                        newCells[i, j] = CellType.Mountain;
                    }
                    else
                    {
                        newCells[i, j] = CellType.Plain;
                    }

                }
            }
            return newCells;
        }
        public bool IsWithinBounds(HexCoord pos)
        {
            return pos.Q >= 0 && pos.Q < Width &&
                pos.R >= 0 && pos.R < Height;
        }
        public bool IsCellFree(HexCoord pos)
        {
            if(!IsWithinBounds(pos)) return false;
            if (Cells[pos.Q,pos.R] == CellType.Mountain || 
                Cells[pos.Q, pos.R] == CellType.Water)
            {
                return false;
            }
            foreach(var unit in Units)
            {
                if (unit.Position.Q == pos.Q && unit.Position.R == pos.R)
                {
                    return false;
                }
            }
            return true;
        }
        public Unit GetUnit(HexCoord pos)
        {
            if(!IsWithinBounds(pos)) return null;
            foreach (var unit in Units)
            {
                if (unit.Position.Q == pos.Q && unit.Position.R == pos.R)
                {
                    return unit;
                }
            }
            return null;
        }
        public int GetMovementCost(HexCoord pos)
        {
            int movementCost;
            switch (Cells[pos.Q, pos.R])
            {
                case CellType.Mountain:
                    movementCost = int.MaxValue;
                    break;
                case CellType.Plain:
                    movementCost = 1;
                    break;
                case CellType.Hill:
                    movementCost = 3;
                    break;
                case CellType.Water:
                    movementCost = int.MaxValue;
                    break;
                case CellType.Forest:
                    movementCost = 2;
                    break;
                case CellType.City:
                    movementCost = 1;
                    break;
                default:
                    movementCost = 1;
                    break;

            };
            return movementCost;
        }
    }
}
