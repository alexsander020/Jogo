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
        UpdateHUDInfo();
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
            if (TacticalCameraController.Instance != null && Selector.Instance != null)
            {
                TacticalCameraController.Instance.FocusOn(Selector.Instance.transform);
            }
            UpdateHUDInfo();
        }
    }

    void UpdateHUDInfo()
    {
        if (BattleHUD.Instance == null) return;

        string extraInfo = "";
        if (Selector.Instance != null && Selector.Instance.tile != null)
        {
            var tile = Selector.Instance.tile;
            var terrain = tile.Terrain;
            extraInfo = $"\n• <color=#00E5FF><b>Terreno: {terrain.displayName}</b></color> ({terrain.GetSummary()})";

            if (tile.content != null)
            {
                var u = tile.content.GetComponent<Unit>();
                if (u != null)
                {
                    string teamColor = u.team == Team.Player ? "#55FF55" : "#FF5555";
                    extraInfo += $"\n• <color={teamColor}><b>{u.unitName} [{u.team}]</b></color> (HP: {u.stats.GetStat(StatEnum.HP)}/{u.stats.GetStat(StatEnum.MaxHp)})";
                }
            }
        }

        BattleHUD.Instance.UpdateControlsPrompt(
            "NAVEGAÇÃO LIVRE (GRID)", 
            $"• [W/A/S/D ou SETAS] : Mover Cursor pelo Tabuleiro{extraInfo}\n• [ESPAÇO / ENTER] : Inspecionar / Abrir Menu\n• [X / ESC] : Retornar à Unidade Ativa"
        );
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