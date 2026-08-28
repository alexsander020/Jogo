using System;
using System.Collections.Generic;
using TacticalBattle.Core;

namespace TacticalBattle.Grid
{
    public static class PathfindingService
    {
        public static List<GridCoord> ComputeReachableCells(
            GridCoord origin,
            int movementBudget,
            GridState grid,
            Func<string, UnitState> unitLookup = null,
            string currentUnitId = null,
            GridConfig config = null,
            int maxClimbHeight = 1)
        {
            config ??= BattleRulesConfig.DefaultGrid;
            var result = new List<GridCoord>();

            if (movementBudget <= 0)
            {
                result.Add(origin);
                return result;
            }

            var visited = new Dictionary<string, int>(); // "x,y" -> menor custo acumulado
            var queue = new Queue<(GridCoord coord, int cost)>();

            string originKey = GridState.Key(origin);
            visited[originKey] = 0;
            queue.Enqueue((origin, 0));

            var neighborOffsets = config.movementType == MovementType.Chebyshev
                ? GridModule.ChebyshevOffsets
                : GridModule.OrthogonalOffsets;

            UnitState currentUnit = !string.IsNullOrEmpty(currentUnitId) && unitLookup != null ? unitLookup(currentUnitId) : null;

            while (queue.Count > 0)
            {
                var (current, currentCost) = queue.Dequeue();

                // Verifica se a célula atual pode ser um destino final válido (não ocupada por outra unidade)
                string occupant = grid.GetOccupant(current);
                bool isOccupiedByOther = !string.IsNullOrEmpty(occupant) && occupant != currentUnitId;

                if (!isOccupiedByOther || current == origin)
                {
                    if (!result.Contains(current))
                    {
                        result.Add(current);
                    }
                }

                foreach (var offset in neighborOffsets)
                {
                    var neighborCoord = new GridCoord(current.x + offset.x, current.y + offset.y);
                    if (!grid.IsInside(neighborCoord.x, neighborCoord.y)) continue;

                    var neighborCell = grid.GetCell(neighborCoord.x, neighborCoord.y);
                    if (neighborCell == null || !neighborCell.isWalkable) continue;

                    // Ajusta coordenada com a altura da célula de destino
                    neighborCoord.z = neighborCell.coord.z;

                    // Checagem de limite de elevação / escalada
                    var currentCell = grid.GetCell(current.x, current.y);
                    int currentZ = currentCell != null ? currentCell.coord.z : current.z;
                    int heightDiff = Math.Abs(neighborCoord.z - currentZ);

                    if (heightDiff > maxClimbHeight)
                    {
                        continue; // Altura além do limite de escalada
                    }

                    // Checagem de ocupação para tráfego
                    string neighborOccupant = grid.GetOccupant(neighborCoord);
                    if (!string.IsNullOrEmpty(neighborOccupant) && neighborOccupant != currentUnitId)
                    {
                        UnitState occupyingUnit = unitLookup != null ? unitLookup(neighborOccupant) : null;
                        if (occupyingUnit != null && currentUnit != null)
                        {
                            bool isAlly = occupyingUnit.team == currentUnit.team;
                            if (isAlly && !config.allowPassThroughAllies)
                            {
                                continue; // Bloqueado por aliado se atravessar não for permitido
                            }
                            if (!isAlly)
                            {
                                continue; // Bloqueado por inimigo
                            }
                        }
                        else
                        {
                            // Se não tiver informações de time, trata como bloqueador por padrão
                            continue;
                        }
                    }

                    int moveCost = neighborCell.terrainCost <= 0 ? 1 : neighborCell.terrainCost;
                    int newCost = currentCost + moveCost;

                    if (newCost > movementBudget) continue;

                    string neighborKey = GridState.Key(neighborCoord);
                    if (!visited.ContainsKey(neighborKey) || visited[neighborKey] > newCost)
                    {
                        visited[neighborKey] = newCost;
                        queue.Enqueue((neighborCoord, newCost));
                    }
                }
            }

            return result;
        }

