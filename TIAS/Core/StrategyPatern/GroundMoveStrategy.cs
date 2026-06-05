using System;
using System.Collections.Generic;
using TIAS.Core.Hex;
using TIAS.Core.Models;
using TIAS.Core.StrategyPatern.Interface;
using TIAS.Core.Structure;

namespace TIAS.Core.StrategyPatern
{
    public class GroundMoveStrategy : IMovementStrategy
    {
        public int Speed { get; private set; }
        private HexCoord _position;
        public HexCoord Position
        {
            get => _position;
            private set
            {
                _position = value;
            }
        }
        private MainWindow _playerPrefs => MainWindow.Instance;
        public List<HexCoord> FullPath { get; private set; }
        private HexMap CurrentLevel => _playerPrefs.Maps[_playerPrefs.CurrentLevel];
        public GroundMoveStrategy(int speed, HexCoord position)
        {
            Speed = speed;
            Position = position;
        }
        public void Move(HexCoord targetPosition)
        {
            if (CurrentLevel.IsCellFree(targetPosition))
            {
                if (IsValidWay(targetPosition))
                {
                    Position = targetPosition;
                }
                else
                {
                    _playerPrefs.ErrorMessages.Add("К сожалению до данной клетки не найден не один путь");
                }
            }
            else
            {
                _playerPrefs.ErrorMessages.Add("К сожалению данная клетка не доступна");
            }
        }
        public bool IsValidWay(HexCoord targetPosition)
        {
            int directDistance = HexMath.Distance(Position, targetPosition);
            if(directDistance > Speed)
            {
                return false;
            }
            var queue = new Queue<(HexCoord position, int costPath, List<HexCoord> path)>();
            var visited = new Dictionary<HexCoord, int>();
            queue.Enqueue((Position, 0, new List<HexCoord> { Position }));
            visited[Position] = 0;
            while(queue.Count > 0)
            {
                var (current, currentCost, path) = queue.Dequeue();
                if (current.Equals(targetPosition))
                {
                    FullPath = path;
                    return true;
                }
                if (currentCost > Speed)
                {
                    continue;
                }
                foreach(var neighbor in HexDirections.GetAllNeighBor(current))
                {
                    if (!CurrentLevel.IsCellFree(neighbor))
                    {
                        continue;
                    }
                    int mostCost = CurrentLevel.GetMovementCost(neighbor);
                    int newCost = currentCost + mostCost;
                    if (visited.ContainsKey(neighbor) && visited[neighbor] <= newCost)
                    {
                        continue;
                    }
                    if(newCost <= Speed)
                    {
                        visited[neighbor] = newCost;
                        var newPath = new List<HexCoord>(path) { neighbor };
                        queue.Enqueue((neighbor, newCost, newPath));
                    }
                }
            }
            return false;
        }
    }
}
