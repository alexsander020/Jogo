using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.Core;
using UnityEngine;

public class AppGappaiState : State
{
    private HashSet<Vector3Int> gappaiAreaTiles = new HashSet<Vector3Int>();
    private List<Unit> alliesInRange = new List<Unit>();

    public override void Enter()
    {
        base.Enter();
        gappaiAreaTiles.Clear();
        alliesInRange.Clear();

        if (currentUnit == null || !currentUnit.CanAct())
        {
            Debug.LogWarning("[AppGappaiState] Unidade atual não pode realizar ações neste turno!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        // 1. O sistema identifica automaticamente o Appmon que está realizando a ação no turno atual
        Debug.Log($"[AppGappaiState] Monstro do turno identificado: {currentUnit.unitName} na posição {currentUnit.gridPosition}.");

        // 2. O jogo destaca no tabuleiro a área de efeito de 4x4 quadros ao redor desse monstro
        CalculateGappai4x4Area();

        if (GridHighlighter.Instance != null && currentUnit.currentTile != null)
        {
            GridHighlighter.Instance.ShowGappaiRange(gappaiAreaTiles, currentUnit.currentTile.pos);
        }

        // 3. Coleta os aliados presentes dentro do raio de 4x4
        FindAlliesIn4x4Area();

        // 4. Configura prompts do HUD
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
            BattleHUD.Instance.UpdateControlsPrompt(
                "SISTEMA DE APP GAPPAI (FUSÃO)",
                "• [W / A / S / D ou SETAS] : Navegar Grade 4x4    [ESPAÇO / ENTER / Z] : Confirmar    [X / ESC] : Voltar ao Menu"
            );
        }

        // 5. Garante e abre a interface do Painel de Fusão em Batalha
        if (AppGappaiMenuUI.Instance == null)
        {
            GameObject uiObj = new GameObject("AppGappaiMenuUI", typeof(AppGappaiMenuUI));
            AppGappaiMenuUI.Instance = uiObj.GetComponent<AppGappaiMenuUI>();
        }

        AppGappaiMenuUI.Instance.Open(currentUnit, alliesInRange, HandleGappaiClosed);
    }

    public override void Exit()
    {
        base.Exit();
        gappaiAreaTiles.Clear();
        alliesInRange.Clear();

        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }

        if (AppGappaiMenuUI.Instance != null)
        {
            AppGappaiMenuUI.Instance.Hide();
        }
    }

    private void CalculateGappai4x4Area()
    {
        gappaiAreaTiles.Clear();
        if (currentUnit == null || currentUnit.currentTile == null) return;

        Vector3Int origin = currentUnit.currentTile.pos;
        int maxReach = 4; // Raio/alcance de 4 quadros ao redor

        for (int dx = -maxReach; dx <= maxReach; dx++)
        {
            for (int dy = -maxReach; dy <= maxReach; dy++)
            {
                // Limita à área 4x4 ao redor (distância máxima de 4 tiles)
                if (Mathf.Abs(dx) <= maxReach && Mathf.Abs(dy) <= maxReach)
                {
                    Vector3Int pos = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
                    TileLogic tile = Board.GetTile(pos);
                    if (tile != null)
                    {
                        gappaiAreaTiles.Add(pos);
                    }
                }
            }
        }
    }

    private void FindAlliesIn4x4Area()
    {
        alliesInRange.Clear();
        if (currentUnit == null || battle == null) return;

        foreach (var u in battle.allUnits)
        {
            if (u != null && u != currentUnit && u.team == currentUnit.team && u.IsAlive && u.gameObject.activeInHierarchy && !u.IsTemporaryFusion)
            {
                if (gappaiAreaTiles.Contains(u.gridPosition))
                {
                    alliesInRange.Add(u);
                }
            }
        }

        // Ordena aliados por proximidade em relação ao monstro do turno atual
        Vector3Int origin = currentUnit.gridPosition;
        alliesInRange.Sort((a, b) =>
        {
            int distA = Mathf.Abs(a.gridPosition.x - origin.x) + Mathf.Abs(a.gridPosition.y - origin.y);
            int distB = Mathf.Abs(b.gridPosition.x - origin.x) + Mathf.Abs(b.gridPosition.y - origin.y);
            return distA.CompareTo(distB);
        });

        Debug.Log($"[AppGappaiState] Aliados encontrados no raio 4x4: {alliesInRange.Count}");
    }

    private void HandleGappaiClosed(bool didPerformAction)
    {
        if (didPerformAction)
        {
            // A fusão ou link consumiu a ação do turno
            Unit activeUnit = (battle != null && battle.currentUnit != null) ? battle.currentUnit : currentUnit;

            if (activeUnit != null)
            {
                activeUnit.hasActed = true;
            }

            // Transiciona para a seleção de facing para finalizar o turno
            machine.ChangeTo<SelectFacingState>();
        }
        else
        {
            // Cancelou sem realizar ação: retorna ao Menu de Ações
            machine.ChangeTo<ChooseActionState>();
        }
    }
}
