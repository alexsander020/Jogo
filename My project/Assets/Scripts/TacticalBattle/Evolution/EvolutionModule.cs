using System;
using TacticalBattle.Core;

namespace TacticalBattle.Evolution
{
    public static class EvolutionModule
    {
        public static event Action<string, EvolutionTier> OnEvolutionSuccess;
        public static event Action<string> OnForcedDevolution;

        // ==========================================
        // 4.2 ORDEM DE EXECUÇÃO NO INÍCIO DO TURNO
        // ==========================================
        public static void OnUnitTurnStart(UnitState unit)
        {
            if (unit == null || unit.evolution == null) return;

            // Invariante do sistema: Rookie NUNCA consome SP
            unit.evolution.spCostPerTurn[EvolutionTier.Rookie] = 0;

            // 1. Verificar se a unidade está em forma evoluída
            if (unit.evolution.currentTier != EvolutionTier.Rookie)
            {
                int cost = 0;
                if (unit.evolution.spCostPerTurn.TryGetValue(unit.evolution.currentTier, out int tierCost))
                {
                    cost = tierCost;
                }

                // 2. Consumir SP
                unit.evolution.currentSP = Math.Max(0, unit.evolution.currentSP - cost);

                // 3. Verificar reversão automática (regra: SP chegou a 0 ou menos)
                if (unit.evolution.currentSP <= 0)
                {
                    RevertToRookie(unit);
                    OnForcedDevolution?.Invoke(unit.id);
                }
            }

            // 4. Somente depois de resolver SP, liberar a ação de evolução voluntária
            unit.canEvolveThisTurn = true;
        }

        // ==========================================
        // 4.3 ATIVAÇÃO DE EVOLUÇÃO ("Ação Inercial")
        // ==========================================
        public static bool CanEvolve(UnitState unit, EvolutionTier targetTier, bool allowMultiTier = BattleRulesConfig.ALLOW_MULTI_TIER_EVOLUTION_JUMP)
        {
            if (unit == null || unit.evolution == null) return false;
            if (!unit.canEvolveThisTurn) return false;
            if (targetTier <= unit.evolution.currentTier) return false;

            // Regra: Não pode pular mais de 1 tier por ativação sem autorização de design
            if (!allowMultiTier && (int)targetTier > (int)unit.evolution.currentTier + 1)
            {
                return false;
            }

            // Regra: Requer SP suficiente para se manter no primeiro turno do tier-alvo
            int requiredSP = 0;
            if (unit.evolution.spCostPerTurn.TryGetValue(targetTier, out int cost))
            {
                requiredSP = cost;
            }

            if (unit.evolution.currentSP < requiredSP)
            {
                return false;
            }

            return true;
        }

        public static bool TryEvolve(UnitState unit, EvolutionTier targetTier, bool allowMultiTier = BattleRulesConfig.ALLOW_MULTI_TIER_EVOLUTION_JUMP)
        {
            if (!CanEvolve(unit, targetTier, allowMultiTier))
            {
                return false;
            }

            unit.evolution.currentTier = targetTier;
            unit.canEvolveThisTurn = false; // Apenas 1 evolução voluntária por turno

            // Atualiza os stats de combate a partir da tabela base
            EvolutionStatsTable.ApplyBaseStatsForTier(unit, targetTier);

            OnEvolutionSuccess?.Invoke(unit.id, targetTier);
            return true;
        }

        // ==========================================
        // 4.4 REVERSÃO AUTOMÁTICA
        // ==========================================
        public static void RevertToRookie(UnitState unit)
        {
            if (unit == null || unit.evolution == null) return;

            unit.evolution.currentTier = EvolutionTier.Rookie;
            unit.evolution.currentSP = Math.Max(0, unit.evolution.currentSP);

            // Reaplicar stats-base da forma Rookie sem deltas cumulativos
            EvolutionStatsTable.ApplyBaseStatsForTier(unit, EvolutionTier.Rookie);
        }
    }
}
