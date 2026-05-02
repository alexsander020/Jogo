using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class StateMachineController : MonoBehaviour
{
  public static StateMachineController Instance;

    State _current;
    bool busy;

    public State current { get { return _current; } }

    public Transform selector;
    [Header("ChooseActionState")]

    public List<Image> ChooseActionButton;
    internal object ChooseActionSelection;

    public Image chaooseActionSelected;
    public PanelPositioner ChooseActionPanel;
    

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
       ChangeTo<LoadState>();
    }

    public void ChangeTo<T>() where T : State
    {
       State state = GetState<T>();

        if (_current != state) { ChangeState(state); }
    }

    public  T GetState <T>() where T : State
    {
        T target = GetComponent<T>();

        if (target == null)
        {
            target = gameObject.AddComponent<T>();
        }
        return target;
    }

    protected void ChangeState(State vale)
    {
       
        if (busy) { return; }

        busy = true;

        if (_current != null) {  _current.Exit(); }

        _current = vale;

        if (_current != null){  _current.Enter(); }
        busy = false;
    }

    internal void Change<T>()
    {
        throw new NotImplementedException();
    }
}