        // Calcula o caminho ótimo de origem para destino usando BFS ponderado
        public static List<GridCoord> FindPath(
            GridCoord origin,
            GridCoord target,
            GridState grid,
            Func<string, UnitState> unitLookup = null,
            string currentUnitId = null,
            GridConfig config = null,
            int maxClimbHeight = 1)
        {
            config ??= BattleRulesConfig.DefaultGrid;
            var prev = new Dictionary<string, GridCoord>();
            var costSoFar = new Dictionary<string, int>();
            var queue = new PriorityQueue<GridCoord, int>();

            string originKey = GridState.Key(origin);
            costSoFar[originKey] = 0;
            queue.Enqueue(origin, 0);

            var neighborOffsets = config.movementType == MovementType.Chebyshev
                ? GridModule.ChebyshevOffsets
                : GridModule.OrthogonalOffsets;

            UnitState currentUnit = !string.IsNullOrEmpty(currentUnitId) && unitLookup != null ? unitLookup(currentUnitId) : null;
            bool found = false;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.x == target.x && current.y == target.y)
                {
                    found = true;
                    break;
                }

                foreach (var offset in neighborOffsets)
                {
                    var neighborCoord = new GridCoord(current.x + offset.x, current.y + offset.y);
                    if (!grid.IsInside(neighborCoord.x, neighborCoord.y)) continue;

                    var neighborCell = grid.GetCell(neighborCoord.x, neighborCoord.y);
                    if (neighborCell == null || !neighborCell.isWalkable) continue;

                    neighborCoord.z = neighborCell.coord.z;

                    var currentCell = grid.GetCell(current.x, current.y);
                    int currentZ = currentCell != null ? currentCell.coord.z : current.z;
                    if (Math.Abs(neighborCoord.z - currentZ) > maxClimbHeight) continue;

                    string neighborOccupant = grid.GetOccupant(neighborCoord);
                    if (!string.IsNullOrEmpty(neighborOccupant) && neighborOccupant != currentUnitId)
                    {
                        bool isTarget = (neighborCoord.x == target.x && neighborCoord.y == target.y);
                        if (isTarget)
                        {
                            // Destino ocupado não é acessível
                            continue;
                        }

                        UnitState occupyingUnit = unitLookup != null ? unitLookup(neighborOccupant) : null;
                        if (occupyingUnit != null && currentUnit != null)
                        {
                            bool isAlly = occupyingUnit.team == currentUnit.team;
                            if (!isAlly || !config.allowPassThroughAllies) continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    int moveCost = neighborCell.terrainCost <= 0 ? 1 : neighborCell.terrainCost;
                    int newCost = costSoFar[GridState.Key(current)] + moveCost;

                    string neighborKey = GridState.Key(neighborCoord);
                    if (!costSoFar.ContainsKey(neighborKey) || newCost < costSoFar[neighborKey])
                    {
                        costSoFar[neighborKey] = newCost;
                        prev[neighborKey] = current;
                        int priority = newCost + GridModule.DistanceManhattan(neighborCoord, target);
                        queue.Enqueue(neighborCoord, priority);
                    }
                }
            }

            if (!found) return new List<GridCoord>();

            // Reconstrói caminho
            var path = new List<GridCoord>();
            var curr = target;
            string currKey = GridState.Key(curr);

            while (prev.ContainsKey(currKey))
            {
                path.Add(curr);
                curr = prev[currKey];
                currKey = GridState.Key(curr);
            }
            path.Add(origin);
            path.Reverse();

            return path;
        }
    }

    // Estrutura de fila de prioridade simples para Pathfinding
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private List<(TElement Element, TPriority Priority)> _elements = new List<(TElement, TPriority)>();

        public int Count => _elements.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            _elements.Add((element, priority));
            _elements.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public TElement Dequeue()
        {
            if (_elements.Count == 0) throw new InvalidOperationException("Fila vazia.");
            var item = _elements[0];
            _elements.RemoveAt(0);
            return item.Element;
        }
    }
}
