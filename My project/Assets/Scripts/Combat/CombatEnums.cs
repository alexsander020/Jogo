using System;
using UnityEngine;

public enum FacingDirection
{
    North = 0, // +Y no grid
    East = 1,  // +X no grid
    South = 2, // -Y no grid
    West = 3   // -X no grid
}

public enum AttackOrientation
{
    Frontal,  // Dano padrão: 100%
    Flank,    // Dano lateral: 125%
    Backstab  // Dano traseiro: 150% + Crítico garantido
}

// 7 Atributos Funcionais de NetShift
public enum FunctionalCategory
{
    Social,        // Buffs de Grupo / CC (vence Navi)
    Navi,          // Precisão, Evasão e Velocidade (vence Tool)
    Tool,          // Tanque / Dano Físico (vence Game)
    Game,          // Crítico / Multi-hit (vence Entertainment)
    Entertainment, // Debuffs / Provocação (vence Life)
    Life,          // Cura / Regeneração (vence System)
    System         // Negação / Dano Puro / Hack (vence Social)
}

// Trindade de Protocolo (Alinhamento Moral / Afinidade)
public enum ProtocolTrinity
{
    Firewall,  // Moral / Proteção (Ordem, retidão)
    Ping,      // Harmonia / Equilíbrio (Pragmatismo, diplomacia)
    Overclock  // Cólera / Ruptura (Impulso, revolta contra o sistema)
}

// Estágios de Evolução / NetFusion
public enum EvolutionRank
{
    Standard,
    Super,
    Ultimate,
    God
}

// Facção / Time em campo
public enum Team
{
    Player,
    Enemy,
    Neutral
}
