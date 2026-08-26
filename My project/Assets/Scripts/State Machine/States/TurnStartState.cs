using System.Collections;
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

        // Move o seletor para a unidade atual
        if (unit.currentTile != null)
        {
            machine.MoveSelectorTo(unit.currentTile);
        }

        // Aguarda breve delay para percepção do turno
        yield return new WaitForSeconds(0.3f);

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
        yield return new WaitForSeconds(0.8f);

        // Transiciona para o encerramento do turno do inimigo
        machine.ChangeTo<TurnEndState>();
    }
}
