using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StateMachineController : MonoBehaviour
{
    public static StateMachineController Instance;

    State _current;
    bool busy;

    public State current { get { return _current; } }

    public Transform selector;

    [Header("ChooseActionState UI")]
    public List<Image> ChooseActionButton;
    public Image chaooseActionSelected;
    public PanelPositioner ChooseActionPanel;

    [HideInInspector]
    public BattleController battleController;

    void Awake()
    {
        Instance = this;
        battleController = GetComponent<BattleController>();
        if (battleController == null)
        {
            battleController = gameObject.AddComponent<BattleController>();
        }

        if (ChooseActionPanel == null)
        {
            ChooseActionPanel = FindFirstObjectByType<PanelPositioner>();
        }

        // Inicializa o BattleHUD automaticamente se não existir na cena
        if (FindFirstObjectByType<BattleHUD>() == null)
        {
            gameObject.AddComponent<BattleHUD>();
        }
    }

    void Start()
    {
        ChangeTo<LoadState>();
    }

    public void ChangeTo<T>() where T : State
    {
        State state = GetState<T>();
        if (_current != state)
        {
            ChangeState(state);
        }
    }

    // Sobrecarga para compatibilidade
    public void Change<T>() where T : State
    {
        ChangeTo<T>();
    }

    public T GetState<T>() where T : State
    {
        T target = GetComponent<T>();
        if (target == null)
        {
            target = gameObject.AddComponent<T>();
        }
        return target;
    }

    protected void ChangeState(State value)
    {
        if (busy) return;

        busy = true;

        if (_current != null)
        {
            _current.Exit();
        }

        _current = value;

        if (_current != null)
        {
            _current.Enter();
        }

        busy = false;
    }

    // Move o seletor visual para uma coordenada de tile
    public void MoveSelectorTo(TileLogic tile)
    {
        if (tile == null || Selector.Instance == null) return;

        Selector.Instance.tile = tile;
        if (Selector.Instance.spriteRenderer != null)
        {
            Selector.Instance.spriteRenderer.sortingOrder = tile.contentOrder;
        }
        Selector.Instance.transform.position = tile.worldPos;
    }
}
