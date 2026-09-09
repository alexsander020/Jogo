using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppLinkState : State
{
    public override void Enter()
    {
        base.Enter();

        if (currentUnit == null)
        {
            Debug.LogWarning("[AppLinkState] Unidade atual inválida!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        if (!currentUnit.CanAct() && !currentUnit.IsLinked)
        {
            Debug.LogWarning("[AppLinkState] Unidade atual não pode realizar ações neste turno!");
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateControlsPrompt(
                    "AÇÃO JÁ REALIZADA",
                    "• A unidade atual já agiu neste turno."
                );
            }
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        // Esconde menu de ações principal temporariamente
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
            BattleHUD.Instance.UpdateControlsPrompt(
                "SISTEMA DE APP-LINK", 
                "• [W / A / S / D ou SETAS] : Navegar na Bag    [ESPAÇO / ENTER / Z] : Vincular    [U] : Desvincular    [X / ESC] : Voltar"
            );
        }

        // Garante o componente AppLinkMenuUI na cena
        if (AppLinkMenuUI.Instance == null)
        {
            GameObject uiObj = new GameObject("AppLinkMenuUI", typeof(AppLinkMenuUI));
            AppLinkMenuUI.Instance = uiObj.GetComponent<AppLinkMenuUI>();
        }

        AppLinkMenuUI.Instance.Open(currentUnit, HandleLinkClosed);
        Debug.Log($"[AppLinkState] Tela de App-Link aberta para {currentUnit.unitName}.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        if (AppLinkMenuUI.Instance != null)
        {
            AppLinkMenuUI.Instance.Hide();
        }
    }

    void OnMove(object sender, object args)
    {
        if (AppLinkMenuUI.Instance == null || !AppLinkMenuUI.Instance.IsOpen) return;
        Vector3Int dir = (Vector3Int)args;
        // Inverte o Y porque no grid Y+ é cima, mas na UI Y+ é descer linha
        AppLinkMenuUI.Instance.Navigate(dir.x, -dir.y);
    }

    void OnFire(object sender, object args)
    {
        if (AppLinkMenuUI.Instance == null || !AppLinkMenuUI.Instance.IsOpen) return;
        int button = (int)args;
        if (button == 1) // Confirmar / Espaço / Enter / Z
        {
            AppLinkMenuUI.Instance.ConfirmSelection();
        }
        else if (button == 2) // Cancelar / X / ESC
        {
            AppLinkMenuUI.Instance.CancelSelection();
        }
    }

    private void HandleLinkClosed(bool didPerformAction)
    {
        if (didPerformAction)
        {
            // A ação de vincular/desvincular consome a ação do turno
            if (currentUnit != null)
            {
                currentUnit.hasActed = true;
            }

            // Se a unidade já se moveu e agora agiu, vai para a seleção de facing de fim de turno
            if (currentUnit != null && currentUnit.hasMoved)
            {
                machine.ChangeTo<SelectFacingState>();
            }
            else
            {
                // Se ainda pode se mover, volta ao menu de ações
                machine.ChangeTo<ChooseActionState>();
            }
        }
        else
        {
            // Cancelou sem realizar ação
            machine.ChangeTo<ChooseActionState>();
        }
    }
}
