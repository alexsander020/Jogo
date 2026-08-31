using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectSkillState : State
{
    private int selectedSkillIndex = 0;
    private List<SkillData> unitSkills = new List<SkillData>();

    public override void Enter()
    {
        base.Enter();
        selectedSkillIndex = 0;

        if (currentUnit == null || !currentUnit.CanAct())
        {
            Debug.LogWarning("[SelectSkillState] Unidade não pode agir ou já atacou neste turno!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        unitSkills = currentUnit.GetSkills();
        if (unitSkills == null || unitSkills.Count == 0)
        {
            unitSkills = new List<SkillData>
            {
                SkillData.CreateBasicAttack("Atacar", 85, currentUnit.attackRange, currentUnit.category)
            };
        }

        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
            BattleHUD.Instance.ShowSkillSelection(true, currentUnit, selectedSkillIndex);
            BattleHUD.Instance.UpdateControlsPrompt(
                "SELEÇÃO DE HABILIDADE",
                "• [W / S ou SETAS] : Selecionar Habilidade    [ESPAÇO / ENTER / Z] : Mirar Alvo    [X / ESC] : Voltar ao Menu"
            );
        }

        Debug.Log($"[SelectSkillState] Aberta seleção de habilidades para {currentUnit.unitName}. Habilidades disponíveis: {unitSkills.Count}");
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowSkillSelection(false);
        }
    }

    void OnMove(object sender, object args)
    {
        if (unitSkills.Count <= 1) return;

        Vector3Int button = (Vector3Int)args;
        if (button == Vector3Int.up || button == Vector3Int.left)
        {
            selectedSkillIndex--;
            if (selectedSkillIndex < 0) selectedSkillIndex = unitSkills.Count - 1;
            UpdateSelection();
        }
        else if (button == Vector3Int.down || button == Vector3Int.right)
        {
            selectedSkillIndex++;
            if (selectedSkillIndex >= unitSkills.Count) selectedSkillIndex = 0;
            UpdateSelection();
        }
    }

    void UpdateSelection()
    {
        if (BattleHUD.Instance != null && currentUnit != null)
        {
            BattleHUD.Instance.UpdateSkillSelectionUI(currentUnit, selectedSkillIndex);
        }
    }

    void OnFire(object sender, object args)
    {
        int button = (int)args;

        if (button == 1) // Confirmar (Espaço / Enter / Z)
        {
            if (selectedSkillIndex >= 0 && selectedSkillIndex < unitSkills.Count)
            {
                SkillData skill = unitSkills[selectedSkillIndex];
                int currentSp = currentUnit.stats != null ? currentUnit.stats.GetStat(StatEnum.SP) : 50;

                if (currentSp < skill.spCost)
                {
                    Debug.LogWarning($"[SelectSkillState] SP insuficiente para {skill.skillName}! (Atual: {currentSp}, Necessário: {skill.spCost})");
                    if (BattleHUD.Instance != null)
                    {
                        BattleHUD.Instance.UpdateControlsPrompt(
                            "SP INSUFICIENTE",
                            $"• {currentUnit.unitName} possui {currentSp} SP, mas {skill.skillName} requer {skill.spCost} SP."
                        );
                    }
                    return;
                }

                Debug.Log($"[SelectSkillState] Habilidade confirmada: {skill.skillName} (Poder: {skill.effectPower}, Custo: {skill.spCost} SP). Indo para mira...");
                
                var attackState = machine.GetState<AttackTargetState>();
                if (attackState != null)
                {
                    attackState.SetSelectedSkill(skill);
                }

                machine.ChangeTo<AttackTargetState>();
            }
        }
        else if (button == 2) // Cancelar (X / Esc)
        {
            Debug.Log("[SelectSkillState] Cancelado. Retornando ao menu de ações.");
            machine.ChangeTo<ChooseActionState>();
        }
    }
}
