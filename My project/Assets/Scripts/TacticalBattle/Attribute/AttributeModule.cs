using System.Collections.Generic;
using TacticalBattle.Core;

namespace TacticalBattle.Attribute
{
    public static class AttributeModule
    {
        // ==========================================
        // 3.2 MATRIZ DE VANTAGEM (TRIÂNGULO DE TIPOS)
        // ==========================================
        // Vacina > Vírus > Dados > Vacina | Free = Neutro
        private static readonly Dictionary<TacticalAttribute, TacticalAttribute?> AdvantageMatrix = new Dictionary<TacticalAttribute, TacticalAttribute?>
        {
            { TacticalAttribute.Vaccine, TacticalAttribute.Virus },  // Vacina é forte contra Vírus
            { TacticalAttribute.Virus,   TacticalAttribute.Data },   // Vírus é forte contra Dados
            { TacticalAttribute.Data,    TacticalAttribute.Vaccine },// Dados é forte contra Vacina
            { TacticalAttribute.Free,    null }                      // Sem vantagem
        };

        public static bool HasAdvantage(TacticalAttribute attacker, TacticalAttribute defender)
        {
            if (attacker == TacticalAttribute.Free || defender == TacticalAttribute.Free) return false;
            return AdvantageMatrix.TryGetValue(attacker, out var target) && target == defender;
        }

        public static bool HasDisadvantage(TacticalAttribute attacker, TacticalAttribute defender)
        {
            if (attacker == TacticalAttribute.Free || defender == TacticalAttribute.Free) return false;
            return AdvantageMatrix.TryGetValue(defender, out var target) && target == attacker;
        }

        public static float GetAttributeMultiplier(TacticalAttribute attacker, TacticalAttribute defender)
        {
            if (attacker == TacticalAttribute.Free || defender == TacticalAttribute.Free)
            {
                return BattleRulesConfig.ATTRIBUTE_NEUTRAL_MULTIPLIER;
            }

            if (HasAdvantage(attacker, defender))
            {
                return BattleRulesConfig.ATTRIBUTE_ADVANTAGE_MULTIPLIER; // +50% (1.50)
            }

            if (HasDisadvantage(attacker, defender))
            {
                return BattleRulesConfig.ATTRIBUTE_DISADVANTAGE_MULTIPLIER; // -25% (0.75)
            }

            return BattleRulesConfig.ATTRIBUTE_NEUTRAL_MULTIPLIER; // Mesmo atributo ou sem relação direta (1.00)
        }
    }
}
