using System;
using System.Collections;
using System.Collections.Generic;
using TacticalBattle.Core;
using TacticalBattle.Grid;
using UnityEngine;

public class MoveTargetState : State
{
    private HashSet<Vector3Int> reachableTiles = new HashSet<Vector3Int>();
    private bool isMoving = false;

    public override void Enter()
    {
        base.Enter();
        isMoving = false;

        if (currentUnit == null || !currentUnit.CanMove())
        {
            Debug.LogWarning("[MoveTargetState] Unidade não pode se mover!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        // Garante que o GridHighlighter existe na cena
        if (GridHighlighter.Instance == null)
        {
            var hl = FindFirstObjectByType<GridHighlighter>();
            if (hl == null)
            {
                GameObject go = new GameObject("GridHighlighter");
                go.AddComponent<GridHighlighter>();
            }
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
        }

        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        CalculateReachableCells();

        // 1. Renderiza os destaques azuis em todas as células alcançáveis
        if (GridHighlighter.Instance != null && currentUnit.currentTile != null)
        {
            GridHighlighter.Instance.ShowMovementRange(reachableTiles, currentUnit.currentTile.pos);
        }

        int mov = currentUnit.stats != null ? currentUnit.stats.GetStat(StatEnum.MOV) : 3;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateControlsPrompt(
                "SELECIONE O DESTINO", 
                $"• [W / A / S / D ou SETAS] : Navegar pelo Grid (Alcance: {mov} tiles)\n• [ESPAÇO / ENTER / Z] : Confirmar Movimento\n• [X / ESC] : Cancelar e Voltar ao Menu"
            );
        }

        if (currentUnit.currentTile != null)
        {
            machine.MoveSelectorTo(currentUnit.currentTile);
        }

        UpdateHUDPrompt();
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
        reachableTiles.Clear();

        // Limpa os destaques ao sair do estado
        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }
    }

    void CalculateReachableCells()
    {
        reachableTiles.Clear();
        if (currentUnit == null || currentUnit.currentTile == null) return;

        GridState grid = BuildGridStateFromBoard();
        int movBudget = currentUnit.stats != null ? currentUnit.stats.GetStat(StatEnum.MOV) : 3;

        int currentZ = currentUnit.currentTile.floor != null && Board.instance != null
            ? Math.Max(0, Board.instance.floors.IndexOf(currentUnit.currentTile.floor))
            : 0;

        var origin = new GridCoord(currentUnit.gridPosition.x, currentUnit.gridPosition.y, currentZ);

        string currentUnitId = currentUnit.gameObject.GetInstanceID().ToString();
        var coords = PathfindingService.ComputeReachableCells(
            origin, 
            movBudget, 
            grid, 
            unitId => FindUnitStateById(unitId), 
            currentUnitId,
            BattleRulesConfig.DefaultGrid,
            maxClimbHeight: 1
        );

        foreach (var c in coords)
        {
            reachableTiles.Add(new Vector3Int(c.x, c.y, 0));
        }

        Debug.Log($"[MoveTargetState] Células alcançáveis calculadas: {reachableTiles.Count} células.");
    }

    void OnMove(object sender, object args)
    {
        if (isMoving) return;

        Vector3Int input = (Vector3Int)args;
        if (input == Vector3Int.zero) return;

        // 1. Gira a unidade para encarar a direção selecionada (Norte, Sul, Leste, Oeste)
        if (currentUnit != null)
        {
            FacingDirection newFacing = DirectionUtils.VectorToDirection(input);
            currentUnit.SetFacing(newFacing);
        }

        // 2. Desloca o seletor pelo grid
        TileLogic t = Board.GetTile(Selector.Instance.position + input);
        if (t != null)
        {
            machine.MoveSelectorTo(t);
        }

        // 3. Atualiza os visuais de destaque do Grid e Seletor
        UpdateHUDPrompt();
    }

    void UpdateHUDPrompt()
    {
        if (currentUnit == null) return;

        bool isReachable = false;
        bool isFree = false;

        if (Selector.Instance != null && Selector.Instance.tile != null)
        {
            Vector3Int currentPos = new Vector3Int(Selector.Instance.tile.pos.x, Selector.Instance.tile.pos.y, 0);
            isReachable = reachableTiles.Contains(currentPos);
            isFree = Selector.Instance.tile.content == null || Selector.Instance.tile.content == currentUnit.gameObject;
        }

        bool isValid = isReachable && isFree;

        // Atualiza a cor do cursor (Branco se válido, Vermelho se inválido/bloqueado)
        if (GridHighlighter.Instance != null && Selector.Instance != null)
        {
            GridHighlighter.Instance.UpdateHover(Selector.Instance.position, isValid);
        }

        if (BattleHUD.Instance != null)
        {
            string facingName = currentUnit.facing switch
            {
                FacingDirection.North => "NORTE (▲ / +Y)",
                FacingDirection.South => "SUL (▼ / -Y)",
                FacingDirection.East => "LESTE (► / +X)",
                FacingDirection.West => "OESTE (◄ / -X)",
                _ => "SUL"
            };

            string statusText = isValid 
                ? "<color=#55FF55><b>● CÉLULA VÁLIDA (BRANCO)</b></color>" 
                : "<color=#FF4444><b>● CÉLULA INVÁLIDA (VERMELHO)</b></color>";

            BattleHUD.Instance.UpdateControlsPrompt(
                $"MOVER — DIREÇÃO: {facingName}",
                $"• {statusText}\n• [W/A/S/D ou SETAS] : Mover Cursor | [AZUL : Área de Alcance]\n• [ESPAÇO / ENTER / Z] : Confirmar Destino\n• [X / ESC] : Cancelar e Voltar"
            );
        }
    }

