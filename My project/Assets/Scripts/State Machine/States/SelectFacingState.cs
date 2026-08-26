using UnityEngine;

public class SelectFacingState : State
{
    public override void Enter()
    {
        base.Enter();
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateControlsPrompt(
                "ESCOLHER DIREÇÃO", 
                "• [▲ / ▼ / ◄ / ► ou W / A / S / D] : Girar Unidade (Norte/Sul/Leste/Oeste)\n• [ESPAÇO / ENTER / Z] : Confirmar Direção e Finalizar Turno\n• [X / ESC] : Cancelar / Voltar ao Menu"
            );
        }

        Debug.Log("[SelectFacingState] Escolha a direção para qual a unidade deve olhar.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
    }

    void OnMove(object sender, object args)
    {
        if (currentUnit == null) return;

        Vector3Int dir = (Vector3Int)args;
        if (dir != Vector3Int.zero)
        {
            FacingDirection newFacing = DirectionUtils.VectorToDirection(dir);
            currentUnit.SetFacing(newFacing);
            Debug.Log($"[SelectFacingState] Unidade {currentUnit.unitName} virada para: {newFacing}");
        }
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1)
        {
            // Confirmou a direção de olhar
            machine.ChangeTo<TurnEndState>();
        }
        else if (button == 2)
        {
            // Cancelar e voltar ao menu
            machine.ChangeTo<ChooseActionState>();
        }
    }
}
