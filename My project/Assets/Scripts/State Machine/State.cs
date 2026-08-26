using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
    protected InputController inputs => InputController.Instance;
    protected StateMachineController machine => StateMachineController.Instance;
    protected BattleController battle => BattleController.Instance;
    protected Unit currentUnit => battle != null ? battle.currentUnit : null;

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }
}