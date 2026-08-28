using System;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Combat
{
    public static class PositionalCombatModule
    {
        // ==========================================
        // 5.1 DETERMINAÇÃO DA POSIÇÃO RELATIVA DE ATAQUE
        // ==========================================
        public static GridFacing GetOppositeFacing(GridFacing facing)
        {
            return facing switch
            {
                GridFacing.North => GridFacing.South,
                GridFacing.South => GridFacing.North,
                GridFacing.East => GridFacing.West,
                GridFacing.West => GridFacing.East,
                _ => GridFacing.South
            };
        }

        // Converte o vetor (alvo -> atacante) na direção cardinal mais próxima
        public static GridFacing GetCardinalDirectionFrom(GridCoord from, GridCoord to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx >= 0 ? GridFacing.East : GridFacing.West;
            }
            else
            {
                return dy >= 0 ? GridFacing.North : GridFacing.South;
            }
        }

        // Determinação determinística por células no grid ortogonal (livre de ambiguidade de ângulos)
        public static RelativeCombatPosition GetRelativePositionGridBased(
            GridCoord attackerCoord, 
            GridFacing defenderFacing, 
            GridCoord defenderCoord)
        {
            if (attackerCoord.x == defenderCoord.x && attackerCoord.y == defenderCoord.y)
            {
                return RelativeCombatPosition.Front;
            }

            GridFacing attackDirection = GetCardinalDirectionFrom(defenderCoord, attackerCoord);
            GridFacing opposite = GetOppositeFacing(defenderFacing);

            if (attackDirection == defenderFacing)
            {
                return RelativeCombatPosition.Front; // O atacante está na frente do defensor
            }
            if (attackDirection == opposite)
            {
                return RelativeCombatPosition.Back; // O atacante está atrás do defensor (Backstab)
            }

            return RelativeCombatPosition.Side; // As duas direções perpendiculares restantes (Flanco)
        }

        // ==========================================
        // 5.2 RESOLUÇÃO POSICIONAL (Multiplicador, Crítico, Guarda)
        // ==========================================
        public static DamageResolution ResolvePositionalDamage(RelativeCombatPosition position, int baseDamage)
        {
            float multiplier = position switch
            {
                RelativeCombatPosition.Back => BattleRulesConfig.POSITIONAL_BACK_MULTIPLIER,   // 1.50 (150%)
                RelativeCombatPosition.Side => BattleRulesConfig.POSITIONAL_SIDE_MULTIPLIER,   // 1.25 (125%)
                _ => BattleRulesConfig.POSITIONAL_FRONT_MULTIPLIER                             // 1.00 (100%)
            };

            float evasionMod = position switch
            {
                RelativeCombatPosition.Back => BattleRulesConfig.EVASION_BACK_MODIFIER,        // 0.50
                RelativeCombatPosition.Side => BattleRulesConfig.EVASION_SIDE_MODIFIER,        // 0.75
                _ => BattleRulesConfig.EVASION_FRONT_MODIFIER                                  // 1.00
            };

            return new DamageResolution
            {
                baseDamage = baseDamage,
                positionalMultiplier = multiplier,
                isCriticalGuaranteed = position == RelativeCombatPosition.Back,
                ignoresGuardStance = position == RelativeCombatPosition.Back,
                evasionModifier = evasionMod
            };
        }

        // ==========================================
        // 5.3 VANTAGEM DE ELEVAÇÃO
        // ==========================================
        public static float GetElevationMultiplier(int attackerZ, int defenderZ, ElevationConfig config = null)
        {
            config ??= BattleRulesConfig.DefaultElevation;
            int heightDiff = attackerZ - defenderZ;

            if (heightDiff <= 0)
            {
                if (config.uphillPenaltyPerLevel > 0f)
                {
                    float penalty = Math.Abs(heightDiff) * config.uphillPenaltyPerLevel;
                    return Math.Max(0.5f, 1.0f - penalty);
                }
                return 1.0f; // Neutro se atacante está no mesmo nível ou abaixo
            }

            // Bônus linear com teto configurável
            float bonus = Math.Min(heightDiff * config.bonusPerLevel, config.maxBonus);
            return 1.0f + bonus;
        }
    }
}
