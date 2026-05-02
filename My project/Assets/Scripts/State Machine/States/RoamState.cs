using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoamState : State
{
    public override void Enter()
    {
        base.Enter();
        InputController.Instance.OnMove += OnMove;
        InputController.Instance.OnFire += OnFire;
        CheckNullPosition();
    }

    public override void Exit()
    {
        base.Exit();
        InputController.Instance.OnMove -= OnMove;
        InputController.Instance.OnFire -= OnFire;
    }

    void OnMove(object sender, object args)
    {
        Vector3Int input = (Vector3Int)args;

        TileLogic t = Board.GetTile(Selector.Instance.position + input);

        if (t != null)
        {
            Selector.Instance.tile = t;
            Selector.Instance.spriteRenderer.sortingOrder = t.contentOrder;
            Selector.Instance.transform.position = t.worldPos;
        }
    }
    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1)
        {

        }
        else if (button == 2)
        {
            StateMachineController.Instance.Change<ChooseActionState>();
        }
    }
    void CheckNullPosition()
    {
     if (Selector.Instance.tile == null)
      {
        TileLogic t = Board.GetTile(new Vector3Int(0, 0, 0));
        Selector.Instance.tile = t;
        Selector.Instance.spriteRenderer.sortingOrder = t.contentOrder;
        Selector.Instance.transform.position = t.worldPos;
      }
    }
    
}