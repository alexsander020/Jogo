using System.Collections;
using UnityEngine;

public class TurnEndState : State
{
    public override void Enter()
    {
        base.Enter();
        StartCoroutine(ProcessTurnEnd());
    }

    IEnumerator ProcessTurnEnd()
    {
        yield return new WaitForSeconds(0.2f);
        battle.EndCurrentUnitTurn();
    }
}
