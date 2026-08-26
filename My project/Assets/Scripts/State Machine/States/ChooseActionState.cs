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
        ChangeSelector();
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        if (machine.ChooseActionPanel != null)
        {
            machine.ChooseActionPanel.MoveTo("Show");
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateControlsPrompt(
                "MENU DE AÇÕES", 
                "• [◄ / ► ou A / D] : Navegar Opções (Mover, Atacar, Deck, Esperar)\n• [ESPAÇO / ENTER / Z] : Confirmar Ação\n• [X / ESC] : Navegação Livre no Grid"
            );
        }

        Debug.Log($"[ChooseActionState] Menu de Ações aberto para {currentUnit?.unitName}.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        if (machine.ChooseActionPanel != null)
        {
            machine.ChooseActionPanel.MoveTo("Hide");
        }
    }

    void OnMove(object sender, object args)
    {
        Vector3Int button = (Vector3Int)args;
        if (button == Vector3Int.left || button == Vector3Int.up)
        {
            index--;
            ChangeSelector();
        }
        else if (button == Vector3Int.right || button == Vector3Int.down)
        {
            index++;
            ChangeSelector();
        }
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
            // Voltar para navegação livre
            machine.ChangeTo<RoamState>();
        }
    }

    void ChangeSelector()
    {
        if (machine.ChooseActionButton == null || machine.ChooseActionButton.Count == 0) return;

        if (index < 0)
        {
            index = machine.ChooseActionButton.Count - 1;
        }
        else if (index >= machine.ChooseActionButton.Count)
        {
            index = 0;
        }

        if (machine.chaooseActionSelected != null && machine.ChooseActionButton.Count > index)
        {
            machine.chaooseActionSelected.transform.position = machine.ChooseActionButton[index].transform.position;
        }
    }

    void ActionButton()
    {
        Debug.Log($"[ChooseActionState] Ação selecionada: {index}");

        switch (index)
        {
            case 0:
                Debug.Log("[Ação] Opção Mover selecionada.");
                break;
            case 1:
                Debug.Log("[Ação] Opção Atacar selecionada.");
                break;
            case 2:
                Debug.Log("[Ação] Opção NetFusion / Deck selecionada.");
                break;
            case 3:
                Debug.Log("[Ação] Opção Esperar selecionada. Escolha a direção...");
                machine.ChangeTo<SelectFacingState>();
                break;
        }
    }
}
