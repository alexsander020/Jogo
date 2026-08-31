using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItemTargetState : State
{
    public static ItemData SelectedItem;

    private HashSet<Vector3Int> validTiles = new HashSet<Vector3Int>();
    private List<Unit> alliesInRange = new List<Unit>();
    private int currentTargetIndex = 0;
    private Unit targetedAlly = null;
    private bool isUsingItem = false;

    public override void Enter()
    {
        base.Enter();
        isUsingItem = false;
        targetedAlly = null;
        currentTargetIndex = 0;
        alliesInRange.Clear();

        if (currentUnit == null || !currentUnit.CanAct() || SelectedItem == null)
        {
            Debug.LogWarning("[UseItemTargetState] Unidade não pode agir ou item inválido!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
            BattleHUD.Instance.ShowItemSelection(false);
        }

        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        CalculateItemRange();
        FindAllAlliesInRange();

        // Destaca os tiles alcançáveis no tabuleiro
        if (GridHighlighter.Instance != null && currentUnit.currentTile != null)
        {
            GridHighlighter.Instance.ShowMovementRange(validTiles, currentUnit.currentTile.pos);
        }

        // Se houver aliados ou a própria unidade no alcance, seleciona o primeiro
        if (alliesInRange.Count > 0)
        {
            // Tenta priorizar o aliado com menor HP proporcional
            alliesInRange.Sort((a, b) =>
            {
                int hpA = a.stats != null ? a.stats.GetStat(StatEnum.HP) : 100;
                int hpB = b.stats != null ? b.stats.GetStat(StatEnum.HP) : 100;
                return hpA.CompareTo(hpB);
            });

            currentTargetIndex = 0;
            SelectAllyByIndex(currentTargetIndex);
        }
        else
        {
            if (currentUnit.currentTile != null)
            {
                machine.MoveSelectorTo(currentUnit.currentTile);
            }

            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateControlsPrompt(
                    $"USAR ITEM: {SelectedItem.itemName.ToUpper()}",
                    $"• Nenhum aliado no alcance ({SelectedItem.maxRange} tiles).\n• Pressione [X / ESC] para voltar aos itens."
                );
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
        validTiles.Clear();
        alliesInRange.Clear();
        targetedAlly = null;

        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }
    }

    void CalculateItemRange()
    {
        validTiles.Clear();
        if (currentUnit == null || currentUnit.currentTile == null || SelectedItem == null) return;

        Vector3Int center = currentUnit.currentTile.pos;
        int maxRange = SelectedItem.maxRange;
        int minRange = SelectedItem.minRange;

        for (int dx = -maxRange; dx <= maxRange; dx++)
        {
            for (int dy = -maxRange; dy <= maxRange; dy++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist >= minRange && dist <= maxRange)
                {
                    Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);
                    TileLogic tile = Board.GetTile(pos);
                    if (tile != null)
                    {
                        validTiles.Add(pos);
                    }
                }
            }
        }
    }

    void FindAllAlliesInRange()
    {
        alliesInRange.Clear();
        if (currentUnit == null || battle == null) return;

        foreach (var u in battle.allUnits)
        {
            if (u != null && u.team == currentUnit.team && u.IsAlive && u.currentTile != null)
            {
                if (validTiles.Contains(u.gridPosition))
                {
                    alliesInRange.Add(u);
                }
            }
        }
    }

    void SelectAllyByIndex(int index)
    {
        if (alliesInRange.Count == 0) return;

        currentTargetIndex = (index + alliesInRange.Count) % alliesInRange.Count;
        targetedAlly = alliesInRange[currentTargetIndex];

        if (targetedAlly != null && targetedAlly.currentTile != null)
        {
            machine.MoveSelectorTo(targetedAlly.currentTile);

            int hp = targetedAlly.stats != null ? targetedAlly.stats.GetStat(StatEnum.HP) : 0;
            int maxHp = targetedAlly.stats != null ? targetedAlly.stats.GetStat(StatEnum.MaxHp) : 100;

            if (BattleHUD.Instance != null && SelectedItem != null)
            {
                string targetDesc = targetedAlly == currentUnit ? $"{targetedAlly.unitName} (Você)" : targetedAlly.unitName;
                BattleHUD.Instance.UpdateControlsPrompt(
                    $"USAR {SelectedItem.itemName.ToUpper()} EM {targetDesc.ToUpper()}",
                    $"• Alvo: {targetDesc} (HP: {hp}/{maxHp})\n• [W / A / S / D ou SETAS] : Alternar Alvo    [ESPAÇO / ENTER / Z] : Confirmar    [X / ESC] : Cancelar"
                );
            }
        }
    }

    void OnMove(object sender, object args)
    {
        if (isUsingItem || alliesInRange.Count <= 1) return;

        Vector3Int dir = (Vector3Int)args;
        if (dir.x != 0 || dir.y != 0)
        {
            if (dir.x > 0 || dir.y > 0)
            {
                SelectAllyByIndex(currentTargetIndex + 1);
            }
            else
            {
                SelectAllyByIndex(currentTargetIndex - 1);
            }
        }
    }

    void OnFire(object sender, object args)
    {
        if (isUsingItem) return;

        int button = (int)args;

        if (button == 1) // Confirmar uso
        {
            if (targetedAlly != null && SelectedItem != null && SelectedItem.quantity > 0)
            {
                StartCoroutine(UseItemRoutine());
            }
        }
        else if (button == 2) // Cancelar e voltar para tela de seleção de itens
        {
            machine.ChangeTo<SelectItemState>();
        }
    }

    IEnumerator UseItemRoutine()
    {
        isUsingItem = true;

        if (currentUnit != null && targetedAlly != null)
        {
            // Vira na direção do alvo
            FacingDirection dirToTarget = DirectionUtils.VectorToDirection(targetedAlly.gridPosition - currentUnit.gridPosition);
            currentUnit.SetFacing(dirToTarget);

            // Consome o item e cura o alvo
            InventoryService.ConsumeItem(SelectedItem, targetedAlly);
            currentUnit.hasActed = true;
        }

        yield return new WaitForSeconds(0.4f);

        isUsingItem = false;

        // Se a unidade ainda puder se mover neste turno, volta para ChooseActionState, senão vai para SelectFacingState
        if (currentUnit != null && currentUnit.CanMove())
        {
            machine.ChangeTo<ChooseActionState>();
        }
        else
        {
            machine.ChangeTo<SelectFacingState>();
        }
    }
}
