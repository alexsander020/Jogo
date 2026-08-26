using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoamState : State
{
    public override void Enter()
    {
        base.Enter();
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;
        CheckNullPosition();

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateControlsPrompt(
                "NAVEGAÇÃO LIVRE (GRID)", 
                "• [W / A / S / D ou SETAS] : Mover Cursor pelo Tabuleiro\n• [ESPAÇO / ENTER] : Inspecionar / Abrir Menu da Unidade\n• [X / ESC] : Retornar à Unidade Ativa e Abrir Menu"
            );
        }
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
    }

    void OnMove(object sender, object args)
    {
        Vector3Int input = (Vector3Int)args;
        TileLogic t = Board.GetTile(Selector.Instance.position + input);

        if (t != null)
        {
            machine.MoveSelectorTo(t);
        }
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1)
        {
            // Se clicou na unidade ativa do jogador, abre o menu de ações
            if (Selector.Instance.tile != null && currentUnit != null && Selector.Instance.tile == currentUnit.currentTile)
            {
                machine.ChangeTo<ChooseActionState>();
            }
            else if (Selector.Instance.tile != null && Selector.Instance.tile.content != null)
            {
                Unit inspected = Selector.Instance.tile.content.GetComponent<Unit>();
                if (inspected != null)
                {
                    Debug.Log($"[Inspeção] {inspected.unitName} | Time: {inspected.team} | Categoria: {inspected.category} | Protocolo: {inspected.protocol} | HP: {inspected.stats.GetStat(StatEnum.HP)}/{inspected.stats.GetStat(StatEnum.MaxHp)} | SP: {inspected.stats.GetStat(StatEnum.SP)}/{inspected.stats.GetStat(StatEnum.MaxSp)}");
                }
            }
        }
        else if (button == 2)
        {
            // Retorna o seletor para a unidade atual e abre o menu
            if (currentUnit != null && currentUnit.currentTile != null)
            {
                machine.MoveSelectorTo(currentUnit.currentTile);
            }
            machine.ChangeTo<ChooseActionState>();
        }
    }

    void CheckNullPosition()
    {
        if (Selector.Instance != null && Selector.Instance.tile == null)
        {
            TileLogic t = currentUnit != null && currentUnit.currentTile != null 
                ? currentUnit.currentTile 
                : Board.GetTile(Vector3Int.zero);

            if (t != null)
            {
                machine.MoveSelectorTo(t);
            }
        }
    }
}