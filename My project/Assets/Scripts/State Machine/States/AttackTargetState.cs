using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTargetState : State
{
    private HashSet<Vector3Int> attackableTiles = new HashSet<Vector3Int>();
    private List<Unit> enemiesInRange = new List<Unit>();
    private int currentTargetIndex = 0;
    private Unit targetedEnemy = null;
    private bool isAttacking = false;
    private SkillData selectedSkill = null;

    public void SetSelectedSkill(SkillData skill)
    {
        selectedSkill = skill;
    }

    public override void Enter()
    {
        base.Enter();
        isAttacking = false;
        targetedEnemy = null;
        currentTargetIndex = 0;
        enemiesInRange.Clear();

        if (currentUnit == null || !currentUnit.CanAct())
        {
            Debug.LogWarning("[AttackTargetState] Unidade não pode agir ou já atacou neste turno!");
            machine.ChangeTo<ChooseActionState>();
            return;
        }

        if (selectedSkill == null)
        {
            var skills = currentUnit.GetSkills();
            if (skills != null && skills.Count > 0)
            {
                selectedSkill = skills[0];
            }
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowActionMenu(false);
            BattleHUD.Instance.ShowSkillSelection(false);
        }

        inputs.OnMove += OnMove;
        inputs.OnFire += OnFire;

        CalculateAttackRange();
        FindAllEnemiesInRange();

        // 1. Renderiza os destaques em Laranja / Âmbar Incandescente no tabuleiro
        if (GridHighlighter.Instance != null && currentUnit.currentTile != null)
        {
            GridHighlighter.Instance.ShowAttackRange(attackableTiles, currentUnit.currentTile.pos);
        }

        // 2. Se houver inimigos no alcance, seleciona o primeiro alvo automaticamente
        if (enemiesInRange.Count > 0)
        {
            currentTargetIndex = 0;
            SelectEnemyByIndex(currentTargetIndex);
        }
        else
        {
            // Nenhum inimigo no alcance: posiciona o seletor na unidade atual
            if (currentUnit.currentTile != null)
            {
                machine.MoveSelectorTo(currentUnit.currentTile);
            }

            int range = selectedSkill != null ? selectedSkill.maxRange : (currentUnit != null ? currentUnit.attackRange : 2);
            string skillName = selectedSkill != null ? selectedSkill.skillName : "Ataque";
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateControlsPrompt(
                    "NENHUM INIMIGO NO ALCANCE",
                    $"• Não há inimigos no alcance de {skillName} ({range} tiles).\n• Pressione [X / ESC] para voltar e selecionar outra habilidade ou mover-se."
                );
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        inputs.OnMove -= OnMove;
        inputs.OnFire -= OnFire;
        attackableTiles.Clear();
        enemiesInRange.Clear();
        targetedEnemy = null;
        selectedSkill = null;

        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowCombatForecastBanner(false);
        }
    }

    void CalculateAttackRange()
    {
        attackableTiles.Clear();
        if (currentUnit == null || currentUnit.currentTile == null) return;

        Vector3Int origin = currentUnit.currentTile.pos;
        int minRange = selectedSkill != null ? selectedSkill.minRange : 1;
        int maxRange = selectedSkill != null ? selectedSkill.maxRange : (currentUnit.attackRange > 0 ? currentUnit.attackRange : 2);

        // Bônus de elevação de alcance (+1 tile se estiver no alto)
        int unitZ = currentUnit.currentTile.floor != null && Board.instance != null
            ? Board.instance.floors.IndexOf(currentUnit.currentTile.floor)
            : 0;
        if (unitZ > 0) maxRange += 1;

        // Calcula todos os tiles no raio de distância Manhattan entre minRange e maxRange
        for (int dx = -maxRange; dx <= maxRange; dx++)
        {
            for (int dy = -maxRange; dy <= maxRange; dy++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist >= minRange && dist <= maxRange)
                {
                    Vector3Int targetPos = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                    TileLogic tile = Board.GetTile(targetPos);
                    if (tile != null)
                    {
                        attackableTiles.Add(targetPos);
                    }
                }
            }
        }
    }

    void FindAllEnemiesInRange()
    {
        enemiesInRange.Clear();
        if (currentUnit == null || battle == null) return;

        foreach (var u in battle.allUnits)
        {
            if (u != null && u.team != currentUnit.team && u.IsAlive && u.currentTile != null)
            {
                if (attackableTiles.Contains(u.gridPosition))
                {
                    enemiesInRange.Add(u);
                }
            }
        }

        // Ordena inimigos: mais próximos primeiro ou da esquerda para direita
        Vector3Int origin = currentUnit.gridPosition;
        enemiesInRange.Sort((a, b) =>
        {
            int distA = Mathf.Abs(a.gridPosition.x - origin.x) + Mathf.Abs(a.gridPosition.y - origin.y);
            int distB = Mathf.Abs(b.gridPosition.x - origin.x) + Mathf.Abs(b.gridPosition.y - origin.y);
            if (distA != distB) return distA.CompareTo(distB);
            return a.gridPosition.x.CompareTo(b.gridPosition.x);
        });

        Debug.Log($"[AttackTargetState] Inimigos encontrados no alcance: {enemiesInRange.Count}");
    }

    void SelectEnemyByIndex(int index)
    {
        if (enemiesInRange.Count == 0) return;

        currentTargetIndex = Mathf.Clamp(index, 0, enemiesInRange.Count - 1);
        targetedEnemy = enemiesInRange[currentTargetIndex];

        if (targetedEnemy.currentTile != null)
        {
            machine.MoveSelectorTo(targetedEnemy.currentTile);
        }

        // Calcula a previsão completa de combate usando a habilidade selecionada
        CombatForecast forecast = CombatService.CalculateForecast(currentUnit, targetedEnemy, selectedSkill);

        // Exibe o Banner Elegante de Previsão de Combate (Estilo Digimon Survive)
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowCombatForecastBanner(true, forecast);

            string targetIndexHeader = enemiesInRange.Count > 1 
                ? $"PREVISÃO: {selectedSkill?.skillName?.ToUpper() ?? "ATAQUE"} [ALVO {currentTargetIndex + 1}/{enemiesInRange.Count}]" 
                : $"PREVISÃO: {selectedSkill?.skillName?.ToUpper() ?? "ATAQUE"}";

            string promptControls = "• [ESPAÇO / ENTER / Z] : Executar Ataque    [X / ESC] : Cancelar";
            if (enemiesInRange.Count > 1)
            {
                promptControls = "• [SETAS / A-D] : Alternar Alvos    " + promptControls;
            }

            BattleHUD.Instance.UpdateControlsPrompt(targetIndexHeader, promptControls);
        }
    }

    void OnMove(object sender, object args)
    {
        if (isAttacking) return;

        Vector3Int button = (Vector3Int)args;

        if (enemiesInRange.Count > 1)
        {
            // Alterna facilmente entre todos os alvos disponíveis no alcance
            if (button == Vector3Int.right || button == Vector3Int.up)
            {
                currentTargetIndex = (currentTargetIndex + 1) % enemiesInRange.Count;
                SelectEnemyByIndex(currentTargetIndex);
                return;
            }
            else if (button == Vector3Int.left || button == Vector3Int.down)
            {
                currentTargetIndex = (currentTargetIndex - 1 + enemiesInRange.Count) % enemiesInRange.Count;
                SelectEnemyByIndex(currentTargetIndex);
                return;
            }
        }
        else if (enemiesInRange.Count == 1)
        {
            // Se houver 1 inimigo, garante que o seletor está nele
            SelectEnemyByIndex(0);
        }
        else
        {
            // Navegação livre no grid caso não haja alvos diretos
            if (machine.selector != null)
            {
                Vector3 currentPos = machine.selector.position;
                Vector3Int currentGridPos = new Vector3Int(Mathf.RoundToInt(currentPos.x), Mathf.RoundToInt(currentPos.y), Mathf.RoundToInt(currentPos.z));
                Vector3Int nextPos = currentGridPos + button;
                TileLogic nextTile = Board.GetTile(nextPos);

                if (nextTile != null)
                {
                    machine.MoveSelectorTo(nextTile);
                }
            }
        }
    }

    void OnFire(object sender, object args)
    {
        if (isAttacking) return;

        int button = (int)args;

        if (button == 1) // Confirmar (Espaço / Enter / Z)
        {
            if (targetedEnemy != null && targetedEnemy.IsAlive)
            {
                StartCoroutine(ExecuteAttackRoutine(targetedEnemy));
            }
            else
            {
                Debug.LogWarning("[AttackTargetState] Nenhum alvo inimigo válido selecionado!");
            }
        }
        else if (button == 2) // Cancelar (X / Esc)
        {
            // Retorna para a tela de Seleção de Habilidades
            machine.ChangeTo<SelectSkillState>();
        }
    }

    IEnumerator ExecuteAttackRoutine(Unit target)
    {
        isAttacking = true;

        string skillName = selectedSkill != null ? selectedSkill.skillName : "Ataque";
        Debug.Log($"[AttackTargetState] {currentUnit.unitName} executando {skillName} contra {target.unitName}!");

        // 1. Atacante vira para encarar o alvo
        Vector3Int diff = target.gridPosition - currentUnit.gridPosition;
        if (diff != Vector3Int.zero)
        {
            currentUnit.SetFacing(DirectionUtils.VectorToDirection(diff));
        }

        // 2. Calcula a resolução de combate com a habilidade selecionada
        CombatForecast forecast = CombatService.CalculateForecast(currentUnit, target, selectedSkill);

        // Registra o ataque para permitir Combo Sincronizado se um parceiro atacar depois
        FunctionalCategory atkCat = selectedSkill != null ? selectedSkill.category : currentUnit.category;
        CombatService.RegisterAttackOnTarget(target, atkCat);

        // 3. Deduz o custo de SP da habilidade
        if (selectedSkill != null && selectedSkill.spCost > 0 && currentUnit.stats != null)
        {
            int currentSp = currentUnit.stats.GetStat(StatEnum.SP);
            int newSp = Mathf.Max(0, currentSp - selectedSkill.spCost);
            currentUnit.stats.SetStat(StatEnum.SP, newSp);
            Debug.Log($"[Combate] {currentUnit.unitName} gastou {selectedSkill.spCost} SP. SP Restante: {newSp}");
        }

        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.ShowCombatForecastBanner(false);
            BattleHUD.Instance.UpdateControlsPrompt(
                "EXECUTANDO ATAQUE!",
                $"• {currentUnit.unitName} usou {skillName} em {target.unitName}!"
            );
        }

        // 4. Animação de ataque com lunge dinâmico
        yield return StartCoroutine(currentUnit.PlayAttackAnimation(target.transform.position));

        // 5. Aplica o dano no defensor
        target.TakeDamage(forecast.finalDamage, forecast.orientation, forecast.isCritical, forecast.hasCategoryAdvantage);
        CombatService.ApplyCombatEffects(currentUnit, target, selectedSkill, forecast.finalDamage);

        // Atualiza o banner do turno e stats no HUD
        if (BattleHUD.Instance != null)
        {
            BattleHUD.Instance.UpdateTurnBanner(currentUnit);
        }

        yield return new WaitForSeconds(0.45f);

        // 5. Marca a ação da unidade como realizada
        currentUnit.hasActed = true;
        isAttacking = false;

        // 6. Verifica condição de vitória / fim de combate
        if (battle != null && battle.CheckBattleEnd(out Team winner))
        {
            Debug.Log($"[NetShift Battle] Combate encerrado! Vencedor: {winner}");
            yield break;
        }

        // 7. Se a unidade ainda pode se mover, volta ao menu de ações; senão, escolhe a direção de término
        if (currentUnit.CanMove())
        {
            machine.ChangeTo<ChooseActionState>();
        }
        else
        {
            machine.ChangeTo<SelectFacingState>();
        }
    }
}
