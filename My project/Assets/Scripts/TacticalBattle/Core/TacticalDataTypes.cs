using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalBattle.Core
{
    // ==========================================
    // 2. GRID & ORIENTAÇÃO
    // ==========================================
    public enum TacticalTeam
    {
        Player,
        Enemy,
        Neutral
    }

    public enum MovementType
    {
        Manhattan, // 4 direções cardinais (|dx| + |dy|)
        Chebyshev  // 8 direções incluindo diagonais (max(|dx|, |dy|))
    }

    public enum GridFacing
    {
        North = 0, // +Y no grid (para cima/norte)
        East = 1,  // +X no grid (para direita/leste)
        South = 2, // -Y no grid (para baixo/sul)
        West = 3   // -X no grid (para esquerda/oeste)
    }

    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int x;
        public int y;
        public int z; // Altura / elevação do terreno

        public GridCoord(int x, int y, int z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public bool Equals(GridCoord other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, z:{z})";
        }

        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
    }

    public enum AttackShapeType
    {
        Single, // Alvo único com minRange e maxRange
        Line,   // Linha reta na direção cardinal
        Cone,   // Cone angular a partir da unidade
        Area    // Área de efeito centrada no ponto-alvo
    }

    [Serializable]
    public struct AttackRangeShape
    {
        public AttackShapeType type;
        public int minRange;
        public int maxRange;
        public int length;        // Para formato 'Line'
        public float angleDegrees;// Para formato 'Cone'
        public int radius;        // Para formato 'Area' ou 'Cone'

        public static AttackRangeShape CreateSingle(int minRange, int maxRange)
        {
            return new AttackRangeShape { type = AttackShapeType.Single, minRange = minRange, maxRange = maxRange };
        }

        public static AttackRangeShape CreateLine(int length)
        {
            return new AttackRangeShape { type = AttackShapeType.Line, length = length, minRange = 1, maxRange = length };
        }

        public static AttackRangeShape CreateArea(int radius, int minRange = 0, int maxRange = 3)
        {
            return new AttackRangeShape { type = AttackShapeType.Area, radius = radius, minRange = minRange, maxRange = maxRange };
        }

        public static AttackRangeShape CreateCone(int radius, float angleDegrees = 60f)
        {
            return new AttackRangeShape { type = AttackShapeType.Cone, radius = radius, angleDegrees = angleDegrees, minRange = 1, maxRange = radius };
        }
    }

    // ==========================================
    // 3. ATRIBUTOS & KARMA
    // ==========================================
    public enum TacticalAttribute
    {
        Vaccine, // Moral
        Data,    // Harmonia
        Virus,   // Cólera
        Free     // Neutro (sem vantagens/desvantagens)
    }

    public enum KarmaEventType
    {
        TalkChoiceMade,
        UnitRecruited,
        CombatAction
    }

    public struct KarmaEvent
    {
        public KarmaEventType type;
        public TacticalAttribute attribute;
        public float weight;
        public bool wasDecisive;
    }

    // ==========================================
    // 4. EVOLUÇÃO & SP
    // ==========================================
    public enum EvolutionTier
    {
        Rookie = 0,   // Criança (forma base, sem custo de SP)
        Champion = 1, // Adulto
        Ultimate = 2, // Perfeito
        Mega = 3      // Extremo
    }

    // ==========================================
    // 5. COMBATE POSICIONAL
    // ==========================================
    public enum RelativeCombatPosition
    {
        Front, // Dano padrão (100%)
        Side,  // Flanco (125%)
        Back   // Traseiro (150% + Crítico garantido + Ignora guarda)
    }

    [Serializable]
    public struct DamageResolution
    {
        public int baseDamage;
        public float positionalMultiplier;
        public bool isCriticalGuaranteed; // true apenas em BACK
        public bool ignoresGuardStance;   // true apenas em BACK
        public float evasionModifier;     // Modificador de evasão do alvo
    }

    // ==========================================
    // 6. TALK SYSTEM (DIÁLOGO / RECRUTAMENTO)
    // ==========================================
    public enum PersonalityTrait
    {
        Brave,      // Corajoso
        Cautious,   // Cauteloso
        Kind,       // Gentil
        Aggressive, // Agressivo
        Logical     // Lógico
    }

    public enum TalkPhase
    {
        Question1 = 1,
        Question2 = 2,
        Question3 = 3,
        Resolution = 4
    }

    public enum TalkResult
    {
        Pending,
        Recruited,
        ItemGranted,
        Failed
    }

    public enum AllyBuffType
    {
        AttackUp,
        SpRegen,
        HpRegen,
        InstantHeal
    }

    [Serializable]
    public struct AllyTalkResult
    {
        public AllyBuffType buffType;
        public int magnitude;
        public int durationTurns; // 0 para instantâneo
    }
}
