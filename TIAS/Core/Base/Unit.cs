using System;
using System.Collections.Generic;
using TIAS.Core.Enums;
using TIAS.Core.Hex;
using TIAS.Core.Models;
using TIAS.Core.StrategyPatern.Interface;
using TIAS.Core.Structure;
using TIAS.Interface;

namespace TIAS.Core.Base
{
    public abstract class Unit : IAttack, IMovement, IHealth, IArmor, ISelectable, ITurnBased
    {
        public int Id { get; set; }
        public TypeAlliance NameAlliance { get; private set; }
        public string UnitName { get; protected set; }

        private float _health;
        public float Health
        {
            get => _health;
            set
            {
                _health = Math.Max(0, Math.Min(value, MaxHealth));
                OnHealthChanged?.Invoke();
            }
        }

        public float MaxHealth { get; private set; }
        public float Damage => AttackStrategy?.Damage ?? 0;
        public float AttackRange => AttackStrategy?.AttackRange ?? 0;
        public int Speed => MovementStrategy?.Speed ?? 0;
        public HexCoord Position => MovementStrategy.Position;
        public bool IsDead => Health <= 0;
        public float Armor { get; private set; }

        // Turn-based properties
        public bool HasMovedThisTurn { get; set; }
        public bool HasAttackedThisTurn { get; set; }
        public bool IsSelected { get; set; }
        public event Action<float> OnTakeDamage;
        // Events
        public event Action OnHealthChanged;
        public event Action OnUnitDied;
        public event Action<Unit> OnSelectedChanged;
        public event Action<HexCoord, HexCoord> OnPositionChanged;

        protected IAttackStrategy AttackStrategy { get; private set; }
        protected IMovementStrategy MovementStrategy { get; private set; }

        // Cached reachable positions
        public HashSet<HexCoord> ReachablePositions { get; private set; }
        public HashSet<HexCoord> AttackablePositions { get; private set; }

        // Reference to current map
        protected HexMap CurrentMap { get; set; }
        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            float reducedDamage = ReduceDamage(damage);
            Health -= reducedDamage;

            // Вызываем событие получения урона
            OnTakeDamage?.Invoke(reducedDamage);

            if (IsDead)
            {
                OnUnitDied?.Invoke();
            }
        }
        protected Unit(int id, float maxHealth, float armor, HexCoord position, TypeAlliance typeAlliance)
        {
            Id = id;
            Armor = armor;
            MaxHealth = maxHealth;
            Health = maxHealth;
            NameAlliance = typeAlliance;
            ReachablePositions = new HashSet<HexCoord>();
            AttackablePositions = new HashSet<HexCoord>();
        }

        public void SetCurrentMap(HexMap map)
        {
            CurrentMap = map;
        }

        protected void InitializeStrategy(IMovementStrategy movementStrategy, IAttackStrategy attackStrategy)
        {
            MovementStrategy = movementStrategy;
            AttackStrategy = attackStrategy;
        }

        public void Attack(Unit target)
        {
            if (IsDead || HasAttackedThisTurn || !CanAttack(target)) return;

            AttackStrategy?.ExecuteAttack(target);
            HasAttackedThisTurn = true;

            // If target died, handle it
            if (target.IsDead)
            {
                target.OnUnitDied?.Invoke();
            }
        }

        public bool CanAttack(Unit target)
        {
            if (AttackStrategy == null || target == null || target.IsDead) return false;
            if (target.NameAlliance == NameAlliance) return false; // Can't attack allies

            float distance = HexMath.Distance(Position, target.Position);
            return AttackRange >= distance - 0.1f; // Небольшой допуск для погрешности
        }

        public void Move(HexCoord target)
        {
            if (IsDead || HasMovedThisTurn || !ReachablePositions.Contains(target)) return;

            var oldPosition = Position;
            MovementStrategy?.Move(target);

            HasMovedThisTurn = true;

            // Вызываем событие об изменении позиции
            OnPositionChanged?.Invoke(oldPosition, target);
        }

