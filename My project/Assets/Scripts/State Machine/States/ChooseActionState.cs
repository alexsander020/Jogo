using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Windows;

public class ChooseActionState : State
{
    int index;
    public override void Enter()
    {
        base.Enter();
        index = 0;
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
    }

    void OnMove(object sender, object args) {

        Vector3Int button = (Vector3Int)args;
        if (button == Vector3Int.left) {
            index--;
            ChangeSelector();
        } else if (button == Vector3Int.right) {
            index++;
            ChangeSelector();
        }
    }

    void OnFire(object sender, object args) {

        int button = (int)args;

        if (button == 1)
        {
            ActionButton();
        }
        else if (button == 2)
        {
            machine.Change<RoamState>();
        }
    }

    void ChangeSelector() {
        if (index == -1) {
            index = machine.ChooseActionButton.Count - 1;
        } 
        else if (index == machine.ChooseActionButton.Count) {
            index = 0;
        }
        machine.ChooseActionSelection.transform.localPosition =
        machine.ChooseActionButton[index].transform.localPosition;
    }

    void ActionButton()
    {
        Debug.Log(index);

        switch (index)
        {
            case 0:
                //machine.ChangeTo<MoveTargetState>();
                break;
            case 1:
                //machine.ChangeTo<ActionSelectState>();
                break;
            case 2:
                //machine.ChangeTo<ItemSelectState>();
                break;
            case 3:
                //machine.ChangeTo<WaitState>();
                break;
        }
    }
}
