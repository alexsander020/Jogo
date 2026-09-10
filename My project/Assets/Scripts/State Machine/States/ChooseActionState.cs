using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseActionState : State
{
    int index;

    public override void Enter()
    {
        base.Enter();
        index = 0;
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(true);
            BattleHUD.Instance.UpdateActionMenuSelection(index, currentUnit);
            BattleHUD.Instance.UpdateTurnBanner(currentUnit);
            BattleHUD.Instance.UpdateControlsPrompt(
                "MENU DE AÇÕES", 
                "• [W / S ou SETAS] : Navegar Opções    [ESPAÇO / ENTER / Z] : Confirmar    [X / ESC] : Modo Livre"
            );
        }

        ChangeSelector();
        Debug.Log($"[ChooseActionState] Menu de Ações aberto para {currentUnit?.unitName}.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
        }
    }

    void OnMove(object sender, object args)
    {
        Vector3Int button = (Vector3Int)args;
        if (button == Vector3Int.up || button == Vector3Int.left)
        {
            NavigateOptions(-1);
        }
        else if (button == Vector3Int.down || button == Vector3Int.right)
        {
            NavigateOptions(1);
        }
    }

    void NavigateOptions(int direction)
    {
        int newIndex = index;
        for (int step = 0; step < 6; step++)
        {
            newIndex += direction;
            if (newIndex < 0) newIndex = 5;
            else if (newIndex > 5) newIndex = 0;

            // Pula a opção de Link (4) se a unidade já estiver vinculada
            if (newIndex == 4 && currentUnit != null && currentUnit.IsLinked)
            {
                continue;
            }
            break;
        }
        index = newIndex;
        ChangeSelector();
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1)
        {
            ActionButton();
        }
        else if (button == 2)
        {
            // Voltar para navegação livre no grid
            machine.ChangeTo<RoamState>();
        }
    }

    void ChangeSelector()
    {
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateActionMenuSelection(index, currentUnit);
        }

        if (machine.ChooseActionButton != null && machine.ChooseActionButton.Count > 0)
        {
            if (index >= 0 && index < machine.ChooseActionButton.Count && machine.chaooseActionSelected != null)
            {
                machine.chaooseActionSelected.transform.position = machine.ChooseActionButton[index].transform.position;
            }
        }
    }

    public void SelectAndExecute(int actionIndex)
    {
        // Se a unidade já estiver vinculada, a opção 4 de Link não pode ser ativada
        if (actionIndex == 4 && currentUnit != null && currentUnit.IsLinked)
        {
            Debug.LogWarning("[ChooseActionState] Ação de Link ignorada: a unidade já possui um App-Link ativo.");
            return;
        }

        index = Mathf.Clamp(actionIndex, 0, 5);
        ChangeSelector();
        ActionButton();
    }

    public void ActionButton()
    {
        Debug.Log($"[ChooseActionState] Ação selecionada: {index}");

        switch (index)
        {
            case 0: // Mover
                if (currentUnit != null && currentUnit.CanMove())
                {
                    Debug.Log("[Ação] Opção Mover selecionada.");
                    machine.ChangeTo<MoveTargetState>();
                }
                else
                {
                    Debug.LogWarning("[Ação] Esta unidade já realizou seu movimento neste turno!");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "MOVIMENTO JÁ REALIZADO", 
                            "• A unidade atual já se moveu neste turno. Escolha Atacar, Item, Evolução ou Encerrar."
                        );
                    }
                }
                break;

            case 1: // Atacar
                if (currentUnit != null && currentUnit.CanAct())
                {
                    Debug.Log("[Ação] Opção Atacar selecionada. Abrindo seleção de habilidades...");
                    machine.ChangeTo<SelectSkillState>();
                }
                else
                {
                    Debug.LogWarning("[Ação] Esta unidade já realizou sua ação/ataque neste turno!");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "AÇÃO JÁ REALIZADA", 
                            "• A unidade atual já atacou neste turno. Escolha Mover ou Encerrar Turno."
                        );
                    }
                }
                break;

            case 2: // Item
                if (currentUnit != null && currentUnit.CanAct())
                {
                    Debug.Log("[Ação] Opção Item selecionada. Abrindo inventário de itens...");
                    machine.ChangeTo<SelectItemState>();
                }
                else
                {
                    Debug.LogWarning("[Ação] Esta unidade já realizou sua ação neste turno!");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "AÇÃO JÁ REALIZADA", 
                            "• A unidade atual já agiu neste turno. Escolha Mover ou Encerrar Turno."
                        );
                    }
                }
                break;

            case 3: // Evolução
                Debug.Log("[Ação] Opção Evolução / NetFusion selecionada.");
                break;

            case 4: // Link (Applink / Conexão com reserva)
                if (currentUnit != null && currentUnit.IsLinked)
                {
                    Debug.LogWarning("[Ação] Esta unidade já possui um App-Link ativo!");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "LINK JÁ ATIVO", 
                            "• Esta unidade já realizou um App-Link e não pode linkar novamente."
                        );
                    }
                }
                else if (currentUnit != null && currentUnit.CanAct())
                {
                    Debug.Log("[Ação] Opção Link selecionada. Abrindo Sistema de App-Link...");
                    machine.ChangeTo<AppLinkState>();
                }
                else
                {
                    Debug.LogWarning("[Ação] Esta unidade já realizou sua ação neste turno!");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "AÇÃO JÁ REALIZADA", 
                            "• A unidade atual já agiu neste turno. Escolha Mover ou Encerrar Turno."
                        );
                    }
                }
                break;

            case 5: // Encerrar Turno
                Debug.Log("[Ação] Opção Encerrar Turno selecionada. Escolha a direção de término...");
                machine.ChangeTo<SelectFacingState>();
                break;
        }
    }
}