        public float ReduceDamage(float incomingDamage)
        {
            return Math.Max(1, incomingDamage - Armor);
        }
        public List<HexCoord> CurrentPath { get; set; }
        public void ResetTurn()
        {
            HasMovedThisTurn = false;
            HasAttackedThisTurn = false;
        }

        public bool CanAct()
        {
            return !IsDead && (!HasMovedThisTurn || !HasAttackedThisTurn);
        }

        public void OnSelected()
        {
            IsSelected = true;
            CalculateReachablePositions();
            CalculateAttackablePositions();
            OnSelectedChanged?.Invoke(this);
        }

        public void OnDeselected()
        {
            IsSelected = false;
            ReachablePositions.Clear();
            AttackablePositions.Clear();
            OnSelectedChanged?.Invoke(this);
        }

        private void CalculateReachablePositions()
        {
            ReachablePositions.Clear();
            if (HasMovedThisTurn) return;

            var map = CurrentMap ?? GetCurrentMapFromMainWindow();
            if (map == null) return;

            // BFS for reachable positions
            var queue = new Queue<(HexCoord pos, int cost)>();
            var visited = new Dictionary<HexCoord, int>();

            queue.Enqueue((Position, 0));
            visited[Position] = 0;
            ReachablePositions.Add(Position);

            while (queue.Count > 0)
            {
                var (current, currentCost) = queue.Dequeue();

                foreach (var neighbor in HexDirections.GetAllNeighBor(current))
                {
                    if (!map.IsWithinBounds(neighbor)) continue;

                    int moveCost = map.GetMovementCost(neighbor);
                    if (moveCost == int.MaxValue) continue; // Impassable

                    int newCost = currentCost + moveCost;

                    if (newCost <= Speed && (!visited.ContainsKey(neighbor) || visited[neighbor] > newCost))
                    {
                        visited[neighbor] = newCost;
                        ReachablePositions.Add(neighbor);
                        queue.Enqueue((neighbor, newCost));
                    }
                }
            }
        }

        private void CalculateAttackablePositions()
        {
            AttackablePositions.Clear();
            if (HasAttackedThisTurn) return;

            var map = CurrentMap ?? GetCurrentMapFromMainWindow();
            if (map == null) return;

            // Get all units that can be attacked
            foreach (var pos in ReachablePositions)
            {
                float distance = HexMath.Distance(pos, Position);
                if (distance <= AttackRange + 0.1f)
                {
                    var unitAtPos = map.GetUnit(pos);
                    if (unitAtPos != null && unitAtPos.NameAlliance != NameAlliance)
                    {
                        AttackablePositions.Add(pos);
                    }
                }
            }
        }
        public List<HexCoord> GetPathTo(HexCoord target)
        {
            if (!ReachablePositions.Contains(target)) return null;

            var map = CurrentMap ?? GetCurrentMapFromMainWindow();
            if (map == null) return null;

            // BFS для поиска пути
            var queue = new Queue<(HexCoord pos, List<HexCoord> path)>();
            var visited = new HashSet<HexCoord>();

            queue.Enqueue((Position, new List<HexCoord> { Position }));
            visited.Add(Position);

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();

                if (current.Equals(target))
                {
                    return path;
                }

                foreach (var neighbor in HexDirections.GetAllNeighBor(current))
                {
                    if (!visited.Contains(neighbor) && map.IsCellFree(neighbor) && ReachablePositions.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<HexCoord>(path) { neighbor };
                        queue.Enqueue((neighbor, newPath));
                    }
                }
            }

            return null;
        }
        private HexMap GetCurrentMapFromMainWindow()
        {
            try
            {
                return MainWindow.Instance?.Maps?[MainWindow.Instance.CurrentLevel];
            }
            catch
            {
                return null;
            }
        }

        float IMovement.Speed => Speed;
    }
}