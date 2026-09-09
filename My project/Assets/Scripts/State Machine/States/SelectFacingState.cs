using UnityEngine;

public class SelectFacingState : State
{
    public override void Enter()
    {
        base.Enter();
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        if (currentUnit != null)
        {
            currentUnit.SetFacing(currentUnit.facing);
            DefenseVfxService.ShowDefensePreview(currentUnit, currentUnit.facing);
        }

        UpdateHUD();
        Debug.Log("[SelectFacingState] Escolha a direção de término do turno com postura defensiva.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        // Se saiu sem confirmar a postura de defesa, limpa o preview
        if (currentUnit != null && !currentUnit.isDefending)
        {
            DefenseVfxService.ClearDefenseVfx(currentUnit);
        }
    }

    void OnMove(object sender, object args)
    {
        if (currentUnit == null) return;

        Vector3Int dir = (Vector3Int)args;
        if (dir != Vector3Int.zero)
        {
            FacingDirection newFacing = DirectionUtils.VectorToDirection(dir);
            currentUnit.SetFacing(newFacing);
            DefenseVfxService.UpdateDefenseDirection(currentUnit, newFacing);
            UpdateHUD();
            Debug.Log($"[SelectFacingState] Unidade {currentUnit.unitName} virada para: {DirectionUtils.GetDirectionName(newFacing)}");
        }
    }

    void UpdateHUD()
    {
        if (BattleHUD.Instance == null || currentUnit == null) return;

        string facingName = DirectionUtils.GetDirectionName(currentUnit.facing);
        BattleHUD.Instance.UpdateControlsPrompt(
            $"ESCOLHER LADO DE DEFESA — ATUAL: {facingName}", 
            "• [W / A / S / D ou SETAS] : Escolher Lado a Defender (Norte/Sul/Leste/Oeste)\n• [ESPAÇO / ENTER / Z] : Confirmar Defesa e Encerrar Turno\n• [X / ESC] : Cancelar / Voltar ao Menu"
        );
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1)
        {
            // Confirmou a direção e finaliza o turno com defesa ativada
            if (currentUnit != null)
            {
                currentUnit.SetDefenseStance(currentUnit.facing);
                DefenseVfxService.ConfirmDefenseStance(currentUnit);
            }
            machine.ChangeTo<TurnEndState>();
        }
        else if (button == 2)
        {
            // Cancelar e voltar ao menu
            if (currentUnit != null)
            {
                DefenseVfxService.ClearDefenseVfx(currentUnit);
            }
            machine.ChangeTo<ChooseActionState>();
        }
    }
}

