using System;
using UnityEngine;

namespace TacticalBattle.Core
{
    [Serializable]
    public class ElevationConfig
    {
        // TODO_DESIGN_CONFIRM: Bônus por nível de altura (padrão 10% por nível)
        public float bonusPerLevel = 0.10f;

        // TODO_DESIGN_CONFIRM: Teto máximo de bônus de altura (padrão +25%)
        public float maxBonus = 0.25f;

        // TODO_DESIGN_CONFIRM: Penalidade ao atacar de baixo para cima (padrão: 0 = neutro / 1.0x)
        public float uphillPenaltyPerLevel = 0.00f;
    }

    [Serializable]
    public class TalkConfig
    {
        // TODO_DESIGN_CONFIRM: Ganho de afinidade ao responder com personalidade alinhada
        public float positiveAffinityGain = 25f;

        // TODO_DESIGN_CONFIRM: Penalidade de afinidade ao responder desalinhado
        public float negativeAffinityPenalty = -15f;

        // TODO_DESIGN_CONFIRM: Limiar padrão para recrutamento com sucesso
        public float defaultRecruitThreshold = 70f;

        // TODO_DESIGN_CONFIRM: Limiar padrão para receber item em vez de falha
        public float defaultItemThreshold = 40f;

        // TODO_DESIGN_CONFIRM: Se o comando Talk consome a ação principal do turno
        public bool talkConsumesMainAction = true;
    }

    [Serializable]
    public class GridConfig
    {
        // TODO_DESIGN_CONFIRM: Tipo de movimentação padrão (Manhattan vs Chebyshev)
        public MovementType movementType = MovementType.Manhattan;

        // TODO_DESIGN_CONFIRM: Se unidades aliadas podem ser atravessadas durante o pathfinding
        public bool allowPassThroughAllies = true;

        // TODO_DESIGN_CONFIRM: Se ataques à distância requerem Linha de Visão (LoS) desobstruída
        public bool checkLineOfSight = false;

        // Limite máximo de escalada vertical padrão por célula
        public int defaultMaxClimbHeight = 1;
    }

    public static class BattleRulesConfig
    {
        // ==========================================
        // 3. ATRIBUTOS (TODO_DESIGN_CONFIRM)
        // ==========================================
        public const float ATTRIBUTE_ADVANTAGE_MULTIPLIER = 1.50f;    // +50% de dano
        public const float ATTRIBUTE_DISADVANTAGE_MULTIPLIER = 0.75f; // -25% de dano
        public const float ATTRIBUTE_NEUTRAL_MULTIPLIER = 1.00f;

        // ==========================================
        // 5. COMBATE POSICIONAL (TODO_DESIGN_CONFIRM)
        // ==========================================
        public const float POSITIONAL_FRONT_MULTIPLIER = 1.00f; // 100% dano base
        public const float POSITIONAL_SIDE_MULTIPLIER = 1.25f;  // 125% flanco
        public const float POSITIONAL_BACK_MULTIPLIER = 1.50f;  // 150% backstab

        public const float EVASION_FRONT_MODIFIER = 1.00f; // Evasão normal
        public const float EVASION_SIDE_MODIFIER = 0.75f;  // -25% evasão
        public const float EVASION_BACK_MODIFIER = 0.50f;  // -50% evasão

        // ==========================================
        // 4. EVOLUÇÃO (TODO_DESIGN_CONFIRM)
        // ==========================================
        // TODO_DESIGN_CONFIRM: Permitir pular mais de 1 tier por evolução voluntária
        public const bool ALLOW_MULTI_TIER_EVOLUTION_JUMP = false;

        // Instâncias padrão de configuração
        public static ElevationConfig DefaultElevation = new ElevationConfig();
        public static TalkConfig DefaultTalk = new TalkConfig();
        public static GridConfig DefaultGrid = new GridConfig();
    }
}
