using System;
using System.Collections.Generic;
using TacticalBattle.Attribute;
using TacticalBattle.Combat;
using TacticalBattle.Core;
using TacticalBattle.Core.EventSystem;
using TacticalBattle.Evolution;
using TacticalBattle.Grid;
using TacticalBattle.Talk;
using UnityEngine;

namespace TacticalBattle.Integration
{
    public class TacticalBattleController
    {
        public BattleState State { get; private set; }
        public KarmaModule KarmaModule { get; private set; }
        public TurnActionBudget CurrentBudget { get; private set; }

        public event Action<UnitState> OnTurnStarted;
        public event Action<UnitState, int, DamageResolution> OnAttackResolved;
        public event Action<UnitState, TalkResult> OnTalkResolved;
        public event Action<UnitState> OnTurnEnded;

        public TacticalBattleController(BattleState initialState = null)
        {
            State = initialState ?? new BattleState();
            KarmaModule = new KarmaModule(State.karma);
        }

        // ==========================================
        // FASE 1 — INÍCIO DO TURNO
        // ==========================================
        public void ExecuteUnitTurnStart(string unitId)
        {
            UnitState unit = State.GetUnitById(unitId);
            if (unit == null) return;

            // 1. Resolve SP e possível reversão automática
            EvolutionModule.OnUnitTurnStart(unit);

            // 2. Inicializa o orçamento de ações do turno
            CurrentBudget = TurnActionBudget.CreateFull(unit.canEvolveThisTurn);

            OnTurnStarted?.Invoke(unit);
        }

        // ==========================================
        // FASE 2 — AÇÕES DISPONÍVEIS
        // ==========================================
        public bool ExecuteMove(string unitId, GridCoord destination)
        {
            if (!CurrentBudget.canMove) return false;

            UnitState unit = State.GetUnitById(unitId);
            if (unit == null) return false;

            var reachable = PathfindingService.ComputeReachableCells(
                unit.coord, 
                unit.movementBudget, 
                State.grid, 
                id => State.GetUnitById(id), 
                unit.id, 
                BattleRulesConfig.DefaultGrid, 
                unit.maxClimbHeight
            );

            if (!reachable.Contains(destination)) return false;

            // Atualiza ocupação no grid
            State.grid.SetOccupant(unit.coord, null);
            unit.coord = destination;
            State.grid.SetOccupant(unit.coord, unit.id);

            var cell = State.grid.GetCell(destination.x, destination.y);
            if (cell != null) unit.coord.z = cell.coord.z;

            // Consome orçamento de movimento
            var budget = CurrentBudget;
            budget.canMove = false;
            CurrentBudget = budget;

            return true;
        }

        public bool ExecuteEvolution(string unitId, EvolutionTier targetTier)
        {
            if (!CurrentBudget.canEvolve) return false;

            UnitState unit = State.GetUnitById(unitId);
            if (unit == null) return false;

            bool success = EvolutionModule.TryEvolve(unit, targetTier);
            if (success)
            {
                var budget = CurrentBudget;
                budget.canEvolve = false;
                CurrentBudget = budget;
            }

            return success;
        }

        // ==========================================
        // FASE 3 — RESOLUÇÃO DE ATAQUE
        // ==========================================
        public int ExecuteAttack(string attackerId, string targetId, int baseAttackPower)
        {
            if (!CurrentBudget.canUseMainAction) return 0;

            UnitState attacker = State.GetUnitById(attackerId);
            UnitState target = State.GetUnitById(targetId);
            if (attacker == null || target == null) return 0;

            // Determina posição relativa (Front/Side/Back)
            RelativeCombatPosition relPos = PositionalCombatModule.GetRelativePositionGridBased(
                attacker.coord, 
                target.facing, 
                target.coord
            );

            int damage = DamageCalculator.ComputeFinalDamageDetailed(
                baseAttackPower,
                attacker.attribute,
                target.attribute,
                relPos,
                attacker.coord.z,
                target.coord.z,
                target.isGuarding,
                BattleRulesConfig.DefaultElevation
            );

            target.hp = Math.Max(0, target.hp - damage);

            // Consome ação principal
            var budget = CurrentBudget;
            budget.canUseMainAction = false;
            CurrentBudget = budget;

            DamageResolution posRes = PositionalCombatModule.ResolvePositionalDamage(relPos, baseAttackPower);
            OnAttackResolved?.Invoke(target, damage, posRes);

            // Emite evento de Karma do combate
            KarmaEventEmitter.Instance.EmitCombatAction(attacker.attribute, wasDecisive: target.hp <= 0);

            return damage;
        }

        // ==========================================
        // FASE 4 — RESOLUÇÃO DE TALK (INIMIGO / RECRUTAMENTO)
        // ==========================================
        public TalkSession StartTalkWithEnemy(string targetId)
        {
            UnitState target = State.GetUnitById(targetId);
            if (target?.talkTarget == null) return null;

            return TalkModule.StartSession(target.talkTarget);
        }

        public TalkSession AnswerTalkQuestion(TalkSession session, PersonalityTrait chosenAnswerAlignment)
        {
            if (session == null) return null;

            session = TalkModule.AnswerQuestion(session, chosenAnswerAlignment);

            if (session.phase == TalkPhase.Resolution)
            {
                if (BattleRulesConfig.DefaultTalk.talkConsumesMainAction)
                {
                    var budget = CurrentBudget;
                    budget.canUseMainAction = false;
                    CurrentBudget = budget;
                }

                UnitState target = State.GetUnitById(session.target.unitId);
                if (target != null)
                {
                    OnTalkResolved?.Invoke(target, session.result);
                }
            }

            return session;
        }

        // ==========================================
        // FASE 4 — RESOLUÇÃO DE TALK (ALIADO / BUFF)
        // ==========================================
        public AllyTalkResult ExecuteTalkWithAlly(string allyId)
        {
            if (!CurrentBudget.canUseMainAction && BattleRulesConfig.DefaultTalk.talkConsumesMainAction)
            {
                return default;
            }

            UnitState ally = State.GetUnitById(allyId);
            if (ally == null) return default;

            PersonalityTrait trait = ally.talkTarget != null ? ally.talkTarget.personality : PersonalityTrait.Brave;
            AllyTalkResult result = AllyTalkService.ResolveAllyTalk(ally.affinityWithPlayer, trait);

            if (BattleRulesConfig.DefaultTalk.talkConsumesMainAction)
            {
                var budget = CurrentBudget;
                budget.canUseMainAction = false;
                CurrentBudget = budget;
            }

            return result;
        }

        // ==========================================
        // FASE 5 — FIM DO TURNO
        // ==========================================
        public void ExecuteUnitTurnEnd(string unitId)
        {
            UnitState unit = State.GetUnitById(unitId);
            if (unit != null)
            {
                unit.isGuarding = false; // Reseta guarda no fim do turno
                OnTurnEnded?.Invoke(unit);
            }
        }
    }
}
