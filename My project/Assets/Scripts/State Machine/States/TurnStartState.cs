using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnStartState : State
{
    public override void Enter()
    {
        base.Enter();
        StartCoroutine(ProcessTurnStart());
    }

    IEnumerator ProcessTurnStart()
    {
        yield return null;

        Unit unit = battle.GetNextUnit();
        if (unit == null)
        {
            Debug.LogWarning("[TurnStartState] Nenhuma unidade disponível na fila de turnos.");
            yield break;
        }

        battle.StartCurrentUnitTurn();

        // Move o seletor para a unidade atual e foca a câmera
        if (unit.currentTile != null)
        {
            machine.MoveSelectorTo(unit.currentTile);
        }

        if (TacticalCameraController.Instance != null)
        {
            TacticalCameraController.Instance.FocusOn(unit.transform);
        }

        // Aguarda breve delay para percepção do turno
        yield return new WaitForSeconds(0.3f);

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateTurnBanner(unit);
        }

        if (unit.team == Team.Player)
        {
            machine.ChangeTo<ChooseActionState>();
        }
        else
        {
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateControlsPrompt(
                    "TURNO INIMIGO", 
                    $"• Aguardando decisão de {unit.unitName} (IA)..."
                );
            }
            StartCoroutine(ProcessEnemyAI(unit));
        }
    }

    IEnumerator ProcessEnemyAI(Unit enemy)
    {
        Debug.Log($"[Enemy AI] Turno da IA inimiga: {enemy.unitName}");
        yield return new WaitForSeconds(0.5f);

        // 1. Procura o jogador mais próximo
        Unit closestPlayer = null;
        int closestDist = int.MaxValue;

        foreach (var u in battle.allUnits)
        {
            if (u != null && u.team == Team.Player && u.gameObject.activeInHierarchy && u.currentTile != null)
            {
                int dist = Mathf.Abs(u.gridPosition.x - enemy.gridPosition.x) + Mathf.Abs(u.gridPosition.y - enemy.gridPosition.y);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPlayer = u;
                }
            }
        }

        // 2. Se encontrou um jogador, move em direção a ele
        if (closestPlayer != null && enemy.movement != null && enemy.CanMove() && Board.instance != null)
        {
            int movBudget = enemy.stats != null ? enemy.stats.GetStat(StatEnum.MOV) : 3;

            int currentZ = enemy.currentTile != null && enemy.currentTile.floor != null
                ? Mathf.Max(0, Board.instance.floors.IndexOf(enemy.currentTile.floor))
                : 0;

            var origin = new TacticalBattle.Core.GridCoord(enemy.gridPosition.x, enemy.gridPosition.y, currentZ);

            var gridState = new TacticalBattle.Core.GridState();
            foreach (var kvp in Board.instance.tiles)
            {
                var pos = kvp.Key;
                var tile = kvp.Value;
                int z = tile.floor != null ? Mathf.Max(0, Board.instance.floors.IndexOf(tile.floor)) : 0;
                gridState.cells[TacticalBattle.Core.GridState.Key(pos.x, pos.y)] = new TacticalBattle.Core.GridCell(pos.x, pos.y, z);

                if (tile.content != null && tile.content != enemy.gameObject)
                {
                    gridState.SetOccupant(new TacticalBattle.Core.GridCoord(pos.x, pos.y, z), tile.content.GetInstanceID().ToString());
                }
            }

            var reachable = TacticalBattle.Grid.PathfindingService.ComputeReachableCells(
                origin, 
                movBudget, 
                gridState, 
                null, 
                enemy.gameObject.GetInstanceID().ToString(),
                TacticalBattle.Core.BattleRulesConfig.DefaultGrid,
                maxClimbHeight: 1
            );

            // Encontra a melhor célula alcançável mais próxima do jogador alvo
            TacticalBattle.Core.GridCoord bestCell = origin;
            int bestDist = closestDist;

            foreach (var cell in reachable)
            {
                var tile = Board.GetTile(new Vector3Int(cell.x, cell.y, 0));
                if (tile != null && (tile.content == null || tile.content == enemy.gameObject))
                {
                    int d = Mathf.Abs(cell.x - closestPlayer.gridPosition.x) + Mathf.Abs(cell.y - closestPlayer.gridPosition.y);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestCell = cell;
                    }
                }
            }

            if (bestCell != origin)
            {
                var pathCoords = TacticalBattle.Grid.PathfindingService.FindPath(
                    origin, 
                    bestCell, 
                    gridState, 
                    null, 
                    enemy.gameObject.GetInstanceID().ToString(),
                    TacticalBattle.Core.BattleRulesConfig.DefaultGrid,
                    maxClimbHeight: 1
                );

                if (pathCoords != null && pathCoords.Count > 1)
                {
                    List<Vector3Int> pathVectors = new List<Vector3Int>();
                    foreach (var c in pathCoords) pathVectors.Add(new Vector3Int(c.x, c.y, 0));

                    yield return StartCoroutine(enemy.movement.Traverse(pathVectors));
                }
            }

            // Gira para encarar o jogador
            Vector3Int diff = closestPlayer.gridPosition - enemy.gridPosition;
            if (diff != Vector3Int.zero)
            {
                enemy.SetFacing(DirectionUtils.VectorToDirection(diff));
            }
        }

        yield return new WaitForSeconds(0.4f);

        // Transiciona para o encerramento do turno do inimigo
        machine.ChangeTo<TurnEndState>();
    }
}
