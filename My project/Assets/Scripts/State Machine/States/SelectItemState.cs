using System.Collections.Generic;
using UnityEngine;

public class SelectItemState : State
{
    private int selectedIndex = 0;
    private List<ItemData> items;

    public override void Enter()
    {
        base.Enter();
        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        items = InventoryService.GetInventory();
        selectedIndex = 0;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowItemSelection(true, selectedIndex);
            BattleHUD.Instance.UpdateControlsPrompt(
                "SELEÇÃO DE ITENS",
                "• [W / S ou SETAS] : Selecionar Item    [ESPAÇO / ENTER / Z] : Mirar / Usar    [X / ESC] : Voltar ao Menu"
            );
        }

        Debug.Log("[SelectItemState] Tela de seleção de itens aberta.");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowItemSelection(false);
        }
    }

    void OnMove(object sender, object args)
    {
        if (items == null || items.Count == 0) return;

        Vector3Int dir = (Vector3Int)args;
        if (dir.y > 0)
        {
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            if (BattleHUD.Instance != null) BattleHUD.Instance.UpdateItemSelectionUI(selectedIndex);
        }
        else if (dir.y < 0)
        {
            selectedIndex = Mathf.Min(items.Count - 1, selectedIndex + 1);
            if (BattleHUD.Instance != null) BattleHUD.Instance.UpdateItemSelectionUI(selectedIndex);
        }
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1) // Confirmar
        {
            if (items != null && selectedIndex >= 0 && selectedIndex < items.Count)
            {
                ItemData selectedItem = items[selectedIndex];
                if (selectedItem.quantity > 0)
                {
                    UseItemTargetState.SelectedItem = selectedItem;
                    machine.ChangeTo<UseItemTargetState>();
                }
                else
                {
                    Debug.LogWarning($"[SelectItemState] Item {selectedItem.itemName} está esgotado (0 unidades)!");
                }
            }
        }
        else if (button == 2) // Cancelar
        {
            machine.ChangeTo<ChooseActionState>();
        }
    }
}