    void OnFire(object sender, object args)
    {
        if (isMoving) return;

        int button = (int)args;

        if (button == 1)
        {
            // Confirmar destino
            TileLogic targetTile = Selector.Instance.tile;
            if (targetTile == null) return;

            Vector3Int targetPos = new Vector3Int(targetTile.pos.x, targetTile.pos.y, 0);

            if (!reachableTiles.Contains(targetPos))
            {
                Debug.LogWarning("[MoveTargetState] Célula selecionada fora do alcance de movimento!");
                return;
            }

            if (targetTile.content != null && targetTile.content != currentUnit.gameObject)
            {
                Debug.LogWarning("[MoveTargetState] Célula de destino já está ocupada por outra unidade!");
                return;
            }

            // Se selecionou a mesma célula onde já está
            if (targetTile == currentUnit.currentTile)
            {
                currentUnit.hasMoved = true;
                machine.ChangeTo<ChooseActionState>();
                return;
            }

            StartCoroutine(ExecuteMovement(targetTile));
        }
        else if (button == 2)
        {
            // Cancelar e retornar ao menu de ações
            if (currentUnit != null && currentUnit.currentTile != null)
            {
                machine.MoveSelectorTo(currentUnit.currentTile);
            }
            machine.ChangeTo<ChooseActionState>();
        }
    }

    IEnumerator ExecuteMovement(TileLogic targetTile)
    {
        isMoving = true;

        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateControlsPrompt(
                "MOVENDO...", 
                $"• {currentUnit.unitName} deslocando-se para o destino..."
            );
        }

        GridState grid = BuildGridStateFromBoard();
        int currentZ = currentUnit.currentTile.floor != null && Board.instance != null
            ? Math.Max(0, Board.instance.floors.IndexOf(currentUnit.currentTile.floor))
            : 0;

        int targetZ = targetTile.floor != null && Board.instance != null
            ? Math.Max(0, Board.instance.floors.IndexOf(targetTile.floor))
            : 0;

        var origin = new GridCoord(currentUnit.gridPosition.x, currentUnit.gridPosition.y, currentZ);
        var dest = new GridCoord(targetTile.pos.x, targetTile.pos.y, targetZ);

        string currentUnitId = currentUnit.gameObject.GetInstanceID().ToString();
        var pathCoords = PathfindingService.FindPath(
            origin, 
            dest, 
            grid, 
            unitId => FindUnitStateById(unitId), 
            currentUnitId,
            BattleRulesConfig.DefaultGrid,
            maxClimbHeight: 1
        );

        if (pathCoords == null || pathCoords.Count == 0)
        {
            Debug.LogWarning("[MoveTargetState] Pathfinding não encontrou caminho detalhado. Usando rota direta.");
            pathCoords = new List<GridCoord> { origin, dest };
        }

        List<Vector3Int> pathVectors = new List<Vector3Int>();
        foreach (var c in pathCoords)
        {
            pathVectors.Add(new Vector3Int(c.x, c.y, 0));
        }

        Movement movComponent = currentUnit.movement ?? currentUnit.GetComponent<Movement>() ?? currentUnit.GetComponentInChildren<Movement>();
        if (movComponent != null)
        {
            yield return movComponent.Traverse(pathVectors);
        }

        currentUnit.PlaceAtTile(targetTile);
        currentUnit.hasMoved = true;
        isMoving = false;

        // Atualiza a posição do seletor para a nova posição da unidade
        if (currentUnit.currentTile != null)
        {
            machine.MoveSelectorTo(currentUnit.currentTile);
        }

        Debug.Log($"[MoveTargetState] Movimento concluído com sucesso para {currentUnit.unitName} na posição {targetTile.pos}. Retornando ao Menu de Ações.");
        machine.ChangeTo<ChooseActionState>();
    }

    private GridState BuildGridStateFromBoard()
    {
        var grid = new GridState();
        if (Board.instance == null || Board.instance.tiles == null) return grid;

        int minX = 0, maxX = 0, minY = 0, maxY = 0;

        foreach (var kvp in Board.instance.tiles)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            int z = tile.floor != null && Board.instance.floors != null
                ? Math.Max(0, Board.instance.floors.IndexOf(tile.floor))
                : 0;

            grid.cells[GridState.Key(pos.x, pos.y)] = new GridCell(pos.x, pos.y, z, terrainCost: 1, isWalkable: true);

            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;

            if (tile.content != null)
            {
                var u = tile.content.GetComponent<Unit>();
                if (u != null)
                {
                    grid.SetOccupant(new GridCoord(pos.x, pos.y, z), u.gameObject.GetInstanceID().ToString());
                }
            }
        }

        grid.width = Math.Max(10, maxX + 5);
        grid.height = Math.Max(10, maxY + 5);
        return grid;
    }

    private UnitState FindUnitStateById(string unitId)
    {
        if (BattleController.Instance == null) return null;
        var unit = BattleController.Instance.allUnits.Find(u => u != null && u.gameObject.GetInstanceID().ToString() == unitId);
        if (unit == null) return null;

        return TacticalBattle.Integration.UnitAdapter.CreateUnitStateFromMono(unit);
    }
}
