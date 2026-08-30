using System;
using TacticalBattle.Attribute;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Combat
{
    public static class DamageCalculator
    {
        // ==========================================
        // 5.4 COMPOSIÇÃO FINAL DO DANO
        // ==========================================
        // Multiplicadores em cadeia: Atributo * Posição * Elevação
        public static int ComputeFinalDamage(
            int baseDamage,
            TacticalAttribute attackerAttribute,
            TacticalAttribute defenderAttribute,
            RelativeCombatPosition position,
            int attackerZ,
            int defenderZ,
            ElevationConfig elevationConfig = null)
        {
            if (baseDamage <= 0) return 0;

            float attributeMult = AttributeModule.GetAttributeMultiplier(attackerAttribute, defenderAttribute);
            DamageResolution positionalRes = PositionalCombatModule.ResolvePositionalDamage(position, baseDamage);
            float elevationMult = PositionalCombatModule.GetElevationMultiplier(attackerZ, defenderZ, elevationConfig);

            float finalDamage = baseDamage * attributeMult * positionalRes.positionalMultiplier * elevationMult;

            // Dano final sempre arredondado para baixo e nunca negativo
            return Math.Max(0, (int)Math.Floor(finalDamage));
        }

        // Sobrecarga completa considerando guarda, bônus de cobertura de terreno e crítico adicional
        public static int ComputeFinalDamageDetailed(
            int baseDamage,
            TacticalAttribute attackerAttribute,
            TacticalAttribute defenderAttribute,
            RelativeCombatPosition position,
            int attackerZ,
            int defenderZ,
            bool isDefenderGuarding,
            ElevationConfig elevationConfig = null,
            float guardDamageReduction = 0.5f,
            float terrainDefenseReduction = 0.0f)
        {
            if (baseDamage <= 0) return 0;

            float attributeMult = AttributeModule.GetAttributeMultiplier(attackerAttribute, defenderAttribute);
            DamageResolution positionalRes = PositionalCombatModule.ResolvePositionalDamage(position, baseDamage);
            float elevationMult = PositionalCombatModule.GetElevationMultiplier(attackerZ, defenderZ, elevationConfig);

            float guardMult = 1.0f;
            if (isDefenderGuarding && !positionalRes.ignoresGuardStance)
            {
                guardMult = Math.Clamp(1.0f - guardDamageReduction, 0.1f, 1.0f);
            }

            // Redução de dano por cobertura de terreno (ex: Barricada -20% de dano recebido)
            float terrainMult = Math.Clamp(1.0f - terrainDefenseReduction, 0.1f, 1.0f);

            float finalDamage = baseDamage * attributeMult * positionalRes.positionalMultiplier * elevationMult * guardMult * terrainMult;

            return Math.Max(0, (int)Math.Floor(finalDamage));
        }
    }
}
